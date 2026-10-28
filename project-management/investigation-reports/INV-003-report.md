# INV-003 — 主菜单独立 Replay 会话与 RitsuLib 适用性调查

状态：Complete（只读调查）  
调查日期：2026-09-04  
游戏目标：public-beta `v0.111.0` / commit `41cef1ea`  
游戏 DLL SHA-256：`0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`

## 0. 结论先行

**A — 主菜单：可行，但没有现成的“并列 Replay 页面”注册 API。** `NMainMenu` 的按钮集合、节点路径、signal 和 submenu 分派都是固定实现。真正与单人/多人并列的入口需要对 `_Ready`/菜单节点树做 Godot 注入，并用 Harmony 或等效 hook 保持初始化、显隐和 focus 行为。Replay 管理页本身可以只是主菜单下的普通 UI，不建立 `RunManager.State`；只有点击“播放”后才进入 run 生命周期。`[Confirmed method body/call site]`

**B — playback session：部分底座存在，但不是 production-ready 的任意单人 Replay API。** `RunManager.SetUpReplay` 不执行 `IncrementNumReloads`，且 `NetReplayGameService` 不发送网络消息；然而唯一 DLL caller 是 debug `NMultiplayerTest.RunReplay`，输入是 `CombatReplay`，并非任意 singleplayer Replay baseline。更重要的是它把 `InitializeShared(... shouldSave: true ...)` 写死为 `true`。即使随后改成 `ShouldSave=false`，combat victory 仍会直接调用 progress 更新与 `SaveProgressFile`。`[Confirmed method body/call site]`

**B — 最值得首先验证的唯一隔离机制：事务式 Replay save context。** 在进入 playback 前暂时把全局 `SaveManager.Instance` 切到一个完整、深拷贝初始化、仅内存的 manager，并在 `SetUpReplay` 后立刻强制 `RunManager.ShouldSave=false`；所有退出/异常路径等待隔离 manager 的任务、`RunManager.CleanUp`、恢复原 manager，最后才创建新主菜单。这里的“内存 manager + ShouldSave=false + finally 恢复”是一个不可拆分的事务机制：前者接住绕过 `ShouldSave` 的直接进度写，后者阻止 run-end metrics/history/stats 等非 store side channel。`[Inferred from confirmed method bodies]`

**C — RitsuLib：本次不建议成为 MVP 的必要运行依赖。** 固定的 `v0.5.18` 明确支持 API `0.111.0`，其 lifecycle、patch diagnostics、settings、UI、localization 都有价值；但它没有通用顶级主菜单 peer-entry API，`SaveScope.InMemory` 只隔离 RitsuLib 自己的 mod data，不能接管游戏 `SaveManager`。它不解决核心 session 隔离，而引入一个玩家必须额外安装的 runtime mod。下一实验直接使用游戏 API/Harmony；待隔离成立后，再单独评估是否为外围 UI/设置/诊断引入 RitsuLib。`[Confirmed upstream API/source + Inferred recommendation]`

**SPIKE-001A2：Retain + revise。** 保留为独立 reconstruction control，继续暂停；不并入下一次 session-isolation 实验，也不取消。其 room-before A/B 能回答重建语义，却不能证明普通 Continue 隔离，合并会让失败归因变得不清楚。`[Inferred]`

本调查没有启动游戏、运行 probe/save/replay、安装 RitsuLib 或实现任何候选。

## 1. 证据等级与固定身份

本文使用以下标签：

- `[Confirmed method body/call site]`：固定 hash 的 `sts2.dll` Cecil 方法体/调用点。
- `[Confirmed upstream API/source]`：固定 RitsuLib tag/commit 的源码或 release metadata。
- `[Documented claim only]`：上游 README/文档声明，未由本任务运行验证。
- `[Inferred]`：由已确认事实得到的设计/风险推论。
- `[Unknown]`：静态调查无法闭合，必须由后续运行实验回答。

本机 DLL 身份复核为 9,757,184 bytes，ProductVersion `0.1.0+41cef1ea4657c524aa50e870df009e56337e8c32`，SHA-256 与任务固定值一致。因此下述游戏方法体结论仅适用于该 hash。复现命令与固定上游索引见 `evidence/inv-003/README.md`。

### RitsuLib 固定对象

