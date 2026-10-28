# SPIKE-INV008A report

Status: `R6 FOUR-PHASE PASS / RETURN TO PROJECT LEAD`

## R6 completion — RESTART_AFTER_POST_LAUNCH_R6 attempt 1 — 2026-09-15

- User observed exact `PASS_RESTART_INTEGRITY`, `Failure site: None`. The final artifact independently records the same terminal: `restart-after-post-launch-r6.json`, 14,057 bytes / SHA-256 `23B7A3C9868829C889F052A40C7AA9A6C8AD9F6354FFF984C9515C2231F8B3AD`.
- Its predecessor hash exactly binds the sealed post-launch business artifact. Prior/recovery identity, GateReady and automatic `RestartAccepted`/`RestartVerified` order passed.
- The final restart was read-only with no override, candidate, run or recovery. Protected content/inventory, fixed mtime whitelist, Continue canonical identity/`NumReloads=4`, null run state and manager baseline all passed.
- Final normally exited log: 181,374 bytes / SHA-256 `CFC35A466497015274E6BECE8794184C0CF672BEB010105BC1105D84FC9F057A`; 007A disabled, exactly 008A loaded, `NGame.Quit` present.
- The complete artifact chain is setup `6A665C06...19F7E` -> setup restart `2FD08866...9C205` -> post-launch `23B6D9B7...6B2DE` -> final restart `23B7A3C9...8B3AD`. Both fault phases and both restart phases passed.
- Validation: `r6-restart-after-post-launch-attempt1-validation.txt`, SHA-256 `9E67E2300ECE2844E10E215C3BDE3A4F7E0F09A6D1A312691234C142512BEAC6`. Project Lead summary: `r6-final-project-lead-summary.md`, SHA-256 `3D03AF12DE32879BD51110EBDB8B900842FBA51017084C1220F57C14FF94B7B0`.
- Strongest remaining Unknown: behavior outside ordered probe-owned synchronous owner faults and normal exits, including forced/native crash, owner-finally escape, replay dispatch, map/room gameplay, natural victory, rewards and exhaustive deferred/native side channels.
- Unique next recommendation: Project Lead review/classification of this sealed four-phase PASS. No crash test, `SPIKE-001A2`, `SPIKE-001B` or additional runtime phase is authorized here.

## R6 POST_LAUNCH_OWNER_FAULT_R6 attempt 1 — 2026-09-15

- User reported that the run matched the expected result. The artifact independently seals `PASS_OWNER_FAULT_TEARDOWN` with failure site `None`: `post-launch-owner-fault-r6.json`, 27,848 bytes / SHA-256 `23B6D9B7A0CC34B2C178F120F5BBDF4D4AE89A4CA2709891A3B33AEF4EC6B2DE`.
- It binds the completed setup-restart artifact exactly. Prior identity/recovery, GateReady, run revalidation, phase-local protection, candidate isolation and Continue checks passed.
- Launch entered/returned exactly once and the owned `NRun` was installed once. The `AfterSceneInstall` controlled fault executed with throw/catch/finally 1/1/1 and did not escape. Every forbidden gameplay, map/room, dispatch, action, reward, progress and replay counter remained zero.
- All five lobby observations, the 13-call core cleanup oracle, tasks and manager restoration passed. The owned scene identity was current at fault and cleanup entry, then a new `NMainMenu` replaced it.
- The repaired callback contract passed: candidate/ready/refresh identity `1257053`, counts 1/1/1, both tasks `RanToCompletion`, no identity/order/load error.
- Protected state and Continue were unchanged inside the transaction. The post-exit comparison retained equal content/length/inventory and only the fixed three mtime updates.
- Post-launch recovery has 12 files with aggregate SHA-256 `DEF53A6A506CF40F7428E9D9817CAF56989694409DA00A940951E8513D502C5C` and must not be restored. Complete log: 187,123 bytes / SHA-256 `CC50CE85D68F2CD4F8556D8DDA3BDE4EF908D9BB89F80769E249CB9C19BDAB87`.
- Validation: `r6-post-launch-attempt1-validation.txt`, SHA-256 `EF4D3C144344560F4095CB5F2BF84DA72AF9A93015A4EC8549AC58A8845C162D`. `RESTART_AFTER_POST_LAUNCH_R6` is the sole unlocked phase.

## R6 RESTART_AFTER_SETUP_R6 attempt 1 — 2026-09-15

