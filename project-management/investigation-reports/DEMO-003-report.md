# DEMO-003 首场完整胜利回放报告

2026-10-26，D-096后续。当前实现结论：OBSERVED_VICTORY_RECORD_PLAY_AND_PRESENTATION_FIX_MATCHED，提交PM验收。用户确认build-002尾延迟修复，继而报告缩放/手牌显示遗漏；build-003本轮20次直接恢复的逻辑/选定显示值匹配、47次forward匹配、3次完整胜利/退出恢复通过，用户反馈“这次测试ok”。范围仍是固定首战样本；build-001的早期Sludge Spinner未封存失败保留，不追认该场成功。PM管理文档未改。

## 增量及真实结束边界

Recorder/ActionMonitor区分普通玩家稳定点与原生胜利终点。普通操作仍要求原action/effect Tasks成功、EndTurn到下一玩家回合及Playable；胜利要求实际CombatWon、私有EndCombatInternal原Task和最后操作/effect Tasks成功、ActionExecutor空闲。build-001还要求死亡动画Tasks成功，该额外门槛已在build-002移除；它不影响真实逻辑结果，不应拖延提交。不会等待不存在的下一玩家回合，也不删除最后一击。

BattleEnd在首战初态保存本场敌人身份锚。原生EndCombatInternal完成AfterCombatEnd/AfterCombatVictory、房间MarkPreFinished、SaveRun和胜利进度更新后发CombatWon。在初始化时订阅该事件，先于后来战斗UI的奖励监听器冻结终点投影：Outcome=Victory、RoomPreFinished、CombatInProgress、完整LiveState比较投影（含玩家/牌堆/相关RNG），及本场原敌人最终HP/存活和完整RNGcounter/state。死亡敌人已从CombatState移除也不会丢失其最终摘要。此投影是在实际事件采集，不是拿一个硬编码结束标记替代执行。

build-002定义“回放结束”为真实最后操作/胜利逻辑已成功及执行器空闲，不再要求所有装饰动画淡出。死亡动画可以继续播放，Stop正常清理场景时NCreature._ExitTree会取消其DeathAnimCancelToken。奖励生成不属于录像，终点投影仍冻结在奖励生成前，比较内容未变；不承诺逐帧电影时序。

录制自动把该真实最后操作追加后Seal，显示“战斗胜利 · 已自动保存”。用户不用抢按结束片段或先退出。正常录制的原生保存/胜利进度/奖励UI完全照常，测试时要求胜利后不领奖、不进下一房间。

## 格式与旧文件

新录使用实验格式2，Operation末尾新增Victory=true标记且只能在最后一个操作，After保存上述实际终点。格式2允许没有EndTurn的真实快速胜利；不能为满足旧格式强迫多打一回合。旧32操作自动截断移除，格式2上限256操作、文件仍4MiB；达到边界会报未封存，不把胜利前段伪装成完整战斗。可提前人工结束仍保留有EndTurn的普通片段。

格式1解析保留，Victory=false默认不序列化，所以旧payload/envelope校验字节保持。明确兼容两个已知格式1录制hash：DEMO-002/build-001和direct-r1/build-002；仍锁精确游戏SHA、profile scope、only ReplayMod和checksum，不接受任意build。原12操作真实私有归档以新Codec重新验证envelope及原hash字段通过、文件字节未改。正常新格式2文件精确锁本候选。

## 播放、后退与隔离

播放最后一击仍走正常TryManualPlay/EndPlayerTurnAction，比较实际completed.Victory及冻结的终点After。成功后提交最后位置并显示“战斗胜利／回放结束”；不会对胜利终点调用DirectCheckpoint或等待Playable。活战斗期间原内存checkpoint/直接后退继续使用。

观看时仅拦截NCombatUi.OnCombatWon的奖励/自动继续入口，让回放停在胜利状态；原生胜利逻辑和进度更新仍在现有独立MockGodotFileIo/SaveManager中执行。两处相关进度写入口检查实际SaveManager为当前隔离manager，记录playback-victory-progress；ShouldSave=false仍不是总保护，原生胜利SaveProgressFile不能漏回原manager。原生CombatReplayWriter继续关闭（其WriteReplay自行检查IsEnabled）。

