# SPIKE-INV007A — minimal-contract direct progress-write isolation sentinel

Status: `R3 VALID TERMINAL — PASS_DIRECT_WRITE_ISOLATED; NO RERUN`

## Project Lead R3 evidence return — 2026-09-14

- The first and only R3 run closed `PASS_DIRECT_WRITE_ISOLATED / None`. The v3 artifact is 19,953 bytes / SHA-256 `AE4D9998CAF03DA3FB2F17656E12A0216B7160FD69017154B128C3F6365C9553`; its 24 events are contiguous and strictly monotonic.
- The detached-oracle gate passed before production mutation: JSON parse succeeded, shared mutable references were `0`, candidate canonical progress and player discovery were unchanged, and update/save/write audit counts remained zero.
- The direct business contract then passed exactly: candidate update/save/write counts `1/1/1`; expected and actual canonical mutation hashes both `F25CA6D8523112C5699A24CEA374380A76CE3645C5EA080EB857EB7C2AE5F2E3`; full mutation/discovery equality true; one synchronous write to the exact logical profile1 progress name; payload deserialized and canonically matched.
- Boundary evidence is clean. Teardown called cleanup exactly once when required, restored the original manager, left run state null and Continue visible, and verified all 12 protected files unchanged before/after.
- R3 recovery `restricted-recovery/r3-20260914T152630.2908445Z/` contains 12 copies matching artifact identities; aggregate SHA-256 is `9719C685A08E86786D96DE55309F19C6242A71D8001B1E841BF88B89598AB9E2`. Fixed R1/R2 artifact identities remain unchanged.
- The sponsor reported a gray background with partial top UI and no visibly started combat. Rendering completeness is outside the authorized direct-write contract and was not investigated or used to qualify the PASS.
- Evidence: `evidence/spike-inv007a/runtime-artifact-r3.json` and `r3-terminal-validation-results.txt`. R3 is terminal and must not be run again.
- Project Lead independently rehashed and parsed the artifact, verified sequence/tick ordering and the oracle/sentinel/audit/teardown contract, compared all 12 protection records field-for-field, and rehashed all 12 recovery copies. The classification is accepted without expanding its scope.

## Controlled R3 implementation handoff — 2026-09-14

- Implemented only the Project Lead-authorized detached mutation oracle revision in the existing 007A source tree. Identity is version `0.3.0-direct-write-r3`, schema `spike-inv007a.direct-write.v3`, terminal destination `runtime-artifact-r3.json`, and unique recovery prefix `r3-*`.
- The oracle now captures candidate baseline JSON/canonical hash, obtains expected state through `JsonSerializationUtility.FromJson<SerializableProgress>`, recursively requires zero shared mutable references, mutates only that detached graph plus an independent discovery set, and rechecks candidate canonical progress, player discovery, and zero update/save/write audit immediately before the production update call.
- Parse failure is fail-closed as `BLOCKED_ORACLE_PARSE`; a shared reference or either pre-call baseline change is fail-closed as `BLOCKED_ORACLE_ALIASING`. Neither gate may call the production update. The original one-update/one-save direct-write business contract is unchanged.
- The R3 prior-attempt gate verifies the fixed R2 artifact and R2 recovery identities, then requires the R2 artifact protection-after set, recovery set, and current allowlist/inventory to agree. Fixed identity mismatch remains `BLOCKED_IDENTITY`; unresolved state remains `BLOCKED_PRIOR_ATTEMPT_UNRESOLVED`.
- All 27 R2 tests and 6 new R3 tests passed (33/33). Release build completed with zero warnings/errors; expanded source/binary audit passed; Harmony smoke patched/unpatched 70 methods.
- Installation was permitted only because the game was absent and the installed input pair exactly matched R2. Installed R3 identities: DLL 187,904 bytes / SHA-256 `9AEC3090F5BC0BC301437D789AAE84748B7A3BCE6AAC9D3111994374C7B1C46E`; manifest 370 bytes / SHA-256 `0E3FA78CE308FD14997A6C2BB02AF12335A629354A7837523EC489E11D005289`.
- Post-install read-only comparison passed for all 12 R2 artifact/recovery/current protected identities and the profile1 inventory. Runtime-only Ready conditions remain pending until the sponsor starts the game. No game process was started or controlled, no Computer Use was used, and no R1/R2 artifact or recovery was changed or rerun.
- Evidence: `evidence/spike-inv007a/r3-preflight-results.txt`, `r3-installation-results.txt`, and `r3-prior-integrity-offline-results.txt`.

## Project Lead R2 disposition — 2026-09-14