- User reported the expected `PASS_RESTART_INTEGRITY`. The artifact independently records the same terminal with failure site `None`: `restart-after-setup-r6.json`, 14,045 bytes / SHA-256 `2FD08866A1F897E4F5905E57E0A5C64C036A3C006A2BB3841C4D9F91ABF9C205`.
- Its predecessor hash exactly binds the sealed setup artifact. Prior identity/recovery, GateReady and automatic `RestartAccepted`/`RestartVerified` ordering passed.
- Restart evidence is read-only and records no override, candidate or run. Protected file set/content/inventory, Continue canonical identity/`NumReloads=4`, null run state and manager baseline all passed.
- The only mtime differences were the fixed settings/prefs-backup/progress-backup whitelist. No restart recovery or business evidence was created.
- The complete normally exited log is 182,263 bytes / SHA-256 `385656AD7CED7C762C417368712A8202014CEFC32E76128D1CB940816AC8E746` and proves 007A disabled with exactly 008A loaded.
- Validation: `r6-restart-after-setup-attempt1-validation.txt`, SHA-256 `DEA659FFF9D624F2003476BE82D5CDE03ADBB0F3EDD6410EA3279AE2CE945F6A`. `POST_LAUNCH_OWNER_FAULT_R6` is the sole unlocked phase.

## R6 SETUP_OWNER_FAULT_R6 attempt 1 — 2026-09-15

- User observed `PASS_OWNER_FAULT_TEARDOWN`. The sealed artifact independently records the same terminal with failure site `None`: `setup-owner-fault-r6.json`, 26,719 bytes / SHA-256 `6A665C06B04DC6A9B96BB98C09A8E903FA46AA95F8ED672A09C67E9C2A719F7E`.
- Exact prior-attempt identity, GateReady, protected baseline, candidate isolation and fresh Continue validation passed. The `AfterSetup` controlled fault executed with throw/catch/finally counts 1/1/1 and did not escape; all gameplay/business counters were zero.
- The phase-aware null-lobby oracle, all 13 unconditional cleanup calls, task observations, manager restoration and setup-scene replacement contracts passed.
- R6's repaired callback evidence passed exactly: candidate, ready and refresh identities were all `44378663`; prefix/ready/refresh counts were 1/1/1; menu-ready and refresh tasks were both `RanToCompletion`; no identity/order or load failure was recorded.
- Protection and Continue remained unchanged inside the transaction. The 12-file recovery aggregate is `DEF53A6A506CF40F7428E9D9817CAF56989694409DA00A940951E8513D502C5C` and must not be restored.
- The complete log is 182,225 bytes / SHA-256 `EFF236DDE1305390CE342FA9D325EFEAD191793FA996C0AC1FAD0445A23C2DB3`; it proves 007A disabled, exactly 008A loaded, and normal `NGame.Quit`.
- Post-exit read-only comparison found equal content/length/inventory and only the three fixed normal-exit mtime changes. No precheck, invalid or later R6 artifact exists.
- Validation record: `r6-setup-attempt1-validation.txt`, SHA-256 `038227CEE334303649911E4FEB118D5C948A93D9134E8E84CD96C57C1BB7F559`. The only unlocked next phase is the automatic fresh-process `RESTART_AFTER_SETUP_R6`.

## R6 implementation and Phase 0 — 2026-09-15

