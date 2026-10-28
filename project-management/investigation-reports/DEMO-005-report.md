# DEMO-005 非初始牌、能力、遗物触发与药水

2026-10-27，沿D-101工作包。Implementation结论：**FIXED_CONTENT_SAMPLE_RUNTIME_PASSED / SUBMITTED_FOR_PM_REVIEW**。build-002首轮两次后退失败保留；build-003原片连续后退、真实胜利、Stop和新Runtime重开/短测/退出核对通过。用户确认连续后退符合预期，并补完成最后重开检查。PM正式验收由PM维护，不外推全部内容支持。

## 候选与证据

当前ReplayMod0.1.0 DEMO-005/build-003，DLL SHA256 `D02C7335EB39C0338C85CF2556687ABA0A1F4D35841FF944D4752EFEC61B1130`，manifest SHA256 `69185E5024D560BC5C9B81FC64CC2F732745BCFBA31E64F069CAEF6AAEABE8FB`。sealed pair在`evidence/demo-005/builds/build-003/`，安装收据`evidence/demo-005/preparation/build-003-f933eb9e.json`。构建0警告/错误；新253事件中32次forward、15次restore/显示核对通过，完整17步胜利并Stop，失败证据保留。

6项纯.NET检查通过，`evidence/demo-005/content-contracts.json`：格式3升级非初始牌/药水玩家目标roundtrip、缺失/混合输入身份或无效目标拒绝、checksum保护新元数据、旧字段默认字节保持、药水原对象/槽位/队列标记与遗物计数/用药历史恢复、实际DEMO-004原11步文件envelope/元数据/Preparation不变。测试未运行游戏或构造器，不能证明药水、遗物或原生UI实测通过。准备与读取脚本语法检查通过。

build-001（`18E2F2A78752FB804BE81F1BFD03906F66D70618A022E09D13AF57661CC48BEA`）是未运行的中间候选，sealed/收据保留。最后源码检查发现原有memento的CurrentMapPointHistoryEntry根未被Data范围接受，本次药水会写PotionUsed，故build-002补齐房间历史、PlayerMapPointHistoryEntry及相关保存DTO子图，并把安全历史摘要加入格式3投影，避免恢复槽位但重复累计用药历史；另加真实用药可用标记/UI核对。没有放宽断言掩盖失败，也无旧失败运行可追认。

## 输入与执行路径

`Clip.cs`格式3增加CardKey（战斗卡ID、升级、状态指纹、目标类型）、PotionKey（槽位、模型、状态指纹、目标类型）和可选TargetRole。原始操作仍有手牌索引、模型、turn、target和After；普通手动卡牌不再绑定3个ID白名单或固定10张/未升级。要求官方程序集的mutable模型、支持的目标机制，并保留文件数量/4MiB/身份/checksum限制。格式1/2仍使用原操作规则/投影，新空字段与默认Enemy角色省略，保留旧envelope字节。

Card输入使用实际有序手牌实例，校验NetCombatCard ID、升级和状态指纹后调用原生TryManualPlay；指纹含持久卡状态、局部能量成本/X值、星能、exhaust标记、DynamicVar Base/Enchanted值等，不含变化中的UI预览值。未以另一组小牌ID列表取代原白名单。当前目标机制None/Self/AnyEnemy/AllEnemies/RandomEnemy/AnyPlayer/AllAllies，不意味着该分类全部内容已验证。

Potion输入从实际UsePotionAction记录槽位、模型、目标及key，等待原始ActionTask和完整PotionModel.OnUseWrapper成功，再比较真实消耗/效果。观看核对实际槽位模型、指纹、可用/移除/队列标记与目标，调用EnqueueManualUse而非写能量/删除槽位。Card Self可为空目标，EnergyPotion AnyPlayer是实际player/CombatId0；目标角色/ID与普通enemy分开定位，不按名称混淆。

遗物效果依附于原生卡牌/EndTurn/用药效果链，**没有额外“遗物触发”玩家指令**。operation-mechanics区分recording/playback，记录原输入、前后能量/能力、遗物SavedProperty/Status/vars、药水槽/vars、各有序战斗牌实例/状态及安全房间历史摘要。新普通投影增加同样Mechanics，胜利仍在真实CombatWon且奖励生成前冻结；不手填预期结果。

