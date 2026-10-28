# SPIKE-INV006B live-seeded direct progress-write isolation sentinel

Status: `COMPLETE — BLOCKED_PRECONDITION`. The single authorized runtime has finished; do not run or modify this probe.

This is a single-run probe for public-beta `v0.111.0` / `41cef1ea`. It re-establishes the accepted SPIKE-INV006A timing gate, waits for `Direct Write Ready`, and accepts exactly one `Run Direct Write Sentinel` click. It then:

1. captures and backs up only active `modded/profile1/**`, exact `modded/profile.save`, exact `settings.save`, and validates the fixed workspace fixture;
2. serializes Profile/Settings/Prefs/Progress from the live manager into a fresh `MockGodotFileIo`, initializes a reference-independent candidate, and verifies raw/canonical equivalence and store containment;
3. starts an identity-first transaction audit, installs the candidate override, reconstructs the fixed ordinary combat without reading or dispatching replay events, and immediately sets `RunManager.ShouldSave=false`;
4. calls only `UpdateProgressAfterCombatWon(player, room)` and `SaveProgressFile()`, checks the full expected mutation and the one synchronous in-memory `progress.save` payload;
5. cleans up once, restores the original manager, rebuilds the main menu, verifies Continue and the protected profile1 set, seals, and writes at most one runtime artifact.

The probe does not call natural victory, `EndCombatInternal`, achievement helpers, reward/map flow, fault/restart paths, or a replay event dispatcher. A PASS only establishes routing of the two direct production progress methods in this fixed isolated session.

## Build and audits

From the repository root:

```powershell
& .\experiments\SPIKE-INV006B\Invoke-Preflight.ps1
```

The script verifies game/fixture identity, runs synthetic identity-first poison/race/schema tests, builds Release, performs source and Mono.Cecil binary order/forbidden-surface audits, and runs offline Harmony patch/unpatch smoke. Production excludes `tests/**`.

Install only `artifacts/SPIKEINV006B.dll` and `artifacts/SPIKEINV006B.json`. Older probes may remain on disk but must be disabled in the game. Do not run this probe if `evidence/spike-inv006b/runtime-artifact.json` already exists.

## Recorded runtime result

The sponsor observed combat appear and then an immediate automatic return to the main menu, without playing a card or performing another action. The sealed artifact classified the run as `BLOCKED_PRECONDITION` at fixed site `RunState`, after baseline/candidate/override but before `SceneReady` and before both direct progress methods.

Candidate construction passed every equivalence, reference, and containment condition. Boundary counts were zero; ordered teardown restored the original manager, null run state, and Continue. All 12 protected file identities and profile1 inventory were unchanged, and all 12 restricted recovery copies verified. Because the fixed failure site covers multiple scene-legality assertions and the schema intentionally contains no exception text or field values, the exact subcondition is unknown. The probe must not be patched or rerun.

## Historical sponsor run protocol

- Confirm v0.111.0, Steam Cloud off, modded profile slot 1, and only `SPIKEINV006B` enabled. Disable SPIKEINV006A, all older probes, RunReplays, BaseLib, RitsuLib, and every other gameplay mod.
- Start the game and remain on the main menu. Do not enter Continue, singleplayer, multiplayer, Settings, or any profile page.
- Wait for `Direct Write Ready`; before it appears, do not click Run.
- Click `Run Direct Write Sentinel` exactly once, then make no input. Do not play cards, end turn, or click combat UI.
- The probe should enter combat, perform both direct calls, execute ordered teardown, and return to the main menu with one terminal classification.
- If a reward screen appears, do not take gold/cards/potions/relics and do not enter the map. Stop and report immediately.
- On return to the main menu, stop all interaction, do not exit, and report the exact terminal UI text. Wait for evidence review before exiting normally.
- Any crash, hang, identity error, missing Continue, unexpected screen, or other failure is terminal for this work package. Do not retry.
