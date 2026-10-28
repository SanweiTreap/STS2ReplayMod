using System.Diagnostics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ReplayMod;

// Liquid shader display has its own ownership. No replay calls PotionModel.OnUse or the native async
// TryApplyLiquidOverlay, whose delayed material restoration would otherwise outlive a history restore.
internal static class PotionVisuals
{
    internal sealed record Sample(Creature Creature,Color Tint,float Hue,float Influence);
    private sealed class Lease(NCreatureVisuals visuals,Material? original,ShaderMaterial material)
    {
        internal readonly NCreatureVisuals Visuals=visuals;
        internal readonly Material? Original=original;
        internal readonly ShaderMaterial Material=material;
        internal float Influence;
        internal long? Tail;
    }
    private static readonly Dictionary<Creature,Lease> Leases=[];
    private static object? Get(object value,string field)=>AccessTools.Field(value.GetType(),field).GetValue(value);
    internal static Sample[] Capture()=>NCombatRoom.Instance?.CreatureNodes.Select(n=>
        Get(n.Visuals,"_currentLiquidOverlayMaterial") is ShaderMaterial material?
            new Sample(n.Entity,material.GetShaderParameter("tint").AsColor(),material.GetShaderParameter("h").AsSingle(),material.GetShaderParameter("overlay_influence").AsSingle()):null)
        .OfType<Sample>().ToArray()??[];
    internal static async Task RetireNative()
    {
        if(NCombatRoom.Instance is not {} room)return;
        var active=room.CreatureNodes.Select(n=>n.Visuals).Where(v=>Get(v,"_currentLiquidOverlayMaterial") is not null).ToArray();
        foreach(var visuals in active)AccessTools.Field(typeof(NCreatureVisuals),"_liquidOverlayTimer").SetValue(visuals,0d);
        for(var i=0;i<4&&active.Any(v=>Get(v,"_currentLiquidOverlayMaterial") is not null);i++)await Runtime.NextFrame();
        Codec.Require(active.All(v=>Get(v,"_currentLiquidOverlayMaterial") is null),"native-potion-overlay-did-not-retire");
        StopOwned();
    }
    internal static void Apply(IReadOnlyList<Sample> samples)
    {
        var active=samples.Select(s=>s.Creature).ToHashSet();
        foreach(var creature in Leases.Keys.Where(c=>!active.Contains(c)).ToArray())Release(creature);
        foreach(var sample in samples)
        {
            var visuals=NCombatRoom.Instance?.GetCreatureNode(sample.Creature)?.Visuals;
            if(visuals?.SpineBody is null)continue;
            if(!Leases.TryGetValue(sample.Creature,out var lease)||!ReferenceEquals(visuals,lease.Visuals))
            {
                Release(sample.Creature);
                var material=(ShaderMaterial)PreloadManager.Cache.GetMaterial("res://materials/vfx/potion/potion_liquid_overlay.tres").Duplicate();
                lease=new(visuals,visuals.SpineBody.GetNormalMaterial(),material);Leases[sample.Creature]=lease;
                visuals.SpineBody.SetNormalMaterial(material);
            }
            lease.Tail=null;lease.Influence=sample.Influence;
            lease.Material.SetShaderParameter("tint",sample.Tint);lease.Material.SetShaderParameter("h",sample.Hue);
            lease.Material.SetShaderParameter("overlay_influence",sample.Influence);
        }
    }
    internal static void ContinueTails(){foreach(var lease in Leases.Values)lease.Tail=Stopwatch.GetTimestamp();}
    internal static void Tick()
    {
        foreach(var (creature,lease) in Leases.ToArray())if(lease.Tail is long start)
        {
            if(!GodotObject.IsInstanceValid(lease.Visuals)){Leases.Remove(creature);continue;}
            var influence=lease.Influence-(float)Stopwatch.GetElapsedTime(start).TotalSeconds;
            if(influence<=0)Release(creature);else lease.Material.SetShaderParameter("overlay_influence",influence);
        }
    }
    private static void Release(Creature creature)
    {
        if(!Leases.Remove(creature,out var lease))return;
        if(GodotObject.IsInstanceValid(lease.Visuals)&&lease.Visuals.SpineBody is {} body&&body.GetNormalMaterial()==lease.Material)
            body.SetNormalMaterial(lease.Original!); // Restore the exact saved native base, including its default/null material.
        lease.Material.Dispose();
    }
    internal static void StopOwned(){foreach(var creature in Leases.Keys.ToArray())Release(creature);}
}
