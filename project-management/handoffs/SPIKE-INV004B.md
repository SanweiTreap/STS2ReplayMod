# SPIKE-INV004B：pre-override deep-copy 等价诊断

> 历史状态（2026-09-10）：本工作包已以 `PARTIAL_DIAGNOSED` 完成并验收。Candidate B 的局部成功不解锁 direct-write；后续只执行 `INV-005` 静态调查，不得重试本 probe。详见 `project-management/investigation-reports/SPIKE-INV004B-report.md` 与 D-034。

任务类型：受限静态调查 + 主菜单运行诊断；需要项目发起人手动启动游戏

## 任务目标

解释 SPIKE-INV004A 中 `CreateIsolatedManager` 为何无法通过 content-equivalence gate，并验证是否能构造一个与当前 live `SaveManager` 状态精确等价、同时不共享可变对象引用的独立 candidate。

本任务只回答 candidate manager 的构造与等价性，不验证 direct progress-write、Replay session、自然战斗胜利、replay fidelity 或完整存档隔离。

## 开始前必须阅读

1. `project-management/CURRENT_STATUS.md`
2. `project-management/TEST_PROTOCOL.md`
3. `project-management/handoffs/SPIKE-INV004A.md`
4. `project-management/investigation-reports/SPIKE-INV003A-report.md`
5. `project-management/investigation-reports/SPIKE-INV004A-report.md`
6. `evidence/spike-inv003a/runtime/phase-b-transaction.json`
7. `evidence/spike-inv004a/runtime/direct-write-transaction.json`
8. `experiments/SPIKE-INV003A/ReplayTransaction.cs`
9. `experiments/SPIKE-INV004A/ReplayTransaction.cs`

## 固定事实与不得扩大解释的边界

