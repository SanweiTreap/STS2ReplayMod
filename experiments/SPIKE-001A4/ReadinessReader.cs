using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace Spike001A4;

internal static class ReadinessReader
{
    internal static RawReadinessSample Read(TargetIdentity target, SemanticExpectation? expectation,
        long tick, ulong frame, long monotonic, int quietFrames, string? priorIdentityToken,
        ulong? priorProcessFrame, bool sameTickRecheck)
    {
        var fields = new List<FieldObservation>();
        var manager = ReadObject("managerPresent", "run.manager", () => RunManager.Instance, fields);
        var state = ReadObject("statePresent", "run.state", () => manager?.DebugOnlyGetState(), fields);
        var player = ReadSinglePlayer(state, fields);
        var pcs = ReadObject("playerCombatStatePresent", "player.combatState", () => player?.PlayerCombatState, fields);
        var hand = ReadObject("handPresent", "playerCombat.hand", () => pcs?.Hand, fields);
        var combat = ReadObject("combatPresent", "combat.instance", () => CombatManager.Instance, fields);
        var executor = ReadObject("executorPresent", "run.executor", () => manager?.ActionExecutor, fields);
        var room = ReadObject("roomPresent", "run.currentCombatRoom", () => state?.CurrentRoom as CombatRoom, fields);

        ReadBool("combatIsInProgress", "combat.isInProgress", () => combat!.IsInProgress, combat is not null, fields);
        ReadBool("combatIsNotOverOrEnding", "combat.isOverOrEnding", () => !combat!.IsOverOrEnding, combat is not null, fields);
        ReadBool("combatIsNotPaused", "combat.isPaused", () => !combat!.IsPaused, combat is not null, fields);
        ReadBool("runIsNotPaused", "run.isPaused", () => !manager!.IsPaused, manager is not null, fields);
        ReadBool("playerActionsEnabled", "combat.playerActionsDisabled", () => !combat!.PlayerActionsDisabled, combat is not null, fields);
        ReadBool("isPartOfPlayerTurn", "combat.isPartOfPlayerTurn", () => combat!.IsPartOfPlayerTurn(player!), combat is not null && player is not null, fields);
        ReadBool("phaseIsPlay", "playerCombat.phase", () => pcs!.Phase == PlayerTurnPhase.Play, pcs is not null, fields);
        ReadBool("executorIsNotRunning", "executor.isRunning", () => !executor!.IsRunning, executor is not null, fields);
        ReadBool("executorIsNotPaused", "executor.isPaused", () => !executor!.IsPaused, executor is not null, fields);
        ReadBool("currentActionIsNull", "executor.currentAction", () => executor!.CurrentlyRunningAction is null, executor is not null, fields);
        ReadBool("currentRunNodePresent", "ngame.currentRunNode", () => NGame.Instance?.CurrentRunNode is not null, true, fields);
        ReadValue("handCount", "playerCombat.hand.count", () => hand!.Cards.Count.ToString(), hand is not null, fields);
        ReadOptionalValue("currentActionType", "executor.currentAction.type",
            () => executor!.CurrentlyRunningAction?.GetType().FullName, executor is not null, fields);

        var targetToken = ReadTarget(state, player, room, combat, target, expectation, fields, out var targetStable);
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
            var value = read();
            fields.Add(new(name, value is null ? ObservationStatus.Missing : ObservationStatus.Value,
                value is null ? null : "true", site, null, null));
            return value;
        }
        catch (Exception ex) { fields.Add(ReadException(name, site, ex)); return null; }
    }

    private static Player? ReadSinglePlayer(RunState? state, List<FieldObservation> fields)
    {
        const string site = "state.players.single";
        if (state is null) { fields.Add(Missing("singlePlayerPresent", site)); return null; }
        try
        {
            var count = state.Players.Count; var value = count == 1 ? state.Players[0] : null;
            fields.Add(new("singlePlayerPresent", ObservationStatus.Value,
                (value is not null).ToString().ToLowerInvariant(), site, null, null));
            fields.Add(new("playerCount", ObservationStatus.Value, count.ToString(), site + ".count", null, null));
            return value;
        }
        catch (Exception ex) { fields.Add(ReadException("singlePlayerPresent", site, ex)); return null; }
    }

    private static void ReadBool(string name, string site, Func<bool> read, bool dependency, List<FieldObservation> fields)
    {
        if (!dependency) { fields.Add(Missing(name, site)); return; }
        try
        {
            var value = read();
            fields.Add(new(name, ObservationStatus.Value, value.ToString().ToLowerInvariant(), site, null, null));
        }
        catch (Exception ex) { fields.Add(ReadException(name, site, ex)); }
    }

    private static string? ReadValue(string name, string site, Func<string?> read, bool dependency,
        List<FieldObservation> fields, bool requireNonEmpty = false)
    {
        if (!dependency) { fields.Add(Missing(name, site)); return null; }
        try
        {
            var value = read();
            if (value is null || (requireNonEmpty && string.IsNullOrWhiteSpace(value)))
            {
                fields.Add(Missing(name, site)); return null;
            }
            fields.Add(new(name, ObservationStatus.Value, value, site, null, null));
            return value;
        }
        catch (Exception ex) { fields.Add(ReadException(name, site, ex)); return null; }
    }

    private static void ReadOptionalValue(string name, string site, Func<string?> read, bool dependency,
        List<FieldObservation> fields) => _ = ReadValue(name, site, read, dependency, fields);

    private static string ReadTarget(RunState? state, Player? player, CombatRoom? room, CombatManager? combat,
        TargetIdentity target, SemanticExpectation? expectation, List<FieldObservation> fields, out bool stable)
    {
        var seed = ReadValue("targetSeed", "target.seed", () => state?.Rng.ToSerializable().Seed,
            state is not null, fields, true);
        var character = ReadValue("targetCharacter", "target.character", () => player?.Character.Id.ToString(),
            player is not null, fields, true);
        var act = ReadValue("targetAct", "target.act", () => state?.CurrentActIndex.ToString(), state is not null, fields);
        var floor = ReadValue("targetPlayableFloor", "target.playableFloor", () => state?.TotalFloor.ToString(), state is not null, fields);
        var row = ReadValue("targetMapRow", "target.map.row", () => state?.CurrentMapCoord?.row.ToString(), state is not null, fields);
        var col = ReadValue("targetMapCol", "target.map.col", () => state?.CurrentMapCoord?.col.ToString(), state is not null, fields);
        var pointType = ReadValue("targetPointType", "target.map.pointType", () => state?.CurrentMapPoint?.PointType.ToString(), state is not null, fields, true);
        var roomId = ReadValue("targetRoomId", "target.room.id", () => room?.Id.ToString(), room is not null, fields);
        var roomModel = ReadValue("targetRoomModel", "target.room.model", () => room?.ModelId.ToString(), room is not null, fields, true);
        var roomType = ReadValue("targetRoomType", "target.room.type", () => room?.RoomType.ToString(), room is not null, fields, true);
        var encounter = ReadValue("targetEncounter", "target.combat.encounter",
            () => room?.CombatState.Encounter?.Id.ToString(), room is not null, fields, true);
        var round = ReadValue("targetCombatRound", "target.combat.round",
            () => combat?.DebugOnlyGetState()?.RoundNumber.ToString(), combat is not null, fields);
        var expectedSemantics = expectation is null
            || (roomModel == expectation.RoomModelId && encounter == expectation.EncounterId);
        stable = seed == target.Seed && character == target.Character && act == target.ActIndex.ToString()
            && floor == target.ExpectedPlayableFloor.ToString() && row == target.MapRow.ToString()
            && col == target.MapCol.ToString() && pointType == target.PointType
            && roomType?.Contains("Monster", StringComparison.OrdinalIgnoreCase) == true && round == "1"
            && roomModel is not null && encounter is not null && expectedSemantics;
        fields.Add(new("targetIdentityStable", ObservationStatus.Value,
            stable.ToString().ToLowerInvariant(), "target.identity", null, null));
        return string.Join("|", seed, character, act, floor, row, col, pointType, roomId,
            roomModel, roomType, encounter, round);
    }

    private static FieldObservation Missing(string name, string site)
        => new(name, ObservationStatus.Missing, null, site, null, null);
    private static FieldObservation ReadException(string name, string site, Exception ex)
        => new(name, ObservationStatus.ReadException, null, site, ex.GetType().FullName, ex.Message);
    private static string? Identity(object? value) => value is null ? null : value is GodotObject godot
        ? value.GetType().FullName + "#" + godot.GetInstanceId()
        : value.GetType().FullName + "#" + RuntimeHelpers.GetHashCode(value);
}
