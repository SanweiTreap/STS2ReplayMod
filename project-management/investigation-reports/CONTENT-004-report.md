# CONTENT-004 储君资源、可变卡牌与操作时间

2026-10-28，D-116。**PM已按固定储君资源／可变卡牌／操作时间组合验收收束CONTENT-004，DONE_WITH_LIMITATIONS**；008、原片和各版证据保留。用户确认008洗牌／开悟正常、完整观看暂无明显bug，未代表全部内容／性能通过。累计Step与连续入队改由D-117／PLAYBACK-001承接，见新实现报告；以下008等待清单和各版结果是原包历史，不作为重测008要求。

## 008实测与等待／阻塞清单（交PM讨论）

### 本轮结论与清单范围

用户完整观看后确认洗牌OK，开悟的等待看起来正常，至少目前没有明显bug；对比后觉得其他动作仍偏慢，建议讨论最接近原版的动画播放感觉。这是体验层面的正向反馈与设计议题，不能改写成已经验证所有内容、完全无bug或性能／整包验收关闭。

008两次运行分别为 `d2a8d534465e4843b25b4444379b856d`（2453事件）和 `e58bfe263a644f4bb33095a39e286991`（402事件），都完整到13步胜利并正常Stop，合计68次接受Back、57次历史显示、127次恢复核对；历史全部RngUnchanged，无会话failed/error/difference，两次原manager及所比较Continue／进度恢复。现场游戏0，原片E975…6DF7未变。原始两轮事件／原片及最新轮Godot日志归档到原恢复点 `runtime/content-004-r7/`，公共摘要 `evidence/content-004/runtime-verification-r7.json`；不声称已保留前一进程的原生日志。本轮只改文档／证据，不构建或安装新DLL，008身份仍为E219…E011。

以下清点的是**当前格式4／5最终决策路线里，由回放系统管理的推进限制、可见等待及相关生命周期等待**，附旧格式分支以免混淆。不是逐一枚举整个游戏所有卡牌／药水／遗物内部的原生等待秒数；那些由当前原生实现决定，本片只有实际13操作的覆盖。代码位置以008源码为准。

### 1. 连续Step与原生连续出牌的结构差异

原生 `CardModel.TryManualPlay → EnqueueManualPlay` 在接受卡牌时先启动 `OnEnqueuePlayVfx`（不await），随即把 `PlayCardAction` 交给 `ActionQueueSynchronizer.RequestEnqueue`；真正的 `OnPlayWrapper` 等原生执行器取到这张牌才运行。因此“允许下一张牌入队”“当前业务执行完成”“所有视觉结束”原本就是不同的时点。依据是本机固定版本反编译 `CardModel.cs:1647/1787`、`ActionQueueSynchronizer.cs:147`、`ActionExecutor.cs:123`，未据此推断STS1或其他版本的具体秒数。

当前mod路径则是：

`接收Step（最多一条待执行意图） → 等当前业务／展示边界 → 执行下一个路线节点 → 原生任务完成 → mod展示稳定判定 → 建立可Back节点 → 才提交下一操作`。

| 限制 | 当前行为 | 归属／影响 | 源码 |
| --- | --- | --- | --- |
| 导航待执行容量 | 动作期间只存1条Step／Back／Pause；已有Pending时后续请求拒绝，不累计成多张牌队列 | mod；连续点击不会对应累计同样数量的后续操作 | `PresentationNavigation.cs:4`、`DecisionRoute.cs:126` |
| Step接受 ≠ 下一张牌已入原生队列 | 排队的Step到达路线Ready才转成cursor意图；尚未调用下一张的TryManualPlay | mod；下张的入队视觉也延后 | `DecisionRoute.cs:140/184` |
| 前沿单个业务宿主 | AwaitingEngine时不启动另一宿主；一个ActionMonitor.Pending管理一张牌／药水／EndTurn及其派生效果 | mod；无重叠业务宿主 | `Clip.cs:177`、`ActionMonitor.cs:23/29` |
| 执行器必须完全空闲 | LiveState.Playable要求原生ActionExecutor不运行、不暂停、CurrentlyRunningAction为空 | 原生状态被mod作为更严格的提交条件；不是原生RequestEnqueue本身的“禁止提前排队”要求 | `LiveState.cs:26`、`PlayerLoop.cs:268` |
| 展示期间Step不抢结束 | Step继续等当前段；历史Back／Pause可快速完成当前显示并切节点，原生业务不被抢断 | mod既有D-112；影响连续前进节奏 | `PresentationNavigation.cs:8`、`NativeDisplayTrace.cs:550` |
| Ready与互斥 | 加载／恢复／到达处理中、工作Task未完成、cursor Moving／AwaitingEngine／Stopping时，不直接执行新节点；首开无节点、终点前沿也不能前进 | mod数据／UI交接限制 | `DecisionRoute.cs:27`、`Clip.cs:186` |
| 录制路径也有单Pending假设 | ActionMonitor.Queue在录制期间同样要求Pending为空且Playable；将来支持真实连续入队，必须一起重审录制归属／接受时刻／checkpoint | mod；不能只增加Step缓冲长度就声称复现原生队列 | `Patches.cs:83`、`ActionMonitor.cs:29` |

### 2. 当前前沿每个动作的业务与展示边界

