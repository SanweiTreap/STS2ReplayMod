# RUN-004：精英、Boss与首条跨幕链

## 当前摘要（2026-11-02）

- 本包进行中；保留 **build-001**，源码 `c835e7e`。r1 暗港精英 Skulking Colony 的真实16步胜利自动封存、40金币与血瓶领取记录及生产读取通过；**尚未观看验证**，Boss/跨幕仍未完成。RUN-003/006 的验收不外推到本包。
- 共用录制入口、输入可用边界、地图播放分派、完成房间 Continue、文件读取接入 Elite/Boss；format10 允许第一、二幕。新增可选幕模型、实际房型、原生 ActFloor/TotalFloor；旧片缺字段不补历史，省略空字段保持旧序列化。观看核同幕/模型/房型/楼层及既有完整状态/RNG。
- 默认关闭的一次性新局准备：铁甲 A0 暗港，原生首节点改精英并与本幕原生 Boss 节点相连；不替换精英/Boss 模型，不在观看中改地图或杀怪。三张防御+、两张岿然不动+、两张全身撞击+先天保留，准备背包、灯笼及三瓶能量药水；全部在录制战前 checkpoint 前创建/登记。起点为精英战前，跳过的幕中房间和之前涅奥不当连续录播证明。
- 首候选只安排精英记录/独立起点观看；Boss 奖励离开、换幕同步、第二幕 Ancient 选择及 Continue 跨幕归属仍待实现/验证，不把放开房型当跨幕完成。非战斗 Back/跨房间 Seek 仍禁用。
- 原生已确认：Boss 奖励页按钮直接经 ActChangeSynchronizer.SetLocalPlayerReady → VoteToMoveToNextActAction → EnterNextAct → EnterAct；不调用普通 ProceedFromTerminalRewardsScreen。后续须单独记录这次实际输入/其自然结果，避免双执行。
- 用户已确认本轮 Cloud 关闭、无新进度；实读当前仍铁甲种子 QL4MLA7ZADU6、首幕首战完成未领奖。game0，补存 `RUN004-pre-build001-current-20261102-194404`，51 文件/4,149,576 bytes，与 RUN003/r5 补存相比零变化/零缺失，全部哈希读回；不 restore，另一账户排除。006 保留至新候选安装，唯一目录 mods/ReplayMod。
- Release 主体与 DirectContracts 构建均 0 warnings/0 errors；15 项 library、11 项 run-envelope、11 项 run-branches 合同通过，新合同区分同坐标不同幕、保留可选身份、旧格式省略新增字段，并按预期 metadata 错误拒绝越界。旧 RUN-003 root/format10 clip 用生产 reader 校验通过且未改原文件；均不是原生动态通过。证据 `evidence/run-004/checks/build-001-*.json`。
- 安装收据 `evidence/run-004/preparation/build-001-d20df527.json`：DLL `8178809C473A012396E85374C9BD2865AD126F7179BCF9DF48DEAFFF719189CC`，manifest `69185E5024D560BC5C9B81FC64CC2F732745BCFBA31E64F069CAEF6AAEABE8FB`。pair 已封存在 `builds/build-001`，006 原 pair 安装前另存并核 hash，根 mods 无第二份；安装后 51 已知文件仅 settings.save 发生预期变化，零缺失、game0，未 restore。见 `preparation/post-build001-check.json`。
- 最新实况：r1 用户额外领取40金币和血瓶，两个 reward 均 complete、未破坏录像，无需重录。当前原生存档 fingerprint 精确匹配 point4 `[1,2,3,4]` 精英胜利未领奖，未包含随后操作5/6；不把录像内领取说成原生持久化，后续 Continue 依实际 point4 恢复所选分支。
- 下一步：用现001从精英合法战前起点观看，手动/Back 原生重做及 Auto 胜利与两项领奖，Stop/重开；随后沿同一新代表局继续奖励/Boss/跨幕，不重打旧普通链。最新补存 `RUN004-post-r1-current-20261102-200240`，94数据文件及51已知文件/4,166,194 bytes，零缺失、game0、无 restore。

## 首候选实现边界

录制的 journal 战斗键原已含 act:row:col；RoomVisit.Id/attempt 为逻辑局内单调序号，当前新增字段记录实际幕模型和原生楼层，不以重复地图坐标合并访问。列表、分场起点和播放状态显示幕号。

记录存档前源由现有原生 SaveRun 冻结，进入可操作点再记实际房型/ActFloor/TotalFloor；自然下一战和独立入口同时核 BattleContext，沿已有输入、快照、任务退出和原 manager 恢复路径。原生运行结果待人工记录后补，数据合同仅证明格式/归属，不证明动画、Boss 或跨幕成功。

## r1：真实精英录制与额外领取（001）

会话 `c7d1f021ccb0431699411c39438d439c`，90事件，零 error/failed/difference/recording-gap 事件。局 `2b0f7e321a3a4dcd9caf9ee83435a06f`，种子 `KTRJB2SKRSYV`，铁甲A0暗港；原生首精英 `ENCOUNTER.SKULKING_COLONY_ELITE`，片 `be74f0a6bbb943578c3ec700c328bfbd`，format10，16步胜利自动封存，生产payload读回完全匹配。实际 Act=0、ActModel=ACT.UNDERDOCKS、RoomType=Elite、ActFloor=2、TotalFloor=2；未按普通房型误记。

原始root6操作/2访问/2attempt，selected `[1,2,3,4,5,6]`：涅奥选择石头加湿器、离开、地图、精英战、40金币、血瓶。root 777,223 bytes、clip 1,047,414 bytes。准备局缺奖励前新局输入，涅奥visit保留缺口标记，不提供从涅奥观看、不把这段准备前历史作为连续性实证。精英visit无缺口，之后奖励资源连续；生产 RunStore/Codec 实读通过，不改原件，不等同观看/Back通过。

原生成功存盘point1–4依次为涅奥入口、选择完成、精英战前、精英胜利；当前 current_run fingerprint 精确匹配point4，原生99金币/无血瓶，与当时成功保存资源一致。随后操作5/6在录像中使金币99→139、增加RELIC.BLOOD_VIAL，但未触发下一次原生SaveRun，正常保存退出未把这两步写入新的成功保存点。后续Continue分支应按point4续接，原5/6保留原始事实，不与重领奖重复拼接。

补存94数据文件、51已知当前文件，实际新局/录制导致latest.mcr、modded1 current_run及backup、progress及backup、settings.backup共6项变化，均保留；无缺失/无restore/另一账户未读。证据 `evidence/run-004/runtime-verification-r1.json`、`collection-r1.json`、`checks/r1-run.json`、`checks/r1-clip.json`。