MemorySession退出核对在原Continue/backup文件hash和原manager progress之外，增加原progress.save文件hash。Stop/失败退出保持原清理/恢复顺序。没有通过恢复备份来声称隔离通过。

胜利终点后退明确禁用并给出原因；死亡/成员变化边界同样在点击前阻止恢复。普通顺序播放可继续经过敌人死亡，不让旧DirectCheckpoint的死亡限制把最后一击变成播放失败。部分敌人死亡后，可为当前仍在场的活对象捕获后续稳定点，但向前跨过那个死亡边界回退仍不支持。没有隐藏重播fallback或声称复活已支持。

## 记录与验证

少量新增事件：recording-victory-observed、clip-sealed(Victory/AutomaticVictorySeal/FinalOperation/FinalStateHash)、playback-victory-observed、playback-victory-progress、playback-rewards-blocked、playback-step(Victory)、playback-victory-ended、playback-closed(VictoryReached/OriginalProgressFileIncluded)。初态、正常forward、直接恢复日志继续保留，无逐帧trace。

DEMO-003/build-001 DLL SHA256：`46BF021546F6A5DB1B1B8D5A2B01D640E4D5BDC6EBB87C885DDE2DBADA28EC91`，manifest仍0.1.0。构建0警告/0错误。

沿用已有小型测试入口的victory模式，仅运行5项相关检查：无EndTurn的真实终点格式可封存/重开；结束只能最后操作且已完成结果/未知格式限制；超过旧32操作但256cap仍约束；false新增字段不改变旧JSON字节；实际旧12操作文件的envelope/hash/兼容元数据保持。结果`evidence/demo-003/victory-contracts.json`。没有运行游戏或构造器、没有重跑旧9项/原直接恢复5项/旧矩阵。Tasks和实际胜利流程仍需游戏运行验证。

## 备份与安装

用户临时暂停期间仅阅读，无实现/安装改动；用户“不打了。继续吧”解除暂停后才实施。换DLL前实际game0核验，并核对最新恢复点中modded1的8个已备份run/progress/prefs文件与当前一致，因此本批复用 `C:/Projects/STS2ReplayMod-local/backups/DEMO002-20261026-211616-87fc23469c/`，没有假设口头“不打了”等于进程已退出，也没有无条件再复制全账号。

安装收据`evidence/demo-003/preparation/build-001-f73aec65.json`：installed=sealed、配置only ReplayMod/modded1、game0；被替换的已验收direct-r1 pair/settings/selector保存在该备份preparations/build-001-f73aec65。原所有pair/录像保留。Cloud沿用用户已明确关闭的本批确认；没有自动改Cloud、启动/操作游戏、恢复存档或读取另一账号。

## 请用户手动验证

1. 原账号modded1、Cloud保持关闭，手动启动。新建铁甲A0，选不改变初始10张基础牌组的开局奖励，进入首排普通战斗，按自己的出牌顺序正常获胜。不要提前按“结束片段”。
2. 最后一击/结算结束应显示“战斗胜利 · 已自动保存”；此时不领奖、不进下一房间，正常保存退出至主菜单。
3. 打开最近完整胜利战斗，默认0。Step两次→后退一步→再Step，确认活战斗段仍正常；然后播放到最后一击，显示“战斗胜利／回放结束”和结果一致，终点后退禁用，不出现可领奖/继续的观看分支。
4. Stop回菜单，再打开应从0；Step一次后Stop，正常关闭游戏并反馈结果。若第一战很短，按实际可用操作数做一次非终点后退即可，不强迫固定操作数或多打一回合。

若出现失败，记录屏幕位置/短错误文字，停止后续操作，允许自动退出或正常关闭；同任务读取日志修复。实际终点、玩家正常Continue/进度是否保持，以及用户体验结论将在这份报告按真实事件补充。

## 当前边界

普通失败结局、奖励选择、下一房间、死亡边界反向恢复、多人与跨版本不在本任务。build-001已测事实与build-002尚待短测的边界如下；拿到可用修复结果即交回PM，不追加旧矩阵。

## build-001实际运行及发现的问题

