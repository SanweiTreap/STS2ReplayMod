# SPIKE-INV012A — 运行前配置与保护预检（纯只读）

状态：`READY_READ_ONLY_PREFLIGHT`（Project Lead，2026-09-28）。本工作包不是探针运行授权；**不得安装/启用 r6、启动/控制 STS2（含 headless）或要求用户打开游戏**。通过后仍须 Project Lead 另行签发人工运行阶段。

## 目标

在 r6 候选保持未安装时，确认下一次 profile1 人工诊断的安装/启用与保护前置条件是否可满足，避免重演 008A 的旧 probe 同时启用、A4 cleanup 的 profile3 偏差，以及把准备窗口的 `settings.save` 变化误当业务写入。只回答 `READY_INSTALL_AND_MANUAL_RUN` 或一个明确的配置/证据阻断；不测试 ticker、writer、重建或任何游戏行为。

先读 `CURRENT_STATUS.md`、`TEST_PROTOCOL.md`、`INV-012-report.md`、`SPIKE-INV012A-Phase0-R6-report.md`、`evidence/spike-inv012a/project-lead-phase0-r6-review.md`。固定候选 r6 DLL SHA-256 `CA1DAF85B5C12AE043A2BF20E7D9026DA894D336F649913A7E1D1BE2C69C1720`，manifest SHA-256 `6B35514CFE0ABF7EEDC7B137759D7D9DB8BB4A2D737809AFC986C1F39D1169FD`；游戏为 v0.111.0 / 固定 DLL SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`。r5/A4/其他旧探针与证据只读。

## 允许的只读检查

1. 固定版本/候选/manifest/本地 Harmony 身份、live mods 目录中**本项目探针**的精确 DLL/manifest 清单及 hash；文件存在与 enabled 状态分开报告。以固定 v0.111.0 `SemanticVersion` 的 parse 语义或可复现离线调用验证 manifest version，不能只凭通用 SemVer 印象。
2. 在不启动游戏的条件下，先以 production 路径规则确认 `user://`、modded account selector 和 account settings 的精确文件身份。仅允许为判断**当前所选 profile ID**和**持久启用的 mod ID 集合**读取这两个已命名的配置文件；解析前必须先校验路径落在预期 account/modded scope，不得枚举、读取或复制其他 profile。只输出 ID/布尔/文件 hash、长度和必要 mtime，不输出原始 JSON、账号数字、其他设置、异常 message、绝对用户路径或其他 profile 数据。若无法无泄漏地解析，报告 `BLOCKED_SCOPE_SAFE_CONFIG_READ`，不尝试扩大扫描。
3. 检查当前选择是否为 modded profile1、A4/AscensionUnlocker/所有退役测试 probe 是否持久 disabled、r6 是否未安装、Steam Cloud 当前状态是否能从允许的证据确认。Cloud 若只能由用户界面确认，明确列为待用户人工启动前确认的条件，不能把历史“关闭”当当前事实。不要读取 cloud payload。
4. 依据 INV-012 固定的 account/modded/profile1 拓扑，拟定下一次运行的准备期、业务期、正常退出期三个独立保护 baseline；列精确 allowlist、hash/length/mtime/inventory、probe-owned `user://SPIKEINV012A` 证据目录的单独允许写入、保护备份位置/恢复权限和停止条件。不要现在建立当前业务 baseline 或 recovery backup，不读取 save/replay payload；不把 `settings.save` 或 `latest.mcr` 的内容差异预先列为允许。
5. 形成一份面向项目发起人的**草案**：如当前选中槽或 enabled 状态不符，说明需要用户怎样在独立准备步骤中纠正，以及何时必须停手；不得自行进行纠正，也不得把“进入 profile3 再切换”默认为安全步骤。只有所有可静态核实的门都通过，才推荐后续安装/人工测试；不得在本任务里安装或发送实际测试请求。

## 交付与停止

报告 `project-management/investigation-reports/SPIKE-INV012A-runtime-preflight-report.md` 与 `evidence/spike-inv012a/preflight/README.md`，仅保存范围过滤后的身份/配置摘要、验证命令和保护方案，不保存设置/profile payload或账号路径。结论只取 `READY_INSTALL_AND_MANUAL_RUN`、`BLOCKED_CONFIG`、`BLOCKED_SCOPE_SAFE_CONFIG_READ` 或 `BLOCKED_IDENTITY`；任何未知值不得当作 PASS。不得修改 `CURRENT_STATUS.md`、`TODO.md`、`DECISIONS.md`、`EXPERIMENT_PLAN.md`，也不得修改、安装或运行任一 probe。

收到报告后由 Project Lead 决定是否签发独立的安装/人工运行工作包；若当前选择并非 profile1，先由 Project Lead 和用户确定安全的准备动作，而不是自动切换或读取其他 profile。
