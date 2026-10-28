# SPIKE-INV007A：最小契约 direct progress-write isolation sentinel

历史状态：原始 R1 因 Ready 后 timeout 与固定 artifact 文件冲突未产生有效业务结论。R2 随后产生有效 terminal，但 mutation oracle 与 candidate 共享 nested stats，只形成 direct-update routing 的局部证据，save 未测试。旧证据必须保留；当前唯一授权是 `project-management/handoffs/SPIKE-INV007A-R3.md` 中的 detached-oracle 窄修订。下文原“一次运行/永久退役”规则依次由 R2、R3 handoff 在明确列出的范围内覆盖。

任务类型：全新受限运行实验；需要项目发起人手动启动游戏

## 唯一目标

在已验证的 scope-safe startup timing gate 后，以 live state 构造等价且引用独立的 in-memory `SaveManager` candidate；建立固定 replay 的 Monster `CombatRoom`，但只检查两个 production progress 方法真实需要的参数与隔离安全条件；依次直接调用一次：

1. `SaveManager.Instance.UpdateProgressAfterCombatWon(player, room)`；
2. `SaveManager.Instance.SaveProgressFile()`。

证明 mutation oracle 与唯一 progress write 均落在 candidate/memory store，随后 ordered teardown 恢复原 manager 并确认 profile1 保护集合不变。

本任务不重演 replay events，不触发自然 victory，不验证 achievement helper、replay fidelity、fault/restart、完整 session isolation、capture/dispatch、reconstruction 或正式架构。

## 开始前必须阅读

1. `project-management/CURRENT_STATUS.md`
2. `project-management/TEST_PROTOCOL.md`
3. `project-management/DECISIONS.md` 中 D-034 至 D-041
4. `project-management/investigation-reports/INV-007-report.md`
5. `evidence/inv-007/README.md`
6. `project-management/investigation-reports/SPIKE-INV006B-report.md`
7. `evidence/spike-inv006b/runtime-review.md`
8. `experiments/SPIKE-INV006B/` 中与 timing gate、candidate、保护集合、boundary audit 和 teardown 有关的源码
9. `evidence/inv-006/scope-safe-audit-contract.md`

006B 只作安全构件和失败反例参考。不得修改、构建、安装或运行任何旧 probe；不得复制其聚合 `RunState` gate 或 fixture-specific monster/character gate。

## 固定身份与全新目录