- 目标游戏固定为 public-beta `v0.111.0`，commit `41cef1ea`。
- `sts2.dll` SHA-256 固定为 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`。
- SPIKE-INV004A 的失败发生在 transaction audit、`MockInstanceForTesting`、`SetUpReplay`、scene launch 和 progress 方法调用之前。
- SPIKE-INV004A 没有证明 direct-write isolation PASS 或 FAIL，也没有否决全局 transaction 候选。
- 相同 disk-rehydrate + JSON comparison 路径曾在 SPIKE-INV003A Phase B 通过；这不证明它当前仍正确，只说明差异可能依赖 session/live state。
- SPIKE-INV004A 只抛出 Settings/Prefs/Progress 合并的不等价异常，尚不知道具体组件、JSON path、值或表示形式。
- profile1 是专用测试槽。其 Continue 只是 active-profile 隔离哨兵；全内容解锁、受保护文件、目录结构和基线仍必须保持不变。不得读取、复制、切换或声称验证其他 profile。
- Steam Cloud 在运行前由项目发起人确认关闭。
- 禁止使用 Computer Use 或其他 GUI 自动化。需要启动游戏时，先提交精确人工测试请求，由项目发起人操作。

## 必须检验的假设

分别检验，不得混为一个 generic mismatch：

1. 当前 live manager 与磁盘文件已经发生合法的内存态漂移。
2. `InitProfileId`、`InitSettingsData`、`InitPrefsData` 或 `InitProgressData` 会应用默认值、迁移、规范化或基于运行环境的变换。
3. 旧 equality gate 比较的是非规范 JSON 字符串，结构相等但属性或集合顺序不同。
4. disk-rehydrated candidate 确实缺少或改变了状态。
5. 以当前 live serialized state 为输入，经过正式初始化路径仍能得到独立且精确等价的 candidate。

## Phase 0：静态门禁

仅在固定 DLL hash 上完成以下调查：

1. 追踪 `SaveManager` 对 profile identity、Settings、Prefs、Progress 的构造和 `Init*` 调用顺序。
2. 列出各 `Init*` 路径中的默认值、migration、repair、normalization、排序、环境读取和可能的异步/延迟 mutation。
3. 识别 Settings/Prefs/Progress 的正式 serialization/deserialization 入口，以及将 live serialized values 安全放入 `MockGodotFileIo` 所需的准确路径和 profile bootstrap。
4. 确认 candidate 初始化不会访问真实写 store；若任何初始化路径可能写入，必须由 mock store 捕获并记录，不能假定只读。
5. 定义结构化 JSON canonicalization：object property 按名称排序；array 默认保序，只有静态证据确认集合语义无序时才允许另做“无序视图”辅助比较。不得以 canonicalization 掩盖真实 array 顺序差异。
6. 定义引用独立性检查，至少覆盖 manager、SettingsSave、PrefsSave、ProgressSave 及静态证据识别出的嵌套可变 collection。不得通过直接赋值 live 对象来制造表面相等。

若无法证明 candidate 的所有读取和潜在写入都在 mock store 内，结果为 `BLOCKED_PRECONDITION`，不得请求启动游戏。

## Phase 1：只构建诊断 probe

新建独立目录：

- `experiments/SPIKE-INV004B/`
- `evidence/spike-inv004b/`

不得修改或重新启用旧 SPIKE probe。新 probe 只允许在主菜单、`RunManager.State == null` 时执行一次诊断，且必须满足：

- 不调用 `SaveManager.MockInstanceForTesting` 或任何全局 instance override。
- 不调用 `RunManager.SetUpReplay`、saved setup、load/launch/cleanup 或 scene transition。
- 不读取或分派任何 replay event/fixture。
- 不调用 `UpdateProgressAfterCombatWon`、`SaveProgressFile` 或其他 production progress mutation。
- 不访问真实 store 进行写/delete；所有 candidate I/O 使用独立 `MockGodotFileIo`。
- 不修改 live manager 或其任一可变子对象。

probe 生成两个 candidate：

### Candidate A：disk-rehydrated

复现 SPIKE-INV004A 的构造，但逐组件记录：

- live 与 candidate 的原始 serialized JSON SHA-256；
- canonical JSON SHA-256；
- Settings、Prefs、Progress 分别是否 raw-equal / canonical-equal；
- 结构化差异的 JSON path、差异类别、值类型和最小必要值摘要；
- profile/current-profile identity 的相关字段；
- candidate mock store 的所有 read/write/delete audit。

不得把完整用户数据或不必要的标识符写入报告。值只保留判断差异所需的最小摘要；敏感或体积较大内容使用类型、长度和 hash。

### Candidate B：live-seeded

按 Phase 0 确认的正式序列化格式，把当前 live Settings/Prefs/Progress 的序列化副本写入一个全新的 mock store，完成正常 candidate 初始化，然后记录与 Candidate A 相同的逐组件结果。

Candidate B 必须额外证明：

- candidate 与 live manager/三个顶层 save object 均不是同一引用；
- 已识别的嵌套可变 collection 不共享引用；
- candidate 初始化和比较前后，live 三组件 canonical hash 不变；
- mock store 的写入只发生在 candidate 自己的 memory store；若 Init 发生 normalization write，逐项列出 path、时机和 payload hash。

禁止为了通过 gate 而在初始化后直接把 live save object 或嵌套 collection 赋给 candidate。

## 人工测试请求格式

完成构建与静态门禁后，Implementation Codex 只能向项目发起人请求以下一次最小运行：

1. 确认游戏为 v0.111.0、Steam Cloud 关闭、当前为模组存档 1 号。
2. 只启用新的 SPIKE-INV004B probe；关闭旧 probe、RunReplays、BaseLib、RitsuLib 与其他 gameplay mod。
3. 启动游戏，停留主菜单，确认 Continue 可见；不进入 Continue、单人游戏、多人游戏或任何战斗。
4. 触发 probe 提供的唯一诊断入口并等待其显示完成/停止状态。
5. 达到结果后立即停止操作；不切换 profile，不进入任何 run，不领取任何奖励。按 Implementation Codex 指示正常退出或保持主菜单以便先收集日志。

请求中必须明确目标、预期 UI/日志结果、失败时立即停止条件、目标后动作和需要用户回传的最小信息。不得要求项目发起人自行解释 JSON 或进行文件操作。

## 运行前后保护证据

必须沿用 `TEST_PROTOCOL.md`：

- 验证 active profile 为 profile1，但不枚举或读取其他 profile。
- 记录原 manager identity 与 live 三组件 canonical hash。
- 对既有 12 个受保护文件记录 path、存在性、长度、mtime、SHA-256；运行后逐项比较。
- 记录受保护目录 inventory；不得把 probe 自己的 evidence/log 目录混入保护集合。
- 确认运行前后 `RunManager.State == null`、Continue 可见、原 manager identity 未变化。
- 保存 probe audit；不得把“未安装 audit”写成 audited zero calls。

任何受保护文件/inventory 变化、live manager/object mutation、真实 store write/delete、run state 建立或 scene transition，立即 `FAIL_SAFETY` 并停止，不得 rebaseline 或重试。

## 结果分类

### `PASS_DIAGNOSED`

仅当同时满足：

- Candidate A 的差异已定位到具体组件和 JSON path，并能区分真实值差异与 raw JSON 表示差异；
- 对差异来源有方法体证据或明确保留为推断；
- Candidate B 完整初始化后与当前 live Settings/Prefs/Progress 逐组件 canonical-equal；
- Candidate B 与 live 状态通过规定的引用独立性检查；
- 所有安全保护项通过。

### `PARTIAL_DIAGNOSED`

具体差异已定位，但 Candidate B 无法同时达到状态等价与引用独立，或差异来源仍有多个无法区分的假设。该结果不得解锁 direct-write sentinel。

### `BLOCKED_IDENTITY` / `BLOCKED_PRECONDITION`

固定构建、profile、模组、Cloud、mock-store containment 或静态前提不满足；不得运行候选构造。

### `FAIL_SAFETY`

出现任何 live mutation、真实 store/write/delete、受保护文件变化、run/scene 建立或其他越界副作用。首个失败即停止，不得重试。

## 交付物

1. `project-management/investigation-reports/SPIKE-INV004B-report.md`
2. `evidence/spike-inv004b/README.md`
3. 单份机器可读结果，至少包含：固定身份、逐组件 raw/canonical hash、结构 diff、引用独立性、candidate mock I/O audit、live before/after、保护文件与 inventory 对比、结果分类和首个失败点
4. `experiments/SPIKE-INV004B/README.md` 与最小 probe 源码/构建说明
5. 静态调用链证据，明确哪些是 Confirmed、Inferred、Unknown

报告末尾必须只推荐一个下一步：若为 `PASS_DIAGNOSED`，建议 Project Lead 编写新的 direct-write sentinel；否则建议一个比重试 SPIKE-INV004A 更窄的静态调查或候选否决点。

## 明确非目标

- 不重试或继续 SPIKE-INV004A。
- 不安装 `SaveManager` override。
- 不进入 Replay、Continue 或战斗。
- 不调用任何 progress update/save sentinel。
- 不测试自然胜利、achievement、fault/restart 或 replay dispatcher。
- 不实现正式 Replay UI、recorder/player、快照、seek、格式或架构。
- 不引入 RitsuLib。
- 不访问其他 profile。

## 停止条件

- DLL/build/profile/mod/Cloud 身份不符。
- 不能证明 candidate I/O 完全封闭于 mock store。
- 需要安装全局 override 或建立 run state 才能取得差异。
- 首次发生真实 store/write/delete、live object mutation、scene/run state 变化或受保护文件变化。
- 已得到上述任一最终结果；不得在同一工作包中继续修 direct write。
