using System.Reflection;
using HarmonyLib;

if (args.Length != 1) throw new ArgumentException("Usage: PatchSmoke <absolute probe DLL>");
var assembly = Assembly.LoadFrom(Path.GetFullPath(args[0]));
var harmony = new Harmony("STS2ReplayMod.SPIKEINV007A.OfflinePatchSmoke");
harmony.PatchAll(assembly);
var count = harmony.GetPatchedMethods().Count();
if (count < 1) throw new InvalidOperationException("No methods were patched.");
Console.WriteLine($"PATCHED {count} methods");
harmony.UnpatchAll(harmony.Id);
Console.WriteLine("UNPATCHED");
