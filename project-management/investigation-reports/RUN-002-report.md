# RUN-002：宝箱、商店、问号与SL尝试归属

## 最新摘要 · 2026-10-31 · 001首个增量已安装，尚无游戏结论

- PM管理包6份原文已独立提交推送885a2f9。当前RUN-002/build-001：DLL `2FB5E5938104CA23256FFF25895DD2AB27A324A31CDD5484704F4149F4C584F3`，manifest保持，收据`preparation/build-001-5dbc3c77.json`。此增量先验证成功存盘锚点、房间尝试和原生宝箱，商店/问号/事件内部战斗仍在本包待接，不宣称整包完成。
- 原始操作/战斗片全部保留，房间尝试记录parent和恢复存盘；最终路线仅选择原始操作身份，不按内容去重。成功native Save任务后读回内容必须匹配该次请求，另存完整native load表示fingerprint及当时路径；恢复只匹配已观察成功写盘，不能用seed/坐标/金币或去掉计时字段的逻辑RunId键猜恢复位置。缺证/歧义保留独立尝试及缺口。
- 宝箱按原生开箱、生成内容、领取遗物、放弃并离开拆开，原生PickRelicAction限当前观看操作，RelicCmd.Obtain及共用选牌沿原函数。观看场景退出取消宝箱集合等待，并关联native延迟启用按钮的场景token；不为Stop留下旧等待。额外奖励页暂明确未接入，普通游戏继续且记录缺口；此为分段增量，非所有宝箱遗物组合通过。
- 主工程/DirectContracts均0警告0错误；7项分支选择合同、8项原有文件/位置合同通过，覆盖A→load→B、相同B独立尝试、重复重进父子、指纹歧义拒绝、完整字段fingerprint、原事实/路径封装读回和完成Continue位置。实际RUN-001两根root新模型生产读取/checksum通过，旧006录制hash显式兼容。以上均非原生运行证明。
- 用户确认Cloud仍关闭、无新进度。当前51文件4,384,674 bytes补存`RUN002-pre001-current-20261031-215540`，与RUN001/r10逐hash相同；安装后仅配置改写、进度未变，game0，无restore/自行启动。原RUN-001/006 pair与所有原片/失败/恢复点保留。证据本地`evidence/run-002/checks/`、`preparation/`。
- 下一步第一次短录：默认关闭的“RUN-002 宝箱SL短路线”开新铁甲A0，普通→宝箱→普通，原3防御+和加2全身撞击+先天/保留、40HP。首战胜利正常领奖，进入宝箱、开箱领取遗物并完成原生后续选择，先不离开宝箱房，保存退出并正常关闭游戏。先核入口存盘/第一次尝试，再用同一局重进核相同操作仍独立、改为不领取后的最终路线及后战自然状态，不提前拼接或补判。

## 001：首个可测增量

沿现有RunDocument追加可选Branches，旧文件字段缺失时序列化不变；新文件保存原始连续Sequence、操作Attempt归属、RoomAttempt父子与来源、成功RunSavePoint及其路径快照。恢复使用冻结在原生RunState重构前的加载DTO，与成功写盘时保存的完整native-load表示比较；保留num_reloads/run_time/save_time用于位置指纹，原RecordingJournal去除这些易变字段的SaveKey仍只负责逻辑RunId，不代替位置证据。写盘被另一请求覆盖而不能对应读回时，留unresolved诊断，不把新内容绑定到旧位置。

每次已证实恢复先选择该存盘路径，再创建新房间尝试；旧操作/片段不删除、不按同名同价同结果去重。当前最终播放视图重新编号仅用于内存执行，SourceSequence/Attempt保留原身份，原件不改；Continue位置亦映射到选中后续操作，已回滚的领奖不会串入新路径。菜单展示采用路径、保留尝试/操作数量，旧记录明示无房间分支索引，保持其已验证合法观看。新非战斗Back/跨房间Seek仍禁用。

宝箱生成入口沿TreasureRoom/NTreasureRoom及TreasureRoomRelicSynchronizer，开箱到遗物展示先作为一个操作；领取通过原生PickRelic及RelicCmd.Obtain任务记录资源/RNG和可能的NDeckCardSelectScreen最终选牌。放弃沿原生Proceed，不另多发一次Skip请求。商店的MerchantEntry.OnTryPurchaseWrapper（删牌另有取消wrapper）、EventSynchronizer及事件内部房间栈的原生入口已定位，需后续同包实装与代表样本，未将目录/字符串Subrooms当播放支持。

第一轮准备只修改下一显式启用的新局，沿真实相连地图节点设普通→宝箱→普通并在记录checkpoint前完成；不对Continue/观看再次注入。构建与模型检查完成后尽早人工验证宝箱和恢复位置，避免尚未验证的尝试归属被多个房间处理器共同使用。用户亲自操作游戏，不Computer Use/替代GUI/headless，不为本增量重新跑三鼠或五角色矩阵。
