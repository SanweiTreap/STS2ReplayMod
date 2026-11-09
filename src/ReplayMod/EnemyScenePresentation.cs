using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.Animation;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Backgrounds;

namespace ReplayMod;

// Stable state of audited visual subtrees. Tracks and particles contain values,
// not native timeline objects that might still be writing to the source scene.
internal sealed class EnemyScenePresentation
{
    private sealed record Particle(NodePath Path,bool Visible,bool Emitting);
    private sealed record Layout(Node[] Nodes,CanvasItem[] Particles,HashSet<CanvasItem> Transient);
    private static readonly ConditionalWeakTable<Node,Layout> Layouts=new();
    private static readonly Dictionary<Node,List<Tween>> SceneTweens=[];
    private static readonly (Type Script,string[] Fields)[] TransientEmitters=
    [
        (typeof(NMechaKnightVfx),["_flameThrowerParticlesDark","_flameThrowerParticlesLight","_cinderParticles","_glowParticles"]),
        (typeof(NFlyconidSporesVfx),["_frailSpores","_vulnerableSpores"]),
        (typeof(NFogmogVfx),["_thrustParicles","_dustLeftParticles","_dustRightParticles"]),
        (typeof(NEntomancerVfx),["_attackingBugParticles"]),
        (typeof(NParafrightVfx),["_particles"]),
        (typeof(NTheObscuraVfx),["_particles"]),
        (typeof(NPhantasmalGardenerVfx),["_spewParticles"]),
        (typeof(NPhrogParasiteVfx),["_bubbleParticlesA","_bubbleParticlesB","_bubbleParticlesC","_gooParticlesDeath","_wormParticlesDeath"]),
        (typeof(NQueenVfx),["_sprayParticles"]),
        (typeof(NSlimedBerserkerVfx),["_gooParticlesR","_gooParticlesL","_gooParticlesVomit"]),
        (typeof(NSpinyToadVfx),["_spineParticles","_spineSubParticles"]),
        (typeof(NLostAndForgottenVfx),["_dustParticles"]),
        (typeof(NLivingGasVfx),["_attackPuffParticles","_attackSparkParticles","_debuffPuffParticles"]),
        (typeof(NFossilStalkerVfx),["_debuffParticles"]),
        (typeof(NHunterKillerVfx),["_particles"]),
        (typeof(NFakeMerchantVfx),["_particles"]),
        (typeof(NVantomVfx),["_sprayParticles","_deathSprayParticles","_deathSprayParticlesBack"]),
        (typeof(NSnappingJaxfruitVfx),["_glowParticles","_blobParticles"]),
        (typeof(NKaiserCrabBossVfx),["_regenSplatParticles","_plowChunkParticles","_steamParticles1","_steamParticles2","_steamParticles3","_smokeParticles","_sparkParticles","_spittleParticles"]),
        (typeof(NTestSubjectVfx),["_burnParticles","_targetedBurnParticle","_burnParticleFountain"])
    ];
    private readonly Particle[] _particles;
    private readonly Track? _secondary;
    private readonly bool _restoreSecondary;
    private readonly bool? _sleeping;
    private readonly Vector2? _coralScale,_vantomScale,_ringsScale;
    private readonly (string Path,Vector2 Position)? _localTarget;

    internal EnemyScenePresentation(NCreature node)
    {
        var visuals=node.Visuals;
        _particles=CaptureParticles(visuals);
        var localPath=node.Entity.Monster switch
        {
            BowlbugSilk or FuzzyWurmCrawler or BygoneEffigy or Tunneler=>"Visuals/SpineBoneNode",
            LeafSlimeM or TwigSlimeM=>"Visuals/SpitTarget",
            KinFollower or MagiKnight=>"Visuals/AttackDistanceControl",
            Guardbot or Noisebot or Stabbot or Zapbot=>"Visuals/FallControl",
            _=>null
        };
        if(localPath is not null&&node.GetSpecialNode<Node2D>(localPath) is {} targetBone)_localTarget=(localPath,targetBone.Position);
        _restoreSecondary=node.Entity.Monster is Aeonglass or LagavulinMatriarch or Queen or SoulNexus or Vantom or WaterfallGiant;
        if(_restoreSecondary)_secondary=Track.Capture(node.SpineAnimation.GetAnimationState(),1);
        if(SleepField(node) is {} field)_sleeping=field.GetValue(node.Entity.Monster) is NSleepingVfx sleep&&
            GodotObject.IsInstanceValid(sleep)&&sleep.IsInsideTree()&&!sleep.IsQueuedForDeletion();
        if(Find<NSewerClamVfx>(visuals) is {} clam&&Get<Node2D>(clam,"_scaleNode") is {} coral)
            _coralScale=CoralTargets.TryGetValue(clam,out var target)?Vector2.One*target:coral.Scale;
        if(node.Entity.Monster is Vantom&&node.GetSpecialNode<Node2D>("Visuals/ScalingBone") is {} scale)
            _vantomScale=VantomTargets.TryGetValue(scale,out var target)?Vector2.One*target:scale.Scale;
        if(Find<NCubexConstructVfx>(visuals) is {} cubex&&Get<Node2D>(cubex,"_rings") is {} rings)
            _ringsScale=RingScales.GetValue(cubex,_=>new(rings.Scale)).Value;
    }

