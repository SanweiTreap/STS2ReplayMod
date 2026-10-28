# SPIKE-INV003A Implementation Codex Handoff

> 历史工作包，已于 2026-09-08 结束。2026-09-10 根据用户澄清修正解释：本文所称 protected ordinary Continue 实际只是 profile1 的测试用隔离哨兵，不是用户其他槽中的真实普通对局；结论范围与后续 prompt 以 D-030、D-032 和 `TEST_PROTOCOL.md` 为准。不得重新执行本工作包。

状态：Complete；实验结果 FAIL  
发出日期：2026-09-04  
父任务：`INV-003`  
任务类型：受限运行实验；Implementation Codex 构建 probe，用户执行游戏内步骤

## 任务目标

只验证一个候选机制：

> 在主菜单进入 playback 前，事务式安装完整、深拷贝初始化的 in-memory `SaveManager`；用 `RunManager.SetUpReplay` 建立最窄 native replay session，随后强制 `RunManager.ShouldSave=false`；所有正常/异常退出统一按顺序等待隔离任务、清理 run、恢复原 manager，最后才重建主菜单。

实验要证明该机制是否能让 Replay 管理入口与播放 session 同普通 Continue 共存，而不对 ordinary current run、progress、history、stats、achievement、daily、leaderboard、metrics 或 cloud 产生 write/delete/submit 副作用。

本任务不证明任意 Replay 重建、玩家操作重放或最终架构。

## 开始前必须阅读

1. `project-management/CURRENT_STATUS.md`
2. `project-management/MVP.md`，重点 `PLAY-SESSION-001` 与 G-11
3. `project-management/EXPERIMENT_PLAN.md`
4. `project-management/investigation-reports/INV-003-report.md`
5. `project-management/investigation-reports/INV-002-report.md`
6. `project-management/investigation-reports/SPIKE-001A-report.md`
7. `project-management/HANDOFF_TEMPLATE.md`
8. `project-management/TEST_PROTOCOL.md`

`SPIKE-001A2.md` 已暂停，不属于本任务。不要合并 room-before reconstruction A/B。

## 固定环境

- 游戏：public-beta `v0.111.0`，commit `41cef1ea`
- `sts2.dll` SHA-256：`0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`
- 测试 profile：固定为模组存档 1 号；该 profile 已完成全内容解锁
- Steam Cloud：必须由用户在当轮测试前确认关闭；不能确认则停止
- RitsuLib：不安装、不启用、不引用
- 其他 gameplay mods：不允许
- 允许：本 probe 及加载/patch 所需的最小官方运行依赖；记录名称、版本与 DLL hash

版本/hash、模组列表或 Cloud 状态不符时，不运行实验。

## 受保护测试 profile

本实验固定使用项目发起人指定的 **模组存档 1 号**。不得自行创建、选择或切换其他 profile。

该 profile 已完成全内容解锁，是项目的长期测试环境。Implementation Codex 应请求用户在其中建立或指定一局真实、可由主菜单 Continue 的普通单人对局，作为本实验的 **protected ordinary Continue**：

- Continue 可以专为本实验通过正常单人流程生成；不得修改或重置该 profile 的全内容解锁进度；
- 进入 Replay 管理/播放后全程不切换 profile；
- 不允许另外切到“Replay profile”规避问题；
- probe 不得创建、删除、复制、重置或切换 profile；游戏内选择与操作全部由用户完成；
- “模组存档 1 号”是用户可见身份，实际 profile id/path 必须在 Phase 0 只读解析，不能硬编码猜测。

Implementation Codex 先交付只读 preflight，用户返回证据后才能继续运行阶段。

## 人工测试交互约束

- 禁止使用 Computer Use 组件或任何 GUI/鼠标/键盘自动化启动、控制 STS2。
- Implementation Codex 负责构建 probe、准备文件、提出测试需求并分析证据；项目发起人负责打开游戏和执行所有游戏内动作。
- 每次测试请求必须按 `TEST_PROTOCOL.md` 给出：测试目标、开始前条件、具体动作、预期结果、停止条件、需回传证据和禁止动作。
- fault matrix 按 phase 逐次解锁。收到并验证上一 phase 的用户观察和 probe evidence 前，不得要求执行下一 phase。

