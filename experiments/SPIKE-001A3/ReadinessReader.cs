using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;

namespace Spike001A3;

internal static class ReadinessReader
{
    internal static RawReadinessSample Read(TargetIdentity target, long tick, ulong frame, long monotonic,
        int quietFrames, string? priorIdentityToken, ulong? priorProcessFrame, bool sameTickRecheck)
    {
        var fields = new List<FieldObservation>();
        var manager = ReadObject("managerPresent", "run.manager", () => RunManager.Instance, fields);
        var state = ReadObject("statePresent", "run.state", () => manager?.DebugOnlyGetState(), fields);
        var player = ReadSinglePlayer(state, fields);
        var pcs = ReadObject("playerCombatStatePresent", "player.combatState", () => player?.PlayerCombatState, fields);
        var hand = ReadObject("handPresent", "playerCombat.hand", () => pcs?.Hand, fields);
        var combat = ReadObject("combatPresent", "combat.instance", () => CombatManager.Instance, fields);
        var executor = ReadObject("executorPresent", "run.executor", () => manager?.ActionExecutor, fields);
        var room = ReadObject("roomPresent", "run.currentRoom", () => state?.CurrentRoom, fields);

        var inProgress = ReadBool("combatIsInProgress", "combat.isInProgress", () => combat!.IsInProgress, combat is not null, fields);
        var notOver = ReadBool("combatIsNotOverOrEnding", "combat.isOverOrEnding", () => !combat!.IsOverOrEnding, combat is not null, fields);
        var combatNotPaused = ReadBool("combatIsNotPaused", "combat.isPaused", () => !combat!.IsPaused, combat is not null, fields);
        var runNotPaused = ReadBool("runIsNotPaused", "run.isPaused", () => !manager!.IsPaused, manager is not null, fields);
        var actionsEnabled = ReadBool("playerActionsEnabled", "combat.playerActionsDisabled", () => !combat!.PlayerActionsDisabled, combat is not null, fields);
        var playerTurn = ReadBool("isPartOfPlayerTurn", "combat.isPartOfPlayerTurn", () => combat!.IsPartOfPlayerTurn(player!), combat is not null && player is not null, fields);
        var phasePlay = ReadBool("phaseIsPlay", "playerCombat.phase", () => pcs!.Phase == PlayerTurnPhase.Play, pcs is not null, fields);
        var executorIdle = ReadBool("executorIsNotRunning", "executor.isRunning", () => !executor!.IsRunning, executor is not null, fields);
        var executorNotPaused = ReadBool("executorIsNotPaused", "executor.isPaused", () => !executor!.IsPaused, executor is not null, fields);
        var currentActionNull = ReadBool("currentActionIsNull", "executor.currentAction", () => executor!.CurrentlyRunningAction is null, executor is not null, fields);
        var runNodePresent = ReadBool("currentRunNodePresent", "ngame.currentRunNode", () => NGame.Instance?.CurrentRunNode is not null, true, fields);
        ReadValue("handCount", "playerCombat.hand.count", () => hand!.Cards.Count.ToString(), hand is not null, fields);
        ReadValue("currentActionType", "executor.currentAction.type",
            () => executor!.CurrentlyRunningAction?.GetType().FullName ?? "<null>", executor is not null, fields);

        var targetToken = ReadTarget(state, player, room, combat, target, fields, out var targetStable);
        var identities = new ObjectIdentities(Identity(state), Identity(player), Identity(pcs), Identity(hand),
            Identity(combat), Identity(executor), Identity(room));
        var identityToken = string.Join("|", new[] { identities.State, identities.Player, identities.PlayerCombatState,
            identities.Hand, identities.Combat, identities.Executor, identities.Room, targetToken });
        var identitiesStable = priorIdentityToken is null || identityToken == priorIdentityToken;
        var consecutive = sameTickRecheck || priorProcessFrame is null || frame == priorProcessFrame.Value + 1;
        var evaluation = ReadinessContract.Evaluate(fields, consecutive, identitiesStable);
        if (!identitiesStable)
        {
            var index = fields.FindIndex(x => x.Name == "targetIdentityStable");
            if (index >= 0) fields[index] = fields[index] with { Value = "false" };
            evaluation = (false, "target-or-object-identity-change");
        }
        return new(tick, frame, monotonic, DateTimeOffset.UtcNow.ToString("O"), fields, identities,
            targetToken, evaluation.Ready && targetStable, quietFrames,
            !targetStable && evaluation.Breaker is null ? "target-identity-false" : evaluation.Breaker);
    }

