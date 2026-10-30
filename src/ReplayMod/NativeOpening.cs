using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ReplayMod;

// Banners keep native timing while opening navigation waits at a real choice,
// never at the unfinished scene before native combat initialization.
internal static class NativeOpening
{
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
