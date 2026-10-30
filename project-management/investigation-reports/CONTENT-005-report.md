# CONTENT-005：五角色基础战斗支持

2026-10-30，D-132授权，同包按A/B/C分段。A段build-002所测共用闭环通过。B段build-004/r3–r4已取得机器人格式8新录→recent→相关Back/续播→回合末球胜利Back再胜利→Stop/短重开真实闭环；C的Osty/Doom尚待实测，不称五角色运行完成。起点PLAYBACK-003/build-008及007/r8既有结论保持，不追认成本包验证。

## A：共用输入与已有资源

新增`OperationKind.DiscardPotion`，追加枚举而不改变旧值。新普通录制使用格式7，旧1–6的结构/序列化与原片保持。格式7丢药必须有槽位、模型、状态指纹、原目标类型及接受/完成时间，不允许携带喝药目标或卡牌；格式1–6拒绝该新操作。仍只开放既有三角色、单人A0首战。

原生`DiscardPotionGameAction`队列接受处记录当前槽位的真实PotionModel，执行开始验证同一实例并刷新指纹；原生播放器也通过该动作调用原版`PotionCmd.Discard`，不调用`OnUseWrapper`或模拟饮用。丢药只有原生action task，没有饮用effect task；结算、下一原生队列项开始前的边界均按此区别处理。执行开始还核对Kind/Potion/Target，而非仅卡牌键。观众自行丢药被现有只读输入隔离拦截。

丢药是一个完整操作边界，沿现有药水槽位/标记/实例memento、原生药水栏重建和隔离manager退出恢复。没有增加冷文件状态恢复声明：新文件保存输入及结果，结果包含槽位；观看内存Back沿已有代码，恢复后原生再丢弃及Continue隔离仍待实测。STATE-001旧单点格式保持3，未扩展。

增加默认关闭、仅下一次新建储君A0消费的`CONTENT-005 共用测试`：复用原储君13牌/王权、四张先天保留牌、星星与锻造药水，追加Toolbox及第3槽供丢弃的能量药水。准备在checkpoint前写入真实原生run并序列化，普通新局/Continue/回放不悄悄注入。主菜单可选旧储君13项原片入口，普通最近入口保持。

旧储君`cd3baa3b1be7466f9cb94d385c3db420`格式5/13项文件经生产读取及既有资源/费用/复制/时序合同检查通过，文件未改；尚非本轮原生播放证明。初始选择代码原已支持，本增量不新造选择页Seek点，拟合并Toolbox实际检查其加载/首操作衔接。

## 构建与离线核对

- 主工程与DirectContracts构建成功，0警告0错误。
- 新丢药4组：旧储君生产读取/原payload不变，格式7落盘读回，旧格式及非法目标/槽位/身份拒绝，完整边界及Back后请求策略。
- D-131现有14组请求规则、4组完整路线/终点规则通过；保留片尾Auto关闭。
- 旧储君真实录制数据合同通过：星星获取/支付、首次及累计锻造、不同实例复制、跨牌堆/跨回合费用与时间。均离线，不执行游戏构造器，不代替显示/原生恢复验证。

输出`evidence/content-005/checks/build-001/`在交付前生成；002只补丢药无独立effect task的原生队列边界，格式和请求合同相关代码未变，未重复未变的离线矩阵。尚无新游戏日志。

## 安装与恢复点

用户现场确认Cloud仍关闭、无新进度。复用已验证原账号恢复点`CONTENT002-20261027-171201-97855f350a`及后续当前文件补存；未声称新增完整账号快照。换DLL时game0、原账号匹配、原生DLL固定hash匹配；仅ReplayMod启用，modded1。无整包恢复，无另一账号读取，无游戏启动/GUI自动化。

- build-001 DLL `597013052AD56A57DD267A9E9CEA5F008972661EE29FE01001D447F8A7BCA21F`，收据`evidence/content-005/preparation/build-001-7e252f7d.json`。交付前安装后静态复核发现丢药无effect task需要区别处理，未请求用户运行此版；pair保留，不追认运行结果。
- 当前build-002 DLL `CEBCF009567E850B9C51974E66046413B80C21E0A97B6A460E3BEEF16FB07C2D`，收据`evidence/content-005/preparation/build-002-ccc8da9a.json`。补上述队列边界后构建通过并安装；前一pair及配置分别保存在恢复点的对应`preparations`中，原008也保留。

## r1：旧储君原生播放与新片真实录制通过

用户确认旧片播放/Back，新战斗开场选择/丢药/星星锻造正常，首战获胜自动保存并正常退出。实际运行build-002，session `bd16f67661a846c3aa38bfada0da518c`，278原事件，game0；模组observer/playback/recording/teardown失败事件为0。原事件、新片及对应Godot退出日志共280文件逐hash封存到原恢复点的`runtime/content-005-r1-*/`，私有manifest记录精确来源；摘要`evidence/content-005/runtime-verification-r1.json`。引擎启动/退出诊断和RID/资源泄漏仍保留，不称整个引擎日志无警告。

旧储君`cd3baa3b1be7466f9cb94d385c3db420`通过`regent-reference`入口打开一次：完整1..13，随后两次胜利Back12→原生13再胜利，共15次原生结果/RNG匹配、3次真实胜利，2次Back状态/RNG/手动模式与清计划匹配。两次恢复各补5个牌堆监听，原ID/计数不变。Stop在13，Error null、Outstanding false、Running/Auto false、Pending0；原manager和所比较Continue/进度恢复，清理Remaining0。该片本轮是可选参考入口，不冒充新片的普通recent入口证明。

新片`76f55fed77ab448b9ff318b9d50eff98`，格式7，11项/2选择，713,538 bytes，SHA256 `5EC1EFC07843E72360953E1C3B4C71BF1857F90A8B64AB1A16B9C4DE076FEA52`，记录build-002。真实胜利自动封存并生产精确读回，独立生产ClipStore兼容读取也通过，原文件未改。工具箱人工开场选择在Boundary0、Chosen1；双持Boundary5是单候选原生自动选择，不称人工双持窗口证明。

首项明确为DiscardPotion、Slot2能量药水、无目标，接受/完成时间11,447.0618/11,449.7484ms。将初态的`Player.Potions[2]`和`Mechanics.Potions[2]`置空后，与操作1结果完整DeepEquals：除药水槽位外，能量、星星、牌堆、历史、RNG等记录状态均未改变，不是一次喝药。星星3→丢药后3→饮星星药6→支付后4；首次锻造王剑ID14/18伤害，双持生成不同ID15，启蒙把手中两剑费用改1；一剑入弃牌，跨回合两剑费用2；第9项锻造药同时把手牌/弃牌两实例改33伤害。第11项王剑正常胜利。记录中的长等待如实保留，未当成原始节奏播放功能。

