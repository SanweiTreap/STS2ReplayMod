# SPIKE-INV012A 运行前只读预检报告

日期：2026-09-28  
**结论：`BLOCKED_CONFIG`。** 当前唯一可范围安全读取的 modded account selector 为 `last_profile_id=3`，持久启用列表含 `AscensionUnlocker`。这两项均与计划中的 profile1、仅 r6 探针环境不符。本任务没有安装/启用 r6、启动游戏或请求玩家操作；运行阶段仍须 Project Lead 另行签发。

## 已执行的只读门

已阅读 [`CURRENT_STATUS.md`](../CURRENT_STATUS.md)、[`TEST_PROTOCOL.md`](../TEST_PROTOCOL.md)、[`INV-012-report.md`](INV-012-report.md)、[`Phase0-R6-report.md`](SPIKE-INV012A-Phase0-R6-report.md)、[Project Lead R6 验收](../../evidence/spike-inv012a/project-lead-phase0-r6-review.md)及[本工作包](../handoffs/SPIKE-INV012A-RUNTIME-PREFLIGHT.md)。完整范围过滤摘要、文件 hash 与复核命令见 [`preflight/README.md`](../../evidence/spike-inv012a/preflight/README.md)。检查前后正式 STS2 进程数均为 0。

| 门 | 结果 |
| --- | --- |
| 固定游戏 | `v0.111.0` / `41cef1ea`；`sts2.dll` 9,757,184 bytes、SHA-256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`；Harmony AssemblyVersion `2.4.2.0`。 |
| r6 workspace 候选 | DLL 52,224 bytes、SHA-256 `CA1DAF85B5C12AE043A2BF20E7D9026DA894D336F649913A7E1D1BE2C69C1720`；manifest SHA-256 `6B35514CFE0ABF7EEDC7B137759D7D9DB8BB4A2D737809AFC986C1F39D1169FD`，ID `SPIKEINV012A`。固定游戏 DLL 的 `SemanticVersion.TryFromString` 实际离线调用接受 `0.1.0-phase0` 与 `0.111.0`，当前 `v0.111.0` 不低于 minimum。 |
| 安装与持久启用 | r6 DLL/manifest 均不在 live mods；九组已安装的旧项目探针 pair 精确 hash 已列证据索引，均有本地来源的显式 disabled 记录。文件存在与 enabled 已分别核对。 |
| selector | 只读 `<M>/profile.save` 的 `last_profile_id=3`，**不满足 profile1**。 |
| mod 配置 | 只读 `<S>/settings.save`：`mods_enabled=true`，唯一持久 enabled ID 为 `AscensionUnlocker`。它的 Workshop 来源记录为 enabled；本地目录存在而无该 ID 的显式本地 disabled 记录。**不满足仅本探针环境**。 |
| Steam Cloud | 当前状态不能从两份允许读取的配置或离线证据确认；历史“关闭”不能当作当前事实。需在另获批准的人工启动前由用户现场确认，预检不读 Cloud payload。 |

路径只使用固定 v0.111.0 `UserDataPathProvider` 与 INV-012 拓扑：`<U>=%APPDATA%/SlayTheSpire2`、`<S>=<U>/steam/<account-id>`、`<M>=<S>/modded`、`<P>=<M>/profile1`。`<U>/steam` 下只有一个纯数字 account 目录；在读取前核对绝对路径段与 reparse 属性。只解析了精确 `<S>/settings.save`、`<M>/profile.save` 两个文件的必要字段，未读取、枚举、复制其他 profile，也未读取 save/replay/cloud/recovery 内容。当前 Steam 登录身份无法由离线配置独立证明，未来获批启动仍须现场核对；本次已证实的配置偏差足以阻断。配置 hash/长度/mtime 仅用作此刻身份摘要，不是业务 baseline。

## 后续保护设计（现在不建立 baseline 或 recovery）

逻辑 allowlist 限于 `<S>/settings.save`、`settings.save.backup`、`settings.save.tmp`、`settings.save.backup.tmp`；`<M>/profile.save` 的同类主文件/backup/tmp；`<P>/saves/{current_run,progress,prefs}.save` 各自主文件/backup/tmp；`<P>/replays/latest.mcr`。对应目录只对该精确 allowlist 做 inventory、SHA-256、length、源 mtime；未知新项或缺项另行标记并停手，不以递归扫描其他 profile 填补。`user://SPIKEINV012A/<fresh-attempt>/NN.json` 是独立 probe-owned 证据写入区，仅在未来获批运行中允许 create-new、每条不超过 4 KiB、总量不超过 16；它不抵消保护文件差异。

