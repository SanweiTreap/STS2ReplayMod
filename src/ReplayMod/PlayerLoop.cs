using System.Diagnostics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace ReplayMod;

internal static class PlayerLoop
{
    internal static bool Active,Dispatching,TearingDown,Loading,CleaningRoom;
    internal static bool Prequeuing;
    internal static int DispatchIndex=-1;
    private static Clip? _playingClip;
    internal static string? Error;
    internal static PlaybackCursor? Cursor;
    internal static SaveManager? IsolatedManager;
    internal static bool VictoryReached;
    internal static bool ChoiceAbort;
    private static int _opens;
    private static readonly Dictionary<int,DirectCheckpoint> Checkpoints=[];
    private static CombatRoster? _roster;
    internal static CombatRoster EnsureRoster()=>_roster??=new(((CombatRoom)LiveState.Run.CurrentRoom!).CombatState);
    private static StateGraph? _fileBindings;
    internal static bool InjectOnContinue;
    internal static bool CanInject=>Active&&!Loading&&!TearingDown&&Cursor is {Position:1,Busy:false,Playing:false,StopRequested:false}&&_fileBindings is not null;
    internal static string? BackDisabledReason=>ChoiceTape.Controllable?"选择尚未完成；可Step／Play／Stop":VictoryReached?"战斗结束后暂不能后退":
        Cursor is {Position:>0} cursor?(Checkpoints.TryGetValue(cursor.Position-1,out var checkpoint)?checkpoint.BlockReason():"此位置没有可恢复稳定点"):null;

    internal static void Control(string control)
    {
        var cursor=Cursor;if(!Active||cursor is null||TearingDown)return;
        if(NativeSeek.Enabled){NativeSeek.Control(control);return;}
        if(DecisionRoute.Enabled){DecisionRoute.Control(control);return;}
        var from=cursor.Position;
        var accepted=false;
        if(control=="stop"&&!cursor.StopRequested)
        {
            ChoiceAbort=ChoiceTape.Controllable;cursor.Stop();accepted=true;
            if(ChoiceAbort&&ChoiceTape.Scope is {} choice)NativeChoice.Cancel(choice);
        }
        else if(ChoiceTape.Controllable)accepted=control switch {"toggle"=>cursor.Toggle(),"step"=>cursor.StepChoice(),_=>false};
        else if(!Loading)accepted=control switch
        {
            "toggle"=>cursor.Toggle(),"step"=>cursor.Step(),"back"=>BackDisabledReason is null&&cursor.Rewind(),_=>false
        };
        // Stop is accepted during loading/restoring, then honored at the next safe boundary.
        Runtime.Log("playback-control",new{Control=control,Accepted=accepted,Position=from,cursor.RewindTarget,cursor.Playing,cursor.Busy,Loading,cursor.StopRequested});
        if(!accepted)return;
        Runtime.Status=control switch
        {
            "back"=>$"恢复中：{from} → {cursor.RewindTarget}，到达后暂停",
            "step"=>ChoiceTape.Controllable?"Step：推进一个选择动作":"Step：等待当前操作或选择界面",
            "stop"=>ChoiceAbort?"Stop：取消选择等待并返回菜单":"已请求 Stop：安全边界后返回主菜单",
            _=>cursor.Playing?"播放中":"暂停：在选择动作或完整操作边界停住"
        };
    }

