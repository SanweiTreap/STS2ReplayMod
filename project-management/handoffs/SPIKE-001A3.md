# SPIKE-001A3 Implementation Codex Handoff

状态：Complete — `BLOCKED_CHECKPOINT_VALIDATION`；永久退役  
发出日期：2026-09-17  
任务类型：受限运行实验；全新 reconstruction control

## 任务目标

在 STS2 public-beta `v0.111.0`、模组 profile1 和一个固定的第一幕普通弱怪节点中，比较：

- **A**：production 完成房间前 `SaveRun(null, true)` 后，不中断地进入该战斗并到达首个稳定可操作点；
- **B**：新进程从 A 所使用的同一房间前 checkpoint，通过主菜单正常 `Continue` 重建该战斗并到达同一稳定可操作点。

本任务只回答 A/B reconstruction equivalence，并同时修复旧 R3 的 checkpoint-baseline 时点和 readiness 可观测性缺口。它不 capture/dispatch 玩家操作，不实现 Replay 会话、快速重演、强记录、纠偏、seek、后退或正式架构。

## 开始前必须阅读

1. `project-management/CURRENT_STATUS.md`
2. `project-management/TEST_PROTOCOL.md`
3. `project-management/investigation-reports/INV-002-report.md`
4. `project-management/investigation-reports/SPIKE-001A2-R2-report.md`
5. `evidence/spike-001a2-r2/project-lead-final-review.md`
6. `project-management/investigation-reports/INV-010-report.md`
7. `evidence/inv-010/reconstruction-contract.json`
8. `evidence/inv-010/lifecycle-readiness-matrix.md`
9. `evidence/inv-010/README.md`

旧 `SPIKE001A2R2` 只可作为只读历史证据，不得修改、重建、安装、启用或运行。

## 固定 identity

