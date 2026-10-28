# SPIKE-INV006A scope-safe outer-cloud timing gate

Status: `COMPLETE — PASS_SCOPE_SAFE_GATE`. The single authorized runtime has finished; do not run this probe again.

This is a new, read-only main-menu probe for public-beta `v0.111.0` / `41cef1ea`. It observes only:

- `NGame.InitializeGraphicsPreferences` entry, normal exit, or a fixed fault-site enum;
- the unique `Task` returned by zero-argument `NGame.DoCloudSync()` and one terminal continuation;
- `NGame.GameStartupComplete` terminal state;
- main-menu readiness, original `SaveManager` runtime identity, null run state, and original current-run task status;
- one probe-owned `Verify Stable Gate` click.

It does not replace a manager, inspect any save-store call, read a logical save name or data argument, construct a candidate, call a save/progress method, or start/transition/clean up a run. The test-only lexical fixture is compiled only into `SPIKEINV006A.Tests`; the production project explicitly excludes `tests/**`.

## State machine

- `WAITING_EVENTS`: Verify disabled while explicit production events are incomplete.
- `Gate Ready`: the unique outer task is terminal (success, fault, and cancellation are all settled), graphics exited normally, startup/menu markers arrived, and manager/run/current-run conditions hold in one locked snapshot.
- Verify: a single click repeats the conditions and requires no second outer/graphics lifecycle.
- Terminal: one CAS winner seals the event set. Only its opaque ticket can write the already-closed snapshot. A write failure stays sealed and displays only `BLOCKED_ARTIFACT_WRITE`.

The timeout is 120 seconds. It can produce `STOP_MISSING_OUTER_TASK` or `FAIL_EVENT_ORDER`; elapsed time can never open the gate.

## Build and preflight

Run from the repository root:

```powershell
& .\experiments\SPIKE-INV006A\Invoke-Preflight.ps1
```

The script validates the fixed game DLL, runs the synthetic contract suite, builds Release with the private .NET 9 SDK, scans production source and the built DLL for forbidden surfaces, and prints artifact identities. Current accepted build:

- `SPIKEINV006A.dll`: 40,960 bytes; SHA-256 is recorded in `evidence/spike-inv006a/preflight-results.txt`.
- `SPIKEINV006A.json`: 370 bytes; SHA-256 is recorded in the same file.

Install only the two files from `artifacts/` into the game's `mods` directory while the game is fully exited. The sponsor must ensure that only `SPIKEINV006A` is enabled; the presence of another mod file in the directory is not itself proof that it is enabled.

## Runtime evidence

The terminal winner wrote exactly one allowlisted JSON to:

`C:\Projects\STS2ReplayMod\evidence\spike-inv006a\runtime-artifact.json`

The artifact is 3,554 bytes, SHA-256 `7F45652CC5AB7524CB3444B99FA8AF8AF5EFF0E47EB433A3306C2FAE44B52823`, and independently validates as `PASS_SCOPE_SAFE_GATE`. The writer first created a same-directory temporary file and atomically renamed it; no temporary file remains. Do not delete or retry after this terminal result.
