# RUN-005：完整三幕录制与结局闭环

## 当前摘要（2026-11-04）

- 当前 **build-016/r19** 已通过主动放弃代表链和遮罩回归。三次确认触发真实原生放弃、资源/RNG 精确核对与隔离 `AbandonedRun`；另一次在确认窗 Stop，只清理弹窗和遮罩，无终局或历史构建。用户确认两种关闭均无残留。此前 **015/r18** 的视觉失败保留。
- 016 复用原生 `NModalContainer.Clear`：确认后执行原生遮罩淡出，窗内 Stop 清理同一弹窗。原版 `NVerticalPopup.Close` 是按钮独立回调，现也纳入放弃回放的输入保护。Release 0 警告/0 错误，安装收据 `evidence/run-005/preparation/build-016-e59cd977.json`。016 不要求重录。
- 余项：从真实新局开始的三幕连续录播，直至原生胜利。之前通过的胜利样本仍为后段。

## build-015 阶段摘要（2026-11-03）

- 本包进行中，当前 **build-015，主动放弃观看候选，待原生播放**。**014/r17真实放弃录制/生产读取已通过**：一张防御独立interrupted前缀，随后原生确认放弃、Terminal/Lifecycle=abandoned，真实history was_abandoned=true/win=false，未误记死亡。015补确认/取消输入仅接受播放器授权，显式兼容该14原片。013/r16死亡分段已有证，旧FAIL保留；放弃观看/确认窗Stop及真正完整新局三幕仍待，不当整包完成。
- Act0/1/2及两次换幕代表链已有证；最后奖励投票/EnterNextAct分支原生进入TheArchitect，独立visit/attempt，不增加幕索引/强制房型/加载后幕checkpoint。与Boss共用原生地图坐标，楼层取实际值（r6 Boss15层/终局16层），不自行增减楼层。
- Lifecycle的Continue/saved-exit、胜利/死亡录制观看及主动放弃录制已有代表证；OnEnded前冻结真实资源/RNG，与同visit/attempt原始来源Lifecycle核对。r17 raw5的放弃After精确等于同visit2/attempt2的abandoned事实State，原生gameover/task完成后封存；播放仍待。r16死亡结局Back/隔离History/物理文件保持已有证，不外推放弃观看。
- 默认关闭后段准备开关：下一次Continue当前第二幕普通保存点，在原生生成地图后、后续Boss checkpoint前将当前节点直连原生Boss；不改资源/RNG/敌人，明确写入gap并标记后段片。只从后续Boss合法存档起点验证，准备前旧记录不冒充连续整局；未来完整新局样本必须另录。
- 原生TheArchitect为Combat布局但不是内部可战斗场景；003只开放该确切原生事件布局与实际按钮/选择任务，视觉生物不纳入战斗召唤追踪。结算页演出/摘要任务加入本次owner收束，原生返回菜单/时间线出口交现有Stop；终局演出中一次公开Stop开关供之后短测，未改正式UI/模式。
- 主体015 Release0warnings/0errors，git diff检查通过；014的15项run-envelope与旧胜利/死亡reader保持。r17新实际放弃root合法source4起点及一防御中断片生产reader通过、原件不变；015仅加观看输入限制/已知来源兼容，没有格式变化，不重复无关矩阵。数据PASS不当原生放弃观看。
- 仍受有界文件限制：run64MiB（原16）、clip16MiB（原4），访问256/路线操作2048/单战动作256保持；支持三幕增长余量，未做长局增长/性能实测，不加逐帧文件读取/全局序列化。新事实只在实际生命周期点落盘。
- 新放弃Run `b5221ca5d465457ab90e1f93231fbfec`、clip `0ae0a298ed894a08ba328e7c426ffd5c`，Opening=null、raw5/source4战斗/source5放弃，不当完整新局。r17归档34文件/29事件，49已知文件：真实放弃正常更新native replay、Progress及backup、settings及backup，原进度保留、无restore。015唯一pair核对、014另存；胜利/死亡/新放弃三份history hash均不变、Continue/backup仍无。最新 `RUN005-post-build015-current-20261103-235107`，安装前相对r17全不变、安装后仅settings.save变化，game0、另一账号未读。
- 下一步：从整局列表最新放弃局source4首战起点Auto，核防御→原生确认放弃→放弃结算及详情后Stop；重开>>完成防御、>打开确认窗但不确认，Stop清理后正常退出。复用现片，不重录。放弃仍只覆盖稳定完整动作前缀的直接战斗；随后真正完整新局三幕。

## r18：主动放弃回放与确认遮罩（015）

