# RUN-005：完整三幕录制与结局闭环

## 当前摘要（2026-11-08）

- **074 整局路线实机通过。** 会话 `c1e47f732f144b39b79aea705b877643` 从原生历史进入 `e7138bcd`，从涅奥播放 235 项到原生胜利详情，Auto 在详情关闭，从原生菜单退出返回同一历史。原上下文与进度已恢复。期间 60 次手动 Back 均匹配状态与 RNG，另有手动前进和 Auto 切换；全程连通已验证，纯 Auto 无输入仍未单独取证。证据 `checks/build-075/build-074-full-route-runtime.json`。
- **076 实验体二阶段听感实机通过。** 会话 `3e001b27e814409292ab26540d669cac` 直接 Auto 播过第 15 步，原生击倒到三头复活相隔约 1.88 秒，等待自然结束。Back 到 14/24 后重做第 15 步也触发同一个原生击倒声。用户确认听感恢复，9 次 Back 的状态和 RNG 均匹配，之后播至胜利详情并正常退出，原进度一致，日志写入失败为 0。实机证据 `checks/build-076/test-subject-runtime.json`。
- 075 首播的第 15 步击倒到三头复活相隔约 0.97 秒。固定版原生音频库离线混音确认两种节奏都能完整播放 1783 毫秒的击倒样本，原日志节奏的配乐停在 B，留出约 1.8 秒后可进入 B Hold／C。076 在原生复活前合并当前击倒声结束与已有的原生敌方演出等待，Back／Stop 接共用收束。构建、五组契约及实验体录像和整局尾段生产读取通过。离线证据 `checks/build-076/test-subject-knockout-fmod.json`，安装回执 `preparation/build-076-203ceb42.json`。
- **075 其余尾段修订已实机试播。** 实验体第 24 步的攻击声和最终死亡声在 074 启动后约 21 毫秒被正常战后交接清理，075 两次胜利交接均保留了这两个活动 one-shot。第 18 步能力药在下一项正要打出药水加入手中的那张牌时等原生手牌位置和命中区。终局信息／详情之间统一关闭 Back。用户反馈基本修复，明确余留第 15 步的二阶段击倒。075 回执 `preparation/build-075-f8004ac4.json`。
- **064 实验体一阶段 Back 已通过。** 会话 `f73d442a763b4baaafcab91079cd02a5` 核对运行 064。第 6 项岿然不动和第 7 项巩固共 16 次在途 Back 为 48–74 ms，其中旧动作收束 10–16 ms；063 对应七次为 477–556 ms。正放重做后激怒加力量分别为 2→4 和 4→6，35 次 Back 均匹配状态与 RNG，Stop 恢复原环境，日志零写入失败并正常退出。064 将 `EnragePower.AfterCardPlayed` 的半秒纯等待接入共用动作计时收束，原生加力量保持原序。编译、四组契约、固定程序集绑定和分场／尾段生产读取通过。安装回执 `build-064-9b6028c1`，安装前后 47 个当前文件一致。
- **063 能力药和遗物计数已实测通过。** 同一会话里，能力药四次在途 Back 为 71–105 ms，均命中小选择防误触的回退处理，随后第 18 项完整重做正常。音叉和钟摆计数由回退前的实际状态恢复，用户确认计数显示正常。27 次 Back 均核对状态和 RNG，Stop 正常恢复原环境。
- **062 建筑师尾段已实测通过。** 会话 `425b6f036bdb4973b016275bfccf50fe` 从 `e7138bcd` 第 3 幕第 15 层／尝试 2（原始 318／最终路线 229）到实验体第 24 项胜利，原生进入建筑师，匹配迅捷药水、能力药水两次丢弃和后续三项对话。原生胜利、详情、Stop 与原环境恢复均通过。离线证据 `checks/build-062-architect-tail-preflight.json`，从涅奥到胜利详情的完整 Auto 仍待验收。
- **063 已安装，先回归能力药及遗物计数。** 原第 18 项能力药的三次在途 Back 为 426–431 ms，收束占 389–393 ms，主要在原生小选牌防误触等待。现在只在 Back／Seek 收束期让已就绪窗口直接接受录像选择，仍走原生 `SelectHolder` 和完成流程。音叉在第 17 项实际计数为 3，062 回退却恢复临时显示的 10。063 恢复时只清除暂时显示触发阈值的标志，阻止旧计时器写入新画面，保留正常切房时计时器收尾和原生效果。按相同字段及异步结构验证了固定版 21 种遗物。构建、四组契约、固定版程序集探针、分场及整局尾段读取通过。安装回执 `build-063-3075723d`，DLL `B2B50B97F86428887960AE5BD7DF3106404E2F23518BB3D31931047B85695963`，备份并比较 47 个当前文件一致，未启动游戏。实机先用同一实验体锚点测 `18→Back 17` 的遮罩和音叉 3，再重做第 18 项确认选择正常。

- **053/r70 千足虫回退及重做通过，整局观看推进至建筑师入口**。进入建筑师后因首项是丢药而被入口校验拒绝。**054/r71** 已安装修复，待第三幕末 Boss 起点短测两次丢药及胜利详情，再跑开局到结局。逐怪跨回合 Back、残留动画／音效及战后 BGM 已另交 PM 规划。052/r67 完整录制通过，单人 Standard 铁甲 A0 `e7138bcd` 最终延续路线 235 项，无录制缺口或资源断裂，原生胜利历史已核并归档。
- 当前 **047/r57** 雕像苏醒后 Back 重播的音乐和音效通过实机验证。**046/r56** 头槌斜线修复通过。**045/r55** 精英战 42 次恢复状态／RNG 全部核对，最慢约 118 ms，战后连续观看至当时录像终点通过。**044/r53–r54** 鲜血药水在第 9 层尝试 3 的胜利奖励页录制和原生观看通过，HP 从 60 恢复到 76。**043** 跨回合预排修复待触发。**042/r52** 第 7 层精英旧缺口保留，**042/r51** 史莱姆头顶遗物退场通过。无缺口三幕整链仍待验证。
- **025/r32** 通过机械骑士胜利 Back/重做和首项领奖。**024/r31** 第三幕11层起连续回放至原生胜利详情通过；第二次观看首次发现差异保留。025 胜利 Back 恢复隔离战斗统计与纪元，原版重算首次发现并通过领奖校验。
- **024/r30** 通过实验体 Back 的音乐恢复、复生收束与洗牌清理。第9/13步边界的15次回退约39–149ms，状态/RNG匹配。余留听感差异是首次正放第10步历史课击倒声；已见未静音的原生调用，尚无无模组对照。
- **019/r24** 同一真实新局 `b85d941b` 录到原生整局胜利，实际历史 win=true、末动作和 Lifecycle 一致；41.5 MB 最终文件正常收束。原先缺口保留，新增第三幕第7层事件自定义领奖缺口。
- **017/r20–r21** 通过新局至首胜录播；**017/r22** 续至二幕，发现灯火钥匙战斗事件与石中剑换玉剑缺口。**019/r23** 后台续录与存退、用户卡顿反馈通过。本次整局不能作为无缺口三幕样本。
- **016/r19** 已通过主动放弃代表链和遮罩回归。三次确认触发真实原生放弃、资源/RNG 精确核对与隔离 `AbandonedRun`；另一次在确认窗 Stop，只清理弹窗和遮罩，无终局或历史构建。用户确认两种关闭均无残留。此前 **015/r18** 的视觉失败保留。
- **025/r33** 代表新局 `06105b5f` 到第一幕 Boss 奖励页存退。涅奥之骨、Whispering Hollow 事件内领奖，以及第11层战后丢药共三处实际缺口。Boss 片和第14层起后段可读，完整首幕未过。当前原生 point27 已备份。下一步先补事件附属领奖和战后丢弃的短路径，避免用又一整幕验证已知缺口。

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

## 017：完整三幕样本的可重现起点

新局 Ironclad A0 三幕可选一次 `ironclad-block-deck-v1`，沿 014 已验证尾段使用的同一套壁垒+、两张不动如山+、巩固+、两张全身撞击+（可附魔牌先天保留），另加 Bag of Preparation、Lantern。复用原生牌注册、遗物归属与抽取池移除，发生在进入第一房间前。开局输入保留原生种子、幕模型、账户输入与明确配方，回放由原生新局工厂重建；没有后幕读档或追加局内准备。新字段 null 时省略，旧录制保持兼容。主体与 direct Release 0警告/0错误，run-envelope 通过。安装收据 `evidence/run-005/preparation/build-017-2db74ff2.json`，安装后 `RUN005-post-build017-current-20261104-140208`、仅 settings.save 变化。开局准备和实际三幕覆盖均待人工验证。

## r20：三幕新局录至首胜存退（017）

会话 `046662492ff644f2a2109f1ff699654e`，37事件。新 Run `b85d941b40764b27b65b0a39dfe8e206`，种子 VT7BECB46PV6，暗港／蜂巢／荣耀。实际新局在首房前应用牌组和遗物配方，涅奥选 PHIAL_HOLSTER 并正常离开，经地图到 SludgeSpinnerWeak；4项路线操作均完成、2个 visit 无 gap。独立三步胜利片 `29157d445aae4a26a3da22a7f1e6be46`。root/clip 生产读取通过，实际从涅奥观看仍待。

真实存退标记 `saved-exit`，原生磁盘 fingerprint 精确命中 point4（PreFinished=true，Path=[1,2,3,4]），菜单读取时观测到的归一化 SaveKey 同点一致。正常退出；三份历史原样。收据 `evidence/run-005/runtime-verification-r20.json`、`collection-r20.json`、`checks/r20-new-run.json`、`checks/r20-first-clip.json`。最新补备份 `RUN005-post-r20-current-20261104-152551`，保留新 Continue 及备份和实际账户进度。

## r21：新局原生回放至首胜（017）

会话 `f5f88b2be92c44209664a45554413021`，78事件。打开 `b85d941b` 的涅奥起点，隔离 manager 以原生新局工厂创建并应用记录的配方，未加载 checkpoint。涅奥选择、离开、地图三项资源和 RNG 匹配；SludgeSpinnerWeak 三个战斗动作及真实胜利投影均通过，Auto 关闭。Stop 收束9个路线任务，零失败或取消，恢复原上下文后游戏正常退出。49个已知文件只改 settings.save.backup；原 Continue/Progress、录像与三份真实历史核对一致。证据 `evidence/run-005/runtime-verification-r21.json`、`collection-r21.json`，最新备份 `RUN005-post-r21-current-20261104-152917`。这次验证范围截至首战，下一步同局续录。

