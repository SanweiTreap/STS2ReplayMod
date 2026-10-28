# SPIKE-INV005A startup timing/barrier probe

Status: retired after one bounded manual run. UI result `FAIL_GATE`; reviewed work-package result `FAIL_SAFETY`. Do not run again.

Release artifact: DLL length 95,744 bytes, SHA-256 `780CD1B761CB1627FEA179379F8D0EB669AACFE2A02764CAFB9F9927AD9E50FB`; manifest SHA-256 `7A52F58537D783F9EA56A194736D6FCEA0685FC4085D4FAAF0641B419B134FB6`. Repository artifacts and installed copies match.

This probe installs its Harmony patches during mod initialization, observes the production `NGame.InitializeGraphicsPreferences` call, awaits the existing `NGame.GameStartupComplete` task without synchronously blocking the main thread, and audits component/store writers owned by the original `SaveManager`. It never creates or installs another manager, starts/loads a run, accesses replay data, or invokes any save/progress mutation.

The overlay starts in `WAITING_EVENTS`. It enables `Verify Stable Gate` only after the graphics callback has exited normally, startup completed successfully, the original run-save task is null/completed, observed writers/tasks are idle and successful, all three component fingerprints are available, and the protected profile1 baseline is established. A 120-second timeout can only produce `STOP_MISSING_EVENT`/`FAIL_GATE`; elapsed time is never a success condition.

## Build

```powershell
& 'C:\Users\sanwe\.dotnet\dotnet.exe' build .\experiments\SPIKE-INV005A\SPIKEINV005A.csproj -c Release
```

Install only `SPIKEINV005A.dll` and `SPIKEINV005A.json` from `artifacts/` while the game is fully exited. Disable SPIKE-INV004B, older probes, RunReplays, BaseLib, RitsuLib, and every other gameplay mod.

## Completed manual run

The 2026-09-13 run observed the graphics callback and successful startup task, but Steam remote leaf writes faulted and the gate never opened. Verify was not clicked. The audit then continued after the first terminal failure and hashed out-of-scope profile payloads, violating the work package's profile boundary.

The immutable runtime result is `C:\Projects\STS2ReplayMod\evidence\spike-inv005a\runtime-artifact.json` (5,599,018 bytes; SHA-256 `C660838C769CEBDC3DA14B0D420CF6106BCCD0836C302439322181FAA45E0451`). It was written directly with `System.IO` and has not been altered. Disable this probe; no retry is permitted under SPIKE-INV005A.
