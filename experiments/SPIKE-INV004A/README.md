# SPIKE-INV004A disposable direct progress-write sentinel

Status: **closed with experimental FAIL**. The isolated manager failed the pre-override content-equivalence gate; no override, combat scene, replay event, progress update, or progress save occurred. Do not patch or rerun this work package.

This probe reuses the reviewed SPIKE-INV003A Phase B in-memory `SaveManager` transaction, native replay state factory, audit, and ordered teardown. Its only runtime mutation is a direct call to `SaveManager.Instance.UpdateProgressAfterCombatWon(player, room)`, followed by `SaveManager.Instance.SaveProgressFile()` on the isolated manager. It never reads or dispatches `replay.events`, never calls `CombatManager.EndCombatInternal`, and never calls the achievement helper.

## Controls

- `F8`: protected Phase 0 preflight and verified backup of active profile1 only.
- `F9`: native parse and identity validation of the fixed protected fixture.
- `F10`: status.
- `Direct Write Sentinel`: shows a locked page while `evidence/spike-inv004a/phase-control.json` is `PREFLIGHT`; after Phase 0 acceptance it exposes the single runtime button.

The runtime automatically enters the fixed normal combat, validates exact encounter/enemy/player baselines, performs the two direct production calls, validates the exact mutation and one in-memory `progress.save` payload, and immediately runs ordered teardown. No card play, end turn, reward, map, Continue, profile switch, or Quit input is part of the test.

## Build

```powershell
& 'C:\Users\sanwe\.dotnet\dotnet.exe' build .\experiments\SPIKE-INV004A\SPIKEINV004A.csproj -c Release
```

Install only `SPIKEINV004A.dll` and `SPIKEINV004A.json` while the game is fully exited. Exactly this probe must be enabled; SPIKE-INV003A, RitsuLib, RunReplays, BaseLib, and all other gameplay mods must be disabled.