会话 `06bb68c038734c29a0bf2354afe9c057`，182事件。一次 Auto 与两次手动均完成防御、原生确认、实际 OnEnded(false)、隔离的原生 AbandonedRun 历史构建及 EOF。Auto 在结算详情留场；手动结算 Back 保持同一 RunState 与资源/RNG。三次 Stop 均恢复原管理器和进度，原生任务无失败/取消，游戏正常退出。三份实际胜利/死亡/放弃 history 哈希保持，Continue 及备份仍无；49 已知文件仅 settings.save 与其备份变化。证据 `evidence/run-005/runtime-verification-r18.json`、`evidence/run-005/collection-r18.json`。

视觉 FAIL 保留。原版按钮的 Released 同时调用 Yes/No 与 `NVerticalPopup.Close`，015 直接调用 Yes 导致弹窗被释放，但没有清理 `NModalContainer.OpenModal` 或背景遮罩。016 显式补原生 Clear，按弹窗身份清理，确认与中断分别记录。当前安装后备份 `RUN005-post-build016-current-20261104-134114`。

## r19：主动放弃遮罩与窗内 Stop（016）

会话 `9998b134d5cf4a418d043681fa2ec28f`，250事件、四次观看。三次 `confirmed` 清理及一次确认前 `interrupted` 清理均启动原生遮罩淡出。确认前 Stop 无确认阶段、终局或新 History，`Complete=false`、`Error=null`，返回原上下文；三次确认均 `Complete=true`。四次原生任务收束无失败或取消，游戏正常退出。三份真实 History 及原录像和战斗片 SHA256 与 r18 一致，原存退与进度检查通过，49 个已知文件仅设置及其备份变化。证据 `evidence/run-005/runtime-verification-r19.json`、`evidence/run-005/collection-r19.json`。最新备份 `RUN005-post-r19-current-20261104-135830`。

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

## D-159与008：用户确认Auto自动展开详细结算（未动态验证）

按用户要求先核007代码/r9后发PM精确行为与证据。PM确认技术逻辑符合临时EOF实现，原生继续不是普通Continue/新的Run业务，当前从主菜单开发入口发起、同名主菜单经安全Stop返回主菜单可作临时出口；正式返回历史详情/回放管理来源及准确文案未定稿，不在本包静默改名或跳页。PM当场询问用户Auto应停哪页，用户明确选择“自动展开分数/徽章等详细结算后停住”，已写D-159/PLAYBACK-EXPERIENCE。本报告此前“产品预期未确认”的状态据此更新，不把r9旧行为追改为新Auto已通过。

008在最后业务严格核对之后采集AutoAtArrival，立即标记录制EOF/清待播/禁前进；若Auto且实际原生gameover，则等原生继续IsEnabled，调用原OpenSummaryScreen，await实际AnimateRunSummary完成后才关闭Auto、保留结算画面。手动EOF不自动展开，继续由用户点击，原生主菜单/Stop保持既有安全桥。摘要是观看呈现，没有新增录制操作/第二次OnEnded/expected资源赋值；没有普通中断片的假结算。记录结束到详细结算完成两阶段分别记run-recorded-end-reached和run-playback-endpoint，额外原生任务沿owner/Stop收束；用户先手动开启时复用已有Task，原生摘要入口的同次重复触发受保护，不重复展开或双计徽章。当前胜利路径候选已构建，死亡录制/播放本来尚未接入，不能因此宣称死亡结算亦通过。

主Release0warnings/0errors；只验证相关编译和原生源码入口，未以布尔镜像测试替代新Auto人工运行。安装 `preparation/build-008-52bbb2fe.json`：DLL `3EC097E27779FB1C2FA466E660D6E467410D33F7F2291FD69EEC74E3C50752C5`，manifest同007，007扫描树外保留。49已知当前文件、精确胜利历史及Continue仍不存在的核对见 `preparation/pre-build008-current.json`、`post-build008-current.json`、`post-build008-check.json`；game0，无restore/另一账号读取。原PM四文档按授权在自然点独立提交，具体D-159原文归PM维护。

## r10：008 Auto细化通过；009仅Stop出口与有缺口起点准入

会话 `ff39df017bb9472699a4fff1a9839e00`，214事件。一条从原始45起的真实终局链成功：Boss13步及source46/47/48匹配；Auto到EOF时原生继续处理函数实际只调用一次，NativeSummaryTask完成/隔离manager，随后Auto=false、Pending0/AdvanceDisabledtrue/SceneKeptOpen，AutomaticExit=false。13个owner任务完成、0取消故障，原manager/context/所比较Continue与Progress恢复。用户确认Auto目标符合预期，同时明确“主菜单应禁用，仅Stop作为退出按钮”。此新指令覆盖D-159此前允许开发临时原生菜单退出的部分，已同步PM；r10实际仍用旧原生菜单退出，不能算only-Stop已验证。

