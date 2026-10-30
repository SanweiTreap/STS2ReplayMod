using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ReplayMod;

internal static class NativePhaseRetirement
{
    private sealed record Owner(object State,int Generation,PendingOperation Operation);
    private static readonly AsyncLocal<Owner?> FlowOwner=new();
    private static readonly Dictionary<(NDebugAudioManager Manager,int Id),Owner> Sounds=[];
    private static Owner? _retiringOwner;
    private static int _stoppedSounds,_mutedSounds;
    private static RetiringPhaseWait _waits=New();
    private static RetiringPhaseWait New()=>new(error=>Runtime.Log("retired-phase-timer-fault",new{Exception=error.ToString(),GameplayTask=false}));
    private static Type Machine(Type owner,string method)=>AccessTools.Method(owner,method)
        .GetCustomAttributes(typeof(AsyncStateMachineAttribute),false).Cast<AsyncStateMachineAttribute>().Single().StateMachineType;
    private static readonly Type TurnMachine=Machine(typeof(CombatManager),"StartTurn");
    private static readonly Dictionary<Type,string> PacingCallers=ReviewedCallers();
    // Fixed native OnPlay has one Wait: a pure anticipation timer, followed by the
    // normal AttackCommand. No damage, hook, UI decision or VFX completion is skipped.
    private static readonly Type AnticipationCaller=Machine(typeof(GrandFinale),"OnPlay");
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
    internal static void Clear(){RetireSounds(null);Sounds.Clear();_retiringOwner=null;_waits=New();FlowOwner.Value=null;}
    internal static void Install(Harmony h)
    {
        h.Patch(AccessTools.DeclaredMethod(typeof(Cmd),"CustomScaledWait",
            [typeof(float),typeof(float),typeof(bool),typeof(CancellationToken)]),postfix:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(WaitAfter)));
        h.Patch(AccessTools.Method(typeof(CombatManager),"StartTurn"),
            prefix:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(TurnBefore)),
            finalizer:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(TurnReturned)));
        h.Patch(AccessTools.DeclaredMethod(typeof(PlayCardAction),"ExecuteAction"),
            prefix:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(CardBefore)),
            finalizer:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(TurnReturned)));
        h.Patch(AccessTools.DeclaredMethod(typeof(Cmd),"Wait",[typeof(float),typeof(bool)]),
            postfix:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(PlainWaitAfter)));
        h.Patch(AccessTools.DeclaredMethod(typeof(NDebugAudioManager),"Play"),
            prefix:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(OneShotVolume)),
            postfix:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(SoundAfter)));
        foreach(var method in AccessTools.GetDeclaredMethods(typeof(NAudioManager)).Where(m=>m.Name=="PlayOneShot"))
            h.Patch(method,prefix:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(OneShotVolume)));
    }
    private static void TurnBefore(object __0,out Owner? __state)
    {
        __state=FlowOwner.Value;
        var state=AccessTools.Property(__0.GetType(),"State").GetValue(__0);
        FlowOwner.Value=NativeSeek.Enabled&&!PlayerLoop.TearingDown&&ActionMonitor.Pending is {Template.Kind:OperationKind.EndTurn} pending&&
            ReferenceEquals(state,CombatManager.Instance.DebugOnlyGetState())?new(state!,NativeSeek.Generation,pending):null;
    }
    private static void CardBefore(GameAction __instance,out Owner? __state)
    {
        __state=FlowOwner.Value;
        if(NativeSeek.Enabled&&!PlayerLoop.TearingDown&&ActionMonitor.Pending is {Template.Kind:OperationKind.Card} pending&&
            ReferenceEquals(pending.Action,__instance)&&CombatManager.Instance.DebugOnlyGetState() is {} state)
            FlowOwner.Value=new(state,NativeSeek.Generation,pending);
        // Derived/automatic actions keep their valid parent operation's ownership.
        // CurrentOwner still rejects an old pending reference, room or generation.
    }
    private static void TurnReturned(Owner? __state)=>FlowOwner.Value=__state;
    private static Owner? CurrentOwner()=>NativeSeek.Enabled&&!PlayerLoop.TearingDown&&FlowOwner.Value is {} owner&&
        owner.Generation==NativeSeek.Generation&&ReferenceEquals(owner.Operation,ActionMonitor.Pending)&&
        ReferenceEquals(owner.State,CombatManager.Instance.DebugOnlyGetState())?owner:null;
    private static void WaitAfter(float __0,float __1,ref Task __result)
    {
        if(CurrentOwner() is not {} owner)return;
        // Match only reviewed DIRECT pacing call sites, not an ancestor. Their
        // damage/AI/hooks still run; other waits, signals and tweens stay intact.
        foreach(var frame in new StackTrace().GetFrames())
        {
            var type=frame.GetMethod()?.DeclaringType;
            if(type?.Assembly!=typeof(CombatManager).Assembly||type==typeof(Cmd)||type.DeclaringType==typeof(Cmd))continue;
            if(PacingCallers.TryGetValue(type,out var source))
            {
                Runtime.Log("native-phase-wait-bound",new{Generation=owner.Generation,Side=((CombatState)owner.State).CurrentSide.ToString(),
                    FastSeconds=__0,StandardSeconds=__1,Retiring=_waits.Active,TimerAlreadyCompleted=__result.IsCompleted,
                    CurrentRoomAndGenerationMatched=true,DirectStartTurnCaller=type==TurnMachine,PacingSource=source,
                    Kind=owner.Operation.Template.Kind.ToString(),owner.Operation.Template.Card});
                __result=_waits.Wrap(__result);
            }
            break;
        }
    }
    private static void PlainWaitAfter(float __0,ref Task __result)
    {
        if(CurrentOwner() is not {Operation.Template.Kind:OperationKind.Card} owner)return;
        foreach(var frame in new StackTrace().GetFrames())
        {
            var type=frame.GetMethod()?.DeclaringType;
            if(type?.Assembly!=typeof(CombatManager).Assembly||type==typeof(Cmd)||type.DeclaringType==typeof(Cmd))continue;
            if(type==AnticipationCaller)
            {
                Runtime.Log("native-phase-wait-bound",new{Generation=owner.Generation,Kind="Card",owner.Operation.Template.Card,
                    FastSeconds=__0,StandardSeconds=__0,Retiring=_waits.Active,TimerAlreadyCompleted=__result.IsCompleted,
                    CurrentRoomAndGenerationMatched=true,DirectStartTurnCaller=false,PacingSource="native-card-visual-anticipation"});
                __result=_waits.Wrap(__result);
            }
            break;
        }
    }
    private static bool Retiring(Owner owner)=>_waits.Active&&_retiringOwner is {} retired&&
        ReferenceEquals(owner.Operation,retired.Operation)&&ReferenceEquals(owner.State,retired.State)&&owner.Generation==retired.Generation;
    private static void SoundAfter(NDebugAudioManager __instance,int __result)
    {
        if(CurrentOwner() is not {} owner)return;
        var playing=(IEnumerable)AccessTools.Field(typeof(NDebugAudioManager),"_playingSounds").GetValue(__instance)!;
        var live=playing.Cast<object>().Select(s=>(int)AccessTools.Field(s.GetType(),"id").GetValue(s)!).ToHashSet();
        foreach(var key in Sounds.Keys.Where(k=>ReferenceEquals(k.Manager,__instance)&&!live.Contains(k.Id)).ToArray())Sounds.Remove(key);
        Sounds[(__instance,__result)]=owner;
        // Play still ran, including native pitch RNG and pool bookkeeping.
        if(Retiring(owner)){__instance.Stop(__result,0);Sounds.Remove((__instance,__result));_stoppedSounds++;Runtime.Log("retired-owned-sound-stopped",new{Id=__result,owner.Generation,NativePlayRan=true});}
    }
    private static void OneShotVolume(ref float volume)
    {if(CurrentOwner() is {} owner&&Retiring(owner)){volume=0;_mutedSounds++;}}
    private static void RetireSounds(Owner? owner)
    {
        if(NDebugAudioManager.Instance is not {} audio)return;
        var playing=(IEnumerable)AccessTools.Field(typeof(NDebugAudioManager),"_playingSounds").GetValue(audio)!;
        foreach(var sound in playing.Cast<object>().ToArray())
        {
            var id=(int)AccessTools.Field(sound.GetType(),"id").GetValue(sound)!;
            if(!Sounds.TryGetValue((audio,id),out var known)||owner is not null&&!ReferenceEquals(known.Operation,owner.Operation))continue;
            audio.Stop(id,0);Sounds.Remove((audio,id));_stoppedSounds++;
        }
    }
    internal static void Begin()
    {
        _retiringOwner=ActionMonitor.Pending is {} pending&&CombatManager.Instance.DebugOnlyGetState() is {} state?
            new(state,NativeSeek.Generation,pending):null;
        _stoppedSounds=0;_mutedSounds=0;
        RetireCues();RetireSounds(_retiringOwner);_waits.Begin();
    }
    internal static void End()
    {
        RetireSounds(_retiringOwner);
        Runtime.Log("native-phase-retirement-finished",new{_waits.Released,_waits.Skipped,
            OwnedDebugSoundsStopped=_stoppedSounds,OwnedAudioCallsMuted=_mutedSounds,
            OnlyReviewedOwnedPacingTimers=true,BusinessTasksCancelled=false,NativeEffectsAndRngStillVerified=true});
        _waits.End();_retiringOwner=null;
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
