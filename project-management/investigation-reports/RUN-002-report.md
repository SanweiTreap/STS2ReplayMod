# RUN-002：宝箱、商店、问号与SL尝试归属

## 最新摘要 · 2026-11-01 · r14货架待购买Stop/重开通过，商店代表样本收束

- 保持RUN-002/build-004，DLL `1C02153EFE00347294CBAADB2789ECDD304CCE495EE5C3C34FDB549047909A8F`。r14会话18db040e共190事件、0 observer-error/资源差异：先完成attempt4的raw14开货架，关闭Auto，在projected8待购买前Stop，未执行raw15购买；用户确认商品待选。9登记任务全部完成、0取消/故障，Error=null、原manager及所比较Continue/Progress恢复true。
- 同进程重开同一录像，首战Step到Position1后再次Stop，0登记任务/故障，原context恢复true。两次Complete/AllOperationsMatched=false均为计划内提前退出；整段原交易/删牌播放及SL改选后战PASS见r11/r13，不把短测当作整段胜利。
- 封存195数据文件，51当前文件4,396,386 bytes补存`RUN002-post-r14-current-20261101-015622`，相对r13全部逐hash不变、无缺失，root亦不变，game0、无restore/自行启动。证据`runtime-verification-r14.json`、`collection-r14.json`。当前原生测试进度仍为第4层完成未领奖point8，原宝箱/商店尝试及旧失败保留。
- 商店首增量按代表范围收束：真实商品/会员卡折扣、买卡/药水/遗物、删牌取消/确认、严格库存/价格/资源/RNG、入口SL改选后自然衔接后战、终点Auto关闭留场、货架待买Stop/重开均已有证据。README已同步稳定能力。删牌窗口中途Stop未单独覆盖，不用货架Stop代替该结论；首次商人教程、特殊遗物选择/嵌套自动购买、全部库存/价格组合等保持限制。
- RUN-002仍IN_PROGRESS，剩余问号实际房型、原生事件多阶段/选牌、内部战斗及返回和相关SL；不重跑宝箱/商店旧矩阵。阶段事实与剩余边界交接PM，不为普通后续候选追加审批。

## r13：商店SL最终路线至后战观看通过

- 保持RUN-002/build-004，无换版/重录。r13会话16826d3a共280事件、0 observer-error/资源差异，9项非战斗操作严格匹配资源/RNG/商店库存及价格，商店只执行attempt4的raw14..18，未执行旧raw7..13的会员卡/愤怒/药水/删牌。首战与后战各8步均原生胜利并匹配原胜利投影。
- 实际Continue.Progress的hash A8BEC876…88449在projected6、原生商店生成前应用到隔离manager，随后各操作用其实际Progress。r12首次/恢复入口的discovered_cards判别差异在严格比较中通过，没有忽略字段或改玩家列表。后战`InitialAndRngMatched=true`、`CheckpointLoaded=false`、`ExpectedStateInjected=false`，证实从中间实际新交易自然延续。
- 用户确认终点Auto关闭；Stop在Operation12、NonCombat=false、AtRecordedEnd=true。11登记任务全部完成、0取消/故障，AllOperationsMatched/Complete/VictoryReached=true、Error=null，原manager和所比较Continue/Progress恢复true，root逐hash不变。
- 封存285数据文件，51当前文件4,396,386 bytes补存`RUN002-post-r13-current-20261101-015132`；相对r12仅Progress.backup变化，新backup与r12已保存主Progress完全相同，主Progress及current_run均不变，无缺失，game0、无restore/自行启动。证据`runtime-verification-r13.json`、`collection-r13.json`。
- 下一做当前路线货架打开待购买时Stop及重开Step1/Stop短测，核商店窗口未关闭时退出。当前最终尝试没有删牌，旧删牌尝试保留原件；不为此新增历史尝试观看或改写选中路线。删牌窗口在途Stop尚未单独覆盖（取消/确认原生播放已r11通过），保持细分限制，不用货架Stop冒充。商店特殊遗物/嵌套自动购买、首次商人教程等未全覆盖；问号/事件内部战斗仍待，本包IN_PROGRESS。

## r12：商店入口SL改选及后战录制通过

