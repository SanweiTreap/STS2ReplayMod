# SPIKE-INV004B — pre-override deep-copy equivalence diagnostic

Status: COMPLETE — `PARTIAL_DIAGNOSED`

## Outcome

The bounded main-menu diagnostic completed normally on 2026-09-10. Candidate B, initialized from serialized live state in a fresh `MockGodotFileIo`, met both required properties: content equivalence and reference independence. Candidate A, initialized from the protected disk snapshot, also matched the live manager in this run. Because the INV004A Candidate A mismatch did not reproduce, this work cannot localize the earlier difference and therefore cannot claim `PASS_DIAGNOSED`.

Machine-readable evidence: `evidence/spike-inv004b/runtime/deep-copy-diagnostic.json`.

## Fixed identity and run gates

- Game: public-beta `v0.111.0`, commit/branch `41cef1ea`.
- `sts2.dll`: 9,757,184 bytes; SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`.
- Probe: SHA-256 `0F934BEE918098F9806CFEAD248575C621434F7A57B6DBD32EB702E44897975F`.
- Final Release rebuild: 0 warnings, 0 errors; repository and installed DLL hashes match.
- Loaded mod IDs: exactly `[SPIKEINV004B]`; fixed-identity mismatches: none.
- Active candidate profile: profile 1 for both candidates.
- The user-observed terminal state was `PARTIAL_DIAGNOSED` followed by a normal exit request.
- The probe displayed a pre-run requirement for Steam Cloud to be disabled. The supplied completion message did not separately restate that manual confirmation; this remains an external confirmation item rather than machine-verifiable evidence.

## Candidate A — disk rehydration

Candidate A did not reproduce the INV004A failure:

| Component | Raw equality | Canonical equality | Structured diffs |
| --- | --- | --- | ---: |
| Settings | pass | pass | 0 |
| Prefs | pass | pass | 0 |
| Progress | pass | pass | 0 |

Its manager, Settings, Prefs, and Progress roots were independent from the live manager. Recursive traversal found 367 mutable nodes on each side and zero shared mutable references. Store containment passed. The audit recorded 28 calls including disk seeding; initialization performed no write, rename, or delete-file operation.

This rules out a stable, deterministic disk-rehydration mismatch for the captured profile state and build. It does not explain the historical INV004A mismatch, which may depend on session timing or a transient live-versus-disk state.

## Candidate B — live-state seeding

Candidate B succeeded:

| Component | Raw equality | Canonical equality | Structured diffs |
| --- | --- | --- | ---: |
| Settings | pass | pass | 0 |
| Prefs | pass | pass | 0 |
| Progress | pass | pass | 0 |

Its manager and all three component roots were independent. Recursive traversal again found 367 mutable nodes per graph and zero shared mutable references, including nested dictionaries, sets, lists, statistics records, and epoch collections. Store containment passed. All 21 audited calls remained in the fresh mock; initialization performed no write, rename, or delete-file operation.

The live Settings, Prefs, and Progress raw and canonical hashes were identical before and after both candidate constructions.

## Safety and scope

- `SaveManager.Instance` retained runtime identity `30416202` throughout.
- No global manager override was installed.
- No run setup, run/scene transition, replay event access, or progress mutation/save sentinel occurred.
- During the diagnostic window, all 12 protected file records and the profile1 inventory were byte-for-byte and metadata-equivalent before versus after.
- Blockers: 0. Safety failures: 0. Exception: none.
- After the user exited normally, an independent check found all 12 protected files still present with identical lengths and SHA-256 hashes. Only `settings.save`, `prefs.save.backup`, and `progress.save.backup` had newer modification timestamps, all at normal-exit time; this occurred outside the diagnostic window and did not alter content.

## Confirmed, inferred, and unresolved

- **Confirmed:** Candidate B is content-equivalent and reference-independent for the captured build/profile state; Candidate A also matches in this run; all candidate I/O remained mock-contained; protected content was unchanged.
- **Inferred:** INV004A most likely observed a session-dependent live/disk divergence rather than an invariant defect in disk rehydration. The present evidence cannot identify its exact mutation or timing source.
- **Unresolved:** the exact path and origin of the historical INV004A mismatch, because no Candidate A diff exists in this run to localize.

## Single recommended follow-up

Perform one narrower static call-site investigation of mutations and save scheduling for `SettingsSave`, `PrefsSave`, and `ProgressState` between production `SaveManager` initialization and `NMainMenu._Ready`, and identify whether any of those paths can make live state temporarily newer than disk before the diagnostic entry becomes available. Do not retry SPIKE-INV004A and do not advance to the direct-write sentinel from this result.
