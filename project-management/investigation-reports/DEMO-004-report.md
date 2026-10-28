# DEMO-004 双怪跨中途死亡直接后退

2026-10-27。Implementation结论：**FIXED_SAMPLE_RUNTIME_PASSED / SUBMITTED_FOR_PM_REVIEW**。用户确认“测试完毕。符合预期”，并补完成胜利后Stop/正常退出。沿D-099 / DEMO-004工作包，未扩大为召唤、胜利终点后退或全部内容支持；PM正式验收结论由PM维护。

## 当前结论

DEMO-004/build-001已构建、封存并安装，仅ReplayMod 0.1.0 / modded1。DLL SHA256 `ECC7F2516BCD8337968CB8D2798F3D81FC57F68B4B9DADE3D15A86D8A9D97987`；manifest SHA256 `69185E5024D560BC5C9B81FC64CC2F732745BCFBA31E64F069CAEF6AAEABE8FB`。游戏SHA沿固定v0.111.0 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`。安装收据见`evidence/demo-004/preparation/build-001-b9fde522.json`，封存pair位于`evidence/demo-004/builds/build-001/`。

源码构建零警告/错误；6项纯.NET memento检查及实际旧9步Shrink Beetle胜利文件兼容检查通过，原文件/元数据未改。随后取得真实双蛞蝓11步胜利录像、两次独立打开，42次forward、31次直接恢复全部匹配；其中7次5→4跨首次击杀，恢复同一Creature/CombatId及同伴能力/AI/姿态。第一次中途Stop，第二次播完胜利后Stop，两次退出无Error，原manager及所比较Continue/progress保持。详见下方实测记录，不将纯.NET检查当作原生运行证据。

已验收起点DEMO-003/build-003及其旧报告/失败证据保持。此前“测试ok”属于DEMO-003显示修复；本次新的“测试完毕。符合预期”及新双怪日志单独构成DEMO-004证据。

## 实现

原生CreatureCmd.Kill会执行真实死亡/AfterDeath hooks，再从CombatManager及CombatState移除敌人。移除解除StateTracker订阅、清空怪物状态机并让Creature脱离CombatState；显示节点另有死亡/淡出Task。CorpseSlug死亡还会让同伴进入吞噬、获得Strength、改变意图，故只恢复HP或模型不足以恢复战斗。

- `CombatRoster.cs`保留首次加载时的真实Creature对象、CombatId、初始顺序和受控显示身份，拒绝新生成/重排/外部节点替换等范围外变化。
- `DirectCheckpoint.cs`的数据根包含全部原始敌人，含已脱离战斗者；memento恢复原列表、字段、RNG、状态机/NextMove及存活者能力。保留当前live turn loop/信号/动作ID。恢复前解除当前StateTracker订阅，恢复后为真实成员订阅；核对CombatState.GetCreature(CombatId)与原对象引用一致。无战斗AddCreature/AfterAddedToRoom/AI重掷。
- 跨死亡成员边界时移除旧敌人显示和关联死亡淡出，取消旧死亡Token；`_ExitTree`在数据恢复前解除原UI订阅。随后用NCombatRoom.AddCreature对恢复后的同一对象创建显示；它是显示路径，不重新生成战斗对象。原生异步移除继续引用旧节点，不引用替代节点。房间、run、manager均不重载，前缀操作0。
- `CreaturePresentation.cs`按真实对象保存位置、基础/临时缩放、色调、动画稳定循环目标；允许绑定本恢复流程创建的显示。复原吞噬循环/普通循环，直接显示血条，继续核对手牌布局与缩放。不是任意形态或逐帧动画快照。
- `playback-roster`、`checkpoint-roster-restored`、`playback-step`增加身份/顺序/目标/能力/吞噬标记日志；已有projection/RNG和退出比较继续保留。正常录制没有场景树memento；日志不发布原生checkpoint。

## 可复现准备

选定**暗港CorpseSlugsWeak，两只同名CorpseSlug**，是明确标记的定向准备实测样本，不是自然随机遭遇样本。新增主菜单明确测试准备按钮，默认关闭、可取消、只消费下一新局。启用后新建铁甲A0，初始化前选择暗港，原生GenerateRooms之后将首个普通遭遇设为CorpseSlugsWeak。预载、地图、进入房间及正式战斗仍走原生流程；实际保存/录像checkpoint序列化实际房间列表。未作用于Continue或回放。

新录像可选Preparation=`DEMO004-directed-Underdocks-CorpseSlugsWeak`，菜单标示定向样本；空值字段省略，旧envelope兼容。对DEMO-003/build-003新增一条精确旧build兼容常量；其余未知build仍拒绝，不修改录像hash或伪造场景。

## 人工步骤

1. 用户手动启动，沿用已确认Cloud关闭与原测试账号/modded1。主菜单点测试准备，再新建铁甲A0；古代事件选不改变初始牌组的选项，不用药水。确认首战两只蛞蝓与“录制中”。
2. 正常打，优先杀靠前者，让另一只仍活；之后至少再进行一个操作，再打到胜利自动保存。不领奖、不进入下一房间，保存退出菜单。
3. 打开最近录像，用Step到第一只死亡的操作K（界面步数即位置）；后退至K−1，确认两只原敌人血量/能力/意图/位置、存活者强化/吞噬姿态回到过去状态。
4. Step重做K及下一步；可在死后两个稳定点间再后退一次。继续播放到胜利，Stop，再打开一次；可第二次中途Stop，不要求重复录制。
5. 正常退出游戏后读取最小日志，确定实际K和目标Index/CombatId变化、恢复实际身份/成员/显示核对、重做结果与RNG、每次打开仅一次room-load/0前缀、胜利与退出原manager/Continue/progress比较。用户体验与日志分别记录。

出现失败保留原版本/日志并同任务修复，不降低关键断言，不要求用户反复盲roll。当前仍拒绝胜利终局后退、新生成/召唤/宠物、玩家死亡、成员重排、任意节点/形态替换；仍固定初始10牌、首排普通战斗、无药水/选牌/后续房间。

## 本轮实测

录像`925df46ff5a34c02aa97079ef165443a`，格式2，seed `X9AVMK8RM6KG`，首排col3，定向`ENCOUNTER.CORPSE_SLUGS_WEAK`，共11操作，第11步Bash获胜自动封存。实际Preparation标记与build-001精确身份一致。文件SHA256 `65DD277256AF3CB33BBC91BA3323689BEDEEFEEECDF7B64ADB31758A12924DFB`，原生checkpoint保留私有；没有修改已录操作或hash。

Runtime `0f7658a62a34451e997b873364d6ff4a`的297个事件覆盖一次录制和两次打开。第5步Strike击杀靠前的CombatId1；之后EndTurn和对CombatId2的操作，最后胜利。同名敌人的目标始终以Index+Model+CombatId核对：击杀前Index0/Id1，击杀后Index0/Id2。没有按名字替代身份。

31次直接恢复均有start/roster/presentation/arrival链；42次forward全部StateMatched，31次恢复均保留原对象、完成订阅协调且无spawn hooks。7次5→4跨死亡均恢复Id1的7HP、Vulnerable2、GLOMP_MOVE，并让Id2从STUNNED/Ravenous=true/Strength4回到GOOP_MOVE/Ravenous=false/无Strength；成员顺序恢复为player0、enemy1、enemy2。第5步在两次观看中实际执行8次且每次projection/RNG匹配，之后对剩余Id2的操作及敌方回合继续成功。

两次打开仅各一次playback-room-load；所有恢复记录RoomRebuilt=false、PrefixOperations=0。恢复耗时本次观察约12.85–81.70ms（含等待帧，仅固定样本观测，非性能上界）。31次显示核对包括原位置/基础及临时缩放/色调/稳定动画循环与手牌顺序布局；跨死亡显示重建7次，死后稳定点间的后退也实际发生（包括6→5四次）。用户确认整体符合预期；该确认不是像素级或所有形态覆盖证明。

首次打开23次forward/23次恢复（跨死亡5次），最后退至0并中途Stop；AllOperationsMatched=false是未播终点的正常停止，不是差异。第二次打开19次forward/8次恢复（跨死亡2次），完整执行第11步、真实胜利任务成功并拦截奖励，然后Stop。两次playback-closed的Error均null、OriginalManagerRestored和NormalContinueAndOriginalProgressUnchanged均true，含原progress文件比较。第一次Stop→重开已实际证明；两次打开属于同一Runtime，不外推跨进程。

用户初次报完成时第二次仍在胜利终点，快照只有294事件；Implementation明确请其Stop/退出后补读至297事件，最后0297-playback-closed，game0。未把早期未退出快照冒称隔离通过。当前完整事件无failed/error/state-difference/checkpoint-unavailable。

## 备份与验证

复用最近完整恢复点`C:/Projects/STS2ReplayMod-local/backups/DEMO003-20261026-231903-3ae7b9b1e9/`。安装前比较该收据列出的36个现有save文件，仅settings.save.backup不同，其他已比较文件匹配；原账号匹配、game0。未重新逐项审计全部存档，也不将备份等同隔离证明。安装脚本另存被替换的DEMO-003/build-003 pair、settings/selector于该恢复点的`preparations/build-001-b9fde522/`。无整存档恢复、无本体改动、无其他账号读取、无自动启动游戏或GUI自动化。

本次新战斗产生正常进度后，用户已正常退出；新增完整本地恢复点`C:/Projects/STS2ReplayMod-local/backups/DEMO004-20261027-003416-2f3bc72631/`，1276文件/132024473 bytes逐项读回并与来源匹配、game0、原账号匹配，私有RESTORE给出精确源/目标。未执行恢复；另一账号排除。收据`evidence/demo-004/preparation/DEMO004-20261027-003416-2f3bc72631-backup.json`。新恢复点保护本轮新增正常进度，仍不替代上文回放隔离比较。

本轮证据：`evidence/demo-004/runtime-r1.json`（297事件，包含用户反馈与录像操作摘要）、`runtime-archive-r1.json`（原录像及逐事件私有副本读回）、`direct-contracts.json`（6项）、`victory-compatibility.json`（实际旧文件）、`build-001-validation.json`（候选构建时状态）、安装收据与sealed pair。准备/读取脚本语法检查通过。私有原始clip与session另存旧恢复点`runtime/demo-004-r1/`，未公开原生checkpoint。提交PM核对本固定样本，保留已知范围限制，不自动扩展下一任务。