本轮没有打开新片，故新片的初始选择自动重现、原生丢药回放/Back/再丢弃、资源Back/Stop及短重开仍Pending。安装保持002，不混入未安装B/C代码。下一仅用这份新片普通recent入口完成这些有界检查，不重录或扩大旧角色矩阵。

## r2：新片recent、丢药/锻造Back、胜利与短重开通过

build-002/session `0f3d8d90c0ed4f14adeb8248aac84914`，276原事件，两次普通recent打开新片；16次原生结果/RNG全部匹配，Retiring0，1次真实完整胜利。4次Back到0/0/9/8均状态/RNG、手动模式和清计划匹配；第一项丢药在首轮实际执行3次、短重开1次，药瓶恢复后再次使用原生DiscardPotion动作。第9项锻造药在Back8后再原生执行并匹配结果，手牌/弃牌剑状态相符。初始工具箱原生窗口两次打开并自动完成，每次initial校验通过，首操作不误消费选择。

Stop在11/1，均Error null、Outstanding false、Running/Auto false、Pending0，原manager/所比较Continue进度恢复；牌堆监听清理Before0/5→Remaining0。第二次是明确短重开/Stop1，不称第二次完整胜利。原片SHA不变，用户确认符合预期；game0。276原事件+原片+对应Godot日志共278数据文件逐hash封存，摘要`evidence/content-005/runtime-verification-r2.json`，引擎诊断保留。

用户另提开局“玩家回合 第一回合”提示与工具箱选牌可进一步细化，想与PM讨论。已按原话交接，未自行指定新先后关系/导航语义，不将此反馈算功能失败或偷偷改D-131；当前开场仍在加载阶段完成原生选择、0为选择后的完整边界。A以所测输入/既有资源闭环通过收束，不代表旧三角色全内容回归。

## B/C候选安装与后续人工步骤

A实际录制已产生新测试进度，换下一候选前将原账号已知当前文件与已知save目录直接叶文件补存：`CONTENT005-post-A-current-20261030-131012`，51文件/3,946,973 bytes，原文件与副本逐hash一致；无history/另一账号遍历，不称新增完整账号快照。收据`evidence/content-005/post-A-current-files.json`，旧完整恢复点、002 pair和原片均保留。

003先安装了1235aaa的B/C机制代码，DLL `51AA08FEBFA6354E4ACDD9B730BBA637526FC3DBAD9E0A15F2DF93538A8CE4F1`，收据`evidence/content-005/preparation/build-003-50d3f152.json`。给步骤前复核发现若第二回合先双放累计暗球，可能提前胜利而漏掉扩槽/随机球取样；仅把界面测试提示改为“扩容→混沌+→双重释放”，再构建安装004，不改变牌效/状态/恢复机制。003未发测、没有运行结论，pair保留。

当前004 DLL `3926F6B5FA74DB355EBD9C19C1A054BA09462A50EA6843E9E1CA810E2487315E`，收据`evidence/content-005/preparation/build-004-f655c640.json`；game0、原账号、本体固定hash、仅ReplayMod/modded1核对通过，Cloud沿本批有效关闭确认，未恢复存档/自动启动游戏。主工程0警告0错误；球/单位各4组关键合同在`checks/build-003/`通过，004仅测试提示调整，未机械重跑未变合同。机制运行仍Pending。

下一先用户明确启用故障机器人准备，首回合暗球/冰球/电击+与集中药水后跨回合，第二回合先扩容和混沌+再双重释放，正常获胜自动保存后退出；核原生文件字段/RNG后安排同片recent/关键Back/续播/Stop短重开。Necrobinder随后分别取初始已有Osty与首次召唤所需样本，避免一场混杂两角色问题。开场产品细化由用户和PM独立讨论，不阻塞本包新角色取样。

## 已发首轮人工步骤（历史）

先复用旧储君样本在原生核心播完、胜利Back再前进及Stop，不重跑旧三角色矩阵。随后用户显式启用共用准备，新建储君A0，正常完成Toolbox初始选择，第一个操作丢弃第3槽能量药水，再按既有星星/陨落之星/精炼/双持/启蒙/王剑流程，跨回合锻造药水，首战正常获胜自动保存后退出。读取本轮新录与旧片播放日志后，安排新片普通入口丢药Back/再丢弃及有关资源Back、Stop/短重开。不要求反复roll或新增无关停顿。

## r3：机器人真实格式8录制通过，包含回合末球致胜

build-004/session `0872198163fa43c6ae2d8754b277a55c`，50原事件，9次原生录制操作接受/完成，6个原生球登记，模组失败0；用户确认正常操作/胜利保存/退出，game0。新片`dfa22d43c501492786b19cacee1411c7`格式8、9项/0选择，588,483 bytes，SHA256 `0BCF4E51723918067EA139BA5A81679832D7FD2E4F35C13AACEA3BCE14EB5467`。游戏封存精确生产读回及独立生产读取/格式检查均通过，尚未打开回放。

原片事实已核：初始电球ID1；暗球ID2私有累计12，第3步满槽激发ID1后队列为2/3/4；集中2使被动8/4/5，跨回合暗球私有值20；第6步容量3→5；第7步混沌+原生CombatOrbGeneration（文件键combat_orbs）计数0→2，产生玻璃ID5、冰ID6，队列2/3/4/5/6；第8步双放暗球后队列3/4/5/6，剩一个1HP敌人。最终第9步为EndTurn而不是人工牌击杀，原生BeforeTurnEnd按冰/电/玻璃/冰处理，结合1HP敌人与电球5点被动可确定电球致死；胜利点玻璃私有值4→3也已记录，保留了电球之后的队列后续结算。

固定源码`CombatManager.DoTurnEnd`先await整个`OrbQueue.BeforeTurnEnd`，阶段随后CheckWinCondition；本片的记录按原生胜利事件冻结、等待胜利Task/当前操作成功后封存，没有在看见HP0时提前截断。文件里Focus在胜利清理后消失、对应球值回落，属于本版本原生终局事实；后续回放/胜利Back需用同一文件严格比较，不手工改成另一套预期。

用户提醒原生旧版本也曾在电球收尾时出现“花粉核心”计数问题。按用户历史反馈登记，不预认当前版本存在相同bug，也不把本Mod当成修复原生旧bug；本片遗物实际是CrackedCore、BagOfPreparation、PhialHolster，未包含该计数遗物，故不提供其运行覆盖结论。相关遗物计数/延迟结算仍须实际样本区分。当前优先用本片的真实回合末球胜利验证恢复后的原生循环和后续球/状态/RNG结果。

50原事件、原片及Godot退出日志共52数据文件逐hash封存，摘要`evidence/content-005/runtime-verification-r3.json`；原始引擎诊断保留。下一同004普通recent打开，取满槽激发、跨回合暗球、扩槽、混沌Back再生成，以及胜利Back8→原生EndTurn再次获胜、重复恢复、Stop与短重开。录制成功不代替这些运行结果，Osty/Doom尚未取样。

## r4：机器人原生回放、球恢复和回合末致胜续播通过

