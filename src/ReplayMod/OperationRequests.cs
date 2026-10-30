namespace ReplayMod;

internal enum ForwardKind { Fine, Whole }
internal readonly record struct PlaybackIntent(bool Running,bool Auto)
{
    internal static PlaybackIntent ForNavigation(bool auto,bool back)=>new(true,!back&&auto);
}
internal sealed record ForwardPoint(int Stage,int Host,int Choice,int Prefix,bool Complete,string Label);
internal sealed record ForwardRequest(long Id,ForwardKind Kind,ForwardPoint Target,bool Automatic=false,bool Continuation=false);

// Targets describe navigation, not saved choice states. Only Complete points may be restored.
internal sealed class OperationRequests
{
    internal readonly ForwardPoint[] Route;
    private readonly Queue<ForwardRequest> _pending=[];
    private long _nextId;
    // Running is an internal lifecycle gate, not a public playback mode.
    internal bool Running {get;private set;}=true;
    internal bool Auto {get;private set;}
    internal bool ManualWaiting=>Running&&!Auto&&Active is null&&_pending.Count==0;
    internal bool CanRequestFine=>ManualWaiting&&Stage<Route[^1].Stage;
    internal PlaybackIntent Intent=>new(Running,Auto);
    internal ForwardRequest? Active {get;private set;}
    internal ForwardRequest[] Pending=>_pending.ToArray();
    internal int Stage {get;private set;}
    internal OperationRequests(Clip clip,int hosts,bool opening=false)
    {
        var points=new List<ForwardPoint>{new(0,opening?-1:0,0,0,true,opening?"战斗开场":"首次可出牌")};
        if(opening)
        {
            foreach(var choice in (clip.Choices??[]).Where(c=>c.Boundary==0&&!c.Automated))
                for(var prefix=0;prefix<SeekPlan.Events(choice).Length;prefix++)
                    points.Add(new(points.Count,0,choice.Sequence,prefix,false,$"开场选择{choice.Sequence} · "+(prefix==0?"已打开":$"已选{prefix}")));
            points.Add(new(points.Count,0,0,0,true,"首次可出牌"));
        }
        foreach(var op in clip.Operations.Take(hosts))
        {
            foreach(var choice in (clip.Choices??[]).Where(c=>c.Boundary==op.Sequence&&!c.Automated))
                for(var prefix=0;prefix<SeekPlan.Events(choice).Length;prefix++)
                    points.Add(new(points.Count,op.Sequence,choice.Sequence,prefix,false,$"选择{choice.Sequence} · "+(prefix==0?"已打开":$"已选{prefix}")));
            points.Add(new(points.Count,op.Sequence,0,0,true,op.Kind==OperationKind.EndTurn?$"结束回合{op.Turn}":
                op.Kind==OperationKind.DiscardPotion?$"丢弃 {op.Potion!.Model}":op.Card??op.Kind.ToString()));
        }
        Route=points.ToArray();
    }
    internal ForwardRequest? Add(ForwardKind kind)
    {
        if(kind==ForwardKind.Fine&&!CanRequestFine)return null;
        var from=_pending.LastOrDefault()?.Target.Stage??Active?.Target.Stage??Stage;
        var target=kind==ForwardKind.Fine?Route.FirstOrDefault(p=>p.Stage>from):Route.FirstOrDefault(p=>p.Stage>from&&p.Complete);
        if(target is null)return null;
        var request=new ForwardRequest(++_nextId,kind,target);_pending.Enqueue(request);return request;
    }
    internal ForwardRequest[] NativeQueueCandidates(int completed)
    {
        var candidates=Active is null?Pending:new[]{Active}.Concat(Pending);
        var selected=new List<ForwardRequest>();var expected=completed+1;
        foreach(var request in candidates)
        {
            // Candidate calculation considers existing requests only, without changing
            // the explicit navigation FIFO or adding automatic targets.
            if(request.Target.Host!=expected||request.Kind==ForwardKind.Fine&&!request.Target.Complete)break;
            selected.Add(request);expected++;
            // Explicit complete-card targets share one FIFO window, regardless of kind.
            // No Auto target is invented here; it only considers already accepted requests.
            if(selected.Count==3)break;
        }
        return selected.ToArray();
    }
    internal void ToggleAuto()=>Auto=!Auto;
    internal bool Acquire()
    {
        if(!Running)return false;
        if(Active is not null)return true;
        if(_pending.TryDequeue(out var request))Active=request;
        else if(Auto&&Route.FirstOrDefault(p=>p.Stage>Stage&&p.Complete) is {} target)
            Active=new(++_nextId,ForwardKind.Whole,target,true);
        return Active is not null;
    }
    internal ForwardRequest? Arrive(ForwardPoint point)
    {
        Stage=point.Stage;
        if(Active is not {} request||point.Stage<request.Target.Stage)return null;
        Active=null;
        // Manual fine arrival keeps the lifecycle gate open. With Auto off, Acquire
        // waits for another explicit target and never invents a Whole continuation.
        if(point.Stage==Route[^1].Stage){Running=false;Auto=false;_pending.Clear();}
        return request;
    }
    internal void Seek(int host)
    {
        var target=Route.Single(p=>p.Complete&&p.Host==host);
        Running=false;_pending.Clear();Active=null;Stage=target.Stage;
    }
    internal void Restore(int host,PlaybackIntent intent)
    {
        Seek(host);Running=intent.Running;Auto=intent.Auto;
    }
    internal void Stop(){Running=false;Auto=false;_pending.Clear();Active=null;}
}