| 项 | 固定值 | 证据等级 |
|---|---|---|
| 仓库 | `https://github.com/BAKAOLC/STS2-RitsuLib` | Confirmed upstream API/source |
| release/tag | `v0.5.18`，2026-08-28 发布 | Confirmed upstream API/source |
| annotated tag / commit | `1be7d64...` / `f224961a9392e010335da092240b90ee8235317f` | Confirmed upstream API/source |
| 源码归档 | `references/STS2-RitsuLib-v0.5.18.zip`；SHA-256 `E6B599CA...FD7DF9D3` | Confirmed upstream API/source |
| manifest | id `STS2-RitsuLib`；name `RitsuLib`；version `0.5.18`；`has_dll:true`；`has_pck:false`；自身 dependencies 为空 | Confirmed upstream API/source |
| NuGet | `STS2.RitsuLib` `0.5.18`（当前最高 API）；旧 API 使用 `STS2.RitsuLib.Compat.<api>` | Confirmed upstream API/source |
| STS2 API targets | `0.111.0;0.110.0;0.109.0;0.107.1`；latest=`0.111.0` | Confirmed upstream API/source |
| v0.111 artifact | 主 `STS2.RitsuLib.0.5.18.github.zip` / `.nupkg`；不是旧版 compat 包 | Confirmed upstream API/source |
| license | MIT；许可证副本已保存 | Confirmed upstream API/source |

`v0.5.12` release note 首次明确写入 STS2 `0.111.0` compatibility；`v0.5.18` 的 csproj 继续把 `0.111.0` 设为 latest target。没有只凭滚动 `main` 推断兼容。`[Confirmed upstream API/source]`

玩家最小安装成本是额外安装 `STS2-RitsuLib` runtime mod；Replay Mod 若编译链接其 public API，manifest 也必须声明 `{ "id": "STS2-RitsuLib" }`。主构建为 DLL-only；variant pack 则是一份 loader DLL 加 `lib/<api-version>/` 多分支目录。release 同时提供主 GitHub zip、NuGet、旧 API compat 和约 19.7 MB variant pack。`[Confirmed upstream API/source + Documented claim only for install guidance]`

## 2. A：主菜单与 scene 入口

### 2.1 方法与职责

| 责任 | v0.111 类型/方法 | 结论 |
|---|---|---|
| 顶级按钮初始化 | `NMainMenu._Ready` | 按固定节点路径取得按钮、连接 signals、初始化 `SubmenuStack`，最后 `RefreshButtons`。`[Confirmed method body/call site]` |
| 顶级按钮集合 | `NMainMenu.get_MainMenuButtons` | 返回固定 8 项：Continue、Abandon、Singleplayer、Multiplayer、Timeline、Settings、Compendium、Quit。`[Confirmed method body/call site]` |
| 单人/多人入口 | `OpenSingleplayerSubmenu` / `OpenMultiplayerSubmenu` | 保存 `_lastHitButton`，再 `PushSubmenuType<T>()`。`[Confirmed method body/call site]` |
| submenu 实例 | `NMainMenuSubmenuStack.GetSubmenuType(Type)` | 对游戏内已知类型做硬编码分派、创建并挂入节点树；没有通用注册表。`[Confirmed method body/call site]` |
| Continue 缓存 | `NMainMenu.RefreshButtons` | `HasRunSave` 时 `LoadRunSave`，结果缓存到 `_readRunSaveResult`；切换 Continue/Abandon 与 Singleplayer 显隐。`[Confirmed method body/call site]` |
| Continue 点击 | `OnContinueButtonPressed` | 消费上述缓存结果，继而进入 `RunState.FromSerializable` / saved-run setup。`[Confirmed method body/call site]` |
| scene 替换 | `NSceneContainer.SetCurrentScene` | 移除并 queue-free 旧 scene，再添加新 scene。`[Confirmed method body/call site]` |
| 回主菜单 | `NGame.ReturnToMainMenu` | fade → preload common/main-menu assets → `RunManager.CleanUp(true)` → `LoadMainMenu(false)`。`[Confirmed method body/call site]` |
| 新菜单加载 | `NGame.LoadMainMenu` | 等待当前 `SaveManager.CurrentRunSaveTask`，创建 `NMainMenu`，清 hotkeys，交给 scene container。`[Confirmed method body/call site]` |

### 2.2 入口候选表

