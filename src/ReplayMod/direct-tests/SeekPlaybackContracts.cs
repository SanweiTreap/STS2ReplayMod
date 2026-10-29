using ReplayMod;

internal static class SeekPlaybackContracts
{
    internal static string[] Run()
    {
        var passed=new List<string>();
        void Need(bool ok,string why){if(!ok)throw new InvalidDataException(why);}
        void Test(string name,Action action){action();passed.Add(name);}
        Test("all-eight-gate-auto-finite-combinations",()=>
        {
            foreach(var running in new[]{false,true})foreach(var auto in new[]{false,true})foreach(var finite in new[]{false,true})
            {
                var p=new SeekPlayback();if(running)p.ToggleRunning();if(auto)p.ToggleAuto();if(finite)p.Step();
                var expected=running&&(auto||finite);
                Need(p.WantsAdvance==expected&&p.Take()==expected,"advance-permission-table");
                Need(p.Pending==(finite&&!expected?1:0)&&p.Running==running&&p.Auto==auto,"dispatcher-changed-mode-or-double-consumed");
                Need(p.Take()==(running&&auto)&&p.Pending==(finite&&!running?1:0),"automatic-and-finite-duplicate-node");
            }
        });
        Test("paused-three-steps-play-exactly-three-and-running-gate-stays-open",()=>
        {
            var p=new SeekPlayback();for(var i=0;i<3;i++)p.Step();
            Need(!p.Running&&!p.Auto&&p.Pending==3&&!p.Take(),"paused-step-started-native-operation");
            p.ToggleRunning();var nodes=0;while(p.Take())nodes++;
            Need(nodes==3&&p.Pending==0&&p.Running&&!p.Auto,"finite-queue-became-infinite-or-gate-closed");
            p.Step();Need(p.Take()&&!p.Take()&&p.Running,"running-empty-gate-did-not-accept-new-step");
        });
        Test("pause-current-node-preserves-remainder-and-new-paused-steps",()=>
        {
            var p=new SeekPlayback();for(var i=0;i<4;i++)p.Step();p.ToggleRunning();
            Need(p.Take()&&p.Pending==3,"current-node-not-reserved-once");
            p.ToggleRunning();Need(!p.Take()&&p.Pending==3,"pause-lost-pending-steps-or-started-next");
            p.Step();Need(p.Pending==4&&!p.Take(),"step-bypassed-pause");
            p.ToggleRunning();var nodes=0;while(p.Take())nodes++;
            Need(nodes==4&&p.Running&&!p.Auto,"resume-did-not-finish-only-finite-remainder");
        });
        Test("auto-toggle-never-opens-gate-and-pause-keeps-auto",()=>
        {
            var p=new SeekPlayback();p.Step();p.Step();p.ToggleAuto();
            Need(p.Auto&&!p.Running&&!p.Take()&&p.Pending==2,"enable-auto-resumed-paused-viewer");
            p.ToggleRunning();Need(p.Take()&&p.Pending==1,"auto-and-finite-not-coalesced");
            p.ToggleRunning();Need(p.Auto&&p.Pending==1&&!p.Take(),"pause-reset-auto-or-remainder");
            p.ToggleAuto();Need(!p.Running&&!p.Auto&&p.Pending==1,"disable-auto-altered-gate-or-finite");
            p.ToggleRunning();Need(p.Take()&&!p.Take()&&p.Running,"auto-off-did-not-respect-finite-remainder");
        });
        Test("seek-and-back-clear-route-pause-preserve-auto-stop-clears-session",()=>
        {
            var p=new SeekPlayback();p.ToggleAuto();p.ToggleRunning();p.Step();p.Step();p.Seek();
            Need(!p.Running&&p.Auto&&p.Pending==0&&!p.Take(),"seek-carried-old-route-or-resumed-auto");
            p.ToggleRunning();Need(p.Take()&&p.Pending==0,"play-lost-preserved-auto");
            p.Step();p.Stop();Need(!p.Running&&!p.Auto&&p.Pending==0&&!p.Take(),"stop-carried-session-controls");
            p.ToggleRunning();p.Step();Need(p.Take()&&!p.Take()&&p.Running,"new-session-carried-auto");
        });
        return passed.ToArray();
    }
}
