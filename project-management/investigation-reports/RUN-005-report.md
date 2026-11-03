# RUN-005：完整三幕录制与结局闭环

## 当前摘要（2026-11-03）

- 本包进行中，保留 **build-007**，源码 `3364995`。**r9原生结算详情与主菜单退出通过**：终局完整核对，EOF待播0/前进禁用/Auto关闭；原生继续完成分数/徽章摘要，主菜单经Stop收束，13个owner任务全部完成/0取消故障，原context/Progress恢复。r8三次终局核心PASS保持，r7/r8旧UI FAIL不追改。用户要求核代码后向PM核对“EOF后原生继续/主菜单”是否目标交互，产品预期尚待回信；终局在途Stop、完整新局、死亡/放弃仍待。
- Act0/1/2及两次换幕代表链已有证；最后奖励投票/EnterNextAct分支原生进入TheArchitect，独立visit/attempt，不增加幕索引/强制房型/加载后幕checkpoint。与Boss共用原生地图坐标，楼层取实际值（r6 Boss15层/终局16层），不自行增减楼层。
- Lifecycle的Continue/saved-exit已有证；003最后事件操作可选Terminal=victory，After严格冻结真正OnEnded(true)前实际资源/RNG，与同visit/attempt/原始来源位置的Lifecycle事实一致；等待原生WinRun后续强杀/结算页后才封存该操作。播放走同边界比较并保留ShouldSave=false的隔离manager，平台统计/上传/删除普通Continue分支不启用。死亡/放弃仍只有分类基础、末动作/观看待接，不因终局强杀伪造死亡。
- 默认关闭后段准备开关：下一次Continue当前第二幕普通保存点，在原生生成地图后、后续Boss checkpoint前将当前节点直连原生Boss；不改资源/RNG/敌人，明确写入gap并标记后段片。只从后续Boss合法存档起点验证，准备前旧记录不冒充连续整局；未来完整新局样本必须另录。
- 原生TheArchitect为Combat布局但不是内部可战斗场景；003只开放该确切原生事件布局与实际按钮/选择任务，视觉生物不纳入战斗召唤追踪。结算页演出/摘要任务加入本次owner收束，原生返回菜单/时间线出口交现有Stop；终局演出中一次公开Stop开关供之后短测，未改正式UI/模式。
- 主体/DirectContracts Release均0warnings/0errors；005的14项run-envelope含终局事实/原始位置、首句选项自动开放但任何资源/RNG变化仍拒绝；r6实际root尾段与新胜利片reader通过且原件不变。数据检查不当终局原生观看通过；旧15项library/13项branch结果及失败按先前记录保留，不重复无关矩阵。
- 仍受有界文件限制：run64MiB（原16）、clip16MiB（原4），访问256/路线操作2048/单战动作256保持；支持三幕增长余量，未做长局增长/性能实测，不加逐帧文件读取/全局序列化。新事实只在实际生命周期点落盘。
- 当前同Run `2b0f7e321a3a4dcd9caf9ee83435a06f`，root48原始/selected43、10visit/17attempt、6,732,066bytes及r6片不变；原生胜利历史不变，Continue/backup仍不存在。game0，最新运行补存 `RUN005-post-r9-current-20261103-143329`：195数据文件/49已知文件，仅settings及backup变化；无缺失/restore/另一账号读取。r8图鉴偏好变化原证保留，未自动恢复。
- 下一步：向PM核对当前原生结算继续/菜单出口只是本包临时EOF处理还是正式目标；终局在途Stop/重开、死亡/放弃和真实新局整链继续本包，不等新工作包。当前无需再次重录/实战/恢复旧存档。

## 首候选实现与边界

RunTailPreparation只在显式Armed、非观看、已启用录制、当前Act1已访问普通节点时生效；在原生NMapScreen.SetMap之前改本次地图连接，下一Boss前存档自然包含准备地图。原RunRecording保留gap事实；没有迁移到新Run、删除旧操作、重设种子/RNG、改Boss内容或在观看中补资源。Marker归属当前RunState，后续战前片据实标记；跨进程后不伪造旧准备输入。

