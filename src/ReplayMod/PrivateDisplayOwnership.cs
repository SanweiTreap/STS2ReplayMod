using System.Runtime.CompilerServices;

namespace ReplayMod;

// Deferred native disposal may arrive after a display cache has been cleared. Weak identity markers
// survive for that callback without keeping the disposed view or its session alive.
internal sealed class PrivateDisplayOwnership<T> where T:class
{
    private readonly ConditionalWeakTable<T,object> _owned=new();
    internal void Mark(T value)=>_owned.GetValue(value,_=>new object());
    internal bool Contains(T value)=>_owned.TryGetValue(value,out _);
}