| ID | 等待／阻塞条件 | 当前值或结束条件 | 归属、适用范围 | 源码 |
| --- | --- | --- | --- | --- |
| B1 | 执行前WaitReady | LiveState.Playable且StartTurnTask成功；逐帧检查 | mod等待真实原生状态；每个新业务宿主 | `PlayerLoop.cs:218/268`、`LiveState.cs:26` |
| B2 | 原生任务完成 | ActionTask成功；牌／药另需EffectTask成功；全部DerivedTasks成功 | 原生任务是事实，mod选择把它们统一作为操作完成边界；具体OnPlay等待因牌而异 | `ActionMonitor.cs:70` |
| B3 | 回合结束完整边界 | Generation增加、StartTurnTask成功、Turn恰好为上一回合+1、Playable；不是只点完结束回合按钮 | mod；防回合尚未结算就推进 | `ActionMonitor.cs:80` |
| B4 | 选择语义结算 | ChoiceTape边界匹配；确认后原生返回结果只能提交一次，等待其后效果完成 | 原生选择＋mod确定性检查；无选择结果时不能跳过业务 | `DecisionRoute.cs:218`、`ChoiceTape.cs:64` |
| P0 | 进入稳定段前让出一帧 | Stable先await一次ProcessFrame，再开始Finish展示检查 | mod；帧数，不是固定毫秒 | `DecisionRoute.cs:83` |
| P1 | 普通飞牌清空 | 仍可见且逻辑牌区是Draw／Discard／Exhaust，或在Hand但尚未归到原生手牌树中的NCard，都视为飞行未结束 | mod的宽泛显示启发式，非逐卡原生完成事件；008排除实际消耗所有权节点的不可见清理 | `NativeDisplayTrace.cs:388` |
| P2 | 手牌布局足够接近 | 每个NHandCardHolder距TargetPosition **<2像素**，角度差 **<0.5度** | mod；全部手牌都满足才算near，不只检查刚变化那张 | `NativeDisplayTrace.cs:390` |
| P3 | 消耗核心保护 | 本段最后一个exhaust／exhaust-quick cue之后 **>0.8秒** | mod通用固定下限，仍存在；不是按开悟绑定。008只去掉原生另等2秒DelayedFree造成的错误阻塞 | `NativeDisplayTrace.cs:393/399` |
| P4 | 药水锻造核心结束 | Completed是Potion时，王剑实际_isForging且_scaleTween／_glowTween运行，或源NCardSmithVfx._tween运行，都继续等 | mod按显示状态阻塞；不是给每种药设固定秒数，普通牌的锻造不走此Potion专用条件 | `NativeDisplayTrace.cs:394`、`RegentPresentation.cs:28` |
| P5 | 胜利退场 | terminal时等手牌_animOutTween、能量_animOutTween、抽／弃／消耗堆_positionTween自然结束 | mod；仅终点完整退场，非每张牌 | `NativeDisplayTrace.cs:397`、`CombatHudTrace.cs:48` |
| P6 | 连续稳定确认 | P1无飞牌、P2布局接近、P3到龄、P4无核心、P5无退场，**连续3次逐帧检查**通过 | mod；任一不满足就把quiet清零。3帧不等于恒定50ms，取决于帧率／调度 | `NativeDisplayTrace.cs:385/399` |
| P7 | 形成下一稳定节点 | 最终取样、检查显示尾未改业务／RNG、冻结显示模型Prime、Capture真实checkpoint、发布状态和显示校验 | mod同步工作＋内存分配；是CPU开销，无设计固定秒数，仍可能影响首帧／GC | `DecisionRoute.cs:87`、`DisplayAssets.cs:61` |

业务完成、P0—P6之后才开放下一前沿节点，因此原生允许重叠的“前一张尾部＋下一张入队视觉”被串行化了一部分。它们的耗时不能简单逐行相加：很多条件同一帧并行检查，P6相当于等最慢的条件；B2内部也可能已经播放了相当多视觉。

**本轮实际测量**：最新402事件运行中，操作3精炼在Finish阶段等1358ms、4双持1208ms、5开悟808ms、6首次出剑1233ms、8锻造药1107ms（130帧核心锻造）、12结束回合801ms、13胜利退场810ms。这些只是原生语义完成后的展示边界等待，**不是点击到动画开始、不是整张牌动画时长，也不是每张卡的配置值**。开悟旧版约2408ms现在约808ms，说明多余的DelayedFree等待已去掉，同时0.8秒通用保护仍在。其他牌约1秒以上的显示稳定检查，比原版可提前入队更容易显得慢。

### 3. 选择、历史显示、Back与Stop的额外等待

| ID | 场景 | 当前条件／数值 | 是否会挡下一步 | 源码 |
| --- | --- | --- | --- | --- |
| C1 | 前沿打开选择 | 先让出1帧；等待源卡PlayPileTween不再运行，再显示选择窗口 | 会；mod为了保留完整出牌到中间动画，原生手动业务并不await自己的这条视觉Tween | `DecisionRoute.cs:56`、`NativeDisplayTrace.cs:167` |
| C2 | 选择网格可用 | grid尺寸均>0、不处于_cardsAnimatingOutForSetCards、至少一个有效可显示holder | 会；状态检查，无固定正常时长 | `NativeChoice.cs:133` |
| C3 | 非网格选择可用 | 手牌选择模式等 **0.2秒**；其他小选择／bundle分支等 **0.36秒** | 会；mod固定门槛，分别为原生backstop约0.2秒、首350ms点击拒绝作保护，不能算所有窗口统一原生动画时长 | `NativeChoice.cs:148` |
| C4 | 最终确认关闭 | await选择结果及UiClosed.Task，真正关闭完成后才向原业务Decision交付一次结果 | 会；选择未关完不能安全交接。每个选中／撤选前缀本身没有额外0.55秒Step延迟 | `DecisionRoute.cs:233` |
| H1 | 历史预热 | await该源段尚未完成的_warmTask，再补缺的Warm/WarmPotions；每个新私有卡牌视图／新药水视图创建后让1帧 | 历史开始会等；前沿已改为后台推进式的同主线程延后预热，不阻塞源段发布；缓存失效／消耗后重建仍可能冷启动 | `NativeDisplayTrace.cs:415/480`、`DisplayAssets.cs:69/100` |
| H2 | 历史显示段 | 按当前观看进程首次前沿捕获的源时钟，播放到最后源帧；因此包含上面的mod展示等待 | Step不截短；原录制的思考时长和操作间停顿没有用于播放 | `NativeDisplayTrace.cs:480/510`、`PresentationTiming.cs:5` |
| H3 | 历史真实状态交接 | 选择打开的源时点或最终源边界await目标Restore；恢复耗时从显示时钟扣除；选择Show的Ready可与余下显示并行，结束时仍需await | 会；交接点不是重发业务。恢复掩膜／手牌核对也有同步开销 | `NativeDisplayTrace.cs:531/557` |
| R1 | Back／历史目标恢复 | 先退役源显示，恢复真实内存、订阅和UI；必要时await敌人UpdateIntent，最后让1帧处理原生UI通知，再布局校验／高亮直接到位 | 会；按当前恢复条件，不能在业务Task运行中任意Restore | `DirectCheckpoint.cs:108/253` |
| R2 | 恢复前药水叠层退役 | CancelLiquidOverlay然后最多让 **4帧**，直到_currentLiquidOverlayMaterial清空；否则报错 | Back／历史状态交接／Stop可能遇到；不是每次用药固定冷却 | `PotionVisuals.cs:30` |
| R3 | 恢复前飞牌取消 | 取消原生飞牌token，最多让 **3帧**等待SwooshAwayCompletion完成；否则报错 | Back／历史状态交接／Stop可能遇到；防旧显示回调写进已恢复的牌堆 | `NativeDisplayTrace.cs:219` |
| S1 | Stop | 等当前Move工作与到达处理中完成，关闭选择／浏览页，必要时恢复前沿、退役显示，再清理房间／返回菜单／日志排空 | Stop专用；不是普通Step时长。原生业务不能强行复活／中断后当已结算 | `DecisionRoute.cs:154`、`PlayerLoop.cs:111` |

