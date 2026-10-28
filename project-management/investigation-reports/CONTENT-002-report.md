# CONTENT-002 最终决策模式与对称双向节点

## 最终交付：DONE_WITH_LIMITATIONS · 2026-10-28

用户最终确认“终于，全部完成了”，本轮 CONTENT-002 到此收束；020 的图标尺寸也已获体验确认，不再是待用户测试项。此次只整理既有证据、文档和恢复点，没有再改功能、构建、换DLL或启动游戏。本轮无待用户操作，也不以剩余覆盖缺口阻止阶段完成。以下历次“当前候选/下一测试”均为历史记录，由本节取代。

### 当前版本与保留物

- 当前可用安装为 **build-020**，manifest 0.1.0，DLL SHA-256 `282805714F5A74B88DE6E44D6993E15B4B13ED9AFD37E14A5F4610B6A1531DD1`。封存、构建产物和实际安装哈希再次只读核对一致；安装收据 [build-020-5c6f068a.json](../../evidence/content-002/preparation/build-020-5c6f068a.json)。游戏仍为本批固定 v0.111.0-41cef1ea 同版本范围。
- 当前源码与构建期合同快照保存在完整恢复点 `C:/Projects/STS2ReplayMod-local/backups/CONTENT002-20261027-171201-97855f350a/preparations/build-020-5c6f068a/source-build-020/`；该 preparation 同时保存替换前019可用 DLL/manifest、settings/selector。最终文档单独留存，不改写构建期源码快照。完整恢复点及更早可用版本/重要失败日志保持，此次没有恢复存档或重新备份整账号。
- 原录像 `d781b488dcc64354b1eb8755c393972e` 原字节保持：15操作、8人工选择、41原始事件，SHA-256 `29B912CBA02A3F97AD92BAD4BB1B676D45F8AEACE38C9655E09B293A84835F80`。默认最终决策投影为33个演示事件，原始撤选/犹豫记录未删改。

### 这轮真正交付的行为

默认演示最终确认的选择顺序，保留原始操作事件供以后使用。选择通过原生手牌模式、抽/弃牌堆网格和生成选择窗口展示，固定原实例、原顺序和一次语义提交；观众可只读浏览，不自行改决定。已访问的开窗、选择前缀、提交后效果和稳定点组成双向语义路线，Step/Back 沿同一节点序列导航。Back 恢复真实内存数据/身份/RNG，不从开头重置后自动播放，不复活旧Task/TCS；不是任意半动画逐帧倒放。

新业务只在前沿执行一次；历史前进用源显示时钟、原生纯显示入口和只读样本，目标数据在选择/稳定边界提交。Step 可提前接收一次请求，按正常时钟等语义边界，不快进跳尾或重复走节点；Back/Pause/Stop 保持互斥和安全退役。选择页开启、手牌入场/整手让位、高亮/费用、药水、能量与回合显示、出牌、消耗及牌堆反馈均完成了本片段反馈闭环。意图保留同一原生控件；死亡、同伴强化/力量图标按显示时钟呈现，正常前进允许专有视觉尾跨节点，Back/Stop 清理。最后020补齐原生图标尺寸策略，用户确认最终体验。

### 用户确认与机器证据分别记录

最终用户体验确认来自PM转达的直接用户原文，覆盖本轮此前提出并逐次修复的问题，包括020图标大小。它不是“所有内容/组合/性能测试通过”的机器结论。

最终 r20 runtime `7a9a35cea336424e8e323c7b3f278a6f` 身份为020：935事件、12原生操作、54路线到达、37恢复、15历史显示；一次宿主12/节点23 Stop，Error=null，原manager和所比较Continue/progress保持，未打到本次胜利。21共享池检查正常；15条记录动画与实际解析一致；1次死亡显示、15次Power交还。Ravenous与Strength图标日志均为40×40、IgnoreSize/KeepAspect；这是控件参数证据，不是像素截图测量。初读与最终归档时均观察游戏进程0，属于此次实测观察，不从旧安装收据外推。

原935事件与Godot日志逐SHA归档同恢复点 `runtime/content-002-r20/`；公开 [runtime-r20.json](../../evidence/content-002/runtime-r20.json)、[runtime-verification-r20.json](../../evidence/content-002/runtime-verification-r20.json) 与 [final-delivery.json](../../evidence/content-002/final-delivery.json)。原片SHA再核对保持。最终未重新扩大存档目录审计；上一r19的54项保持证据保留，最终Stop自身对原进度的比较另有本次日志支持。

020构建零错误/警告，既有显示时钟/导航、5项最终路线数据合同、原片生产读取检查已通过，见build-020相关证据；此次收束没有重跑。旧018曾有完整15步胜利，最终020只到宿主12，不把旧胜利冒充最终build全程复测，也不由纯合同推出所有原生UI集成通过。

### 已知范围与保留的未实测项

- 当前对称路线中，**同一牌效内连续/嵌套选择**及**普通手牌/网格选择中Stop**没有专门验证。旧CONTENT-001连续弃牌/结果级回退、旧D-108初始选择Stop等证据仍只属于原范围，不转移为D-109/最终020的路线覆盖。本次Stop为稳定点，PendingNativeChoiceCancelled=false。
- 原生bundle、二候选布局等虽有接入代码，未有充分新原生覆盖；牌库升级/变换等专用预览没有并入本组支持。保存原始点击事件不等于已交付按原始犹豫节奏的全操作复演模式。
- 当前是固定版本、原测试内容的一场战斗原型；单位生成/分裂、玩家死亡/宠物、任意模型形态变换、胜利终点继续回退、后续房间/整局、多人和一般跨版本不在完成承诺内。不是所有牌/遗物/药水/自定义shader与VFX组合的通用保证。
- 没有完整新矩阵、穷尽取消/长时间池压力测试或GPU帧率基准；日志阶段耗时不等于全内容性能验收。专用投影/实例/RNG核对不证明一切未纳入字段。以上作为后续覆盖边界保留，不是今晚追加任务。

本轮按 **DONE_WITH_LIMITATIONS** 交付，保留当前可用安装、源码、原片和重要失败；后续范围由用户与PM再定。以下均为过程历史。

当前修复候选build-005，DLL `D045E580FE2E8F06AE50EF910186E900C384DF133C34E0744A297506D4281DF6`，收据build-005-4f785995。r3/build-004出现全部Back点击无效，D-109不通过；UI刷新已修，构建零错误/警告，尚待短测。以下build-004候选说明和架构保留，实际r3及005修复见文末。

2026-10-27，D-109修订已实现为build-004候选并安装；**新目标尚待原生实测**。build-003按旧D-108目标的用户确认和机器日志已核对通过，不算新目标通过。当前DLL `86AA29E3A7DC38328348A13D5CD9EACB163E5703D2CD358B500D48EC248DF9D1`，收据build-004-cbd899fa。Release零错误/警告、五项新路线合同/七项既有选择合同通过；原片不改、直接复用。管理决策/任务由PM按用户新原文修订，取代原“仅宿主Back”约束。

## D-108首版记录（历史，以下安装状态由D-109交付取代）

2026-10-27，D-108/D-092。**r1/build-002真实选择过程及完整胜利录制核对通过，尚待原生观看/子动作Step/选择中Stop验证。** 当前build-003修正旧片结果演示的多余确认，Release零错误/警告、七项数据/控制合同通过，并已安装、显式兼容这份build-002原片。不挪用CONTENT-001的36操作/19后退作为本包观看结论。身份及保留旧pair收据见 `evidence/content-002/preparation/`。

录制版build-002 DLL SHA256 `0C6FEC6168D75F73C53388FBE0112E9681497159CA7E6556E4BA04BBD28B6DA0`，当时sealed/output/installed一致，manifest0.1.0，收据build-002-fac00f36。其安装前53个saves只存在上轮已知progress备份差异是录制前事实；本次新局产生了正常进度，已更新完整恢复点。原F658 pair/settings/selector仍在CONTENT001恢复点preparations/build-001-43400a4c。

当前build-003 DLL `0247258A51E615F1841950F29675C00DB75877940436F59123A2927D51A74369`，收据build-003-ecfe6d27，尚无实际观看日志。仅修旧片ResultDemo：FromChooseACardScreen实际点选自动提交，不能因CardSelectCmd元数据Manual=true再造Confirm；逻辑移入生产ChoiceTrace，数据测试同时覆盖该标志与旧CONTENT-001实际15帧结果演示，未重跑旧战斗矩阵。新片有原始Events，不走该结果演示分支；新增0C6F精确前驱兼容，生产ClipStore.Read已独立读取原片并保持原SHA。录制版pair/settings/selector保留于新CONTENT002恢复点preparations/build-003-ecfe6d27。

同次准备阶段build-001（6628D5D7…）未运行；最后源码核对将Close采集移到NOverlayStack.Remove/手牌AfterCardsSelected真实入口，原生关闭方法返回后再释放scope包装等待，避免原生TCS同步续接令外层牌效/下个窗口早于旧窗口移除。相应改为build-002并重新封存安装；保留未测build-001收据与pair，不能把它写成失败运行。

## 实现与边界

ChoiceTape保留原CardSelectCmd叶入口、候选、scope/宿主操作和最终原生结果核对。观看ShouldSelectLocalCard改走本地原生UI，由NativeChoice在原界面调用原决策控件一次，原命令自己SyncLocalChoice/SignalEnded/执行后续效果；Remote直接投递旧方案已移除。当前native choice ID仍用live单调ID，Back只恢复已完成操作的逻辑选择位置，未恢复Task/TCS。原生自动/确定分支不强造UI或点击。

接入NPlayerHand原选择模式、NCombatPileCardSelectScreen的真实pile/filter、NSimpleCardSelectScreen、NChooseACardSelectionScreen、NChooseABundleSelectionScreen。手牌简单模式满选替换先录实际隐式撤选，再录新选中；嵌套自动Complete不伪造一次用户确认。bundle撤回预览为Cancel后重新选择，非结束整条动作。新格式4可选Events保存Open/Select/Deselect/Confirm/Skip/Cancel/Close及相对scope时间，候选索引指向当次固定原实例，最终有序结果仍单独验证。手牌顺序严格核对；网格原生HashSet可能复用删除槽，过程末尾核对成员，真正返回顺序另与原片逐项核对。

旧格式4无Events，只能显式标记结果演示。ResultDemo使用最终结果，通过原生窗口演示选中/必要确认，不能推测原撤选/点击节奏。旧片原字节/hash保持，可选null字段不参与序列化；实际旧fc6432b4…文件checksum经过本包读取检查。明确兼容F1D9与F658原录制build，未修改原期望。

PlaybackCursor分开完整宿主操作和选择动作许可。Loading初始选择、Busy中的选择也可Play/Pause/Step；Step到打开停止，下一Step执行一次真实语义动作，自动提交时不人为加确认。操作计数只在整个宿主稳定后增加。Back在选择中明确禁用，Stop仍可取消本次原生completion source、清理窗口/手牌模式并退出独立会话；不悄悄提交决定来抵达稳定点。初始Stop早于UI打开时，后打开的等待也取消。以上清理的实际时序须由新运行确认。

观看选择时透明输入遮罩仅放行原生浏览，选中/撤选/确认/跳过/取消入口在控件层阻止观众输入，含快捷键入口；所需mod内部驱动使用同一入口。选中容器外层也阻止移除holder，避免拦截下游后留下半个撤选。普通出牌/药水/结束回合仍阻止；意外自动动作保留诊断。暂停继续前比较专用投影/RNG/选择cursor，专用投影不访问尚未初始化的敌人意图。投影未变不等于所有内部字段均已证明。

网格先按原CardModel对象在完整排序列表定位，计算原生滚动位置并重配虚拟holder，再校验holder仍指向该实例及聚焦/原生选中。观众暂停时的滚动/悬停不写入录像、不改变决定或逻辑光标；未做精美焦点。Play目前0.6秒子动作间隔、窗口首次点击前保留原生约0.4秒入口保护；记录时间保留但未做原节奏播放器。等待观众不计60秒宿主超时，提交后原生选择不结束另有60秒诊断。

牌库升级/变换/附魔专用预览确认UI没有并入这组支持，观看明确报native-deck-preview-process-not-supported-yet；并发、任意半态倒放、所有卡牌与整局仍不包含。二候选数据/原生同控件路径有代码支持，尚未实际二候选布局验证；bundle也未实测。

## 探寻打击与准备

固定安装PCK只读取得localization/zhs/cards.json：SEEKER_STRIKE.title为“探寻打击”，中文描述为抽牌堆随机若干张选一。源码SeekerStrike先原生攻击，再用CombatCardSelection RNG StableShuffle/Take(Cards=3)，经FromCombatPile filter筛子集。实际UI保持抽牌堆选牌，候选不会扩成全堆，不伪装成生成牌三选一。映射短证据 `evidence/content-002/native-card-mapping.json`；原生实际表现待用户本轮样本。

