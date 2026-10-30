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