- Project Lead sealed R5 and authorized only the restored-menu observer identity/order repair plus independent signal reporting. The four business phases, fault sites, teardown order, protection, mtime rules, 10-second wait and PASS standards are unchanged.
- R6 uses schema `spike-inv008a.owner-fault.v6`, revision `0.6.0-owner-fault-matrix-r6-b1`, manifest SemVer `0.6.0`, and fresh R6-only artifact/precheck/recovery names.
- The active restored-menu candidate is registered in `_Ready` prefix. `RefreshButtons` is accepted only from that exact instance, and menu-ready completes only in its matching `_Ready` postfix. Wrong, duplicate, replaced and order-invalid callbacks fail explicitly; task states are persisted independently.
- The real fixed R5 artifact/recovery and accepted observer-defect contract are mandatory prior inputs. Positive tests prove the complete prior gate reaches GateReady; invalid R5 identity, contract, later artifacts or recovery chain cannot unlock R6.
- All 73 tests passed; Release build completed with 0 warnings and 0 errors; source/IL audit passed and proved the production `_Ready` call order; Harmony patched and unpatched 112 methods.
- R6 Release identities: DLL 288,768 bytes / SHA-256 `9058DD9B3CE2503F06A550642C961B936E5138976240AC820034E0C7E525C83D`; manifest 353 bytes / SHA-256 `5442294037F63781E57BC1803CFE4258BD51AE06138B097FDCB224B40DD8A17D`.
- Safe installation verified the stopped game, exact installed R5 pair, latest complete log proving 007A disabled and exactly 008A loaded, then replaced only 008A's installed DLL/manifest. Post-install checks reconfirmed sealed R5 inputs, fixed settings and unchanged 003A/007A trees.
- No game was started or controlled. No R6 runtime/precheck artifact or recovery exists, and only `SETUP_OWNER_FAULT_R6` is unlocked.
- Evidence: `r6-phase0-preflight-results.txt` SHA-256 `78109259FB9013D92E665FB2F81F20041DDC0FA731D774CAC8E44E55EA88E1F8`; `r6-static-audit-results.txt` SHA-256 `C2CB2BBC5FAB3A100FE5E6CC3A74BC4E23C168F375F4FCAFACF6186822FAC2CA`; `r6-install-results.txt` SHA-256 `30F4A40E0AFD02407B284ACA5E25F4B20BEDC8BAD5EC722787F3CC2DEBFEFF1D`; `r6-post-install-integrity.txt` SHA-256 `86775734375B3A8C9E1550041E66AD5DC621B6F9263C9207D5D2EAD8874D0D61`.

## Project Lead R5 acceptance — 2026-09-15

- 接受R5 artifact身份与所有局部正面证据；最终解释分类为`VALID BUSINESS TERMINAL / MENU OBSERVER ORDERING DEFECT / PRODUCTION MENU FAILURE NOT SUPPORTED`。
- 固定`NMainMenu._Ready` IL在`IL_0493`调用`RefreshButtons()`后才返回。probe的`RefreshButtons` postfix要求传入menu与`_menu`引用相同，但`_menu`直到`_Ready` postfix的`OnMainMenuReady`才更新，因此新menu唯一refresh事件按确定顺序被丢弃。
- `WaitForRestoredMenuAsync`先`Task.WhenAll`再返回两个task状态；refresh task不完成时抛TimeoutException，caller tuple assignment不发生，故artifact中两个false不能证明menu-ready task也未完成。
- artifact的restored scene为NMainMenu、Continue可读且不变，支持production menu已建立；完整log中的`Invalid Task ID`位于初始main-menu完成后、约265秒后的RunAccepted之前，时间上不能由teardown触发，也不能解释本次signal丢失。
- R5永久封存，不重跑、不继续restart。批准R6只修复observer identity建立顺序与独立signal报告；完整分析见`evidence/spike-inv008a/project-lead-r5-menu-observer-review.md`。

## R5 SETUP_OWNER_FAULT_R5 attempt 1 — 2026-09-15

- The user reported normal entry/READY/button behavior, clicked `Run Setup Fault` once, did not enter combat, then saw a transient unread status followed by `FAIL_TEARDOWN_MENU`, `Failure site: MainMenu`, on the returned main menu. The complete log proves a subsequent normal exit.
- The immutable R5 artifact is `setup-owner-fault-r5.json`: 25,790 bytes / SHA-256 `9EE346E65985EE3C64AF34470DF65FFC46C1DD0608C1D7C271F4EF32516A30F9`, terminal sequence 29.
- Prior-attempt integrity and GateReady passed. Phase-aware lobby evidence passed: null before setup, Replay/null after setup and at cleanup entry, with stable null identity and a passing conditional lobby oracle.
- The controlled `AfterSetup` exception was thrown, caught and finalized exactly once without escaping. No Launch, scene install, map/room, process-frame, dispatch, action, victory, reward, progress or replay-write path ran.
- Cleanup entered and returned once with no exception. Every one of the 13 unconditional core cleanup calls occurred exactly once; the lobby unsubscribe/dispose pair was correctly N/A for null lobby. State, ShouldSave, IsCleaningUp and local net ID reached the required values, and no replay write occurred.
- Protection and Continue/NumReloads were unchanged. The original manager was restored. Scene evidence passed and identified the restored scene as main menu.
- The sole failing contract was teardown's menu-completion observation: `menuReadyObserved=false`, `refreshButtonsObserved=false`, `stepErrors=[main-menu-load:TimeoutException]`. The process log contains `ERROR: Invalid Task ID` before the later main-menu startup completion lines, but causality is not established.
- R5 recovery `r5-setup-e41635f71d044ca29e8105ae8a007522` contains 12 files with aggregate SHA-256 `DEF53A6A506CF40F7428E9D9817CAF56989694409DA00A940951E8513D502C5C`; it is sealed and must not be restored. Complete log: 183,524 bytes / SHA-256 `6C2794022719702B3CCF76DDFCBDFE8B2F2942D225AE0306AEE28DA9875BC848`.
- This is a valid non-precheck R5 business FAIL, so the bounded harness-repair authority is closed. R5 stops immediately: no rerun, recovery restore, rebaseline, `RESTART_AFTER_SETUP_R5`, or later phase. Return to Project Lead with the menu-observation/root-cause question unresolved.

