using System.Reflection;
using HarmonyLib;
using Perf001Candidate.Core;

namespace Perf001Candidate;

public sealed record PatchSpec(string Key,string Group,string Type,string Method,string[] Parameters,string? Prefix,string? Postfix);
public static class PatchCatalog
{
    private const string Runs="MegaCrit.Sts2.Core.Runs.",Actions="MegaCrit.Sts2.Core.GameActions.",Combat="MegaCrit.Sts2.Core.Combat.";
    public static readonly PatchSpec[] Specs=
    [
        new("menu","Common","MegaCrit.Sts2.Core.Nodes.Screens.MainMenu.NMainMenu","_Ready",[],null,"Menu"),
        new("fresh","Common",Runs+"RunManager","SetUpNewSingleplayer",[Runs+"RunState","System.Boolean","System.Nullable`1<System.DateTimeOffset>"],null,"Fresh"),
        new("continue","Common",Runs+"RunManager","SetUpSavedSingleplayer",[Runs+"RunState","MegaCrit.Sts2.Core.Saves.SerializableRun"],"Saved",null),
        new("map","Common",Runs+"RunManager","EnterMapCoord",["MegaCrit.Sts2.Core.Map.MapCoord"],"MapBefore","MapAfter"),
        new("queue","Common",Actions+"Multiplayer.ActionQueueSynchronizer","RequestEnqueue",[Actions+"GameAction"],"Queued",null),
        new("execute","Common",Actions+"GameAction","Execute",[],"Execute",null),
        new("map-move-action","Common",Actions+"MoveToMapCoordAction","ExecuteAction",[],null,"MapMoveTask"),
        new("card-action","Common",Actions+"PlayCardAction","ExecuteAction",[],null,"ActionTask"),
        new("end-action","Common",Actions+"EndPlayerTurnAction","ExecuteAction",[],null,"ActionTask"),
        new("card","Common","MegaCrit.Sts2.Core.Models.CardModel","OnPlayWrapper",[Actions+"Multiplayer.PlayerChoiceContext","MegaCrit.Sts2.Core.Entities.Creatures.Creature","System.Boolean","MegaCrit.Sts2.Core.Entities.Cards.ResourceInfo","System.Boolean"],"CardBefore","CardAfter"),
        new("end-ready","Common",Combat+"CombatManager","SetReadyToEndTurn",["MegaCrit.Sts2.Core.Entities.Players.Player","System.Boolean","System.Func`1<System.Threading.Tasks.Task>"],"EndBefore","EndAfter"),
        new("turn","Common",Combat+"CombatManager","StartTurn",[Combat+"CombatTurnState","System.Func`1<System.Threading.Tasks.Task>"],"TurnBefore","TurnAfter"),
        new("cancel","Common",Actions+"GameAction","Cancel",[],"Cancelled",null),
        new("candidate-map","Candidate",Runs+"RunManager","EnterMapCoord",["MegaCrit.Sts2.Core.Map.MapCoord"],null,"CandidateMap"),
        new("candidate-card","Candidate","MegaCrit.Sts2.Core.Models.CardModel","OnPlayWrapper",[Actions+"Multiplayer.PlayerChoiceContext","MegaCrit.Sts2.Core.Entities.Creatures.Creature","System.Boolean","MegaCrit.Sts2.Core.Entities.Cards.ResourceInfo","System.Boolean"],"CandidateCard",null),
        new("candidate-end","Candidate",Combat+"CombatManager","SetReadyToEndTurn",["MegaCrit.Sts2.Core.Entities.Players.Player","System.Boolean","System.Func`1<System.Threading.Tasks.Task>"],null,"CandidateEnd")
    ];
    public static PatchSpec[] Plan(Mode mode)=>Specs.Where(x=>x.Group=="Common"||mode!=Mode.C0).ToArray();
    public static string TypeKey(Type type)=>type.IsGenericType?type.GetGenericTypeDefinition().FullName+"<"+string.Join(",",type.GetGenericArguments().Select(TypeKey))+">":type.FullName!;
    public static MethodInfo Resolve(Assembly game,PatchSpec spec)
        =>game.GetType(spec.Type,true)!.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static|BindingFlags.DeclaredOnly)
            .Single(m=>m.Name==spec.Method&&m.GetParameters().Select(p=>TypeKey(p.ParameterType)).SequenceEqual(spec.Parameters));
    public static void Install(Harmony harmony,Assembly game,Mode mode,Type callbacks)
    {
        foreach(var spec in Plan(mode))
        {
            HarmonyMethod? Handler(string? name)=>name is null?null:new HarmonyMethod(callbacks.GetMethod(name,BindingFlags.Public|BindingFlags.Static)!)
                {priority=spec.Group=="Common"?Priority.First:Priority.Last};
            harmony.Patch(Resolve(game,spec),prefix:Handler(spec.Prefix),postfix:Handler(spec.Postfix),
                finalizer:spec.Group=="Common"?Handler("OriginalFault"):null);
        }
    }
}