- `runtime-artifact-r2.json` is a valid closed v2 artifact: 19,259 bytes / SHA-256 `4B48A75F650A687F206255F2425140B3F640277E3D62F21495F99CC6086B691F`. Events 1–22 are contiguous with strictly increasing monotonic ticks. Timing, prior-attempt integrity, protection baseline, live-seeded candidate, scene load, and direct arguments all passed.
- Production `UpdateProgressAfterCombatWon` was observed exactly once on the candidate. Candidate save/write counts remained zero; original/unknown manager/store and external-boundary flags remained false. This is accepted as a partial routing result, not as mutation-correctness or complete direct-write isolation.
- The recorded `FAIL_MUTATION` does not demonstrate a production mutation defect. The oracle obtained expected state from `candidate.Progress.ToSerializable()` and then mutated existing nested stats. Fixed v0.111.0 IL shows that `ToSerializable()` creates outer lists with `ToList()` but retains the existing EncounterStats/EnemyStats objects and their FightStats graphs. `ApplyExpected` therefore incremented candidate state once before the production call; the production call incremented it again, producing different expected/actual canonical hashes.
- Because the mutation gate failed, `SaveProgressFile` was not called. Save routing, synchronous memory write, and payload equivalence remain untested.
- Teardown passed: cleanup when required, original manager restored, run state null, Continue visible. All 12 before/after protected file records are identical, and all 12 R2 recovery copies independently match their recorded source hashes.
- The incomplete/gray scene rendering is consistent with teardown beginning roughly 3.4 ms after `SceneLoadComplete`; rendering fidelity is outside this sentinel. A reported `Invalid Task ID` is not linked by current evidence to the mutation mismatch and is not added to R3 scope.
- R2 must not be rerun or overwritten. Project Lead authorizes only the detached-oracle R3 revision defined in `project-management/handoffs/SPIKE-INV007A-R3.md`.

## Controlled R2 implementation handoff — 2026-09-14

- Project Lead's narrow R2 authorization was implemented in the existing 007A source tree as version `0.2.0-direct-write-r2`, schema `spike-inv007a.direct-write.v2`, and terminal destination `runtime-artifact-r2.json`.
- GateReady now atomically disarms the startup timeout. The timeout callback returns without events, seal, or artifact after GateReady or Run-owner acceptance. Artifact output uses an R2-only create-new temporary and destination path.
- A read-only prior-attempt integrity gate now runs before Ready. It validates the immutable R1 artifact/recovery identities, exact active modded profile1 allowlist content and inventory while ignoring mtime alone, Continue visibility, null run state, and stable original manager identity. Any current content/inventory/state mismatch seals `BLOCKED_PRIOR_ATTEMPT_UNRESOLVED`; fixed evidence identity mismatch seals `BLOCKED_IDENTITY`.
- The direct-write transaction, live-seed candidate, boundary audit, contract rules, and scope-safe session retained their exact R1 source identities. No direct update/save business contract, mutation oracle, teardown order, or protection allowlist was broadened.
- 27/27 tests passed (the original 19 and all 8 required R2 cases). Release build completed with zero warnings/errors; static source/binary gates passed; Harmony smoke patched/unpatched 70 methods.
- Installation replaced only the exact known R1 `SPIKEINV007A.dll`/manifest pair while the game was absent. Installed R2 identities are DLL 174,592 bytes / SHA-256 `5447E69F11041639ED9DD92E152D1B78B7A9A6A6025906C7BDD9900929021ACD`, manifest 370 bytes / SHA-256 `AA910BF2376FE5642372FF8C64E8F3138708F843BA2335112611F3E954E32869`.
- Before the R2 runtime, post-install read-only comparison passed for the selector, all 12 current allowlisted file identities, and profile1 file/directory inventory. R1 artifact and recovery aggregate remained byte-identical. Runtime-only Continue/run-state/manager checks subsequently passed and are recorded in the R2 artifact; see the Project Lead R2 disposition above.
- Evidence: `evidence/spike-inv007a/r2-preflight-results.txt`, `r2-installation-results.txt`, and `r2-prior-integrity-offline-results.txt`. The implementation did not start or control the game.

## Project Lead post-run disposition — 2026-09-14

- The first process reached `GateReady`, but the still-armed 120-second startup timeout sealed `STOP_MISSING_TIMING_GATE / TimeoutCallback` before the sponsor clicked Run. The closed artifact is 3,108 bytes / SHA-256 `D38EC65F8380DDC2AFF1C8F6EB9E7FDEBEAB1D706424C4471C36DAAD0488ACA2`; it contains events 1–9, no `RunAccepted`, protection baseline, candidate, audit, sentinel, or teardown.
- A later process accepted the actual click and created 12 restricted recovery backups under `20260914T081712.1432035Z`, but the fixed artifact destination already existed. The writer refused overwrite and the UI reported `BLOCKED_ARTIFACT_WRITE`; the business terminal from that attempt was not preserved.
- Neither attempt provides a valid direct-write result. The second attempt is evidence-incomplete, so its update/save execution, boundary result, teardown evidence, and post-protection result remain unknown.
- Existing artifact and recovery evidence must remain unchanged. Project Lead has authorized only the narrow R2 harness revision in `project-management/handoffs/SPIKE-INV007A-R2.md`, conditional on a new-process prior-attempt integrity gate.