    internal static void ClearStartupTrack(NCreature node)=>Track.Restore(node.SpineAnimation.GetAnimationState(),0,null);

    internal void Restore(NCreature node,bool freshScene)
    {
        var visuals=node.Visuals;
        if(_restoreSecondary)Track.Restore(node.SpineAnimation.GetAnimationState(),1,_secondary);
        if(_localTarget is {} target&&node.GetSpecialNode<Node2D>(target.Path) is {} bone)bone.Position=target.Position;
        if(_sleeping is {} sleeping)RestoreSleep(node,sleeping);
        RetireEntomancerSwarm(node);
        if(Find<NSnappingJaxfruitVfx>(visuals) is {} fruit)
        {fruit.SetTarget(null);Invoke(fruit,"ResetCast");}
        if(Find<NMyteVfx>(visuals) is {} myte)myte.SetTarget(null);
        foreach(var driver in GetLayout(visuals).Nodes.OfType<NDecimillipedeSegmentDriver>())
        {
            Kill(Get<Tween>(driver,"_attackTween"));
            AccessTools.Field(typeof(NDecimillipedeSegmentDriver),"_decimillipedeStrikeOffset").SetValue(driver,Vector2.Zero);
        }
        if(Find<NCubexConstructVfx>(visuals) is {} cubex)
        {
            if(Get<NLaserVfx>(cubex,"_laser") is {} laser){laser.ResetLaser();laser.Hide();}
            if(Get<Node2D>(cubex,"_rings") is {} rings){if(_ringsScale is {} size)rings.Scale=size;rings.Hide();}
        }
        if(Find<NSoulFyshVfx>(visuals) is {} fysh)
        {
            Get<ShaderMaterial>(fysh,"_soundShaderMat")?.SetShaderParameter("amount",.3f);
            Get<ShaderMaterial>(fysh,"_beckonShaderMat")?.SetShaderParameter("amount",.3f);
        }
        if(Find<NVantomVfx>(visuals) is {} vfx)
        {
            Get<ShaderMaterial>(vfx,"_tailShaderMat")?.SetShaderParameter("step",0f);
            if(_vantomScale is {} size&&node.GetSpecialNode<Node2D>("Visuals/ScalingBone") is {} scale)
            {scale.Scale=size;SetVantomTarget(scale,size.X);}
        }
        if(_coralScale is {} targetScale&&Find<NSewerClamVfx>(visuals) is {} clam&&Get<Node2D>(clam,"_scaleNode") is {} coral)
        {coral.Scale=targetScale;SetCoralTarget(clam,targetScale.X);}
        if(Find<NTestSubjectVfx>(visuals) is {} subject)
        {
            foreach(var field in new[]{"_burnTween1","_burnTween2","_burnTween3"})Kill(Get<Tween>(subject,field));
            Invoke(subject,"OnEndBurnVfx");Invoke(subject,"ClearBurnFire");
            foreach(var field in new[]{"_frontBurnVfxController","_backBurnVfxController"})
                Track.Restore(Get<MegaSprite>(subject,field)?.GetAnimationState(),0,null);
        }
        RestoreParticles(visuals,_particles,freshScene);
    }

    internal static void RetireEntomancerSwarms(NCombatRoom room)
    {
        var count=0;
        foreach(var node in room.CreatureNodes.Concat(room.RemovingCreatureNodes).Distinct())
            if(GodotObject.IsInstanceValid(node)&&RetireEntomancerSwarm(node))count++;
        if(count>0)Runtime.Log("native-entomancer-swarm-retired",new{Count=count,NativeSeek.Generation});
    }

