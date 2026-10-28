# SPIKE-INV004A — direct progress-write isolation sentinel

状态：COMPLETE — FAIL at the pre-override isolated-manager content-equivalence gate; direct-write sentinel was not reached. Stopped after the first auditable failure.

## Scope

This experiment tests only whether direct calls to the production `SaveManager.UpdateProgressAfterCombatWon(player, room)` and `SaveProgressFile()` methods are routed to the isolated in-memory manager/store in the fixed replay session. It does not dispatch replay events and does not test natural combat victory, the achievement helper, replay fidelity, reconstruction, or complete session isolation.

## Fixed identity

- Game: public-beta `v0.111.0`, commit `41cef1ea`.
- `sts2.dll`: 9,757,184 bytes; SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`.
- Fixture: 20,441-byte protected native `.mcr`; SHA-256 `4DFB27BF3BE315F85ED7306C2953675E6A7888378ACF5F2B8110465695EDC631`.
- Probe: `0.1.0-direct-write-sentinel`; DLL SHA-256 `5332C311FCC363A69B724620EA0B334AF4A5F62DB10A4A85117CCE85E2B53141`; manifest SHA-256 `2554D7CD6A4FF09AFA5CEA8E03E57130237DC0740E5838958EA29B07D7FB2581`.
- Test environment requires modded profile1, Steam Cloud off, and exactly `SPIKEINV004A` enabled.

## Phase 0 static gate

The hash-gated method-body findings, exact expected mutation, write path/payload model, and audit coverage are recorded in `evidence/spike-inv004a/phase-0-static.md`.

Expected fixed normal-room delta: encounter/IRONCLAD wins `+1`; repeated TOADPOLE/IRONCLAD wins `+2`; losses and all unrelated progress fields unchanged. Live `DiscoveredEnemies` changes only if the TOADPOLE enemy stats had an entirely empty fight-stat list, in which case one TOADPOLE id is appended on the first duplicate.

## Reuse and material differences

Reviewed SPIKE-INV003A Phase B code was reused for the protected preflight/backup, fully initialized deep-copy `MockGodotFileIo`-backed `SaveManager`, pre-setup override, native `SetUpReplay` state factory, `ShouldSave=false`, boundary audit, and ordered teardown. This probe has a separate source/evidence root and fixed artifact identity. It removes the old phase matrix, Stop wait, fault injection, and replay-event dispatcher, and adds exact legality, mutation, path/count, payload-deserialization, inventory, Continue, and original-progress checks.

## Build validation

- `dotnet build -c Release`: 0 warnings, 0 errors.
- Offline Harmony patch smoke: 78 methods patched and unpatched successfully (14 SaveManager, 41 store, 14 external submit/unlock, 4 main-menu run guards, 2 override, plus fixed patches).
- Repository artifact and installed mod hashes match.

## Runtime evidence

F8 produced `AWAITING_USER_CONFIRMATION` with `blockingReasons=[]`; F9 produced `VALID_NATIVE_FIXTURE`. The active machine identity was profile id 1 / `modded/profile1`, Continue visible/enabled, `RunManager.State == null`, exactly this probe loaded, and no pending save task. All ten profile backup copies matched source hashes. An independent post-F9 check found zero differences across all 12 protected file hash/length/UTC-mtime identities and zero profile/account inventory differences.

`User-observed runtime result`: the project sponsor confirmed Steam Cloud was disabled before this session, the visible active profile was modded profile slot 1, Continue was the intended isolation sentinel, and profile1 remained fully unlocked. Before unlock, the same game process remained open and all 12 protected identities were rechecked with zero differences. Phase 0 passed and `phase-control.json` advanced once to `DIRECT_WRITE`.

### Direct-write attempt

`User-observed runtime result`: after clicking `Run Direct Write Sentinel`, the project sponsor observed that the game did not enter combat.

Machine evidence `runtime/direct-write-transaction.json` records a `FAIL` at `2026-09-10T09:17:06Z`. `CreateIsolatedManager` loaded the protected profile/settings data into a new `MockGodotFileIo`-backed manager, initialized profile/settings/prefs/progress, and then rejected the context because at least one of canonical Settings, Prefs, or Progress differed from the current original manager. The exception was `InvalidDataException: Isolated manager deep-copy content does not match the original manager.`

This occurred before `TransactionAudit.Start`, before `SaveManager.MockInstanceForTesting`, before `SetUpReplay`, and before scene launch. Consequently:

- `sceneLaunched=false`; no legal live `Player`/`CombatRoom` sentinel parameters were reached;
- no replay events were read or dispatched by probe code;
- `UpdateProgressAfterCombatWon` actual call count: 0;
- `SaveProgressFile` actual call count: 0;
- isolated `progress.save` write count: 0; there is no payload to deserialize;
- `EndCombatInternal` and the achievement helper were not called;
- no manager override was installed, and no original/isolated/external boundary audit session started. The lone audit event is the unconditional `begin-restore` bookkeeping marker, so it must not be misreported as audited zero-call coverage.

The generic equivalence exception does not identify which of Settings, Prefs, or Progress differed, so the exact in-memory drift component remains unknown. The missing expected update/save/write entries appended to `differences` are consequences of the precondition failure, not additional side effects.

### Recovery and protection evidence

The shared `finally` owner executed best-effort recovery. Because no run or override existed, `CleanUp` was correctly not called. The captured original manager remained current, its progress canonical hash was identical before/after (`975456DA161A5B992260C2A7F3D23B0FEECBE7AAE73F7162962A57F78912C4C5`), `LoadMainMenu(false)` rebuilt the menu, `RunManager.State == null`, and Continue was visible/enabled.

The probe's before/after evidence found zero differences across all 12 protected hash/length/UTC-mtime identities, the ten-file profile inventory, and account top-level inventory. A separate post-failure filesystem comparison independently found zero protected identity differences. No backup was restored and no other profile was read or copied.

## Result

**FAIL.** The approved direct-write sentinel could not establish the required content-equivalent isolated manager in this session and stopped before installing the manager or launching combat. Therefore the task cannot claim that the direct production progress-update/save methods were routed to the memory manager/store; those methods were never called.

This failure is not evidence of an original-store leak, mutation mismatch, payload mismatch, natural-victory failure, achievement behavior, replay fidelity, reconstruction, or complete session isolation. The protected profile1 Continue sentinel, full-unlock progress files, settings identity, and inventories remained unchanged. Per the handoff's first-failure stop rule, `phase-control.json` is now `STOPPED`; the probe must not be patched and retried under SPIKE-INV004A.