## R5 implementation and Phase 0 — 2026-09-15

- Project Lead accepted INV-009 and authorized the narrow R5 phase-aware harness revision. The four business phases, fault injection points, teardown order, observer, protection, mtime rules and PASS standards remain unchanged.
- R5 uses schema `spike-inv008a.owner-fault.v5`, revision `0.5.0-owner-fault-matrix-r5-b1`, legal manifest SemVer `0.5.0`, and fresh R5-only artifact/precheck/recovery names.
- The cleanup oracle preserves the exact-once 13-call unconditional core. The `RunLobby` unsubscribe/dispose calls are a conditional exact pair based on recorded phase-boundary lobby state. Pre-setup stale lobby blocks before recovery/candidate/override/run/fault; post-launch phases also prove owned-scene identity and replacement/restoration.
- The real sealed R4 artifact/recovery and accepted INV-009 outputs are mandatory fixed prior inputs. Positive tests show the complete prior gate reaches GateReady and the R4 artifact receives only the narrower core/conditional-lobby result, not retroactive full PASS.
- All 62 tests passed; Release build completed with 0 warnings and 0 errors; source/IL audit passed; Harmony patched and unpatched 112 methods.
- R5 Release identities: DLL 275,456 bytes / SHA-256 `20C5BC84E378E507013A51887629779BC31966A23CEE637C2E0DD2D0DF1E7D3B`; manifest 353 bytes / SHA-256 `385E1D6D633757D93A326B2846950EA06D1F505AC1D0A520069B6D58D0E796FD`.
- Safe installation verified the stopped game, exact installed R4 pair, and latest complete log showing 007A disabled and exactly 008A loaded, then replaced only 008A's installed DLL/manifest. Post-install checks reconfirmed all sealed prerequisites, fixed settings, and unchanged 003A/007A trees.
- No game was started or controlled. At Phase 0 handoff no R5 runtime/precheck artifact or recovery existed, and only `SETUP_OWNER_FAULT_R5` was unlocked.
- Evidence: `r5-phase0-preflight-results.txt` SHA-256 `88917B7D92769D7A0AC18C5129E48FD153B1AA33082D027285691DA4890410D3`; `r5-static-audit-results.txt` SHA-256 `E7FC96987444F4944F2A9AC5C195100DB0B5253B8A811C173FFB668FE165FFD9`; `r5-install-results.txt` SHA-256 `F2BA5637F8900C97628280C42AAAC505F61A3A145B4F3304F9BE3EE2B4C71EA7`; `r5-post-install-integrity.txt` SHA-256 `CED682B36453D2490A323E0908121447D04FA4474DD219D6F51C3027B2972CAD`.

## Project Lead R4 acceptance — 2026-09-15

- Accepted `setup-owner-fault-r4.json` as a valid R4 business artifact: 23,241 bytes, SHA-256 `40F9B6DA2B3ED3B4D07A619E9EB9E35BA8C25EBEC13C5355F2D4897697EED040`, terminal `FAIL_TEARDOWN_CLEANUP`, failure site `Cleanup`.
- The controlled exception was injected after `ReplaySetupComplete` and before Launch. Fault, owner catch and owner finally each ran exactly once; the exception did not escape. No scene/map/room/action/reward/replay/progress path ran.
- Protection and restoration evidence passed: all 12 protected contents and Continue remained equal, `RunManager.State` became null, the original manager was restored, and cleanup itself recorded no thrown exception.
- The fixed oracle required 15 cleanup calls and observed 13. The only missing calls were `RunLobby.remove_RemotePlayerDisconnected` and `RunLobby.Dispose`.
- Existing fixed IL shows `SetUpReplay` calls `InitializeRunLobby`, while `CleanUp` executes those two calls only when `RunLobby != null`. The artifact does not record the lobby identity/null transition. Therefore R4 proves a cleanup-contract failure, but does not yet distinguish a production lobby-lifecycle defect from an unconditional oracle requirement that is inapplicable at this setup-fault boundary.
- R4 is permanently closed. Do not rerun, restore/rebaseline recovery, or execute `RESTART_AFTER_SETUP_R4`. The only approved next work is pure static `INV-009`; no R5 is authorized.

