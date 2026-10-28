using System.Reflection;
using HarmonyLib;

if (args.Length != 1) throw new ArgumentException("Pass probe DLL path.");
var dll = Path.GetFullPath(args[0]);
var assembly = Assembly.LoadFrom(dll);
if (typeof(Godot.SceneTree).GetEvent(nameof(Godot.SceneTree.ProcessFrame))?.EventHandlerType != typeof(Action))
    throw new InvalidOperationException("SceneTree.ProcessFrame must be a parameterless frame callback.");
if (assembly.GetTypes().Any(t => t.IsSubclassOf(typeof(Godot.Node))))
    throw new InvalidOperationException("This plain .NET probe must not use an unregistered custom Godot Node ticker.");
var harmony = new Harmony("STS2ReplayMod.SPIKEINV012A.OfflinePatchSmoke");
try
{
    harmony.PatchAll(assembly);
    var names = harmony.GetPatchedMethods().Select(m => m.DeclaringType!.FullName + "::" + m.Name).OrderBy(x => x).ToArray();
    if (names.Length != 9) throw new InvalidOperationException("Expected 9 exact target methods, got " + names.Length);
    foreach (var name in names) Console.WriteLine("PATCHED " + name);
}
finally { harmony.UnpatchAll(harmony.Id); }
if (harmony.GetPatchedMethods().Any()) throw new InvalidOperationException("Unpatch incomplete.");
Console.WriteLine("PASS PATCH/UNPATCH 9/9; SceneTree frame event available; no custom Node ticker; no target invoked");
