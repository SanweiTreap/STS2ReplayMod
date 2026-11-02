# RUN-004：精英、Boss与首条跨幕链

## 当前摘要（2026-11-02）

- 本包进行中，保留 **build-004**，源码 `3a82316`（主体修复 `9b07bd1`）。**r6精英合法起点→奖励→Boss→奖励/原生换幕→第二幕Orobas第二项→首普通胜利完整观看PASS**：62战斗动作与12路线操作全匹配、零错误、Stop恢复通过；仍差原定换幕Loading中公开Stop/重开。r2误报与r5 Continue边界FAIL均保留。
- r3真正Continue精确匹配原point4，仍同一Run，保留旧领取5/6为原始事实而不纳入最终路线；新领奖/跳卡→原生地图→瀑布巨人Boss22步胜利自动封存，未领奖。Boss当前仅录制/生产读取通过，未观看；精英四次Back/重做已在r2通过，无需复测旧卡矩阵。
- 当前局 `2b0f7e321a3a4dcd9caf9ee83435a06f`，种子KTRJB2SKRSYV；原生fingerprint精确匹配point12，第二幕首普通完成未领奖。root原始20操作，最终selected18操作（旧领奖5/6仍只保留为原始事实），5访问/7attempt。三片为精英 `be74f0a6bbb943578c3ec700c328bfbd`、Boss `27099a921185492ca48419b00b23a80f`、第二幕普通 `4c538903df5241cf9451bcc87fd7c779`；前两片hash未改。
- r6唯一原生Boss奖励页投票、EnterNextAct/EnterAct自然换幕与实际第二幕地图/古神匹配；Boss、第二幕首普通自然初态/RNG匹配，未读后幕checkpoint。新加载Stop开关仍未测；第二幕只覆盖Orobas/RadiantPearl及ExoskeletonsWeak代表样本。
- 当前已有精英/Boss共用录制、输入/Back与format10第一二幕身份；可选ActModel/RoomType/原生ActFloor/TotalFloor保持旧片省略字段。短准备改暗港首节点为精英并连到本幕原生Boss，牌组/遗物/药水在checkpoint前创建；准备前涅奥虽保留操作，Opening=null，不伪称从涅奥完整重建。普通新局RUN-003路径保留。非战斗Back/跨房间Seek仍禁用。
- r4证据 `evidence/run-004/runtime-verification-r4.json`、`checks/r4-run.json`、`checks/r4-act2-clip.json`；最新补存 `RUN004-post-r4-current-20261102-222133`，132数据文件、51已知文件/4,169,988 bytes，零缺失，game0，无restore。r4仅modded1录制/原生进度及settings.backup共6项变化；r3已保留modded2更新，本轮未再变。Cloud本轮用户确认关闭，另一账户排除。
- r6两次战后Continue均先核本次战斗，再载本次播放原生保存边界的自算快照，同隔离manager原生重建奖励，资源/RNG通过。精英/Boss战后暗亮对应这两处显式淡出/淡入。**D-147用户澄清：当前最终延续demo应剪去SL/Continue重建的观看过程，两次暗亮是暂留呈现偏差，未获目标效果接受。** 后台必要恢复保持，本轮暂缓呈现修改，继续既定退出测试；稍后由用户/PM讨论两种播放方式，不新增隐藏过场测试。
- 下一步：保留004，启用现有换幕一次性Stop开关，从Boss起点Auto验证新增加载Stop；回菜单后重开一步Stop。无需重录/整链再测。r6证据 `runtime-verification-r6.json`，最新补存 `RUN004-post-r6-current-20261102-234618`，887数据文件/51已知文件，较r5仅settings.backup变化；root/三片hash不变、native仍point12、game0、无restore。

## 首候选实现边界

录制的 journal 战斗键原已含 act:row:col；RoomVisit.Id/attempt 为逻辑局内单调序号，当前新增字段记录实际幕模型和原生楼层，不以重复地图坐标合并访问。列表、分场起点和播放状态显示幕号。

