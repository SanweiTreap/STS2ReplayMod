using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace ReplayMod;

// Persistent native display nodes, keyed by the actual card instance. No ForgeCmd/business calls.
internal sealed class RegentPresentation
{
    private static readonly HashSet<NSovereignBladeVfx> Known=[];
    private sealed record ForgeStart(float Damage,Vector2 Scale,Color Glow);
    private static readonly Dictionary<NSovereignBladeVfx,ForgeStart> Forges=[];
    private static readonly string[] PartFields=["_spineNode","_bladeGlow","_hilt","_hilt2","_detail","_forgeSparks","_spawnFlames","_spawnFlamesBack","_slashParticles","_chargeParticles","_spikeParticles","_spikeParticles2","_spikeCircle","_spikeCircle2"];
    private static object? Get(object node,string field)=>AccessTools.Field(node.GetType(),field).GetValue(node);
    private static void Set(object node,string field,object value)=>AccessTools.Field(node.GetType(),field).SetValue(node,value);
    private static bool Valid(NSovereignBladeVfx node)=>GodotObject.IsInstanceValid(node)&&!node.IsQueuedForDeletion();
    private static void Discover()
    {
        if(NCombatRoom.Instance?.GetCreatureNode(LiveState.Player.Creature) is not {} player)return;
        foreach(var node in player.GetChildren().OfType<NSovereignBladeVfx>())if(Valid(node))Known.Add(node);
    }
    internal static void Clear(){Known.Clear();Forges.Clear();}
    internal static bool CoreForgeAnimating=>Known.Any(n=>Valid(n)&&n.Visible&&(bool)Get(n,"_isForging")!&&
        new[]{"_scaleTween","_glowTween"}.Any(field=>Get(n,field) is Tween tween&&GodotObject.IsInstanceValid(tween)&&tween.IsRunning()));
    private static void Stop(NSovereignBladeVfx node)
    {
        if((bool)Get(node,"_isAttacking")!)AccessTools.Method(typeof(NSovereignBladeVfx),"CleanupAttack").Invoke(node,null);
        foreach(var field in new[]{"_attackTween","_scaleTween","_sparkDelay","_glowTween","_trailFadeTween"})
            if(Get(node,field) is Tween tween&&GodotObject.IsInstanceValid(tween))tween.Kill();
        Set(node,"_isForging",false);Set(node,"_isAttacking",false);
    }
    internal sealed record Part(string Path,Transform2D Transform,Color Color,Color SelfColor,bool Visible,bool? Emitting=null,int Amount=0);
    internal sealed record Phase(string Animation,float Time,bool Loop);
    internal sealed record ForgeMotion(float Damage,Vector2 StartScale,Color StartGlow,double? ScaleTime,double? GlowTime);
    internal sealed record Pose(CardModel Card,Transform2D Transform,double Orbit,float BladeSize,Vector2 Target,bool Behind,Part[] Parts,Phase? Animation,ForgeMotion? Forging,int Layer)
    {
        internal static Pose Capture(NSovereignBladeVfx node)
        {
            Known.Add(node);
            var parts=PartFields.Select(field=>(CanvasItem)Get(node,field)!).Select(part=>new Part(
                NativeDisplayTrace.RelativePath(node,part),part is Node2D n?n.Transform:((Control)part).GetTransform(),part.Modulate,part.SelfModulate,part.Visible,
                (part as GpuParticles2D)?.Emitting,(part as GpuParticles2D)?.Amount??0)).ToArray();
            Phase? phase=null;
            using(var state=((MegaSprite)Get(node,"_animController")!).TryGetAnimationState())
            using(var track=state?.GetCurrent(0))if(track is not null)phase=new(track.GetAnimationName(),track.GetTrackTime(),track.IsLoop());
            double? Time(string field)=>Get(node,field) is Tween tween&&GodotObject.IsInstanceValid(tween)&&tween.IsRunning()?tween.GetTotalElapsedTime():null;
            var scale=Time("_scaleTween");var glow=Time("_glowTween");
            var motion=(bool)Get(node,"_isForging")!&&Forges.TryGetValue(node,out var start)&& (scale is not null||glow is not null)
                ?new ForgeMotion(start.Damage,start.Scale,start.Glow,scale,glow):null;
            return new(node.Card,node.Transform,node.OrbitProgress,(float)Get(node,"_bladeSize")!,(Vector2)Get(node,"_targetOrbitPosition")!,
                (bool)Get(node,"_isBehindCharacter")!,parts,phase,motion,node.GetIndex());
        }
        internal NSovereignBladeVfx Apply(bool resumeForge=true)
        {
            var node=Resolve(Card);Stop(node);node.Transform=Transform;node.OrbitProgress=Orbit;
            Set(node,"_bladeSize",BladeSize);Set(node,"_targetOrbitPosition",Target);Set(node,"_isBehindCharacter",Behind);
            node.GetParent().MoveChild(node,Math.Clamp(Layer,0,node.GetParent().GetChildCount()-1));
            foreach(var part in Parts)
            {
                var item=node.GetNode<CanvasItem>(part.Path);item.Modulate=part.Color;item.SelfModulate=part.SelfColor;item.Visible=part.Visible;
                if(item is Node2D n)n.Transform=part.Transform;
                else if(item is Control control){control.Position=part.Transform.Origin;control.Rotation=part.Transform.Rotation;control.Scale=part.Transform.Scale;}
                if(item is GpuParticles2D particles&&part.Emitting is {} emitting){particles.Amount=part.Amount;particles.Emitting=emitting;}
            }
            if(Animation is {} phase)SetPhase(node,phase);
            if(resumeForge&&Forging is {} motion)ContinueForge(node,motion);
            node.Visible=true;node.SetProcess(true);((Control)Get(node,"_hitbox")!).MouseFilter=Control.MouseFilterEnum.Ignore;
            return node;
        }
    }
    private readonly Pose[] _poses;
    internal RegentPresentation()
    {
        Discover();_poses=Known.Where(n=>Valid(n)&&n.Visible).Select(Pose.Capture).ToArray();
    }
    private static NSovereignBladeVfx Resolve(CardModel card)
    {
        Discover();var node=Known.FirstOrDefault(n=>Valid(n)&&ReferenceEquals(n.Card,card));
        if(node is not null)return node;
        node=NSovereignBladeVfx.Create(card)??throw new InvalidDataException("native-blade-display-create");
        NCombatRoom.Instance!.GetCreatureNode(LiveState.Player.Creature)!.AddChild(node);Known.Add(node);return node;
    }
    internal void Restore(bool continuePresentation)
    {
        Discover();var cards=_poses.Select(p=>p.Card).ToHashSet(ReferenceEqualityComparer.Instance);
        foreach(var node in Known.Where(Valid).ToArray())if(!cards.Contains(node.Card)){Stop(node);node.Visible=false;node.SetProcess(false);}
        foreach(var pose in _poses)
        {
            var node=Resolve(pose.Card);
            if(continuePresentation&&node.Visible)continue;
            node=pose.Apply(resumeForge:false);
            SetPhase(node,new("idle_loop",pose.Animation is {Animation:"idle_loop"} idle?idle.Time:0,true));
            ((Node2D)Get(node,"_spineNode")!).Scale=Vector2.One*Mathf.Lerp(.9f,2,pose.BladeSize);
            ((Node2D)Get(node,"_bladeGlow")!).Visible=false;
            foreach(var field in new[]{"_forgeSparks","_spawnFlames","_spawnFlamesBack","_slashParticles"})
            {var particles=(GpuParticles2D)Get(node,field)!;particles.Emitting=false;particles.Visible=false;}
        }
        if(_poses.Length>0)Runtime.Log("native-blade-displays-restored",new{Cards=_poses.Select(p=>p.Card.Id.ToString()).ToArray(),Count=_poses.Length,
            ContinuedNativeDisplay=continuePresentation,ForgeBusinessRedispatched=false});
    }
    internal static void Forge(Pose before,float damage,bool flames)
    {
        var node=before.Apply();
        foreach(var field in new[]{"_forgeSparks","_spawnFlames","_spawnFlamesBack","_slashParticles"})((CanvasItem)Get(node,field)!).Visible=true;
        node.Forge(damage,flames);
        Runtime.Log("native-forge-display-replayed",new{Model=before.Card.Id.ToString(),Damage=damage,Flames=flames,ForgeCmdCalled=false});
    }
    internal static void Attack(Pose before,Vector2 target)
    {
        var node=before.Apply();((Line2D)Get(node,"_trail")!).ClearPoints();((CanvasItem)Get(node,"_slashParticles")!).Visible=true;node.Attack(target);
        Runtime.Log("native-blade-attack-phase",new{Card=before.Card.Id.ToString(),before.Animation,before.Forging,before.Orbit,before.BladeSize,
            SourceParticles=before.Parts.Where(p=>p.Emitting is not null).Select(p=>new{p.Path,p.Visible,p.Emitting,p.Amount}).ToArray(),NativeAttackCalledOnce=true});
    }
    internal static void NoteForge(NSovereignBladeVfx node,float damage)
        =>Forges[node]=new(damage,((Node2D)Get(node,"_spineNode")!).Scale,((CanvasItem)Get(node,"_bladeGlow")!).Modulate);
    private static void SetPhase(NSovereignBladeVfx node,Phase phase)
    {
        var sprite=(MegaSprite)Get(node,"_animController")!;
        node.RunWhenSpineReady(sprite,state=>
        {
            state.SetAnimation(phase.Animation,phase.Loop);
            using(var track=state.GetCurrent(0)){track?.SetMixDuration(0);track?.SetTrackTime(phase.Time);}
            state.Update(0);using var skeleton=sprite.GetSkeleton();if(skeleton is not null)state.Apply(skeleton);
        });
    }
    private static void ContinueForge(NSovereignBladeVfx node,ForgeMotion motion)
    {
        var spine=(Node2D)Get(node,"_spineNode")!;var glow=(CanvasItem)Get(node,"_bladeGlow")!;
        var scale=Vector2.One*Mathf.Lerp(.9f,2,Mathf.Clamp(motion.Damage/200,0,1));
        Set(node,"_isForging",true);
        if(motion.ScaleTime is {} scaleTime)
        {
            var tween=node.CreateTween();Set(node,"_scaleTween",tween);
            tween.TweenProperty(spine,"scale",scale*1.2f,.05).From(motion.StartScale).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
            if(scaleTime<.05)tween.TweenCallback(Callable.From(()=>{AccessTools.Method(typeof(NSovereignBladeVfx),"FireSparks").Invoke(node,null);}));
            tween.TweenProperty(spine,"scale",scale,.3).SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Cubic);
            tween.CustomStep(scaleTime);
        }
        if(motion.GlowTime is {} glowTime)
        {
            var color=Color.FromHtml("#ff7300");var end=color;end.A=0;
            var tween=node.CreateTween();Set(node,"_glowTween",tween);
            tween.TweenProperty(glow,"modulate",color,.05).From(motion.StartGlow).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(glow,"modulate",end,.5).SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Cubic);
            tween.TweenCallback(Callable.From(()=>{AccessTools.Method(typeof(NSovereignBladeVfx),"CleanupForge").Invoke(node,null);}));
            tween.CustomStep(glowTime);
        }
    }
    internal static void Orbit((CardModel Card,double Orbit)[] layout)
    {
        foreach(var (card,orbit) in layout){var node=Resolve(card);node.OrbitProgress=orbit;}
    }
    internal static (CardModel Card,double Orbit)[] Layout()
    {
        Discover();return Known.Where(n=>Valid(n)&&n.Visible).Select(n=>(n.Card,n.OrbitProgress)).ToArray();
    }
}
