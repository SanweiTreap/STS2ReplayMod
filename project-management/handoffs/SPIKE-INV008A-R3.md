# SPIKE-INV008A-R3：loaded-mod 生命周期修复与原 controlled fault/restart matrix

任务类型：同一 008A 实验 lineage 的窄 harness revision；保留 v1/R2 全部证据，业务契约不变

## Project Lead 处置

R2 没有获得 fault/restart 业务结论。唯一 R2 runtime artifact 为 `precheck-modset-r2-998e07af004a4268b1c33dd2e7644bc5.json`（242 bytes；SHA-256 `2194A2A05E24ECCBDE8C39035BF389A99511E0FD50F4DD998B3688A87E9B9916`），它在 initializer 中把尚未发布的 `ModManager.GetLoadedMods()` 空集合误判为 wrong configuration。对应完整日志（252,133 bytes；SHA-256 `7F01AE43BA2E43776D7A60413B3D6BC07875BFF38B92DE448420DDC573540171`）随后明确报告 007A disabled、loaded list 恰好为 `SPIKEINV008A`、`Loaded 1 mods (16 total)`。

该 attempt 未建立 protection、candidate、override、run、recovery 或业务调用，且没有 R2 business/invalid artifact。分类为 `PREBUSINESS_PRECHECK_TIMING_DEFECT / NO R2 FAULT-RESTART CONCLUSION`。允许在相同 mod id/source tree 上进入 R3，只修复 loaded-mod precheck 的观察时机与相应 identity/evidence。

## 开始前必须阅读

1. `project-management/CURRENT_STATUS.md`
2. `project-management/TEST_PROTOCOL.md`，特别是“无效 harness attempt 与受控修订”“工作包内的有限修复授权”“Probe 安装与启用状态生命周期”
3. `project-management/DECISIONS.md` 中 D-045 至 D-047
4. `project-management/handoffs/SPIKE-INV008A.md`
5. `project-management/handoffs/SPIKE-INV008A-R2.md`
6. `project-management/investigation-reports/SPIKE-INV008A-report.md`
7. `evidence/spike-inv008a/README.md` 与 R2 precheck artifact/validation

## 必须保留与验证的历史证据

- v1 `setup-owner-fault.json`：2,359 bytes；SHA-256 `56E0725994F19BF8917AB34EA7AFD8851340CF9A12CE78635C7AD6E71A310B48`
- v1 `invalid-phase.json`：1,479 bytes；SHA-256 `A1D6BC41405FFCBCC2797E742A1D0E424FE4CFD1330BA7E3A8C8601CEB0AEC8F`
- R2 precheck artifact：242 bytes；SHA-256 `2194A2A05E24ECCBDE8C39035BF389A99511E0FD50F4DD998B3688A87E9B9916`
- 当前 R2 installed DLL：229,888 bytes；SHA-256 `D5746F9C560B6A0E09BE6235491554A47DAB3B1AEA84078EB25BF2E8077D2615`
- 当前 R2 installed manifest：353 bytes；SHA-256 `0FF3138632515C43F98E111752336C223C89B751AFFA8740491AFD11FF3118A1`
- R3 prior direct-write artifact/recovery 与 003A/007A 源码/证据继续使用既有固定身份。

所有历史 artifact、log、recovery、源码快照和 evidence 不得删除、覆盖、移动、改名或重写。R3 prior-attempt gate 还必须确认 R2 四个 business artifact、`invalid-phase-r2.json` 与 R2 recovery 均不存在。

## 必须修复的根因

1. 删除 `ProbeController.Activate()` 中对最终 loaded-mod set 的同步判定。initializer 阶段只允许安装 observer/patch、解析 phase 与开始安全 startup observation；不能把 `GetLoadedMods()` 的空或部分结果封存为配置失败。
2. 把 exact loaded set precheck 移到 loader 已完成发布之后、且仍在任何 profile/protection/candidate/override/run/recovery 访问之前的边界。当前首选边界是首次正常 `NMainMenu._Ready` callback 的最前部：先读取并验证 loaded IDs，再读取 `SaveManager`、Continue 或任何 profile 状态。
3. precheck PASS 后才允许继续原 menu/timing/prior-integrity gate。precheck FAIL 仍只写 bounded、attempt-specific、create-new evidence，不消费业务 artifact。
4. `TryOpenGate` 与 `RunPriorGate` 的 revalidation 必须继续要求 exact loaded set；不能因移动首次 precheck 而降低后续身份检查。
5. 保持 Ready 时取消人工等待 timeout 的现有修复。