| 候选 | 是否满足“与单人/多人并列” | 所需机制 | 结论 |
|---|---:|---|---|
| 注入 `MainMenuTextButtons` peer | 是 | hook `NMainMenu._Ready`，创建 Godot button、连接 signal，补显隐/focus/layout | 首选；仍是 fragile patch surface。`[Inferred from confirmed body]` |
| 自定义 `NSubmenu` | 入口可满足，页面可复用 native stack | 上述按钮注入 + patch `NMainMenuSubmenuStack.GetSubmenuType` | 可行但多一个硬编码分派 patch。`[Inferred]` |
| 独立 Control/scene child | 是 | 上述按钮注入；页面自己处理显隐、focus/back，不创建 run | **管理页首选**，少 patch 一个 type dispatcher。`[Inferred]` |
| RitsuLib settings shortcut | 否 | 它把 64px gear shortcut 放在 Patch Notes 下方并打开 mod settings | 明确不是 peer entry。`[Confirmed upstream API/source]` |
| RitsuLib top-bar button | 否 | 注入 `NRun.GlobalUi.TopBar` | 只在 run 顶栏，不是主菜单。`[Confirmed upstream API/source]` |
| RitsuLib runtime UI/overlay | 否（可作为页面素材） | overlay/window/shell primitives | 能承载 UI，不提供产品要求的入口层级。`[Confirmed upstream API/source]` |

RitsuLib 的 `MainMenuReadyEvent` 只是把 `NMainMenu._Ready` postfix 延迟发布；它能提供较干净的注入时机，但不替 Replay Mod 注册 peer button。RitsuLib 自己的 settings submenu 也正是通过 Harmony prefix 扩展 `GetSubmenuType`，反证该处没有通用原生注册 API。`[Confirmed upstream API/source]`

### 2.3 管理页与 teardown

打开列表、筛选、删除/导入确认等管理 UI 时保持 `RunManager.State == null`，只向当前 `NMainMenu` 节点树加普通 UI；关闭时断开 signals、queue-free 页面、恢复 `MainMenuTextButtons`/profile/patch-notes 显隐、focus owner、active screen context 与菜单音频。不应调用 `RunManager.CleanUp`，因为根本没有 run。`[Inferred]`

实际 playback 结束时则必须走完整 run teardown；若使用临时全局 save context，不能直接无拦截地调用 stock `ReturnToMainMenu`，因为恢复点必须位于 `CleanUp` 与 `LoadMainMenu` 之间。需要等隔离 save task → `CleanUp(true)` → 恢复原 `SaveManager` → `LoadMainMenu(false)`。新 `NMainMenu._Ready/RefreshButtons` 才会从真实磁盘重新装载普通 Continue。`[Inferred from confirmed order]`

## 3. B：独立 playback run/session

### 3.1 `SetUpReplay` 的真实边界

`RunManager.SetUpReplay(RunState, CombatReplay, ulong)`：

1. 要求当前 `RunManager.State` 为空，再设置传入 state；
2. 从 `CombatReplay.serializableRun` 取 baseline；
3. 创建 `NetReplayGameService`；
4. 调 `InitializeShared(..., shouldSave: true, DailyTime, StartTime, RunTime, WinTime, NumReloads)`；
5. 由 replay players 建 lobby，再 `InitializeSavedRun(serializableRun)`。

方法本身没有调用 `IncrementNumReloads`。`[Confirmed method body/call site]`

`NetReplayGameService` 报告 replay 类型、connected=true，send/register/update 是 no-op，因此没有实际多人网络发送。`[Confirmed method body]`

DLL 中唯一 `SetUpReplay` caller 是 debug `NMultiplayerTest.RunReplay`：它从 `CombatReplay` 创建 `RunState`，关掉 combat state synchronizer，preload/launch/create `NRun`，重建 map/room，再迭代 combat replay events。它只对 git commit 不同发 warning，不是具备严格兼容 gate、任意 singleplayer baseline、错误恢复和普通存档隔离的 production API。`[Confirmed method body/call site]`

**问题 B1 回答：** 不存在已确认 production-usable 的完整 replay/test/spectate session。存在一个有用的 replay setup primitive，且它绕过 reload increment，但输入/唯一 caller/默认保存语义都不足以直接作为产品入口。`[Confirmed + Inferred]`

