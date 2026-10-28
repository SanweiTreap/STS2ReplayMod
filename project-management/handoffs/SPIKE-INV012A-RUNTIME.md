# SPIKE-INV012A — 安装、配置与窄 runtime 诊断

签发：Project Lead，2026-10-09。状态：`COMPLETE / CLOSEOUT_ACCEPTED`。runtime-r1窄诊断及收尾均获PM验收：17份记录归档hash通过，两个精确安装文件已移除，源码/候选/原始证据可恢复；settings的enabled条目未改，不宣称持久disabled或当前modded配置有效。下文保留已执行合同，不据此重复运行。收尾复核见`evidence/spike-001a5/project-lead-phase0-review-2026-10-09.md`。

## 只回答一个问题

在一次 Act 1 普通 Monster 入房后，原进程 ticker 是否持续运行、20 个 readiness 字段是否达到 30 连续帧，以及正常退出时两条已列明的 native writer 是否被调用。允许得到可解释的否定/诊断结果；**不做 A/B、不实现恢复/seek、不证明 isolation 或 crash recovery**。不声称复现了 A4 原进程首因。

先读 CURRENT_STATUS、TEST_PROTOCOL、WORKFLOW、PREP 验收和本包；按需查 r6 源码/Phase 0 审计，不完整重读所有历史。继续同一 SPIKE-INV012A 实现对话。PM 负责权威状态文件，Implementation 只更新本任务源码、报告和证据。

## 固定初始候选与现状

- 游戏：v0.111.0 / 41cef1ea；`sts2.dll` 9,757,184 bytes，SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`；Harmony 2.4.2.0。
- 初始候选：`experiments/SPIKE-INV012A/artifacts-phase0-r6/SPIKEINV012A.dll`，52,224 bytes，SHA-256 `CA1DAF85B5C12AE043A2BF20E7D9026DA894D336F649913A7E1D1BE2C69C1720`；同目录 manifest SHA-256 `6B35514CFE0ABF7EEDC7B137759D7D9DB8BB4A2D737809AFC986C1F39D1169FD`。
- PREP 已通过：modded profile1、AscensionUnlocker enabled、旧探针 disabled、游戏退出、r6 未安装。每次操作前仍核对当下状态，不能用 10 月 9 日摘要代替现场配置。
- 不改旧 Phase 0-r6 封存产物；有授权修复则新增产物目录/build identity，不覆盖旧候选/证据。旧 A2/A3/A4、007A、008A 等均不运行、不修订。

## 个人数据与文件范围

唯一个人保护槽是 **unmodded profile1**：不得读取、复制、改写、恢复、清理或进入。当前实验仅用 modded profile1；本包不需要 modded2/3。

允许的 production 状态变化是用户在测试槽新建 Standard 单人对局、完成必要的起始选择、点击目标入房以及正常退出的原生写入。它们可以改变 `current_run/progress/prefs`、native `replays/latest.mcr` 及其 backup/temp；**不作为本诊断的自动 isolation FAIL**。不要建立十二份 recovery 副本或跨历史 raw-hash 恒等门，本包不测 transaction isolation。准备时 account `settings.save` 的模组开关及 modded `profile.save` 可因明确步骤变化；不随意改显示设置/其他字段。

只读检查 account settings 的 enabled IDs/mods flag、modded selector 和必要的 profile1 文件 metadata/hash；不导出 production payload，不为诊断额外读取 save/replay 内容。实际 `user://` 从 production 映射/当次证据核对，不能硬编码账号数字。探针证据仍限 `user://SPIKEINV012A/<fresh-attempt>/NN.json`，create-new、最多 16 份且每份 4 KiB，白名单/schema 不变。仓库证据只归档过滤后的本任务记录和有关日志，不保留账号、本机路径或存档 payload。

## A. 安装与模组配置（非业务会话）

1. 核对游戏进程=0、固定身份、profile selector=1、modded 开启且 AscensionUnlocker enabled。检查 live `mods` 精确同名 pair 不存在；未知冲突先停，不覆盖。Steam Cloud 现场关闭是**下一次游戏启动**的门，不阻止不启动游戏、不访问存档的离线安装；安装完成后将Cloud确认与第一轮人工启动步骤一次性提出。
2. 只安装上列 r6 DLL/manifest 到游戏根 `mods/SPIKEINV012A.dll` 和 `mods/SPIKEINV012A.json`，校验安装哈希。既有离线 gate 已验收且身份未变，不再重跑整套静态调查。
3. 提出一次**仅配置**人工启动：保持 modded，停留主菜单/profile1，不开始/Continue、不按 F8。在设置中启用 SPIKEINV012A、禁用 AscensionUnlocker及其他旧测试 probe，保持 modding 开启，然后正常退出。不得先制造零启用模组并启动。
4. 此配置进程若同时加载 AscensionUnlocker+r6，r6 的 `PRECHECK_BLOCKED` 是**预先允许的配置拒绝**，不是业务失败；其身份门先于 attempt 创建，不能消耗正式诊断。若新 probe 默认 disabled，缺少 probe 标记也可接受，核对配置持久化。出现其他异常则记录，不强行进入业务。
5. 游戏退出后过滤复核：selected modded profile1、仅 r6 enabled、旧 probe/Ascension disabled。满足后可在本包直接请求正式启动；无需再回 PM。配置修正最多两轮，不无限循环。