    private static bool RetireEntomancerSwarm(NCreature node)
    {
        if(node.Entity.Monster is not Entomancer||!GodotObject.IsInstanceValid(node.Visuals)||
            Find<NEntomancerVfx>(node.Visuals) is not {} swarm||
            !GodotObject.IsInstanceValid(swarm)||!swarm.IsInsideTree()||
            AccessTools.Field(typeof(NEntomancerVfx),"_swarming").GetValue(swarm) is not true)return false;
        // The native cancel kills the position tween and returns the ambient
        // emitter to its base without stopping that emitter.
        Invoke(swarm,"CancelSwarmAttack");
        return true;
    }

    private static System.Reflection.FieldInfo? SleepField(NCreature node)=>node.Entity.Monster switch
    {
        LagavulinMatriarch=>AccessTools.Field(typeof(LagavulinMatriarch),"_sleepingVfx"),
        SlumberingBeetle=>AccessTools.Field(typeof(SlumberingBeetle),"_sleepingVfx"),
        _=>null
    };
    private static void RestoreSleep(NCreature node,bool sleeping)
    {
        if(SleepField(node) is not {} field||node.GetSpecialNode<Marker2D>("%SleepVfxPos") is not {} marker)return;
        var current=field.GetValue(node.Entity.Monster) as NSleepingVfx;
        foreach(var old in marker.GetChildren().OfType<NSleepingVfx>().ToArray())
        {
            if(sleeping&&ReferenceEquals(old,current)&&!old.IsQueuedForDeletion())continue;
            marker.RemoveChild(old);old.QueueFree();
        }
        if(!sleeping){field.SetValue(node.Entity.Monster,null);return;}
        if(current is not null&&GodotObject.IsInstanceValid(current)&&ReferenceEquals(current.GetParent(),marker)&&!current.IsQueuedForDeletion())return;
        if(NSleepingVfx.Create(marker.GlobalPosition) is not {} fresh)return;
        marker.AddChild(fresh);fresh.Position=Vector2.Zero;
        field.SetValue(node.Entity.Monster,fresh);
    }

    internal sealed class CrabBackground
    {
        private readonly object _armPhase;
        private readonly Track?[] _tracks;
        private readonly Particle[] _particles;
        private static readonly System.Reflection.FieldInfo ArmPhase=AccessTools.Field(typeof(NKaiserCrabBossBackground),"_rightArmState");
        private CrabBackground(NKaiserCrabBossBackground background)
        {
            _armPhase=ArmPhase.GetValue(background)!;
            var animation=Get<MegaSprite>(background,"_animController")?.GetAnimationState();
            _tracks=Enumerable.Range(0,3).Select(index=>Track.Capture(animation,index)).ToArray();
            _particles=CaptureParticles(background);
        }
        private static NKaiserCrabBossBackground? FindBackground(NCombatRoom room)=>room.Background?.GetNodeOrNull<NKaiserCrabBossBackground>("%KaiserCrab");
        internal static CrabBackground? Capture(NCombatRoom room)=>FindBackground(room) is {} background?new(background):null;
        internal void Restore(NCombatRoom room)
        {
            if(FindBackground(room) is not {} background)return;
            ArmPhase.SetValue(background,_armPhase);
            var animation=Get<MegaSprite>(background,"_animController")?.GetAnimationState();
            for(var index=0;index<_tracks.Length;index++)Track.Restore(animation,index,_tracks[index]);
            Track.Restore(animation,3,null);
            RestoreParticles(background,_particles);
        }
    }

    private sealed record Track(string Name,bool Loop)
    {
        internal static Track? Capture(MegaAnimationState? state,int index)
        {
            using var current=state?.GetCurrent(index);
            if(current is null)return null;
            var queue=state!.GetQueuedAnimationNames(index);
            if(queue.Count>1)return queue[^1] is "<empty>" or ""?null:new(queue[^1],true);
            var name=current.GetAnimationName();
            return name is "<empty>" or ""?null:new(name,current.IsLoop());
        }
        internal static void Restore(MegaAnimationState? state,int index,Track? saved)
        {
            if(state is null)return;
            using var before=state.GetCurrent(index);
            if(saved is null)
            {
                if(before is not null&&state.BoundObject.HasMethod("clear_track"))state.BoundObject.Call("clear_track",index).Dispose();
                return;
            }
            if(saved.Loop&&before is not null&&before.IsLoop()&&before.GetAnimationName()==saved.Name&&state.GetQueuedAnimationNames(index).Count==1)return;
            state.SetAnimation(saved.Name,saved.Loop,index);
            using var restored=state.GetCurrent(index);
            if(restored is null)return;
            restored.SetMixDuration(0f);
            if(saved.Loop)return;
            var end=restored.GetAnimationDuration();
            restored.SetTrackTime(end);
            if(restored.BoundObject.HasMethod("set_animation_last"))restored.BoundObject.Call("set_animation_last",end).Dispose();
        }
    }