未被这些门槛阻塞的装饰尾部允许继续：普通死亡淡出、消耗原生DelayedFree、已交还的粒子／药水光效／Smith飞回等；Back／Stop会按所有权退役。消耗的0.8秒核心保护与“等待所有尾部释放”是两件不同的事。历史实现也没有在每张卡之外再等一遍全部原生动画；只是其源段已经含当前mod选择的稳定等待。

### 4. 自动播放、加载、超时与旧格式分支

| 条件 | 实际含义 | 当前数值／范围 | 源码 |
| --- | --- | --- | --- |
| Play自动间隔 | Tick检查距离上次Move结束是否足够久；**只在Playing时**检查，手动Step不使用 | **0.55秒**。历史／选择的Move通常覆盖整段，所以往往是到达后额外停0.55秒；前沿Move提交宿主即返回，等待业务时这0.55秒可同时流逝，不能当每步固定额外0.55秒 | `DecisionRoute.cs:177/257` |
| 首开准备 | 等未完成的存档Task、资源／房间加载、StartTurn可用、初始状态核对；最终决策路线还等CombatStart／PlayerTurn横幅消失 | 首开专用，横幅检查上限12秒；不是首张牌的冷却 | `PlayerLoop.cs:71/138/159`、`NativeDisplayTrace.cs:183` |
| 错误上限 | 用来判失败，正常路径不睡到截止时间；应与设计等待区分 | 业务／WaitReady 60秒（扣选择等待）；展示settle和选择入场Tween／grid ready 3秒；开场横幅12秒；取消清理3／4帧 | `ActionMonitor.cs:76`、`PlayerLoop.cs:268`及以上函数 |
| 胜利语义 | 等Won事件、冻结投影、原生VictoryTask成功、执行器空闲；**不等敌人死亡动画Task完成** | 状态条件，无统一死亡秒数；之后仍有P5完整HUD退场 | `BattleEnd.cs:47` |
| 旧选择驱动 | 格式4／5 DecisionRoute不走ChoiceTape.Drive；旧分支打开选择等0.4秒，自动事件间0.6秒，关闭最多60秒 | **不是当前008路线的额外0.4／0.6秒**，不能叠加进当前等待 | `ChoiceTape.cs:192/226` |
| 旧完整操作checkpoint | 非DecisionRoute才在CaptureCheckpoint让1帧＋WaitReady；旧Rewind流程另有Ready检查 | 当前格式5不再额外走此路 | `PlayerLoop.cs:175` |
| 取样频率与显示Tween | 源取样最多120Hz；历史按真实单调时钟取源帧；颜色渐变／粒子寿命／能量／橙条尾部有自身Tween | 它们本身不是统一推进等待；只有被P1—P5等明确检查时才阻塞。1/120秒不是Step冷却 | `NativeDisplayTrace.cs:294`、`CombatHudTrace.cs`、`HealthDisplayTail.cs` |
| 状态比较／日志 | 状态快照、冻结显示模型、Renderer建立、JSON序列化发生在主线程；磁盘日志写入已移到有序writer，Stop排空 | 真实计算开销，不是固定Delay，也不能因无显式await便当耗时0 | `Runtime.cs:83/95`、`PlayerLoop.cs:221`、`DisplayAssets.cs:61` |

### 5. 交给PM的决策问题（本轮未实现）

1. **Step的产品含义**：继续定义为“完成一个可逆可观察节点”，还是允许连点累积多条命令，并在原生允许时提前提交／播入队视觉？选择节点是否仍要每个目标一Step？
2. **单步与连续播放是否共享相同提交策略**：保持单步停在可Back节点，同时让连续前进尽量重叠原生尾部，能更接近原版，但需明确前沿与历史都遵守的规则；不能只加速历史。
3. **展示边界保留哪些条件**：0.8秒消耗保护、全部手牌2px／0.5度、连续3帧、选择前等待PlayPileTween、药水核心完成、自动0.55秒间隔，分别保留／缩短／替换原生事件／只在暂停停点执行？不能简单全删，否则会重新出现截帧、回合计数和UI交接问题。
4. **接受下一操作与建立Back节点是否解耦**：允许视觉／入队先行时，要明确Back在有排队业务、选择、跨回合／胜利时回到哪里，以及取消的是导航意图还是实际原生队列。
5. **录制／回放数据是否要记录输入排队事实**：当前一个Pending、一个宿主、语义完成后checkpoint与格式5Accepted／Completed时间假设需要一起审视。只改Step按钮缓冲不能解决原生排队动画启动时刻。

建议用当前13操作作为对照样本，先定“接受输入／提交原生队列／业务完成／允许暂停Back／装饰尾释放”各时点，再选择要改变的门槛。还没有提出或安装一版任意统一缩短等待的实现；由用户与PM讨论后派发下一目标。本轮不要求额外测试。

## r6 实测与 build-008 修复

007 runtime `a9e8657312154671ae2eca0ee61df63f`，3693事件、两次完整13步胜利／正常Stop；80次接受Back、78次历史播放全部RngUnchanged、160次恢复显示及78次手牌叠放核对，无会话failed/error/difference，两次关闭均原manager与所比较Continue／进度恢复。用户确认整体更流畅，未再报告叠放问题；状态及层级断言不替代所有画面验收。原片E975…6DF7不变、游戏0；3693原事件、原片、Godot日志已复制读回到原恢复点 `runtime/content-004-r6/`，公共摘要 `evidence/content-004/runtime-verification-r6.json`。原生日志异常仍单独保留，不以会话通过抹掉退出资源诊断。