The Phase 0 material below remains a historical record of the original build and must not be read as the current execution status.

## Scope

This work package tests only whether one direct call to production `SaveManager.UpdateProgressAfterCombatWon(player, room)` followed by one direct call to `SaveProgressFile()` is routed to a live-seeded, reference-independent candidate and its memory store in the fixed Monster replay session. It does not dispatch replay events, trigger natural victory, invoke the achievement helper, or test replay fidelity, reconstruction, fault/restart, capture/dispatch, rewards, or complete session isolation.

## Confirmed

- A wholly new probe exists under id `SPIKEINV007A`, source `experiments/SPIKE-INV007A/`, evidence `evidence/spike-inv007a/`, and schema `spike-inv007a.direct-write.v1`.
- The implementation did not modify, build, install, or run a retired probe. The new offline Harmony smoke project is contained under 007A and does not invoke an older project.
- The implementation did not start STS2, use Computer Use, or use any GUI automation.
- Fixed identity gates passed for public-beta `v0.111.0`, commit/branch `41cef1ea`, `sts2.dll` 9,757,184 bytes / SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`, and fixture 20,441 bytes / SHA-256 `4DFB27BF3BE315F85ED7306C2953675E6A7888378ACF5F2B8110465695EDC631`.
- The Release build completed with 0 warnings and 0 errors. Nineteen of nineteen synthetic tests passed.
- Synthetic tests cover original/unknown store poison arguments, candidate-only argument access, sealed entry, manager identity negatives, single-CAS terminal winner, stable closed set, post-seal rejection, winner ticket, schema whitelist, fixed identity negatives, exact ordinal progress name, independent precondition enum coverage, update-without-write mutation, swallowed-save-with-zero-write rejection, exact one-write acceptance, and duplicate-write rejection.
- Production source and binary audits reject retired identities/schema, fixed character/monster text, fixed enemy-count gates, the former aggregate failure site, direct-call precondition executor/frame waits, replay-event access, natural-victory and achievement surfaces.
- The source audit confirms exactly one direct update call followed by exactly one direct save call. Actual model IDs feed only the in-memory oracle; they are not compared to fixed values.
- Each precondition has its own failure site: fixture player cardinality, replay setup, `ShouldSave=false`, `NGame`, scene/load completion, manager override identity, run-state binding, combat room, Monster scenario scope, combat state, encounter, character, discovered enemies, enemy enumeration, enemy entry/id, and progress subsystem.
- Generic uncategorized exceptions remain `BLOCKED_INSTRUMENTATION/RunCallback`; they cannot be reported as a confirmed named precondition failure.
- The boundary observer checks seal/enabled first. Original or unknown store identity is rejected before any argument-array element, path, or payload read. Only the exact candidate memory store may expose arguments, and only for the first synchronous exact logical `modded/profile1/saves/progress.save` write.
- Mutation and write are separate terminal classes. Mutation requires update count 1 and memory-write count 0. Write requires save count 1, synchronous memory-write count 1, exact logical name, successful payload deserialization, and canonical equivalence. A swallowed `SaveProgressFile` write error therefore fails.
- Single-CAS terminal sealing, post-seal rejection, winner-ticket artifact authorization, schema whitelist, protection containment, live-seeded four-component equivalence/reference checks, and ordered teardown remain present.
- Offline Harmony smoke patched and unpatched 70 methods.
- Final artifacts are `SPIKEINV007A.dll` 157,696 bytes / SHA-256 `41E2187CD469DF4F76E37FDCCDB867035347B106D6B5FF1B3FCF1412324C730A`, and `SPIKEINV007A.json` 371 bytes / SHA-256 `C8136FC68132A946959C82EE2C4E1BBDB6AD9AB1F5A758021E886BC278CEC1E3`.
- Installed copies match those exact identities. Installation copied only the new DLL and manifest and did not overwrite any existing file.

## Inferred

- The new implementation preserves the already accepted timing, candidate, protection, identity-first audit, terminal and teardown mechanisms while narrowing the direct-call contract to the fields established by INV-007.
- Passing static and offline gates materially reduces instrumentation risk, but it does not demonstrate that the runtime timing gate, scene reconstruction, candidate equivalence, direct mutation/write, protection comparison, or teardown will pass in the next game process.

## Unknown

- No runtime result exists yet. No direct progress method has been called by this probe.
- No profile1 protection baseline, restricted recovery backup, after snapshot, or closed runtime artifact exists yet; those are created only after the one authorized Run click.
- The runtime character, encounter and enemy IDs, whether the fixed load produces a Monster `CombatRoom`, and whether the candidate receives the exact expected mutation/write remain unknown.
- The final classification remains unknown and must not be inferred from Phase 0.

## Single recommended next action

The project sponsor should perform the documented one-click manual game test exactly once, stop at the first terminal result, exit immediately without retrying, and return the exact terminal text plus whether combat and Continue were visible. Implementation Codex will then read only the closed 007A artifact and allowlisted 007A evidence.
