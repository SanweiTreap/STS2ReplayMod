using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json.Nodes;

namespace Perf001;

public static class Tests
{
    public static object Run(string workspace, string output)
    {
        var passed = new List<string>();
        void Check(string name, Action test) { test(); passed.Add(name); }
        void Need(bool value) { if (!value) throw new InvalidDataException("test-assertion"); }
        void Reject(Action action) { try { action(); } catch (ArgumentException) { return; } catch (InvalidDataException) { return; } throw new InvalidDataException("expected-rejection"); }
        Check("nearest-rank-not-interpolation", () => { Need(Statistics.Quantile([5,1,5,2],.5)==2); Need(Statistics.Quantile([5,1,5,2],0)==1); Need(Statistics.Quantile([5,1,5,2],1)==5); });
        Check("known-100-tail-ranks", () => { var a=Enumerable.Range(1,100).Select(x=>(double)x);var s=Statistics.Describe(a);Need(s.P50Milliseconds==50&&s.P95Milliseconds==95&&s.P99Milliseconds==99); });
        Check("empty-is-unavailable-not-zero", () => {var s=Statistics.Describe([]);Need(s.Count==0&&s.MeanMilliseconds is null&&s.MaxMilliseconds is null);Reject(()=>Statistics.Quantile([],.5));});
        Check("small-action-sample-retains-raw-no-percentiles", () => {var s=Statistics.Describe([1,100]);Need(s.P99Milliseconds is null&&s.P50Milliseconds is null&&s.SmallSampleValuesMilliseconds.SequenceEqual(new[]{1d,100d}));});
        Check("tail-retained-and-threshold-strict", () => {var s=Statistics.Describe(Enumerable.Repeat(1d,999).Append(10000));Need(s.Count==1000&&s.MaxMilliseconds==10000&&s.ThresholdCounts.Single(x=>x.GreaterThanMilliseconds==100).Count==1);Need(Statistics.Describe([100d]).ThresholdCounts.Single(x=>x.GreaterThanMilliseconds==100).Count==0);});
        Check("reject-invalid-duration-numbers", () => {foreach(var x in new[]{double.NaN,double.PositiveInfinity,-1})Reject(()=>Statistics.Describe([x]));});
        Check("reject-invalid-quantile-probability", () => {foreach(var x in new[]{double.NaN,-.1,1.1})Reject(()=>Statistics.Quantile([1],x));});
        Check("ticks-use-supplied-frequency-not-timespan", () => {Need(Math.Abs(Statistics.TicksToMilliseconds(1,3)-1000d/3)<1e-9);Need(Statistics.TicksToMilliseconds(15,10)==1500);Reject(()=>Statistics.TicksToMilliseconds(-1,10));Reject(()=>Statistics.TicksToMilliseconds(1,0));});
        Check("monotonic-timestamps-and-duration", () => {var w=new TimestampWindow(10);w.Add(100);w.Add(110);w.Add(140);Need(w.Intervals().SequenceEqual(new[]{1000d,3000d}));});
        Check("duplicate-reversed-negative-timestamps-reject-retain-raw", () => {foreach(var a in new[]{new long[]{100,100},new long[]{100,99},new long[]{-1,100}}){var w=new TimestampWindow(10);foreach(var t in a)w.Add(t);Need(w.Raw.SequenceEqual(a));Reject(()=>w.Intervals());}});
        using var fixtures = new Fixtures(workspace);var refs=new References(fixtures,5);
        Check("actual-five-public-DTOs-pass-source-gate",()=> {Need(Fixtures.Approved.Length==5);fixtures.VerifyUnchanged();Need(fixtures.Input(0).GetProperty("Player").GetProperty("Hand").GetArrayLength()==5);});
        Check("candidate-encoding-checksum-matches-independent-envelopes",()=> {using var h=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);for(var i=1;i<=5;i++)h.AppendData(Codec.Encode(fixtures,i).Bytes.Span);Need(Convert.ToHexString(h.GetHashAndReset())==refs.SHA256);});
        Check("frozen-buffer-and-oracle-isolation",()=> {var raw=new byte[]{1,2,3};var frozen=new FrozenRecord(1,0,raw);raw[0]=9;Need(frozen.Bytes.Span[0]==1);var expected=fixtures.Oracle(0);var mutable=fixtures.Oracle(0);mutable["Player"]!["Hp"]=1;Need(expected["Player"]!["Hp"]!.GetValue<int>()==80);Need(fixtures.Input(0).GetProperty("Player").GetProperty("Hp").GetInt32()==80);});
        Check("real-writer-fully-drained-and-parseable",()=> {var path=Path.Combine(output,"valid.ndjson");var w=new BoundedWriter(path,5,new(1024*1024));foreach(var r in refs.Records)Need(w.TryEnqueue(r));w.CompleteAdding();var o=w.Finish(TimeSpan.FromSeconds(3));Need(o.Complete&&o.Accepted==5&&o.Written==5&&o.WriteCalls==5&&o.FlushCalls==1&&o.WrittenBytes==refs.Bytes);Need(IntegrityCheck.FileRecords(path,fixtures,refs).Complete);});
        Check("overflow-cannot-pass-even-after-accepted-records-drain",()=> {using var gate=new ManualResetEventSlim();var w=new BoundedWriter(Path.Combine(output,"overflow.ndjson"),2,new(1024*1024),capacity:1,testGate:gate);Need(w.TryEnqueue(refs.Records[0]));Need(!w.TryEnqueue(refs.Records[1]));w.CompleteAdding();gate.Set();var o=w.Finish(TimeSpan.FromSeconds(3));Need(!o.Complete&&o.Overflows==1&&o.Written==1);});
        Check("byte-capacity-overflow-is-failure",()=> {using var gate=new ManualResetEventSlim();var w=new BoundedWriter(Path.Combine(output,"byte-limit.ndjson"),1,new(1024*1024),capacityBytes:1,testGate:gate);Need(!w.TryEnqueue(refs.Records[0]));w.CompleteAdding();gate.Set();Need(!w.Finish(TimeSpan.FromSeconds(3)).Complete);});
        Check("undrained-timeout-stays-failed-after-later-stop",()=> {using var gate=new ManualResetEventSlim();var w=new BoundedWriter(Path.Combine(output,"undrained.ndjson"),1,new(1024*1024),testGate:gate);Need(w.TryEnqueue(refs.Records[0]));w.CompleteAdding();Need(!w.Finish(TimeSpan.Zero).Complete);gate.Set();Need(!w.Finish(TimeSpan.FromSeconds(3)).Complete);});
        Check("writer-io-fault-never-complete",()=> {var w=new BoundedWriter(Path.Combine(output,"fault.ndjson"),1,new(1024*1024),faultSequence:1);Need(w.TryEnqueue(refs.Records[0]));w.CompleteAdding();var o=w.Finish(TimeSpan.FromSeconds(3));Need(!o.Complete&&o.Failure=="writer-IOException");});
        Check("output-budget-exhaustion-never-complete",()=> {var w=new BoundedWriter(Path.Combine(output,"budget.ndjson"),1,new(0));Need(w.TryEnqueue(refs.Records[0]));w.CompleteAdding();Need(!w.Finish(TimeSpan.FromSeconds(3)).Complete);});
        Check("duplicate-or-out-of-order-sequence-reject",()=> {var w=new BoundedWriter(Path.Combine(output,"sequence.ndjson"),2,new(1024*1024));Need(!w.TryEnqueue(refs.Records[1]));w.CompleteAdding();Need(!w.Finish(TimeSpan.FromSeconds(3)).Complete);});
        Check("oversize-record-reject",()=> {var w=new BoundedWriter(Path.Combine(output,"oversize.ndjson"),1,new(1024*1024),maximumRecordBytes:10);Need(!w.TryEnqueue(refs.Records[0]));w.CompleteAdding();Need(!w.Finish(TimeSpan.FromSeconds(3)).Complete);});
        Check("missing-complete-adding-cannot-pass",()=> {var w=new BoundedWriter(Path.Combine(output,"not-completed.ndjson"),0,new(1024*1024));Need(!w.Finish(TimeSpan.Zero).Complete);w.CompleteAdding();Need(!w.Finish(TimeSpan.FromSeconds(3)).Complete);});
        Check("wrong-output-payload-or-order-detected",()=> {var p=Path.Combine(output,"wrong.ndjson");using(var f=new FileStream(p,FileMode.CreateNew)){foreach(var r in refs.Records.Reverse())f.Write(r.Bytes.Span);}Reject(()=>IntegrityCheck.FileRecords(p,fixtures,refs));});
        fixtures.VerifyUnchanged();
        return new {Schema=1,Revision="offline-r1",Result="PASS_FUNCTIONAL_STATISTICS_AND_INTEGRITY_ONLY",Tests=passed,Count=passed.Count,Inputs=Fixtures.Approved,InputsUnchanged=true,GameRuntimeAttempts=0,TimingBenchmarkRun=false,Note="Functional assertions only; test durations are not performance measurements"};
    }
}