同会话前三次选旧精英起点报run-completed-map-load-anchor-missing，在RunPlayback.Begin、创建/安装隔离manager之前即拒绝；日志OriginalManagerRestored=false等因尚无memory context，不据此猜真实进度被改。该准备局在这些起点后有显式gap，UI原来只检查资源不连续，仍允许点进，实际Begin却先构造完成Continue锚点映射导致不清晰报错。009共用HasUnrecordedAfter给列表/生产Begin/生产reader：有gap或未完成项在所选起点之后就提前禁用/解释，外部强行调用也在continuation映射前拒绝；尾段45及独立分场仍可读，不放宽保护/删除旧事实。实际reader证明source4旧战斗起点因gap拒绝、45通过且原件不变；初次CLI误传非战斗source1的拒绝亦记在收据，不当游戏失败，见 `checks/build009-rejected-prefix.json`、`build009-valid-tail.json`。

009在同owner观看gameover进入与原AnimateRunSummary完成后禁用实际_mainMenuButton（原生也可将此同一按钮标为解锁出口），OnMainMenuButtonPressed另加防御拦截，普通游戏不受影响。继续、Auto展开和终点队列规则保持，仅Stop作为结算页可用退出按钮；内部意外return的安全收束兜底保留，不当另一个可点击出口。状态文案同步只提示Stop退出，不暗示已禁用按钮还可用。未新增正式观看来源返回设计。

原FAIL与目标PASS分开保留在 `runtime-verification-r10.json`、`collection-r10.json`；219归档/49已知当前文件4,042,458bytes，原root/片/胜利历史不变、Continue仍不存在，仅settings及backup变化。主/DirectContracts Release0warnings/0errors，相关实际数据双向准入检查通过，按钮实际置灰/终局在途Stop仍待用户窄测。009安装 `preparation/build-009-354be681.json`：DLL `C29347063776CC467C8BE77860E811296B2F34B5D1B80277536261A90F40B768`，manifest同008，008封存保留；49文件/历史/Continue核对见 `preparation/pre-build009-current.json`、`post-build009-current.json`、`post-build009-check.json`，game0，无restore/另一账号读取。

## r11/r12：009 only-Stop与终局在途退出通过

r11会话 `c653637402014322bbe984169204ad36`，194事件零错误，完整终局Auto核对与摘要结束正常；gameover-entry及summary-completed两点实际NativeButtonDisabled=true/OnlyStopExits=true，Auto关闭/待播0/前进禁用/留场，13个owner任务完成、0取消故障后Stop恢复。用户确认主菜单禁用可见，未再用原生菜单退出。199文件归档/49已知文件补存，只有settings及backup变化。

r12会话 `440d7acce9854ac98598e173d66e59ae`，414事件零错误。两次一次性开关在phase=native-OnEnded-before-WinRun-kill-and-game-over经公开PlayerLoop.Control(stop)发请求；原生胜利已严格核对，WinRun强杀/gameover后续仍实际完成，再移除场景、各9个owner任务全完成/0取消故障后恢复原manager/所比较Continue/Progress。两次Complete=false/AllOperationsMatched=false为计划在最后选项结果返回前中断，不当整链失败或完整播放；这也是用户感到自动Stop“偏早”的明确设计位置。夹带一次重开同Boss原生Step到1后手动Stop，0个路线owner任务、原context恢复、无错误。该测试开关不定义普通Auto终点；关闭时仍沿D-159展开详情后留场，自动Stop并非产品默认自动退出。

证据 `runtime-verification-r11.json`、`runtime-verification-r12.json`、`collection-r11.json`、`collection-r12.json`；419第二会话归档和49已知文件相对r11全不变，root/片/胜利历史不变，Continue仍按先前通关正常不存在。game0、无restore/另一账号读取。此代表后段的实际胜利录制/观看/详情/only-Stop/在途退出已获证，继续本包死亡/放弃和真正新局整链，仍不能把准备前gap补为完整新局。

## 010：死亡末动作录制候选（待实际短测）

原生CreatureCmd.Kill在全体玩家死亡时调用LoseCombat登记pending loss，随后立即OnEnded(false)/ShowGameOverScreen；CombatManager稍后在安全点ProcessPendingLoss才发CombatEnded。原ActionMonitor只认胜利或下一可出牌回合，致死EndTurn因此没有可封存边界。010在真正OnEnded、原生历史/发现统计前冻结LiveState及RunResources，只有当前正在执行的记录动作、同CombatRoom、实际玩家已死才登记死亡投影；不以长动画结束、超时或预期录像内容推定死亡。

