using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using System.Reflection;

namespace ReplayMod;

// These controllers are reused across attacks. Their native tasks must reach a
// stable end before the restored fight can start another effect on the same controller.
internal static class NativeCombatCamera
{
    private sealed class Scope(NCombatRoom room)
    {
        internal NCombatRoom Room {get;}=room;
        internal (NRadialBlurVfx Effect,Task Task)? Blur;
        internal (NHitStop Controller,CancellationTokenSource Token)? HitStop;
        internal List<Task> HitStopTasks {get;}=[];
    }
    private static Scope? _scope;
    private static readonly System.Reflection.FieldInfo HitStopToken=AccessTools.Field(typeof(NHitStop),"_cancelToken");
    private static readonly System.Reflection.FieldInfo RoomBlur=AccessTools.Field(typeof(NCombatRoom),"_radialBlur");
    private static readonly System.Reflection.FieldInfo BlurTween=AccessTools.Field(typeof(NRadialBlurVfx),"_tween");
    private static readonly FieldInfo ScreenShake=AccessTools.Field(typeof(NGame),"_screenShake");
    private static readonly FieldInfo ShakeOrigin=AccessTools.Field(typeof(NScreenShake),"_originalTargetPosition");
    private static readonly FieldInfo Punch=AccessTools.Field(typeof(NScreenShake),"_shakeInstance");
    private static readonly FieldInfo Rumble=AccessTools.Field(typeof(NScreenShake),"_rumbleInstance");
    private static readonly FieldInfo TraumaRumble=AccessTools.Field(typeof(NScreenShake),"_traumaRumble");
    private static readonly FieldInfo Trauma=AccessTools.Field(typeof(ScreenTraumaRumble),"_trauma");