## r22：真实新局续录至二幕，发现两个缺口和长局写入卡顿

会话 `77bd6ed5f2964051a58ea06b91b342a5`，858事件。同局 Continue 命中 point4，新 attempt3，原生经过暗港 Boss 和帕尔。二幕第7层 visit24 `THE_LANTERN_KEY` 的 Combat 布局与不返回父事件的内部战斗及任务牌奖励触发现有支持边界，raw108–110 为显式 gap。二幕第12层 visit29 `INFESTED_PRISMS_ELITE` 的胜利 hook 将五计数的 `SwordOfStone` 原生替换为 `SwordOfJade`；原 `RelicMembership` 在战斗观察中阻断录像，前5个完整动作之后的胜利片未封存，raw136 pending。真实当前存盘精确匹配 point51，PreFinished=true；其边界早于后续领取开心小花。保持原存档及原 139 项路线、17 个封存片，不改写缺口。证据 `collection-r22.json`，安装前 `RUN005-pre-build018-current-20261104-154449` 补存当前 Continue 和备份，019 安装后两份存盘仍在。

用户补充的原生进度变化已定点核对：愧疚在 `AfterCombatEnd` 第五场离开永久牌组；石中剑在第五场精英胜利经 `RelicCmd.Replace` 替换；玩具盒每三场战斗经 `RelicCmd.Melt` 熔化一个蜡制遗物，遗物仍在列表但状态变化；寻龙尺给出的寻龙牌在 `BeforeRoomEntered` 第五个问号触发变成富足；灯火钥匙在后续历史学家事件消费并可换历史课。分别覆盖原生战斗结算、入房和事件边界。尚未逐项实测，保留遗物 guard，后续应按原生操作归属补录播和跨变化 Back。

录制文件到 raw139 为 22,059,141 字节，旧 `RunRecording.Save` 在主线程每次验证、两次序列化并整文件重写。隔离复制上的 writer 检查实测旧同步保存119.8ms，新快照排队最高1.5ms，后台最终写入146.6ms，12个紧邻提交合并到最新一份并由生产 reader 校验。该数字不是游戏帧率。018 首安装未人工测，019 增加正常进程退出时的后台写入收束，构建0警告/0错误、旧017文件来源显式兼容；新失败的未封存战斗标记 missing、日志列出实际遗物和操作。019 收据 `evidence/run-005/preparation/build-019-056280ec.json`，性能检查 `evidence/run-005/checks/r22-writer-benchmark.json`，最新备份 `RUN005-post-build019-current-20261104-154607`。强退仍可能丢失最后一份未提交快照，旧文件保留为最近一次原子提交。

## r23：大录像后台续录与第二次换幕（019）

会话 `4397cb63f033498bb477894630341a25`，143事件。Continue 恢复同局 point51、attempt31，封存二幕精英六步、Boss十步胜利；正常换幕、选择荣耀古神 NONUPEIPE、进 DevotedSculptorWeak。首战两步片为 interrupted，真实存档匹配 point61、PreFinished=false、所选路径止于 source166；重进该场开头。旧两处缺口保留，本轮无新 gap 或录制错误。

用户确认继续走了几层已经不卡。退出前 84 个整局快照请求合并为 78 次写入，后台单次最大 152.3 ms，最终整局 27,687,112 bytes／167 原始操作通过生产读取，source160 至第三幕开场的后段和首战中断片检查通过；游戏正常退出，三份历史未变。证据 `runtime-verification-r23.json`、`collection-r23.json`、`checks/r23-later-chain.json`，最新备份 `RUN005-post-r23-current-20261104-155814`。后段的实际观看和最终胜利仍待。

额外回合补查：r23 第三幕首战片 `35228c92` 的第1步原生 EndTurn 触发佩尔眼，`Player.TurnNumber` 1→2、`RoundNumber` 仍为1、Side 保持 Player，七张手牌进入消耗堆，佩尔眼 Active→Normal；第2步在该回合出壁垒。录制已取证。通用 EndTurn 等下一次原生玩家 StartTurn 和玩家回合号，不要求敌方回合。龙涎香的原生 AmbergrisPower 接同一额外回合机制，未在 r22/r23 封存战斗片观测到饮用触发。两种来源的原生回放与 Back/Step 均待实测。

## r24：同一真实新局录至原生整局胜利（019）

会话 `fb5b03ab66d544deb2f329fbcee6eedb`，444事件。先原生 Continue 匹配 point61，完成第三幕；在最终 Boss 战中保存退出，第二次 Continue 精确匹配战前 point80，旧中断片 raw225 保留并从最终所选路径排除。重打 Boss 完成 raw226 的 19 步胜利片，随后原生进入最终事件，原生 OnEnded(true) 与 raw230 `PROCEED` 的终局及资源一致。真实 history `1793777059.run` 胜利、未放弃，原生 Continue 及备份在结算后正常删除，之前三份 history 不变。最终 41,530,959 字节、49 visit、230 raw／225 selected，正常后台收束和退出。

第三幕5层历史学家开笼选择及离开已录，7层 `THE_FUTURE_OF_POTIONS` 额外领奖窗触发原有保护，raw190/191 缺口。生产 reader 另定位第一幕9层和二幕14层战后奖励之间的丢药水状态变化（raw42、154），后续须覆盖奖励界面丢弃或选择合适的代表路线。上述均保留，未拼为整局。source206（第三幕11层精英）起的后段和 source226 胜利片生产读取通过。

raw225 独立中断片 `897c2da1` 第10步再次录到佩尔眼，玩家回合3／战斗轮2；所选路径的 r24 战斗片未发现龙涎香饮用。证据 `runtime-verification-r24.json`、`collection-r24.json`、`checks/r24-third-act-tail.json`、`checks/r24-boss-extra-turn-fragment.json`，最新备份 `RUN005-post-r24-current-20261104-160755`。

## r25：实验体第一次倒下被检查点拒绝（019）

会话 `7aee9e26e92c464aa985b4a956804191`，486事件。source206 起三场原生胜利和路线匹配，进入实验体，前8步正常。第9步全身撞击使同一敌人 HP100→0、保留 AdaptablePower、进入 RESPAWN_MOVE；原生操作结果/RNG 验证在 Capture 前通过，`CombatUnits.Restorable` 拒绝这名仍在场的倒地敌人，`direct-back-roster-unsupported` 后恢复原上下文。正常退出，四份实际历史和原录像一致。证据 `runtime-verification-r25.json`、`collection-r25.json`。

020 仅允许仍在所属战斗 roster、带 AdaptablePower 并处于 RESPAWN_MOVE 的倒地实验体进入既有检查点；正放继续由原版驱动击倒、复生和最后胜利。Back 跨存活状态或复生次数时重建敌人显示节点，恢复原生稳定动画、该形态的 bounds、颜色和 HUD，逻辑对象与 RNG 沿用检查点，旧显示任务留在退役节点。主体 Release 0警告/0错误，加入019 producer 兼容，收据 `preparation/build-020-418b2a42.json`；安装后 `RUN005-post-build020-current-20261104-162003`，只变 settings.save，实机待测。

## r26：原生胜利通过，复生演出 Back 和音乐待修（020）

会话 `b52d544381ec4f5f8423631a50ec1e9d`，1915事件。第15层起点完整完成 Boss 19 步、最终事件和隔离原生胜利详情。多次回到首次倒下及复生前后资源/RNG 匹配，未出现观察器错误，已正常退出。实测五次复生附近 Back 总耗时1537–2396ms，绝大部分在原生动作收束，已有缩短转场节奏仍未覆盖 `TestSubject.RespawnMove` 两段 `Cmd.Wait(0.8f)`、`Cmd.Wait(1.15f)`。音乐参数由原版在倒下/复生/击杀调用，但不在旧检查点记录内。原 run、Boss 片、四份实体历史一致，已归档 `runtime-verification-r26.json` 与 `collection-r26.json`。

021 复用现有受审计的回合等待归属，绑定上述两段只负责复生演出节奏的等待，只有在在途动作收束时释放或跳过；原生动画触发、形态/HP/能力与结果校验仍执行。通过观察原生 `NRunMusicController.UpdateMusicParameter` 保存每个检查点的 `test_subject_progress`，入场默认段0，Back 调同一原生 API 复原。纯计时测试4项、主体0警告/0错误，收据 `preparation/build-021-0c9a4454.json`，安装后 `RUN005-post-build021-current-20261104-163446`。实际加速和音轨听感待测。

## r27：参数写回后音乐仍偏离，复生后抽牌和自动出牌阻塞（021）

会话 `82a735fb58184393bdaabadf9caff554`，1367事件。32次 Back 的战斗状态/RNG 均匹配，用户澄清状态问题主要是音乐阶段。6次原生参数写回（含2→1与1→0），听感仍未回到相应段。长 Back 有一次1.568s、一次0.646s，输入均发生在两段复生等待结束附近或之后。第一例1.500s原生收束里，已跳过换回玩家回合停顿，之后约0.82s抽牌并触发历史课，约0.51s才开始其自动全身撞击的攻击，恢复数据及界面只用约68ms。源日志 `runtime-verification-r27.json`、`collection-r27.json`，实体历史与原录像一致。

022 为每次原生音乐参数调用附一个检查点内序号。跨过调用才通过原生 PlayCustomMusic 重启实验体专曲并恢复目标参数，涵盖一次动作内1→2→1、回到数值相同但时间线已前进的情况。选择按阶段重建，精确曲内时间不作为目标。缩短仅实验体战的 EndTurn 收束：原生 Shuffle/CardModel 演出定时进入既有 wait broker，CardPileCmd.Add/Shuffle 等待的本场动画 Tween 在收束时加速到原生完成信号，保留原生回调及状态核对。Release 0警告/0错误，安装收据 `preparation/build-022-12af2812.json`，安装后备份 `RUN005-post-build022-current-20261104-164942`。实际听感及遮罩时长待复测。

## r28：022 初始化重载歧义（023 修复候选）