首次出剑的额外排队实为前一步开悟的消耗尾部：两次操作5展示边界分别等待2408.0301／2411.8473ms，CoreForgeFrames均0。反编译Enlightenment只改费用、没有专属等待；NCardExhaustVfx原生燃尽约0.4秒后材质完全透明，再异步等2秒才释放卡牌。NCard自身IsVisibleInTree仍为true，旧统一飞牌检查把这段不可见清理也等进去了。008按实际消耗所有权跟踪节点／模型身份，普通飞牌检测与后续源取样均跳过它；NCard.Create重新借用共享节点时撤销所有权，Clear随观看生命周期清空。保留原生燃尽、既有通用核心消耗保护和异步DelayedFree，不加单卡秒数、不提前销毁节点或重发业务。后续源段也不会把旧消耗卡重新显示成普通卡牌。

历史洗牌使用一个原生飞牌及其自建的一条原生拖尾，没有另录一条洗牌拖尾。此前历史创建时均先以默认原点进入场景，之后首个Process才跟随，存在首帧粒子从画面原点拉斜线的时机差异。008在AddChild／_Ready前分别设置飞牌源起点与拖尾跟随起点；普通卡牌拖尾同步修正初始位置。原生粒子／材质／渐变亮度保持，新增StartPositionSeededBeforeTree及NativeTrailCount诊断。斜线／亮度是否完全符合仍待人眼对照，不用人为降低alpha掩盖。

008 DLL `E2194EB9C410296EB3A8F0238D2A59C074463ED652EC2F97D254878F01D2E011`，安装收据 `evidence/content-004/preparation/build-008-17d7cbb5.json`。Release零警告／错误，显示时钟合同及原片生产读取／CardKey／资源时间核对通过；未改的身份／时间／日志写盘合同沿用既有结果，不宣称重跑。007 pair／配置及008源／检查归档到原恢复点对应preparation；本批Cloud关闭、原片未写、新进度未恢复，未读其他账号或自动启动游戏。

短测：同片Step到5后较快再按Step排队出剑，比较是否还多等；6→Back→Step对照，12→Back→Step看洗牌斜线与亮度，Stop后正常退出。不重录／启用准备，不要求再完整胜利。以下保留各版历史步骤，不能当当前待办。

## r5 实测与 build-007 叠放顺序修复

006 runtime `478f1e73e90d4854a1d07574ec36115e`，543事件、完整13步胜利／正常Stop；11次接受Back、8次历史播放全部RngUnchanged、20次恢复显示核对，无会话failed/error/difference，原manager／所比较Continue和进度恢复，日志排空Failed=0。实际药水核心展示边界等待130帧／1107.2237ms，终点退场94帧／819.6478ms，均非固定延迟；只证明路径执行，不代替用户视觉确认。原片E975…6DF7不变、进程0，543原事件／原片／Godot日志已复制读回到原恢复点 `runtime/content-004-r5/`，公共摘要 `runtime-verification-r5.json`。锻造Callable的Variant异常0，原生日志保留12条退出资源诊断。

用户确认略流畅但还卡，并发现历史手牌叠放错误。006源取样从UI树改为卡牌跟踪集合后，轨道发现顺序不再等于原生手牌绘制顺序；旧HandPose没有保存兄弟顺序和悬停Z层，因此私有holder按创建顺序叠放。这是006的显示回归，不涉及真实手牌逻辑顺序、CardKey或录像文件变更。

007 DLL `F17920D962D93C6FDDC6FE34F266965352E1D17655D0E5617DE7C81DCBBE2695`，receipt `evidence/content-004/preparation/build-007-3875edcd.json`。源帧记录实际holder兄弟索引／ZIndex／ZAsRelative及卡牌自身Z状态；历史逐帧按源顺序排私有holder，悬停独立恢复，入手时立即设置初始层级；退场转换成全局位置视图时仍按源手牌顺序叠放。消失／尚未入手的holder整体隐藏，避免计数标签残留；交还原生卡牌时同步原Z状态。新增native-hand-stack-verified核对可见holder的源顺序／悬停深度。真实牌堆顺序不被改动。

剩余停顿分开记录：本轮原生Step请求到捕获有1034／534ms的排队等待，历史有746ms排队；历史显示准备冷时36～37ms、重复7～8ms，源取样仍有约30ms回收停顿。不能拿排队时间当全程CPU卡住，也不能据此宣布其他停顿解决。用户未留意等待提示。007只将提示写清为“操作已排队，当前动作结束后继续”，没有削弱结算／展示边界或新加固定等待，也没有再并入大范围性能改动。

007构建0警告／0错误、显示时钟合同和原片生产读取／CardKey／资源时间重新核对通过；身份与时间合同源没有改动，保留006证据，不重跑旧矩阵。旧006 pair／配置及007源／检查归档到原恢复点该preparation，Cloud沿用本批关闭，无重录／整盘恢复／其他账号读取／自动启动。

短测：同片到3／4／5，各做Back→Step，观察生成／复制／降费前后手牌左右遮挡顺序；往前走几步再倒回，确认顺序不因缓存复用变化。若再遇停顿，观察是否出现新的排队提示即可，不要求额外反复复现。Stop并正常退出，本轮不要求再完整胜利。

## r4 实测与 build-006 修复候选

005 runtime `cef1907ebfbc4d33b48ad83cdac482b2`，1600事件、两次完整13步胜利与正常Stop；34次接受Back、33次历史播放全部RngUnchanged、68次恢复显示核对。会话failed/error/difference为0、原manager及所比较Continue／进度恢复，日志排空两次Failed=0、最大主线程序列化9.2481ms。原片E975…6DF7未变，进程0。1600原事件、原生日志、原片已复制读回到原恢复点 `runtime/content-004-r4/`；公共摘要 `runtime-verification-r4.json`。用户确认大部分旧问题解决，不据此逐项追认所有旧图像／音频通过。