实际额外选择通过ReserveChoiceId检测；自动出牌链、生成新战斗单位、生成卡牌、战斗中遗物成员变化及其他不支持动作明确将本场标记不完整/不封存，观看异常额外动作报错而非静默跳过。正常游戏原生动作继续，本mod不把这些操作漏掉后封存完整胜利。该机制界定的是第一批输入通路，不宣称全部原版卡牌/遗物/药水支持。

## 直接恢复与显示

memento保留真实Card/Potion/Relic对象、完整选定数据图和RNG，补入NetCombatCardDb的ID双向映射/nextId（不恢复其live订阅列表），协调实际战斗卡StateTracker订阅，尤其是能力牌从战斗牌堆移除后的回退。原敌人身份/顺序/AI和相关订阅沿DEMO-004机制；没有重载房间或前缀重演。房间历史和PotionUsed也回到相应稳定点，不以药水槽恢复代替全部状态。

在数据恢复前退役旧NPotionHolder，让旧消费Tween/延迟灰化/取消回调只引用旧显示节点；按恢复后的原Potion对象和实际槽位建立新显示，正常前进仍用原生消费动画。刷新NRelicInventoryHolder的实际计数/Status，恢复后核对标签文本、药水槽模型引用/空槽alpha/使用标记、能量标签、手牌实例顺序/布局以及既有模型缩放/位置/色调/稳定动画循环。

HappyFlower原生AfterSideTurnStart更新TurnsSeen、Status并GainEnergy保持。其私有DoActivateVisuals仅Flash、临时IsActivating显示3、等待1秒再写false；观看用即时Flash代替这个装饰任务，保留逻辑计数显示，避免旧延迟setter影响恢复，不等待装饰完成。正常录制不修改这条视觉路径。未把“装饰显示3”当作权威TurnsSeen；不是任意遗物异步行为已通用解决。

## 定向准备与人工检查

默认新局入口不变。可选主菜单“测试准备：升级牌／能力／遗物／药水”，可取消、只消费下一支持的新局；选择暗港CorpseSlugsWeak，真实初始10牌后增加升级ShrugItOff与Inflame，原生创建/加入牌组、升级并Finalize；加入HappyFlower（TurnsSeen初始0）与0号槽EnergyPotion。准备在SetUpNewSingleplayer完成后、首战checkpoint之前，保存实际12牌/遗物/药水。观看只从原生checkpoint加载，不再次注入。新录像Preparation=`DEMO005-directed-CorpseSlugs-ShrugPlus-Inflame-HappyFlower-EnergyPotion`及菜单明确标定向，不冒充随机掉落/自然遭遇。

用户手动启动，沿用本批Cloud关闭与原账号/modded1，点测试准备后新建铁甲A0。古代事件选不改变牌组的选项；摸到耸肩无视+和燃烧就真正打出，在花掉部分能量后实际喝能量药水。至少等到第三玩家回合，小花计数2→0、实际自动获得1能量，再打完双怪。不领奖、不进入下一房间，保存退出菜单。

打开同一录像，在用药、两张新牌、进入第三回合的EndTurn边界分别后退并重做；检查槽位、抽牌/牌堆、Strength、能量、小花计数和显示，可同场检查跨一次中途击杀。随后播到胜利，Stop、再打开并中途Stop，最后正常退出游戏收齐日志。出现未封存/回放失败保留提示、同任务排查，不要求重复盲roll。

初次真实运行已取得下文部分证据，但两次恢复失败，本包尚未通过。待修复后补10→9快速恢复/重做、完整观看胜利/Stop重开及退出context证据；不用重录。

## build-002 首轮实测与 build-003 修复

用户反馈录制、能量药水成功；开心小花回退偶发失败，在进入下一回合时连续点击更容易复现。实际新clip `269735cd78054ec7b5002fd28928229a`，格式3，seed `ETHWSQJ07VG3`，17步胜利自动封存，来源build-002精确hash不变。文件SHA256 `32C77BE1DDAC6252742CF74F33B2EFF4F22B160E4ADA572398D168F1C59BA374`。