原ActionTask、EffectTask和派生任务必须成功；实际gameover已出现、CombatManager不再进行战斗、executor空闲后，该真实动作标记Defeat=true并自动封存。format10保留，新可选字段在false时省略，Battle.Completion=death；Codec拒绝死亡/胜利并存、死亡不在最后、存活HP或终局资源仍存活、旧格式携带死亡标记。WinRun本身随后强杀仍由原生上传标记和首次终局事实保护，不会改记死亡。保存退出/主动放弃不走此死亡封存路径。

开发开关“RUN-005 死亡录制短测（下一铁甲A0新局，首普通1HP）”默认关闭、下一符合条件新局消耗；正常涅奥后，仅首普通原生战前checkpoint写入前把实际HP设1，并写明确路线gap。沿用原首普通双蛞蝓准备，原卡组/资源和战斗播放不人工编排；从原生checkpoint之后验证末次EndTurn死亡，不作真实完整新局证据。NativePlaybackSupport目前明确阻止新死亡片观看，先获实际录制再接原生播放，不将完成录制误作死亡回放通过。

主体及DirectContracts Release最终0warnings/0errors；新direct checker最初Span.Contains用法编译失败，改IndexOf后通过，无生产逻辑调整。九项数据策略检查通过，原r6的13步format10胜利片生产reader/校验和与字节不变；仅合成死亡投影约束，不是原生死亡证据。证据 `checks/build010-death-recording-policy.json`、`checks/build010-old-native-clip.json`。新增verify-native-death供下一实际样本读取，尚未执行。

安装收据 `preparation/build-010-23ac6a40.json`：DLL `045FAF8B7A31A005889D591573919361828BACCC2BA9DF6CF00B044C1FB72172`，manifest `69185E5024D560BC5C9B81FC64CC2F732745BCFBA31E64F069CAEF6AAEABE8FB`。唯一mods/ReplayMod pair与封存010一致，无平铺重复；原009另存且hash核对。49已知文件安装前相对r12无变化、安装后仅settings.save变化，原生胜利历史hash不变、Continue/backup仍无；game0、无restore/另一账号读取。备份 `RUN005-pre-build010-current-20261103-190240` / `RUN005-post-build010-current-20261103-190311`，证据 `preparation/pre-build010-current.json`、`post-build010-current.json`、`post-build010-check.json`。

## r13：原生死亡录制通过，结算页模型问题未定因

会话 `be41c2d4929d494394397ef3e89e7052`，27事件、零observer/recording失败。Run `b77bd626b0614153a25adf0c598099eb`，root SHA `7260414C7FB78C96C76AB7242C0721F3656BBE6EF06EAFDD141311341C8C8CAB`，535,711bytes，2visit/2attempt、raw5。首普通前原80HP→1HP，原生checkpoint及实际可出牌初态均1HP；原始map3 missing/gap4是明确准备边界，合法战斗source5以后无缺口。Opening=null，不代表真正完整新局开场。

19:12:28.870真正OnEnded(false)登记death，随后冻结死亡投影、显示原生gameover；19:12:28.979自动封存clip `80f28133c9b44249ba764fc1d3e32ca4`，format10、1步EndTurn/Turn1、Completion=death、Defeat=true、Victory=false、Interrupted=false。终局资源与Lifecycle.State严格相同，BeforeOperation6指向raw battle5结束、visit2/attempt2；生产回读精确匹配。原生 `history/1793704325.run` win=false/was_abandoned=false，hash `CAC489165B6CD6F9EAC8F74CE754278861384034EC0BF84413DB0B0D4C8EEC94`，正常移除Continue/backup，用户正常结算返回菜单及退出。

用户确认看到铁甲倒下，但死亡结算页模型消失。原生Kill只对monster走RemoveCreatureNode；玩家StartDeathAnim(false)保留场景。NGameOverScreen.AfterOverlayOpened将现有CreatureNodes.Visuals转入结算_creatureContainer。010本轮新增封存/重置只改录制数据，不包含移动/隐藏角色的调用；现有Godot日志没有相关移层/节点异常，进程退出RID/resource泄漏信息不能当作已查明的模型根因。无实际节点显示日志/画面，不宣称原版问题或已修复。011下一次观看添加边界观测，不先补模型/重播动画掩盖问题。

证据 `runtime-verification-r13.json`、`checks/r13-death-recording.json`、`collection-r13.json`；归档32文件（含精确新history与日志），49已知文件相对010安装变化为本次真实死亡后的native replay、Progress及backup、settings及backup；无restore/另一账号读取，game0。原胜利历史保持。最新补存 `RUN005-post-r13-current-20261103-191451`；不是整账户无变化或死亡视觉/回放通过。

## 011：原生死亡观看与结算页显示观测候选