## R4 implementation, positive reachability proof, and Phase 0 — 2026-09-15

- R4 uses schema `spike-inv008a.owner-fault.v4`, revision `0.4.0-owner-fault-matrix-r4-b1`, legal SemVer `0.4.0`, and R4-only artifact/precheck/recovery names. R3 remains permanently sealed.
- The prior-integrity contract now compares the fixed historical profile/account chain by content, validates `settings.save` against the Project Lead post-R3 identity, captures fresh Continue at GateReady, and uses phase-local source mtimes rather than recovery-copy mtimes.
- A positive integration test consumed the actual fixed 007A-R3 artifact and recovery plus the fixed v1/R2/R3 schemas/current identities and proved the full gate reaches `GateReady`. Independent negative cases cover historical settings, active content, inventory, fresh Continue categories, and restart mtime policy.
- All 51 tests passed; Release build completed with 0 warnings and 0 errors; source/IL audit passed; Harmony patched and unpatched 112 methods.
- Installed R4 identity: DLL 245,248 bytes / SHA-256 `422FF9C84722394E3E99A708DA3EE31140540AE067DCD22318BB7330188BFB16`; manifest 353 bytes / SHA-256 `C5E73FEAAC16C49BFF2C14625D8D7340A4D17D683F4D2B4A5F21D31DDE259D26`.
- Safe installation started from the exact installed R3 pair with the game stopped and replaced only 008A's pair. Fixed R3 evidence/workspace artifacts, 003A/007A trees, enabled configuration, and fixed settings remained unchanged.
- No R4 runtime/precheck artifact or recovery exists. Codex did not start or control the game. `SETUP_OWNER_FAULT_R4` is the sole unlocked manual phase; no owner-fault/restart conclusion exists yet.

## Project Lead R3 acceptance — 2026-09-15

- Accepted the R3 artifact identity and its null business fields, but reclassified the meaning as `VALID ARTIFACT / IMPOSSIBLE PRIOR GATE`, not evidence of transaction or player-state corruption.
- Independent length/hash comparison found all ten active-profile files plus AccountSelector content still equal to the fixed 007A-R3 state. Only `settings.save` content differs; this component contains mod enable/disable configuration that the test setup intentionally changed.
- Source review confirmed two deterministic gate defects: recovery-copy mtimes were compared as if they were source mtimes, and the fixed 007A-R3 artifact lacks both Continue structures accepted by `ReadContinue`, making `continueEqual` unconditionally false.
- R3 remains permanently closed. R4 is authorized by `project-management/handoffs/SPIKE-INV008A-R4.md` with fresh phase-local protection/Continue baselines and real-schema positive tests.
- Full review: `evidence/spike-inv008a/project-lead-r3-prior-integrity-review.md`.

## R3 SETUP_OWNER_FAULT attempt 1 — 2026-09-15

### User observation

- The entry appeared. After opening it, the page remained at `WAITING_EVENTS` for about ten minutes and never showed READY. The user did not click `Run Setup Fault` and exited normally when instructed.

### Confirmed machine evidence

- `setup-owner-fault-r3.json` is 3,740 bytes / SHA-256 `5C8E49B98F7BC9A647033F779DC3CACCEBE3D840214E4D95995EBADB7C0B13F0`; it sealed at sequence 8 / 119,398,825 monotonic ticks as `BLOCKED_PRIOR_INTEGRITY`, site `PriorIntegrity`.
- The post-loader mod-set check passed: no R3 precheck artifact exists, while the complete log confirms 007A disabled and exactly 008A loaded. The v1/R2 prior-attempt gate also passed.
- Prior integrity confirmed the fixed INV007A-R3 artifact/recovery identities, profile 1, inventory, null run state and stable manager, but `currentContentEqual=false`, `mtimeWhitelistPassed=false`, and `continueEqual=false`. Non-whitelisted mtime differences were also recorded.
- `protectionBefore/After`, candidate, recovery, fault, audit, teardown and restart are null. No action button was clicked and no controlled owner fault ran.
- Complete log: 184,193 bytes / SHA-256 `D6AABEF1BDB7C2102CFA338CFC16EC10DF157806CBEF15C6001C9385727BA31F`. The game exited normally.

