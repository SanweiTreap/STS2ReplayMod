using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ReplayMod;

internal static class NativePhaseRetirement
{
    private sealed record Owner(object State,int Generation);
    private static readonly AsyncLocal<Owner?> TurnOwner=new();
    private static RetiringPhaseWait _waits=New();
    private static RetiringPhaseWait New()=>new(error=>Runtime.Log("retired-phase-timer-fault",new{Exception=error.ToString(),GameplayTask=false}));
    private static readonly Type TurnMachine=AccessTools.Method(typeof(CombatManager),"StartTurn")
        .GetCustomAttributes(typeof(AsyncStateMachineAttribute),false).Cast<AsyncStateMachineAttribute>().Single().StateMachineType;
    internal static bool Active=>_waits.Active;
    internal static void Clear(){_waits=New();TurnOwner.Value=null;}
    internal static void Install(Harmony h)
    {
        h.Patch(AccessTools.DeclaredMethod(typeof(Cmd),"CustomScaledWait",
            [typeof(float),typeof(float),typeof(bool),typeof(CancellationToken)]),postfix:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(WaitAfter)));
        h.Patch(AccessTools.Method(typeof(CombatManager),"StartTurn"),
            prefix:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(TurnBefore)),
            finalizer:new HarmonyMethod(typeof(NativePhaseRetirement),nameof(TurnReturned)));
    }
    private static void TurnBefore(object __0,out Owner? __state)
    {
        __state=TurnOwner.Value;
        var state=AccessTools.Property(__0.GetType(),"State").GetValue(__0);
        TurnOwner.Value=NativeSeek.Enabled&&!PlayerLoop.TearingDown&&ReferenceEquals(state,CombatManager.Instance.DebugOnlyGetState())?
            new(state!,NativeSeek.Generation):null;
    }
    private static void TurnReturned(Owner? __state)=>TurnOwner.Value=__state;
    private static void WaitAfter(float __0,float __1,ref Task __result)
    {
        if(!NativeSeek.Enabled||PlayerLoop.TearingDown||ActionMonitor.Pending?.Template.Kind!=OperationKind.EndTurn||
            TurnOwner.Value is not {} owner||owner.Generation!=NativeSeek.Generation||
            !ReferenceEquals(owner.State,CombatManager.Instance.DebugOnlyGetState()))return;
        // Match the DIRECT native caller, not any ancestor: hooks and visual tasks
        // invoked by StartTurn retain their own waits, including their polling loops.
        foreach(var frame in new StackTrace().GetFrames())
        {
            var type=frame.GetMethod()?.DeclaringType;
            if(type?.Assembly!=typeof(CombatManager).Assembly||type==typeof(Cmd)||type.DeclaringType==typeof(Cmd))continue;
            if(type==TurnMachine)
            {
                Runtime.Log("native-phase-wait-bound",new{Generation=owner.Generation,Side=((CombatState)owner.State).CurrentSide.ToString(),
                    FastSeconds=__0,StandardSeconds=__1,Retiring=_waits.Active,TimerAlreadyCompleted=__result.IsCompleted,
                    CurrentRoomAndGenerationMatched=true,DirectStartTurnCaller=true});
                __result=_waits.Wrap(__result);
            }
            break;
        }
    }
    internal static void Begin()
    {
        RetireCues();_waits.Begin();
    }
    internal static void End()
    {
        Runtime.Log("native-phase-retirement-finished",new{_waits.Released,_waits.Skipped,
            OnlyDirectStartTurnTimer=true,BusinessTasksCancelled=false,NativeEffectsAndRngStillVerified=true});
        _waits.End();
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