仍有：首次前进按Step后的静止间隔；重复同段Back-Step流畅，但往前走再倒回、隔一会再播又有冷启动感；历史洗牌缺失；首次锻造药水未展完便出剑；历史胜利手牌退场中途被切掉。用户再次明确首次前进更明显。源捕获慢取样中19次伴随Gen0回收，常见约30～40ms、单次约100～155KB分配；这是相关证据，不是已经量得全部100～200ms停顿。r4两次首次走完主要用Play，21次Step大多发生在历史，只有1个带请求戳的原生操作，因此不能声称已完整测到人工首次Step到首帧的耗时。

原生日志另外有2次预载Invalid Task ID、12类退出资源诊断，以及7次 `System.Object` 无法转换为Variant。最后一项已定位到005锻造续接的两个Callable：反射Invoke表达式被推导为有返回值的委托。006改为显式void语句块，避免Godot尝试转换反射返回值；会话状态比较通过并不覆盖这类回调异常。

006 DLL `7D494CAA8BB089A1A667DE6D7D8AD9979872A88950F85BAA6114204A89314B9C`，收据 `evidence/content-004/preparation/build-006-6b904e5a.json`；005 pair／配置及006源／检查保存在同一原恢复点的对应preparation。原片不改写，仍显式接受002格式5录制哈希，Cloud沿用本批关闭，无新录制／整盘恢复／其他账号读取／自动启动。

- 首次等待：原Finish在动作完成后仍await逐帧创建历史显示视图，每个新视图让出一帧；此时下一Step虽可排队，却会等预建结束。006将纯显示预建改为同一Godot主线程上的延后任务，不阻塞正常前进节点开放；真正进入历史时才等该段所需视图就绪。它没有变成后台线程操作Godot。会话generation、Active／TearingDown及房间存活检查阻止Stop／重开后旧预建复活缓存。
- 分配：通过NCard原生创建钩子／原有卡牌跟踪定位场上实际卡牌，替代每次源取样遍历整个UI树；固定血条／HUD／意图子控件按实际节点缓存，动态数值仍逐帧读取；缓存Godot StringName和NodePath，减少隐式包装与回收压力。旧最大值不足以证明此轮改善，待真实新计时；不关业务／RNG比较，也不固定跳过开头。
- 点击诊断：补鼠标按下到按钮提交、请求到正常／历史准备、历史准备到播放时钟等阶段。旧标记只进入原生捕获，006也覆盖历史路径，便于比较“同段反复”及“离开后回来”。
- 洗牌：捕获NCardFlyShuffleVfx的实际起终点、轨迹资源和原生随机运动参数，历史重建原场景／轨迹。其完成通知只发给新建、无订阅者的显示CardPile，不接触实际牌堆，也不调用Shuffle业务／洗牌RNG。新视觉任务的取消与尾部纳入已有Back／Stop清理。
- 锻造药水：语义完成时原生锻造／Smith展示刚开始。006在同一展示边界内等本次药水的核心剑体缩放／发光与卡牌Smith Tween完成，才开放下一实际动作；忽略永久粒子及后续纯装饰飞回，不加固定秒数。历史使用同一更完整的源段时长；native-presentation-boundary记录等待帧数。
- 胜利退场：终点源捕获等原生手牌／能量／牌堆退场Tween自然结束。手牌退出阶段改按源全局位置插值，避免把父级退场又套一遍手牌平滑运动；完整离屏后才释放显示视图，末端同步原生手牌根位置（含Peek按钮）及牌堆根位置。Back仍直接恢复正常手牌布局。

006 Release构建0警告／0错误，身份6／时间14／显示时钟及原片生产读取／CardKey与资源时间核对通过；这些是离线合同，不是新洗牌／核心展示边界／退出或性能实测。

最短复测：同片**先用Step逐步到5，不先点Play**，观察精炼／双持／开悟启动；到8后较快再按Step，检查先完成核心锻造再出剑；到12后Back→Step看洗牌；至13胜利后Back→Step看完整手牌退场。期间挑一张之前卡的牌反复Back-Step，再往前走几步、倒回并稍停后重播作比较。Stop并短重开一步Stop、正常退出，以核对延后预建清理。不重录，不重跑旧包矩阵。

## r3 实测与 build-005 修复候选

003 runtime `0a93a1d2e66c400e85581ed8c6b88929` 最终5747事件、7次打开／7次正常Stop，停在操作10／11／12／9／9／5／7；**本轮没有完整胜利**。145次接受Back、133次历史播放全部RngUnchanged、280次恢复显示校验，无会话failed/error/difference，7次关闭均原manager／所比较Continue和进度恢复。开场横幅错入短动作的源捕获为0（完整r2汇总为4）；不追认为所有开场视觉都经用户验收。原片E975…6DF7未变。原始5747事件、原片、Godot日志已复制读回到原恢复点 `runtime/content-004-r3/`；公共摘要 `evidence/content-004/runtime-verification-r3.json`。原生日志还保留2次资源预载Invalid Task ID及8条退出资源诊断，未据此断言上述视觉问题的原因。

用户明确：药水短暂跳到右下角／空槽亮度闪；首个结束回合有一次第二敌人橙条长出再收回；大剑历史动画仍不完整、不同打开有差异；历史锻造药／复制缺音效。首次精炼／双持／开悟的卡顿细化为**按Step后约100～200ms才开始、出牌开头缺帧；每次重开后的首次前进有，Back→Step顺畅**。003最大显示取样47.1625ms，上一版字段缓存没有消除人眼问题，不能用数值变化宣称流畅度通过。

005 DLL `F6EFDA73EB28FF0751C18BA8DE0DCF45DB774E5AF13A6920858E1B94F70E97B9`，安装收据 `evidence/content-004/preparation/build-005-7ddadb6b.json`。004是同轮未运行的准备中间版（1BF49B…2188，receipt build-004-4dcfcd3a），最终检查调整药水“先布局、后位置”的应用顺序后在人工启动前由005替代；没有004运行成功／失败结论。原003 pair／配置保存在004准备点，004 pair及005源／检查保存在005准备点；原片仍显式按002哈希接受。

