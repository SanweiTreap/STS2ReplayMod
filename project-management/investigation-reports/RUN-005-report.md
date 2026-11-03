# RUN-005：完整三幕录制与结局闭环

## 当前摘要（2026-11-03）

- 本包进行中，保留 **build-002**，源码 `ff4919c`。**r4后段原生观看通过两次完整链**：第二幕Boss19步→奖励→第二次换幕→第三幕Nonupeipe/FurCoat→ScrollsOfBitingWeak5步胜利；5次Back恢复（含第二次Boss选择前2次）均匹配，零报错，三次Stop/重开一步退出恢复正常。r3/001遗漏CARD.SLOTH发现项的FAIL保留，002发现门控同步回退已有原生证据。r1/r2录制及第三幕重启Continue保持；完整新局→三幕→原生终局同份证据仍待，RUN-004原结果保持。
- 录制入口、format10场景身份、LiveState输入/恢复、共用换幕投票与第三幕Ancient分派接到Act0/1/2；保持同Run/ActModel/visit/attempt、自然资源/RNG和既有自算保存点恢复。第一/二次换幕共用原生EnterNextAct路径；第三幕Boss后的终局入口仍待接，不能只删白名单后说完整通关。
- 新增可选Lifecycle事实：r1实际Continue与saved-exit正确登记，未误判放弃；无字段旧片保持未知。真正OnEnded胜利/死亡/主动放弃的登记基础已实现、尚未动态验证；死亡末尾动作/死亡观看、最终事件/胜利观看仍待实现。WinRun后的强杀不得覆盖胜利。
- 默认关闭后段准备开关：下一次Continue当前第二幕普通保存点，在原生生成地图后、后续Boss checkpoint前将当前节点直连原生Boss；不改资源/RNG/敌人，明确写入gap并标记后段片。只从后续Boss合法存档起点验证，准备前旧记录不冒充连续整局；未来完整新局样本必须另录。
- 原生已查：第三幕最后Boss领奖后经ActChangeSynchronizer/EnterNextAct进入TheArchitect，实际选项/演出最终调用WinRun。TheArchitect为自定义Combat事件布局，尚未接入普通事件播放器。OnEnded真正落统计/历史/删Continue，主动Abandon先置IsAbandoned再强杀，普通保存退出只ReturnToMainMenu；不按末击/CleanUp/角色死亡猜胜利。
- 主体/DirectContracts Release均0warnings/0errors；12项run-envelope、15项library、13项branch合同通过，含第三幕metadata及生命周期胜利防覆盖/存退可续/死亡与放弃区分/旧字段省略；旧RUN-004 root/第三场clip生产读取通过且原件不变。均非新增原生运行通过。
- 仍受有界文件限制：run64MiB（原16）、clip16MiB（原4），访问256/路线操作2048/单战动作256保持；支持三幕增长余量，未做长局增长/性能实测，不加逐帧文件读取/全局序列化。新事实只在实际生命周期点落盘。
- 当前实读同Run `2b0f7e321a3a4dcd9caf9ee83435a06f`、种子KTRJB2SKRSYV，native精确point20：Act2/ACT.GLORY第三幕首普通**完成未领奖**。原始root36项、selected33项、8visit/12attempt及三片不变；最终路线用新胜利source36，不含旧一步source35。Lifecycle仍为continued→saved-exit。game0，最新补存 `RUN005-post-r4-current-20261103-123446`，1089数据文件/51已知文件、零缺失/无restore；仅settings.save及backup变化，另一账户未读。
- 下一步：接第三幕最终Boss之后的TheArchitect原生对话/演出/WinRun及死亡末尾动作与观看收尾，再安排最短终局样本；已通过后段不重跑。正式UI/播放模式及D-147暗亮隐藏继续不实现。

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

## 后续必须完成

- 当前后段原生观看及第三幕真实Continue分别由r4/r2证明；新增终局/换幕在途Stop可与后续样本合并。
- 最后Boss之后TheArchitect实际布局/对话/动作与真正WinRun闭环，终局统计/退出在隔离manager/store内收束，不能把胜利后的强杀误判死亡。
- 最短死亡与主动放弃样本：分类、最后真实动作/落盘、合理观看EOF/Stop，保存退出不当放弃。
- 真实新局合法起点至三幕原生终局同份完整录制/连续观看，准备若存在必须在新局实际输入处明确可重现；当前后段/旧准备局Opening=null不能替代此证据。
