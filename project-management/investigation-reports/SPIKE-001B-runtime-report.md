# SPIKE-001B runtime 记录

创建2026-10-22，收尾交付2026-10-23（Asia/Shanghai，跨午夜）。当前：`CLOSED / SEMANTIC_EQUIVALENT / READY_FOR_PROJECT_LEAD_REVIEW`。依最新handoff runtime节与PM `ACCEPT_PHASE0_R2 / AUTHORIZE_TWO_ACTION_RUNTIME` 完成实际两操作A/B：三点逐字节相等、差异0，两端各两条accepted/完成。正式已Arm **1/3**；已有有效答案，不再追加运行。用户已正常退出，probe已持久禁用，exact本包pair已移除，候选/源码/private及公开证据保留；待PM独立验收。

## 交付摘要

- 唯一正式attempt `4f958fdc207f4b9ba83ede826848f9ed`；seed EBJVET20T859、Standard单人Ironclad A0、Act1 SHRINKER_BEETLE_WEAK、row1col1，CP Floor1→playable Floor2。A process `71984:639282781797626835`，fresh B `110832:639282809090047208`。两次用户均现场确认原账号/Cloud关闭，实际only SPIKE001B/modded1。
- **结果Confirmed：Initial / PostCard / NextTurn三点Round1/1/2全部相等，A/B各两条实际accepted且完成，B intent2/accepted2/完成2。** 完整hash/commit/window、逐点早停门与同CP/fresh provenance均核对；不是synthetic合同或画面动画代替运行结果。
- runtime候选未改：DLL200192 bytes / `F68D3510E331A9661BE2F2E387CC77D266E9EDA0A41FEA40F9E0F1C320075EE7`，manifest367 bytes / `22F4DAFF17705B748B69C6EB16EA089D361D868AA373266CF8F3C185A6AFDD65`。复用已验Release0 warning/error、109/109、7/7 patch/unpatch、两种dummy和159引用审计；因业务source/candidate未变，不重复构建/跑离线合同。外部核验脚本的null绑定/BOM解析问题已记录并修正，未换binary、未消耗额外Arm或降低任何门。
- 公开attempt92份，归档均与private白名单原件hash相同。A退出、B启动前native与seal45122 bytes逐字节相同；B退出后观察到NumReloads0→1，属于预声明正常Continue reload窗口，不要求再同A字节、不restore。
- 用户确认B游戏内保存退出→主菜单退出、无异常；game0。本包2个精确安装文件已移除、workspace可恢复，18个其他项目旧文件保持；29份剩余installed manifests均明确disabled。下次启动预计无有效mod并进入unmodded树，这是配置推断，未启动验证或读取个人槽。
- 范围只证明上述房间/两操作/冻结投影。其他seed/内容、完整战斗、性能、确定性覆盖、导航/纠偏、独立Replay会话与个人槽安全仍Unknown，未解锁PERF/DET或产品能力。建议PM独立验收本样本，再决定后续范围；Implementation不自动推进。
- 交付索引：`evidence/spike-001b/runtime-delivery-2026-10-23.json`；只读复核：`evidence/spike-001b/runtime/Verify-RuntimeDelivery.ps1`；收尾：`runtime/closeout-ad1367c17b53478eb88b574c2d908ccf.json`。后文按时间记录历史门，早期“待完成”状态不覆盖本摘要。

## 原账号配置与精确安装

执行 `evidence/spike-001b/runtime/Prepare-R2.ps1`；收据 `preparation-f773e618364d4f069e1fae7685e8c508.json`。

