# SPIKE-001A2-R2 reconstruction control probe

Disposable one-shot probe for STS2 public-beta `v0.111.0` (`41cef1ea`). It compares the first actionable combat state reached uninterrupted (A) with the state reconstructed by the production main-menu Continue path (B), using the same native room-before checkpoint.

## Safety contract

- Mod ID: `SPIKE001A2R2`; current revision `0.1.2-r3`; schema: `spike-001a2-r2.reconstruction-control.v3`.
- Dependencies: none beyond the game-provided `sts2`, Godot, Harmony, and Steamworks assemblies.
- It never calls `SetUpSavedSingleplayer`, `LoadRun`, `ToSave`, a save/write/delete API, profile switching, or input simulation.
- It only patches `NMainMenu._Ready` and `NGame._Input`. An owned ticker node performs observation after the main-menu gate. F8 is a manual baseline arm action; all gameplay actions remain user-driven.
- Evidence uses create-new, attempt-specific or phase-specific files under `evidence/spike-001a2-r2/`. A first terminal locks the probe permanently.
- The only predeclared semantic save exclusion for B is `NumReloads`, which must change by exactly `+1`. No serialization metadata exclusions are declared.
- Snapshot comparison excludes only capture UTC/process and the two observation frame numbers. The 30-frame quiet count and every raw readiness field remain compared.

## Build and validate

```powershell
& 'C:\Users\sanwe\.dotnet\dotnet.exe' build 'C:\Projects\STS2ReplayMod\experiments\SPIKE-001A2-R2\tests\SPIKE001A2R2.Tests.csproj' -c Release --nologo
& 'C:\Users\sanwe\.dotnet\dotnet.exe' 'C:\Projects\STS2ReplayMod\experiments\SPIKE-001A2-R2\tests\bin\Release\net9.0\SPIKE001A2R2.Tests.dll'
& 'C:\Projects\STS2ReplayMod\experiments\SPIKE-001A2-R2\Invoke-StaticAudit.ps1'
```

The current build copies the exact R3 runtime pair to `artifacts-r3/`; preserved R1/R2 pairs remain in `artifacts/` and `artifacts-r2/`. Installation is intentionally a separate lifecycle gate. R3 replacement uses `Invoke-Install-R3.ps1` and verifies both installed-old and source-new identities.

## Runtime states and manual actions

1. `READY_FIXTURE_PREP`: on modded profile 1, create a fixed-seed Standard run and stop on the Act 1 map before selecting the first ordinary weak-monster node. Press F8 once to arm A; this is not yet the formal baseline.
2. `READY_A_NODE`: click only that recorded weak-monster node, then stop. At first actionable readiness the probe first requires and seals the production `PreFinishedRoom=null` checkpoint, then captures A after 30 consecutive ready frames.
3. `READY_EXIT_AFTER_A`: do not play, End Turn, open choices, or collect rewards. Exit normally.
4. In a new process, `READY_B_CONTINUE` appears only after exact checkpoint content and protection verification. Click normal Continue exactly once, then stop.
5. The probe captures B at the same readiness boundary and emits one of the contracted terminals. Exit normally and never rerun the probe.

Any version/hash/mod/profile/Cloud/checkpoint/protection/protocol mismatch is a hard stop. Do not restore, rebaseline after formal baseline, or use another profile.