- 保持RUN-002/build-004，无换版。r12会话ff3a0336共57事件、0 observer-error：真实Continue精确匹配point5商店入口，visit3新attempt4(parent3/restoredSave5)，旧13操作及原首战片均逐项/逐hash不变。新raw14开货架、15只购买另一张牌、16关货架、17前进、18地图、19后战均complete；默认最终路线[1..6,14..19]，旧会员卡/愤怒/药水/删牌仍保留事实而不选。
- 恢复后原生库存及原价与首次入口整串相同，删牌价恢复75，无旧会员卡折扣；新购CARD.BOLAS花178，1012→834金币。后战4929f45a共8步胜利，真实checkpoint为834金币/13张牌/5张打击，无会员卡、愤怒或力量药水，明确排除了旧交易和旧删牌，而非仅推断投影正确。
- 两次商店入口完整资源唯一差异为players[0].discovered_cards：首次有PROLONG/BOLAS，本次为空；库存、其他资源及RNG均相同。本次实际Continue.Progress已seen两牌，提供“房间生成前应用新环境”的判别样本；没有从expected改玩家发现列表或忽略字段。录制有证，观看该环境的应用与结果尚待验证。
- 新point6仍为商店入口，point7为下一战入口，point8为第4层完成未领奖。当前原生完整fingerprint精确匹配point8（PreFinished=true），834金币/13张牌；完整指纹、原件保留与资源细节见`runtime-verification-r12.json`。root及新后战生产读取/checksum、选中路线连续性通过，root实际2,646,712 bytes，在16MB上限内。
- 封存62数据文件，51当前文件4,396,358 bytes补存`RUN002-post-r12-current-20261101-014651`；latest.mcr、current_run及backup、Progress及backup共5项正常变化，无缺失，game0、无restore/自行启动。证据`runtime-verification-r12.json`、`collection-r12.json`、`checks/r12/`。
- 下一步同一82beed25从第2层Auto，核商店只执行新购BOLAS和原生离开、旧交易不播放，新房间生成时正确使用Continue环境，后战初态/资源/RNG自然匹配并到胜利终点，Auto关闭后Stop/正常退出；不用正常Continue或重录。之后仍有商店删牌窗口Stop/重开短测、问号/事件内部战斗等，本包IN_PROGRESS。

## r11：商店原生观看通过

- 保持RUN-002/build-004，无换版/重录。r11会话2c05ba77共170事件、0 observer-error/资源差异：82beed25首战8步原生胜利，随后11项非战斗操作严格资源/RNG匹配，其中包括商店库存/现价、会员卡确切实例原生领取、原生买愤怒/力量药水，以及删牌窗口取消和确认。未复制库存、发牌/改钱或手写折扣。
- 各操作实际Progress按正确SourceSequence/Attempt只应用到隔离manager；会员卡新增归属日志核确切Player和Entry.Model实例。r10实际记录中的折扣、库存变化及取消不扣钱/确认扣37均在观看后资源比较通过，不能仅凭UI动画判成功。
- 到Operation13关闭货架终点Auto=false、AwaitingStop=true、SceneKeptOpen=true，手动Stop AtRecordedEnd=true；15登记任务全部完成、0取消/故障，AllOperationsMatched/Complete/VictoryReached=true、Error=null，原manager及所比较Continue/Progress恢复true，root逐hash不变。
- 封存174数据文件，51当前文件4,375,351 bytes补存`RUN002-post-r11-current-20261101-013906`。相对r10有Progress.save.backup和settings.backup两项变化，无缺失；主Progress与current_run均逐hash不变，Progress.backup现与r10已保存主Progress完全相同（原backup是首战入口旧上下文），不宣称全部51文件未变。game0、无restore/自行启动。证据`runtime-verification-r11.json`、`collection-r11.json`。
- 下一步正常Continue同局，应回point5商店入口；只买另一张牌（不买会员卡/愤怒/药水，不删牌），关闭货架并前进到所连第4层最左普通战斗，获胜后暂不领奖、保存退出正常关游戏。核实际成功load锚点/独立尝试、库存及原价生成、旧raw7..13事实保留而不选、下一战不带入旧交易，再观看最终路线。本包IN_PROGRESS，删牌窗口Stop/重开及商店后战/问号等仍待。

## r10：商店交易录制通过

- 保持RUN-002/build-004，无换版。r10会话ea71b598共68事件、0 observer-error，真实新局82beed25，首战c7998d15共8步胜利自动封存。13项原始操作均complete，3访问/3尝试；首战领奖/跳卡、进入连接的第3层商店、开关货架和交易都已记录，无merchant-proceed，尚未SL或播放商店。
- 实际交易顺序是会员卡228金币、愤怒24金币、力量药水25金币，随后删牌取消和确认。会员卡原生折扣使删牌现价75→37，商店库存/价格快照已包含前后变化；金币1012→784→760→735→735→698。取消Selected=[]、Success=false，完整资源前后不变；确认Selected=[3]对应未升级打击、Success=true，扣37金币，牌组13→12。未自行模拟折扣或删牌。
- 当前原生完整fingerprint精确匹配成功point5：visit3/attempt3商店入口，PreFinished=false、路径[1..6]，金币1012、牌组12、无会员卡/所买愤怒，交易没有写进当前局内save。领取/购买时全局Progress的正常变化另存，不由“没点前进”或资源相似推断位置。root及首战生产读取/checksum、选中路线资源连续性通过，root实际1,768,369 bytes，16MB上限内。
- 72数据文件封存，51当前文件4,375,233 bytes补存`RUN002-post-r10-current-20261101-011819`；latest.mcr、current_run及backup、Progress及backup、settings.backup共6项正常变化，无缺失，game0、无restore/自行启动。证据`runtime-verification-r10.json`、`collection-r10.json`、`checks/r10/`，完整保存指纹与交易证据在本地，不公开原始数据。
- 下一步同版从主菜单82beed25第2层Auto到关闭货架终点，核原生会员卡折扣/买牌/药水/删牌取消与确认、资源/RNG/库存及终点Auto关闭留场，再Stop/正常退出。先保留当前选择路径作首次播放验证，不正常Continue或重录；通过后再用point5实测商店入口SL、相同/不同交易与下一战。本包IN_PROGRESS，问号/事件内部战斗仍待。

