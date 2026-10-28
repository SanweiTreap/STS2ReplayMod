# SPIKE-INV007A minimal-contract direct progress-write isolation sentinel

Status: `R3 VALID TERMINAL — PASS_DIRECT_WRITE_ISOLATED — DO NOT RERUN`.

This is the controlled R3 revision of the same single-run probe for public-beta `v0.111.0` / `41cef1ea`. Its identity is `SPIKEINV007A`, source directory is `experiments/SPIKE-INV007A/`, evidence directory is `evidence/spike-inv007a/`, and runtime schema is `spike-inv007a.direct-write.v3`.

The probe re-establishes the accepted outer-cloud, graphics and startup/menu timing gate; protects only modded profile1 plus the exact account selector and machine settings; creates a live-seeded, recursively reference-independent in-memory candidate; loads the fixed Monster combat without replay-event dispatch; evaluates each setup/argument precondition under its own failure enum; calls the two production progress methods once each; and runs the same ordered teardown on success or failure.

Actual character, encounter and enemy IDs are read only to calculate the in-memory mutation oracle. They are not fixed pre-call identity gates. The business path contains no executor wait or additional frame wait. Empty enemy lists are legal. The progress save is accepted only if the audit observes exactly one synchronous write to the exact logical profile1 progress name and its deserialized canonical payload equals the candidate mutation.

## Phase 0 commands

From the repository root:

```powershell
& .\experiments\SPIKE-INV007A\Invoke-Preflight.ps1
& .\experiments\SPIKE-INV007A\Invoke-Install.ps1
```

The preflight uses only the 007A projects. It does not invoke a retired probe project. The install script refuses to run while the game process is present, requires the destination to be the exact known R2 pair, and replaces only the DLL and manifest.

## Manual run protocol

Before process startup, ensure Steam Cloud is off, the active modded save is profile1, and the exact enabled-mod set contains only `SPIKEINV007A`. Disable every retired probe, RunReplays, BaseLib, RitsuLib, and every other gameplay mod. If this exact state cannot be guaranteed before startup, do not launch.

At the main menu, do not open Continue, Singleplayer, Multiplayer, Settings, Modding, or a profile page. Wait for:

```text
Direct Write Ready — R3
R2 artifact, recovery, and active baseline verified. Click once; no timeout while waiting.
```

Click `Run Direct Write Sentinel` exactly once. After the click, provide no input: do not play a card, end the turn, claim gold/cards/potions/relics, enter the map, or operate any other UI. Wait for automatic teardown and return to the main menu.

At the first terminal text, note it, whether combat was visible, and whether Continue is still visible. Exit the game immediately and do not retry. Any crash, hang, reward screen, identity error, missing Continue, unexpected screen, or other anomaly is terminal and must not be retried.

No R3 runtime artifact or R3 recovery backup exists at Phase 0. They may be created only by the one authorized Run click. All R1/R2 artifacts and recoveries remain immutable.