用户启动后主菜单回放入口缺失。启动日志 `ReplayMod unavailable: AmbiguousMatchException`，初始化错误触发本模组 Harmony 补丁回滚。新增 `TweenHelper.AwaitFinished` 绑定漏指定参数，实际有 `(Tween,Node)` 与 `(Tween,CancellationToken)` 两个重载。023 指定本来要挂的 `(Tween,Node)`；实际游戏程序集反射校验唯一目标且返回 Task&lt;bool&gt;，主体 Release 0警告/0错误。022 未进入回放。收据 `runtime-verification-r28.json`、`checks/build023-tween-target.json`、`preparation/build-023-cbbfd7a3.json`；安装后备份 `RUN005-post-build023-current-20261104-165333`，只有 settings.save 变化。

## r29：入口通过，实验体复生收束尾和正放音频待测（023/024）

会话 `f8d133a1a8bf45cb8b39143398b06099`，1872事件，入口、回放、状态/RNG核对及正常退出通过。第二次复生 Back 五次约792–823ms；代表一例两个 `RespawnMove` 等待已释放，约0.62秒才到 `MonsterModel.PerformMove` 收尾，此后才进抽牌/洗牌。洗牌飞行本身独立运行，原生正放未被旧 SourceVisuals 收纳，故可在退完后残留。原件及四份历史保持。证据 `runtime-verification-r29.json`、`collection-r29.json`。

024 将本 Boss EndTurn 中的 `CreatureCmd.Heal`、`PowerCmd.Apply/Remove` 原生演出计时纳入在途收束。把原生 Shuffle 飞行及配对拖尾纳入既有 Back 清理；恢复后通过原生牌堆刷新。原生复生时释放已留场击倒的旧 `DeathAnimationTask` 引用，允许下一次原生 `StartDeathAnim` 触发原音效；旧非移除任务继续原有收尾。记录实际阶段参数请求和原生死亡音效调用，以区别听感与触发，二阶段音乐仍待实测。主体 Release 0警告/0错误，固定 DLL 反射检查了新增签名。收据 `preparation/build-024-1818de71.json`、`checks/build024-native-targets.json`，安装后备份 `RUN005-post-build024-current-20261104-170638`。

## r30：实验体 Back 回归通过（024）

会话 `8d9cce0215fe4f99a644407762bf4eb6`，1760事件、零观察器失败。第9/13步边界共15次返回，端到端约38.6–148.8ms，全部状态/RNG匹配。已观测原生洗牌飞行与配对拖尾被同次清理；用户确认 Back 音乐、音效和大部分死亡声符合预期。第10步首次完整正放和 Back 后重做均有未处于收束的原生 `SfxCmd.PlayDeath(TestSubject)` 调用，请求同一 `test_subject_knock_out`；首次听感差异保留，不能据此定性为原版缺陷，也未追加人工重复音效。回放按本轮短测 Stop，游戏正常退出；四份实体历史和原录像一致。证据 `runtime-verification-r30.json`、`collection-r30.json`，备份 `RUN005-post-r30-current-20261104-171314`。

## r31：连续后段通过，首次敌人发现的胜利重做回归（024）

会话 `c36e37005dd54512841e75a269de98a8`，1158事件。第一遍 source206 起四场胜利、路线、原生 OnEnded(true) 和隔离 FalseVictory 历史及详情通过，Stop 恢复原上下文。第二遍在机械骑士胜利后 Back 到6/7，重做第7步成功；显示战斗奖励后第一个领奖操作（selected203）进入前失败，唯一差异为缺少 `MONSTER.MECHA_KNIGHT` 的本局首次发现。原生 UpdateAfterCombatWon 在隔离进度已有该敌人的 FightStats 时不再追加发现，旧 Back 只恢复了玩家发现和卡牌/遗物等门控。两遍收束/正常关游戏，录像及四份实体历史一致。

025 只在原生胜利更新前取一次隔离 `EncounterStats/EnemyStats/FightStats/Epochs` 的内存快照，胜利 Back 时与既有玩家快照一起恢复。重做仍经原生胜利路径并严格核对资源/RNG。Release 0警告/0错误。证据 `runtime-verification-r31.json`、`collection-r31.json`、`preparation/build-025-a13281c0.json`，安装后备份 `RUN005-post-build025-current-20261104-184725`，只变 settings.save。025 实机待验证。

## r32：原生首次发现的胜利 Back 重做通过（025）

会话 `2c37dd6b48fa4f399a6b35d2988ac5c6`，161事件。第三幕第11层机械骑士7步胜利，Back 到6/7，恢复隔离战斗统计与纪元，重做胜利后首项领奖 selected203/source207 原生资源和 RNG 全匹配。按计划在 selected204 前 Stop（Complete=false、Error=null），3个任务收束、零失败或取消，恢复原上下文，正常退出。一号槽进度、原录像和四份实体历史核对通过。另见二号槽写入的三项文件 mtime 早于本次进程（20:03:53–20:04:18，启动20:05:50），该槽多人 Continue/备份在本次补存时已缺，保留实际现状。收据 `runtime-verification-r32.json`、`collection-r32.json`，最新 `RUN005-post-r32-current-20261104-200918`。

## r33：第二个代表新局到首幕 Boss，发现三处链路缺口（025）

会话 `7fe3e28f81d74d93b7c39b3fb95734dd`，395事件。真实新局 `06105b5f6f524f3b93e6b3b84197ef1b`，E7RVJN3VR4TA、茂盛/蜂巢/荣耀、合法起点配方。涅奥第一次尝试选 Precise Scissors 时存退，raw1 未完成但被分支排除；Continue 后选 Neow's Bones 的额外遗物领奖使 raw2/3 成缺口。第8层 Whispering Hollow 的 GOLD 选项打开两瓶药水的额外奖励，raw38/39 缺口。第11层 Byrdonis 战后选牌与领药之间原生丢弃 FIRE_POTION，日志证实，缺少路线动作造成 raw55／selected54 前唯一资源不连续。78 raw／77 selected、17 visit、8个封存战斗片，正常后台写入和关闭。

原生存退准确命中 point27、PreFinished=true、路径止于 raw78；第一幕 Boss 胜利片 `2cb52b03a4154bc6967a5eb12b7ccb9d` 8步 production reader 通过，第14层 source64 起后段可读，从第11层 source51 的读取因丢药差异正确拒绝。实机观看未验。四份既有历史相同，当前 Continue/备份在 `RUN005-r33-continue-current-20261104-202158` 单列补存。证据 `runtime-verification-r33.json`、`collection-r33.json`、`checks/r33-act1-after-gaps.json`、`checks/r33-act1-boss.json`。

## 026：事件领奖与战后丢药换新短测候选

事件选项保留原生未完成任务，在同一操作内记录并验证原生遗物或药水奖励页、领取结果和玩家主动关闭。战后奖励页丢药单独登记药水栏位与身份，回放走原生药水弹窗和队列，再接原生领奖。旧记录的缺口保留。短测新局用铁甲 A0，沿用格挡牌组，涅奥首项固定为涅奥之骨，药水栏预填并由白兽雕像保障首战药水奖励。这个样本只覆盖开局与首战。

主体及 DirectContracts Release 均 0 warnings／0 errors，15 项 run-envelope 通过。安装收据 `preparation/build-026-ac9a19c1.json`，DLL `177EE6F3709A7316761AEAB8E673FDF5A2FD4688E3C7851B19F594D029A14BF6`。49 个已知文件安装后仅 settings.save 变化，第一幕 Boss Continue 及备份哈希均保持。原生录制、观看和 Stop 待用户短测。

## r34：涅奥领奖和战后丢药换新均已录入（026）

会话 `92998a93e9e94f2d81d2bb1ab89e4937`，42 事件，无录制错误。录像 `e58e76eae0674089ad6151e1e514b5af` 共 6 个操作，最终路线全部保留。涅奥之骨打开原生额外奖励，依次领取 CURSED_PEARL、PRECISE_SCISSORS，剪刀删牌及领取结果依序登记在同一选项内。首战三步获胜，操作 5 记录丢弃 0 号栏能量药水，操作 6 在该栏领取再生药水。两个房间均无缺口，资源连续性检查无差异，正常存退，写入队列已收尾。

整局和战斗通过生产 reader，证据 `checks/r34-rewards-short-run.json`、`checks/r34-rewards-short-battle.json`。观看前备份 `RUN005-post-r34-current-20261104-204509`（49 项，全部读回，game0）。下一轮复用同份录像验证从涅奥起 Auto 播到领取再生药水，以及额外领奖页尚未选择时 Stop 和重开。

## r35：Auto 与逐步领奖通过，Stop 曾放行诅咒尾部（026 → 027）

会话 `7d3f9d407e6048b6a13a5f6141e5337f` 三次观看。一次从开局 Auto 到尾、一次逐步领奖和丢药，均在操作 6 核对资源与 RNG 后留场，两个 Stop 都回菜单。第三次在操作 1 额外领奖页尚未领取时 Stop，未进入任何领取阶段，但约 0.79 秒后原生任务全部成功退出，用户看到领奖页关闭及诅咒动画。026 清理调用原生跳过按钮，正常完成 `RewardsSet.Offer`，因而放行涅奥随后加诅咒及预览。

027 在播放器拥有的事件领奖等待边界取消调用方，原生同步器释放后台奖励集合，领奖页随场景退场。正常观看主动关闭仍走原生按钮。已安装 `preparation/build-027-e8b7e918.json`，Release 0 warnings／0 errors、run-envelope 通过。短测录像和战斗片哈希保持，Continue 安装前后保持。原生 Stop 视觉及取消任务日志待复测。

## r36：额外领奖页 Stop 复测通过（027）

会话 `55a992a61faf4196b837e2f45597a2e4` 从 `e58e76ea` 开局按一次细步，原生额外领奖页出现后 Stop。约 4 ms 内确认 `OptionCanceled=true`、`NativeSetSettled=true`、`ProceedInvoked=false`；六个拥有任务全部收束，其中两个取消、零故障。约 140 ms 回主菜单，原 context 恢复。Continue、Progress 及两者备份与安装后哈希一致，录像原件未变。

细分同一次 Stop：4.16 ms 取消事件，37.87 ms 旧场景已移除且任务排空，137.22 ms 主菜单读回 Continue，138.28 ms 主菜单 `_Ready` 日志。后两阶段间没有动画等待调用，包含原存档隔离核对与原生菜单创建。菜单淡入由本体异步启动，可能提前返回，未测量屏幕首次实际呈现。

## 028：问号事件的部分领奖短测

Stop 中约 100 ms 落在恢复存档校验及主菜单创建阶段，当前日志无法拆出菜单创建和校验各自成本。没有找到可直接删除的动画等待。保留退出边界，继续事件药水短测。

