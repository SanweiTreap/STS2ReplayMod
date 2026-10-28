# SPIKE-001A4 Investigation Report

更新日期：2026-09-24  
状态：`FAIL_HARNESS_AFTER_BASELINE`；terminal sealed；A4 persisted disabled；final cleanup verification protocol FAIL；work closed

## Current conclusion

The fresh `SPIKE001A4` Phase 0 implementation is complete. Original r1 Release build passed with 0 warnings and 0 errors; A4 tests passed 59/59; the unchanged INV-011 contract suite passed 40/40; source and compiled-IL audits passed. After the pre-business UI defect, narrow r2 passed Release 0/0, A4 61/61, INV-011 40/40, and source/IL audit. The r2 pair is sealed and remains installed; r1 remains sealed for rollback. The runtime work package terminated as `FAIL_HARNESS_AFTER_BASELINE`; reconstruction equivalence remains Unknown.

Implementation Codex did not start or control the game; the project initiator performed every requested manual launch. No other profile was read or selected by Implementation Codex. The sole A4 business attempt contains sealed arm, baseline, recovery, and terminal evidence, but no A snapshot and no A/B comparison. `SPIKE001A3` remains retired and read-only. `SPIKE-001B` remains blocked and was not entered.

The project initiator manually completed install lifecycle Gates 1 and 2 while A4 was absent. Gate 2 completed log `godot.log` (45,927 bytes, SHA-256 `2850D234FB210EE3EBA1D55673600CE2307B710D8AAB46AB815EA6B13D07D0ED`) proves normal shutdown, empty loaded-mod set, and persisted-disabled evidence for all eight installed project probes. The read-only enablement precheck passed.

Gate 3 then installed only the new A4 DLL/manifest pair while the game was stopped. Installed hashes exactly match the sealed Release artifacts, and the installer verified every pre-existing live-mod file remained byte-identical. This does not establish enabled state or runtime behavior.

The first A4-only main-menu process reached `READY_FIXTURE_PREP`, loaded exactly `[SPIKE001A4]`, exited normally, and created no business artifact. Its completed log also recorded a deterministic A4 `ProbeUi.Show` ZIndex range error: r1 assigned `10000`, above Godot's `CANVAS_ITEM_Z_MAX` of `4096`. Because this occurred before any attempt/baseline/recovery/snapshot, the handoff's pre-business UI repair envelope applied.

Narrow r2 changes only the UI ZIndex plus required revision identities (`0.1.1-r2`, schema v2, r2 namespace). Release build passed 0/0, A4 tests passed 61/61, INV-011 remained 40/40, and source/IL audit passed. The sealed r2 pair was installed while stopped after exact r1/log/no-attempt gates; r1 artifacts and log remain preserved.

The r2 A4-only smoke then loaded exact set `[SPIKE001A4]`, initialized assembly version `0.1.1.0`, showed `READY_FIXTURE_PREP` exactly once, emitted no prior ZIndex condition and no A4 BLOCKED/FAIL/terminal, exited normally, and left all business artifact classes absent. Gate 4 is PASS. Current Continue location remains unknown and must not be assumed from the retired A3 history.

Fixture reconnaissance on 2026-09-24 used normal profile1 Continue once and restored into Act 1 floor 3 normal weak combat (`FUZZY_WURM_CRAWLER_WEAK`). Live read-only evidence showed exact loaded set `[SPIKE001A4]`, READY only, no arm/phase/terminal output, and no business attempt directory. This combat may be completed as the new normal-Monster predecessor, but no target is selected yet.

The initiator completed floor 3 and skipped rewards. The next node is a mandatory question mark; after it the route branches to a left question mark and a right normal Monster. Because entering the mandatory question mark replaces the prior combat checkpoint with a null map-entry checkpoint, floor 3 cannot be the final predecessor. The right floor 5 Monster must be completed before any later directly reachable Monster may be armed.

The initiator then entered the floor 4 event and reported `脑蛭`, HP `80/80`, gold `112`. Live log evidence confirms the Event Room loaded in the same process, with no A4 arm/terminal output and zero business attempt directories. The visible event options were not included in the user report, so no option is guessed or authorized yet.

The initiator subsequently reported both exact options: Share Knowledge forces one of five random cards into the deck; RIP costs 5 HP and grants a colorless card reward. Existing fixed replay evidence records the RIP sequence as `ClaimReward`, `TakeCard skip`, then `PROCEED`. Therefore RIP plus skipping the reward is the narrower fixture-preparation path: a predeclared 5 HP change and no deck addition. The next request deliberately stops immediately after RIP before any reward interaction.

