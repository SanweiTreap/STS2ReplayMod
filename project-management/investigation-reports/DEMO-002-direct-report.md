# DEMO-002 direct-r1 直接后退候选报告

2026-10-26，D-095修订。当前实现结论：OBSERVED_DIRECT_BACK_RESUME_AND_STOP_MATCHED，待PM关键验收。用户反馈“测试完毕。符合预期”。两次独立Runtime session中，已有12操作clip共30次直接后退和38次正常前进核对匹配，覆盖跨EndTurn恢复后重新执行、完成后后退、退回0及中途Stop。实际恢复记录约12–52ms；没有房间重建或前缀重演。结论只限该固定环境/样本，不宣称完整MVP、任意状态恢复或正式延迟达标。原重建式R1报告、pair、clip和验收保留；PM管理文档未改。

## 已实现的路径

在PlayerLoop初态及每个完整语义操作后，仅在观看模式缓存DirectCheckpoint。每个稳定点等待一帧正常deferred通知、确认可操作和当前EndTurn/BeginEnemyTurn信号仍未完成，再复制所选数据图。没有给普通录制热路径增加每操作快照，也没有持久化快照或改变文件格式。

IdentityMemento以本会话对象作身份锚，复制字段值、有序数组/List、Dictionary、HashSet的内容并递归捕获选定数据对象。可变容器不是只保留live引用；例如卡从手牌到弃牌堆、Power对象从列表移除、字段引用换成新对象后，旧快照仍保存当时的成员/数值。每个快照各自保存这些值，可多次恢复。Rng用新的SerializableRng值副本保存完整counter/s0–s3，再用LoadFromSerializable恢复原Rng实例，保留引用关系。

明确的数据根为本房间CombatState、Player、CombatManager、run RNG/odds/ExtraFields/当前MapPointHistory。字段遍历包括相关卡牌/能量cost/动态变量/各有序pile、玩家/敌人HP/block/powers、回合及phase/ready sets、敌人状态机及MoveState/StateLog/NextMove/意图、CombatHistory和相关可变模型。Canonical模型、场景节点、网络/执行队列及整个RunState不作为可变数据图盲clone。此为固定游戏构建/当前基础牌普通战斗的候选充分性，不能由字段遍历本身推出任意内容完整恢复。

特别处理后台循环：源码显示玩家稳定点的CombatManager turn loop正等待当前EndTurnSignalSource。旧回合的信号可能已完成，因此不恢复Task、TaskCompletionSource、取消源、锁或委托。保留当前live turn loop/待完成信号/单调action IDs，仅恢复其读取的逻辑状态、集合和RNG。恢复后观察器保留Generation单调，清除旧Completed，标记已稳定；下一次仍由原TryManualPlay/EndPlayerTurnAction执行，必须再次与录像After匹配。本轮已观察到第4/8步EndTurn回退后重新执行匹配，不外推任意后台异步内容。

后退路径现为checkpoint.Restore→刷新局部UI→比较目标投影→提交游标并暂停。没有调用ClearRoom、LoadRoom、CleanUp、MemorySession.Restart或Execute前缀。ClearRoom只留在最终Stop/失败退出；LoadRoom只用于打开录像的初态。

UI保留房间及生物节点，局部重建手牌holder和能力图标；刷新HP、topbar、能量、stars、遗物和敌方意图，修正能力增删后的可视订阅，使用正常StateTracker通知刷新派生卡值。没有通过治疗、抽牌、获得block等新语义操作凑数。等待一帧UI后检查实际手牌节点顺序及逻辑投影，成功才提交位置。死亡/生物成员变化、宠物暂不支持，拒绝并记录，不隐式退回重播。

沿用既有互斥游标；内部Rebuilding名称是上一实现遗留的导航忙标记，本路径实际执行直接恢复。恢复期间Step/播放/重复后退禁用，Stop安全退出。Stop释放本会话全部checkpoint。

## 兼容与事件

增加单条明确兼容规则：DEMO-002/build-001录制hash `8A7A03F9190EE81EFFD024013518B9697A7A9A12E44D1658353013F65984D6C7` 的格式1可在当前候选播放。该版checkpoint/operation/After schema和游戏逻辑来源与本次相同；仍要求精确游戏SHA、profile scope、only ReplayMod:0.1.0和全部内容检查/checksum。没有改clip的ModHash、时间或其他原始元数据，没有接受任意旧build或一般跨版本。打开事件同时记录RecordedModHash、PlaybackModHash及已知同格式规则，因此可直接使用用户刚录的12操作文件，不必新建局。

新事件：checkpoint-captured(position/turn/数据对象及写入数量/耗时)、checkpoint-restore-start/arrived/stopped/failed(from/target/实际到达/核对/耗时/RoomRebuilt=false/PrefixOperations=0)。playback-room-load只记录实际首次房间加载。恢复后的普通playback-step仍按forward核对；不把前缀事件重命名成直接恢复。

## 构建、检查、安装

候选direct-r1/build-002，DLL SHA256 `C825AF5E111AC5C76DE2815EFEA40A9DC5AC4DE18B58C77B2F30CA0BF4992D1B`，manifest版本仍0.1.0。构建0警告/0错误，pair在`evidence/demo-002/direct-r1/builds/build-002/`。未重跑原9项或旧游戏矩阵。

`direct-tests/DirectContracts.csproj`的5项针对性检查通过：有序pile/Power移除及字段替换后身份与数值恢复；独立快照反复恢复不漂移；集合/数组/含引用struct复制；恢复逻辑旧回合和RNG时保留当前待完成信号；明确已知录制build可读且保留原元数据，其他build及null兼容绕过拒绝。结果`evidence/demo-002/direct-r1/direct-contracts.json`。这些不运行游戏或其构造器，不能当跨EndTurn实测。