## Phase 0：保护基线与硬停止条件

只读收集并写入 mod-owned evidence：

1. active profile id、规范化 profile/save 根目录和 `RunManager.State == null`。
2. 主菜单 Continue 可见；解析其 `SerializableRun`，记录 act/floor/map coord、seed、`NumReloads` 和非敏感摘要。
3. 枚举该 profile 下 ordinary current-run/backup、progress、profile/preferences、run history、stats/achievement 等相关文件；对每个记录规范化路径、SHA-256、长度和 UTC mtime。
4. 记录相关目录的文件名 inventory，便于检测新增/删除；不得递归扫描无关用户目录。
5. 创建一份仅来自该测试 profile 的恢复副本到 `evidence/spike-inv003a/protected-backup/`，记录来源与 hash。不得备份其他 profile；报告交付前注明其可能包含测试 run 数据。
6. 记录真实 `SaveManager.Instance` 对象身份、store 类型、profile id provider、current run path、save task 状态和 cloud wrapper 状态。
7. 记录启用模组及 artifact hash。

以下任一情况立即 `BLOCKED`，不得安装 transaction：

- active profile 不是用户确认的模组存档 1 号，或无法把其用户可见编号与实际 id/path 对应；
- Continue 不存在或无法解析；
- active profile/path 不能唯一确定；
- Cloud 未关闭；
- 发现目标版本或模组环境漂移；
- 不能建立完整 ordinary 文件/inventory 基线；
- 游戏当前已有 `RunManager.State`；
- 任何 save task 未完成且无法只读等待。

Phase 0 不修改任何游戏 save/profile。备份只复制到本任务 evidence，不写回。

## Phase 1：最小 probe 实现

在 `experiments/SPIKE-INV003A/` 创建 disposable probe。优先复用既有项目的版本/hash gate、日志与 snapshot 工具，但不要修改 SPIKE-001A 源码。

### 1. 主菜单入口

- 在 `NMainMenu._Ready` 后向与 Singleplayer/Multiplayer 相同的 `MainMenuTextButtons` peer group 注入一个临时 `Replay Isolation Probe` 按钮。
- 按钮必须有确定的 signal disconnect、node cleanup、focus/layout/visibility 恢复。
- 点击后先打开最小 fixture 页面；管理页打开/关闭期间断言 `RunManager.State == null`，不得安装 fake manager。
- 这是入口/生命周期 probe，不是产品 UI。不要实现 replay 列表、导入、删除、路线图或时间轴。

### 2. Transactional ReplaySaveContext

实现单实例、不可嵌套、不可并发的 transaction：

1. 捕获并记录原 `SaveManager.Instance` 身份。
2. 使用 memory-backed `ISaveStore` 创建完整可运行的隔离 `SaveManager`。
3. 隔离 manager 所需 Settings/Prefs/Profile/Progress 等对象必须通过序列化 round-trip 或等价深拷贝初始化；不得与原 manager 共享可变引用。
4. 在调用任何 replay setup 前，通过游戏明确提供的 override 构件安装隔离 manager；安装后断言 `SaveManager.Instance` 为隔离对象。
5. 调用最窄 `RunManager.SetUpReplay`/native `CombatReplay` fixture；setup 返回后立即设置并断言 `RunManager.ShouldSave=false`。
6. 所有代码路径都进入同一个 `try/finally` owner；禁止 fire-and-forget teardown。
7. transaction 活跃期间拒绝第二次启动、profile change、normal Continue、Singleplayer/Multiplayer 和 Quit；UI 给出 probe 状态，而不是让请求落入真实 store。

不得 patch 掉具体 save call 来制造通过。核心机制必须是 memory manager 接住 direct writes，`ShouldSave=false` 阻止非必要 run-end 路径；patch 只可用于观测、故障注入和 fail-closed gate。

