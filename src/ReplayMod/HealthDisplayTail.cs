using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ReplayMod;

// Retain the native delayed HP tween's phase, without retaining its original callback/task.
internal static class HealthDisplayTail
{
    internal sealed record Motion(float Start,float Target,double Delay,double Elapsed);
    internal sealed record Sample(Creature Creature,Motion Motion);
    private sealed record Running(Tween Tween,Motion Motion);
    private static readonly Dictionary<NHealthBar,Running> RunningTweens=[];
    private static object Get(object node,string field)=>NativeDisplayTrace.ReadField(node,field)!;
    internal static void Clear()=>RunningTweens.Clear();
    internal static void SettleRestored(IEnumerable<Creature> creatures)
    {
        if(NativeDisplayTrace.Replaying)return;
        var count=0;
        foreach(var creature in creatures)if(Bar(creature) is {} bar&&Get(bar,"_middlegroundTween") is Tween tween&&GodotObject.IsInstanceValid(tween)&&tween.IsRunning())
        {tween.CustomStep(2.1);count++;}
        Runtime.Log("back-health-tails-settled",new{Count=count,RestoredHpDoesNotAnimateAsHealing=true});
    }
    internal static void Install(Harmony harmony)=>harmony.Patch(AccessTools.DeclaredMethod(typeof(NHealthBar),"RefreshMiddleground"),
        prefix:new HarmonyMethod(typeof(HealthDisplayTail),nameof(Before)),postfix:new HarmonyMethod(typeof(HealthDisplayTail),nameof(After)));
    private static void Before(NHealthBar __instance,out (Tween? Tween,float Offset) __state)
        =>__state=(Get(__instance,"_middlegroundTween") as Tween,((Control)Get(__instance,"_hpMiddleground")).OffsetRight);
    private static void After(NHealthBar __instance,(Tween? Tween,float Offset) __state)
    {
        if(!NativeDisplayTrace.Capturing||Get(__instance,"_middlegroundTween") is not Tween tween||ReferenceEquals(tween,__state.Tween))return;
        var poison=(Control)Get(__instance,"_poisonForeground");var foreground=(Control)Get(__instance,"_hpForeground");
        var target=poison.Visible?poison.OffsetRight:foreground.OffsetRight;
        RunningTweens[__instance]=new(tween,new(__state.Offset+1,target-2,target>=__state.Offset?0:1,0));
    }
    internal static Sample[] Capture(IEnumerable<Creature> creatures)=>creatures.Select(creature=>
    {
        var bar=Bar(creature);
        return bar is not null&&RunningTweens.TryGetValue(bar,out var running)&&GodotObject.IsInstanceValid(running.Tween)&&running.Tween.IsRunning()
            ?new Sample(creature,running.Motion with{Elapsed=running.Tween.GetTotalElapsedTime()}):null;
    }).OfType<Sample>().ToArray();
    private static NHealthBar? Bar(Creature creature)=>NCombatRoom.Instance?.GetCreatureNode(creature) is {} node?
        Get(Get(node,"_stateDisplay"),"_healthBar") as NHealthBar:null;
    internal static void Continue(Sample[] samples)
    {
        foreach(var sample in samples)
        {
            if(Bar(sample.Creature) is not {} bar)continue;
            var control=(Control)Get(bar,"_hpMiddleground");var motion=sample.Motion;
            if(Get(bar,"_middlegroundTween") is Tween old&&GodotObject.IsInstanceValid(old))old.Kill();
            control.OffsetRight=motion.Start;
            var tween=bar.CreateTween();tween.TweenProperty(control,"offset_right",motion.Target,1).SetDelay(motion.Delay)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
            AccessTools.Field(typeof(NHealthBar),"_middlegroundTween").SetValue(bar,tween);
            tween.CustomStep(motion.Elapsed);
        }
        Runtime.Log("native-health-tails-continued",new{Count=samples.Length,NativePhaseRetained=true,GameplayRedispatched=false});
    }
}