显式开发开关准备铁甲 A0、六张格挡牌、100 金币与首普通→相连最左问号。首战 checkpoint 前安排事件池中的 Whispering Hollow 和 Unknown 当前赔率；观看从该首战原生合法起点进入，由本体掷房、扣金币及生成药水。领一瓶后主动关闭，覆盖原生事件奖励部分领取与剩余跳过。Build 028 Release 与 run-envelope 通过，安装 `preparation/build-028-781bc3b3.json`，原 Continue 已备份。

## r37：Whispering Hollow 药水部分领取录制通过（028）

会话 `73dca8a55f924a388ed9798466d7d353`，录像 `47f5e076328347109efcd922321164e4`。原生 Unknown roll 命中事件。操作 9 依序记录两瓶原生奖励、领取再生药水成功及主动关闭；金币 115→74，原生历史再生已拿、速度已跳。首战第 1 幕 2 层 source 5 到事件结束文字连续通过生产 reader，资源无断点，战斗三步胜利片也可读。涅奥万花筒产生附属 CardReward，当前仅允许 RelicReward／PotionReward，所以 source 1–2 明确保留缺口，范围从首战合法起点开始。正常存退、队列全部写入，game0。证据 `checks/r37-hollow-tail.json`、`checks/r37-hollow-battle.json`、`preparation/post-r37-current.json`。观看从第 1 幕 2 层起点 Auto。

## r38：问号事件部分领奖 Auto 通过，复核其他涅奥遗物原生路径

会话 `9e65b3c9566f4b849d9df1d6072004d3`，`47f5e076` 从 source 5 播放，第二次观看原生 Unknown roll 命中 Whispering Hollow，Auto 执行领奖、主动关闭，source 9 资源和 RNG 匹配，自动关闭并留场，Stop 无错误。存档、进度及备份、录像哈希保持。

固定本体版本源码确认：涅奥之骨与万花筒共享 `RewardsSet.Offer`，差异为奖励条目类型；万花筒的 CardReward 已有普通战后选择驱动，事件奖励层目前明确只接遗物与药水。沉重石板和巨大卷轴调用独立 `FromChooseACardScreen`，该原生选择页目前仅由战斗选择带支持，事件路径缺接入。巨大扭蛋直接嵌套调用 `RelicCmd.Obtain`，当前事件单槽授权不容纳同步重入的首个子遗物。巨大卷轴原生要求多人，当前单人 RUN-005 范围不含该实机验证。三件遗物为源码判定，未动态验证。

## 029：按原生入口接入其他涅奥奖励

事件附属奖励增加 `CardReward`，复用已存在的奖励按钮、原生卡牌奖励选择、退出清理和结果校验。独立 `NChooseACardSelectionScreen` 加入非战斗选择记录，验证候选顺序和是否允许跳过，从原生选择或跳过入口提交。事件授予遗物从单槽改为异步调用上下文，精确识别当前玩家和遗物实例，允许 `RelicCmd.Obtain` 在原生效果内嵌套调用，离开调用时恢复父上下文。

Release 构建、run-envelope 与 run-branches 通过。DLL `FA4777854413CD4725266D4509E49F71DD50D20F8C1539D7D7A942ECC9DB2F4B`，未替换已安装 028。万花筒、沉重石板和巨大扭蛋待独立实机取证。巨大卷轴本体仅多人可选，共用选择接入仅有代码证据。旧记录仍保留已录缺口。

后续覆盖讨论按入口分组。现成代表包括 Colorful Philosophers 的多份 CardReward、The Future of Potions 的奖励生成后升级、Lead Paperweight 的独立三选一。独立选卡包、领取途中叠开第二个 RewardsSet、事件内战斗返回追加奖励、事件奖励页满槽丢药以及战斗内遗物替换各有单独边界。

## 030：三条涅奥入口的原生短测候选

开发下拉为下一铁甲 A0 新局选择万花筒、沉重石板或巨大扭蛋，复用六张格挡牌开局配方，借本体涅奥 DebugOption 显示目标选项。录制实际工厂输入，观看原生重新生成选择/遗物/后续战斗。三种输入均通过 run-envelope roundtrip，普通输入约束保持。主体及合同 Release 无错误或警告。安装收据 `preparation/build-030-10d4b1c5.json`，49 项核对读回，安装只影响设置，原 Continue 已备份。

## r39：涅奥三条入口录播通过，发现地图移动在途 Stop 边界

会话 `051aafbc27b1402caf6fc7690274403f` 共 528 事件。万花筒 `17571cc3` 录播领取第一组一张卡、在第二组选牌页点跳过并关闭奖励页；沉重石板 `5614c8eb` 录播三选一的第二张；巨大扭蛋 `da925d3a` 原生连续授予 WAR_PAINT、STRAWBERRY，再加两张基础牌。三份各 4 路线操作、2 首战操作，均从真实开局完整播到胜利，所有选项操作的资源与 RNG 匹配，无路线缺口，独立生产读取通过。万花筒领奖页与选牌页、沉重石板三选一待选时 Stop 各有零故障的取消与回菜单证据。巨大卷轴仍只限多人，沿共用代码不外推实机。证据 `checks/r39-*-run.json`、`checks/r39-*-battle.json`；正常退出，49 项补存于 `preparation/post-r39-current.json`。

沉重石板另一次 Stop 在原生地图移动提交后约 65 ms，地图点 TextureRect 已被退场清理而原生移动还在继续，记录 1 个 disposed fault；原存档上下文仍正常恢复，后一次完整观看胜利。修复把 Stop 意图与原生入场选择取消拆开，让原生开场加载器自行到达完成或真正的选择等待边界再清理。源码 Release 通过，030 仍是当前安装版，回归可复用已有沉重石板录像。

## 031：卡包、独立网格和附魔的原生选择候选

共用非战斗选择记录新增三种原生窗口。卡包比对所有包及每包有序候选，原生打开预览并确认所选包。多选网格比对候选和选择上下限，保留最终选择顺序，区分原生自动提交与显式零选／多选确认。附魔另比对原生附魔 ID 和数量，经原生选中、预览、确认或关闭，由原触发方应用实际效果。外层仍由事件或商店操作保存及比较资源和 RNG；暂未用海玻璃或第二归属入口动态取证。

新局短测输入选择 Scroll Boxes，保留原生卡包生成，沿既有准备牌组加两张原生基础防御以保证木雕资格和可选附魔目标；开局同一配方在首战后的相连最左点放原生 Unknown，由本体掷房并选到木雕。另加仅一次的地图 Stop 开关，在原生地图移动已提交、入场任务尚未开始时触发公开 Stop，复用 `5614c8eb`。030 root/clip 显式兼容。

主体及 DirectContracts Release 通过，run-envelope 和 run-branches 通过，固定本体程序集反射核对 11 个选择窗口签名，区分附魔 PreviewSelection 重载。已安装 `preparation/build-031-79d0f7b3.json`，安装前 49 项对 r39 无变化，安装后仅 settings.save 变化；游戏未启动，原生短测待用户。

## r40：卡包与木雕附魔录播通过，整项前进过早确认（031 → 032）

会话 `ac623521534c4b9fa0afa199bc67f475`，新录像 `a3be7f3a20fe4258a5551c84329066d5`，9 项、3 房间、无缺口。涅奥记录第二包，首战自然生成 NIBBITS_WEAK，木雕在蛇选项附魔第六个候选，完成后原生前进为实际记录终点。资源和 RNG 核对匹配，原录像及战斗片通过生产读取。卡包预览中 Stop 回主菜单，零任务故障。旧沉重石板录像实测已提交地图移动后触发 Stop，直到原生安全边界，11 个任务收束、零故障、原 context 恢复。

`>>` 预览与确认在同一帧，卡包间隔约 1–2 ms，附魔同类间隔约 2 ms。032 等原生卡包展开 tween 结束，下一帧后才允许确认；卡包和附魔的整项确认都复用 Auto 的既有 0.65 秒预览节奏，仍只消费一个整项请求。已编译并安装 `preparation/build-032-9d450422.json`，保留 031 录像和战斗片兼容。复测看 `a3be7f3a` 从涅奥起用 `>>`，无须重录。

## r41：整项卡包与附魔预览通过（032）

会话 `e4a3f9b1da854621b7e00cab345a9e0a` 复用 `a3be7f3a`。卡包展开到确认约 657 ms，附魔选中到确认约 658 ms，两项均 Whole=true，资源和 RNG 匹配。停在木雕结尾，正常 Stop，17 任务收束零故障。实际主 Continue 与主进度文件保持，关游戏后的 progress.save.backup 与设置文件变化已单列补存。

## 033：先古多选和附魔短测

新增两种明确的开局准备输入。第一幕从涅奥直接连到同一张原生地图的 Boss 点，原生 Boss 与换幕投票保持，第二幕原生古神配置为奥罗巴斯，并通过本体 DebugOption 选择海玻璃或 Electric Shrymp。沿格挡准备牌组加两张未附魔基础防御和两瓶能量药。观看从真实新局工厂复建同一准备；实际奖励、候选、扣付与结算仍由原版执行。这是定向短样本。

主体和 DirectContracts Release、run-envelope、run-branches 通过。已安装 `preparation/build-033-013a72a7.json`。49 项备份读回，安装后仅设置变化，原生录播待测。

## r42：海玻璃多选与先古附魔录制通过（033）

会话 `ddcd7d3e302148a9b0f1100a4cb911f5`。海玻璃 `43e7c7ba` 从涅奥经原生第一幕 Boss、投票换幕到奥罗巴斯，15 张候选选中 0-based 9、11（VICIOUS、OFFERING），牌组 8→10；先古附魔 `d2c4fcc9` 在相同原生房间链给防御附加 IMBUED 1，牌组 8 张。分别 11／10 项、无缺口，实际记录终点包含奥罗巴斯前进至地图。两份整局和 Boss 战斗片均通过生产读取，`checks/r42-*.json`。49 项补存 `preparation/post-r42-current.json`。

海玻璃准备使用本体 DebugOption，从 Orobas.AllPossibleOptions 枚举首个 SEA_GLASS，实物序列化为 CHARACTER.IRONCLAD。普通生成先从其他已解锁职业抽取，只有无其他候选才回退本人。短测职业与正式常见奖励不同，仍有效覆盖 15 项候选、最终多选和原生加牌。保留实际准备以复用录像，不把此样本当随机职业生成验收。