NativeEnd对死亡观看在同一ShouldSave=false、隔离SaveManager内核真正OnEnded(false)的RunResources与录制终局资源；路线还核所选最后battle及同visit/attempt原始来源的Lifecycle death事实。仍用原生EndTurn、攻击、死亡与gameover；BattleEnd冻结实际死亡投影，ActionMonitor等待原动作/派生任务成功、gameover已显示、combat结束及executor idle，NativeSeek严格核After/Victory/Defeat再落到EOF，不把死亡现场作为可恢复的普通checkpoint。

从致死动作开始至终局，Back/Seek暂禁；此前稳定点不变。原因是原生OnEnded及gameover转移模型后的副作用尚无恢复路径，不冒用普通战斗checkpoint。死亡路线EOF复用胜利的EndAtCurrent，Auto原生展开详情后关闭并留场；手动Step保留原生Continue入口。原生MainMenu/unlock出口仍禁用，只用Stop退出。独立战斗入口也沿相同原生死亡支持，结算任务在场景退出后、恢复原manager之前收束；没有写普通Progress/history/Continue来伪造终局。

GameOverVisuals仅在死亡页出现前、原生Visuals移层后、AnimateIn完成后采集当前Creature/Visuals/Body的实际父节点、可见性、alpha、位置/缩放及canvas坐标；不移动节点、不修改动画、不补造模型或逐帧序列化。正常录制及观看都可观察同原生交接；r13旧记录不能补造当时显示日志。当前模型根因/视觉结果仍Unknown，下一短测复用r13片。

主体及DirectContracts Release最终0warnings/0errors；最初只读观测将NCreature当Node2D导致编译拒绝，改为其共同CanvasItem后通过，无显示写入。九项死亡数据策略、实际死亡片/合法source5起点root、原r6胜利片生产reader均通过，原件校验保持；见 `checks/build011-death-policy.json`、`build011-actual-death-read.json`、`build011-actual-death-run.json`、`build011-old-victory-read.json`。尚未执行原生死亡观看/详情/Stop，不将Admission或数据PASS充作运行验证。

安装 `preparation/build-011-b933c506.json`：DLL `48E8DD06A917B6BE803EECE38435075D1E2F318F8DE2E56E180A47EC3526D7E2`、manifest `69185E5024D560BC5C9B81FC64CC2F732745BCFBA31E64F069CAEF6AAEABE8FB`。唯一mods/ReplayMod pair一致、无平铺重复，原010另存核hash；原胜利/新死亡history hash均不变、Continue/backup仍无。49已知文件安装前相对r13全不变，安装后仅settings.save变化，无restore/game0。补存 `RUN005-pre-build011-current-20261103-192755` / `RUN005-post-build011-current-20261103-192812`，安装前后收据/核对见 `preparation/*build011*.json`。

## r14：死亡原生观看/详情/Stop通过，模型移层后坐标失败

会话 `894e0b16689d491f87dca216164de8a1`，60事件，零observer/播放/退出失败。source5单步EndTurn原生死亡，OnEnded资源/RNG与Lifecycle及录制终局投影严格匹配。Auto到原生gameover后调用原生Continue一次展开详情，Auto关闭/待播0/前进禁用/留场；gameover-entry和summary-completed均NativeButtonDisabled=true、OnlyStopExits=true。Stop后4个owner任务完成、0取消故障，turn loop完成，恢复原manager/所比较Continue/Progress，Complete=true。只观察到这一次Auto/Stop，重开Step短测未获日志，不补造通过。

模型视觉失败有明确定位：同一Visuals节点始终Visible/VisibleInTree=true、alpha1、scale1、未Queued；原生Reparent到结算_creatureContainer后仍在(479.77255,740)，AnimateIn完成后却变成(-6.114194e-13,0)，Body随之到(5,-19)。不是节点被删、藏起或缩放。固定原生NCreature.AnimShake的_shakeTween持续一秒，将Visuals.Position写为Right*10*sin(4t)*sin(t/2)，本来只在NCreature局部空间有效；结算页仅Reparent Visuals、没有结束仍绑定旧NCreature的Tween。其终点t=2π的float结果-6.1141934e-13与日志X在1e-18内一致，Y恒0，形成精确写回指纹。

证据 `runtime-verification-r14.json`、`collection-r14.json`；65数据文件归档、49已知文件补存，只有settings及backup变化，root/clip/原生死亡history不变，game0、无restore/另一账号读取。最新 `RUN005-post-r14-current-20261103-202206`。本次证明死亡业务/EOF/退出代表链，视觉仍FAIL；没有因此宣称无模组原版视觉已实测。

## 012：结束移层前旧局部坐标Tween（待视觉复测）

