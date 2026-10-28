# SPIKE-INV005A — startup timing/barrier probe

Status: `COMPLETE — FAIL_SAFETY`

## Outcome

The sponsor-operated main-menu run on 2026-09-13 did not open the gate. The probe UI and immutable runtime artifact self-classified `FAIL_GATE` because an observed original-manager Steam remote `WriteFileAsync` task faulted. Review retains that machine result but elevates the work-package outcome to `FAIL_SAFETY`: after the first terminal gate condition was already known, the probe continued auditing startup cloud synchronization and computed payload hashes for operations targeting two out-of-scope profiles. That violates the work package's profile-1-only boundary.

The run must not be retried under this work package. It did not install an override, construct a candidate, start a run, call a save/progress mutation, or reach Verify.

Machine-readable evidence: `evidence/spike-inv005a/runtime-artifact.json`, length 5,599,018 bytes, SHA-256 `C660838C769CEBDC3DA14B0D420CF6106BCCD0836C302439322181FAA45E0451`. The artifact is preserved exactly as emitted; its original `FAIL_GATE` field has not been rewritten.

## Confirmed

- The session log reports public-beta `v0.111.0`, commit `41cef1ea`, and exactly one loaded mod: `SPIKEINV005A`. The built and installed 95,744-byte probe DLLs matched SHA-256 `780CD1B761CB1627FEA179379F8D0EB669AACFE2A02764CAFB9F9927AD9E50FB`.
- Patch-active milestone sequence 1 preceded graphics entry sequence 21. The single graphics call exited normally at sequence 22; no graphics call remained active and no graphics exception was recorded.
- `NGame.GameStartupComplete` reached `RanToCompletion` at sequence 7165. The original manager identity remained `36429313` and `CurrentRunSaveTask` remained null.
- The first failed leaf task was recorded at sequence 26, before main-menu ready. It was the original `SteamRemoteSaveStore.WriteFileAsync` for the account-scoped `modded/profile.save` and failed because the Steam API call was invalid. Production logged that the local file was preserved.
- The artifact contains 1,952 observed async tasks: 940 ended `RanToCompletion` and 1,012 ended `Faulted`. The outer cloud operations can complete while their caught Steam leaf writes fault, but the work package explicitly required every observed async store task to complete without fault/cancellation, so the event gate could not open.
- Gate baseline was never established, `Verify Stable Gate` was never enabled/clicked, and no protected-file comparison window began.
- The probe did not stop audit capture when the first async fault became known. It subsequently emitted 3,473 writer-audit records for one out-of-scope profile and 3,605 for another, including byte/string payload length and SHA-256. This is the safety violation that controls the reviewed outcome.
- No unknown-store identity, manager replacement, run state, component writer left active, or synchronous store operation left active was recorded at terminal.

## Inferred

- The Steam leaf-task failures are consistent with production attempting `OverwriteCloudWithLocal` while Steam remote storage calls are unavailable/disabled; the log explicitly says local files were preserved. This does not prove the UI's Steam Cloud setting by itself.
- The instrumentation was too broad for the profile restriction: hashing every `WriteFileAsync` payload before checking its logical scope made passive observation itself cross the work-package boundary.
- Delaying fail-closed termination until menu/startup milestones allowed thousands of later audit events after the first irreversible gate failure. A correct bounded observer would seal evidence immediately on the first terminal condition and would never hash or persist an out-of-scope payload.

## Unknown

- The artifact's `fixedIdentity` field is null because failure was latched before the menu-time identity capture. The session log independently confirms game version/commit and the one-mod set, but Steam Cloud state and visible active profile remain external confirmations rather than machine-verified fields in this artifact.
- Settings/Prefs/Progress gate fingerprints and profile1 protected-file/inventory stability are unknown because no gate baseline existed.
- Whether a scope-safe observer would reach a stable gate is unknown. This run cannot answer it and cannot be rebaselined.
- No conclusion is available for direct-write isolation, manager override, transaction/session behavior, reconstruction, or replay fidelity.

## Single recommended next action

Project Lead should authorize one narrower, static-only investigation of Steam-Cloud-disabled startup semantics and define a profile-scope-safe audit contract: identify which task layer is the production cloud barrier, why caught Steam leaf tasks fault under the fixed environment, and how an observer can redact/ignore all non-profile1 path and payload arguments before any future runtime probe is considered. Do not advance to a live-seeded direct-write sentinel.