- 游戏：STS2 public-beta `v0.111.0`，commit `41cef1ea`。
- `sts2.dll`：9,757,184 bytes；SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`。
- INV-010 report：SHA-256 `59D52227F5146253CEF41C5E93EDBC5AC4195EEE754CD4DCE6F39000F5FC61E0`。
- INV-010 contract：SHA-256 `54D99A392E5B113E78F770984100549E486A19D06145469E1D22BC54F1B02CF1`。
- 新 mod ID：`SPIKE001A3`。
- 新 schema：`spike-001a3.reconstruction-control.v1`。
- artifact/recovery namespace：`spike-001a3-r1-{attemptGuid}`；所有运行文件 create-new、不可覆盖。

任一固定输入不符时停止为 `BLOCKED_IDENTITY`。不得复制 001A2-R2 的 runtime ID、schema、artifact、recovery 或安装文件名。

## 固定安全边界

- 只允许 `modded/profile1`。物理路径必须由当次 production API 解析；不得读取、枚举、比较或切换其他 profile。
- Steam Cloud 必须满足既有 production conjunction 为 off。
- 正式运行时 loaded mod set 必须精确为 `[SPIKE001A3]`；所有旧项目 probe 必须 disabled。
- 不使用 Computer Use、GUI 自动化或模拟输入。游戏只能由项目发起人手动启动与点击。
- 不 patch 掉 production save，不写回 checkpoint，不 restore、不 rebaseline、不修改 production save 语义。
- recovery 只用于不可覆盖的证据与离线校验，本工作包绝不授权恢复。
- profile1 Continue 是测试哨兵，不是其他 profile 中的真实普通对局；结果不得外推到其他槽。

## Phase 0：实现与离线门禁

在 `experiments/SPIKE-001A3/` 创建全新 probe；证据写入 `evidence/spike-001a3/`。先完成静态/离线验证，不得自行启动游戏。

必须实现并验证：

1. **透明 checkpoint observer**：只在已 arm、目标身份匹配且参数为 `preFinishedRoom == null && saveProgress == true` 时包装 production `SaveManager.SaveRun(AbstractRoom,bool)` 返回 Task。必须先 await 原 Task；observer 自身完成前 stable-read、解析并封存 checkpoint/baseline，随后才允许 `EnterMapPointInternal` 继续。
2. **写成功验证**：Task completed 不是成功充分条件。必须要求 `current_run.save` 存在、两次稳定读取 identity 相等、可解析、seed/character/act/map 与 arm target 相符、`PreFinishedRoom == null`，且内容 identity 不等于 arm 时的 non-null save。
3. **原子 baseline**：在同一临界区封存 checkpoint raw/canonical hash、length、`NumReloads`、完整 allowlisted profile1 protection inventory/baseline。正式保护窗口从该 baseline 开始。
4. **独立 deadline**：观察到原 production save Task 完成时启动 30 秒 monotonic capture deadline；baseline 验证耗时包含在内。deadline 必须由独立 owner Task/timer 驱动，不依赖 ticker。人工 arm 后等待用户点击永不超时。
5. **持久 ticker**：直接挂到 `NGame`，显式 `SetProcess(true)`、effective `ProcessMode=Always`。记录 attach、parent/path、instance id、inside-tree、process enabled/effective mode、tree paused、首个全局/baseline后/combat tick、总 tick/combat tick、last frame/time，以及 enter/exit/tree-exiting 生命周期。
6. **逐字段 telemetry**：每个 readiness 字段独立读取为 `value/null/exception`；异常记录 site、完整类型、message、count、first/last tick/time。不得 catch-all 后只返回 null。
7. **有界采样**：记录首样本、字段状态变化、异常、每 15 tick 周期样本及 terminal 前最后样本；设置固定事件上限与 compact/drop counter，不得无限写日志。
8. **30 连续帧**：严格实现 INV-010 contract 的 required fields、identity stability 与 continuity breakers。engine process-frame 不连续、任一 required false/null/exception 或 target identity 变化均将 quiet count 重置为 0，并记录 breaker。第 30 帧同 tick 重读完整 vector，仍满足才捕获 snapshot。
9. **独立 terminal writer**：即使 ticker 停止，deadline 也必须 create-new 写 terminal，包含 baseline identity、heartbeat、最后 raw sample、transition/counter、exception summaries 和明确 failure site。UI 不得在 terminal 后继续显示 waiting。
10. **RoomEntered guard**：若目标 `RunManager.RoomEntered` 先于已验证 baseline，立即 `BLOCKED_CHECKPOINT_OBSERVATION_MISSED`；不得事后补 baseline。

离线测试至少覆盖：

- INV-010 20 项 contract tests 原样继续通过；
- ticker 零 tick/中途停止时独立 deadline 仍落盘；
- 每个 required read site 的 null/exception、quiet reset 与 bounded aggregation；
- 人工 Ready 超过 120 秒不超时；
- production save Task fault/cancel/内部吞错但 checkpoint 缺失或不可解析；
- RoomEntered-before-baseline 竞态；
- terminal 与 UI first-terminal-wins；
- create-new artifact/recovery 冲突；
- 使用真实固定前驱 schema 的正向 preflight test，而非只用手工简化对象。

Release build、source/IL audit、synthetic tests 全部通过后，先回传构建结果、安装计划和精确人工准备请求。新 probe 的构建与安装是两个 gate；必须依照 `TEST_PROTOCOL.md` 先确认旧 probe disabled，再安装 A3。

## 有限 harness repair envelope

Implementation Codex 可以在**同一个 Codex task 和同一 A3 lineage**内修改、测试、构建和替换当前 probe，无需为每个纯 harness 缺陷申请新工作包，但只限：

- loaded-mod final-set 观察点、precheck UI/Ready 展示；
- ticker/heartbeat/独立 deadline/terminal writer 的实现缺陷；
- artifact create-new、attempt identity、离线 validator 或报告工具缺陷；
- checkpoint observer 未命中、错误重载选择或 telemetry 字段读取错误，前提是尚未观察到匹配的 production save Task completion、尚未建立正式 baseline，且没有 recovery/业务窗口。

最多允许 **2 次 pre-business code revision**。每次必须保留旧 artifact/log、提升 build/attempt identity、增加复现该缺陷的 regression test、重新通过全部离线门禁，并只在游戏退出且 installed pair hash 精确匹配时替换 A3 自己的文件。

以下任一发生后 repair envelope 立即终止：匹配的 production save Task completion 被观察到、正式 baseline/recovery 已建立、A snapshot 开始、任何保护差异、结果不明，或需要改变场景、保护集合、允许 I/O、PASS 标准与业务假设。此后不得自行修补或重跑，必须交回 Project Lead。

纯 wrong-mod-set/configuration precheck 必须使用 attempt-specific precheck evidence，不能占用业务 artifact；只有可证明未读取 profile/protection、未建立 recovery、未观察业务事件时，才可按 `TEST_PROTOCOL.md` 请求用户修正 enabled 状态。

## Phase A：uninterrupted snapshot

### fixture 准备

用户可以在**arm 和正式 baseline 之前**于 profile1 准备一个第一幕普通弱怪节点；准备过程不属于受测窗口。Implementation Codex 必须明确告诉用户哪些奖励/节点操作属于准备、何时停止。不得假定当前 Continue 已处于任何特定位置。

只有用户停在目标节点点击前的地图、probe 显示可 arm 时，才允许 arm。arm 仅记录目标与 pre-baseline observation，不建立正式 protection baseline；等待用户点击无 timeout。

### 用户动作

用户只点击已记录的普通弱怪节点，随后立即停止所有输入：不出牌、不 End Turn、不打开选择界面、不暂停、不领取奖励、不进入下一节点。

### 自动观察与 A capture

checkpoint observer 必须在 production room-before save 的 await 边界封存正式 baseline，之后由 ticker 等待同一 target 的 30 个连续 quiet frames并捕获 A。

A snapshot 至少包括：

- act、floor、map coord、room/encounter stable identity；
- player stable identity、HP/max HP/energy；
- hand/draw/discard/exhaust 的有序 card identity及可取得的实例语义字段；
- 敌人 stable identity、CombatId、HP/max HP、alive/dead/escaping；
- 完整可取得的 `RunRngSet` entry stable name、state/counter/digest；
- raw readiness vector、object identities、quiet-frame起止 tick/frame与 snapshot tick/frame。

A 成功后 UI 明确要求保持不操作并正常退出游戏。不得使用 Save & Quit、不得打牌或领奖。

游戏退出后，Implementation Codex 只读验证 A 使用的 `current_run.save` 内容与正式 checkpoint baseline **字节 identity 精确一致**，并审计完整 protection inventory。mtime-only 不自动接受，必须按 `TEST_PROTOCOL.md` 分项报告。若 source 内容/路径/inventory 改变，终止为 `BLOCKED_SOURCE_CHANGED_AFTER_A`；不得恢复、复制回或重建 baseline。

只有 A 完整且 A-post 审计通过，才解锁 Phase B。

## Phase B：normal Continue snapshot

使用全新进程。在用户点击 Continue 前，probe 必须确认：

- exact game/DLL/probe/loaded-mod identity；
- active profile1 与 Cloud-off；
- checkpoint hash/length/raw bytes 与 A baseline一致；
- protection inventory满足 A-post 允许边界；
- 没有现存 run/combat state，且本 attempt 尚未消费 Continue。

用户只点击主菜单正常 `Continue` 一次，随后停止所有输入。probe 不得自行调用 setup/load、模拟按钮或改变 save。

在与 A 相同的 target identity 和 30 连续 quiet-frame contract 下捕获 B。捕获后立即要求用户不出牌、不 End Turn、不打开界面、不领奖，并正常退出游戏。

production Continue 的已知允许持久变化仅为：

- profile1 `current_run.save` 的 `NumReloads` 精确增加 1；
- Phase 0 已由固定 schema/IL 证明并在报告中预先列出的非游戏语义序列化元数据变化；不得事后扩表。

必须解析 baseline/B save 并证明除此之外 canonical-equal。`current_run.save` 不得从保护集合移除；其他 profile1 文件内容/length/inventory 不得变化。正常退出后的 mtime 只按 `TEST_PROTOCOL.md` 固定白名单解释。

## A/B 判定

比较前唯一允许排除：`NumReloads`、wall-clock、probe artifact/log sequence和纯观察 tick/frame。不得看见 diff 后扩大排除项。

- `PASS_RECONSTRUCTION_EQUIVALENT`：A/B 所有规定游戏语义字段、card/enemy identity和完整可取得 RNG digest相等；B save 仅含预声明允许差异；profile1 protection通过。
- `FAIL_RECONSTRUCTION_DIFF`：环境与协议有效，但任一规定游戏语义字段不同。写首个及完整 diff并停止。
- `BLOCKED_CHECKPOINT_*`：未在批准边界验证并原子封存正确 checkpoint/baseline。
- `BLOCKED_CAPTURE_DEADLINE`：baseline后30秒未捕获A/B；必须包含完整 bounded telemetry，不能只报 timeout。
- `BLOCKED_SOURCE_CHANGED_AFTER_A`：A后无法证明同一checkpoint仍可供B使用。
- `FAIL_ISOLATION_OR_PROTOCOL`：正式baseline后出现非白名单持久变化、路径/profile漂移或额外gameplay操作。
- `BLOCKED_ENVIRONMENT`：版本、hash、Cloud、loaded mod、profile或前驱证据门禁不满足。
- `FAIL_HARNESS_AFTER_BUSINESS_BOUNDARY`：正式业务边界后probe自身失败或证据不完整；不得把它写成production reconstruction FAIL。

只允许一个有效 A/B compare。任一业务 terminal 后永久停止 A3；不得重跑、restore、rebaseline或自行进入 `SPIKE-001B`。

## 明确非目标

- 不验证 Replay transaction、direct progress write、fault/restart或强制 crash。
- 不测试中途战斗恢复、第二种房间、自然胜利、奖励、事件、商店或Boss。
- 不 capture/dispatch玩家操作，不实现正式Replay文件、UI、性能优化、时间戳、强记录、纠偏、前进/后退或seek。
- 不读取/切换其他profile，不调查无直接因果的`Invalid Task ID`或旁支问题。
- PASS不自动解锁`SPIKE-001B`；只交回Project Lead复审。

## 交付物

- 源码、build与运行说明：`experiments/SPIKE-001A3/`
- 不可覆盖证据：`evidence/spike-001a3/`
- 报告：`project-management/investigation-reports/SPIKE-001A3-report.md`

报告必须区分 Confirmed / Inferred / Unknown，并列出：固定identity、所有用户动作、每个attempt/revision、checkpoint命中与baseline原子边界、driver heartbeat、raw readiness与exception summaries、A/B snapshots及完整diff、save canonical diff、protection审计、偏离与停止原因。

Implementation Codex 不得修改 `CURRENT_STATUS.md`、`ROADMAP.md`、`TODO.md`、`DECISIONS.md`、`MVP.md` 或 `EXPERIMENT_PLAN.md`。

## 分阶段交付与停止

Phase 0 完成后先回传静态验证、安装/启用生命周期计划及第一轮精确人工请求；不得自行启动游戏。每次只请求当前已解锁 phase，并严格按 `TEST_PROTOCOL.md` 写清动作、目标、预期、失败停止条件、回传证据、禁止动作和目标后动作。

任一 terminal、结果不明或 repair envelope终止后立即停止并交回Project Lead。