记录存档前源由现有原生 SaveRun 冻结，进入可操作点再记实际房型/ActFloor/TotalFloor；自然下一战和独立入口同时核 BattleContext，沿已有输入、快照、任务退出和原 manager 恢复路径。原生运行结果待人工记录后补，数据合同仅证明格式/归属，不证明动画、Boss 或跨幕成功。

## r1：真实精英录制与额外领取（001）

会话 `c7d1f021ccb0431699411c39438d439c`，90事件，零 error/failed/difference/recording-gap 事件。局 `2b0f7e321a3a4dcd9caf9ee83435a06f`，种子 `KTRJB2SKRSYV`，铁甲A0暗港；原生首精英 `ENCOUNTER.SKULKING_COLONY_ELITE`，片 `be74f0a6bbb943578c3ec700c328bfbd`，format10，16步胜利自动封存，生产payload读回完全匹配。实际 Act=0、ActModel=ACT.UNDERDOCKS、RoomType=Elite、ActFloor=2、TotalFloor=2；未按普通房型误记。

原始root6操作/2访问/2attempt，selected `[1,2,3,4,5,6]`：涅奥选择石头加湿器、离开、地图、精英战、40金币、血瓶。root 777,223 bytes、clip 1,047,414 bytes。准备局缺奖励前新局输入，涅奥visit保留缺口标记，不提供从涅奥观看、不把这段准备前历史作为连续性实证。精英visit无缺口，之后奖励资源连续；生产 RunStore/Codec 实读通过，不改原件，不等同观看/Back通过。

原生成功存盘point1–4依次为涅奥入口、选择完成、精英战前、精英胜利；当前 current_run fingerprint 精确匹配point4，原生99金币/无血瓶，与当时成功保存资源一致。随后操作5/6在录像中使金币99→139、增加RELIC.BLOOD_VIAL，但未触发下一次原生SaveRun，正常保存退出未把这两步写入新的成功保存点。后续Continue分支应按point4续接，原5/6保留原始事实，不与重领奖重复拼接。

补存94数据文件、51已知当前文件，实际新局/录制导致latest.mcr、modded1 current_run及backup、progress及backup、settings.backup共6项变化，均保留；无缺失/无restore/另一账户未读。证据 `evidence/run-004/runtime-verification-r1.json`、`collection-r1.json`、`checks/r1-run.json`、`checks/r1-clip.json`。

## r2：精英观看数据/视觉通过，遗物归属误报保留（001）

完整观看及一次重开会话 `943411d4235f490888f0a446d4ad9015`，403事件、20次原生动作校验、4次checkpoint恢复。战斗胜利与两项reward后资源/RNG均精确匹配，终点Auto关闭并留场，用户接受Back/重做与播放；领取血瓶时 observer-error `incomplete-unsupported-relic-membership-change` 留在最终Error中。后续原context/manager及所比较Continue/Progress恢复通过，3个已拥有原生任务均完成、0取消/0fault。不能因Complete=true或画面正常覆盖这个内部失败。

同进程另一重开Stop无错；另进程 `65a1feec2ba74048bcd1f3b2b4ff85ee`，39事件，实际前进一步/Stop无错，恢复通过，0待退任务。提前Stop的Complete=false不当整链失败。root/clip与r1 SHA保持，当前native仍point4、99金币/77HP/两瓶能量药水、无已领奖血瓶；Boss真实输入已生成 `ENCOUNTER.WATERFALL_GIANT_BOSS`，第二幕实际古神 `EVENT.OROBAS`，尚未进入/录制。

原因：既有 RelicMembership 只登记宝箱、商店、事件来源，未登记战后 RelicReward.OnSelect → RelicCmd.Obtain 的原生来源；原生领取继续且资源比对通过，但观察器留下错误。002沿实际奖励Task补同一实例归属，不修改领取业务、效果、RNG或Back。证据 `evidence/run-004/runtime-verification-r2.json`、`collection-r2.json`。

用户询问涅奥未一并播放：本样本实际涅奥选择/离开已在root操作1/2中，`Opening=null`，因为 RunOpening.Capture 对 TestEncounter.Armed 的准备局尚不捕获可重建的新局输入。准备会在工厂内额外改首幕、牌组/遗物/药水和地图；从未经准备的普通新局重建会失配，所以本次保留精英战前合法起点。正常无该准备的新局仍走已验RUN-003奖励前捕获，不是普通新局能力回退；不事后给此片编造Opening。当前优先沿此合法起点证明精英→Boss→实际第二幕链，不要求再重录涅奥。