## r43：奥罗巴斯开局 Auto 通过，Boss 起点与战斗 Back 问题（033 → 034）

会话 `4386134040c645bc957c2e854815c83d`。两份从涅奥起的 Auto 均到终点，资源和 RNG 匹配。从 Boss 起点观看 `43e7c7ba` 两次失败于 op10 before，唯一差异为首项 SEA_GLASS 对 GLASS_EYE，事件 RNG 相同。原录像和 Boss 保存包含真实奥罗巴斯，但短测 DebugOption 原只在新局工厂注册。034 对合法 Boss 起点校验同种子、角色与存档中的原生古神，恢复录像明确的准备选项，未注入结果。

同族小队 `d2c4fcc9` Back 59 次相关日志。首结束回合个别约 0.78–1.00 s，第二结束回合最高约 1.42 s，绝大部分在原生旧动作收束。034 扩展经过核对的可提前结束的原生呈现等待，包括出牌、能力飞行、药水投掷、PowerCmd 和洗牌；保留原生效果、RNG 和待完任务。沿既有清理登记能力牌飞行、药水投掷、能量粒子、格挡提示、能力移除、台词及遗物闪光。按节点生命周期隐藏或退场，能力牌旧显示由原生清理。胜利 Back 对有自定义曲目的 Boss 调原生音乐控制器，实验体仍走此前的阶段音乐恢复。仅候选，实机视觉与听感待复测。

034 Release、run-envelope、run-branches 通过，10 个原生入口签名反射核对，实际两份保存种子与开局一致且第二幕古神为奥罗巴斯。安装 `preparation/build-034-42d4188e.json`，备份 49 项，安装仅设置变化。

## r44／035：神官命中粒子绕过共用清理

会话 `6b1724fb7b3841c59f82efcaf702acc9`。海玻璃从涅奥进到终点；同族小队两次结束回合前后 Back 代表组分别最长 99.9／103.2 ms，原生显示清理日志有能力飞行、投药、格挡与台词等，用户确认基本修复。该会话没有 Boss 锚点打开海玻璃或同族小队胜利 Back，因此两项改动仍缺对应日志。

神官虚弱／脆弱弹用 `AttackCommand.WithHitVfxNode` 把 `NKinPriestGrenadeVfx` 直接挂到目标，不经 `VfxCmd`。035 补该工厂和自定义攻击者工厂的回调结果登记；既有清理路径优先。未知回调节点在回退时隐藏，留原生所有者收尾；神官手榴弹有明确的原生取消令牌，可取消后释放。原地复用的神官光束单独调用原生结束方法，不释放模型节点。检查另识别到直接回调产生的镜头震动／hit-stop／径向模糊、挂在模型上的 Spine 触发以及自管的延迟粒子、声音和共享牌节点，不能用临时场景节点的方式统一释放。整体回归按这些生命周期入口分组。

主体 Release、run-envelope 和新挂钩签名核对通过，已安装 `preparation/build-035-2e5bf69c.json`。49 项补存，安装仅设置变化。复用 `d2c4fcc9` 第 4 步结束回合，神官手榴弹命中后 Back；实机待验证。

## r45／036：思考气泡与两层受击震动

035 会话 `ff74b8d58eeb4e24993f66ed28a19f84`，同族小队 `d2c4fcc9`，36 次导航的状态和 RNG 匹配，最大 129 ms。14 次登记并清理 `NKinPriestGrenadeVfx`，用户确认粒子消失；一次正常原生胜利，Stop 恢复管理器、Continue 和 Progress，日志正常收束。余留用户现象为抽牌不足思考气泡与铁甲晃动。035 未单列气泡创建和震动状态，不能仅凭其日志区分首次出现与旧实例留场。证据 `runtime-verification-r45.json`。

原生 `CardPileCmd` 在抽牌和弃牌堆均空时由 `ThinkCmd` 创建 `NThoughtBubbleVfx`，绕过 `TalkCmd` 的语音气泡钩。036 将角色来源的思考气泡接入既有清理，移除时由原生 `_ExitTree` 收掉计时和 tween。导航时停止 `NCreature.AnimShake`、还原本地模型位置，切回稳定 Spine 循环时去掉旧受击姿态混合，并仅清掉绑定当前战斗场景的单次镜头震动。正常前进仍运行同一原生抽牌判断和效果。Release 与 run-envelope 通过，原生签名核对通过。安装 `preparation/build-036-222dcf35.json`，补存 `RUN005-post-build036-current-20261105-163109`，49 项仅设置变化。下一次复用同片第 4／11 步，分别核前进、在途 Back／已访问战斗节点 Seek、原生重做。

同类盘点确认机械骑士重劈还通过 `AfterAttackerAnim` 直接触发房间径向模糊和全局 hit-stop。前者是场景内可复用节点加独立 tween，后者是逐帧写全局时间倍率的原生任务。低血红边也是复用全局 UI 节点。它们均不经过这次的节点工厂或单次镜头震动路径，暂列独立实机样本；优先用已录的机械骑士核 Back／战斗内 Seek，再决定需要哪些清理。历史华丽收场和实验体证据保留。

## r46／037：回合结束的状态牌、诅咒牌与角色气泡

036 会话 `0349e10911d74ddcbf474704766866e9`，同族小队 `d2c4fcc9`，9 次 Back 的状态与 RNG 匹配。5 次退场 `NThoughtBubbleVfx`，4 次停止铁甲局部抖动。用户确认残留消失。此轮未观测到活动镜头 punch 或受击 Spine 混合，因此对应独立清理路径仍缺触发日志。Stop 无错误并恢复原管理器、Continue 和 Progress。证据 `runtime-verification-r46.json`。

原生回合末先消耗应触发的虚无牌，再按顺序结算各牌的手牌效果。效果牌依原生错峰进入出牌区，逐张等前一张效果完成，再去弃牌或消耗。灼烧和感染直接挂载 `NGroundFireVfx` 和 `NWormyImpactVfx`，绕过原有登记。037 接两种原生工厂的角色重载，兼顾火焰药水及使用同工厂的其他战斗动作。羞愧走现有减益路径，悔恨按本回合结束前的手牌数结算不可格挡伤害，沿用原生行动、数据快照和既有伤害显示退场。保留原生错峰和结算顺序；连续多张在途 Back 的等待尚待实测。战斗角色说话气泡改在原生角色工厂统一登记，和 036 的思考气泡对应；事件、商人、营火的非角色坐标重载仍由其页面管理。

Release、run-envelope、原生工厂签名以及代表片 `f97fb002` 的生产读取均通过。安装 `preparation/build-037-12d7006c.json`，补存 `RUN005-post-build037-current-20261105-184924`，49 项仅设置变化。可复用 `06105b5f` 的首场史莱姆战 `f97fb002`，第 5 步悔恨以 3 张手牌扣 3 HP，第 6 步火焰药水复用灼烧的火焰工厂。角色说话可复用 `d2c4fcc9` 第 10 步击杀第二只同族信徒时的神官台词。悔恨、共享火焰及台词重做待实机；这两份样本不含羞愧、灼烧本身或感染实际在手结算的覆盖。

## 038：战斗场景归属和共用镜头退场审计

原 `SourceVisuals` 和历史渲染继续的锻牌效果是独立静态集合，正常跨房和 NativeSeek Stop 的调用链均可绕过 `RetireSourceVisuals`。Godot 正常释放节点，集合会继续持有旧包装器和仍存活的节点引用。038 统一由当前 `NCombatRoom` 登记原生工厂产物、回调、发射器及继续的锻牌；同一次 Back／Seek 只读取该房间登记。驱动关闭、原生房间退出、历史 Trace Clear 都丢弃旧登记，记录房间实例、登记和仍存活数。自然播放交回场景原生销毁，旧 DecisionRoute Stop 在释放节点之后再 Clear。原生异步仍持有的短效节点保留原生完成流程，池化卡牌保留安全归还，持久光束只结束当前演出；未知攻击回调的节点先隐藏，仍由其原生所有者释放。火焰、感染、气泡等普通临时节点沿共用退场。

`NCombatRoom.RadialBlur` 与 `NGame.DoHitStop` 走跨敌人共用入口。只在导航收束时屏蔽废弃操作发出的新镜头效果。已启动径向模糊将原生 tween 推到终点，等原生 `Animate` 完成并隐藏。命中停顿只取消本场记录且仍由当前控制器持有的令牌，恢复原生终点 TimeScale=1，并在可重做之前等待旧循环退出，避免旧循环读取重做换入的新令牌。自然房间退出时，房间内模糊走原生析构，仍在运行的全局命中停顿同步取消。正常首播和原生重做继续调用同一效果。Godot `Tween.CustomStep` 结束事件已核官方实现。低血红边、其他全局震动渠道本轮无新证据，单列待审。

Release、run-envelope、4 个镜头入口和原生退出签名反射、`68196fed` 缩小甲虫片 production reader 均通过。安装 `preparation/build-038-4b126183.json`，补存 `RUN005-post-build038-current-20261105-190403`，49 项仅设置变化。暂未有 037/038 实机日志。`06105b5f` 整局从悔恨所在 source9 启动仍被旧后段缺口正确拒绝，应从“查看本局分场”单开 `f97fb002`；自然切房用 `d2c4fcc9` 从同族小队战斗锚点继续到奥罗巴斯。机械骑士 `8c0fd551` 7 步均在第一回合，未触发重劈。现有 `68196fed` 第 3 步会触发缩小甲虫的共用径向模糊，可测首次正放、Back、战斗内 Seek 和重做。已找到的旧录像无命中停顿实录，仍需真正打到重劈或其他原生使用该控制器的招式。

## r47／039：跨回合护盾碎片、胜利回血与固定等待

038 会话 `be3ccef882b14aa692d6704f0273712f`，四次打开均正常关闭，47 次 Back／Seek 状态与 RNG 匹配。神官说话 3 次在目标前退场，同族 Stop/重开房间 ID 不同，第二次原生播至奥罗巴斯，旧战斗登记在驱动关闭时释放。缩小甲虫首次/重做走原生镜头，8 次在途退场覆盖 Back 和 Seek，原生镜头任务结束。史莱姆悔恨第 5 步结算重做匹配；第 6 步 16 次导航记录火焰工厂的退场，用户确认火焰正常，但跨回合护盾碎裂、胜利回血图标／数字与等待仍有异常。16 次胜利 Back 原生结算退场最大 16.3 ms，总最大 360.1 ms。证据 `runtime-verification-r47.json`。