### 3. 单一 ordered teardown

所有 Stop、setup failure、post-launch failure 和返回主菜单共用：

1. 锁定 transaction，拒绝新的 run/save 操作。
2. 停止/断开 probe-owned replay callbacks，不新增 dispatcher。
3. 等待并观察隔离 manager 的 current save/progress tasks；异常进入报告，不可吞掉。
4. 若 `RunManager.State != null`，调用一次完整 `RunManager.CleanUp(true)`；等待其同步/异步后果稳定。
5. 再次等待隔离 save tasks，检查 run/service/synchronizer/scene 残留。
6. 清除 override，恢复并断言原 `SaveManager.Instance` 对象身份。
7. 只有原 manager 恢复后，才允许 `NGame.LoadMainMenu(false)` 创建新菜单。
8. 新 `NMainMenu._Ready/RefreshButtons` 后验证 Continue 缓存来自原 manager。

teardown 任一步异常都必须继续尝试恢复原 manager，同时标记本 phase FAIL；不得在失败后启动下一 phase。

## Audit 与 fail-closed 观测

建立结构化 counters/event log，至少覆盖：

- 原 manager/store 的 write、delete、batch/save task；
- 隔离 manager/store 的 write/delete，并标注逻辑目标；
- `SaveRun`、`SaveProgressFile`、`UpdateProgressAfterCombatWon`；
- run history、stats、metrics/mod metrics；
- daily/leaderboard/network submit；
- platform achievement 调用边界；
- cloud store/wrapper 写入或同步；
- profile change、manager identity change；
- `RunManager.State`、`ShouldSave`、scene 与关键 synchronizer 生命周期。

Audit patch 不改变原方法返回值或业务分支。任何原 store write/delete、外部 submit、profile change 或未知 manager identity 立即触发 fail-closed teardown。

日志只允许写入 `evidence/spike-inv003a/runtime/`；避免每帧同步写盘，只记录边界事件和最终 counters。

## Native fixture 限制

- 使用最窄、可追溯的 native `CombatReplay`/游戏已有 replay event path。
- fixture 来源、版本、内容、hash 和构造方式必须记录。
- 不实现 Replay Mod 自己的 capture、parser 或 dispatcher。
- 不从普通 `current_run.save` 生成中途 fixture。
- 如果没有办法在不实现新 replay 系统的前提下得到合法 native fixture，报告 `BLOCKED_FIXTURE`，不得扩大范围。

## Phase 2：有界 fault matrix

由用户按 Implementation Codex 给出的逐步清单执行；Implementation Codex 不得打开或自动控制游戏。每一项开始前必须重新确认 active profile 与保护基线；按顺序分轮请求，首个失败后停止。

### A. Management-only

- 打开/关闭 probe fixture 页面，不建立 run。
- 必须始终满足 `RunManager.State == null`、真实 manager 身份不变、ordinary inventory 不变。

### B. Watch / normal Stop

- 启动 transaction 和最窄 fixture，达到首个可观看 scene 后立即 Stop。
- 执行唯一 teardown，回到新主菜单并验证 Continue。

### C. Direct-write victory sentinel

- 重新启动同一 transaction/fixture，只通过游戏已有 native replay event path 达成一次 combat victory。
- 必须观察到 `UpdateProgressAfterCombatWon`/`SaveProgressFile` 尝试落入隔离 manager；原 manager/store 和外部 submit counters 必须为 0。
- 达成观测后立即 teardown，不进入奖励、地图或下一房间。

### D. Setup failure

- 在 `SetUpReplay` 已赋 `RunManager.State`、但 scene launch 前注入一次明确、可重复的 probe exception。
- 验证相同 finally teardown 恢复原 manager 和 Continue。

### E. Post-launch failure

- launch 后、首个可观看 scene 稳定时在 probe owner 边界注入一次异常。
- 不经过自定义 dispatcher；验证相同 teardown。

### F. Restart check