    internal static async Task Open(string id,bool fromFile=false,bool nativeSeek=true,string entry="recent")
    {
        if(Active)return;
        if(fromFile){nativeSeek=false;entry="state-file";}
        Active=true;TearingDown=false;Loading=true;VictoryReached=false;ChoiceAbort=false;IsolatedManager=null;Error=null;Cursor=null;Checkpoints.Clear();_roster=null;_fileBindings=null;InjectOnContinue=false;Prequeued.Clear();Committed.Clear();
        MemorySession? memory=null;var checks=new List<string>();var restored=false;var complete=false;var started=Stopwatch.GetTimestamp();
        try
        {
            Runtime.CheckEnvironment();Codec.Require(RunManager.Instance.DebugOnlyGetState() is null,"open-from-main-menu-only");
            var clip=Runtime.ReadClip(id);_playingClip=clip;LiveState.Format=clip.Format;Cursor=new(clip.Operations.Length);
            if(nativeSeek){DecisionRoute.Clear();NativeSeek.Start(clip);}else DecisionRoute.Start(clip);
            Runtime.Status="加载回放：独立内存会话";Ui.Playback();ActionMonitor.Reset();ChoiceTape.Play(clip);
            if(SaveManager.Instance.CurrentRunSaveTask is {} saving)await saving;
            memory=new();memory.Install();IsolatedManager=memory.Isolated;
            await LoadRoom(clip);checks.Add("initial");
            if(nativeSeek)
            {
                Runtime.Log("native-playback-open",new{clip.Id,clip.Format,Entry=entry,Core="native-actions",Open=++_opens,
                    TotalOperations=clip.Operations.Length,ExpectedVictory=clip.Operations[^1].Victory,InitialMatched=true,
                    InitialChoices=(clip.Choices??[]).Count(c=>c.Boundary==0),FixedClipIdRequired=false,DisplayTracePlayed=false});
                await NativeSeek.Run();complete=NativeSeek.AllMatched;Cursor.Stop();checks.Add("native-complete-single-battle");return;
            }
            if(clip.Authority is not null)_fileBindings=RecordedState.Bind(_roster!);
            if(fromFile)
            {
                Codec.Require(clip.Authority is not null,"clip-has-no-authority-state");
                await RecordedState.Apply(clip,_fileBindings!,_roster!,"cold-open");
                Cursor.ArriveFromFile(1);checks.Add("authority-cold-arrival-1");await CaptureCheckpoint(1,clip.Operations[0].After);
            }
            else if(DecisionRoute.Enabled)await DecisionRoute.Stable(0,false);
            else await CaptureCheckpoint(0,clip.Initial);
            Loading=false;if(!DecisionRoute.Enabled)Runtime.Status=$"回放已暂停：{Cursor.Position}/{clip.Operations.Length}。"+(fromFile?"已从文件直达第1步；可启用扰动后播放":"Step执行一个完整操作");
            Runtime.Log("playback-open",new{clip.Id,clip.Format,Open=++_opens,Count=clip.Operations.Length,ExpectedVictory=clip.Operations[^1].Victory,InitialMatched=true,FromRecordedState=fromFile,Position=Cursor.Position,PrefixOperations=0,Runtime.ProcessId,Runtime.ProcessStartedUtc,RecordedModHash=clip.ModHash,PlaybackModHash=Runtime.ModHash,BuildRule=clip.ModHash==Runtime.ModHash?"exact":"explicit-known-format-"+clip.Format+"-recording-build"});
            while(!Cursor.StopRequested)
            {
                ThrowIfError();
                if(Cursor.Rebuilding)
                {
                    complete=false;await Rewind(clip);complete=!Cursor.Rebuilding&&Cursor.Finished;continue;
                }
                // A native lookahead already consumed its user request. Enter its bookkeeping
                // host without requesting another Step or dispatching the card a second time.
                var submitted=Committed.Contains(Cursor.Position);
                if(!DecisionRoute.CanExecute||!Cursor.TryBegin(alreadySubmitted:submitted)){await Runtime.NextFrame();continue;}
                if(submitted)Runtime.Log("playback-committed-host-entered",new{Sequence=Cursor.Position+1,UserRequestConsumedAgain=false,NativeDispatchRepeated=false});
                var index=Cursor.Position;
                if(!await Execute(clip,index,"forward"))break;VictoryReached=clip.Operations[index].Victory;
                if(!VictoryReached&&!DecisionRoute.Enabled)await CaptureCheckpoint(index+1,clip.Operations[index].After);
                checks.Add("operation-"+(index+1));Cursor.Complete();
                complete=Cursor.Finished;if(!DecisionRoute.Enabled)ShowPosition(clip);
                if(DecisionRoute.Enabled)await DecisionRoute.Stable(Cursor.Position,VictoryReached);
                if(VictoryReached)Runtime.Log("playback-victory-ended",new{clip.Id,Position=Cursor.Position,complete,StateMatched=true,NativeVictoryTaskSucceeded=BattleEnd.VictoryTask?.IsCompletedSuccessfully==true,RewardsAllowed=false,BackEnabled=DecisionRoute.Enabled});
            }
        }
        catch(OperationCanceledException) when(ChoiceAbort&&Cursor?.StopRequested==true)
        {
            Runtime.Log("playback-choice-stopped",new{Position=Cursor.Position,Loading,AtStableBoundary=false,RequestedByViewer=true});
        }
        catch(Exception ex)
        {
            Error=ex is InvalidDataException?ex.Message:ex.GetType().Name;Runtime.Status="回放失败："+Error;
            Runtime.Log("playback-failed",new{Reason=Error,Checks=checks});
        }
        finally
        {
            if(DecisionRoute.Enabled)
            {
                if(!Cursor!.StopRequested)DecisionRoute.Control("stop");
                try{await DecisionRoute.StopTask;}catch(Exception ex){Runtime.Log("route-stop-cleanup-failed",new{Reason=ex.GetBaseException().Message});}
            }
            TearingDown=true;Dispatching=false;Loading=false;
            if(ChoiceTape.Scope is {} choice)
                try{NativeChoice.Cancel(choice);}catch(Exception ex){Error="choice-cleanup-"+ex.GetBaseException().GetType().Name;Runtime.Log("choice-cleanup-failed",new{Reason=Error});}
            try
            {
                if(memory is not null){await ClearRoom(memory);restored=memory.Restore();Codec.Require(restored,"normal-Continue-or-original-progress-changed");}
                var load=(Task)AccessTools.Method(typeof(NGame),"LoadMainMenu",[typeof(bool)]).Invoke(NGame.Instance,[false])!;await load;
                if(Error is null)Runtime.Status=complete?"回放完成并退出。正常Continue保留，可再次打开":"回放已停止。正常Continue保留，可再次打开";
            }
            catch(Exception ex){Error="teardown-"+ex.GetType().Name;Runtime.Status="退出回放异常："+Error;Runtime.Log("teardown-failed",new{Reason=Error});}
            finally
            {
                // Restore even when scene cleanup fails, while reporting the failure rather than claiming isolation passed.
                if(memory is not null&&!ReferenceEquals(SaveManager.Instance,memory.Original))try{restored=memory.Restore();}catch{restored=false;}
                Runtime.Log("playback-closed",new{Id=id,Entry=entry,Core=NativeSeek.Enabled?"native-actions":nativeSeek?"not-started":"legacy-tool",TotalOperations=_playingClip?.Operations.Length,
                    Checks=checks,Position=NativeSeek.Enabled?(int?)NativeSeek.Position:Cursor?.Rebuilding==true?(int?)null:Cursor?.Position,AllOperationsMatched=complete,VictoryReached,Error,NativeSeek=NativeSeek.Enabled,OriginalManagerRestored=memory is not null&&ReferenceEquals(SaveManager.Instance,memory.Original),NormalContinueAndOriginalProgressUnchanged=restored,OriginalProgressFileIncluded=true,WallMilliseconds=Stopwatch.GetElapsedTime(started).TotalMilliseconds});
                Active=false;TearingDown=false;ChoiceAbort=false;Prequeuing=false;DispatchIndex=-1;_playingClip=null;Prequeued.Clear();Committed.Clear();IsolatedManager=null;Ui.ClosePlayback();ActionMonitor.Reset();ChoiceTape.Clear();NativeSeek.Clear();DecisionRoute.Clear();Checkpoints.Clear();_roster=null;_fileBindings=null;InjectOnContinue=false;
                await Runtime.FlushLog();
            }
        }
    }

