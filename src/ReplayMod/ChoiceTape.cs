using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace ReplayMod;

internal sealed class ChoiceScope(Player player,CardModel[][] options,ChoiceFrame frame,int generation,long? started=null)
{
    internal readonly Player Player=player;
    internal readonly CardModel[][] Options=options;
    internal readonly ChoiceFrame Frame=frame;
    internal readonly int Generation=generation;
    internal readonly long Started=started??Stopwatch.GetTimestamp();
    internal ChoiceFrame? Expected;
    internal uint? Id;
    internal PlayerChoiceResult? NativeResult;
    internal object? Ui;
    internal readonly List<ChoiceEvent> Events=[];
    internal ChoiceEvent[] PlaybackEvents=[];
    internal int EventPosition;
    internal bool Completed;
    internal bool Cancelled;
    internal bool Closed;
    internal readonly TaskCompletionSource<bool> UiClosed=new();
    internal Task? Driver;
    internal CardSelectorPrefs Prefs;
    internal CardPile? Pile;
    internal AbstractModel? SourceModel;
    internal readonly TaskCompletionSource<PlayerChoiceResult> Decision=new();
}

internal static class ChoiceTape
{
    private static readonly List<ChoiceFrame> Recorded=[];
    private static ChoiceFrame[] _play=[];
    private static ChoiceScope? _scope;
    private static int _generation,_boundary;
    private static bool _record,_watch;
    private static long _started;
    private static double _waitingSeconds;
    internal static int Position {get;private set;}
    internal static bool Waiting=>_scope is not null;
    internal static ChoiceScope? Scope=>_scope;
    internal static bool Watching=>_watch&&PlayerLoop.Active;
    internal static bool Recording=>_record&&Recorder.CollectingChoices;
    internal static bool Controllable=>NativeSeek.Enabled?NativeSeek.HasView:DecisionRoute.Enabled?DecisionRoute.HasView:Watching&&_scope is {Ui:not null,Completed:false,Closed:false};
    internal static double WaitingSeconds=>_waitingSeconds+(_scope is {} s?Stopwatch.GetElapsedTime(s.Started).TotalSeconds:0);
    internal static string Display="";
    private static double _showUntil;
    internal static string VisibleDisplay=>Waiting||Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency<_showUntil?Display:"";
    internal static void Clear(){_generation++;_scope=null;Recorded.Clear();_play=[];_boundary=0;Position=0;_record=false;_watch=false;Display="";_waitingSeconds=0;}
    internal static void Record(){Clear();_record=true;_started=RecordingClock.Active?RecordingClock.Origin:Stopwatch.GetTimestamp();}
    internal static void Play(Clip clip){Clear();_watch=clip.Format>=4;_play=clip.Choices??[];_started=Stopwatch.GetTimestamp();}
    internal static ChoiceFrame[] Snapshot(){Codec.Require(!Waiting,"choice-still-in-progress");return Recorded.ToArray();}
    internal static ChoiceFrame[] CompletedPrefix(int operations)=>Recorded.Where(f=>f.Boundary<=operations).ToArray();
    internal static void Boundary(int boundary){Codec.Require(!Waiting,"choice-boundary-in-flight");_boundary=boundary;}
    internal static void AssertBoundary(int boundary)
    {
        Codec.Require(!Waiting,"choice-boundary-in-flight");
        if(_watch)Codec.Require(Position==_play.Length||_play[Position].Boundary>boundary,"recorded-choice-not-consumed");
    }
    internal static void Restore(int position)
    {
        Codec.Require(!Waiting&&position>=0&&position<=_play.Length,"choice-restore-cursor");
        var pending=(System.Collections.IList)AccessTools.Field(typeof(PlayerChoiceSynchronizer),"_receivedChoices").GetValue(RunManager.Instance.PlayerChoiceSynchronizer)!;
        Codec.Require(pending.Count==0,"choice-restore-has-native-waiters");
        Position=position;Display="";
        Runtime.Log("choice-cursor-restored",new{Position,NativeChoiceIdsKeptMonotonic=true,NativeWaiters=0});
    }
    internal static void ViewPosition(int position){Codec.Require(position>=0&&position<=_play.Length,"choice-view-cursor");Position=position;}
    private static ChoiceCard Card(CardModel c,Player p)
    {
        uint? id=c.IsMutable&&NetCombatCardDb.Instance.TryGetCardId(c,out var value)?value:null;
        var index=p.Deck.Cards.ToList().FindIndex(v=>ReferenceEquals(v,c));
        var fingerprint=c.IsMutable?LiveState.Fingerprint(LiveState.CardDetails(c)):Codec.Hash(System.Text.Encoding.UTF8.GetBytes(c.Id.ToString()+":canonical"));
        // The native title already includes its upgrade suffix.
        var label=c.Title;if(label.Length>512)label=label[..512];
        return new(c.Id.ToString(),c.CurrentUpgradeLevel,c.IsMutable,id,index>=0?index:null,fingerprint,label);
    }
    // Only non-delegating CardSelectCmd entry points are patched. Their actual native effects and signals remain intact.
    internal static ChoiceScope? Begin(MethodBase method,object[] args)
    {
        if(RunPlayback.NonCombat)return null;
        if(!(_record&&Recorder.CollectingChoices||_watch&&PlayerLoop.Active&&!PlayerLoop.TearingDown))return null;
        Codec.Require(_scope is null,"overlapping-player-choice-not-supported");
        Codec.Require(CardSelectCmd.LocalSelector is null,"external-local-test-selector-active");
        var kind=method.Name;var player=args.OfType<Player>().FirstOrDefault()??LiveState.Player;
        if(kind is "FromHand" or "FromHandForUpgrade"&&CombatManager.Instance.IsOverOrEnding)return null;
        if(kind is "FromSimpleGrid" or "FromSimpleGridForRewards" or "FromCombatPile" or "FromChooseABundleScreen"&&CombatManager.Instance.IsEnding)return null;
        Codec.Require(ReferenceEquals(player,LiveState.Player),"choice-only-single-local-player");
        var context=args.OfType<PlayerChoiceContext>().FirstOrDefault();
        var source=args.OfType<AbstractModel>().FirstOrDefault()??context?.LastInvolvedModel??(context as HookPlayerChoiceContext)?.Source??
            (AbstractModel?)ActionMonitor.Pending?.Card??ActionMonitor.Pending?.Potion;
        var prefs=args.OfType<CardSelectorPrefs>().FirstOrDefault();
        var filter=args.OfType<Func<CardModel,bool>>().FirstOrDefault();
        CardModel[] Filter(IEnumerable<CardModel> cards)=>cards.Where(filter??(_=>true)).ToArray();
        CardModel[][] options;
        if(kind=="FromChooseABundleScreen")options=((IReadOnlyList<IReadOnlyList<CardModel>>)args[1]).Select(b=>b.ToArray()).ToArray();
        else
        {
            IEnumerable<CardModel> cards=kind switch
            {
                "FromHand"=>Filter(player.PlayerCombatState!.Hand.Cards),
                "FromHandForUpgrade"=>player.PlayerCombatState!.Hand.Cards.Where(c=>c.IsUpgradable),
                "FromCombatPile"=>Filter(args.OfType<CardPile>().Single().Cards),
                "FromDeckForUpgrade"=>player.Deck.Cards.Where(c=>c.IsUpgradable),
                "FromDeckForTransformation"=>player.Deck.Cards.Where(c=>c.IsTransformable),
                "FromDeckGeneric"=>args.OfType<Func<CardModel,int>>().FirstOrDefault() is {} sort?Filter(player.Deck.Cards).OrderBy(sort):Filter(player.Deck.Cards),
                "FromSimpleGridForRewards"=>args.OfType<List<CardCreationResult>>().Single().Select(r=>r.Card),
                _=>args.OfType<IReadOnlyList<CardModel>>().Single()
            };
            options=cards.Select(c=>new[]{c}).ToArray();
        }
        if(kind=="FromChooseACardScreen")prefs=new(new MegaCrit.Sts2.Core.Localization.LocString("gameplay_ui","CHOOSE_CARD_HEADER"),args.OfType<bool>().FirstOrDefault()?0:1,1){Cancelable=args.OfType<bool>().FirstOrDefault()};
        if(kind=="FromHandForUpgrade")prefs=new(new MegaCrit.Sts2.Core.Localization.LocString("gameplay_ui","CHOOSE_CARD_HEADER"),1);
        if(kind=="FromChooseABundleScreen")prefs=new(new MegaCrit.Sts2.Core.Localization.LocString("gameplay_ui","CHOOSE_CARD_HEADER"),0,1){Cancelable=true};
        var boundary=_record?Recorder.ChoiceBoundary:_boundary;
        var native=kind is "FromHand" or "FromHandForUpgrade" or "FromCombatPile"?"CombatCard":kind.StartsWith("FromDeck",StringComparison.Ordinal)?"DeckCard":"Index";
        var automatic=CardSelectCmd.Selector is not null||options.Length==0||kind is not ("FromChooseACardScreen" or "FromChooseABundleScreen")&&
            !prefs.RequireManualConfirmation&&options.Length<=prefs.MinSelect;
        var min=Math.Max(0,prefs.MinSelect);var max=Math.Max(min,Math.Min(Math.Max(0,prefs.MaxSelect),options.Length));
        var candidates=options.Select(o=>o.Select(c=>Card(c,player)).ToArray()).ToArray();var began=Stopwatch.GetTimestamp();
        var frame=new ChoiceFrame(Position+1,boundary,LiveState.Turn,kind,source?.Id.ToString(),min,max,prefs.Cancelable,prefs.RequireManualConfirmation,
            candidates,[],Stopwatch.GetElapsedTime(_started,began).TotalMilliseconds,0,native,null,automatic,RequestedMax:prefs.MaxSelect);
        Codec.Require(options.Length<=1024&&frame.Max<=1024,"choice-candidate-limit");
        var scope=new ChoiceScope(player,options,frame,_generation,began){Prefs=prefs,Pile=args.OfType<CardPile>().FirstOrDefault(),SourceModel=source};
        if(_watch)
        {
            Codec.Require(Position<_play.Length,"unexpected-choice-after-recording");scope.Expected=_play[Position];ChoiceContract.Match(scope.Expected,frame);
        }
        _scope=scope;Runtime.Log("choice-begun",new{Mode=_watch?"playback":"recording",frame.Sequence,frame.Boundary,frame.Kind,frame.Source,frame.Turn,frame.Min,frame.Max,Candidates=frame.Options,frame.Automated});
        return scope;
    }
    internal static void Reserved(Player player,uint id)
    {
        if(!(_watch||_record&&Recorder.CollectingChoices))return;
        Codec.Require(_scope is {} s&&ReferenceEquals(s.Player,player),"choice-reserved-outside-supported-command");_scope!.Id=id;
    }
    internal static void Synced(Player player,uint id,PlayerChoiceResult result)
    {
        if(!(_watch||_record&&Recorder.CollectingChoices))return;
        Codec.Require(_scope is {} s&&ReferenceEquals(s.Player,player)&&s.Id==id&&s.NativeResult is null,"unexpected-or-duplicate-local-choice");_scope!.NativeResult=result;
    }
    internal static bool Remote(Player player,uint id,ref Task<PlayerChoiceResult> result)
    {
        if(!_watch||!PlayerLoop.Active)return true;
        if(NativeSeek.Enabled){result=Task.FromException<PlayerChoiceResult>(new InvalidDataException("operation-expected-native-local-choice"));return false;}
        if(DecisionRoute.Enabled){result=DecisionRoute.Open(_scope??throw new InvalidDataException("unscoped-route-choice"),player,id);return false;}
        result=Task.FromException<PlayerChoiceResult>(new InvalidDataException("unexpected-remote-choice-in-native-ui-playback"));return false;
    }
    internal static bool Local(ref bool result)
    {
        if(!_watch||!PlayerLoop.Active)return true;result=!DecisionRoute.Enabled;return false;
    }
    private static void Complete(ChoiceScope s,IEnumerable<CardModel?> returned)
    {
        Codec.Require(s.Generation==_generation&&ReferenceEquals(_scope,s),"choice-completion-context");
        var cards=returned.OfType<CardModel>().ToArray();int[] chosen;
        if(s.Frame.Kind=="FromChooseABundleScreen")
        {
            var index=s.NativeResult?.AsIndex()??Array.FindIndex(s.Options,b=>b.SequenceEqual(cards));chosen=index<0?[]:[index];
        }
        else chosen=cards.Select(c=>Array.FindIndex(s.Options,o=>o.Length==1&&ReferenceEquals(o[0],c))).ToArray();
        if(s.Ui is not null)
        {
            Codec.Require(s.Closed,"choice-ui-not-closed");
            if(_watch)Codec.Require(s.EventPosition==s.PlaybackEvents.Length,"choice-process-not-consumed");
        }
        var frame=s.Frame with{Chosen=chosen,DurationMilliseconds=Stopwatch.GetElapsedTime(s.Started).TotalMilliseconds,NativeId=s.Id,NativeType=s.NativeResult?.ChoiceType.ToString()??s.Frame.NativeType,
            Events=s.Ui is null||_watch?null:s.Events.ToArray()};
        ChoiceContract.Validate([frame with{Sequence=1}],frame.Boundary);
        if(_watch)
        {
            Codec.Require(chosen.SequenceEqual(s.Expected!.Chosen)&&frame.NativeType==s.Expected.NativeType,"choice-final-result-mismatch");
            Runtime.Log("choice-delivered",new{frame.Sequence,frame.Boundary,CurrentNativeId=s.Id,RecordedNativeId=s.Expected.NativeId,frame.NativeType,frame.Chosen,ReplayedOriginalDecision=true,ThroughNativeUi=s.Ui is not null});
        }
        else Recorded.Add(frame);
        s.Completed=true;Position++;Runtime.Log("choice-completed",new{Mode=_watch?"playback":"recording",frame.Sequence,frame.Boundary,frame.Kind,frame.Source,frame.Chosen,frame.NativeId,frame.NativeType,frame.DurationMilliseconds,frame.Automated,ProcessEvents=s.Events.Count,Matched=_watch});
        _showUntil=Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency+2;
    }
    internal static void Event(string kind,int? option=null)
    {
        if(!Recording||_scope is not {} s||s.Completed)return;
        Codec.Require(s.Events.Count<4096,"choice-event-limit");
        var e=new ChoiceEvent(kind,option,Stopwatch.GetElapsedTime(s.Started).TotalMilliseconds);s.Events.Add(e);
        Runtime.Log("choice-recorded-event",new{s.Frame.Sequence,s.Frame.Boundary,e.Kind,e.Option,e.Milliseconds});
    }
    internal static void Opened(object ui)
    {
        if(NativeSeek.Enabled){if(_scope is {} native&&native.Ui is null)NativeSeek.Opened(native,ui);return;}
        if(DecisionRoute.Enabled)return;
        if(!(Recording||Watching)||_scope is not {} s||s.Ui is not null)return;
        s.Ui=ui;Codec.Require(!s.Frame.Automated,"unexpected-ui-for-automatic-choice");
        if(Recording){Event("Open");return;}
        if(PlayerLoop.Cursor!.StopRequested){PlayerLoop.ChoiceAbort=true;NativeChoice.Cancel(s);return;}
        s.PlaybackEvents=s.Expected!.Events??NativeChoice.ResultDemo(s.Expected,ui);
        s.EventPosition=1;PlayerLoop.Cursor!.ChoiceOpened();
        Display=$"选择 {s.Frame.Sequence} · {(s.Expected.Events is null?"结果演示（旧片未记录点击）":"原始选择过程")}\n已打开 · Step点选／撤选／确认；可滚动查看，Back仅完整操作边界";
        Runtime.Status=$"{(PlayerLoop.Loading?"开场":"操作中")}选择已打开 · {(PlayerLoop.Cursor.Playing?"播放":"暂停")}";
        Runtime.Log("choice-process-open",new{s.Frame.Sequence,s.Frame.Boundary,Ui=ui.GetType().Name,Trace=s.Expected.Events is null?"result-demo":"recorded-process",Events=s.PlaybackEvents.Length,ReadOnly=true});
        s.Driver=Drive(s);
    }
    internal static void Closed(object ui)
    {
        if(NativeSeek.Enabled){NativeSeek.Closed(ui);return;}
        if(DecisionRoute.Enabled){DecisionRoute.Closed(ui);return;}
        if(!(Recording||Watching)||_scope is not {} s||!ReferenceEquals(s.Ui,ui)||s.Closed||s.Cancelled)return;
        if(Recording)Event("Close");
        else
        {
            Codec.Require(s.EventPosition==s.PlaybackEvents.Length-1&&s.PlaybackEvents[s.EventPosition].Kind=="Close","choice-native-close-before-process-complete");
            s.EventPosition++;
            Runtime.Log("choice-process-event",new{s.Frame.Sequence,s.Frame.Boundary,Kind="Close",s.EventPosition,ActualNativeUiClose=true});
        }
        s.Closed=true;
    }
    internal static void ClosureSettled(object ui)
    {
        if(NativeSeek.Enabled){NativeSeek.Closed(ui,settled:true);return;}
        if(DecisionRoute.Enabled){DecisionRoute.ClosureSettled(ui);return;}
        if(_scope is {} s&&ReferenceEquals(s.Ui,ui)&&s.Closed&&!s.Cancelled)s.UiClosed.TrySetResult(true);
    }
    private static async Task Drive(ChoiceScope s)
    {
        try
        {
            // Let the native window finish its normal entrance before accepting a recorded click.
            var at=Stopwatch.GetTimestamp();
            while(Stopwatch.GetElapsedTime(at).TotalSeconds<.4){if(s.Cancelled)return;PlayerLoop.RequireHealthy();await Runtime.NextFrame();}
            string? pausedState=null;var pausedPosition=0;long? closing=null;
            while(!s.Completed&&!s.Cancelled)
            {
                PlayerLoop.RequireHealthy();Codec.Require(s.Generation==_generation,"choice-driver-session");
                if(s.Closed||s.EventPosition>=s.PlaybackEvents.Length-1)
                {
                    closing??=Stopwatch.GetTimestamp();Codec.Require(Stopwatch.GetElapsedTime(closing.Value).TotalSeconds<60,"choice-ui-did-not-complete");
                    await Runtime.NextFrame();continue;
                }
                if(!PlayerLoop.Cursor!.TakeChoiceStep())
                {
                    if(pausedState is null){pausedState=LiveState.ChoiceProjection();pausedPosition=Position;}
                    await Runtime.NextFrame();continue;
                }
                if(pausedState is not null)
                {
                    var unchanged=pausedState==LiveState.ChoiceProjection()&&pausedPosition==Position;
                    Runtime.Log("choice-paused-resumed",new{s.Frame.Sequence,s.EventPosition,ComparedProjectionAndRngUnchanged=unchanged,ChoiceCursorUnchanged=pausedPosition==Position});
                    Codec.Require(unchanged,"choice-paused-logical-state-changed");pausedState=null;
                }
                Codec.Require(s.EventPosition<s.PlaybackEvents.Length&&s.PlaybackEvents[s.EventPosition].Kind!="Close","choice-ui-did-not-complete");
                var e=s.PlaybackEvents[s.EventPosition++];
                Display=$"选择 {s.Frame.Sequence} · {(s.Expected!.Events is null?"结果演示":"原始选择过程")} · {s.EventPosition-1}/{s.PlaybackEvents.Length-2}\n{e.Kind}"+
                    (e.Option is {} option?" · "+string.Join(" / ",s.Frame.Options[option].Select(c=>c.Label)):"");
                Runtime.Log("choice-process-event",new{s.Frame.Sequence,s.Frame.Boundary,e.Kind,e.Option,s.EventPosition});
                NativeChoice.Apply(s,e);
                if(s.Completed)break;
                at=Stopwatch.GetTimestamp();while(PlayerLoop.Cursor.Playing&&Stopwatch.GetElapsedTime(at).TotalSeconds<.6){PlayerLoop.RequireHealthy();await Runtime.NextFrame();}
                await Runtime.NextFrame();
            }
        }
        catch(OperationCanceledException) when(PlayerLoop.Cursor?.StopRequested==true) {NativeChoice.Cancel(s);}
        catch(Exception ex){PlayerLoop.Error="choice-process-"+ex.GetBaseException().Message;NativeChoice.Cancel(s);Runtime.Log("choice-process-failed",new{Reason=PlayerLoop.Error});}
    }
    internal static Task<IEnumerable<CardModel>> Wrap(Task<IEnumerable<CardModel>> task,ChoiceScope? s)=>s is null?task:Finish(task,s);
    internal static Task<CardModel?> Wrap(Task<CardModel?> task,ChoiceScope? s)=>s is null?task:FinishOne(task,s);
    private static async Task<IEnumerable<CardModel>> Finish(Task<IEnumerable<CardModel>> task,ChoiceScope s)
    {
        try{var result=(await task).ToArray();if(s.Ui is not null&&!s.Closed)await s.UiClosed.Task;if(s.Generation==_generation){if(_watch)Complete(s,result);else Runtime.Guard(()=>Complete(s,result));}return result;}
        finally{if(ReferenceEquals(_scope,s)){_waitingSeconds+=Stopwatch.GetElapsedTime(s.Started).TotalSeconds;_scope=null;}}
    }
    private static async Task<CardModel?> FinishOne(Task<CardModel?> task,ChoiceScope s)
    {
        try{var result=await task;if(s.Ui is not null&&!s.Closed)await s.UiClosed.Task;if(s.Generation==_generation){if(_watch)Complete(s,[result]);else Runtime.Guard(()=>Complete(s,[result]));}return result;}
        finally{if(ReferenceEquals(_scope,s)){_waitingSeconds+=Stopwatch.GetElapsedTime(s.Started).TotalSeconds;_scope=null;}}
    }
}
