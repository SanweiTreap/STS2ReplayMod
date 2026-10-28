namespace Spike001A4;

internal enum ProductionOutcome { Success, Faulted, Canceled }

internal static class ProductionTaskObserver
{
    internal static async Task WrapAsync(Task productionTask, Func<Task> observer,
        Func<ProductionOutcome, Exception?, Task> terminal)
    {
        ArgumentNullException.ThrowIfNull(productionTask);
        try
        {
            await productionTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            await BestEffortTerminal(terminal, ProductionOutcome.Canceled, ex).ConfigureAwait(false);
            throw;
        }
        catch (Exception ex)
        {
            await BestEffortTerminal(terminal, ProductionOutcome.Faulted, ex).ConfigureAwait(false);
            throw;
        }

        try
        {
            await observer().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await BestEffortTerminal(terminal, ProductionOutcome.Success, ex).ConfigureAwait(false);
        }
    }

    private static async Task BestEffortTerminal(Func<ProductionOutcome, Exception?, Task> terminal,
        ProductionOutcome outcome, Exception? exception)
    {
        try { await terminal(outcome, exception).ConfigureAwait(false); }
        catch { }
    }
}
