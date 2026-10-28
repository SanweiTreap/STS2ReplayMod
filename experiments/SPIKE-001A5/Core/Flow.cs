namespace Spike001A5.Core;

public enum Stage { Startup, ReadyArm, AwaitSave, ProductionSave, Seal, CaptureA, AComplete, ReadyB, CaptureB, Complete, Terminal }
public enum FieldStatus { False, True, Missing, Exception }
public sealed record Field(string Name, FieldStatus Status);
public enum SampleStatus { NoSample, Sampled }
public sealed record CaptureSample(long EngineFrame,long Monotonic,Field[] Fields,string? BlockingField,string? ExceptionField,
    int QuietBefore,int QuietAfter,bool ContinuityIdentityPresent);
public sealed record CaptureDiagnostics(Stage Phase,long Callbacks,SampleStatus SampleStatus,CaptureSample? LastSample,
    int Consecutive,int HeartbeatAttempts);
public sealed record Event(string Name, Stage Stage, long Monotonic, long? Frame, int Consecutive, string? Reason,CaptureDiagnostics? Diagnostics);
public sealed record QuietWindow(long FirstFrame,long LastFrame,long StartMs,long EndMs,int Count,long MaxGapMs,Field[] Fields);

public sealed class Flow
{
    public static readonly string[] FieldNames = ["managerPresent","statePresent","singlePlayerPresent","playerCombatStatePresent","handPresent","combatPresent","executorPresent","roomPresent","combatIsInProgress","combatIsNotOverOrEnding","combatIsNotPaused","runIsNotPaused","playerActionsEnabled","isPartOfPlayerTurn","phaseIsPlay","executorIsNotRunning","executorIsNotPaused","currentActionIsNull","currentRunNodePresent","targetIdentityStable"];
    public const int MaxHeartbeats=20,MaxEventBytes=4096,MaxRecordBytes=8192;
    public const int MaxHeartbeatBytes=MaxHeartbeats*MaxRecordBytes;
    private readonly object _sync = new();
    private readonly IClock _clock;
    private readonly Action<Event> _write;
    private Stage _stage = Stage.Startup;
    private long? _deadline;
    private long _lastFrame=-1, _lastTick=-1;
    private long _quietFrame,_quietMs,_maxGap;
    private Field[]? _lastFields;
    private int _consecutive;
    private Stage? _capturePhase;
    private long _callbacks,_nextHeartbeat;
    private int _heartbeatAttempts;
    private CaptureSample? _lastSample;
    private CaptureDiagnostics? _frozenTerminal;
    private string? _identity, _terminal;
    public Stage Stage { get { lock(_sync) return _stage; } }
    public string? TerminalReason { get { lock(_sync) return _terminal; } }
    public CaptureDiagnostics? Diagnostics { get { lock(_sync) return Copy(_frozenTerminal??Snapshot()); } }
    public QuietWindow Window { get { lock(_sync) { Contract.Require(_stage is Stage.CaptureA or Stage.CaptureB && _consecutive==30 && _lastFields is not null,"window-not-ready");
        return new(_quietFrame,_lastFrame,_quietMs,_lastTick,30,_maxGap,_lastFields.Select(x=>x with{}).ToArray()); } } }
    public Flow(IClock clock, Action<Event> write) { _clock=clock; _write=write; _deadline=clock.Milliseconds+10000; }
    public bool FirstFrame(bool b)
    {
        lock(_sync)
        {
            if (_stage!=Stage.Startup || !Alive()) return false;
            if (!Emit("first-tick") || !Alive() || !Emit(b?"ready-B-continue":"ready-A-arm") || !Alive()) return false;
            _stage=b?Stage.ReadyB:Stage.ReadyArm; _deadline=null; return true;
        }
    }
    public bool Arm()
    { lock(_sync) { if(_stage!=Stage.ReadyArm || !Alive())return false; _stage=Stage.AwaitSave; _deadline=null; return Emit("armed"); } }
    public bool SaveStarted()
    { lock(_sync) { if(_stage!=Stage.AwaitSave || !Alive())return false; _stage=Stage.ProductionSave; _deadline=_clock.Milliseconds+15000; return Emit("production-save-start"); } }
    public bool BeginSeal()
    { lock(_sync) { if(_stage!=Stage.ProductionSave || !Alive())return false; _stage=Stage.Seal; _deadline=_clock.Milliseconds+5000; return Emit("seal-start"); } }
    public bool Sealed()
    {
        lock(_sync)
        {
            if(_stage!=Stage.Seal || !Alive())return false;
            if(!Emit("checkpoint-sealed") || !Alive())return false;
            _stage=Stage.CaptureA; _deadline=_clock.Milliseconds+20000; StartCapture();
            return Emit("A-capture-start") && Alive();
        }
    }
    public bool ContinueStarted()
    { lock(_sync) { if(_stage!=Stage.ReadyB || !Alive())return false; _stage=Stage.CaptureB; _deadline=_clock.Milliseconds+20000; StartCapture(); return Emit("B-capture-start"); } }
    // Frame number is Engine.GetProcessFrames, not a fabricated callback sequence.
    public bool Tick(long engineFrame, Field[] fields, string localIdentity)
    {
        lock(_sync)
        {
            if(_stage is not (Stage.CaptureA or Stage.CaptureB))return false;
            if(!Alive())return false;
            // Count callbacks and retain only whitelist status values before any failure/reset.
            // The supplied samples were read on the game thread. Poll never reads live objects.
            _callbacks++;
            var safeFields=FieldNames.Select(name=>new Field(name,
                fields.FirstOrDefault(x=>x.Name==name) is { } value && Enum.IsDefined(value.Status)?value.Status:FieldStatus.Missing)).ToArray();
            _lastSample=new(engineFrame,_clock.Milliseconds,safeFields,
                safeFields.FirstOrDefault(x=>x.Status!=FieldStatus.True)?.Name,
                safeFields.FirstOrDefault(x=>x.Status==FieldStatus.Exception)?.Name,_consecutive,_consecutive,!string.IsNullOrWhiteSpace(localIdentity));
            if(fields.Length!=20 || fields.Select(x=>x.Name).Distinct().Count()!=20 || !fields.Select(x=>x.Name).Order().SequenceEqual(FieldNames.Order()))
            { FailLocked("readiness-field-set"); return false; }
            if(fields.Any(x=>x.Status==FieldStatus.Exception)) { FailLocked("readiness-getter-exception"); return false; }
            var now=_clock.Milliseconds;
            var good=fields.All(x=>x.Status==FieldStatus.True) && !string.IsNullOrWhiteSpace(localIdentity);
            if(!good) { ResetQuiet(); _lastFrame=engineFrame; _lastTick=now; _lastSample=_lastSample with{QuietAfter=0}; return false; }
            if(_lastFrame<0 || engineFrame!=_lastFrame+1 || now-_lastTick>250 || _identity!=localIdentity) _consecutive=0;
            if(_consecutive==0) { _quietFrame=engineFrame; _quietMs=now; _maxGap=0; }
            else _maxGap=Math.Max(_maxGap,now-_lastTick);
            _identity=localIdentity; _lastFrame=engineFrame; _lastTick=now; _consecutive++;
            _lastFields=fields.Select(x=>x with{}).ToArray();
            _lastSample=_lastSample with{QuietAfter=_consecutive};
            return _consecutive==30;
        }
    }
    // Snapshot I/O occurs outside the flow lock. This commit can lose to any deadline/terminal.
    public bool Captured()
    {
        lock(_sync)
        {
            if(_stage is not (Stage.CaptureA or Stage.CaptureB) || _consecutive!=30 || !Alive())return false;
            if(!Emit("capture-30-frames") || !Alive())return false;
            var next=_stage==Stage.CaptureA?Stage.AComplete:Stage.Complete;
            if(!Emit(next==Stage.AComplete?"A-stop-input":"compare-complete") || !Alive())return false;
            _stage=next; _deadline=null; return true;
        }
    }
    public bool Poll()
    {
        lock(_sync)
        {
            if(!Alive())return false;
            var now=_clock.Milliseconds;
            if(_stage is Stage.CaptureA or Stage.CaptureB && now>=_nextHeartbeat && _heartbeatAttempts<MaxHeartbeats)
            {
                // One write at most per elapsed second, no catch-up burst or Ready-stage I/O.
                _nextHeartbeat=now+1000; _heartbeatAttempts++;
                if(!Emit("capture-heartbeat") || !Alive())return false;
            }
            return true;
        }
    }
    public bool Fail(string reason) { lock(_sync) return FailLocked(reason); }
    private bool Alive()
    {
        if(_stage is Stage.Terminal or Stage.Complete)return false;
        if(_deadline.HasValue && _clock.Milliseconds>=_deadline.Value) { FailLocked("deadline-"+_stage); return false; }
        return true;
    }
    private bool Emit(string name)
    {
        try { Write(new(name,_stage,_clock.Milliseconds,_lastSample?.EngineFrame,_consecutive,null,Snapshot())); return true; }
        catch { FailLocked("evidence-write-failure"); return false; }
    }
    private bool FailLocked(string reason)
    {
        if(_stage is Stage.Terminal or Stage.Complete)return false;
        _frozenTerminal=Snapshot();
        _stage=Stage.Terminal; _terminal=reason; _deadline=null;
        // First terminal wins. Do not reset or retry even when its only write fails.
        try { Write(new("terminal",_stage,_clock.Milliseconds,_lastSample?.EngineFrame,_consecutive,reason,Copy(_frozenTerminal))); } catch { }
        return true;
    }
    private void Write(Event value)
    {
        Contract.Require(System.Text.Encoding.UTF8.GetByteCount(Json.Serialize(value))<=MaxEventBytes,"diagnostic-event-size");
        _write(value);
    }
    private CaptureDiagnostics? Snapshot()=>_capturePhase.HasValue?
        new(_capturePhase.Value,_callbacks,_lastSample is null?SampleStatus.NoSample:SampleStatus.Sampled,
            CopySample(_lastSample),_consecutive,_heartbeatAttempts):null;
    private static CaptureSample? CopySample(CaptureSample? sample)=>sample is null?null:sample with{Fields=sample.Fields.Select(x=>x with{}).ToArray()};
    private static CaptureDiagnostics? Copy(CaptureDiagnostics? value)=>value is null?null:value with{LastSample=CopySample(value.LastSample)};
    private void StartCapture()
    {
        ResetQuiet(); _capturePhase=_stage; _callbacks=0; _lastSample=null; _heartbeatAttempts=0; _nextHeartbeat=_clock.Milliseconds+1000;
    }
    private void ResetQuiet() { _consecutive=0; _identity=null; _lastFrame=-1; _lastTick=-1; _lastFields=null; }
}