- 所有前述 phase 通过并正常退出游戏后，由用户重新启动。
- 在未进入新 run 前重新采集保护 inventory、Continue 摘要、`NumReloads` 和原 manager/store 身份。

这些 phase 是同一 transaction 机制的故障覆盖，不是多个候选。每个 phase 可以独立重启游戏以清除测试内存，但不得切换 profile。

## PASS / FAIL / BLOCKED

### PASS

必须全部满足：

- Management-only 期间从未建立 run 或替换 manager。
- 每个 playback/failure phase 原 manager/store write/delete counter 为 0；外部 submit counter 为 0。
- victory sentinel 明确在隔离 manager 中观察到 direct progress save attempt。
- 每次 teardown 后 `RunManager.State == null`，原 manager 对象身份恢复，隔离任务均完成且无未观察异常。
- 原 manager 恢复早于新主菜单创建；Continue 可见且反序列化摘要等于 baseline。
- ordinary 文件逐字节 hash、长度、mtime、`NumReloads`、全内容解锁状态和目录 inventory 完全不变；重启后仍相同。
- 未产生 run history、stats、achievement、daily/leaderboard、metrics 或 cloud 副作用。
- 只产生明确列出的 probe/evidence 文件。

### FAIL

以下任一即失败并停止：

- 任意 ordinary 文件/inventory/reload count/Continue 内容变化；
- 原 store write/delete 或任意外部 submit；
- direct-write sentinel 未命中隔离 manager；
- manager 恢复顺序错误、身份错误或新菜单缓存 fake Continue；
- cleanup 后残留 state、scene、synchronizer、callback 或挂起 task；
- 异常越过 transaction owner、未进入 finally 或使游戏无法安全回菜单；
- transaction 期间发生 profile change、嵌套 session 或真实 Quit save。

不要修补后继续矩阵；记录首个失败边界并交回 Project Lead。

### BLOCKED

- Phase 0 无法建立保护基线；
- 无合法 native fixture；
- 无法构造完整、深拷贝的 memory SaveManager；
- audit 不能覆盖已知 direct write/submit 边界；
- 目标版本、依赖或 API 与固定身份不符。

Blocked 时不得退回 disposable Replay profile、普通 saved setup 或 patch-away-every-write 方案。

## 明确非目标

- 不使用或评测 RitsuLib。
- 不执行 SPIKE-001A2 room-before A/B。
- 不 capture/dispatch 玩家操作，不实现 Replay Mod command stream。
- 不验证任意 singleplayer baseline、跨房间、奖励或完整对局播放。
- 不实现正式 Replay 管理页、配置、文件格式、纠偏、时间轴、seek、后退或多人。
- 不把测试 API `MockInstanceForTesting` 直接宣布为 production 架构。
- 不修改或写回 ordinary protected save；恢复备份只用于用户在实验外手动灾难恢复，不得作为 PASS 流程一部分。

## 交付物

- probe 源码和构建说明：`experiments/SPIKE-INV003A/`
- runtime/preflight/fault evidence：`evidence/spike-inv003a/`
- 报告：`project-management/investigation-reports/SPIKE-INV003A-report.md`

报告必须列出：固定身份、实际启用模组、fixture 来源、transaction 对象图、深拷贝策略、audit 覆盖矩阵、各 phase 操作与 counters、所有保护文件前后 identity、Continue 摘要、teardown 顺序、首个失败或完整 PASS，以及仍未知的 side channel。

Implementation Codex 不得修改 `CURRENT_STATUS.md`、`ROADMAP.md`、`TODO.md`、`DECISIONS.md`、`MVP.md`、`EXPERIMENT_PLAN.md` 或其他已验收报告。

## 验收停止条件

- 得到一个完整 PASS，或首个可审计 FAIL/BLOCKED；
- 模组存档 1 号的 ordinary Continue、全内容解锁进度和其他受保护状态未被实验写入；
- 没有进入 reconstruction、capture/dispatch 或产品 UI；
- 报告已落盘。

满足后立即停止，不执行 SPIKE-001A2 或 SPIKE-001B。
