# RUN-002：宝箱、商店、问号与SL尝试归属

## 最新摘要 · 2026-11-01 · r6原生领取观看与上下文修复通过

- 保持RUN-002/build-003，DLL `1AD070C28A881D7E9ACA8A7F13EFFD5F975F56217C6ECB8A43F8672AECB15C08`，无换版/重录。r6会话4e0ae469共151事件、0 observer-error、0资源差异；旧首战8步原生胜利，后三项领奖/前进/地图/宝箱开箱和领取共7项非战斗操作均严格资源/RNG匹配。执行的是attempt5的raw13/14，没有重放已放弃的8/9/10或11/12。
- Load.Progress于projected7地图操作、原生生成宝箱房间前应用；开箱/领取的Operation.Progress分别于projected8/9应用，三个hash都与r5实际记录一致（`29BF8B34E957473DA31981E996EDF3955C9E3C9D79BF861E2ABE7EE78AE0DDC3`）。该环境与旧首战未seen白星具有判别差异；只更新隔离Progress，没有覆盖局内资源/RNG或expected。D-139本代表样本的修复有实际运行证据，其他账户副作用时序仍不外推。
- 到projected9终点Auto=false、AwaitingStop=true、SceneKeptOpen=true；用户确认领取动画正常。随后手动Stop，11登记任务全部收束（1取消/0故障），AllOperationsMatched/Complete/VictoryReached=true，Error=null，原manager与所比较原生Continue/Progress恢复true，当前root逐hash不变。r4仍保持真实FAIL，不追改历史。
- 封存155数据文件，51当前文件4,380,277 bytes补存`RUN002-post-r6-current-20261101-000732`；相对r5仅settings.save.backup正常变化，无缺失，game0、无restore/自行启动。证据`runtime-verification-r6.json`、`collection-r6.json`。
- 下一步同版本正常Continue入口，开箱但不拿白星、等跳过可用后跳过离开，进入所连第4层最左侧普通战斗并获胜，不领奖后保存退出正常关游戏。核新增独立尝试回到point7入口、最终路线只含不拿分支、金币不重复/无白星及下一战实际状态，再观看该分支。宝箱阶段尚缺这项实质不同分支与后战，商店/问号仍待；本包IN_PROGRESS。

## r5：短领取与真实上下文录制通过

- 保持RUN-002/build-003，DLL `1AD070C28A881D7E9ACA8A7F13EFFD5F975F56217C6ECB8A43F8672AECB15C08`，无换版/重打首战。r5会话e543191d共12事件、0 observer-error：实际Continue精确匹配成功save-point6，visit3新增attempt5(parent4/restoredSave6)，raw13开箱、14领取均complete，没新增前进。当前路线为[1..7,13,14]，旧raw1..12及原8步胜利片逐项/逐hash不变；3访问、5尝试，不按同内容去重。
- 新Continue.Progress及两个非战斗Operation.Progress均真实记录57,863 UTF8 bytes，均含账户已发现WHITE_STAR；旧首战Progress不含该项，具备检验新输入上下文是否真的生效的判别力。玩家领取后discovered_relics仍空，完整开箱前/领取后资源与旧attempt4分别逐字相同，金币117→163、白星0→1；这些是正常录制结论，不提前当作观看修复PASS。
- 成功存盘观察新增point7，位置仍为visit3/attempt5入口、路径[1..7]。当前原生文件完整fingerprint `ACCAEC74DD9CDDE0DDBCDB292CAA65336F95B7AC04A4C17100E675DBF41BF218` 精确匹配point7，金币117、无白星；不凭界面或用户未点前进假定存盘位置。root生产读取/checksum与选中路线资源连续性通过；root实际1,005,441 bytes，在16MB上限内。
- r5封存16数据文件，51当前文件4,380,277 bytes补存`RUN002-post-r5-current-20261101-000042`；原生latest.mcr、current_run/save.backup及settings.backup共4项正常变化、无缺失，game0、无restore/自行启动。证据`runtime-verification-r5.json`、`collection-r5.json`、`checks/r5/run-production-read.json`。旧r4缺输入的严格FAIL及原件保持。
- 下一步只观看同一b0d06809，从第2层首战锚点Auto至宝箱白星领取终点；应完整播完领取动画、自动关闭Auto并留场等待手动Stop。需核新Load上下文在原生入房前应用、raw13/14操作上下文归属与严格资源/RNG相等，以及Stop/原context恢复；不用再Continue或重录。通过后才做不拿分支与后战；本包仍IN_PROGRESS，商店/问号等仍待。

