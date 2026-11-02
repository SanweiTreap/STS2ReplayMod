# RUN-005：完整三幕录制与结局闭环

## 当前摘要（2026-11-03）

- 本包已开始实现，**build-001 首个后段增量待安装**，尚无RUN-005原生运行结论；RUN-004/004原代表验收保持。完整目标仍需同一真实新局→三幕→原生终局的一份录制与连续观看，不把本次第二幕后段准备拼成完整整局。
- 录制入口、format10场景身份、LiveState输入/恢复、共用换幕投票与第三幕Ancient分派接到Act0/1/2；保持同Run/ActModel/visit/attempt、自然资源/RNG和既有自算保存点恢复。第一/二次换幕共用原生EnterNextAct路径；第三幕Boss后的终局入口仍待接，不能只删白名单后说完整通关。
- 新增可选Lifecycle事实：原生新局/Continue、保存退出（可续）、中断、真正OnEnded胜利/死亡/主动放弃。WinRun先OnEnded(true)后强制杀玩家，再次OnEnded(false)不覆盖胜利；无字段旧片保持未知，不补胜负。当前仅登记原生结局事实基础，死亡末尾动作/死亡观看、终局事件/胜利观看尚未实现和动态验证。
- 默认关闭后段准备开关：下一次Continue当前第二幕普通保存点，在原生生成地图后、后续Boss checkpoint前将当前节点直连原生Boss；不改资源/RNG/敌人，明确写入gap并标记后段片。只从后续Boss合法存档起点验证，准备前旧记录不冒充连续整局；未来完整新局样本必须另录。
- 原生已查：第三幕最后Boss领奖后经ActChangeSynchronizer/EnterNextAct进入TheArchitect，实际选项/演出最终调用WinRun。TheArchitect为自定义Combat事件布局，尚未接入普通事件播放器。OnEnded真正落统计/历史/删Continue，主动Abandon先置IsAbandoned再强杀，普通保存退出只ReturnToMainMenu；不按末击/CleanUp/角色死亡猜胜利。
- 主体/DirectContracts Release均0warnings/0errors；12项run-envelope、15项library、13项branch合同通过，含第三幕metadata及生命周期胜利防覆盖/存退可续/死亡与放弃区分/旧字段省略；旧RUN-004 root/第三场clip生产读取通过且原件不变。均非新增原生运行通过。
- 仍受有界文件限制：run64MiB（原16）、clip16MiB（原4），访问256/路线操作2048/单战动作256保持；支持三幕增长余量，未做长局增长/性能实测，不加逐帧文件读取/全局序列化。新事实只在实际生命周期点落盘。
- 用户本轮确认Cloud关闭、无新进度；实读当前仍KTRJB2SKRSYV、ACT.HIVE/ExoskeletonsWeak胜利未领奖、point12，game0。补存 `RUN005-pre-build001-current-20261103-013141`，51文件/4,169,988bytes，较RUN004/r7零变化/缺失，全部hash读回，无restore/另一账户读取。
- 下一步：装001后用户启用后段准备→Continue当前第二幕完成房间→领奖/原生Boss战胜利→正常奖励/第二次换幕→第三幕古神→首普通打一张防御后存退；先核后段增量与第三幕真正Continue，再接后续终局。正式UI由PM讨论，当前“回放”明确只是开发临时入口；D-147两次SL暗亮仍是待改呈现偏差，本包不隐藏过场/拆模式。

## 首候选实现与边界

RunTailPreparation只在显式Armed、非观看、已启用录制、当前Act1已访问普通节点时生效；在原生NMapScreen.SetMap之前改本次地图连接，下一Boss前存档自然包含准备地图。原RunRecording保留gap事实；没有迁移到新Run、删除旧操作、重设种子/RNG、改Boss内容或在观看中补资源。Marker归属当前RunState，后续战前片据实标记；跨进程后不伪造旧准备输入。

RunEndings在真正OnEnded首次调用前冻结实际RunResources并按isVictory/IsAbandoned分类，原生已上传标记和本会话首次终局事实保护后续强杀/清理。RunRecording在普通暂停菜单CloseToMenu登记存退意图、实际Cleanup收尾为可续saved-exit；没有凭退出新造成功SaveRun位置，Continue仍只依原有实际成功存盘证据。此候选未声称死亡录像已有最终动作/可播，也没有实现最终自定义事件。

新RunDocument.Lifecycle为可选省略null字段；旧记录字节/校验保持，旧没有结局字段显示“未知”并保留范围/缺口。运行事实含visit/act/BeforeOperation/attempt与原生来源，不给新主菜单/正式UI设计结论。第三幕与前两幕继续用相同本体战斗、native queue/选择/Back、EnterNextAct与Ancient路径；所选新敌人/古神和第二次换幕需人工动态验证。

## 后续必须完成

- 当前后段真实录制/读取与原生观看、第三幕实际保存点真正重启Continue/续录；新增终局/换幕Stop可与此样本合并。
- 最后Boss之后TheArchitect实际布局/对话/动作与真正WinRun闭环，终局统计/退出在隔离manager/store内收束，不能把胜利后的强杀误判死亡。
- 最短死亡与主动放弃样本：分类、最后真实动作/落盘、合理观看EOF/Stop，保存退出不当放弃。
- 真实新局合法起点至三幕原生终局同份完整录制/连续观看，准备若存在必须在新局实际输入处明确可重现；当前后段/旧准备局Opening=null不能替代此证据。