默认关闭的一次性ChoiceProcess准备：新铁甲A0首战双蛞蝓，34张deck，秘密武器、探寻打击、全息影像+、早有准备+、发现、重锤+六张用原生RoyallyApproved附魔取得先天/保留；其余28张铁甲打击。直接AddKeyword不进入原生SerializableCard，因此使用会序列化并在FromSerializable调用ModifyCard的原生附魔。工具箱、准备背包、蛇之戒、灯笼、锚；赌徒佳酿、攻击药水、能量药水。实际checkpoint应含牌/附魔/遗物/药水，观看不重新注入准备，实际文件待核对。

Cloud用户已为本批确认仍关闭；换DLL前核对游戏0。没有新正常游玩进度，复用完整恢复点CONTENT001-20261027-145813-b34d252680；1279文件/132049476 bytes原读回记录沿用，不重审整包。原F1D9已测pair与F658当前pair均保留；上轮progress.save.backup与主progress相同的后副本及原前副本继续留存，不声称全存档零变化。不自动启动游戏、headless、Computer Use或其他账号读取。

## 第一段人工录制（r1已完成，保留原步骤）

1. 手动启动，主菜单只启用“CONTENT过程测试”，新建铁甲A0；古代事件选不改牌组的选项。工具箱开场亲自任选一张，不急着打赠牌。六张机制牌应有先天/保留附魔。
2. 先打秘密武器：在较长的抽牌堆选择中滚动到下方，任选一张打击。再打探寻打击，对尚存活敌人攻击后，从它筛出的最多三张里亲自选一张。
3. 打早有准备+：先选一张打击，再点击选中区把它撤选；然后重新选它和另一张打击，确认弃两张。保留全息影像+和重锤+。打发现，并喝攻击药水，分别完成原生三选一；随机新牌无需打出。先不要杀光敌人。
4. 喝赌徒佳酿，把除全息影像+、重锤+外的手牌选中并确认弃掉重抽；然后结束回合。保留的两张留到下一回合，弃牌堆应积累较多卡。
5. 下回合打全息影像+，在弃牌堆中滚动并选择一张。若列表仍不足以滚动，不必重录：先多结束一个回合，随后再次抽到全息影像时用它；日志会核对真实候选数。之后正常打赢，能量药水可供重锤击杀；不要求打随机赠牌、不刷掉落。
6. 胜利自动保存后，不领奖或去下一房间；保存退出主菜单并正常退出游戏，告知完成。若出现未封存/报错或机制牌不符合准备，保留提示正常退出，不反复重录。先核对原文件Events、真实撤选与附魔checkpoint，再给对应观看步骤。

## 第二段观看停点（待录制核对后发送）

最近新片菜单打开，开场工具箱保持原生窗口且暂停；Step自动选择。沿所录操作Step至手牌/抽牌堆/弃牌堆界面，每次打开停住；暂停滚动/悬停并尝试观众误选后Step应定位原目标，按录制撤选重选和确认走。选择中Back明确不可用，完整宿主结束后Back/重做应再呈现同一过程。至少一次初始/普通选择中Stop并重开，最终Play到胜利、Stop和短重开，正常退出。按实际覆盖记录，二候选/未出现机制维持代码支持而非通过。

## 检查与交付停点

`evidence/content-002/choice-process-contracts.json`六项检查生产ChoiceTrace和PlaybackCursor：撤选重选/有序手牌/长停留roundtrip；非法索引/时间/顺序/提交后点击拒绝；合法空确认和二候选自动提交；HashSet成员语义；Loading与Busy子动作许可/Play暂停/Back稳定边界/Stop；实际旧格式4原checksum保持。未实例化原生游戏对象、未headless。Release构建通过；实际界面、选择中Stop、隔离和胜利仍待人工，不以这些纯检查替代集成通过。

源码README、默认关闭准备、安装封存与人工步骤已交付。PM管理状态由PM维护；不扩大本任务到精美焦点或完整半态seek。

## r1 真实录制核对与观看请求

用户确认撤选重选、抽/弃牌堆选择正常，胜利自动保存后正常退出。Runtime `e355b181851a40f3b17357e80cf27426`，PID49976，82事件/零failed、error、unsupported或difference；两条menu-ready对应启动和胜利保存退出回菜单，当前进程0。没有独立shutdown事件，不把返回菜单日志单独当退出证明，结合用户报告与game0。

原片 `d781b488dcc64354b1eb8755c393972e`，格式4，1717632 bytes，seed YRV09DTLFQTX、首排col3双蛞蝓，build-002/0C6F录制。生产Codec/ChoiceTrace/ClipStore独立读取、原envelope及byte hash验证通过；34张实际deck、6个原生RoyallyApproved附魔都在checkpoint。每个Initial/操作After的ChoicesConsumed与边界帧数一致；这不是播放通过。

| 选择序号 / 宿主边界 | 来源与真实候选 | 过程及结果 |
| --- | --- | --- |
| 1 / 0 | 工具箱，3张 | Open→Select0→Close；永恒铠甲，生成后combat ID34 |
| 2 / 1 | 秘密武器，抽牌堆25张攻击 | Select4，打击ID8实际入手；有20.8秒真实UI停留，不据此声称实测了60秒以上选择 |
| 3 / 2 | 探寻打击，筛出的3张 | Select1，打击ID13入手；3个原实例均来自该时点实际24张抽牌堆，保留筛选子集 |
| 4 / 3 | 早有准备+，手牌10张 | Select6→Deselect6→Select4→Select8→Deselect4→Deselect8→Select4→Select9→Confirm；最终ID0、4实际弃入discard |
| 5 / 4 | 发现，3张 | Select0→Close；战鼓生成后ID35，无人为确认事件 |
| 6 / 5 | 攻击药水，3张 | Select2→Close；飞剑回旋镖生成后ID36，无人为确认事件 |
| 7 / 6 | 赌徒佳酿，手牌9张 | Select0/3/4/5/6/7/8→Deselect6→Select6→Confirm；最终顺序0/3/4/5/7/8/6，7个实例实际弃牌后重抽 |
| 8 / 8 | 全息影像+，弃牌堆18张 | Select9→Close，原飞剑回旋镖ID36实际回手 |

8帧全Automated=false，native ID0…7。41个原始语义事件为8 Open、19 Select、4 Deselect、2 Confirm、8 Close；逐帧与82事件日志中的choice-recorded-event逐项比Kind/Option/时间，8个最终结果与choice-completed一致。原始过程不是根据Chosen补造，包含早有准备的3次与佳酿的1次撤选。3次原生生成日志对应工具箱/发现/攻击药水。操作10重锤击杀第一只，11飞剑回旋镖、12 EndTurn后13/14/15打击，15实际胜利并自动封存，原生VictoryTask成功。未测二候选、bundle、skip、deck专用预览或全部牌库。

独立证据：`recording-files-r1.json`、`recording-files-build-003-r1.json`、`recording-verification-r1.json`、`recorded-choices-r1.json`、`runtime-r1.json`。原checkpoint不公开。原clip byte SHA256 `29B912CBA02A3F97AD92BAD4BB1B676D45F8AEACE38C9655E09B293A84835F80`，未改变原Events、期望或原录制ModHash。

本次正常进度完整恢复点 `C:/Projects/STS2ReplayMod-local/backups/CONTENT002-20261027-171201-97855f350a/`，1280文件、132063795 bytes，来源/副本全部读回且来源稳定。原活动账号匹配、game0、Cloud沿本批用户确认关闭；另一账号排除，无整存档恢复。原片与82个原始事件另逐SHA归档runtime/content-002-r1。备份收据 `recording-archive-r1.json`；观看期复用此点，不再重复全账号备份。

本轮只请求打开这份新录像，无需重录或启用准备：先初始工具箱窗口Stop并重开；再Step完成工具箱到0/15，Step打开秘密武器窗口后Stop并重开；第三次按Step看25候选抽牌堆暂停滚动、探寻3候选、早有准备的真实撤选/重选/确认和后续小选择，稳定点1/3/8分别Back一步并重做。第8步全息影像18候选时暂停滚至别处，下一Step应定位原ID36；手牌/小窗口中观众误点击不能替换原决定。之后Play到15真实胜利、Stop，重开短Step/Stop，正常退出。选择中Back应有明确不可用提示，Stop可用；遇失败保留提示退出，不继续反复重录。上述为D-108当时请求，实际结果见r2；Back限制已被D-109取代。

## r2 / build-003旧目标实际观看

用户确认整体符合旧预期且无bug，同时要求D-109。Runtime `9d1e4adb8fca49d7a176361fbe44039e`，446事件、零failed/error/unsupported/difference。3次room-load/closed：首次在初始工具箱Stop，尚未Initial匹配；第二次25次实际forward含重做、全场到15胜利；第三次重开Step操作1后Stop。合计26 forward/10直接恢复全部StateMatched、恢复0prefix/无room重建；playback-open仅2条是因为初始取消未加载完，不是失败。

恢复4→3、3→2、5→4、6→5、10→9、9→8两次、8→7两次、7→6。17个选择窗口、16次完成/交付、1次初始取消；46个暂停投影/RNG及选择cursor比较通过；6次原实例定位holder匹配（秘密武器排序下标19两次、探寻下标1一次、全息下标9三次）。3次关闭均原manager/比较的Continue与主progress保持，含完整胜利、初始Stop与短重开；没有普通选择中Stop记录，按实际记未观察，不为已改范围补跑旧矩阵。

原片SHA未变，相对CONTENT002完整恢复点，54个modded1/saves内容全部相同（原汇总53已于r4按实际清单补正；不等于证明没有同值写入）。原始446事件逐SHA归档runtime/content-002-r2，D-109前源码留source-before-D109；安全日志和断言runtime-r2.json、runtime-verification-r2.json。这些只支持旧目标，不支持新默认及对称选择路线。

## D-109 / build-004候选实现与边界

最终路线由Chosen有序实例投影，省略思考撤选，保留原始Events。实际原片41→33事件；早有准备11→5，佳酿12→10，佳酿最终0/3/4/5/7/8/6保持原生生效顺序。旧无Events片明确是最终结果展示，第二套原始过程模式未产品化。

DecisionCursor/DecisionRoute区分显示历史与原生执行前沿。真实待决点保存图数据和原生UI规格（来源、prefs、原对象、pile与冻结候选子集）。同一未提交前缀用原控件正选/撤选，跨窗口/确认后/跨Open恢复对应实际数据，再创建新原生只读UI及前缀。牌/药水/能力/历史/牌区/房间完成标记/玩家敌人/RNG投影核对，额外核对持有但不在槽/牌堆中的来源和候选。Room.IsPreFinished也恢复，防止只退UI却保留胜利完成标记。

业务只在新前沿结算一次；历史确认直接恢复已保存的后态，不重复出牌、用药、生成/变化/抽牌或遗物触发。历史Play自动走到前沿后再接新命令。CardSelectCmd保留Reserve/SignalBegun/Ended及原效果，观看走remote适配的当前语义等待；NativeChoice创建同类原生UI和独立展示等待，UI原实例/顺序核对后只在前沿交付一次原生结果。历史非展示业务入队拒绝；浏览仍是原生滚动/悬停，不写决定或消费位置。

当前仍挂起的语义等待保持挂起；历史UI新建展示TCS，二者隔离。Task/TCS/执行对象不进路线memento，不复活已完成旧任务。Owned根包含浮动/未选生成牌、已移出槽的在用药水及已遇到来源模型，避免恢复牌堆却遗漏被当前命令持有的可变对象；回前沿先还原这些字段。Native choice IDs单调，显示消费位置随节点恢复，到前沿才继续当前等待。Stop关闭展示等待，历史区先回前沿上下文，再取消尚未提交的当前语义等待，不用确认来退出。

共享DirectCheckpoint的route分支支持待决/只读历史，旧格式3仍RequireReady。新增房间根/执行字段过滤仅用于路线memento，旧STATE静态绑定过滤不变。恢复同run/room/turn对象，非从头重演。最早初始窗口之前没有历史；连续确认边到下一个真实选择；胜利结果也接入历史后退/再前进，终点不派新业务。这些均是尚待实测的新实现，不因代码存在或PM允许就算通过。此能力只在本次观看内存，不扩文件强记录或每个半态可接管分支。

牌库专用预览、并发/新单位/其他角色入口等限制保持。二候选、bundle、复杂卡牌和新状态/异步路径未因构建而被宣称实际支持全部内容。原格式和原事件未改，未加新schema、未改expected。