## 004：商店首增量实现与安装

- 宝箱首增量r6/r8/r9代表结论经PM接受，旧r4严格FAIL不追改；原文CURRENT_STATUS/TODO独立提交aae3318。RUN-002仍IN_PROGRESS，沿原授权继续商店/问号，不重跑宝箱旧矩阵。
- 当前RUN-002/build-004，DLL `1C02153EFE00347294CBAADB2789ECDD304CCE495EE5C3C34FDB549047909A8F`，manifest仍`69185E5024D560BC5C9B81FC64CC2F732745BCFBA31E64F069CAEF6AAEABE8FB`，收据`preparation/build-004-14623129.json`。显式兼容003原首战后战及旧root，001/002/003 pair均保留。主构建0警告0错误，9项分支/上下文与8项文件/位置合同、当前root和003后战生产读取通过，尚无004商店游戏结论。
- 原生NMerchantRoom打开/关闭库存/前进、NMerchantSlot.OnSelected与MerchantEntry及删牌独立Wrapper已接录制/播放；确认/取消结果和最终选牌沿共用NDeckCardSelectScreen，等待原生购买、Hook和UI任务。商店原生库存虽不在入口save内，加入严格资源快照核模型/卡牌详情/现价/库存；按索引和整库存定位后调用原生入口，不复制库存、改钱/发牌或忽略RNG。遗物新增只准当前已归属购买的确切Entry.Model和本地Player实例，其他增删保护保持。
- 实际Continue及操作Begin的账户Progress输入继续按既有因果边界只应用于隔离manager。商品自动套购/嵌套购买、非可取消特殊删牌和首次商人教程阻断离开会明确缺口，普通原生游戏仍继续；其他商店遗物特殊选择/效果不预认覆盖。问号/事件内部战斗仍未接。
- 新默认关闭准备入口`RUN-002 商店SL短路线`只消费下一新铁甲A0：录制checkpoint前准备40HP、1000金币、三张原防御+与两张新增全身撞击+先天保留，真实连接普通→商店→普通，首两普通为弱双蛞蝓；Continue/观看不再次准备。通过人工普通首战进入商店，合并买卡/药水/被动遗物、删牌取消及确认后保存退出；先核成功存盘真实位置，再做相同/不同交易的恢复尝试和原生观看，不预设购买持久化边界。
- 安装前最新恢复点`RUN002-post-r9-current-20261101-002534`保留；安装后51文件核验只有允许的settings.save配置变化、无缺失/进度变化，game0、无restore/自行启动，见`preparation/post-004-progress-check.json`。原正常测试局仍在第4层完成未领奖point10；新局由用户手动开启，原片和已封存证据不删。

## r9：选择中Stop/重开通过，宝箱首增量代表样本收束

- 保持RUN-002/build-003，DLL `1AD070C28A881D7E9ACA8A7F13EFFD5F975F56217C6ECB8A43F8672AECB15C08`，无换版/重录。r9会话bd4ca1a6共183事件、0 observer-error/资源差异：先完成raw15原生开箱，在projected9待跳过前关闭Auto并Stop，未执行raw16；用户确认白星仍待选。10登记任务收束（1取消/0故障），原manager及所比较Continue/Progress恢复true。
- 同进程重开同一root，首战Step到Position1后再次Stop，0悬挂登记任务/故障，Error=null、原context恢复true。两次Complete/AllOperationsMatched=false均为计划内提前退出，不作整段失败或胜利。整段领取与不拿后战播放的PASS分别见r6/r8。
- 188数据文件封存，51当前文件4,395,380 bytes补存`RUN002-post-r9-current-20261101-002534`；相对r8全部逐hash不变、无缺失，root亦逐hash不变，game0、无restore/自行启动。证据`runtime-verification-r9.json`、`collection-r9.json`。旧r3/r4失败及旧候选/原片保持。
- 宝箱首增量可按代表样本收束：入口存盘/真实连续Load、相同选择独立尝试与改选不拿、最终路线不重复领取/金币、账户发现输入与严格资源/RNG比较、下一战自然衔接、终点Auto关闭、待选Stop/重开均已有证据。白星之外遗物、领取附加奖励/嵌套选牌、多玩家/所有箱子组合仍未测；非战斗Back/跨房间Seek及完整经历模式未开放。
- 已同步稳定能力README；本包仍IN_PROGRESS，后续同包接商店购物/删牌/取消与SL、问号实际房型、事件多阶段/选牌/内部战斗及返回，不要求重复本轮首战长测。原生当前进度保留在第4层已完成未领奖point10，后续准备前继续补存，不自动还原旧局。

