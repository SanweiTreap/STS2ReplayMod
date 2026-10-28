# SPIKE-INV008A

Fresh four-process probe for the fixed `v0.111.0` controlled-owner-fault matrix. R6 is the current revision (`spike-inv008a.owner-fault.v6`, artifact revision `0.6.0-owner-fault-matrix-r6-b1`, manifest SemVer `0.6.0`). It is independent of `SPIKE-INV003A` and `SPIKE-INV007A`; those probes and all v1/R2/R3/R4/R5 artifacts, recovery and evidence are immutable prerequisites.

## Fixed phase chain

1. `SETUP_OWNER_FAULT_R6` -> `setup-owner-fault-r6.json`
2. `RESTART_AFTER_SETUP_R6` -> `restart-after-setup-r6.json`
3. `POST_LAUNCH_OWNER_FAULT_R6` -> `post-launch-owner-fault-r6.json`
4. `RESTART_AFTER_POST_LAUNCH_R6` -> `restart-after-post-launch-r6.json`

There is no phase selector. A phase is unlocked only by the complete, identity-valid, PASS-only chain of prior create-new artifacts. Fault phases create distinct pre-action recovery directories. Restart phases are automatic and read-only and create no recovery.

## Offline commands

Use the pinned SDK:

```powershell
& 'C:\Users\sanwe\.dotnet\dotnet.exe' run --project '.\tests\SPIKEINV008A.Tests.csproj' -c Release
& 'C:\Users\sanwe\.dotnet\dotnet.exe' build '.\SPIKEINV008A.csproj' -c Release
& '.\Invoke-StaticAudit.ps1' -Configuration Release
```

For R6 use `Invoke-Preflight-R6.ps1`, `Invoke-Install-R6.ps1`, and `Invoke-PostInstall-R6.ps1`. The build writes only to `artifacts-r6`; immutable v1/R2/R3/R4/R5 workspace pairs remain in their existing directories. The installer requires the stopped game, exact installed R5 pair, the fixed Project Lead settings identity, and the latest complete startup log proving 007A disabled and exactly 008A loaded before it replaces only 008A's live pair.

The initializer never treats its provisional loaded set as final. The first exact loaded-set decision is the first R6 operation in normal `NMainMenu._Ready`, before `SaveManager.Instance`, Continue, prior-integrity, profile or protection reads. A failure writes only bounded create-new `precheck-modset-r6-<attempt>.json`. R6 validates the sealed R5 terminal/recovery and its accepted observer-defect contract, historical profile content, the fixed current settings identity, and a fresh complete Continue capture. The first fault phase captures the pre-setup lobby boundary before creating its fresh source snapshot/recovery and before candidate/override/setup work.

R6 retains the complete R5 business contract. Its sole semantic repair registers the active restored-menu candidate in the matching `_Ready` prefix, accepts `RefreshButtons` only from that exact object, and completes menu-ready in the matching `_Ready` postfix. Menu-ready and refresh task states are persisted independently after the unchanged 10-second bound; identity/order violations and load failures remain dedicated failures. No wait, menu gate, teardown, protection, mtime or PASS rule is weakened.

## Manual-run boundary

Codex must not start or control the game. The user must enable exactly `SPIKEINV008A`, select mod save profile 1, confirm Steam Cloud is off, and perform only the currently requested phase. Any `FAIL_*`, `BLOCKED_*`, invalid/missing artifact, or unexpected UI/business behavior stops the work package without retry, backup restoration, rebaseline, or later phases.

The probe does not authorize or test process killing, native crash, replay event dispatch, natural victory, map/room entry, card play, rewards, or gray/incomplete UI investigation.
