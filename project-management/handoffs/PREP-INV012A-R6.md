# PREP-INV012A-R6 — 简化的人工测试环境准备与只读复核

状态：`COMPLETE / READY_R6_INSTALL_REVIEW`（2026-10-09验收；原签发2026-09-28）。结果见 `../investigation-reports/PREP-INV012A-R6-report.md`；后续已另签 `SPIKE-INV012A-RUNTIME.md`。下文保留原准备合同，不继续执行。本工作包本身**不授权修改探针代码、安装r6或执行SPIKE-INV012A诊断**；也不授权Codex启动/控制游戏。项目发起人已允许使用modded profile1/2/3及切换模组。唯一须作为个人进度保护的是不加载模组的profile1。

## 目的

让已验收的R6原候选逐步满足其既有身份门：未来诊断启动时应为modded profile1，且实际只加载`SPIKEINV012A`。当前R6尚未安装；上次只读预检看到modded selector=3与`AscensionUnlocker`启用，这不是用户配置错误，只是R6原门禁不接受。

本工作包只处理**安装前的profile选择**：由用户保持当前modded/`AscensionUnlocker`环境，按熟悉的游戏界面流程将modded选择切到profile1，停留主菜单、不进入任何对局后正常退出。本阶段**不禁用`AscensionUnlocker`**：在R6尚未安装时，不先制造“没有启用模组”的启动状态，以免意外切入不加载模组的profile1。若界面已经转到不加载模组的profile1或出现其Continue，立即停手，不点Continue、不开始对局，并报告界面状态。若无法在modded环境内完成，停止并交回Project Lead；不自行编辑配置文件。

Implementation Codex只能向用户给出这一轮具体、简短的人工准备请求，说明预期画面与上述停止条件；不得使用Computer Use或替代GUI。准备前请用户现场确认Steam Cloud当前关闭；若无法确认，先不开始。不要要求用户为准备步骤建立战斗、领取奖励或运行旧probe。

## 准备后只读复核

用户报告已退出且游戏进程结束后，仅按既有范围过滤方式读取account`settings.save`的`mods_enabled`与enabled ID集合、modded`profile.save`的`last_profile_id`，并核对固定游戏/r6候选身份及游戏进程=0。不得读取任何profile的save/progress/prefs/replay payload，特别是不加载模组的profile1。允许settings/selector因用户明确的准备动作变化；它们不与旧预检hash比较成业务FAIL。

只有`last_profile_id=1`、`mods_enabled=true`、`AscensionUnlocker`仍按本轮准备预期启用、旧probe均持久disabled、R6仍未安装、游戏已退出且未触及不加载模组的profile1时，报告`READY_R6_INSTALL_REVIEW`。任一条件不满足则报告`NEED_ENV_PREP_REVIEW`并停手，不循环试错、不安装、不请求诊断启动。未来安装R6之后，须另行安排禁用`AscensionUnlocker`与runtime loaded-set验证；持久enabled列表不能替代runtime证明。

交付一份简短的`project-management/investigation-reports/PREP-INV012A-R6-report.md`及范围过滤的预检摘要；只写必需的profile ID、mod ID和hash，不输出账号数字或原始配置payload。Project Lead验收后才另行签发R6安装与人工诊断阶段。旧`INV-013`不执行；R6候选及证据、A4和其他旧probe均不修改或重跑。
