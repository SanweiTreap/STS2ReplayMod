# INV-007 — SPIKE-INV006B `RunState` 前置条件静态定位

调查日期：2026-09-14  
任务类型：纯静态调查  
固定游戏：public-beta `v0.111.0` / commit `41cef1ea`  
固定 DLL：9,757,184 bytes / SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`  
固定 fixture：20,441 bytes / SHA-256 `4DFB27BF3BE315F85ED7306C2953675E6A7888378ACF5F2B8110465695EDC631`  
006B closed artifact：17,487 bytes / SHA-256 `98B91618F67E7AD5E651C1BC1B41515040A599976FC237E8FC24279134538511`

## Confirmed

### 调查边界与身份

- 本调查没有启动游戏，没有运行、构建、安装或修改 `SPIKE-INV006B`，没有实现 runtime probe，也没有访问当前存档、当前 profile、Steam/cloud、account selector、machine settings 或 recovery backup 内容。
- 新增工具只读取固定 `sts2.dll` 与工作包明确允许的固定 `.mcr`；两者均先检查 exact path，并在解释前检查 length/SHA-256。fixture checker 只反序列化 `SerializableRun`，不调用 `RunState.FromSerializable`，不建立 run 或 scene。
- fixture 完整消费 `20,441/20,441` bytes；其 `SerializableRun` 为单玩家 `CHARACTER.IRONCLAD`，`PreFinishedRoom == null`，有 4 个 visited map coords，最后坐标 `(row=3,col=5)`。这些事实不直接证明 replay load 后会生成哪一个 encounter 或多少敌人。

### 聚合 `RunState` 断言矩阵

下表只列出会显式构造 `StopRunException(BlockedPrecondition, RunState)` 的条件，并按 `DirectWriteTransaction.RunAsync` 的执行顺序排列。`ThrowBoundary()`、fixture identity、candidate gate 及普通异常不属于这个聚合结果。

| 顺序 | 006B 条件 | 时点 | 检查类别 | 固定输入/IL 能否证明应成立 | 若不成立，是否使两个 direct progress 调用本身无效 |
|---:|---|---|---|---|---|
| 1 | `runState.Players.Count != 1` | setup 前，`FromSerializable` 后 | fixture 身份与参数选择 | **是。** 固定 fixture 的 serialized player count 为 1；`FromSerializable` 逐项生成 player。 | **不属于方法通用条件。** 方法只需一个合法 `Player`；但 006B 用 `[0]`/`Single()` 选择固定 local player，因此对该 probe 是必要的选择条件。 |
| 2 | 设置 false 后 `RunManager.ShouldSave == true` | setup 后、scene/load 前 | isolation safety | **是。** setter/getter 均直接写读同一 backing field；若反射设置本身失败会走普通异常，而不是本行 `RunState`。 | **否。** 两个 progress 方法均不读取 `ShouldSave`；它只保护同一 replay session 的其他 production save 分支。 |
| 3 | `NGame.Instance == null` | `Launch` 后、设置 scene 前 | probe setup 基础设施 | **仅 IL 不能证明。** 官方 caller 直接解引用它；006B 显式 fail closed。 | **否。** 两个同步 progress 方法都不读取 `NGame`，但当前 scene factory 无法在这里继续。 |
| 4a | load 后 `SaveManager.Instance` 不是 candidate | load 后、direct call 前 | isolation safety | **固定 game IL 不能单独证明全局身份在整个异步窗口绝不被外部代码改变。** 006B 在安装后已即时确认一次。 | **是（对 isolation 目标）。** 调用仍可能在功能上有效，但会失去“只写 isolated manager/store”的目标。 |
| 4b | `RunManager.DebugOnlyGetState()` 不是原 `runState` | load 后、direct call 前 | probe setup 完整性 | **固定路径可证明。** `SetUpReplay` 把同一对象写入 `RunManager.State`；`DebugOnlyGetState` 只是返回该 backing field；所查 setup/load 路径没有替换它。 | **否（对固定 Monster 分支）。** 方法不读取 `RunManager.State`；但这是合理的 setup-integrity 断言。 |
| 5 | `runState.CurrentRoom is not CombatRoom` | load 后 | production 参数取得 | **仅 fixture bytes 不能证明。** `PreFinishedRoom == null`，所以 `AbstractRoom.FromSerializable` 返回 null，`LoadIntoLatestMapCoord` 根据生成地图的最后坐标创建/进入 fresh room。 | **是。** 没有 `CombatRoom` 参数便不能按该 caller 调用 `UpdateProgressAfterCombatWon`。 |
| 6 | `room.Encounter == null` | load 后 | production 必要条件 | **若已有正常构造的 `CombatRoom`，构造链把 mutable encounter 放入 `CombatState`；但 fixture bytes 不能独立证明当前 room 的构造结果。** | **是。** update 读取 `CombatState.Encounter.Id`。 |
| 7 | `room.CombatState.Encounter == null` | load 后 | 重复的 production 条件 | **与上一项是同一读取。** `CombatRoom.get_Encounter()` 的方法体就是 `get_CombatState() → CombatState.get_Encounter()`；不是第二份 encounter。 | **是，但完全重复。** 不需要单独的“引用一致”断言。 |
| 8 | `encounter.SpawnedEnemies == null` | load 后 | probe 诊断便利（写成 null gate） | **getter 正常返回时必定非 null。** 它先 `AssertMutable()`，再返回 `_spawnedEnemies ?? new List<MonsterModel>()`。失败形态是 `AssertMutable` 抛普通异常，而不是 null。 | **空列表不会使调用无效。** update 可枚举空列表；只有 getter/枚举或元素访问失败才会使调用无效。 |
| 9 | `room.RoomType != Monster` | load 后最终 OR | fixture/scenario 范围 | **fixture bytes 不能证明 fresh reconstruction 的 room type；现有成功观看证据只能补强推断。** | **否，方法支持 Monster/Elite/Boss。** 但 Elite/Boss 会进入额外 unlock helper，超出当前最窄 mutation oracle，因此新 sentinel 可把 `Monster` 保留为独立的 scenario-scope gate。 |
| 10 | `!ReferenceEquals(room.CombatState.RunState, runState)` | load 后最终 OR | probe setup 完整性 | **正常 `CombatRoom(encounter, runState)` 构造会把传入对象原样存入 `CombatState.RunState`。** 对 fresh-room 完整调用链可强支持，但不是 fixture 序列化字段。 | **固定 Monster 分支否。** update 读取该值到 local，但只在 Boss 分支解引用 `CurrentActIndex`。 |
| 11 | player character 不是 `CHARACTER.IRONCLAD` | load 后，`spawnedMatch` | fixture 身份 | **serialized player 为 IRONCLAD；`FromSerializable` 使用 `CharacterId` 还原 character。** | **否。** update 对任意合法 character id 建 encounter/enemy fight stats。 |
| 12 | enemy count 不是 2，或任一 enemy 不是 `MONSTER.TOADPOLE` | load 后，`spawnedMatch` | fixture 身份 | **不能由 initial `SerializableRun` 静态证明。** fixture checksums 证明原录制战斗有两只 TOADPOLE，但 `PreFinishedRoom == null`，replay load 走 fresh room generation；原始 full-state 不是 reconstruction 后对象身份的 serialized source。 | **否。** update 枚举实际列表；数量与具体 ID 只决定 mutation 内容，不决定调用合法性。 |

`runState.Players.Single()` 不在表中：它不是显式 `BlockedPrecondition + RunState`，若先前 count gate 后集合异常变化而抛出，外层 generic catch 会分类为 `BlockedInstrumentation + RunCallback`。同样，`CombatState` 为 null、`SpawnedEnemies.AssertMutable()` 失败、enemy 元素为 null等也不会落入旧聚合枚举。这是 006B failure taxonomy 的遗漏。

### 006B 中计算但未正确进入前置判定的事实

- `fixtureEncounter = room.RoomType == Monster && encounter.Id.Equals(combatEncounter.Id)` 在 direct call 前被计算，但**没有进入最终 pre-call `if`**。它只在两次调用之后写入 `SentinelEvidence`，最终可能造成 `FailMutationOrWrite + Write`。
- `encounter` 与 `combatEncounter` 来自同一 getter 链；在 getter 成功且结果非 null时，ID 相等检查不提供独立身份信息。
- `SpawnedEnemies ?? throw` 的 null 分支不可达；真实遗漏的是 getter 的 mutability requirement、enumeration 是否成功、以及元素/ID 是否可读。
- `spawnedMatch` 在 pre-call gate 已检查一次，又进入 post-call `SentinelPassed`，是重复 gate。
- `SentinelEvidence` 的 `LegalScene` 被硬编码为 `true`，没有对应独立计算变量；旧 `SceneReady` 事件只说明聚合断言全部通过，不能区分 scene 合法性的子条件。

### 三路径 setup 顺序对照

| 边界 | SPIKE-INV006B | SPIKE-INV003A Phase B（已正常观看/Stop） | 官方 `NMultiplayerTest.RunReplay` | 结论 |
|---|---|---|---|---|
| run state / player | `FromSerializable`；要求 count=1；取 `[0].NetId` | `FromSerializable`；直接取 `[0].NetId` | `FromSerializable`；按 caller 的 `playerIndex` 取 player/net id | `FromSerializable` 顺序相同；单玩家断言是 006B fixture selector，不是 official caller 条件。 |
| setup | `SetUpReplay` → 反射置 `ShouldSave=false` → getter 复核 | 相同 | `SetUpReplay`，不改 `ShouldSave` | `SetUpReplay` 内先设 `State`，再以 `shouldSave=true` 调 `InitializeShared`；false 是 transaction isolation 加固，不是 replay caller 或 direct method 前置。 |
| synchronizer / preload | disable combat-state sync → preload characters → preload act | 相同 | 相同 | 无顺序差异。 |
| launch / scene | `Launch` → stop music → 显式检查 `NGame` → `NRun.Create(runState)` → set current scene → await `GenerateMap` | 相同，`NGame` 用普通异常 | 相同，但直接解引用 `NGame.Instance` | 006B 的显式 null 分类只改善 fail-closed taxonomy。 |
| replay ids | action → hook → checksum → choice → reward fast-forward/load | 相同 | 相同 | 无顺序差异。 |
| room source | `FromSerializable(PreFinishedRoom, runState)` → `LoadIntoLatestMapCoord` | 相同 | 相同 | 固定 fixture 的 `PreFinishedRoom` 为 null；三者均让 load 根据 latest map coord 创建 fresh room。 |
| pause/frame | while `ActionExecutor.IsPaused` 每帧等待；退出后**额外等 1 frame** | Phase B 只等到 unpaused；不额外等 frame即宣布 watchable | 只等到 unpaused，然后开始 event loop | extra frame 是 006B 独有，且两个 direct progress 方法不读取 executor/frame/scene tree。 |
| post-load gate | manager/run identity、CurrentRoom、encounter、room type、run reference、character、enemy count/IDs | Phase B 无这些 gate；等待人工 Stop | official caller 无这些 gate；直接 dispatch events | 006B 的新增聚合断言是其在 `SceneReady` 前停止的唯一结构差异区域。 |

`RunState.FromSerializable` 只还原 shared run/player/acts/RNG/odds/history 等 run 数据；它不从本 fixture 构造 `CurrentRoom`。`RunState.CurrentRoom` 是 `_currentRooms.LastOrDefault()`，在 `LoadIntoLatestMapCoord`/room enter 后才有意义。`AbstractRoom.FromSerializable(null, runState)` 明确返回 null。以上均固定到 v0.111.0 IL。

### 两个 direct progress 方法的最小前置条件

| 方法 | 直接读取/动作 | 最小契约 | 006B 非必要断言 |
|---|---|---|---|
| `SaveManager.UpdateProgressAfterCombatWon(player, room)` wrapper | 只取 `_progressSaveManager`，转发两个参数 | 当前 `SaveManager` 必须是目标 candidate，且其 `_progressSaveManager` 已初始化 | `NGame`、CurrentRoom identity、`DebugOnlyGetState`、`ShouldSave`、frame/executor 均不被读取。 |
| `ProgressSaveManager.UpdateAfterCombatWon` common path | `room.CombatState.RunState`（先保存 local）、`player.Character.Id`、`room.CombatState.Encounter.Id`、candidate `Progress` encounter stats | 非 null player/character；非 null room/combat state/encounter；合法 ModelId；已初始化且可变的 candidate Progress | encounter 双引用一致、固定 character ID、具体 encounter ID均不需要。 |
| room-type branch | `room.RoomType`；Boss 才解引用 `RunState.CurrentActIndex` 并调用 boss helpers；Elite 调 elite helper；Monster不走这些 helper | 若新 sentinel 固定为 Monster，只需 room type 可读且为 Monster；若扩到 Boss，则另需 non-null run state/current act及 helper 契约 | `ReferenceEquals(CombatState.RunState, runState)` 对 Monster mutation 不是必要条件。 |
| enemy loop | `room.Encounter.SpawnedEnemies`；每个 `enemy.Id`；按 character 增加 enemy win；首次 enemy stats 时写 `player.DiscoveredEnemies` | encounter 必须 mutable到可读 getter；列表可枚举；元素/ID合法；`DiscoveredEnemies` 在首次发现分支可写 | enemy 数量可为 0；无需 2；ID无需 TOADPOLE；无需把 list 与 checksum creatures 对齐。 |
| `SaveManager.SaveProgressFile()` wrapper | 只取 `_progressSaveManager` 并调用 `SaveProgress` | candidate progress manager 已初始化 | 不读取 run/room/player/encounter/ShouldSave。 |
| `ProgressSaveManager.SaveProgress` | `Progress.ToSerializable`；migration latest schema；JSON serialize；`_saveStore`；`_profileIdProvider.CurrentProfileId`；progress logical path；同步 `WriteFile` | candidate Progress、migration manager、profile provider/profile id、memory store均有效；写路径解析到允许的 profile1 progress path；payload可序列化 | scene、run state、fixture monster identity全不需要。该方法内部捕获写异常，所以 sentinel 必须用 audit/write-count/payload反序列化确认成功，不能只依赖“未抛异常”。 |

production victory caller 的实际局部顺序是：先完成 room/run teardown 和 `SaveRun(room,false)`，再 `UpdateProgressAfterCombatWon(localPlayer, room)`，接着调用 `AchievementsHelper.CheckForDefeatedAllEnemiesAchievement(...)`，然后 `SaveProgressFile()`。新的 direct-write sentinel 若只连续调用两个 progress 方法，验证的是刻意隔离出的 manager/store 路由，不等同于自然 victory caller，也不覆盖夹在两者之间的 achievement helper。

### 唯一下一建议：`READY_NEW_SENTINEL`

静态证据足以为**全新 ID/schema** 的 direct-write isolation sentinel 给出最小修订契约；不得重用、修补或重跑 006B。

应保留的 pre-call gate（每项独立枚举）：

1. 固定 DLL/fixture identity，以及 serialized player count=1（只作为固定参数选择）。
2. timing/candidate/reference-independence/protection gates与 candidate progress subsystem 初始化状态。
3. `SetUpReplay` 完成；`ShouldSave=false` 且复核（标为 isolation safety，不标为 progress method requirement）。
4. scene factory 所需的 `NGame` 可用；`LoadIntoLatestMapCoord` task 正常完成。
5. override 后及 direct call 紧前 `SaveManager.Instance == candidate`。
6. `DebugOnlyGetState() == runState` 可保留为 setup-integrity gate，但使用独立失败枚举。
7. 从 `runState.CurrentRoom` 取得 `CombatRoom`；`CombatState`、`Encounter`、`player.Character`、`player.DiscoveredEnemies` 可用；`SpawnedEnemies` getter/枚举及每个 enemy ID 可读。
8. `RoomType == Monster` 只作为本 sentinel 避开 Elite/Boss helper 的 scenario-scope gate，不宣称为 production 通用前置条件。

应删除的 pre-call gate：

- `CombatState.RunState` 与外部 `runState` 的引用相等（固定 Monster 方法体不需要）。
- `room.Encounter` 与 `room.CombatState.Encounter` 的独立 null/equality 双重检查；只读取一次 `CombatState.Encounter`。
- `SpawnedEnemies != null` 形式的不可达 null gate；改为 getter/枚举成功的明确 gate。
- fixed encounter reference/ID 一致、enemy count=2、全部 enemy 为 TOADPOLE、character 必须是 IRONCLAD。
- `ActionExecutor.IsPaused` wait 和额外一 frame；它们属于 event dispatcher readiness，不属于同步 progress mutation/save 的字段契约。load task 正常完成应作为边界。

应延后到调用后验证：

- 从调用前实际读取的 `character.Id`、`CombatState.Encounter.Id`、实际 enemy IDs 与 candidate progress baseline计算 mutation oracle；调用后比较完整 canonical progress及 `DiscoveredEnemies` 的预期变化。
- `UpdateProgressAfterCombatWon` audit count=1；在它之后、save之前 memory write count=0。
- `SaveProgressFile` audit count=1；memory store exact logical progress write count=1、同步写、payload可反序列化且 canonical等于 mutation 后 candidate progress。
- original/unknown manager/store、cloud/external boundary保持零；随后按既有唯一 teardown owner恢复并复核保护证据。
- fixed character/monster描述若确有诊断价值，只能作为非判定的固定枚举 observation；不得进入 PASS 或方法合法性条件。

建议的新独立事件枚举为：`RunStateParsed`、`ReplaySetupComplete`、`SceneLoadComplete`、`DirectArgumentsReady`、`ProgressUpdated`、`ProgressSaved`。建议的新 precondition failure 枚举为：`FixturePlayerCardinality`、`ShouldSaveDisableFailed`、`NGameUnavailable`、`ManagerOverrideLost`、`RunStateBindingLost`、`CurrentRoomNotCombat`、`UnsupportedRoomType`、`CombatStateUnavailable`、`EncounterUnavailable`、`CharacterUnavailable`、`DiscoveredEnemiesUnavailable`、`EnemyListUnreadable`、`EnemyEntryInvalid`、`ProgressSubsystemUnavailable`。mutation/write/boundary/teardown 失败继续使用各自独立类别，禁止再次汇总为 `RunState`。

此建议只解锁由 Project Lead 决定是否签发的新 sentinel。即使未来通过，也只证明 direct progress mutation/write 在该 transaction 中路由至 isolated memory manager/store；不代表 direct-write isolation 当前已通过，不代表 session isolation、自然胜利、replay fidelity、fault/restart、capture/dispatch 或 seek 已通过。

## Inferred

- 用户看见战斗画面、日志出现 combat-room marker、且 teardown 确实执行 cleanup，强支持 006B 已越过 `NGame` 检查并进入 post-load 区域；这不能把某一 post-load 子条件升级为 Confirmed。
- 因 1-player fixture 已静态确认，`ShouldSave` backing field 读写直接，run-state getter identity 路径直接，而 encounter/monster identity 是 006B 相对既有成功 setup 新增且最强的 fixture-specific限制，所以旧失败更可能位于 post-load scene/fixture assertions；仍无法确定具体一项。
- 固定 replay 的 checksums 证明原录制战斗曾有两只 TOADPOLE，但由于 initial replay save 的 `PreFinishedRoom` 为 null，不能把它等同于“fresh reconstruction 后 `SpawnedEnemies` 必为相同两个对象”。这是删除具体 monster gate 的核心理由。
- 对新的 fixed-Monster sentinel，保留 `RoomType == Monster` 是为了保持 mutation oracle 和副作用范围最窄，而不是因为 production update 只支持 Monster。

## Unknown

- closed 006B artifact 没有保存子条件枚举或字段值，故旧运行究竟失败于 manager identity、CurrentRoom、room type、run reference还是 `spawnedMatch`，仍不可恢复。`READY_NEW_SENTINEL` 不声称定位了旧运行的唯一失败子条件。
- 纯 IL 不能证明所有第三方 callback 在异步 scene/load 窗口内都不会替换全局 manager，故新 sentinel仍须在 direct call 紧前复核 candidate identity。
- 本调查没有建立 scene，因而没有动态确认 fresh room 的实际 encounter、enemy list或 exact room type；这些值也不再需要作为具体 fixture identity gate。
- direct progress 方法尚未在 isolated transaction 中执行；其 mutation、write、异常吞并后的 audit表现与 teardown安全性均仍待一个获批的新 runtime工作包验证。
