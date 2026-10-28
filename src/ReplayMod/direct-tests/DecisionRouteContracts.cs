using System.Text.Json;
using ReplayMod;

internal static class DecisionRouteContracts
{
    internal static void Run(string output,string originalPath)
    {
        var checks=new List<string>();void Need(bool ok){if(!ok)throw new Exception("decision-route-contract");}
        void Reject(Action run){try{run();}catch(InvalidDataException){return;}throw new Exception("bad-route-transition-accepted");}
        var bytes=File.ReadAllBytes(originalPath);var envelope=JsonSerializer.Deserialize<ClipEnvelope>(bytes,Codec.Json)!;var clip=envelope.Data;
        Need(Codec.Hash(Codec.Bytes(clip))==envelope.SHA256);
        var hand=clip.Choices!.Single(c=>c.Source=="CARD.PREPARED");var brew=clip.Choices!.Single(c=>c.Source=="POTION.GAMBLERS_BREW");
        foreach(var f in clip.Choices!)
        {
            var projected=ChoiceTrace.ResultDemo(f);ChoiceTrace.Validate(f with{Events=projected});
            Need(projected.All(e=>e.Kind!="Deselect")&&projected.Where(e=>e.Kind=="Select").Select(e=>e.Option!.Value).SequenceEqual(f.Chosen));
        }
        Need(hand.Events!.Count(e=>e.Kind=="Deselect")==3&&ChoiceTrace.ResultDemo(hand).Length==5);
        Need(brew.Events!.Count(e=>e.Kind=="Deselect")==1&&ChoiceTrace.ResultDemo(brew).Length==10);
        checks.Add("actual-recording-final-projection-uses-confirmed-instance-order-and-keeps-original-deselect-events");
        var route=new DecisionCursor();Need(!route.Ready&&!route.CanBack&&route.Position==-1);
        route.Append();Need(route.Ready&&!route.CanBack&&route.Position==0);
        Need(route.Step()&&!route.Step()&&route.Take()==1&&route.Moving);route.Append(); // first final selection
        Need(route.Step()&&route.Take()==1);route.Append(); // second final selection
        Need(route.Back()&&route.Take()==-1);route.Arrive(1);
        Need(route.Back()&&route.Take()==-1);route.Arrive(0);
        Need(!route.CanBack&&route.Step()&&route.Take()==1);route.Arrive(1);
        Need(route.Step()&&route.Take()==1);route.Arrive(2);
        checks.Add("selection-prefix-nodes-walk-identical-edges-backward-and-forward-without-crossing-origin");
        var state=new Model();var before=new IdentityMemento([state],v=>v is Model,_=>true);
        Need(route.Step()&&route.Take()==1);route.EngineWait();Need(!route.Ready&&!route.CanBack);
        state.Energy--;state.Discard.Add(8);state.TriggerCount++;state.Rng++;var after=new IdentityMemento([state],v=>v is Model,_=>true);
        route.Append();Need(route.Position==3&&state.TriggerCount==1);
        Need(route.Back()&&route.Take()==-1);before.Restore();route.Arrive(2);
        Need(state.Energy==3&&state.Discard.Count==0&&state.TriggerCount==0&&state.Rng==4);
        Need(route.Step()&&route.Take()==1);after.Restore();route.Arrive(3);
        Need(state.Energy==2&&state.Discard.SequenceEqual(new[]{8})&&state.TriggerCount==1&&state.Rng==5);
        checks.Add("committed-effect-history-restores-real-data-and-forward-restores-result-without-reexecuting-effect");
        Need(route.Back()&&route.Take()==-1);route.Arrive(2);Need(route.Toggle()&&route.Take()==1);route.Arrive(3);
        Need(route.Take()==1);route.EngineWait();Need(!route.Ready&&route.Playing);
        route.Append();route.Pause();Need(route.CanBack&&route.Position==4&&!route.Playing);
        checks.Add("history-play-automatically-reaches-frontier-and-only-then-requests-new-native-execution");
        Reject(()=>route.Arrive(0));Reject(()=>route.Append());
        route.Stop();Need(!route.Ready&&!route.CanBack&&!route.Step()&&!route.Back()&&!route.Toggle()&&route.Take()==0);
        Need(bytes.SequenceEqual(File.ReadAllBytes(originalPath))&&clip.Choices!.Sum(c=>c.Events!.Length)==41);
        checks.Add("overlapping-or-skipped-node-arrivals-stop-controls-and-original-clip-integrity");
        File.WriteAllBytes(output,Codec.Bytes(new{Result="CONTENT002_D109_FINAL_ROUTE_DATA_CONTRACTS_PASS_NOT_NATIVE_RUNTIME",Count=checks.Count,Checks=checks,
            RawEvents=41,ProjectedEvents=clip.Choices!.Sum(f=>ChoiceTrace.ResultDemo(f).Length),OriginalBytesUnchanged=true,NativeGameObjectsConstructed=false,NativeUiOrTaskIntegrationTested=false}));
        Console.WriteLine("DECISION_ROUTE_CONTRACTS_PASS "+checks.Count);
    }
    private sealed class Model {public int Energy=3,TriggerCount,Rng=4;public List<int> Discard=[];}
}