## r8：不拿分支至后战观看通过

- 保持RUN-002/build-003，DLL `1AD070C28A881D7E9ACA8A7F13EFFD5F975F56217C6ECB8A43F8672AECB15C08`，无换版/重录。r8会话08122d67共260事件、0 observer-error/资源差异：旧首战8步和第4层新战7步均原生胜利与原胜利投影匹配；8项非战斗操作严格资源/RNG匹配，宝箱采用attempt6的raw15/16/17，只开箱/跳过，不串入旧领取。
- Load上下文在projected7原生入房前应用，随后三项操作用各自实际Progress；下一战`run-next-battle-initial-verified`通过，InitialAndRngMatched=true、CheckpointLoaded=false、ExpectedStateInjected=false。结合r7的163金币/无白星真实checkpoint，本代表样本证实最终路线从原生中间操作自然延续，不靠后战checkpoint覆盖结果。
- 用户确认胜利终点Auto关闭。Stop日志Operation11、NonCombat=false、AtRecordedEnd=true；12登记任务收束（1取消/0故障），两场共15步执行，AllOperationsMatched/Complete/VictoryReached=true、Error=null。原manager及所比较Continue/Progress恢复true，root逐hash不变。
- 265数据文件封存，51当前文件4,395,380 bytes补存`RUN002-post-r8-current-20261101-001207`，相对r7全部逐hash不变、无缺失，game0、无restore/自行启动。证据`runtime-verification-r8.json`、`collection-r8.json`。r4严格FAIL、r6领取观看及r8不拿后战观看分别保留，不外推其他遗物/附加奖励。
- 宝箱核心领取/不拿、连续真实Load产生独立同内容与不同选择尝试、最终路线不重复资源、后战状态/RNG已有代表证据；尚需在原生白星待选时Stop再重开短测，验证本包新增选择退出边界。之后接同包商店/问号及内部战斗等，RUN-002仍IN_PROGRESS，不作整包收束。

## r7：不拿分支及下一战录制通过

- 保持RUN-002/build-003，无换版。r7会话0dfdf7c2共51事件、0 observer-error：Continue精确匹配宝箱入口point7，同visit3新增attempt6(parent5/restoredSave7)。新raw15开箱、16跳过前进、17地图、18第4层战斗均complete；没有新treasure-pick。选中路线[1..7,15..18]，旧raw1..14逐项与原首战片逐hash不变，4访问/7尝试，宝箱访问entries4。
- 开箱金币117→163，跳过后仍163且无白星。真实下一战ENCOUNTER.CORPSE_SLUGS_WEAK，片8218896a共7步胜利；其原生checkpoint为163金币且无白星，不是仅从选中路线推测没带入旧领取。原首战/新后战及root生产读取/checksum、选中路线资源连续性通过；尚未观看不拿分支。
- 新成功存盘point8仍宝箱入口，point9是下一战入口，point10是下一战已完成/未领奖，当前原生完整fingerprint精确匹配point10，163金币且无白星，PreFinished=true，所选路径保留至raw18。原始完整指纹见`runtime-verification-r7.json`；root实际1,575,034 bytes，16MB上限内。
- 56数据文件封存，51当前文件4,395,380 bytes补存`RUN002-post-r7-current-20261101-000940`；原生latest.mcr、current_run及backup、Progress及backup共5项正常变化，无缺失，game0、无restore/自行启动。证据`runtime-verification-r7.json`、`collection-r7.json`、`checks/r7/`。r4旧FAIL与r6领取观看PASS均保持。
- 下一步主菜单同一b0d06809从第2层Auto至第4层胜利，观察只开箱/跳过一次、不领取白星、能继续进入后战，终点Auto关闭后Stop并正常退出。先核最终路线原生结果及退出隔离，不做正常Continue/领奖，以免把其他操作混入本短样本。本包仍IN_PROGRESS，宝箱其他Stop/细分覆盖以及商店/问号等未结项。

## r6：原生领取观看与上下文修复通过

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
