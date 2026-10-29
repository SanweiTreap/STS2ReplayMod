namespace ReplayMod;

internal enum ForwardKind { Fine, Whole }
internal sealed record ForwardPoint(int Stage,int Host,int Choice,int Prefix,bool Complete,string Label);
internal sealed record ForwardRequest(long Id,ForwardKind Kind,ForwardPoint Target,bool Automatic=false,bool Continuation=false);

// Targets describe navigation, not saved choice states. Only Complete points may be restored.
internal sealed class OperationRequests
{
    internal readonly ForwardPoint[] Route;
    private readonly Queue<ForwardRequest> _pending=[];
    private long _nextId;
    internal bool Running {get;private set;}
    internal bool Auto {get;private set;}
    internal ForwardRequest? Active {get;private set;}
    internal ForwardRequest[] Pending=>_pending.ToArray();
    internal int Stage {get;private set;}
    internal OperationRequests(Clip clip,int hosts)
    {
        var points=new List<ForwardPoint>{new(0,0,0,0,true,"战斗开始")};
        foreach(var op in clip.Operations.Take(hosts))
        {
            foreach(var choice in (clip.Choices??[]).Where(c=>c.Boundary==op.Sequence&&!c.Automated))
                for(var prefix=0;prefix<SeekPlan.Events(choice).Length;prefix++)
                    points.Add(new(points.Count,op.Sequence,choice.Sequence,prefix,false,$"选择{choice.Sequence} · "+(prefix==0?"已打开":$"已选{prefix}")));
            points.Add(new(points.Count,op.Sequence,0,0,true,op.Kind==OperationKind.EndTurn?$"结束回合{op.Turn}":op.Card??op.Kind.ToString()));
        }
        Route=points.ToArray();
    }
    internal ForwardRequest? Add(ForwardKind kind)
    {
        var from=_pending.LastOrDefault()?.Target.Stage??Active?.Target.Stage??Stage;
        var target=kind==ForwardKind.Fine?Route.FirstOrDefault(p=>p.Stage>from):Route.FirstOrDefault(p=>p.Stage>from&&p.Complete);
        if(target is null)return null;
        var request=new ForwardRequest(++_nextId,kind,target);_pending.Enqueue(request);return request;
    }
    internal void ToggleRunning()
    {
        Running=!Running;
        // Play from a fine stop continues only its already-started native host. A queued
        // explicit target takes precedence. No new operation is invented at a boundary.
        if(Running&&Active is null&&_pending.Count==0&&!Route[Stage].Complete)
            Active=new(++_nextId,ForwardKind.Whole,Route.First(p=>p.Complete&&p.Stage>Stage),Continuation:true);
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
        // A final explicit fine target is an observation stop, even with Auto enabled.
        // Following explicit requests retain FIFO order; Auto never swallows this final stop.
        if(request.Kind==ForwardKind.Fine&&_pending.Count==0)Running=false;
        if(point.Stage==Route[^1].Stage){Running=false;_pending.Clear();}
        return request;
    }
    internal void Seek(int host)
    {
        var target=Route.Single(p=>p.Complete&&p.Host==host);
        Running=false;_pending.Clear();Active=null;Stage=target.Stage;
    }
    internal void Stop(){Running=false;Auto=false;_pending.Clear();Active=null;}
}
