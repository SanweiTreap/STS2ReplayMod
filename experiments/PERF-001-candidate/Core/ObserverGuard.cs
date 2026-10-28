namespace Perf001Candidate.Core;

public static class ObserverGuard
{
    public static void Run(Action observation,Action<Exception> fail)
    {
        try{observation();}catch(Exception ex){try{fail(ex);}catch{/* observer cannot replace an original result/exception */}}
    }
}