build-004/session `fa13be2ba0184bd7a2ba8f0f5f71c681`，697原事件，两次普通recent打开原格式8片，28次原生结果/RNG匹配，Retiring0，18次Back状态/RNG/手动模式与清计划匹配。18次球显示重建均通过槽容量/队列顺序/原实例绑定/当前VFX监听数检查；20次退出旧球显示（18恢复+2Stop）均NativeVfxListenersRemaining0。扩槽3/5反复恢复，混沌多次原生再生成玻璃/冰球，逻辑ID5/6和RNG/结果保持；暗球跨回合重复执行，短重开还Back0后再次原生创建暗球ID2。

4次真实胜利均为最后EndTurn的原生球结算；三次胜利Back8后确实走`native-turn-loop-continued`：新信号、原生阶段方法、旧Task未恢复、开场/回合开始未重跑，随后EndTurn再次成功且完整结果/RNG匹配。由此取得此前仅源码/离线的续播桥接运行证明，范围是本片的玩家回合末电球致胜；不代替Necrobinder的Doom敌方回合结算证明。

Stop在9/1，均Error null、Outstanding false、Running/Auto false、Pending0，原manager/所比较Continue进度恢复；牌堆登记监听Before0/5→Remaining0，球VFX监听零余量。Osty清理事件Known0，不能当宠物订阅运行覆盖。原片SHA保持，用户确认视觉/交互符合预期，game0；697事件+原片/Godot共699数据文件逐hash封存，摘要`evidence/content-005/runtime-verification-r4.json`，引擎诊断保留。

覆盖边界：用户反馈包含满槽激发Back/重做正常；原日志里第3项只执行一次，Back目标为4/4/6/5/5/6/5/6/5/7/6/7/6/5/8/8/8/0，没有目标2，因此只把满槽激发的首次原生结果列为日志证明，不冒充独立跨该步恢复/重做。所测球删除/重生、容量/暗球私有值/随机球、实际回合末胜利等闭环成立，不机械追加旧步骤或宣称全球/遗物矩阵完成；花粉核心依旧未覆盖。

本轮后补存原账号51份已知当前文件/3,942,450 bytes，`CONTENT005-post-Defect-current-20261030-133452`，原件/副本逐hash一致，收据`evidence/content-005/post-Defect-current-files.json`；非全账号快照、无history/其他账号遍历/恢复。原拟接C，随后PM传达用户已确认的D-133顺序，故先做下文D，再接回C；C未实际开录，不能称Osty/Doom已验收。

## D-133：开场控制与完整开场Back，006实现历史

新原生入口在加载Task未完成期间运行同一OperationRequests驱动。请求路线增加完整开场-1、Boundary0原生选择细阶段、首次可出牌0，既有操作1..N不重编号。首开自然走原生开场，遇选择手动等；单次Whole以0为目标，补完所有初始选择和原生开场尾业务，不附送操作1。Fine只推进下一阶段，Auto连续；没有选择的首开自然到0。选择页仍不是独立Seek目标，原始Initial比较仍在操作1之前。

普通Back可0→开场；开场选择中Back撤整段开场、清FIFO/Active/Auto。首次实现不是开场直接memento：先结束或取消旧房间原生等待、清理监听/场景，确认旧turn-loop任务已结束，再以房间前checkpoint和新的隔离SaveManager重建Run/战斗；在新的StartCombatInternal入口门控等待。新请求释放后原生初始化、遗物、抽牌/选择、单位/球和RNG重新运行，不能用开场后Initial倒填前态，也不复活旧Task/TCS。旧房间memento清空，重新访问后才可Seek。日志记录请求至开场门控就绪耗时，当前未测延迟。Back清Auto；Seek开场保留请求时Auto，沿既有D-131。

取消顺序区分原生入口和旧STATE工具：原生开场先RunManager.CleanUp使turn-state不再live，再取消新开场gate/选择TCS，等待旧turn-loop退休；加载资产/场景本身不可取消时先到原生安全等待再退出，不让晚到loader写入新run。旧STATE原有选择取消次序保留。只移除原生入口的WaitForOpeningPresentation额外尾等，保留原生横幅动画时长；横幅只作展示不吞鼠标，迟到的回合1横幅若当前回合已更晚则不创建，旧房间节点随退出取消自身动画。真实取消/层级/横幅效果需本轮人工日志与观察，不由编译证明。

新增可选已有工具箱片入口`76f55fed77ab448b9ff318b9d50eff98`，不改recent指针/原片；新DLL显式兼容004的Defect录制hash，格式仍8。主工程及DirectContracts构建0警告0错误；7组开场请求政策（自然选择等待、Fine、Whole止0、Auto接1、Back清计划、无选择/Seek模式、连续初始选择）、14组D-131、4组完整路线及真实A/B生产读回通过。输出`evidence/content-005/checks/opening/`；均不是原生场景重入或视觉运行证明。

安装006 DLL `80B35DCAC8D0798DED2F32E26159C7CF154153B758A8B06FF588C2693390457C`、manifest `69185E5024D560BC5C9B81FC64CC2F732745BCFBA31E64F069CAEF6AAEABE8FB`，收据`evidence/content-005/preparation/build-006-207d814c.json`；005是同轮封存但未测试的中间pair，006补保留旧STATE取消路径，005/004及其配置均私有保留。安装前game0、原账号匹配、固定native hash、仅ReplayMod/modded1；post-Defect 51份当前源文件全部仍与备份hash相同，Cloud沿本批关闭确认，无整档恢复/其他账号读取。下一人工复用工具箱片：初始手动等、Whole止0、0/选择中Back→干净开场再Fine/Whole、后续操作与相关Stop/重开；原生开场重入/延迟Pending。必要时再补抽牌后ChoicesParadox代表片，C仍本包接回。

## r5 / 007修复：006开场Back落点错误，改为自然到原生选择

006/session `1065489da97f4855a809b21be5c5c509`，131事件，工具箱参考3次及普通Defect1次打开。首开工具箱可手动Fine/Whole完成选择，Initial匹配、没有消费操作1；首项丢药原生结果匹配，Back0状态/RNG匹配。两次0→开场干净重入分别327/295ms到StartCombatInternal前门控，无mod observer/playback异常。用户明确指出“回到开场后停在一个默认场景，没有手牌也没打开工具箱选牌页面”，故不能把无代码异常当D-133体验通过。两次重入后均未再发前进请求，不能称重入后候选/再选/Initial或完整胜利已验证。

4次Stop均Error null、原manager/所比较Continue进度恢复，开场选择中Stop及初态短开正常；6次房间任务退休均旧loop完成（预期取消）、未复用旧Task。原A/B片hash不变。131事件+两原片+Godot共134数据文件逐hash封存于原备份的`runtime/content-005-r5-20261030-135155`；摘要`evidence/content-005/runtime-verification-r5.json`。Godot的房间预载Invalid Task ID及退出RID/resource诊断原样保留，不预认与门控落点同根因。