- 私有药水视图重挂父节点时固定左上锚点，先应用子控件布局／尺寸，再应用瓶身和容器的源位置／缩放，防止布局覆盖刚设的动画坐标。空槽交还时恢复源局部色／SelfModulate，并从源已过时间继续原生0.2s延迟＋0.2s亮起Tween，不在显示交还后直接跳到白色。
- Back恢复血量后直接完成该恢复引起的middleground Tween，避免把逻辑回退显示成回血；历史前进仍保留真实伤害的源橙条尾部。这些都是显示属性，不发治疗或伤害业务。
- 重新检查Regent、SovereignBlade、ForgeCmd和NSovereignBladeVfx的完整原生调用。大剑Pose现在记录13组部件的变换／颜色／可见性、粒子Emitting／Amount、骨骼动画名称／相位／loop、实际兄弟层级，以及尚在进行的锻造缩放／发光相位。Back静态恢复不再为了重建几何调用Forge并人工播完；攻击前恢复完整源Pose，原生Attack继续同一锻造尾部，避免强制停止其缩放／发光。纯Tween续接沿反编译原生曲线，无ForgeCmd、卡牌业务或旧Task复活。记录native-blade-attack-phase供核对；本片仍没有追踪之刃群攻验收。
- 源片段的锻造药没有sound cue、双持段也缺大剑生成声。捕获点从上层SfxCmd.Play移到实际NAudioManager.PlayOneShot字典重载，保存事件路径／参数／音量；历史直接重放这一显示API，不再依赖当前业务是否处于IsEnding。历史显示内部重复声由同一入口抑制，已有原生死亡fade声继续自管。音频实际可闻效果仍待测。
- 卡顿不再猜测已解决：Runtime先在主线程序列化不可变bytes，交单一有序后台写盘，Stop／正常进程退出排空；不把Godot对象送到工作线程。缓存／及时释放重复NodePath，减少源取样中的分配和原生包装析构。追加点击到捕获、首个出牌cue、原生就绪／状态比较／enqueue的分段耗时；慢取样分为卡牌／血条／药水／HUD，附本线程分配与GC次数，最多保留24项。没有加固定播放等待或跳帧，也没有关闭状态／RNG校验。下一次实测用于判定这些开销是否真是缺失开头的原因。

005 Release构建0警告／0错误；6项身份、14项时间、显示时钟、原储君生产读取／CardKey／资源时间检查通过，另有序日志100次写入顺序／排空读回及I/O失败后继续写入通过。未运行游戏，不以离线检查代替上述声音／图像结果。

最短复测仍用同片：首次Step到操作9，留意3／4／5刚启动的动画，1／4／6／8／9各Back→Step一次，听复制／锻造音效，看瓶身／空槽及两次单体出剑；第7步检查敌人橙条不凭空长出。之后正常至胜利Stop，短重开到第6步比较大剑再Stop、正常退出。若卡顿仍在，说明哪张牌开头还有即可，已有分段日志，不需连续试旧版或重录。

## r2 实测与 build-003 修复候选

002 runtime `da6c625557874b0b9e5589b9c639bacd` 共4999事件、7次打开／正常Stop，3次完整13步胜利，其余为短测主动退出；133次接受Back、121次历史显示播放（全部RngUnchanged）、257次恢复显示校验。没有会话failed/error/difference；7次关闭均原manager／所比较Continue和进度恢复。原录像E975…6DF7逐字节未变，游戏进程0。原始事件4999文件、原片、Godot日志已复制到原恢复点的 `runtime/content-004-r2/` 并逐文件校验，公共摘要见 `evidence/content-004/runtime-verification-r2.json`。

用户仍报告开场第一回合横幅错入星药动作、历史大剑攻击缺效、药水图标偶发变大、胜利／Back能量位置错误、血条橙色残留，以及首次原生播放精炼／双持／开悟偶发卡顿。状态通过不覆盖这些视觉失败。原生日志另外有1次Combat Room资源预载期间的 `Invalid Task ID`、12条退出时资源未释放诊断；原文私存，未因会话无失败而忽略，也没有证据把这些诊断归因于上述卡顿。

003 DLL `D2FD86D737D0758334A804167007469B9A0082C0ED675FE797F3EC6D0DCB88E9`，安装收据 `evidence/content-004/preparation/build-003-5c532089.json`。原002 pair／配置留在原恢复点 `preparations/build-003-5c532089/`，003源和检查同处归档。继续显式接受002的格式5录制哈希，不改写原片或放宽一般跨版本校验。

- 开场：NCombatStartBanner的异步Display稍后创建NPlayerTurnBanner(1)。完整r2汇总有4次短动作捕获这一迟到横幅；003在开放初始Step前等待这两个原生开场节点自然结束，后续动作不额外等待装饰尾部。未添加固定延迟或重发第一回合业务。
- 大剑：单体路径除NSovereignBladeVfx.Attack外，还使用NBigSlashVfx及NBigSlashImpactVfx；003捕获最内层坐标工厂的原生颜色／朝向／角度，历史只重建显示节点与尾部；Back调用原生CleanupAttack清除旧剑轨迹。追踪之刃群攻继续使用原有VfxCmd通路，本片没有追踪之刃，**不声称群攻实测通过**。
- 药水：新增源NPotion的实际Size、内部Control布局及TextureRect Expand／Stretch／minimum，空槽也保存同样策略，在缓存节点重用时先恢复纹理布局再应用尺寸，避免靠Scale猜测图标大小。
- 能量：检查点保存原HUD位置；Back暂停时立即恢复。历史尾部保存原生AnimIn／AnimOut的方向和已过时间，交还时从同一相位继续，而非在半途Kill后永久留在屏幕边缘；与星星交接独立，旧角色也不依赖星星控件存在。
- 血条：保存原生RefreshMiddleground新Tween的起点／终点／延迟／已过时间；目标恢复后重新应用最后源帧健康显示，继续相同Expo尾部到结束，无需再按Step。不重发伤害、力量或死亡业务。
- 首次卡顿：r2最大单次显示采样51.7393ms，原生和历史均保留真实时间；不是“已证明每次卡顿都是反射”的结论。003缓存HUD反射字段及身份图字段枚举，减少首次动画期间的重复搜索／分配，保留原采样耗时以复测比较。**流畅度仍待真人确认**。

双持多候选会打开FromHand选择，当前泛化路线保留。此原片只有一张合格王剑、原生自动选中，不能据此宣称多候选选择UI已在本包实测。

003 Release构建0警告／0错误，6项身份恢复、14项时间合同、显示时钟合同及原储君生产读取／13组CardKey与资源时间核对通过，见 `evidence/content-004/checks/build-003/`。这些离线检查不是原生动画验收。

