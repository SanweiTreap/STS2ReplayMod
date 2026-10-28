using System.Text.Json;
using System.Diagnostics;

namespace Perf001Candidate.Core;

public sealed class Session
{
    public const int WarmupSeconds=5,IdleSeconds=10;
    private readonly object _sync=new();private readonly IClock _clock;private readonly ILiveView _live;private readonly OwnedStore _store;
    private readonly FrameStamp[] _frames;private int _frameCount,_statusSequence,_segment;private long _observedFrames,_unrecordedFrames;
    private FrameStamp? _lastObserved;private bool _tailPending;private readonly List<object> _idleWindows=[];
    private long _idleBegin;private int _idleOrdinal;
    private readonly List<Boundary> _boundaries=[];
    private readonly List<StageSpan> _spans=[];private readonly List<SceneFacts> _scenes=[];
    private readonly List<object> _qualificationCosts=[];private readonly List<EventKind> _commonEvents=[];
    private readonly RecordWriter? _writer;private readonly Recorder? _recorder;
    private readonly long _born;private long _stageAt,_activeTicks,_lastFrame=-1,_lastStamp=-1,_lastBoundary=-1;
    private long? _deadlineAt;private long _sequence;private Task? _mapTask,_mapTravelTask,_actionTask,_effectTask,_turnTask;
    private int _playerTurnGeneration,_minimumTurnGeneration,_oldTurn;private bool _fresh,_initialStartingEntry,_mapInput,_mapEntry,_mapCommitted,_cardAccepted,_endAccepted;
    private sealed class TaskWatch(string name,Task original,long returned)
    {public string Name=name;public Task Original=original;public long Returned=returned;public long? LastPending,FirstSuccess;}
    private readonly List<TaskWatch> _taskWatches=[];
    private readonly List<object> _processCounters=[];
    public Configuration Configuration {get;}
    public Stage Stage {get;private set;}=Stage.Startup;
    public string? Failure {get;private set;}
    public WriterReport? WriterResult {get;private set;}
    public bool Terminal=>Stage is Stage.Complete or Stage.Failed;
    public int FrameCount=>_frameCount;
    public IReadOnlyList<Boundary> Boundaries=>_boundaries;
    public IReadOnlyList<SceneFacts> Scenes=>_scenes;
    public IReadOnlyList<EventKind> CommonEvents=>_commonEvents;
    public IReadOnlyList<RecorderSample> RecorderSamples=>_recorder?.Samples??Array.Empty<RecorderSample>();
    public Session(Configuration configuration,PublishedIdentity published,string namespaceRoot,IClock clock,ILiveView live,
        int frameCapacity=65536,WriterTestOptions? writerTest=null)
    {
        Configuration=JsonSerializer.Deserialize<Configuration>(Identity.Bytes(configuration),Identity.Json)!;
        Configuration.Validate();Configuration.Check(published);
        Identity.Require(frameCapacity>0&&frameCapacity<=65536,"frame-capacity");
        _clock=clock;_live=live;_born=_stageAt=clock.Now;Identity.Require(clock.Frequency>0,"clock-frequency");
        _frames=new FrameStamp[frameCapacity];_store=new(namespaceRoot,configuration.RunId);
        try
        {
            Counter("startup-before-files");
            _store.Write("configuration.json",Configuration);_store.Write("published-identity.json",published);
            if(Configuration.Mode==Mode.C2){_writer=new(_store,writerTest);_recorder=new(clock,_writer);}
            Publish();
        }
        catch(Exception ex){Fail("startup-"+ex.GetType().Name);}
    }
    public void Observe(Action action)
    {
        lock(_sync)
        {
            if(Terminal)return;
            try{action();}catch(Exception ex){FailLocked("observer-"+ex.GetType().Name+":"+ex.Message);}
        }
    }
    public void Fail(string reason){lock(_sync){if(!Terminal)FailLocked(reason);}}
    private void FailLocked(string reason)
    {
        Failure=reason;_spans.Add(new(Stage,_stageAt,_clock.Now,"terminal-failure; invalid clock retained if reversed"));Stage=Stage.Failed;_deadlineAt=null;
        try{WriterResult=_writer?.Stop(_recorder?.Expected.Count??0,false);}catch(Exception ex){Failure+=";writer-stop-"+ex.GetType().Name;}
        _mapTask=_mapTravelTask=_actionTask=_effectTask=_turnTask=null;
        try{_store.Write("failure.json",new{Stage,Failure,Writer=WriterResult,LastObservedCallback=_lastObserved,ObservedCallbacks=_observedFrames,OriginalProduction="not cancelled/replaced/suppressed"});WriteEvidence(false);Publish();}catch{/* terminal remains failed even if failure receipt cannot be written */}
    }
    private bool Active(Stage stage)=>stage is Stage.Warmup or Stage.MapIdle or Stage.Loading or Stage.CardRequest or Stage.CardEffect or Stage.EndRequest or Stage.EndTurn;
    private bool Recording(Stage stage)=>Active(stage)||stage==Stage.Draining;
    private void Append(FrameStamp frame)
    {
        Identity.Require(_frameCount<_frames.Length,"frame-array-overflow-measured-windows");
        _frames[_frameCount]=frame with{Sequence=_frameCount+1,Segment=_segment};_frameCount++;
    }
    private void SetStage(Stage value,string reason,bool publish=true)
    {
        var now=_clock.Now;Identity.Require(now>=_stageAt,"stage-clock-reversed");
        if(Stage==Stage.MapIdle)_idleWindows.Add(new{Ordinal=_idleOrdinal,BeginTick=_idleBegin,EndTick=now,
            Completed=value==Stage.ReadyMapNode,Contaminated=value!=Stage.ReadyMapNode,Reason=reason});
        if(value==Stage.MapIdle){_idleOrdinal++;_idleBegin=now;}
        if(!Recording(Stage)&&Recording(value))
        {
            _segment++;_tailPending=false;
            if(_lastObserved.HasValue)Append(_lastObserved.Value); // real most recent callback, never a fabricated frame
        }
        else if(Recording(Stage)&&!Recording(value))_tailPending=true;
        _spans.Add(new(Stage,_stageAt,now,reason));if(Active(Stage))_activeTicks+=now-_stageAt;
        Counter("exit-"+Stage+"-enter-"+value);Stage=value;_stageAt=now;if(publish)Publish();
    }
    private void Counter(string at)
    {
        Identity.Require(_processCounters.Count<64,"process-counter-bound");
        long? cpu=null;try{using var process=Process.GetCurrentProcess();cpu=process.TotalProcessorTime.Ticks;}catch{/* genuinely unavailable */}
        _processCounters.Add(new{At=at,Tick=_clock.Now,Thread=Environment.CurrentManagedThreadId,
            CurrentThreadCumulativeAllocatedBytes=GC.GetAllocatedBytesForCurrentThread(),ProcessCumulativeAllocatedBytes=GC.GetTotalAllocatedBytes(true),
            GcCollections=new[]{GC.CollectionCount(0),GC.CollectionCount(1),GC.CollectionCount(2)},ProcessCumulativeCpuTimeSpanTicks=cpu,
            CpuUnavailable=cpu is null?"process counter unavailable":null,
            Scope="Common boundary snapshots include observer allocation/CPU and writer/other process threads. CurrentThread is not whole async-action allocation; do not sum overlapping walls."});
    }
    private void Publish(){Identity.Require(_statusSequence<40,"status-bound");_store.Status(Stage,Failure,++_statusSequence);}
    private void Mark(string name,string semantics="observed-on-main-thread")
    {
        var now=_clock.Now;Identity.Require(now>=_lastBoundary&&_boundaries.Count<64,"boundary-clock-or-capacity");
        _lastBoundary=now;_boundaries.Add(new(++_sequence,name,now,Stage,semantics));
    }
    private void CommonEvent(EventKind kind)
    {
        var order=Enum.GetValues<EventKind>();Identity.Require(_commonEvents.Count<order.Length&&order[_commonEvents.Count]==kind,"common-event-order");
        _commonEvents.Add(kind);Mark(kind.ToString());
    }
    public void PollDeadlines()=>Observe(()=>
    {
        if(Stage==Stage.Startup&&_clock.Now-_born>5*_clock.Frequency)throw new TimeoutException("no-first-frame");
        if(_deadlineAt.HasValue&&_clock.Now>_deadlineAt.Value)throw new TimeoutException("active-action-deadline");
        var active=_activeTicks+(Active(Stage)?_clock.Now-_stageAt:0);
        Identity.Require(active<=120*_clock.Frequency,"active-observation-budget");
        Identity.Require(_writer?.Failure is null,"writer-failed");
    });
    public void NewRun(string gameMode,string seed,bool shouldSave,int players,int modifiers,int ascension)=>Observe(()=>
    {
        Identity.Require(Stage==Stage.AwaitFreshCustom&&!_fresh,"fresh-new-run-only");
        Identity.Require(gameMode=="Custom"&&seed==Configuration.Seed&&shouldSave&&players==1&&modifiers==0&&ascension==0,"new-custom-origin");
        _fresh=true;Mark("fresh-custom-normal-save");SetStage(Stage.AwaitMap,"fresh-custom");
    });
    public void ContinueObserved()=>Fail("saved-continue-not-fresh");
    public void InitialStartingMapObserved()=>Observe(()=>
    {
        Identity.Require(Stage==Stage.AwaitMap&&_fresh&&!_initialStartingEntry,"unexpected-initial-starting-entry");
        _initialStartingEntry=true;Mark("normal-initial-starting-coordinate-not-measured-monster");
    });
    public void Frame(long engineFrame,LightState light)=>Observe(()=>
    {
        var now=_clock.Now;_observedFrames++;
        var raw=new FrameStamp(_observedFrames,engineFrame,now,Stage,light.Focused,light.Paused);
        _lastObserved=raw;
        if(Recording(Stage)||_tailPending){Append(raw);_tailPending=false;}else _unrecordedFrames++;
        Identity.Require(engineFrame>_lastFrame&&(_observedFrames==1||engineFrame==_lastFrame+1)&&now>_lastStamp,"duplicate-reversed-or-missed-frame-stamp");
        _lastFrame=engineFrame;_lastStamp=now;
        if(Stage==Stage.Startup){Identity.Require(now-_born<=5*_clock.Frequency,"late-first-frame");Mark("first-process-frame");SetStage(Stage.AwaitFreshCustom,"first-frame");}
        if(Stage is Stage.AwaitFreshCustom or Stage.AwaitMap)
        {
            if(Stage==Stage.AwaitMap&&light.MapVisible){Qualify("map",Stage.Warmup);Mark("warmup-begin");}return;
        }
        if(Stage==Stage.AwaitWarmupFocus)
        {
            if(light.Focused&&!light.Paused){SetStage(Stage.Warmup,"focus-returned-restart-untimed-warmup");Mark("warmup-begin");}
            return;
        }
        if(Stage is Stage.Warmup or Stage.MapIdle&&(!light.Focused||light.Paused))
        {
            Mark(Stage==Stage.MapIdle?"map-idle-invalid-focus-window-retained":"untimed-warmup-interrupted-focus-or-pause");
            SetStage(Stage.AwaitWarmupFocus,"retain-interrupted-pre-action-window-wait-for-focus");return;
        }
        // Manual Ready waits are retained/tagged, but have no active-action deadline.
        // Focus/pause contamination during actual idle/loading/card/EndTurn measurement still fails.
        if(Stage is Stage.Loading or Stage.CardRequest or Stage.CardEffect or Stage.EndRequest or Stage.EndTurn)
            Identity.Require(light.Focused&&!light.Paused,"focus-or-pause-contaminated");
        PollDeadlines();if(Terminal)return;
        if(Stage==Stage.Draining){Complete();return;}
        if(Stage==Stage.Warmup&&now-_stageAt>=WarmupSeconds*_clock.Frequency){Mark("warmup-end");SetStage(Stage.MapIdle,"fixed-warmup-"+WarmupSeconds+"s");Mark("map-idle-begin");}
        else if(Stage==Stage.MapIdle&&now-_stageAt>=IdleSeconds*_clock.Frequency){Mark("map-idle-end");SetStage(Stage.ReadyMapNode,"fixed-idle-"+IdleSeconds+"s");}
        else if(Stage==Stage.Loading)
        {
            CheckTask(_mapTask,"map");CheckTask(_mapTravelTask,"map-travel");CheckTask(_turnTask,"initial-turn");
            if(_mapCommitted&&_mapTask?.IsCompletedSuccessfully==true&&_mapTravelTask?.IsCompletedSuccessfully==true&&_turnTask?.IsCompletedSuccessfully==true&&light.Playable)
            {Mark("map-load-first-observed-success-and-idle");_deadlineAt=null;Qualify("combat",Stage.ReadyDefend);}
        }
        else if(Stage is Stage.CardRequest or Stage.CardEffect)
        {
            CheckTask(_actionTask,"card-action");CheckTask(_effectTask,"card-effect");
            if(_cardAccepted&&_actionTask?.IsCompletedSuccessfully==true&&_effectTask?.IsCompletedSuccessfully==true&&light.Playable)
            {
                Mark("card-first-observed-success-and-idle","joint success/idle first observed at process-frame; task-observations preserves separate async completion bounds");
                _deadlineAt=null;CommonEvent(EventKind.CardResolved);Candidate(EventKind.CardResolved);if(Terminal)return;
                Qualify("card",Stage.ReadyEndTurn);
            }
        }
        else if(Stage is Stage.EndRequest or Stage.EndTurn)
        {
            CheckTask(_actionTask,"end-action");CheckTask(_turnTask,"next-player-turn");
            if(_endAccepted&&_actionTask?.IsCompletedSuccessfully==true&&_playerTurnGeneration>=_minimumTurnGeneration&&
                _turnTask?.IsCompletedSuccessfully==true&&light.Playable&&light.PlayerSide&&light.Turn==_oldTurn+1)
            {
                Mark("next-player-turn-first-observed-success-and-idle","original StartTurn Task success plus next turn and lightweight idle; process-frame observation bound");
                _deadlineAt=null;CommonEvent(EventKind.NextPlayerTurnReady);Candidate(EventKind.NextPlayerTurnReady);if(Terminal)return;
                Qualify("turn",Stage.Draining);BeginDrain();
            }
        }
    });
    private void CheckTask(Task? task,string name)
    {
        // Do not await, wrap, replace, cancel or read Exception (which would mark production fault observed).
        Identity.Require(task is null||!task.IsFaulted&&!task.IsCanceled,name+"-original-task-fault-or-cancel");
        if(task is null)return;
        var key=name is "initial-turn" or "next-player-turn"?"player-turn-"+_playerTurnGeneration:name;
        var watch=_taskWatches.LastOrDefault(x=>x.Name==key&&ReferenceEquals(x.Original,task));
        Identity.Require(watch is not null,"original-task-not-bound");
        if(!task.IsCompleted)watch!.LastPending=_clock.Now;
        else if(task.IsCompletedSuccessfully&&watch!.FirstSuccess is null)
        {watch.FirstSuccess=_clock.Now;Mark(watch.Name+"-first-success-observed","original Task; lower completion bound is last observed pending if any, otherwise unknown; upper bound is this process-frame poll");}
    }
    private void BindTask(string name,Task task)
    {Identity.Require(_taskWatches.Count<8,"task-watch-bound");_taskWatches.Add(new(name,task,_clock.Now));}
    private void Qualify(string point,Stage next)
    {
        Mark(point+"-raw-window-end-before-qualification");SetStage(Stage.Validation,point+"-validation-begin");
        var at=_clock.Now;var allocation=GC.GetAllocatedBytesForCurrentThread();
        var facts=_live.Qualify(point).Detached();Identity.Require(facts.Point==point,"qualification-point");facts.Validate(Configuration);
        _scenes.Add(facts);_store.Write("scene-"+point+".json",facts);
        _qualificationCosts.Add(new{Point=point,Ticks=_clock.Now-at,SyncThreadAllocatedBytes=GC.GetAllocatedBytesForCurrentThread()-allocation});
        Mark(point+"-qualification-end");SetStage(next,point+"-qualified");
    }
    public void MapInputRequested(bool expectedDirectOrdinaryMonster)=>Observe(()=>
    {
        Identity.Require(Stage==Stage.ReadyMapNode&&_fresh&&!_mapInput&&expectedDirectOrdinaryMonster,"map-request-qualification-or-early-input");
        _mapInput=true;Mark("map-vote-input-request");_deadlineAt=_clock.Now+20*_clock.Frequency;SetStage(Stage.Loading,"map-vote-to-room-loading");
    });
    public void MapMoveQueued(bool expectedTarget)=>Observe(()=>
    {Identity.Require(Stage==Stage.Loading&&_mapInput&&expectedTarget,"map-host-move-origin");Mark("normal-map-host-move-queued");});
    public void MapRequested(bool expectedDirectOrdinaryMonster)=>Observe(()=>
    {Identity.Require(Stage==Stage.Loading&&_mapInput&&!_mapEntry&&expectedDirectOrdinaryMonster,"map-enter-without-valid-input");_mapEntry=true;Mark("map-production-enter");});
    public void MapTravelTaskReturned(Task original)=>Observe(()=>
    {Identity.Require(Stage==Stage.Loading&&_mapInput&&_mapTravelTask is null,"map-travel-task-binding");_mapTravelTask=original;BindTask("map-travel",original);Mark("original-map-travel-task-returned");});
    public void MapReturned(Task original)=>Observe(()=>
    {
        Identity.Require(Stage==Stage.Loading&&!_mapCommitted,"map-return-race");_mapTask=original;
        _mapCommitted=true;BindTask("map",original);CommonEvent(EventKind.MapCommit);
    });
    public void ActionRequested(bool card,int turn)=>Observe(()=>
    {
        Identity.Require(Stage==(card?Stage.ReadyDefend:Stage.ReadyEndTurn),"unexpected-extra-or-early-action");
        _oldTurn=turn;_actionTask=_effectTask=null;_deadlineAt=_clock.Now+20*_clock.Frequency;
        if(!card)_minimumTurnGeneration=_playerTurnGeneration+1;
        Mark(card?"card-request":"end-request");SetStage(card?Stage.CardRequest:Stage.EndRequest,"normal-action-request");
    });
    public void ActionTaskReturned(Task original)=>Observe(()=>
    {Identity.Require(Stage is Stage.CardRequest or Stage.CardEffect or Stage.EndRequest or Stage.EndTurn,"action-task-binding-race");Identity.Require(_actionTask is null,"duplicate-action-task");_actionTask=original;BindTask(Stage is Stage.CardRequest or Stage.CardEffect?"card-action":"end-action",original);Mark("original-action-task-returned-not-completed");});
    public void CardEntered(bool realDefendInHand,bool isAutoPlay)=>Observe(()=>
    {
        Identity.Require(Stage==Stage.CardRequest&&realDefendInHand&&!isAutoPlay&&!_cardAccepted,"card-not-legal-expected-defend");
        _cardAccepted=true;CommonEvent(EventKind.CardAccepted);SetStage(Stage.CardEffect,"legal-card-wrapper-entered");
    });
    public void CardTaskReturned(Task original)=>Observe(()=>
    {Identity.Require(Stage==Stage.CardEffect&&_effectTask is null,"effect-task-binding-race");_effectTask=original;BindTask("card-effect",original);Mark("original-effect-task-returned-not-completed");});
    public void EndAccepted(bool wasReady,bool nowReady,bool expectedActor)=>Observe(()=>
    {
        Identity.Require(Stage==Stage.EndRequest&&!wasReady&&nowReady&&expectedActor&&!_endAccepted,"end-acceptance-transition");
        _endAccepted=true;CommonEvent(EventKind.EndTurnAccepted);SetStage(Stage.EndTurn,"normal-ready-to-end-transition");
    });
    public int TurnStarted(bool playerSide)
    {
        var generation=0;Observe(()=>{if(playerSide)generation=++_playerTurnGeneration;});return generation;
    }
    public void TurnTaskReturned(int generation,Task original)=>Observe(()=>
    {if(generation>0){Identity.Require(generation==_playerTurnGeneration,"stale-turn-task-race");_turnTask=original;BindTask("player-turn-"+generation,original);Mark("original-player-start-turn-task-returned");}});
    public void Candidate(EventKind kind)
    {
        // C1 path is this one guard and return. No payload delegate invocation/serialization/writer.
        if(Configuration.Mode!=Mode.C2||Terminal)return;
        Observe(()=>_recorder!.Record(kind,_clock.Now,()=>_live.Capture(kind)));
    }
    private void BeginDrain()
    {
        Identity.Require(Stage==Stage.Draining&&_scenes.Count==4&&_commonEvents.SequenceEqual(Enum.GetValues<EventKind>()),"completion-missing-scenes-or-semantic-events");
        Mark("drain-begin");WriterResult=_writer?.Stop(5,true);
        Identity.Require(WriterResult is null||WriterResult.Complete,"writer-not-drained");
        if(_recorder is not null)OwnedStore.CheckEvents(_store.PathFor("events.ndjson"),_recorder.Expected);
        Mark("drain-and-integrity-end");
        // One further real frame retains the interval containing final qualification/drain work.
    }
    private void Complete()
    {
        Identity.Require(Stage==Stage.Draining,"commit-state");WriteEvidence(true);
        _store.Commit(new{Schema=1,Revision=Identity.Revision,Mode=Configuration.Mode,Stage=Stage.Complete,Terminal=true,
            Result="COMPLETE_PILOT_METHOD_ONLY_NOT_GAME_PERFORMANCE",ExpectedEvents=_commonEvents,RecordedEvents=_recorder?.Expected.Count??0,Writer=WriterResult});
        SetStage(Stage.Complete,"complete-last-atomic-commit",false);_mapTask=_mapTravelTask=_actionTask=_effectTask=_turnTask=null;
    }
    private void WriteEvidence(bool successful)
    {
        // At most once on success, or once on terminal failure. These files do not overwrite prior artifacts.
        var prefix=successful?"":"failure-";
        Counter(successful?"after-drain-before-metadata-io":"failure-after-bounded-stop");
        _store.Write(prefix+"frames.json",_frames.Take(_frameCount).ToArray());
        _store.Write(prefix+"sampling-windows.json",new{ObservedCallbacks=_observedFrames,StoredSamplingRecords=_frameCount,UnrecordedManualWaitingCallbacks=_unrecordedFrames,
            IdleWindows=_idleWindows,Rule="Raw samples only in measurement segments plus real lead/tail callback. Main-menu and manual waits retain duration spans, not an unbounded per-frame trace. Never compute intervals across Segment gaps. Interrupted idle raw data retained and explicitly ineligible; latest completed idle is separate."});
        if(successful)_store.Write("frame-statistics.json",FrameStatistics.Build(_frames.Take(_frameCount).ToArray(),_boundaries,_clock.Frequency));
        _store.Write(prefix+"boundaries.json",_boundaries);_store.Write(prefix+"spans.json",_spans);
        _store.Write(prefix+"recorder-metrics.json",new{Samples=RecorderSamples,Writer=WriterResult,Stage=Stage,Success=successful,Failure,
            StopwatchFrequency=_clock.Frequency,AllocationScope="synchronous producer/thread; separate dedicated writer; wall times overlap",
            GpuAndPresentation="Unavailable",GcPause="Unavailable",DurablePersistence="Unavailable; FileStream.Flush only"});
        _store.Write(prefix+"qualification-costs.json",_qualificationCosts);
        _store.Write(prefix+"process-counters.json",_processCounters);
        if(_recorder is not null)_store.Write(prefix+"expected-events.json",_recorder.Expected);
        _store.Write(prefix+"task-observations.json",_taskWatches.Select(w=>new{w.Name,ReturnedTick=w.Returned,LastObservedPendingTick=w.LastPending,FirstObservedSuccessTick=w.FirstSuccess,
            Semantics="No exact async finish timestamp; first main-thread observation has an upper bound, and a lower bound only if observed pending. Original Task is never replaced/awaited/cancelled."}).ToArray());
    }
    public void OwnerExit()=>Fail("owner-exit-before-complete");
}