RunEndings在真正OnEnded首次调用前冻结实际RunResources并按isVictory/IsAbandoned分类，原生已上传标记和本会话首次终局事实保护后续强杀/清理。RunRecording在普通暂停菜单CloseToMenu登记存退意图、实际Cleanup收尾为可续saved-exit；没有凭退出新造成功SaveRun位置，Continue仍只依原有实际成功存盘证据。此候选未声称死亡录像已有最终动作/可播，也没有实现最终自定义事件。

新RunDocument.Lifecycle为可选省略null字段；旧记录字节/校验保持，旧没有结局字段显示“未知”并保留范围/缺口。运行事实含visit/act/BeforeOperation/attempt与原生来源，不给新主菜单/正式UI设计结论。第三幕与前两幕继续用相同本体战斗、native queue/选择/Back、EnterNextAct与Ancient路径；所选新敌人/古神和第二次换幕需人工动态验证。

001安装收据 `evidence/run-005/preparation/build-001-b9115c1f.json`：DLL `4B591FA3D071A67E8D4332355825BF769B6708C919D2093A62F7531332DF8657`，manifest `69185E5024D560BC5C9B81FC64CC2F732745BCFBA31E64F069CAEF6AAEABE8FB`；唯一mods/ReplayMod pair与封存001一致，原RUN-004/004另存核hash。51已知文件安装后仅settings.save变化、零缺失、game0、无restore，见 `preparation/post-build001-check.json`。工具只补run-005和既有备份名，不新建账户/扫描工作流。

PM UI分线：已结束对局未来正式入口为百科大全→历史记录→当前局详情观看，左右切换继续本体；当前主菜单回放仅开发临时。UI讨论/历史身份绑定不在本候选实现；已知原生history/{StartTime}.run身份可由后续真正终局事实保留，当前Battle.RunStartTime已登记真实起始时间，不借此宣称正式关联已完成。

## r1：第二次换幕与第三幕存退录制（001）

会话 `4b24899b06e742d4a47d121105a4d5b0`，137事件、零observer-error/unsupported-or-failed/missing。Continue精确恢复原point12同Run、创建attempt8；显式后段准备在原生地图生成后把当前第二幕普通节点连到本幕原生Boss，原资源/RNG/敌人不改，原始操作21明确gap。该gap是预定准备范围，不当录制程序失败，也不视作完整新局证明。

第二幕 `ENCOUNTER.KNOWLEDGE_DEMON_BOSS`19步胜利封存片 `37525b91f57c4fcb92895ad4ee008173`，Battle.Act1/ACT.HIVE/Floor16/TotalFloor6；实际奖励与第二次act-proceed完成后，进入新Visit7、Act2/ACT.GLORY/EVENT.NONUPEIPE，选择第一项RELIC.FUR_COAT，离开并自然进入Visit8 `ENCOUNTER.SCROLLS_OF_BITING_WEAK`。一张CARD.DEFEND_IRONCLAD完整结算后保存退出，format10一步中断片 `34f3f3c2d444422eb54baa2c76481d73`，Battle.Act2/Floor2/TotalFloor8、Completion=interrupted、Victory=false。

Lifecycle明确原生SetUpSavedSingleplayer的continued和暂停CloseToMenu→Cleanup的saved-exit，非死亡/放弃。当前原生fingerprint精确point18（PreFinished=false，Path到原始34），没有把未存盘的原始battle35一步操作拼进保存位置；第三幕真正重启Continue与新attempt等待下一轮。root35原始/selected33，8visit/11attempt，后段覆盖从原始26第二幕Boss合法checkpoint起；未混淆三个幕复用坐标。

