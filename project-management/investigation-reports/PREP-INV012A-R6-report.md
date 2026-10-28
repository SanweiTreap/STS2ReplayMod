# PREP-INV012A-R6 — Project Lead 验收

日期：2026-10-09。结果：`COMPLETE / READY_R6_INSTALL_REVIEW`。

用户报告已完成人工准备；Project Lead 在无游戏进程时只读复核 account settings 的过滤字段、modded selector、游戏及候选身份。结果见 [过滤摘要](../../evidence/spike-inv012a/preparation-2026-10-09/filtered-check.json)。

## Confirmed

- 当前 modded selector=1，`mods_enabled=true`，唯一持久 enabled ID 为 AscensionUnlocker；九项旧探针均 disabled。
- STS2 进程数=0；v0.111.0 / 41cef1ea 固定 DLL、Harmony 2.4.2.0、r6 DLL/manifest 身份匹配，r6 尚未安装。
- 本次复核未读取任何 profile 的 run/progress/prefs/replay payload，没有修改配置、安装探针或启动游戏。

## User-reported / Unknown

- 人工准备完成来自用户。Cloud 关闭按该准备完成报告记录，未获机器独立验证，下一正式启动前须现场确认。
- 没有独立观察用户 UI 路径，不能声称已通过实际读写验证 unmodded profile1 安全。
- persisted enabled 集合不是下一进程 loaded 集合；真实 ticker/readiness、退出 writer 和 A/B 仍未知。

## 决策与下一步

准备通过。签发合并的 [安装、配置与窄诊断工作包](../handoffs/SPIKE-INV012A-RUNTIME.md)，保留 r6 初始候选，不为配置适配新造 R7。权限与判定按新工作包；旧 Phase 0 报告中的未来运行草案不是当前授权合同。保留所有旧结果，不解锁 SPIKE-001B。
