# Replay Mod 项目管理索引

本目录是项目的长期记忆。每次 Project Lead 工作开始时，先阅读 `CURRENT_STATUS.md`，再按任务需要查阅其余文档。

## 核心文档

- `CURRENT_STATUS.md`：当前阶段、已确认事实、未知项、风险与下一步。
- `REPOSITORY.md`：GitHub私有仓库、提交范围与多对话Git协作。
- `ROADMAP.md`：阶段目标、验证顺序与进入/退出条件。
- `TODO.md`：可交给 Implementation Codex 的近期任务与验收标准。
- `INVESTIGATIONS.md`：技术调查结论、证据与仍待验证的问题。
- `DECISIONS.md`：已作出的重要决策及其依据；未决定事项不得写成决策。
- `HANDOFF_TEMPLATE.md`：Implementation Codex 的统一交付格式。
- `TEST_PROTOCOL.md`：备份/恢复、默认测试槽和简明人工测试协作；用户已放宽原个人槽保护，按当前D-092执行，不沿用历史禁读。
- `handoffs/`：已拆分、可独立执行的 Implementation Codex 任务说明。
- `USER_INPUT_CHECKLIST.md`：需要项目发起人逐项回答或运行验证的问题清单。
- `MVP.md`：单人 MVP 玩家闭环、已确认要求、验收方向与排除项。
- `EXPERIMENT_PLAN.md`：从定向 hook 调查到最小探针、性能、确定性、恢复、导航、时序与导入的验证顺序。

## 维护规则

1. 已观察到的事实、外部报告、推断和假设必须明确区分。
2. 技术结论应注明适用游戏版本与证据来源。
3. Todo 只有在验收证据齐全后才能标记完成。
4. 阶段切换前必须核对 Roadmap 的退出条件，不能用“已经写了代码”代替验证。
5. 会影响后续工作的聊天结论必须在本目录落盘。
6. 当前 milestone 之外的问题记录到 backlog，不顺手扩大范围。
7. D-092允许先构建可体验原型，按实际缺口验证；不要把Roadmap所有研究门当每次写代码前的审批。大段旧状态/任务已归档到 `archive/2026-10-26-before-D092/`，开始任务只读当前页和工作包。
