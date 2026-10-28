using System.Diagnostics;

namespace SpikeInv012A.Core;

public enum Stage { ManualReady, ProductionSave, CheckpointSeal, Capture, AwaitExit, Terminal }
public enum Outcome { None, CaptureComplete, SaveTimeout, SealTimeout, CaptureTimeout, SaveFault, SaveCanceled, ObserverFailure, EvidenceFailure, RestartIncomplete, PrebusinessOwnerUnavailable }
public enum FieldStatus { True, False, Missing, Exception }
public readonly record struct FieldSample(string Name, FieldStatus Status, string? ExceptionType = null, string? Site = null);
public readonly record struct StageStamp(string Attempt, string Process, Stage Stage, long Started, long Duration, long Generation);
public interface IClock { long Now { get; } long Frequency { get; } }
public interface IEvidence { bool TryWrite(Record record, bool final = false); }
public sealed record Record(int Schema, string Attempt, string Process, string Boot, string Event, Stage Stage,
    long Monotonic, long Generation, long Ticks, string? Name = null, string? Status = null,
    string? ExceptionType = null, string? Site = null, bool? Flag = null, long Count = 0, string? Summary = null);

// The only state owner. No production object, Task, save path or payload is retained.
public sealed class DiagnosticCore
{
    public const int Schema = 1;
    public const int QuietFrames = 30;
    public const int DeadlineSeconds = 30;
    private readonly object _gate = new();
    private readonly IClock _clock;
    private readonly IEvidence _evidence;
    private readonly string _attempt, _process, _boot;
    private static readonly HashSet<string> AllowedFields = new(StringComparer.Ordinal)
    {
        "managerPresent", "statePresent", "singlePlayerPresent", "playerCombatStatePresent", "handPresent",
        "combatPresent", "executorPresent", "roomPresent", "combatIsInProgress", "combatIsNotOverOrEnding",
        "combatIsNotPaused", "runIsNotPaused", "playerActionsEnabled", "isPartOfPlayerTurn", "phaseIsPlay",
        "executorIsNotRunning", "executorIsNotPaused", "currentActionIsNull", "currentRunNodePresent", "targetIdentityStable"
    };
    private readonly Dictionary<string, (long Count, long First, long Last)> _rejects = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (long Entered, long Returned, long FirstOrder, long LastOrder, bool? EntryFlag, bool? ReturnFlag)> _writers = new(StringComparer.Ordinal);
    private long _writerOrder;
    private StageStamp? _deadline;
    private long _generation, _ticks, _lastHeartbeat, _lastFrame = -1;
    private int _quiet;
    private bool _evidenceLost, _firstTickWritten, _attached, _terminal;
    private Outcome _outcome;
    private Stage _stage = Stage.ManualReady;

    public DiagnosticCore(IClock clock, IEvidence evidence, string attempt, string process, string boot)
    {
        _clock = clock; _evidence = evidence; _attempt = attempt; _process = process; _boot = boot;
        Write("boot");
    }
    public Stage Stage { get { lock (_gate) return _stage; } }
    public Outcome Outcome { get { lock (_gate) return _outcome; } }
    public bool EvidenceComplete { get { lock (_gate) return !_evidenceLost; } }
    public long Ticks { get { lock (_gate) return _ticks; } }
    public StageStamp? Deadline { get { lock (_gate) return _deadline; } }