### 3.2 生命周期图

```text
NMainMenu._Ready
  └─ RefreshButtons ── real SaveManager.LoadRunSave ── cache ordinary Continue
       │
       └─ injected Replay peer button
            └─ Replay management Control (RunManager.State remains null)
                 └─ Start playback transaction
                      ├─ capture original SaveManager and ordinary-file inventory
                      ├─ install fully initialized in-memory SaveManager override
                      ├─ RunState.FromSerializable(replay baseline)
                      ├─ RunManager.SetUpReplay(...)
                      ├─ immediately force ShouldSave=false
                      ├─ preload → RunManager.Launch → NRun scene → reconstruct
                      └─ watch / stop / failure
                           └─ one finally-owned teardown path
                                ├─ await isolated save tasks
                                ├─ RunManager.CleanUp(true) if State != null
                                ├─ clear global mock/restore original manager
                                └─ NGame.LoadMainMenu(false)
                                     └─ new _Ready/RefreshButtons reloads real Continue

Process crash/restart
  └─ static override disappears; real on-disk Continue remains authoritative
     (only if no ordinary/cloud/side-channel write escaped before crash)
```

The transaction shown is the experiment candidate, not an implemented architecture. `[Inferred]`

### 3.3 写入与 side-channel 矩阵

| 路径 | 行为 | `ShouldSave=false` | Replay net type | 隔离 store | 证据 |
|---|---|---:|---:|---:|---|
| `RunSaveManager.SaveRun(AbstractRoom)` | current-run live save | 阻止 | 无关 | 也可接住 | Confirmed method body/call site |
| normal saved-run setup `IncrementNumReloads` | 直接改 `SerializableRun` 并写回 | 不覆盖 | 无关 | 可接住，但 `SetUpReplay` 不调用它 | Confirmed method body/call site |
| `CombatManager.EndCombatInternal` → `SaveRun` | combat-end current-run save | 阻止 | 无关 | 可接住 | Confirmed method body/call site |
| 同方法 → `UpdateProgressAfterCombatWon` → `SaveProgressFile` | encounter/enemy fight stats、discovery、epochs/unlocks | **不阻止** | 无关 | **必须由隔离 manager 接住** | Confirmed method body/call site |
| 同方法 → achievement checks | achievement helper call | 不阻止 | 无关 | store 未必覆盖平台成就 | Confirmed call site |
| `RunManager.OnEnded` 内 progress/history/metrics/stats | progress、run history、mod metrics hooks、architect damage | 阻止整块 | 无关 | 辅助防线 | Confirmed method body/call site |
| `OnEnded` current-run delete | 删除 ordinary save | 由保存块 gate；且只对 net type 1/2 | replay type 4 不删 | 辅助防线 | Confirmed method body |
| `OnEnded` daily leaderboard | 上传 daily score | `DailyTime` 分支在 gate 外 | replay type 4 不上传 | 不足以覆盖网络 | Confirmed method body |
| `AchievementsHelper.*` / `AchievementsUtil.Unlock` | 理论成就 side channel | 不应依赖 gate | 无关 | 不保证平台隔离 | **本固定 DLL 方法体均为 `ret`**；仅 hash-bound confirmed |
| settings/profile/background save | session 内其他系统的全局 SaveManager 调用 | 不一定 | 无关 | 全局 override 会一并接住 | Inferred risk |
| Steam/cloud wrapper | default manager 可能在 initialized Steam 下包 CloudSaveStore | 不完整 | 无关 | 若所有写均走 override 则应隔离 | Confirmed constructor/source shape；运行覆盖 Unknown |

**问题 B2/B3 回答：** setup 自身不写 reload，但 room/combat/run end 都存在写入或提交路径。`ShouldSave=false` 能挡 live run save 和 `OnEnded` 中的大部分 progress/history/metrics/stats；挡不住 combat victory 的直接 `UpdateProgressAfterCombatWon`/`SaveProgressFile`，也不能被当作任意平台/第三方 side-channel 的通用沙箱。`[Confirmed]`

### 3.4 SaveManager/store 注入边界

`SaveManager` 构造器接收 `ISaveStore`；默认生产路径使用 Godot file I/O，并可能包 cloud store。`SaveManager.Instance` 优先返回静态 `_mockInstance`，`MockInstanceForTesting(manager)` 设置它，`ClearInstanceForTesting()` 清除它。`HasRunSave` 和 `CurrentRunSavePath` 每次从当前实例/store/profile-id provider 求值。`[Confirmed method body/call site]`

