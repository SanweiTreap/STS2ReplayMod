# 当前待办

更新：2026-11-08。两路推进沿 D-153。近期阶段与验收顺序见[路线图](ROADMAP.md)。

## 当前两路工作

- [RUN-005](investigation-reports/RUN-005-report.md) 代表性整局在 074 从涅奥播到原生胜利详情并返回原历史，含手动导航；纯 Auto 留作后续回归。076 实验体二阶段击倒音效复测通过。
- 战斗恢复：[COMBAT-PRESENTATION-001](handoffs/COMBAT-PRESENTATION-001.md#build-060-共用恢复边界的覆盖规则) 已按代表机制实测。068 日志确认停止召唤与碎骨的原生声音。069 的碎骨、敌方回合在途 Stop 与重开短测均通过。见[069 Stop 实测](investigation-reports/COMBAT-PRESENTATION-001-report.md#build-069-首次-stop-实测)。
- 录像库加载（独立）：当前主菜单库在出现前同步读取 24 份整局、约 162 MiB。同库生产读取与路线整理约 1.3–1.4 秒，056／059 相近。单独排查如何先展示列表、再按需读取详情。见[实测报告](investigation-reports/COMBAT-PRESENTATION-001-report.md#build-059-首轮实机归类)。
- UI：[UI-001 小鸡煲原型](technical-notes/REPLAY-NATIVE-UI.md#ui-001-小鸡煲游戏内原型) 已完成首轮实机体验。录像详情 v3 样稿初步获认可，覆盖逐房间浏览、抓牌前后牌组、大牌组滚动与批注；接真实录像后再验收。
- [UI-002](handoffs/UI-002.md) 原生历史入口已验收，073 按钮效果已获用户确认。[SEEK-002](handoffs/SEEK-002.md) 已派发，合并首次定位、桌宠上一／下一回合、原生地图房间跳转。两个落点分别验收，再连成同一条实机短测。
- 共享历史与录像关联后，卡册先接收藏入口和一份真实录像详情，核对房间路线、抓牌候选和前后牌组，再接批注、牌包与导入导出。播放设置继续独立推进。

## RUN-004历史交接点

- r6代表跨幕完整观看、r7换幕Loading中公开Stop/重开已通过；精英Back、实际保存点跨进程Continue沿已有证据收束，准备局Opening=null，不当涅奥到第二幕完整录播。
- 保留004与当前原生point12第二幕首普通完成未领奖，补存新进度、不restore；不继续扩Boss/古神/角色或任意Stop矩阵。
- D-147当前最终路线应剪掉SL/Continue重建观看过程，后台必要恢复保持；两次暗亮是暂留呈现缺口。RUN-004未实现隐藏过场，后续不扩进RUN-005录制任务。

## RUN-003历史收束

- 铁甲A0新叶单张变形：奖励前真实输入经原生新局流程到涅奥/地图/首战胜利，资源/RNG与胜利Back重做有证据；选择中、首战入场中Stop/重开与原context恢复通过。
- 主页整局入口、新片开局与旧局合法起点已实用；run-003准备及单文件夹安装已实际识别。坏条目隔离有合同，独立分场按钮/全部焦点组合GUI未专测。
- 限制仍为未实测：无准备候选、其他涅奥奖励/窗口、多张变形、Neow SL及其他角色新开局。记录限制，不扩成完成当前段的全套前置。

## 后续候选

完整录制与UI讨论之外的全部难度/机制、三种模式实现、隐藏SL呈现/导航/新恢复平台另行安排，不借当前任务自动扩展。沿已有Implementation短交接，不擅自新建对话或改模型。详见[整局规划](technical-notes/RUN-REPLAY-PLAN.md)。

完整内容/场景、通用文件权威恢复/未访问Seek、节奏/倍速/路线图/导入与发布预算见[MVP](MVP.md)/[Roadmap](ROADMAP.md)，不重复全部历史矩阵。

## 协作

PM按阶段维护本页和CURRENT_STATUS，细节由Implementation维护一个任务报告，DECISIONS只记新决定。沿[WORKFLOW](WORKFLOW.md)执行；游戏仍由用户操作，无Computer Use/自行headless。模型设置保持用户选择。