最短复测：同一原片首次Step到第9步，观察开场、3／4／5首次动画；对1／6／8／9分别Back→Step，观察图标、两次单体剑攻击及橙色条自然收完。然后至胜利，停约2秒看能量完全退出，Back后保持暂停约2秒看其位置恢复，再Step回胜利。Stop后短重开一步、Stop并正常退出。不重录、不启用准备，不重跑旧包矩阵。

胜利后Back是先前统一节点路线已经允许、此前未专项验收的行为，不是这轮新加功能；本轮只修用户实际遇到的同片终点HUD问题。

## r1 真实录制与时间核验

用户按步骤完成并确认星星获取／支付、复制／降费、跨回合锻造及胜利保存，报告中途停约5秒、正常退出。Runtime `33eb2288b348457384c14ede047d7b92` 对应002，28事件，无failed/error/difference，现场游戏进程0。原事件／Godot日志／原片已备份并逐文件读回哈希；见 `evidence/content-004/runtime-r1.json`、`runtime-verification-r1.json` 及 `checks/regent-recording-r1.json`。

真实录像 `cd3baa3b1be7466f9cb94d385c3db420`，格式5，802,127 bytes，SHA256 `E9758B3B37045917B3FAA411EFA7AA518F3F3E5D549B966CED0CC0F0467E6DF7`；13操作／3回合、最终操作13胜利自动保存。实际checkpoint为储君、13牌／4个序列化先天保留附魔，正常开局5手牌；遗物DivineRight及用户原生涅奥所得SilkenTress，后者非准备注入。

原生DivineRight钩子实际完成并记录星星3；操作1星药后6，操作2起始攻击支付后4。操作3首次锻造生成ID13，伤害18、2费、CreatedThroughForge=true；操作4双持产生不同ID14，18伤害／2费、CreatedThroughForge=false，两个实例IsDupe均false。该次双持只有一张合格候选，原生自动选择，只有一条自动ChoiceFrame／无UI事件；不把它写成人工选择或新选择窗口通过。

操作5使两剑降到1费；用户在操作6打出复制剑ID14，能量1→0、该剑回弃牌堆，原剑13仍在手。操作7到真实第二回合，能量4（含精炼下一回合能量），两剑费用均恢复2。操作8锻造药水按原生规则把手中13及弃牌14都从18加到33，次数仍1，身份与牌区保持；操作9再支付2能量打出保留剑。独立验片工具核对全部卡牌输入的索引／CombatIndex／指纹与前一份实际状态相符，全部选择光标对应宿主，未只靠字段roundtrip声称真实效果通过。

13操作全部具备有限、顺序正确的Accepted/Completed字段；游戏封存后 `clip-production-readback` 记录ExactPayloadMatched=true、TimedOperations=13，离线再走相同ClipStore／Codec生产读取也通过。初始可操作边界1592.3704ms；首个接受13768.2799ms，不把开场加载当思考。指定停顿所在“操作3完成→操作4接受”为 **24833.2229ms**，即31.42606s→56.2592829s之间全部等待，包含用户报告的约5秒停顿及其它操作前等待。无输入／焦点片段标签，不能把24.833秒硬解释为精确5秒，也不能单独还原其中哪5秒；其它长等待如出剑到EndTurn同样如实保留，没有用观看时钟补造。

本次只新增独立验片工具／记录及文档，没有改游戏逻辑／重建DLL／安装／自动启动游戏；原铁甲／猎人两片哈希保持。当前002继续可测，不需重录。**储君原生观看、直接恢复／RNG匹配、新星星及锻造显示、预览中Stop仍待下面一轮实测。**

下一轮：打开最近储君录像。操作1至8每次到达后各做一次Back→Step，检查星星3→6→4、王剑无→一把→两把、降费／实际出剑／跨回合费用恢复，以及第8步手牌与弃牌剑都变33；之后正常播放到13步胜利并Stop。短重开，再到第8步喝锻造药水后立即Stop，随后正常退出，以补新预览尾部取消生命周期。本片双持自动选择，不额外要求旧手牌选择中Stop矩阵。观看仍按当前节奏，原始等待时间只存数据，不控制本轮速度。

## 当前实现

新录制使用格式5，普通入口先支持铁甲、猎人、储君，仍限固定v0.111.0、单人A0、第一幕首排普通战斗、无run modifiers、仅ReplayMod、modded1，Continue不录制。Codec和LiveState资格同步检查；checkpoint实际CharacterId一致性、原生RunState重建与角色资源预载继续复用已有路径，菜单按真实角色显示。格式4猎人及旧铁甲保持原哈希校验；STATE-001仍限铁甲格式3固定首张防御文件点，没有扩成储君文件恢复。

现有内存身份图继续保存Stars、Energy、牌区／CombatCardDb、临时费用、可变DynamicVars和SovereignBlade私有累计值。格式5卡牌比较额外明确IsDupe／DupeOf身份、CreatedThroughForge、剑的伤害和次数，保留同名不同实例；不以升级次数代替锻造值。ForgeCmd对所有哪些剑生效仍由原生规则决定，未将复制牌预设为独立变化。日志记录原生Forge完成时各原实例／牌区／伤害／费用，原生DivineRight房间触发也另有结果日志。

星星HUD新增原生数字、主题颜色、旋转／缩放与HSV取样；增益特效调用原生显示处理器。历史播放临时接管显示时钟，交还原生插值／速度／显示计数，继续尚未结束的数字变化。直接Back恢复稳定计数，不再次支付或获取星星。

王剑的持续原生VFX按真实CardModel身份保存／对齐：历史仅调用审查过的NSovereignBladeVfx.Forge／Attack、原生轨道布局及NCardSmithVfx显示API，**不重跑ForgeCmd**。Back隐藏历史上尚不存在的持续显示，恢复原生几何／轨道；前进交还时保留正在播放的原生视觉。离手剑预览复用原生Smith流程及飞回牌堆，使用冻结显示模型与私有NCard创建／释放通路，避免显示缓存借用共享池；取消时先终止原生飞行等待再清理预览。

