# SPIKE-001A4 reconstruction control

Fresh successor control for STS2 public-beta `v0.111.0` (`41cef1ea`). Runtime ID is `SPIKE001A4`. The current narrow UI repair build is `0.1.1-r2`, schema is `spike-001a4.reconstruction-control.v2`, and each business attempt uses a create-new `spike-001a4-r2-{guid}` namespace. The sealed r1 artifacts and first runtime log remain preserved.

The probe only compares an uninterrupted A capture with a new-process normal Continue B capture from the same native room-before checkpoint. It does not capture or dispatch player actions, implement replay sessions, restore saves, switch profiles, or enter `SPIKE-001B` work.

## Phase 0 build and validation

```powershell
& 'C:\Users\sanwe\.dotnet\dotnet.exe' build 'C:\Projects\STS2ReplayMod\experiments\SPIKE-001A4\tests\SPIKE001A4.Tests.csproj' -c Release --nologo
& 'C:\Users\sanwe\.dotnet\dotnet.exe' 'C:\Projects\STS2ReplayMod\experiments\SPIKE-001A4\tests\bin\Release\net9.0\SPIKE001A4.Tests.dll'
& 'C:\Projects\STS2ReplayMod\evidence\inv-011\Test-SuccessorContract.ps1'
& 'C:\Projects\STS2ReplayMod\experiments\SPIKE-001A4\Invoke-StaticAudit.ps1'
```

The Release build copies only the DLL/manifest pair into `artifacts-r2`; `artifacts-r1` is retained unchanged for repair rollback. Build and live installation are separate gates. These commands do not start the game.

## Frozen implementation rules

- Arm binds non-negative equal live/save predecessor floors, derives playable floor with checked `+1`, and stores checkpoint floor as explicit `Unbound`.
- The checkpoint reader requires exactly one player, non-empty seed/character, at least one visited coordinate, stable bytes, null pre-finished room, target identity, and checkpoint floor equal to predecessor floor before a one-time bind.
- A and B readiness compare only live `RunState.TotalFloor` with expected playable floor.
- Actual combat-room model ID and actual `CombatState.Encounter.Id` are read independently. A binds them; B must match them.
- Cross-process comparison uses `SemanticProjection`; process/tick/time and CLR/Godot object identities remain phase-local telemetry.
- Production-task fault/cancel is preserved. Any observer-only or terminal-writer failure is swallowed with respect to a successful production task, seals the in-memory first terminal, and stops experiment collection.
- The only predeclared B save semantic difference is exactly `NumReloads +1`. No other canonical field is allowlisted.

## Installation lifecycle (not executed by Phase 0)

1. Keep A4 absent. In one manual process, disable `SPIKE001A3`, every other retired project probe, and every non-project mod; exit normally.
2. Still with A4 absent, perform a second verification launch. Do not enter a run. Exit normally. `Invoke-EnablementPrecheck.ps1` must prove a normal exit, empty loaded set, and persisted-disabled evidence for every installed `SPIKE*` manifest.
3. While the game is stopped, run `Invoke-Install-R1.ps1` with the exact Release pair hashes and the exact verification-log hash. It installs only `SPIKE001A4.dll/json` create-new and proves every pre-existing mod file stayed byte-identical.
4. Manually configure A4 as the only enabled mod if needed, exit normally, then launch once more. The probe's deferred main-menu gate must report exact loaded set `[SPIKE001A4]`, v0.111.0 identity, modded profile1, and Cloud-off before it can show `READY_FIXTURE_PREP`.
5. Only after that smoke gate may fixture preparation be requested. The Implementation Codex never starts or controls the game.