没有 per-run `SaveManager` 参数或 scope；游戏调用点普遍访问全局 `SaveManager.Instance`。因此：

- **问题 B4：否。** 未发现“不替换全局 manager/context、但只给该 Replay run 注入 store”的入口。`[Confirmed method/caller search]`
- RitsuLib `SaveScope.InMemory` 也不改变这一结论；它只是 `ModDataStore` 内一个 no-op-on-save 的 entry 类型。`[Confirmed upstream source]`

若临时替换全局 context，必须具备：

1. 在安装 override 前保存原 manager；隔离对象的 Settings/Prefs/Profile/Progress 等要由**序列化深拷贝**初始化，不能共享可变对象；
2. setup 后立即设 `ShouldSave=false`；任何中间异常都进入同一个 `finally`；
3. state 已赋值时必须 `CleanUp`，隔离 save task 必须先完成/取消并被观察；
4. `ClearInstanceForTesting`/恢复必须先于 `LoadMainMenu`，否则新菜单会缓存 fake store 的 Continue 状态；
5. 禁止并发/嵌套 replay transaction；全局 override 期间所有游戏 save consumer 都被重定向；
6. 记录原 manager 的 write/delete/cloud 调用；任一触发都 fail closed；
7. 最终断言 `RunManager.State == null`、原 manager 身份恢复、Continue 内容/hash/reload count 不变。

`[Inferred from confirmed lifecycle]`

仍可能泄漏的状态包括 `RunManager`/`LocalContext`/scene 单例、未取消 callback、第三方 mod lifecycle handler、platform/metrics side channel、共享对象引用，以及 override 安装前或恢复后的延迟任务。标准 `CleanUp` 负责大量 run service/synchronizer/reset 与 `State=null`，但不能证明第三方完全纯净。`[Confirmed method body + Inferred risk]`

### 3.5 ordinary Continue 的保留机制

在主菜单时，普通 run 不在 `RunManager.State` 中，只以磁盘 save 和当前 `NMainMenu` 的 `_readRunSaveResult` 缓存存在。因此在 `State == null` 时建立 replay state 是可行的；磁盘上同时存在普通 Continue 并不冲突。`[Confirmed method body + Inferred]`

真正冲突来自默认 `SetUpReplay` 的 `ShouldSave=true` 和后续保存路径，而不是“内存里已有正常 run”。事务隔离成立时，原 manager 及其普通 save 始终不被替换/消费；回菜单前恢复原 manager，新菜单的 `RefreshButtons` 会重新 `HasRunSave/LoadRunSave`。进程重启会自然清掉静态 mock，但只有在 crash 前没有外泄写入时才算安全。`[Inferred]`

**问题 B6：静态上可行，端到端仍为 Unknown，需下一实验。** 特别是异常退出、cloud 和延迟 callback 无法仅靠 IL 证明。`[Unknown]`

### 3.6 SPIKE-001A2

结论：**Retain + revise 为单独 reconstruction control；保持 paused。** 它以后只比较 room-before baseline 对房间重建的影响，不承担存档隔离证明；不作为下一实验的 phase，也不与 session setup、capture 或 dispatcher 混跑。`[Inferred]`

## 4. C：RitsuLib 逐能力适用性

