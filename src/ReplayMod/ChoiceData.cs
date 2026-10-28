namespace ReplayMod;

public sealed record ChoiceCard(string Model,int Upgrade,bool Mutable,uint? CombatIndex,int? DeckIndex,string Fingerprint,string Label);
public sealed record ChoiceFrame(int Sequence,int Boundary,int Turn,string Kind,string? Source,int Min,int Max,bool Cancelable,bool Manual,
    ChoiceCard[][] Options,int[] Chosen,double BeganMilliseconds,double DurationMilliseconds,string NativeType,uint? NativeId,bool Automated,int PlayerSlot=0,int? RequestedMax=null,
    [property:System.Text.Json.Serialization.JsonIgnore(Condition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)] ChoiceEvent[]? Events=null);

// Times are relative to this choice. Null Events means a legacy result-only recording.
public sealed record ChoiceEvent(string Kind,int? Option,double Milliseconds);

internal static class ChoiceTrace
{
    internal static ChoiceEvent[] ResultDemo(ChoiceFrame f)
    {
        var events=new List<ChoiceEvent>{new("Open",null,0)};
        foreach(var i in f.Chosen)events.Add(new("Select",i,0));
        if(f.Kind=="FromChooseACardScreen")
        {
            // Native generated-card UI submits the click, even when command metadata says Manual.
            if(f.Chosen.Length==0)events.Add(new("Skip",null,0));
        }
        else if(f.Kind is "FromHand" or "FromHandForUpgrade" or "FromChooseABundleScreen"||f.Manual||f.Chosen.Length<f.Max)
            events.Add(new("Confirm",null,0));
        events.Add(new("Close",null,0));return events.ToArray();
    }
    internal static void Validate(ChoiceFrame f)
    {
        if(f.Events is not {} events)return;
        Codec.Require(!f.Automated&&events.Length is >=2 and <=4096&&events[0].Kind=="Open"&&events[^1].Kind=="Close","choice-trace-envelope");
        var selected=new List<int>();var at=0d;var submitted=false;
        for(var i=0;i<events.Length;i++)
        {
            var e=events[i];Codec.Require(double.IsFinite(e.Milliseconds)&&e.Milliseconds>=at&&e.Milliseconds<=f.DurationMilliseconds+1,"choice-event-time");at=e.Milliseconds;
            Codec.Require(e.Kind is "Open" or "Select" or "Deselect" or "Confirm" or "Skip" or "Cancel" or "Close","choice-event-kind");
            Codec.Require(e.Kind is "Select" or "Deselect"?e.Option is >=0&&e.Option<f.Options.Length:e.Option is null,"choice-event-option");
            Codec.Require(!submitted||e.Kind=="Close","choice-event-after-submit");
            switch(e.Kind)
            {
                case "Open":Codec.Require(i==0,"choice-event-open");break;
                case "Select":Codec.Require(!selected.Contains(e.Option!.Value)&&selected.Count<f.Max,"choice-event-select");selected.Add(e.Option.Value);break;
                case "Deselect":Codec.Require(selected.Remove(e.Option!.Value),"choice-event-deselect");break;
                case "Cancel":Codec.Require(f.Kind=="FromChooseABundleScreen"&&selected.Count==1,"choice-event-cancel");selected.Clear();break;
                case "Skip":Codec.Require(f.Cancelable&&selected.Count==0,"choice-event-skip");submitted=true;break;
                case "Confirm":Codec.Require(selected.Count>=Math.Min(f.Min,f.Options.Length)||f.Cancelable,"choice-event-confirm");submitted=true;break;
                case "Close":
                    Codec.Require(i==events.Length-1&&selected.Count==f.Chosen.Length&&selected.ToHashSet().SetEquals(f.Chosen)&&
                        (f.Kind is not ("FromHand" or "FromHandForUpgrade")||selected.SequenceEqual(f.Chosen)),"choice-event-final-result");
                    Codec.Require(submitted||f.Kind=="FromChooseACardScreen"&&selected.Count==1||
                        f.Kind is "FromCombatPile" or "FromSimpleGrid" or "FromSimpleGridForRewards"&&!f.Manual&&selected.Count>=f.Max,"choice-event-missing-submit");break;
            }
        }
    }
}

internal static class ChoiceContract
{
    internal static bool Same(ChoiceCard a,ChoiceCard b)=>a with{Label=""}==b with{Label=""};
    internal static void Validate(ChoiceFrame[] frames,int operations)
    {
        Codec.Require(frames.Length<=1024,"choice-count-limit");var boundary=0;
        for(var i=0;i<frames.Length;i++)
        {
            var f=frames[i];Codec.Require(f.Sequence==i+1&&f.PlayerSlot==0&&f.Boundary>=boundary&&f.Boundary<=operations&&f.Turn>=0&&
                f.Min>=0&&f.Max>=f.Min&&f.Max<=1024&&f.Kind.Length is >0 and <100&&f.Options.Length<=1024&&
                (f.RequestedMax is null||f.RequestedMax>=f.Max)&&
                double.IsFinite(f.BeganMilliseconds)&&f.BeganMilliseconds>=0&&double.IsFinite(f.DurationMilliseconds)&&f.DurationMilliseconds>=0,"choice-metadata");
            boundary=f.Boundary;
            Codec.Require(f.Options.All(o=>o.Length is >0 and <=128&&o.All(c=>c.Model.StartsWith("CARD.",StringComparison.Ordinal)&&c.Model.Length<=128&&
                c.Upgrade is >=0 and <=20&&c.Fingerprint.Length==64&&c.Label.Length<=512&&c.CombatIndex is null or <=65535&&c.DeckIndex is null or >=0 and <128)),"choice-candidates");
            Codec.Require(f.Chosen.All(n=>n>=0&&n<f.Options.Length)&&f.Chosen.Distinct().Count()==f.Chosen.Length&&f.Chosen.Length<=f.Max&&
                (f.Chosen.Length>=Math.Min(f.Min,f.Options.Length)||f.Cancelable),"choice-result-count-or-index");
            Codec.Require(f.NativeType is "Index" or "CombatCard" or "DeckCard","choice-result-type");
            ChoiceTrace.Validate(f);
        }
    }
    internal static void Match(ChoiceFrame expected,ChoiceFrame actual)
    {
        Codec.Require(expected.PlayerSlot==actual.PlayerSlot&&expected.Boundary==actual.Boundary&&expected.Turn==actual.Turn&&expected.Kind==actual.Kind&&expected.Source==actual.Source&&
            expected.Min==actual.Min&&expected.Max==actual.Max&&expected.RequestedMax==actual.RequestedMax&&expected.Cancelable==actual.Cancelable&&expected.Manual==actual.Manual&&
            expected.Automated==actual.Automated&&expected.Options.Length==actual.Options.Length,"choice-context-mismatch");
        for(var i=0;i<expected.Options.Length;i++)Codec.Require(expected.Options[i].Length==actual.Options[i].Length&&
            expected.Options[i].Zip(actual.Options[i]).All(p=>Same(p.First,p.Second)),"choice-candidate-mismatch-"+i);
    }
}