## 003：严格账户上下文修复与候选安装

- D-139选严格上下文修复，不放宽discovered_*比较：实际Continue的Progress写入带Attempt/BeforeOperation的可选RunContinuation.Progress；新非战斗语义操作Begin另存实际Operation.Progress，覆盖Continue之后提示/悬停造成的输入变化。旧字段缺失不假补，旧raw3/4尝试及r4不追改通过。
- 观看投影只保留选中尝试的上下文。未完成房间入口Load的环境安排在前一地图操作之后、原生房间生成之前；该Load比保留的旧地图操作更晚，其Progress覆盖旧地图操作的输入上下文。完成战斗Load仍保留原生历史收束，首战自身Load可作初始内存环境；各操作输入在执行前作用于同一隔离manager.Progress。没有重建Run/manager、覆盖player/金币/遗物/RNG、从expected反推，严格资源比对不变。
- 当前RUN-002/build-003，DLL `1AD070C28A881D7E9ACA8A7F13EFFD5F975F56217C6ECB8A43F8672AECB15C08`，manifest仍`69185E5024D560BC5C9B81FC64CC2F732745BCFBA31E64F069CAEF6AAEABE8FB`，收据`preparation/build-003-5ddc2d68.json`。主程序及合同程序均0警告0错误，9项尝试/上下文合同与8项文件/位置合同通过，当前root/旧首战production read通过，均不替代原生播放。显式兼容001/002，旧pair保留。
- 安装后对r4的51当前文件核验，只有允许的settings.save配置变化，Continue/Progress无变化、无缺失，game0、无restore/自行启动，见`preparation/post-003-progress-check.json`。003尚无游戏结论。下一步复用现局正常Continue宝箱入口，再开箱领白星、等动画结算、不前进、保存退出正常关游戏；应新增attempt5与真实上下文，不重录首战。先核新账户环境含白星而旧首战不含、精确Load归属及当前路径，再做同root观看；当前旧attempt4缺关键输入，不再让用户反复测旧路径。
- 本包仍IN_PROGRESS；新领取严格通过后才继续不拿分支及下一战、商店/问号/事件内部战斗。本轮不建逐悬停事件流，不承诺操作内部所有账户副作用时序；低频完整Progress快照须关注真实文件大小，16MB上限保持显式失败。

## r4：原生领取已执行，缺账户发现输入导致严格FAIL

- r4在002观看同一b0d06809，原生领取白星的确切实例通过归属保护，11个登记任务收束（1取消/0故障）、0 observer-error；仍在projected9/source12领取后保护退出，未到正常终点，不能判通过。唯一叶级差异为`players[0].discovered_relics`：记录为空，观看为WHITE_STAR；其他完整资源/RNG字段相同。不是再次触发旧AddRelic保护。
- 原生ProgressSaveManager仅在账户第一次seen且模型属于本地玩家时向Player.DiscoveredRelics追加；NHoverTipSet展示提示也会MarkRelicAsSeen，未归属模型可先改账户seen而不改玩家统计。battle.Progress尚无白星，raw9和12.After都没有玩家新发现，r1封存的原生profile1 Progress已有白星。故缺失账户上下文可能在首次领取前的提示/悬停已出现，不能只归因SL或从expected反推已发现集合。已交PM核定元上下文及旧片兼容原则，尚未忽略该差异或改写原片。
- r4共147事件、151原始数据文件封存；51当前文件4,379,881 bytes补存`RUN002-post-r4-current-20261031-235123`，相对r3仅settings.save.backup正常变化、无缺失，原manager及所比较Continue/Progress恢复true，game0，无restore。证据`runtime-verification-r4.json`、`collection-r4.json`。当前仍002；以下r3与安装结论保留为历史，002不再标记未测。

## r3：旧保护修复与002安装

