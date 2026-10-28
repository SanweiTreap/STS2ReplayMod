# INV-006 — cloud barrier and scope-safe audit contract

Status: `COMPLETE`

Decision: **`READY_SCOPE_SAFE_TIMING`**

> Project Lead acceptance amendment, 2026-09-13: the recommendation is accepted with one normative clarification to `scope-safe-audit-contract.md`. Ordinary artifact-request callbacks must fast-return after seal, but the terminal CAS winner must receive an opaque winner ticket that authorizes exactly one write of the already-closed snapshot. Without this exception, the original blanket first-instruction rule for “artifact helper” contradicted the required post-seal artifact write. No runtime evidence or cloud-barrier conclusion changed.

## Scope and identity

This was a static-only investigation. No game process or probe was run; no save/profile/cloud data was opened, enumerated, copied, hashed, or modified; and the restricted SPIKE-INV005A runtime artifact was not opened or processed. Runtime facts come only from the accepted report.

The inspected assembly exactly matched public-beta v0.111.0 / commit `41cef1ea`: 9,757,184 bytes, SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`. The hash-gated query and complete narrow textual output are in `evidence/inv-006/`.

## Executive result

Production's cloud admission rule is narrower and more tolerant than SPIKE-INV005A assumed. `GameStartup` stores the `Task` returned by `DoCloudSync`, polls only `IsCompleted`, and never retrieves its result. Therefore completed, faulted, and canceled are all accepted. If Steam becomes uninitialized during the loop, production logs and proceeds even before the task is terminal.

Within `DoCloudSync`, the manager-wide overwrite/sync task awaits batches of per-file cloud-wrapper tasks. Expected Steam remote write exceptions are caught in `CloudSaveStore`, logged with local preservation, and converted into successful per-file completion. A Steam leaf can therefore fault beneath a successful manager task and successful `DoCloudSync`. Requiring every observed leaf to succeed was not production-aligned.

`GameStartupComplete` is not the cloud barrier. Its wrapper sets the completion source on normal startup and in the startup exception path. It also cannot distinguish the Steam-uninitialized early escape. A future observer can instead patch the zero-argument `NGame.DoCloudSync`, capture its single returned task without reading any arguments, and require that task to be terminal before claiming cloud work settled. No global store patch, store batch observation, leaf observation, path access, or payload access is necessary.

## A. Production cloud barrier

### A1. Creation, storage, polling, and status handling

**Confirmed.** `NGame.GameStartup.MoveNext` (`0x0600b59f`) calls `NGame.DoCloudSync` (`0x06005ab2`) at `IL_00b5` and stores the result in `<cloudSavesTask>5__2`. At `IL_0286–0291` it polls `Task.IsCompleted`; it does not call `GetAwaiter`, `GetResult`, inspect `Status`, or inspect `Exception` for this task.

- Completed successfully: production proceeds.
- Faulted: `IsCompleted` is true, so production proceeds without observing the fault.
- Canceled: `IsCompleted` is true, so production proceeds without observing cancellation.
- Still pending while `SteamInitializer.Initialized` becomes false: production logs an error and proceeds early.

`DoCloudSync.MoveNext` (`0x0600b59d`) itself truly awaits the selected `SaveManager` task, so uncaught manager-task fault/cancellation propagates to the outer task. Production's caller simply does not retrieve it.

### A2. Direction and fan-out

**Confirmed.** `ShouldOverwriteCloudWithLocal` (`0x06000750`) selects overwrite immediately when the user Cloud setting is false. When enabled, it selects overwrite only for the initial-local-data/no-remote-data condition; otherwise it selects cloud-to-local sync.

Both manager directions build a static task set consisting of the account selector plus per-profile categories for each fixed profile slot: progress, run variants, prefs, and history-directory entries. The manager state machines (`0x060096db`, `0x060096df`) group these tasks eight at a time and await `Task.WhenAll`. This describes fixed call structure only; no user file list or other-profile runtime data was inspected or derived.

### A3. Catch/log/convert boundary

**Confirmed.** For local-to-cloud work, `CloudSaveStore.OverwriteCloudWithLocal.MoveNext` (`0x06009086`) awaits the Steam remote leaf inside a catch region. On exception it logs a cloud-write warning, states that local content is preserved, calls its guarded timestamp sync, and then completes. The normal async `CloudSaveStore.WriteFileAsync` state machines (`0x0600909d`, `0x0600909f`) use the same local-first, caught-remote pattern.

For cloud-to-local work, `CloudSaveStore.SyncCloudToLocal.MoveNext` (`0x06009091`) catches errors from the internal per-file sync, logs/skips, optionally reports, and completes. Consequently, expected remote leaf failures do not reach manager `Task.WhenAll`; the manager task and `DoCloudSync` can reach `RanToCompletion`.

`SteamRemoteSaveStore.WriteFileAsync` state machine (`0x060099b2`) canonicalizes its path, calls `SteamRemoteStorage.FileWriteAsync`, throws on an invalid call handle, awaits the call result otherwise, and throws when the result is not OK. Those throws fault/cancel the leaf task before the cloud-wrapper catch converts them.

### A4. Disabled/unavailable Steam semantics

**Confirmed from game IL:** a remote store is constructed whenever Steam was initialized when `SaveManager` was constructed. User-disabled Cloud makes `ShouldOverwriteCloudWithLocal` choose overwrite; it does not skip remote calls. There is no second availability check before each remote method. If Steam later becomes uninitialized, the startup loop can abandon its wait.

**Confirmed from Valve's public contract:** Steamworks interfaces require successful initialization; account-wide and per-app Cloud settings are separate; `FileWriteAsync` returns an invalid handle for documented request failures and reports completion through a result code. Sources were accessed 2026-09-13: [ISteamRemoteStorage](https://partner.steamgames.com/doc/api/isteamremotestorage?l=english) and [Steamworks API Overview](https://partner.steamgames.com/doc/sdk/api?l=english). Valve's live pages state no document revision or interface-version identifier, recorded as “not stated.” The bundled managed binding identifies itself as Steamworks.NET product build `1.0.0+c49be60d78117d599e17f278ab752151e27863d1`.

**Unknown:** no unique static or official-contract inference ties any particular invalid handle to Cloud disabled, client closure, remote-service availability, quota, or another documented failure. The accepted runtime fact establishes an invalid remote call in that run, not its unique external cause.

### A5. Minimal barrier choice

For “startup cloud work has settled,” observe the captured outer `NGame.DoCloudSync` task and accept any terminal status as settled while recording the status enum. Do not require internal tolerant leaves to succeed. `GameStartupComplete` remains a later lifecycle marker; it is not a substitute. A store batch is unrelated to startup cloud sync, and individual per-file/leaf tasks are below the production acceptance layer.

If Steam becomes uninitialized and production proceeds while the outer task remains pending, a future observer must seal as missing outer terminal and must not claim quiescence. This is the only fixed-code branch where production admission is weaker than proof of settled work.

No global store patch is required. It should be deleted from the next probe's instrumentation design.

## B. Profile-scope-safe observation contract

The normative, handoff-ready contract is `evidence/inv-006/scope-safe-audit-contract.md`. Its controlling rules are:

1. sealed/enabled checks are the first observer action in every callback and continuation, before `__args`, path conversion, payload access, length/hash, sequence allocation, timestamps, exception formatting, or `CallState` allocation;
2. path-bearing patches are unnecessary for the minimal probe; if later retained, they lexically authorize only exact `modded/profile.save`, exact/rooted-below `modded/profile1`, or exact `settings.save`, with ordinal segment boundaries and fail-closed rejection of traversal, roots, URI forms, percent encodings, ambiguous separators, and case variants;
3. a rejected first path is never persisted, and the second argument is never accessed;
4. payload access and hashing are forbidden even for allowed paths because timing acceptance does not depend on payload;
5. out-of-scope invocations are completely ignored; even a global boolean is unnecessary and would be derived information with no acceptance value;
6. exactly one terminal cause wins a CAS; event-lock rechecks close the set before artifact generation; callbacks and already registered continuations fast-return after seal;
7. artifact fields are an explicit enum/identity/timing whitelist with no path, payload, length/hash, inventory, exception text, Steam/user identity, or profile statistics;
8. artifact failure, event/terminal races, registration races, and patch exceptions remain sealed and never resume or broaden capture;
9. fourteen original synthetic negative/race cases cover other profile input, pseudo-prefixes, traversal, rooted/URI/encoded paths, separator/case ambiguity, post-seal callbacks, poison payloads, atomic races, finalizer semantics, and artifact failure; the Project Lead amendment adds a fifteenth unauthorized-artifact-call case for the winner-ticket rule.

The contract avoids reparse risk by performing no physical file I/O. Any future proposal to open an allowed file requires a separate review with reparse rejection and final-handle containment; string canonicalization alone is insufficient.

## C. Precise old-probe failure analysis

The minimal causal chain is fully visible in the retired source:

1. `WriterAudit.StoreEntry` calls `SummarizeArguments` at `WriterAudit.cs:68`. It then allocates and fills `CallState` at lines 69–73. Only at line 74 does it test `_enabled`.
2. `SummarizeArguments` reads/sanitizes the first argument at lines 116–118 and reads the second argument to compute byte/string length and SHA-256 at lines 119–123. Thus disabling audit never prevented argument/payload processing.
3. The async continuation records a terminal leaf fault and calls `ProbeController.AsyncTaskTerminalObserved` (`WriterAudit.cs:95–101`). That method only latches `GateFailures` (`ProbeController.cs:132–135`).
4. `CheckContinuousSafety` does not call `Finish` for a pre-gate failure until `_startupTerminalRecorded` and a graphics normal exit are both present (`ProbeController.cs:191–192`). Audit therefore continued after the leaf fault was known.
5. `Finish` sets `_classification`, then records another milestone, disposes watchers, and only then calls `WriterAudit.Stop()` (`ProbeController.cs:400–412`). Even after `Stop`, every later store prefix still enters `StoreEntry` and runs `SummarizeArguments` before seeing `_enabled == false`. Concurrent prefixes that began earlier also have no seal recheck.
6. The emitted classifier knew about async leaf faults and therefore wrote `FAIL_GATE`. It had no pre-argument scope authorization and no terminal rule for observer access to out-of-scope profiles/data. Project review correctly applied `FAIL_SAFETY`; a machine classification that cannot detect its own unauthorized observation cannot supersede the governing safety boundary.

Moving the `_enabled` line alone would not fix the design. A new probe needs first-instruction checks at every entry point, lexical authorization before state allocation, zero payload access, immediate single-winner sealing, an event-lock closed set, continuation rechecks, a strict artifact whitelist, safe exception/finalizer behavior, and removal of the unnecessary global store patch. It must use a new ID, directory, and schema.

## Final recommendation

The production-aligned outer barrier is observable without touching any profile path or payload, and the audit contract can reject unauthorized inputs before argument-derived processing. Project Lead may issue a wholly new timing-probe handoff under a new ID/directory/schema, limited to the captured `DoCloudSync` task plus lifecycle events and the contract above. This does not authorize a retry or modification of SPIKE-INV005A and does not unlock live-seeded direct-write by itself.