如果 Implementation Codex 通过静态证据发现 `NMainMenu._Ready` 仍早于 loaded-set 发布，不得猜测或删除门禁；可在本工作包的 repair envelope 内把观察点进一步延后到一个可证明的 post-loader、pre-profile-business callback，并记录证据。

## R3 identity 与 artifact

- manifest 使用合法 SemVer `0.3.0`；artifact schema 使用 `spike-inv008a.owner-fault.v3`，内部 revision 明确含 R3/build revision。
- R3 四个 business artifact必须为 create-new 专属名称：
  - `setup-owner-fault-r3.json`
  - `restart-after-setup-r3.json`
  - `postlaunch-owner-fault-r3.json`
  - `restart-after-postlaunch-r3.json`
- invalid phase 与 recovery 也必须使用 R3 专属名称/目录。
- configuration failure 使用 `precheck-modset-r3-<attempt>.json`；每次 attempt 唯一且不可覆盖。
- R2/v1 artifact 只作为 prior evidence，不能被 PhasePolicy 解释为 R3 PASS；但其固定 identity 与 pre-business/null/absence contract 必须先验证。

## 必须新增的 tests/audit

- 模拟 initializer 时 `loadedIds=[]`，断言不会写 precheck/business artifact、不会封存 session，且后续 post-loader `[SPIKEINV008A]` 可以通过。
- 模拟 post-loader 仍为空、含007A、含其他 mod，分别只生成 attempt-specific precheck evidence且无 profile/protection/candidate/override/run/recovery。
- 断言首次 loaded-set 读取在 `NMainMenu._Ready` callback 内发生，并在 `SaveManager.Instance`、Continue、prior integrity 与其他 profile access 之前。
- precheck PASS 后保留 gate-ready、timeout cancel、exact revalidation 与原 41 项 R2 tests；重新执行 Release 0 warning/error、source/IL audit 和 Harmony patch/unpatch smoke。
- manifest `0.3.0` 动态日志不得产生 invalid SemVer warning。

## 有限代码修改授权（harness repair envelope）

Implementation Codex 获准在 `experiments/SPIKE-INV008A/` 内自行修改、测试、构建并安全替换 008A 自己的 DLL/manifest，以处理本 R3 的 **pre-business harness 缺陷**。不需要为每次此类代码修改返回 Project Lead。

授权条件全部满足时，可在分析失败证据后直接准备下一 build，并向项目发起人请求下一次人工启动：

- 失败发生在 protection baseline、candidate、override、run、recovery、fault 与业务调用之前；
- 旧 artifact/log 保留，新的 precheck attempt/build identity 可区分；
- 修复只属于 loaded-set 生命周期、precheck UI/dispatch、Ready/timeout、attempt writer 或离线 test/audit；
- 增加 regression test并重跑所有门禁；
- 游戏进程停止，installed pair 精确匹配上一 build，且只替换 008A 自己的 DLL/manifest；
- 不改变四阶段业务契约或任何安全/PASS 边界。

本 envelope 最多允许 **两次 R3 pre-business 修复后的追加人工启动**。达到上限、无法证明 pre-business、出现任一 protection/candidate/override/run/recovery/fault/业务 artifact，或需要改变业务契约时，立即停止并交回 Project Lead。Implementation Codex 始终禁止启动/控制游戏或使用 Computer Use。

## 安装与人工运行

安装前确认游戏进程停止、R2 installed pair 精确匹配上述 hash、007A 持久化 disabled，且最近完整日志实际 loaded set 只有008A。随后只替换 008A pair；不得修改 enabled 配置或007A文件。安装后由项目发起人确认 Steam Cloud关闭、仍使用模组profile1并手动启动。

四阶段业务矩阵、fault点、teardown、boundary、protection、mtime whitelist、用户动作与停止规则完全继承原 handoff。每次只请求一个已解锁 phase；不进入map/room/frame/action/reward/progress，不调查UI渲染，不强杀进程。

## 交付与验收

更新现有 008A report/evidence README，保留 v1/R2 历史；提交 R3 preflight/tests/build/audit/install证据、所有precheck evidence、已解锁business artifact/recovery及每次人工测试请求/结果。

Implementation Codex不得修改 Project Lead 管理文档。四个R3业务phase全部PASS才得到`PASS_CONTROLLED_FAULT_RESTART_ISOLATION`；即使PASS，也不覆盖强杀/native crash、replay fidelity或完整产品session isolation。
