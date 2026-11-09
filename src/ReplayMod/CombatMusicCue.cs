namespace ReplayMod;

internal readonly record struct CombatMusicCue(float Value,int Revision)
{
    // Test Subject's phase cue may have advanced the event even when its
    // parameter ends up with the same numeric value at two visited points.
    internal CombatMusicCue Written(string parameter,float value)=>
        new(value,parameter=="test_subject_progress"?Revision+1:0);
}

// Sections of the two shipped FMOD events, verified against the fixed game bank.
// Entry points lie just after their native transition destinations.
internal readonly record struct BossScoreSection(int Entry,int Lower,int Upper,int Cue)
{
    internal bool Contains(int position)=>position>=Lower&&position<Upper;
    internal static BossScoreSection Subject(int respawns,bool knockedOut)=> (respawns,knockedOut) switch
    {
        (0,true)=>new(84200,84000,137000,1),
        (1,false)=>new(137400,137000,236800,2),
        (1,true)=>new(237000,236800,272100,1),
        (2,false)=>new(272400,272100,int.MaxValue,2),
        _=>new(0,0,84000,0)
    };
    internal static BossScoreSection Beast(bool second)=>second?new(83500,83000,int.MaxValue,1):new(0,0,83000,0);
}