root后段与Boss片、第三幕一步片分别使用生产reader校验通过且原件不变；纯验证工具补显式起点与中断片选项，仍分别标注尾段/中断/非观看，不把胜利专用checker的拒绝解释为游戏故障。最初以整局默认起点检查被准备gap的Continue前保护拒绝，以胜利专用checker检查一步中断片亦被拒绝；这些边界保留在 `checks/r1-scope-boundaries.json`，未放宽runtime/删除gap或改数据以求PASS。新检查CLI在direct-tests内，主DLL/001不变。

补存142数据文件、51已知当前文件/4,136,544bytes，6项实际modded1录制/进度及settings.backup更新保留，零缺失/game0/无restore/另一账户未读。证据 `runtime-verification-r1.json`、`collection-r1.json`、`checks/r1-tail-run.json`、`checks/r1-clip-37525b91.json`、`checks/r1-fragment.json`。**本轮只录制/实读，第二次换幕及第三幕自然观看尚未证明。**

## r2：第三幕跨进程Continue与独立胜利录制（001）

会话 `e5b44a6e3205405b89f5c223ece0e208`，39事件，零observer-error/unsupported-or-failed/missing/gap。真正重启后Continue匹配原point18同一native SaveKey，Act2、PreFinished=false，加载到setup之间未变；visit8新attempt12明确RestoredSave18、Parent11。原一步防御片及source35保留在原始历史，最终selected不含35；新clip `3923cae38edb4e019a44631b531b4c63` 为format10、continue-known、Battle5/Attempt2、5步胜利，source36进入最终路线。没有将旧防御操作接在本次战斗前。

本次原生point19登记加载边界，胜利原生SaveRun登记point20，NativeFingerprint精确匹配当前Continue，PreFinished=true，path最终为source36；用户未领奖/未前进，生命周期再次continued→saved-exit，无终局误判。root36原始/selected33，8visit/12attempt，5,040,165bytes。生产root后段reader（原始26起）及新胜利片reader/兼容验证通过且原件不变；均不是原生播放通过。证据 `runtime-verification-r2.json`、`checks/r2-tail-run.json`、`checks/r2-clip-3923cae3.json`。

补存45数据文件与51已知当前文件/4,147,384bytes，零缺失；6项变化为modded1最新replay、current_run及backup、progress及backup、settings.save.backup。game0、唯一安装001 hash不变、无restore/另一账户未读，见 `collection-r2.json`。下一轮只需同版本观看合法后段，不重打旧Boss或再录第三幕首战。

## r3失败及002修复：战斗Back遗漏发现门控上下文

会话 `5c13952b5c44482da107c85690609349`，426事件。Boss原生19步及3次Back恢复/重做已执行，最终胜利核对通过；首次奖励前，selected25/source27报 `run-resources-or-rng-mismatch-25-before`。逐项比较仅玩家discovered_cards期望6/实际5、少CARD.SLOTH，其余资源/RNG无差异；不能把本轮称整链或第二次换幕通过。`playback-closed`确认原manager、Continue及原Progress恢复，root hash保持r2原值；51已知文件相对r2全不变。失败保留 `runtime-verification-r3.json`、432数据文件归档/`collection-r3.json`，最新补存 `RUN005-post-r3-current-20261103-121622`。

原生ProgressSaveManager.MarkCardAsSeen先调用ProgressState内_discoveredCards.Add，仅首次seen且LocalContext匹配才向player.DiscoveredCards追加。DirectCheckpoint原来只回退局内Player，没有回退隔离Progress的门控集合；第一次13步选择看见Sloth后Back，重做时门控已为seen，玩家发现列表因而漏项。002在实际本次观看checkpoint采集六个原生发现/FTUE集合，用既有IdentityMemento保存/恢复集合内容，随局内数据一起回退；捕获/恢复均严格绑定同一隔离manager及Progress实例。不是载入录制expected、改玩家发现项、放宽校验、写原账户或全量序列化档案。文件冷恢复不借UI快照伪造该上下文；新增结构不进录像格式。001来源兼容显式加入，原始片无需迁移。

