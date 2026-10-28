namespace Spike001A3;

internal static class CheckpointObservation
{
    internal static async Task AwaitProductionAsync(Task productionTask)
    {
        ArgumentNullException.ThrowIfNull(productionTask);
        await productionTask;
    }

    internal static bool RoomEnteredBeforeBaseline(bool targetMatches, bool baselineExists)
        => targetMatches && !baselineExists;
}