039 将 `NBlockBrokenVfx` 和 `NHealNumVfx` 接到同一战斗登记。Back 收束期间屏蔽新碎片、新回血数字和原生遗物展示节点，已经出现的效果由登记清理；原生遗物触发与治疗照常进入原生胜利业务、验证结果后恢复。`NCombatUi.PostCombatCleanUp` 独自创建出牌区淡出 tween，现按原生调用前后的 Tween ID 登记，回退时只停止这段写入，出牌区由原检查点恢复，移除胜利后固定等 300 ms 的窗口。Release、run-envelope 和新签名通过；安装 `preparation/build-039-a7d42ba0.json`，补存 `RUN005-post-build039-current-20261105-194133`，49 项仅设置变化。复用史莱姆第 5／6 步，重点复测在碎盾时 Back、在火焰药水飞行时 Back、正常重做到胜利后立即 Back 两条路径。

## r48／040：燃烧之血的独立治疗粒子

039 会话 `fc7c19e179b14217bacb1781336b9a29`，史莱姆 `f97fb002`，12 次 Back 状态/RNG 匹配。胜利 Back 共 10 次，范围约 20–103 ms，首次后最大约 64 ms，实测退场仍在运行的原生胜利淡出 1 次。用户确认停顿基本消失，仍见燃烧之血的效果。数次在飞行中 Back 的退场源有火焰、命中与一份通用 `NVfxParticleSystem`，没有新 `NHealNumVfx`；这与 `CreatureCmd.Heal` 在创建数字之外还直接调用通用治疗光效相符。用户所见具体像素仍由实机复测确认。Stop 原管理器及 Continue/Progress 恢复，证据 `runtime-verification-r48.json`。

040 在共用 `VfxCmd.PlayVfx` 抑制导航已受理后的普通治疗／Osty 治疗粒子，已有粒子仍走场景登记退场。回血数字和遗物闪光也从接受 Back／Seek 起抑制，并分别写日志；实际治疗、原生胜利钩和重播保留。Release 与 run-envelope 通过，安装 `preparation/build-040-47535f93.json`，补存 `RUN005-post-build040-current-20261105-195028`。复用史莱姆第 6 步在药水飞行时 Back，随后正常播到胜利作对照。

## r49／041：定位到遗物闪光粒子的实际发射边界

用户澄清 039 后治疗光效和回血数字已正常，仍残留的是火焰药水飞行时按 Back、回退途中出现的遗物闪光。040 会话 `ebe1c5537ccf4c52bf585bcd364279cd` 三次该时序确实记录 `NRelicInventoryHolder.DoFlash` 前缀抑制燃烧之血，另外三次在胜利后 Back。六次均匹配状态与 RNG，Stop 恢复原上下文。旧日志只证明托管显示入口执行了抑制分支，对玩家仍看到的闪光不作通过结论。证据 `runtime-verification-r49.json`。

读取固定游戏 PCK 的 `relic_inventory_flash_vfx.tscn`，确认根脚本为 `NVfxParticleSystem`，其 GPU 发射器默认关闭、由原生 `_Ready` 开始发射。041 把遗物闪光的拦截和登记移到该实际粒子节点，按原生场景路径识别，适用于所有遗物。接受 Back／Seek 同时隐藏和释放顶栏中已在运行的同类节点。普通前进走原生发射。新增 `native-relic-flash-emitter` 区分实物节点是否发射，`native-relic-flash-navigation` 记录已在途清理。Release、run-envelope、`_Ready` 签名通过；安装 `preparation/build-041-92abd4a1.json`，补存 `RUN005-post-build041-current-20261105-195822`。需复用同一史莱姆第 6 步核视觉结果。

## r50／042：角色头顶的遗物图标

041 会话 `1e930c133ca74ce7988fe2573fd8da60`，史莱姆片 9 次 Back 状态/RNG 匹配，6 次在途顶栏粒子实际抑制，1 次正常前进允许顶栏发射。用户截图显示剩余火焰图标位于铁甲头顶。扩大检索到所有 `RelicModel.Flashed` 订阅者，发现独立的 `Player.OnRelicFlashed` 会用 `NRelicFlashVfx.Create(relic,target)` 将遗物图标放在目标 `GetTopOfHitbox()`。之前定位到顶栏路径，未覆盖截图里的另一条。证据 `runtime-verification-r50.json`。

042 在共用角色目标工厂抑制导航已受理后新建的遗物图标，正常前进时登记到现有战斗场景；接受 Back／Seek 即隐藏释放仍在场的已登记头顶图标。原生胜利钩与治疗业务保留，顶栏亦沿 041 路径。`native-relic-target-flash` 记录原生遗物 ID、角色与抑制状态。Release、run-envelope 及两组原生重载签名通过，安装 `preparation/build-042-dbe5175d.json`，补存 `RUN005-post-build042-current-20261105-231708`。同片第 6 步在途 Back 和正常胜利的头顶图标待实机对照。

## r51：042 角色遗物图标实机通过

用户确认残留消失。会话 `a4fcb7239b6a49adb970b0bab404e3e8`，史莱姆片 `f97fb002`，12 次 Back 状态/RNG 全匹配。两次在火焰药水尚在执行时 Back 均阻止燃烧之血创建角色头顶图标，六次正常胜利允许创建；共七次立即收掉已在场的头顶图标。灯笼另有四次从同一原生角色工厂正常创建的记录，支持入口为共用。最后正常重做到第 6 步胜利，全操作匹配，Stop 恢复原管理器和 Continue/Progress，日志无失败。证据 `runtime-verification-r51.json`；已补存 `RUN005-post-r51-current-20261105-232453`。

## r52／043：原生跨回合预排输入与自然新局录制缺口

2026-11-06，会话 `cbf6cc62d6dc48ca8d1ecc68538df8ed`，自然新局 `9b9979fb` 录至第 8 层营火存退。第 7 层 `ENCOUNTER.PHROG_PARASITE_ELITE` 第 7 回合末，完成 26 项后观察器在第 27 项结束回合进行中收到额外请求，报 `wait-for-completed-operation`。本体后续实际执行两张打击并胜利。该战 raw30 明确 `missing`，其余领奖、地图、营火和存退正常。前三份战斗片生产读取通过。未尝试拼出失败战斗，原局文件和原生存档保留。证据 `runtime-verification-r52.json`。

原生 `RequestEnqueue` 会把 `NotPlayPhase` 的 `CombatPlayPhaseOnly` 请求延迟，在 `PlayPhase` 重送；`EndTurnPhaseOne` 同类请求由原生队列取消。043 录制器只在实际进入原生可出牌阶段时登记，队列释放若同步启动第一张牌，则在原生下一回合准备已结束的边界先封存上一回合，再记录出牌。既有播放和普通回合内路径保持；加入本次真实 042 录制的精确 build 兼容。Release、run-envelope、三片生产读取通过。安装 `preparation/build-043-359a1fcd.json`，补存 `RUN005-post-build043-current-20261106-010437`；安装比较仅 settings.save。需以后续自然战斗原生预排及封存验证，旧缺口仍保留。

## 044：普通战外用药的原生路线动作

`use-potion` 记录原生 `UsePotionAction` 的槽位、药水、单人目标和实际顶层界面。复用战后丢药的药水弹窗入口，观看由原生使用按钮和 `EnqueueManualUse` 提交，等待真实动作结束，核对药水被消耗以及结算前后的血量、药水槽、历史和 RNG。消耗槽允许被混沌药水重新填入。回放 Stop 若已提交则先让原生动作在所属房间收束。普通 `AnyTime` 自身／单人目标使用同一路径；污浊药水商人目标、仍在结算的事件附属奖励和任意弹窗内用药另有归属，未接入。记录新字段只在存在时写，既有运行档未改。

主体和 DirectContracts Release 通过，run-envelope 新增药水参数往返、非法目标拒绝和旧字段省略；真实 `9b9979fb` 旧 run 通过生产 writer／reader 字节检查。原生弹窗和动作签名核对通过。安装 `preparation/build-044-d5e4fb79.json`，补存 `RUN005-post-build044-current-20261106-012028`。当前真实存档第 8 层营火，鲜血药水槽 0。建议下一场普通战斗胜利后先在奖励界面喝药，正常继续到地图并保存退出，再从这场战斗的独立整局锚点看至地图。短测待实机，旧精英战缺口保持。

## r53：044 鲜血药水战后录制通过

会话 `a0b0e750a8324137bf12e8eec1eb3cbd`，原局 `9b9979fb` 从原生营火保存点继续。第 9 层实际为精英 `ENCOUNTER.BYGONE_EFFIGY_ELITE`，两次主动存退的中断战斗片各自保留，最终路线采用第 3 次尝试的 14 步胜利片 `c78f848f`（raw44）。raw45 `use-potion` 完整，奖励页槽 0 对自己使用鲜血药水，HP 60→76／80，槽 1 易伤药水保留，Run RNG 不变。raw46–50 领奖、跳过选牌和前往地图，保存退出及日志写入正常。原生实际磁盘存档仍是该战胜利的自动保存点 raw44，录像尾部确实录到回地图。日志无 043 预排入口命中。

从 raw44 的生产尾部读取及胜利片读取通过，无新增录像缺口或资源断裂。观看使用整局详情的「从已记录起点观看 · 第1幕9层／尝试3」，Auto 到地图并 Stop。证据 `runtime-verification-r53.json`、`checks/r53-blood-potion-tail.json`、`checks/r53-blood-potion-battle.json`。当前档案补存 `RUN005-post-r53-current-20261106-110510`，049 份已知文件均读回。保留 044 等待观看实机。

## r54：044 鲜血药水观看通过，045 战斗演出候选

会话 `8cb1b0fd780c48518b51ff027b59f0a9` 以 044 打开 `9b9979fb` 第 1 幕第 9 层精英。raw45 在奖励页原生喝药，资源与 RNG 一致，无预期状态注入。第一次观看在继续领奖时 Stop，第二次在头槌后 Stop，两次恢复原上下文且无播放错误。用户确认喝药正常。回退检查使用战斗片 `c78f848f183b4d65a4773d9ab95bdee2`。结束回合第 6／10 步最慢约 599／628 ms，燃烧契约第 1 步最慢约 620 ms。第 9 步头槌选牌窗口就绪到选择交付仅 5.5 ms。证据 `runtime-verification-r54.json`。