主DLL Release0warnings/0errors；既有6项IdentityMemento合同（含集合/独立边界反复恢复）通过，仅数据机制验证，原生修复仍待用户测。002安装 `preparation/build-002-6b5c296e.json`，DLL `90B730881BE26B2B35E23E44383DADD976CF6C17CD40CAE97BF6DE0F203BE5F3`，manifest同001；001封存保留、唯一安装pair匹配。安装前后51已知文件核对见 `preparation/pre-build002-current.json`、`post-build002-current.json`、`post-build002-check.json`，game0、无restore/其他账号读取。

## r4：002后段原生观看与发现回退修复通过

会话 `8ee7f2e42c984fd38af04fe3c380e8d6`，1083事件、零failed/observer-error/resource-difference/unsupported-or-failed。两次从原始26/selected24第二幕Boss合法起点完整观看到selected33第三幕首战胜利；每次Boss19步及首战5步原生结果匹配，8项奖励/路线操作逐项资源/RNG匹配。Act1→2由同一RunState的原生奖励投票/EnterNextAct单次调用产生，Nonupeipe及普通场景原生生成，后战初态/RNG自然匹配，未加载后幕checkpoint/注入expected。第三幕采用attempt12的source36，旧source35仍不拼接。

5次Back恢复到0/0/12/12/0，发现门控集合均从同次观看内存恢复；Boss第二次选择前反复Back/重做后，原r3失败的首次领奖校验通过，CARD.SLOTH无缺项。两次完整链Stop及第三次重开第三幕首战一步后Stop皆无错误；前两次16个owner任务、第三次0个任务完成/零取消故障后恢复原manager/Continue/Progress，第三次Complete=false为计划内短测。不是新增换幕在途Stop专项或最终整局胜利证据。

证据 `runtime-verification-r4.json`、`collection-r4.json`；1089数据文件与51已知当前文件/4,147,384bytes补存核hash。原始root及三片不变、native仍point20第三幕首战完成未领奖，modded1存档/进度不变，只有settings.save及backup变化；game0、无restore/另一账号读取。至此第二次换幕/第三幕代表录制、真实Continue与连续观看已获证，真正终局及完整新局仍待。

## 003：第三幕Boss后的原生胜利候选（未动态验证）

RunActs沿原生奖励按钮→SetLocalPlayerReady→VoteToMoveToNextActAction→EnterNextAct分流；最后一幕保持Act2并验证实际TheArchitect胜利房间，而非SetActInternal到Act3。RunRecording.Room为原生胜利房间建立单独Victory visit/attempt，RunEvents保留该确切Combat事件的每个实际按钮与Chosen任务，后续对话/攻击原样运行；其他自定义Combat事件仍不开放。该房间的视觉creature不当作受控战斗中的召唤。

可选RunOperation.Terminal只允许最终TheArchitect的event-choice原生victory事实；After与Lifecycle.State均冻结在第一次OnEnded(true)入口、统计/平台副作用和WinRun强杀之前。生产reader要求位置/visit/attempt/状态吻合，原始SourceSequence经SL选路重排后仍用于关联；不是把强杀后的账户统计差异过滤掉，新的终局操作从定义起就使用这个原生边界。最后提交的原生Task及gameover出现均确认后才封存/比较；同次WinRun随后OnEnded(false)依原生已上传标记不覆盖胜利。播放维持隔离manager/ShouldSave=false，原native WinRun/强杀/gameover仍执行，普通账户的历史统计/平台上传/删Continue分支不启用。

gameover的AnimateIn/Quote/Summary加入owner Task追踪；Stop可取消显示但必须在scene/task退休后恢复原context。原生游戏结算返回菜单/时间线转既有Stop出口；最后WinRun业务已提交时直接等其完成再处理Stop，一次测试开关在OnEnded入口经公开PlayerLoop.Control(stop)触发，候选未证明在途退出通过。旧001/002来源hash显式兼容，旧无Terminal字段仍省略、原件不改。

