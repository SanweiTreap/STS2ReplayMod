using System.Diagnostics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;

namespace ReplayMod;

// The engine only executes new business effects at its frontier. Visited display nodes restore real data;
// their native UI is a fresh presentation wait, never a resurrected gameplay Task or synchronizer waiter.
internal static class DecisionRoute
{
    private sealed record Node(DirectCheckpoint Data,ChoiceScope? Choice,int Stage,int Host,bool Terminal,NativeDisplayTrace? Trace);
    private static readonly List<Node> Nodes=[];
    private static readonly HashSet<AbstractModel> Owned=[];
    private static DecisionCursor _cursor=new();
    private static ChoiceScope? _gate,_view;
    private static Task<CardModel[]>? _viewResult;
    private static Clip? _clip;
    private static Task? _work,_stop;
    private static bool _arriving;
    private static bool _arrivalTerminal;
    private static readonly PresentationNavigation Navigation=new();
    private static readonly AdvanceRequests Requests=new();
    private static readonly HashSet<int> QueuedHosts=[];
    private static long _last;
    internal static bool Enabled {get;private set;}
    internal static bool HasView=>Enabled&&_view is {Cancelled:false,Closed:false};
    internal static bool HasOverlay=>HasView&&_view!.Ui is not MegaCrit.Sts2.Core.Nodes.Combat.NPlayerHand;
    internal static bool Ready=>Enabled&&!_arriving&&_work is not {IsCompleted:false}&&_cursor.Ready;
    private static bool AcceptDuringDisplay=>Enabled&&!_cursor.Stopping&&!Navigation.Pending&&
        (NativeDisplayTrace.Replaying||NativeDisplayTrace.Settling||(_cursor.AwaitingEngine||_arriving)&&_cursor.Count>0);
    private static int IncomingPosition=>_cursor.Position+(NativeDisplayTrace.Replaying||_cursor.AwaitingEngine?1:0);
    internal static bool CanBack=>Ready&&_cursor.CanBack||AcceptDuringDisplay&&IncomingPosition>0;
    internal static bool CanStep=>Enabled&&!_cursor.Stopping&&_cursor.Count>0&&!_arrivalTerminal&&
        !(Ready&&(Current.Terminal&&_cursor.AtFrontier||_cursor.HasIntent));
    internal static bool FinishPresentation=>Navigation.InterruptsDisplay||_cursor.Stopping;
    internal static bool CanPausePresentation=>AcceptDuringDisplay;
    internal static bool NavigationPending=>Navigation.Pending;
    internal static string PendingDisplay=>Navigation.Pending?"（已接收，正在切换节点）":Requests.Count>0?$"（待前进 {Requests.Count} 步）":"";
    internal static bool Playing=>_cursor.Playing;
    internal static bool Historical=>Enabled&&!_cursor.AtFrontier;
    internal static bool CanRestore=>Enabled&&!_cursor.AwaitingEngine&&(_cursor.Moving||_cursor.Stopping);
    internal static bool CanExecute=>!Enabled||_cursor.AwaitingEngine&&_gate is null;
    internal static Task StopTask=>_stop??Task.CompletedTask;
    private static Node Current=>Nodes[_cursor.Position];
    internal static void Start(Clip clip)
    {
        Clear();Enabled=clip.Format>=4;_clip=clip;if(Enabled)NativeDisplayTrace.Begin();
    }
    internal static void Clear()
    {
        Enabled=false;NativeDisplayTrace.Clear();DisplayAssets.Clear();RegentPresentation.Clear();PotionVisuals.StopOwned();Nodes.Clear();Owned.Clear();_cursor=new();_gate=null;_view=null;_viewResult=null;_clip=null;_work=null;_stop=null;_last=0;_arriving=false;_arrivalTerminal=false;Navigation.Clear();Requests.Clear();QueuedHosts.Clear();
    }
    private static DirectCheckpoint Capture(int host,ChoiceScope? choice=null)
    {
        // Removed potions and unchosen generated cards can be held by a suspended command while absent from piles.
        foreach(var model in (choice?.Options.SelectMany(c=>c).Cast<AbstractModel>()??[])
            .Concat(new AbstractModel?[]{choice?.SourceModel,ActionMonitor.Pending?.Card,ActionMonitor.Pending?.Potion}.OfType<AbstractModel>()))Owned.Add(model);
        return new(host,PlayerLoop.EnsureRoster(),route:true,extraRoots:Owned.Cast<object>().Append(LiveState.Run.CurrentRoom!));
    }
    internal static async Task<PlayerChoiceResult> Open(ChoiceScope scope,Player player,uint id)
    {
        Codec.Require(ReferenceEquals(scope.Player,player)&&scope.Id==id&&_gate is null,"route-native-gate-context");
        _gate=scope;
        if(PlayerLoop.Cursor!.StopRequested){PlayerLoop.ChoiceAbort=true;scope.Decision.TrySetCanceled();return await scope.Decision.Task;}
        _arriving=true;
        try
        {
            await Runtime.NextFrame();
            if(_cursor.Stopping){PlayerLoop.ChoiceAbort=true;scope.Decision.TrySetCanceled();return await scope.Decision.Task;}
            await NativeDisplayTrace.WaitForChoiceEntry();
            if(_cursor.Stopping){PlayerLoop.ChoiceAbort=true;scope.Decision.TrySetCanceled();return await scope.Decision.Task;}
            var data=Capture(PlayerLoop.Cursor.Position,scope);
            Nodes.Add(new(data,scope,0,PlayerLoop.Cursor.Position,false,null));_cursor.Append();
            NativeDisplayTrace.MarkChoiceEntry();
            await Show(scope,0);
            NativeDisplayTrace.MarkChoiceReady();
            DisplayAssets.Prime(scope.Options.SelectMany(v=>v));
            var trace=await NativeDisplayTrace.Finish(settle:false);
            Nodes[^1]=Nodes[^1] with{Data=Capture(PlayerLoop.Cursor.Position,scope),Trace=trace};Publish("choice-open");
        }
        finally{_arriving=false;DrainNavigation();}
        try{return await scope.Decision.Task;}
        finally{if(ReferenceEquals(_gate,scope))_gate=null;}
    }
    internal static async Task Stable(int host,bool terminal)
    {
        if(QueuedHosts.Remove(host))return;
        if(!Enabled||_cursor.Stopping)return;_arriving=true;_arrivalTerminal=terminal;
        try
        {
            Codec.Require(_gate is null,"route-stable-with-live-choice");await Runtime.NextFrame();if(_cursor.Stopping)return;
            var beforeTail=LiveState.ChoiceProjection();var trace=await NativeDisplayTrace.Finish(settle:true,terminal:terminal);
            Codec.Require(beforeTail==LiveState.ChoiceProjection(),"native-display-tail-changed-business-or-rng");if(_cursor.Stopping)return;
            DisplayAssets.Prime(LiveState.Player.PlayerCombatState!.AllCards);
            Nodes.Add(new(Capture(host),null,0,host,terminal,trace));_cursor.Append();
            if(terminal){Requests.Clear();_cursor.Pause();}Publish(terminal?"victory":"operation-complete");
        }
        finally{_arriving=false;_arrivalTerminal=false;DrainNavigation();}
    }
    internal static void QueuedBoundary(int host)
    {
        if(!Enabled||_cursor.Stopping)return;
        Codec.Require(_gate is null&&_cursor.AwaitingEngine&&PlayerLoop.Cursor!.Position==host-1,"queued-boundary-context");
        var finish=NativeDisplayTrace.Finish(settle:false,continueCards:true);
        Codec.Require(finish.IsCompletedSuccessfully,"queued-boundary-was-not-synchronous");
        Nodes.Add(new(Capture(host),null,0,host,false,finish.Result));_cursor.Append();QueuedHosts.Add(host);
        var playing=_cursor.Playing;
        Codec.Require(_cursor.Step()&&_cursor.Take()==1,"queued-next-node-reservation");_cursor.EngineWait();
        if(playing)_cursor.Toggle();
        NativeDisplayTrace.Begin();
        Runtime.Log("queued-stable-checkpoint",new{Host=host,Node=_cursor.Position,NativeExecutorStillRunning=true,
            CapturedBeforeNextEffect=true,DisplayWaitAdded=false,RestoresTasks=false});
    }
    private static async Task Show(ChoiceScope source,int stage,int? displayNode=null)
    {
        Codec.Require(_view is null,"route-overlapping-presentation");
        var view=new ChoiceScope(source.Player,source.Options,source.Frame,source.Generation)
        {Expected=source.Expected,Prefs=source.Prefs,Pile=source.Pile,SourceModel=source.SourceModel};
        view.PlaybackEvents=ChoiceTrace.ResultDemo(source.Expected!).Where(e=>e.Kind is not ("Open" or "Close")).ToArray();
        _view=view;_viewResult=NativeChoice.Show(view);
        if(view.Ui is null){await _viewResult;throw new InvalidDataException("route-ui-not-created");}
        await NativeChoice.WaitReady(view);
        NativeChoice.Prefix(view,stage);
        var shownNode=displayNode??_cursor.Position;
        Runtime.Log("route-choice-view",new{Node=shownNode,Host=Nodes[shownNode].Host,Choice=source.Frame.Sequence,Stage=stage,
            Mode="final-decision",RawEvents=source.Expected!.Events?.Length,ProjectedEvents=view.PlaybackEvents.Length+2,FreshUiWait=true,NativeBusinessWaitUntouched=true,Historical});
    }
    internal static void Closed(object ui)
    {
        if(_view is {} view&&ReferenceEquals(view.Ui,ui)&&!view.Cancelled)view.Closed=true;
    }
    internal static void ClosureSettled(object ui)
    {
        if(_view is {} view&&ReferenceEquals(view.Ui,ui)&&view.Closed&&!view.Cancelled)view.UiClosed.TrySetResult(true);
    }
    private static async Task CloseView()
    {
        if(_view is not {} view)return;
        var result=_viewResult;_view=null;_viewResult=null;
        if(!view.Closed)NativeChoice.Cancel(view);
        if(result is not null)try{await result;}catch(OperationCanceledException){}
    }
    internal static bool Control(string command)
    {
        if(command=="step")
        {
            var acceptedStep=CanStep;
            if(acceptedStep){_cursor.Pause();Requests.Add();NativeDisplayTrace.MarkStepRequested();}
            Runtime.Log("route-control",new{Command=command,Accepted=acceptedStep,Node=_cursor.Position,_cursor.Count,
                EngineHost=PlayerLoop.Cursor?.Position,_cursor.Playing,_cursor.AwaitingEngine,Historical,QueuedSteps=Requests.Count});
            return acceptedStep;
        }
        var pause=command=="toggle"&&(_cursor.Playing||Requests.Count>0||AcceptDuringDisplay);
        if(command is "back" or "stop"||pause)Requests.Clear();
        if(pause)
        {
            _cursor.Pause();var acceptedPause=!_cursor.Stopping;
            if(acceptedPause&&AcceptDuringDisplay)Navigation.Request("pause");
            Runtime.Log("route-control",new{Command="pause",Accepted=acceptedPause,Node=_cursor.Position,QueuedSteps=Requests.Count,
                NativeBusinessInterrupted=false});return acceptedPause;
        }
        var queued=AcceptDuringDisplay&&((command=="step"&&CanStep||command=="back"&&CanBack)&&Navigation.Request(command)||command=="toggle"&&Navigation.Request("pause"));
        var accepted=queued||command switch
        {
            "stop" when !_cursor.Stopping=>StopRequested(),
            "toggle" when !Navigation.Pending&&(CanStep||Playing)=>_cursor.Toggle(),
            "step" when CanStep=>_cursor.Step(),
            "back" when CanBack=>_cursor.Back(),_=>false
        };
        if(accepted&&command=="step")NativeDisplayTrace.MarkStepRequested();
        Runtime.Log("route-control",new{Command=command,Accepted=accepted,Node=_cursor.Position,_cursor.Count,EngineHost=PlayerLoop.Cursor?.Position,
            _cursor.Playing,_cursor.AwaitingEngine,Historical,QueuedDuringPresentation=queued});return accepted;
    }
    private static void DrainNavigation()
    {
        if(_cursor.Stopping){Navigation.Clear();return;}
        if(!_cursor.Ready)return;
        var direction=Navigation.Take();
        if(direction==0)return;
        if(direction==2){_cursor.Pause();Runtime.Log("route-presentation-navigation",new{Direction="pause",Node=_cursor.Position,Accepted=true,NativeBusinessInterrupted=false});return;}
        var accepted=direction>0?!(Current.Terminal&&_cursor.AtFrontier)&&_cursor.Step():_cursor.Back();
        Runtime.Log("route-presentation-navigation",new{Direction=direction,Node=_cursor.Position,Accepted=accepted,NativeBusinessInterrupted=false});
    }
    private static bool StopRequested()
    {
        Requests.Clear();Navigation.Clear();_cursor.Stop();PlayerLoop.Cursor!.Stop();_stop=StopCore();return true;
    }
    private static async Task StopCore()
    {
        // Finish an already-started navigation before removing its UI; no new decision is submitted.
        if(_work is {} work)try{await work;}catch{}
        while(_arriving)await Runtime.NextFrame();
        await PlayerLoop.DrainCommitted();
        try
        {
            RoutePresentation.CloseBrowser();
            NativeDisplayTrace.RetireOwnedTails();
            await CloseView();
            if(Nodes.Count>0&&!_cursor.AtFrontier&&!_cursor.AwaitingEngine)await Nodes[^1].Data.Restore();
        }
        finally
        {
            NativeDisplayTrace.Clear();
            try{await NativeDisplayTrace.RetireSourceVisuals();}
            finally
            {
                if(_gate is {} gate){PlayerLoop.ChoiceAbort=true;gate.Decision.TrySetCanceled();}
                Runtime.Log("route-stopped",new{Node=_cursor.Position,EngineHost=PlayerLoop.Cursor?.Position,PendingNativeChoiceCancelled=PlayerLoop.ChoiceAbort,NoDecisionSubmittedForStop=true});
            }
        }
    }
    internal static void Tick()
    {
        if(Enabled&&!PlayerLoop.TearingDown&&!_cursor.Stopping&&!_arriving&&!Navigation.Pending&&_cursor.AwaitingEngine&&_gate is null&&
            (_cursor.Playing||Requests.Count>0)&&PlayerLoop.TryQueueNext())
        {if(!_cursor.Playing)Requests.Take();}
        if(!Enabled||PlayerLoop.TearingDown||_arriving||_work is {IsCompleted:false}||_cursor.Stopping)return;
        if(_cursor.Ready&&Requests.Count>0)
        {
            if(Current.Terminal&&_cursor.AtFrontier){Requests.Clear();return;}
            if(_cursor.Step())Requests.Take();
        }
        if(_clip!.Format<6&&_cursor.Playing&&Stopwatch.GetElapsedTime(_last).TotalSeconds<.55)return;
        var direction=_cursor.Take();if(direction==0)return;
        _work=Move(direction);
    }
    private static async Task Move(int direction)
    {
        var requestedAt=NativeDisplayTrace.TakeStepRequest();
        var historicalTransition=false;
        try
        {
            RoutePresentation.CloseBrowser();
            if(_view?.Ui is MegaCrit.Sts2.Core.Nodes.Combat.NPlayerHand hand)hand.PeekButton.SetPeeking(false);
            if(direction<0||!_cursor.AtFrontier)
            {
                var from=_cursor.Position;var target=from+direction;var node=Nodes[target];
                historicalTransition=true;_arrivalTerminal=node.Terminal&&target==Nodes.Count-1;
                if(direction<0)NativeDisplayTrace.RetireOwnedTails();
                if(node.Choice is not null&&ReferenceEquals(Current.Choice,node.Choice)&&ReferenceEquals(Current.Data,node.Data)&&_view is not null)
                {
                    NativeChoice.Prefix(_view,node.Stage);_cursor.Arrive(target);
                    Codec.Require(LiveState.ChoiceProjection()==node.Data.Projection,"route-ui-undo-changed-business-state");
                }
                else
                {
                    await CloseView();
                    var shownDuringTrace=false;
                    if(direction>0&&node.Trace is {} trace)
                    {
                        Func<Task>? show=node.Choice is {} traceChoice?async()=>{await Show(traceChoice,node.Stage,target);shownDuringTrace=true;}:null;
                        await trace.Play(()=>node.Data.Restore(),show,requestedAt);
                    }
                    else await node.Data.Restore();
                    _cursor.Arrive(target);
                    if(node.Choice is {} choice&&!shownDuringTrace&&!_cursor.Stopping)await Show(choice,node.Stage);
                }
                if(node.Terminal)_cursor.Pause();Publish(direction<0?"back":"history-forward");
                Runtime.Log("route-arrived",new{From=from,To=target,Host=node.Host,Choice=node.Choice?.Frame.Sequence,node.Stage,
                    StateMatched=true,NativeEffectsRedispatched=false,RoomRebuilt=false,PrefixOperations=0,Historical});return;
            }
            Codec.Require(!Current.Terminal,"route-forward-past-end");
            if(Current.Choice is {} source)
            {
                Codec.Require(ReferenceEquals(_gate,source)&&_view is not null,"route-forward-without-live-frontier");
                var view=_view!;var action=view.PlaybackEvents[Current.Stage];
                var commits=Current.Stage+1==view.PlaybackEvents.Length;
                if(commits)NativeDisplayTrace.Begin(requestedAt);
                view.EventPosition=Current.Stage+1;NativeChoice.Apply(view,action);
                Codec.Require(!view.Closed||commits,"route-native-ui-closed-too-early");
                if(!commits)
                {
                    Codec.Require(action.Kind=="Select","route-final-mode-nonselection");
                    NativeChoice.AssertPrefix(view,Current.Stage+1);
                    Codec.Require(LiveState.ChoiceProjection()==Current.Data.Projection,"route-ui-select-changed-business-state");
                    Nodes.Add(new(Current.Data,source,Current.Stage+1,Current.Host,false,null));_cursor.Append();Publish("select");
                }
                else
                {
                    var cards=await _viewResult!;await view.UiClosed.Task;
                    var expected=source.Expected!;var chosen=cards.Select(c=>Array.FindIndex(source.Options,o=>o.Contains(c))).Distinct().ToArray();
                    if(source.Frame.Kind=="FromChooseABundleScreen")chosen=[Array.FindIndex(source.Options,o=>o.SequenceEqual(cards))];
                    Codec.Require(chosen.SequenceEqual(expected.Chosen),"route-native-ui-result-mismatch");
                    var native=NativeResult(source);_view=null;_viewResult=null;_cursor.EngineWait();_gate=null;
                    Codec.Require(source.Decision.TrySetResult(native),"route-duplicate-semantic-choice");
                    Runtime.Log("route-choice-commit",new{Node=_cursor.Position,expected.Sequence,expected.Boundary,expected.Chosen,NativeDecisionDeliveredOnce=true});
                }
            }
            else
            {
                Codec.Require(PlayerLoop.Cursor!.Position==Current.Host&&!PlayerLoop.Cursor.Busy,"route-native-host-frontier");
                NativeDisplayTrace.Begin(requestedAt);Codec.Require(PlayerLoop.Cursor.Step(),"route-native-step-refused");_cursor.EngineWait();
            }
        }
        catch(Exception ex)
        {
            PlayerLoop.Error="route-"+ex.GetBaseException().Message;Runtime.Log("route-failed",new{Reason=PlayerLoop.Error,Node=_cursor.Position});
            if(_gate is {} gate)gate.Decision.TrySetException(new InvalidDataException(PlayerLoop.Error));
        }
        finally{if(historicalTransition)_arrivalTerminal=false;_last=Stopwatch.GetTimestamp();DrainNavigation();}
    }
    private static PlayerChoiceResult NativeResult(ChoiceScope source)
    {
        var f=source.Expected!;var cards=f.Chosen.SelectMany(i=>source.Options[i]).ToArray();
        return f.NativeType switch
        {
            "CombatCard"=>PlayerChoiceResult.FromMutableCombatCards(cards),"DeckCard"=>PlayerChoiceResult.FromMutableDeckCards(cards),
            "Index" when f.Kind is "FromChooseACardScreen" or "FromChooseABundleScreen"=>PlayerChoiceResult.FromIndex(f.Chosen.Length==0?-1:f.Chosen[0]),
            "Index"=>PlayerChoiceResult.FromIndexes(f.Chosen.ToList()),_=>throw new InvalidDataException("route-native-result-type")
        };
    }
    private static void Publish(string reason)
    {
        var node=Current;
        RoutePresentation.Synchronize(continuePresentation:reason!="back");RoutePresentation.Access();
        if(reason=="back")HandPresentation.FinishHighlights(MegaCrit.Sts2.Core.Nodes.Combat.NPlayerHand.Instance!);
        Runtime.Log("route-display-verified",new{Node=_cursor.Position,Reason=reason,Display=RoutePresentation.Check()});
        Runtime.Status=$"最终决策 · 节点 {_cursor.Position}/{Nodes.Count-1} · 操作 {node.Host}/{_clip!.Operations.Length} · "+
            (node.Terminal?"战斗胜利":node.Choice is not null?$"选择 {node.Choice.Frame.Sequence} · {node.Stage} 个目标已选":_cursor.Playing?"播放":"暂停");
        ChoiceTape.Display=node.Choice is null?"":$"最终确认序列 · {(node.Choice.Expected!.Events is null?"旧片仅有最终结果，未录点击过程":"原始操作事件已保留")}\nStep / Back 沿同一路线；可滚动浏览，原决定只读";
        Runtime.Log("route-node",new{Reason=reason,Node=_cursor.Position,Frontier=Nodes.Count-1,Host=node.Host,Choice=node.Choice?.Frame.Sequence,node.Stage,
            node.Terminal,Mode="final-decision",Historical,ChoicesConsumed=ChoiceTape.Position});
    }
}