    private static async Task LoadRoom(Clip clip)
    {
        Runtime.Log("playback-room-load",new{clip.Id,Reason="open",Position=0});
        ActionMonitor.Reset();
        var parsed=JsonSerializationUtility.FromJson<SerializableRun>(clip.Checkpoint);
        Codec.Require(parsed.Success&&parsed.SaveData is not null,"checkpoint-parse");var save=parsed.SaveData!;
        Codec.Require(save.Players.Count==1&&save.Players[0].CharacterId?.ToString()==clip.Character,"checkpoint-character-mismatch");
        var state=RunState.FromSerializable(save);
        Codec.Require(state.Players.Count==1&&state.Players[0].Character.Id.ToString()==clip.Character,"rebuilt-character-mismatch");
        await RunManager.Instance.SetUpSavedSingleplayer(state,save);
        AccessTools.Property(typeof(RunManager),nameof(RunManager.ShouldSave)).SetValue(RunManager.Instance,false);
        RunManager.Instance.CombatStateSynchronizer.IsDisabled=true;RunManager.Instance.CombatReplayWriter.IsEnabled=false;
        await PreloadManager.LoadRunAssets(state.Players.Select(p=>p.Character));await PreloadManager.LoadActAssets(state.Act);
        RunManager.Instance.Launch();NAudioManager.Instance?.StopMusic();Runtime.Game.RootSceneContainer.SetCurrentScene(NRun.Create(state));
        await RunManager.Instance.GenerateMap();
        await RunManager.Instance.LoadIntoLatestMapCoord(AbstractRoom.FromSerializable(save.PreFinishedRoom,state));
        await WaitReady();LiveState.Supported();ChoiceTape.AssertBoundary(0);Check("initial",clip.Initial,LiveState.Capture());BattleEnd.Begin();
        Runtime.Log("playback-character-verified",new{Character=LiveState.Player.Character.Id.ToString(),RecordedCharacter=clip.Character,
            Relics=LiveState.Player.Relics.Select(r=>r.Id.ToString()).ToArray(),NativeCharacterAssets=true,Authority=clip.Authority});
        EnsureRoster();
        if(DecisionRoute.Enabled||NativeSeek.Enabled)await NativeDisplayTrace.WaitForOpeningPresentation();
    }