在原生NGameOverScreen.MoveCreaturesToDifferentLayerAndDisableUi之前，对本次真实死亡所属Run、原生即将移走的CreatureNodes，仅Kill仍在运行的NCreature._shakeTween。原生AnimShake为void、没有业务等待者；结束它不取消致死动作/原生任务。保留此时模型姿态与全局坐标，由原方法执行原来的Reparent。没有重建模型、强设最终坐标、重放Spine动画、改血条淡出或延期gameover；正常录制和观看均处理相同交接。只读三处节点日志继续保留以核修复后实际位置，不在逐帧补写坐标。

主体Release0warnings/0errors，git diff检查通过。此增量只有两个显示/终局hook源码文件，无格式/资源/RNG/数据reader变化，未重复无关合同矩阵；原生视觉仍待用户复用同片观看，不能把构建当修复通过。

安装 `preparation/build-012-70c6b317.json`：DLL `A4C4C327519CB21AE787C1D95189D2756A1511978BF6B70F0944223CD57EDF79`、manifest `69185E5024D560BC5C9B81FC64CC2F732745BCFBA31E64F069CAEF6AAEABE8FB`。唯一mods/ReplayMod pair一致、无平铺重复，原011另存核hash，原胜利/死亡history均不变、Continue/backup仍无。49已知文件安装前相对r14全不变，安装后仅settings.save变化，game0、无restore/另一账号读取。备份 `RUN005-pre-build012-current-20261103-202404` / `RUN005-post-build012-current-20261103-202443`，核对见 `preparation/*build012*.json`。

## r15：012模型修复通过，缺原生History及结局控制修订

会话 `4a6b6b2dfdc54c85923e35aaf981a14d`（61事件）和 `ac39c65414a540cc84fbc2f7a7027fa5`（288事件），共六次原生单步EndTurn死亡，Auto两次、手动>或>>四次。每次动作投影/资源/RNG核对、Stop及原manager/所比较Continue/Progress恢复均通过；owner任务分别4/1/2/5/5/4个，0取消/故障。5次有完整AnimateIn结束观察的Visuals仍在约(479.7726,740)、Visible=true/alpha1，不再到零；另一次提前Stop不补造完整动画证据。用户确认模型正常。原root/clip校验不变。

用户发现手动死亡后点原生“继续”出现“试图修复漏洞”调试兜底文案，当前Godot日志5次“How did the game end??”。源码定位：OnEnded在ShouldSave=false时整块跳过CreateRunHistoryEntry，观看中RunManager.History仍null；NGameOverScreen._Ready回退new RunHistory{Win=false}，没有KilledByEncounter/Event，NRunHistory.GetGameOverType返回None，GetDeathQuote因此取MAP_POINT_HISTORY.debug。这是缺实际终局摘要，不应换自定义文案掩盖。

同时用户明确修订：观看时死亡后的原生“继续”不能直接点击，必须作为>/>>/Back控制的动作；Auto沿同一路径。此优先于先前D159的手动直接Continue许可，已交PM并获规格同步；只Stop退出保持。013按此实现，原r15手动Continue并非伪称已符合新规则。

证据 `runtime-verification-r15.json`、`collection-r15.json`，354数据文件归档/49已知文件补存，仅settings及backup变化；原死亡Progress/history及root/clip不变，game0、无restore/另一账号读取。最新 `RUN005-post-r15-current-20261103-203402`。模型/原生业务通过不代替缺文案及新控制的通过。

## 013：原生实际History与受控结局翻页（待手测）

OnEnded后置hook只在已核真实终局、ShouldSave=false且全局SaveManager严格等于本次IsolatedManager时，用本次原生返回的SerializableRun调用原生RunHistoryUtilities.CreateRunHistoryEntry。既有MemorySession由MockGodotFileIo同时作为FileIo/SaveStore；该工厂只在内存写history并设置RunManager.History，未启用原生统计/成就/指标上传/删除普通Continue的外层分支。核胜负/放弃标记与实际死亡Encounter，记录原生GameOverType。没有读录制后的history文件/expected补演，没有自己拼死因/徽章或改本地化文案。首次终局flag保护WinRun后续强杀，不重复建死亡History；正常录制继续原来ShouldSave=true流程。

EndingPlayback提供信息0→详情1的观看动作节点：手动>/>>调用原生OpenSummaryScreen一次，Auto也调用同一handler；详情完成后Auto关闭并留场。原生Continue与MainMenu均Disable，Continue方法另有只允许本次播放器动作进入的prefix，鼠标/键盘不能绕过；Stop保持原入口。结局动作是对当前实际原生UI的观看导航，不往旧r13文件中伪造当时未记录的点击事实、不改录像格式/校验。

Back在详情原生任务完成后可用：把原生移进overlay的现有CreatureVisuals按当前全局变换交回原Creature，然后通过NOverlayStack.Remove释放旧结算页，用同一RunState及本次实际OnEnded返回DTO重新调用原生ShowGameOverScreen；不重复OnEnded/建History/致死动作、不加载战斗checkpoint、不复活战斗。回退前后严格核实际RunResources/RNG不变。原生信息页入场/装饰仍由工厂执行；新翻页的在途Back暂不开放，普通战斗终局恢复仍未支持。正式进度条/管理入口不在此改动。

