using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using SpikeInv012A.Core;

namespace SpikeInv012A;

internal static class Readiness
{
    private sealed record ArmTarget(string Seed, string Character, int Act, int Floor, int Row, int Col, string PointType);
    private static ArmTarget? _target;
    private static RunState? _stateIdentity;
    private static CombatRoom? _roomIdentity;
    internal static void CaptureTarget()
    {
        var state = RunManager.Instance?.DebugOnlyGetState() ?? throw new InvalidOperationException("run-unavailable");
        if (state.Players.Count != 1 || !state.GameMode.ToString().Equals("Standard", StringComparison.OrdinalIgnoreCase) ||
            state.CurrentActIndex != 0 || CombatManager.Instance?.IsInProgress == true ||
            state.CurrentMapPoint is null || state.CurrentMapCoord is null)
            throw new InvalidOperationException("arm-scene-invalid");
        var candidates = state.CurrentMapPoint.Children.Where(x => x.PointType == MapPointType.Monster)
            .Select(x => (Point: x, Coord: FindCoord(state.Map, x))).OrderBy(x => x.Coord.row)
            .ThenBy(x => x.Coord.col).ToArray();
        if (candidates.Length == 0) throw new InvalidOperationException("target-unavailable");
        var chosen = candidates[0];
        _target = new(state.Rng.ToSerializable().Seed ?? throw new InvalidOperationException("seed-unavailable"), state.Players[0].Character.Id.ToString(),
            state.CurrentActIndex, checked(state.TotalFloor + 1), chosen.Coord.row, chosen.Coord.col,
            chosen.Point.PointType.ToString());
    }
    internal static bool TargetMatchesCurrentMap()
    {
        var coord = RunManager.Instance?.DebugOnlyGetState()?.CurrentMapCoord;
        return _target is not null && coord?.row == _target.Row && coord?.col == _target.Col;
    }
    private static MapCoord FindCoord(ActMap map, MapPoint point)
    {
        for (var row = 0; row < map.GetRowCount(); row++)
        for (var col = 0; col < map.GetColumnCount(); col++)
            if (ReferenceEquals(map.GetPoint(col, row), point)) return new MapCoord(col, row);
        throw new InvalidOperationException("target-coordinate-unavailable");
    }
    internal static FieldSample[] Read()
    {
        var f = new List<FieldSample>(20);
        var manager = Object("managerPresent", "run.manager", () => RunManager.Instance, f);
        var state = Object("statePresent", "run.state", () => manager?.DebugOnlyGetState(), f);
        Player? player = null;
        try { if (state?.Players.Count == 1) player = state.Players[0]; Add(f, "singlePlayerPresent", player is not null); }
        catch (Exception ex) { Error(f, "singlePlayerPresent", "state.players.single", ex); }
        var pcs = Object("playerCombatStatePresent", "player.combatState", () => player?.PlayerCombatState, f);
        var hand = Object("handPresent", "playerCombat.hand", () => pcs?.Hand, f);
        var combat = Object("combatPresent", "combat.instance", () => CombatManager.Instance, f);
        var executor = Object("executorPresent", "run.executor", () => manager?.ActionExecutor, f);
        var room = Object("roomPresent", "run.currentCombatRoom", () => state?.CurrentRoom as CombatRoom, f);
        Bool(f, "combatIsInProgress", "combat.isInProgress", combat is not null, () => combat!.IsInProgress);
        Bool(f, "combatIsNotOverOrEnding", "combat.isOverOrEnding", combat is not null, () => !combat!.IsOverOrEnding);
        Bool(f, "combatIsNotPaused", "combat.isPaused", combat is not null, () => !combat!.IsPaused);
        Bool(f, "runIsNotPaused", "run.isPaused", manager is not null, () => !manager!.IsPaused);
        Bool(f, "playerActionsEnabled", "combat.playerActionsDisabled", combat is not null, () => !combat!.PlayerActionsDisabled);
        Bool(f, "isPartOfPlayerTurn", "combat.isPartOfPlayerTurn", combat is not null && player is not null,
            () => combat!.IsPartOfPlayerTurn(player!));
        Bool(f, "phaseIsPlay", "playerCombat.phase", pcs is not null, () => pcs!.Phase == PlayerTurnPhase.Play);
        Bool(f, "executorIsNotRunning", "executor.isRunning", executor is not null, () => !executor!.IsRunning);
        Bool(f, "executorIsNotPaused", "executor.isPaused", executor is not null, () => !executor!.IsPaused);
        Bool(f, "currentActionIsNull", "executor.currentAction", executor is not null, () => executor!.CurrentlyRunningAction is null);
        Bool(f, "currentRunNodePresent", "ngame.currentRunNode", true, () => NGame.Instance?.CurrentRunNode is not null);
        if (_stateIdentity is null && state is not null) _stateIdentity = state;
        if (_roomIdentity is null && room is not null) _roomIdentity = room;
        try
        {
            var coord = state?.CurrentMapCoord;
            var targetStable = _target is not null && state is not null && player is not null && room is not null &&
                combat is not null && coord is not null && ReferenceEquals(_stateIdentity, state) &&
                ReferenceEquals(_roomIdentity, room) && state.Rng.ToSerializable().Seed == _target.Seed &&
                player.Character.Id.ToString() == _target.Character && state.CurrentActIndex == _target.Act &&
                state.TotalFloor == _target.Floor && coord.Value.row == _target.Row && coord.Value.col == _target.Col &&
                state.CurrentMapPoint?.PointType.ToString() == _target.PointType &&
                room.RoomType.ToString().Contains("Monster", StringComparison.OrdinalIgnoreCase) &&
                room.ModelId.ToString() is { Length: > 0 } && room.CombatState.Encounter?.Id.ToString() is { Length: > 0 } &&
                combat.DebugOnlyGetState()?.RoundNumber == 1;
            Add(f, "targetIdentityStable", targetStable);
        }
        catch (Exception ex) { Error(f, "targetIdentityStable", "target.identity", ex); }
        _ = hand;
        return f.ToArray();
    }
    private static T? Object<T>(string name, string site, Func<T?> get, List<FieldSample> list) where T : class
    {
        try { var value = get(); Add(list, name, value is not null); return value; }
        catch (Exception ex) { Error(list, name, site, ex); return null; }
    }
    private static void Bool(List<FieldSample> list, string name, string site, bool available, Func<bool> get)
    {
        if (!available) { list.Add(new(name, FieldStatus.Missing)); return; }
        try { Add(list, name, get()); } catch (Exception ex) { Error(list, name, site, ex); }
    }
    private static void Add(List<FieldSample> list, string name, bool value) => list.Add(new(name, value ? FieldStatus.True : FieldStatus.False));
    private static void Error(List<FieldSample> list, string name, string site, Exception ex)
        => list.Add(new(name, FieldStatus.Exception, ex.GetType().Name, site));
}