    internal static void Install(Harmony harmony)
    {
        void Hook(Type type,string name,Type[] arguments,string handler,bool postfix=false)
        {
            var target=AccessTools.DeclaredMethod(type,name,arguments)??throw new MissingMethodException(type.FullName,name);
            var patch=new HarmonyMethod(typeof(NativeCombatCamera),handler);
            harmony.Patch(target,prefix:postfix?null:patch,postfix:postfix?patch:null);
        }
        Hook(typeof(NCombatRoom),nameof(NCombatRoom.RadialBlur),[typeof(VfxPosition)],nameof(AllowEffect));
        Hook(typeof(NGame),nameof(NGame.DoHitStop),[typeof(ShakeStrength),typeof(ShakeDuration)],nameof(AllowEffect));
        Hook(typeof(NRadialBlurVfx),"Animate",[],nameof(BlurStarted),postfix:true);
        Hook(typeof(NHitStop),"HitStopTask",[typeof(MegaCrit.Sts2.Core.Helpers.Ease.Functions),typeof(float)],nameof(HitStopStarted),postfix:true);
        Hook(typeof(NScreenShake),nameof(NScreenShake.Shake),[typeof(ShakeStrength),typeof(ShakeDuration),typeof(float)],nameof(AllowScreenOffset));
        Hook(typeof(NScreenShake),nameof(NScreenShake.Rumble),[typeof(ShakeStrength),typeof(ShakeDuration),typeof(RumbleStyle)],nameof(AllowScreenOffset));
        Hook(typeof(NScreenShake),nameof(NScreenShake.AddTrauma),[typeof(ShakeStrength)],nameof(AllowScreenOffset));
    }
    private static bool AllowEffect()=>!NativeSeek.RetiringPresentation&&!NativePhaseRetirement.RetiredPresentationOrigin;
    private static bool AllowScreenOffset(NScreenShake __instance)=>AllowEffect()||NCombatRoom.Instance is not {} room||
        !ReferenceEquals(__instance.ShakeTarget,room.SceneContainer);
    internal static void SettleOffsets(NCombatRoom room,string boundary)
    {
        if(NGame.Instance is not {} game||ScreenShake.GetValue(game) is not NScreenShake shake||
            shake.ShakeTarget is not {} target||!ReferenceEquals(target,room.SceneContainer))return;
        var origin=(Vector2)ShakeOrigin.GetValue(shake)!;
        var delta=target.Position-origin;
        var punch=Punch.GetValue(shake) is not null;
        var rumble=Rumble.GetValue(shake) is not null;
        var trauma=TraumaRumble.GetValue(shake) as ScreenTraumaRumble;
        var traumaAmount=trauma is null?0:(float)Trauma.GetValue(trauma)!;
        if(punch)Punch.SetValue(shake,null);
        if(rumble)Rumble.SetValue(shake,null);
        if(traumaAmount>0)Trauma.SetValue(trauma,0f);
        target.Position=origin;
        Runtime.Log("native-combat-camera-settled",new{Boundary=boundary,NativeSeek.Generation,OffsetX=delta.X,OffsetY=delta.Y,
            Punch=punch,Rumble=rumble,Trauma=traumaAmount,RoomTargetKept=true});
    }
    private static Scope ForRoom(NCombatRoom room)
    {
        if(_scope is {} current&&!ReferenceEquals(current.Room,room))RoomExiting(current.Room);
        return _scope??=new(room);
    }
    private static void BlurStarted(NRadialBlurVfx __instance,Task __result)
    {
        if(!NativeSeek.Enabled||NCombatRoom.Instance is not {} room||!ReferenceEquals(RoomBlur.GetValue(room),__instance))return;
        ForRoom(room).Blur=(__instance,__result);
        Runtime.Log("native-combat-camera-started",new{Effect="radial-blur",NativeSeek.Generation,Native=true});
    }
    private static void HitStopStarted(NHitStop __instance,Task __result)
    {
        if(!NativeSeek.Enabled||NCombatRoom.Instance is not {} room||HitStopToken.GetValue(__instance) is not CancellationTokenSource token)return;
        var scope=ForRoom(room);
        scope.HitStop=(__instance,token);scope.HitStopTasks.RemoveAll(task=>task.IsCompleted);scope.HitStopTasks.Add(__result);
        Runtime.Log("native-combat-camera-started",new{Effect="hit-stop",NativeSeek.Generation,Native=true});
    }
    private static Task[] CancelHitStop(Scope scope)
    {
        if(scope.HitStop is not {} hit||!ReferenceEquals(HitStopToken.GetValue(hit.Controller),hit.Token))return [];
        var active=scope.HitStopTasks.Where(task=>!task.IsCompleted).ToArray();
        if(active.Length==0)return [];
        hit.Token.Cancel();
        Engine.TimeScale=1; // Same endpoint as the original HitStopTask.
        return active;
    }
    internal static void RoomExiting(NCombatRoom room)
    {
        if(_scope is not {} scope||!ReferenceEquals(scope.Room,room))return;
        _scope=null;
        var hitStops=CancelHitStop(scope);
        if(hitStops.Length>0)Runtime.Log("native-combat-camera-retired",new{Reason="native-room-exit",HitStops=hitStops.Length,TimeScale=Engine.TimeScale});
    }
    internal static async Task RetireForNavigation(NCombatRoom room)
    {
        if(_scope is not {} scope||!ReferenceEquals(scope.Room,room))return;
        _scope=null;
        var hitStops=CancelHitStop(scope);
        var blur=scope.Blur is {} native&&!native.Task.IsCompleted?native.Effect:null;
        if(blur is not null&&GodotObject.IsInstanceValid(blur)&&BlurTween.GetValue(blur) is Tween tween&&tween.IsValid()&&tween.IsRunning())
        {
            // Run the original one-second tween through its Finished signal. Killing it
            // would leave Animate awaiting the signal while the room remains in-tree.
            tween.CustomStep(2);
        }
        if(blur is not null)await scope.Blur!.Value.Task;
        // The native loop checks the controller's current token on the next frame.
        // Wait for every abandoned loop before allowing a redo to replace that token.
        if(hitStops.Length>0)await Task.WhenAll(hitStops);
        if(blur is not null||hitStops.Length>0)Runtime.Log("native-combat-camera-retired",new{Reason="navigation",RadialBlur=blur is not null,
            HitStops=hitStops.Length,TimeScale=Engine.TimeScale,NativeTasksFinished=true});
    }
}