按用户反馈，007删除StartCombatInternal前的人工gate及其patch，仍以房间前checkpoint干净重建、新内存manager和新原生任务。重入自然执行到首个开场选择UI准备好才解除重建遮罩/手动等，不在未初始化场景要求额外`>`；没有开场选择则自然到playable0。选择中Back仍撤整段开场后重开第一选择、清旧计划/Auto，Whole仍止0，Auto可续到正常操作。所有实际原生初始化/候选/Initial校验保留。日志新增`native-opening-reentry-ready`，区分native-opening-choice与playable-zero落点并记请求至就绪耗时；真实落点、重入再选及延迟需人工验证。

主工程/DirectContracts构建0警告0错误；更新的7组开场请求政策（重入自然到选择后Fine）及14组D-131、4组完整路线、原A片生产读回通过，输出`evidence/content-005/checks/opening-fix/`。只证明请求政策/兼容，不代表场景重入PASS。007 DLL `639B6229CDD2BC482567587CF9E3406D0DC6E9EE7AB4E0DA17ABBCB66061C581`已安装，receipt `evidence/content-005/preparation/build-007-5c350abf.json`；006 pair/config/重要失败原始证据保留。安装前game0，原账号/固定native hash匹配，仅ReplayMod/modded1，Cloud沿本批关闭确认，无整档恢复。补存`CONTENT005-post-opening-r5-current-20261030-135321`已知当前51份文件/3,942,450 bytes，源/副本逐hash一致，receipt `evidence/content-005/post-opening-r5-current-files.json`，不遍历history/其他账号。下一工具箱原片验证Back直接回选牌→Whole0→首项/Back→选择中Stop和重开Auto完整胜利；D未验收，C仍随后接回。

## r6 / 008修复：分开房间显露与选择就绪，保留原生可见入场

007/session `952656ce399040e1a2fe0cfadd9678a7`最终92事件，一次工具箱参考打开。两次干净重入确实走原生选择，至选择Ready耗时1663/1652ms；第一次从0 Back，第二次在开场选择中Back再重入。随后Whole完成该次原生选择、Initial精确匹配回0；首开Whole也止0，首项丢药/普通Back0匹配。Stop0无Error，旧loop退休、原manager/所比较Continue进度恢复；用户正常退出，game0。92事件+原片/Godot共94数据文件封存`runtime/content-005-r6-20261030-140140`，摘要`evidence/content-005/runtime-verification-r6.json`；未取完整胜利/短重开，不当全流程PASS。

用户进一步说明：Back应回到刚进入战斗房间，自动展示原生开场和工具箱窗口出现，当前像直接切页，质疑是否又在补动画。核源码：007从真实房间前checkpoint重入，但`_rebuildingOpening`保持到PrepareChoiceCore等待选择Ready后才清除，Ui遮罩因此覆盖原生入场；同一Covered还作为SilentRebuild，在NativeDisplayTrace.OneShot/Shake阻断声音/震动。不是直接套选择快照，却把真实过程隐藏了。PM与用户语义对齐：只遮必要重建，房间可展示即显露；选牌可接请求和Initial核对是更晚的独立条件。选牌中Back保持D-133整宿主/整开场，不增加选择页Seek。

008用独立`_openingReentryPending`记录重入至选择Ready，`_rebuildingOpening`只控制必要遮罩/静默。当前代新NCombatRoom.OnCombatSetUp postfix，在原生Ui.Activate/SetUpBackground完成后立即解除遮罩并刷新；核当前room实例与当前CombatState归属，普通memento恢复/旧room不触发。此时原生StartCombatInternal/AfterRoomEntered/选择链仍自然进行，不人工挂gate、提前选择或重播动画。原生房间背景/战斗UI可见后，OneShot/Shake静默也同时解除，后续选择Ready仍控制用户提交，未改Hook顺序。增加`native-opening-room-revealed`与RoomAlreadyRevealed诊断，分开观察显露和选择就绪时间。真实效果/音效/时序仍待人工验证。

主工程构建0警告0错误；原有请求政策未改，不新增动画模拟测试。008 DLL `792B181B68CDB8267F3565EDC09B030352E019C63472623482FED3E5C3200024`已安装，收据`evidence/content-005/preparation/build-008-e48f6522.json`。安装前已核游戏正常Quit/Steamworks shutdown及game0、固定本体/原账号、仅ReplayMod/modded1，Cloud沿本批关闭确认；51份当前文件另存`CONTENT005-post-opening-r6-current-20261030-140149`/3,942,450 bytes，逐hash读回，收据`evidence/content-005/post-opening-r6-current-files.json`，不遍历history/其他账号、不restore。007 pair/config及原始失败体验证据保留。下一同片确认Back后可见原生进入房间→工具箱开窗→等待，选择中Back也重放该流程，再Whole0/Stop；D视觉未验收，C仍随后接回。

## r7：008原生可见开场、选择中Back与完整胜利通过，接回C

008/session `80a4353d76384319a3f41cede2925893`最终204事件，一次工具箱参考打开。三次开场重入均在当前新NCombatRoom.OnCombatSetUp后先显露，345/287/297ms时ChoiceReady false、NativeEffectsAllowed true、未新增请求/未播放Mod动画；其后1696/1640/1627ms到原生选择Ready，RoomAlreadyRevealed均true。第二次重入尚在选择时又Back取消等待，再次干净重入；共三次开场完成（首开与两次完成重入）均Initial匹配、Operation1Started false。用户确认实际看到入房间/工具箱窗口出现过程、选择中Back正常；没有复活旧Task或直接恢复选择页。

随后Auto依次完整1..11原生操作，11结果/RNG匹配、Retiring0，真实胜利且原生VictoryTask成功、未生成奖励。终点/Stop的Auto关、Running false、Pending0、Outstanding false；Stop11 Error null，原manager/所比较Continue/进度恢复。4次房间turn-loop退休均完成且为预期取消、未复用旧Task，牌监听5/5/5/0→Remaining0。原A片SHA `5EC1EFC07843E72360953E1C3B4C71BF1857F90A8B64AB1A16B9C4DE076FEA52`未变，用户正常退出，Godot Quit/shutdown及game0已核。204事件+原片/Godot共206数据文件逐hash封存`runtime/content-005-r7-20261030-140735`，摘要`evidence/content-005/runtime-verification-r7.json`，引擎原始诊断保留。

D-133按本工具箱片所测可见流程/开场请求/整体Back/完整胜利与Stop阶段闭环；本轮没有独立短重开，不重复补称，前006/r5与007/r6短开/Stop证据分别保留。没有开场选择的Defect0短开来自006/r5，不追认成008完整回归；ChoicesParadox抽牌后原生顺序有源码事实，未另取实际样本。开场重入使用房间前checkpoint，耗时含必要重建与自然初始化，不称直接memento恢复，也不改A/B既有验收边界。

