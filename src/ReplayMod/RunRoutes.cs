namespace ReplayMod;

// Raw operations and clips remain append-only facts. The selected path references
// those identities; a successful native save freezes its own path, not its later
// resource lookalikes. No operation is deduplicated by content.
internal static class RunRoutes
{
    internal static int StartAttempt(RunDocument run,int visit,int? parent,int? restored,string reason)
    {
        var branch=run.Branches??throw new InvalidDataException("run-has-no-attempt-index");
        var id=branch.Attempts.Count+1;
        branch.Attempts.Add(new(id,visit,parent,restored,DateTimeOffset.UtcNow,reason));
        branch.ActiveAttempt=id;return id;
    }
    internal static void Append(RunDocument run,RunOperation operation)
    {
        run.Operations.Add(operation);run.Branches?.Selected.Add(operation.Sequence);
    }
    internal static RunSavePoint Observe(RunDocument run,int visit,int attempt,int[] path,string raw,string native,string saveKey,bool preFinished)
    {
        var branch=run.Branches??throw new InvalidDataException("run-has-no-attempt-index");
        var point=new RunSavePoint(branch.Saves.Count+1,visit,attempt,DateTimeOffset.UtcNow,raw,native,saveKey,preFinished,path.ToArray());
        branch.Saves.Add(point);return point;
    }
    internal static RunSavePoint? Restore(RunDocument run,string fingerprint)
    {
        var branch=run.Branches??throw new InvalidDataException("run-has-no-attempt-index");
        var matches=branch.Saves.Where(s=>s.Fingerprint==fingerprint||s.NativeFingerprint==fingerprint).ToArray();
        if(matches.Length==0)return null;
        var point=matches[^1];
        Codec.Require(matches.All(p=>p.Visit==point.Visit&&p.PreFinished==point.PreFinished&&p.Path.SequenceEqual(point.Path)),"run-load-anchor-ambiguous");
        branch.Selected.Clear();branch.Selected.AddRange(point.Path);
        StartAttempt(run,point.Visit,point.Attempt,point.Id,"native-load");return point;
    }
    internal static RunDocument Playback(RunDocument raw)
    {
        if(raw.Branches is not {} branch)return raw;
        Codec.Require(raw.Operations.All(o=>o.SourceSequence is null),"run-projection-cannot-be-projected-again");
        var positions=branch.Selected.Select((id,index)=>(id,index)).ToDictionary(p=>p.id,p=>p.index+1);
        var operations=branch.Selected.Select((id,index)=>raw.Operations[id-1] with{Sequence=index+1,SourceSequence=id}).ToList();
        var continuations=raw.Continuations.Where(c=>c.Status=="native-continue"&&c.Attempt is {} attempt&&c.BeforeOperation is {} before&&
            positions.ContainsKey(before)&&raw.Operations[before-1].Attempt==attempt)
            .Select(c=>c with{BeforeOperation=positions[c.BeforeOperation!.Value]}).ToList();
        return raw with{Operations=operations,Continuations=continuations};
    }
    internal static void Validate(RunDocument run)
    {
        if(run.Branches is not {} branch)return;
        Codec.Require(branch.Selected.Distinct().Count()==branch.Selected.Count&&branch.Selected.All(s=>s>0&&s<=run.Operations.Count)&&
            branch.Selected.SequenceEqual(branch.Selected.Order()),"run-selected-path-order");
        Codec.Require(branch.Attempts.Count<=2048&&branch.Saves.Count<=2048&&
            branch.Attempts.Select(a=>a.Id).SequenceEqual(Enumerable.Range(1,branch.Attempts.Count))&&
            branch.Saves.Select(s=>s.Id).SequenceEqual(Enumerable.Range(1,branch.Saves.Count)),"run-attempt-order");
        Codec.Require(branch.ActiveAttempt==0&&branch.Attempts.Count==0||branch.Attempts.Any(a=>a.Id==branch.ActiveAttempt),"run-active-attempt-missing");
        Codec.Require(branch.Attempts.All(a=>run.Visits.Any(v=>v.Id==a.Visit)&&
            (a.Parent is null||a.Parent>0&&a.Parent<a.Id)&&
            (a.RestoredSave is null||branch.Saves.Any(s=>s.Id==a.RestoredSave&&s.Visit==a.Visit&&s.Attempt==a.Parent))),"run-attempt-parent-or-anchor");
        Codec.Require(branch.Saves.All(s=>branch.Attempts.Any(a=>a.Id==s.Attempt&&a.Visit==s.Visit)&&
            s.Path.Distinct().Count()==s.Path.Length&&s.Path.SequenceEqual(s.Path.Order())&&s.Path.All(id=>id>0&&id<=run.Operations.Count)),"run-save-point-path");
        Codec.Require(run.Operations.All(o=>o.SourceSequence is null&&o.Attempt is {} id&&branch.Attempts.Any(a=>a.Id==id&&a.Visit==o.Visit)),"run-operation-attempt-missing");
    }
}