公开原始脱敏事件`evidence/demo-003/runtime-r1.json`保留本会话全部152事件，包含失败，不能称整段无错误。私有完整录像/原session在旧恢复点`runtime/demo-003-r1/`，公开归档收据`runtime-archive-r1.json`；clip复制hash读回匹配。

该Runtime session先有两次初始支持范围资格拒绝（只知道demo-only-first-monster-Ironclad-A0-starter-deck，不猜用户当时选择），并播放旧12步文件两次到8/6后Stop，两次比较的原context保持。接着首个实际Sludge Spinner录制观察到胜利（22:52:21.1026954），3.13秒后保存退出时仍有Pending，报wait-before-finishing-clip/CompletedOperations=15并未封存。不能把它补成成功，也没有可发布的该场完整clip。

之后真实新录Beetle片段`ac3f58addef24defbabb2ae1b533714c`，seed NPMLZ95ZJUQ3，共10操作，最后第10次Strike为胜利。22:53:46.475006实际CombatWon，22:53:50.8821169自动封存，完整文件238842字节/格式2/Victory=true，没有手工补标最后动作。

同会话从菜单打开新文件两次：第一次初态及1–10全部匹配，实际执行最后Strike、原生胜利Task成功、IsolatedManager=true胜利进度更新、奖励入口拦截，VictoryReached=true/AllOperationsMatched=true。Stop后原manager及所比较Continue/backup、原progress.save与原manager progress保持，Error=null。第二次重新从0打开，完成13次forward/6次直接恢复（2→1、1→0、4→3、5→4两次、4→3），全部匹配；在6/10执行EndTurn时接受Stop，完成到7后退出，同样恢复核对通过。没有跨进程重开证据。

用户所述“第9步到胜利”在该样本日志中的时间为22:54:15.9034211→22:54:20.3713204，约4.468秒；原生胜利事件22:54:16.0403311已触发，之后额外约4.331秒才提交终点。源码NCreature.AnimDie不仅等待当前死亡动画，还包括0.5秒余量、淡出VFX、disable UI tween及IDeathDelayer。因此额外门槛主要等的是装饰性死亡/淡出Task，而非刻意设定十秒、状态差异重试或奖励等待。不能将用户体感与日志时长混为精确测量。

## build-002修复与待验证

BattleEnd.Ready移除死亡动画完成/成功门槛，仍严格要求实际CombatWon、冻结投影、原生胜利Task和最后操作/effect成功以及执行器空闲。死亡动画Task仅用于记录还有几个待播放，不强制跳过或修改游戏动画。新增recording/playback-victory-ready记录WonToReadyMilliseconds、DeathAnimationsPending及WaitedForDeathAnimations=false。Recorder.Seal也会在保存退出恰好早于下一观察帧、但胜利已逻辑就绪时先补齐真实最后操作/自动封存，避免单纯observer帧次序丢掉已完成胜利。

保持终点After的采样点和JSON结构不变，明确兼容build-001录制hash `46BF021546F6A5DB1B1B8D5A2B01D640E4D5BDC6EBB87C885DDE2DBADA28EC91` 的格式2。已用真实10步归档验证envelope、格式、终点及原文件字节保持，结果`victory-compatibility-r2.json`；本轮只跑这项相关检查，未重复全部旧检查。

build-002 DLL SHA256 `5F710B9FB7FE10B962F84D47698CE0C9B0151756A4BE0894FDD6B3834B4C65F7`，构建零警告/错误，安装收据`preparation/build-002-0070aa57.json`。因本轮有两场正常战斗的新进度，安装前另建完整新恢复点`C:/Projects/STS2ReplayMod-local/backups/DEMO003-20261026-230039-b9fdbc38d9/`，逐文件读回匹配，保留被替换build-001 pair/settings/selector；旧所有恢复点/pair/clip继续保留。game0时安装，未启动/操作游戏或恢复存档，Cloud沿用本批明确关闭确认。

build-002当时请求只重播10步录像；用户实际还新录了一场9操作Beetle并测试后退，结果如下。即使有新录成功，也不将原Sludge Spinner失败改写成已补证。

## build-002实际修复结果与显示层缺口