    private static async Task ClearRoom(MemorySession memory)
    {
        await memory.Drain();
        CleaningRoom=true;
        try
        {
            if(RunManager.Instance.DebugOnlyGetState() is not null)RunManager.Instance.CleanUp(true);
            // Remove the old NRun before yielding: its _Process uses RunManager's current net service.
            Runtime.Game.RootSceneContainer.SetCurrentScene(new Control{Name="ReplayModRebuilding"});
        }
        finally{CleaningRoom=false;}
        await Runtime.NextFrame();await memory.Drain();ActionMonitor.Reset();
    }

    private static async Task CaptureCheckpoint(int position,string expected)
    {
        // Drain the normal deferred state/UI notification before taking the data memento.
        await Runtime.NextFrame();await WaitReady();Check("checkpoint-"+position,expected,LiveState.Capture());
        var unavailable=_roster!.Reason(((CombatRoom)LiveState.Run.CurrentRoom!).CombatState);
        if(unavailable is not null||LiveState.Player.PlayerCombatState!.Pets.Count>0)
        {
            Checkpoints.Remove(position);Runtime.Log("checkpoint-unavailable",new{Position=position,Reason=unavailable??"pet-not-restorable",ForwardPlaybackContinues=true});return;
        }
        var at=Stopwatch.GetTimestamp();var checkpoint=new DirectCheckpoint(position,_roster);Checkpoints[position]=checkpoint;
        checkpoint.LogCapture(Stopwatch.GetElapsedTime(at).TotalMilliseconds);
    }

    private static async Task Rewind(Clip clip)
    {
        var cursor=Cursor!;var from=cursor.Position;var target=cursor.RewindTarget!.Value;
        var started=Stopwatch.GetTimestamp();
        Codec.Require(BackDisabledReason is null,"direct-back-disabled-boundary");
        Runtime.Log("checkpoint-restore-start",new{clip.Id,From=from,Target=target,RoomRebuilt=false,PrefixOperations=0});
        try
        {
            Codec.Require(Checkpoints.TryGetValue(target,out var checkpoint),"direct-back-checkpoint-missing");
            await checkpoint!.Restore();ThrowIfError();
            Check("direct-back-arrival-"+target,target==0?clip.Initial:clip.Operations[target-1].After,LiveState.Capture());
            if(cursor.StopRequested)
            {
                Runtime.Log("checkpoint-restore-stopped",new{clip.Id,From=from,Target=target,Arrived=target,StateMatched=true,RoomRebuilt=false,PrefixOperations=0,Milliseconds=Stopwatch.GetElapsedTime(started).TotalMilliseconds});return;
            }
            HandPresentation.FinishHighlights(MegaCrit.Sts2.Core.Nodes.Combat.NPlayerHand.Instance!);
            cursor.CompleteRewind(target);ShowPosition(clip);
            Runtime.Log("checkpoint-restore-arrived",new{clip.Id,From=from,Target=target,Arrived=cursor.Position,CheckpointTurn=checkpoint.Turn,StateMatched=true,cursor.Playing,RoomRebuilt=false,PrefixOperations=0,Milliseconds=Stopwatch.GetElapsedTime(started).TotalMilliseconds});
        }
        catch(Exception ex)
        {
            Runtime.Log("checkpoint-restore-failed",new{clip.Id,From=from,Target=target,Reason=ex.Message,RoomRebuilt=false,PrefixOperations=0,Milliseconds=Stopwatch.GetElapsedTime(started).TotalMilliseconds});throw;
        }
    }