当前008安装不变，C仍未录制；按已确认顺序接回默认起始Osty的代表片，首回合能量药水→保镖→释放→碎骨→第二张保镖→EndTurn，第二回合死亡使者+→EndTurn取Doom真正胜利，不额外攻击。先保留BoundPhylactery，不勾首次召唤；以后按需要用已有可选准备取首次创建。C录制成功/生产读回不代替后续原生回放、Back/重召订阅与Doom胜利续播。接C前再补存已知当前51份文件（`evidence/content-005/post-opening-r7-current-files.json`），逐hash读回，原账号/固定本体/当前008核对、game0，无history/其他账号遍历/整档恢复，Cloud沿本批已确认关闭。

## r8：C默认起始Osty已录成，原生回放与恢复待测

008/session `b55645bb6d32493390bc3a66df44e952`最终49事件。新片`7393a0279941453297b75597dcea5446`，格式8/9项/0选择，574,612 bytes，SHA `73079FEFAD3ECB156299038422DE5D1A4FC4A9D34C7D79554791334637CD6A5B`；mod仍008。实际准备保留原10牌/BoundPhylactery，附加Bodyguard/BoneShards/Deathbringer+与Bag/能量药水，Neow实际获得FishingRod。步骤：1能量药水，2Bodyguard，3Unleash，4BoneShards，5第二Bodyguard，6EndTurn，7Deathbringer+，8DefendNecrobinder，9EndTurn真实胜利。额外防御不影响所需机制，无需重录。

文件单位事实明确：Osty原生ID3、Owner0、NextIdentity4、Pets[3]，初始HP1/1→操作2成长6/6；3攻击第二敌26→14；4群伤后敌18/5、玩家Block9，Osty死0/6但留在Allies/Pets；5同ID重召活5/5；6新回合成长6/6；7双方Doom26/Weak1，8防御受脆弱得Block3。9在敌方阶段结算胜利，玩家受1伤至65，Osty死0/6仍在Allies、胜利清空Pets；Defeated含原两敌ID1/2，不把宠物误列敌人，FishingRod的CombatsSeen0→1也在胜利状态。起始对象重召不是首次新建对象证明，未勾可选首次召唤。

真实胜利自动seal、原生VictoryTask成功，生产读取ExactPayloadMatched；额外直接生产读回/兼容检查通过（`evidence/content-005/checks/necrobinder-recording/production-read.json`），保留格式8单位/资源/胜利状态/RNG原字符串，不提供任意点冷恢复。该session前一次准备入口报test-entry-character-or-A0-mismatch，随后未准备首战零操作退出触发clip-operation-count，两条observer-error与一条recording-unsupported-or-failed均保留，不猜具体选错条件；后续有效定向片从recording-start到封存无失败，不把整session称零错误。

49事件+原片/Godot共51数据文件逐hash封存`runtime/content-005-r8-20261030-141840`，摘要`evidence/content-005/runtime-verification-r8.json`。用户正常退出、Godot正常Quit/shutdown、game0已核；播放前补存已知当前51文件/3,953,133 bytes，收据`evidence/content-005/post-necrobinder-recording-current-files.json`，源/副本逐hash匹配，不遍历history/其他账号、无restore。008保持；接同片普通recent原生全播、成长/死亡/同对象重召/Doom回退重做、胜利Back8再EndTurn、Stop与短重开。C回放/显示/订阅/恢复及Doom胜利续播仍Pending，不以录成代验收。

## r9 / 009修复：骨妹逻辑恢复与Doom续播匹配，重召血条视觉失败

008/session `e6c5f8467f2440a7b9ebb7bfdbde1073`最终1038事件，两次普通recent打开C原片；43次原生结果/RNG、29次Back状态/RNG匹配，包含多次4→5同ID重召与3→4死亡重做。3次真实Doom胜利；两次胜利Back8都新建信号以原生阶段方法续接，第一次之后确实EndTurn再次获胜，第二次先再Back到更早状态，不能计为第二次立即末步再胜。C敌方回合末Doom胜利后的原生循环桥接已取得实际运行证明，不重跑开场/旧Task。

29次Osty当前Pets/Died回调修复有运行检查，Stop9/5均Known1、NativePetDiedCallbacksRemaining0，牌登记监听0/5→0；两次Stop无Error/Outstanding、Running/Auto false/Pending0，原manager/所比较Continue进度恢复。原片SHA `73079FEFAD3ECB156299038422DE5D1A4FC4A9D34C7D79554791334637CD6A5B`未变；1038事件+原片/Godot共1040数据文件逐hash封存`runtime/content-005-r9-20261030-142529`，摘要`evidence/content-005/runtime-verification-r9.json`，正常Quit/shutdown及game0已核，原始引擎诊断保留。

用户发现“5/9做Back后再Step，召唤出来的小手没有血条”。源码根因是CreaturePresentation.Restore对死亡Osty调用ToggleIsInteractable(false)，将_stateDisplay.Visible=false；原生NCreature.StartReviveAnim仅调用AnimEnableUi把alpha淡回1，不负责把Visible打开。数据/回调/原生结果可匹配，却因重建的死亡显示不符合原生不变量而无法显示血条，因此C视觉未验收。

009在恢复Osty时保留状态栏节点可见（尊重原生debug隐藏），活体alpha1；死体alpha0，再调用原生DisableInteractionForDeath与OnCreatureDied恢复死亡hitbox行为，后续HealInternal/Revived/StartReviveAnim按原生链淡入。没有在Step末尾强制补血条或手写召唤动画。增加原生StartReviveAnim postfix只读核对/日志，记录Visible与原生fade拥有alpha，隐藏且非debug状态会显式报错；没有修改Observer看到的Visible。显式兼容008录制hash，格式仍8，原片不改。

主工程构建0警告0错误，C原片生产读回/兼容检查通过（`evidence/content-005/checks/osty-hud-fix/production-read.json`）；不以离线读取冒充原生UI验证。009 DLL `990FBAE08166BEA8E7C524A1A7375EE6AA6B745E2DD43B2779CE3184403A2F50`已安装，收据`evidence/content-005/preparation/build-009-7c09473c.json`；008 pair/config保留，录后51份已知当前文件备份源hash安装前全部仍一致，原账号/固定本体/仅ReplayMod/modded1核对，Cloud沿本批关闭，无restore/其他账号遍历。下一同片先到5，再Back4重做5多次查原生血条淡入，必要Back3重做死亡再重召；随后真实胜利/Stop及一次短重开。首次新建Osty仍未覆盖，待默认对象修复验证后接已有可选准备。

## r10：009默认Osty恢复/重召血条、真实胜利与短重开通过