用户明确反馈“死亡动画的问题已经解决了”，并提出两个真实bug：Shrinker Beetle缩小能力回退后玩家仍小；后退时全部手牌有短暂入场动画。公开本轮152事件在`runtime-r2.json`，无failed/error/difference事件，但那些核对只覆盖逻辑投影，不能因此否认显示层bug。

Runtime 762491943be84423b81fc2d57bb366f4先从磁盘打开旧10操作胜利片段，全部匹配并Stop；后又真实新录`3bad460ee4404d85b1adeae82a9463c9`，J9LNLLHTSVKA/SHRINKER_BEETLE_WEAK，9操作，第4步EndTurn进入缩小后的玩家回合，第9步Strike胜利。自动封存后完整播放9步，再次打开进行后退与继续。共3次open/34次forward/9次直接恢复，两个完整胜利终点和一个在6/9处中途Stop；三个closed均Error=null、原manager及所比较Continue/backup/progress文件和原progress对象保持。

旧录像播放WonToReady=15.316ms，新录制WonToReady=12.493ms、新录像播放=5.0204ms，三次均DeathAnimationsPending=1/WaitedForDeathAnimations=false，原生胜利Task成功且执行器空闲。新9步录像自动封存距ready约4.7ms（实际Seal=3.1449ms）。因此本轮对延迟门槛移除及新的录制封存路径均有实际证据，不再仅是源代码推断；不外推任意其他战斗或原失败那场Sludge Spinner。

新9步完整文件与原session私有归档于23:00恢复点的`runtime/demo-003-r2/`，复制hash读回，公开收据`runtime-archive-r2.json`。格式/原hash及元数据未改。

## build-003：已有生物缩放及手牌呈现修复

根因：原直接恢复有意不盲clone Godot SceneTree，但RefreshUi只补手牌/图标/数值/意图，遗漏了独立显示状态。ShrinkPower.AfterApplied/AfterRemoved通过NCreature.ScaleTo修改 `_tempScale` 和Visuals.Scale，并有独立Tween；逻辑Power已恢复并不自动重新调用这些回调。原手牌实现每次清空/重建所有holder，又调用正常平滑布局/缩放，造成入场感；这不是产品设计的回退效果。

新增CreaturePresentation为场上已有玩家/敌人保存DefaultScale、temporaryScale及显示缩放值，不依赖只给玩家判ShrinkPower。若捕获时原ScaleTo尚在动画中，保存其稳定目标DefaultScale×temporaryScale，避免缓存0.75秒缩小动画的中间尺寸。恢复时验证原NCreature/Visuals/Body仍存在且身份未替换，取消旧scale Tween，直接设置这几个值，经原DoScaleTween和UpdateBounds刷新orb/边界/意图位置；不重复施加能力或生成语义操作。恢复一帧后核对实际Scale/默认值/临时值相符。已有小体型对象的基础scale也会保留，不强制全恢复1。

新增HandPresentation只移除不在目标手牌的holder，按卡牌实例身份复用仍在手中的holder，仅创建缺少的牌；按逻辑牌序重排后，取消位置/角度/缩放及hand自身动画，按原HandPosHelper直接设置最终位置/角度/缩放。新NCard在加入前设正常子节点scale，避开SetCard自带缩放Tween；原deferred通知后再snap一次并验证牌序与布局。后退不会把所有卡牌从起点重新飞入，正常向前的原生出牌/抽牌动画仍照常。

新增checkpoint-presentation-restored事件，记录玩家/敌人的基础/临时/实际目标scale、模型角色、手牌复用/创建/删除数量、HandEntranceAnimation=false及实际读回结果；原checkpoint-captured记录这些scale值。逻辑投影仍核对，显示层不再仅靠它冒称已恢复。

范围仍明确：本修复是已有同身份对象的缩放，并非通用换皮/骨骼动画/特效/场景树快照。死亡、生成/成员变化的反向恢复仍不支持；模型或body节点被替换时明确禁用后退。用户举的Beetle Juice/其他层生成小怪未在本轮录制范围，不为证明泛化而加新房间/药水测试，也不宣称那些场景已验证。

