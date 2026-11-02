# RUN-004：精英、Boss与首条跨幕链

## 当前摘要（2026-11-02）

- 本包进行中，**build-003 跨幕候选待安装**。r3/002精英完整16步/胜利/40金币与血瓶领奖全匹配，误报已消除，Stop后原manager/context及所比较Continue/Progress恢复。r2的内部遗物误报mixed FAIL保留，不追改。
- r3真正Continue精确匹配原point4，仍同一Run，保留旧领取5/6为原始事实而不纳入最终路线；新领奖/跳卡→原生地图→瀑布巨人Boss22步胜利自动封存，未领奖。Boss当前仅录制/生产读取通过，未观看；精英四次Back/重做已在r2通过，无需复测旧卡矩阵。
- 当前局 `2b0f7e321a3a4dcd9caf9ee83435a06f`，种子KTRJB2SKRSYV；原生fingerprint精确匹配point7，首幕Boss完成未领奖，57HP/139金币。root原始12操作，最终selected `[1,2,3,4,7,8,9,10,11,12]`，3访问/4attempt。Boss片 `27099a921185492ca48419b00b23a80f`、精英片 `be74f0a6bbb943578c3ec700c328bfbd`。
- 003沿Boss奖励页原生SetLocalPlayerReady→唯一原生Vote→EnterNextAct任务记录/播放act-proceed，实际加载Task收束后核同Run第二幕MapRoom/空地图访问；不直接重复调用EnterAct、不读后幕checkpoint。接第二幕Ancient实际入口/原生选项和首普通战斗；新增默认关闭换幕加载中公开Stop一次性开关。**这些新增路径尚无原生动态结论**。
- 当前已有精英/Boss共用录制、输入/Back与format10第一二幕身份；可选ActModel/RoomType/原生ActFloor/TotalFloor保持旧片省略字段。短准备改暗港首节点为精英并连到本幕原生Boss，牌组/遗物/药水在checkpoint前创建；准备前涅奥虽保留操作，Opening=null，不伪称从涅奥完整重建。普通新局RUN-003路径保留。非战斗Back/跨房间Seek仍禁用。
- r3证据 `evidence/run-004/runtime-verification-r3.json`、`checks/r3-run.json`、`checks/r3-boss.json`；补存 `RUN004-post-r3-current-20261102-221110`，343数据文件、51已知文件/4,159,131 bytes，零缺失，game0，无restore。modded1原生录制更新及已知modded2三项新文件均保留，单列；不称全账户不变。Cloud本轮用户确认关闭，无新进度，另一账户排除。
- 下一步：安装003后用户Continue同一Boss胜利点，正常奖励/换幕→实际第二幕Orobas选择→首普通战斗胜利不领奖并正常退出；一次观看合并验证精英起点至第二幕自然链、Boss及第二幕状态/RNG。之后以同片作新增换幕Stop/重开，不要求重打整幕。

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