操作1为Inflame，操作5为升级1的ShrugItOff，操作7为0号槽EnergyPotion→player0（0→2能量，用药历史空→一条），操作9击杀enemy2，操作10 EndTurn进入第三回合（HappyFlower TurnsSeen 2→0、Status Active→Normal、能量0→4），操作17最终胜利。准确的SavedProperty在原始Initial/After投影及`recorded-mechanics-r1.json`安全摘录中；通用operation-mechanics事件的SavedDTO字段序列化较少，不把其props空对象当没有计数。

Runtime `336af9167a524cffa5623058670cd7c8`，381事件，两次打开；45次forward、25次成功restore/内容UI/显示核对匹配，含7→6两次和10→9五次成功，实际用药前进4次、触发小花的第10步前进7次。**另有两次10→9失败**，事件0112与0378的原因均为`The given key 'Creature 噬尸蛞蝓' was not present in the dictionary.`，对应两次playback-failed。两次closed均Error=KeyNotFoundException、原manager及所比较Continue/progress保持，未播到观看胜利。多数恢复成功和真实录制胜利均不能追认观看完整成功。

源码定位：`DirectCheckpoint.RefreshUi`遍历整个NRun子树，假定每个NCreature都有previousPowers活成员锚。第9步死去的enemy2已经移出CombatState，但其异步死亡/淡出节点仍可能在场景树；第10步逻辑就绪后立即后退，当前/目标成员相同，不走跨成员显示重建，于是访问已移除者的字典项报错。两次日志游标都是已完成捕获的10→9，不是两个后退同时执行。小花触发与短暂死亡显示窗口重叠，不需增加遗物或死亡动画结算等待。

build-003在每次恢复前清理已退场原敌人的显示/淡出，即使当前与目标成员相同；原跨成员恢复仍清理/重建相应显示。UI遍历限定当前CombatState且与NCombatRoom活动节点绑定的真实成员，排除退场子树。对真正活成员仍严格要求原能力锚，保留完整projection、UI、RNG、同对象及忙碌保护断言；未以忽略所有缺失项掩盖恢复缺陷。新增checkpoint-display-cleanup记录清理数量，WaitedForDeathAnimations=false。未修改小花原生能量/计数逻辑或录制schema。

明确兼容本次E428…686A录制build，原17步文件envelope、升级/药水玩家目标与元数据读回通过，证据`content-compatibility-r1.json`；原文件不改hash。安装当时只完成构建和原文件兼容检查，未以纯.NET假场景代替手测；随后取得下文r2/r3实际修复结果。

失败原381事件见`runtime-r1.json`，原文件/逐事件副本hash读回收据`runtime-archive-r1.json`；原始clip/checkpoint与session私有存于旧恢复点`runtime/demo-005-r1/`。读取脚本输出深度改48避免新Mechanics日志在深度12被截断，公开运行事件按原文件重新读取，不使用早期截断副本作完整证据。

## build-003 原片重测 r2

用户确认连续后退符合预期并已正常退出。Runtime `8ed0e030ba89454984a48f552284f3cc`，253事件，loaded build-003、仅ReplayMod/modded1；仍读build-002原17步文件，原SHA32C7…A374不变。32次forward均StateMatched，15次恢复均有cleanup/内容UI/模型显示/roster/arrival链且原对象保留；未出现failed/error/difference/unavailable。

具体后退：10→9七次、11→10一次、7→6三次、9→8三次、6→5一次。第10步原生小花触发实际前进8次，第7步真实药水前进4次，均与原投影/RNG匹配；15次药水槽/原模型/使用标记、遗物计数及能量标签、手牌布局与选定模型显示检查通过。此前r1已观察1→0和5→4，分别覆盖能力和升级抽牌回退；不把这些r1局部成功改写为整个失败观看通过。

4次同成员恢复到9的cleanup确实清理一个退场NCreature及一个死亡淡出，RebuildLiveEnemies=false、WaitedForDeathAnimations=false；覆盖了原失败窗口。另3次9→8跨中途死亡重建显示、恢复原enemy2，并能重做第9步。所有恢复RoomRebuilt=false、PrefixOperations=0，本次观看仅首次room-load一次。实测恢复约19.03–111.08ms，仅本样本观察，不是性能上界。