- 固定v0.111.0/41cef1ea、sts2.dll 9757184 bytes / SHA256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`、Harmony2.4.2.0通过；准备前后game=0。
- exact r2 pair create-new安装：DLL200192 bytes / `F68D3510E331A9661BE2F2E387CC77D266E9EDA0A41FEA40F9E0F1C320075EE7`，manifest367 bytes / `22F4DAFF17705B748B69C6EB16EA089D361D868AA373266CF8F3C185A6AFDD65`。workspace sealed pair未改，schema2/private phase0-r2；18个其他旧probe文件长度/hash不变。
- 当前Steam ActiveUser唯一对应目录，与已知A5成功attempt的native-path.private.json中的原账号/modded1路径metadata在内存匹配。仅读取该metadata，不读A5 raw checkpoint、当前native或另一账号settings/selector/payload；账号数字和私有绝对路径未导出。
- 原settings先create-new完整备份到SPIKE001B私有config-backups，backup hash与before一致；只在mod_settings中启用本地SPIKE001B、禁用其他当前installed/退役probe、补齐必要记录。原记录保留，其他mod_settings字段及非mod canonical值不变。after实际installed manifest30份，全部(id,source)有显式配置，唯一有效enabled为SPIKE001B。
- modded/profile.save已经last_profile_id=1，未写；selector hash保持 `9739AD8352E422CDD47C1BDD54CD0895CF37B5473BB0D7F9BA7F1AA0D5630829`。未读取unmodded selector或个人unmodded1。
- settings before hash `3A2AAE02D6C28F0736D20BB6C25E60862E73720BD008188891A8C569E3015A15`，after hash `81781BD0B74C8AC25B0BB4ABBE3021AABDB183BC42A504A30E9F2D9E03745476`。写前重新校验进程/原hash，写后完整范围回读通过；完整配置和备份只留private。
- 本candidate journal尚不存在，下一次为fresh A；没有复用A5或其他attempt。

## 当前解锁的人工步骤

没有待执行游戏步骤。最后实际请求 `evidence/spike-001b/runtime/manual-request-B1-exit-2026-10-22.md` 已由用户完成并报告无异常；游戏进程0，探针禁用/精确pair清理和公开封存已完成。等待Project Lead独立验收，不请求新游戏测试。

本次用户现场确认原A5账号、启动前Cloud关闭、只停主菜单且未Continue/新建局/F8。实际loaded/Ready由本进程证据独立核对，不以安装或持久配置替代。前一实际请求见 `evidence/spike-001b/runtime/manual-request-A1-main-menu-2026-10-22.md`。

后续按合同逐步核对三点/两accepted/完整A commit与ready proof→用户正常退出→native同seal bytes→fresh B主菜单→用户Continue一次后probe自动两操作分派。任一有效差异停止后续分派，不为追求等价重测；不restore native、不自动解锁PERF/DET或产品能力。结果与正式预算将在观察后追加。

## A1 主菜单预检通过

- 本进程PID71984，启动UTC `2026-10-22T15:02:59.7626835Z`，process identity `71984:639282781797626835`；attempt `4f958fdc207f4b9ba83ede826848f9ed`。固定游戏、Harmony及exact r2 installed/workspace pair通过。
- 本attempt的schema2/phase0-r2 public事件为first-tick 522ms、ready-A-arm 560ms，绑定上述process与候选hash。未出现arm、terminal或activation journal；正式计数仍0/3。两份事件create-new归档于 `evidence/spike-001b/runtime/records-4f958fdc207f4b9ba83ede826848f9ed/`。
- 当前godot.log支持SPIKE001B initializer成功、Loaded1、唯一B、modded/profile1和 `PRECHECK_PASS READY_A_ARM`；当前日志账号token、Steam ActiveUser与已知原A5 metadata scope在内存匹配。未导出账号数字/私有路径或raw log。只按用户本次现场确认记录Cloud关闭，日志中的SteamRemoteStore调用不独立证明Cloud客户端设置。
- 初查时当前godot.log尚未flush，轮转文件的名字/mtime与内容身份不一致；其旧A5内容已明确排除，未用于本门。当前日志flush后快照SHA256 `9162F329E05C88EEF2ECC165067386DFAA3947E4F8498D1FBF59CB01C1B0BC67`。已批准manifest的SemVer warning存在，但initializer/实际加载/Ready均成功，候选未改。
- 核验脚本 `evidence/spike-001b/runtime/Verify-A1MainMenu.ps1` 和收据 `precheck-pass-4f958fdc207f4b9ba83ede826848f9ed.json`。本门尚未读取当前native、旧raw checkpoint或个人/另一账号数据，未自行启动/操作游戏；只解锁A新局到地图的准备。

## A1 到图后Arm前复核

- User-observed runtime result：已到Act 1地图，未点节点、未按F8，游戏保持运行，未做其他操作。
- `Verify-A1Map.ps1`通过；收据 `map-prearm-pass-4f958fdc207f4b9ba83ede826848f9ed.json`。仍为同一process `71984:639282781797626835`，exact候选/manifest、原scope的modded1/actual only B和首帧/Ready事件hash保持；当前日志无B拒绝/终态/READY_A_NODE，private无arm/terminal/第三事件/commit/claim/journal。正式Arm仍0/3。
- 本门未读native payload，地图到达属于用户报告；Standard/Ironclad/A0/Act1及live地图的最终校验仍由候选Arm内门负责。只请求F8一次后停手，不提前请求节点或卡牌。

## A1 Arm与唯一目标门通过

- User-observed runtime result：已按F8一次，仍停地图，未点节点，保持运行，未见异常。
- `Verify-A1Arm.ps1`通过；收据 `arm-pass-4f958fdc207f4b9ba83ede826848f9ed.json`。同process/candidate，actual only B/modded1及当前原账号scope保持；arm schema2/phase0-r2、armed事件、唯一activation-0001及日志READY_A_NODE一致，无terminal、seal、production-save或初始point文件。
- Arm记录：seed `EBJVET20T859`，CHARACTER.IRONCLAD/Act0，前驱row0 col3 / finished-room / singleplayer / floor1；目标row1 col1 / ArmFloor1。前驱hash `5B31A5D4A56D7FC34879FF725130C991206F3BA6D6319384C36A37FC08B58E52`。Arm代码先校验Standard/Ironclad/A0/Act1及live地图，再在直接子节点普通Monster中按row/col选首个；目标因此用“起点直接连接第一排普通怪物中最左侧”唯一描述，无需固定像素或截图。
- 正式计数 **1/3**，本attempt首次Arm。arm.json、A.event-003.json和activation-0001.json以create-new/hash归档到本attempt records目录；未导出native-path或raw checkpoint，本核验脚本未读native payload。候选Arm按授权只读原测试modded1的当前前驱，尚未建立room-before seal。
- 下一步只点击上述节点一次后停手，等待seal、初始point0及READY_A_CARD。不得提前出牌、EndTurn、暂停或领奖。

## A1 room-before seal及初始稳定点通过

- User-observed runtime result：起点直接连接第一排最左侧普通怪物仅点击一次，随后保持运行/聚焦，未F8、出牌、EndTurn、暂停、药水或领奖，未见异常。
- 同process/candidate，匹配 `MATCHED_SAVE_TASK_WRAPPED`；production-save-start→seal-start→checkpoint-sealed→A-capture-start→capture-30-frames→point-captured事件顺序完整。private checkpoint在内存hash与seal一致：`98667861E129F8E3AD182520ED2E227E7D75D2DC7002EAD9330F2A080CEABF45`，room-before=true、singleplayer、row1col1、CP Floor1 / playable TotalFloor2。
- point0为 `ENCOUNTER.SHRINKER_BEETLE_WEAK` / Monster / Round1，semantic7520 bytes / SHA256 `BBBD2D4C1B71FCB0CBF8747224717A888137C377569ACD654074BF54E58E8E08`。窗口frames41796–41825，30相邻帧、20字段全部True，235ms跨度，MaxGap9ms≤250ms；capture SHA256 `E5958D391915F11BD758C2A66348313615E332337BCA2AD5FF4A0385959583AC`。无terminal/action-1/post-card/complete/ready-B，未进行任何B。
- `READY_A_CARD hand_index_0based=1 model=CARD.DEFEND_IRONCLAD target=Self:null` 与pure Core Select/Resolve回读一致。当前hand依次Strike、Defend、Strike、Stomp、Strike；唯一指定第2张，未升级、1费、5格挡，无affliction/cost modifier/keyword/replay等不合格属性。完整hand hash `5563216C04D2A9A2A1588D534F4EE4C55FBB7E500B78F26CC09D3CC91FF8D2A1`，card fingerprint `9CF13CCCB12D4C8E25C5B961B6BBED77DDBF07127A07DDFDA620AC55C2E4AFDC`。
- `Verify-A1Initial.ps1`及收据 `initial-pass-4f958fdc207f4b9ba83ede826848f9ed.json`；seal、point0 semantic/capture和A.event004–010公开白名单共10文件create-new归档。private checkpoint只读hash、不导出，不读取当前native/另一账号/个人槽。外部核验只调用纯Core/metadata方法，没有生产API或Godot操作。
- 外部核验脚本首次因PowerShell将nullable string实参$null转换为空字符串，被Contract.Playable的room-model条件拒绝；已将外部调用改为本次已观察到的明确room/encounter值后通过。候选、游戏、运行记录和验收标准均未改，此为离线核验调用绑定问题，不是probe终态或新增正式Arm。
- 下一步只请求上述第2张Defend一次，等待真实accepted/action-1和point1/READY_A_ENDTURN；不能仅根据动画/手牌减少判断可EndTurn。

## A1 CardPlay accepted及出牌后稳定点通过

- User-observed runtime result：当前从左第2张防御仅打出一次并停手，保持运行/聚焦，未EndTurn/其他牌/F8/暂停/药水/领奖，未见异常。
- `Verify-A1PostCard.ps1`通过；收据 `post-card-pass-4f958fdc207f4b9ba83ede826848f9ed.json`。同process/candidate；原seal与point0的private/公开hash保持。A.action-1为seq1/CardPlay/Initial/Round1/PlayerTurn1，绑定指定index1/full-hand hash/card fingerprint/Self:null及point0 BeforeHash，SHA256 `677448C4AAF59C7B4780440725650E564051A709AB4CAE22819188BC7D7CB1E8`。
- A.event011–020记录Queue binding→Requested→BeforeExecuted→CardEffect→AfterFinished→action-accepted-success→point-capture-start→30frames→point-captured。最后accepted诊断Requested/Executed/CardEffectSeen/Finished=true，原effect Task最后观测RanToCompletion(5)、ProductionExceptionObserved=false、无cancel/choice/rejection。该证据连同ActionBridge已固定接受条件支持实际成功，不以intent/通用Finished或画面变化代替；诊断Round/PlayerTurn头部是绑定的前状态，非回调时重新live采样。
- action请求1608547ms，accepted1608745ms，point1提交1609000ms，均处于各20s门内。post-card semantic7519 bytes / SHA256 `0C61F1495F3139352A2ECB13B9FA01AA8E78B01B007141B1C1907E297C850D64`，capture SHA256 `37F594E7A5473A2FFD2F2A305873BCC7403D25DECD22488FC5AA992448978ACD`。Round1、same row1col1/floor2/SHRINKER_BEETLE_WEAK，frames51153–51182、30相邻帧、20字段全True、232ms、MaxGap11ms。
- 观察到HP80、Energy2、Block5，hand剩Strike/Strike/Stomp/Strike、discard含Defend；这些是语义结果，accepted结论来自上述原Action/效果证据。纯Core trajectory回读两点及action1完整顺序通过；下一条计划seq2/EndTurn/PostCard/Round1/PlayerTurn1、Card=null、BeforeHash=point1。
- 日志唯一READY_A_ENDTURN，无terminal/action2/point2/A complete/ready-B/B claim。本步白名单action1、point1 semantic/capture及events011–020共13文件create-new归档；未读取或导出private raw。正式1/3，accepted1/2，A点2/3，B仍未运行。

## A1 EndTurn accepted、Round2及完整A提交通过

- User-observed runtime result：正常点击EndTurn一次并停手，保持运行/聚焦，未再出牌/EndTurn/F8/暂停/药水/领奖，未见异常。
- `Verify-A1Complete.ps1`通过；收据 `A-complete-pass-4f958fdc207f4b9ba83ede826848f9ed.json`。同process/candidate及所有前驱已归档文件hash保持，A.action-2为seq2/EndTurn/PostCard/Round1/PlayerTurn1、Card=null、BeforeHash=point1，SHA256 `062F1823180E2132D4FB185A7B6CC6F743E8A8BC16A2C345A0FFD4D25CEC2F7C`。
- A.event021–033完整记录Queue binding→Requested→BeforeExecuted→EndReady→AfterFinished→action-accepted-success→point-capture-start→心跳→30frames→point-captured→trajectory-complete。EndReady真实false→true及same running Action/owner/scheduled turn关系由固定ActionBridge接受条件保证；最后accepted诊断Requested/Executed/EndReadySeen/Finished=true、ProductionExceptionObserved=false、无cancel/choice/rejection。EndTurn没有CardEffect Task，null为未适用，未冒充效果失败。两动作合计10次观察，≤32上限。
- EndTurn请求1821756ms、accepted1821770ms，point2提交1824697ms；各20s门内。point2 semantic7559 bytes / SHA256 `E37A7DC38A021526253415C28B832165A6916962269C55F0125113B096D44023`，capture SHA256 `93079F7DCC35AF0C3ACEF446781ACBD103067824400190194A7822E66A30A226`。Round2、same row1col1/floor2/encounter，frames58097–58126，30相邻帧、20字段全部True、492ms跨度、最大相邻间隔34ms≤250ms。Round2来自独立投影/readiness；动作诊断头部仍正确保留操作前Round1/PlayerTurn1。
- A.complete SHA256 `90C43D54D6AF684DB4A561FE0AEC9457439DD2BFAFDFFC390E01AF00724D4F99`，绑定same candidate/process/attempt/CP及3 snapshot、3 capture、2 action的完整hash；A.ready-B SHA256 `59B1FB763352CCFD1E94AF845900340F625158E790C973E5F44CFD1D3069CFB3`的CompleteHash与typed serialize及原complete bytes均一致。所有window/phase/Round1,1,2及pure Core ValidateA完整链通过。
- 日志唯一A_COMPLETE_STOP_INPUT、trajectory-complete，无terminal/B claim/dispatch/action。新增18份白名单文件create-new归档；private raw/path/current native本步均未读取或导出。**A完成：3/3点、2/2 accepted；B比较0点、分派0、accepted0，不能提前称轨迹等价。**
- 当前只请求按游戏内保存→主菜单→退出游戏正常结束A。退出之后必须进程0并验证当前native/private CP仍为seal45122 bytes及exact bytes，候选/完整commit/原scope保持才fresh B；当前该退出后输入门仍待完成，不restore/rebaseline。

## A退出、有限配置补齐及B输入门通过

- 用户在正常退出请求后先误发标点，随后回复“好了，我发错了”；按其上下文记为完成回传，未虚构逐步退出路径/异常的额外陈述。Implementation独立确认游戏进程0；此前请求的退出路径是游戏内保存退出→主菜单退出，未自行操作游戏。
- `Verify-A1ExitInput.ps1`首次在scoped-config严格31条计数处停止，尚未写入：退出后配置只保留20条，11条原disabled记录缺失，无新增记录、无保留记录开关变化，mods_enabled=true/selector1保持；原生存档exact-byte门已通过。此处不把配置记录缺失视作业务输入变化或清理另一账号；未调查游戏重写配置的具体根因。
- 按最新handoff明确允许的B同范围配置回读/修正，执行 `Reconcile-B1DisabledRecords.ps1`；收据 `B1-config-reconciliation-fc8c218dc16c4046baa626b723dbcf15.json`。先create-new完整private settings备份并核hash，仅向当前20条末尾补回准备收据中的11条disabled(id/source)记录；现有记录全部字段、其余settings canonical、selector及CP exact bytes不变。未启用旧probe、未写native/A journal/业务证据、未换candidate。完整设置/备份/私有路径不公开。
- 随后完整重跑退出输入门通过，收据 `A-exit-input-pass-4f958fdc207f4b9ba83ede826848f9ed.json`。game0、固定游戏/Harmony/exact installed与sealed pair、原Steam ActiveUser与B/A5已知path metadata scope、only有效配置B/modded1均通过。18个其他旧probe文件长度/hash保持；A公开46/46文件与private白名单原件及前驱收据全部匹配，唯一activation保持，无terminal/B claim。
- 当前原账号modded1 native连续两读与private checkpoint逐字节相同，均45122 bytes / SHA256 `98667861E129F8E3AD182520ED2E227E7D75D2DC7002EAD9330F2A080CEABF45`；配置补齐前后再次检查相同。A.complete/ready proof仍原hash。没有restore、rebaseline、读取旧raw checkpoint、个人槽或另一账号。
- 此结果仅证明A seal→两操作→正常退出后输入内容保持，未声称production零写入或mtime恒等。fresh B正常Continue的reload保存属于另一个预声明写入窗口；之后不要求native继续等于A原字节。
- 正式1/3、A三点/两accepted完整，B0比较/0分派/0accepted。仅解锁fresh B主菜单预检，本次Cloud仍需用户启动前现场确认；未提前执行Continue或将A完整称作A/B等价。

## B1 fresh主菜单预检通过

- User-observed runtime result：原测试账号手动重启到主菜单，启动前现场确认Cloud关闭，保持运行；未Continue/新建局/F8/切profile/改设置，未见异常。Cloud只按本次用户报告记录。
- 新PID110832，启动UTC `2026-10-22T15:48:29.0047208Z`，process identity `110832:639282809090047208`，与A不同。固定游戏/Harmony及exact r2 pair通过，本进程日志actual only SPIKE001B、Loaded1、modded/profile1，profile账号token与当前原SteamActiveUser/本attempt私有scope在内存匹配。
- 同attempt唯一B.claim在fresh process绑定，73 bytes / SHA256 `E3C652DEF05F4C17013EEFF2320840FF85E3CB6CC2856368F26564733FCBB1B5`。B first-tick560ms、ready-B-continue582ms，schema2/phase0-r2/candidate/process/attempt一致；唯一READY_B_CONTINUE，无fresh A marker、terminal、point0/dispatch/action/complete。
- `Verify-B1MainMenu.ps1`与 `B-precheck-pass-4f958fdc207f4b9ba83ede826848f9ed.json`通过。A全部46白名单原件/公开hash保持、唯一activation不变；本次主菜单下native双读与private CP仍45122 bytes逐字节一致，hash仍 `98667861E129F8E3AD182520ED2E227E7D75D2DC7002EAD9330F2A080CEABF45`。候选只有完成BInput全部验证后才create-new B.claim，原完整A门未跳过。
- claim及两个事件共3公开白名单文件create-new归档。当前日志快照SHA256 `DC3315EFFA3C345DB52490670432F5FCD67B1D230A1AF27CBE0DE8F96BCC9E46`，未归档raw log或private path/checkpoint，未改配置/native。
- 当前B比较0、dispatch intent0、accepted0。只请求正常Continue一次并停手；其后的CardPlay/EndTurn由probe正常逻辑API自动分派，逐步相等才继续，任何有效差异停止后续分派，不以PASS为由重跑。

## B1 完整两操作轨迹：SEMANTIC_EQUIVALENT

- User-observed runtime result：主菜单Continue仅点击一次后停手并保持运行/聚焦；未F8、手动出牌/EndTurn、暂停、药水、领奖或改设置，未见异常；另报告自动防御和EndTurn正常运行。用户观察与机器accepted/compare/commit证据分别记录。
- `Verify-B1Complete.ps1`通过，收据 `B-complete-pass-4f958fdc207f4b9ba83ede826848f9ed.json`。同fresh B process110832/exact r2/同attempt及原CP；A全部46记录与B claim/首帧/Ready共49前驱文件保持。B实际运行新增43份公开白名单create-new归档，完整attempt公开记录92份，raw checkpoint/path/current native均未在本步读取或导出。

| 稳定点 | Round | A/B semantic bytes | 比较差异 | A/B最大相邻帧间隔 |
| --- | --- | --- | --- | --- |
| Initial，操作前 | 1 | 7520 / 7520，逐字节相同 | 0 | 9 / 9 ms |
| PostCard，防御后 | 1 | 7519 / 7519，逐字节相同 | 0 | 11 / 9 ms |
| NextTurn，下一玩家回合 | 2 | 7559 / 7559，逐字节相同 | 0 | 34 / 9 ms |

- 三组A/B SHA256分别完全相同：point0 `BBBD2D4C1B71FCB0CBF8747224717A888137C377569ACD654074BF54E58E8E08`，point1 `0C61F1495F3139352A2ECB13B9FA01AA8E78B01B007141B1C1907E297C850D64`，point2 `E37A7DC38A021526253415C28B832165A6916962269C55F0125113B096D44023`。独立byte comparison、SHA256及pure Core Compare均通过，不只信Result字符串。三份compare绑定same candidate/attempt/A+B process/CP，Differences=[]。
- B三个capture分别frames6298–6327、6347–6376、6660–6689；均30相邻帧、20字段全部True、max gap9ms，Round1/1/2、same room/encounter/row1col1/playable Floor2。对应capture SHA256为 `3CF2F3EDF9C6058C779D9B57DB546310E931CB9E9DA06F1FC5A44426FA78712B`、`4A8335403660E6B93456EF2F9803599598B4E7756194077DAD0A339860D8A945`、`F0E59C4D5940B796081AE5EC1D83CC6C3F3188E6F1C92283DA9924266CF55B76`。
- B.dispatch-1/2各存在一次，完整操作内容与A.action-1/2以及B.action-1/2相同；完整hand slot/order/fingerprint/Self:null及操作前hash保持。B accepted action SHA256分别 `0E3895F5C8D8047711199E0C7B5332278C2D7620EBA538888368C9EE0181608A`、`7330F76F7BBB4E9DFEA50FEAB52B3529DB5434FE0180D464675DB5379FCD469C`。**计数分别为：B intent2、实际accepted2、完成2；A accepted2、完成2。** 不把B.complete内DispatchedOperations字段或dispatch文件数单独当作accepted。
- CardPlay存在BeforeExecuted/CardEffect/原effect Task RanToCompletion/AfterFinished成功；EndTurn存在BeforeExecuted/真实End-ready转换/AfterFinished成功。两条均Exception未观察到、无cancel/choice/rejection，诊断10次≤32，header仍是绑定操作前Round1/PlayerTurn1。初始point-captured146471ms→Card accepted146670ms→post-card captured146927ms→End accepted146954ms→Round2 captured149583ms，顺序及各20s门通过；每条分派均在前一点相等之后，用户没有补输入。
- B.complete950 bytes / SHA256 `2EB7140EB9A78E2FC74F3596CEA93289383B6FCB755E9E0ACD8FF0F239B42262`，3snapshot/3capture/2action全部hash与原件一致，ComparedPoints=3、DispatchedOperations=2、Result=SEMANTIC_EQUIVALENT。唯一trajectory-complete及B_COMPLETE_STOP_INPUT SEMANTIC_EQUIVALENT，无terminal/额外dispatch/action。pure Core按实际A/B文件重验完整顺序通过。
- 结论限seed EBJVET20T859、Standard单人Ironclad A0、Act1 SHRINKER_BEETLE_WEAK、一张Defend及一次EndTurn、冻结语义投影。不能外推任意seed/房间/卡/完整战斗、确定性覆盖、session isolation、PERF/DET或正式产品能力。正式1/3，有效答案已得到，剩余预算不再使用。
- 当前只请求B正常退出，随后离线禁用/精确清理本包pair并封存索引、交PM验收。B Continue已经进入预声明reload保存窗口，不以当前native!=A raw作为失败，不restore。

## B正常退出及离线收尾

- User-observed runtime result：已按游戏内保存退出回主菜单，再点退出游戏，未做其他操作，退出过程未见异常。Implementation独立核对game0；未自行启动、控制、Alt+F4或强杀游戏。
- `Closeout-B1.ps1`在game0、原scope、92/92 whitelist、完整A/B及exact live/workspace pair门通过后，先create-new完整private settings备份；仅改允许的mod_list启用布尔/恢复准备前global mods_enabled，保留当前记录及其他所有字段canonical。依据准备时实际settings备份恢复非probe开关（原本全disabled），所有SPIKE退役项保持disabled；缺失disabled记录补回。没有全settings restore，没有改selector/native/journal/source或其他账号。
- 持久禁用完成后，仅非递归移除 `mods/SPIKE001B.dll` 和 `mods/SPIKE001B.json`；每次删除前重验绝对parent为固定mods目录、game0、exact长度/hash及workspace sealed可恢复。两个目标现不存在，18个其他项目旧probe文件仍原长度/hash；workspace/source/sealed pair及所有证据保留。
- 原收尾脚本在末尾installed-inventory遇到JsonReaderException：该外部scanner漏掉Prepare-R2已使用的UTF8 BOM trimming。保留原脚本及一次性执行状态，**没有重复禁用/删除**。新增 `Finish-B1Closeout.ps1`只读回验补齐后置条件、再写公开receipt；相关中断记录 `closeout-interruption-ad1367c17b53478eb88b574c2d908ccf.json`，最终收据 `closeout-ad1367c17b53478eb88b574c2d908ccf.json`。此问题不涉及game/probe、额外正式attempt或新binary。
- BOM-aware盘点29份剩余manifest，全部与明确disabled记录对应，唯一SPIKE001B记录也disabled。另有独立外部内容漂移：未加载的Workshop `AutoAnthony` manifest从 `A7C55ED32E24D80508C7F30433018A1BD96663074BA922284378C35B178FF495` 变为 `E0885A4B7D472E2BB0EA112EB6E541203B835209C3F6B7E16ADF55EF71CDBF3C`；本项不含BOM，与scanner缺陷是两个事实。其准备/收尾均disabled，A/B实际loaded集合不含它；Implementation未写Workshop内容，不归因于probe或声称所有外部manifest不变，也不扩大调查/补跑。
- 收尾settings before SHA256 `A96874B895012E4481C9A6F6656EF7A39D6AC2AEEFE47C961E71CCB115E4567A`，after `42325CB3760063A83A9DAD3158A2FAE2C2F980B1DF235C291E6BE6263FB85D14`。Global mods_enabled=true、全部installed/retired flags=false；selector未写且仍1/hash `9739AD8352E422CDD47C1BDD54CD0895CF37B5473BB0D7F9BA7F1AA0D5630829`。完整配置/backup/path只在private。
- B退出后native45386 bytes / SHA256 `FF209A377EEC75608B3F1C0F66572970F2542CEDB4F1B49AA95A197CEBA1A1AB`，NumReloads在seal为0、此时1，内容与A seal不同是预声明正常Continue后的观察；没有将其扩展成“全部差异仅此字段”的断言。private CP仍45122 bytes/原hash，最终native连续读稳定，未restore/rebaseline或写native。
- 收尾会话从10月22日跨至10月23日00点；运行receipt中Date=2026-10-22是本轮session归属，不替代文件时间/实际进程StartUTC。本报告/最终交付索引采用10月23日交付日期。

## 写入窗口、证据边界和预算

| 窗口 | 授权与实际观察 | 解释限制 |
| --- | --- | --- |
| 离线准备 | 原账号有限mod配置、exact pair安装；private完整备份 | 不把持久enabled当actual loaded |
| A建局/Neow | 可消耗modded1正常准备，正常production保存 | 尚未seal，不冒充测试窗口 |
| A目标保存/封存 | 唯一匹配SaveRun room-before Task wrapper；private CP seal | 不改production参数/原Task结果；raw不公开 |
| A两操作→正常退出 | 三点/两accepted完整；退出后native仍seal exact bytes | 只证明内容相同，不声称零写入/mtime恒定 |
| A→B配置补齐/主菜单 | 补disabled记录，前后再次同CP；fresh B claim验证完整A | 无restore、无跨candidate拼接 |
| B normal Continue/两自动操作 | 预声明reload保存；三点逐步比较，两次正常逻辑API | 不再要求native同A；不改变HP/牌堆/RNG纠偏 |
| B正常退出/离线收尾 | game0，NumReloads0→1，有限禁用+exact pair删除 | 不外推个人unmodded槽安全或无外部内容漂移 |

- 正式预算使用 **1/3**：只有本attempt Arm，A/B完整有效，无runtime候选修订、无用户中断/无不合格fixture重准备；不因剩余预算追加测试。配置/外部核验脚本失败不伪称业务失败或新的Arm。
- 原始private checkpoint、native-path和完整config备份未导出；只公开schema允许的semantic、metadata、hash和有限events。没有glob整private目录、读取另一账号/个人unmodded1，未验证/宣称它们的实际安全状态。
- 交付后等待Project Lead独立核验。PM可只读重算索引/三点字节比较及accepted/commit证据；**不得重放Prepare/Reconcile/Closeout或任何游戏动作脚本**。运行阶段Verify-A/B脚本绑定当时live process，并会create-new receipt，不是当前可重跑的验收命令；使用新的只读Verify-RuntimeDelivery。

## 旧交付保留与离线复核

- 只读执行 `Verify-LegacyPreservation.ps1`，新收据 `runtime/legacy-preservation-2026-10-23.json`：r2 **2137/2137**、r1 **951/951**、A5 runtime **405/405**及旧 **24/24、331/331、326/326**目标长度/hash全部保持。没有重新build或跑不必要的全套合同；本轮候选为同一个已验r2。
- r2交付索引SHA256仍 `F61C59147B8D450102885522F11D6135616452AE5D715847FF080FA7D2E41CE4`；原r1仍 `1A1986330D28EAF15445BC1762F08BC366C51224225201A17D08C22FAEBA0860`，A5 runtime仍 `20977315629860C991832C6113651DE0E6AA8CD2A1873126F8775ABCD8AE4C64`。历史报告/产物不覆盖。
- 最终索引列出本轮公开文件、报告、交回说明、exact候选pair及已批准source身份，包含每文件长度/SHA256；相关旧索引只作为不可变引用。PM只读命令：`& 'C:/Projects/STS2ReplayMod/evidence/spike-001b/runtime/Verify-RuntimeDelivery.ps1'`，不写文件、不读account/private、不执行游戏动作。它重算交付hash，独立比较三点bytes并检查窗口、accepted诊断、操作/commit链及pair已移除；不冒充新的实际运行或PM验收结论。