主体Release最终0warnings/0errors（新Back显示检查的nullable编译错误已通过显式节点owner检查修正），git diff检查通过；无数据格式变化，未重复无关合同。新原生History工厂、手动翻页/Back/Auto和账户恢复仍未手测，不把编译当运行通过；后续须核实际NativeGameOverType=CombatDeath与原死亡history物理hash不变。

安装 `preparation/build-013-1de19212.json`：DLL `7BBF061A5668779C5FA6669727B7ADBD8166C01923A3A668B0F2E34739B694D5`、manifest `69185E5024D560BC5C9B81FC64CC2F732745BCFBA31E64F069CAEF6AAEABE8FB`。唯一mods/ReplayMod pair一致，无平铺重复；012另存核hash，原胜利/死亡history均不变、Continue/backup仍无。49已知文件安装前相对r15全不变，安装后仅settings.save变化，game0、无restore/另一账号读取。备份 `RUN005-pre-build013-current-20261103-205315` / `RUN005-post-build013-current-20261103-205451`，收据/核对见 `preparation/*build013*.json`。

## r16：D-166死亡说明/详情节点与隔离History通过

会话 `d4e0394322dd477ea786f8852fc6050b`，160事件零失败。首轮手动两次>及一次>>调用原生继续、两次Back回信息页；每次Back保持同RunState、严格资源/RNG不变、不重复OnEnded/致死或重播战斗。第二轮Auto同路径展开详情、Auto关闭留场。两个原生OnEnded分别只生成一次实际隔离History，NativeGameOverType=CombatDeath、KilledByEncounter=CorpseSlugsWeak；4次gameover显示含两次UI Back重建，不混为4次死亡。

用户确认原生Continue不可直接操作、翻页正常、调试文案消失；原生信息就绪/详情完成等节点继续锁定Continue/Menu。两次Stop分别13/4个owner任务完成、0取消故障，原manager与所比较Continue/Progress恢复，原生胜利/死亡history物理hash及root/clip严格保持。Godot缺死因“How did the game end??”告警为0。该最短死亡录制/原生观看/模型/结算控制/退出代表链收束，不外推全部死亡机制或从死亡恢复战斗。

证据 `runtime-verification-r16.json`、`collection-r16.json`：165数据文件归档、49已知文件补存，只有settings及backup变化，无restore/另一账号读取，game0。最新 `RUN005-post-r16-current-20261103-225326`。已向PM作死亡分段handoff；本包继续主动放弃及真正新局三幕，不等待UI分线或新包。

## 014：原生确认主动放弃候选（待首个实际样本）

RunAbandon只在实际NAbandonRunConfirmPopup确认回调触发RunManager.Abandon时，从已完成、无在途动作/选择的直接战斗封存原完成前缀（Completion=interrupted），再新建abandon操作；不把force-kill当新的手动卡牌或EndTurn。等待实际AbandonInternal task结束及原生gameover，Terminal=abandoned/After取同次OnEnded(false)前冻结的真实RunResources，Lifecycle仍据IsAbandoned区别死亡/保存退出。数据reader必须同时有native-confirm-popup标记、直接战斗visit和同原始位置/尝试的abandoned事实/相同After，否则拒绝。缺稳定前缀/事件嵌套等场合明确gap，不伪造成功。

播放从原战前checkpoint重放该完成前缀，再经原生确认popup与OnYesButtonPressed调用同RunManager.Abandon/AbandonInternal；精细>可停确认窗，整项>>/Auto仍沿同一原生确认路径。原生IsAbandoned、终局资源/RNG/Lifecycle严格核对，ShouldSave=false及同隔离manager约束保持；原生History工厂按实际abandoned生成，既有D-166结局0↔1及Stop复用，不用预期状态补写结果。在确认窗Stop走原生取消回调清理modal；这些播放/退出路径尚未实际运行。

默认关闭“RUN-005 主动放弃短测（下一铁甲A0新局）”，沿既有新局准备只把首普通改双蛞蝓、原牌组和生命不另改。正常涅奥后打一张防御、待动作完成，再暂停菜单放弃并确认；初次只录/读取，之后复用文件观看。准备局Opening=null，不能替代真正完整新局证据。死亡准备的旧过期“未开放观看”提示一并改正。

主体及DirectContracts Release均0warnings/0errors；15项run-envelope含新放弃约束及错误kind/位置/结果/输入/房型拒绝，原胜利source45和死亡source5实际root reader/原件保持通过。证据 `checks/build014-run-envelope.json`、`build014-old-victory-run.json`、`build014-old-death-run.json`；仅数据机制，原生放弃录制/观看/物理history与Progress保持均待后续取证，不提前PASS。

