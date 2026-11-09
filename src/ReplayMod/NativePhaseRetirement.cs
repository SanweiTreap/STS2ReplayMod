using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Backgrounds;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReplayMod;

internal static class NativePhaseRetirement
{
    private sealed record Owner(object State,int Generation,PendingOperation Operation);
    private sealed record SoundOwner(CombatState State,int Generation,int Host,string Path);
    private const int FmodStopped=2; // FMOD_STUDIO_PLAYBACK_STOPPED
    private static readonly AsyncLocal<Owner?> FlowOwner=new();
    private static readonly ConditionalWeakTable<Node,Owner> PileFlights=new();
    private static readonly HashSet<(NDebugAudioManager Manager,int Id)> Sounds=[];
    private static readonly Dictionary<GodotObject,SoundOwner> FmodSounds=[];
    private static readonly List<string> StoppedFmodPaths=[];
    private static readonly Dictionary<Tween,Owner> PileTweens=[];
    private static readonly Dictionary<Tween,Owner> SandpitTails=[];
    private static Owner? _retiringOwner;
    private static int? _retirementTarget;
    private static int _stoppedSounds,_mutedSounds,_acceleratedPileTweens;
    private static RetiringPhaseWait _waits=New();
    private static RetiringPhaseWait New()=>new(error=>Runtime.Log("retired-phase-timer-fault",new{Exception=error.ToString(),GameplayTask=false}));
    private static Type Machine(Type owner,string method,Type[]? parameters=null)=>AccessTools.Method(owner,method,parameters)
        .GetCustomAttributes(typeof(AsyncStateMachineAttribute),false).Cast<AsyncStateMachineAttribute>().Single().StateMachineType;
    private static readonly Type TurnMachine=Machine(typeof(CombatManager),"StartTurn");
    private static readonly Dictionary<Type,string> PacingCallers=ReviewedCallers();
    private static readonly Dictionary<Type,string> EnemyPlainTimers=Sites(
        (typeof(FakeMerchantMonster),"ShowDialogueForMove"),(typeof(MagiKnight),"DampenMove"),(typeof(FrogKnight),"BeetleChargeMove"),
        (typeof(SpinyToad),"ExplosionMove"),(typeof(Noisebot),"NoiseMove"),(typeof(SoulFysh),"BeckonMove"),(typeof(SoulFysh),"GazeMove"),
        (typeof(TheInsatiable),"LiquifyMove"),(typeof(Axebot),"BootUpMove"),(typeof(CeremonialBeast),"BeastCryMove"),
        (typeof(HauntedShip),"HauntMove"),(typeof(TerrorEel),"TerrorMove"),(typeof(DecimillipedeSegment),"AnimSegmentsAttack"),
        (typeof(Tunneler),"BelowMove"),(typeof(TwoTailedRat),"CallForBackup"),(typeof(FatGremlin),"FleeMove"),
        (typeof(ThievingHopper),"ThieveryMove"),(typeof(ThievingHopper),"FlutterMove"),(typeof(ThievingHopper),"EscapeMove"),
        (typeof(OwlMagistrate),"JudicialFlightMove"),(typeof(OwlMagistrate),"VerdictMove"),(typeof(Vantom),"DismemberMove"),
        (typeof(CubexConstruct),"ChargeUpMove"),(typeof(CubexConstruct),"RepeaterBlastMove"),(typeof(CubexConstruct),"ExpelBlastMove"));
    private static readonly Dictionary<Type,string> EnemyScaledTimers=Sites(
        (typeof(CalcifiedCultist),"IncantationMove"),(typeof(DampCultist),"IncantationMove"),(typeof(GremlinMerc),"GimmeMove"),
        (typeof(CeremonialBeast),"StampMove"),(typeof(HauntedShip),"HauntMove"),(typeof(SoulFysh),"BeckonMove"),
        (typeof(TerrorEel),"TerrorMove"),(typeof(FatGremlin),"FleeMove"),(typeof(Vantom),"InkBlotMove"),
        (typeof(Vantom),"InkyLanceMove"),(typeof(Vantom),"PrepareMove"));
    private static readonly Dictionary<Type,string> PowerPlainTimers=Sites((typeof(SandpitPower),"AfterRemoved"),
        (typeof(WitheringPresencePower),"AfterCardPlayed"),(typeof(PersonalHivePower),"AfterDamageReceived"),
        // Only the delay before applying Strength. The power command still runs.
        (typeof(EnragePower),nameof(EnragePower.AfterCardPlayed)));
    private static readonly Dictionary<Type,string> BackgroundTimers=Sites(
        (typeof(NKaiserCrabBossBackground),"PlayAttackAnim"),(typeof(NKaiserCrabBossBackground),"PlayRightSideChargeUpAnim"),
        (typeof(NKaiserCrabBossBackground),"PlayRightSideHeavy"),(typeof(NKaiserCrabBossBackground),"PlayRightRecharge"));
    private static readonly Type SandpitPositionCaller=Machine(typeof(SandpitPower),"UpdateCreaturePositions");
    private static readonly Type SandpitRemovedCaller=Machine(typeof(SandpitPower),"AfterRemoved");
    private static Dictionary<Type,string> Sites(params (Type Owner,string Method)[] sites)=>sites.ToDictionary(
        site=>Machine(site.Owner,site.Method),site=>$"{site.Owner.Name}.{site.Method}");
    // Fixed native OnPlay has one Wait: a pure anticipation timer, followed by the
    // normal AttackCommand. No damage, hook, UI decision or VFX completion is skipped.
    private static readonly Type AnticipationCaller=Machine(typeof(GrandFinale),"OnPlay");
    private static readonly Type DoomVisualPacingCaller=Machine(typeof(DoomPower),"PlayVfx");
    private static readonly Type TestSubjectRespawnCaller=Machine(typeof(TestSubject),"RespawnMove");
    private static readonly Type EffigySleepCaller=Machine(typeof(BygoneEffigy),"InitialSleepMove");
    private static readonly Type EffigyWakeCaller=Machine(typeof(BygoneEffigy),"WakeMove");
    private static readonly Type EffigySlashCaller=Machine(typeof(BygoneEffigy),"SlashMove");
    private static readonly Type TurboPreviewCaller=Machine(typeof(Turbo),"OnPlay");
    private static readonly Type SculptorCastCaller=Machine(typeof(DevotedSculptor),"ForbiddenIncantationMove");
    private static readonly Type CeremonialPlowCaller=Machine(typeof(CeremonialBeast),"PlowMove");
    private static readonly Type ManualPlayPileCaller=Machine(typeof(CardPileCmd),nameof(CardPileCmd.AddDuringManualCardPlay));
    private static readonly Type ShuffleCaller=Machine(typeof(CardPileCmd),nameof(CardPileCmd.Shuffle));
    private static readonly Type AddCardsCaller=Machine(typeof(CardPileCmd),nameof(CardPileCmd.Add),
        [typeof(IEnumerable<CardModel>),typeof(CardPile),typeof(CardPilePosition),typeof(AbstractModel),typeof(bool),typeof(bool)]);
    private static readonly Type GeneratedPreviewCaller=Machine(typeof(CardPileCmd),nameof(CardPileCmd.AddToCombatAndPreview),
        [typeof(Creature),typeof(PileType),typeof(int),typeof(Player),typeof(CardPilePosition)]);
    private static readonly Type PlayCardCaller=Machine(typeof(CardModel),nameof(CardModel.OnPlayWrapper));
    private static readonly Type PowerCardFlightCaller=Machine(typeof(CardModel),"PlayPowerCardFlyVfx");
    private static readonly Type PotionUseCaller=Machine(typeof(PotionModel),nameof(PotionModel.OnUseWrapper));
    private static readonly Type HealCaller=Machine(typeof(CreatureCmd),nameof(CreatureCmd.Heal),
        [typeof(MegaCrit.Sts2.Core.Entities.Creatures.Creature),typeof(decimal),typeof(bool)]);
    private static readonly Type ApplyPowerCaller=Machine(typeof(PowerCmd),nameof(PowerCmd.Apply),
        [typeof(PlayerChoiceContext),typeof(PowerModel),typeof(MegaCrit.Sts2.Core.Entities.Creatures.Creature),typeof(decimal),
            typeof(MegaCrit.Sts2.Core.Entities.Creatures.Creature),typeof(CardModel),typeof(bool)]);
    private static readonly Type RemovePowerCaller=Machine(typeof(PowerCmd),nameof(PowerCmd.Remove),[typeof(PowerModel)]);
    private static readonly Type ModifyPowerCaller=Machine(typeof(PowerCmd),nameof(PowerCmd.ModifyAmount));
    private static readonly Type GainBlockCaller=Machine(typeof(CreatureCmd),nameof(CreatureCmd.GainBlock),
        [typeof(Creature),typeof(decimal),typeof(ValueProp),typeof(CardPlay),typeof(bool)]);
    private static Dictionary<Type,string> ReviewedCallers()
    {
        var callers=new Dictionary<Type,string>
        {
            [TurnMachine]="CombatManager.StartTurn",
            [Machine(typeof(NCreature),"PerformIntent")]="NCreature.PerformIntent",
            [Machine(typeof(MonsterModel),"PerformMove")]="MonsterModel.PerformMove",
            [Machine(typeof(CreatureCmd),"TriggerAnim")]="CreatureCmd.TriggerAnim",
            [Machine(typeof(AttackCommand),"Execute")]="AttackCommand.Execute.before-hit"
        };
        // Damage's only CustomScaledWait follows all damage/death hooks and Kill.
        // Include its async overload types, but keep their awaited business intact.
        foreach(var method in AccessTools.GetDeclaredMethods(typeof(CreatureCmd)).Where(m=>m.Name=="Damage"))
            if(method.GetCustomAttributes(typeof(AsyncStateMachineAttribute),false).OfType<AsyncStateMachineAttribute>().SingleOrDefault() is {} asyncMethod)
                callers.Add(asyncMethod.StateMachineType,"CreatureCmd.Damage");
        return callers;
    }
    internal static bool Active=>_waits.Active;
    internal static void Clear(bool preserveAudio=false)
    {
        if(preserveAudio)
        {
            PruneFinishedSounds();
            if(FmodSounds.Count>0)Runtime.Log("native-forward-combat-audio-preserved",new{Paths=FmodSounds.Values.Select(sound=>sound.Path).Distinct().ToArray()});
        }
        else{RetireFmodSounds(stop:true);RetireSounds();Sounds.Clear();}
        StoppedFmodPaths.Clear();PileTweens.Clear();SandpitTails.Clear();_retirementTarget=null;_retiringOwner=null;_waits=New();FlowOwner.Value=null;
    }
    internal static void Install(Harmony h)
    {
        h.Patch(AccessTools.DeclaredMethod(typeof(Cmd),"CustomScaledWait",
            [typeof(float),typeof(float),typeof(bool),typeof(CancellationToken)]),postfix:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(WaitAfter)));
        h.Patch(AccessTools.Method(typeof(CombatManager),"StartTurn"),
            prefix:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(TurnBefore)),
            finalizer:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(TurnReturned)));
        h.Patch(AccessTools.DeclaredMethod(typeof(PlayCardAction),"ExecuteAction"),
            prefix:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(ActionBefore)),
            finalizer:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(TurnReturned)));
        h.Patch(AccessTools.DeclaredMethod(typeof(UsePotionAction),"ExecuteAction",[]),
            prefix:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(ActionBefore)),
            finalizer:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(TurnReturned)));
        h.Patch(AccessTools.DeclaredMethod(typeof(Cmd),"Wait",[typeof(float),typeof(bool)]),
            postfix:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(PlainWaitAfter)));
        h.Patch(AccessTools.DeclaredMethod(typeof(Cmd),"Wait",[typeof(float),typeof(CancellationToken),typeof(bool)]),
            postfix:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(BackgroundWaitAfter)));
        h.Patch(AccessTools.DeclaredMethod(SandpitRemovedCaller,"MoveNext"),
            transpiler:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(TrackSandpitTail)));
        h.Patch(AccessTools.DeclaredMethod(typeof(TweenHelper),nameof(TweenHelper.AwaitFinished),[typeof(Tween),typeof(Node)]),
            prefix:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(PileTweenBefore)));
        var pileCallbacks=new HarmonyMethod(typeof(NativePhaseRetirement),nameof(BindPileCallbacks));
        foreach(var builder in AccessTools.GetDeclaredMethods(typeof(CardPileCmd)).Where(method=>method.Name==nameof(CardPileCmd.GetTweenForCardsChangingPiles)))
            h.Patch(builder,transpiler:pileCallbacks);
        var removeCards=Machine(typeof(CardPileCmd),nameof(CardPileCmd.RemoveFromCombat),[typeof(IEnumerable<CardModel>),typeof(bool)]);
        h.Patch(AccessTools.DeclaredMethod(removeCards,"MoveNext"),transpiler:pileCallbacks);
        h.Patch(AccessTools.DeclaredMethod(typeof(CardCmd),"PreviewInternal"),transpiler:pileCallbacks);
        h.Patch(AccessTools.DeclaredMethod(typeof(NCardFlyVfx),"Create",[typeof(NCard),typeof(PileType),typeof(bool),typeof(string)]),
            postfix:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(PileFlightCreated)));
        h.Patch(AccessTools.DeclaredMethod(typeof(NCardFlyShuffleVfx),"Create",[typeof(CardPile),typeof(CardPile),typeof(string)]),
            postfix:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(PileFlightCreated)));
        foreach(var type in new[]{typeof(NCardFlyVfx),typeof(NCardFlyShuffleVfx)})
            h.Patch(AccessTools.DeclaredMethod(type,"_Ready",[]),
                prefix:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(PileFlightReady)),
                finalizer:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(TurnReturned)));
        foreach(var type in new[]{typeof(NCombatCardPile),typeof(NExhaustPileButton)})
            h.Patch(AccessTools.DeclaredMethod(type,"AddCard",[]),
                prefix:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(AllowPileArrivalFeedback)));
        h.Patch(AccessTools.DeclaredMethod(typeof(NDebugAudioManager),"Play"),
            prefix:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(OneShotVolume)),
            postfix:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(SoundAfter)));
        h.Patch(AccessTools.DeclaredMethod(typeof(NAudioManager),"PlayOneShot",[typeof(string),typeof(Dictionary<string,float>),typeof(float)]),
            prefix:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(FmodOneShot)));
    }
    private static void TurnBefore(object __0,out Owner? __state)
    {
        __state=FlowOwner.Value;
        var state=AccessTools.Property(__0.GetType(),"State").GetValue(__0);
        FlowOwner.Value=NativeSeek.Enabled&&!PlayerLoop.TearingDown&&ActionMonitor.Pending is {Template.Kind:OperationKind.EndTurn} pending&&
            ReferenceEquals(state,CombatManager.Instance.DebugOnlyGetState())?new(state!,NativeSeek.Generation,pending):null;
    }
    private static void ActionBefore(GameAction __instance,out Owner? __state)
    {
        __state=FlowOwner.Value;
        if(NativeSeek.Enabled&&!PlayerLoop.TearingDown&&ActionMonitor.Pending is {Template.Kind:OperationKind.Card or OperationKind.Potion} pending&&
            ReferenceEquals(pending.Action,__instance)&&CombatManager.Instance.DebugOnlyGetState() is {} state)
            FlowOwner.Value=new(state,NativeSeek.Generation,pending);
        // Derived/automatic actions keep their valid parent operation's ownership.
        // CurrentOwner still rejects an old pending reference, room or generation.
    }
    private static void TurnReturned(Owner? __state)=>FlowOwner.Value=__state;
    internal static int? VisualFlowGeneration=>FlowOwner.Value?.Generation;
    private static bool RetiredPresentation(Owner owner)=>owner.Generation!=NativeSeek.Generation||NativeSeek.RetiringPresentation;
    internal static bool RetiredPresentationOrigin=>CombatVisualLayers.RetiredCue||FlowOwner.Value is {} owner&&RetiredPresentation(owner);
    private static bool RebuildingPrefix=>NativeSeek.RebuildingTurn||RunPlayback.RebuildingRoom;
    private static Owner? CurrentOwner()=>NativeSeek.Enabled&&!PlayerLoop.TearingDown&&FlowOwner.Value is {} owner&&
        owner.Generation==NativeSeek.Generation&&ReferenceEquals(owner.Operation,ActionMonitor.Pending)&&
        ReferenceEquals(owner.State,CombatManager.Instance.DebugOnlyGetState())?owner:null;
    private static Type? DirectNativeCaller(Type helper)=>new StackTrace().GetFrames().Select(frame=>frame.GetMethod()?.DeclaringType)
        .FirstOrDefault(type=>type?.Assembly==typeof(CombatManager).Assembly&&type!=helper&&type.DeclaringType!=helper);
    private static bool IsDrawHook(Type caller)=>caller.DeclaringType is {} model&&typeof(AbstractModel).IsAssignableFrom(model)&&
        AccessTools.DeclaredMethod(model,nameof(AbstractModel.AfterCardDrawn),[typeof(PlayerChoiceContext),typeof(CardModel),typeof(bool)])
            ?.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType==caller;

    private static void BackgroundWaitAfter(float __0,ref Task __result)
    {
        if(CurrentOwner() is not {Operation.Template.Kind:OperationKind.EndTurn} owner||
            DirectNativeCaller(typeof(Cmd)) is not {} caller||!BackgroundTimers.TryGetValue(caller,out var source))return;
        BindTimer(owner,source,__0,__0,ref __result);
    }

    private static void BindTimer(Owner owner,string source,float fast,float standard,ref Task timer)
    {
        var rebuilding=RebuildingPrefix&&!NativeSeek.RetiringPresentation;
        Runtime.Log("native-phase-wait-bound",new{owner.Generation,Kind=owner.Operation.Template.Kind.ToString(),owner.Operation.Template.Card,
            FastSeconds=fast,StandardSeconds=standard,Retiring=_waits.Active,TimerAlreadyCompleted=timer.IsCompleted,
            CurrentRoomAndGenerationMatched=true,DirectStartTurnCaller=source=="CombatManager.StartTurn",PacingSource=source,
            RebuildingPrefix=rebuilding});
        timer=rebuilding?_waits.Skip(timer):_waits.Wrap(timer);
    }
    private static void WaitAfter(float __0,float __1,ref Task __result)
    {
        if(CurrentOwner() is not {} owner)return;
        // Match only reviewed DIRECT pacing call sites, not an ancestor. Their
        // damage/AI/hooks still run; other waits, signals and tweens stay intact.
        foreach(var frame in new StackTrace().GetFrames())
        {
            var type=frame.GetMethod()?.DeclaringType;
            if(type?.Assembly!=typeof(CombatManager).Assembly||type==typeof(Cmd)||type.DeclaringType==typeof(Cmd))continue;
            var source=PacingCallers.GetValueOrDefault(type);
            if(owner.Operation.Template.Kind==OperationKind.EndTurn)source??=EnemyScaledTimers.GetValueOrDefault(type);
            if(type==ShuffleCaller)source="CardPileCmd.Shuffle";
            if(type==PlayCardCaller)source="CardModel.OnPlayWrapper";
            if(type==PowerCardFlightCaller)source="CardModel.PlayPowerCardFlyVfx";
            if(type==ApplyPowerCaller)source="PowerCmd.Apply";
            if(type==RemovePowerCaller)source="PowerCmd.Remove";
            if(type==ModifyPowerCaller)source="PowerCmd.ModifyAmount";
            // Block and its history are already updated. AfterBlockGained still runs.
            if(type==GainBlockCaller)source="CreatureCmd.GainBlock";
            if(type==SculptorCastCaller&&owner.Operation.Template.Kind==OperationKind.EndTurn&&__0==0.75f&&__1==1f)
                source="DevotedSculptor.ForbiddenIncantationMove.before-buff";
            // This wait is between committed HP/history and the HP-changed hook.
            if(type==HealCaller&&__0==0.1f&&__1==0.25f)source="CreatureCmd.Heal.before-hp-hook";
            if(source=="MonsterModel.PerformMove"&&__0==0.1f&&__1==0.2f&&IsTestSubjectEndTurn(owner)&&
                !RetiredPresentation(owner)&&!RebuildingPrefix)
                __result=Task.WhenAll(__result,WaitForTestSubjectKnockout(owner));
            if(source is not null)BindTimer(owner,source,__0,__1,ref __result);
            break;
        }
    }
    private static void PlainWaitAfter(float __0,ref Task __result)
    {
        if(CurrentOwner() is not {} owner)return;
        var kind=owner.Operation.Template.Kind;
        var bossTurn=IsTestSubjectEndTurn(owner);
        foreach(var frame in new StackTrace().GetFrames())
        {
            var type=frame.GetMethod()?.DeclaringType;
            if(type?.Assembly!=typeof(CombatManager).Assembly||type==typeof(Cmd)||type.DeclaringType==typeof(Cmd))continue;
            var source=type==AnticipationCaller&&kind==OperationKind.Card?"native-card-visual-anticipation":null;
            source??=PowerPlainTimers.GetValueOrDefault(type);
            // DoomKill still awaits PlayVfx, kills each creature, then runs AfterDiedToDoom.
            if(type==DoomVisualPacingCaller&&__0 is 0.25f or 1.5f)source="DoomPower.PlayVfx";
            // These two pauses pace the native respawn presentation. Its scale,
            // HP, power and AI updates still execute in their original order.
            if(type==TestSubjectRespawnCaller&&bossTurn&&(__0 is 0.8f or 1.15f))
                source="TestSubject.RespawnMove";
            if(kind==OperationKind.EndTurn)
            {
                source??=EnemyPlainTimers.GetValueOrDefault(type);
                if(__0==0.5f&&type==EffigySleepCaller)source="BygoneEffigy.InitialSleepMove";
                if(__0==0.5f&&type==EffigyWakeCaller)source="BygoneEffigy.WakeMove";
                if(__0==0.25f&&type==EffigySlashCaller)source="BygoneEffigy.SlashMove";
                if(__0==0.3f&&type==SculptorCastCaller)source="DevotedSculptor.ForbiddenIncantationMove.before-scream";
                if(type==CeremonialPlowCaller&&__0 is 0.5f or 0.2f)source="CeremonialBeast.PlowMove";
            }
            if(type==TurboPreviewCaller&&kind==OperationKind.Card&&__0==0.5f)source="Turbo.OnPlay.after-preview";
            // At this shared site all generated cards and their add hooks already finished.
            if(__0==1f&&(type==GeneratedPreviewCaller||type.IsConstructedGenericType&&type.GetGenericTypeDefinition()==GeneratedPreviewCaller))
                source="CardPileCmd.AddToCombatAndPreview.after-add";
            // In the fixed native build these direct AfterCardDrawn waits only pace
            // the following energy loss or damage. Still await the full draw hook.
            if(__0 is 0.25f or 0.5f&&IsDrawHook(type))source="Hook.AfterCardDrawn.effect-pacing";
            if(type==ShuffleCaller)source="CardPileCmd.Shuffle";
            if(type==PotionUseCaller&&kind==OperationKind.Potion&&__0==0.5f)source="PotionModel.OnUseWrapper.before-effect";
            if(source is not null)BindTimer(owner,source,__0,__0,ref __result);
            break;
        }
    }
    private static bool Retiring(Owner owner)=>_waits.Active&&_retiringOwner is {} retired&&
        ReferenceEquals(owner.Operation,retired.Operation)&&ReferenceEquals(owner.State,retired.State)&&owner.Generation==retired.Generation;
    private static bool IsTestSubjectEndTurn(Owner owner)=>owner.Operation.Template.Kind==OperationKind.EndTurn&&
        ((CombatState)owner.State).Encounter is TestSubjectBoss;

    private static async Task WaitForTestSubjectKnockout(Owner owner)
    {
        var state=(CombatState)owner.State;
        var subject=state.Enemies.Select(enemy=>enemy.Monster).OfType<TestSubject>()
            .FirstOrDefault(monster=>monster.Creature.IsDead&&monster.NextMove.Id=="RESPAWN_MOVE");
        if(subject is null)return;
        var path=subject.DeathSfx;
        var host=owner.Operation.PlaybackIndex+1;
        var sound=FmodSounds.FirstOrDefault(pair=>ReferenceEquals(pair.Value.State,state)&&pair.Value.Host<=host&&
            pair.Value.Generation<=owner.Generation&&pair.Value.Path==path).Key;
        if(sound is null||!FmodSoundPlaying(sound))return;

        // Respawn raises the boss music parameter again. Keep the native knockout
        // cue distinct and give the score time to take its beat-synced transition.
        var started=Stopwatch.GetTimestamp();
        var limit=TimeSpan.FromSeconds(2);
        while(!RetiredPresentation(owner)&&FmodSoundPlaying(sound)&&Stopwatch.GetElapsedTime(started)<limit)
            await Runtime.NextFrame();
        var elapsed=Stopwatch.GetElapsedTime(started);
        Runtime.Log("test-subject-knockout-presentation-boundary",new{Host=host,owner.Generation,elapsed.TotalMilliseconds,
            Retiring=RetiredPresentation(owner),TimedOut=elapsed>=limit});
    }
    private static IEnumerable<CodeInstruction> BindPileCallbacks(IEnumerable<CodeInstruction> source)
    {
        var native=AccessTools.DeclaredMethod(typeof(Callable),nameof(Callable.From),[typeof(Action)]);
        var instructions=source.ToArray();
        Codec.Require(instructions.Any(i=>i.Calls(native)),"combat-pile-callback-sites-missing");
        return instructions.MethodReplacer(native,AccessTools.DeclaredMethod(typeof(NativePhaseRetirement),nameof(PileCallback)));
    }
    private static Callable PileCallback(Action callback)=>Callable.From(WithPileOwner(callback));
    private static Action WithPileOwner(Action callback)
    {
        if(FlowOwner.Value is not {} owner)return callback;
        // Godot invokes the delegate on another frame, outside its scheduling flow.
        // Execute it intact with its original owner, including any native cleanup.
        return () =>
        {
            var previous=FlowOwner.Value;FlowOwner.Value=owner;
            try{callback();}finally{FlowOwner.Value=previous;}
        };
    }
    private static void PileFlightCreated(Node? __result)
    {
        if(__result is not null&&FlowOwner.Value is {} owner)PileFlights.Add(__result,owner);
    }
    private static void PileFlightReady(Node __instance,out Owner? __state)
    {
        __state=FlowOwner.Value;
        if(PileFlights.TryGetValue(__instance,out var owner))FlowOwner.Value=owner;
    }
    private static bool AllowPileArrivalFeedback(NCombatCardPile __instance)
    {
        // This subscriber only updates the HUD and plays its pulse. The pile's
        // completion notification still reaches its other native subscribers.
        if(!NativeSeek.RetiringPresentation&&!RetiredPresentationOrigin)return true;
        Runtime.Log("retired-pile-arrival-feedback",new{Button=__instance.GetType().Name,SourceGeneration=VisualFlowGeneration,
            NativeSeek.Generation,NativeNotificationKept=true});
        return false;
    }
    private static void PileTweenBefore(Tween __0,Node __1)
    {
        if(CurrentOwner() is not {} owner||!ReferenceEquals(__1,NCombatRoom.Instance))return;
        var caller=DirectNativeCaller(typeof(TweenHelper));
        if(caller!=AddCardsCaller&&caller!=ShuffleCaller&&caller!=ManualPlayPileCaller&&caller!=SandpitPositionCaller)return;
        foreach(var tween in PileTweens.Keys.Where(t=>!GodotObject.IsInstanceValid(t)||!t.IsRunning()).ToArray())PileTweens.Remove(tween);
        var retiring=Retiring(owner);
        if(retiring||NativeSeek.RebuildingTurn)
        {
            AcceleratePileTween(__0);
            if(!retiring)Runtime.Log("cold-turn-pile-tween-accelerated",new{
                Host=owner.Operation.PlaybackIndex+1,NativeCompletionKept=true,Caller=caller.Name});
        }
        else PileTweens[__0]=owner;
    }
    private static IEnumerable<CodeInstruction> TrackSandpitTail(IEnumerable<CodeInstruction> source)
    {
        var original=AccessTools.Method(typeof(Node),nameof(Node.CreateTween));
        var replacement=AccessTools.DeclaredMethod(typeof(NativePhaseRetirement),nameof(CreateSandpitTail));
        var instructions=source.ToArray();
        Codec.Require(instructions.Count(i=>i.Calls(original))==1,"sandpit-position-tail-call-mismatch");
        return instructions.MethodReplacer(original,replacement);
    }
    private static Tween CreateSandpitTail(Node room)
    {
        var tween=room.CreateTween();
        if(CurrentOwner() is {} owner&&ReferenceEquals(room,NCombatRoom.Instance))
        {
            foreach(var old in SandpitTails.Keys.Where(t=>!GodotObject.IsInstanceValid(t)||!t.IsRunning()).ToArray())SandpitTails.Remove(old);
            SandpitTails[tween]=owner;
        }
        return tween;
    }
    private static void AcceleratePileTween(Tween tween)
    {
        if(!GodotObject.IsInstanceValid(tween)||!tween.IsRunning())return;
        // Keep the native completion signal and every callback on the tween.
        tween.SetSpeedScale(16);
        _acceleratedPileTweens++;
    }
    private static void SoundAfter(NDebugAudioManager __instance,string __0,int __result)
    {
        if(FlowOwner.Value is not {} owner)return;
        var playing=(IEnumerable)AccessTools.Field(typeof(NDebugAudioManager),"_playingSounds").GetValue(__instance)!;
        var live=playing.Cast<object>().Select(s=>(int)AccessTools.Field(s.GetType(),"id").GetValue(s)!).ToHashSet();
        Sounds.RemoveWhere(key=>ReferenceEquals(key.Manager,__instance)&&!live.Contains(key.Id));
        Sounds.Add((__instance,__result));
        // Play still ran, including native pitch RNG and pool bookkeeping.
        if(RetiredPresentation(owner))
        {
            __instance.Stop(__result,0);Sounds.Remove((__instance,__result));_stoppedSounds++;
            Runtime.Log("retired-owned-sound-stopped",new{Id=__result,Sound=__0,owner.Generation,NativePlayRan=true});
        }
    }
    private static void OneShotVolume(ref float volume)
    {if(RetiredPresentationOrigin||RebuildingPrefix){volume=0;_mutedSounds++;}}
    private static bool FmodOneShot(string path,Dictionary<string,float> parameters,ref float volume)
    {
        if(TestMode.IsOn)return true;
        if(RebuildingPrefix&&path.StartsWith("event:/sfx/",StringComparison.Ordinal))return false;
        if(!NativeSeek.Enabled)return true;
        var owner=CurrentOwner();
        // Combat commands converge on this one-shot entry. Own their cues by
        // the current action, so individual cards need no sound registration.
        var combatCue=path.StartsWith("event:/sfx/enemy/",StringComparison.Ordinal)||path==FmodSfx.doomKill||
            owner is not null&&path.StartsWith("event:/sfx/",StringComparison.Ordinal);
        if(combatCue&&(NativeSeek.RetiringPresentation||FlowOwner.Value is {} flow&&flow.Generation!=NativeSeek.Generation))
        {_mutedSounds++;return false;}
        if(RetiredPresentationOrigin)
        {
            _mutedSounds++;
            volume=0;
            return true;
        }
        if(!combatCue||PlayerLoop.TearingDown||NCombatRoom.Instance is null||CombatManager.Instance.DebugOnlyGetState() is not {} state||
            !Engine.HasSingleton("FmodServer"))return true;
        var server=Engine.GetSingleton("FmodServer");
        if(server is null||!server.Call("check_event_path",path).AsBool())return true;
        HashSet<string>? validParameters=null;
        if(parameters.Count>0)
        {
            if(!server.HasMethod("get_event"))return true;
            using var descriptorResult=server.Call("get_event",path);
            if(descriptorResult.AsGodotObject() is not {} descriptor||!descriptor.HasMethod("get_parameters"))return true;
            using var parameterResult=descriptor.Call("get_parameters");
            if(parameterResult.VariantType!=Variant.Type.Array)return true;
            validParameters=parameterResult.AsGodotArray().Select(entry=>entry.AsGodotObject())
                .Where(parameter=>parameter is not null&&parameter.HasMethod("get_name"))
                .Select(parameter=>parameter!.Call("get_name").AsString()).ToHashSet(StringComparer.Ordinal);
        }
        var sound=server.Call("create_event_instance",path).AsGodotObject();
        if(sound is null||!FmodSoundValid(sound))return true;
        if(parameters.Count>0&&!sound.HasMethod("set_parameter_by_name"))
        {sound.Call("release");return true;}
        // The game's proxy starts this same event with its configured volume and
        // releases it immediately. Hold the handle so Back can stop just this cue.
        sound.Call("set_volume",volume);
        if(validParameters is not null)
            foreach(var (parameter,value) in parameters)
                if(validParameters.Contains(parameter))sound.Call("set_parameter_by_name",parameter,value);
        sound.Call("start");
        var host=owner is null?NativeSeek.Host:owner.Operation.PlaybackIndex+1;
        FmodSounds.Add(sound,new(state,NativeSeek.Generation,host,path));
        Runtime.Log("native-owned-fmod-started",new{Path=path,NativeSeek.Generation,Host=host,ParameterCount=parameters.Count});
        return false;
    }
    private static bool FmodSoundValid(GodotObject sound)=>GodotObject.IsInstanceValid(sound)&&
        (!sound.HasMethod("is_valid")||sound.Call("is_valid").AsBool());
    internal static bool FmodSoundPlaying(GodotObject sound)=>FmodSoundValid(sound)&&sound.Call("get_playback_state").AsInt32()!=FmodStopped;
    private static void RetireFmodSounds(bool stop,int? afterHost=null)
    {
        const int immediate=1; // FMOD_STUDIO_STOP_IMMEDIATE
        foreach(var (sound,known) in FmodSounds.ToArray())
        {
            if(!FmodSoundValid(sound)){FmodSounds.Remove(sound);continue;}
            if(sound.Call("get_playback_state").AsInt32()!=FmodStopped)
            {
                if(!stop||afterHost is {} target&&known.Generation<=NativeSeek.Generation&&known.Host<=target&&
                    ReferenceEquals(known.State,CombatManager.Instance.DebugOnlyGetState()))continue;
                sound.Call("stop",immediate);StoppedFmodPaths.Add(known.Path);
            }
            sound.Call("release");FmodSounds.Remove(sound);
        }
    }
    internal static void PruneFinishedSounds()
    {
        if(FmodSounds.Count>0)RetireFmodSounds(stop:false);
    }
    private static void RetireSounds()
    {
        if(NDebugAudioManager.Instance is not {} audio)return;
        var playing=(IEnumerable)AccessTools.Field(typeof(NDebugAudioManager),"_playingSounds").GetValue(audio)!;
        foreach(var sound in playing.Cast<object>().ToArray())
        {
            var id=(int)AccessTools.Field(sound.GetType(),"id").GetValue(sound)!;
            if(!Sounds.Contains((audio,id)))continue;
            audio.Stop(id,0);Sounds.Remove((audio,id));_stoppedSounds++;
        }
    }
    internal static void Begin(int target)
    {
        _retiringOwner=ActionMonitor.Pending is {} pending&&CombatManager.Instance.DebugOnlyGetState() is {} state?
            new(state,NativeSeek.Generation,pending):null;
        _stoppedSounds=0;StoppedFmodPaths.Clear();_mutedSounds=0;_acceleratedPileTweens=0;
        _retirementTarget=target;
        RetireFmodSounds(stop:true,afterHost:target);
        if(NCombatRoom.Instance is {} room)EnemyScenePresentation.RetireEntomancerSwarms(room);
        RetireCues();RetireSounds();_waits.Begin();
        NativeDisplayTrace.RetireSculptorParticles();
        NativeDisplayTrace.RetireNativeOwnedAttacks();
        NativeDisplayTrace.RetireEnemyRoomEffects();
        foreach(var (tween,owner) in PileTweens.ToArray())
            if(Retiring(owner))AcceleratePileTween(tween);
        PileTweens.Clear();
    }
    internal static void End()
    {
        RetireFmodSounds(stop:true,afterHost:_retirementTarget);
        RetireSounds();
        foreach(var (tween,owner) in SandpitTails.ToArray())
        {
            if(owner.Generation!=NativeSeek.Generation||!ReferenceEquals(owner.State,CombatManager.Instance.DebugOnlyGetState()))continue;
            if(GodotObject.IsInstanceValid(tween)&&tween.IsRunning())tween.Kill();
            SandpitTails.Remove(tween);
        }
        Runtime.Log("native-phase-retirement-finished",new{_waits.Released,_waits.Skipped,
            OwnedDebugSoundsStopped=_stoppedSounds,OwnedFmodSoundsStopped=StoppedFmodPaths.Count,StoppedFmodPaths=StoppedFmodPaths.ToArray(),
            OwnedAudioCallsMuted=_mutedSounds,OwnedPileTweensAccelerated=_acceleratedPileTweens,
            OnlyReviewedOwnedPacingTimers=true,BusinessTasksCancelled=false,NativeEffectsAndRngStillVerified=true});
        _waits.End();_retiringOwner=null;_retirementTarget=null;
    }
    private static void RetireCues()
    {
        int banners=0,sounds=0;
        IEnumerable<Node> Nodes(Node root)
        {yield return root;foreach(var child in root.GetChildren())foreach(var node in Nodes(child))yield return node;}
        if(NCombatRoom.Instance is {} room)
            foreach(var node in Nodes(room).Where(n=>n is NEnemyTurnBanner or NPlayerTurnBanner or NCombatStartBanner).ToArray())
            {node.GetParent()?.RemoveChild(node);node.QueueFree();banners++;}
        if(NDebugAudioManager.Instance is {} audio)
        {
            var playing=(IEnumerable)AccessTools.Field(typeof(NDebugAudioManager),"_playingSounds").GetValue(audio)!;
            foreach(var sound in playing.Cast<object>().ToArray())
            {
                var player=(AudioStreamPlayer)AccessTools.Field(sound.GetType(),"player").GetValue(sound)!;
                if(player.Name.ToString() is not ("StreamPlayer-player_turn.mp3" or "StreamPlayer-enemy_turn.mp3"))continue;
                audio.Stop((int)AccessTools.Field(sound.GetType(),"id").GetValue(sound)!,0);sounds++;
            }
        }
        Runtime.Log("native-retired-turn-cues",new{Banners=banners,Sounds=sounds,UnrelatedSoundsStopped=false});
    }
}