002 安装收据 `preparation/build-002-260da47d.json`：DLL `69A1529F939E9452F2FF4AB96A753C532153C287378C63E01CE69154753CC0E7`，manifest 保持 `69185E5024D560BC5C9B81FC64CC2F732745BCFBA31E64F069CAEF6AAEABE8FB`。唯一 `mods/ReplayMod` pair 与封存002一致，原001备存核hash；51已知文件安装后仅settings.save变化、零缺失、game0、无restore，见 `preparation/post-build002-check.json`。现有精英片无需重录；下一步仅Auto旧片确认领取归属，再沿真实Continue进入Boss并正常录制。

## r3：精英修复通过、真正Continue后Boss录制（002）

会话 `fcd1c11225b442d880598f7be95ec84d`，338事件、零observer-error/failed/difference。原001精英片16操作原生结果/RNG全部匹配，两项reward完整匹配，血瓶在当前reward对应同一实例的原生Obtain内获归属，完整终点Auto关闭、Stop恢复无错。后续Continue在该新进程读取真实保存表示，point4精确命中，MatchedObservedSave=true，保留同Run、创建attempt3，原始操作5/6不删除/不拼接。

重领奖金币/血瓶、跳过奖励卡、Proceed与原生地图进入 `ENCOUNTER.WATERFALL_GIANT_BOSS`，记录22步胜利format10片 `27099a921185492ca48419b00b23a80f`。Battle.Act=0、ActModel=ACT.UNDERDOCKS、RoomType=Boss、ActFloor=17、TotalFloor=3，明确区分短准备的地图层与实际访问数。根原始12操作、selected10操作、3访问/4attempt；当前原生精确匹配point7 `[1,2,3,4,7,8,9,10,11,12]`，Boss完成未领奖。root/clip生产读取通过，尚非Boss观看实证；后续实际第二幕配置为ACT.HIVE/EVENT.OROBAS。

补存343数据文件、51已知当前文件，相对r2有modded1 latest.mcr/current_run及backup/progress及backup、settings.backup正常录制更新；已知modded2 latest.mcr/progress及backup另有更新（文件时间21:54–21:55），已原样补存，不说全账户零写入、不自动restore、不扩扫history。当前游戏进程0。未产生其他账户读取。证据 `runtime-verification-r3.json`、`collection-r3.json`。

## 003：原生换幕与第二幕入口候选（待动态验证）

新增RunActs只包装原生因果：记录实际Boss奖励页SetLocalPlayerReady输入为一次act-proceed；原生VoteToMoveToNextActAction仍进本体队列，驱动EnterNextAct/EnterAct/SetActInternal。播放沿原生按钮，仅接受当前本地玩家、同Run、同源幕、唯一投票与唯一转换Task，等待真实原生加载/场景/AfterActEntered结束，严格核下一幕MapRoom与清空访问，不额外调用EnterAct。不加载后幕checkpoint、不注入预期资源/RNG。

第二幕地图首点在CurrentMapPoint=null时只接受本体StartingMapPoint，随后仍沿MapTravel实际可达节点；目的访问核Act/ActModel，Ancient实际房型/模型、候选/页面/RNG及选择结果沿RunEvents/RunChoices。实际第二幕Orobas当前只计划选原生第二项代表奖励，不外推其他古神奖励布局；原有自定义布局/额外reward页保护保持。新增Stop开关在原生SetActInternal完成但EnterAct场景未结束时调用公开Stop，直接等待已提交原生转换完成后清理；默认关闭，不让用户抢帧。真实Stop结果待有片后验证。

主体/DirectContracts Release均0warnings/0errors；12项branch合同及11项run-envelope通过，新增合同只证明Boss因果操作和第二幕新visit/真实save分支关系，不当原生换幕证据。r3实际root/Boss片生产reader通过；001/002精确已知录制build显式兼容。证据 `checks/build-003-branches.json`、`checks/build-003-run.json`。

