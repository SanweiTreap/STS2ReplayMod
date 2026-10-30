using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ReplayMod;

// Only a clean opening re-entry waits here. The initial open proceeds naturally to
// its first choice or playable state. This gate owns NEW native tasks, never a memento.
internal static class NativeOpening
{
    private static TaskCompletionSource? _gate;
    private static bool _hold,_invoking;
    internal static bool Waiting=>_gate is {Task.IsCompleted:false};
    internal static void Arm(){Cancel();_hold=true;}
    internal static void Release()=>_gate?.TrySetResult();
    internal static void Cancel(){_hold=false;_gate?.TrySetCanceled();_gate=null;}
    internal static bool Start(CombatManager manager,object state,ref Task result)
    {
        if(!NativeSeek.Enabled||!_hold||_invoking)return true;
        _hold=false;var gate=new TaskCompletionSource();_gate=gate;
        result=Continue(manager,state,gate.Task);NativeSeek.OpeningStartReady();return false;
    }
    private static async Task Continue(CombatManager manager,object state,Task gate)
    {
        await gate;Task original;
        try
        {
            _invoking=true;
            original=(Task)AccessTools.Method(typeof(CombatManager),"StartCombatInternal").Invoke(manager,[state])!;
        }
        finally{_invoking=false;}
        await original;
    }
    internal static bool TurnBanner(int turn,ref NPlayerTurnBanner? result)
    {
        if(!NativeSeek.Enabled||PlayerLoop.TearingDown)return true;
        var state=CombatManager.Instance.DebugOnlyGetState();
        if(state is null||state.Players.Count!=1||state.Players[0].PlayerCombatState is not {} pcs||pcs.TurnNumber<=turn)return true;
        result=null;Runtime.Log("opening-stale-banner-skipped",new{RequestedTurn=turn,CurrentTurn=pcs.TurnNumber,GameplayUnchanged=true});return false;
    }
    internal static void BannerReady(Control banner)
    {
        if(!NativeSeek.Enabled)return;
        void Ignore(Control node)
        {
            node.MouseFilter=Control.MouseFilterEnum.Ignore;
            foreach(var child in node.GetChildren().OfType<Control>())Ignore(child);
        }
        Ignore(banner);
        Runtime.Log("native-banner-display-only",new{Type=banner.GetType().Name,NativeTiming=true,BlocksReplayControl=false});
    }
}