后段准备开关扩到Act1或2，仍仅已访问普通节点在下一Boss checkpoint前改地图连接，资源/RNG/原生Boss不改，按真实幕号明确gap/marker；当前第三幕后段将另从本幕Boss合法起点验证，不让准备前链冒充完整连续新局。主DLL/DirectContracts Release0warnings/0errors，13项run-envelope含真实终局事实与来源位置拒绝、13项branches和15项library读取合同通过，r4实际root新reader实读通过。library首试在合成journal的File.Move遇AccessDenied，原FAIL与测试目录保留；未改生产写入，单独新目录复跑通过，原因未证，非游戏运行失败/终局PASS。

安装 `preparation/build-003-55bcaf3c.json`：DLL `565CD1A9A9AF0C54B09E6FF3E1179FB341D412ABF56AB5ED2D1FD7E0B551DB7A`、manifest同002；002扫描树外封存保留。51已知文件备份/安装核对见 `preparation/pre-build003-current.json`、`post-build003-current.json`、`post-build003-check.json`；game0，无restore/另一账户读取。首轮仅请用户录制第三幕Boss→真正终局，死亡/放弃不混进同轮长测。

## r5：003终局录制未完成；004准备加强

会话 `e1ad3a97bb5447f6a0ab77b65d9264d7`，179事件、零观察器错误，唯一gap是本轮显式第三幕中段直连准备。实际Continue恢复第三幕普通完成point20，领奖/跳卡/前进与地图到Aeonglass Boss正常，native point22为新Boss战前。用户打不赢当前准备，正常存退后clip `404789de133e45a2b85715bfdeb68819`封存format10、37步interrupted/Victory=false，Battle6/Attempt1/Act2/第15层；rootsource42，未进入TheArchitect，无整局胜利/死亡/放弃。reader仅中断片合同通过，非新增播放/终局通过；事实 `runtime-verification-r5.json`、`checks/r5-interrupted.json`、`collection-r5.json`。保留183数据文件及51已知当前文件/4,223,057bytes，6项正常modded1最新replay/current_run及backup/progress/settings及backup变化，game0、无restore。

原生Aeonglass开场512HP、3层Artifact，旧两张消耗Impervious/两张BodySlam准备缺持续格挡且不能依赖易伤。004不换怪/降HP/跳杀，新增一次默认关闭的RunBossPreparation：仅下一第三幕铁甲A0单人、无modifier、未完成Boss Continue且尚未创建房间；保留当前遗物/药水，原生RunState.CreateCard/归属登记构建Barricade+、2Impervious+、Entrench+、2BodySlam+，回满HP。皇家许可按原生CanEnchantCardType仅给五张攻击/技能先天保留，壁垒为能力牌不非法附魔，六牌加现有BagOfPreparation足以首回合全抽到。准备前写明确gap，删除旧deck牌也用原生RemoveInternal/RunState.RemoveCard；旧37步片/失败尝试不删，未按录像expected赋值，run RNG实采前后严格相同。

在LoadIntoLatestMapCoord原生调用前，由真实RunManager.ToSave(null)取得本次准备的实际DTO，调用原生RunSaveManager.SaveRun(dto,false)，等待成功、精确读取存储fingerprint并用原生LoadRunSave再次读回；同一DTO避开第二次ToSave的时钟字段差异。Recorder.PreparedContinue只为后续BeginEntry替换此次实际已成功写读的战前输入，不追改原loaded/setup日志、不重关联原SL、不修改播放。然后调用一次原LoadIntoLatestMapCoord创建战斗，新片marker明确六牌/回满血/前缀缺口；后续播放只从该合法新战前点原生执行。错误不自动回滚，准备后原生Continue会保留六牌，安装前现有point22和旧牌组已备份。

