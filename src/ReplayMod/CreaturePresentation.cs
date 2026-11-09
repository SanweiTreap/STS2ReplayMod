using System.Collections;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;

namespace ReplayMod;

// Presentation values are bound to the original creature identity, not to a disposable enemy scene.
internal sealed class CreaturePresentation
{
    private readonly Creature _creature;
    private NCreature _node;
    private NCreatureVisuals _visuals;
    private Node2D _body;
    private readonly float _defaultScale,_temporaryScale,_hue;
    private readonly Vector2 _scale,_position;
    private readonly string? _animation;
    private readonly bool _wasDead;
    private readonly bool _interactable;
    private readonly bool? _spawnVisible;
    private readonly EnemyScenePresentation? _enemyScene;
    private readonly Texture2D? _segmentPhobiaTexture;
    private readonly (int Respawns,bool Dead,Color Tint)? _testSubject;
    private CreatureAnimator? _restoreAnimator;
    private AnimState? _restoreLoop;
    private bool _continuedAnimation,_rebuiltEnemy;
    private static readonly System.Reflection.FieldInfo TemporaryScale=AccessTools.Field(typeof(NCreature),"_tempScale");
    private static readonly System.Reflection.FieldInfo Animator=AccessTools.Field(typeof(NCreature),"_spineAnimator");
    private static readonly System.Reflection.FieldInfo Hue=AccessTools.Field(typeof(NCreatureVisuals),"_hue");
    private static readonly System.Reflection.FieldInfo ShakeTween=AccessTools.Field(typeof(NCreature),"_shakeTween");

    internal static void RetainedEnemyReviveStarted(NCreature node)
    {
        if(!NativeSeek.Enabled||!node.Entity.IsAlive||node.DeathAnimationTask is not {} previous)return;
        var subject=node.Entity.Monster is TestSubject&&node.Entity.HasPower<AdaptablePower>();
        var segment=CombatUnits.IsRetainedSegment(node.Entity);
        if(!subject&&!segment&&!CombatUnits.IsRetainedIllusion(node.Entity))return;
        // These retained enemies' AnimDie(false) has no removal work after
        // its wait. An earlier knockout must not gate the next native knockout.
        node.DeathAnimationTask=null;
        Runtime.Log(subject?"test-subject-knockout-animation-released":segment?"decimillipede-knockout-animation-released":"illusion-knockout-animation-released",
            new{node.Entity.CombatId,PreviousStillRunning=!previous.IsCompleted});
    }
    internal static void TestSubjectDeathSound(MonsterModel? __0)
    {
        if(NativeSeek.Enabled&&__0 is TestSubject subject)
            Runtime.Log("test-subject-native-death-sound",new{Sound=subject.DeathSfx,Retiring=NativePhaseRetirement.Active});
    }