    internal static async Task<bool> Execute(Clip clip,int index,string mode)
    {
        var op=clip.Operations[index];
        if(ActionMonitor.Submitted(index) is {} submitted)return await AwaitSubmitted(clip,index,submitted,mode);
        var preparing=Stopwatch.GetTimestamp();
        await WaitReady();
        var ready=Stopwatch.GetTimestamp();
        if(index==1&&InjectOnContinue){InjectOnContinue=false;RecordedState.Perturb();}
        await EnsureState(clip,index);
        var verified=Stopwatch.GetTimestamp();
        if(Cursor!.StopRequested)return false;
        ChoiceTape.Boundary(op.Sequence);
        Codec.Require(op.Turn==LiveState.Turn,"operation-turn-mismatch");Dispatching=true;DispatchIndex=index;
        try
        {
            if(op.Kind==OperationKind.Card)
            {
                var hand=LiveState.Player.PlayerCombatState!.Hand.Cards;Codec.Require(op.HandIndex is {} n&&n>=0&&n<hand.Count,"recorded-hand-index");
                var card=hand[op.HandIndex!.Value];Codec.Require(card.Id.ToString()==op.Card,"recorded-card-content");
                if(clip.Format>=3)Codec.Require(LiveState.CardIdentity(card)==op.CardState,"recorded-card-instance-upgrade-or-state");
                Codec.Require(card.TryManualPlay(LiveState.Resolve(op.Target)),"normal-play-rejected");
            }
            else if(op.Kind==OperationKind.Potion)
            {
                var key=op.Potion??throw new InvalidDataException("recorded-potion-key-missing");
                var potion=LiveState.Player.GetPotionAtSlotIndex(key.Slot)??throw new InvalidDataException("recorded-potion-slot-empty");
                Codec.Require(LiveState.PotionIdentity(potion,key.Slot)==key,"recorded-potion-content-or-state");
                Codec.Require(!potion.IsQueued&&!potion.HasBeenRemovedFromState&&potion.PassesCustomUsabilityCheck&&LiveState.Player.CanUseOrRemovePotions,"recorded-potion-not-usable");
                var target=LiveState.Resolve(op.Target);Codec.Require(potion.IsValidTarget(target),"recorded-potion-invalid-target");potion.EnqueueManualUse(target);
            }
            else RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(new EndPlayerTurnAction(LiveState.Player,op.Turn));
        }
        finally{Dispatching=false;DispatchIndex=-1;}
        Runtime.Log("native-operation-dispatch-timing",new{op.Sequence,op.Card,Kind=op.Kind.ToString(),
            ReadyMilliseconds=Stopwatch.GetElapsedTime(preparing,ready).TotalMilliseconds,StateCheckMilliseconds=Stopwatch.GetElapsedTime(ready,verified).TotalMilliseconds,
            NativeEnqueueMilliseconds=Stopwatch.GetElapsedTime(verified).TotalMilliseconds});
        var pending=ActionMonitor.Submitted(index)??throw new InvalidDataException("dispatch-not-observed");
        return await AwaitSubmitted(clip,index,pending,mode);
    }
    internal static void SubmitOperation(Operation op,int index,CardModel? card)
    {
        Dispatching=true;DispatchIndex=index;Prequeuing=true;
        try
        {
            if(op.Kind==OperationKind.Card)Codec.Require(card!.TryManualPlay(LiveState.Resolve(op.Target)),"operation-native-play-rejected");
            else if(op.Kind==OperationKind.Potion)
            {
                var key=op.Potion!;var potion=LiveState.Player.GetPotionAtSlotIndex(key.Slot)??throw new InvalidDataException("operation-potion-missing");
                Codec.Require(LiveState.PotionIdentity(potion,key.Slot)==key,"operation-potion-identity");potion.EnqueueManualUse(LiveState.Resolve(op.Target));
            }
            else RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(new EndPlayerTurnAction(LiveState.Player,op.Turn));
        }
        finally{Prequeuing=false;Dispatching=false;DispatchIndex=-1;}
    }
    private static async Task<bool> AwaitSubmitted(Clip clip,int index,PendingOperation pending,string mode)
    {
        var op=clip.Operations[index];
        while(!pending.Delivery.Task.IsCompleted){ThrowIfError();await Runtime.NextFrame();}
        ThrowIfError();var completed=await pending.Delivery.Task;
        Codec.Require(completed.Victory==op.Victory,"operation-victory-result-mismatch");
        Check("after-"+op.Sequence,op.After,completed.After);
        Runtime.Log("playback-step",new{clip.Id,op.Sequence,Kind=op.Kind.ToString(),Mode=mode,Position=index+1,StateMatched=true,Victory=completed.Victory,
            Target=op.Target,op.CardState,op.Potion,Creatures=CombatRoster.Summary(((CombatRoom)LiveState.Run.CurrentRoom!).CombatState.Creatures)});
        return true;
    }
    internal static void VerifySubmittedStart(PendingOperation pending)
    {
        if(!Prequeued.Remove(pending.PlaybackIndex))return;
        var clip=_playingClip!;var index=pending.PlaybackIndex;var op=clip.Operations[index];
        Check("queued-before-"+op.Sequence,index==0?clip.Initial:clip.Operations[index-1].After,LiveState.Capture());
        Codec.Require(pending.Template.HandIndex==op.HandIndex&&pending.Template.CardState==op.CardState&&LiveState.Turn==op.Turn,"queued-execution-input-mismatch");
        ChoiceTape.Boundary(op.Sequence);
        Runtime.Log("queued-native-input-verified",new{op.Sequence,op.Card,InstanceMatched=true,PriorResultMatched=true});
    }
    private static readonly HashSet<int> Prequeued=[];
    private static readonly HashSet<int> Committed=[];
    internal static bool TryQueueNext()
    {
        if(!Active||TearingDown||_playingClip is not {Format:>=6} clip||Cursor?.StopRequested!=false||
            ActionMonitor.Pending is not {Card:{} current,ActionTask:{IsCompleted:false}} pending||!LiveState.InputPhase||ChoiceTape.Waiting)return false;
        var index=pending.PlaybackIndex+1;
        if(index<=0||index>=clip.Operations.Length||ActionMonitor.Submitted(index) is not null)return false;
        var previous=clip.Operations[index-1];var next=clip.Operations[index];
        if(previous.Victory||next.Victory||next.Kind!=OperationKind.Card||next.Turn!=previous.Turn||
            clip.Choices!.Any(c=>c.Boundary==previous.Sequence||c.Boundary==next.Sequence))return false;
        var card=LiveState.Player.PlayerCombatState!.Hand.Cards.FirstOrDefault(c=>LiveState.CardIdentity(c).CombatIndex==next.CardState!.CombatIndex);
        // First slice: nonlethal, no-choice self skills. Eligibility uses native card category and
        // recorded boundaries, never a card-name timer or a second execution of the business.
        if(card is null||current.Type!=MegaCrit.Sts2.Core.Entities.Cards.CardType.Skill||card.Type!=MegaCrit.Sts2.Core.Entities.Cards.CardType.Skill||
            current.TargetType is not (MegaCrit.Sts2.Core.Entities.Cards.TargetType.Self or MegaCrit.Sts2.Core.Entities.Cards.TargetType.None)||
            card.TargetType is not (MegaCrit.Sts2.Core.Entities.Cards.TargetType.Self or MegaCrit.Sts2.Core.Entities.Cards.TargetType.None)||
            card.Id.ToString()!=next.Card||LiveState.CardIdentity(card)!=next.CardState)return false;
        var target=LiveState.Resolve(next.Target);
        if(!card.CanPlayTargeting(target))return false;
        Prequeuing=true;Dispatching=true;DispatchIndex=index;Prequeued.Add(index);Committed.Add(index);
        try
        {
            Codec.Require(card.TryManualPlay(target)&&ActionMonitor.Submitted(index) is not null,"queued-native-play-rejected");
            Runtime.Log("playback-native-input-prequeued",new{next.Sequence,next.Card,WhileAction=pending.Template.Card,
                EarlierOriginalActionTaskIncomplete=!pending.ActionTask.IsCompleted,AcceptedByOriginalQueue=true});return true;
        }
        finally{Prequeuing=false;Dispatching=false;DispatchIndex=-1;}
    }
    internal static async Task DrainCommitted()
    {
        var started=Stopwatch.GetTimestamp();
        while(Committed.Any(i=>ActionMonitor.Submitted(i)?.Delivery.Task.IsCompleted!=true))
        {
            Codec.Require(Stopwatch.GetElapsedTime(started).TotalSeconds<60,"committed-queue-did-not-drain");
            ThrowIfError();await Runtime.NextFrame();
        }
        foreach(var index in Committed)
        {
            var actual=await ActionMonitor.Submitted(index)!.Delivery.Task;var expected=_playingClip!.Operations[index];
            Codec.Require(actual.Victory==expected.Victory,"committed-drain-victory-mismatch");
            Check("committed-drain-"+expected.Sequence,expected.After,actual.After);
            Runtime.Log("playback-committed-drained",new{expected.Sequence,StateMatched=true,NativeDispatchRepeated=false});
        }
        Prequeued.Clear();Committed.Clear();
    }