    public void TickerAttached(bool insideTree, bool processing)
    {
        lock (_gate)
        {
            if (_terminal || _attached) return;
            _attached = true;
            Write("ticker-attached", flag: insideTree && processing);
            if (_evidenceLost) Seal(Outcome.EvidenceFailure, "ticker-evidence");
        }
    }
    public void Arm() { lock (_gate) { if (!_terminal && _stage == Stage.ManualReady) { Write("manual-ready"); if (_evidenceLost) Seal(Outcome.EvidenceFailure, "arm-evidence"); } } }
    public void Precheck(bool passed) { lock (_gate) { if (!_terminal) { Write("precheck", flag: passed); if (_evidenceLost) Seal(Outcome.EvidenceFailure, "precheck-evidence"); } } }
    public void SaveEntered(bool roomBefore, bool saveProgress)
    {
        lock (_gate)
        {
            if (_terminal || _stage != Stage.ManualReady || !roomBefore || !saveProgress) return;
            if (!Start(Stage.ProductionSave)) return;
            Write("save-entry", flag: saveProgress);
        }
    }
    public void SaveCompleted(bool success, bool canceled)
    {
        lock (_gate)
        {
            if (_terminal || _stage != Stage.ProductionSave) return;
            if (ExpireIfDue()) return;
            if (!success) { Seal(canceled ? Outcome.SaveCanceled : Outcome.SaveFault, "save-outcome"); return; }
            if (!Start(Stage.CheckpointSeal)) return;
            Write("save-complete");
        }
    }
    // This seals probe-owned checkpoint metadata only. It never reads or writes a game checkpoint.
    public void SealCheckpointMetadata()
    {
        lock (_gate)
        {
            if (_terminal || _stage != Stage.CheckpointSeal || ExpireIfDue()) return;
            Write("checkpoint-metadata-sealed");
            if (_evidenceLost) { Seal(Outcome.EvidenceFailure, "seal-evidence"); return; }
            Start(Stage.Capture);
        }
    }
    public void Tick(long frame, IReadOnlyList<FieldSample> fields, bool recording)
    {
        lock (_gate)
        {
            if (_terminal || ExpireIfDue()) return;
            _ticks++;
            if (!_firstTickWritten) { _firstTickWritten = true; Write("first-tick", flag: recording); }
            if (_stage != Stage.Capture) return;
            if (fields.Count != AllowedFields.Count || fields.Select(x => x.Name).Distinct(StringComparer.Ordinal).Count() != AllowedFields.Count ||
                fields.Any(x => !AllowedFields.Contains(x.Name)))
            { Seal(Outcome.ObserverFailure, "field-contract"); return; }
            bool ready = true;
            foreach (var f in fields)
            {
                if (f.Status == FieldStatus.True) continue;
                ready = false;
                var key = f.Name + ":" + f.Status;
                var prior = _rejects.GetValueOrDefault(key);
                _rejects[key] = (prior.Count + 1, prior.Count == 0 ? frame : prior.First, frame);
                if (prior.Count == 0) Write("reject", f.Name, f.Status.ToString(), CleanToken(f.ExceptionType), CleanToken(f.Site));
            }
            _quiet = ready && frame == _lastFrame + 1 ? _quiet + 1 : ready ? 1 : 0;
            _lastFrame = frame;
            if (_clock.Now - _lastHeartbeat >= 5 * _clock.Frequency)
            {
                _lastHeartbeat = _clock.Now;
                Write("heartbeat", flag: recording, count: _quiet);
            }
            if (_quiet >= QuietFrames)
            {
                if (ExpireIfDue()) return;
                if (_evidenceLost) { Seal(Outcome.EvidenceFailure, "capture-evidence"); return; }
                Write("capture-ready", flag: recording, count: _quiet, final: true);
                if (_evidenceLost) { Seal(Outcome.EvidenceFailure, "capture-evidence"); return; }
                _deadline = null; _stage = Stage.AwaitExit; _outcome = Outcome.CaptureComplete;
            }
        }
    }
    public void Writer(string name, bool entry, bool? flag)
    {
        lock (_gate)
        {
            // Names are fixed literals supplied by the patch layer, never caller paths.
            if (name is not ("NRun.Notification" or "NGame.Quit" or "RunManager.CleanUp" or
                "RunManager.WriteReplay" or "SettingsSaveManager.SaveSettings" or "CombatReplayWriter.WriteReplay")) return;
            var prior = _writers.GetValueOrDefault(name);
            var order = ++_writerOrder;
            _writers[name] = (prior.Entered + (entry ? 1 : 0), prior.Returned + (entry ? 0 : 1),
                prior.FirstOrder == 0 ? order : prior.FirstOrder, order,
                entry ? flag : prior.EntryFlag, entry ? prior.ReturnFlag : flag);
            // Two bounded flushes cover either Godot notification order. A normal return is
            // observed; an entry without return remains distinguishable in the counters.
            if (!entry && name is ("NRun.Notification" or "NGame.Quit"))
            {
                var summary = string.Join(';', _writers.OrderBy(x => x.Key, StringComparer.Ordinal)
                    .Select(x => $"{x.Key}:{x.Value.Entered}:{x.Value.Returned}:{x.Value.FirstOrder}:{x.Value.LastOrder}:" +
                        $"{x.Value.EntryFlag?.ToString() ?? "null"}:{x.Value.ReturnFlag?.ToString() ?? "null"}"));
                Write("writer-summary", flag: !_evidenceLost, summary: summary);
            }
        }
    }
    public void ObserverFailed(string type, string site)
    {
        lock (_gate)
        {
            if (_terminal) return;
            Write("observer-exception", exceptionType: CleanToken(type), site: CleanToken(site));
            Seal(Outcome.ObserverFailure, "observer-failure");
        }
    }
    public void PrebusinessOwnerUnavailable()
    {
        lock (_gate)
        {
            if (!_terminal && _stage == Stage.ManualReady)
                Seal(Outcome.PrebusinessOwnerUnavailable, "ticker-owner-unavailable");
        }
    }
    public void Poll(long generation = -1)
    {
        lock (_gate)
        {
            if (_terminal || (generation >= 0 && _deadline?.Generation != generation)) return;
            ExpireIfDue();
        }
    }
    public void MarkRestart(string priorProcess, Stage priorStage)
    {
        lock (_gate)
        {
            if (priorProcess == _process || priorStage is Stage.AwaitExit or Stage.Terminal) return;
            Seal(Outcome.RestartIncomplete, "prior-process-incomplete");
        }
    }
    private bool Start(Stage stage)
    {
        _stage = stage;
        _deadline = new(_attempt, _process, stage, _clock.Now, DeadlineSeconds * _clock.Frequency, ++_generation);
        Write("stage-start", stage.ToString());
        if (_evidenceLost) Seal(Outcome.EvidenceFailure, "stage-evidence");
        return !_terminal;
    }
    private bool ExpireIfDue()
    {
        if (_deadline is not { } d || _clock.Now - d.Started < d.Duration) return false;
        Seal(d.Stage switch { Stage.ProductionSave => Outcome.SaveTimeout, Stage.CheckpointSeal => Outcome.SealTimeout,
            _ => Outcome.CaptureTimeout }, "deadline");
        return true;
    }
    private void Seal(Outcome outcome, string reason)
    {
        if (_terminal) return;
        _terminal = true; _stage = Stage.Terminal; _deadline = null; _outcome = outcome;
        var summary = string.Join(';', _rejects.OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => $"{x.Key}:{x.Value.Count}:{x.Value.First}:{x.Value.Last}"));
        if (summary.Length > 2048) { _evidenceLost = true; summary = "aggregate_overflow"; }
        Write("terminal", reason, outcome.ToString(), flag: !_evidenceLost,
            count: _rejects.Values.Sum(x => x.Count), final: true, summary: summary);
    }
    private void Write(string eventName, string? name = null, string? status = null, string? exceptionType = null,
        string? site = null, bool? flag = null, long count = 0, bool final = false, string? summary = null)
    {
        try
        {
            if (!_evidence.TryWrite(new(Schema, _attempt, _process, _boot, eventName, _stage,
                _clock.Now, _generation, _ticks, name, status, exceptionType, site, flag, count, summary), final))
                _evidenceLost = true;
        }
        catch { _evidenceLost = true; }
    }
    private static string? CleanToken(string? token)
    {
        if (token is null) return null;
        if (token.Length > 128 || token.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '+')))
            return "invalid_token";
        return token;
    }
}
public sealed class StopwatchClock : IClock
{
    public long Now => Stopwatch.GetTimestamp();
    public long Frequency => Stopwatch.Frequency;
}