    internal CreaturePresentation(Creature creature)
    {
        _creature=creature;
        _wasDead=creature.IsDead;
        _node=NCombatRoom.Instance?.GetCreatureNode(creature)??throw new InvalidDataException("checkpoint-creature-visual-missing");
        _interactable=_node.IsInteractable;
        if(NativeSpawnPresentation.Reveals(creature))_spawnVisible=_node.Visible||NativeSpawnPresentation.WillReveal(creature);
        _visuals=_node.Visuals;_body=_node.Body;_defaultScale=_visuals.DefaultScale;_temporaryScale=(float)TemporaryScale.GetValue(_node)!;
        if(NativeSeek.Enabled&&creature.IsEnemy)_enemyScene=new(_node);
        _position=_node.Position;_hue=(float)Hue.GetValue(_visuals)!;
        if(creature.Monster is DecimillipedeSegment&&_visuals.IsUsingPhobiaModeBody&&_body is Sprite2D sprite)
            _segmentPhobiaTexture=sprite.Texture;
        if(creature.Monster is TestSubject subject)
            _testSubject=(TestSubjectRespawns(subject),creature.IsDead,_node.GetSpecialNode<CanvasGroup>("%CanvasGroup")!.SelfModulate);
        if(Animator.GetValue(_node) is CreatureAnimator animator)
        {
            var state=(AnimState)AccessTools.Field(typeof(CreatureAnimator),"_currentState").GetValue(animator)!;
            // Gameplay can finish during hurt/attack/devour_start. Capture its stable loop destination.
            for(var i=0;i<16&&!state.IsLooping&&state.GetNextState() is {} next;i++)state=next;
            Codec.Require(state.IsLooping,"checkpoint-no-stable-creature-animation");_animation=state.Id;_restoreAnimator=animator;_restoreLoop=state;
        }
        var tween=AccessTools.Field(typeof(NCreature),"_scaleTween").GetValue(_node) as Tween;
        // ScaleTo returns a completed gameplay task while its visual tween may still be running.
        // Store its stable destination rather than one intermediate rendered size.
        _scale=tween is not null&&GodotObject.IsInstanceValid(tween)&&tween.IsRunning()?
            Vector2.One*_defaultScale*_temporaryScale:_visuals.Scale;
        if(creature.Monster is Osty)
        {
            var size=Mathf.Lerp(Osty.ScaleRange.X,Osty.ScaleRange.Y,Mathf.Clamp((creature.IsAlive?creature.MaxHp:0)/150f,0,1));
            _scale=Vector2.One*size*_defaultScale;
            if(!NCombatRoom.Instance!.SceneContainer.IsAncestorOf(_visuals))_scale*=creature.CombatState!.Encounter!.GetCameraScaling();
            _position=NCombatRoom.Instance.GetCreatureNode(creature.PetOwner!.Creature)!.Position+NCreature.GetOstyOffsetFromPlayer(creature);
        }
    }
    internal bool SameNodes=>GodotObject.IsInstanceValid(_node)&&!_node.IsQueuedForDeletion()&&
        ReferenceEquals(NCombatRoom.Instance?.GetCreatureNode(_creature),_node)&&ReferenceEquals(_node.Visuals,_visuals)&&ReferenceEquals(_node.Body,_body);
    private static int TestSubjectRespawns(TestSubject subject)=>(int)AccessTools.Field(typeof(TestSubject),"_respawns").GetValue(subject)!;
    internal bool RequiresEnemyDisplayRebuild=>(_creature.Monster is DecimillipedeSegment||CombatUnits.IsRetainedIllusion(_creature))&&_wasDead!=_creature.IsDead||
        _testSubject is {} phase&&_creature.Monster is TestSubject subject&&
        (phase.Dead!=_creature.IsDead||phase.Respawns!=TestSubjectRespawns(subject));
    internal void BindEnemyDisplay()
    {
        Codec.Require(!_creature.IsPlayer,"direct-back-cannot-rebind-player");
        var restored=NCombatRoom.Instance!.GetCreatureNode(_creature)??throw new InvalidDataException("direct-back-restored-enemy-node-missing");
        _rebuiltEnemy=!ReferenceEquals(_node,restored);
        _node=restored;
        _visuals=_node.Visuals;_body=_node.Body;
    }
    internal void Restore(bool continueAnimation=false)
    {
        Codec.Require(SameNodes,"direct-back-creature-visual-replaced");
        var rebuiltEnemy=NativeSeek.Restoring&&_creature.IsEnemy&&_rebuiltEnemy;
        if(_spawnVisible is {} visible)_node.Visible=visible;
        var retiredShake=NativeSeek.Restoring&&StopShake(_node);
        if(retiredShake)_visuals.Position=Vector2.Zero;
        var retiredHurt=NativeSeek.Restoring&&_animation is not null&&_visuals.IsPlayingHurtAnimation();
        if(AccessTools.Field(typeof(NCreature),"_scaleTween").GetValue(_node) is Tween tween&&GodotObject.IsInstanceValid(tween))tween.Kill();
        _node.Position=_position;
        _visuals.SetScaleAndHue(_defaultScale,_hue);TemporaryScale.SetValue(_node,_temporaryScale);
        AccessTools.Method(typeof(NCreature),"DoScaleTween").Invoke(_node,[_scale]);
        if(_animation is not null&&Animator.GetValue(_node) is CreatureAnimator animator)
        {
            var oldState=(AnimState)AccessTools.Field(typeof(CreatureAnimator),"_currentState").GetValue(animator)!;
            var retiredTransition=NativeSeek.Restoring&&
                (!oldState.IsLooping&&oldState.Id!=_animation||_creature.Monster is DevotedSculptor);
            var state=ReferenceEquals(animator,_restoreAnimator)?_restoreLoop:null;
            state??=AnimationStates(animator).FirstOrDefault(s=>s.Id==_animation&&s.IsLooping)
                ??throw new InvalidDataException("direct-back-animation-state-missing");
            _restoreAnimator=animator;_restoreLoop=state;
            _continuedAnimation=!rebuiltEnemy&&continueAnimation&&Destination(animator)==_animation;
            if(rebuiltEnemy)EnemyScenePresentation.ClearStartupTrack(_node);
            if(!_continuedAnimation)AccessTools.Method(typeof(CreatureAnimator),"SetNextState").Invoke(animator,[state]);
            if(rebuiltEnemy||retiredHurt||retiredTransition)
            {
                // No old action or fresh scene's default pose should blend into the restored loop.
                using var track=_node.SpineAnimation.GetCurrentTrack();
                track?.SetMixDuration(0);
            }
        }
        if(retiredShake||retiredHurt)Runtime.Log("native-creature-impact-motion-retired",new{_creature.CombatId,Model=_creature.ModelId.ToString(),
            LocalShake=retiredShake,HurtBlend=retiredHurt});
        var display=(Control)AccessTools.Field(typeof(NCreature),"_stateDisplay").GetValue(_node)!;
        if(AccessTools.Field(display.GetType(),"_showHideTween").GetValue(display) is Tween entrance&&GodotObject.IsInstanceValid(entrance))entrance.Kill();
        display.Position=(Vector2)AccessTools.Field(display.GetType(),"_originalPosition").GetValue(display)!;
        display.Modulate=Colors.White;display.Visible=true;
        if(NativeSeek.Restoring&&_creature.IsEnemy&&_creature.IsAlive)_node.ToggleIsInteractable(_interactable);
        if(CombatUnits.IsRetainedSegment(_creature))
        {
            _node.ToggleIsInteractable(!_creature.IsDead);
            AccessTools.Method(display.GetType(),_creature.IsDead?"OnCreatureDied":"OnCreatureRevived").Invoke(display,[_creature]);
            if(_segmentPhobiaTexture is not null&&_body is Sprite2D sprite)sprite.Texture=_segmentPhobiaTexture;
        }
        if(_creature.Monster is Osty||_testSubject.HasValue||CombatUnits.IsRetainedIllusion(_creature))
        {
            // Native death fades this existing display, while StartReviveAnim only
            // restores alpha. A node rebuilt while dead must therefore remain Visible;
            // ToggleIsInteractable(false) would leave it hidden after native revival.
            _node.ToggleIsInteractable(true);
            if(_creature.IsDead)
            {
                display.Modulate=new Color(1,1,1,0);
                _node.DisableInteractionForDeath();
                AccessTools.Method(display.GetType(),"OnCreatureDied").Invoke(display,[_creature]);
            }
            else if(CombatUnits.IsRetainedIllusion(_creature))
                AccessTools.Method(display.GetType(),"OnCreatureRevived").Invoke(display,[_creature]);
            if(_testSubject is {} subject)_node.GetSpecialNode<CanvasGroup>("%CanvasGroup")!.SelfModulate=subject.Tint;
            else Runtime.Log(_creature.Monster is Osty?"osty-hud-restored":"illusion-hud-restored",new{_creature.CombatId,Alive=_creature.IsAlive,display.Visible,
                Alpha=display.Modulate.A,NativeReviveCanFadeIn=true,ReviveAnimationRun=false});
        }
        AccessTools.Method(display.GetType(),"RefreshValues").Invoke(display,null);
        Node bounds=_testSubject is {Respawns:>0} phase?_visuals.GetNode<Control>($"RespawnBounds{phase.Respawns}"):
            NativeSeek.Restoring&&_restoreLoop?.BoundsContainer is {} path?_visuals.GetNode<Control>(path):_visuals;
        AccessTools.Method(typeof(NCreature),"UpdateBounds",[typeof(Node)]).Invoke(_node,[bounds]);
        if(_creature.Monster is ThievingHopper)RestoreStolenCardDisplay();
        _enemyScene?.Restore(_node,rebuiltEnemy);
        _rebuiltEnemy=false;
    }
    private void RestoreStolenCardDisplay()
    {
        if(_node.GetSpecialNode<Marker2D>("%StolenCardPos") is not {} marker)return;
        // SwipePower owns the theft; the marker only mirrors its local cards.
        var missing=_creature.GetPowerInstances<SwipePower>().Select(p=>p.StolenCard).OfType<CardModel>().Where(LocalContext.IsMine).ToHashSet();
        foreach(var node in marker.GetChildren().OfType<NCard>())
        {
            if(node.Model is {} model&&missing.Remove(model))continue;
            marker.RemoveChild(node);node.QueueFreeSafely();
        }
        foreach(var model in missing)
        {
            var card=NCard.Create(model)??throw new InvalidDataException("restored-stolen-card-display-missing");
            marker.AddChild(card);
            card.Position+=card.Size*.5f;
            card.UpdateVisuals(PileType.Deck,CardPreviewMode.Normal);
        }
    }
    internal static bool StopShake(NCreature node)
    {
        if(ShakeTween.GetValue(node) is not Tween shake||!GodotObject.IsInstanceValid(shake)||!shake.IsRunning())return false;
        shake.Kill();
        return true;
    }
    private static string Destination(CreatureAnimator animator)
    {
        var state=(AnimState)AccessTools.Field(typeof(CreatureAnimator),"_currentState").GetValue(animator)!;
        for(var i=0;i<16&&!state.IsLooping&&state.GetNextState() is {} next;i++)state=next;
        return state.Id;
    }
    internal static void VerifyNativeOstyRevive(NCreature node)
    {
        if(!NativeSeek.Enabled||node.Entity.Monster is not Osty)return;
        var display=(Control)AccessTools.Field(typeof(NCreature),"_stateDisplay").GetValue(node)!;
        Codec.Require(NCombatUi.IsDebugHidingHpBar||display.Visible,"osty-native-revive-health-display-hidden");
        Runtime.Log("osty-native-revive-hud",new{node.Entity.CombatId,Alive=node.Entity.IsAlive,
            display.Visible,Alpha=display.Modulate.A,NativeFadeOwnsAlpha=true,VisibilityChangedByObserver=false});
    }
    internal static string? CurrentState(NCreature node)=>Animator.GetValue(node) is CreatureAnimator animator?
        ((AnimState)AccessTools.Field(typeof(CreatureAnimator),"_currentState").GetValue(animator)!).Id:null;
    internal static void PlayRecorded(NCreature node,string trigger,string? animation)
    {
        if(animation is not null&&Animator.GetValue(node) is CreatureAnimator animator)
        {
            var state=AnimationStates(animator).FirstOrDefault(s=>s.Id==animation)
                ??throw new InvalidDataException("recorded-creature-animation-state-missing");
            AccessTools.Method(typeof(CreatureAnimator),"SetNextState").Invoke(animator,[state]);
        }
        else node.SetAnimationTrigger(trigger);
        Runtime.Log("native-creature-animation-replayed",new{Model=node.Entity.ModelId.ToString(),Trigger=trigger,Recorded=animation,Actual=CurrentState(node),BusinessConditionsReevaluated=false});
    }
    private static IEnumerable<AnimState> AnimationStates(CreatureAnimator animator)
    {
        var seen=new HashSet<object>(ReferenceEqualityComparer.Instance);var states=new List<AnimState>();
        void Visit(object? item)
        {
            if(item is null||item is string||item is Delegate||!seen.Add(item))return;
            if(item is AnimState state)
            {
                states.Add(state);foreach(var f in IdentityMemento.Fields(typeof(AnimState)))Visit(f.GetValue(state));
            }
            else if(item is IDictionary dict){foreach(var entry in dict.Values)Visit(entry);}
            else if(item is IEnumerable list){foreach(var entry in list)Visit(entry);}
            else if(item.GetType().DeclaringType==typeof(AnimState))foreach(var f in IdentityMemento.Fields(item.GetType()))Visit(f.GetValue(item));
        }
        Visit(AccessTools.Field(typeof(CreatureAnimator),"_currentState").GetValue(animator));
        Visit(AccessTools.Field(typeof(CreatureAnimator),"_anyState").GetValue(animator));return states;
    }
    internal bool Matched=>SameNodes&&_visuals.Scale.IsEqualApprox(_scale)&&Mathf.IsEqualApprox(_visuals.DefaultScale,_defaultScale)&&
        Mathf.IsEqualApprox((float)TemporaryScale.GetValue(_node)!,_temporaryScale)&&_node.Position.IsEqualApprox(_position)&&
        Mathf.IsEqualApprox((float)Hue.GetValue(_visuals)!,_hue)&&(_animation is null||_node.SpineAnimation.GetCurrentAnimationName()==_animation||
            _continuedAnimation&&Animator.GetValue(_node) is CreatureAnimator animator&&Destination(animator)==_animation);
    internal object Summary()=>new{Role=_creature.IsPlayer?"player":_creature.PetOwner is not null?"pet":"enemy",Model=_creature.ModelId.ToString(),_creature.CombatId,
        DefaultScale=_defaultScale,TemporaryScale=_temporaryScale,ScaleX=_scale.X,ScaleY=_scale.Y,PositionX=_position.X,PositionY=_position.Y,Hue=_hue,Animation=_animation};
}
