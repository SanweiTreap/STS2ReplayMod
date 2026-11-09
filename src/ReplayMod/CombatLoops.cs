using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ReplayMod;

internal static class CombatLoops
{
    private sealed record Playing(bool UsesLoopParam,Dictionary<string,float> Parameters);
    internal sealed record Instance(bool UsesLoopParam,KeyValuePair<string,float>[] Parameters);
    internal sealed record Voice(string Path,Instance[] Instances);
    internal sealed record Emitter(Creature Unit,KeyValuePair<string,(string Param,float StopValue)>[] Paths);
    internal sealed record Point(Emitter[] Emitters,Voice[] Voices);
    private static readonly System.Reflection.FieldInfo NodeLoops=AccessTools.Field(typeof(NCreature),"_sfxLoops");
    private static readonly HashSet<string> Reviewed=new(StringComparer.Ordinal)
    {
        "event:/sfx/enemy/enemy_attacks/cubex_construct/cubex_construct_charge_attack",
        "event:/sfx/enemy/enemy_attacks/owl_magistrate/owl_magistrate_fly_loop",
        "event:/sfx/enemy/enemy_attacks/kaiser_crab/kaiser_crab_right_buff",
        "event:/sfx/enemy/enemy_attacks/egg_layer/egg_layer_idle_loop",
        "event:/sfx/enemy/enemy_attacks/orb_plant/orb_plant_idle_loop",
        "event:/sfx/enemy/enemy_attacks/slumbering_beetle/slumbering_beetle_sleep_loop",
        "event:/sfx/enemy/enemy_attacks/thieving_hopper/thieving_hopper_hover_loop",
        "event:/sfx/enemy/enemy_attacks/waterfall_giant/waterfall_giant_ambient"
    };
    private static readonly Dictionary<string,List<Playing>> Active=new(StringComparer.Ordinal);
    private static CombatState? _combat;

    internal static void Install(Harmony harmony)
    {
        foreach(var (native,observer) in new[]{("PlayLoop",nameof(Started)),("StopLoop",nameof(Stopped)),("SetParam",nameof(Parameter)),("StopAllLoops",nameof(Clear))})
            harmony.Patch(AccessTools.DeclaredMethod(typeof(NAudioManager),native),
                postfix:new HarmonyMethod(typeof(CombatLoops),observer));
    }
    internal static void Clear(){_combat=null;Active.Clear();}
    private static void Bind(CombatState combat)
    {
        if(ReferenceEquals(_combat,combat))return;
        Clear();_combat=combat;
    }
    private static bool Observe(string path)
    {
        if(!NativeSeek.Enabled||!Reviewed.Contains(path)||CombatManager.Instance.DebugOnlyGetState() is not {} combat)return false;
        Bind(combat);return true;
    }
    private static void Started(string __0,bool __1)
    {
        if(!Observe(__0))return;
        if(!Active.TryGetValue(__0,out var voices))Active.Add(__0,voices=[]);
        voices.Add(new(__1,new(StringComparer.Ordinal)));
    }
    private static void Stopped(string __0)
    {
        if(!Observe(__0)||!Active.TryGetValue(__0,out var voices))return;
        voices.RemoveAt(0);
        if(voices.Count==0)Active.Remove(__0);
    }
    private static void Parameter(string __0,string __1,float __2)
    {
        // The damage callback pulses this cue on the ambient event. It is not a loop phase.
        if(__1!="enemy_hurt"&&Observe(__0)&&Active.TryGetValue(__0,out var voices))
            foreach(var voice in voices)voice.Parameters[__1]=__2;
    }

    private static Dictionary<string,(string Param,float StopValue)> Paths(NCreature node)=>
        (Dictionary<string,(string Param,float StopValue)>)NodeLoops.GetValue(node)!;

    internal static Point Capture(CombatState state,NCombatRoom room)
    {
        Bind(state);
        return new(state.Enemies.Select(unit=>new Emitter(unit,room.GetCreatureNode(unit) is {} node?
            Paths(node).Where(pair=>Reviewed.Contains(pair.Key)).ToArray():[])).ToArray(),
            Active.Select(pair=>new Voice(pair.Key,pair.Value.Select(voice=>new Instance(voice.UsesLoopParam,voice.Parameters.ToArray())).ToArray())).ToArray());
    }

    internal static void Restore(Point point,NCombatRoom room)
    {
        if(NAudioManager.Instance is not {} audio)return;
        var before=Active.Values.Sum(voices=>voices.Count);
        foreach(var emitter in point.Emitters)
        {
            if(room.GetCreatureNode(emitter.Unit) is not {} node)continue;
            var desired=emitter.Paths.ToDictionary();
            foreach(var (path,config) in Paths(node).Where(pair=>Reviewed.Contains(pair.Key)).ToArray())
                if(!desired.TryGetValue(path,out var saved)||saved!=config)node.StopSfxLoop(path);
            foreach(var (path,config) in emitter.Paths)
                if(!Paths(node).ContainsKey(path))node.StartSfxLoop(path,config.Param,config.StopValue);
        }

        var desiredPaths=point.Voices.Select(voice=>voice.Path).ToHashSet(StringComparer.Ordinal);
        foreach(var path in Active.Keys.Where(path=>!desiredPaths.Contains(path)).ToArray())StopPath(path);
        foreach(var voice in point.Voices)
        {
            if(Active.TryGetValue(voice.Path,out var playing))
            {
                if(playing.Count==voice.Instances.Length&&playing.Zip(voice.Instances).All(pair=>Matches(pair.First,pair.Second)))continue;
                if(playing.Count==1&&voice.Instances is [var target]&&playing[0].UsesLoopParam==target.UsesLoopParam&&
                    !playing[0].Parameters.Keys.Except(target.Parameters.Select(pair=>pair.Key)).Any())
                {
                    foreach(var (parameter,value) in target.Parameters)
                        if(!playing[0].Parameters.TryGetValue(parameter,out var previous)||previous!=value)audio.SetParam(voice.Path,parameter,value);
                    continue;
                }
                StopPath(voice.Path);
            }
            // Native SetParam broadcasts to existing instances. Apply each
            // instance's known parameters before starting the next instance.
            foreach(var instance in voice.Instances)
            {
                audio.PlayLoop(voice.Path,instance.UsesLoopParam);
                foreach(var (parameter,value) in instance.Parameters)audio.SetParam(voice.Path,parameter,value);
            }
        }
        if(before>0||point.Voices.Length>0)
            Runtime.Log("native-combat-loops-restored",new{Before=before,Target=point.Voices.Select(voice=>new{voice.Path,Count=voice.Instances.Length}).ToArray(),
                Active=Active.Values.Sum(voices=>voices.Count)});
        static bool Matches(Playing current,Instance target)=>current.UsesLoopParam==target.UsesLoopParam&&
            current.Parameters.Count==target.Parameters.Length&&target.Parameters.All(pair=>
                current.Parameters.TryGetValue(pair.Key,out var actual)&&actual==pair.Value);
        void StopPath(string path)
        {
            if(!Active.TryGetValue(path,out var playing))return;
            var count=playing.Count;
            for(var index=0;index<count;index++)audio.StopLoop(path);
        }
    }
}
