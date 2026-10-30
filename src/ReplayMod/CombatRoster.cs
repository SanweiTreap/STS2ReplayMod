using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace ReplayMod;

// Playback-only anchors. A removed enemy is restored as the SAME combat object, never spawned again.
internal sealed class CombatRoster
{
    internal readonly Creature[] Initial;
    private readonly Dictionary<Creature,NCreature> _knownNodes;
    private readonly Dictionary<Creature,(NCreatureVisuals Visuals,Node2D Body)> _knownBodies;
    private readonly Dictionary<Creature,uint?> _ids;
    private readonly uint _nextId;
    internal IEnumerable<Creature> Anchors=>_knownNodes.Keys;
    internal CombatRoster(CombatState state)
    {
        Initial=state.Creatures.ToArray();
        _nextId=(uint)AccessTools.Field(typeof(CombatState),"_nextCreatureId").GetValue(state)!;
        _ids=Initial.ToDictionary(c=>c,c=>c.CombatId);
        _knownNodes=Initial.ToDictionary(c=>c,c=>NCombatRoom.Instance!.GetCreatureNode(c)??throw new InvalidDataException("direct-roster-node-missing"));
        _knownBodies=_knownNodes.ToDictionary(pair=>pair.Key,pair=>(pair.Value.Visuals,pair.Value.Body));
        Codec.Require(Reason(state) is null,"direct-roster-initial-unsupported");
        Runtime.Log("playback-roster",new{Creatures=Summary(Initial),SameObjectsRequired=true});
    }
    internal string? Reason(CombatState state)
    {
        var current=state.Creatures.ToArray();
        var added=current.Except(Initial).ToArray();
        if(added.Any(c=>!CombatUnits.IsOsty(c))||added.Length>1||current.Count(CombatUnits.IsOsty)>1||
            added.Length>0&&Initial.Any(CombatUnits.IsOsty))return "不支持的单位生成或宠物类型";
        if((uint)AccessTools.Field(typeof(CombatState),"_nextCreatureId").GetValue(state)! !=_nextId+(uint)added.Length||
            added.Any(c=>c.CombatId!=_nextId))return "单位分配计数与身份不一致";
        if(!current.Where(c=>c.IsEnemy).SequenceEqual(Initial.Where(c=>c.IsEnemy&&current.Contains(c)))||
            !state.Allies.Where(c=>c.IsPlayer).SequenceEqual(Initial.Where(c=>c.IsPlayer)))return "生成或成员重排暂不能后退";
        if(current.Any(c=>c.PetOwner is not null&&!CombatUnits.IsOsty(c))||current.Any(c=>!CombatUnits.Restorable(c))||
            Initial.Where(c=>c.IsPlayer).Any(c=>!current.Contains(c)))return "玩家死亡、其他宠物或未完成死亡边界暂不能后退";
        foreach(var creature in added.Where(c=>!_knownNodes.ContainsKey(c)))
        {
            var node=NCombatRoom.Instance?.GetCreatureNode(creature);if(node is null)return "新Osty场景尚未就绪";
            _ids.Add(creature,creature.CombatId);_knownNodes.Add(creature,node);_knownBodies.Add(creature,(node.Visuals,node.Body));
        }
        if(current.Any(c=>c.CombatId!=_ids[c])||Initial.Except(current).Any(c=>c.IsAlive))return "敌人身份或成员关系变化暂不能后退";
        foreach(var creature in current)
        {
            // History can show the native death tail while its real data still describes the
            // source checkpoint. Only explicitly owned display deaths may leave the UI roster.
            if(NativeDisplayTrace.Replaying&&NativeDisplayTrace.OwnsDeathDisplay(creature))continue;
            var node=NCombatRoom.Instance?.GetCreatureNode(creature);
            if(node is null||!GodotObject.IsInstanceValid(node)||node.IsQueuedForDeletion()||!ReferenceEquals(node,_knownNodes[creature]))return "形态或模型节点变化暂不能后退";
            if(!ReferenceEquals(node.Visuals,_knownBodies[creature].Visuals)||!ReferenceEquals(node.Body,_knownBodies[creature].Body))return "形态或模型节点变化暂不能后退";
        }
        return null;
    }
    internal sealed record RemovedDisplays(int Creatures,int Fades,int ContinuedDeaths=0);
    internal RemovedDisplays RemoveEnemyDisplays()=>RemoveDisplays(Initial.Where(c=>c.IsEnemy).ToHashSet());
    internal RemovedDisplays RemoveForForwardTarget(IEnumerable<Creature> target)=>RemoveDisplays(Initial.Where(c=>c.IsEnemy&&!target.Contains(c)).ToHashSet());
    internal RemovedDisplays RemoveRetiredEnemyDisplays(CombatState state)=>RemoveDisplays(Initial.Where(c=>c.IsEnemy&&!state.ContainsCreature(c)).ToHashSet());
    internal RemovedDisplays RemovePetDisplays()=>RemoveDisplays(Anchors.Where(CombatUnits.IsOsty).ToHashSet());
    private RemovedDisplays RemoveDisplays(HashSet<Creature> retired)
    {
        var creatureCount=0;var fadeCount=0;var continued=0;
        var room=NCombatRoom.Instance!;
        var active=(List<NCreature>)AccessTools.Field(typeof(NCombatRoom),"_creatureNodes").GetValue(room)!;
        var removing=(List<NCreature>)AccessTools.Field(typeof(NCombatRoom),"_removingCreatureNodes").GetValue(room)!;
        IEnumerable<Node> Nodes(Node root)
        {
            yield return root;foreach(var child in root.GetChildren())foreach(var node in Nodes(child))yield return node;
        }
        // A fade may already have reparented an old body into its viewport. Remove only fades tied
        // to this original roster; leaving them alive would show a second corpse over the revived enemy.
        var visuals=_knownBodies.Where(pair=>retired.Contains(pair.Key)).Select(pair=>pair.Value.Visuals).ToHashSet(ReferenceEqualityComparer.Instance);
        foreach(var doom in Nodes(room).OfType<NDoomVfx>().ToArray())
            if(AccessTools.Field(typeof(NDoomVfx),"_creatureVisuals").GetValue(doom) is NCreatureVisuals owner&&visuals.Contains(owner))
            {doom.Visible=false;doom.GetParent()?.RemoveChild(doom);doom.QueueFree();fadeCount++;}
        foreach(var fade in Nodes(room).OfType<NMonsterDeathVfx>().ToArray())
        {
            var owners=(List<NCreature>)AccessTools.Field(typeof(NMonsterDeathVfx),"_creatureNodes").GetValue(fade)!;
            if(NativeDisplayTrace.Replaying&&owners.All(n=>NativeDisplayTrace.OwnsDeathDisplay(n.Entity)))continue;
            if(owners.Any(n=>retired.Contains(n.Entity)))
            {fade.Visible=false;fade.GetParent()?.RemoveChild(fade);fade.QueueFree();fadeCount++;}
        }
        foreach(var node in active.Concat(removing).Distinct().Where(n=>retired.Contains(n.Entity)).ToArray())
        {
            // _ExitTree unsubscribes current powers BEFORE the logical graph is restored. Cancellation and
            // native removal continuations refer to this old node only; they cannot remove its replacement.
            active.Remove(node);removing.Remove(node);
            if(!GodotObject.IsInstanceValid(node)||node.IsQueuedForDeletion()){creatureCount++;continue;}
            if(NativeDisplayTrace.Replaying&&NativeDisplayTrace.OwnsDeathDisplay(node.Entity)){continued++;continue;}
            node.DeathAnimCancelToken.Cancel();
            node.HideHoverTips();node.GetParent()?.RemoveChild(node);node.QueueFree();creatureCount++;
        }
        return new(creatureCount,fadeCount,continued);
    }
    internal void AddEnemyDisplays(CombatState state,bool onlyMissing=false)
    {
        var room=NCombatRoom.Instance!;
        foreach(var creature in state.Enemies)
        {
            if(onlyMissing&&room.GetCreatureNode(creature) is {} kept&&GodotObject.IsInstanceValid(kept)&&!kept.IsQueuedForDeletion())continue;
            room.AddCreature(creature);var node=room.GetCreatureNode(creature)??throw new InvalidDataException("direct-roster-recreated-node-missing");
            _knownNodes[creature]=node;_knownBodies[creature]=(node.Visuals,node.Body);
        }
        // Native AddCreature above is a scene-only operation. Do not call CombatManager.AddCreature:
        // that would reset the move state machine, reroll AI, and reapply starting powers.
    }
    internal void AddPetDisplays(CombatState state)
    {
        var room=NCombatRoom.Instance!;
        foreach(var creature in state.Allies.Where(CombatUnits.IsOsty))
        {
            room.AddCreature(creature);var node=room.GetCreatureNode(creature)??throw new InvalidDataException("restored-osty-node-missing");
            node.TrackBlockStatus(creature.PetOwner!.Creature);
            _knownNodes[creature]=node;_knownBodies[creature]=(node.Visuals,node.Body);
        }
    }
    internal static object[] Summary(IEnumerable<Creature> creatures)=>creatures.Select((c,index)=>(object)new{
        Index=index,Model=c.ModelId.ToString(),c.CombatId,c.CurrentHp,c.IsAlive,Attached=c.CombatState is not null,
        Move=c.IsAlive&&c.Monster is not null?c.Monster.NextMove?.Id:null,
        Ravenous=c.Monster is CorpseSlug slug?(bool?)slug.IsRavenous:null,
        Powers=c.Powers.Select(p=>new{Model=p.Id.ToString(),p.Amount}).ToArray()}).ToArray();
}