回放真实执行至17胜利，原生胜利Task成功、奖励UI拦截，最终closed Position17 / AllOperationsMatched=true / VictoryReached=true / Error=null / OriginalManagerRestored=true / NormalContinueAndOriginalProgressUnchanged=true，含原progress文件。读取时game0。r2只有一次打开，故没有单凭它声称胜利Stop后的重开；请求只补打开/Step/Stop/退出，已由下文r3补齐，不重复全场。

证据`runtime-r2.json`与`runtime-archive-r2.json`；原clip和253个原始事件逐项copy/hash读回，私有归档位于最新DEMO005恢复点`runtime/demo-005-r2/`。没有重新录制、篡改原文件或重新构建安装。r1两次失败与原始候选pair仍保留。

## build-003 重开短测 r3 与最终结论

用户按请求重开并答“好了”。新Runtime `9f7b3f543cf54398bd1019b2923286cd`，45事件，build-003精确身份再次打开同一原17步文件，InitialMatched=true。4次forward（第1步三次，第2步一次）、3次直接恢复（1→0两次、2→1一次）及内容/显示核对全部通过，覆盖本修复版Inflame能力撤销和按原录像重做。最后在位置1中途Stop，Error=null、OriginalManagerRestored=true、NormalContinueAndOriginalProgressUnchanged=true；AllOperationsMatched=false/VictoryReached=false是此次有意短测停止，不是播放差异。读取时game0，无错误/差异/不可恢复事件。

修复后r2+r3共两个Runtime、298事件、36次forward/18次直接恢复，均匹配；一次完整17步胜利Stop，随后一次新Runtime重开/能力恢复短测Stop。原失败窗口10→9七次成功、四次同成员清理真实残留死亡显示，药水恢复/真实重用、计数/能量/历史/显示核对与跨中途死亡恢复均有上述具体证据。r1的能力/升级抽牌局部通过、首轮两次失败与r2/r3修复后结果分别解释，不合并伪称所有观看成功。

证据`runtime-r3.json`、`runtime-archive-r3.json`；原clip SHA32C7…A374保持、45个原事件逐项副本hash验证，私有归档在最新DEMO005恢复点`runtime/demo-005-r3/`。现源码/sealed/installed D02C…1130保持，不再构建/追加矩阵。本轮未产生新的正常录制进度，无需再做整包备份；原恢复点及r1/r2/r3私有原片/日志均留存。

提交PM按这份固定组合样本核对收束。仍只证明本次ShrugItOff+、Inflame、HappyFlower、EnergyPotion和双蛞蝓组合，不证明所有卡/遗物/药水、额外选择/自动出牌链/生成、终局回退、玩家失败、后续房间、持久化强记录/纠偏或一般跨版本。用户整体体验与逻辑投影/RNG、选定UI读回分别记录，非任意像素/动画顺序完备保证。

## 安装与恢复

沿D-092，game0/原账号匹配；本次安装前比较最新恢复点收据的36个现有save文件，全部匹配，复用`C:/Projects/STS2ReplayMod-local/backups/DEMO004-20261027-003416-2f3bc72631/`。原DEMO-004/build-001 pair/settings/selector在该恢复点`preparations/build-001-bc6a32ee/`，中间DEMO-005/build-001在`preparations/build-002-3df2e037/`。旧所有恢复点、可用pair、原始录像与297事件留存，未整存档恢复、修改本体或读取其他账号。未自动启动游戏/GUI操作/正式headless。Cloud沿用户已确认状态，不自动修改。

build-002初装身份核对见`evidence/demo-005/build-002-validation.json`，其状态不覆盖后续真实失败；候选安装和纯.NET检查不代表运行/产品隔离已通过。PM管理状态文件由PM维护，后续按本次真实运行补报告，不自动扩到全内容或下一能力。

本次新正常战斗进度后，在用户正常退出/game0/原账号匹配时建立新完整恢复点`C:/Projects/STS2ReplayMod-local/backups/DEMO005-20261027-013423-1f0ea24e59/`，1277文件/132040266 bytes来源与副本逐项匹配；收据`preparation/DEMO005-20261027-013423-1f0ea24e59-backup.json`。build-002 pair/settings/selector替换前保留于新点`preparations/build-003-f933eb9e/`，未恢复整存档。当前源码/sealed/installed build-003相同，game0，见`build-003-validation.json`；旧build-002 validation是历史候选检查，不覆盖本轮失败。
