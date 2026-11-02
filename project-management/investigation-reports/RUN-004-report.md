# RUN-004：精英、Boss与首条跨幕链

## 当前摘要（2026-11-02）

- 本包进行中；首个精英增量 build-001 待安装/人工记录，尚无 RUN-004 原生运行结论。RUN-003/006 的验收不外推到本包。
- 共用录制入口、输入可用边界、地图播放分派、完成房间 Continue、文件读取接入 Elite/Boss；format10 允许第一、二幕。新增可选幕模型、实际房型、原生 ActFloor/TotalFloor；旧片缺字段不补历史，省略空字段保持旧序列化。观看核同幕/模型/房型/楼层及既有完整状态/RNG。
- 默认关闭的一次性新局准备：铁甲 A0 暗港，原生首节点改精英并与本幕原生 Boss 节点相连；不替换精英/Boss 模型，不在观看中改地图或杀怪。三张防御+、两张岿然不动+、两张全身撞击+先天保留，准备背包、灯笼及三瓶能量药水；全部在录制战前 checkpoint 前创建/登记。起点为精英战前，跳过的幕中房间和之前涅奥不当连续录播证明。
- 首候选只安排精英记录/独立起点观看；Boss 奖励离开、换幕同步、第二幕 Ancient 选择及 Continue 跨幕归属仍待实现/验证，不把放开房型当跨幕完成。非战斗 Back/跨房间 Seek 仍禁用。
- 原生已确认：Boss 奖励页按钮直接经 ActChangeSynchronizer.SetLocalPlayerReady → VoteToMoveToNextActAction → EnterNextAct → EnterAct；不调用普通 ProceedFromTerminalRewardsScreen。后续须单独记录这次实际输入/其自然结果，避免双执行。
- 用户已确认本轮 Cloud 关闭、无新进度；实读当前仍铁甲种子 QL4MLA7ZADU6、首幕首战完成未领奖。game0，补存 `RUN004-pre-build001-current-20261102-194404`，51 文件/4,149,576 bytes，与 RUN003/r5 补存相比零变化/零缺失，全部哈希读回；不 restore，另一账户排除。006 保留至新候选安装，唯一目录 mods/ReplayMod。
- Release 主体与 DirectContracts 构建均 0 warnings/0 errors；15 项 library、11 项 run-envelope、11 项 run-branches 合同通过，新合同区分同坐标不同幕、保留可选身份、旧格式省略新增字段，并按预期 metadata 错误拒绝越界。旧 RUN-003 root/format10 clip 用生产 reader 校验通过且未改原文件；均不是原生动态通过。证据 `evidence/run-004/checks/build-001-*.json`。

## 首候选实现边界

录制的 journal 战斗键原已含 act:row:col；RoomVisit.Id/attempt 为逻辑局内单调序号，当前新增字段记录实际幕模型和原生楼层，不以重复地图坐标合并访问。列表、分场起点和播放状态显示幕号。

记录存档前源由现有原生 SaveRun 冻结，进入可操作点再记实际房型/ActFloor/TotalFloor；自然下一战和独立入口同时核 BattleContext，沿已有输入、快照、任务退出和原 manager 恢复路径。原生运行结果待人工记录后补，数据合同仅证明格式/归属，不证明动画、Boss 或跨幕成功。
