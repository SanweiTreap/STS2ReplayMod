# SPIKE-001A3 reconstruction control

Fresh one-shot control for STS2 public-beta `v0.111.0` (`41cef1ea`). It compares the first stable actionable combat state reached uninterrupted (A) with a new-process production Continue reconstruction (B) from the exact same native room-before checkpoint.

## Safety and observation contract

- Runtime ID `SPIKE001A3`; current repair build identity `0.1.0-r2`; schema `spike-001a3.reconstruction-control.v2`.
- Each R2 business attempt uses a create-new `spike-001a3-r2-{guid}` directory under `evidence/spike-001a3/`; recovery bytes are evidence only and are never restored. The preserved R1 namespace is never resumed or overwritten.
- Runtime access is restricted to production-resolved `modded/profile1`. It does not enumerate, read, compare, or switch other profiles.
- It never calls production save/load/setup, profile switch, delete, restore, or simulated input APIs. The SaveRun Harmony postfix only replaces the returned Task with a transparent observer Task after the production call has already occurred.
- A formal protection baseline is sealed after the matching production `SaveRun(null,true)` Task completes and before its caller continuation resumes. Completion alone is insufficient: the checkpoint must pass two identical stable reads, native parse, target identity, `PreFinishedRoom == null`, and arm-save inequality checks.
- The 30-second capture deadline starts at observed A production-task completion, and at the B normal-Continue setup entry. Its owner Task does not depend on ticker callbacks.
- The ticker is a direct `NGame` child with explicit `ProcessMode=Always` and `SetProcess(true)`. Telemetry is bounded and retains last sample, transitions, breakers, compact/drop counts, lifecycle heartbeat, and per-site exception summaries.
- Arm/Ready waiting before the user clicks has no timeout. The user alone starts and controls the game.

## Offline build and validation

```powershell
& 'C:\Users\sanwe\.dotnet\dotnet.exe' build 'C:\Projects\STS2ReplayMod\experiments\SPIKE-001A3\tests\SPIKE001A3.Tests.csproj' -c Release --nologo
& 'C:\Users\sanwe\.dotnet\dotnet.exe' 'C:\Projects\STS2ReplayMod\experiments\SPIKE-001A3\tests\bin\Release\net9.0\SPIKE001A3.Tests.dll'
& 'C:\Projects\STS2ReplayMod\evidence\inv-010\Test-ReconstructionContract.ps1'
& 'C:\Projects\STS2ReplayMod\experiments\SPIKE-001A3\Invoke-StaticAudit.ps1'
```

Build and installation are separate gates. Do not install A3 until a normal-exit verification process, run while A3 is absent, proves every retired project probe disabled. Then run `Invoke-EnablementPrecheck.ps1` and pass its exact log hash plus the release pair hashes to `Invoke-Install-R1.ps1` while the game is stopped.

## Runtime states (not executed during Phase 0)

1. `READY_FIXTURE_PREP`: on modded profile 1, prepare an Act 1 map position with a reachable normal weak-monster node and stop before clicking it. F8 arms a deterministic displayed row/column target but does not establish the formal baseline and has no timeout.
2. `READY_A_NODE`: click only the displayed target and stop all input. The observer seals the production null checkpoint/baseline before room creation and captures A only after 30 consecutive process frames plus a same-tick full reread.
3. `READY_EXIT_AFTER_A`: do not play, End Turn, pause, open choices, or collect rewards; exit normally.
4. In a new process, `READY_B_CONTINUE` appears only after A-post/B-pre checkpoint, protection, no-run, identity, profile, Cloud, and exact-mod-set gates. Click normal Continue exactly once and stop all input.
5. B is captured at the same readiness contract, canonical save/protection and full semantic snapshots are compared, and the first terminal permanently ends A3.
