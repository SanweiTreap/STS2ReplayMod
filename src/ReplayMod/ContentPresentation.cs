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