- 当前RUN-002/build-002，DLL `0DE66D1936532107143E6AE9B42E80CC94A6D5884A7C2FD5939C51EBDEE25B3E`，收据`preparation/build-002-ed3ed5d9.json`；001和006 pair保留。构建0警告0错误，显式兼容001原片/root，尚无002游戏结论。当前51文件安装后无进度变化，game0，无restore/自行启动。
- r3原生观看b0d06809：首战与三项领奖、真实地图入宝箱、开箱和生成白星均资源/RNG匹配；日志明确执行的是sourceSequence11/attempt4，旧raw8/9/10没有串入。到projected9/source12领取时，原Player.AddRelicInternal保护报告`incomplete-unsupported-relic-membership-change`，自动保护退出，整条领取回放FAIL，不误判正常终点。
- 旧RelicMembership原只适用于战斗内不支持的遗物增删，仍覆盖了新宝箱原生Obtain。002只在已核PickRelicAction派生的Obtain同步调用中，核对确切Player和Relic实例再允许Add；按移除/战斗/未归属变化继续诊断。主任务仅捕获该次主Obtain，不把嵌套同模型领取当成主任务。没有手写遗物效果、改金币或改expected。
- r3共146事件，11个登记任务退场收束（1取消/1故障），原manager与所比较Continue/Progress恢复通过；该故障轮不判清理成功体验。002另补退场故障详情日志以便必要时定位。150原始数据文件封存，51当前文件4,379,881 bytes补存`RUN002-post-r3-current-20261031-231836`，相对r2逐hash无变化。关键本地证据`runtime-verification-r3.json`、`collection-r3.json`、`preparation/post-002-progress-check.json`。
- r1/r2入口锚点及独立相同尝试录制结论保持：point5→attempt4(parent3)，当前路径[1..7,11,12]，旧10项与原片保留；内容/资源/RNG相同也没有去重。仍未完成领取终点观看，下一只重开当前路线从第2层Auto至宝箱领取终点、关Auto等待手动Stop并正常退出；不重录/不Continue/不改变当前主线。
- 本包仍IN_PROGRESS；领取回放通过后再安排第三次入口重进改为不拿、进入后战核最终延续路径；商店/问号/事件内部战斗仍待接。非战斗Back/跨房间Seek禁用，其他遗物/附加奖励的覆盖继续分别报告。

## r2：宝箱入口Load与相同领取独立尝试通过
- 保持RUN-002/build-001，无换版/重录。r2真正新进程Continue精确匹配成功入口save-point5，仍在b0d06809/visit3，entries1→2；新attempt4的parent3/restoredSave5正确。12事件、0 observer-error，成功存盘观察新增point6，位置仍是入口路径[1..7]。只按此样本认宝箱房前SL，不外推商店/问号。
- 再次开箱领取仍为RELIC.WHITE_STAR，但没有合并/去重：原raw1..10逐项及8步胜利片hash原封不动，新raw11开箱、12领取独立归属attempt4。当前最终延续路线为[1..7,11,12]，原8/9/10事实保留而不选择；总访问仍3次/尝试4次，不虚增楼层。
- 两次生成物相同，开箱前完整资源/RNG及领取后完整资源/RNG字符串亦逐字一致；金币都是入口117、开箱后163。当前原生存档精确匹配point6（attempt4），仍117金币且无白星；不是以内容相同推断SL或默认为重掷。root生产读取/checksum及选中路线资源连续性通过，尚未执行观看。
- r2共16数据文件封存，当前51文件4,379,881 bytes补存`RUN002-post-r2-current-20261031-231254`；5个正常Continue/配置backup文件变化，无缺失，game0，无restore/自行启动。关键本地证据`runtime-verification-r2.json`、`collection-r2.json`、`checks/r2/run-production-read.json`；r1误前进及全部旧事实/恢复点保留。
- 下一步先用当前选中领取路线做一次Auto观看：主菜单b0d06809从第2层战斗锚点原生播放，应只开箱/领取一次、到宝箱领取终点关闭Auto等待手动Stop，不点宝箱前进。先验证领取原生调用和最终路径真的不重复执行，再安排再次真正Continue改为不领取、进入后战取自然状态。此顺序保留当前主线的领取样本，不需要完整经历模式。
- 本包仍IN_PROGRESS；商店交易/删牌、问号实际房型/事件多阶段及内部战斗仍待接。领取后共用选牌/附加奖励页的实际覆盖继续单列，未测不判通过；新非战斗Back/跨房间Seek仍禁用。按D-135普通轮次直接与用户修测，不逐轮唤醒PM。

## r1：首次宝箱录制及误前进后实际入口存盘核验
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