    private static T? ReadObject<T>(string name, string site, Func<T?> read, List<FieldObservation> fields) where T : class
    {
        try
        {
            var value = read(); fields.Add(new(name, value is null ? "null" : "value", value is null ? null : "true", site, null, null));
            return value;
        }
        catch (Exception ex) { fields.Add(Exception(name, site, ex)); return null; }
    }

    private static Player? ReadSinglePlayer(RunState? state, List<FieldObservation> fields)
    {
        const string site = "state.players.single";
        if (state is null) { fields.Add(new("singlePlayerPresent", "null", null, site, null, null)); return null; }
        try
        {
            var count = state.Players.Count; var value = count == 1 ? state.Players[0] : null;
            fields.Add(new("singlePlayerPresent", "value", (value is not null).ToString().ToLowerInvariant(), site, null, null));
            fields.Add(new("playerCount", "value", count.ToString(), site + ".count", null, null));
            return value;
        }
        catch (Exception ex) { fields.Add(Exception("singlePlayerPresent", site, ex)); return null; }
    }

    private static bool ReadBool(string name, string site, Func<bool> read, bool dependency, List<FieldObservation> fields)
    {
        if (!dependency) { fields.Add(new(name, "null", null, site, null, null)); return false; }
        try { var value = read(); fields.Add(new(name, "value", value.ToString().ToLowerInvariant(), site, null, null)); return value; }
        catch (Exception ex) { fields.Add(Exception(name, site, ex)); return false; }
    }

    private static void ReadValue(string name, string site, Func<string> read, bool dependency, List<FieldObservation> fields)
    {
        if (!dependency) { fields.Add(new(name, "null", null, site, null, null)); return; }
        try { fields.Add(new(name, "value", read(), site, null, null)); }
        catch (Exception ex) { fields.Add(Exception(name, site, ex)); }
    }

    private static string? ReadText(string name, string site, Func<string> read, bool dependency, List<FieldObservation> fields)
    {
        if (!dependency) { fields.Add(new(name, "null", null, site, null, null)); return null; }
        try { var value = read(); fields.Add(new(name, "value", value, site, null, null)); return value; }
        catch (Exception ex) { fields.Add(Exception(name, site, ex)); return null; }
    }

    private static string ReadTarget(RunState? state, Player? player, object? room, CombatManager? combat, TargetIdentity target,
        List<FieldObservation> fields, out bool stable)
    {
        var seed = ReadText("targetSeed", "target.seed", () => state!.Rng.ToSerializable().Seed ?? "", state is not null, fields);
        var character = ReadText("targetCharacter", "target.character", () => player!.Character.Id.ToString(), player is not null, fields);
        var act = ReadText("targetAct", "target.act", () => state!.CurrentActIndex.ToString(), state is not null, fields);
        var floor = ReadText("targetFloor", "target.floor", () => state!.TotalFloor.ToString(), state is not null, fields);
        var row = ReadText("targetMapRow", "target.map.row", () => state!.CurrentMapCoord?.row.ToString() ?? "<null>", state is not null, fields);
        var col = ReadText("targetMapCol", "target.map.col", () => state!.CurrentMapCoord?.col.ToString() ?? "<null>", state is not null, fields);
        var roomId = ReadText("targetRoomId", "target.room.id", () => state!.CurrentRoom?.Id.ToString() ?? "<null>", state is not null && room is not null, fields);
        var roomModel = ReadText("targetRoomModel", "target.room.model", () => state!.CurrentRoom?.ModelId?.ToString() ?? "<null>", state is not null && room is not null, fields);
        var roomType = ReadText("targetRoomType", "target.room.type", () => state!.CurrentRoom?.RoomType.ToString() ?? "<null>", state is not null && room is not null, fields);
        var round = ReadText("targetCombatRound", "target.combat.round", () => combat!.DebugOnlyGetState()?.RoundNumber.ToString() ?? "<null>", combat is not null, fields);
        stable = seed == target.Seed && character == target.Character && act == target.ActIndex.ToString()
            && floor == target.TotalFloor.ToString() && row == target.MapRow.ToString() && col == target.MapCol.ToString()
            && roomType?.Contains("Monster", StringComparison.OrdinalIgnoreCase) == true && round == "1";
        fields.Add(new("targetIdentityStable", "value", stable.ToString().ToLowerInvariant(), "target.identity", null, null));
        return string.Join("|", seed, character, act, floor, row, col, roomId, roomModel, roomType, round);
    }

    private static FieldObservation Exception(string name, string site, Exception ex)
        => new(name, "exception", null, site, ex.GetType().FullName, ex.Message);
    private static string? Identity(object? value) => value is null ? null : value is GodotObject godot
        ? value.GetType().FullName + "#" + godot.GetInstanceId()
        : value.GetType().FullName + "#" + RuntimeHelpers.GetHashCode(value);
}
