# INV-005 — SaveManager startup mutation and save scheduling

Status: `COMPLETE`

Decision: **`REVISE_WITH_TIMING_GATE`**

## Scope and identity

This is a bounded static investigation only. No game process was started; no profile, save, cloud, or replay data was read, copied, or modified. The inspected production assembly exactly matched public-beta v0.111.0 / commit `41cef1ea`: 9,757,184 bytes, SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`.

Reproducible query, textual IL result, and standalone tables are in `evidence/inv-005/`. Every critical assembly conclusion below is backed by a method token and caller/callee direction in `il-query-output.txt`.

## Executive result

The three component initialization and normal save methods are synchronous. Startup cloud synchronization is genuinely asynchronous, but its internal writes are awaited and `GameStartup` does not initialize profile/progress/prefs until that task reports complete. `LoadMainMenu` separately awaits the tracked `CurrentRunSaveTask` before creating the menu. No confirmed Settings/Prefs/Progress writer task remains pending at that point.

However, the existing mod entry is attached by a Harmony postfix on `NMainMenu._Ready`, before `GameStartup` finishes and before `NGame.GameStartupComplete` is signalled. Earlier in startup, production queues `InitializeGraphicsPreferences` through `Callable.CallDeferred`. That callback can conditionally mutate live `SettingsSave`, performs no immediate save, and has no task handle, completion signal, pending counter, or flush API. Static IL does not prove the Godot queue has drained before the peer entry becomes usable.

Consequently, live-seeded construction remains viable, and there is no evidence requiring the transaction candidate to stop, but the current entry is not a statically provable quiescent capture boundary. The next experiment must first establish an explicit timing gate for this callback in a narrower run that installs no manager override and calls no progress method.

## A. Production startup sequence

### A1. Real manager construction

**Confirmed.** The first `SaveManager.Instance` access lazily calls `SaveManager.ConstructDefault` (`0x06000732`). In non-test production it creates:

- a local `GodotFileIo` rooted at `UserDataPathProvider.GetAccountScopedBasePath(...)`;
- when Steam is initialized, a `SteamRemoteSaveStore` wrapped with that local store in `CloudSaveStore`; otherwise the local store alone;
- `new SaveManager(profileStore, settingsStore: localGodotFileIo, forceSynchronous: false)`.

Thus profile/prefs/progress/run/history use the cloud wrapper when Steam is initialized, while Settings always uses the separate local store. Profile id is not supplied to the constructor. Later `GameStartup` calls `InitProfileId(null)`, causing `ProfileSave.LastProfileId` to select the active id.

`SaveManager::.ctor(ISaveStore, ISaveStore, bool)` (`0x06000731`) captures the stores in child managers. The `false` synchronous option is passed specifically to `RunSaveManager`; it does not make Settings/Prefs/Progress saves asynchronous.

### A2. Exact init order and awaits

**Confirmed.** The production order is not the order previously summarized in SPIKE-INV004B Phase 0. It is:

1. `GameStartup` awaits `OneTimeInitialization.ExecuteVeryEarly`.
2. `ExecuteVeryEarly.MoveNext` (`0x0600afab`) synchronously calls `InitSettingsData` (`0x06000746`), then awaits `ModManager.Initialize`.
3. `GameStartup` starts and later gates asynchronous cloud synchronization.
4. After `cloudSavesTask.IsCompleted`, it synchronously calls `InitProfileId(null)` (`0x06000733`).
5. It synchronously calls `InitProgressData` (`0x06000748`).
6. It synchronously calls `InitPrefsData` (`0x06000747`).

The four `Init*` methods return `void` or `ReadSaveResult<T>`, not `Task`; there is nothing to await per component. All component loading/default/migration work happens inline. The enclosing startup does await mod initialization and cloud sync work. This corrects the prior “profile → settings → prefs → progress” statement for production startup without changing the candidate initializer order used in earlier probes.

### A3. Main-menu creation and peer callback

**Confirmed.** After profile/progress/prefs initialization and the startup mutations below, `GameStartup.MoveNext` (`0x0600b59f`) awaits `LaunchMainMenu`. `LaunchMainMenu.MoveNext` (`0x0600b5ab`) awaits `LoadMainMenu(false)`. `LoadMainMenu.MoveNext` (`0x0600b5af`) first awaits any non-null `SaveManager.CurrentRunSaveTask`, then calls `NMainMenu.Create` (`0x06007474`) and installs the scene with `NSceneContainer.SetCurrentScene`.

`NMainMenu._Ready` (`0x06007475`) reads Settings and Progress to initialize UI. It does not mutate the three save components. SPIKE-INV004B's own source patches this method with a synchronous postfix, `MainMenuReadyPatch.Postfix → ProbeController.ObserveMainMenu → ProbeUi.Attach`; therefore the peer entry becomes attachable immediately after the production `_Ready` body, while the enclosing `LaunchMainMenu`/`GameStartup` call stack has not yet returned.

After `LaunchMainMenu` returns, `GameStartup` still runs three popup checks and registers `ModManager.OnModDetected`; those bounded bodies do not mutate the components. Only after `GameStartup` returns does `GameStartupWrapper.MoveNext` (`0x0600b5a3`) complete `_gameStartupComplete`, exposed as `NGame.GameStartupComplete`.

### A4. Ready/quiescent gates

- **Confirmed:** `NGame.GameStartupComplete` is a stronger observable task than menu visibility or the `_Ready` postfix.
- **Confirmed:** `SaveManager.CurrentRunSaveTask` is observable but covers only run saving; `LoadMainMenu` already awaits it.
- **Confirmed:** startup's cloud task is locally held and gated before component initialization, but is not exposed as a reusable global pending-save set.
- **Unknown:** a deterministic public barrier for the `Callable.CallDeferred(InitializeGraphicsPreferences)` callback. No task/signal/counter/flush was found in the closed call set.
- **Unknown:** a general “all component writers idle” API. No such pending counter/task set exists in the narrowed save namespace; quiet frames are not a proven substitute.

## B. Startup-to-entry live mutations

### SettingsSave

**Confirmed reachable candidates:**

1. `SettingsSaveManager.LoadSettings` (`0x06000aae`) deserializes Settings. On missing/failed load it creates defaults, applies Steam Deck platform defaults, and immediately saves synchronously.
2. `ExecuteEssential` (`0x06004d5f`) constructs `LocManager` (`0x06004aac`). If saved language is empty it selects platform language or English; if unsupported it replaces it with English. Both paths set live `SettingsSave.Language` and do **not** immediately save. This is synchronous and completes before profile initialization.
3. `GameStartup` queues `InitializeGraphicsPreferences` (`0x06005ab7`) with `CallDeferred`. Its `ApplyDisplaySettings` (`0x06005ab8`) can repair a first-time target display, force platform-compatible fullscreen, and replace invalid/missing-display window positions. It mutates live Settings without calling `SaveSettings`. This is a deferred engine callback with no awaitable completion object.

These branches can make live Settings newer than disk. They are candidates for a session-dependent Candidate A mismatch, not a claim about the unrecorded historical field.

### PrefsSave

**Confirmed reachable candidate:** after `InitPrefsData`, `GameStartup.MoveNext` IL `0307–0330` checks for a non-editor process and changes `PrefsSave.FastMode` from enum value `3` to `2`. It does not save immediately. A later Settings close or normal exit saves Prefs. This exact one-time normalization can produce “one session differs, a later session equals”: the first main-menu snapshot sees the changed live value while disk still has `3`; normal quit persists `2`; a later startup no longer changes it.

This is the strongest method-body candidate for the reported cross-session shape, but **the historical root cause remains Unknown** because SPIKE-INV004A did not record the differing component/path.

### ProgressState

**Confirmed:** `ProgressSaveManager.LoadProgress` (`0x06000a70`) either creates defaults, marks Ironclad starter cards/relics seen, and immediately saves; or calls `ProgressState.FromSerializable`, which validates/normalizes the loaded representation into a new live state. In the closed forward startup/menu set, no production method mutates Progress after this load and before the peer entry. `NMainMenu._Ready` only reads epochs/discovered counts.

**Inferred:** deserialization normalization can explain live-versus-raw-disk representation differences in general, but Candidate A repeats the same loader and normalization, so it is not by itself an explanation for a live-versus-Candidate-A mismatch on stable disk input.

### Mod and frame-driven boundaries

**Confirmed call / Unknown effects:** `ModManager.Initialize` receives the live `SettingsSave.ModSettings` and is awaited during very-early startup. The fixed production assembly cannot bound arbitrary external mod entry-point behavior. The investigated probe's own menu callback is synchronous and only attaches UI. No production frame/update-driven Settings/Prefs/Progress mutation was found in the closed set; the one relevant queued path is `CallDeferred` graphics initialization.

## C. Save/write scheduling and pending work

### Component APIs

**Confirmed:** all six public/child component saves are synchronous `void`:

- `SaveManager.SaveSettings` (`0x06000742`) → `SettingsSaveManager.SaveSettings` (`0x06000aad`)
- `SaveManager.SavePrefsFile` (`0x0600075f`) → `PrefsSaveManager.SavePrefs` (`0x06000a62`)
- `SaveManager.SaveProgressFile` (`0x0600075d`) → `ProgressSaveManager.SaveProgress` (`0x06000a6f`)

Each serializes inline and calls `ISaveStore.WriteFile`, also `void`. `GodotFileIo.WriteFile` (`0x06000498`) synchronously copies the prior primary to `.backup.tmp`, fsyncs and renames it to `.backup`, writes the new payload to `.tmp`, fsyncs, and renames it over the primary. There is no task to discard or continue for these three paths.

`WriteFileAsync` returns `Task`, but reverse callers show it is used by run saving and cloud synchronization. `CloudSaveStore.WriteFileAsync` awaits local then remote writes. The normal component save methods call the synchronous `CloudSaveStore.WriteFile` instead.

### Startup callers and captured stores

**Confirmed:** `DoCloudSync` is started before profile initialization. Its state machines capture the original manager/cloud/local stores and could write while running, but `DoCloudSync` awaits all selected sync operations, and `GameStartup` loops until that task is completed before calling `InitProfileId`, Progress, or Prefs. Therefore no confirmed startup cloud writer remains after that gate.

**Confirmed:** async run saves are tracked by `SaveManager.CurrentRunSaveTask`; `LoadMainMenu` awaits the task before menu creation. Such a state machine captures its owning manager/child store, so in general it could complete after a global override if the caller bypassed the menu barrier. The production startup/menu path does not bypass it.

**Unknown:** there is no universal global task registry covering arbitrary mod work or engine-deferred callbacks. Static evidence cannot prove that every external callback is idle.

### Override and teardown risk

- Before override: no confirmed Settings/Prefs/Progress write task can cross the proposed main-menu boundary, because their saves are synchronous; cloud and tracked run save have production gates. The unresolved graphics callback mutates `SaveManager.Instance` at execution time rather than capturing a manager/store in its delegate, so if delayed past override it could mutate the isolated manager. This is still a transaction correctness risk even though it is not an original-store writer.
- After restoring the original manager: component sentinel saves are synchronous, so they cannot outlive teardown. An isolated `RunSaveManager.SaveRun` task, if any session path starts one, captures the isolated manager/store and can outlive restoration because subsequent `LoadMainMenu` reads `CurrentRunSaveTask` from the restored original manager, not from the isolated manager. The direct-write sentinel should therefore assert the isolated manager's `CurrentRunSaveTask` is null/completed before teardown even though the proposed sentinel does not intentionally invoke run saving.
- Deterministic barrier: `GameStartupComplete` + observed graphics-callback completion + original `CurrentRunSaveTask` null/completed is the strongest candidate. The second term is not presently exposed; hence static result **Unknown**, and no quiet-frame number is recommended.

## D. Normal-exit mtime changes

**Confirmed chain:** window close notification `1006` or menu confirm → `NGame.Quit` (`0x06005abe`) → optional Settings window size/display/position mutation → synchronous `SaveSettings` → if profile initialized, synchronous `SavePrefsFile`, `SaveProgressFile`, and `SaveProfile` → `SceneTree.Quit`.

The observed hashes remaining identical does not mean no write occurred:

- Settings uses the separate local `GodotFileIo`; temp-write + rename replaces `settings.save`, so identical content receives a new primary mtime.
- Prefs and Progress use `CloudSaveStore`. Their local `GodotFileIo` first overwrites `.backup` with a copy of the old primary, so an unchanged primary yields an unchanged backup hash but a new backup mtime. After primary and remote writes, `CloudSaveStore.SyncLocalTimestamp` (`0x0600045e`) sets the local primary mtime to the remote store's reported timestamp. This explains the exact shape “backup mtime advanced while primary mtime remained recorded.”

This is sufficient baseline production behavior; no mod unload behavior is required. From the fixed assembly alone, additional Harmony behavior cannot be excluded, so attribution of the real run is **Inferred from an exact production mechanism**, not a dynamic proof.

For the next runtime control, record primary and backup hash/length/mtime at: process start, after `GameStartupComplete`, after observed graphics callback, immediately before candidate snapshot, immediately after candidate-only work, after teardown, and after normal exit. Audit `ISaveStore.WriteFile/WriteFileAsync/RenameFile/DeleteFile/SetLastModifiedTime` with manager/store identity and operation owner. A no-override/no-progress timing run must establish the baseline first; only a later separately approved sentinel may compare transaction-window events against it.

## E. One decision

### `REVISE_WITH_TIMING_GATE`

Why this choice:

- Method-body evidence establishes real, condition-dependent live/disk drift paths: localization and deferred display repair for Settings, plus non-editor `FastMode 3→2` for Prefs.
- All component save paths are synchronous, startup cloud work is gated, and current-run save is awaited before menu creation. There is no confirmed unbounded real-store component writer requiring the transaction candidate to stop.
- The current `_Ready` postfix exposes the peer entry before `GameStartupComplete`, and static IL cannot prove the handle-less graphics `CallDeferred` has run. Therefore a quiescent snapshot boundary is not yet proven.

The single next experiment should be a main-menu-only timing/barrier probe that installs no `SaveManager` override, calls no progress mutation/save method, and starts no run. It should await `NGame.GameStartupComplete`, instrument entry/exit of `InitializeGraphicsPreferences`, audit all relevant store operations with original manager/store identity, and take component hashes at the explicit milestones above. It must stop if the graphics callback is not observed or if any writer remains active; it must not replace that observation with a fixed number of quiet frames.

Why the other choices do not apply:

- Not `GO_LIVE_SEEDED_SENTINEL`: there is no static proof that the queued graphics mutation has completed when the existing peer entry becomes usable, nor a built-in all-writers barrier.
- Not `STOP_TRANSACTION_CANDIDATE`: every discovered production component writer is either synchronous or bounded by a production task gate before menu creation. The remaining uncertainty is observable with one narrow timing probe and does not establish an uncontrollable original-store writer.

Remaining Unknowns are limited to runtime ordering/observation of the engine-deferred graphics callback, arbitrary external mod behavior outside the fixed assembly, and whether an isolated replay session unexpectedly starts a run-save task. No historical JSON field is claimed as identified.
