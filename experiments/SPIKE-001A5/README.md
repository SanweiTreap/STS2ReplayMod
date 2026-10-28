# SPIKE-001A5 — Phase0 离线候选

2026-10-09，`PHASE0_COMPLETE / AWAITING_PROJECT_LEAD_REVIEW`。新 identity `SPIKE001A5`，独立 owner `STS2ReplayMod.SPIKE001A5.RoomBefore.r1`，schema=1、revision=`phase0-r1`。当前只完成离线准备；人工运行须由 Project Lead 验收放行。

唯一问题：同一个 production room-before checkpoint，不中断进入的 A 与 fresh process 正常 Continue 的 B，在规定语义投影上是否等价。源码只观察、封存私有证据和比较，人工驱动所有游戏动作。

入口及范围见 [Phase0 报告](../../project-management/investigation-reports/SPIKE-001A5-Phase0-report.md)、[数据合同](../../evidence/spike-001a5/data-contract.md)、[人工草案](../../evidence/spike-001a5/manual-request-draft.md)。测试用现有 `.NET SDK 9.0.311 / runtime 9.0.13`，不需要改系统安装。

```powershell
& 'C:/Projects/STS2ReplayMod/experiments/SPIKE-001A5/Invoke-OfflineValidation.ps1'
```

每次验证生成新的 `evidence/spike-001a5/validation-r1-<guid>/`，含 Release build、99 项合同、patch/unpatch、dummy、IL、源码/候选身份收据；不会覆盖封存验证。固定候选位于 `artifacts-phase0-r1/`。构建输出供开发使用，正式 review/runtime 以封存 hash 为准。

原始 production checkpoint 与解析出的账号路径只允许留在 `user://SPIKE001A5/phase0-r1/<attempt>/` 的 `*.private.json`。不得把它们整份导出到仓库。原生存档读取只限 API 解析并验证的 modded profile1 `saves/current_run.save`；探针没有原生写入或恢复代码。`Test-SealedInput.ps1` 是未来 A 退出后的只读核对工具，Phase0 没有执行。

`active.json` create-new 指向一个不可覆盖 attempt；A 完整提交后才允许同候选 fresh B claim。终态、未完成 A、已消费 B claim、已比较 attempt 都不能继续业务。有限修复必须更新 revision/owner/manifest、重新封存候选并使用全新完整 A/B，旧证据保留。

上一 012A 已通过 PM 诊断验收并完成归档/安装清理；历史候选、源码、报告保持可恢复。其结论没有提供重建 A/B 答案。