| 独立窗口 | 未来应封存的两端与允许变化 | 失败/停止门 |
| --- | --- | --- |
| **准备期** | 用户获独立授权前先封存准备前 inventory/hash/length/mtime；将禁用旧 mod、选择 profile1 和以后获批安装 r6 导致的 `settings.save` / selector 变化单独记录，完成正常退出后再封存准备后状态。只比较此窗口，不拿历史 r5/A4 字节当准备后业务基线。 | 未能确认无运行/无其他 profile 操作、启用集合、selector、Cloud 或文件变化来源；旧 probe 意外启用、任一范围外保护文件变化。 |
| **业务期** | 在准备变化全部结束、实际 loaded set/profile1/Cloud 门通过之后，才独立封存业务前基线；至 capture-ready/timeout/terminal **且在退出前** 比较。只预声明目标 room-before `current_run.save` 的限定生产保存，probe-owned 记录另计。 | 非目标内容/length/inventory 变化、A 类 capture 证据缺失或写入失败、额外 gameplay/未知 writer、wrong loaded set/profile。 |
| **正常退出期** | 以业务期末为新起点，正常退出后独立比较 account settings、modded selector、profile1 saves/backup/native replay 及目录 inventory，并关联精确 writer 调用摘要。`settings.save` 与 `latest.mcr` 的内容变化**未获允许**；仅 `settings.save`、`prefs.save.backup`、`progress.save.backup` 的 mtime-only 变化可按既有严格条件解释：hash/length、inventory、Continue canonical、`NumReloads` 与其他保护语义均不变。 | 任一未经预声明的内容/长度/inventory/mtime 差异、writer-summary 不完整、Cloud 或退出边界不清；不得事后 rebaseline 或追认 A4。 |

恢复副本也只在未来独立运行工作包批准后创建：使用新 attempt 专属、create-new 的 workspace 证据位置，保存每个来源文件当时的 hash/length/**源 mtime**与 inventory，副本 mtime 不代替源 mtime。当前不创建 backup，不恢复任何文件。若以后需恢复，必须由 Project Lead 明确授权范围和用户动作，先核对目标身份与保护比较；profile3 或其他 profile 永不进入此方案。

## 面向项目发起人的后续准备草案（本次不发出操作请求）

Project Lead 应先与用户确定一个**独立、无业务 run**的安全准备工作包。先确认游戏已退出、r6 仍未安装，并设计如何在主菜单/受控选择界面把当前 selector 从 3 改到 1；不得默认“先进入 profile3 再切换”安全。若界面无法证明不触碰 profile3 业务状态，停止并重新设计，不自行编辑 selector 文件。`AscensionUnlocker` 的 Workshop enabled 开关及任何本地副本也需在独立准备步骤中由用户手动设为 disabled；不要把九组旧 probe 的文件存在误当 enabled。准备变更与正常退出的 `settings.save`/selector 差异只归准备期，不能并入之后的业务窗口。

准备完成并正常退出后，应重新执行同一范围安全只读预检，要求 selector 精确为 profile1、全部旧 probe/`AscensionUnlocker` 持久 disabled、r6 仍未安装、游戏/候选 hash 不变；Steam Cloud 当前关闭须在未来获批启动前由用户现场确认。只有这些门由 Project Lead 验收并另行签发安装/人工运行工作包，才可讨论 r6 安装与动作。若出现其他 enabled ID、路径/解析不明、Cloud 未确认、profile3 操作风险或保护 inventory 异常，保持停手，不安装、不启动诊断、不请求继续。

本预检不验证 ticker、readiness、writer、A4 首因、重建或任何游戏行为；`SPIKE-001B` 仍阻塞。