004只增准备/003来源兼容，不改003终局核心；主Release0warnings/0errors，未增加镜像合同替代原生检验。安装 `preparation/build-004-71f96920.json`：DLL `329A01248024F56D130AF6A09022BD9DCA2B540C5B260D413A05BA4F3B891310`、manifest同003；003扫描树外保留。51已知当前文件补存及核对见 `preparation/pre-build004-current.json`、`post-build004-current.json`、`post-build004-check.json`；game0、无restore/另一账号读取。下一轮只需Boss重试至终局，不重跑已通过的第二次换幕链。

## r6：真正胜利录制；005接入终局首句独立Task（观看待测）

会话 `1d8040a3cd4e4385a707446b8562874a`，95事件、零观察器错误。首次Continue未做加强/零步即存退，保留source43 missing空尝试、未伪造clip；第二次Continue同Run/visit9新attempt16，六牌/满血准备以真实ToSave DTO调用原生SaveRun写入并精确读回，原生Save point25路径含明确准备gap44。随后clip `2db465d7fe7a4c7ab379d4425e7649fc`，format10、Battle6/Attempt3/Act2/第15层、13步胜利，source45；旧37步与空尝试保留但不拼接。

source46原生act-proceed进入独立visit10/attempt17的EVENT.THE_ARCHITECT，仍Act2、原坐标row14col3、实际ActFloor16；source47选择THE_ARCHITECT.dialogue.0，source48选择PROCEED并执行演出/WinRun。Lifecycle在真正OnEnded(true)登记victory/BeforeOperation49，和source48.Terminal/After冻结状态精确一致；随后native gameover出现，后续强杀/清理未覆盖为死亡/存退。原生 `saves/history/1793620706.run` 实际win=true、was_abandoned=false，当前Continue及backup由原生结束正常删除。终局史按精确已知StartTime路径定点复制核hash，未扫描账户其他history；数据 `runtime-verification-r6.json`、`collection-r6.json` 与clip reader收据保留。整局Opening仍null，准备前gap保留，不能冒充完整新局通关。

首次尾段reader拒绝source46.After→47.Before，实读差异仅event_state.Options由空变成首句选项；所有资源/RNG相同。原生TheArchitect.OnRoomEnter先ClearCurrentOptions，再独立TaskHelper.RunSafely(PlayCurrentLine)，EnterNextAct结束时首句Task仍在播放，之后自己SetEventState恢复选项。不是漏录玩家决策，也不是录像损坏；首次拒绝保存 `checks/r6-initial-boundary-failure.json`，不追改原始两个快照。

005钩住原生PlayCurrentLine并登记owner，最后act-proceed的新录制等待该Task后再封存；观看亦等原生首句结束，Stop前收束。兼容本次已有早边界时，只识别“最终Boss→明确Victory/TheArchitect→首句dialogue.0、Options空→1且其余所有JSON内容/资源/RNG严格相同、下一项RewardKey与实际ready事件状态一致”的原生自动开放。播放仍逐项严格核对：用本次原生EnterRoom完成（外层淡入前）的真实入场快照核旧After，再用实际首句完成状态核下项Before；未注入expected、删除快照或把资源/RNG差异藏为过场。读取结果明确列NativeFinalOpeningTransitions，合成反例要求HP/RNG/选项/幕身份任何错误仍拒绝。r6后段reader从原始45起现通过、原root字节未变；属于兼容/读取证据，原生观看仍待。

101归档文件含胜利历史、2片、root、journal及会话；49当前已知文件/4,042,456bytes，原51中current_run及backup缺失对应实际Win删除，5项正常latest replay/progress及backup/settings及backup更新保留，无restore。005安装 `preparation/build-005-fe092253.json`：DLL `5F0AE9B1DFFE7C9121D25B57C0513417D95A913E005CE1FE2C976BC1464193A6`，manifest同004，004封存保留。49文件/精确胜利history与Continue仍不存在的核对见 `preparation/pre-build005-current.json`、`post-build005-current.json`、`post-build005-check.json`；game0/另一账号未读。下一轮直接看现成终局片，不要求重打/自动restore。

