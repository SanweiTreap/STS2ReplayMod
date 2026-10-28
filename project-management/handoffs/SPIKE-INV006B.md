# SPIKE-INV006B：live-seeded direct progress-write isolation sentinel

历史状态：`COMPLETE — BLOCKED_PRECONDITION`（2026-09-14 经 Project Lead 验收）。本 probe 已退役，禁止修改、安装或重跑；当前后续任务见 `project-management/handoffs/INV-007.md`。

任务类型：全新受限运行实验；需要项目发起人手动启动游戏

## 唯一目标

在 `SPIKE-INV006A` 已验证的 scope-safe startup timing gate 之后，构造与当前 live `SaveManager` 内容等价且引用独立的 in-memory manager，安装一次全局 override，并在不分派 replay event、不触发自然胜利的普通 replay combat scene 中依次直接调用：

1. `SaveManager.Instance.UpdateProgressAfterCombatWon(player, room)`；
2. `SaveManager.Instance.SaveProgressFile()`。

验证两次 production method 的 mutation 与唯一 progress write 只发生在 isolated manager/store；随后 ordered teardown 恢复原 manager、返回主菜单，并确认 profile1 保护集合不变。

本任务不验证自然胜利、achievement helper、replay fidelity、fault/restart、reconstruction、capture/dispatch 或完整 session isolation。

## 开始前必须阅读

1. `project-management/CURRENT_STATUS.md`
2. `project-management/TEST_PROTOCOL.md`
3. `project-management/DECISIONS.md` 中 D-034 至 D-039
4. `project-management/investigation-reports/SPIKE-INV004A-report.md`
5. `project-management/investigation-reports/SPIKE-INV004B-report.md`
6. `evidence/spike-inv004a/phase-0-static.md`
7. `project-management/investigation-reports/INV-005-report.md`
8. `project-management/investigation-reports/INV-006-report.md`
9. `project-management/investigation-reports/SPIKE-INV006A-report.md`
10. `evidence/inv-006/scope-safe-audit-contract.md`
11. `experiments/SPIKE-INV004B/DeepCopyDiagnostic.cs`
12. `experiments/SPIKE-INV004A/ReplayTransaction.cs`
13. `experiments/SPIKE-INV006A/ScopeSafeSession.cs`

旧源码只作候选与失败反例。不得运行或修改旧 probe，不得读取 SPIKE-INV005A restricted artifact，也不得复制旧 broad `TransactionAudit` 后原样启用。

## 固定身份与新目录

