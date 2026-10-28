# SPIKE-INV007A-R2：harness 修复后的受控第二次尝试

历史状态：R2 已完成并验收为 `VALID TERMINAL — ORACLE_ALIASING`。不得修改或重跑；当前后续仅见 `project-management/handoffs/SPIKE-INV007A-R3.md`。

任务类型：对现有 007A 的窄范围 harness 修订；经 Project Lead 特别授权一次新的人工运行

## 为什么允许原地修订

R1 的唯一 closed artifact 在 `GateReady` 后、人工 Run 前被仍存活的 120 秒 startup timeout 错误封存，没有建立 protection baseline、candidate、override 或调用 direct methods。随后另一进程中的实际 Run 已建立 restricted recovery backup，但由于固定 artifact 文件已存在，writer 拒绝覆盖；该次业务终态不可恢复。

这不是 direct-write 假设的有效 FAIL。Project Lead 因此允许保留同一 mod id/source tree，以新 revision、schema 和 attempt-specific artifact 完成一次 R2。原 artifact、backup 与 Phase 0 证据必须原样保留。

## 开始前必须阅读

1. `project-management/CURRENT_STATUS.md`
2. `project-management/TEST_PROTOCOL.md`，尤其“无效 harness attempt 与受控修订”
3. `project-management/DECISIONS.md` 中 D-041、D-042
4. `project-management/handoffs/SPIKE-INV007A.md`
5. `project-management/investigation-reports/SPIKE-INV007A-report.md`
6. `evidence/spike-inv007a/runtime-artifact.json`
7. `experiments/SPIKE-INV007A/ProbeController.cs`

## 固定旧证据

- R1 artifact：`evidence/spike-inv007a/runtime-artifact.json`
- 长度：3,108 bytes
- SHA-256：`D38EC65F8380DDC2AFF1C8F6EB9E7FDEBEAB1D706424C4471C36DAAD0488ACA2`
- R1 terminal：`STOP_MISSING_TIMING_GATE / TimeoutCallback`
- R1 events：1–9，`GateReady` 为 8，之后只有错误 timeout terminal
- 第二次尝试 recovery root：`evidence/spike-inv007a/restricted-recovery/20260914T081712.1432035Z/`
- recovery files：12；不得删除、覆盖、移动、改名或当作新 baseline
- 原 installed DLL：157,696 bytes / SHA-256 `41E2187CD469DF4F76E37FDCCDB867035347B106D6B5FF1B3FCF1412324C730A`
- 原 manifest：371 bytes / SHA-256 `C8136FC68132A946959C82EE2C4E1BBDB6AD9AB1F5A758021E886BC278CEC1E3`

任一旧证据 identity 不符即停止并报告 `BLOCKED_IDENTITY`。

## 仅允许的源码改动

继续使用：

- mod id `SPIKEINV007A`
- source `experiments/SPIKE-INV007A/`
- evidence root `evidence/spike-inv007a/`

必须提升：

- probe version 至明确的 R2 revision；
- runtime schema 至 `spike-inv007a.direct-write.v2`；
- runtime artifact 固定为新的 `runtime-artifact-r2.json`，不得寻找、覆盖或删除 R1 artifact；
- R2 recovery backup 使用新的唯一 attempt 子目录，不得复用旧目录。

只批准以下功能改动：

1. startup missing-event timeout 在 `GateReady` 原子提交成功时被取消/解除武装；
2. timeout callback 进入锁后若 `_gateReady` 或 Run owner 已接受，立即无副作用返回；
3. artifact writer 写入 R2 专属文件，且仍使用 create-new/no-overwrite 语义；
4. 增加下述 prior-attempt integrity gate；
5. 安装脚本可在游戏进程不存在且目标恰好匹配上述 R1 DLL/manifest identity 时，用 R2 产物替换这两个 **SPIKEINV007A 专属安装文件**。不得覆盖任何其他文件。

不得改动 direct-write 业务契约、candidate、boundary audit、protection allowlist、mutation oracle、teardown、结果分类或原 007A 的产品/实验范围。不得借本修订处理其他问题。

## 必须增加的 synthetic/race tests

- `GateReady` 先于 timeout：timeout callback 不 seal、不加 event、不写 artifact；
- timeout 先于缺失 gate：仍只产生一个合法 missing-event terminal；
- `GateReady` 与 timeout 并发：只有一个锁内结果；GateReady 一旦提交，timeout 永远不能成为 winner；
- Run accepted 后 timeout callback 无副作用；
- R1 artifact 存在不阻止 R2 artifact 写入；
- R2 artifact 已存在时在业务动作前 fail closed，且不得覆盖；
- R1 artifact/hash 与旧 recovery tree 在 build/install 前保持不变；
- install 只允许替换 exact known R1 installed pair，其他 identity 或额外目标全部拒绝。

Release build、source/binary audit、Harmony smoke 与全部原 19 项 tests 仍须通过。新增 tests 不能替代原 tests。

## prior-attempt integrity gate

第二次实际点击的业务终态缺失，所以 R2 在显示 Ready 和允许 Run 前，必须在新进程中进行一次只读完整性检查：

- active profile 机器确认仍为 modded profile1；
- 当前 allowlisted profile1、exact account selector、machine settings 的文件内容与旧 recovery root 中 12 份 pre-attempt backup逐项比较 existence、length、SHA-256；
- profile1 文件/目录 inventory 必须与该 recovery set及既有固定目录集合一致；
- Continue 可见，`RunManager.State == null`，original manager identity稳定；
- 不以 mtime 单独判失败，但内容/inventory 任何差异都必须 `BLOCKED_PRIOR_ATTEMPT_UNRESOLVED`；
- 不得自动恢复、删除、覆盖或 rebaseline。

该 gate 通过只表示前次尝试没有留下当前可见的 allowlisted持久化差异，不恢复其丢失的业务结论，也不证明不存在所有 platform/side-channel 影响。

## R2 人工运行

prior-attempt integrity、timing gate 和 Phase 0 全部通过后才显示：

```text
Direct Write Ready — R2
Prior attempt baseline verified. Click once; no timeout while waiting.
```

Implementation Codex 不得启动或控制游戏，必须向项目发起人提交精确请求。用户只点击一次 Run，随后不出牌、不结束回合、不领取奖励、不操作其他 UI，等待自动 teardown。Ready 状态允许无限期等待人工点击，不得设置人为 action deadline。

首个 R2 terminal 后立即停止，不再次尝试。Implementation Codex随后只读取 `runtime-artifact-r2.json`、R2 allowlisted protection evidence 和既有固定证据。

## 结果与交付

R2 沿用 007A 原有业务分类，并新增 `BLOCKED_PRIOR_ATTEMPT_UNRESOLVED`。只有 R2 artifact 完整记录 direct update/save、audit、teardown 与 protection，才可产生业务结论。

更新而非另建主报告：

1. `project-management/investigation-reports/SPIKE-INV007A-report.md`：保留 R1/丢失终态尝试历史，新增 R2 实现与结果；
2. `evidence/spike-inv007a/README.md`：列出各 attempt、schema、artifact 与 recovery identity；
3. `runtime-artifact-r2.json`：若 R2 进入 terminal，恰好一份且不可覆盖；
4. R2 test/build/static-audit/install/prior-integrity/protection evidence。

Implementation Codex 不得修改 `CURRENT_STATUS.md`、`TODO.md`、`DECISIONS.md`、`EXPERIMENT_PLAN.md` 或 Roadmap。
