using Perf001;

if (args.Length < 3 || args[0] is not ("test" or "bench" or "verify"))
    throw new ArgumentException("test|bench|verify workspace package-output [--timing-window-confirmed]");
var workspace = Path.GetFullPath(args[1]);
if (args[0] == "verify")
{
    try { Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(Verification.Run(workspace, args[2]), DataJson.Options)); }
    catch (Exception ex) { Console.Error.WriteLine("READ_ONLY_VERIFY_FAILED " + ex.Message); Environment.ExitCode = 1; }
    return;
}
if (args[0] == "bench" && (args.Length != 4 || args[3] != "--timing-window-confirmed"))
    throw new ArgumentException("bench requires explicit timing window confirmation; do not run while user is gaming");
var output = PackagePaths.NewOutput(workspace, args[2], args[0] == "test" ? "validation" : "runs");
try
{
    var result = args[0] == "test" ? Tests.Run(workspace, output) : Pipeline.Run(workspace, output);
    DataJson.CreateJson(Path.Combine(output, "result.json"), result);
    Console.WriteLine(args[0] == "test" ? "PASS functional statistics/integrity; no timing experiment" : "PASS bounded OFFLINE proxy; not game performance");
}
catch (Exception ex)
{
    DataJson.CreateJson(Path.Combine(output, "failure.json"), new {Result="FAILED_NOT_PERFORMANCE_COMPLETE",Type=ex.GetType().Name,Reason=ex.Message,GameRuntimeAttempts=0});
    Console.Error.WriteLine("FAIL " + ex.GetType().Name + "; preserved failure.json; no performance completion");
    Environment.ExitCode = 1;
}