- 游戏：public-beta `v0.111.0`，commit/branch `41cef1ea`
- `sts2.dll`：9,757,184 bytes；SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`
- 固定 fixture：`evidence/spike-inv003a/protected-backup/20260907T175924.5461327Z/profile1/replays/latest.mcr`
- fixture：20,441 bytes；SHA-256 `4DFB27BF3BE315F85ED7306C2953675E6A7888378ACF5F2B8110465695EDC631`
- 新 probe id：`SPIKEINV007A`
- 新源码：`experiments/SPIKE-INV007A/`
- 新证据：`evidence/spike-inv007a/`
- 新 runtime schema：`spike-inv007a.direct-write.v1`

任一 identity 不符即 `BLOCKED_IDENTITY`。不得换 build/fixture、覆盖旧证据或沿用旧 artifact schema。

## 固定环境与人工操作

- 只使用模组存档 1 号；Continue 是测试隔离哨兵，全内容解锁及其文件仍受保护。
- Steam Cloud 由项目发起人在运行前确认关闭。
- 只启用 `SPIKEINV007A`；关闭 `SPIKEINV006B`、其他旧 probe、RunReplays、BaseLib、RitsuLib 和其他 gameplay mod。
- Implementation Codex 不得使用 Computer Use 或任何 GUI 自动化；必须先交付精确的人工测试请求，由项目发起人启动游戏并点击。
- 不得切换、读取、枚举、复制或备份其他 profile；结论不能外推到其他槽。
- UI 到达 Ready 后只允许一次 Run 点击。点击后用户不出牌、不结束回合、不领取奖励、不操作其他 UI，等待自动 teardown 和终态；出现首个终态后立即退出，不重跑。

## Phase 0：实现与静态安全门禁

### A. 重新建立既有安全构件

在全新源码中重新审查并移植：

- 006A/006B 的 outer-cloud + graphics + startup/menu timing gate；
- profile1-only 保护 baseline 与 restricted recovery backup；
- live-seeded Profile/Settings/Prefs/Progress candidate 等价与递归引用独立检查；
- pre-argument identity-first boundary audit、single-CAS terminal seal、winner ticket；
- 单一 teardown owner 与原 manager/主菜单/Continue/保护集合复核。

不得降低既有约束：original/unknown manager/store 或 external boundary 一旦出现立即 fail closed；原/未知 store 在读取 path、payload 或 argument-array element 前按 identity 返回；candidate 只允许同步写一次 exact logical profile1 progress path。禁止 global account-root 枚举、其他 profile、broad path/payload observer、Steam/cloud leaf patch。

### B. 新契约的 source/binary tests

安装前测试和审计至少证明：

- 新 ID、目录、schema 与旧 probe 完全分离；
- production source 不包含 `TOADPOLE`、固定 `IRONCLAD` 判定、enemy count `== 2`、聚合 `FailureSite.RunState`、`ActionExecutor.IsPaused` 或额外 frame wait；
- 每个 precondition 有独立枚举，generic exception 不能伪装成某个已确认前置失败；
- actual character/encounter/enemy IDs 只用于内存中计算 expected mutation，不作为 pre-call PASS gate；
- `SaveProgressFile` 抛错吞并场景必须由 audit/write count/payload equivalence 判失败；
- terminal seal、post-seal rejection、winner ticket、schema whitelist、identity negatives 与 original/unknown pre-argument return 均有 synthetic tests；
- Release build、固定 DLL/fixture identity、Harmony patch smoke、source/binary forbidden-surface audit 全部通过。

## Phase 1：一次性 runtime sentinel

### A. timing、保护与 candidate

Run 点击后必须在同一 owner 中重新验证完整 timing gate，随后建立 profile1-only protection baseline/recovery backup，再从 gate 后 live manager 内存序列化构造 candidate。

override 前必须全部通过：current profile 1；四组件 raw/canonical 等价；根对象和递归 mutable graph 无共享引用；所有 candidate store 仅为新 memory store；初始化 I/O 全部封闭且无 mutator；original canonical state 未变化；timing gate 再验证稳定。

### B. setup 与独立事件

使用与官方/003A 一致的最小顺序：`RunState.FromSerializable` → `SetUpReplay` → `ShouldSave=false` → disable combat synchronizer → preload → `Launch` → `NRun.Create` → `GenerateMap` → replay id fast-forward/load → `LoadIntoLatestMapCoord`。

固定事件至少分开为：

- `RunStateParsed`
- `ReplaySetupComplete`
- `SceneLoadComplete`
- `DirectArgumentsReady`
- `ProgressUpdated`
- `ProgressSaved`

`LoadIntoLatestMapCoord` 正常完成后直接进入参数检查；不得等待 `ActionExecutor.IsPaused`，不得额外等待 frame，也不得读取或分派 replay events。

### C. 最小 pre-call gate

每项必须独立失败枚举，不得重新合并为 `RunState`：

- fixture player cardinality 可选择唯一 player；
- `ShouldSave=false` 设置并复核成功（isolation safety）；
- scene factory 所需 `NGame` 可用，setup/load task 正常完成；
- override 后和 direct call 紧前 `SaveManager.Instance == candidate`；
- `RunManager.Instance.DebugOnlyGetState() == runState`（setup integrity，不宣称为 progress method requirement）；
- `runState.CurrentRoom` 是 `CombatRoom`；
- `CombatState`、同一次读取所得 `Encounter`、`player.Character`、`player.DiscoveredEnemies` 可用；
- `SpawnedEnemies` getter 和枚举成功，每个实际元素及其 ID 可读；空列表本身不得失败；
- `RoomType == Monster` 只作为本实验避免 Elite/Boss helper 的 scenario-scope gate；
- candidate progress/migration/profile-provider/memory-store 子系统具备执行 update/save 的已初始化状态。

明确禁止作为 pre-call gate：固定 character ID、固定 encounter/enemy ID、enemy count、两个 encounter getter 的引用/ID 对比、`CombatState.RunState` 与外部 runState 的引用相等、checksum creature 对齐、executor/frame readiness。

推荐独立 failure enums 至少覆盖：`FixturePlayerCardinality`、`ShouldSaveDisableFailed`、`NGameUnavailable`、`ManagerOverrideLost`、`RunStateBindingLost`、`CurrentRoomNotCombat`、`UnsupportedRoomType`、`CombatStateUnavailable`、`EncounterUnavailable`、`CharacterUnavailable`、`DiscoveredEnemiesUnavailable`、`EnemyListUnreadable`、`EnemyEntryInvalid`、`ProgressSubsystemUnavailable`。boundary、mutation、write、teardown、artifact failure 保持各自类别。

### D. 动态 mutation oracle 与唯一 write

调用前从 candidate canonical Progress baseline、实际 `character.Id`、实际 `CombatState.Encounter.Id`、实际 enemy IDs 和 player `DiscoveredEnemies` 计算 expected result。oracle 必须由固定 IL 规则独立实现；不得通过在另一个 manager 上调用 production update 来生成 expected。

随后：

1. 复核 candidate identity 与 boundary clean；调用一次 `UpdateProgressAfterCombatWon(player, room)`。
2. 必须观察 update audit count=1、candidate canonical Progress 等于 expected、`DiscoveredEnemies` 等于 expected，且 memory write count仍为 0。
3. 将 expected canonical 交给 write audit；调用一次 `SaveProgressFile()`。
4. 必须观察 save audit count=1、恰好一次同步 candidate memory-store write、exact logical profile1 progress name、payload 可反序列化且 canonical 等于 update 后 candidate Progress。

actual model IDs 仅可用于内存 oracle。closed artifact 不记录原始 progress/save payload、物理路径、用户名、Steam 标识或任意 profile 数据；只记录固定 enum/booleans/counts、canonical SHA-256、runtime identity、monotonic event order 与 allowlisted logical scope。若记录 model observation，只允许有界的合法游戏 `ModelId` 文本，并且它不得参与 pre-call PASS；更优先只记录 `idsReadable` 与 expected/actual canonical hash。

### E. ordered teardown

两个调用及其验证完成后立即进入既有 ordered teardown。任何失败也由同一个 owner 按相同顺序 teardown：禁止新业务动作 → bounded wait → state 存在时恰好一次 `CleanUp(true)` → clear override 并恢复 original manager → original task terminal → `LoadMainMenu(false)` → run state null、Continue visible → profile1 保护集合/inventory 复核 → seal → winner-ticket 写唯一 artifact。

不得自动恢复、覆盖或 rebaseline 任何受保护文件。若保护集合变化，保留 recovery evidence 并停止，由 Project Lead 决定后续。

## 结果分类

- `PASS_DIRECT_WRITE_ISOLATED`
- `FAIL_PREOVERRIDE_EQUIVALENCE`
- `FAIL_BOUNDARY`
- `FAIL_MUTATION`
- `FAIL_WRITE`
- `FAIL_TEARDOWN`
- `FAIL_SAFETY`
- `STOP_MISSING_TIMING_GATE`
- `BLOCKED_IDENTITY`
- 各独立 precondition classification/site
- `BLOCKED_INSTRUMENTATION`
- `BLOCKED_ARTIFACT_WRITE`

首个 failure/terminal 必须停止业务动作并 teardown。任一结果到达后本 probe 永久退役，不得现场修补或重跑。

PASS 只允许表述为：两个 direct production progress methods 在该固定 live-seeded isolated Monster replay session 中被路由至 candidate/memory store，且保护集合与 teardown 通过。不得升级为 natural victory、achievement、fault/restart、完整 session isolation 或 replay fidelity PASS。

## 人工测试请求格式

Implementation Codex 完成 Phase 0 后，不得自行启动游戏。它必须向项目发起人提交一段可直接执行的请求，明确：

- 启用/关闭的模组与 Steam Cloud 状态；
- 进入 profile1 后应看到的 Ready 文案；
- 唯一允许动作是点击一次 Run；
- 点击后不出牌、不结束回合、不领取奖励、不操作 UI；
- 预期自动返回主菜单并显示终态；
- 首个异常/终态立即停止，不重试；
- 回传终态文本、是否看见 combat、Continue 是否仍可见；
- Implementation Codex 随后只读取本 probe 的封存 artifact 和 allowlisted evidence。

## 交付物

1. `project-management/investigation-reports/SPIKE-INV007A-report.md`
2. `experiments/SPIKE-INV007A/` 全新源码、tests、preflight/build/static-audit 与 README
3. `evidence/spike-inv007a/README.md`
4. Phase 0 的 test/build/static-audit/installation identity 结果
5. profile1-only restricted recovery backup 与 before/after evidence
6. 若运行则恰好一份 closed runtime artifact

报告严格区分 Confirmed、Inferred、Unknown，只保留一个后续建议。Implementation Codex 不得修改 `CURRENT_STATUS.md`、`TODO.md`、`DECISIONS.md`、`EXPERIMENT_PLAN.md` 或 Roadmap。