| 能力 | RitsuLib 具体 API/实现 | v0.111 证据 | Replay Mod 候选用途 | 是否解决核心隔离 | 建议 |
|---|---|---|---|---:|---|
| 主菜单并列入口 | `MainMenuReadyEvent`；内部 settings shortcut patch | v0.5.18 main target=0.111；源码只发布 ready event/注入专用 gear | 提供注入时机，按钮仍需自建 | 否 | **Direct game API preferred** |
| Replay 管理/runtime UI | `Ui/Overlay`、shell/theme、window primitives | 固定源码编译到 0.111 main artifact | 页面 chrome、toast、诊断窗口 | 否 | **Adopt candidate**（隔离通过后、外围） |
| 生命周期事件 | `SubscribeLifecycle<TEvent>`；main-menu/run/room/combat patches | 固定源码 patches + contracts | 非热路径通知、teardown observation | 否 | **Adopt candidate**（不用于首个实验核心） |
| Harmony patch/IL 验证 | `CreatePatcher`、patch target/diagnostic registration | 固定源码 + README public entry | fail-closed target audit、patch diagnostics | 间接提高安全性，不提供 store | **Adopt candidate**（以后评估） |
| 全局设置与设置 UI | `RegisterModSettings`、settings submenu/shortcut | 固定源码；其 submenu 也 patch hardcoded dispatcher | auto-record 等用户设置 | 否 | **Adopt candidate**（外围） |
| `SaveScope.InMemory` | `ModDataStore.InMemoryDataEntry<T>`；`Save()` no-op | 固定实现 | 临时 ReplayMod 自有 UI state | **否，只隔离 RitsuLib mod data** | **Not applicable**（核心） |
| run-sidecar/persistence | `GetRunSavedDataStore`、profile/global JSON paths | 固定源码/README | 小型 mod-owned metadata；不适合可分享 replay 主文件 | 否 | **Not applicable**（核心 session） |
| diagnostics/compatibility gates | diagnostics、startup audits、API capability gate | 固定源码 | hash/API gate、清晰禁用原因 | 不直接 | **Adopt candidate** |
| localization/assets | localization registration、embedded PNG/resource helpers | 固定源码 | Replay 菜单文案/图标 | 否 | **Adopt candidate**（外围） |

### 4.1 patch 维护与开销

RitsuLib 能减少 settings、diagnostics、localization、通用 UI/lifecycle 的自维护代码，也能让 patch 失败更可诊断；但主菜单 peer injection、游戏 `SaveManager` transaction、`ShouldSave` safety gate 仍是 Replay Mod 自己的 v0.111-sensitive patch surface。因此它**不能显著消除本调查的核心 fragile patches**。`[Inferred]`

若在录制热路径订阅细粒度 lifecycle，额外开销类型包括每事件的 patch dispatch、delegate/event publication、可能的参数/record allocation、订阅者工作和异常/日志路径；若只用于主菜单、设置、启动审计和 run boundary，则不必进入 capture hot path。持久化还会带来 JSON 与文件 I/O，UI 则增加节点和每帧处理；本任务没有量化，具体幅度为 `[Unknown]`。

### 4.2 依赖与替代策略

- 链接 RitsuLib public API 时，它成为玩家必要 runtime dependency；缺失会由 mod dependency/load 关系阻止正常加载。`[Confirmed upstream documentation/source]`
- 不使用它时，替代是直接 Harmony patch 固定游戏方法、Godot node/resource API、项目自有 JSON/mod-owned directory、自己的 hash/target diagnostics。`[Inferred]`
- 对核心隔离，任何依赖缺失、API mismatch、patch target mismatch、override 安装失败都必须 **fail closed**：隐藏/禁用“播放”，保留管理/诊断能力，绝不能退回普通 `SaveManager` 播放。`[Inferred]`
- 对纯外围 Ritsu UI/settings/localization，若未来做成反射式可选 adapter，可以降级到内建 UI/default config/logging；但这不是首个实验范围。`[Inferred]`

## 5. 唯一的下一运行实验

### `SPIKE-INV003A — Transactional ReplaySaveContext session isolation`

这是本报告推荐的**唯一**运行候选。它只证伪 session/store/teardown 边界；不实现 capture、自定义 dispatcher、seek、room-before A/B 或完整产品 UI。

#### 假设

在一个真实 profile 已有普通 Continue 时，使用“全局 in-memory `SaveManager` transaction + `ShouldSave=false` + ordered restore”的单一机制，可让 native replay setup/最窄 fixture 从主菜单进入和退出，而普通 current run、progress、stats、history、achievement、daily、leaderboard 和 cloud 均不发生写/delete/submit。

#### 前置保护与范围