同一NCard在一个动作中变值时，旧轨道只记首次冻结模型，会漏掉变化。现在通过原生UpdateVisuals事件使显示变体失效，在同一运动轨道上按源帧切换冻结变体；不为每帧／每张牌重新复制整个状态。实际画面及新生命周期仍待实测，不以构建替代原生验收。

## 格式5时间契约

- 零点：Recorder观察到首战checkpoint并通过资格检查时启动Stopwatch，存相对毫秒；Basis=`checkpoint-stopwatch-elapsed-realtime-v1`，与CreatedUtc的墙钟元数据分开。
- InitialReadyMilliseconds：首次观察到原生房间进入／StartTurn成功、LiveState可操作的边界。零点到这里包含初始加载和可能的开场选择，不能作为玩家操作前思考时间。
- Operation.Timing.AcceptedMilliseconds：ActionMonitor在原生RequestEnqueue入口、通过本项目可操作输入路径时观察到该出牌／用药／EndTurn的时刻；不是鼠标按下时刻或网络确认时刻。
- CompletedMilliseconds：观察到原动作／派生Task与本项目语义边界完成时的时刻；EndTurn包含敌方行动和下一可操作回合，胜利包含已有胜利完成边界。不代表等待所有装饰尾部结束。
- 选择BeganMilliseconds使用同一RecordingClock原点；Duration及局部Events仍是选择等待的相对时长，可以映射到Began＋事件毫秒。原始单调实时时间包含菜单暂停、切后台、加载／停顿和选择等待，没有扣除或伪造；不采全量输入／焦点事件。
- 格式5要求有限、非负、接受／完成顺序正确，并验证选择时间落在对应宿主操作或初始加载区间内。封存后通过Runtime.ReadClip→ClipStore→Codec生产路径重新读取，比较序列化payload并记录clip-production-readback。原始文件里保留这份时间，不用会话日志或观看动画采样补造。
- 旧格式1至4的Timing为空／未知，重新序列化仍省略新字段，保留原片校验。STATE-001特测仍走旧格式3／未知时间。菜单说明旧片时间未知；没有新增原始节奏／变速／时间轴，D-112 Step时钟和D-115 Back高亮保持。

14项时间／角色文件合同含生产Store读回和受控6000ms停顿样本，**该假样本不是人工作战停顿实证**。本次真人录制的实际字段与等待间隔另见顶部r1，不混用。

## 一份真实准备组合

主菜单 `CONTENT-004 储君测试` 默认关闭，一次性用于下一储君A0真实新局，不应用于Continue或观看。

- 保留储君10张起始牌及DivineRight，新增RefineBlade、DualWield、Enlightenment各一张，共13牌；无预置SovereignBlade、锻造数值或额外能量。
- 对这三张及原起始牌FallingStar附原生RoyallyApproved，取得先天／保留；没有升级或硬改费用。
- 星星药水StarPotion放槽0，锻造15的KingsCourage放槽1；无额外准备遗物。DivineRight和正常涅奥所选遗物按实际基线记录。
- 第一幕设为Underdocks，首个普通遭遇为CorpseSlugsWeak；敌人使用原生正常状态。上述准备在首战checkpoint之前进入真实可序列化牌组／药水／遭遇状态，观看不注入牌或expected。

第一轮录制步骤：手动启动并只启用本包准备，新建储君A0进首排普通战斗，开局奖励避免替换起始遗物／变换测试牌。第一回合先喝获得3星星的药水，用FallingStar（0能量、2星星）打左敌，打RefineBlade（1费、锻造8并下回合＋1能量）。**停约5秒不操作一次**，再DualWield选王剑，Enlightenment将手牌费用降到1，任选一把王剑打右敌，留另一把并结束回合。第二回合先喝锻造15的药水，观察手里和离手的两把剑依原生规则变值，再用保留王剑打左敌，之后正常获胜。胜利自动保存后不领奖／进下个房间，保存退出并正常退出游戏。

这份组合覆盖起始遗物／药水获取星星、星星付款、首次锻造、真实复制／同名实例、临时降费和实际支付、跨回合费用重置及离手再次锻造。具体实例／原生变化以实际录像为准；X费没有为了凑样本额外并入，本包费用切片先取该共用通路。

录制核对后再给最短同片观看步骤，跨支付／首次生成／复制／降费／离手锻造做Back/Step并继续至胜利。新遇选择／资源生命周期按实际需要做Stop；不重跑旧包矩阵。

## 候选、检查与恢复

DLL SHA256 `EBB189A1D9982A631E7810313224B729B6F5BC1AD860F0BF637D2ECF392C5A88`，manifest0.1.0。Release0错误／0警告；14时间合同、6身份恢复合同、9旧固定文件点合同、显示合同及铁甲／猎人原片生产读取通过，原片SHA保持。001为封存后、人工启动前的准备版；002将ChoiceScope.Started与Frame.Began使用同一次Stopwatch取值，去掉本地事件到统一时钟映射的构造偏移，未更改格式／播放语义。001 pair／源码仍保留，不算运行失败或通过。没有启动正式／headless游戏、构造原生测试场景或Computer Use。

沿本批已确认Cloud关闭，现场原账号匹配、游戏进程0。复用完整恢复点 `C:/Projects/STS2ReplayMod-local/backups/CONTENT002-20261027-171201-97855f350a/`，另在其 `CONTENT004-current-saves-20261028-150051/` 更新当前save/config／原生combat replay文件快照：41文件、4,471,059 bytes，来源稳定／副本哈希读回。该部分快照不回滚历史run树，旧完整恢复点仍保留；未执行恢复，另一账号不读取。安装前另保留可用CONTENT-003/004 pair、settings、selector和本候选源码；最终安装收据位于 `evidence/content-004/preparation/`。

## 尚未证明

真实储君录制与13个操作的时间落盘／生产读回已核对；原生播放／回退／RNG恢复仍待测。新王剑／预览／星星HUD及变体切换的历史时序与生命周期仍需真人验证。该次复制是IsDupe=false的普通克隆，未覆盖真正Dupe；双持只有单候选而自动选择，未覆盖本来源的手动选择UI。仍限首战单人范围，不包含球／宠物／新单位、后续房间／整局、全部储君牌库／费用修饰／X费／药水遗物、任意并发选择、通用文件强记录或性能保证。旧铁甲／猎人原生验收仍按其版本归属，本次兼容读取不改写为新候选完整回归。
