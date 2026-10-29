namespace ReplayMod;

internal static class PileSubscriptionRepair
{
    internal static int AttachMissing<T>(IEnumerable<T> piles,IEnumerable<T> subscribed,Action<T> attach) where T:class
    {
        var present=new HashSet<T>(subscribed,ReferenceEqualityComparer.Instance);var added=0;
        foreach(var pile in piles)if(present.Add(pile)){attach(pile);added++;}
        return added;
    }
}