009/session `bc93e4325a99485283ab22756f84ea9d`最终916事件，两次普通recent打开默认C原片，35次原生结果/RNG、25次Back状态/RNG匹配，无observer/mod失败。多次5→Back4→原生重召、退3重做碎骨死亡与重召均有原生结果匹配；25条osty-hud-restored均Visible true，其中8条死亡alpha0，9条native-revive-hud均Visible true，原生fade拥有alpha、诊断未改可见性。用户明确确认血条/死亡重做视觉正常，取得009修复实际证明。

一次Back0后再Back完整开场，原生重建至无选择初态、Initial匹配，随后继续正常出牌；本轮不声称重测工具箱/ChoicesParadox。完整9项Doom真实胜利，终点Auto关，Stop9与短重开Stop1均Error null、Outstanding false、Pending0，原manager/所比较Continue进度恢复。含该次开场重入的三次房间清理，Osty回调均Known1/Remaining0，牌登记监听5/0/5→0。原片SHA `73079FEFAD3ECB156299038422DE5D1A4FC4A9D34C7D79554791334637CD6A5B`未变；916事件+原片/Godot共918数据文件逐hash封存`runtime/content-005-r10-20261030-151419`，摘要`evidence/content-005/runtime-verification-r10.json`，正常Quit/shutdown及game0已核，原始引擎诊断保留。

默认起始Osty所测录制→生产读回→成长/死亡/同对象重召/Back→真实Doom胜利→Stop/短重开闭环按008/r8–r9与009/r10分版本成立；Doom胜利Back后原生循环续接是008/r9的已测路径，009仅改显示恢复与诊断，不追认本轮新增该项运行证据。首次新建Osty仍未覆盖，C基础范围尚有该明确任务，不把起始对象复活当成生成证明。

009保持，无需新DLL；接已有可选准备勾“Osty首次召唤”，在checkpoint前移除BoundPhylactery并增加第三Bodyguard，使战斗初始无宠物。首回合能量药水→Bodyguard首次创建→第二Bodyguard成长→Unleash→BoneShards死亡→第三Bodyguard重召→EndTurn，第二回合Deathbringer+→EndTurn取胜。随后同片跨首次创建Back/原生再创建，核存在性/原生ID未来计数/当前节点与订阅，必要普通相关bug同包修。当前已知51份文件另存逐hash备份（`evidence/content-005/pre-first-Osty-current-files.json`），原账号/game0及当前009核对，Cloud沿本批关闭，无restore/history/其他账号遍历。

## r11：初始无Osty→首次原生创建的新片录成，跨不存在回退待测

009/session `eaf5ab334cae4db894943dab9aba8652`最终44事件、无observer/mod失败。新片`45c3b2f4c3af4782973d0bb61ea6137c`，格式8/9项/0选择、589,556 bytes，SHA `E1CCDFC37BEE7A2BCF67E416C8EFD451BBC14BBBD1ED790B33EDFDB7BEBE96BA`，recordedMod为009。准备元数据明确no-BoundPhylactery/三Bodyguard/六机制牌先天保留；实际起始遗物BagOfPreparation与Neow获得的Kaleidoscope，不含起始遗物。Initial与操作1均只有玩家盟友、Pets[]、NextIdentity3，第一只Osty不被伪装成初始已存在。

实际操作1能量药水；2Bodyguard首次生成Osty ID3/Owner0、HP5/5、Pets[3]/NextIdentity4；3第二Bodyguard成长10/10；4Unleash打第一敌27→11；5BoneShards后敌HP2/16、Osty死0/10；6第三Bodyguard同ID重召5/5。7EndTurn在敌方行动中又使Osty死亡，玩家66HP，次回合没有BoundPhylactery自动重召；8Deathbringer+给双方Doom26，9EndTurn在敌方阶段真实Doom胜利，玩家60HP、Osty死0/5、Pets清空、Next4，Defeated仍只两原敌ID1/2。死亡/复活与首次生成数据明确区分。

真实VictoryTask成功、自动seal及生产ExactPayloadMatched，额外直接生产读回/兼容检查通过（`evidence/content-005/checks/first-osty-recording/production-read.json`）；仅证明新片事实/读取，不代替跨不存在Back、未来ID原生再分配或HUD订阅重建。用户确认录制中首次召唤/成长/死亡重召视觉正常，正常Quit/shutdown/game0已核。44事件+原片/Godot共46数据文件逐hash封存`runtime/content-005-r11-20261030-152120`，摘要`evidence/content-005/runtime-verification-r11.json`，原始引擎诊断保留。

当前009不换版，播放前补存已知当前51文件/3,953,223 bytes，`CONTENT005-post-first-Osty-recording-current-20261030-152121`，收据`evidence/content-005/post-first-Osty-recording-current-files.json`，逐hash读回、原账号/game0核对，不遍历history/其他账号/不restore。下一同片第2步Back1让宠物/节点完全不存在，再原生创建（反复），核对应ID3/未来4、无幽灵节点/旧监听；合并成长与死亡/同对象重召，再Doom胜利Back8后结束回合、Stop与短重开到2生成后Stop。C首次创建恢复仍Pending，包未据录成关闭。

## r12：首次Osty跨不存在恢复通过；010在途回退等待优化候选

009/session `decb20266c38486c8243deccbe79113d`最终978事件，2recent，36次原生结果/RNG匹配（含Retiring4）、28次Back状态/RNG匹配。5次回操作1，宠物完全不存在，随后原生创建对应ID3、Next4；反复生成累计6个观看Osty引用。成长、死亡/重召及HUD通过，用户确认无残留；2次真实Doom胜利与2次胜利Back循环续接，其中一次最后EndTurn再胜，另一次再退往更早点。本轮主回放之后专门反复试EndTurn Back，最终Stop位置6，不冒充Stop9；短重开到操作2生成后Stop2。两次Stop无Error/Outstanding、Running/Auto false/Pending0，原manager/所比较Continue进度恢复，牌监听均5→0；Osty回调Known6/1各余量0。原片SHA `E1CCDFC37BEE7A2BCF67E416C8EFD451BBC14BBBD1ED790B33EDFDB7BEBE96BA`不变，无observer/mod失败，正常Quit/shutdown/game0已核。

978事件+原片/Godot共980数据文件逐hash封存`runtime/content-005-r12-20261030-153809`，摘要`evidence/content-005/runtime-verification-r12.json`。C首次创建的录制→生产读回→原生播放→不存在/未来ID/新实例/节点/回调恢复→Doom胜利Back→Stop/重开按该样本取得闭环；其他单位生成/变形/复活、任意文件点冷恢复及未测组合不因此开放。A/B/D/C代表性覆盖与下面新优化结论分开保留，不因优化重开旧矩阵。

用户新反馈在EndTurn切入敌人回合时Back，像要等音效完成且遮罩很长。009实际4次in-flight EndTurn退役等待分别2524/2424/2649/1959ms，总到达2589/2464/2690/2002ms，数据恢复差值约40–65ms。ActionMonitor/Drain要求当前原生EndTurn、敌回合及下一玩家初始化到安全边界；NEnemyTurnBanner的音效Play返回ID、不被await，故不能把相关性当音频阻塞。稳定边界Back多为几十ms，不能当优化的在途证据。