### UI diagnosis and disposition

- Terminal sealing occurred before the probe page was opened. `ShowTerminalFixed` had no live status label to update; opening the page later rendered the stale non-ready `WAITING_EVENTS` text. Waiting longer could not change the already sealed result.
- This is a non-precheck BLOCKED with prior-integrity/profile access and an R3 business artifact. The R3 repair envelope therefore terminates. No repair/rebuild/reinstall/rerun, backup restore, rebaseline, or later phase is authorized. Result: `NO OWNER-FAULT OR RESTART CONCLUSION / RETURN_TO_PROJECT_LEAD`.

## R3 implementation and Phase 0 — 2026-09-15

### Confirmed

- R3 uses schema `spike-inv008a.owner-fault.v3`, revision `0.3.0-owner-fault-matrix-r3-b1`, legal manifest SemVer `0.3.0`, and R3-only artifact/recovery/precheck names.
- The initializer no longer reads or evaluates `ModManager.GetLoadedMods()`. The first exact loaded-set decision is at the start of the first normal `NMainMenu._Ready`, before `OwnerFaultTransaction.Active`, `SaveManager.Instance`, Continue, prior-integrity, profile, protection, candidate, override or run access. Exact revalidation remains at the later gates.
- Prior-attempt validation requires the exact two v1 artifacts, exact sole R2 precheck artifact and bounded contract, absence of all R2 business/invalid artifacts, and absence of v1/R2 recovery.
- The original 41 R2 tests remain and two R3 lifecycle suites were added: initializer deferral followed by post-loader `[SPIKEINV008A]` PASS, plus post-loader empty/007A/other failures that create only precheck evidence. Total: 43/43 PASS.
- Release build has 0 warnings/errors; source/IL audit passes; Harmony patched and unpatched 112 methods. Frozen fault, teardown, observer, protection, candidate, UI, entry, JSON and session source hashes remain unchanged.
- Offline and install gates confirmed the game was stopped, installed 008A exactly matched the R2 pair, the fixed complete log proved 007A disabled and only 008A loaded, then replaced only the 008A DLL/manifest. Enabled configuration and 007A files were not modified.
- Installed R3 identities: DLL 235,008 bytes / SHA-256 `2FE763CC52805D1C9ED513D97B0AF8370EB4BF659E6CB8BF376F99747D047F01`; manifest 353 bytes / SHA-256 `1B0ECD1E95639DA0B95EB84576C17D4D0CAB67D70D2E15496702533A028D22A3`.
- No R3 runtime artifact, precheck or recovery exists. Codex did not start or control the game.

### Unknown

- Runtime post-loader precheck, timing/prior-integrity, owner fault, teardown, protection and restart behavior remain unknown. Only `SETUP_OWNER_FAULT_R3` is unlocked.

## Project Lead R2 acceptance — 2026-09-15

- Independently verified the 242-byte precheck artifact SHA-256 as `2194A2A05E24ECCBDE8C39035BF389A99511E0FD50F4DD998B3688A87E9B9916` and parsed only the bounded v2 precheck fields with `loadedModCount=0` / `WrongLoadedModSet`.
- Independently verified the current complete log records 007A skipped as disabled, exactly one loaded entry (`SPIKEINV008A`), and `Loaded 1 mods (16 total)` with no 008A invalid-SemVer warning.
- Source review confirms `ProbeEntryPoint.Initialize()` calls `ProbeController.Activate()`, whose line 91 synchronously evaluates `GetLoadedModIds()` before the loader logs initializer completion and publishes its final set.
- Accepted classification: `PREBUSINESS_PRECHECK_TIMING_DEFECT / NO R2 FAULT-RESTART CONCLUSION`. No transaction, protection, fault, teardown, restart, or isolation claim is accepted from R2.
- R2 is closed. `project-management/handoffs/SPIKE-INV008A-R3.md` authorizes a narrow same-lineage revision plus a bounded pre-business harness repair envelope; all v1/R2 evidence remains immutable.

## R2 configuration precheck attempt 1 — 2026-09-15

### User-observed runtime result

