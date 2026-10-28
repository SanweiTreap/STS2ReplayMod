# SPIKE-INV003A disposable replay-isolation probe

Current source: **transaction probe**. The experiment is closed after the first auditable FAIL in Phase C. `evidence/spike-inv003a/phase-control.json` is locked to `STOPPED`; do not run or unlock D/E/F under this work package.

## Fixed target

- STS2 public-beta `v0.111.0`, commit `41cef1ea`
- `sts2.dll` SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`
- exactly one enabled mod: `SPIKEINV003A`
- RitsuLib, RunReplays, BaseLib, SPIKE-001A, unlock mods, and all other gameplay mods disabled
- visible active profile: modded profile slot 1, confirmed by the user
- Steam Cloud disabled, confirmed by the user for the current test session

## Build

```powershell
& 'C:\Users\sanwe\.dotnet\dotnet.exe' build .\experiments\SPIKE-INV003A\SPIKEINV003A.csproj -c Release
```

Copy `SPIKEINV003A.dll` and `SPIKEINV003A.json` from `bin/Release/net9.0/` into the game's `mods` directory only while the game is fully exited.

## Phase 0 / Phase A controls

- `F8`: run the one-shot read-only preflight from the main menu.
- `F9`: after Phase 0 is accepted, validate the unchanged `replays/latest.mcr` with the game's native `PacketReader` while remaining at the main menu with `RunManager.State == null`.
- `F10`: print status only.
- `Replay Isolation Probe`: injected into the native `MainMenuTextButtons` peer group. In the Phase A build it opened the management-only page; in the transaction build it displays only the phase currently unlocked by `phase-control.json`.

## Transaction controls

- `Replay Isolation Probe` opens the currently unlocked transaction phase after Phase A has passed.
- `F7` is the sole normal Stop input for Phase B after the first watchable replay scene appears.
- Every transaction captures the original manager identity, creates and deep-initializes a `MockGodotFileIo`-backed manager, installs the official test override, calls native `SetUpReplay`, immediately forces `ShouldSave=false`, and owns cleanup/restore/main-menu reconstruction in one `finally`.
- Audit patches observe original/isolated manager and store writes/deletes/batches, profile and override changes, plus metric/leaderboard/achievement submit boundaries. They do not replace save method results or patch writes away.
- Phase C dispatches only the game's native `CombatReplayEvent` path and requires both `UpdateProgressAfterCombatWon` and a `progress.save` write to land in the isolated manager/store.
- Phases D and E inject one deterministic exception at the specified owner boundaries and use the same teardown.

The preflight waits for the current save task, requires `RunManager.State == null`, verifies the visible Continue isolation sentinel, parses its native `SerializableRun`, inventories only the resolved active-profile tree, and records exact identities for the modded account `profile.save` plus the account-scoped machine `settings.save`. It hashes every protected file and creates a verified copy of only the active-profile files under `evidence/spike-inv003a/protected-backup/`.

It writes `evidence/spike-inv003a/runtime/preflight.json`. Account-level `profile.save` and account-scoped machine `settings.save` are hashed but deliberately not copied because they are not exclusive to one profile. The backup may contain protected test-run data and must be handled accordingly.

Do not press F8 unless the visible profile and Cloud prerequisites have been checked manually. Do not click Continue, Abandon, Singleplayer, Multiplayer, change profile, or quit while the preflight is running.
