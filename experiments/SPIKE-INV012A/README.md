# SPIKEINV012A 已完成的窄诊断

2026-10-09：`runtime-r1` 已获 Project Lead 验收为 `ACCEPT_DIAGNOSTIC_PASS / NO_RECONSTRUCTION_CONCLUSION`，本实验停止追加运行。真实结果与范围见 [`runtime 报告`](../../project-management/investigation-reports/SPIKE-INV012A-runtime-report.md)及[独立验收](../../evidence/spike-inv012a/runtime/project-lead-runtime-review-2026-10-09.md)。r6 主菜单的 3 条、runtime-r1 的 14 条原始探针记录已不可覆盖归档；本轮 live DLL/manifest 已移除，settings 未修改且 enabled 条目可能残留，见[收尾收据](../../evidence/spike-inv012a/runtime/closeout-receipt-2026-10-09.json)。源码、r5/r6 与 `artifacts-runtime-r1` 候选全部保留。

Phase 0 的历史固定 DLL/Harmony、patch 目标、证据白名单与离线验证见 [`R6 报告`](../../project-management/investigation-reports/SPIKE-INV012A-Phase0-R6-report.md)；此前状态见 [`r5 报告`](../../project-management/investigation-reports/SPIKE-INV012A-Phase0-report.md)。后续工作为新身份 SPIKE001A5，不复用本探针运行。

工程布局：`Probe.cs` 为身份门、loaded-set/profile 门与只读入口；`Patches.cs` 为 9 个 exact Harmony target 的 16 个只读回调；`Readiness.cs` 在内存中计算 20 字段及目标身份布尔；`Core/DiagnosticCore.cs` 为独立 monotonic 阶段/terminal/聚合合同；`EvidenceSink.cs` 只写 probe-owned create-new 记录；`tests/` 是纯 .NET 合同、真实方法 patch/unpatch 与 dummy 非干扰 smoke。