    private static void ShowPosition(Clip clip)
    {
        var cursor=Cursor!;
        var suffix=BackDisabledReason is {} reason?"。"+reason:"";
        Runtime.Status=VictoryReached?$"战斗胜利／回放结束：{cursor.Position}/{clip.Operations.Length}，结果一致。Stop返回菜单；终点后退暂不支持":
            cursor.Finished?$"片段完成：{cursor.Position}/{clip.Operations.Length}，状态一致。Stop退出{suffix}":$"回放 {(cursor.Playing?"播放":"暂停")}：{cursor.Position}/{clip.Operations.Length}，状态一致{suffix}";
    }
    private static void ThrowIfError(){if(ChoiceAbort)throw new OperationCanceledException();if(Error is not null||ActionMonitor.Error is not null)throw new InvalidDataException(Error??ActionMonitor.Error);}
    internal static void RequireHealthy()=>ThrowIfError();
    private static async Task WaitReady()
    {
        var at=Stopwatch.GetTimestamp();var choiceAtStart=ChoiceTape.WaitingSeconds;
        while(!LiveState.Playable||ActionMonitor.StartTurnTask?.IsCompletedSuccessfully!=true)
        {
            Codec.Require(Stopwatch.GetElapsedTime(at).TotalSeconds-(ChoiceTape.WaitingSeconds-choiceAtStart)<60,"playback-room-not-ready");
            ThrowIfError();Codec.Require(ActionMonitor.StartTurnTask is not {IsFaulted:true} and not {IsCanceled:true},"start-turn-failed");
            await Runtime.NextFrame();
        }
        ThrowIfError();
    }
    private static void Check(string point,string expected,string actual)
    {
        if(expected==actual)return;
        Runtime.Log("state-difference",new{Point=point,Expected=expected,Actual=actual});throw new InvalidDataException("state-mismatch-"+point);
    }
    private static async Task EnsureState(Clip clip,int position)
    {
        var expected=position==0?clip.Initial:clip.Operations[position-1].After;var actual=LiveState.Capture();
        if(expected==actual)return;
        Runtime.Log("state-difference",new{Point="before-"+(position+1),Position=position,Expected=expected,Actual=actual,CanRecover=position==1&&_fileBindings is not null});
        Codec.Require(position==1&&_fileBindings is not null,"state-mismatch-no-supported-authority-point");
        var cursor=Cursor!;var playing=cursor.Playing;
        await RecordedState.Apply(clip,_fileBindings!,_roster!,"comparison-correction");
        ThrowIfError();Check("authority-correction-"+position,expected,LiveState.Capture());
        Runtime.Log("authority-correction-verified",new{Position=position,StateMatched=true,PreviousPlaying=playing,cursor.Playing,cursor.Busy,cursor.StopRequested,
            CursorUnchanged=cursor.Position==position,WillContinueOriginalOperation=!cursor.StopRequested,FileReadAgain=true});
    }
}