- 游戏：public-beta `v0.111.0`，commit/branch `41cef1ea`
- `sts2.dll`：9,757,184 bytes；SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`
- 固定 native fixture：`evidence/spike-inv003a/protected-backup/20260907T175924.5461327Z/profile1/replays/latest.mcr`
- fixture：20,441 bytes；SHA-256 `4DFB27BF3BE315F85ED7306C2953675E6A7888378ACF5F2B8110465695EDC631`
- 新 probe id：`SPIKEINV006B`
- 新源码：`experiments/SPIKE-INV006B/`
- 新证据：`evidence/spike-inv006b/`
- 新 runtime schema：`spike-inv006b.direct-write.v1`

任一 identity 不符即 `BLOCKED_IDENTITY`。不得换 fixture/build、沿用旧 runtime artifact 或覆盖旧证据。

## 固定环境

- 使用模组存档 1 号；Continue 是测试槽隔离哨兵，全内容解锁和已有文件仍受保护。
- Steam Cloud 由项目发起人在运行前确认关闭。
- 只启用 `SPIKEINV006B`；关闭 `SPIKEINV006A`、所有旧 probe、RunReplays、BaseLib、RitsuLib 和其他 gameplay mod。
- Implementation Codex 不得使用 Computer Use 或任何 GUI 自动化；游戏只由项目发起人手动启动和点击。
- 不得切换、读取、枚举、复制或备份其他 profile；结论不能外推到其他槽的真实普通对局。

## Phase 0：实现与安全预检

### A. 复用 scope-safe timing gate

在新 probe 中重新实现/审查 SPIKE-INV006A 的最小 gate：graphics normal exit、唯一 `DoCloudSync` outer task terminal、startup/menu marker、original manager identity、`RunManager.State == null`、original `CurrentRunSaveTask` null/terminal。

它只作为本工作包 precondition，不生成 `PASS_SCOPE_SAFE_GATE` 终态。主菜单显示 `Direct Write Ready` 后，用户的一次 Run 点击必须在同一锁内重新验证全部 gate 条件并以 CAS 取得唯一 transaction owner。pending/missing outer task、第二 lifecycle 或 identity 变化均停止，不得靠 quiet time 放行。

### B. scoped protected baseline

在 Run 点击后、candidate/override 前，只允许解析和读取：active `modded/profile1/**`、exact account selector `modded/profile.save`、exact machine `settings.save`，以及上述固定 workspace fixture。

必须先机器确认 active profile id 为 1，再进行 allowlist canonicalization。只枚举 profile1 subtree，不枚举 account root。记录保护集合的 logical scope、relative name、existence、length、SHA-256、mtime 与 profile1 inventory；artifact/evidence 不写 physical path、Steam/user id。为全部存在的保护文件创建同一新 evidence 目录下的 recovery backup 并逐文件校验。backup 标记为 restricted local evidence，不得发布或作通用 fixture；发生差异后不得自动恢复或 rebaseline。

### C. live-seeded candidate

从 gate 后的 original manager 内存序列化 Profile/Settings/Prefs/Progress，在全新 `MockGodotFileIo` 中 seed 并完成 `InitProfileId(null)`、Settings、Prefs、Progress 初始化。禁止从 disk protected files 重建 candidate。

override 前必须全部通过：

- candidate current profile 为 1；
- Settings/Prefs/Progress raw 与 canonical 均和 live snapshot 等价；
- Profile、Settings、Prefs、Progress 根对象均不共享引用，递归 mutable graph 共享引用为 0；
- candidate 所有 manager store 都只指向新 memory store；
- candidate 初始化 I/O 全部封闭于 memory store，且 original live canonical state 在构造前后不变；
- timing gate 仍有效，original manager identity 与 current-run task 状态未变化。

任一失败立即 `FAIL_PREOVERRIDE_EQUIVALENCE`，不得安装 override 或进入 combat。

## Phase 1：scope-safe transaction audit

禁止旧式按全局调用先读取 path/payload 的 audit。只允许：

1. SaveManager boundary patch 在 enabled/sealed 检查后先比较 `__instance` identity，不读取其他参数。只允许 isolated manager 上恰好一次 update、一次 save；original/unknown manager 调用立即 latch failure。
2. store mutator patch在 first-instruction gate 后只读取 `__instance`。captured original/unknown store 调用只设置固定 boundary-failure enum，绝不读取 path、payload、exception 或详细计数。
3. 只有确认 `__instance` 是 candidate 的 exact memory store 后，才允许读取 path/payload；只允许 exact logical `modded/profile1/saves/progress.save` 的一次同步 `WriteFile`。payload 只在内存中反序列化比较，不把原文、path、length/hash 或字段值写入 artifact。
4. 外部 achievement/metric/leaderboard/daily/unlock boundary 只记录固定 boolean，不得格式化参数。任何命中立即 failure。
5. audit 从 override 前一刻持续到 original manager 恢复且新主菜单 ready；PASS 必须证明整个窗口没有 original/unknown store 或 external boundary。

每个 patch/continuation/callback 在 observer work 前检查 enabled/sealed，并在提交前锁内复核。首个 violation 原子 latch，禁止后续 sentinel action并立即交给唯一 teardown owner；不得继续收集 path/payload。teardown 使用独立、字段固定的 recovery lane，只记录预定 stage enum、manager identity/status boolean，不处理 callback 参数。最终 seal 后只有 CAS winner ticket 能写一次 closed artifact。

安装前必须有 source/binary forbidden-order audit 和 synthetic poison tests，证明 original/unknown store 分支在 path/payload getter 前返回；测试不得使用真实存档。

## Phase 2：isolated direct-write sentinel

只有 candidate 等价、保护 baseline/backup、scope-safe audit 和 timing gate 全部通过后：

1. 安装 `SaveManager.MockInstanceForTesting(candidate)` 并断言 identity。
2. 原生解析固定 fixture，使用既有 `RunState.FromSerializable` + `RunManager.SetUpReplay` 建立合法普通 combat scene；不读取/分派 `replay.events`。
3. setup 返回后立即设置并断言 `RunManager.ShouldSave == false`。
4. 到达稳定 scene 后，确认唯一 local player、普通非 elite/boss `CombatRoom`、run/room identity、fixture encounter 与 spawned enemies 符合已接受 Phase-0 静态模型。
5. 从 isolated progress baseline 计算完整 expected copy：encounter/character wins `+1`；两个重复 spawned enemy 对相同 character 的 wins 合计 `+2`；losses 和其他 progress 字段不变；`DiscoveredEnemies` 只按既有 empty-list 条件允许 0 或 1 个预期新增。
6. 调用 isolated `UpdateProgressAfterCombatWon(player, room)`，要求完整 canonical state 与 expected copy 相等，且尚无 store write。
7. 调用 isolated `SaveProgressFile()`，要求 exact isolated memory path 上恰好一次同步 write；将 payload 在内存反序列化为 `SerializableProgress`，与 mutation 后 isolated progress canonical-equal。
8. 立即进入 ordered teardown；不等待自然胜利，不调用 `EndCombatInternal`/achievement helper，不进入奖励或地图。

任何 precondition、delta、write count/path/payload 或 boundary 不符，停止余下 sentinel 并进入 teardown。

## Ordered teardown 与保护复核

唯一 owner 在 success/failure 下都按固定顺序尽力执行：

1. 禁止新用户操作和新 sentinel action；
2. 等待 isolated `CurrentRunSaveTask` bounded terminal；
3. state 非 null 时恰好一次 `RunManager.CleanUp(true)`；
4. 再次等待 isolated task；
5. 清除 override 并恢复/断言 original manager identity；
6. 等待 original current-run task；
7. original 恢复后才调用 `LoadMainMenu(false)`；
8. 等待主菜单 ready，确认 `RunManager.State == null`、Continue 可见；
9. 重新读取同一 allowlisted 保护集合，比较 hash/length/mtime/profile1 inventory；不枚举 account root；
10. seal 并写单份 artifact。

若真实保护数据发生变化，分类 `FAIL_SAFETY`，保留 before/after 与 recovery backup，不自动恢复、不 rebaseline；立即通知 Project Lead 和项目发起人，在明确决定前不再启动游戏。

## 人工测试请求

Implementation Codex 必须先完成代码审查、synthetic tests、Release build、forbidden-surface/order audit、game/probe/fixture identity 和安装准备，然后按 `TEST_PROTOCOL.md` 提交一轮精确请求：

1. 确认 v0.111.0、Cloud 关闭、模组存档 1 号，只启用 `SPIKEINV006B`。
2. 启动并停留主菜单，不进入 Continue/单人/多人/设置/profile 页面。
3. 等待 `Direct Write Ready`；此前不点击 Run。
4. Ready 后只点击一次 `Run Direct Write Sentinel`，之后不做任何输入。
5. probe 应自动进入 combat、执行两个 direct method、teardown 并返回主菜单；用户不得出牌、结束回合或点击任何战斗 UI。
6. 若出现奖励页面，不领取金币、卡牌、药水、遗物，也不进入地图；立即停止并回报。
7. 返回主菜单并显示 terminal classification 后立即停止操作、不要退出，回传 UI 结果；等待 Implementation Codex 完成证据复核后再正常退出。

失败、崩溃、卡死、身份错误、Continue 消失或任何非预期界面均立即停止，不重试。不得要求用户手动读取其他 profile 或分析日志。

## PASS 标准

`PASS_DIRECT_WRITE_ISOLATED` 必须同时满足：

- timing gate、protection baseline/backup、live-seeded equivalence/reference/store containment 全部通过；
- override 前无 run/scene/progress mutation；
- 不读取/dispatch replay events，不调用自然 victory/achievement；
- isolated update 精确符合完整 expected mutation；
- isolated save 恰好产生一次允许的 memory write，payload 等价；
- transaction 窗口 original/unknown manager/store、cloud/external boundary 为 0；
- ordered teardown 完成，original manager 恢复，run state null，Continue 可见；
- allowlisted protected files 与 profile1 inventory全部不变；artifact schema/safety 与 installed identity 通过。

PASS 只允许表述为：两个 direct production progress methods 在本固定 live-seeded isolated replay session 中被路由至 memory manager/store。不得升级为 natural victory、fault recovery、完整 isolation 或 replay fidelity PASS。

## 结果分类与停止条件

- `PASS_DIRECT_WRITE_ISOLATED`
- `FAIL_PREOVERRIDE_EQUIVALENCE`
- `FAIL_BOUNDARY`
- `FAIL_MUTATION_OR_WRITE`
- `FAIL_TEARDOWN`
- `FAIL_SAFETY`
- `STOP_MISSING_TIMING_GATE`
- `BLOCKED_IDENTITY` / `BLOCKED_PRECONDITION` / `BLOCKED_INSTRUMENTATION` / `BLOCKED_ARTIFACT_WRITE`

首个 failure 必须停止业务动作并进入 teardown；首个终态后不得修补重跑。任何结果都不在本任务内进入 fault/restart。

## 交付物

1. `project-management/investigation-reports/SPIKE-INV006B-report.md`
2. `experiments/SPIKE-INV006B/`：全新源码、tests、build/static-audit scripts 和 README
3. `evidence/spike-inv006b/README.md`
4. preflight/test/static-audit 结果
5. restricted allowlisted recovery backup 与 before/after protection evidence
6. 单份 runtime artifact（若进入运行）

报告严格区分 Confirmed/Inferred/Unknown，只给一个后续建议。Implementation Codex 不得修改 `CURRENT_STATUS.md`、`TODO.md`、`DECISIONS.md`、`EXPERIMENT_PLAN.md` 或 Roadmap。