045 将精英初睡、苏醒和斩击的纯演出等待，以及内核加速生成后的展示等待，接入当前动作的可收束计时器。回退燃烧契约时收束原生手牌选择位移动画，仍由原生 UI 提交录像决定。战斗牌堆选择在 `>>` 和 Auto 中从就绪起至少展示 400 ms。接受导航时隐藏原生战斗预览、飞牌和其同级拖尾，由原生继续完成回调并在恢复前清理。飞牌拖尾在入树前用实际父节点坐标初始化，保留原生短拖尾。截图中的斜光疑似来自粒子起点跳变，需实机确认。

主体及 DirectContracts Release、`run-envelope`、`run-branches`、`phase-wait` 和 r54 原战斗片／整局尾部生产读取通过。安装 `preparation/build-045-1a9f36a1.json`，补存 `RUN005-post-build045-current-20261106-112906`，安装只改设置。复用同一片第 1、6、9、10、11 步，分别核选择中立即 Back、两个回合收束、头槌首次及重做、虚空预览或飞行中 Back。

## r55：045 回退时长和预览通过，046 补牌堆拖尾与精英切段

会话 `9abe1bc2ba8d4b7d9f5c59dd36f8f0fa`，42 次 Back 全部精确恢复。第 1 步最慢 103 ms，第 6／10 步最慢 98／118 ms；头槌七次整步牌堆窗展示 401–409 ms。已验证胜利后继续至当时录像终点，恢复原上下文，`Error=null`。用户确认上一轮其余问题基本解决，附图和无模组对照证实仍有斜线，另报雕像苏醒后 Back 重播缺少转段音效。本次正常游玩又生成第 11 层 19 步胜利片 `5abec0a710c74f81bd4c42fc821762c8`。证据 `runtime-verification-r55.json`。

头槌弃牌堆到抽牌堆走 `NCardFlyShuffleVfx`，原版拖尾场景的两条 `NCardTrail` 把相邻全局坐标连为线段。045 只预置了拖尾，跟随节点要等原生第一帧才从默认原点移动。046 在原生 `_Ready` 前将跟随节点放到原生 `_startPos`，保留原版随机控制点、速度、曲线和原生拖尾。苏醒的 `TriggerEliteSecondPhase` 经原生控制器设置全局音乐阶段；046 在同一战斗的检查点保存这个状态，跨阶段回退后调用原生 `UpdateTrack` 恢复初段，再由重做的原生苏醒重新触发转段。

主体及 DirectContracts Release、`run-envelope`、`phase-wait`、044／045 两份战斗片与更新后的整局尾部读取均通过。安装 `preparation/build-046-d09a0fae.json`，备份 `RUN005-post-build046-current-20261106-115258`，保留用户新进度。复用第 9 层同片，第 9 步对照头槌首次和 Back 重做，第 10 步苏醒后 Back 到 9/14，再重做观察音效。

## r56：046 头槌通过，047 重建精英原生音乐实例

会话 `fd814a7ee09b45fa8d962eb3a60a7493`，头槌五次使用原生弃牌堆起点 `(1866,1025)` 代替默认 `(0,0)`，用户确认长斜线消失。第 10 步有四次正常的 `TriggerEliteSecondPhase`，三次跨回苏醒前均调用原生控制器恢复第一阶段。用户仍听不到重做音效。18 次 Back 的状态与 RNG 一致，两次 Stop 正常。证据 `runtime-verification-r56.json`。

用本机自带 FMOD Studio 2.03.06、原生第一幕两套音乐 bank、静音输出和 timeline marker 回调做对照。保留旧事件，仅 `Progress 8→6→8` 时，两套曲目均未在再次切段后的 4 秒观察窗到达 `Elite Phase 2`。先设 `Progress=6` 并重建同一曲目的事件实例，等待约 1.4 秒再切到 `8`，两套曲目均在观察窗到达原生 `Elite Phase 2` marker。047 仅在跨回第一阶段时，用 `UpdateTrack` 和 `PlayCustomMusic` 执行这个顺序。实际游戏音效仍待听测。

主体 Release 与差异检查通过。安装 `preparation/build-047-91d969a2.json`，备份 `RUN005-post-build047-current-20261106-120859`，安装仅改变设置。复用第 9 层同片，第 10 步苏醒后 Back 到 9/14，再重做听转段。

## r57：047 雕像苏醒音效实机通过

会话 `1fb2c9d392ea4729805962d3537be176`，用户确认声音正确。第 10 步四次正常触发原生二阶段切换，三次退回苏醒前均重建原生音乐实例。10 次 Back 的状态与 RNG 全部匹配，播放到原生胜利后亦验证回退。Stop 恢复原上下文，无播放错误，游戏退出且日志写入无失败。证据 `runtime-verification-r57.json`。

2026-11-08 的 build-070 原型短测中用户报同一音效回归。会话 `66155eecd81140f5b20b390dba2bf0e9` 第 10 步仍有 6 次正放原生二阶段调用。共用音乐恢复把顺序改成先重建曲目、再设 `Progress=6`。build-071 恢复共用幕音乐的先设阶段、再重开曲目。用本机 FMOD 静音对照，两首 Overgrowth 曲目均仅在修复顺序下重新触发 `Elite Phase 2`。证据 `checks/build-071/elite-fmod-phase-order.json`，已安装 `preparation/build-071-f4091920.json`。

build-071 用户确认苏醒音效恢复。会话 `de06435d3f5a423e89784229f7823873` 播放同一片 `c78f848f`，第 10 步有 5 次正放二阶段调用；6 次回到苏醒前均按修正顺序重开曲目并核对状态与 RNG。该次路线完整播放，关闭无错误，原上下文已恢复，日志写入零失败。

## r58：047 整局第一幕录制与第二幕保存通过

会话 `5be5a1f5cc654281ac7af34815cedba4`，新局 `e7138bcda8374257ab1ffcdbe1955ac3`。69 项原始／68 项所选操作、18 个访问，涅奥到达尔弗无 `gap`／`missing` 或资源断裂。仪式鹿 11 步胜利片 `fa6a428c9e124344a3ddb8d2feed3a15` 通过生产读取，记录中未见 `Ringing`。原生奖励、换幕和达尔弗选择均已录，保存点 28 的真实磁盘读取表示匹配。最后一次离开达尔弗在该保存点之后，重开后的最终分支以原生 Continue 为准。记录写入完成，游戏已退出。证据 `runtime-verification-r58.json` 与 `checks/r58-act1-baseline-tail.json`。

查原生 `Ringing`、`Bound` 和 `Smog`，三者均为 `AfflictionModel`，录制边界已包含牌上的类型和数值，正放由原生战斗逻辑触发。现有 Back 的原生手牌更新不重载复用牌的覆盖层，而字段恢复也不会发送 `AfflictionChanged`。候选改为只在附着类型变化时调用原生 `ReloadOverlay`，Back 校验另纳入卡牌身份、附着类型及数值。Release 通过，候选保留 047 录制兼容，尚未安装或实机验证。基线录制继续使用 047。

## r59：047 第二幕录制与第二次换幕，达尔弗 Continue 候选

会话 `42065f3a51174000a53be377d4f0cef3` 真实 Continue 命中 r58 的保存点 28，同一局从达尔弗进入第二幕路线并战胜贪食者，20 步 Boss 片通过生产读取。原生换幕到第三幕坦克斯开场，当前实际加载表示精确匹配保存点 52，写入已收束。146 原始／144 所选操作，34 个访问，文件 28,833,096 字节，零 `gap`／`missing`。49 项备份读回。证据 `runtime-verification-r59.json` 和 `checks/r59-act2-boss.json`。

047 的全链静态核对拒绝 raw68 选择白星到 raw70 读档后离开达尔弗。原版读档重新初始化已完成的先古，达尔弗生成候选时 `DustyTome.SetupForPlayer` 消耗一次奖励 RNG，房间历史也在真实 Load 时更新。候选将真实保存点、前次选择和新尝试中的离开绑定，在观看时捕获本次原生选择实际生成的保存 DTO，沿隔离读档路径重建，随后精确比较 raw70 的资源与 RNG。原始录像保持，缺少匹配存档的相同资源跳变仍拒绝。Release、`run-branches`、`run-envelope`、r59 原录像生产读取通过；实际从第一幕 Boss 到达尔弗的短重播和整链仍待验证。当前继续用 047 完成第三幕。

## r60：047 原生三幕胜利与第三幕录制缺口

会话 `04e1393bc98c453f8524e0e7f62ad52e` 原生 Continue 命中保存点 52，完成实验体与建筑师，raw233 和原生 `OnEnded(true)` 的胜利状态一致，实际历史 `win=true`，实验体片通过生产读回。233 原始／231 所选操作，49 个访问，文件 50,475,966 字节。第三幕 4 层假商人关闭货架时普通商店强转 `InvalidCastException`，raw167 明确缺口。raw220、221、231 前分别缺原生 SPEED_POTION、EXPLOSIVE_AMPOULE、POTION_OF_BINDING 丢弃，前两次在商店，第三次在终局。原始 047 录像从涅奥及该缺口后的尾部均不能作为完整可播证明。证据 `runtime-verification-r60.json`，原始录像与胜利历史已核哈希归档 `RUN005-r60-original-victory-20261106-132440`。

048 将假商人的原生库存、打开、关闭和离开入口接入商店录播，保留独立 `EventRoom` 与原生库存生成；商店和建筑师房间的丢药复用战后丢药的原生动作，单独记录槽位、药水和场景。假商人扔臭药水触发的内部战斗仍待覆盖。文件上限从 64 MiB 调到 96 MiB，容纳重新实录第三幕时保留旧分支。Release、`run-branches`、`run-envelope` 通过。安装 `preparation/build-048-032fb432.json`；当前档案已备份，原生胜利后 Continue 槽位为空。候选待短测。备份 `RUN005-post-r59-act2-current-20261106-130635` 的真实坦克斯开场和备份槽位哈希通过，等待用户同意恢复这两个文件，未执行恢复。

用户确认后只将该备份的 `current_run.save` 和 `.backup` 复制到当时空置的 modded1 槽位，两份哈希逐项读回，原录像及胜利历史哈希一致。恢复收据 `preparation/r60-restore-act3-current.json`。下一轮手动 Continue，沿坦克斯→咬人卷轴→湿滑桥→假商人。打开货架、关闭、离开，在下一场战斗胜利奖励页存退，先核新分支和原生续录点。

