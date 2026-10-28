# SPIKE-INV012A Phase 0-R6 — 离线合同窄修订

状态：`READY_OFFLINE_ONLY`（Project Lead，2026-09-28）。这是同一 `SPIKEINV012A` 探针的 Phase 0 修订，不是新运行实验；r5 已封存，运行阶段未获授权。

先读 `CURRENT_STATUS.md`、原 `handoffs/SPIKE-INV012A.md`、`SPIKE-INV012A-Phase0-report.md` 与 `evidence/spike-inv012a/project-lead-phase0-review.md`。保留 r5 DLL/manifest/证据的原字节和 hash，不覆盖任何旧 artifact，不修改 A4 或其他 probe。

## 唯一任务

1. 修复 evidence 写失败导致 `Start()` 提前封存后，`SaveEntered` / `SaveCompleted` 仍继续尝试写业务记录的状态转移。补纯合成测试：让 `stage-start` 写入失败而下一次 sink 写入可成功，证明同一业务 lane 只有一次 terminal，terminal 后无 `save-entry` / `save-complete`；正常路径与独立退出 writer-summary lane 不得退化。
2. 将 loaded-set、modded profile1 和必要 owner 的无 payload precheck 放在新 attempt 文件创建之前；身份不符时只产生明确、无敏感字段的阻断标记，不创建探针目录，不允许 F8 arm。身份通过但 `NGame.Instance`/ticker owner 不可用时，形成明确的 prebusiness terminal，而非静默返回。补负向测试并证明无越界文件、无业务调用。
3. 全量重跑固定 v0.111.0 / Harmony 2.4.2.0 的 Release build、合成合同、9 目标 patch/unpatch、dummy non-interference、编译后 IL/数据白名单 audit。新候选使用独立 r6 artifact/输出和 hash；报告列出与 r5 的精确差异。r5 仍只能作为历史离线证据，不得安装。

## 不变边界

不得更改被测生产方法、patch 目标、readiness 20 字段、30 帧/30 秒语义、writer 观察白名单、允许 I/O/保护窗口或 PASS 标准；不得用修补扩大到 A/B、Continue B、manager override、文件 payload、全局 file/store hook。可在当前新探针源码和测试中完成上述有限修复，无需每个离线代码修改再申请，但任何范围外设计变更须停止交回 Project Lead。

不安装/启用任何 probe，不启动或控制正式游戏（包括 `--headless`），不读取当前 account/profile/save/replay/settings/cloud/recovery 或其他 profile 内容，不使用 Computer Use。只提交离线 r6 报告和证据；不能直接请求用户打开游戏。

交付：`project-management/investigation-reports/SPIKE-INV012A-Phase0-R6-report.md`、`evidence/spike-inv012a/` 下不可覆盖的 r6 输出、新 candidate 与版本/hash，以及更新后的未来人工运行草案。不得修改 `CURRENT_STATUS.md`、`TODO.md`、`DECISIONS.md` 或 `EXPERIMENT_PLAN.md`。Project Lead 再验收后才决定是否签发独立运行阶段。
