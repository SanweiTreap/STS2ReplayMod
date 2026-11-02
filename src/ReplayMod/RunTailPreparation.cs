using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;

namespace ReplayMod;

// Explicit developer-only prefix gap. The next battle checkpoint is the legal
// tail start; the prepared shortcut is never claimed as a full native new run.
internal static class RunTailPreparation
{
    internal static bool Armed;
    private static RunState? _prepared;
    internal static string? Marker=>ReferenceEquals(_prepared,RunManager.Instance.DebugOnlyGetState())?
        "RUN005-explicit-act2-current-map-to-native-Boss-prefix-gap":null;
    internal static void Apply(ActMap map)
    {
        if(!Armed||PlayerLoop.Active||!RunRecording.Enabled)return;
        var run=LiveState.Run;
        if(run.CurrentActIndex!=1||run.CurrentMapCoord is not {} coord)return;
        Codec.Require(run.AscensionLevel==0&&run.Modifiers.Count==0&&run.Players.Count==1&&
            map.GetPoint(coord) is {} point&&point.PointType==MapPointType.Monster&&coord.row<map.BossMapPoint.coord.row,
            "tail-preparation-not-at-act2-ordinary-room");
        Armed=false;_prepared=run;
        RunRecording.Unsupported("explicit-tail-preparation","显式后段准备跳过第二幕中段；此前路线不作为连续整局观看证明");
        var source=map.GetPoint(coord)!;
        foreach(var child in source.Children.ToArray())source.RemoveChildPoint(child);
        source.AddChildPoint(map.BossMapPoint);
        Runtime.Log("run-tail-shortcut-prepared",new{Act=run.CurrentActIndex,Model=run.Act.Id.ToString(),From=new{coord.row,coord.col},
            Boss=new{map.BossMapPoint.coord.row,map.BossMapPoint.coord.col},BeforeNextBattleCheckpoint=true,
            NativeBossModelUnchanged=true,PrefixExplicitlyGapped=true,PlayerResourcesAndRngUnchanged=true});
        Runtime.Status="RUN-005后段准备：当前第二幕普通节点直连原生Boss；从Boss合法起点验证后段，不当完整三幕";
    }
}