003安装收据 `preparation/build-003-5d10c7a3.json`：DLL `BAE17032415D20C4F7844779B921379D8994ABC7C3BE69AC9106988EDADDA70B`，manifest `69185E5024D560BC5C9B81FC64CC2F732745BCFBA31E64F069CAEF6AAEABE8FB`。`mods/ReplayMod`唯一pair与封存003一致、原002另存核hash；51已知文件安装后仅settings.save变化、零缺失、game0、无restore，见 `preparation/post-build003-check.json`。本轮先续录跨幕与第二幕首普通胜利；换幕Stop开发开关暂不启用，等待该实际链有录像后再合并观看验证。

## r4：真实Boss奖励、换幕、Orobas与第二幕首普通录制（003）

会话 `a049368624d04d3fab96def089abfac8`，126事件、零observer-error/failed/difference/gap/missing。真正新进程Continue读取原生Boss完成保存point7，仍同Run，创建attempt5；Boss领取100金币并选择CARD.UNMOVABLE。只出现一次原生act-proceed（原始操作15，Visit3归因Boss），Before.current_act_index=0、After=1；之后地图实际进入新Visit4/Act1/ACT.HIVE/EVENT.OROBAS，无同坐标并入首幕或新Run。

Orobas实际候选GLASS_EYE / RADIANT_PEARL / TOUCH_OF_OROBAS，选第二项RELIC.RADIANT_PEARL，完成原生选择/离开与地图进入Visit5 `ENCOUNTER.EXOSKELETONS_WEAK`；24步/5回合胜利自动封存format10片 `4c538903df5241cf9451bcc87fd7c779`。实际RoomType=Monster、Act=1、ActModel=ACT.HIVE、ActFloor=2、TotalFloor=5。古神候选与效果未强制，第一项额外领奖页/第三项替换遗物未测，不外推。

root20原始操作、selected18操作、5visit/7attempt，selected `[1,2,3,4,7,8,9,10,11,12,13,14,15,16,17,18,19,20]`；跨幕因果完整在选中分支中，旧两项领取不重复执行。current_run精确匹配最新成功point12完整selected路径、第二幕首普通胜利未领奖；point9/10为第二幕古神前/完成，point11为首普通战前。文件root2,810,210 bytes、第二幕clip1,686,782 bytes；生产reader与资源连续性检查通过、精英/Boss原片hash不变。**本轮仅录制/文件实读，不当跨幕观看成功。**

备份132数据文件、51已知文件/4,169,988 bytes，全部哈希读回、零缺失、game0，无restore；原生modded1 latest/current_run/progress及各backup和settings.backup共6项预期更新保留，modded2本轮无新增变化。证据 `runtime-verification-r4.json`、`collection-r4.json`、`checks/r4-run.json`、`checks/r4-act2-clip.json`。

## r5：完整观看在精英完成Continue边界失败（003）

会话 `1d3a6955dd6d41f18acb529b50666352`，225事件，精英16个原生动作与胜利结果全部匹配，未到Boss/换幕。投影操作5（原始7）首项重领奖前，ApplyContinuations只调用原生UpdatePlayerStatsInMapPointHistory，未重建真正Load的Run；唯一leaf差异 `/players/0/discovered_enemies`：expected缺省空，actual `[MONSTER.SKULKING_COLONY]`。严格校验报 `run-resources-or-rng-mismatch-5-before` 并安全退出，Complete=false。1已拥有任务完成、0取消/0fault，原manager/context及所比较Continue/Progress恢复通过；root/三片与r4保持，当前native精确point12。231数据文件/51已知文件补存，较r4仅settings.backup变化。

本体CombatManager胜利流程为MarkPreFinished → SaveRun(room, saveProgress:false) → UpdateProgressAfterCombatWon → SaveProgressFile；首次敌人记录在后一步ProgressSaveManager中追加到Player.DiscoveredEnemies。因此真实Continue载回的局内发现列表不含这次存盘后追加项，而同一播放中的连续活Run保留该项。r3观看尚无已记录Continue边界，不能替代此新增边界验证。证据 `runtime-verification-r5.json`、`r5-resource-leaf-difference.json`、`collection-r5.json`；该失败不追改为成功。

