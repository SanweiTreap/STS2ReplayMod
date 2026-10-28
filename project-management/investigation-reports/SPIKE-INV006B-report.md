# SPIKE-INV006B — live-seeded direct progress-write isolation sentinel

Status: `COMPLETE — BLOCKED_PRECONDITION`

## Scope

This work package tests only whether direct calls to production `SaveManager.UpdateProgressAfterCombatWon(player, room)` and `SaveProgressFile()` mutate and write exclusively through a live-seeded, reference-independent in-memory manager/store in the fixed replay combat. It does not test replay event dispatch, natural victory, achievement helpers, rewards, reconstruction fidelity, fault/restart, or complete session isolation.

## Confirmed

- A wholly new probe exists under ID `SPIKEINV006B`, source `experiments/SPIKE-INV006B/`, evidence `evidence/spike-inv006b/`, and schema `spike-inv006b.direct-write.v1`. No SPIKE-INV005A restricted runtime artifact was read or reused.
- Static preflight matches game `v0.111.0`, commit `41cef1ea`, branch `v0.111.0`, `sts2.dll` length 9,757,184 / SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`, and fixture length 20,441 / SHA-256 `4DFB27BF3BE315F85ED7306C2953675E6A7888378ACF5F2B8110465695EDC631`.
- The Release build completes with zero warnings/errors. All 12 synthetic tests pass and cover original/unknown store poison arguments, candidate-only argument access, sealed entry, manager identity, atomic terminal, post-seal rejection, winner ticket, schema whitelist, logical path ordinal matching, and identity negatives.
- Source and Mono.Cecil binary audits verify that the store observer's first instruction is the sealed/enabled gate and that captured-original/unknown-store identity branches precede the first argument-array element read. The production binary contains none of the retired probe, account-root inventory, replay-event access, natural-victory, or achievement-helper surfaces.
- Offline Harmony smoke patches and unpatches 70 methods successfully. Final artifacts and installed copies match: DLL 151,040 bytes / SHA-256 `190131CF5743D5F3B1F285490D37214D536B9408CE757B68DB8F3345475841EA`; manifest 370 bytes / SHA-256 `5EEDAB5FE9383C501C11C35C563175EA8074F3110BFD5CAF428C58CA8EEEE0E6`.
- Protected runtime I/O is constructed from the machine-confirmed active profile id 1. It walks only the resolved profile1 subtree one directory at a time, rejects every reparse point before descent, verifies opened-file final-handle containment, reads only exact account selector and machine settings outside that subtree, records logical scope/relative name only, and backs up every existing protected file into restricted local evidence with per-file hash verification.
- Candidate creation serializes live Profile/Settings/Prefs/Progress to a fresh mock store, runs `InitProfileId(null)` plus Settings/Prefs/Progress initialization, checks four-component raw/canonical equality, root/reference independence including recursive mutable graphs, store containment, initialization-call containment, original canonical stability, and a second timing-gate snapshot before override.
- The audit is enabled immediately before override. Manager and store patches classify exact instance identity before any operation/path/payload processing; only the exact candidate memory store may expose path/payload, and only for one synchronous exact `modded/profile1/saves/progress.save` write. External boundaries record only a fixed boolean/failure enum.
- Success and failure share one teardown owner and the required order: disable actions, bounded isolated task wait, at-most-once cleanup when state exists, second isolated wait, clear/restore manager, original task wait, `LoadMainMenu(false)`, menu/Continue/state checks, profile1-only protection comparison, audit seal, and one winner-ticket artifact write.
- Implementation Codex did not start or control the game and used no GUI automation.
- `User-observed runtime result`: combat appeared and the probe immediately returned to the main menu. The sponsor did not play a card or perform another action. The visible terminal classification was `BLOCKED_PRECONDITION`.
- The closed runtime artifact is 17,487 bytes with SHA-256 `98B91618F67E7AD5E651C1BC1B41515040A599976FC237E8FC24279134538511`. It records terminal sequence 17, classification `BLOCKED_PRECONDITION`, and fixed failure site `RunState`.
- The timing gate passed and the Run-click snapshot exactly preserved original manager identity `31289896`, outer task identity `8263554`, outer/startup `RanToCompletion`, null current-run task, null run state, graphics `1/1/0`, and every gate condition true.
- Protection baseline and all 12 restricted recovery copies completed before candidate construction. Candidate Profile, Settings, Prefs, and Progress all passed raw and canonical equality; roots were independent, recursive shared mutable reference count was zero, every store was contained in the fresh memory store, candidate initialization had no mutator, and original canonical state was unchanged.
- Events reached `BaselineReady`, `CandidateReady`, and `OverrideInstalled`. No `SceneReady`, `ProgressUpdated`, or `ProgressSaved` event exists. Sentinel evidence is null; candidate update/save/write counts are `0/0/0`. No original/unknown manager/store or external boundary was observed.
- Ordered teardown called `RunManager.CleanUp(true)` exactly once, cleared the override directly back to the original manager, returned with `RunManager.State == null`, and found Continue visible.
- Independent review found zero differences among all 12 before/after protected existence/length/SHA-256/mtime records and verified all 12 recovery copies against both recorded source and backup hashes. The internal profile1 inventory comparison also passed. The evidence directory contains exactly one runtime JSON, no temporary artifact, and the artifact contains no physical path, Steam/user token, or raw payload.

## Inferred

- The combination of the user's visible combat observation, the game log's combat-room creation marker, cleanup being required, and the missing `SceneReady` event makes one of the final post-load scene-legality assertions the leading failure region. This is not exact localization because the same fixed `RunState` site is also used by earlier setup assertions.

## Unknown

- The exact failed `RunState` subcondition is unknown. The fail-closed artifact intentionally records no exception text, encounter/player/enemy field values, or diagnostic payload, and several assertions share that fixed site.
- Direct production progress routing remains untested: neither `UpdateProgressAfterCombatWon` nor `SaveProgressFile` ran, and no candidate memory write occurred. This result therefore says nothing positive or negative about the requested direct-write isolation behavior itself.

## Single recommended next action

Project Lead should commission a static-only localization of the fixed fixture's reconstructed room/encounter/spawned-enemy identities against the SPIKE-INV006B legality assertions, then decide whether a wholly new probe with separate identity/schema is justified. SPIKE-INV006B must not be patched or rerun.
