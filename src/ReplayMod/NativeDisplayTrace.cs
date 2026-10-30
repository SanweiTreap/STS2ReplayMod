using System.Diagnostics;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx.Ui;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;

namespace ReplayMod;

// Watching-session presentation only. Sample combat cards, not an entire scene or the recording file.
// Cues are newly constructed calls to audited visual APIs; no original Task/TCS/delegate is retained.
internal sealed class NativeDisplayTrace
{
    private sealed record IndexLabel(Vector2 Position,Vector2 Scale,float Rotation,Vector2 Size,Vector2 Pivot,Color Color,string Text,Font Font,int FontSize,Color FontColor,Color Outline,int OutlineSize,HorizontalAlignment Horizontal,VerticalAlignment Vertical);
    private sealed record CostLabel(string Field,string Text,Color FontColor,Color Outline,Color Color,bool Visible);
    private sealed record CostIcon(string Field,bool Visible);
    private sealed record Highlight(float Width,Color Color,bool Visible);
    private sealed record HandTarget(Vector2 Position,Vector2 Scale,float Angle);
    private sealed record HandPose(HandTarget Current,HandTarget Target,Vector2 CardPosition,Vector2 CardScale,float CardAngle,int Order,int ZIndex,bool ZAsRelative);
    private sealed record Pose(int Id,Vector2 Position,Vector2 Scale,float Rotation,Color Color,Vector2 BodyScale,Color BodyColor,PileType Pile,PileType ActualPile,IndexLabel? Index,Highlight Highlight,CostLabel[] Costs,CostIcon[] CostIcons,HandPose? Hand,DisplayAssets.Asset Variant,int ZIndex,bool ZAsRelative);
    private sealed record ThrownItem(int Id,Texture2D? Texture,Vector2 Position,Vector2 Scale,float Rotation,Color Color);
    private sealed record Health(Creature Creature,string Path,Vector2 Position,Vector2 Size,Color Color,Color SelfColor,bool Visible,string? Text,Color? FontColor,Color? Outline);
    private sealed record Pile(PileType Type,int Count);
    private sealed record PotionPart(string Path,Vector2 Position,Vector2 Size,Vector2 Scale,Vector2 Pivot,TextureRect.ExpandModeEnum? Expand,TextureRect.StretchModeEnum? Stretch,Vector2 Minimum);
    private sealed record Bottle(PotionModel Model,Vector2 Position,Vector2 Scale,float Rotation,Vector2 Pivot,Vector2 Size,Color Color,Vector2 ContainerPosition,Vector2 ContainerScale,Color ContainerColor,bool Outline,PotionPart[] Parts);
    private sealed record EmptySlot(int Slot,Texture2D? Texture,Vector2 Position,Vector2 Scale,float Rotation,Vector2 Pivot,Vector2 Size,Color Color,bool Visible,TextureRect.ExpandModeEnum Expand,TextureRect.StretchModeEnum Stretch,Vector2 Minimum,Color LocalColor,Color SelfColor,double? FadeElapsed);
    private sealed record HandExit(Vector2 Position,Color Color);
    private sealed record ShufflePose(Vector2 Start,Vector2 End,string Trail,PileType Target,float Offset,float Speed,float Acceleration,float Arc,float Duration);
    private sealed record Frame(double At,Pose[] Cards,Health[] Health,Pile[] Piles,CardModel[] Hand,PotionVisuals.Sample[] Liquids,Bottle[] Bottles,EmptySlot[] EmptySlots,ThrownItem[] Items,CreatureHudTrace.Sample Hud,CombatHudTrace.Sample CombatHud,PowerHudTrace.Sample Powers,HealthDisplayTail.Sample[] HealthTails,HandExit? Exit);
    private sealed record Cue(double At,string Kind,Action<Renderer> Fire);
    private sealed record CardSpec(DisplayAssets.Asset Asset,PileType Pile);
    private sealed record HandStart(int Id,Vector2 Position,Vector2 Scale,float Rotation);
    private sealed record HandMove(HandStart Before,int Id,HandPose VisibleStart);
    private sealed record PlayMove(int Id,Vector2 Position,Vector2 Scale,float Angle,PileType? From,PileType ActualPile,bool CardEntry);
    private sealed record BlockCapture(NativeDisplayTrace Trace,HashSet<NBlockBrokenVfx> Previous);
    private readonly List<Frame> _frames=[];
    private sealed record CardTailFrame(double At,Pose[] Cards);
    private readonly List<CardTailFrame> _cardTailFrames=[];
    private readonly Dictionary<CardModel,int> _tailModels=new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<CardModel> _inheritedTails=new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<int,double> _tailTrailFades=[];
    private bool _queuedCut;
    private static readonly List<NativeDisplayTrace> TailRecorders=[];
    private readonly List<Cue> _cues=[];
    private readonly Dictionary<NCard,(CardModel Model,int Id)> _cards=[];
    private readonly Dictionary<int,CardSpec> _specs=[];
    private readonly Dictionary<int,DisplayAssets.Asset> _variants=[];
    private readonly HashSet<NCard> _dirtyCards=[];
    private readonly Dictionary<NCardTrailVfx,int> _trails=[];
    private readonly HashSet<int> _exhaust=[];
    private readonly HashSet<PotionModel> _potionModels=[];
    private readonly HashSet<int> _potionSlots=[];
    private readonly Dictionary<NItemThrowVfx,int> _items=[];
    private readonly HashSet<Creature> _creatures=[];
    private readonly Dictionary<int,HandMove> _handMoves=[];
    private readonly long _started=Stopwatch.GetTimestamp();
    private static long? _requested;
    private static long? _pointerDown;
    private readonly long? _requestAt;
    private NativeDisplayTrace(long? requestAt){_requestAt=requestAt;}
    private double? _firstPlayAt;
    private readonly List<object> _slowSamples=[];
    private Task? _warmTask;
    internal static void MarkStepPointerDown()=>_pointerDown=Stopwatch.GetTimestamp();
    internal static void MarkStepRequested()
    {
        _requested=Stopwatch.GetTimestamp();
        Runtime.Log("step-input-timing",new{PointerHoldMilliseconds=_pointerDown is {} down?(double?)Stopwatch.GetElapsedTime(down,_requested.Value).TotalMilliseconds:null});
        _pointerDown=null;
    }
    internal static long? TakeStepRequest(){var requested=_requested;_requested=null;return requested;}
    private double _last=-1;
    private double _sampleMilliseconds,_maximumSampleMilliseconds;
    private bool _copyBatch;
    private int _handBoundarySamples;
    private double? _choiceEntryAt,_choiceReadyAt;
    private string? _copyProjection;
    private static NativeDisplayTrace? _capture;
    private static readonly HashSet<Node> SourceVisuals=[];
    private static readonly HashSet<Node> ContinuedSmiths=[];
    private static readonly HashSet<Node> OwnedTails=[];
    private static readonly Dictionary<Creature,NCreature> DeathDisplays=[];
    internal static bool OwnsDeathDisplay(Creature creature)=>DeathDisplays.ContainsKey(creature);
    private static (NCombatCardPile Button,int Count)? _pileDisplayOverride;
    private sealed class TriggerCapture(Creature creature,string trigger,TriggerCapture? previous)
    {
        internal readonly Creature Creature=creature;
        internal readonly string Trigger=trigger;
        internal readonly TriggerCapture? Previous=previous;
        internal string? Animation;
    }
    private static TriggerCapture? _triggerCapture;
    private static bool _recordedShake;
    private static bool _recordedSound;
    private static bool _creatingBlockBreakDisplay;
    private static readonly Dictionary<(Type,string),FieldInfo> Fields=[];
    private static readonly Dictionary<string,StringName> Names=[];
    internal static StringName Name(string value)
    {
        if(Names.TryGetValue(value,out var name))return name;
        name=new(value);Names.Add(value,name);return name;
    }
    private static readonly Dictionary<(Node,Node),string> Paths=[];
    private static readonly HashSet<NCard> KnownCards=[];
    // Native exhaust keeps its card alive for a delayed free after the burn is invisible.
    // Track ownership across segments; it must neither block Step nor become a new card pose.
    // NCard.Create clears ownership when the shared pool lends this node again.
    private static readonly Dictionary<NCard,CardModel> SourceExhaustOwners=[];
    private static readonly Dictionary<NHealthBar,Control[]> HealthControls=[];
    private static readonly string[] CostFields=["_energyLabel","_starLabel"];
    private static readonly string[] CostIcons=["_energyIcon","_unplayableEnergyIcon","_starIcon","_unplayableStarIcon"];
    internal static string RelativePath(Node root,Node node)
    {
        if(Paths.TryGetValue((root,node),out var cached))return cached;
        using var path=root.GetPathTo(node);var text=path.ToString();Paths.Add((root,node),text);return text;
    }
    internal static bool Replaying {get;private set;}
    internal static bool Capturing=>_capture is not null;
    internal static bool Settling {get;private set;}
    private double Elapsed=>Stopwatch.GetElapsedTime(_started).TotalSeconds;
    private static Color EffectiveColor(CanvasItem item)
    {
        var color=item.Modulate*item.SelfModulate;
        for(var parent=item.GetParent();parent is not null;parent=parent.GetParent())if(parent is CanvasItem canvas)color*=canvas.Modulate;
        return color;
    }
    internal static object? ReadField(object owner,string field)
    {
        var key=(owner.GetType(),field);if(!Fields.TryGetValue(key,out var info))Fields.Add(key,info=AccessTools.Field(key.Item1,field));
        return info.GetValue(owner);
    }
    private static object? Get(object owner,string field)=>ReadField(owner,field);
    private static IEnumerable<Node> Descendants(Node root)
    {
        yield return root;
        if(DisplayAssets.IsCacheRoot(root)||OwnedTails.Contains(root))yield break;
        if(root is NCard or NCardExhaustVfx)yield break;
        foreach(var child in root.GetChildren())foreach(var node in Descendants(child))yield return node;
    }
    private static IEnumerable<NCard> TableCards()
    {
        if(NCombatRoom.Instance is not {} room)yield break;
        foreach(var card in KnownCards)
            if(GodotObject.IsInstanceValid(card)&&!card.IsQueuedForDeletion()&&card.IsInsideTree()&&!DisplayAssets.IsPrivate(card)&&
                (room.Ui.IsAncestorOf(card)||room.CombatVfxContainer.IsAncestorOf(card)))yield return card;
    }
    private static void CardCreated(NCard? __result)
    {if(PlayerLoop.Active&&__result is not null&&!DisplayAssets.IsPrivate(__result)){KnownCards.Add(__result);SourceExhaustOwners.Remove(__result);}}
    private static bool OwnedSourceExhaust(NCard card)=>SourceExhaustOwners.TryGetValue(card,out var model)&&ReferenceEquals(model,card.Model);
    internal static void Begin(long? requestedAt=null)
    {
        PotionVisuals.StopOwned();
        Codec.Require(_capture is null&&!Replaying,"overlapping-native-display-capture");
        _capture=new(requestedAt);_capture.Sample(force:true);
        _capture._inheritedTails.UnionWith(TailRecorders.SelectMany(t=>t._tailModels.Keys));
    }
    internal static async Task WaitForChoiceEntry()
    {
        var started=Stopwatch.GetTimestamp();var frames=0;var source=ActionMonitor.Pending?.Card;
        var before=LiveState.ChoiceProjection();
        // Manual play deliberately does not await its own visual tween. A fast choice must not
        // truncate that visual edge and then snap the played card to the middle on history replay.
        while(source is not null&&TableCards().Any(c=>ReferenceEquals(c.Model,source)&&c.PlayPileTween is {} tween&&GodotObject.IsInstanceValid(tween)&&tween.IsRunning()))
        {
            PlayerLoop.RequireHealthy();Codec.Require(Stopwatch.GetElapsedTime(started).TotalSeconds<3,"choice-entry-card-animation-not-finished");
            await Runtime.NextFrame();frames++;
        }
        _capture?.Sample(force:true);
        Codec.Require(before==LiveState.ChoiceProjection(),"choice-entry-visual-wait-changed-business-or-rng");
        Runtime.Log("native-choice-entry-presentation",new{Source=source?.Id.ToString(),Frames=frames,Milliseconds=Stopwatch.GetElapsedTime(started).TotalMilliseconds,NativeBusinessNotAdvanced=true});
    }
    internal static void Clear(){_capture=null;TailRecorders.Clear();_requested=null;_pointerDown=null;Settling=false;RetireOwnedTails();HealthDisplayTail.Clear();Paths.Clear();KnownCards.Clear();SourceExhaustOwners.Clear();HealthControls.Clear();CombatHudTrace.ClearControls();CreatureHudTrace.ClearControls();}
    internal static async Task WaitForOpeningPresentation()
    {
        var started=Stopwatch.GetTimestamp();var frames=0;
        while(NCombatRoom.Instance is {} room&&Descendants(room).Any(n=>n is NCombatStartBanner or NPlayerTurnBanner))
        {
            PlayerLoop.RequireHealthy();
            Codec.Require(Stopwatch.GetElapsedTime(started).TotalSeconds<12,"opening-banner-did-not-finish");
            await Runtime.NextFrame();frames++;
        }
        Runtime.Log("opening-presentation-settled",new{Frames=frames,Milliseconds=Stopwatch.GetElapsedTime(started).TotalMilliseconds,FirstOperationNotStarted=true});
    }
    internal static void RetireOwnedTails()
    {
        foreach(var node in OwnedTails.Where(n=>GodotObject.IsInstanceValid(n)&&!n.IsQueuedForDeletion()).ToArray())
        {if(node is NCreature creature)creature.DeathAnimCancelToken.Cancel();node.GetParent()?.RemoveChild(node);node.QueueFree();}
        OwnedTails.Clear();DeathDisplays.Clear();CombatHudTrace.RetireEnergyEffects();
    }
    private static void OwnTail(Node? node)
    {
        if(node is null)return;TrackTail(node);NCombatRoom.Instance!.AddChild(node);
    }
    private static void TrackTail(Node node)
    {
        if(OwnedTails.Add(node))node.TreeExited+=()=>{if(node.IsQueuedForDeletion())OwnedTails.Remove(node);};
    }
    internal static void MarkChoiceEntry()
    {
        var capture=_capture??throw new InvalidDataException("choice-entry-without-display-capture");
        Codec.Require(capture._choiceEntryAt is null,"duplicate-display-choice-entry");
        capture._choiceEntryAt=capture.Elapsed;capture._cues.Add(new(capture._choiceEntryAt.Value,"choice-open",_=>{}));
    }
    internal static void MarkChoiceReady()
    {
        var capture=_capture??throw new InvalidDataException("choice-ready-without-display-capture");
        Codec.Require(capture._choiceEntryAt is not null,"display-choice-ready-before-entry");capture._choiceReadyAt=capture.Elapsed;
    }
    internal static async Task RetireSourceVisuals()
    {
        if(NCombatRoom.Instance is not {} room){SourceVisuals.Clear();return;}
        await PotionVisuals.RetireNative();
        // Native fly completion is display bookkeeping. Retire it before replacing data so that its
        // delayed CardAddFinished cannot update the counters of an earlier history node.
        var flights=Descendants(room.Ui).Concat(Descendants(room.CombatVfxContainer))
            .Concat(Descendants(NRun.Instance!.GlobalUi.TopBar.TrailContainer)).OfType<NCardFlyVfx>().Distinct().ToArray();
        foreach(var flight in flights)((CancellationTokenSource)Get(flight,"_cancelToken")!).Cancel();
        // Cancel while still in-tree: its current visual coroutine completes its existing signal once.
        // Removing the card first would let TreeExited complete it before the cancellation SetResult.
        for(var i=0;i<3&&flights.Any(f=>f.SwooshAwayCompletion is {Task.IsCompleted:false});i++)await Runtime.NextFrame();
        Codec.Require(flights.All(f=>f.SwooshAwayCompletion is null or {Task.IsCompleted:true}),"native-display-flight-cancel-pending");
        foreach(var flight in flights)
        {
            if(Get(flight,"_card") is NCard card&&GodotObject.IsInstanceValid(card)&&!card.IsQueuedForDeletion())
            {card.PlayPileTween?.Kill();card.QueueFreeSafely();}
            if(GodotObject.IsInstanceValid(flight)&&!flight.IsQueuedForDeletion()){flight.GetParent()?.RemoveChild(flight);flight.QueueFree();}
        }
        var sources=SourceVisuals.Concat(ContinuedSmiths).Distinct().Where(n=>GodotObject.IsInstanceValid(n)&&!n.IsQueuedForDeletion()).ToArray();
        var sourceTypes=sources.GroupBy(n=>n.GetType().Name).Select(g=>new{Type=g.Key,Count=g.Count()}).ToArray();
        foreach(var node in sources)
        {
            if(node is NCardSmithVfx)
            {
                // Exit completes its new visual await before releasing preview cards. Return real
                // source cards normally; private frozen preview cards use their private disposal hook.
                node.GetParent()?.RemoveChild(node);
                foreach(var card in Descendants(node).OfType<NCard>().ToArray())if(GodotObject.IsInstanceValid(card)&&!card.IsQueuedForDeletion())card.QueueFreeSafely();
                node.QueueFree();continue;
            }
            // Native exhaust has an uncancelled delayed free. Let it release its shared-pool
            // source card exactly once; removing the effect now would free that card twice.
            if(node is NCardExhaustVfx normal){normal.Visible=false;continue;}
            if(node is NCardExhaustQuickVfx quick)
            {
                quick.Visible=false;
                if(Get(quick,"_cardNode") is NCard card&&GodotObject.IsInstanceValid(card)&&!card.IsQueuedForDeletion()&&card.IsAncestorOf(quick))
                {
                    var anchor=new Control{Name="ReplayRetiringNativeExhaust",Visible=false,MouseFilter=Control.MouseFilterEnum.Ignore};room.Ui.AddChild(anchor);
                    var finishing=(bool)Get(quick,"_isFinishing")!;
                    AccessTools.Field(typeof(NCardExhaustQuickVfx),"_isFinishing").SetValue(quick,true);
                    try{card.Reparent(anchor);}finally{AccessTools.Field(typeof(NCardExhaustQuickVfx),"_isFinishing").SetValue(quick,finishing);}
                    card.TreeExited+=()=>{if(GodotObject.IsInstanceValid(anchor)&&!anchor.IsQueuedForDeletion())anchor.QueueFree();};
                }
                continue;
            }
            node.GetParent()?.RemoveChild(node);node.QueueFree();
        }
        SourceVisuals.Clear();ContinuedSmiths.Clear();
        Runtime.Log("native-display-retired",new{CardFlights=flights.Length,SourceEffects=sourceTypes,DelayedNativePileCallbacksRetired=true});
    }
    internal static void Tick()
    {
        PotionVisuals.Tick();_capture?.Sample();
        if(_capture is {} current)
            foreach(var tail in TailRecorders.ToArray())
            {
                var poses=current._frames[^1].Cards.Where(p=>tail._tailModels.ContainsKey(p.Variant.Source)&&
                    p.ActualPile is PileType.Draw or PileType.Discard or PileType.Exhaust)
                    .Select(p=>p with{Id=tail._tailModels[p.Variant.Source],Variant=tail._specs[tail._tailModels[p.Variant.Source]].Asset}).ToArray();
                tail._cardTailFrames.Add(new(tail.Elapsed,poses));
                if(poses.Length==0)TailRecorders.Remove(tail);
                Codec.Require(tail._cardTailFrames.Count<14400,"queued-card-tail-recording-too-long");
            }
    }
    private int Card(NCard node)
    {
        KnownCards.Add(node);
        var model=node.Model??throw new InvalidDataException("native-display-card-without-model");
        if(_cards.TryGetValue(node,out var entry)&&ReferenceEquals(model,entry.Model))
        {if(_dirtyCards.Remove(node))_variants[entry.Id]=Frozen(model);return entry.Id;}
        var id=_specs.Count;
        var asset=Frozen(model);_variants[id]=asset;_dirtyCards.Remove(node);
        _specs.Add(id,new(asset,node.DisplayingPile));_cards[node]=(model,id);return id;
    }
    private DisplayAssets.Asset Frozen(CardModel model)
    {
        string? before=null;
        var asset=DisplayAssets.Get(model,()=>
        {
            if(model.Owner.PlayerCombatState is null||model.Owner.Creature.CombatState is null||LiveState.Run.CurrentRoom is null)return;
            if(_copyBatch)_copyProjection??=LiveState.ChoiceProjection();else before=LiveState.ChoiceProjection();
        });
        if(before is not null)Codec.Require(before==LiveState.ChoiceProjection(),"native-display-model-copy-mutated-business-or-rng");
        return asset;
    }
    private void Sample(bool force=false)
    {
        if(NCombatRoom.Instance is not {} room)return;
        var now=Elapsed;if(!force&&now-_last<1.0/120)return;_last=now;
        Codec.Require(_frames.Count<14400,"native-display-trace-too-long");
        var sampleStarted=Stopwatch.GetTimestamp();var allocated=GC.GetAllocatedBytesForCurrentThread();
        var gen0=GC.CollectionCount(0);var gen1=GC.CollectionCount(1);var gen2=GC.CollectionCount(2);var models=DisplayAssets.ModelsCreated;
        var poses=new List<Pose>();
        _copyBatch=true;_copyProjection=null;
        try{foreach(var card in TableCards().Where(c=>c.IsNodeReady()&&c.Model is not null&&c.IsVisibleInTree()&&!OwnedSourceExhaust(c)))
        {
            var id=Card(card);if(_exhaust.Contains(id))continue;
            var color=EffectiveColor(card);
            var transform=card.GetGlobalTransform();
            IndexLabel? index=null;HandPose? handPose=null;
            for(var parent=card.GetParent();parent is not null;parent=parent.GetParent())if(parent is NHandCardHolder holder)
            {
                handPose=CaptureHand(holder,card);
                var label=(MegaLabel)Get(holder,"_handIndexLabel")!;
                if(label.IsVisibleInTree())
                {
                    var t=label.GetGlobalTransform();index=new(label.GlobalPosition,t.Scale,t.Rotation,label.Size,label.PivotOffset,EffectiveColor(label),label.Text,
                        label.GetThemeFont(NativeDisplayTrace.Name("font")),label.GetThemeFontSize(NativeDisplayTrace.Name("font_size")),label.GetThemeColor(NativeDisplayTrace.Name("font_color")),label.GetThemeColor(NativeDisplayTrace.Name("font_outline_color")),label.GetThemeConstant(NativeDisplayTrace.Name("outline_size")),label.HorizontalAlignment,label.VerticalAlignment);
                }
                break;
            }
            var glow=card.CardHighlight;
            var costs=CostFields.Select(field=>
            {
                var label=(MegaLabel)Get(card,field)!;
                return new CostLabel(field,label.Text,label.GetThemeColor(NativeDisplayTrace.Name("font_color")),label.GetThemeColor(NativeDisplayTrace.Name("font_outline_color")),label.Modulate*label.SelfModulate,label.Visible);
            }).ToArray();
            var icons=CostIcons
                .Select(field=>new CostIcon(field,((Control)Get(card,field)!).Visible)).ToArray();
            poses.Add(new(id,card.GlobalPosition,transform.Scale,transform.Rotation,color,card.Body.Scale,card.Body.Modulate,card.DisplayingPile,card.Model?.Pile?.Type??PileType.None,index,
                new(((ShaderMaterial)glow.Material).GetShaderParameter(NativeDisplayTrace.Name("width")).AsSingle(),glow.Modulate*glow.SelfModulate,glow.Visible),costs,icons,handPose,_variants[id],card.ZIndex,card.ZAsRelative));
        }}finally
        {
            _copyBatch=false;if(_copyProjection is not null)Codec.Require(_copyProjection==LiveState.ChoiceProjection(),"native-display-model-copy-mutated-business-or-rng");_copyProjection=null;
        }
        var cardsFinished=Stopwatch.GetTimestamp();var health=new List<Health>();
        foreach(var creatureNode in room.CreatureNodes)
        {
            _creatures.Add(creatureNode.Entity);
            var display=Get(creatureNode,"_stateDisplay");if(display is null||Get(display,"_healthBar") is not NHealthBar bar||!bar.IsNodeReady())continue;
            if(!HealthControls.TryGetValue(bar,out var controls)){controls=Descendants(bar).OfType<Control>().ToArray();HealthControls.Add(bar,controls);}
            foreach(var control in controls)health.Add(new(creatureNode.Entity,RelativePath(bar,control),
                control.Position,control.Size,control.Modulate,control.SelfModulate,control.Visible,control is MegaLabel label?label.Text:null,
                control is MegaLabel text?text.GetThemeColor(NativeDisplayTrace.Name("font_color")):null,control is MegaLabel outline?outline.GetThemeColor(NativeDisplayTrace.Name("font_outline_color")):null));
        }
        var healthFinished=Stopwatch.GetTimestamp();
        var piles=new (NCombatCardPile Button,PileType Type)[]{(room.Ui.DrawPile,PileType.Draw),(room.Ui.DiscardPile,PileType.Discard),(room.Ui.ExhaustPile,PileType.Exhaust)};
        var potionContainer=NRun.Instance!.GlobalUi.TopBar.PotionContainer;
        var bottles=new List<Bottle>();
        foreach(var potion in Descendants(potionContainer).OfType<NPotion>().Where(p=>p.IsNodeReady()&&p.IsVisibleInTree()))
        {
            _potionModels.Add(potion.Model);var t=potion.GetGlobalTransform();var container=(Control)Get(potion,"_container")!;
            var parts=Descendants(potion).OfType<Control>().Where(c=>c!=potion).Select(c=>new PotionPart(RelativePath(potion,c),c.Position,c.Size,c.Scale,c.PivotOffset,
                (c as TextureRect)?.ExpandMode,(c as TextureRect)?.StretchMode,c.CustomMinimumSize)).ToArray();
            bottles.Add(new(potion.Model,potion.GlobalPosition,t.Scale,t.Rotation,potion.PivotOffset,potion.Size,EffectiveColor(potion),container.Position,container.Scale,container.Modulate,potion.Outline.Visible,parts));
        }
        var holders=(List<NPotionHolder>)Get(potionContainer,"_holders")!;var emptySlots=new List<EmptySlot>();
        for(var slot=0;slot<holders.Count;slot++)
        {
            var icon=(TextureRect)Get(holders[slot],"_emptyIcon")!;var t=icon.GetGlobalTransform();_potionSlots.Add(slot);
            var fade=Get(holders[slot],"_emptyPotionTween") as Tween;
            emptySlots.Add(new(slot,icon.Texture,icon.GlobalPosition,t.Scale,t.Rotation,icon.PivotOffset,icon.Size,EffectiveColor(icon),icon.IsVisibleInTree(),icon.ExpandMode,icon.StretchMode,icon.CustomMinimumSize,
                icon.Modulate,icon.SelfModulate,fade is not null&&GodotObject.IsInstanceValid(fade)&&fade.IsRunning()?fade.GetTotalElapsedTime():null));
        }
        var items=new List<ThrownItem>();
        foreach(var (fx,id) in _items)if(GodotObject.IsInstanceValid(fx)&&fx.IsInsideTree()&&Get(fx,"_itemSprite") is Sprite2D sprite&&sprite.IsVisibleInTree())
        {
            var t=sprite.GetGlobalTransform();items.Add(new(id,sprite.Texture,sprite.GlobalPosition,t.Scale,t.Rotation,EffectiveColor(sprite)));
        }
        var potionFinished=Stopwatch.GetTimestamp();
        var nativeHand=NPlayerHand.Instance!;var outTween=Get(nativeHand,"_animOutTween") as Tween;
        var exiting=nativeHand.Position.IsEqualApprox((Vector2)Get(nativeHand,"_hidePosition")!)||outTween is not null&&GodotObject.IsInstanceValid(outTween)&&outTween.IsRunning();
        _frames.Add(new(now,poses.ToArray(),health.ToArray(),piles.Where(p=>p.Button.IsNodeReady()).Select(p=>new Pile(p.Type,(int)Get(p.Button,"_currentCount")!)).ToArray(),
            LiveState.Player.PlayerCombatState!.Hand.Cards.ToArray(),PotionVisuals.Capture(),bottles.ToArray(),emptySlots.ToArray(),items.ToArray(),CreatureHudTrace.Capture(_creatures),CombatHudTrace.Capture(),PowerHudTrace.Capture(_creatures),HealthDisplayTail.Capture(_creatures),exiting?new(nativeHand.Position,nativeHand.Modulate):null));
        var milliseconds=Stopwatch.GetElapsedTime(sampleStarted).TotalMilliseconds;_sampleMilliseconds+=milliseconds;_maximumSampleMilliseconds=Math.Max(_maximumSampleMilliseconds,milliseconds);
        if(milliseconds>=8&&_slowSamples.Count<24)_slowSamples.Add(new{At=now,Milliseconds=milliseconds,
            Cards=Stopwatch.GetElapsedTime(sampleStarted,cardsFinished).TotalMilliseconds,Health=Stopwatch.GetElapsedTime(cardsFinished,healthFinished).TotalMilliseconds,
            Potions=Stopwatch.GetElapsedTime(healthFinished,potionFinished).TotalMilliseconds,Hud=Stopwatch.GetElapsedTime(potionFinished).TotalMilliseconds,
            AllocatedBytes=GC.GetAllocatedBytesForCurrentThread()-allocated,Collections=new[]{GC.CollectionCount(0)-gen0,GC.CollectionCount(1)-gen1,GC.CollectionCount(2)-gen2},NewDisplayModels=DisplayAssets.ModelsCreated-models});
    }
    internal static async Task<NativeDisplayTrace> Finish(bool settle,bool terminal=false,bool continueCards=false)
    {
        var trace=_capture??throw new InvalidDataException("native-display-capture-missing");
        // Capture the native display tail, including concurrent hand layout/fly/exhaust work.
        if(settle)
        {
            Settling=true;
            try
            {
            var start=Stopwatch.GetTimestamp();var quiet=0;var forgeFrames=0;var exitFrames=0;
            while(quiet<3&&Stopwatch.GetElapsedTime(start).TotalSeconds<3)
            {
                var flying=TableCards().Any(c=>c.IsVisibleInTree()&&!OwnedSourceExhaust(c)&&(c.Model?.Pile?.Type is PileType.Draw or PileType.Discard or PileType.Exhaust||
                    c.Model?.Pile?.Type==PileType.Hand&&!NPlayerHand.Instance!.IsAncestorOf(c)));
                var hand=NPlayerHand.Instance!;
                var near=hand.CardHolderContainer.GetChildren().OfType<MegaCrit.Sts2.Core.Nodes.Cards.Holders.NHandCardHolder>()
                    .All(h=>h.Position.DistanceTo(h.TargetPosition)<2&&Math.Abs(h.RotationDegrees-h.TargetAngle)<.5);
                var exhaust=trace._cues.LastOrDefault(c=>c.Kind.StartsWith("exhaust",StringComparison.Ordinal));
                var coreForge=ActionMonitor.Completed?.Kind==OperationKind.Potion&&(RegentPresentation.CoreForgeAnimating||
                    SourceVisuals.OfType<NCardSmithVfx>().Any(n=>GodotObject.IsInstanceValid(n)&&!n.IsQueuedForDeletion()&&n.IsInsideTree()&&
                        Get(n,"_tween") is Tween tween&&GodotObject.IsInstanceValid(tween)&&tween.IsRunning()));
                var exit=terminal&&CombatHudTrace.ExitAnimating();
                if(coreForge)forgeFrames++;if(exit)exitFrames++;
                quiet=!flying&&near&&!coreForge&&!exit&&(exhaust is null||trace.Elapsed-exhaust.At>.8)?quiet+1:0;
                await Runtime.NextFrame();
            }
            Codec.Require(quiet>=3,"native-display-tail-did-not-settle");
            Runtime.Log("native-presentation-boundary",new{Terminal=terminal,CoreForgeFrames=forgeFrames,ExitFrames=exitFrames,
                Milliseconds=Stopwatch.GetElapsedTime(start).TotalMilliseconds,FixedCooldownAdded=false});
            }
            finally{Settling=false;}
        }
        trace.Sample(force:true);
        if(continueCards)
        {
            trace._queuedCut=true;
            var cards=trace._frames[^1].Cards.Where(p=>!trace._inheritedTails.Contains(p.Variant.Source)&&
                p.ActualPile is PileType.Draw or PileType.Discard or PileType.Exhaust).ToArray();
            foreach(var pose in cards)trace._tailModels.TryAdd(pose.Variant.Source,pose.Id);
            if(cards.Length>0){trace._cardTailFrames.Add(new(trace.Elapsed,cards));TailRecorders.Add(trace);}
        }
        _capture=null;trace._cards.Clear();if(!continueCards)trace._trails.Clear();trace._dirtyCards.Clear();
        trace._warmTask=trace.WarmForHistory();
        PresentationTiming.Validate(trace._frames.Select(f=>f.At).ToArray());
        PresentationTiming.ValidateChoice(trace._choiceEntryAt,trace._choiceReadyAt,trace._frames[^1].At);
        Runtime.Log("native-display-captured",trace.Summary());return trace;
    }
    private async Task WarmForHistory()
    {
        var generation=DisplayAssets.Generation;var started=Stopwatch.GetTimestamp();var created=DisplayAssets.ViewsCreated;
        try
        {
            await Runtime.NextFrame();if(!DisplayAssets.CanWarm(generation))return;
            await DisplayAssets.Warm(_specs.Values.Select(s=>s.Asset));if(!DisplayAssets.CanWarm(generation))return;
            await DisplayAssets.WarmPotions(_potionModels,_potionSlots);
            Runtime.Log("native-display-assets-prepared",new{Milliseconds=Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                ViewsCreated=DisplayAssets.ViewsCreated-created,BlockedNativeForward=false,SessionGeneration=generation});
        }
        catch(Exception ex)
        {
            if(!DisplayAssets.CanWarm(generation))return;
            PlayerLoop.Error="display-preparation-"+ex.GetBaseException().Message;
            Runtime.Log("display-preparation-failed",new{Reason=PlayerLoop.Error});
        }
    }
    private object Summary()=>new{Frames=_frames.Count,CardTracks=_specs.Count,Duration=_frames.LastOrDefault()?.At??0,
        QueuedCheckpointSplit=_queuedCut,QueuedTailCards=_tailModels.Count,InheritedCardTailCount=_inheritedTails.Count,
        PeakSimultaneousCards=_frames.Select(f=>f.Cards.Length).DefaultIfEmpty().Max(),
        SampleMilliseconds=_sampleMilliseconds,MaximumSampleMilliseconds=_maximumSampleMilliseconds,
        StepToCaptureMilliseconds=_requestAt is {} requested?(double?)Stopwatch.GetElapsedTime(requested,_started).TotalMilliseconds:null,
        FirstPlayMilliseconds=_firstPlayAt*1000,SlowSamples=_slowSamples,
        IndexSamples=_frames.Sum(f=>f.Cards.Count(c=>c.Index is not null)),PotionBottleSamples=_frames.Sum(f=>f.Bottles.Length),LiquidOverlaySamples=_frames.Sum(f=>f.Liquids.Length),
        CardSamples=_frames.Sum(f=>f.Cards.Length),HealthControlSamples=_frames.Sum(f=>f.Health.Length),
        HealthTextStyleSamples=_frames.Sum(f=>f.Health.Count(h=>h.FontColor is not null)),
        HealthTextStyles=_frames.SelectMany(f=>f.Health).Where(h=>h.FontColor is not null).GroupBy(h=>(h.Creature,h.Path)).Select(g=>new{
            Creature=g.Key.Creature.ModelId.ToString(),g.Key.Path,Styles=g.Select(h=>new{Font=h.FontColor!.Value.ToHtml(),Outline=h.Outline!.Value.ToHtml()}).Distinct().ToArray()}).ToArray(),
        HighlightSamples=_frames.Sum(f=>f.Cards.Count(c=>c.Highlight.Width>0)),ThrownItemSamples=_frames.Sum(f=>f.Items.Length),
        CostSamples=_frames.Sum(f=>f.Cards.Sum(c=>c.Costs.Length)),IntentGlyphSamples=_frames.Sum(f=>f.Hud.Glyphs.Length),IntentTextSamples=_frames.Sum(f=>f.Hud.Texts.Length),
        HandBoundarySamples=_handBoundarySamples,
        NativeHandArrivalTracks=_handMoves.Count,
        NativePlayMoves=_cues.Count(c=>c.Kind is "native-play-pile" or "native-card-to-play"),
        NativePileFeedback=_cues.Count(c=>c.Kind.StartsWith("pile-add:",StringComparison.Ordinal)),
        PowerHudSamples=_frames.Sum(f=>f.Powers.Values.Length),NativePowerCues=_cues.Count(c=>c.Kind.StartsWith("power-",StringComparison.Ordinal)),
        NativeDeathDisplays=_cues.Count(c=>c.Kind=="creature-death-display"),
        NativeBlockBreaks=_cues.Count(c=>c.Kind=="native-block-break"),
        NativeForgeCues=_cues.Count(c=>c.Kind.StartsWith("regent-",StringComparison.Ordinal)),
        NativeSmithCues=_cues.Count(c=>c.Kind.StartsWith("native-smith",StringComparison.Ordinal)),
        NativeShuffleFlights=_cues.Count(c=>c.Kind=="native-shuffle-flight"),HandExitSamples=_frames.Count(f=>f.Exit is not null),
        MutableCardVariantTracks=_frames.SelectMany(f=>f.Cards).GroupBy(p=>p.Id).Count(g=>g.Select(p=>p.Variant).Distinct().Count()>1),
        HandMotionTracks=_frames.SelectMany(f=>f.Cards).Where(p=>p.Hand is not null).Select(p=>p.Id).Distinct().Count(),
        HandEntries=_handMoves.Values.Select(m=>new{m.Id,Model=_specs[m.Id].Asset.Source.Id.ToString(),
            Before=new[]{m.Before.Position.X,m.Before.Position.Y},VisibleStart=new[]{m.VisibleStart.Current.Position.X,m.VisibleStart.Current.Position.Y},
            Target=new[]{m.VisibleStart.Target.Position.X,m.VisibleStart.Target.Position.Y},
            RemainingPixels=m.VisibleStart.Current.Position.DistanceTo(m.VisibleStart.Target.Position)}).ToArray(),
        ChoiceEntryAt=_choiceEntryAt,ChoiceReadyAt=_choiceReadyAt,
        FirstDisplayedTurn=_frames.FirstOrDefault()?.CombatHud.Turn,LastDisplayedTurn=_frames.LastOrDefault()?.CombatHud.Turn,
        HiddenCreatureHudSamples=_frames.Sum(f=>f.Hud.States.Count(s=>!s.Visible||s.Color.A<.99f)),
        MaximumSampleGapMilliseconds=_frames.Zip(_frames.Skip(1),(a,b)=>(b.At-a.At)*1000).DefaultIfEmpty().Max(),
        Tracks=MotionSummary(),
        Cues=_cues.Select(c=>new{c.At,c.Kind}).ToArray(),NativeMotionSamples=true,WholeSceneCopied=false,RecordingFileChanged=false};
    private object[] MotionSummary()=>_specs.Select(entry=>
    {
        var samples=_frames.Select(f=>(f.At,Pose:f.Cards.FirstOrDefault(p=>p.Id==entry.Key))).Where(s=>s.Pose is not null).ToArray();
        var intervals=samples.Zip(samples.Skip(1),(a,b)=>new{Milliseconds=(b.At-a.At)*1000,Distance=a.Pose!.Position.DistanceTo(b.Pose!.Position)}).ToArray();
        var first=samples.FirstOrDefault().Pose;var last=samples.LastOrDefault().Pose;
        return (object)new{Id=entry.Key,Model=entry.Value.Asset.Source.Id.ToString(),Samples=samples.Length,
            MovingIntervals=intervals.Count(i=>i.Distance>1),MaximumGapMilliseconds=intervals.Select(i=>i.Milliseconds).DefaultIfEmpty().Max(),
            MaximumStepPixels=intervals.Select(i=>i.Distance).DefaultIfEmpty().Max(),
            First=first is null?null:new[]{first.Position.X,first.Position.Y},Last=last is null?null:new[]{last.Position.X,last.Position.Y},
            IndexAlignments=samples.Where(s=>s.Pose!.Index is not null).Select(s=>s.Pose!.Index!.Horizontal.ToString()).Distinct().ToArray(),
            EnergyDisplays=samples.SelectMany(s=>s.Pose!.Costs).Where(c=>c.Field=="_energyLabel").Select(c=>new{c.Text,Color=new[]{c.FontColor.R,c.FontColor.G,c.FontColor.B,c.FontColor.A}})
                .DistinctBy(c=>c.Text+string.Join(',',c.Color)).ToArray()};
    }).ToArray();
    private static void Record(string kind,Action<Renderer> fire){if(_capture is {} trace)trace._cues.Add(new(trace.Elapsed,kind,fire));}
    internal async Task Play(Func<Task> applyTarget,Func<Task>? showChoice=null,long? requestedAt=null)
    {
        Codec.Require(_capture is null&&!Replaying&&_frames.Count>0,"native-display-replay-context");
        Replaying=true;var prepared=Stopwatch.GetTimestamp();
        try
        {
            if(_warmTask is not null)await _warmTask;
            PlayerLoop.RequireHealthy();
            await DisplayAssets.Warm(_specs.Values.Select(s=>s.Asset));
            await DisplayAssets.WarmPotions(_potionModels,_potionSlots);
            var warmMilliseconds=Stopwatch.GetElapsedTime(prepared).TotalMilliseconds;
            // Older card flights have their own recorded display tail. Do not resurrect them
            // when starting this edge from a settled Back checkpoint, or draw them twice.
            var frames=_frames.Select(f=>f with{Cards=f.Cards.Where(p=>!_inheritedTails.Contains(p.Variant.Source)).ToArray()}).ToArray();
            var constructorProjection=LiveState.ChoiceProjection();
            var handTracks=frames.SelectMany(f=>f.Cards).Where(p=>p.Hand is not null).Select(p=>p.Id).Concat(_handMoves.Keys).Distinct().ToArray();
            var pendingArrivals=_handMoves.Keys.Except(frames[0].Cards.Where(p=>p.Hand is not null).Select(p=>p.Id)).ToArray();
            using var renderer=new Renderer(_specs,_potionModels,_potionSlots,_creatures,handTracks,pendingArrivals);var times=frames.Select(f=>f.At).ToArray();
            Codec.Require(constructorProjection==LiveState.ChoiceProjection(),"display-holder-construction-changed-business-or-rng");
            var constructedMilliseconds=Stopwatch.GetElapsedTime(prepared).TotalMilliseconds;
            renderer.Apply(frames[0],frames[0],0);
            var beforeRestore=Stopwatch.GetElapsedTime(prepared).TotalMilliseconds;
            // Keep real source data until the same visible choice/settlement boundary as native
            // execution. Display-only effects do not need the result state prepared up front.
            var targetApplied=false;var dataRestoreMilliseconds=0d;
            var restoredMilliseconds=Stopwatch.GetElapsedTime(prepared).TotalMilliseconds;
            renderer.Prepared();renderer.Apply(frames[0],frames[0],0);
            var preparationMilliseconds=Stopwatch.GetElapsedTime(prepared).TotalMilliseconds;
            var before=LiveState.ChoiceProjection();
            var started=Stopwatch.GetTimestamp();var frame=0;var cue=0;var pausedSeconds=0d;
            Runtime.Log("history-display-start-timing",new{RequestToPreparationMilliseconds=requestedAt is {} request?(double?)Stopwatch.GetElapsedTime(request,prepared).TotalMilliseconds:null,
                PreparationMilliseconds=Stopwatch.GetElapsedTime(prepared,started).TotalMilliseconds,FirstPlaySourceMilliseconds=_firstPlayAt*1000,
                RequestToClockMilliseconds=requestedAt is {} requestStart?(double?)Stopwatch.GetElapsedTime(requestStart,started).TotalMilliseconds:null});
            Task? choiceTask=null;double? choiceRequestedAt=null;double? choiceRequestedWallAt=null;var choiceDelivered=false;
            var previousElapsed=0d;var maximumFrameGap=0d;var maximumApplyMilliseconds=0d;var totalApplyMilliseconds=0d;var replayFrames=0;var skippedSamples=0;
            var slowCues=new List<object>();
            var interrupted=false;
            while(true)
            {
                PlayerLoop.RequireHealthy();
                var elapsed=Stopwatch.GetElapsedTime(started).TotalSeconds-pausedSeconds;
                if(replayFrames>0)maximumFrameGap=Math.Max(maximumFrameGap,(elapsed-previousElapsed)*1000);previousElapsed=elapsed;
                var oldFrame=frame;
                frame=PresentationTiming.Frame(times,elapsed,frame);
                skippedSamples+=Math.Max(0,frame-oldFrame-1);
                var next=Math.Min(frame+1,frames.Length-1);
                var applying=Stopwatch.GetTimestamp();
                renderer.Apply(frames[frame],frames[next],(float)PresentationTiming.Blend(times[frame],times[next],elapsed));
                var applyMilliseconds=Stopwatch.GetElapsedTime(applying).TotalMilliseconds;
                totalApplyMilliseconds+=applyMilliseconds;maximumApplyMilliseconds=Math.Max(maximumApplyMilliseconds,applyMilliseconds);replayFrames++;
                while(cue<_cues.Count&&_cues[cue].At<=elapsed)
                {
                    var displayEvent=_cues[cue++];
                    if(displayEvent.Kind=="choice-open")
                    {
                        Codec.Require(showChoice is not null&&choiceTask is null&&PresentationTiming.Once(_choiceEntryAt,elapsed,ref choiceDelivered),"display-choice-entry-context");
                        if(!targetApplied)
                        {
                            Codec.Require(LiveState.ChoiceProjection()==before,"native-display-replay-changed-business-or-rng");
                            var restoring=Stopwatch.GetTimestamp();await applyTarget();targetApplied=true;renderer.Prepared();
                            before=LiveState.ChoiceProjection();
                            dataRestoreMilliseconds+=Stopwatch.GetElapsedTime(restoring).TotalMilliseconds;
                            pausedSeconds+=Stopwatch.GetElapsedTime(restoring).TotalSeconds;
                        }
                        choiceRequestedAt=elapsed;choiceRequestedWallAt=Stopwatch.GetElapsedTime(started).TotalSeconds;choiceTask=showChoice!();
                        Runtime.Log("native-display-choice-entry",new{SourceAt=displayEvent.At,ActualAt=elapsed,ActualWallAt=choiceRequestedWallAt,BoundaryRestoreMilliseconds=dataRestoreMilliseconds,NativeReadinessWaitConcurrent=true,FinalTailNotAddedToOpenDelay=true});
                    }
                    else
                    {
                        var firing=Stopwatch.GetTimestamp();displayEvent.Fire(renderer);var milliseconds=Stopwatch.GetElapsedTime(firing).TotalMilliseconds;
                        if(milliseconds>=8)slowCues.Add(new{displayEvent.Kind,displayEvent.At,Milliseconds=milliseconds});
                    }
                }
                if(DecisionRoute.FinishPresentation)
                {
                    interrupted=true;renderer.Apply(frames[^1],frames[^1],0);break;
                }
                if(elapsed>=times[^1])break;await Runtime.NextFrame();
            }
            Codec.Require(LiveState.ChoiceProjection()==before,"native-display-replay-changed-business-or-rng");
            if(!targetApplied)
            {
                var restoring=Stopwatch.GetTimestamp();await applyTarget();dataRestoreMilliseconds+=Stopwatch.GetElapsedTime(restoring).TotalMilliseconds;
                targetApplied=true;before=LiveState.ChoiceProjection();
            }
            if(choiceTask is not null)await choiceTask;
            renderer.Prepared();renderer.Handoff(frames[^1],_specs,_queuedCut);
            Codec.Require(LiveState.ChoiceProjection()==before,"native-display-replay-changed-business-or-rng");
            var actual=LiveState.Player.PlayerCombatState!.Hand.Cards;
            Codec.Require(_frames[^1].Hand.SequenceEqual<CardModel>(actual,ReferenceEqualityComparer.Instance),"native-display-handoff-hand-data");
            var holders=NPlayerHand.Instance!.CardHolderContainer.GetChildren().OfType<NHandCardHolder>().Select(h=>h.CardModel);
            Codec.Require(holders.SequenceEqual<CardModel?>(actual,ReferenceEqualityComparer.Instance),"native-display-handoff-hand-holders");
            Codec.Require(HandPresentation.IndicesMatched(NPlayerHand.Instance),"native-display-handoff-hand-indices");
            if(interrupted)PotionVisuals.StopOwned();else{PotionVisuals.ContinueTails();renderer.ContinueTails();renderer.ContinueSampledCardTails(this);}
            Runtime.Log("native-display-replayed",new{Trace=Summary(),ActualDataUnchanged=true,RngUnchanged=true,NativeBusinessEffectsRedispatched=false,
                NativeVisualCuesRecreated=true,OriginalTasksResumed=false,DataAppliedBeforeAnimation=false,TargetDataAppliedUnderMask=true,TargetDataAppliedAtVisibleBoundary=true,
                HandoffHandCount=actual.Count,HandoffHandIdentityMatched=true,HandoffHandIndicesMatched=true,WholeHolderContainersMasked=true,PotionBarMasked=false,
                PresentationInterrupted=interrupted,RemainingCuesSkipped=interrupted?_cues.Count-cue:0,PreparationMilliseconds=preparationMilliseconds,
                ChoiceRequestedAt=choiceRequestedAt,ChoiceRequestedWallAt=choiceRequestedWallAt,ChoiceRequestedOnSourceClock=choiceTask is not null,TargetDataRestoredOnce=targetApplied,
                ReplayFrames=replayFrames,MaximumReplayFrameGapMilliseconds=maximumFrameGap,SkippedSourceSamples=skippedSamples,
                MaximumApplyMilliseconds=maximumApplyMilliseconds,TotalApplyMilliseconds=totalApplyMilliseconds,
                SlowVisualCues=slowCues,
                AssetWarmMilliseconds=warmMilliseconds,RendererMilliseconds=constructedMilliseconds-warmMilliseconds,
                DataRestoreMilliseconds=dataRestoreMilliseconds,FrameSetupMilliseconds=preparationMilliseconds-restoredMilliseconds+beforeRestore-constructedMilliseconds,
                DisplayModelsCreated=DisplayAssets.ModelsCreated,DisplayViewsCreated=DisplayAssets.ViewsCreated,DisplayViewsReused=DisplayAssets.LeasesReused,
                renderer.TextWrites,renderer.PropertyWrites});
        }
        finally{Replaying=false;}
    }
    private sealed class Renderer : IDisposable
    {
        internal readonly Control Root=new(){Name="ReplayNativeDisplay",MouseFilter=Control.MouseFilterEnum.Ignore};
        private readonly Control _handRoot=new(){Name="ReplayHandDisplay",MouseFilter=Control.MouseFilterEnum.Ignore};
        private readonly Control _playRoot=new(){Name="ReplayPlayDisplay",MouseFilter=Control.MouseFilterEnum.Ignore};
        private readonly Control _potionRoot=new(){Name="ReplayPotionDisplay",MouseFilter=Control.MouseFilterEnum.Ignore};
        private readonly Dictionary<int,NCard> _ghosts=[];
        private readonly Dictionary<int,DisplayAssets.View> _views=[];
        private readonly Dictionary<PotionModel,NPotion> _bottles=new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<int,TextureRect> _emptyIcons=[];
        private readonly Dictionary<int,Sprite2D> _items=[];
        internal readonly Dictionary<int,NCardTrailVfx> Trails=[];
        internal readonly HashSet<int> Owned=[];
        internal readonly List<Node> Effects=[];
        private readonly List<(NCard Card,bool Visible)> _hidden=[];
        private readonly List<(Control Node,bool Visible)> _masks=[];
        private readonly Dictionary<int,IndexLabel> _indexStyles=[];
        private readonly Dictionary<(int,string),CostLabel> _costStyles=[];
        private readonly Dictionary<(Creature,string),Control> _healthBindings=[];
        private readonly Dictionary<int,NHandCardHolder> _motionHolders=[];
        private readonly HashSet<int> _moving=[];
        private readonly Dictionary<int,HandTarget> _nativeTargets=[];
        private readonly Dictionary<int,int> _nativeIndices=[];
        private readonly HashSet<int> _pendingArrivals=[];
        private readonly Dictionary<int,PileType> _playMoves=[];
        private readonly Dictionary<Node,int> _exhaustEffects=[];
        private readonly Dictionary<Node,Control> _exhaustAnchors=[];
        private readonly HashSet<int> _continuedCards=[];
        private readonly HashSet<Node> _continuedEffects=[];
        private readonly List<Node> _powerEffects=[];
        private CreatureHudTrace.View? _hud;
        private readonly CombatHudTrace.View _combatHud=new();
        private readonly PowerHudTrace.View _powers=new();
        private bool _preparing=true;
        internal int TextWrites {get;private set;}
        internal int PropertyWrites {get;private set;}
        private void Mask(Control node){if(!_masks.Any(m=>ReferenceEquals(m.Node,node)))_masks.Add((node,node.Visible));node.Visible=false;}
        private void MaskPotionContents()
        {
            var container=NRun.Instance!.GlobalUi.TopBar.PotionContainer;
            // Keep the slots/background and the top-bar canvas. Only substitute the moving contents.
            foreach(var node in Descendants(container).OfType<NPotion>().Where(p=>!_potionRoot.IsAncestorOf(p)))Mask(node);
            foreach(var holder in (List<NPotionHolder>)Get(container,"_holders")!)Mask((Control)Get(holder,"_emptyIcon")!);
        }
        internal Renderer(Dictionary<int,CardSpec> specs,IEnumerable<PotionModel> potions,IEnumerable<int> slots,IEnumerable<Creature> creatures,IEnumerable<int> handTracks,IEnumerable<int> pendingArrivals)
        {
            try
            {
                _pendingArrivals.UnionWith(pendingArrivals);
                foreach(var card in TableCards()){_hidden.Add((card,card.Visible));card.Visible=false;}
                var hand=NPlayerHand.Instance!;
                Mask(hand.CardHolderContainer);Mask((Control)Get(hand,"_selectedHandCardContainer")!);Mask(NCombatRoom.Instance!.Ui.PlayContainer);
                // Sibling anchors preserve the native hand/backstop/played-card ordering. A top-level
                // combat-wide canvas would keep cards above the modal until the display is disposed.
                Anchor(_handRoot,hand.CardHolderContainer);Anchor(_playRoot,NCombatRoom.Instance.Ui.PlayContainer);
                NRun.Instance!.GlobalUi.TopBar.PotionContainer.AddChild(_potionRoot);MaskPotionContents();
                foreach(var node in NCombatRoom.Instance.CreatureNodes)Mask((NHealthBar)Get(Get(node,"_stateDisplay")!,"_healthBar")!);
                NCombatRoom.Instance!.Ui.AddChild(Root);
                _hud=new(creatures);_hud.MaskIntents();
                _powers.Prepare(creatures);
                _combatHud.Prepare();
                var cardSlots=new Dictionary<DisplayAssets.Asset,int>();
                foreach(var (id,spec) in specs)
                {
                    var slot=cardSlots.GetValueOrDefault(spec.Asset);cardSlots[spec.Asset]=slot+1;
                    var view=DisplayAssets.Lease(spec.Asset,slot,Root);var node=view.Card;
                    node.UpdateVisuals(spec.Pile,CardPreviewMode.Normal);node.Visible=false;view.Index.Visible=false;_ghosts.Add(id,node);_views.Add(id,view);
                    node.CardHighlight.AnimHideInstantly();
                }
                foreach(var id in handTracks)PrepareMotion(id);
                foreach(var potion in potions)_bottles.Add(potion,DisplayAssets.LeasePotion(potion,_potionRoot));
                foreach(var slot in slots)_emptyIcons.Add(slot,DisplayAssets.LeaseEmpty(slot,_potionRoot));
            }
            catch{Dispose();throw;}
        }
        private static void Anchor(Control display,Control original)
        {
            var parent=original.GetParent();parent.AddChild(display);parent.MoveChild(display,original.GetIndex()+1);
            display.ZIndex=original.ZIndex;display.ZAsRelative=original.ZAsRelative;
        }
        private NHandCardHolder PrepareMotion(int id)
        {
            if(_motionHolders.TryGetValue(id,out var existing))return existing;
            var before=_preparing?null:LiveState.ChoiceProjection();
            var card=Card(id);card.Scale=Vector2.One;
            var holder=NHandCardHolder.Create(card,NPlayerHand.Instance!);_handRoot.AddChild(holder);holder.Visible=false;
            holder.SetClickable(false);holder.MouseFilter=Control.MouseFilterEnum.Ignore;holder.Hitbox.MouseFilter=Control.MouseFilterEnum.Ignore;
            card.CardHighlight.AnimHideInstantly();_motionHolders.Add(id,holder);
            foreach(var key in _costStyles.Keys.Where(k=>k.Item1==id).ToArray())_costStyles.Remove(key);
            if(before is not null)Codec.Require(before==LiveState.ChoiceProjection(),"display-holder-reentry-changed-business-or-rng");
            return holder;
        }
        internal void Prepared()
        {
            MaskPotionContents();_hud!.MaskIntents();_powers.Prepare(NCombatRoom.Instance!.CreatureNodes.Select(n=>n.Entity));_combatHud.Prepare();_healthBindings.Clear();
            foreach(var node in NCombatRoom.Instance!.CreatureNodes)Mask((NHealthBar)Get(Get(node,"_stateDisplay")!,"_healthBar")!);
            _preparing=false;
        }
        internal NCard Card(int id)=>_ghosts[id];
        internal void PowerEffect(Node? effect)
        {
            if(effect is null)return;_powerEffects.Add(effect);NCombatRoom.Instance!.CombatVfxContainer.AddChild(effect);
        }
        internal void Shuffle(ShufflePose pose)
        {
            var effect=PreloadManager.Cache.GetScene(NCardFlyShuffleVfx.AssetPaths.Single()).Instantiate<NCardFlyShuffleVfx>();
            void Set(string field,object value)=>AccessTools.Field(typeof(NCardFlyShuffleVfx),field).SetValue(effect,value);
            Set("_startPos",pose.Start);Set("_endPos",pose.End);Set("_trailPath",pose.Trail);
            // The native completion notification belongs only to this unsubscribed display pile.
            // Never hand the coroutine a real combat pile that Back can replace underneath it.
            Set("_targetPile",new CardPile(pose.Target));
            var parent=NCombatRoom.Instance!.CombatVfxContainer;
            effect.Position=parent.GetGlobalTransform().AffineInverse()*pose.Start;
            PowerEffect(effect);
            Set("_controlPointOffset",pose.Offset);Set("_speed",pose.Speed);Set("_accel",pose.Acceleration);
            Set("_arcDir",pose.Arc);Set("_duration",pose.Duration);
            if(Get(effect,"_vfx") is Node trail)_powerEffects.Add(trail);
            Runtime.Log("native-shuffle-flight-replayed",new{pose.Target,NativeSceneAndMotion=true,StartPositionSeededBeforeTree=true,
                NativeTrailCount=Get(effect,"_vfx") is NCardTrailVfx?1:0,ActualPileNotification=false,ShuffleBusinessRedispatched=false});
        }
        internal void CardTrail(int id,string path)
        {
            var card=Card(id);var effect=NCardTrailVfx.Create(card,path);if(effect is null)return;
            effect.Position=Root.GetGlobalTransform().AffineInverse()*card.GlobalPosition;
            Root.AddChild(effect);Trails[id]=effect;
        }
        internal void StarIncrease(int oldStars,int newStars)
        {
            var stars=(Node)Get(NCombatRoom.Instance!.Ui,"_starCounter")!;var previous=stars.GetChildren().ToHashSet();
            CombatHudTrace.StarIncrease(oldStars,newStars);
            foreach(var effect in stars.GetChildren().Where(n=>!previous.Contains(n)))_powerEffects.Add(effect);
        }
        internal void SmithCard(int id)
        {
            var effect=NCardSmithVfx.Create(Card(id),false);if(effect is null)return;
            _powerEffects.Add(effect);NRun.Instance!.GlobalUi.AboveTopBarVfxContainer.AddChild(effect);
        }
        internal void SmithCards(DisplayAssets.Asset[] cards)
        {
            var effect=NCardSmithVfx.Create(cards.Select(c=>c.Model),false);if(effect is null)return;
            _powerEffects.Add(effect);NRun.Instance!.GlobalUi.CardPreviewContainer.AddChild(effect);
        }
        internal void BlockBreak(Creature creature,Transform2D transform,Color color)
        {
            var node=NCombatRoom.Instance?.GetCreatureNode(creature);if(node is null)return;
            var bar=(NHealthBar)Get(Get(node,"_stateDisplay")!,"_healthBar")!;
            NBlockBrokenVfx? effect;
            _creatingBlockBreakDisplay=true;
            try{effect=NBlockBrokenVfx.Create();}finally{_creatingBlockBreakDisplay=false;}
            if(effect is null)return;
            _powerEffects.Add(effect);bar.AddChild(effect);effect.Transform=transform;effect.Modulate=color;
            Runtime.Log("native-block-break-replayed",new{Creature=creature.ModelId.ToString(),NativeScene=true,OriginalHealthBarParent=true,GameplayRedispatched=false});
        }
        internal void StartHandMove(HandMove move)
        {
            _playMoves.Remove(move.Id);Card(move.Id).PlayPileTween?.Kill();
            _pendingArrivals.Remove(move.Id);
            if(!_moving.Contains(move.Id))BeginHandMotion(move.Id,move.VisibleStart);
        }
        internal void StartPlayMove(PlayMove move)
        {
            ReleaseMotion(move.Id);_pendingArrivals.Remove(move.Id);_playMoves[move.Id]=move.ActualPile;
            var card=Card(move.Id);if(card.GetParent()!=_playRoot)card.Reparent(_playRoot);
            var transform=_playRoot.GetGlobalTransform();card.GlobalPosition=move.Position;
            card.Scale=move.Scale/transform.Scale;card.Rotation=move.Angle-transform.Rotation;card.Visible=true;
            card.PlayPileTween?.Kill();card.PlayPileTween=null;
            if(move.CardEntry)card.AnimCardToPlayPile();
            else
            {
                var tween=card.CreateTween().SetParallel();card.PlayPileTween=tween;
                // This native helper only configures position/scale tweens. Its caller's pile,
                // queue, callbacks and business effects are deliberately not dispatched.
                AccessTools.Method(typeof(CardPileCmd),"AppendPileLerpTween").Invoke(null,[tween,card,PileType.Play,move.From]);
            }
        }
        internal void PileAdded(PileType type,int count)
        {
            var ui=NCombatRoom.Instance!.Ui;var button=type switch{PileType.Draw=>(NCombatCardPile)ui.DrawPile,PileType.Discard=>ui.DiscardPile,_=>ui.ExhaustPile};
            var previous=_pileDisplayOverride;_pileDisplayOverride=(button,count);
            try{AccessTools.Method(button.GetType(),"AddCard").Invoke(button,null);}
            finally{_pileDisplayOverride=previous;}
            RoutePresentation.Access();
        }
        private void BeginHandMotion(int id,HandPose pose)
        {
            var holder=PrepareMotion(id);var card=Card(id);var transform=_handRoot.GetGlobalTransform();
            holder.Visible=true;card.Visible=true;card.Position=pose.CardPosition;card.Rotation=pose.CardAngle;card.Scale=pose.CardScale;
            holder.Scale=pose.Current.Scale/transform.Scale;holder.Rotation=pose.Current.Angle-transform.Rotation;holder.GlobalPosition=pose.Current.Position;
            _moving.Add(id);SetHandTarget(id,pose.Target);
            holder.ZIndex=pose.ZIndex;holder.ZAsRelative=pose.ZAsRelative;
            _handRoot.MoveChild(holder,Math.Clamp(pose.Order,0,_handRoot.GetChildCount()-1));
        }
        private void SetHandTarget(int id,HandTarget target)
        {
            var holder=_motionHolders[id];var transform=_handRoot.GetGlobalTransform();
            var local=new HandTarget(transform.AffineInverse()*target.Position,target.Scale/transform.Scale,target.Angle-transform.Rotation);
            if(_nativeTargets.TryGetValue(id,out var previous)&&previous==local)return;
            holder.SetTargetPosition(local.Position);
            holder.SetTargetAngle(Mathf.RadToDeg(local.Angle));holder.SetTargetScale(local.Scale);
            _nativeTargets[id]=local;
        }
        private void ReleaseMotion(int id)
        {
            if(!_motionHolders.Remove(id,out var holder))return;
            var card=Card(id);
            AccessTools.Method(typeof(NHandCardHolder),"StopAnimations").Invoke(holder,null);
            if(GodotObject.IsInstanceValid(card)&&!card.IsQueuedForDeletion())
            {
                AccessTools.Method(typeof(NHandCardHolder),"UnsubscribeFromEvents").Invoke(holder,[card.Model]);
                card.ModelChanged-=AccessTools.Method(typeof(NHandCardHolder),"OnModelChanged").CreateDelegate<Action<CardModel?>>(holder);
                card.Reparent(Root);
            }
            holder.GetParent()?.RemoveChild(holder);holder.QueueFree();_moving.Remove(id);_nativeTargets.Remove(id);_nativeIndices.Remove(id);
        }
        internal void Handoff(Frame frame,Dictionary<int,CardSpec> specs,bool queuedCheckpoint=false)
        {
            var queuedHand=queuedCheckpoint&&frame.Cards.Any(p=>p.ActualPile==PileType.Hand&&p.Hand is null);
            var ordered=frame.Cards.Where(p=>p.Hand is not null&&_moving.Contains(p.Id)&&Card(p.Id).Visible).OrderBy(p=>p.Hand!.Order).ToArray();
            var actual=ordered.OrderBy(p=>_motionHolders[p.Id].GetIndex()).Select(p=>p.Id);
            Codec.Require(actual.SequenceEqual(ordered.Select(p=>p.Id)),"native-display-hand-stack-order");
            Codec.Require(ordered.All(p=>_motionHolders[p.Id].ZIndex==p.Hand!.ZIndex&&_motionHolders[p.Id].ZAsRelative==p.Hand.ZAsRelative),"native-display-hand-stack-depth");
            Runtime.Log("native-hand-stack-verified",new{Tracks=ordered.Select(p=>new{p.Id,Model=specs[p.Id].Asset.Source.Id.ToString(),p.Hand!.Order,p.Hand.ZIndex}).ToArray(),
                SourceOrderMatched=true,SourceHoverDepthMatched=true,CreationOrderUsed=false});
            var cards=0;var continuedHands=0;var unsettledHands=0;var maximumHandRemainder=0f;
            foreach(var pose in frame.Cards)
            {
                var source=specs[pose.Id].Asset.Source;var native=NCard.FindOnTable(source);
                if(native is null||!GodotObject.IsInstanceValid(native)||native.IsQueuedForDeletion())continue;
                // A queued card is still logically in Hand but has no source holder. Restoring
                // this intermediate node must leave an ordinary usable hand, not a phantom queue.
                if(queuedHand&&pose.ActualPile==PileType.Hand&&native.GetParent() is NHandCardHolder)
                {HandPresentation.FinishHighlight(native);continue;}
                native.ZIndex=pose.ZIndex;native.ZAsRelative=pose.ZAsRelative;
                if(pose.Hand is {} sourceHand&&native.GetParent() is NHandCardHolder sourceHolder)
                {sourceHolder.ZIndex=sourceHand.ZIndex;sourceHolder.ZAsRelative=sourceHand.ZAsRelative;}
                if(_moving.Contains(pose.Id)&&native.GetParent() is NHandCardHolder target)
                {
                    // The logical checkpoint is settled; its fresh native view may still be moving.
                    // Transfer the current display pose instead of revealing its snapped endpoint.
                    var shown=_motionHolders[pose.Id];var remaining=shown.Position.DistanceTo(shown.TargetPosition);
                    if(remaining>2)unsettledHands++;maximumHandRemainder=Math.Max(maximumHandRemainder,remaining);
                    HandPresentation.ContinueFrom(target,shown);continuedHands++;
                }
                var glow=native.CardHighlight;
                if(Get(glow,"_curTween") is Tween tween&&GodotObject.IsInstanceValid(tween))tween.Kill();
                ((ShaderMaterial)glow.Material).SetShaderParameter(NativeDisplayTrace.Name("width"),pose.Highlight.Width);glow.Modulate=pose.Highlight.Color;glow.Visible=pose.Highlight.Visible;
                foreach(var cost in pose.Costs)
                {
                    var label=(MegaLabel)Get(native,cost.Field)!;label.SetTextAutoSize(cost.Text);
                    label.AddThemeColorOverride(NativeDisplayTrace.Name("font_color"),cost.FontColor);label.AddThemeColorOverride(NativeDisplayTrace.Name("font_outline_color"),cost.Outline);
                }
                cards++;
            }
            _hud!.Handoff(frame.Hud);
            _powers.Handoff(frame.Powers);
            _combatHud.Handoff(frame.CombatHud);
            ApplyHealth(frame.Health);
            HealthDisplayTail.Continue(frame.HealthTails);
            HandoffPotionSlots(frame.EmptySlots);
            if(frame.Exit is {} exit){NPlayerHand.Instance!.Position=exit.Position;NPlayerHand.Instance.Modulate=exit.Color;}
            Runtime.Log("native-display-handoff",new{Cards=cards,HandMotionsContinued=continuedHands,UnsettledHands=unsettledHands,MaximumRemainingHandPixels=maximumHandRemainder,
                HighlightTweenRestarted=false,IntentClockTransferred=true,NativeOverlayLayering=true,TopLevelCardCanvas=false});
        }
        internal void Apply(Frame from,Frame to,float blend)
        {
            var next=to.Cards.ToDictionary(p=>p.Id);var visible=from.Cards.Select(p=>p.Id).ToHashSet();
            foreach(var (id,card) in _ghosts)if(!Owned.Contains(id)&&GodotObject.IsInstanceValid(card)&&card.Visible!=visible.Contains(id)){card.Visible=visible.Contains(id);PropertyWrites++;}
            foreach(var (id,holder) in _motionHolders)holder.Visible=visible.Contains(id)&&!_pendingArrivals.Contains(id)&&!Owned.Contains(id);
            foreach(var (id,view) in _views)if(!visible.Contains(id)&&view.Index.Visible)view.Index.Visible=false;
            foreach(var pose in from.Cards)
            {
                if(Owned.Contains(pose.Id))continue;var card=Card(pose.Id);if(!GodotObject.IsInstanceValid(card))continue;
                card.ZIndex=pose.ZIndex;card.ZAsRelative=pose.ZAsRelative;
                var variantChanged=!ReferenceEquals(card.Model,pose.Variant.Model);
                if(variantChanged)card.Model=pose.Variant.Model;
                if(variantChanged||card.DisplayingPile!=pose.Pile)
                {card.UpdateVisuals(pose.Pile,CardPreviewMode.Normal);foreach(var cost in pose.Costs)_costStyles.Remove((pose.Id,cost.Field));}
                var b=next.GetValueOrDefault(pose.Id,pose);
                if(_pendingArrivals.Contains(pose.Id)){card.Visible=false;_views[pose.Id].Index.Visible=false;continue;}
                if(_playMoves.TryGetValue(pose.Id,out var playPile)&&pose.ActualPile!=playPile&&pose.ActualPile!=PileType.Hand)
                {_playMoves.Remove(pose.Id);card.PlayPileTween?.Kill();card.PlayPileTween=null;}
                if(from.Exit is not null&&_motionHolders.ContainsKey(pose.Id))ReleaseMotion(pose.Id);
                if(from.Exit is null&&pose.Hand is not null&&!_moving.Contains(pose.Id)&&!_playMoves.ContainsKey(pose.Id))BeginHandMotion(pose.Id,pose.Hand);
                if(pose.Hand is null&&_motionHolders.ContainsKey(pose.Id)&&(!_moving.Contains(pose.Id)||pose.Pile!=PileType.Hand))ReleaseMotion(pose.Id);
                var nativeMotion=_moving.Contains(pose.Id);
                if(!nativeMotion&&!_playMoves.ContainsKey(pose.Id))
                {
                var parent=pose.Pile==PileType.Play?_playRoot:Root;if(card.GetParent()!=parent)card.Reparent(parent);
                var transform=parent.GetGlobalTransform();
                Scale(card,pose.Scale.Lerp(b.Scale,blend)/transform.Scale);
                var rotation=Mathf.LerpAngle(pose.Rotation,b.Rotation,blend)-transform.Rotation;if(!Mathf.IsEqualApprox(card.Rotation,rotation)){card.Rotation=rotation;PropertyWrites++;}
                Position(card,pose.Position.Lerp(b.Position,blend));
                }
                else if(nativeMotion)
                {
                    var holder=_motionHolders[pose.Id];holder.Hitbox.MouseFilter=Control.MouseFilterEnum.Ignore;
                    if(pose.Hand is {} handPose)
                    {
                        SetHandTarget(pose.Id,handPose.Target);
                        var handNext=b.Hand??handPose;
                        card.Position=handPose.CardPosition.Lerp(handNext.CardPosition,blend);
                        card.Scale=handPose.CardScale.Lerp(handNext.CardScale,blend);
                        card.Rotation=Mathf.LerpAngle(handPose.CardAngle,handNext.CardAngle,blend);
                    }
                }
                var parentColor=EffectiveColor((CanvasItem)card.GetParent());var color=pose.Color.Lerp(b.Color,blend);
                Color(card,new(parentColor.R==0?0:color.R/parentColor.R,parentColor.G==0?0:color.G/parentColor.G,parentColor.B==0?0:color.B/parentColor.B,parentColor.A==0?0:color.A/parentColor.A));
                Scale(card.Body,pose.BodyScale.Lerp(b.BodyScale,blend));Color(card.Body,pose.BodyColor.Lerp(b.BodyColor,blend));
                var glow=card.CardHighlight;Visible(glow,pose.Highlight.Visible);Color(glow,pose.Highlight.Color.Lerp(b.Highlight.Color,blend));
                ((ShaderMaterial)glow.Material).SetShaderParameter(NativeDisplayTrace.Name("width"),Mathf.Lerp(pose.Highlight.Width,b.Highlight.Width,blend));
                foreach(var cost in pose.Costs)
                {
                    var key=(pose.Id,cost.Field);
                    var text=(MegaLabel)Get(card,cost.Field)!;
                    if(!_costStyles.TryGetValue(key,out var previousCost)||previousCost.FontColor!=cost.FontColor||previousCost.Outline!=cost.Outline)
                    {text.AddThemeColorOverride(NativeDisplayTrace.Name("font_color"),cost.FontColor);text.AddThemeColorOverride(NativeDisplayTrace.Name("font_outline_color"),cost.Outline);}
                    if(text.Text!=cost.Text){text.SetTextAutoSize(cost.Text);TextWrites++;}
                    Color(text,cost.Color);Visible(text,cost.Visible);_costStyles[key]=cost;
                }
                foreach(var icon in pose.CostIcons)Visible((Control)Get(card,icon.Field)!,icon.Visible);
                var label=_views[pose.Id].Index;
                if(nativeMotion)
                {
                    label.Visible=false;
                    var nativeIndex=pose.Index is {} number&&int.TryParse(number.Text,out var indexValue)?indexValue:0;
                    if(!_nativeIndices.TryGetValue(pose.Id,out var previousIndex)||previousIndex!=nativeIndex)
                    {_motionHolders[pose.Id].SetIndexLabel(nativeIndex);_nativeIndices[pose.Id]=nativeIndex;}
                    continue;
                }
                if(pose.Index is {} index)
                {
                    var transform=Root.GetGlobalTransform();
                    Visible(label,true);if(label.ZIndex!=1)label.ZIndex=1;
                    if(!_indexStyles.TryGetValue(pose.Id,out var previous)||previous.Font!=index.Font||previous.FontSize!=index.FontSize||previous.FontColor!=index.FontColor||previous.Outline!=index.Outline||previous.OutlineSize!=index.OutlineSize)
                    {
                        label.AddThemeFontOverride(NativeDisplayTrace.Name("font"),index.Font);label.AddThemeFontSizeOverride(NativeDisplayTrace.Name("font_size"),index.FontSize);
                        label.AddThemeColorOverride(NativeDisplayTrace.Name("font_color"),index.FontColor);label.AddThemeColorOverride(NativeDisplayTrace.Name("font_outline_color"),index.Outline);label.AddThemeConstantOverride(NativeDisplayTrace.Name("outline_size"),index.OutlineSize);_indexStyles[pose.Id]=index;
                    }
                    if(label.Text!=index.Text){label.Text=index.Text;TextWrites++;}
                    label.HorizontalAlignment=index.Horizontal;label.VerticalAlignment=index.Vertical;
                    if(!label.Size.IsEqualApprox(index.Size))label.Size=index.Size;
                    if(!label.PivotOffset.IsEqualApprox(index.Pivot))label.PivotOffset=index.Pivot;
                    var targetIndex=b.Index??index;
                    Scale(label,index.Scale.Lerp(targetIndex.Scale,blend)/transform.Scale);Color(label,index.Color.Lerp(targetIndex.Color,blend));
                    var indexRotation=Mathf.LerpAngle(index.Rotation,targetIndex.Rotation,blend)-transform.Rotation;
                    if(!Mathf.IsEqualApprox(label.Rotation,indexRotation))label.Rotation=indexRotation;
                    Position(label,index.Position.Lerp(targetIndex.Position,blend));
                }
                else Visible(label,false);
            }
            var bottleModels=from.Bottles.Select(b=>b.Model).ToHashSet();
            foreach(var (model,node) in _bottles)Visible(node,bottleModels.Contains(model));
            foreach(var bottle in from.Bottles)
            {
                var node=_bottles[bottle.Model];var b=to.Bottles.FirstOrDefault(v=>ReferenceEquals(v.Model,bottle.Model))??bottle;
                var transform=_potionRoot.GetGlobalTransform();
                node.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
                if(!node.Size.IsEqualApprox(bottle.Size))node.Size=bottle.Size;
                if(!node.PivotOffset.IsEqualApprox(bottle.Pivot))node.PivotOffset=bottle.Pivot;
                Scale(node,bottle.Scale.Lerp(b.Scale,blend)/transform.Scale);
                var rotation=Mathf.LerpAngle(bottle.Rotation,b.Rotation,blend)-transform.Rotation;
                if(!Mathf.IsEqualApprox(node.Rotation,rotation)){node.Rotation=rotation;PropertyWrites++;}Position(node,bottle.Position.Lerp(b.Position,blend));Color(node,bottle.Color.Lerp(b.Color,blend));
                var container=(Control)Get(node,"_container")!;var position=bottle.ContainerPosition.Lerp(b.ContainerPosition,blend);
                foreach(var part in bottle.Parts)
                {
                    if(node.GetNodeOrNull<Control>(part.Path) is not {} control)continue;
                    control.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
                    if(control is TextureRect texture){texture.ExpandMode=part.Expand!.Value;texture.StretchMode=part.Stretch!.Value;}
                    control.CustomMinimumSize=part.Minimum;control.Size=part.Size;control.PivotOffset=part.Pivot;
                    // Container movement is sampled independently above (drinking/bounce).
                    if(!ReferenceEquals(control,container)){control.Position=part.Position;control.Scale=part.Scale;}
                }
                // Layout/anchor changes must finish before the moving container's pose is set.
                if(!container.Position.IsEqualApprox(position)){container.Position=position;PropertyWrites++;}
                Scale(container,bottle.ContainerScale.Lerp(b.ContainerScale,blend));Color(container,bottle.ContainerColor.Lerp(b.ContainerColor,blend));
                Position(node,bottle.Position.Lerp(b.Position,blend));
                Visible(node.Outline,bottle.Outline);
            }
            foreach(var empty in from.EmptySlots)
            {
                var node=_emptyIcons[empty.Slot];var b=to.EmptySlots.FirstOrDefault(e=>e.Slot==empty.Slot)??empty;
                node.ExpandMode=empty.Expand;node.StretchMode=empty.Stretch;node.CustomMinimumSize=empty.Minimum;
                if(node.Texture!=empty.Texture)node.Texture=empty.Texture;Visible(node,empty.Visible);if(!node.Size.IsEqualApprox(empty.Size))node.Size=empty.Size;
                if(!node.PivotOffset.IsEqualApprox(empty.Pivot))node.PivotOffset=empty.Pivot;
                Scale(node,empty.Scale.Lerp(b.Scale,blend)/_potionRoot.GetGlobalTransform().Scale);
                var rotation=empty.Rotation-_potionRoot.GetGlobalTransform().Rotation;if(!Mathf.IsEqualApprox(node.Rotation,rotation))node.Rotation=rotation;
                Position(node,empty.Position.Lerp(b.Position,blend));
                Color(node,empty.Color.Lerp(b.Color,blend));
            }
            var visibleItems=from.Items.Select(i=>i.Id).ToHashSet();
            foreach(var (id,sprite) in _items)sprite.Visible=visibleItems.Contains(id);
            foreach(var item in from.Items)
            {
                if(!_items.TryGetValue(item.Id,out var sprite)){sprite=new Sprite2D();Root.AddChild(sprite);_items.Add(item.Id,sprite);}
                var b=to.Items.FirstOrDefault(i=>i.Id==item.Id)??item;var transform=Root.GetGlobalTransform();
                sprite.Texture=item.Texture;sprite.Visible=true;sprite.GlobalPosition=item.Position.Lerp(b.Position,blend);
                sprite.Scale=item.Scale.Lerp(b.Scale,blend)/transform.Scale;
                sprite.Rotation=Mathf.LerpAngle(item.Rotation,b.Rotation,blend)-transform.Rotation;sprite.Modulate=item.Color.Lerp(b.Color,blend);
            }
            ApplyHandOrder(from);
            if(_preparing)return;
            _combatHud.Apply(from.CombatHud,to.CombatHud,blend);
            _powers.Apply(from.Powers,to.Powers,blend);
            _hud!.Apply(from.Hud,to.Hud,blend);
            ApplyHealth(from.Health);
            var ui=NCombatRoom.Instance!.Ui;
            foreach(var value in from.Piles)
            {
                var button=value.Type switch{PileType.Draw=>(NCombatCardPile)ui.DrawPile,PileType.Discard=>ui.DiscardPile,_=>ui.ExhaustPile};
                if((int)Get(button,"_currentCount")!!=value.Count)AccessTools.Field(typeof(NCombatCardPile),"_currentCount").SetValue(button,value.Count);
                var label=(MegaLabel)Get(button,"_countLabel")!;var text=value.Count.ToString();if(label.Text!=text){label.SetTextAutoSize(text);TextWrites++;}
            }
            PotionVisuals.Apply(from.Liquids.Select(l=>l with{Influence=Mathf.Lerp(l.Influence,to.Liquids.FirstOrDefault(n=>ReferenceEquals(n.Creature,l.Creature))?.Influence??0,blend)}).ToArray());
        }
        private void ApplyHealth(IEnumerable<Health> samples)
        {
            foreach(var value in samples)
            {
                var key=(value.Creature,value.Path);
                if(!_healthBindings.TryGetValue(key,out var control))
                {
                    var node=NCombatRoom.Instance!.GetCreatureNode(value.Creature);if(node is null)continue;
                    var bar=(NHealthBar)Get(Get(node,"_stateDisplay")!,"_healthBar")!;
                    control=value.Path=="."?bar:bar.GetNodeOrNull<Control>(value.Path);if(control is null)continue;_healthBindings[key]=control;
                }
                if(!GodotObject.IsInstanceValid(control)||control.IsQueuedForDeletion())continue;
                if(!control.Position.IsEqualApprox(value.Position)){control.Position=value.Position;PropertyWrites++;}
                if(!control.Size.IsEqualApprox(value.Size)){control.Size=value.Size;PropertyWrites++;}
                Color(control,value.Color);if(!control.SelfModulate.IsEqualApprox(value.SelfColor)){control.SelfModulate=value.SelfColor;PropertyWrites++;}
                if(control.Visible!=value.Visible){control.Visible=value.Visible;PropertyWrites++;}
                if(control is MegaLabel label)
                {
                    if(value.Text is not null&&label.Text!=value.Text){label.SetTextAutoSize(value.Text);TextWrites++;}
                    if(value.FontColor is {} font&&label.GetThemeColor(NativeDisplayTrace.Name("font_color"))!=font){label.AddThemeColorOverride(NativeDisplayTrace.Name("font_color"),font);PropertyWrites++;}
                    if(value.Outline is {} outline&&label.GetThemeColor(NativeDisplayTrace.Name("font_outline_color"))!=outline){label.AddThemeColorOverride(NativeDisplayTrace.Name("font_outline_color"),outline);PropertyWrites++;}
                }
            }
        }
        private void ApplyHandOrder(Frame frame)
        {
            var index=0;
            foreach(var pose in frame.Cards.Where(p=>p.Hand is not null&&!Owned.Contains(p.Id)).OrderBy(p=>p.Hand!.Order))
            {
                var hand=pose.Hand!;
                if(_moving.Contains(pose.Id)&&_motionHolders.TryGetValue(pose.Id,out var holder))
                {
                    holder.ZIndex=hand.ZIndex;holder.ZAsRelative=hand.ZAsRelative;
                    if(holder.GetIndex()!=index)_handRoot.MoveChild(holder,index);
                    index++;
                }
                else if(frame.Exit is not null&&Card(pose.Id).GetParent()==Root)
                {
                    var card=Card(pose.Id);
                    card.ZIndex=pose.ZAsRelative?pose.ZIndex+hand.ZIndex:pose.ZIndex;
                    if(card.GetIndex()!=index)Root.MoveChild(card,index);
                    index++;
                }
            }
        }
        private static void HandoffPotionSlots(EmptySlot[] samples)
        {
            var holders=(List<NPotionHolder>)Get(NRun.Instance!.GlobalUi.TopBar.PotionContainer,"_holders")!;
            foreach(var sample in samples)
            {
                if(sample.Slot>=holders.Count)continue;
                var holder=holders[sample.Slot];var icon=(TextureRect)Get(holder,"_emptyIcon")!;
                if(Get(holder,"_emptyPotionTween") is Tween old&&GodotObject.IsInstanceValid(old))old.Kill();
                icon.Modulate=sample.LocalColor;icon.SelfModulate=sample.SelfColor;
                if(sample.FadeElapsed is not {} elapsed)continue;
                var tween=holder.CreateTween();tween.TweenProperty(icon,"modulate",Colors.White,.2).From(Colors.Transparent).SetDelay(.2);
                AccessTools.Field(typeof(NPotionHolder),"_emptyPotionTween").SetValue(holder,tween);tween.CustomStep(elapsed);
            }
            Runtime.Log("native-potion-slot-handoff",new{Slots=samples.Length,ContinuedFades=samples.Count(s=>s.FadeElapsed is not null),SourceBrightnessRetained=true});
        }
        private void Position(Control node,Vector2 value){if(!node.GlobalPosition.IsEqualApprox(value)){node.GlobalPosition=value;PropertyWrites++;}}
        private void Scale(Control node,Vector2 value){if(!node.Scale.IsEqualApprox(value)){node.Scale=value;PropertyWrites++;}}
        private void Color(CanvasItem node,Color value){if(!node.Modulate.IsEqualApprox(value)){node.Modulate=value;PropertyWrites++;}}
        private void Visible(CanvasItem node,bool value){if(node.Visible!=value){node.Visible=value;PropertyWrites++;}}
        internal void Exhaust(int id,bool quick)
        {
            ReleaseMotion(id);_playMoves.Remove(id);Card(id).PlayPileTween?.Kill();Card(id).PlayPileTween=null;
            Owned.Add(id);_views[id].Index.Visible=false;var card=Card(id);card.Visible=true;
            var anchor=new Control{Name="ReplayExhaustTail",MouseFilter=Control.MouseFilterEnum.Ignore};
            Anchor(anchor,NCombatRoom.Instance!.Ui.PlayContainer);
            if(quick)
            {
                card.Reparent(anchor);
                var fx=NCardExhaustQuickVfx.Create(card);if(fx is not null){Effects.Add(fx);_exhaustEffects.Add(fx,id);_exhaustAnchors.Add(fx,anchor);_ = Observe(fx.PlayAnimation());}
            }
            else
            {
                var fx=NCardExhaustVfx.Create(card);if(fx is not null){Effects.Add(fx);_exhaustEffects.Add(fx,id);_exhaustAnchors.Add(fx,anchor);anchor.AddChild(fx);_ = Observe(fx.PlayAnimation());}
            }
        }
        internal void ContinueTails()
        {
            foreach(var effect in _powerEffects)if(GodotObject.IsInstanceValid(effect)&&!effect.IsQueuedForDeletion())
            {if(effect is NCardSmithVfx)ContinuedSmiths.Add(effect);else TrackTail(effect);_continuedEffects.Add(effect);}
            foreach(var (effect,id) in _exhaustEffects)
            {
                if(!GodotObject.IsInstanceValid(effect)||effect.IsQueuedForDeletion())continue;
                var card=Card(id);var anchor=_exhaustAnchors[effect];TrackTail(anchor);
                // Already attached to its persistent native-order anchor before PlayAnimation.
                // Reparenting an active normal effect would invoke its destructive ExitTree.
                if(GodotObject.IsInstanceValid(card)&&!card.IsQueuedForDeletion())_continuedCards.Add(id);
                TrackTail(effect);_continuedEffects.Add(effect);
            }
            Runtime.Log("native-exhaust-tails-continued",new{Effects=_continuedEffects.Count,Cards=_continuedCards.Count,WaitedForDecorativeTails=false,NativeDelayedFreeRetained=true});
        }
        internal void ContinueSampledCardTails(NativeDisplayTrace trace)
        {
            foreach(var id in trace._tailModels.Values.Distinct())
            {
                if(Owned.Contains(id)||!_ghosts.TryGetValue(id,out var card)||!GodotObject.IsInstanceValid(card))continue;
                ReleaseMotion(id);card.PlayPileTween?.Kill();card.PlayPileTween=null;
                var anchor=new Control{Name="ReplayQueuedCardTail",MouseFilter=Control.MouseFilterEnum.Ignore};
                Anchor(anchor,NCombatRoom.Instance!.Ui.PlayContainer);card.Reparent(anchor);TrackTail(anchor);_continuedCards.Add(id);
                _views[id].Index.Visible=false;
                Trails.TryGetValue(id,out var trail);
                if(trail is not null&&GodotObject.IsInstanceValid(trail))trail.Reparent(anchor);
                _=Observe(PlaySampledCardTail(anchor,card,trail,id,trace._frames[^1].At,trace._cardTailFrames.ToArray(),trace._tailTrailFades.GetValueOrDefault(id,double.PositiveInfinity)));
            }
            if(trace._tailModels.Count>0)Runtime.Log("queued-card-tails-continued",new{Cards=trace._tailModels.Count,Samples=trace._cardTailFrames.Count,
                NormalClock=true,BusinessRedispatched=false,SourceOnlyCardMotion=true});
        }
        private static async Task PlaySampledCardTail(Control anchor,NCard card,NCardTrailVfx? trail,int id,double origin,CardTailFrame[] frames,double fadeAt)
        {
            if(frames.Length==0)return;
            var started=Stopwatch.GetTimestamp();var index=0;Task? fading=null;
            bool Alive()=>GodotObject.IsInstanceValid(anchor)&&!anchor.IsQueuedForDeletion()&&anchor.IsInsideTree()&&GodotObject.IsInstanceValid(card);
            while(Alive())
            {
                var elapsed=origin+Stopwatch.GetElapsedTime(started).TotalSeconds;
                while(index+1<frames.Length&&frames[index+1].At<=elapsed)index++;
                var pose=frames[index].Cards.FirstOrDefault(p=>p.Id==id);
                if(pose is null)break;
                var next=Math.Min(index+1,frames.Length-1);var target=frames[next].Cards.FirstOrDefault(p=>p.Id==id)??pose;
                var blend=(float)PresentationTiming.Blend(frames[index].At,frames[next].At,elapsed);
                var transform=anchor.GetGlobalTransform();
                card.GlobalPosition=pose.Position.Lerp(target.Position,blend);card.Scale=pose.Scale.Lerp(target.Scale,blend)/transform.Scale;
                card.Rotation=Mathf.LerpAngle(pose.Rotation,target.Rotation,blend)-transform.Rotation;
                card.Modulate=pose.Color.Lerp(target.Color,blend);card.Body.Scale=pose.BodyScale.Lerp(target.BodyScale,blend);
                card.Body.Modulate=pose.BodyColor.Lerp(target.BodyColor,blend);card.Visible=true;
                if(elapsed>=fadeAt&&fading is null&&trail is not null&&GodotObject.IsInstanceValid(trail))fading=trail.FadeOut();
                if(elapsed>=frames[^1].At)break;
                await Runtime.NextFrame();
            }
            if(!Alive())return;
            card.Visible=false;
            if(fading is null&&trail is not null&&GodotObject.IsInstanceValid(trail)&&!trail.IsQueuedForDeletion())fading=trail.FadeOut();
            if(fading is not null)await fading;
            if(GodotObject.IsInstanceValid(anchor)&&!anchor.IsQueuedForDeletion()){OwnedTails.Remove(anchor);anchor.GetParent()?.RemoveChild(anchor);anchor.QueueFree();}
        }
        private static async Task Observe(Task task)
        {
            try{await task;}catch(ObjectDisposedException){}catch(OperationCanceledException){}
            catch(Exception ex){PlayerLoop.Error="native-display-visual-task-"+ex.GetBaseException().Message;Runtime.Log("native-display-visual-task-failed",new{Reason=PlayerLoop.Error});}
        }
        public void Dispose()
        {
            foreach(var id in _motionHolders.Keys.ToArray())ReleaseMotion(id);
            _hud?.Dispose();
            _powers.Dispose();
            _combatHud.Dispose();
            foreach(var effect in Effects.Where(e=>!_continuedEffects.Contains(e)&&GodotObject.IsInstanceValid(e)&&!e.IsQueuedForDeletion())){effect.GetParent()?.RemoveChild(effect);effect.QueueFree();}
            foreach(var effect in _powerEffects.Where(e=>!_continuedEffects.Contains(e)&&GodotObject.IsInstanceValid(e)&&!e.IsQueuedForDeletion())){effect.GetParent()?.RemoveChild(effect);effect.QueueFree();}
            foreach(var (effect,anchor) in _exhaustAnchors)if(!_continuedEffects.Contains(effect)&&GodotObject.IsInstanceValid(anchor)&&!anchor.IsQueuedForDeletion())anchor.QueueFree();
            foreach(var (id,view) in _views)
            {
                if(_continuedCards.Contains(id)){DisplayAssets.ContinueConsumed(view);continue;}
                DisplayAssets.Return(view,Owned.Contains(id));
            }
            foreach(var bottle in _bottles.Values)DisplayAssets.Return(bottle);
            foreach(var icon in _emptyIcons.Values)DisplayAssets.Return(icon);
            Root.GetParent()?.RemoveChild(Root);Root.QueueFree();
            _handRoot.GetParent()?.RemoveChild(_handRoot);_handRoot.QueueFree();
            _playRoot.GetParent()?.RemoveChild(_playRoot);_playRoot.QueueFree();
            _potionRoot.GetParent()?.RemoveChild(_potionRoot);_potionRoot.QueueFree();
            foreach(var (card,visible) in _hidden)if(GodotObject.IsInstanceValid(card)&&!card.IsQueuedForDeletion())card.Visible=visible;
            foreach(var (node,visible) in _masks)if(GodotObject.IsInstanceValid(node)&&!node.IsQueuedForDeletion())node.Visible=visible;
        }
    }
    internal static void Install(Harmony harmony)
    {
        CreatureHudTrace.Install(harmony);
        HealthDisplayTail.Install(harmony);
        void Hook(Type type,string method,string handler,Type[]? parameters=null,bool postfix=false)
        {
            var target=AccessTools.DeclaredMethod(type,method,parameters)??throw new MissingMethodException(type.FullName,method);
            var patch=new HarmonyMethod(typeof(NativeDisplayTrace),handler);harmony.Patch(target,postfix?null:patch,postfix?patch:null);
        }
        harmony.Patch(AccessTools.DeclaredMethod(typeof(NCreature),"SetAnimationTrigger"),
            prefix:new HarmonyMethod(typeof(NativeDisplayTrace),nameof(TriggerBefore)),postfix:new HarmonyMethod(typeof(NativeDisplayTrace),nameof(Trigger)));
        Hook(typeof(CreatureAnimator),"SetNextState",nameof(TriggerResolved));
        Hook(typeof(NCreature),"StartDeathAnim",nameof(DeathStarted),postfix:true);
        Hook(typeof(NMonsterDeathVfx),"Create",nameof(DeathFadeCreated),[typeof(NCreature),typeof(CancellationToken)],postfix:true);
        Hook(typeof(NPowerAppliedVfx),"Create",nameof(PowerApplied),postfix:true);
        Hook(typeof(NPowerFlashVfx),"Create",nameof(PowerFlash),postfix:true);
        Hook(typeof(NPowerAppliedBuffVfx),"Create",nameof(PowerBuff),postfix:true);
        Hook(typeof(NPowerAppliedDebuffVfx),"Create",nameof(PowerDebuff),postfix:true);
        Hook(typeof(VfxCmd),"PlayVfx",nameof(Vfx));Hook(typeof(VfxCmd),"PlayVfx",nameof(VfxCreated),postfix:true);
        Hook(typeof(NDamageNumVfx),"Create",nameof(Damage),[typeof(Vector2),typeof(int)]);
        Hook(typeof(NDamageNumVfx),"Create",nameof(VisualCreated),[typeof(Vector2),typeof(int)],postfix:true);
        Hook(typeof(NHitSparkVfx),"Create",nameof(Spark),postfix:true);
        Hook(typeof(NBigSlashVfx),"Create",nameof(BigSlash),[typeof(Vector2),typeof(bool),typeof(Color)],postfix:true);
        Hook(typeof(NBigSlashImpactVfx),"Create",nameof(BigSlashImpact),[typeof(Vector2),typeof(float),typeof(Color)],postfix:true);
        harmony.Patch(AccessTools.DeclaredMethod(typeof(NHealthBar),"RefreshBlockUi"),
            prefix:new HarmonyMethod(typeof(NativeDisplayTrace),nameof(BlockUiBefore)),postfix:new HarmonyMethod(typeof(NativeDisplayTrace),nameof(BlockUiAfter)));
        Hook(typeof(NBlockBrokenVfx),"Create",nameof(BlockBreakFactory),[]);
        // This coordinate factory only creates a native display node; it does not execute an attack.
        Hook(typeof(NShivThrowVfx),"Create",nameof(ShivThrow),[typeof(Vector2),typeof(Vector2),typeof(Color)],postfix:true);
        Hook(typeof(NSovereignBladeVfx),"Forge",nameof(ForgeBlade));
        Hook(typeof(NSovereignBladeVfx),"Attack",nameof(AttackBlade));
        Hook(typeof(ForgeCmd),"PlayCombatRoomForgeVfx",nameof(ForgeLayout),postfix:true);
        Hook(typeof(NCardSmithVfx),"Create",nameof(SmithCard),[typeof(NCard),typeof(bool)],postfix:true);
        Hook(typeof(NCardSmithVfx),"Create",nameof(SmithCards),[typeof(IEnumerable<CardModel>),typeof(bool)],postfix:true);
        Hook(typeof(NAudioManager),"PlayOneShot",nameof(OneShot),[typeof(string),typeof(Dictionary<string,float>),typeof(float)]);
        Hook(typeof(NDebugAudioManager),"Play",nameof(DebugSound));
        Hook(typeof(NGame),"ScreenShake",nameof(Shake));
        Hook(typeof(NCardTrailVfx),"Create",nameof(Trail),postfix:true);
        Hook(typeof(NCardTrailVfx),"FadeOut",nameof(TrailFade));
        Hook(typeof(NCardFlyShuffleVfx),"_Ready",nameof(ShuffleReady),postfix:true);
        Hook(typeof(NCardExhaustVfx),"Create",nameof(Exhaust));Hook(typeof(NCardExhaustQuickVfx),"Create",nameof(QuickExhaust));
        Hook(typeof(NCardExhaustVfx),"Create",nameof(VisualCreated),postfix:true);Hook(typeof(NCardExhaustQuickVfx),"Create",nameof(VisualCreated),postfix:true);
        Hook(typeof(NGrandFinaleVfx),"Create",nameof(VisualCreated),[typeof(Vector2)],postfix:true);
        Hook(typeof(NGrandFinaleImpactVfx),"Create",nameof(VisualCreated),[typeof(Vector2),typeof(Vector2)],postfix:true);
        Hook(typeof(CardPileCmd),"AppendPileLerpTween",nameof(PileMove));
        Hook(typeof(NCard),"AnimCardToPlayPile",nameof(CardToPlay));
        Hook(typeof(NCard),"UpdateVisuals",nameof(CardVisualsChanged),postfix:true);
        Hook(typeof(NCard),"Create",nameof(CardCreated),[typeof(CardModel),typeof(ModelVisibility)],postfix:true);
        Hook(typeof(NCombatCardPile),"AddCard",nameof(PileAdded),postfix:true);
        Hook(typeof(NSplashVfx),"Create",nameof(Splash));Hook(typeof(NSplashVfx),"Create",nameof(VisualCreated),postfix:true);
        Hook(typeof(NItemThrowVfx),"Create",nameof(ItemCreated),postfix:true);
        // The grid's synchronous close can span several rendered frames. Capture both sides of
        // the actual hand attachment so interpolation still has the incoming card's starting pose.
        Hook(typeof(NPlayerHand),"Add",nameof(HandBoundary));Hook(typeof(NPlayerHand),"Add",nameof(HandBoundary),postfix:true);
        harmony.Patch(AccessTools.DeclaredMethod(typeof(NPlayerHand),"Add"),
            prefix:new HarmonyMethod(typeof(NativeDisplayTrace),nameof(HandMoveBefore)),postfix:new HarmonyMethod(typeof(NativeDisplayTrace),nameof(HandMoveAfter)));
        Hook(typeof(NEnergyCounter),"OnEnergyChanged",nameof(EnergyChanged));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(NStarCounter),"UpdateStarCount"),
            prefix:new HarmonyMethod(typeof(NativeDisplayTrace),nameof(StarsBefore)),postfix:new HarmonyMethod(typeof(NativeDisplayTrace),nameof(StarsAfter)));
        Hook(typeof(NPlayerTurnBanner),"Create",nameof(PlayerBanner),postfix:true);
        Hook(typeof(NEnemyTurnBanner),"Create",nameof(EnemyBanner),postfix:true);
    }
    private static void EnergyChanged(NEnergyCounter __instance,int __0,int __1)
    {
        if(__instance.IsNodeReady()&&__0<__1)Record("energy-increase",_=>CombatHudTrace.EnergyIncrease(__0,__1));
    }
    private static void BigSlash(Vector2 __0,bool __1,Color __2,NBigSlashVfx? __result)
    {
        if(__result is null)return;VisualCreated(__result);
        Record("native-big-slash",_=>OwnTail(NBigSlashVfx.Create(__0,__1,__2)));
    }
    private static void BigSlashImpact(Vector2 __0,float __1,Color __2,NBigSlashImpactVfx? __result)
    {
        if(__result is null)return;VisualCreated(__result);
        Record("native-big-slash-impact",_=>OwnTail(NBigSlashImpactVfx.Create(__0,__1,__2)));
    }
    private static void CardVisualsChanged(NCard __instance){if(_capture is {} trace)trace._dirtyCards.Add(__instance);}
    private static void StarsBefore(NStarCounter __instance,out Node[]? __state)=>__state=_capture is not null?__instance.GetChildren().ToArray():null;
    private static void StarsAfter(NStarCounter __instance,int __0,int __1,Node[]? __state)
    {
        if(_capture is null||__state is null)return;
        foreach(var node in __instance.GetChildren().Except(__state))SourceVisuals.Add(node);
        if(__0<__1)Record("star-increase",r=>r.StarIncrease(__0,__1));
    }
    private static void ForgeBlade(NSovereignBladeVfx __instance,float __0,bool __1)
    {
        if(_capture is null)return;var before=RegentPresentation.Pose.Capture(__instance);
        RegentPresentation.NoteForge(__instance,__0);
        Record("regent-forge",_=>RegentPresentation.Forge(before,__0,__1));
    }
    private static void AttackBlade(NSovereignBladeVfx __instance,Vector2 __0)
    {
        if(_capture is null)return;var before=RegentPresentation.Pose.Capture(__instance);
        Record("regent-blade-attack",_=>RegentPresentation.Attack(before,__0));
    }
    private static void ForgeLayout()
    {
        if(_capture is null)return;var layout=RegentPresentation.Layout();Record("regent-orbit",_=>RegentPresentation.Orbit(layout));
    }
    private static void SmithCard(NCard __0,NCardSmithVfx? __result)
    {
        if(_capture is not {} trace||__result is null)return;VisualCreated(__result);var id=trace.Card(__0);
        Record("native-smith-hand",r=>r.SmithCard(id));
    }
    private static void SmithCards(IEnumerable<CardModel> __0,NCardSmithVfx? __result)
    {
        if(_capture is null||__result is null)return;VisualCreated(__result);var before=LiveState.ChoiceProjection();
        var cards=__0.Select(c=>DisplayAssets.Get(c,()=>{})).ToArray();
        Codec.Require(before==LiveState.ChoiceProjection(),"smith-display-copy-changed-business-or-rng");
        Record("native-smith-preview",r=>r.SmithCards(cards));
    }
    private static void PlayerBanner(int __0,NPlayerTurnBanner? __result)
    {
        if(__result is null)return;VisualCreated(__result);Record("player-turn-banner",_=>OwnTail(NPlayerTurnBanner.Create(__0)));
    }
    private static void EnemyBanner(NEnemyTurnBanner? __result)
    {
        if(__result is null)return;VisualCreated(__result);Record("enemy-turn-banner",_=>OwnTail(NEnemyTurnBanner.Create()));
    }
    private static void HandBoundary()
    {
        if(_capture is not {} capture)return;capture.Sample(force:true);capture._handBoundarySamples++;
    }
    private static void PileMove(Tween? __0,NCard __1,PileType __2,PileType? __3)
    {
        if(__0 is not null&&__2==PileType.Play)RecordPlay(__1,__3,false);
    }
    private static void CardToPlay(NCard __instance)=>RecordPlay(__instance,null,true);
    private static void RecordPlay(NCard card,PileType? from,bool cardEntry)
    {
        if(_capture is not {} trace)return;
        trace._firstPlayAt??=trace.Elapsed;
        var id=trace.Card(card);var transform=card.GetGlobalTransform();
        var move=new PlayMove(id,card.GlobalPosition,transform.Scale,transform.Rotation,from,card.Model?.Pile?.Type??PileType.None,cardEntry);
        trace.Sample(force:true);Record(cardEntry?"native-card-to-play":"native-play-pile",r=>r.StartPlayMove(move));
    }
    private static void PileAdded(NCombatCardPile __instance)
    {
        if(_pileDisplayOverride is {} value&&ReferenceEquals(value.Button,__instance))
        {
            // The native handler owns all feedback tweens. Correct its display-only count to
            // the captured event result; its live pile can still be at the source checkpoint.
            AccessTools.Field(typeof(NCombatCardPile),"_currentCount").SetValue(__instance,value.Count);
            var label=(MegaLabel)Get(__instance,"_countLabel")!;label.SetTextAutoSize(value.Count.ToString());label.PivotOffset=label.Size*.5f;
            return;
        }
        if(_capture is null||Get(__instance,"_pile") is not CardPile pile)return;
        var type=pile.Type;var count=(int)Get(__instance,"_currentCount")!;
        Record("pile-add:"+type,r=>r.PileAdded(type,count));
    }
    private static void HandMoveBefore(NCard __0,out HandStart? __state)
    {
        __state=null;if(_capture is not {} trace||NPlayerHand.Instance!.IsAncestorOf(__0))return;
        var transform=__0.GetGlobalTransform();__state=new(trace.Card(__0),__0.GlobalPosition,transform.Scale,transform.Rotation);
    }
    private static void HandMoveAfter(HandStart? __state,NHandCardHolder __result)
    {
        if(__state is null||_capture is not {} trace)return;
        // Add/RefreshLayout advance native motion synchronously. Its pre-call pile position is
        // not necessarily a rendered arrival frame (large grids can already place it near the hand).
        var move=new HandMove(__state,__state.Id,CaptureHand(__result,__result.CardNode!));
        trace._handMoves[__state.Id]=move;Record("native-hand-arrival",renderer=>renderer.StartHandMove(move));
    }
    private static HandPose CaptureHand(NHandCardHolder holder,NCard card)
    {
        var transform=holder.GetGlobalTransform();var parent=((Control)holder.GetParent()).GetGlobalTransform();
        return new(new(holder.GlobalPosition,transform.Scale,transform.Rotation),
            new(parent*holder.TargetPosition,parent.Scale*(Vector2)Get(holder,"_targetScale")!,parent.Rotation+Mathf.DegToRad(holder.TargetAngle)),
            card.Position,card.Scale,card.Rotation,holder.GetIndex(),holder.ZIndex,holder.ZAsRelative);
    }
    private static void ItemCreated(NItemThrowVfx? __result)
    {
        if(_capture is not {} capture||__result is null)return;
        capture._items.TryAdd(__result,capture._items.Count);SourceVisuals.Add(__result);
    }
    private static void TriggerBefore(NCreature __instance,string __0,out TriggerCapture? __state)
    {
        __state=null;if(_capture is null)return;
        _triggerCapture=__state=new(__instance.Entity,__0,_triggerCapture);
    }
    private static void TriggerResolved(AnimState __0)
    {
        if(_triggerCapture is {} capture)capture.Animation=__0.Id;
    }
    private static void Trigger(TriggerCapture? __state)
    {
        if(__state is null)return;_triggerCapture=__state.Previous;
        // An ignored native trigger has no SetNextState call and must not restart its old loop.
        if(__state.Animation is not {} animation)return;
        var creature=__state.Creature;var trigger=__state.Trigger;
        Record("creature:"+trigger,_=>
        {
            var node=NCombatRoom.Instance?.GetCreatureNode(creature);
            if(node is not null)CreaturePresentation.PlayRecorded(node,trigger,animation);
        });
    }
    private static void DeathStarted(NCreature __instance,bool __0)
    {
        if(_capture is null||!__0)return;var creature=__instance.Entity;
        Record("creature-death-display",renderer=>
        {
            var node=NCombatRoom.Instance?.GetCreatureNode(creature);if(node is null||DeathDisplays.ContainsKey(creature))return;
            DeathDisplays.Add(creature,node);TrackTail(node);node.DisableInteractionForDeath();
            foreach(var intent in node.IntentContainer.GetChildren().OfType<NIntent>())intent.SetFrozen(true);
            // AnimDie contains native HUD/fade/decorative cleanup, not creature removal hooks,
            // AfterDeath, power application, AI or combat data changes. This is a new display task.
            var task=(Task)AccessTools.Method(typeof(NCreature),"AnimDie").Invoke(node,[true,node.DeathAnimCancelToken.Token])!;
            node.DeathAnimationTask=task;_ = ObserveDeath(task);
            NCombatRoom.Instance!.RemoveCreatureNode(node);
            Runtime.Log("native-death-display-started",new{Model=creature.ModelId.ToString(),NativeFadeAndTail=true,OriginalTaskResumed=false,CreatureBusinessRemoved=false});
        });
    }
    private static async Task ObserveDeath(Task task)
    {
        try{await task;}catch(OperationCanceledException){}catch(ObjectDisposedException){}
        catch(Exception ex){PlayerLoop.Error="native-death-display-"+ex.GetBaseException().Message;Runtime.Log("native-death-display-failed",new{Reason=PlayerLoop.Error});}
    }
    private static void DeathFadeCreated(NCreature __0,NMonsterDeathVfx? __result)
    {
        if(__result is not null&&DeathDisplays.ContainsKey(__0.Entity))TrackTail(__result);
    }
    private static void PowerApplied(PowerModel __0,int __1,bool __2,NPowerAppliedVfx? __result)
    {
        VisualCreated(__result);if(_capture is null||__result is null)return;
        Record("power-applied",r=>r.PowerEffect(NPowerAppliedVfx.Create(__0,__1,__2)));
    }
    private static void PowerFlash(PowerModel __0,NPowerFlashVfx? __result)
    {
        VisualCreated(__result);if(_capture is null||__result is null)return;
        Record("power-flash",r=>r.PowerEffect(NPowerFlashVfx.Create(__0)));
    }
    private static void PowerBuff(Vector2 __0,NPowerAppliedBuffVfx? __result)
    {
        VisualCreated(__result);if(_capture is null||__result is null)return;Record("power-buff",r=>r.PowerEffect(NPowerAppliedBuffVfx.Create(__0)));
    }
    private static void PowerDebuff(Vector2 __0,NPowerAppliedDebuffVfx? __result)
    {
        VisualCreated(__result);if(_capture is null||__result is null)return;Record("power-debuff",r=>r.PowerEffect(NPowerAppliedDebuffVfx.Create(__0)));
    }
    private static void Vfx(Vector2 __0,string __1,Control? __2,out Node[]? __state)
    {
        __state=_capture is not null||NativeSeek.Enabled?__2?.GetChildren().ToArray():null;
        Record("scene:"+__1,r=>VfxCmd.PlayVfx(__0,__1,r.Root));
    }
    private static void VfxCreated(Control? __2,Node[]? __state)
    {
        if((_capture is not null||NativeSeek.Enabled)&&__state is not null&&__2 is not null)foreach(var child in __2.GetChildren().Except(__state))SourceVisuals.Add(child);
    }
    private static void VisualCreated(Node? __result){if((_capture is not null||NativeSeek.Enabled)&&__result is not null)SourceVisuals.Add(__result);}
    private static void Damage(Vector2 __0,int __1)=>Record("damage-number",r=>{var fx=NDamageNumVfx.Create(__0,__1);if(fx is not null)r.Root.AddChild(fx);});
    private static void Splash(Vector2 __0,Color __1)=>Record("potion-splash",r=>{var fx=NSplashVfx.Create(__0,__1);if(fx is not null)r.Root.AddChild(fx);});
    private static void Spark(Creature __0,NHitSparkVfx? __result)
    {
        if(__result is not null){VisualCreated(__result);Record("hit-spark",r=>{var fx=NHitSparkVfx.Create(__0,false)??throw new InvalidDataException("native-display-hit-target-missing");r.Root.AddChild(fx);});}
    }
    private static void ShivThrow(Vector2 __0,Vector2 __1,Color __2,NShivThrowVfx? __result)
    {
        if(_capture is null||__result is null)return;VisualCreated(__result);
        Record("native-projectile:"+nameof(NShivThrowVfx),r=>r.PowerEffect(NShivThrowVfx.Create(__0,__1,__2)));
    }
    private static void BlockUiBefore(NHealthBar __instance,out BlockCapture? __state)
    {
        __state=_capture is {} trace?new(trace,__instance.GetChildren().OfType<NBlockBrokenVfx>().ToHashSet()):null;
    }
    private static void BlockUiAfter(NHealthBar __instance,BlockCapture? __state)
    {
        if(__state is null||!ReferenceEquals(_capture,__state.Trace))return;
        foreach(var effect in __instance.GetChildren().OfType<NBlockBrokenVfx>().Where(e=>!__state.Previous.Contains(e)))
        {
            // RefreshBlockUi assigns the native position AFTER Create/_Ready. Capture here rather
            // than guessing the block transition or recording the factory's unpositioned node.
            var creature=(Creature)Get(__instance,"_creature")!;var transform=effect.Transform;var color=effect.Modulate;
            VisualCreated(effect);Record("native-block-break",r=>r.BlockBreak(creature,transform,color));
        }
    }
    private static bool BlockBreakFactory(ref NBlockBrokenVfx? __result)
    {
        // Restoring data/UI is not a new block loss. The source-clock cue owns history effects.
        if(_creatingBlockBreakDisplay||!Replaying&&!DecisionRoute.CanRestore&&!NativeSeek.CanRestore)return true;
        __result=null;return false;
    }
    private static bool OneShot(string __0,Dictionary<string,float> __1,float __2)
    {
        if(NativeSeek.SilentRebuild)return false;
        if(Replaying&&!_recordedSound)return __0=="event:/sfx/enemy/enemy_fade";
        if(_capture is null||__0=="event:/sfx/enemy/enemy_fade")return true;
        var path=__0;var parameters=new Dictionary<string,float>(__1);var volume=__2;
        Record("sound:"+path,_=>
        {
            _recordedSound=true;
            try{NAudioManager.Instance?.PlayOneShot(path,parameters,volume);}finally{_recordedSound=false;}
        });
        return true;
    }
    private static void DebugSound(MethodBase __originalMethod,object[] __args)
    {
        if(_capture is null||__args.OfType<string>().Any(s=>s is "player_turn.mp3" or "enemy_turn.mp3"))return;
        var args=__args.ToArray();Record("debug-sound",_=>__originalMethod.Invoke(NDebugAudioManager.Instance,args));
    }
    private static bool Shake(MethodBase __originalMethod,object[] __args)
    {
        if(NativeSeek.SilentRebuild)return false;
        // Native display coroutines can emit their own shake (e.g. projectile impact). During a
        // sampled history segment the source clock already owns it. Keep exactly that recorded cue.
        if(Replaying&&!_recordedShake)return false;
        if(_capture is not null)
        {
            var args=__args.ToArray();Record("screen-shake",_=>
            {
                _recordedShake=true;try{__originalMethod.Invoke(NGame.Instance,args);}finally{_recordedShake=false;}
            });
        }
        return true;
    }
    private static void Trail(Control __0,string __1,NCardTrailVfx? __result)
    {
        // Native shuffle adds the trail during _Ready. Seed it before AddChild so particles
        // cannot emit at (0,0) for a frame before following the flight's first position.
        if(Replaying&&__0 is NCardFlyShuffleVfx&&__result is not null)
        {
            var parent=NCombatRoom.Instance!.CombatVfxContainer;
            __result.Position=parent.GetGlobalTransform().AffineInverse()*__0.GlobalPosition;
            __result.Rotation=__0.Rotation;
        }
        if(_capture is not {} capture||__0 is not NCard card||__result is null)return;var id=capture.Card(card);capture._trails[__result]=id;
        SourceVisuals.Add(__result);
        Record("card-trail",r=>r.CardTrail(id,__1));
    }
    private static void ShuffleReady(NCardFlyShuffleVfx __instance)
    {
        if(_capture is null)return;SourceVisuals.Add(__instance);
        if(Get(__instance,"_vfx") is Node trail)SourceVisuals.Add(trail);
        var pose=new ShufflePose((Vector2)Get(__instance,"_startPos")!,(Vector2)Get(__instance,"_endPos")!,
            (string)Get(__instance,"_trailPath")!,((CardPile)Get(__instance,"_targetPile")!).Type,
            (float)Get(__instance,"_controlPointOffset")!,(float)Get(__instance,"_speed")!,(float)Get(__instance,"_accel")!,
            (float)Get(__instance,"_arcDir")!,(float)Get(__instance,"_duration")!);
        Record("native-shuffle-flight",r=>r.Shuffle(pose));
    }
    private static void TrailFade(NCardTrailVfx __instance)
    {
        foreach(var tail in TailRecorders)
            if(tail._trails.TryGetValue(__instance,out var tailId))tail._tailTrailFades.TryAdd(tailId,tail.Elapsed);
        if(_capture is {} trace&&trace._trails.TryGetValue(__instance,out var id))Record("card-trail-fade",r=>{if(r.Trails.TryGetValue(id,out var fx))_ = fx.FadeOut();});
    }
    private static void Exhaust(NCard __0)=>RecordExhaust(__0,false);
    private static void QuickExhaust(NCard __0)=>RecordExhaust(__0,true);
    private static void RecordExhaust(NCard card,bool quick)
    {
        if(_capture is not {} trace)return;trace.Sample(force:true);var id=trace.Card(card);trace._exhaust.Add(id);
        SourceExhaustOwners[card]=card.Model!;
        Record(quick?"exhaust-quick":"exhaust",r=>r.Exhaust(id,quick));
    }
}