## r7：005空奖励页就绪等待停滞；006修复

会话 `9cae75c210d749d4b75160525ec15566`，767事件。两次实际Boss13步原生胜利全部匹配，之后均停在selected41/source46的“显示战斗奖励”；没有任何路线操作完成/原生终局入口记录。Auto切换和手动连续Step请求均被接收（第二次Whole累计13），但卡在奖励就绪等待，未消费后续请求；用户两次Stop，Complete=false且Error=null。这是可观察停滞FAIL，不以未抛异常当通过；原manager/所比较Continue及Progress恢复、原native胜利历史/root/clip不变，005首句边界及终局Stop尚未触及。

原生NRewardsScreen.UpdateScreenState在_rewardButtons.Count==0时将IsComplete=true、隐藏Rewards窗口至alpha0并开启Proceed；该次最终Boss没有待领奖项，原录像直接act-proceed。播放器第一层却无条件RewardsReady(requireVisible:true)，永远等不到窗口alpha>=.99，虽然原生页面已完成。006仅按下一实际操作决定：reward仍要求可见offer；proceed/act-proceed允许原生IsComplete完成状态，继续等待既有原生Proceed按钮可用后调用原OnProceedButtonPressed。未减少后端/RNG/内容校验，不丢弃待播请求，不手动显示已隐藏窗口或跳过原生奖励业务；新增run-rewards-ready日志记实际IsComplete/奖励数便于核验。

证据 `runtime-verification-r7.json`、`collection-r7.json`，772归档含精确胜利历史/片/root/journal，49已知当前文件/4,042,456bytes只有settings.save变化。主Release0warnings/0errors，变化仅原生UI就绪条件与诊断，不另造镜像测试代替人工回归。006安装 `preparation/build-006-c37ddd42.json`：DLL `DAD2FA00626330E2937F9D8D85909E00696108E260A601F5CFF3A41769327848`，manifest同005；005封存保留。49文件及胜利history/Continue仍不存在核对见 `preparation/pre-build006-current.json`、`post-build006-current.json`、`post-build006-check.json`；game0，无restore/另一账号读取。下一轮先一次完整观看确认这处接通。

## r8：006终局核心播放通过；007修复终点原生继续与无效队列

会话 `9b06517bf2ec4eada6e2659debd5ea1a`，599事件、零failed/observer-error/resource-difference。3次完整从原始45/selected40的Boss起点播放：每次Boss13步匹配，原生空奖励Rewards=0/IsComplete=true就绪接受，原生EnterNextAct单次投票进入TheArchitect，首句Task完成且实际entry snapshot/后续ready状态严格匹配；source46/47/48共9次路线操作核对，3次真正OnEnded胜利/原生gameover均匹配。同RunState/隔离manager，未载后幕checkpoint或注入expected。3次最终Complete=true/Error=null、Stop后原manager及所比较Continue/Progress恢复；终局在途Stop开关未触发，不外推该路径通过。

用户仍不能点gameover第一屏的继续，且结束后>>仍累加待播。原因是RunPlayback非战斗分支到EOF后只有auto=false/等待Stop，没有终点标志；Ui始终套只读输入罩、没有禁用前进，Control仍收请求。这不等于最后原生WinRun漏播：真实native-game-over-shown和资源核对均完成，Auto在记录EOF关闭原设计保持。007增加完成标志，实际EOF清空fine/whole/continuation、禁用前进/Auto并从控制层拒绝新请求，Stop始终允许；只在同owner实际gameover已到录像EOF时让输入罩穿透，允许原生继续→分数/徽章详情。没有伪造新的录像动作或自动点击未记录的继续，不放开普通事件/商店的非记录决策。摘要沿原OpenSummaryScreen/AnimateRunSummary运行并加入owner任务、记录实际完成/隔离manager事实；EOF仍监测原生Task失败，退出先收束，原生结算返回菜单/时间线仍走已有Stop。

