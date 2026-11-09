namespace ReplayMod;

internal sealed record RunRoomPoint(RoomVisit Visit,int? Attempt,string? RoomType,string? Model,RunOperation? Entry,RunOperation? Anchor)
{
    internal int? StartSource=>Anchor is {} anchor?anchor.SourceSequence??anchor.Sequence:Entry is null?0:null;
    internal bool Direct=>Entry is null||Anchor?.Visit==Visit.Id;
}

internal static class RunRoomPoints
{
    internal static bool BattleAnchor(RunOperation operation)=>operation is {Kind:"battle",Status:"complete",Clip:not null}
        &&operation.Battle?.Entry!="event";

    internal static RunRoomPoint[] Read(RunDocument route,int[] discontinuities)
    {
        var operations=route.Operations;
        var earliest=Math.Max(operations.LastOrDefault(o=>o is not {Status:"complete",Kind:not "gap"})?.Sequence+1??1,
            discontinuities.LastOrDefault());
        var visits=route.Visits.ToDictionary(visit=>visit.Id);
        var points=new List<RunRoomPoint>();
        if(operations.FirstOrDefault() is {} first&&visits[first.Visit] is {Act:0,MapType:"Ancient",Model:"EVENT.NEOW"} neow)
            points.Add(new(neow,first.Attempt,neow.RoomType,neow.Model,
                earliest<=1&&RunStore.HasNativeOpening(route)?null:first,null));
        RunOperation? anchor=null;
        for(var i=0;i+1<operations.Count;i++)
        {
            var entry=operations[i];
            if(entry.Sequence>=earliest&&BattleAnchor(entry))anchor=entry;
            var next=operations[i+1];
            if(entry.Kind!="map"||entry.Visit==next.Visit||!visits.TryGetValue(next.Visit,out var visit)||
                entry.Argument.Row!=visit.Row||entry.Argument.Col!=visit.Col)continue;
            var destination=RunRoutes.Destination(route,entry,next);
            var selected=next.Sequence>=earliest&&BattleAnchor(next)?next:entry.Sequence>=earliest?anchor:null;
            points.Add(new(visit,next.Attempt,destination.Type,destination.Model,entry,selected));
        }
        // Final route order matters when old attempts reused an act coordinate.
        return points.DistinctBy(point=>point.Visit.Id).ToArray();
    }
}
