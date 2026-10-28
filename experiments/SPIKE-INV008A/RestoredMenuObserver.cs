using System.Runtime.CompilerServices;

namespace SpikeInv008A;

internal sealed class RestoredMenuObserver
{
    private readonly object _sync = new();
    private TaskCompletionSource<bool>? _menuReady;
    private TaskCompletionSource<bool>? _refreshButtons;
    private TaskCompletionSource<bool>? _observerFailure;
    private object? _candidate;
    private int _candidateIdentity, _readyIdentity, _refreshIdentity;
    private int _prefixCount, _readyCount, _refreshCount;
    private FailureSite _failureSite;
    private string _failure = "None";

    internal void Reset()
    {
        lock (_sync)
        {
            _menuReady = NewSignal();
            _refreshButtons = NewSignal();
            _observerFailure = NewSignal();
            _candidate = null;
            _candidateIdentity = _readyIdentity = _refreshIdentity = 0;
            _prefixCount = _readyCount = _refreshCount = 0;
            _failureSite = FailureSite.None;
            _failure = "None";
        }
    }

    internal bool RegisterCandidate(object menu)
    {
        lock (_sync)
        {
            _prefixCount++;
            if (!Initialized()) return Fail(FailureSite.RestoredMenuIdentityOrder, "prefix-before-reset");
            if (_candidate is not null) return Fail(FailureSite.RestoredMenuIdentityOrder, "duplicate-ready-prefix");
            _candidate = menu;
            _candidateIdentity = Identity(menu);
            return true;
        }
    }

    internal bool ObserveRefreshButtons(object menu)
    {
        lock (_sync)
        {
            _refreshCount++;
            _refreshIdentity = Identity(menu);
            if (!Initialized() || _candidate is null)
                return Fail(FailureSite.RestoredMenuIdentityOrder, "refresh-before-candidate");
            if (!ReferenceEquals(menu, _candidate))
                return Fail(FailureSite.RestoredMenuIdentityOrder, "refresh-from-wrong-menu");
            if (_refreshCount != 1)
                return Fail(FailureSite.RestoredMenuIdentityOrder, "duplicate-refresh-callback");
            return _refreshButtons!.TrySetResult(true);
        }
    }

    internal bool ObserveReady(object menu)
    {
        lock (_sync)
        {
            _readyCount++;
            _readyIdentity = Identity(menu);
            if (!Initialized() || _candidate is null)
                return Fail(FailureSite.RestoredMenuIdentityOrder, "ready-postfix-without-prefix");
            if (!ReferenceEquals(menu, _candidate))
                return Fail(FailureSite.RestoredMenuIdentityOrder, "ready-postfix-identity-replaced");
            if (_readyCount != 1)
                return Fail(FailureSite.RestoredMenuIdentityOrder, "duplicate-ready-postfix");
            return _menuReady!.TrySetResult(true);
        }
    }

    internal async Task<RestoredMenuSignalEvidence> WaitAsync(TimeSpan timeout)
    {
        Task menu;
        Task refresh;
        Task failure;
        lock (_sync)
        {
            if (!Initialized()) return SnapshotLocked(FailureSite.RestoredMenuIdentityOrder, "wait-before-reset");
            menu = _menuReady!.Task;
            refresh = _refreshButtons!.Task;
            failure = _observerFailure!.Task;
        }
        Exception? waitError = null;
        try
        {
            var both = Task.WhenAll(menu, refresh);
            var completed = await Task.WhenAny(both, failure).WaitAsync(timeout);
            if (ReferenceEquals(completed, failure)) waitError = new InvalidOperationException("observer identity/order failure");
            else await both;
        }
        catch (Exception exception) { waitError = exception; }
        lock (_sync) return SnapshotAfterWaitLocked(waitError);
    }

    internal RestoredMenuSignalEvidence SnapshotAfterWait(Exception? waitError = null)
    {
        lock (_sync) return SnapshotAfterWaitLocked(waitError);
    }

    internal RestoredMenuSignalEvidence SnapshotLoadException(Exception exception)
    {
        lock (_sync) return SnapshotLocked(FailureSite.MainMenu, "load-main-menu:" + exception.GetType().Name);
    }

    private RestoredMenuSignalEvidence SnapshotAfterWaitLocked(Exception? waitError)
    {
        if (_failureSite != FailureSite.None) return SnapshotLocked(_failureSite, _failure);
        var menuStatus = Status(_menuReady?.Task);
        var refreshStatus = Status(_refreshButtons?.Task);
        if (waitError is not null)
        {
            if (menuStatus != ObservedTaskStatus.RanToCompletion)
                return SnapshotLocked(FailureSite.MenuReady, waitError.GetType().Name);
            if (refreshStatus != ObservedTaskStatus.RanToCompletion)
                return SnapshotLocked(FailureSite.RefreshButtons, waitError.GetType().Name);
            return SnapshotLocked(FailureSite.MainMenu, waitError.GetType().Name);
        }
        if (menuStatus != ObservedTaskStatus.RanToCompletion)
            return SnapshotLocked(FailureSite.MenuReady, "menu-ready-incomplete");
        if (refreshStatus != ObservedTaskStatus.RanToCompletion)
            return SnapshotLocked(FailureSite.RefreshButtons, "refresh-buttons-incomplete");
        return SnapshotLocked(FailureSite.None, "None");
    }

    private RestoredMenuSignalEvidence SnapshotLocked(FailureSite site, string failure)
    {
        var menuStatus = Status(_menuReady?.Task);
        var refreshStatus = Status(_refreshButtons?.Task);
        var passed = site == FailureSite.None && _candidate is not null && _prefixCount == 1 && _readyCount == 1 &&
                     _refreshCount == 1 && menuStatus == ObservedTaskStatus.RanToCompletion &&
                     refreshStatus == ObservedTaskStatus.RanToCompletion && _candidateIdentity == _readyIdentity &&
                     _candidateIdentity == _refreshIdentity;
        return new(_candidateIdentity, _readyIdentity, _refreshIdentity, _prefixCount, _readyCount, _refreshCount,
            menuStatus, refreshStatus, site, failure, passed);
    }

    private bool Initialized() => _menuReady is not null && _refreshButtons is not null && _observerFailure is not null;
    private bool Fail(FailureSite site, string failure)
    {
        if (_failureSite == FailureSite.None) { _failureSite = site; _failure = failure; }
        _observerFailure?.TrySetResult(true);
        return false;
    }
    private static int Identity(object value) => RuntimeHelpers.GetHashCode(value);
    private static ObservedTaskStatus Status(Task? task) => task is null ? ObservedTaskStatus.Null :
        !task.IsCompleted ? ObservedTaskStatus.Pending : task.IsCanceled ? ObservedTaskStatus.Canceled :
        task.IsFaulted ? ObservedTaskStatus.Faulted : ObservedTaskStatus.RanToCompletion;
    private static TaskCompletionSource<bool> NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