## B. 一次正式人工诊断

把以下动作改写成简短中文测试请求，逐阶段确认并由 Implementation 读取本任务 log 标记。不要让用户猜日志是否已出现，也不要凭配置文件推断 runtime loaded set。

1. 用户确认 Cloud 关闭后启动；实际 loaded set 必须仅 SPIKEINV012A，modded profile1，观察 `SPIKEINV012A PRECHECK_PASS`。身份拒绝则不 F8/不开始对局；安全配置问题可按 A 修正，不占正式业务 attempt。
2. 在测试槽新建普通 Standard 单人对局（建议 Ironclad、A0，不用每日/自定义/多人）。允许覆盖该测试槽 Continue；必要起始选择仅用于到达 Act 1 地图。不要进入 unmodded 槽。
3. 到地图且未在战斗时按 F8 一次，等 `SPIKEINV012A READY_A_NODE`。探针目标是**当前节点直接可达的普通 Monster 中，先按 row 再按 col 排序的第一项**；通常同排最左弱怪，但不能只凭屏幕“第一个图标”猜目标。Implementation 先根据代码/当前地图说明唯一要点的节点；无法确认就不点、不擅自更改目标。
4. 点击该目标后立即停手。保持游戏运行/聚焦，不暂停、不出牌、不 End Turn、不领取任何奖励、不去下一房间、不切 profile。等待 `CAPTURE_READY_STOP_INPUT` 或 `PROBE_TERMINAL_STOP_INPUT`；各阶段独立 30 秒 deadline，不给玩家在地图等待期间套超时。若战斗画面出现后 45 秒无任一标记，报告 stalled，保持不操作，按下步正常退出，不声称 PASS。
5. 达到任一标记后正常退出整个游戏以观察退出 writer；不要再点其他节点/开新局。Implementation 确认进程结束后归档此 attempt 证据、身份和过滤日志。若退出记录缺失，只写该窗口 Unknown，不能因此自动再启动或补造回调。

正式 attempt 必须有 fresh identity、明确 build/installed hash、runtime loaded/profile 门证据及序列。`capture-ready` 证明本次 readiness 门达到，不是 A snapshot。capture timeout/逐字段拒绝/observer failure 可成为有价值的诊断结果；不能以“不 PASS”自动新开研究任务。

## C. 有限 harness 修复权限

授权初次正式诊断，加至多 **2 次已定位 harness 修复后的 fresh-process 重测**；配置会话不占此业务预算。修复可以发生在开始新局/元数据基线之后，不限于 pre-business；但旧 attempt 一旦终态就停止，保留完整证据。

可修：人工 Arm/提示可见性、ticker 挂载/计数、observer 异常处理、deadline/状态机竞态、field telemetry、attempt evidence 生命周期。必须证明缺陷在 harness，且仍测**同一生产条件**；不得降低 20 字段/30 连续帧来制造成功，不把未经证明的目标/楼层断言默默删除。已定位实现错误的等价修正需报告 rationale 与 regression test。

每次：先报告原因 → 修复最小代码 → 新 build identity/独立候选 → 有区分力的回归和相关 build/patch/非干扰验证 → 游戏退出且原安装 pair 精确匹配后替换 → 新 attempt。无代码变化不重建全套证据。允许重新生成可消耗测试局作为新 attempt 的准备，不覆盖/重分类旧 attempt，也不恢复旧 recovery。

不得新增生产方法调用、改生产参数/返回值/Task/异常、manager/store override、replay dispatch、存档内容读取、自动游戏操作或扩大 payload/schema。改变业务问题、验收条件/隐私边界，未知 production fault或可能触及个人槽，证据不足以定位 harness，预算耗尽：停止交回 PM。已获得完整诊断结果无需为了追求 PASS 消耗修复预算。

## 交付与结束

- 提交 `SPIKE-INV012A-runtime-report.md` 和 attempt 证据索引；已有文件不得覆盖，续写须保留旧结论及 identity。
- 分别写：真实 ticker/readiness 结果、native writer **调用元数据**、未观测窗口、已定位 harness 缺陷/修复、个人数据边界与正常测试写入。writer 回调不证明具体文件内容变化的因果或 isolation。
- 一份完整可解释结果或修复预算结束后停；不自动创建 successor/A/B，不解锁 SPIKE-001B。
- 游戏退出后可选择清理**本包自行安装且 hash 匹配、workspace可恢复**的精确 r6/获准修订 DLL+manifest；不得递归删除 mods，不清理旧 probe 或任何证据。记录两个精确目标/身份与可恢复来源，不为了清理再启动游戏。若保留安装文件，明确通知用户下次正常游玩前禁用探针。