- With 007A disabled and only 008A selected, the user reached the main menu but the `SPIKE-INV008A` entry did not appear.

### Confirmed machine evidence

- `precheck-modset-r2-998e07af004a4268b1c33dd2e7644bc5.json` is 242 bytes / SHA-256 `2194A2A05E24ECCBDE8C39035BF389A99511E0FD50F4DD998B3688A87E9B9916`. It records `loadedModCount=0`, an empty bounded ID list, and `WrongLoadedModSet`.
- The corresponding complete process log is 252,133 bytes / SHA-256 `7F01AE43BA2E43776D7A60413B3D6BC07875BFF38B92DE448420DDC573540171`. It explicitly reports 007A skipped because it is disabled, lists exactly `SPIKEINV008A`, and reports `Loaded 1 mods (16 total)`.
- The log has no `SPIKEINV008A` invalid Semantic Version warning, dynamically confirming the manifest `0.2.0` correction.
- No R2 business/invalid artifact or recovery exists. The fixed v1 artifacts are unchanged. The game exited normally.

### Diagnosis and disposition

- R2 calls `ModManager.GetLoadedMods()` from the 008A initializer. At that moment the loader has not yet published the final loaded collection, so the precheck observed zero even though the loader completed with exactly 008A. The isolated precheck correctly avoided consuming a business artifact, but its observation point is invalid.
- This is a pre-business harness lifecycle/timing defect. It is not an enabled-state error and provides no transaction, owner-fault, teardown, protection or restart conclusion.
- The handoff's retry authorization covers an actual wrong-mod-set configuration that the user can correct. Here the configuration was already correct, so no retry is authorized. Work stops and returns to Project Lead; Implementation Codex will not create an R3 or move the precheck without a new handoff.

## R2 implementation and Phase 0 — 2026-09-15

### Confirmed

- R2 uses schema `spike-inv008a.owner-fault.v2`, artifact revision `0.2.0-owner-fault-matrix-r2`, legal manifest SemVer `0.2.0`, four R2-only business artifact names, `invalid-phase-r2.json`, and `r2-setup-`/`r2-postlaunch-` recovery prefixes.
- Exact v1 artifact identities and their pre-business-null fields are mandatory prior-attempt inputs. Any mismatch or v1 recovery produces `BLOCKED_PRIOR_ATTEMPT_UNRESOLVED`; v1 terminal results are not interpreted as R2 phase results.
- Wrong loaded-mod sets are isolated in bounded, attempt-specific, create-new `precheck-modset-r2-<attempt>.json` evidence before profile/protection/candidate/override/run access. Such evidence cannot unlock or consume a business phase.
- R3-to-current first-phase integrity now compares content, length, inventory, Continue canonical/`NumReloads`, and records mtime-only differences under the same exact three-item whitelist.
- All original 34 tests plus 7 R2 tests pass (41/41); Release build has 0 warnings/errors; source/IL audit and 112-method Harmony patch/unpatch smoke pass.
- Static audit hashes confirm no change to the v1 transaction, fault injection points, teardown, observer, protection, candidate, UI actions, entry point, JSON support, or terminal-session implementation.
- Installation lifecycle passed in order: stopped game; exact installed v1 008A pair; latest complete log proving 007A disabled and exactly 008A loaded; replacement of only 008A's pair. Installed 007A identities remained unchanged.
- Installed R2 identities are DLL 229,888 bytes / SHA-256 `D5746F9C560B6A0E09BE6235491554A47DAB3B1AEA84078EB25BF2E8077D2615` and manifest 353 bytes / SHA-256 `0FF3138632515C43F98E111752336C223C89B751AFFA8740491AFD11FF3118A1`.
- No R2 runtime artifact, configuration-precheck artifact, or recovery directory exists; Codex did not start or control the game.

### Inferred

- The valid SemVer manifest removes the specific v1 invalid-version warning cause. Runtime confirmation requires the first R2 startup log.
- The isolated precheck structure prevents a future wrong-mod-set attempt from consuming `setup-owner-fault-r2.json`; patch smoke is not runtime proof of that behavior.

### Unknown

- R2 timing/prior-integrity gate, exact owner fault, teardown, boundary, protection and restart behavior remain unknown until their authorized fresh-process phases run.
- `SETUP_OWNER_FAULT_R2` is the only unlocked runtime phase. All later phases remain locked.

## Project Lead v1 runtime disposition — 2026-09-15