证据 `runtime-verification-r8.json`、`collection-r8.json`：604归档含精确胜利历史/root/片，49已知当前文件/4,042,458bytes，root/片/胜利历史/普通Progress不变；4项变化为prefs及backup、settings及backup。prefs准确仅bestiary_actions_preferred由true变false，原生写点为NBestiary.OnSubmenuClosed，具体操作时间未证、不擅称观看期间写入或全账户零变化，保留并补存。无缺失/game0/restore/另一账号读取。

007主Release0warnings/0errors；本次为原生UI门控与EOF控制，不写布尔镜像测试替代人工。安装 `preparation/build-007-1cce6ad9.json`：DLL `4290F164B3A6339E923E9033C3AEC023C94265CBFFDA3537A11293E0C5BCDE03`，manifest同006，006封存保留。49文件/原生胜利history及Continue仍不存在核对见 `preparation/pre-build007-current.json`、`post-build007-current.json`、`post-build007-check.json`；game0，无restore/另一账号读取。下一轮先确认原生继续可点、摘要完成与Stop恢复。

## r9：007代码边界与原生结算详情/主菜单退出核验

用户确认原生继续和主菜单现可点击，要求先核实现再与PM讨论预期。会话 `3f38c3c89e094cc4b7a5049de7747915`，190事件、零failed/observer-error/resource-difference；原生胜利终局及三项路线核对完成。EOF日志Pending=0/AdvanceDisabled=true/Auto=false，NativeGameOverControls=true；原生摘要Task实际完成，IsolatedManagerOnly=true、AdditionalRunDecision=false。用户本次通过原生主菜单按钮退出，source=native-game-over-exit，NormalSaveRequested=false；13个owner任务全部完成、0取消/故障后才恢复原manager/context/所比较Continue与Progress。Complete=true、Error=null。原root/片/精确胜利历史未变，Continue仍正常不存在；195归档和49已知文件只settings及backup更新，见 `runtime-verification-r9.json`、`collection-r9.json`。

定点代码核对：Ui仅在RunPlayback.Finished且同RunState的GameOverActive成立时穿透只读罩，普通战斗/事件决策不因此开放；Control在EOF拒绝前进/Auto、Stop入口仍在前。原生OpenSummaryScreen只打开AnimateRunSummary，徽章写入使用当前隔离SaveManager，实际Task登记owner。原生主菜单分支的ReturnToMainMenuAfterRun/GoToTimelineAfterRun均被ExitAfterRun导向现有ExitFromMenu/Stop，不继续普通Run/额外Save或进入真实解锁流程；原native调用者不等待自身引发的完整teardown，避免相互等待，实际任务收束与恢复由PlayerLoop finally统一执行。本轮代码路径与当前实现意图一致，没有需先修的运行错误；这不等于产品已认可“EOF在gameover第一页、再手动继续查看摘要/主菜单”的交互。向PM交接时明确当前仅主菜单开发入口，未来从历史/回放管理发起观看的返回上下文未实现。

本轮无新DLL、不重复已答长链；原生结算在途Stop开关未触发，不能因普通菜单退出通过而声称所有退出时机通过。剩余工作按原包继续，交互结论待PM/用户明确后再写为正式目标。

## 后续必须完成

- 当前后段原生观看及第三幕真实Continue分别由r4/r2证明；新增终局/换幕在途Stop可与后续样本合并。
- 最后Boss→TheArchitect/WinRun录制/核心观看/摘要交互与普通菜单退出已有r6/r8/r9；终局在途Stop仍待。EOF/原生按钮产品预期需PM核对，不把技术通过自动写成最终交互规格。
- 最短死亡与主动放弃样本：分类、最后真实动作/落盘、合理观看EOF/Stop，保存退出不当放弃。
- 真实新局合法起点至三幕原生终局同份完整录制/连续观看，准备若存在必须在新局实际输入处明确可重现；当前后段/旧准备局Opening=null不能替代此证据。