decision-route-contracts.json五组生产数据/控制检查：实际原片最终顺序与原始撤选保留；选中前缀同边双向；用生产IdentityMemento还原模型状态/RNG/触发计数而不再执行效果；历史Play到前沿才请求新执行；非法/重叠跳转、Stop及源byte完整性。七组旧选择合同见choice-process-contracts-build-004.json。检查未实例化游戏/原生控件，不替代集成；生产ClipStore已读原build-002片并验证当前build-004精确兼容，recording-files-build-004.json保持原ModHash/SHA。

已装build-004，SHA `86AA29E3A7DC38328348A13D5CD9EACB163E5703D2CD358B500D48EC248DF9D1`，sealed/output/installed一致，manifest0.1.0；收据build-004-cbd899fa、验证build-004-validation.json明确未原生运行。game0、仅ReplayMod/modded1、Cloud沿本批用户确认关闭；最新CONTENT002完整恢复点复用，已测build-003/0247258A pair/settings/selector保留其preparations/build-004-cbd899fa。无整存档恢复、其他账号访问或自动启动游戏/headless/Computer Use。

## D-109当前人工检查

无需重录/开启准备：最近15操作片，初始工具箱完成后Back回窗口，再Step；秘密武器/探寻选择后Back重开，再Back跨Open回操作前，检查原牌/牌区和探寻伤害撤销，再沿原路Step。早有准备默认只选最终两张并确认；确认后Back回“两张已选”，逐Back到一张/零张/操作前，再Step/Play前进。佳酿只选最终七张，不重演原思考撤选。全息影像确认后Back应把回手牌退回弃牌堆并重开选择；暂停滚至别处，下一Step重新定位原实例。从历史点Play自动接回前沿，完整胜利；可从胜利Back一次再Step回胜利，核对未重复伤害或结算。

菜单“打开已有连续选择样本（CONTENT-001）”只读原fc6432b4…，不改latest或Continue。到第11步EndTurn后的必备工具/暴政/熵连续选择，在相邻窗口间Back/Step，检查前一决定的弃牌/消耗/变化撤回/恢复；在历史选择处Stop，重开短Step/Stop后正常退出。最近片完整胜利加这个连续停点即可，不重跑旧16操作全矩阵。若报错保留提示退出，不反复重录。

## r3 / build-004 Back输入失败与build-005修复

用户报告所有Back无效。Runtime 9f17246dda5645aebf63a6b3edd36553，168事件；15个原生操作After匹配、8个选择结果和8次前沿交付、33个最终路线节点、完整胜利/Stop，所比较原manager/Continue/主progress保持，Error=null。**零Back请求、零route-arrived/数据恢复**：这说明请求没有进入控制器，不是后退已经运行且成功。用户观察与机器诊断一致，D-109仍不通过，不能以零error或正向胜利掩盖Back失败。原始168事件逐SHA保存CONTENT002恢复点runtime/content-002-r3/session，安全日志/断言为runtime-r3.json、runtime-verification-r3.json。