本轮用户已有新进度，因此安装前重新备份当前账号完整本地状态：`C:/Projects/STS2ReplayMod-local/backups/DEMO002-20261026-211616-87fc23469c/`，1270文件/131996144字节，逐个读回hash匹配，源稳定；被替换的可用R1 pair/settings/selector另存其preparations。安装收据`evidence/demo-002/direct-r1/preparation/build-002-f4cfa414.json`，installed=sealed、只配置ReplayMod/modded1、game0。Cloud沿用用户本批明确关闭的确认，没有自动改Cloud、启动/操作游戏、恢复存档或读另一账号。旧所有恢复点/录像仍保留。

## 提供给用户的最小手动验证（计划）

沿用已确认关闭Cloud及原账号modded1，手动启动并打开现有12操作录像。无需重录，不进正常Continue或新建局。

1. 初态0/12，Step至1（Defend）→后退到0→再Step到1。确认直接呈现旧手牌/能量/block，未重置房间或播放旧牌。
2. Step到3后，再Step执行第4步EndTurn，到4/12。后退4→3应直接恢复原玩家回合末状态与手牌/敌方意图；再Step执行同一个EndTurn，应回到4/12并显示一致。
3. 可顺着本片段到第8步EndTurn，再8→7→Step到8，覆盖更后面的抽牌/敌人移动状态；不要求额外矩阵。
4. 在未播完位置Stop回菜单，再打开应从0开始；Stop并正常关闭。若失败记录短原因/位置，停止后续动作，让自动退出或正常关闭，不继续普通进度。

## 实际用户运行与证据

用户已按自己的自然操作完成，反馈符合预期；实际比计划更充分，无需补跑矩阵。公开脱敏事件：`evidence/demo-002/direct-r1/runtime-r1.json`。两次Runtime session均加载本候选hash、only ReplayMod:0.1.0/profile1，打开同一个旧录制clip，事件记录known-DEMO002-R1-identical-format及原/当前两个hash。原clip完整文件hash与R1私有归档相同，兼容没有更改原文件。

| Runtime session | 初态匹配 | 正常前进匹配 | 直接恢复匹配 | 实际房间加载/前缀 | 恢复记录耗时 |
| --- | --- | --- | --- | --- | --- |
| 3f24ac0836de486087e86527fda8745a | 1 | 26 | 22 | 1 / 0 | 12.0385–51.517ms |
| 5ba807c146e2454993ba9129c6d7659d | 1 | 12 | 8 | 1 / 0 | 12.0545–30.144ms |

共30个restore-start/arrived配对，全部Target=Arrived、StateMatched=true、Playing=false、RoomRebuilt=false、PrefixOperations=0；实际仅各一次首次playback-room-load，38个playback-step全部Mode=forward且StateMatched=true，没有back-prefix或failed/state-difference/observer-error。共40个checkpoint-captured事件包含重复访问位置的更新。

第一session实际轨迹的关键点：2→1→0再出牌；第4步EndTurn后4→3→原第4步EndTurn匹配（之后又重复一次）；第8步EndTurn后8→7→原第8步EndTurn匹配；正常执行9、10、11、12到片段末，之后12→11证明完成后后退；再逐步退到0，并从0继续执行1–4。其间也有普通牌回退后重出，如11→10→Step11。不是仅恢复旧投影后停住。

该session在位置3/12且Busy=true时接受Stop，第4步EndTurn已在执行，日志先完成该操作核对再closed.Position=4，符合语义边界Stop；closed.Error=null、OriginalManagerRestored=true、NormalContinueAndOriginalProgressUnchanged=true。虽然曾访问完成位置12，退出前已回退，因此closed.AllOperationsMatched=false只描述退出时未在完成位置，不能用它抹掉曾播完的实际step12事件，也不能报告退出发生在片段末。

第二session重新从磁盘打开并初态0，1→0→Step1匹配，播放到8后8→7，再连续退/进（第7步同牌多次重出匹配）并回到4。最终位置4/12、Busy=false时Stop，closed同样无错误且原manager/所比较Continue及progress保持。两次使用不同Runtime.SessionId且各Open=1；没有逐OS进程PID绑定，因此仅据此确认跨Runtime session从磁盘重开，不额外声称已保存完整进程身份链或同session二次open样本。

恢复计时是checkpoint.Restore包含数据写回、局部UI刷新及等待一帧后核对的记录，不是显示器端延迟测量或发布阈值。直接呈现体验来自用户总体确认；没有逐个控件视觉日志。实际证据说明该样本的恢复与后续逻辑足够，不证明未投影/未触发的所有内容。

完整原clip及两个session原日志私有归档到`C:/Projects/STS2ReplayMod-local/backups/DEMO002-20261026-211616-87fc23469c/runtime/direct-r1/`，公开收据`evidence/demo-002/direct-r1/runtime-archive.json`，clip复制hash读回且OriginalR1ClipHashUnchanged=true。没有发布原生checkpoint。读回时game0，installed仍等于sealed build-002；本次收尾未重构建/重跑离线测试、未换DLL/恢复存档/控制游戏。

## 结论边界与下一步

本固定12操作铁甲/Seapunk样本已给出直接后退、同牌及跨EndTurn继续、完成后后退、退0/重复恢复、跨Runtime session读旧文件及两种中途Stop的实际证据。候选仍安装保留可用，提交PM验收此能力增量。

未覆盖死亡/生物成员变化/宠物、奖励或其他房间、任意敌人/卡牌/遗物、多人与一般跨版本；没有恢复中Stop或同Runtime二次open样本。正常录制无每操作快照，但本轮未量化其发布开销。下一步应按PM决定扩展能力，不为已经回答的直接后退问题追加大矩阵，也不把本候选当完整MVP。