## 004：真实战后Continue沿自算原生保存点重建（待动态验证）

沿既有D-140的内存保存/原生Load路径处理本次有据的地图战斗完成SL；新增RunRoutes.CompletedMapBattle依据真实attempt.RestoredSave、save.Path末项的已胜利battle、同visit/act/parent及后继领奖/Proceed定位来源，不能按资源相似/seed/坐标猜。实际root两个Continue分别锚定原始精英操作4和Boss操作12；旧event分支保持原路径，缺真实来源的旧片不编造保存位置。

RunCombatLoads只在所需且本次正在播放的普通/精英/Boss胜利原生RunSaveManager.SaveRun前缀，捕获本次ToSave(room)深冻结JSON（原生保存发生时、progress更新前）。原生战斗全部校验后，在已记录Continue边界同隔离manager中调用扩展后的LoadComputedContinuation；真实FromSerializable/SetUpSavedSingleplayer/LoadIntoLatestMapCoord进入FinishedCombat重生奖励，实际加载Task结束再淡入。无读录制后战checkpoint、无expected资源赋值、无删除发现字段；同样补齐其他原生Load归一化行为，避免逐字段修动画/状态。此处是既有已记录Load重演，不新增Seek/Back地点。

Release主体与DirectContracts均0warnings/0errors；13项branch合同通过，新增普通/Elite/Boss保存证据归属、异幕拒绝及非完成边界不猜；实际root生产读取通过并明确两处源操作，均为结构检查非运行通过。证据 `checks/build-004-branches.json`、`checks/build-004-actual-run.json`。

004安装收据 `preparation/build-004-d0fabcad.json`：DLL `7B7410D2A03968C5C8032E1351B078A8508D44348306F314848695BEDFE4E0B1`，manifest仍 `69185E5024D560BC5C9B81FC64CC2F732745BCFBA31E64F069CAEF6AAEABE8FB`；唯一pair已封存/安装核对、原003另存核hash，51已知文件安装后仅settings.save变化、零缺失、game0、无restore。001/002/003精确录制build显式兼容，可直接沿原root/三片重测，不改旧数据。见 `preparation/post-build004-check.json`。

## r6：精英到第二幕首普通完整原生观看通过（004）

会话 `2022f62e9da4494fbacd5e903aae6eb0`，881事件、零observer-error/failed/difference。从精英合法战前入口开始，16/22/24三个战斗原生driver全部完成、AllOperationsMatched=true、三次胜利；12个非战斗操作包括精英奖励/跳卡、地图、Boss奖励选卡、一次act-proceed、第二幕Orobas第二项/离开/首普通地图均资源与RNG精确匹配。Boss及第二幕普通自然初态核对通过，CheckpointLoaded=false/ExpectedStateInjected=false。原生换幕只拥有一次当前幕/玩家投票，实际同Run0→1/ACT.HIVE正确，无额外EnterAct。

精英及Boss分别在原始battle4/12原生SaveRun边界捕获本次播放自算快照；已核战斗完成后，按真实记录的Continue原生重建，使用相同隔离SaveManager，无录制后战checkpoint/expected注入，两个nativeLoad实际成功收束。显示上各有一次主动RoomFadeOut/RoomFadeIn；最初已解释对应录制SL而非每场胜利固定插入。随后D-147明确最终延续demo应剪去此观看过程，所以这两次过场是暂留呈现偏差，不是目标视觉已接受；后台必要恢复与观众展示须分开，暂不改实现、不加测试，待用户/PM讨论播放方式后处理。

根Complete=true、Error=null，最后第二幕胜利AllOperationsMatched=true；22个已拥有任务均完成、0取消/0fault，Stop后原manager/context与所比较Continue/Progress恢复通过。root/三片SHA与r4保持，当前native精确point12。887数据文件及51已知文件补存，较r5仅settings.backup变化，无缺失/无restore/game0。剩余原定换幕加载Stop/重开未测，未提前标包完成；代表性限制及旧失败保留。证据 `runtime-verification-r6.json`、`collection-r6.json`。