010实现有界加速：仅包装固定CombatManager.StartTurn直接调用CustomScaledWait(.5,.8)的纯阶段计时，不泛化跳过所有Cmd.Wait。AsyncLocal捕获真实StartTurn的combat state/SeekGeneration，核当前状态/代归属，再以最近native调用帧等于StartTurn async-state-machine限定直接调用，Hook/Card/VFX子调用和其他任务不匹配。Back/Seek Drain期间释放已归属的当前纯timer等待，并跳过后续同处阶段timer；原计时Task不取消、不恢复旧Task，正常fault/cancel保留，已退役纯timer的迟到异常另记诊断。纯timer内部只有await、无后续业务，并发调用方顺序是否仍等价尚待原生测试，不能由此宣称安全/性能已证明。

退役仍等待原生业务全部完成并精确核对结果/RNG；不改FastMode/Engine.TimeScale、不提前标完成、不给卡牌写播放脚本。当前room的旧CombatStart/PlayerTurn/EnemyTurn横幅退役，精确停止NDebugAudioManager中player_turn.mp3/enemy_turn.mp3当前播放，其他声音不StopAll；退役期不新建已撤销的阶段横幅，保持必要遮罩/静默和“快速收束已撤销操作”提示，不以可见地快打敌人再倒回代替延迟优化。显示/实际apply标志与SilentRebuild分开管理。日志记录phase wait的绑定/代/阵营、released/skipped，既有退役/恢复/总耗时继续可比较。

主工程/DirectContracts构建0警告0错误，4组纯async计时合同通过（正常等待/异常、释放当前不取消timer且业务续接一次、下一代正常等待隔离、取消与迟到timer异常观察）；另14组D-131、7组开场政策与原C片生产读取通过，输出`evidence/content-005/checks/phase-retirement/`。这些不执行原生方法/归属绑定，也不证明实际RNG/时序或提速。010 DLL `A6B6A9E231F9661BAAC172E2F06CABE7A46A8EE78F26E8BD326E3E8385D0AD1D`已安装，收据`evidence/content-005/preparation/build-010-3ac58595.json`；009可用pair/config及r12完整证据保留。51份当前文件/3,953,223 bytes补存`CONTENT005-post-first-Osty-playback-current-20261030-153906`逐hash读回，收据`evidence/content-005/post-first-Osty-playback-current-files.json`，原账号/固定本体/仅ReplayMod/modded1/game0核对、Cloud沿本批关闭，无restore/history/其他账号遍历。下一仅同片真正in-flight EndTurn Back/Seek，比较归属/释放/到达耗时、继续到原生胜利及Stop/重开；失败按实际等待缩窄修，不放松断言。010实际优化Pending。

## r13 / 011候选：010释放生效但提速不明显，扩通用已审pacing

010/session `04d8d6921048473bbdcc55f9fa81486f`最终346事件、两次recent打开。17次原生结果/RNG（Retiring5）、6次Back/Seek状态匹配，真实Doom胜利1，Stop9/2均Error null、Outstanding false、Pending0、Auto关，原manager/所比较Continue进度恢复；Osty清理Known1/1回调余量0、牌登记0/5→0。mod/observer错误与retired-phase-timer-fault均0，正常Quit/shutdown/game0已核。原片不变，346事件+原片/Godot共348数据文件逐hash封存`runtime/content-005-r13-20261030-160157`，摘要`evidence/content-005/runtime-verification-r13.json`，引擎诊断保留。

阶段timer真实绑定到当前state/代。5次真正in-flight EndTurn Back/Seek6的退役等待1782/2116/2023/2120/1989ms，总到达1879/2165/2066/2176/2044ms；其中2次Released1/Skipped1，3次Released0/Skipped1（请求时敌阶段timer已过）。另一次稳定7→6为52ms、Released/Skipped0，不是退役优化证据。用户反馈“提速不明显”，E不验收；不能用机制确实释放代替体验通过，也不以与009不同请求时点的均值差宣称固定提速。每次跳过玩家阶段timer后仍约.54秒才完成初始化，之前还有约1.2–1.6秒在敌行动中。

继续核固定原生共享调用：NCreature.PerformIntent末尾CustomScaledWait(.25,.4)，MonsterModel.PerformMove招式前(.1,.2)/后(.1,.4)，CreatureCmd.TriggerAnim末尾起手等待，以及CreatureCmd.Damage在完整伤害/HP/受伤/死亡Hook和Kill之后的(.1,.2)。011将这四个通用调用点的async-state-machine直接调用计时加入现有allowlist，不按牌或怪物特判；仍须在当前EndTurn的StartTurn AsyncLocal真实state/SeekGeneration内，最近native调用帧匹配才包装。未取消这些方法的业务/Hook/死亡任务，也未放行未知子方法、信号或视觉循环。纯计时提前释放的并发调用端是否仍等价必须由实际状态/RNG验证，源码不代实测。

剩余玩家准备等待涉及CardPileCmd.Add原生Tween及其Finished、AfterCardChangedPiles等回调，011保留完整流程，不用skipVisuals/假完成信号抹掉牌堆事件。必要退役遮罩/静默、旧回合cue定向清理、普通播放/录制/原游戏速度边界沿010保持。日志增加PacingSource，便于分辨实际释放的共享调用点；未知等待原样执行，不为提速降低断言。预计减少更多敌行动pacing只是待测推断，不承诺下一候选已达到目标。

主工程构建0警告0错误、既有4组纯async计时合同和原片生产读取通过，输出`evidence/content-005/checks/expanded-retirement/`；原生新增caller绑定/状态RNG/延迟Pending。011 DLL `E222994401F1B3BD89465E540645031BF7214C531EE05F5A81F9D40FD7F321A9`已安装，收据`evidence/content-005/preparation/build-011-ef2b7c26.json`，010/009 pair/config与重要数据保留。当前51文件/3,953,223 bytes补存`CONTENT005-post-phase-wait-r13-current-20261030-160200`逐hash读回，收据`evidence/content-005/post-phase-wait-r13-current-files.json`；原账号/固定本体/仅ReplayMod/modded1/game0核对，Cloud沿本批关闭，无restore/history/其他账号遍历。下一同片真正在途EndTurn Back与Seek、继续到原生胜利和Stop/短重开，按数据与体感判断，不扩全局加速平台或新矩阵。

## B：球机制源码增量，所测闭环通过

用户进行002人工测试期间继续开发，安装保持002。下一增量源码使用格式8：`Mechanics.OrbQueue`明确保存容量、有序球条目、每个原生实例的Replay逻辑ID、未来身份计数、所有者、移除标记、当前被动/激发值及各球派生模型的私有标量字段。暗球`_evokeVal`、玻璃球`_passiveVal`不会只靠模型名或当前画面推断。已有格式1–7不新增字段、不修改原片，精确兼容002录制build。

