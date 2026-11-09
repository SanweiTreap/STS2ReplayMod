using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ReplayMod;

internal static class ContentPresentation
{
    private sealed record RelicActivation(RelicModel Relic,int Epoch);
    private static readonly AsyncLocal<RelicActivation?> ActivationOrigin=new();
    private static readonly Dictionary<Type,(FieldInfo Flag,MethodInfo Setter)> Activations=[];
    private static int _activationEpoch;
    internal static void Install(Harmony harmony)
    {
        HarmonyMethod Hook(string name)=>new(typeof(ContentPresentation),name);
        foreach(var type in typeof(RelicModel).Assembly.GetTypes().Where(type=>type.IsSubclassOf(typeof(RelicModel))))
        {
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly;
            var activation=type.GetMethod("DoActivateVisuals",flags,null,Type.EmptyTypes,null);
            var active=type.GetField("_isActivating",flags);
            var setter=type.GetProperty("IsActivating",flags)?.GetSetMethod(true);
            // Fixed native build: these tasks own a transient display flag and
            // a flash, while the calling hook owns the actual counter/effect.
            if(activation?.GetCustomAttribute<AsyncStateMachineAttribute>() is not {} machine||active?.FieldType!=typeof(bool)||setter is null)continue;
            Activations.Add(type,(active,setter));
            harmony.Patch(activation,prefix:Hook(nameof(BeginRelicActivation)),finalizer:Hook(nameof(EndRelicActivation)));
            harmony.Patch(AccessTools.DeclaredMethod(machine.StateMachineType,"MoveNext"),transpiler:Hook(nameof(BindRelicActivationWrites)));
        }
    }
    private static void BeginRelicActivation(RelicModel __instance,out (bool Entered,RelicActivation? Previous) __state)
    {
        __state=(true,ActivationOrigin.Value);
        if(NativeSeek.Enabled)ActivationOrigin.Value=new(__instance,_activationEpoch);
    }
    private static void EndRelicActivation((bool Entered,RelicActivation? Previous) __state)
    {if(__state.Entered)ActivationOrigin.Value=__state.Previous;}
    private static bool AllowRelicActivationWrite(RelicModel __instance)=>ActivationOrigin.Value is not {} origin||
        !ReferenceEquals(origin.Relic,__instance)||origin.Epoch==_activationEpoch;
    private static void WriteRelicActivation(RelicModel relic,bool active)
    {if(AllowRelicActivationWrite(relic))Activations[relic.GetType()].Setter.Invoke(relic,[active]);}
    private static IEnumerable<CodeInstruction> BindRelicActivationWrites(IEnumerable<CodeInstruction> source,MethodBase __originalMethod)
    {
        var setter=Activations[__originalMethod.DeclaringType!.DeclaringType!].Setter;
        var instructions=source.ToArray();
        Codec.Require(instructions.Count(i=>i.Calls(setter))==2,"relic-activation-display-write-sites");
        return instructions.MethodReplacer(setter,AccessTools.DeclaredMethod(typeof(ContentPresentation),nameof(WriteRelicActivation)));
    }
    internal static void SettleRestoredRelicActivations(IEnumerable<RelicModel> relics)
    {
        // A normal transition to the next room must still let its timers end.
        _activationEpoch++;
        var settled=new List<string>();
        foreach(var relic in relics)
        {
            if(!Activations.TryGetValue(relic.GetType(),out var activation)||activation.Flag.GetValue(relic) is not true)continue;
            // The memento restored the logical count and status. Only the
            // old one-second display override has no task in the new picture.
            activation.Flag.SetValue(relic,false);settled.Add(relic.Id.ToString());
        }
        if(settled.Count>0)Runtime.Log("native-restored-relic-counter-overrides-settled",new{NativeSeek.Generation,Relics=settled});
    }
    private static object? Get(object value,string field)=>AccessTools.Field(value.GetType(),field).GetValue(value);
    private static void Invoke(object value,string method,params object?[] args)=>AccessTools.Method(value.GetType(),method).Invoke(value,args);
    private static IEnumerable<Node> Nodes(Node root)
    {
        yield return root;foreach(var child in root.GetChildren())foreach(var node in Nodes(child))yield return node;
    }
    // Retire old holders BEFORE restoring item fields. Their use/remove tweens and delayed gray-out
    // callbacks retain only the old holder/potion, never the replacements created below.
    internal static void RetirePotions()
    {
        var container=NRun.Instance!.GlobalUi.TopBar.PotionContainer;
        var holders=(List<NPotionHolder>)Get(container,"_holders")!;
        foreach(var holder in holders.ToArray())
        {
            holder.CancelPotionUseOrDiscard();holder.GetParent().RemoveChild(holder);holder.QueueFree();
        }
        holders.Clear();AccessTools.Field(typeof(NPotionContainer),"_focusedHolder").SetValue(container,null);
    }
    internal static bool CanKeepPotions(IReadOnlyList<PotionModel?> expected)
    {
        var holders=(List<NPotionHolder>)Get(NRun.Instance!.GlobalUi.TopBar.PotionContainer,"_holders")!;
        return holders.Count==expected.Count&&holders.Select((h,i)=>ReferenceEquals(h.Potion?.Model,expected[i])&&
            !(bool)Get(h,"_disabledUntilPotionRemoved")!&&h.Potion?.Model.IsQueued!=true&&
            h.Potion?.Model.HasBeenRemovedFromState!=true&&h.Modulate.IsEqualApprox(Colors.White)&&
            Mathf.IsEqualApprox(((CanvasItem)Get(h,"_emptyIcon")!).Modulate.A,h.HasPotion?0:1)&&
            !h.GetChildren().OfType<NPotion>().Any(p=>!ReferenceEquals(p,h.Potion))).All(v=>v);
    }
    internal static bool SettleComputedPotions(IReadOnlyList<PotionModel?> expected)
    {
        if(CanKeepPotions(expected))return false;
        // The native slot is already empty logically, but may still own the
        // outgoing bottle and empty-slot fade. Retire that picture under cover.
        RetirePotions();
        Restore(refreshRelics:false);
        Codec.Require(CanKeepPotions(expected),"computed-turn-potion-display-mismatch");
        return true;
    }
    internal static void Restore(bool keptPotions=false,bool refreshRelics=true)
    {
        var player=LiveState.Player;var container=NRun.Instance!.GlobalUi.TopBar.PotionContainer;
        if(!keptPotions)
        {
            Invoke(container,"GrowPotionHolders",player.MaxPotionCount);
            foreach(var potion in player.Potions)Invoke(container,"Add",potion,true);
        }
        if(refreshRelics)RefreshRelics();
    }
    internal static void RefreshRelics()
    {
        foreach(var holder in Nodes(NRun.Instance!.GlobalUi.RelicInventory).OfType<NRelicInventoryHolder>())
        {
            Invoke(holder,"RefreshAmount");Invoke(holder,"RefreshStatus");
        }
    }
    internal static object CheckAndSummary()
    {
        var player=LiveState.Player;var container=NRun.Instance!.GlobalUi.TopBar.PotionContainer;
        var holders=(List<NPotionHolder>)Get(container,"_holders")!;
        Codec.Require(holders.Count==player.PotionSlots.Count,"direct-back-ui-potion-slot-count");
        var potions=holders.Select((holder,i)=>
        {
            Codec.Require(ReferenceEquals(holder.Potion?.Model,player.PotionSlots[i]),"direct-back-ui-potion-identity");
            Codec.Require(holder.Potion is null||!holder.Potion.Model.IsQueued&&!holder.Potion.Model.HasBeenRemovedFromState,"direct-back-potion-use-flags");
            Codec.Require(!(bool)Get(holder,"_disabledUntilPotionRemoved")!&&holder.Modulate.IsEqualApprox(Colors.White),"direct-back-ui-potion-disabled");
            var empty=(CanvasItem)Get(holder,"_emptyIcon")!;
            Codec.Require(Mathf.IsEqualApprox(empty.Modulate.A,holder.HasPotion?0:1),"direct-back-ui-potion-empty-state");
            return new{Slot=i,Model=holder.Potion?.Model.Id.ToString(),Queued=holder.Potion?.Model.IsQueued};
        }).ToArray();
        var relics=Nodes(NRun.Instance!.GlobalUi.RelicInventory).OfType<NRelicInventoryHolder>().Where(h=>player.Relics.Contains(h.Relic.Model)).Select(holder=>
        {
            var model=holder.Relic.Model;var label=(Control)Get(holder,"_amountLabel")!;
            var text=(string)AccessTools.Property(label.GetType(),"Text").GetValue(label)!;
            Codec.Require(!model.ShowCounter||label.Visible&&text==model.DisplayAmount.ToString(),"direct-back-ui-relic-counter");
            return new{Model=model.Id.ToString(),model.ShowCounter,model.DisplayAmount,ActualText=text,Status=model.Status.ToString()};
        }).ToArray();
        Codec.Require(relics.Length==player.Relics.Count,"direct-back-ui-relic-membership");
        var energy=Get(NCombatRoom.Instance!.Ui,"_energyCounter")!;var energyLabel=Get(energy,"_label")!;
        var energyText=(string)AccessTools.Property(energyLabel.GetType(),"Text").GetValue(energyLabel)!;
        Codec.Require(energyText==$"{player.PlayerCombatState!.Energy}/{player.PlayerCombatState.MaxEnergy}","direct-back-ui-energy");
        return new{Potions=potions,Relics=relics,EnergyText=energyText,PotionSlotsMatched=true,RelicCountersMatched=true,EnergyMatched=true};
    }
}