- `setup-owner-fault.json` is a valid immutable v1 artifact: 2,359 bytes / SHA-256 `56E0725994F19BF8917AB34EA7AFD8851340CF9A12CE78635C7AD6E71A310B48`, terminal `BLOCKED_IDENTITY / Identity`, events 1–8 contiguous and monotonic.
- The artifact stopped immediately after startup/timing observations because the loaded mod set contained both `SPIKEINV007A` and `SPIKEINV008A`. `protectionBefore/After`, prior integrity, candidate, recovery root, fault, audit, teardown and restart are all null. It provides no owner-fault, teardown, protection or restart result.
- After the user disabled 007A, the next process log showed exactly one loaded mod, 008A. However the immutable v1 setup artifact had already consumed the phase slot; `invalid-phase.json` (1,479 bytes / SHA-256 `A1D6BC41405FFCBCC2797E742A1D0E424FE4CFD1330BA7E3A8C8601CEB0AEC8F`) contains only `PatchActive → BLOCKED_PHASE_ORDER / PhaseOrder`, with all business fields null.
- Project Lead independently reran all 34 synthetic tests, the zero-warning/error Release build, source/binary audit and 112-method Harmony patch/unpatch smoke; Phase 0 evidence remains accepted.
- Classification: pre-business environment/install-lifecycle block, not a transaction or fault/restart FAIL. V1 artifacts are permanently closed and may not be overwritten or rerun.
- A narrow same-source R2 is authorized by `project-management/handoffs/SPIKE-INV008A-R2.md`. It fixes only revision/artifact identity, prior-attempt handling and install/configuration precheck lifecycle; the four-phase business contract is unchanged.

## Phase 0

### Confirmed

- The independent `SPIKEINV008A` source, tests, manifest, build, audit, preflight, and create-new installer exist.
- All 34 synthetic tests pass; Release build has zero warnings and zero errors; source/Mono.Cecil audit passes; Harmony patch/unpatch smoke patched 112 methods and removed them.
- The fixed game, fixture, R3 artifact, and 12-file R3 recovery identities match the handoff.
- Preflight and post-install aggregates show `SPIKE-INV003A`, `SPIKE-INV007A`, and both old evidence trees unchanged. No old probe was built or run. The installed 007A pair retained its prior hashes.
- The new installed pair is DLL SHA-256 `4C17CA45F142EFA7ADB684D1B54706D07FDE0B0FB4243B2D535BE2A211667717` and manifest SHA-256 `FD25C4E83E304204DFB4E177E56074E7D7D63822871577A2912E7C5629A6D6C4`.
- At Phase 0 completion, no game process had been started or controlled and no 008A phase artifact or recovery directory existed. The later v1 runtime disposition above supersedes only the artifact-existence part of this historical statement.

### Inferred

- Static source/IL structure enforces the four create-new artifact chain, atomic READY timeout disarm, exact owner-fault catch/finally shape, single-CAS teardown ownership, cleanup observation, non-skippable manager restoration, explicit main-menu/RefreshButtons gates, and exact restart mtime whitelist.
- Patch smoke confirms the fixed assembly can resolve and patch its declared v0.111.0 targets outside the game process. This is not runtime behavioral proof.

### Unknown

- Runtime gate order, candidate equivalence, exact controlled fault, all boundary counters, cleanup/disposer reachability, task terminal states, manager/menu restoration, Continue equivalence, and protection equivalence remain unknown until the authorized phase executes.
- Restart integrity and all later phases remain locked.

## Historical v1 SETUP_OWNER_FAULT

Not run. The filename was consumed by the pre-business `BLOCKED_IDENTITY` artifact before any protection or recovery was created.

## Historical v1 RESTART_AFTER_SETUP

Locked pending a valid Phase A PASS artifact and a normal user exit. No recovery is allowed in this phase.

## Historical v1 POSTLAUNCH_OWNER_FAULT

Locked pending valid Phase A and B PASS artifacts.

## Historical v1 RESTART_AFTER_POSTLAUNCH

Locked pending valid Phase A, B, and C PASS artifacts. No recovery is allowed in this phase.

## Scope limit

Even a complete four-phase PASS would cover only probe-owned synchronous owner exceptions, ordered teardown, and normal-exit restart integrity on the fixed build. It cannot establish safety for strong process termination, native crash, replay event dispatch, natural victory, map/room progression, gameplay actions, rewards, or unobserved native/deferred side channels.