The initiator then reported completing RIP, skipping the card, pressing proceed, and returning to the map, which exceeded the RIP-only stop boundary. This is preserved as a user-observed pre-arm fixture deviation. It occurred before any protection baseline or business attempt, matched the already established RIP/skip/proceed sequence, and added no card. A read-only check at `2026-09-24T16:32:09.5975708+08:00` confirmed the same process, zero A4 attempt directories, and no A4 arm/BLOCKED/FAIL/terminal. The current map offers a left question mark and a right normal Monster; only the right Monster is suitable for preparing a non-null predecessor.

The initiator completed the right floor 5 Monster and reported a directly reachable normal Monster node whose encounter uses the strong pool. Read-only runtime evidence identifies the completed predecessor as `SLIMES_WEAK` at coord `(1,4)` and confirms its victory, the same process, zero A4 attempt directories, and no A4 arm/BLOCKED/FAIL/terminal. A normal Monster point remains in-scope regardless of whether its selected encounter is from the strong pool. The fixture is now suitable for a standalone F8 arm request.

The initiator pressed F8 once and observed `READY_A_NODE`. Authoritative log/artifact evidence resolves the transcribed coordinate as Act 1 row 5 col 2 `Monster`, with predecessor floor 5 and expected playable floor 6. Unique attempt `spike-001a4-r2-7623499da7d34f808fdeb9b9c5e2b0f8` contains only `arm.json` (8,718 bytes, SHA-256 `B08D696E0ECDDB1E7D4112166FFD722BC3D8BE56FAE5FAE9A2039D0E179EC4DB`). The stable non-null checkpoint is 54,114 bytes / `57B293DE6016A66F7E78DAD5E034699FBA00E4714977BF47A26A612AC5565FE0`, floor 5 at predecessor coord row 4 col 1. Environment, profile1, exact loaded set, fixed identities and Cloud-off all pass. Checkpoint floor remains explicitly Unbound and no baseline/recovery/A snapshot/terminal exists yet.

The target click then reached coord `(2,5)` and created `RUBY_RAIDERS_NORMAL`. Production room-before save completed and formal baseline/recovery sealed successfully. `checkpoint-baseline.json` is 51,213 bytes / `4BD7F1D8903EB43012027A9F8452342A4CBAF76FDE5662535627E6D653CB92B4`; recovery and the live save are 54,765 bytes / `18D2BBAB2556A0FEB8633C0800349CE97C0D803880041CB574615E588F9D1ADB`. The checkpoint floor is Bound(5), `PreFinishedRoom=null`, at the exact target coord.

However, 172 seconds after production completion there was still no `snapshot-a.json`, no `terminal.json`, and the UI remained the stale `READY_A_NODE`. Code review confirms r2 cancels the capture deadline immediately after baseline creation and the expiry callback returns whenever baseline exists, so the required 30-second post-baseline A capture deadline is not enforced. This is a harness failure after baseline, not a reconstruction difference. Because baseline and recovery already exist, the repair envelope is terminated by the handoff: no code repair, replacement, restore, rebaseline, rerun, Phase B, or `SPIKE-001B` is permitted.

The user then exited with Alt+F4. The process ended normally and the completed log contains the expected `NGame.Quit` and Steamworks shutdown chain. Post-exit `current_run.save` remains byte-identical to the baseline/recovery (54,765 bytes, `18D2BBAB2556A0FEB8633C0800349CE97C0D803880041CB574615E588F9D1ADB`). Protection directory/file inventories are equal, but protection comparison fails: `replays/latest.mcr` changed content and length, and `settings.save` changed content; prefs/progress backup changes are mtime-only and allowlisted. This secondary failure does not convert the missing-A harness result into a reconstruction diff. The completed log is preserved at 202,723 bytes / `4BB90404B1BFF000E360457BE9BE5FF5BEADBA2B1874FE56A0DDF1A7C252D5B4`.

A no-run cleanup launch then terminalized the incomplete attempt exactly as designed on resume. `terminal.json` is 2,379 bytes / `67C3545720D5B7A9EF8B475832235F9EB00BAB755D9F49105DABB4EA5373D649`, with classification `FAIL_HARNESS_AFTER_BASELINE`, site `attempt.resume`, reason `formal baseline process ended without A`, and the correct embedded checkpoint identity. The cleanup log contains no run entry and a normal shutdown; it is preserved at 188,108 bytes / `FD5E4ED1B8E4D43C10F8447600991A632488BD145B2D7A14943FFD205D3E82E8`.

