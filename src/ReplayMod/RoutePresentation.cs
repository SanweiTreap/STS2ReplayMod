using System.Collections;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;

namespace ReplayMod;

// Display only. History never calls CardPileCmd, invokes pile events, or resumes an old UI task.
internal static class RoutePresentation
{
    private static object? Get(object owner,string name)=>AccessTools.Field(owner.GetType(),name).GetValue(owner);
    private static void Kill(object owner,string name)
    {
        if(Get(owner,name) is Tween tween&&GodotObject.IsInstanceValid(tween))tween.Kill();
    }
    internal static void CloseBrowser()
    {
        if(NCapstoneContainer.Instance?.CurrentCapstoneScreen is NCardPileScreen)NCapstoneContainer.Instance.Close();
    }
    private static IEnumerable<(NCombatCardPile Button,PileType Type)> Piles()
    {
        var ui=NCombatRoom.Instance!.Ui;
        return new (NCombatCardPile,PileType)[]{(ui.DrawPile,PileType.Draw),(ui.DiscardPile,PileType.Discard),(ui.ExhaustPile,PileType.Exhaust)};
    }
    internal static void Access()
    {
        if(!DecisionRoute.Enabled||NCombatRoom.Instance is null)return;
        var hand=NPlayerHand.Instance!;
        var browse=DecisionRoute.Ready&&(!hand.IsInCardSelection||hand.PeekButton.IsPeeking)&&!DecisionRoute.HasOverlay;
        foreach(var (button,type) in Piles())
        {
            var enabled=browse&&(type!=PileType.Exhaust||type.GetPile(LiveState.Player).Cards.Count>0);
            if(button.IsEnabled!=enabled)button.SetEnabled(enabled);
        }
    }
    internal static void Browsed(NCombatCardPile button)
    {
        if(!DecisionRoute.Enabled)return;
        var pile=(CardPile?)Get(button,"_pile");
        Runtime.Log("route-pile-browse",new{Pile=pile?.Type.ToString(),Count=pile?.Cards.Count,Display=(int)Get(button,"_currentCount")!,
            button.IsEnabled,Opened=NCapstoneContainer.Instance?.CurrentCapstoneScreen is NCardPileScreen,ReadOnly=true});
    }
    internal static void Synchronize(bool continuePresentation=false)
    {
        var ui=NCombatRoom.Instance!.Ui;
        // A native peek tween can otherwise move an old played card after the history restore.
        Kill(ui,"_playContainerPeekModeTween");
        ((IDictionary)Get(ui,"_originalPlayContainerCardPositions")!).Clear();
        ((IDictionary)Get(ui,"_originalPlayContainerCardScales")!).Clear();
        var expected=PileType.Play.GetPile(LiveState.Player).Cards;
        var keep=new Dictionary<CardModel,NCard>(ReferenceEqualityComparer.Instance);
        foreach(var card in ui.PlayContainer.GetChildren().OfType<NCard>().Concat(ui.GetChildren().OfType<NCard>()).ToArray())
        {
            card.PlayPileTween?.Kill();card.PlayPileTween=null;
            if(card.Model is {} model&&expected.Contains(model)&&keep.TryAdd(model,card))continue;
            card.QueueFreeSafely();
        }
        for(var i=0;i<expected.Count;i++)
        {
            var model=expected[i];
            if(!keep.TryGetValue(model,out var card))card=NCard.Create(model)??throw new InvalidDataException("route-play-card-create");
            if(card.GetParent()!=ui.PlayContainer)ui.AddToPlayContainer(card);
            ui.PlayContainer.MoveChild(card,i);
            card.UpdateVisuals(PileType.Play,CardPreviewMode.Normal);
            card.Position=PileType.Play.GetTargetPosition(card);card.Scale=Vector2.One*.8f;card.Rotation=0;
            card.Modulate=Colors.White;card.Visible=true;
        }
        ui.PlayContainer.Modulate=Colors.White;
        foreach(var (button,type) in Piles())
        {
            var count=type.GetPile(LiveState.Player).Cards.Count;
            AccessTools.Field(typeof(NCombatCardPile),"_currentCount").SetValue(button,count);
            var label=(MegaLabel)Get(button,"_countLabel")!;label.SetTextAutoSize(count.ToString());
            // A semantic arrival does not end the native add/entrance pulse. Direct Back still
            // restores a stable endpoint; forward publication must leave those display tails alone.
            if(continuePresentation)continue;
            Kill(button,"_positionTween");Kill(button,"_bumpTween");label.Scale=Vector2.One;
            var icon=(Control)Get(button,"_icon")!;icon.Scale=Vector2.One;icon.Modulate=Colors.White;
            button.Position=(Vector2)Get(button,"_showPosition")!;button.Visible=type!=PileType.Exhaust||count>0;
        }
    }
    internal static object Check()
    {
        var ui=NCombatRoom.Instance!.Ui;
        var play=ui.PlayContainer.GetChildren().OfType<NCard>().ToArray();
        var expected=PileType.Play.GetPile(LiveState.Player).Cards;
        Codec.Require(play.Select(c=>c.Model).ToHashSet().SetEquals(expected)&&play.Length==expected.Count,"route-play-container-membership");
        Codec.Require(ui.GetChildren().OfType<NCard>().Count()==0,"route-floating-card-outside-play-container");
        var piles=Piles().Select(p=>new{Pile=p.Type.ToString(),Actual=p.Type.GetPile(LiveState.Player).Cards.Count,
            Display=(int)Get(p.Button,"_currentCount")!,Text=((MegaLabel)Get(p.Button,"_countLabel")!).Text,p.Button.IsEnabled}).ToArray();
        Codec.Require(piles.All(p=>p.Actual==p.Display&&p.Text==p.Actual.ToString()),"route-pile-counter-mismatch");
        return new{PlayCards=play.Select(c=>c.Model!.Id.ToString()).ToArray(),NoFloatingCards=true,Piles=piles};
    }
}