安装 `preparation/build-014-9aab21c3.json`：DLL `5EF89002446D5E464717FC8E8A2F87845C3805871332274FE75FB6FAC1C9760B`、manifest `69185E5024D560BC5C9B81FC64CC2F732745BCFBA31E64F069CAEF6AAEABE8FB`。唯一mods/ReplayMod pair一致、无平铺重复，013另存核hash，既有胜利/死亡history不变、Continue/backup仍无。49已知文件安装前相对r16全不变，安装后仅settings.save变化，game0、无restore/另一账号读取。备份 `RUN005-pre-build014-current-20261103-230838` / `RUN005-post-build014-current-20261103-231006`，核对见 `preparation/*build014*.json`。

## r17：原生主动放弃录制与生产读回通过

会话 `e8b5cff299184972ac11c3665be9e63d`，29事件、零observer/录制失败。Run `b5221ca5d465457ab90e1f93231fbfec`，raw5、2visit/2attempt、Opening=null；source4完成战斗前缀clip `0ae0a298ed894a08ba328e7c426ffd5c` 为format10、单步未升级DefendIronclad、Completion=interrupted、Victory/Defeat均false。原生确认触发source5 abandon，Argument.Option=native-confirm-popup、Status=complete、Terminal=abandoned；After与同visit2/attempt2、BeforeOperation6的实际native-Abandon-OnEnded(false)事实State精确相同。

原生gameover kind=abandoned、operation5，原生任务返回后操作完成。真实history/1793720545.run win=false/was_abandoned=true、KilledByEncounter=CorpseSlugsWeak，SHA `135E09C1BACAC77C586DF6F55108AC25781E30056E7317424EAF5CE4D0F8E84C`；正常移除Continue/backup，用户正常结算返回菜单/退出。新root/source4及实际前缀分别通过生产reader，原件不变；不把interrupted前缀当胜利或整局中断，整局明确主动放弃。

证据 `runtime-verification-r17.json`、`checks/r17-abandon-run.json`、`checks/r17-fragment.json`、`collection-r17.json`；34数据文件归档/49已知文件补存。本次真实新局放弃按原生更新native replay、Progress及backup、settings及backup，未自动还原；无其他账号读取/game0。最新 `RUN005-post-r17-current-20261103-234414`。记录/读取通过，原生放弃观看/确认窗Stop尚未执行。

## 015：放弃确认窗输入归播放器控制（待观看）

补审新放弃popup的Yes/No回调：观看时只允许RunPlayback.Driving下的原生调用，直接鼠标/键盘回调不能越过>／>>的确认阶段；正常游戏仍原逻辑。播放器确认与Stop finally中的原生取消均在该授权范围内。既有只读鼠标遮罩之外增加方法入口检查，避免键盘/原生回调绕过播放节点；没有修改记录中的选择事实。

显式兼容14实际生产来源hash，root与clip同规则；新Schema/战斗动作未改。主体Release0warnings/0errors，git diff检查通过；r17实际新文件reader结果保持，不重复无关合同。放弃的原生任务/隔离History/确认窗Stop/结算控制与账户保持均待用户复用该片观看，不提前PASS。

安装 `preparation/build-015-62ba4b6c.json`：DLL `CD1FCEB61AC112E5FB509DDA11C621EB91F157EC08966860BC5C3814D688CF5C`、manifest `69185E5024D560BC5C9B81FC64CC2F732745BCFBA31E64F069CAEF6AAEABE8FB`。唯一mods/ReplayMod pair一致、无平铺重复，014另存核hash，旧胜利/死亡及新放弃三份history物理hash均保持、Continue/backup仍无。49已知文件安装前相对r17全不变，安装后仅settings.save变化，game0、无restore/另一账号读取。备份 `RUN005-pre-build015-current-20261103-234854` / `RUN005-post-build015-current-20261103-235107`，核对见 `preparation/*build015*.json`。

## 后续必须完成

- 当前后段原生观看及第三幕真实Continue分别由r4/r2证明；新增终局/换幕在途Stop可与后续样本合并。
- 最后Boss→TheArchitect/WinRun录制/核心观看/摘要/only-Stop/终局在途Stop已有r6/r8/r9/r10/r11/r12，所测后段结束；正式返回来源UI继续另线。
- 最短死亡与主动放弃样本：分类、最后真实动作/落盘、合理观看EOF/Stop，保存退出不当放弃。
- 真实新局合法起点至三幕原生终局同份完整录制/连续观看，准备若存在必须在新局实际输入处明确可重现；当前后段/旧准备局Opening=null不能替代此证据。