The initiator then performed the end-of-work disable process and closed the game. Its completed log is preserved at 187,096 bytes / `820162FF3829B941D57553EED11AFCE4C5AB3BD2A1BF718175BC7E207E99D60B`. That process still loaded A4, displayed `TERMINAL_ALREADY_RECORDED`, wrote `settings.save`, entered no run, and shut down normally; this is the expected process in which the enabled-state change is submitted. The terminal identity remained unchanged. One separate no-run launch is still required to prove A4 is absent from the next process's loaded set.

The next independent process proved that `SPIKE001A4` was persisted disabled: the loader explicitly skipped it, and no A4 assembly, initializer, UI, or terminal marker ran. The completed log is preserved at 368,279 bytes / `04DBDAE2FB7ACEEC9780E10CE28386EE8F9412FF64EEC68046207E82784E9C99`; the terminal identity remained unchanged. However, this final cleanup process did not follow its no-profile/no-run contract. It loaded `AscensionUnlocker`, entered profile3, started and repeatedly continued a run, performed gameplay, and wrote profile3 save/progress/prefs state. This cleanup verification is therefore a protocol failure, while the narrower A4 persisted-disable criterion is PASS. Other-profile effects are not investigated or restored, and no retry is permitted.

An earlier immutable r2 precheck from 2026-09-23 recorded `BLOCKED_ENVIRONMENT` because the active profile was `modded/profile2`. It consumed no business attempt. The initiator subsequently corrected to profile1 before the current READY/Continue process. The precheck remains preserved and no profile2 save/content was read by Implementation Codex.

## Confirmed

- Current repair identity: `SPIKE001A4`, build `0.1.1-r2`, schema `spike-001a4.reconstruction-control.v2`, create-new namespace `spike-001a4-r2-{guid}`. Original r1 identity and artifacts remain preserved.
- Fixed game and INV-011 hashes match the handoff.
- Floor state uses arm predecessor, explicit unbound/bound checkpoint floor, and checked derived playable floor.
- Concrete wrapper behavior is verified using real completed/faulted/canceled tasks, original exception/token semantics, and terminal-writer failure branches.
- Semantic projection excludes phase-local identities and obtains room and encounter identity independently.
- Required card/RNG/room/encounter/creature values fail capture when missing or invalid rather than comparing fallback sentinels.
- Ticker/deadline/race/terminal/create-new and save/protection offline gates pass.
- Compiled observer-only branch contains no rethrow into the production continuation.
- Install lifecycle Gates 1 and 2 completed with normal-shutdown logs; Gate 2 loaded no mods.
- A4 r2 remains installed with sealed repair hashes. Its only business attempt terminated after baseline without A; the immutable terminal records `FAIL_HARNESS_AFTER_BASELINE / attempt.resume`.
- A4 is persisted disabled and was absent from the final process's loaded set; the game process is stopped.

## Inferred

- The production task completed and the formal checkpoint hook sealed baseline/recovery without faulting the map action, supporting observer non-interference up to that boundary. The post-baseline A-capture deadline was not enforced because the harness canceled it too early.

## Unknown

- A/B semantic equivalence, because A was never captured and Phase B is permanently prohibited.

## Artifacts

- r1 DLL: 193,024 bytes; SHA-256 `3B3318D7BB1C87769613F2F5F097F2374B44B3EF2F972FC55A66A59F4BD18DE1`.
- r1 manifest: 362 bytes; SHA-256 `39A04B7436E1D38615BAF337D36C1A316D338A491CAC96FD4936AB78C76F34A1`.
- r2 DLL: 193,024 bytes; SHA-256 `8A686F518B3A56A356197495BA05A30E701371FF89C12F8448DCC4E066D7D915`.
- r2 manifest: 362 bytes; SHA-256 `E73EB4BB8E8F1721BFCB5140D9ABB93C88148532B20ED41D93FD35E25FBF29F5`.
- r2 repair evidence: `evidence/spike-001a4/repair-r2/repair-validation.md`.
- Full Phase 0 evidence: `evidence/spike-001a4/phase0-validation.md`.
- Installation lifecycle: `evidence/spike-001a4/installation-lifecycle-plan.md`.

## Next gate

Business work is stopped permanently, the terminal is sealed, and A4 is persisted disabled. The final cleanup verification violated its no-profile/no-run contract and is preserved as protocol failure evidence; it is not retried. No further manual gate is unlocked. No repair/rerun, Phase B, or `SPIKE-001B` is allowed.
