namespace Spike001A4;

internal static class ReadinessContract
{
    internal static readonly IReadOnlyList<string> RequiredFields =
    [
        "managerPresent", "statePresent", "singlePlayerPresent", "playerCombatStatePresent", "handPresent",
        "combatPresent", "executorPresent", "roomPresent", "combatIsInProgress", "combatIsNotOverOrEnding",
        "combatIsNotPaused", "runIsNotPaused", "playerActionsEnabled", "isPartOfPlayerTurn", "phaseIsPlay",
        "executorIsNotRunning", "executorIsNotPaused", "currentActionIsNull", "currentRunNodePresent",
        "targetIdentityStable"
    ];

    internal static (bool Ready, string? Breaker) Evaluate(IReadOnlyList<FieldObservation> fields,
        bool consecutiveFrame, bool identitiesStable)
    {
        if (!consecutiveFrame) return (false, "non-consecutive-process-frame");
        if (!identitiesStable) return (false, "target-or-object-identity-change");
        foreach (var name in RequiredFields)
        {
            var field = fields.FirstOrDefault(x => x.Name == name);
            if (field is null) return (false, name + ":missing");
            if (field.Status != ObservationStatus.Value || field.Value != "true")
                return (false, field.Name + ":" + field.Status + ":" + field.Value);
        }
        return (true, null);
    }
}
