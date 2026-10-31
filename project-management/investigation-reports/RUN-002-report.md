# RUN-002：宝箱、商店、问号与SL尝试归属

## 最新摘要 · 2026-10-31 · 001/r1首次宝箱录制通过，入口存盘已核，尚未Load

- 保持RUN-002/build-001，DLL `2FB5E5938104CA23256FFF25895DD2AB27A324A31CDD5484704F4149F4C584F3`，收据`preparation/build-001-5dbc3c77.json`。r1新局b0d06809：首战8步胜利，正常领奖后进入第3层宝箱，开箱/领取白星/离开到地图均记录complete；商店、问号/事件内部战斗仍在本包待接，尚未播放或验证SL主线。
- 65事件、0 observer-error、0 unresolved save、10项原始操作、3访问/3尝试、5次成功存盘观察。实际root及胜利片生产读取/checksum通过，原件未改。原生开箱后金币117→163，领取遗物RELIC.WHITE_STAR，保留完整开箱/领取/前进三项事实。
- 用户误点前进仅打开地图，没有选择下一节点/新增存盘。当前文件完整fingerprint `E3F22514BCD1FF318869317EF2EC3C9DB06C3CF5D104EC621AC10F9C949ED4A8`精确匹配save-point5：visit3/attempt3、未完成房间、路径[1..7]。存档金币117且没有白星；实际领奖为raw8/9/10。不是凭“前进不会存”假定，而是已读回当前文件匹配入口锚点；恢复前不提前截断当前路线。
- 69数据文件封存；51当前已知文件4,378,599 bytes补存`RUN002-post-r1-current-20261031-230532`，6个正常录制/配置backup文件变化，无缺失，game0，无restore/自行启动。关键本地证据`runtime-verification-r1.json`、`collection-r1.json`、`checks/r1/`；RUN-001/006与用户原新进度恢复点保留。
- 下一步同局真正重启Continue，应回宝箱入口；再次开箱领取、完成后续选择并等结算，不离开宝箱房，保存退出且正常关游戏。核完整native-load指纹是否匹配point5、同visit entries增长、新attempt parent3/restoredSave5、原8/9/10仍保留而选中路径恢复到入口后再追加。若内容相同也应是独立尝试，生成结果/RNG另行核，不预设一定相同。
- 之后再安排一次改为不领取并进入后战，用最终实际延续路线检查不重复领物/加钱；既有模型合同不代替这些原生结论。001共用NDeckCardSelectScreen及宝箱领取后选择尚无本轮样本，附加奖励页明确暂未接入；新非战斗Back/跨房间Seek仍禁用。普通迭代直接用户短测，不逐轮唤醒PM。

## 001：首个可测增量

沿现有RunDocument追加可选Branches，旧文件字段缺失时序列化不变；新文件保存原始连续Sequence、操作Attempt归属、RoomAttempt父子与来源、成功RunSavePoint及其路径快照。恢复使用冻结在原生RunState重构前的加载DTO，与成功写盘时保存的完整native-load表示比较；保留num_reloads/run_time/save_time用于位置指纹，原RecordingJournal去除这些易变字段的SaveKey仍只负责逻辑RunId，不代替位置证据。写盘被另一请求覆盖而不能对应读回时，留unresolved诊断，不把新内容绑定到旧位置。

每次已证实恢复先选择该存盘路径，再创建新房间尝试；旧操作/片段不删除、不按同名同价同结果去重。当前最终播放视图重新编号仅用于内存执行，SourceSequence/Attempt保留原身份，原件不改；Continue位置亦映射到选中后续操作，已回滚的领奖不会串入新路径。菜单展示采用路径、保留尝试/操作数量，旧记录明示无房间分支索引，保持其已验证合法观看。新非战斗Back/跨房间Seek仍禁用。

宝箱生成入口沿TreasureRoom/NTreasureRoom及TreasureRoomRelicSynchronizer，开箱到遗物展示先作为一个操作；领取通过原生PickRelic及RelicCmd.Obtain任务记录资源/RNG和可能的NDeckCardSelectScreen最终选牌。放弃沿原生Proceed，不另多发一次Skip请求。商店的MerchantEntry.OnTryPurchaseWrapper（删牌另有取消wrapper）、EventSynchronizer及事件内部房间栈的原生入口已定位，需后续同包实装与代表样本，未将目录/字符串Subrooms当播放支持。

第一轮准备只修改下一显式启用的新局，沿真实相连地图节点设普通→宝箱→普通并在记录checkpoint前完成；不对Continue/观看再次注入。构建与模型检查完成后尽早人工验证宝箱和恢复位置，避免尚未验证的尝试归属被多个房间处理器共同使用。用户亲自操作游戏，不Computer Use/替代GUI/headless，不为本增量重新跑三鼠或五角色矩阵。