    private static Layout GetLayout(Node root)=>Layouts.GetValue(root,owner=>
    {
        static IEnumerable<Node> Walk(Node node)
        {yield return node;foreach(var child in node.GetChildren())foreach(var item in Walk(child))yield return item;}
        var nodes=Walk(owner).ToArray();
        var particles=nodes.Where(node=>node is GpuParticles2D or CpuParticles2D).Cast<CanvasItem>().ToArray();
        var transient=nodes.SelectMany(node=>TransientEmitters.Where(rule=>rule.Script==node.GetType())
            .SelectMany(rule=>rule.Fields.Select(field=>Get<CanvasItem>(node,field)).OfType<CanvasItem>())).ToHashSet();
        return new(nodes,particles,transient);
    });
    private static T? Find<T>(Node root) where T:Node=>GetLayout(root).Nodes.OfType<T>().FirstOrDefault();
    private static T? Get<T>(object owner,string field) where T:class=>AccessTools.Field(owner.GetType(),field).GetValue(owner) as T;
    private static void Invoke(object owner,string method)=>AccessTools.DeclaredMethod(owner.GetType(),method).Invoke(owner,null);
    private static void Kill(Tween? tween){if(tween is not null&&GodotObject.IsInstanceValid(tween)&&tween.IsRunning())tween.Kill();}
    private static Particle[] CaptureParticles(Node root)
    {
        var layout=GetLayout(root);
        return layout.Particles.Where(node=>GodotObject.IsInstanceValid(node)&&root.IsAncestorOf(node))
            .Select(node=>new Particle(root.GetPathTo(node),node.Visible,!layout.Transient.Contains(node)&&node switch
        {
            GpuParticles2D gpu=>!gpu.OneShot&&gpu.Emitting,
            CpuParticles2D cpu=>!cpu.OneShot&&cpu.Emitting,
            _=>false
        })).ToArray();
    }
    private static void RestoreParticles(Node root,Particle[] points,bool freshScene=false)
    {
        foreach(var point in points)
        {
            switch(root.GetNodeOrNull(point.Path))
            {
                case GpuParticles2D gpu:
                    // A fresh emitter has no old particles to clear. Restart would
                    // issue a new burst even for an emitter the checkpoint wants off.
                    if(!point.Emitting&&!freshScene)gpu.Restart();
                    if(gpu.Emitting!=point.Emitting)gpu.Emitting=point.Emitting;
                    gpu.Visible=point.Visible;break;
                case CpuParticles2D cpu:
                    if(!point.Emitting&&!freshScene)cpu.Restart();
                    if(cpu.Emitting!=point.Emitting)cpu.Emitting=point.Emitting;
                    cpu.Visible=point.Visible;break;
            }
        }
        foreach(var trail in GetLayout(root).Nodes.OfType<NBasicTrail>().Where(GodotObject.IsInstanceValid))trail.ClearPoints();
    }

    private sealed record InitialScale(Vector2 Value);
    private static readonly ConditionalWeakTable<Node,InitialScale> RingScales=new();
    private static readonly Dictionary<Node,float> CoralTargets=[];
    private static readonly Dictionary<Node2D,float> VantomTargets=[];
    private static void SetCoralTarget(Node node,float target)=>CoralTargets[node]=target;
    private static void SetVantomTarget(Node2D node,float target)=>VantomTargets[node]=target;
    internal static void Clear(){SceneTweens.Clear();CoralTargets.Clear();VantomTargets.Clear();}
    internal static void RetireTweens()
    {
        foreach(var list in SceneTweens.Values)foreach(var tween in list)Kill(tween);
        SceneTweens.Clear();
        foreach(var node in CoralTargets.Keys.Where(node=>!GodotObject.IsInstanceValid(node)||!node.IsInsideTree()).ToArray())CoralTargets.Remove(node);
        foreach(var node in VantomTargets.Keys.Where(node=>!GodotObject.IsInstanceValid(node)||!node.IsInsideTree()).ToArray())VantomTargets.Remove(node);
    }