1. 只允许目标 DLL hash 精确匹配；只加载实验 mod 与必要依赖；RitsuLib 不加入本实验。
2. 开始前退出到主菜单，确认 `RunManager.State == null` 且 Continue 可见；记录其位置摘要与 `NumReloads`。
3. 对当前 profile 的 `current_run.save`/backup、progress/profile/preferences、run-history、stats/achievement 相关文件及 cloud-visible inventory 做路径、长度、mtime、SHA-256 基线；另做可恢复的只读备份。实验绝不以 disposable profile 代替该验收。
4. 给**原始** save/cloud store 的 write/delete，以及 metrics、leaderboard、daily、platform achievement、run-history/stats 提交边界加 audit-only counters；counter 不改变返回值。
5. 实验 UI 只需一个临时、真正位于主菜单 peer group 的 Replay 按钮和一个最小 fixture 选择；管理页打开期间不得建立 state。
6. 创建完全独立的 memory-backed `SaveManager`；所需 prefs/profile/progress 用序列化 round-trip 深拷贝。安装 override 后才 setup；`SetUpReplay` 返回后第一时间断言并置 `ShouldSave=false`。

#### 一个实验内的有界 fault matrix

按顺序执行，任一步失败立即停止后续 phase：

1. **watch/normal stop**：进入 fixture，达到首个可观看 scene，立即 Stop，执行唯一 teardown。
2. **direct-write sentinel**：同一 fixture 仅使用游戏已有 replay event path 达成一次 combat victory，以确认 `SaveProgressFile` 命中 memory manager、原 manager counter 仍为零；不加入 Replay Mod capture/dispatcher。
3. **setup failure**：在 `SetUpReplay` 已赋 `State`、scene launch 前注入一次受控异常，验证 finally cleanup/restore。
4. **post-launch failure**：launch 后在实验 harness 边界注入一次异常，验证相同 teardown；不测试自定义 dispatcher。
5. **restart check**：所有正常 phase 完成并退出游戏后重新启动，确认同一 profile 的 ordinary Continue 仍由原文件加载且与 baseline 一致。

每个 phase 使用同一个 transaction 实现和同一个 teardown；这些是同一隔离机制的故障覆盖，不是多个架构候选。

#### 通过标准

- 管理页打开/关闭期间 `RunManager.State` 始终为空；Replay 入口层级为 main-menu peer。
- 每个 playback phase 中原 manager/store 的 write/delete counter 为 0；所有外部提交 counter（metrics、daily、leaderboard、achievement、stats/history）为 0。
- victory sentinel 确实在 memory manager 观察到预期 progress-save attempt，证明实验覆盖了绕过 `ShouldSave` 的路径。
- 每次 teardown 都满足：隔离 save task 已观察完成；`RunManager.State == null`；global manager 恢复为原对象；恢复发生在新 `NMainMenu._Ready` 前。
- 普通 `current_run.save` 与 backup 的 bytes/hash/mtime、`NumReloads`、位置摘要完全不变；其他 ordinary progress 文件 inventory 完全不变。
- return-to-menu 后 Continue 可见且缓存内容等于 baseline；游戏重启后仍相同。
- 没有生成普通 run history、cloud payload、daily/leaderboard 或平台成就副作用；只允许明确列出的 mod-owned evidence/log 文件变化。

#### 失败标准与停止条件

以下任一发生即 **FAIL-CLOSED 并停止本实验**：目标 hash 漂移；无法完整初始化隔离 manager；任何原 store/cloud write/delete；任何提交 counter 非零；ordinary 文件 metadata/hash 或 reload count 变化；manager 未在菜单创建前恢复；cleanup 后 state/scene/synchronizer 残留；Continue 消失/内容不符；未捕获异常或挂起 save task。

若 phase 1 失败，不进入 victory/fault/restart phase；若任何后续 phase 失败，不尝试 room-before、capture、dispatcher 或替代隔离机制。把最小复现和 mod-owned 日志交回调查。若全部通过，只能说明该机制值得进入下一设计门，不能据此宣称任意 Replay 重建已完成。

## 6. 未闭合项

- `[Unknown]` native `CombatReplay` 能否覆盖 MVP 任意 singleplayer Replay baseline；当前唯一 caller 是 debug combat replay。
- `[Unknown]` 所有 Steam/cloud/platform/第三方 mod side channel 是否都能由建议的 audit points 完整观察。
- `[Unknown]` memory manager 的最小初始化对象图、延迟 save task 与 scene teardown 在真实运行中的竞态。
- `[Unknown]` RitsuLib 各项开销数量级与社区依赖接受度；它们不影响下一隔离实验选择。

以上 Unknown 均没有被包装为已确认能力。A/B/C 已达到静态调查停止条件；下一步只有 `SPIKE-INV003A` 一个运行候选，本任务在此停止。
