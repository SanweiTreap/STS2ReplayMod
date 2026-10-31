using System.Text.Json;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReplayMod;

internal static class RunChoices
{
    internal static void Install(Harmony h)
    {
        h.Patch(AccessTools.Method(typeof(NCardRewardSelectionScreen),"OptionSelected"),postfix:new HarmonyMethod(typeof(RunChoices),nameof(Reward)));
        h.Patch(AccessTools.Method(typeof(NCardGridSelectionScreen),"CardsSelected"),postfix:new HarmonyMethod(typeof(RunChoices),nameof(Deck)));
        foreach(var (type,methods) in new[]{(typeof(NCardRewardSelectionScreen),new[]{"SelectCard","OnAlternateRewardSelected"}),
            (typeof(NDeckUpgradeSelectScreen),new[]{"OnCardClicked","ConfirmSelection","CancelSelection","CloseSelection"})})
            foreach(var method in methods)h.Patch(AccessTools.DeclaredMethod(type,method),prefix:new HarmonyMethod(typeof(RunChoices),nameof(Input)));
    }
    private static bool Input()=>!RunPlayback.Active||!RunPlayback.NonCombat||RunPlayback.Driving;
    internal static string CardKey(CardModel card)=>card.Id+":"+card.CurrentUpgradeLevel+":"+
        JsonSerializer.Serialize(card.ToSerializable(),JsonSerializationUtility.GetTypeInfo<SerializableCard>());
    private static void Reward(NCardRewardSelectionScreen __instance,ref Task<int?> __result)
    {
        if(!RunRecording.Choosing&&!RunPlayback.NonCombat)return;
        var cards=((IEnumerable<CardCreationResult>)RunPatches.Field(__instance,"_options")!).Select(c=>c.Card).ToArray();
        var alternatives=((System.Collections.IEnumerable)RunPatches.Field(__instance,"_extraOptions")!).Cast<object>().ToArray();
        var options=cards.Select(CardKey).Concat(alternatives.Select(a=>"alternative:"+a.GetType().Name)).ToArray();
        var native=__result;
        __result=Watch(native,"reward-card",options,n=>n is {} i?new[]{i}:[],async decision=>
        {
            await Ready(__instance);
            var index=decision.Selected.Length==0?-1:decision.Selected.Single();
            Codec.Require(index>=0&&index<options.Length,"unsupported-empty-card-reward-decision");
            await RunPlayback.TakePhase("选择奖励卡牌");
            RunPlayback.Native(()=>
            {
                if(index<cards.Length)AccessTools.Method(typeof(NCardRewardSelectionScreen),"SelectCard").Invoke(__instance,[__instance.GetCardHolder(cards[index])]);
                else AccessTools.Method(typeof(NCardRewardSelectionScreen),"OnAlternateRewardSelected").Invoke(__instance,[index-cards.Length]);
            });
        });
    }
    private static void Deck(NCardGridSelectionScreen __instance,ref Task<IEnumerable<CardModel>> __result)
    {
        if(!RunRecording.Choosing&&!RunPlayback.NonCombat)return;
        Codec.Require(__instance is NDeckUpgradeSelectScreen,"run-unsupported-deck-choice");
        var cards=((IEnumerable<CardModel>)RunPatches.Field(__instance,"_cards")!).ToArray();
        var native=__result;
        __result=Watch(native,"deck-upgrade",cards.Select(CardKey).ToArray(),selected=>selected.Select(c=>Array.FindIndex(cards,v=>ReferenceEquals(v,c))).ToArray(),async decision=>
        {
            await Ready(__instance);
            foreach(var index in decision.Selected)
            {
                Codec.Require(index>=0&&index<cards.Length,"run-upgrade-choice-index");
                await RunPlayback.TakePhase("选中升级卡牌");
                RunPlayback.Native(()=>AccessTools.Method(typeof(NDeckUpgradeSelectScreen),"OnCardClicked").Invoke(__instance,[cards[index]]));
            }
            await RunPlayback.TakePhase(decision.Selected.Length==0?"取消升级":"确认升级");
            RunPlayback.Native(()=>AccessTools.Method(typeof(NDeckUpgradeSelectScreen),decision.Selected.Length==0?"CloseSelection":"ConfirmSelection").Invoke(__instance,[null]));
        });
    }
    private static async Task<T> Watch<T>(Task<T> native,string kind,string[] options,Func<T,int[]> selected,Func<RunDecision,Task> drive)
    {
        if(RunPlayback.NonCombat)
        {
            var expected=RunPlayback.Decision(kind,options);var driver=drive(expected);
            await driver;var result=await native;
            Codec.Require(selected(result).SequenceEqual(expected.Selected),"run-native-choice-result-mismatch");return result;
        }
        var value=await native;Runtime.Guard(()=>RunRecording.Decision(new(kind,options,selected(value))));return value;
    }
    private static async Task Ready(Node node)
    {
        while(!node.IsNodeReady()){RunPlayback.CheckStop();await Runtime.NextFrame();}
        // The original reward/deck screens finish a common entrance before input.
        var at=System.Diagnostics.Stopwatch.GetTimestamp();
        while(System.Diagnostics.Stopwatch.GetElapsedTime(at).TotalSeconds<.6){RunPlayback.CheckStop();await Runtime.NextFrame();}
    }
}