Ui.Refresh每帧先按旧Cursor/BackDisabledReason写Disabled=true，再按DecisionRoute.CanBack写false；旧逻辑在选择中、初始点、没有旧稳定点字典和终局均会禁Back，因此新Back虽看起来启用，却反复被重置。Godot 4.5的[BaseButton::set_disabled原始实现](https://github.com/godotengine/godot/blob/4.5/scene/gui/base_button.cpp#L241-L255)明确在禁用时清空press_attempt/pressing_inside，而默认释放触发Pressed依赖这些状态。结合本地赋值顺序和零Back请求定位了输入层根因。

build-005仅把路线与旧UI控制状态放进if/else互斥分支，四个按钮不再先禁后启；状态/恢复架构未改变。增加Back ButtonDown/ButtonUp日志，区分下次是未收到按下、未形成点击、控制拒绝还是恢复异常。Release零错误/警告，未再重复不涉及此次UI输入的纯光标检查，也未实例化正式游戏/原生按钮或headless伪造验证。

已装D045E580…81DF6，sealed/output/installed匹配，收据build-005-4f785995。失败build-004/86AA pair/settings/selector保留当前恢复点preparations/build-005-4f785995，原片SHA仍29B912CB…835F80。game0、同批Cloud用户已确认关闭、仅ReplayMod/modded1，未恢复存档、未访问其他账号。build-005-validation.json明确按钮修复未运行、后退恢复尚未真正测到。

下一段只请求短测：最近片初始工具箱Step完成至操作0，再Back回工具箱，然后Step返回操作0，Stop正常退出。若按钮能发出Back但恢复出现新错误，保留本次失败继续同包修；不先要求重录/完整胜利/旧矩阵或重新申请范围。短测通过后再继续D-109选择路线检查。

## r4 / build-005初始选择双向恢复通过

用户确认Back回工具箱、Step和Stop正常并已退出。Runtime 503cf886ad274f55b4553af6f3c95803，PID27636，Mod D045E580…81DF6，37事件、零failed/error/difference。Back按下/释放均Enabled=true，控制器收到并接受Back；路线1→0进入历史工具箱、0→1回已完成初始状态，两条route-arrived均StateMatched=true、NativeEffectsRedispatched=false、RoomRebuilt=false、PrefixOperations=0。到达前的数据/RNG投影与Owned来源/候选比较均经过生产断言，非只倒放光标。

实际牌区：Back时Hand0/Draw34，Step回结果Hand10/Draw25，其中原永恒铠甲战斗ID34在手；HP80、能量4/3、格挡10及药水/遗物UI核对保持。card-generation-completed与route-choice-commit都只有1次，历史Step没有重复生成/交付。展示UI取消1次是关闭历史只读窗口，不是第二次原生选择。Stop位于宿主操作0，checks仅initial，AllOperationsMatched=false为预定短停，不是失败；Error=null，原manager/比较Continue与主progress保持，当前game0，与用户正常退出报告一致。

原始37事件逐SHA归档CONTENT002恢复点runtime/content-002-r4，安全日志runtime-r4.json、断言runtime-verification-r4.json。原片byte SHA未改。按最新恢复点真实清单比较54项modded1/saves，内容均相同；不等于宣称没有同值写入。r2汇总曾硬编码继承旧恢复点的53项，本次补正为新恢复点实际54项并保留原值/更正注记；r2原比较循环本就覆盖该完整元数据清单，原始事件不改。

本次证明按钮修复及初始“已提交选择→Back→原窗口→Step→原结果”路径，不证明仍挂起原生等待时跨Open、多选/牌堆/连续选择、历史Stop或终局恢复。当前DLL保持，不为已通过初始点再换版或重录。下一短段：秘密武器窗口未选时Back跨Open再Step；探寻选择后Back重开、再Back撤销前段伤害并重做；早有准备确认后逐Back两张/一张/空选/操作前，再Step/Play回第3步结果并Stop退出。读这段后再继续佳酿/全息/连续/胜利导航，避免未测路径出错后要求用户重复整场。

## r5 / build-005显示失败与build-006候选

用户报告早有准备+/发现打出后的中央卡牌不随节点恢复、半途Back悬空；佳酿历史Step缺抽牌动画，牌堆数量错误且无法浏览。Runtime 193ca1b0691b41f998771bf4348a12e0，793事件，82 route-arrived、52实际图恢复、7原生操作检查。遍历节点0…23，覆盖多选各前缀，最后显示回节点1、执行前沿仍为操作7；历史Stop退出，无当前原生待决选择。Error=null、所比较原manager/Continue/主progress保持，54项saves内容保持，原片SHA仍29B912CB…835F80。用户正常退出后game0。原始793事件逐SHA归档当前CONTENT002恢复点runtime/content-002-r5/session；安全日志runtime-r5.json及runtime-verification-r5.json。

本轮判失败。旧ChoiceProjection/RNG、手牌布局断言只证明其覆盖的逻辑及部分展示；没有中央PlayContainer、牌堆按钮缓存/文本或历史迁移动画诊断，不能从零error推断画面正确，也不能宣称从旧日志独立复现了用户看到的每一帧。

源码缺口：DirectCheckpoint.RefreshUi只重建手牌、调用牌区ContentsChanged，未清理/重建中央原生NCard。NCombatCardPile的_currentCount和label由CardAddFinished/CardRemoveFinished递增递减维护，ContentsChanged不刷新它们。历史Step恢复图后直接Snap，缺少原生业务首次执行时的迁移展示；输入遮罩仅选择中放行，稳定历史结果的牌堆入口被遮住，native UI还可能保留等待中禁用状态。另发现展示用SelectCards传来源模型，AfterCardsSelected会注册来源ExecutionFinished；历史来源可能早已结束，或取消后该回调仍挂着，可能遗留选中容器及迟到的归手。旧逻辑断言未覆盖此风险。

build-006新增RoutePresentation：每个节点按真实Play牌区同步唯一中央NCard，清理已不属于该节点的中央/Ui直接卡及旧peek tween；同步抽/弃/消耗按钮_currentCount、原生label、图标和位置。展示手牌选择传null来源，使选中容器在展示完成/取消时立即收回，不注册业务来源事件。历史数据仍原对象/RNG恢复，不调用旧业务任务。只读观众输入在导航展示完成后放行，三个牌堆按钮随真实就绪/overlay/手牌peek状态切换，原生手动出牌/喝药/EndTurn均拒绝；导航自动关闭只读牌堆浏览窗。

在原生CardPileCmd.GetTweenForCardsChangingPiles及AddDuringManualCardPlay入口只采集实际成功的原实例/来源/目标顺序，按相邻已访问节点保存。历史正向在逻辑恢复后播放NCard纯展示迁移，隐藏最终已抽到的holder直到显示到达；不调用CardPileCmd、不发牌堆事件、不交付选择、不重新计算效果。迁移结束核对ChoiceProjection/RNG未变，重新对齐手牌/中央/牌堆。Back直接同步目标节点。此次不扩为全部攻击/敌人动画的录像。Ready还覆盖当前导航展示任务，避免动画/重开窗口途中接受重叠导航。

新增route-display-verified、route-restored-display-verified核对中央成员/悬空直接Ui卡/牌堆缓存与文本；route-transfer-captured记录实际前沿迁移；route-transfer-animation记录历史纯展示且状态/RNG保持；route-pile-browse记录原生浏览入口、实际/显示计数及是否打开。此前793事件不伪造这些新诊断。

Release零警告/错误。原片生产ClipStore读取、文件校验/候选实例/兼容当前viewer通过，recording-files-build-006.json；不把它当新原生展示通过。build-006-validation.json明确未构造正式游戏/原生UI、尚无新画面复测，D-109未验收。已装FB40ADB4…88CFE，sealed/output/installed一致，收据build-006-0a69ad5e；005失败pair/settings/selector保留当前恢复点同名preparation，006源码也归档。用户本批Cloud已确认关闭，原活动账号匹配，仅ReplayMod/modded1，game0，未自动启动/使用Computer Use/做整存档恢复或访问其他账号。

下一轮无需重录/完整胜利：最近片在早有准备+/发现选择中及完成后Back跨Open到出牌前，再Step，检查中央卡随节点出现和清理。继续到佳酿结果，Back一次再Step，观察抽弃展示与牌堆最终数量，分别打开抽/弃牌堆核对；Stop正常退出。只在这两处通过后继续连续选择/终局历史导航。

## r6 / build-006逻辑检查通过，历史动画效果未达标

用户反馈Back逻辑应该正确，但Back后重新Step所有动画看起来错误/简化，询问是否刻意设计或绑定错误。Implementation已说明：006的统一ghost迁移动画是自行采用的临时展示取舍，不是原生动画绑定错误，先前没有向用户清楚说明视觉差距；不能以已有状态校验把该效果当通过。此轮主要问题是确认原因，已完成源码核对/证据交接，不擅自另扩全动画平台，不换DLL、不再让用户重复测006动画。

Runtime d9e325b6e432498da48a1fd240bd1229，1035事件；80 route-arrived/62图恢复、109节点显示检查及62恢复显示检查均无错误，29 route-transfer-animation后的状态/RNG不变。前沿原生操作1…12匹配，8次选择交付。Stop发起时显示节点28、执行前沿11，退出前当前操作12结束并核对，AllOperationsMatched=false、VictoryReached=false为实际未完成全场；Error=null，原manager及比较Continue/主progress保持，game0。只观察一次Draw原生浏览，实际23/显示23/Opened=true；该事件返回时IsEnabled=false是打开浏览后的禁用状态，不能因此误判未能点击。未观察Discard浏览，不据用户Back总体表述补造该项通过。

原始1035事件逐SHA归档当前恢复点runtime/content-002-r6/session，安全日志runtime-r6.json、汇总runtime-verification-r6.json。54项saves内容与备份相同，原片byte SHA仍29B912CB…835F80。未写生产代码/构建/换DLL/恢复存档/访问其他账号。当前仍006/FB40ADB4…88CFE；D-109未验收。

源码证据：DecisionRoute.Move历史分支先恢复目标Data，再RoutePresentation.Animate。Move只含Card/From/To，Animate逐张创建临时NCard，固定.65起始scale、.16秒global_position/scale tween、.17秒等待，结束销毁并Snap。其采集没有原生批次并行、实际起止时序、FastMode/原生飞行弧线/拖尾/消耗/生成/攻击等完整展示语义。由此本来不同的动画被同一种移动代替；历史区也没有重新分派原生业务来产生原效果。此处是实现偏差，不是某张卡动画资源绑定错。

核对原生CardPileCmd.GetTweenForCardsChangingPiles：除创建动画还会MoveCardNodeToNewPileBeforeTween、重挂Hand/PlayContainer，部分分支InvokeCardAddFinished，使用NCardFlyVfx/NCardExhaustQuickVfx/NCardExhaustVfx等。NCardFlyVfx.PlayAnim还读取当前Model.Pile、发完成通知；其Ready用Chaotic视觉随机生成弧线速度等。直接把这个入口接到已恢复结果的历史节点，不等于安全复用纯动画，可能重触通知或使UI重复移动。下一实现需要明确前进展示语义及可安全复用的原生显示边界，保持现有真实Back数据恢复，不复活旧Task、不重新累计业务效果、不从头可见重演；不能再把统一插值称为原生重播。

已把用户原话、上述实际范围及原因发送PM，管理文档由PM维护。后续原生动画一致性的具体实现/验收目标不由这轮有限日志推定完成；本轮不再请求人工重复测试。

## D-110 / build-007原生展示切片候选

PM直接读到用户动画反馈，明确沿CONTENT-002同包修复，不把简化迁移作为默认完成形态。要求同速下普通出牌/抽弃回手/生成消耗及片内必要攻击反馈类型、批次/并发/先后一致可辨认；Back保持直接节点恢复，不要求全特效倒放/像素逐帧一致，不扩全游戏动画平台。已落实可对比的观看期切片，不等待额外批准，也不让用户重复测006。

007删除RoutePresentation.Animate/Move/Positions及原有牌区迁移hook缓冲；保留其正确的中央/牌堆同步、浏览和无来源展示手牌选择。新NativeDisplayTrace只在观看前沿新引擎段或最终确认段采集，用户思考/历史浏览不采样；每30Hz保存战斗NCard实际global位置、角度、scale、颜色、Body变化、显示牌区，以及血条和三个堆的显示变化。使用同一时钟重现实际轨迹和并发，而非按卡循环固定动画。不是逐帧复制整个场景/默认录制开销，也不变更原片/Events/schema/checkpoint。每个已访问节点的入边保留该同会话展示片段。

已审阅并接入的原生纯显示入口：NCreature.SetAnimationTrigger只触发原animator；VfxCmd.PlayVfx实例化指定场景；NDamageNumVfx最低层位置/数字Create、NHitSparkVfx.Create；NCardTrailVfx Create/FadeOut；NCardExhaustVfx/QuickVfx Create/PlayAnimation；SfxCmd.Play/调试声音和ScreenShake。这些cue按首次实际调用的时间/顺序与卡牌轨迹共同播放。各次新建显示对象/显示任务，不保存或恢复原Task/TCS，也不复用业务委托/执行ActionQueue。新构造的cue只闭包保存必要数值/字符串/Creature锚及审查过的显示方法元数据。原CardPileCmd/伤害/抽牌/生成等业务不在历史区调用，也没有全局屏蔽通知。

历史前进先关闭只读展示等待，在旧逻辑节点上播放上述展示（输入/重叠导航锁住），比较前后ChoiceProjection/RNG保持；再用原DirectCheckpoint恢复目标真实数据、校验来源/候选/逻辑/RNG、重开新原生选择窗口。这样命中特效在目标死亡/消失数据应用前还能定位当前原对象；本次切片不据此宣称死亡视觉已支持。选择界面仍由fresh native UI打开，不把整个overlay场景录成视频。

Render模型为独立显示副本：MemberwiseClone后先清所有实例委托字段，再走原生DeepCloneFields/AfterCloned（先清可防深拷贝附魔setter误发复制的订阅）；不注册NetCombatCardDb，不执行卡牌/模型效果。每次在可用战斗上下文内复制前后比较逻辑/RNG。正式游戏/原生对象只由之后用户正常启动时构造，当前没有native/headless构造测试。

原生段结束捕获实际视觉尾：飞牌退尽、手牌接近原生layout目标并连续3帧安静，消耗留足其原生展示时间；最长3秒，未稳明确失败，不无条件认正常。等待前后校验逻辑/RNG不得变化。旧NCardFlyVfx在数据恢复前先取消其仍在运行的显示协程、等当前完成信号（最多3帧），再移除卡及场景，避免先TreeExited后取消时重复SetResult；这是结束当前显示工作，不是复活旧等待。审查过的原生场景/伤害数/火花/拖尾归属跟踪并在恢复前退役，防旧卡入堆完成通知改早节点计数。Stop无论显示退役是否异常都在finally取消尚挂起的语义gate，保留会话清理路径。

新日志native-display-captured含frame/card/health样本量、cue时间/类型、最大及累计采样耗时；native-display-replayed确认实际数据/RNG保持、无业务重派/旧任务恢复；native-display-retired记录旧飞牌清理。原route/牌堆/中央/来源断言继续存在。这些是007以后运行诊断，不回填006通过。缓存随DecisionRoute.Clear释放；尚无实际007采样开销数字或视觉一致性保证。

Release零警告/错误。生产PresentationTiming纯合同检查共用时钟、同时间样本/跨帧追赶、相邻样本插值边界以及拒绝无效/倒序时间；证据presentation-clock-contracts.json明确不是native渲染证明。原片生产ClipStore/实例结果/文件校验/当前viewer兼容通过，recording-files-build-007.json。build-007-validation.json明确原生展示未复测、D-110未验收。未把时钟合同和旧Back/逻辑数据通过当原生观感通过。

已装F8595D9C…3A77F，sealed/output/installed一致，收据build-007-6d0e7a54。原006/FB40 pair/settings/selector保留当前恢复点同preparation，007源码/合同也归档；原片byte SHA仍29B912CB…835F80。原活动账号匹配、同批用户已确认Cloud关闭、仅ReplayMod/modded1、game0，无整存档恢复/其他账号访问或自动启动/Computer Use。

下一人工只需最近片对照：按Step首次到探寻打击的选择窗口，观察出牌/攻击/命中；Back一次回操作1结果，再Step重开该窗口，比较这段效果。继续正常Step到佳酿，完成7张最终决定并观察原生弃/抽批次；Back一次回七张已选，再Step看同段历史展示。核对最终数量及分别打开抽/弃牌堆，Stop正常退出，停在宿主6，不继续EndTurn/击杀/完整胜利。本次不要求重录。普通秘密武器抽回手、早有准备多选中仍可在路线中自然经过，不补全旧矩阵。

明确未覆盖的视觉：杀死/死亡淡出、完整敌方回合、所有自定义VFX工厂/生成变化动画、药水液体shader覆盖等；NLiquidOverlayVfx会启动NCreatureVisuals自身延迟材质任务，当前未未经隔离接到历史区。也未重新实测连续选择、胜利导航、Discard浏览或原STATE矩阵。上述不是从MVP删需求；切片通过后依实际证据继续同包补齐并向PM报告，不能默认用户接受这些缺口。

## r7 / build-007实际逻辑通过、手牌衔接和药水表现失败；build-008修复候选

用户反馈整体逻辑不错，但手牌计数/显示像多次硬刷新并卡顿，怀疑动画未同步手牌逻辑；药水Back后Step无饮用动画。Runtime 541e5274b1c448f69851f594bcb4f16c，共1851事件、3次打开/3次关闭、129 route-arrived/82图恢复/40历史显示重播；30原生操作为15步真实胜利、10步、5步三个打开。40次显示前后逻辑/RNG和205节点/82恢复显示检查均无错误，退出均Error=null、原manager/比较Continue主progress保持。没有route-pile-browse事件，未补造抽/弃浏览通过。56采集、1799帧，采集总2124.6557ms、单次最大73.2144ms，不得以无error称画面/性能正常。原始1851事件逐SHA归档当前恢复点runtime/content-002-r7/session；安全日志runtime-r7.json、汇总runtime-verification-r7.json。54项saves内容和原片SHA29B912CB…835F80保持，game0，未恢复存档/读取其他账号。

本轮D-110仍失败。旧断言证明节点手牌逻辑/所覆盖UI正确，不能证明播放中的编号、显示层衔接和帧时间正确。源码确认007只隐藏NCard，留真实NHandCardHolder的编号层可见；展示副本没有这些编号。每条历史边重建全套NCard，结束销毁，再在无遮挡时Restore/Snap真实手牌；还对未变化的health labels每帧SetTextAutoSize。冻结模型每条边重复深拷贝，并对每张新显示卡分别做全状态hash校验，形成采集峰值。药水瓶/空槽和液体效果是上版明确未接的缺口；不能用这点把本轮用户需求排除。

008隐藏完整手牌holder、选中容器、PlayContainer和药水容器；采集原生手牌编号的文字/字体/位置/角度/颜色并与卡片一起显示，消耗时也清理对应编号。HandPresentation.Snap明确设置1…N编号，Matched及handoff均核对真实编号；交还真实UI前核对目标数据中的原手牌实例/顺序、真实holders和编号。两个显示层不再同时露出编号。

DisplayAssets/DisplayVariantCache按原实例+内容fingerprint缓存冻结模型，等名等状态的两张打击不共享身份；同实例回到旧内容变体复用其冻结资产，不改新旧模型。场景显示卡/编号/药瓶节点会话内池化，首次构造分帧；消耗原生效果销毁的显示卡按需补回，不复用已释放对象。只有新缓存变体才深拷贝，复制前先清委托；采样内所有新复制按一批比较逻辑/RNG，工厂外单次复制仍单次校验。不是每次Step重建整手。池随DecisionRoute.Clear释放，不改正常录制文件。

历史播放建立遮罩和首帧展示后，在同成员场景先恢复目标真实数据、准备真实holders，再沿原生显示时钟播放；数据/RNG比较基线在目标应用之后。成员变化为保留已有击杀逻辑路径仍先展示旧成员，末尾也在遮罩内恢复目标，再撤遮罩。两种都在renderer释放前完成数据/UI准备，避免结束时露出整套重建。没有在历史重新分派牌效/喝药/生成/伤害，也没有声称动画每一帧的业务图都重新执行；节点数据和动画内展示数值分别协调。目标数据应用与纯展示状态检查的阶段在新日志明确标出。

文字、health/堆计数、纹理、可见性和固定geometry只在值变时写，不再每帧给不变文字重做autosize。记录native-display-replayed中的PreparationMilliseconds、TextWrites/PropertyWrites、显示模型/视图创建/复用量、handoff手牌数/身份/编号和整容器遮罩标志；采集保留原max/total耗时。008尚无实际FPS/耗时改善数字，这些代码改动不能当性能通过。

药水新增NPotion原生瓶子缩放/位置/空槽渐入的观看期样本与池化显示；保留原生pivot并先缩放/旋转后设置global位置，避免缩瓶中心偏移。NSplashVfx原生工厂按实际时间重建，归属清理。液体覆盖读取原生shader tint/hue/influence样本，在历史用独立复制材质和帧更新显示，不调用TryApplyLiquidOverlay的旧异步协程。数据恢复前让正在运行的原生液体timer归零、最多4帧等其自己还原正常材质；不提前置空其引用，不取消Visuals全局CTS。历史材质自己持有原正常材质，结束仍有淡出时由会话tick继续，开始新业务/恢复/Stop时归还且停止自身lease，避免旧回调把新场景材质盖回去。无OnUseWrapper/UsePotion/输入语义交付，药水库存/RNG仍由真实快照恢复一次。

Release零警告/错误。presentation-contracts-build-008.json保留原显示时钟合同并新增身份变体缓存反例：两个Equals相同的卡实例不能alias、同实例内容变体分开、回到旧变体不再建副本、清理后不跨会话复用；不是原生构造/渲染证明。原片生产ClipStore读取、校验/候选结果、当前viewer兼容通过，recording-files-build-008.json。build-008-validation.json明确未做正式游戏/headless/原生UI构造，008原生画面/性能未复测，D-110未验收。

已装9FA167AE…2DAE003，sealed/output/installed一致，收据build-008-6693e749；007/F859 pair/settings/selector和008源码/合同保留同恢复点preparation。原片未改，game0，原活动账号匹配、仅ReplayMod/modded1、Cloud沿本批用户确认关闭，无存档恢复/其他账号访问/自动启动或Computer Use。

下一只请求最近片：早有准备+确认后Back→Step，检查编号/张数和动画交还是否跳刷；佳酿选择窗口刚打开时Back回用药前，再Step对比瓶子/泼洒/玩家液体效果；完成七张选择后Back一次再Step看抽弃批次、手牌数/编号与衔接，宿主6 Stop正常退出。饮用属于打开选择窗口之前的边，不要求在已喝药的选择前缀内重复消费/显示喝药。无需重录或再全场。液体覆盖/手牌修复只有后续实际证据可确认，其他自定义VFX、所有击杀/敌方回合/连续/胜利视觉仍不据此全覆盖。

## r8 / build-008共享原生对象池回归与恢复卡顿；build-009候选

用户报告原有显示/卡顿仍存在，探寻打击无法正常运行。Runtime a3230a5a524345f9a3aeba3792afba99，共104事件，原生仅操作1通过，4次实际数据恢复/到达、2次历史显示重播；到节点4的探寻候选窗口后Step失败，首因route-choice-grid-holder-not-located。退出期间又有original-operation-task-fault-or-cancel，旧Runtime.Guard覆盖了首因；009保留首故障并继续单独记录清理噪音。原manager/所比较Continue及主progress保持，当前game0；54项saves及原片SHA29B912CB…835F80保持。原104事件和最新Godot完整原log逐SHA归档当前恢复点runtime/content-002-r8（native log私有）；安全日志runtime-r8.json、runtime-verification-r8.json、native-pool-failure-r8.json。

原生log提供关键证据：ObjectDisposedException对象NCard，栈NCard.OnReturnedFromPool→NCard.Create→NCardGrid.InitGrid。不是候选卡身份/录像被改，也不是增加locator重试便能治本。008的展示卡通过NCard.Create借用了共享NodePool；NCardExhaustVfx._ExitTree会对其卡QueueFreeSafely（移出场景，再延迟NodePool.Free），而DisplayAssets.Return(consumed)随后直接QueueFree同卡。共享池于是可能在延迟归还时把已经销毁的卡放入free列表，下一次grid创建拿到坏对象，InitGrid任务异常而无holders。判定为本mod生命周期回归。

009显示卡改从原PackedScene直接创建私有NCard，不借原生pool。PrivateDisplayOwnership用弱引用身份表标记私有卡，不用卡ID/Equals混淆身份，也不会因缓存Clear就丢失仍在路上的延迟回调。只对被标记的私有NCard拦截NodePool.Free(IPoolable)，走真正销毁并保留原生池不变；普通原生卡的Free逻辑不变。缓存自己的销毁仅作用私有节点。RoutePresentation及旧飞牌清理中的原生NCard改用QueueFreeSafely；HandPresentation移除holder时也把已拆出的原生NCard归还，避免既漏回收又持有失效项。共享原生池不被扫描修补/重置；新进程从干净池开始，观看前检查free列表无disposed/private项，异常明确报告。

grid就绪改看实际cards/holders与原生初始化标志，不把固定400ms当证明。保持原候选集合/筛选/原实例滚动定位，不为失败跳过选择、换目标或强行重建控件。小选择遵守350ms原生防误点门槛，手牌背板保留200ms入场；加choice-grid-ready及失败尺寸/holder/初始化诊断。

卡顿并未因008通过：2次历史准备耗时129.68/303.15ms，采集最大33.7294ms。009去掉恢复时对隐藏展示cache和每张卡深子树的遍历；恢复路径仍按所需真实HUD端点更新，不遍历无关的显示树。来源/槽/flags/空槽显示均稳定一致时复用药水holders，否则仍退役以隔离旧callback。遗物counter/status/texture经NativeInventoryHolder统一刷新，不在route路径再次Reload全量NRelic。CreaturePresentation复用同Animator内已找到的稳定loop状态，敌方显示重绑后重新查找，不复用旧任务。完整状态比较中的每张牌CardDetails只计算一次再同时用于Key hash及State，保持原JSON内容/顺序，不改expected；实际恢复仍做完整投影/Owned来源/RNG比较。常规恢复日志改精简实例/牌区/能量/遗物/药水摘要，避免重算并重写整副卡牌细节，原生实际操作日志及旧失败仍保留。显示帧缓存health控制绑定和反射元数据，减少循环查树。

新增checkpoint-restore-timing分退役/Data/Creature与content/UI/校验日志阶段，native-display-replayed再分AssetWarm/Renderer/DataRestore/FrameSetup，不以总体一个时间掩盖耗时来源。此轮尚无009真实帧时间或改善结果，不能宣称流畅度已解决。

Release零警告/错误。显示时钟/内容变体合同保留，新增延迟生命周期反例：清掉显示缓存后迟到的私有释放仍不能进入native pool，Equals相同的真正原生对象仍走原生归还。production PrivateDisplayOwnership参与该纯合同；未实例化正式游戏或原生UI，也不能据此称池完整性已实测。presentation-contracts-build-009.json及build-009-validation.json明确009尚未原生复测，D-110未验收；原片生产ClipStore校验/兼容当前viewer通过，recording-files-build-009.json。

已装6C68780A…C4B7792，sealed/output/installed一致，收据build-009-a5b42356；008/9FA1 pair/settings/selector和009源码合同保留同恢复点该preparation。Cloud沿同批用户确认off、原活动账号匹配、仅ReplayMod/modded1、game0，无整存档恢复/其他账号访问/自动启动或Computer Use。

下一仅原失败路径：最近录像工具箱完成后，秘密武器选牌结果Back→Step做两次，再推进探寻打击窗口并Step完成原选择。若健康，可在探寻完成后Back/Step一次看恢复衔接，随后宿主2 Stop正常退出。不要先重录/完整胜利或再补后段矩阵；先证实私有消耗释放不污染原生pool、探寻可完成，并用新阶段时间判断卡顿。未到的药水/多选/全视觉项仍待后续，不追认008或009通过。

## r9 / 009探寻修复通过，历史视觉与响应性仍失败；010候选

用户确认探寻可完成；首次正常，Back后的历史前进仍有卡顿、缺手牌动态高亮，所有动作等整段动画，佳酿缺瓶身旋转且药水栏消失。r9 runtime 0ab4dce1f40a4c55ad5e0547cf86361a，1743事件，2次打开/关闭均Error=null，分别到宿主11和15胜利，26原生操作、106历史到达、74恢复、37展示重播、52共享卡池检查无disposed/private；Godot不再有disposed NCard。不能把逻辑无异常当画面通过。原始1743事件和完整Godot逐SHA归档当前恢复点runtime/content-002-r9，安全runtime-r9.json/runtime-verification-r9.json。54项saves及原片保持，现场game0，无存档恢复/其他账号访问。

真实历史准备中位62.9053/max329.9541ms，UI阶段中位44.0886/max345.7579ms；数据阶段中位1.7768/max41.3647ms。用户提出缺动态高亮可造成“卡住”观感，是有用解释，不能否定真实帧开销。代码确认原生NHandCardHolder.UpdateCard通过NCardHighlight的独立shader width/颜色显示可打出光边，008/009替代卡只更新NCard本体，遗漏该效果；全部holder遮罩让遗漏持续至整体交还。药水整container遮罩且替代瓶在combat canvas，缺槽背景/原topbar层；实际旋转瓶身属于NItemThrowVfx，旧捕捉只有NPotion缩放、Splash/人体shader。以上为已确认遗漏；“所有一卡一卡均由高亮缺失导致”仍不能作实证结论。

010加入实际highlight width/色/visible采样，在展示私有材质上按相邻样本插值，shader本身的动态保留；不依赖冻结模型在目标逻辑上重新判断CanPlay。槽背景保持原生可见，替代NPotion/空图标放同TopBar canvas，数据恢复后只遮原生内容，避免重建的新瓶同时露出；其余瓶按原样本持续显示。NItemThrowVfx只在真实前沿记录已生成Sprite的纹理、global位置/缩放/旋转与颜色，历史用私有Sprite插值，既保留实际投掷旋转又不重跑其Rng.Chaotic/异步ThrowItem。原生cue/液体继续保留，源特效生命周期归档/清理方式不变，009私有卡归属修复保留。采样上限放宽至process帧（不超过120Hz），增实际最大采样间隔、highlight/thrown-item样本诊断；未以此声称实测流畅。

按PM D-111同包处理输入：历史展示/准备中，Step/Back/Pause可接收一次请求，互斥完成目标数据/UI交还，跳过剩余专有显示cue，再从目标执行请求。Back返回该边源节点；Step请求下一节点；Pause立即结束纯显示后停当前目标；Stop清意图并有序退役。不是启动并发导航，也不重派业务/复活旧Task。实际原生AwaitingEngine或尾采集阶段也接收意图，在新选择或稳定边界排入；状态明确等待结算，不强行取消半态原生业务。pending时阻止重复排入，正常控制的原有互斥分支保留，避免禁用/启用按钮丢release的旧bug。日志route-control.QueuedDuringPresentation、route-presentation-navigation以及PresentationInterrupted/RemainingCuesSkipped用来核对实际边数，不用合同代替原生按钮验证。

恢复UI进一步直接绑定TopBar HP/Gold、combat Energy/Star、当前原始creature的health/powers/intent，而非遍历NRun；ContentPresentation能量校验直接取原能量端点，去掉此前遗漏的整场景（包含大量私有缓存卡子节点）扫描。能力容器按原模型身份保留/增删节点、刷新内容并恢复顺序，避免每次全部销毁重建。完整数据/Owned/RNG和手牌/牌堆检查保留，不硬改expected。该轮没有010原生耗时结果。

Release零警告/错误；原片生产读取/兼容通过；5项最终路线合同、时钟/变体/延迟归属及新增导航合同通过。新增模拟反例覆盖历史动画中Back到源节点、原生业务忙时只排意图、不重复Take、Pause/Stop清意图，使用production PresentationNavigation/DecisionCursor。没有构造游戏/原生UI，不能证明视觉、按钮实际时序或性能通过。证据presentation-contracts-build-010.json、decision-route-contracts-build-010.json、recording-files-build-010.json、build-010-validation.json。

已安装01955728…6E840C9C，sealed/output/installed一致，收据build-010-f4cbcfc1；前009 pair/settings/selector及010源码/合同保留当前恢复点该preparation。Cloud沿本批用户确认off、原活动账号匹配、仅ReplayMod/modded1、game0，无自动启动/Computer Use/其他账号/整存档恢复。D-110/D-111尚未验收。

下一最短原片对照：秘密武器完成后Back/Step看高亮与入手移动，在历史移动中Back一次应及时回选择窗口，再Step；可在历史移动中试暂停一次看是否停目标。继续到佳酿窗口，Back到饮用前再Step，对比旋转瓶/人体效果/无关药水槽，保持窗口Stop正常退出即可，不需重录/七张选择/全场/击杀矩阵。

## r10 / 010局部改善与剩余HUD/编号/费用/入场缺陷；011候选

用户称上一组bug看起来都已修好，保留该反馈；新的明确缺陷为重锤斩杀时血条/意图迟至首敌尸体完全结束才更新；三种药水历史显示中编号像右侧有不存在第8牌挤位，结束复位；秘密武器入场瞬移、窗口过快，确认后打击只有左右端少数帧，编号与两张免费牌绿色0同时显示不一致。用户补充只确认Back后，不记得首次，未要求他在旧版补测。‘第8张挤位’只作为显示观感，没有真实多牌证据。

r10 runtime 7a96a8d5c8bc445495ccb3475f9ac7b3，共3227事件、10原生操作到重锤斩杀、233历史到达、189恢复、90显示重播、15已排意图处理，85池检查未见坏对象。一次关闭宿主10，Error=null，所比较原manager/Continue/主progress保持，非胜利退出。原3227文件与完整Godot逐SHA归档当前恢复点runtime/content-002-r10；安全runtime-r10.json/runtime-verification-r10.json。54项saves及原片保持，game0。历史准备中位42.5136/max259.0961ms，UI中位25.4427/max344.9139ms，数据中位2.0801/max39.9862ms；不同测试路径不作严格性能前后实验，也不把‘数字卡’直接写成全部掉帧根因。

确认的源码遗漏：替代编号Label没复制原生水平/垂直对齐及轴心，且其位置/缩放/旋转没有跟NCard共用插值，交还正常UI时样式/位置会切换。费用则由冻结副本在当下战斗上下文初始化，没有记录真实文本/颜色/图标，因此‘费用0应绿’可能显示成副本计算结果。011采实际MegaLabel能源/星费文本、字体/描边色、调制色和四种费用图标Visible，私有卡初始化或变牌区后明确覆盖，观众可见输出不由末态副本决定。编号复制完整对齐/尺寸/轴心，位置/角度/缩放/颜色按同时间轴插值。实际画面效果仍待011确认，不能仅源码认定全部偏移或卡顿已解。

秘密武器choice-open来源Trace仅0.0636861秒/3帧，足以确认旧路线在PlayPileTween尚运行时结束了入场边；历史目标恢复又把卡放到中间，导致入场截断。011只等待ActionMonitor当前来源卡自身仍运行的PlayPileTween，保留其原速/真实采样，不统一等手牌/全场特效、不加固定延迟，也不改业务提交。等待前后完整投影/RNG比较，结束后若Stop已请求先取消当前语义等待，不append停止后的节点。原生AwaitingEngine期间的导航意图继续接收，在安全选择/稳定边界处理。

秘密武器确认后Trace虽57帧/1.00894秒，但最大采样间隔156.5669ms，最大一次采样43.4423ms；不能据总帧数宣称入手细节完整。011在真实NPlayerHand.Add的前/后额外采样，保留挂入手牌的起始位置及已有卡重排边界，仍使用同一原生采样时钟插值，不用自定线性飞牌或重复CardPileCmd。来源卡牌cache树也直接剪枝。新增每个轨迹的实际样本数、移动间隔、最大间隔/位移、首末位置、编号对齐和费用显示色，能区分轨迹缺起点与单纯字体位置切换；不输出整个场景/所有帧。该举措旨在补缺失起点，尚无新原生轨迹证明。

死亡旧显示只采NHealthBar子控件，没采其NCreatureStateDisplay父级淡出/位移/可见性、成员退场和意图，且变成员路径延后真实数据恢复，HUD于是停留旧显示。011新增小范围CreatureHudTrace：采已遇到原Creature身份的state display父属性；原生intent的实际Sprite帧纹理/全局变换/颜色和RichText数值/字体等。历史按原时刻更新state父级，原节点不再存在则隐藏HUD；意图使用私有只读Sprite/RichText，隐藏原生intent容器，避免NIntent._Process覆盖历史帧。健康条仅退役已审查的_blockTween/_hpLabelFadeTween/_middlegroundTween，防其process后写回覆盖采样；不改HP/NextMove、不复活死亡任务/提前派业务。死亡/幸存者意图的像素显示不用等整段尸体完成或最终成员快照。该实现仅针对已测HUD路径，不扩为全怪物视觉平台；原生实际时间仍为唯一对照。

新native-display-captured日志加Cost/Intent/HUD隐藏/HandBoundary样本及逐轨迹摘要；native-display-replayed加实际更新帧数、最大更新间隔、跳过来源样本数和Apply总/最大耗时，以分别看状态不一致和真实循环开销（不是GPU FPS测量）。010的私有卡归属、药水栏原canvas、瓶身/液体、高亮及D-111意图互斥机制保留。

Release零警告/错误；显示时钟/身份变体/延迟归属/导航合同、5项最终路线合同与真实原片生产读取/兼容通过。没有新原生构造、自动启动或图形测试；合同不能证明字号/对齐/绿色0、入手连续性或死亡HUD通过。证据presentation-contracts-build-011.json、decision-route-contracts-build-011.json、recording-files-build-011.json、build-011-validation.json。已装D7621D3F…4D43144，sealed/output/installed一致，收据build-011-806220f2；前010 pair/settings/selector及011源码/合同保留同恢复点该preparation。Cloud沿同批用户确认off、原账号/ReplayMod/modded1、game0，无存档恢复/其他账号/Computer Use。D-110/D-111未整体验收。

下一仅原片：秘密武器从操作前Step到窗口，完成选择后Back/Step，看完整入场/打击入手、编号和绿色0；推进能量药水后Back/Step一次，看编号不假性挤位、费用色不中断；重锤斩杀后Back/Step一次，看首敌血条淡出及幸存者意图与首次同刻更新，再Stop正常退出，不需重录/胜利/反复补旧版。

## r11 / 011局部通过，D-112统一时钟、边界及原生HUD事件；012候选

用户确认刚刚的编号/费用/秘密武器/死亡HUD问题修复，首次循序基本正常；新反馈明确限历史：攻击药饮用到开窗的间隔不对，能量图标动画缺失，EndTurn中按Step导致回合显示不对，希望历史与首次同样接收请求后等待结算边界，并质疑逐药人工模拟。PM D-112明确纠正此前把接收Step扩成截尾的解释；保留Back/Pause/Stop既有安全规则，历史Step不得跳尾或多走节点。用户已确认正常退出，无需重测旧版。

最终r11 runtime 0a9e2da1cb194fc388c1769f9ddbaf35，4035事件，四次关闭宿主12/8/12/12，Error均null、所比较manager/Continue/主progress保持，无胜利记录。44原生操作、250历史到达、199恢复、83重播，101共享池检查无disposed；29次展示/业务中排Step、19次历史展示被截断（包含各类导航，不能把19次都归Step），旧tape没有energy-increase显示cue。源逻辑断言未报错，不能由此否认回合显示错误或认定是逻辑多结算。原4035文件及完整Godot逐SHA归档当前恢复点runtime/content-002-r11，安全runtime-r11.json/runtime-verification-r11.json，54saves/原片保持，game0。旧准备中位40.0731/max260.4981ms、UI中位26.4485/max272.1357ms、数据中位2.1925/max33.4786ms，不据不同路径作严格性能比较。

根因不只缺一个药水特效。旧tape把Show及其WaitReady记录在整段时间里，但历史在整段结束后才另开同一窗口、再等原生准备，延长了饮用至开窗；历史Step的pending直接触发FinishPresentation，跳掉后续cue；同成员路径提前恢复目标业务数据，而能量/EndTurn HUD没有自己的源时钟，原生按钮回合标签又由TurnStarted等事件刷新，不由恢复字段自动刷新。视觉可提前露末态、缺原生显示事件、被Step截断，是互相关联的时序问题。

012改变共同机制：PresentationNavigation区分Pending与InterruptsDisplay，Step仅Pending，不进入截尾路径；首次/历史/界面初始化期间均可排一次意图，到同等可用节点再交付。初始无历史不允许虚构Back，终点无下一节点不提前接收Step；排队状态明确写等待当前结算边界。Back/Pause/Stop依旧互斥、有序退役，不强行取消原生业务，不复活旧Task。

choice-open成为按真实首次调用时刻记录的显示事件，另记录choice-ready时间。历史在同一cue队列到达该时间时启动fresh只读NativeChoice.Show，必要初始化/防误点等待与剩余展示时间并行，末尾只await已经进行中的准备，不第二次Show/累加同一WaitReady。保留真实grid-ready检查和350ms防误点。日志同时记源时刻、时间轴请求时刻、实际wall请求时刻及边界恢复开销，不把恢复暂停从wall延迟中隐去。窗口事件只交付一次，Stop不为落点额外新开窗口。

历史目标数据统一延后至窗口或稳定显示边界，仅应用一次；展示期间保持源数据，纯视觉前后完整投影/RNG检查保留，边界恢复仍做既有Owned/实例/牌区/手牌/数据断言。窗口需要目标候选时，在该marker下有序恢复再建新UI；稳定结果在相同观察区间结束时恢复。没有通过隐藏从头重演或再次调用Card/Potion/EndTurn业务实现相似效果。此前同成员预应用为减少末尾恢复所作优化不再保留；当前优先一致的提交时刻，012边界耗时/画面仍待实看。

CombatHudTrace只绑定已知能量和EndTurn端点，采其显示文本/字色、材质、位置/可见性和旋转等，按源时钟显示，防末态模型或旧控件缓存先露下一回合/能量。只暂停能量控件自身的旋转_Process并退役其位置/淡出及按钮位置/hover纯视觉Tween，粒子子节点继续运行；不是冻结场景。恢复稳定节点补纯显示回合标签，不调用OnTurnStarted业务。

能量增加按通用NEnergyCounter.OnEnergyChanged(old,new)显示事件采集，历史调用该已审查原生函数（实际只Restart前后粒子），覆盖药水、牌、遗物、回合补能等来源，不按Potion种类写分支，也不重调OnUse/再加能量。回合横幅用原生NPlayerTurnBanner/NEnemyTurnBanner创建及源round参数，保留本来动画和声音；不再单独复制它们已自带的debug音效。专有横幅可在稳定节点后自然结束，能量粒子同样留原生尾，不要求所有装饰FX归零才继续。Back/Stop才清这些专有尾及能量残留；首次原生横幅依旧原逻辑。普通回合样本待验证，不由此外推额外回合、多人和全部HUD。

Release零错误/警告；既有显示/变体/归属/导航合同、5路线合同及原片生产读取兼容通过。新增反例用production Navigation/Timing验证Step排队不截尾、不重复意图，开窗在源marker而非尾后只触发一次，ready/entry非法顺序及范围拒绝；这些没有构造原生UI/游戏，不能代替实际窗口/能量/回合动画或帧耗时验收。证据presentation-contracts-build-012.json、decision-route-contracts-build-012.json、recording-files-build-012.json、build-012-validation.json。

已安装813EFBF0…527438BF，收据build-012-860df0e5，sealed/output/installed一致；前011pair/settings/selector与012源码/合同保留同恢复点preparation。原活动账号/ReplayMod/modded1、Cloud沿本批确认off、game0，无恢复存档/其他账号/自动启动或Computer Use。D-110/D-111/D-112未整体验收，r11用户局部通过单独保留。

下一最短原片：攻击药窗口Back到饮用前再Step比间隔；推进能量药水Back/Step比图标动画；再Back到首个EndTurn前，重播中提前按一次Step，应显示等待边界、当前段正常播放，下一回合/后续窗口只到达一次且回合号正确；然后Stop正常退出，不重录/胜利/旧版矩阵。

## r12 / 012时钟问题局部通过，013修复显示交还与原生窗口层级

用户确认上一组已修好，继续报告历史段末端手牌高亮灭后再起、敌人/意图回默认再播，选择界面一瞬间被手牌/意图盖住且背景不暗，秘密武器打击入手仍有问题，整体像强制播两遍。r12 runtime498dcf60540e49849feed018a4c5ff1d，3009事件，15原生操作完整胜利，216历史到达、162恢复、81重播，84共享池检查无disposed；一次胜利Stop Error=null、所比较原manager/Continue/主progress保持。原3009事件和完整Godot逐SHA归档runtime/content-002-r12，安全runtime-r12.json/runtime-verification-r12.json，54saves/原片保持。用户确认退出且现场game0。旧准备中位6.2968/max161.844ms、UI中位21.2433/max343.9427ms、Data中位2.0212/max31.8371ms；不是013结果或GPU帧率。

静态根因明确：业务数据提交后的CreaturePresentation每次SetNextState会重新SetAnimation并重设循环偏移；Hand.UpdateCard会对新原生NCard从0再次AnimShow高亮；UpdateIntent会在名称变化时重置帧时钟，且旧私有意图在销毁后直接露默认相位。Renderer.Root/PotionRoot Z1000也会跨过原生overlay和backstop，导致面板已开而旧手牌/意图仍画在上层。012数据边界修正保留，此轮在显示交还层修，不再调开窗延迟或重派业务。

013原生交还前把真实手牌/Play卡的高亮宽度/色/可见性和费用样式接到末帧，kill仅该高亮渐入Tween；不再从0闪回。CreaturePresentation在历史提交中仅当当前动画可自然到达相同目标循环时保留当前播放，不重置轨道；不兼容目标或直接Back仍恢复正确稳定循环，匹配检查保留目标循环目的地及尺度/身份要求。前进跨击杀只退役目标以外敌人、仅创建确实缺失节点，保留幸存者原节点/动画，并按实际是否重建重新核对能力订阅；直接Back的原重建路径保持。意图按原Creature/槽/动画名传递实际动画累加时间、当前帧及bob相位，新的原生intent正确接续，不复活旧Task或派TurnStarted。末帧交还日志native-display-handoff/native-intent-handoff可查；实际是否无闪烁尚待013。

两展示Root去掉强制Z1000，使用各自原生父级下的正常层级，overlay/backstop及原生淡入覆盖/变暗自然生效，不在全屏绘制一层假的暗色。D-112 choice-open源时钟、原生准备约束、Step等待边界、Back/Pause/Stop互斥和业务仅一次保留。

秘密武器结果来源63帧/1.0047067秒，新增打击62样本、23移动区间，但最大单步1498.5681px、附近最大gap143.5559ms；native AnimPosition以delta*7插值，该幅度不能当未被观察器扰动的正常基准。复制/投影校验或首次资源准备阻塞是待核实解释，不能据采自原生就判正常。013把可预备的全战斗卡冻结模型放稳定点，把选择候选放已就绪窗口阶段预备，批次前后投影/RNG比较，不创建全部NCard UI，原始记录不改；运动中新变体仍有原保护。Prime日志记新模型数/耗时，逐轨迹源样本间隔/位移和显示更新开销继续保留。旨在降低首次新卡复制落在运动内的概率，尚无013原生数据，可能只是移动停顿；没有人为慢飞/插造固定曲线来掩盖单跳，也不建通用性能平台。

Release零错误/警告，既有显示时钟/归属/导航及5路线合同、真实原片生产读取/兼容通过；没有构造游戏/原生UI，不能用这些验证新动画相位、层级、连续性或性能。013 SHA C35FFFF6…0294E16D，收据build-013-87f9b516，sealed/output/installed一致。前012pair/settings/selector及013源码/合同保留同恢复点preparation，原账号/ReplayMod/modded1、Cloud沿本批确认off、game0，无存档恢复/其他账号/自动游戏/Computer Use。D-110/111/112未整体验收，012用户局部通过单列。

下一最短仅原片：秘密武器入口和确认后Back-Step查入手/高亮交还；探寻命中及开窗Back-Step查敌人/意图是否二次起播、窗口和暗背景是否及时覆盖；推进发现三选一检查另一类窗口覆盖，到宿主4 Stop正常退出，不重录/胜利或补旧版。

## r13 / 013局部改善，014同一原生意图控件和原生入手运动候选

用户确认013修了大部分问题，手牌/敌人模型似已正常；仍拒绝意图刷新（含二倍时间表述重新显示）、发现/早有准备窗口初现时意图浮上层，以及探寻打击。补充明确探寻是选择窗口或打击入手动画卡顿，并非操作结果失败，不要求用户补测旧版。

r13 runtime 6950fed9a41146649e843f963d726f0d：2431事件，14原生操作、176路线到达、118恢复、49历史显示；一次宿主14 Stop、Error=null，未到15步胜利。原manager/所比较Continue和progress保持，53共享池检查未见disposedNCard。49次旧意图handoff均实际传了1或2个clock，因此不是简单漏调用，复制图元交还仍有可见切换。原2431文件及Godot逐SHA归档，同恢复点runtime/content-002-r13，公开runtime-r13.json/runtime-verification-r13.json；54saves/原片保持、game0。

013 Prime未解决来源轨迹：秘密武器结果62帧，最大gap157.1686ms、采样27.2131ms，入手打击61样本/20移动区间、最大单跳1363.7819px；探寻结果95帧、最大gap42.2806ms/采样17.9438ms，入手94样本/45移动区间、最大单跳508.08575px。Prime新增25模型批次只1.8293ms，不能继续把卡顿直接归于模型复制，更不能把受观察器扰动可能的来源路径当干净原生基准。具体阻塞来源仍未严格证明。

014意图不再建私有Sprite/RichText影子，不隐藏原生IntentContainer。以原Creature和原生槽标识接管同一个NIntent：暂停其自身process，暂时拦截UpdateVisuals显示刷新，但UpdateIntent仍更新模型上下文；采样驱动其实际子控件和父容器/holder的显示与bob相位。边界交还同步协调一次原生显示模型，再恢复采样末帧时钟/纹理/phase，过程无帧让出；Dispose撤销托管并还原原process开关。原控件始终在原生层级中，overlay/backstop负责前台与变暗，不复制顶层图元。新增SameNativeControls/ShadowIntentViews/Mismatches诊断。不能静态保证新槽创建、所有怪物或原生叠层画面均通过。

014通用入手适配：在真正NPlayerHand.Add前记录入手卡的起始全局位姿，返回后记录NHandCardHolder真实目标位置/角度/尺度，同源cue时钟启动。历史提前创建私有只读展示卡对应的新NHandCardHolder，仅挂展示Root，不调用真实hand.Add/AddCardHolder、不订阅真实手牌按下处理；用原生SetTargetPosition/SetTargetAngle/SetTargetScale及其原插值规则完成运动，后续采样目标变化仍交同一函数处理。不是重新派发CardPileCmd或喝药/抽牌业务，没有逐卡/逐药固定曲线，也不复活旧异步任务。原有卡仍按采样显示，入手卡不再被异常的大位移样本逐帧强写；编号使用holder本身标签，费用/高亮保留记录外观。

私有holder原生Ready可能启动高亮Tween，构造后立即取消该显示Tween；整个构造前后业务/RNG投影必须相等。退役先StopAnimations，显式解绑模型事件和NCard.ModelChanged，再分离卡并归还私有显示缓存，避免原生OnChildExitingTree清空CardNode后遗漏退订。Back/Pause/Stop仍按既有边界互斥清理，不改变D-112。此处使用新的原生显示任务，需人工确认其生命周期/索引/层级；纯合同未覆盖实际Godot对象构造。

Release零错误/警告；presentation-contracts-build-014.json、decision-route-contracts-build-014.json、recording-files-build-014.json通过，原41事件/15操作/8选择未改。build-014-validation.json明确未原生测试/未量化性能，不以静态通过替代画面验收。014 SHA163A39B5…CFE497BF，收据build-014-0b36f769，sealed/output/installed一致；前013 pair/settings/selector及014源码/合同保存同恢复点preparation。原账号/ReplayMod/modded1、Cloud沿本批确认off、game0，无存档恢复/其他账号/游戏自动启动或Computer Use。D-110/111/112不标整体验收。

下一仅原片：探寻打击至窗口、完成选择后的入手，分别Back-Step看窗口/入手连续性；早有准备和发现各检查一次Back-Step，观察意图不中途二次起播、选择页面出现即位于前台并正确变暗。宿主4 Stop正常退出，无需重录/完整胜利。

## r14 / 用户纠正卡名，意图局部通过；015整手运动、可见起点及在途交还候选

用户纠正此前“探寻打击没好”为秘密武器。历史r8探寻共享池故障和009修复证据保持；不回改原日志模型名或原片，不继续把014探寻功能/卡顿作为当前失败项。用户确认014意图显示正确；选择界面现在意图层级正确，但手牌及打出的牌没有即时切层。新细节是早有准备历史抽牌未完就定终态，秘密武器正常时打击更像在手牌对应位置小幅出现，历史其余九牌让第十牌位置时向左闪现。014意图原位控件处理保留。

r14 runtime 0906bd99bfbc4becb8ff588407cfd9d8：5384事件，29原生操作、300到达、271恢复、121历史显示，7次Stop宿主12/5/5/1/2/2/2，Error均null，原manager/所比较Continue和progress保持，未胜利。164共享池检查无disposedNCard。原5384文件及完整Godot逐SHA归档同恢复点runtime/content-002-r14，公开runtime-r14.json/runtime-verification-r14.json；54saves/原片SHA保持、game0，无恢复存档。

首次秘密武器结果60帧/0.9981144秒，打击源第一点55/1025至1570/1118、最大单跳1350.1614px/gap136.36ms；对应历史例子最大帧gap147.3699/141.4583ms而Apply最大仅0.5868/0.818ms，不能将所有停顿归于Apply或模型复制，也不能判原生基准已干净。早有准备入口trace约0.415363秒，在0.2078908秒到选择边界；原生选择/可恢复业务边界不等于手牌视觉全部结束。

源码差异：014仅新增入手卡拥有原生holder移动，其余九牌仍逐帧按来源采样位置强写；NPlayerHand.Add内部RefreshLayout会同步推进实际位姿，取Add调用前的牌堆位置会把不可见的完整大距离也重播出来，不能简单把所有来源统一成从左飞入。目标Restore的HandPresentation.Snap在遮罩内正确重建逻辑/稳定布局，但014交还没有续接仍运动的展示holder，导致选择边界强制露出终态。Root虽去Z1000仍TopLevel=true，且全牌共用combat层，无法保留手牌选择backstop/原生hand前移的细分排序；这是层级待实测的代码依据，非静态画面通过。

015新增HandPose，分开记录holder实际位姿/目标及卡本地位姿。入手cue从Add返回后的实际可见位姿开始，pre-Add未被渲染的牌堆pose不提前显示；不是逐秘密武器或按牌名猜动画。所有已出现手牌都用新的私有NHandCardHolder通过原生SetTargetPosition/Angle/Scale推进，后续目标变动也走原生机制，原有九牌不再被异常位置采样强写。保留卡本地缩放/旋转、费用、高亮；编号用原生标签且仅编号变化时写。新生只读holder不加入真实手牌或业务数据库，构造/重入都有业务/RNG投影保护，清理显式退订和取消原生显示任务。

手牌/出牌显示根分别锚在原生CardHolderContainer/PlayContainer的相邻同层位置，取消显示根TopLevel，沿原生父级接受hand前移、backstop和overlay排序；全局位置按各自父变换转换，颜色扣除已继承父色避免重复调制。并非延时隐藏手牌或增加假遮罩。边界的业务数据、成员/实例/编号/稳定恢复检查仍保持，随后把私有holder当前显示位姿交给真实holder，以真实目标和同一原生函数继续运动，不因交还定到终态，不等待全场动画再开窗口。直接Back仍可定位目标稳定点。D-112源时钟、Step排队/语义边界不改，不重新执行抽牌/药水等业务，不复活旧Task。

日志新增HandMotionTracks、HandEntries（Before/VisibleStart/Target/RemainingPixels）、交还HandMotionsContinued/UnsettledHands/MaximumRemainingHandPixels、TopLevelCardCanvas=false，并在结果摘要列出耗时至少8ms的显示cue，辅助区分位置强写和真实停顿，不建设全场性能平台。现阶段未量化015性能，未构造原生UI或自动启动游戏；新的锚点排序、手牌共享运动及原生续接生命周期均待人工画面/日志确认。

Release零错误/警告；presentation-contracts-build-015.json、decision-route-contracts-build-015.json、recording-files-build-015.json通过，原片41事件/15操作/8选择及SHA保持。build-015-validation.json列明限制，既有纯合同未覆盖新Godot层级和holder集成。015 SHA2951B8C5…D1DFB01，收据build-015-440f9608，sealed/output/installed一致；前014 pair/settings/selector及015源码/合同保留同恢复点preparation。原账号/ReplayMod/modded1、Cloud沿同批确认off、game0，无其他账号读取/存档恢复/Computer Use。014意图用户局部确认与D-110/111/112整体未验收分开记录。

下一最短仅原片：秘密武器入口开窗Back-Step查手牌/打出牌即时层级，确认后Back-Step查打击在对应位置的入场和其余九牌连续让位；早有准备开窗Back-Step查抽牌尾自然接续和手牌选择层级，确认后Stop正常退出。无需重录/胜利/补旧版/专门重测已通过意图。

## r15 / 015上一组用户通过；016共用出牌、消耗尾及牌堆反馈候选

PM转达用户直接确认“现在这些问题都解决了”，对应015的可见入手起点、整手让位、早有准备抽牌尾交接和层级，014意图亦保持此前确认。新问题限定历史：早有准备、发现、探寻、秘密武器等出牌动画缺失；打出的牌消耗动画提前截断，右侧消耗牌堆计数变化反馈错误。按共用Hand→Play→效果→离开生命周期修，不逐卡名特判、不要求补旧版或重新录制。

r15身份核对为015 SHA2951B8C5…D1DFB01，runtime7fae13a4bb3142c2be793d09dce475ce：3272事件、33原生操作、171到达、137恢复、64历史显示，五次Stop宿主11/6/2/10/4，Error均null及原manager/所比较Continue/progress保持，无胜利。99池检查无disposedNCard。原3272文件/Godot逐SHA归档同恢复点runtime/content-002-r15，公开runtime-r15.json/runtime-verification-r15.json；54saves/原片SHA保持，game0。旧日志可证明状态/退出和构建身份，不能据此替代用户的出牌/尾/计数画面失败。

静态缺口明确：历史出牌仍只依赖有限位置样本，015原生holder覆盖的是手牌移动，尚未记录共用的原生Play Tween入口；Card.DisplayingPile是样式区，不一定等于卡的实际牌区，不能独自决定出牌/离开的展示生命周期。消耗fx已用原生Create/PlayAnimation，但Renderer.Dispose在每条展示边结束时直接销毁效果和已消耗私有卡，原生还应保留的粒子尾/延迟清理被截；就算未按Step跳cue也会发生。牌堆采样仅数字，未调用原生AddCard纯UI反馈；RoutePresentation.Synchronize每个发布还kill其_positionTween/_bumpTween并把Scale归一，覆盖原生计数反馈。各缺口贡献仍待新画面对照，不预认唯一根因。

016在实际CardPileCmd.AppendPileLerpTween目标为Play和NCard.AnimCardToPlayPile入口采集显示参数/cue，历史释放私有手牌holder后，在正确Play显示锚调用审查过的原生纯Tween配置或原生AnimCardToPlayPile；不调用外层CardPileCmd.Add/Play、queue逻辑或其业务回调。源实际Pile与DisplayingPile分开记录，用于区分原生手牌运动、出牌及弃/消耗接管，原生出牌Tween运行期间不被位置样本覆盖。015手牌可见起点/整手让位、卡本地姿态、高亮/费用/层级和014同一意图控件保持。

正常前进到语义节点后，普通/quick消耗仍有效的纯显示效果交给原生父层级及原生DelayedFree，不随Renderer归还销毁；quick先挂卡内再移出/释放卡的原生生命周期保留。专属尾登记在OwnedTails并从下一段TableCards/遮罩/源退役扫描排除；Back/Stop按既有规则退役，正常Step仍到语义边界，不等所有装饰尾。持续消耗所占私有卡立即标Retiring，DisplayAssets.Valid拒绝再借这张仍活着但待清理的卡；原私有池归属身份持续至原生延迟Free，避免下一段复借后旧回调误销毁或污染共享池。源前沿实际产生的两种消耗fx也登记SourceVisuals，历史恢复退役原来源显示，避免旧前沿残影混入。

牌堆共用NCombatCardPile.AddCard显示事件按源时刻记录，历史调用同一原生UI处理（含NExhaustPileButton第一次显现/AnimIn和Icon/Label的原生bump）。该处理只读真实pile并写显示计数和Tween：因真实data仍可处源checkpoint，限定事件上下文在基类postfix将显示数字协调到记录的原生事件结果；不改真实牌堆、不派CardAddFinished或其他业务通知、不重复计数。反馈后立即重施只读浏览权限。前进/选择发布及历史目标restore保留牌堆已运行位置/bump Tween，只协调数字；直接Back仍kill并恢复稳定端点，严格实际牌堆/显示最终数检查保持。

新增NativePlayMoves/NativePileFeedback cue统计和native-exhaust-tails-continued日志；既有HandEntries、handoff和SlowVisualCues继续保留。Release零警告/错误，presentation-contracts-build-016.json、decision-route-contracts-build-016.json、recording-files-build-016.json通过，原片41事件/15操作/8选择/SHA不变。没有游戏/原生UI构造、GUI或headless测试，这些合同不覆盖新Tween、尾生命周期和实际计数反馈；build-016-validation.json明确新原生待测、性能未测。016 SHA60650879…73F1F04，收据build-016-2b86479a，sealed/output/installed一致；前015可用pair/settings/selector及016源码/合同保留同恢复点preparation。原账号/ReplayMod/modded1、Cloud沿本批确认off、game0，无整账号备份重做/存档恢复/其他账号读取。D-110/111/112不标整体通过。

下一最短仅已有片段：秘密武器从操作前历史Step到选择窗，查看完整出牌；选择确认后的消耗结果Back-Step，查看消耗动画/粒子尾和右侧消耗堆首次显现、数字缩放反馈；随后Step进入下一动作一次，确认前一尾不会随下一段清掉，再Stop正常退出。无需重录/胜利或重测已解决项。

### 本轮最终候选为018；016/017在人工测试前由退出回收审查替代

016和017封版后未交用户运行；分别保留了原pair/源码和superseded验证说明。017补了尾不能成为下一段被遮罩的PlayContainer子孙这一点。随后对原生NCardExhaustVfx._ExitTree复核发现，运行中的normal消耗fx重挂也会触发其清理卡牌；原生DelayedFree又会在稍后处理该卡。因此最终018不在交还时重挂活fx，而是在Exhaust.Create/Play前就建好同层独立持续锚点，private quick卡也在Create前进入锚点。边界只转移所有权标记，native fx保持原位置和退出生命周期；下一段遮罩排除持续锚点。私有卡Retiring标记继续避免延迟清理前重新借出。

原生前沿来源fx使用共享池卡，不能直接销毁后让原生DelayedFree再次归池。018对已离开时间线的normal来源fx只隐藏，保持其原生一次延迟清理；quick若仍寄生在旧卡内，则隐藏并把旧卡同步移到隐藏退休锚点，重挂期间临时沿原生_isFinishing退出保护，立即恢复原值，不复活/重派旧Task；原生当前quick协程以后正常完成一次卡回收和粒子延迟清理。旧卡于是既不参与Play容器的成员/FindOnTable查找，也不被提前共享回收。来源退休锚点随卡原生退出清理。这是根据原生清理语义的定向处理，不能从静态检查认池生命周期集成已通过。

最终build-018 SHA1BEE9423D41847F132745822C7A657F1DCCD460C73C04079C94B8564827DE9D9，收据build-018-17d92e65，sealed/output/installed一致，game0。018 Release零错误警告，presentation/decision-route/recording-files-build-018通过；build-018-validation.json明确无原生UI、没有新画面/性能/延迟回收集成通过。015可用pair仍在build-016-2b86479a preparation，016/017中间pair和对应源码亦保留，018源码和合同保存同恢复点当前preparation。前述016机制说明由本段生命周期修正覆盖，PM和用户只测最终018。原录像及正常进度不改，不增加整场或池压力测试矩阵；下一仍只秘密武器出牌/消耗/消耗堆反馈及随后Step一次的最短片段。

## r18 / 018此前问题用户通过，019死亡同伴强化时钟及完整尾候选

用户明确“已经都好了”，指018此前出牌、消耗尾及牌堆反馈修复；当前余项是蛞蝓死亡与同伴4力量显示在历史里不同步，缺强化动画，首只死亡不完整。保留已通过014意图、015入手/让位/层级以及018出牌/消耗机制，不重新扩大旧矩阵。

核对r18 runtime2f41e361390c493db82e4310bdbd04e6为018 SHA1BEE9423…827DE9D9：1581事件，39原生操作、56路线到达、57恢复、27历史显示，3次正常Stop宿主15/12/12；15步完整胜利一次，Error均null、原manager/所比较Continue/progress保持。37池检查无disposedNCard。原1581事件及Godot逐SHA归档同恢复点runtime/content-002-r18，公开runtime-r18.json/runtime-verification-r18.json；54saves及原片SHA保持，game0。该结果证明最终状态/退出，不能据此否认力量/死亡的显示时刻错误。

原生RavenousPower.AfterDeath在同伴死亡时先Flash/SFX/DevourStartTrigger，等待短段后将_IsRavenous设真、Stun、Apply Strength。第一次重锤来源trace1.8742565秒/124帧：Dead0.1031589秒、DevourStart0.1060277秒，0.3593341秒有buff声音。旧未采Power HUD及PowerApplied/Flash原生fx，不能把声音当精确力量图标时刻；历史力量图标确实只能随末端DataRestore更新。旧仅重播Dead触发，未跑原生AnimDie/fade纯显示流程；目标RemoveForForwardTarget还会取消并清理死者场景，可能截断长尾。

019新增PowerHudTrace：采实际原生NPower图标纹理/位置/尺度/透明度和数量文字/字体/对齐，按源共享时钟以小范围只读图元展示。真实PowerContainer整段遮罩，显示锚在其同父同层位置，避免目标恢复新建NPower时露出额外入场；数量和入场取源采样，不给真实Power改数/挂事件或接受输入。交还把末值接到真实NPower并退役其额外_animInTween，再揭开原容器。只覆盖当前已遇到的端点，不宣称所有Power shader或粒子都已采全。

共用NPowerAppliedVfx/NPowerFlashVfx/NPowerAppliedBuffVfx/NPowerAppliedDebuffVfx.Create仅在来源实际返回效果时录cue，历史调用这些审查过的原生纯显示入口并沿正常CombatVfx父级播放；正常边界的纯特效尾加入既有OwnedTails，Back/Stop退役，不调用PowerCmd/AfterDeath/业务事件，不重复加4，不提前提交目标真实数据。VFX使用源Power身份及记录的增量/类型，badge文字/纹理本身是样本，最终业务/RNG保护仍在；其他动态Power内容覆盖不得外推。

NCreature.SetAnimationTrigger来源现在只在其确实进入CreatureAnimator.SetNextState时录下实际选中的AnimState.Id。历史直接让同一原生Animator选这个显示状态，避免延后恢复的数据重判条件；被原生忽略的trigger不再以旧循环冒充解析结果而重启。旧日志没有解析后的状态，所以不能据它证明此前哪个Devour条件实际误判，新增native-creature-animation-replayed提供Recorded/Actual核对。最终目标交还仍沿013自然目标循环规则，不额外重置幸存者。

死亡来源StartDeathAnim记录显示cue；历史在已播放记录的Dead动画后，启动新的原生NCreature.AnimDie(true,新节点token)显示任务并调用NCombatRoom.RemoveCreatureNode这个UI移出入口。它负责原生意图/死亡HUD、等待自身死亡动画、NMonsterDeathVfx、淡出和节点延迟清理，未执行CreatureCmd死亡/AfterDeath/PowerCmd/AI/战斗数据移除，不复活旧Task。UI死亡名单有显式原Creature→当前NCreature归属；仅在历史播放期间允许这些已归属死者的UI节点离开旧数据checkpoint的显示名单。真实target Graph、成员/身份/RNG核对不放松。前进提交将死者从UI列表清出但不取消OwnedDeath/原生fade；自然结束可跨下一节点。Back/Stop取消专有死亡显示并按原规则重建，fade也按原死亡归属退役。原生fade自己播放enemy_fade声音，通用声音cue不重复该声。

新日志包括NativeDeathDisplays/NativePowerCues/PowerHudSamples、native-death-display-started、native-creature-animation-replayed和native-power-handoff，供最短人工检查实际时刻/解析状态/清理。Release零错误警告，presentation-contracts-build-019.json、decision-route-contracts-build-019.json、recording-files-build-019.json通过，原片41事件/15操作/8选择/SHA不变。纯合同未构造原生UI，不验证新增死亡协程/fade、取消退役、Power层级或视觉时刻，build-019-validation.json明确实际未测/性能未测。019 SHA7E675E35…4330F112，收据build-019-5082923a，sealed/output/installed一致；018可用pair/settings/selector及019源码/合同保留同恢复点preparation。原账号/ReplayMod/modded1、Cloud沿本批确认off、game0，无自动启动/Computer Use、其他账号或存档恢复。D-110/111/112不标整体通过。

下一仅原片首个重锤击杀：先自然Step到首杀结果，再Back回重锤前Step，观察同伴devour/强化FX和4力量图标在死亡过程中出现、首只死亡/淡出完整；再Step下一动作一次查尾仍继续，随后Stop正常退出。无需重录/完整胜利或复测已解决项。

## r19 / 019死亡与强化用户通过；020状态图标尺寸修正

用户确认上一轮死亡/强化同步已好，只报告历史底部状态栏图标变大。核对r19 runtime a3c24c7288db4365b81fdca29e62aa79 为019：866事件，13原生操作、44到达/38恢复/13历史显示，一次宿主13 Stop，Error=null及原manager/所比较Continue/progress保持，未胜利。29条native-creature-animation-replayed的Recorded/Actual全部一致，5次原生死亡显示、13次Power交还、15个历史Power cue；这些支持019新路径实际执行，不能据此判图标尺寸正确。原866事件/Godot逐SHA归档同恢复点runtime/content-002-r19，runtime-r19.json/runtime-verification-r19.json；54saves/原片SHA保持，game0。

PowerHudTrace的重播TextureRect是新建控件，019只写Texture/Size/Scale，没有同步原生ExpandMode/StretchMode/CustomMinimumSize；默认KeepSize会让纹理自身最小尺寸限制目标小尺寸，是代码层的明确遗漏。020在采样中保留这三个原生属性及pivot/rotation，展示时先设置尺寸策略和最小尺寸，再赋纹理与实际Size，不猜固定像素或缩小系数；正常交还沿同一属性，原有数量/字体/对齐/时钟保持。native-power-handoff新增源图标矩形和模式摘要方便核对。只改PowerHudTrace.cs，不动019死亡/条件动画/数据恢复/原生Power特效及018/015/014已确认机制。

Release零错误警告，presentation/decision-route/recording-files-build-020通过，原片41事件/15操作/8选择/SHA保持；没有原生UI构造/画面测试，020大小改善尚待人工。build-020-validation.json记录范围及限制。SHA28280571…A1531DD1，收据build-020-5c6f068a，sealed/output/installed一致，前019pair/settings/selector及020源码/合同保留同恢复点preparation；原账号/ReplayMod/modded1、Cloud沿同批确认off、game0，无游戏自动启动/Computer Use、存档恢复或其他账号。D-110/111/112不在此标整体通过。

下一仅原片任意一个有底部图标的动作Back-Step，对照图标大小，Stop正常退出即可，不要求重录/胜利或复测已过死亡强化。