build-003 DLL SHA256 `FBC3B3DAEE1DF3A546917B24C12CCB7A0A6F6A0406AF480253CB5AB4669AB580`，构建零警告/错误。显式兼容build-002新录格式2，真实9步文件envelope/元数据/原字节读取校验通过（`victory-compatibility-r3.json`）。没有离线构造Godot节点/启动正式游戏或重跑旧矩阵；视觉修复由后续用户运行验证如下。

因用户本轮又有正常战斗新进度，安装前另建完整恢复点`C:/Projects/STS2ReplayMod-local/backups/DEMO003-20261026-231903-3ae7b9b1e9/`，逐文件读回匹配；保留被替换的已测build-002 pair/settings/selector及所有旧版本/录像。安装收据`preparation/build-003-5d33b572.json`确认installed=sealed、only ReplayMod/modded1、game0；Cloud沿本批已确认关闭状态。未启动/控制游戏、恢复存档或读另一账号。

最短重测：打开现有9步甲虫录像，Step到4（首个EndTurn后缩小）→后退4→3应立即恢复正常大小→Step4再缩小；再Step5→退5→4，缩小应保持且手牌直接就位，无整副入场。可再4→3确认变大，然后播放至胜利/Stop即可。无需新录、药水或其他房间。

## build-003实际验证与交付边界

用户最新原话：“可以了，这次测试ok。”随后询问生成/死亡未支持对群怪的含义，并明确这不在本轮范围，要求交接PM，稍后与PM讨论下一任务和具体目标。实现者只收尾与交接，不自动开始下项能力。

Runtime session `1e6a13d9896c46999556cbbc7b909558`，候选hash/only ReplayMod/profile1匹配，同一已有9步甲虫录像打开三次，初态各自匹配。共47次forward全部StateMatched=true；20次直接恢复Target=Arrived、StateMatched=true、Playing=false、RoomRebuilt=false、PrefixOperations=0；实际房间加载仅各次打开一次。20个checkpoint-presentation-restored均CreatureScalesMatched/HandOrderAndLayoutMatched=true、HandEntranceAnimation=false，实际读回玩家scale=0.5九次、scale=1十一次，涵盖4→3跨缩小边界和5→4保留缩小、多次退0及重新前进。

三次都正常执行至第9步真实胜利，末步/终点匹配；均原生胜利更新在隔离manager执行、奖励入口拦截，closed.Position=9、AllOperationsMatched/VictoryReached=true、Error=null、OriginalManagerRestored/NormalContinueAndOriginalProgressUnchanged=true，比较包含原progress.save。WonToReady为12.5728、6.0194、6.0781ms，均仍有1个死亡动画待播放且未等待它。直接恢复记录10.1837–51.2258ms，不当发布延迟阈值。

公开全事件及摘要`evidence/demo-003/runtime-r3.json`，没有failed/error/difference；显示值核对是所选transform/布局读回加用户体验确认，不冒称像素采集或所有显示状态覆盖。完整原session与9步clip私有归档最新23:19恢复点`runtime/demo-003-r3/`；归档读回且与R2原clip文件hash相同（`runtime-archive-r3.json`），没有改原clip录制hash或元数据。

“生成/死亡未支持”主要指不能直接后退跨过对象生成/死亡边界去复活/撤销对象，当前该边界会禁用后退；顺序播放仍走原生创建/死亡流程，本次完整胜利已经包含最后一个敌人死亡，但没有群怪/召唤场景样本。不能据此说群怪顺播必然渲染错误，也不能反过来保证任意群怪无问题。对未触及该边界的已有活对象直接恢复与缩放修复，不等于已完成生成/死亡逆恢复、换皮或任意形态系统。

本轮完成首战真实胜利自动封存/完整播放/Stop/重开及两项后退显示修复的固定样本增量；未支持或未测范围包括群怪整体、生成/死亡逆恢复、药水/奖励选择/后续房间、失败结局、多人与一般版本迁移，交PM规划，不扩大本轮。旧Sludge Spinner未封存失败仍保留；新Beetle自动封存成功证明修复后的实际路径，不追认旧那场。

收尾读取时游戏进程0；build-003继续保留安装和当前启用配置，未再次构建、测试、换DLL、备份正常账号、恢复存档或控制游戏。只保存本轮日志/录像私有副本与报告，移交PM后等待用户与PM确定下一步。