    internal static void Install(Harmony harmony)
    {
        void Patch(Type type,string method,string handler,bool after=false)
        {
            var hook=new HarmonyMethod(typeof(EnemyScenePresentation),handler);
            harmony.Patch(AccessTools.DeclaredMethod(type,method),prefix:after?null:hook,postfix:after?hook:null);
        }
        foreach(var type in new[]{typeof(NMechaKnightVfx),typeof(NFlyconidSporesVfx),typeof(NEntomancerVfx),typeof(NVantomVfx),typeof(NMyteVfx),
            typeof(NSnappingJaxfruitVfx),typeof(NKaiserCrabBossVfx),typeof(NTestSubjectVfx),typeof(NSoulFyshVfx),
            typeof(NSewerClamVfx),typeof(NCubexConstructVfx),typeof(NDecimillipedeSegmentVfx),typeof(NKinFollowerVfx),
            typeof(NAeonGlassVfx),typeof(NWaterfallGiantVfx),typeof(NLivingGasVfx),typeof(NGasBombVfx),typeof(NKnowledgeDemonVfx),
            typeof(NParafrightVfx),typeof(NTheObscuraVfx),typeof(NSoulNexusVfx)})
            Patch(type,"OnAnimationEvent",nameof(AllowCue));
        foreach(var (type,method) in new (Type,string)[]{(typeof(NVantomVfx),"DissolveTail"),(typeof(NSewerClamVfx),"ScaleCoralTo"),
            (typeof(NSoulFyshVfx),"StartSoundwave"),(typeof(NSoulFyshVfx),"EndSoundwave"),(typeof(NSoulFyshVfx),"StartBeckon"),(typeof(NSoulFyshVfx),"EndBeckon"),
            (typeof(NCubexConstructVfx),"StartLaser"),(typeof(NLaserVfx),nameof(NLaserVfx.ExtendLaser)),(typeof(NLaserVfx),nameof(NLaserVfx.RetractLaser))})
            harmony.Patch(AccessTools.DeclaredMethod(type,method),transpiler:new HarmonyMethod(typeof(EnemyScenePresentation),nameof(OwnTweens)));
        Patch(typeof(NSewerClamVfx),"ScaleCoralTo",nameof(CoralTarget));
        Patch(typeof(Vantom),nameof(Vantom.ScaleTo),nameof(VantomTarget),after:true);
        Patch(typeof(NCubexConstructVfx),"StartLaser",nameof(CubexRings));
    }
    private static bool AllowCue()=>!NativeSeek.RetiringPresentation;
    private static void CoralTarget(NSewerClamVfx __instance,float __0){if(NativeSeek.Enabled)SetCoralTarget(__instance,__0);}
    private static void VantomTarget(Vantom __instance,float __0)
    {
        if(!NativeSeek.Enabled||__instance.Creature.GetCreatureNode()?.GetSpecialNode<Node2D>("Visuals/ScalingBone") is not {} scale)return;
        SetVantomTarget(scale,__0);
        if(AccessTools.Property(typeof(Vantom),"ScaleTween").GetValue(__instance) is Tween tween)RememberTween(scale,tween);
    }
    private static void CubexRings(NCubexConstructVfx __instance)
    {if(NativeSeek.Enabled&&Get<Node2D>(__instance,"_rings") is {} rings)RingScales.GetValue(__instance,_=>new(rings.Scale));}
    private static IEnumerable<CodeInstruction> OwnTweens(IEnumerable<CodeInstruction> source)
    {
        var original=AccessTools.Method(typeof(Node),nameof(Node.CreateTween));
        var replacement=AccessTools.DeclaredMethod(typeof(EnemyScenePresentation),nameof(CreateSceneTween));
        var instructions=source.ToArray();
        Codec.Require(instructions.Any(instruction=>instruction.Calls(original)),"enemy-scene-tween-call-mismatch");
        return instructions.MethodReplacer(original,replacement);
    }
    private static Tween CreateSceneTween(Node owner)
    {
        var tween=owner.CreateTween();
        if(NativeSeek.Enabled&&NCombatRoom.Instance is {} room&&room.IsAncestorOf(owner))RememberTween(owner,tween);
        return tween;
    }
    private static void RememberTween(Node owner,Tween tween)
    {
        foreach(var dead in SceneTweens.Keys.Where(node=>!GodotObject.IsInstanceValid(node)||!node.IsInsideTree()).ToArray())SceneTweens.Remove(dead);
        if(!SceneTweens.TryGetValue(owner,out var list))SceneTweens[owner]=list=[];
        list.RemoveAll(item=>!GodotObject.IsInstanceValid(item)||!item.IsRunning());
        list.Add(tween);
    }
}
