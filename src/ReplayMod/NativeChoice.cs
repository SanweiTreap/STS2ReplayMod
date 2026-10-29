using System.Collections;
using System.Diagnostics;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;

namespace ReplayMod;

// Drive the same native decision endpoints as mouse/keyboard, with one submission path.
// Hover, scrolling, inspection and grid virtualization remain native and do not touch the tape.
internal static class NativeChoice
{
    private static bool _driving;
    private static int _inputDepth;
    private static object? Field(object o,string name)=>AccessTools.Field(o.GetType(),name)?.GetValue(o);
    private static object? Call(object o,string name,params object?[] args)=>AccessTools.Method(o.GetType(),name).Invoke(o,args);
    internal static void Install(Harmony h)
    {
        h.Patch(AccessTools.Method(typeof(NOverlayStack),"Push"),postfix:new HarmonyMethod(typeof(NativeChoice),nameof(OverlayOpened)));
        h.Patch(AccessTools.Method(typeof(NOverlayStack),"Remove"),prefix:new HarmonyMethod(typeof(NativeChoice),nameof(OverlayClosed)),postfix:new HarmonyMethod(typeof(NativeChoice),nameof(OverlayClosureSettled)));
        h.Patch(AccessTools.Method(typeof(NPlayerHand),"SelectCards"),postfix:new HarmonyMethod(typeof(NativeChoice),nameof(HandOpened)));
        h.Patch(AccessTools.Method(typeof(NPlayerHand),"AfterCardsSelected"),prefix:new HarmonyMethod(typeof(NativeChoice),nameof(HandClosed)),postfix:new HarmonyMethod(typeof(NativeChoice),nameof(HandClosureSettled)));
        var decisions=new (Type Type,string[] Methods)[]
        {
            (typeof(NChooseACardSelectionScreen),["SelectHolder","OnSkipButtonReleased"]),
            (typeof(NCombatPileCardSelectScreen),["OnCardClicked","CompleteSelection"]),
            (typeof(NSimpleCardSelectScreen),["OnCardClicked","CompleteSelection"]),
            (typeof(NPlayerHand),["SelectCardInSimpleMode","SelectCardInUpgradeMode","DeselectCard","OnSelectModeConfirmButtonPressed"]),
            (typeof(NChooseABundleSelectionScreen),["OnBundleClicked","CancelSelection","ConfirmSelection"])
        };
        foreach(var (type,methods) in decisions)foreach(var name in methods)
            h.Patch(AccessTools.DeclaredMethod(type,name),new HarmonyMethod(typeof(NativeChoice),nameof(Decision)),
                finalizer:new HarmonyMethod(typeof(NativeChoice),nameof(DecisionEnded)));
        // A selected holder must not be removed when its downstream DeselectCard is blocked.
        h.Patch(AccessTools.Method(typeof(NSelectedHandCardContainer),"DeselectHolder"),new HarmonyMethod(typeof(NativeChoice),nameof(AllowDecision)));
    }
    private static void OverlayOpened(IOverlayScreen __0)
    {
        if(__0 is NChooseACardSelectionScreen or NCombatPileCardSelectScreen or NSimpleCardSelectScreen or NChooseABundleSelectionScreen)
            Runtime.Guard(()=>ChoiceTape.Opened(__0));
        else if(ChoiceTape.Watching&&ChoiceTape.Waiting&&__0 is NCardGridSelectionScreen)
            Runtime.Guard(()=>throw new InvalidDataException("native-deck-preview-process-not-supported-yet"));
    }
    private static void HandOpened(NPlayerHand __instance)=>Runtime.Guard(()=>ChoiceTape.Opened(__instance));
    private static void OverlayClosed(IOverlayScreen __0)=>Runtime.Guard(()=>ChoiceTape.Closed(__0));
    private static void HandClosed(NPlayerHand __instance)=>Runtime.Guard(()=>ChoiceTape.Closed(__instance));
    private static void OverlayClosureSettled(IOverlayScreen __0)=>ChoiceTape.ClosureSettled(__0);
    private static void HandClosureSettled(NPlayerHand __instance)=>ChoiceTape.ClosureSettled(__instance);
    private static bool AllowDecision()=>!ChoiceTape.Watching||_driving;
    private static bool Decision(object __instance,MethodBase __originalMethod,object[] __args,out bool __state)
    {
        __state=false;if(!AllowDecision())return false;
        if(!ChoiceTape.Recording||ChoiceTape.Scope is not {} s||!ReferenceEquals(s.Ui,__instance))return true;
        var nested=_inputDepth++>0;__state=true;if(nested)return true;
        Runtime.Guard(()=>
        {
            var name=__originalMethod.Name;
            int Index(CardModel? card)=>Array.FindIndex(s.Options,o=>o.Length==1&&ReferenceEquals(o[0],card));
            if(name=="SelectHolder")
            {
                var opened=(ulong)Field(__instance,"_openedTicks")!;
                if(Time.GetTicksMsec()-opened>350)ChoiceTape.Event("Select",Index(((NCardHolder)__args[0]).CardModel));
            }
            else if(name=="OnCardClicked")
            {
                var card=(CardModel)__args[0];var selected=((IEnumerable<CardModel>)Field(__instance,"_selectedCards")!).ToArray();
                if(selected.Contains(card))ChoiceTape.Event("Deselect",Index(card));
                else if(selected.Length<s.Frame.Max)ChoiceTape.Event("Select",Index(card));
            }
            else if(name.StartsWith("SelectCardIn",StringComparison.Ordinal))
            {
                var selected=((IEnumerable<CardModel>)Field(__instance,"_selectedCards")!).ToArray();
                if(selected.Length>=s.Frame.Max)ChoiceTape.Event("Deselect",Index(selected[^1]));
                ChoiceTape.Event("Select",Index(((NCardHolder)__args[0]).CardModel));
            }
            else if(name=="DeselectCard")ChoiceTape.Event("Deselect",Index(((NCard)__args[0]).Model));
            else if(name=="OnSkipButtonReleased")ChoiceTape.Event("Skip");
            else if(name=="CancelSelection")ChoiceTape.Event("Cancel");
            else if(name=="OnBundleClicked")
            {
                var bundle=(IReadOnlyList<CardModel>)AccessTools.Property(__args[0].GetType(),"Bundle").GetValue(__args[0])!;
                ChoiceTape.Event("Select",Array.FindIndex(s.Options,b=>b.SequenceEqual(bundle)));
            }
            else ChoiceTape.Event("Confirm");
        });
        return true;
    }
    private static void DecisionEnded(bool __state){if(__state)_inputDepth--;}
    internal static async Task<CardModel[]> Show(ChoiceScope view)
    {
        DisplayAssets.VerifyNativePool();
        var options=view.Options.SelectMany(c=>c).ToHashSet();Task<IEnumerable<CardModel>> task;
        if(view.Frame.Kind is "FromHand" or "FromHandForUpgrade")
        {
            var hand=NPlayerHand.Instance!;view.Ui=hand;
            // This UI is a presentation wait. Do not register ExecutionFinished on a historical source:
            // that event may already have fired, leaving selected holders attached indefinitely.
            task=hand.SelectCards(view.Prefs,c=>options.Contains(c),null,
                view.Frame.Kind=="FromHandForUpgrade"?NPlayerHand.Mode.UpgradeSelect:NPlayerHand.Mode.SimpleSelect);
        }
        else if(view.Frame.Kind=="FromChooseACardScreen")
        {
            var screen=NChooseACardSelectionScreen.ShowScreen(view.Options.Select(c=>c.Single()).ToArray(),view.Frame.Cancelable)!;
            view.Ui=screen;task=screen.CardsSelected();
        }
        else if(view.Frame.Kind=="FromChooseABundleScreen")
        {
            var screen=NChooseABundleSelectionScreen.ShowScreen(view.Options.Select(c=>(IReadOnlyList<CardModel>)c).ToArray());
            view.Ui=screen;return (await screen.CardsSelected()).SelectMany(c=>c).ToArray();
        }
        else if(view.Frame.Kind=="FromCombatPile")
        {
            var screen=NCombatPileCardSelectScreen.Create(view.Pile!,view.Prefs,c=>options.Contains(c));view.Ui=screen;
            NOverlayStack.Instance!.Push(screen);task=screen.CardsSelected();
        }
        else if(view.Frame.Kind is "FromSimpleGrid" or "FromSimpleGridForRewards")
        {
            var screen=NSimpleCardSelectScreen.Create(view.Options.Select(c=>c.Single()).ToArray(),view.Prefs);view.Ui=screen;
            NOverlayStack.Instance!.Push(screen);task=screen.CardsSelected();
        }
        else throw new InvalidDataException("route-native-preview-kind-not-supported");
        return (await task).ToArray();
    }
    internal static async Task WaitReady(ChoiceScope view)
    {
        var started=Stopwatch.GetTimestamp();
        if(view.Ui is NCardGridSelectionScreen ui)
        {
            var grid=(NCardGrid)Field(ui,"_grid")!;
            while(grid.Size.X<=0||grid.Size.Y<=0||(bool)Field(grid,"_cardsAnimatingOutForSetCards")!||
                !grid.CurrentlyDisplayedCardHolders.Any(h=>h.CardNode is {} card&&GodotObject.IsInstanceValid(card)&&!card.IsQueuedForDeletion()))
            {
                PlayerLoop.RequireHealthy();Codec.Require(Stopwatch.GetElapsedTime(started).TotalSeconds<3,"native-choice-grid-not-ready");
                await Runtime.NextFrame();
            }
            Runtime.Log("choice-grid-ready",new{view.Frame.Sequence,VisibleHolders=grid.CurrentlyDisplayedCardHolders.Count(),Milliseconds=Stopwatch.GetElapsedTime(started).TotalMilliseconds});
            return;
        }
        // Native small-choice clicks reject the first 350ms; hand mode animates its backstop for 200ms.
        var minimum=view.Ui is NPlayerHand?.2:.36;
        while(Stopwatch.GetElapsedTime(started).TotalSeconds<minimum)await Runtime.NextFrame();
    }
    internal static void Prefix(ChoiceScope view,int stage)
    {
        while(view.EventPosition>stage)
        {
            var selected=view.PlaybackEvents[--view.EventPosition];
            Codec.Require(selected.Kind=="Select","route-non-reversible-ui-prefix");
            Apply(view,new(view.Frame.Kind=="FromChooseABundleScreen"?"Cancel":"Deselect",view.Frame.Kind=="FromChooseABundleScreen"?null:selected.Option,0));
        }
        while(view.EventPosition<stage)
        {
            var next=view.PlaybackEvents[view.EventPosition++];Codec.Require(next.Kind=="Select","route-uncommitted-prefix-has-submit");Apply(view,next);
        }
        AssertPrefix(view,stage);
    }
    internal static void AssertPrefix(ChoiceScope view,int stage)
    {
        if(view.Frame.Kind=="FromChooseABundleScreen")return;
        var expected=view.PlaybackEvents.Take(stage).Where(e=>e.Kind=="Select").Select(e=>view.Options[e.Option!.Value].Single()).ToArray();
        var actual=Field(view.Ui!,"_selectedCards") as IEnumerable<CardModel>;
        Codec.Require(actual is null?expected.Length==0:actual.SequenceEqual(expected),"route-native-selected-prefix-mismatch");
        Runtime.Log("route-ui-prefix-verified",new{Choice=view.Frame.Sequence,Stage=stage,Count=expected.Length,OriginalInstances=true});
    }
    internal static void SettleSeekHand(ChoiceScope view)
    {
        if(view.Ui is not NPlayerHand hand)return;
        Codec.Require(hand.IsVisibleInTree()&&hand.CurrentMode==NPlayerHand.Mode.SimpleSelect,"seek-hand-layout-context");
        Call(hand,"UpdateSelectModeCardVisibility"); // Native filter, layout targets and focus neighbours.
        var active=hand.ActiveHolders.ToArray();
        foreach(var holder in active)
        {
            // Direct Seek presents the native selection layout at its endpoint. Do not reset the
            // whole hand to Play mode or move selected cards back out of their native container.
            Call(holder,"StopAnimations");holder.Position=holder.TargetPosition;
            holder.SetAngleInstantly(holder.TargetAngle);holder.SetScaleInstantly((Vector2)Field(holder,"_targetScale")!);
            if(holder.CardNode is {} card)HandPresentation.FinishHighlight(card);
        }
        if(Field(hand,"_selectedCardScaleTween") is Tween tween&&GodotObject.IsInstanceValid(tween)&&tween.IsRunning())tween.CustomStep(1);
        var selected=((IEnumerable<CardModel>)Field(hand,"_selectedCards")!).ToArray();
        var container=(NSelectedHandCardContainer)Field(hand,"_selectedHandCardContainer")!;
        var expectedHand=LiveState.Player.PlayerCombatState!.Hand.Cards.Where(c=>!selected.Contains(c)).ToArray();
        var holders=hand.CardHolderContainer.GetChildren().OfType<NHandCardHolder>().ToArray();
        var expectedVisible=expectedHand.Where(c=>view.Options.Any(o=>o.Contains(c))).ToArray();
        Codec.Require(holders.Select(h=>h.CardModel).SequenceEqual(expectedHand)&&
            active.Select(h=>h.CardModel).SequenceEqual(expectedVisible)&&
            container.Holders.Select(h=>h.CardNode?.Model).SequenceEqual(selected),"seek-choice-hand-layout-membership");
        Codec.Require(active.All(h=>h.Position.IsEqualApprox(h.TargetPosition)&&Mathf.IsEqualApprox(h.RotationDegrees,h.TargetAngle)&&
            h.Scale.IsEqualApprox((Vector2)Field(h,"_targetScale")!)),"seek-choice-hand-layout-targets");
        Runtime.Log("native-seek-choice-layout-verified",new{Choice=view.Frame.Sequence,Stage=view.EventPosition,
            HandCards=expectedHand.Length,VisibleHolders=active.Length,SelectedHolders=selected.Length,NativeMode=hand.CurrentMode.ToString(),
            SceneVisible=hand.IsVisibleInTree(),NativeSelectionLayout=true,SelectedContainerPreserved=true});
    }
    internal static ChoiceEvent[] ResultDemo(ChoiceFrame f,object ui)
    {
        return ChoiceTrace.ResultDemo(f);
    }
    internal static void Apply(ChoiceScope s,ChoiceEvent e)
    {
        var ui=s.Ui!;_driving=true;
        try
        {
            CardModel? card=e.Option is {} index?s.Options[index][0]:null;
            if(ui is NPlayerHand hand)
            {
                if(e.Kind=="Select")
                {
                    hand.PeekButton.SetPeeking(false);
                    var holder=hand.GetCardHolder(card!)??throw new InvalidDataException("choice-hand-instance-missing");holder.TryGrabFocus();
                    Call(hand,hand.CurrentMode==NPlayerHand.Mode.UpgradeSelect?"SelectCardInUpgradeMode":"SelectCardInSimpleMode",holder);
                }
                else if(e.Kind=="Deselect")
                {
                    if(hand.CurrentMode==NPlayerHand.Mode.SimpleSelect)((NSelectedHandCardContainer)Field(hand,"_selectedHandCardContainer")!).DeselectCard(card!);
                    else
                    {
                        var preview=Field(hand,"_upgradePreview")!;AccessTools.Property(preview.GetType(),"Card").SetValue(preview,null);
                        ((Control)Field(hand,"_upgradePreviewContainer")!).Visible=false;
                        hand.DeselectCard(NCard.Create(card!)!);
                    }
                }
                else if(e.Kind=="Confirm")Call(hand,"OnSelectModeConfirmButtonPressed",Field(hand,"_selectModeConfirmButton"));
                else throw new InvalidDataException("unsupported-hand-process-event");
            }
            else if(ui is NChooseACardSelectionScreen)
            {
                if(e.Kind=="Select")
                {
                    ((NPeekButton)Field(ui,"_peekButton")!).SetPeeking(false);
                    var holder=((Node)Field(ui,"_cardRow")!).GetChildren().OfType<NCardHolder>().Single(h=>ReferenceEquals(h.CardModel,card));
                    holder.TryGrabFocus();Call(ui,"SelectHolder",holder);
                }
                else if(e.Kind=="Skip")Call(ui,"OnSkipButtonReleased",Field(ui,"_skipButton"));
                else throw new InvalidDataException("unsupported-small-choice-event");
            }
            else if(ui is NCardGridSelectionScreen)
            {
                ((NPeekButton)Field(ui,"_peekButton")!).SetPeeking(false);
                if(e.Kind is "Select" or "Deselect")
                {
                    var grid=(NCardGrid)Field(ui,"_grid")!;Locate(grid,card!);
                    var selected=(IEnumerable<CardModel>)Field(ui,"_selectedCards")!;
                    Codec.Require(selected.Contains(card!)==(e.Kind=="Deselect"),"choice-grid-selection-state");Call(ui,"OnCardClicked",card);
                }
                else if(e.Kind=="Confirm")Call(ui,"CompleteSelection");
                else throw new InvalidDataException("unsupported-grid-process-event");
            }
            else if(ui is NChooseABundleSelectionScreen)
            {
                if(e.Kind=="Select")
                {
                    var row=(Node)Field(ui,"_bundleRow")!;
                    var bundle=row.GetChildren().Single(n=>AccessTools.Property(n.GetType(),"Bundle")?.GetValue(n) is IReadOnlyList<CardModel> b&&b.SequenceEqual(s.Options[e.Option!.Value]));
                    Call(ui,"OnBundleClicked",bundle);
                }
                else if(e.Kind=="Cancel")Call(ui,"CancelSelection",Field(ui,"_previewCancelButton"));
                else if(e.Kind=="Confirm")Call(ui,"ConfirmSelection",Field(ui,"_previewConfirmButton"));
                else throw new InvalidDataException("unsupported-bundle-process-event");
            }
            else throw new InvalidDataException("unsupported-native-choice-window");
        }
        finally{_driving=false;}
    }
    private static void Locate(NCardGrid grid,CardModel card)
    {
        // The grid recycles holders. Resolve against its full sorted model list, never a visible index.
        var cards=((IEnumerable<CardModel>)Field(grid,"_cards")!).ToList();var index=cards.FindIndex(c=>ReferenceEquals(c,card));
        Codec.Require(index>=0,"choice-grid-candidate-not-present");
        var columns=(int)AccessTools.Property(typeof(NCardGrid),"Columns").GetValue(grid)!;
        var size=(Vector2)Field(grid,"_cardSize")!;var pad=(float)AccessTools.Property(typeof(NCardGrid),"CardPadding").GetValue(grid)!;
        var y=grid.Size.Y*.5f-(grid.YOffset+80+size.Y*.5f+index/columns*(size.Y+pad));
        var bottom=(float)AccessTools.Property(typeof(NCardGrid),"ScrollLimitBottom").GetValue(grid)!;
        var top=(float)AccessTools.Property(typeof(NCardGrid),"ScrollLimitTop").GetValue(grid)!;
        grid.SetScrollPosition(Math.Clamp(y,Math.Min(top,bottom),Math.Max(top,bottom)));
        for(var i=0;i<cards.Count+1&&grid.GetCardHolder(card) is null;i++)Call(grid,"AllocateCardHolders");
        var holder=grid.GetCardHolder(card);
        if(holder is null)
        {
            Runtime.Log("choice-grid-location-failed",new{Candidates=cards.Count,ExpectedIndex=index,Size=grid.Size.ToString(),VisibleHolders=grid.CurrentlyDisplayedCardHolders.Count(),
                Initializing=(bool)Field(grid,"_cardsAnimatingOutForSetCards")!});
            throw new InvalidDataException("choice-grid-holder-not-located");
        }
        holder.TryGrabFocus();
        Runtime.Log("choice-target-located",new{Model=card.Id.ToString(),SortedIndex=index,ByOriginalInstance=true,VisibleHolderMatched=ReferenceEquals(holder.CardModel,card)});
    }
    internal static void Cancel(ChoiceScope s)
    {
        if(s.Completed||s.Cancelled||s.Ui is not {} ui)return;s.Cancelled=true;s.UiClosed.TrySetCanceled();
        if(ui is NPlayerHand hand)
        {
            hand.PeekButton.SetPeeking(false);
            Call(hand,"CancelHandSelectionIfNecessary");
            Runtime.Log("choice-wait-cancelled",new{s.Frame.Sequence,s.Frame.Boundary,NativeWaitCancelled=true,ResultSubmitted=false,PresentationOnly=DecisionRoute.Enabled});return;
        }
        var source=Field(ui,ui is NPlayerHand?"_selectionCompletionSource":"_completionSource");
        if(source is not null)AccessTools.Method(source.GetType(),"TrySetCanceled",Type.EmptyTypes).Invoke(source,null);
        if(ui is IOverlayScreen overlay&&GodotObject.IsInstanceValid((Node)ui))NOverlayStack.Instance!.Remove(overlay);
        Runtime.Log("choice-wait-cancelled",new{s.Frame.Sequence,s.Frame.Boundary,NativeWaitCancelled=true,ResultSubmitted=false,PresentationOnly=DecisionRoute.Enabled});
    }
}
