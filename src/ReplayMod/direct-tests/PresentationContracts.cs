using ReplayMod;

internal static class PresentationContracts
{
    internal static void Run(string output)
    {
        void Need(bool value,string why){if(!value)throw new InvalidDataException(why);}
        // Native draw A and B overlap, while a later discard starts after both have begun.
        // A single clock must not turn these into sequential per-card durations.
        double[] times=[0,.04,.08,.08,.16,.24,.48];PresentationTiming.Validate(times);
        var index=0;var checks=new List<string>();
        foreach(var (at,expected) in new (double,int)[]{(0,0),(.03,0),(.08,3),(.15,3),(.2,4),(.4,5),(1,6)})
        {index=PresentationTiming.Frame(times,at,index);Need(index==expected,"native-batch-shared-clock-or-equal-time-order");}
        checks.Add("Shared clock preserves overlapping batch timing, duplicate sample order and late-frame catch-up");
        Need(Math.Abs(PresentationTiming.Blend(.08,.16,.12)-.5)<1e-10,"native-motion-interpolation");
        Need(PresentationTiming.Blend(.08,.16,0)==0&&PresentationTiming.Blend(.08,.16,1)==1&&PresentationTiming.Blend(.08,.08,.08)==0,"native-motion-boundary");
        checks.Add("Interpolation follows adjacent actual samples, without fixed per-card duration or overshoot");
        foreach(var bad in new double[][]{[],[-.1],[0,.2,.1],[double.NaN],[double.PositiveInfinity]})
        {var rejected=false;try{PresentationTiming.Validate(bad);}catch(InvalidDataException){rejected=true;}Need(rejected,"invalid-native-timing-accepted");}
        checks.Add("Reject empty, negative, non-finite and reordered display samples");
        var cache=new DisplayVariantCache<EqualLookingCard,object>();var first=new EqualLookingCard("Strike");var second=new EqualLookingCard("Strike");var copies=0;
        object Create(){copies++;return new object();}
        var a=cache.Get(first,"base",Create);var b=cache.Get(second,"base",Create);var upgraded=cache.Get(first,"upgraded",Create);
        Need(!ReferenceEquals(a,b)&&!ReferenceEquals(a,upgraded)&&ReferenceEquals(a,cache.Get(first,"base",Create))&&copies==3,"frozen-display-identity-or-history-alias");
        cache.Clear();Need(!ReferenceEquals(a,cache.Get(first,"base",Create))&&copies==4,"display-cache-outlived-session");
        checks.Add("Equal-looking instances never alias; old content variants reuse their frozen view; clear releases session cache");
        var ownership=new PrivateDisplayOwnership<EqualLookingCard>();var privateCard=new EqualLookingCard("Strike");var nativeCard=new EqualLookingCard("Strike");
        ownership.Mark(privateCard);var visibleCache=new List<EqualLookingCard>{privateCard};var nativeReturns=new List<EqualLookingCard>();
        Action DeferredReturn(EqualLookingCard card)=>()=>{if(!ownership.Contains(card))nativeReturns.Add(card);};
        var latePrivateFree=DeferredReturn(privateCard);var ordinaryNativeFree=DeferredReturn(nativeCard);
        visibleCache.Clear();latePrivateFree();ordinaryNativeFree();
        Need(nativeReturns.Count==1&&ReferenceEquals(nativeReturns[0],nativeCard)&&ownership.Contains(privateCard)&&!ownership.Contains(nativeCard),"deferred-private-free-contaminated-native-pool");
        checks.Add("Deferred disposal remains private after view-cache clear, while equal-looking native cards retain their native return path");
        var navigation=new PresentationNavigation();
        Need(!navigation.Request("toggle")&&navigation.Request("step")&&!navigation.Request("back")&&navigation.Pending,"presentation-overlapping-navigation");
        Need(navigation.Take()==1&&!navigation.Pending&&navigation.Take()==0,"presentation-navigation-delivered-more-than-once");
        var route=new DecisionCursor();route.Append();Need(route.Step()&&route.Take()==1,"presentation-route-begin");route.Append();
        Need(route.Back()&&route.Take()==-1,"presentation-route-back");route.Arrive(0);
        Need(route.Step()&&route.Take()==1&&navigation.Request("back"),"presentation-history-step");
        // The target is handed off exactly once; a queued Back returns along that same edge.
        route.Arrive(1);Need(navigation.Take()==-1&&route.Back()&&route.Take()==-1,"presentation-queued-back-target");route.Arrive(0);
        Need(route.Step()&&route.Take()==1,"presentation-return-frontier");route.Arrive(1);
        Need(route.Step()&&route.Take()==1,"presentation-native-begin");route.EngineWait();
        Need(navigation.Request("step")&&!route.Ready&&!route.Step(),"presentation-native-business-interrupted");
        route.Append();Need(navigation.Take()==1&&route.Step()&&route.Take()==1,"presentation-native-boundary-drain");
        navigation.Request("back");navigation.Clear();Need(navigation.Take()==0,"presentation-stop-left-navigation");
        Need(navigation.Request("pause")&&navigation.Take()==2,"presentation-pause-not-accepted");
        checks.Add("One queued navigation survives an atomic visual handoff; Back returns to its source; native business stays atomic and Stop clears intent");
        var settledStep=new PresentationNavigation();Need(settledStep.Request("step")&&settledStep.Pending&&!settledStep.InterruptsDisplay,"step-fast-forwarded-historical-settlement");
        Need(!settledStep.Request("step")&&settledStep.Take()==1&&!settledStep.Pending,"queued-step-duplicated");
        Need(settledStep.Request("back")&&settledStep.InterruptsDisplay,"back-cannot-retire-pure-presentation");settledStep.Clear();
        Need(settledStep.Request("pause")&&settledStep.InterruptsDisplay,"pause-cannot-retire-pure-presentation");settledStep.Clear();
        // Attack potion entry is an event inside the sampled interval, not end+another UI delay.
        PresentationTiming.ValidateChoice(.51,.87,.91);var delivered=false;
        Need(!PresentationTiming.Once(.51,.49,ref delivered)&&!delivered,"choice-opened-before-native-marker");
        Need(PresentationTiming.Once(.51,.53,ref delivered)&&delivered,"choice-waited-for-tail-instead-of-source-marker");
        Need(!PresentationTiming.Once(.51,.91,ref delivered),"choice-opened-twice-at-tail");
        var unseen=false;Need(!PresentationTiming.Once(null,1,ref unseen),"invented-choice-marker");
        foreach(var invalid in new (double?,double?,double)[]{(.8,.5,1),(-.1,.3,1),(.5,1.2,1),(null,.5,1),(double.NaN,.5,1)})
        {
            var rejected=false;try{PresentationTiming.ValidateChoice(invalid.Item1,invalid.Item2,invalid.Item3);}catch(InvalidDataException){rejected=true;}
            Need(rejected,"invalid-choice-clock-accepted");
        }
        checks.Add("Step queues without truncating history; choice-open fires once on source clock before readiness/tail, with invalid markers rejected");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllBytes(output,Codec.Bytes(new{Result="DISPLAY_CLOCK_CONTRACTS_PASSED_NOT_NATIVE_RENDER_VERIFICATION",Checks=checks,NativeUiConstructed=false}));
        Console.WriteLine("DISPLAY_CLOCK_CONTRACTS_PASSED");
    }
    private sealed record EqualLookingCard(string Id);
}
