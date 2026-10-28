using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ReplayMod;

internal static class HandPresentation
{
    internal sealed record Changes(int Reused,int Created,int Removed);
    private static void KillTween(object owner,string field)
    {
        if(AccessTools.Field(owner.GetType(),field).GetValue(owner) is Tween tween&&GodotObject.IsInstanceValid(tween))tween.Kill();
    }
    internal static Changes Synchronize(NPlayerHand hand,IReadOnlyList<CardModel> expected)
    {
        Codec.Require(!hand.InCardPlay&&!hand.IsInCardSelection,"direct-back-hand-ui-busy");
        AccessTools.Property(typeof(NPlayerHand),nameof(NPlayerHand.FocusedHolder)).SetValue(hand,null);
        var wanted=new HashSet<CardModel>(expected,ReferenceEqualityComparer.Instance);
        var keep=new Dictionary<CardModel,NHandCardHolder>(ReferenceEqualityComparer.Instance);var removed=0;var created=0;
        foreach(var holder in hand.CardHolderContainer.GetChildren().OfType<NHandCardHolder>().ToArray())
        {
            if(holder.CardModel is {} model&&wanted.Contains(model)&&keep.TryAdd(model,holder))continue;
            var removedCard=holder.CardNode;hand.RemoveCardHolder(holder);
            if(removedCard is not null)removedCard.QueueFreeSafely();removed++;
        }
        var reused=keep.Count;
        for(var i=0;i<expected.Count;i++)
        {
            var model=expected[i];
            if(!keep.TryGetValue(model,out var holder))
            {
                var card=NCard.Create(model)??throw new InvalidDataException("direct-back-card-ui-create");
                card.Scale=Vector2.One; // Avoid NHandCardHolder.SetCard's automatic scale-in tween.
                holder=hand.Add(card);created++;
            }
            hand.CardHolderContainer.MoveChild(holder,i);holder.UpdateCard();
        }
        AccessTools.Method(typeof(NPlayerHand),"RefreshLayout").Invoke(hand,null);
        Snap(hand);return new(reused,created,removed);
    }
    internal static void Snap(NPlayerHand hand)
    {
        KillTween(hand,"_animInTween");KillTween(hand,"_animOutTween");KillTween(hand,"_animEnableTween");
        AccessTools.Field(typeof(NPlayerHand),"_isDisabled").SetValue(hand,false);
        hand.Position=Vector2.Zero;hand.Modulate=Colors.White;
        var holders=hand.CardHolderContainer.GetChildren().OfType<NHandCardHolder>().ToArray();
        for(var i=0;i<holders.Length;i++)
        {
            var h=holders[i];var position=HandPosHelper.GetPosition(holders.Length,i);var angle=HandPosHelper.GetAngle(holders.Length,i);var scale=HandPosHelper.GetScale(holders.Length);
            KillTween(h,"_hoverTween");AccessTools.Method(typeof(NHandCardHolder),"StopAnimations").Invoke(h,null);
            AccessTools.Field(typeof(NHandCardHolder),"_targetPosition").SetValue(h,position);
            AccessTools.Field(typeof(NHandCardHolder),"_targetAngle").SetValue(h,angle);
            AccessTools.Field(typeof(NHandCardHolder),"_targetScale").SetValue(h,scale);
            h.Position=position;h.SetAngleInstantly(angle);h.SetScaleInstantly(scale);h.Modulate=Colors.White;h.Visible=true;
            h.SetIndexLabel(i+1);
            h.Hitbox.SetEnabled(true);
            if(h.CardNode is {} card)
            {
                if(card.PlayPileTween is {} tween&&GodotObject.IsInstanceValid(tween))tween.Kill();card.PlayPileTween=null;
                card.Position=Vector2.Zero;card.Rotation=0;card.Scale=Vector2.One;card.Modulate=Colors.White;
            }
        }
    }
    internal static bool Matched(NPlayerHand hand,IReadOnlyList<CardModel> expected)
    {
        var holders=hand.CardHolderContainer.GetChildren().OfType<NHandCardHolder>().ToArray();
        return IndicesMatched(hand)&&holders.Select(h=>h.CardModel).SequenceEqual(expected)&&holders.Select((h,i)=>h.Position.IsEqualApprox(HandPosHelper.GetPosition(holders.Length,i))&&
            Mathf.IsEqualApprox(h.RotationDegrees,HandPosHelper.GetAngle(holders.Length,i))&&h.Scale.IsEqualApprox(HandPosHelper.GetScale(holders.Length))).All(v=>v);
    }
    internal static bool IndicesMatched(NPlayerHand hand)=>hand.CardHolderContainer.GetChildren().OfType<NHandCardHolder>().Select((h,i)=>
        ((MegaCrit.Sts2.addons.mega_text.MegaLabel)AccessTools.Field(typeof(NHandCardHolder),"_handIndexLabel").GetValue(h)!).Text==(i+1).ToString()).All(v=>v);
    internal static void FinishHighlights(NPlayerHand hand)
    {
        var completed=0;var states=new List<object>();
        foreach(var holder in hand.CardHolderContainer.GetChildren().OfType<NHandCardHolder>())
        {
            if(holder.CardNode is not {} card)continue;
            var highlight=card.CardHighlight;
            var tween=AccessTools.Field(typeof(NCardHighlight),"_curTween").GetValue(highlight) as Tween;
            if(tween is not null&&GodotObject.IsInstanceValid(tween)&&tween.IsRunning())
            {
                // Fixed native show/hide lasts .5s, flash .45s. Finish the existing shader-only
                // tween through its own endpoint; keep native eligibility, width and color rules.
                tween.CustomStep(1);completed++;
            }
            states.Add(new{Model=card.Model?.Id.ToString(),Width=((ShaderMaterial)highlight.Material).GetShaderParameter("width").AsSingle(),
                TweenRunning=tween is not null&&GodotObject.IsInstanceValid(tween)&&tween.IsRunning()});
        }
        Runtime.Log("back-hand-highlights-settled",new{Cards=states.Count,CompletedNativeTweens=completed,States=states,
            InstantAtBackArrival=true,NativeEligibilityAndColorsPreserved=true,CardVisualsRebuilt=false});
    }
    internal static void FinishHighlight(NCard card)
    {
        if(AccessTools.Field(typeof(NCardHighlight),"_curTween").GetValue(card.CardHighlight) is Tween tween&&
            GodotObject.IsInstanceValid(tween)&&tween.IsRunning())tween.CustomStep(1);
    }
    internal static void ContinueFrom(NHandCardHolder target,NHandCardHolder display)
    {
        var position=target.TargetPosition;var angle=target.TargetAngle;
        var scale=(Vector2)AccessTools.Field(typeof(NHandCardHolder),"_targetScale").GetValue(target)!;
        AccessTools.Method(typeof(NHandCardHolder),"StopAnimations").Invoke(target,null);
        var shownTransform=display.GetGlobalTransform();var parent=((Control)target.GetParent()).GetGlobalTransform();
        target.GlobalPosition=display.GlobalPosition;target.Rotation=shownTransform.Rotation-parent.Rotation;target.Scale=shownTransform.Scale/parent.Scale;
        if(target.CardNode is {} card&&display.CardNode is {} shown)
        {card.Position=shown.Position;card.Rotation=shown.Rotation;card.Scale=shown.Scale;}
        target.SetTargetPosition(position);target.SetTargetAngle(angle);target.SetTargetScale(scale);
    }
}