## r61：048 假商人录制短测通过

会话 `7404ecc0643347ae9acb7aa907bf8fb2` 从同一局原生保存点 52 开新尝试，旧 233 项原始记录保留。第 4 层假商人的打开、关闭、前进为 raw257–259，三项均完成，下一场雕刻师的八步胜利片 `87e765d88c9c459d96abe04c237532a2` 通过生产读回。整局 261 项原始／172 项所选操作，57,466,874 字节，所选路径无缺口、无资源断裂，raw167 仍保留于旧分支。胜利奖励页保存点 83 原生读取表示精确匹配，原始胜利历史哈希保持，49 项备份 `RUN005-post-r61-fake-merchant-current-20261106-173220`。证据 `runtime-verification-r61.json` 与 `checks/r61-selected-baseline-tail.json`。

原生观看短测用同一整局两次开启。先从第 3 幕第 2 层尝试 2 Auto 过假商人，到第 5 层胜利后 Stop。再从第 1 幕第 17 层尝试 1 Auto 过达尔弗，从读档页离开并到第二幕首战后 Stop。商店与建筑师丢药仍待实际触发。

## r70–r71 053 实机结果与 054 建筑师丢药入口

053 千足虫短测 `d75da30be5a146068970ccfdeb76c831` 有 54 次 Back 的状态和 RNG 一致、8 次原生胜利通过。整局观看 `bab4e9a5958b445381393bcfd7e1d588` 从涅奥推进到实验体第 24 步击杀和建筑师进场，原生对白开场完成后，入口在执行首项前抛出 `run-final-event-destination-mismatch`。raw319 的结束状态等于 raw320 的开始状态，后续实际是丢药两次（raw320/321）再选对话。054 接受同一建筑师胜利房间的已完成丢药记录作为首项，原生丢药、事件选项与状态及 RNG 仍各自核对。入口合同覆盖首选丢药、首选对话与错误房间或丢药上下文拒绝。Release、run-envelope、run-branches、diff 检查通过。安装 `preparation/build-054-b594da2a.json`，备份 `RUN005-post-build054-current-20261106-215215`。

整局会话的 176 次 Back 中，以 200 ms 作排查线，贪食者跨回合最慢 2638 ms（结算收束 2586 ms），斧机器人最慢 298 ms（结算收束 260 ms）。其余有跨回合样本的五场均在 101 ms 内，动画和听感仍需实机核对。贪食者 7 次胜利 Back 记录 BGM 恢复调用，机械骑士 4 次和昆虫术士 1 次胜利 Back 未记录同类调用，未能由日志判断实际听感。逐怪矩阵已发 PM，覆盖正放、跨回合攻击前／演出中／结束后、Back 后重做、胜利 Back 的 BGM。实机证据及逐场时延见 `runtime-verification-r70.json`。

## r68–r69 052 开局 Auto 阻断与 053 千足虫修复

会话 `8d45936021744677ab0586d262b5e1a2` 从涅奥开局原生推进，经达尔弗并抵达第二幕第 7 层精英，路线前 90 项完成。残杀千足虫片 `181ae86d43ad44b8b766ffecedc7ba5c` 第 8 步全身撞击结果与 RNG 通过比较，尾节归零后仍留在原生战斗队列，携带 `REATTACH_POWER` 与 `DEAD_MOVE`。回退快照拒绝这类仍在场的死敌，终止观看并恢复原上下文。片中第 10 步另有 `REATTACH_MOVE`，第 11 步三节皆倒下后原生获胜。证据 `runtime-verification-r68.json`。

053 支持仍在场的倒下与待复活节段，跨生死恢复原生模型、血条与交互，保留复活和伤害等原生结算。战斗胜利可从三节集体退场的状态恢复，撤销尚未启动的旧集体退场，保留原生结果核对。Release、enemy-generation、units、run-envelope、run-branches、diff 检查通过。安装凭证 `preparation/build-053-6f7b9807.json`，备份 `RUN005-post-build053-current-20261106-205758`。当前原片保持，待实机复验。

## r67 052 完整录制通过，开局连续观看待验收

会话 `864db61b81df4cc5ac3b42c4e9eed3c7` 由原生 Continue 命中点 83，以同一 Run `e7138bcda8374257ab1ffcdbe1955ac3` 续录。第三幕 Boss 24 步片 `d259fac1f42b45be8f0752cb92bf0602` 通过生产读回。建筑师房间录到两次丢药和三次事件选择，raw324 标记胜利，状态精确等于 `native-OnEnded(true)`。原生三幕历史 `1793940083.run` 的种子与开局相同，`win=true`、`was_abandoned=false`。录像写入已收束。

生产读取由 324 项原始操作选出 235 项，首战前的涅奥选择、离开和地图输入均完成。最终路线无缺口、无资源或 RNG 断裂。旧尝试留在原件。源文件和 29 个引用战斗片、现有 catalog、录制会话、原生胜利历史共 529 文件已归档并核对哈希。证据 `runtime-verification-r67.json`、`checks/r67-completed-baseline.json` 和 `collection-r67.json`。下一步在 052 从「从开局观看 · 涅奥奖励」Auto 到建筑师及胜利详情后 Stop，核整链原生状态与恢复上下文。

## r65–r66 051 实机与 052 意图残影修复

051 会话 `7afe664a64cc4029add0ad082b16b2ab` 中，咔咔 9 次叫声、12 次攻击的原生粒子启动均可见并发射，18 次从 Back 隐藏恢复，无已释放对象异常。36 次 Back 的状态与 RNG 均核对。用户确认咔咔粒子通过，另报意图图标残影。证据 `runtime-verification-r65.json`。

052 在 Back 收束开始与恢复前清理 `NIntent` 的原生一次性粒子。图标和数值仍由原生控件显示。重做时只在下一个原生意图执行时重启已清理的发射器。Release 构建、presentation、phase-wait、run-envelope 与 diff 检查通过。安装凭证 `preparation/build-052-283e04d7.json`，存档备份 `RUN005-post-build052-current-20261106-190741`。

052 会话 `cbef7e5d8653491994db71e773b370c3` 记录 12 次意图粒子清理及 12 次原生重启，14 次 Back 的状态与 RNG 均匹配。咔咔战斗正常播完并退出，用户确认残影修复。证据 `runtime-verification-r66.json`。

## r64–r65 050 实机与 051 咔咔粒子修复

050 会话 `732e78597d9f4db9b6f71c34c5ce9ea6` 中，仪式鹿 10 次 Back 用时约 15–67 ms，壁垒 8 次约 21–52 ms，状态和 RNG 均匹配，用户确认通过。咔咔正放时叫声及攻击粒子缺失，Godot 日志记录 6 次 `NDevotedSculptorVfx.OnAnimationEvent` 访问已释放的信号参数。证据 `runtime-verification-r64.json`。

051 让原生回调先发射粒子，再登记及恢复被 Back 隐藏的发射器。传入的骨骼事件由信号持有。已进入回退的回调仍被拦截，已开始的粒子与音效沿用现有撤销清理。Release 构建和 diff 检查通过，待实机复测。安装凭证 `preparation/build-051-56bfc32c.json`，存档备份 `RUN005-post-build051-current-20261106-184517`。

## r63–r64 049 实机与 050 Back 候选

049 会话 `4fd98190c9d3448db6ae4da364cf03ad` 到达达尔弗，由同次回放的原生保存点继续并核对第二幕首战初态与 RNG，Stop 正常恢复上下文。第三幕从第 2 层开始观看至第 5 层胜利，正常收束。仪式鹿第二次结束回合的 4 次 Back 用时 961–1731 ms。壁垒 16 次 Back 最慢 156 ms，其中威吓头盔触发后多等约 100 ms。咔咔两回合的 Back 最慢 73 ms，用户仍观察到已经开始的台词音效和攻击粒子残留。上述 Back 的状态及 RNG 均匹配。证据 `runtime-verification-r63.json`。

050 给仪式鹿冲撞的四段演出间隔和原生格挡结算后的等待接入现有撤销流程，保留伤害、格挡记录及后续钩子。格挡布局刷新保留原生基准 Y，防止入场中的偏移被缓存。咔咔两组原生骨骼粒子在 Back 时隐藏并停止发射，下次原生事件再恢复。咔咔与仪式鹿的指定战斗音效保留独立 FMOD 句柄，允许停止已经播放的实例。正常推进继续使用原生事件和原有节奏。

Release 构建零警告零错误，phase-wait、run-envelope、run-branches 合同及 diff 检查通过。安装凭证 `preparation/build-050-a987d300.json`，存档备份 `RUN005-post-build050-current-20261106-182848`。原生试听与 Back 时序待复测。

## r62：048 假商人回放通过，049 回放与 Back 候选

会话 `f98000d351a84509b2dc089d8bced6ee` 假商人 raw257–259 三步原生执行、资源和 RNG 匹配，到第 5 层胜利、Stop 恢复原上下文。第三幕首战壁垒回退最慢约 200 ms，卡牌原生 `AddDuringManualCardPlay` 的能力牌入场 tween 未接入收束。第 5 层首回合五次 Back 约 1077–1103 ms，与 `DevotedSculptor.ForbiddenIncantationMove` 的 300 ms 与 750 ms 演出等待匹配。用户报动作残留和盾图标位置异常。截图中 92 HP、4 格挡、荆棘 3、力量 1 和壁垒与第 2 层第 1 步完成后的录像状态一致。

会话 `dbd79662efec4bd09ff764d3f72ab073` 第一幕 Boss 11 步均匹配，首次领奖前校验失败。差异只在旧事件选择历史的 `variables` 空对象与省略，非达尔弗候选或 RNG 差异。049 只归一化此空字段，非空变量和其他字段仍核对。候选回退时加速原生能力牌入场 tween 与雕刻师两处演出等待，抑制旧 Spine 动作过渡并恢复格挡图标布局。原生结束回合键的用户点击与长按回调在观看态被阻止，Back 恢复按钮状态并按原生可出牌条件刷新脉冲。事件、营火、领奖按钮走同一原生按下和释放，补回原生点击音。构建和合同通过，安装 `preparation/build-049-bb589599.json`，存档备份 `RUN005-post-build049-current-20261106-175848`。同一录像待短测，证据 `runtime-verification-r62.json`。

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