逻辑ID在原生`OrbQueue.TryEnqueue`实际可接纳处按实例引用登记，同种同值球不会合并；前进仍执行原生OrbCmd/Hook/OrbModel，不自写球效果或RNG。RunRng既有CombatOrbGeneration继续沿原生流落盘/比较。身份映射和计数器加入观看内存memento，与原队列成员、容量、球私有字段一同恢复，恢复后新原生球获得对应分配序列。

Back前移除旧原生球槽/淡出节点，让`NOrbVfx._ExitTree`用旧实例退订被动/激发和玩家Power事件；保留NOrbManager自身当前StateTracker订阅，不恢复旧delegate/Task。数据恢复后使用原生槽与`NOrb.ReplaceOrb`重建，原生初始化绑定恢复后的球对象，只完成纯显示入场/布局tween以即时到达Back位置；核对槽容量、顺序/实例、每个当前球恰好一个被动/激发VFX监听、已退场球零监听。Stop在场景退出前同样清理并核对零球VFX监听。真实节点/反射绑定/清理核对仍待实际运行，纯模型测试没有执行这些调用。

已加默认关闭、checkpoint前序列化的故障机器人准备：保留原10张和破损核心，追加暗球/冰球/扩容/混沌+，升级原电击，六机制牌先天保留、背包及集中药水；可取充能/满槽激发/暗球跨回合累计/主动激发/容量/随机球与集中的一份短片。主菜单测试区域改为可滚动容器以容纳入口。尚未给用户新一轮步骤，不在002游戏运行中替换DLL。

主工程/DirectContracts构建0警告0错误，4组球离线合同通过：同种同值不同引用身份；跨球移除/暗球累计/扩槽及反复memento恢复后未来ID；格式8生产封存读回字段；旧格式不得宣称Defect、重复/超界ID、容量和所有者拒绝。输出`evidence/content-005/checks/defect-draft/orbs.json`。这是代码/文件读取与内存模型证明，不是原生播放、HUD、订阅或冷文件恢复PASS。

格式8事实已保存并参与结果字符串校验；观看内存恢复代码已接入；从任意文件点重建球对象/ID映射的权威冷恢复未实现，也未将这些字段接入STATE-001旧单点。尚无新录→普通入口→胜利/Back→Stop的Defect实际运行结论，回合被动致死后的原生循环恢复尚待同包处理/验证。

## C：Osty/Doom源码增量，已安装待实测

沿尚未发布的同一格式8加入`Mechanics.CombatUnits`：原生未来CreatureId计数、有序盟友ID/模型、所有者、存活/HP/上限/格挡、招式、怪物RNG及Power序列，另存玩家Pets有序ID。初始文件的成员与后续出现的ID区别保留，不按同名模型或当前列表下标猜身份。Codec对所有状态及胜利内嵌状态检查容量/唯一ID/未来计数/玩家与Osty角色/所有者/Pets引用；原格式1–7不补写未知事实。输入目标增加格式8的Pet角色与稳定ID，仍只限本地Osty。

原生`OstyCmd.Summon`已经区分已有活体增长、原死Osty复活、首次`PlayerCmd.AddPet`创建。`DieForYouPower`使死Osty留场，`PlayerCombatState.OnPetDied`在此情况下也保留Pets成员和原生监听；不能把“死了”当作对象/宠物列表消失。观察期仅放行当前玩家侧的原生Osty创建，其他召援仍拒绝。Roster放行初始Osty存活/死亡状态；若初始没有Osty，可追加一个原生新实例并核对其CombatId和未来计数。其他怪生成/多宠物/一般复活换形不因此开放。

观看memento包含有序盟友/Pets、原单位对象和后续Osty锚点、所有者、HP/Power/RNG及原生分配计数。回到首次召唤前时恢复成员/计数并detach未来分支的旧宠物对象；再次召唤让原生创建新实例，不回收旧Task。每次恢复先退出旧Osty场景，数据恢复后仅场景`AddCreature`重建和绑定当前主人格挡显示，不再次执行原生业务AddCreature/AI初始化/召唤Hook。死亡循环、生命对应尺寸/位置、交互性、HP条及Power订阅按恢复后的对象处理；新节点不重复套旧节点的差量Power订阅。

恢复后用当前PCS绑定原生`OnPetDied`回调，删除同目标/方法的旧副本后按实际Pets成员恰好添加一次，不还原旧delegate。Stop在Run清理后去掉已知Osty的该原生回调并核对零余量；场景ExitTree负责原生Power/格挡/HUD退订。胜利Defeated集合改为真正敌人，避免把Osty误计作被击败敌人。Doom退场VFX在恢复前按原显示主人清理，避免旧身体留在特效视口。

回合结算（Doom、球被动等）致胜可能让原生`StartCombatInternal`循环正常返回，区别于手动击杀后仍等待EndTurn的既有路径。新增`NativeTurnContinuation`：原循环/两信号仍在等时保持原路径；否则先等原循环成功结束，创建全新的两个原生TCS，恢复数据后从原生`AwaitTurnEndAndSwitchSides`继续，再调用原生`StartTurn`交替双方。不重跑开场/起始抽牌/回合开始Hook，不恢复旧Task/TCS；异常会报观察失败。Back在Drain中才刚好致胜时也改为按Drain后的真实终局状态判断。此桥接代码仅编译/源码核对，尚无原生Doom致死后Back运行PASS。

默认关闭的Necrobinder准备保留原10张/BoundPhylactery，增加第二张Bodyguard、BoneShards及Deathbringer+，五张机制牌先天保留，背包和能量药水。拟用一份短片观察Osty增长/攻击/主动死亡/原单位重召及跨回合，随后Doom在回合结算真正结束战斗。另有默认关闭、用户明确勾选的“首次召唤”准备：checkpoint前移除BoundPhylactery并追加第三张Bodyguard，六牌先天保留，实际准备内容进入文件与日志；原片普通回放不注入。它可短取初始无宠物→首次创建/成长→死亡→复用原单位重召，供实际Back到首次召唤前再原生创建。两种准备及相应路径均尚未运行，不把默认有起始Osty的片当首次创建恢复证明。

主工程/DirectContracts构建0警告0错误；4组单位纯模型/生产格式合同、更新后球4组通过，输出`evidence/content-005/checks/necrobinder-draft/`。覆盖字段读回、非法所有者/单位/ID/未来计数、留场死宠原对象反复恢复、首次生成前成员/计数恢复模型；未执行原生OstyCmd、Doom、回合桥接、场景/反射订阅或冷文件恢复。球/单位的新文件事实与观看memento已接入，任意文件点重建单位/球及其权威冷恢复仍未实现。

当前004已安装，先请求机器人代表片；后续同包实测并修复。没有以解除白名单宣称新角色完成，没有全牌库覆盖结论；D-131、单人A0首战和用户手动游戏边界保持。
