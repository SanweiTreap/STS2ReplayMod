# SPIKE-INV004B disposable deep-copy diagnostic

Status: complete; the single main-menu runtime diagnostic returned `PARTIAL_DIAGNOSED`.

Artifact identity: DLL SHA-256 `0F934BEE918098F9806CFEAD248575C621434F7A57B6DBD32EB702E44897975F`; manifest SHA-256 `262BEF934445D1F2BC5A60766E455F58145299F5A82833414730A985805B8B19`. Repository and installed copies match.

This probe adds one `Replay Copy Diagnostic` main-menu entry. Its completed single run constructed:

- Candidate A from the same protected disk files used by the prior disk-rehydrate approach;
- Candidate B from freshly serialized live Profile/Settings/Prefs/Progress values.

Both candidates use their own `MockGodotFileIo`. The probe never calls `SaveManager.MockInstanceForTesting`, never starts or loads a run, never reads replay events, never changes scenes, and never calls progress update/save methods. It recursively canonicalizes JSON object properties while preserving array order, emits minimal structured diffs, and checks top-level and nested mutable reference independence.

## Build

```powershell
& 'C:\Users\sanwe\.dotnet\dotnet.exe' build .\experiments\SPIKE-INV004B\SPIKEINV004B.csproj -c Release
```

Install only `SPIKEINV004B.dll` and `SPIKEINV004B.json` while the game is fully exited. The runtime gate requires exactly this probe to be loaded. SPIKE-INV004A, SPIKE-INV003A, RunReplays, BaseLib, RitsuLib, and all other gameplay mods must be disabled.

## Manual run

At the stable main menu, open `Replay Copy Diagnostic`, press `Run Deep-Copy Diagnostic` once, and do nothing until the panel reports a terminal classification. Exit normally immediately afterward; do not enter Continue, Singleplayer, Multiplayer, profile selection, or any run/reward screen.

The completed run's machine-readable result is `evidence/spike-inv004b/runtime/deep-copy-diagnostic.json`. Do not run the probe again under this work package.
