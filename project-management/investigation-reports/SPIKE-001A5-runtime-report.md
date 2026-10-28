# SPIKE-001A5 runtime 记录

创建：2026-10-09；更新：2026-10-22。当前：`RUNTIME_COMPLETE / READY_FOR_PM_REVIEW`。已完成一个有效实际A/B，结果为**SEMANTIC_EQUIVALENT，差异0**；B正常退出及有限收尾完成，游戏进程=0。A5持久disabled，精确安装pair已移除，workspace候选/源码/公开与private证据保留。正式已Arm2/3（1个用户中断、1个有效A/B），不再运行第3轮；等待Project Lead验收，不自动解锁SPIKE-001B。

## 验收入口与判定汇总

| Gate | 最终证据 |
| --- | --- |
| 固定环境 | v0.111.0 / 41cef1ea / game hash 0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9；Harmony2.4.2.0；原A5账号modded1；actual only SPIKE001A5 |
| 有效attempt / candidate | 3e6aaa0160e441c293b5ac4a0030b6d0；runtime-r1 DLL AB97852918D5291B5D38E4D80C199C38A71F154A94EC982F5CE70510ACB52596；同candidate A/B |
| A / fresh B进程 | 92576:639281931959340480 / 105216:639281953241414060 |
| 同一room-before输入 | PreFinishedRoom=null；sealed hash BD4E2D804E5929224301BFE0AB564BC1322B3E7B2FFE8A987C8DE4A0D734B100；A退出后、B配置后、fresh B主菜单均保持exact bytes；Continue prefix成功路径再次验证 |
| floor / 场景 | checkpoint Floor=1；A/B playable TotalFloor=2、Act0 row1 col2、round1；Standard Ironclad A0，seed JFZS2H4V4XN2，ENCOUNTER.CORPSE_SLUGS_WEAK |
| A window | Engine42433–42462，30帧、20字段全True、max gap34ms；79 callbacks、2 heartbeat |
| B window | Engine8137–8166，30帧、20字段全True、max gap34ms；332 callbacks、4 heartbeat |
| oracle / compare | 两份独立8088-byte snapshot，hash均DF34448C39CCEBC0DAE1F1C76006656D11BF00766A0B3A1EDB2A15EA506B4717；Differences=[]；独立JSON DeepEquals=true |
| commit有效性 | A.complete与A.capture相同；B.capture及compare-complete存在；A/B marker、process/schema/revision/checkpoint均匹配；始终无terminal |
| 退出 / 安装收尾 | 用户报告两端正常菜单退出，无额外游戏动作；最终进程=0，精确两个安装文件移除、A5 disabled、原账号29份剩余installed manifest全部有disabled source记录 |
| 原产物保留 | 原r1索引24/24、r2索引331/331、runtime-r1 build索引326/326 hash全部匹配；18个其他旧probe安装文件未变；旧/新private active与checkpoint保留 |

主要复核文件：[AB结果及独立核对](../../evidence/spike-001a5/runtime/AB-result-3e6aaa0160e441c293b5ac4a0030b6d0.json)、[A退出/B输入](../../evidence/spike-001a5/runtime/A-exit-input-3e6aaa0160e441c293b5ac4a0030b6d0.json)、[B fresh主菜单](../../evidence/spike-001a5/runtime/B-precheck-3e6aaa0160e441c293b5ac4a0030b6d0.json)、[最终收尾](../../evidence/spike-001a5/runtime/closeout-runtime-r1-2026-10-22.json)、[runtime交付索引](../../evidence/spike-001a5/runtime/runtime-delivery-2026-10-22.json)。公开原始snapshot/event/metadata在records-3e6aaa0160e441c293b5ac4a0030b6d0，未导出raw checkpoint/path/config。

结论只覆盖下文冻结投影与这一固定入口的首个稳定操作点；其他内容/版本、后续操作、未投影字段、完整Replay、隔离、性能、crash recovery仍未验证。Cloud关闭、UI动作及正常退出是用户现场报告；进程、文件、metadata、窗口与比较结果为机器证据。

## 准备窗口

sealed phase0-r2 DLL/manifest精确安装，DLL SHA-256 `8DFB0C4AE6393F61D178EBA7A380690F0574BBE27C69246BB53B214306C5BF09`，manifest `F6A3230D5827A0B010C3BF2F572781A13959E9052253C54C200AB87019328759`；固定游戏/Harmony身份通过。所有旧probe安装文件hash不变。

配置修改只在游戏关闭时发生：保留原20条mod记录，将SPIKEINV012A残留enabled改false，添加本地SPIKE001A5 enabled及7个已安装但配置缺失的Workshop disabled记录；mods_enabled原已true。所有实际installed (id,source)均有显式记录，唯一有效enabled为本地A5。完整原settings只留probe私有配置备份，不进入公开evidence。

非mod account设置canonical不变，原mod记录除了is_enabled均不变、不删除。modded selector已为1，未写。没有读取unmodded selector或任何profile payload，没有恢复native。设置原hash `7642CDC6B6DB6EAF22655622DFE616EC621F0C39B8AE020119DE9C400DDCC7CB`，准备后hash `3BC1DB270333E2564335988FA5D509EBF9FD343AD9FDA172575C1877E1B3633D`；selectorhash `9739AD8352E422CDD47C1BDD54CD0895CF37B5473BB0D7F9BA7F1AA0D5630829`。

准备脚本遇到PowerShell JsonObject枚举、UTF8 BOM及语法/回读问题，均在相同配置范围修复；其中第一次写入已完成allowed变更，但回读集合检查被脚本枚举问题拦住。随后用原始private备份与当前配置独立比较，证明严格allowed范围，再通过完整安装/有效source集合回读；未启动游戏，不占正式业务或probe修复预算。探针源码/候选没有因此变化。

证据：[准备收据](../../evidence/spike-001a5/runtime/preparation-e8cd4e7b7a144bd4a9f9f0ce90687cfe.json)、runtime/configuration-change-*.json（原始变更核对）、[固定schema IL](../../evidence/spike-001a5/runtime/config-schema-il-2026-10-09.txt)、[实际A1请求](../../evidence/spike-001a5/runtime/manual-request-A1-main-menu-2026-10-09.md)。Phase0-r1/r2封存报告、331文件交付索引与原草案均未覆盖。

## 等待事项

用户本次现场确认Cloud关闭并手动进入modded1主菜单后，先验证actual loaded only A5与PRECHECK_PASS/READY_A_ARM，才安排Standard Ironclad A0/Neow、F8与唯一Monster目标。A的snapshot/commit及正常退出后seal exact bytes必须成立，才放行fresh B正常Continue。一个有效等价或差异答案即停。

## 2026-10-21 主菜单拒绝与配置修正

用户报告“已到主菜单，Cloud关闭”。当时进程PID=77640、StartUTC=2026-10-21T11:33:45.7152688Z；游戏仍为固定v0.111.0/41cef1ea，game/Harmony/live r2 pair身份未变。日志只有A5完成初始化，actual Loaded 1 mods；初始化profile token却是modded/profile2，且出现PRECHECK_BLOCKED、没有READY_A_ARM。退出后modded selector=2与此一致。因此是配置偏离目标导致身份门正确拒绝，不是readiness或重建失败。

用户按请求主菜单正常退出，进程=0；没有F8/Arm，r2 active pointer不存在。此前同revision的两个历史目录仅见first-tick、无arm；本拒绝进程没有对应采样事件，不能将历史Ready当作本进程通过。未读取任何profile payload，未开始业务attempt。Cloud关闭只作为用户报告，不能由日志中的SteamRemoteStore调用独立证实或否定客户端实际同步设置。

过滤拒绝证据：[precheck-rejected-2026-10-21.json](../../evidence/spike-001a5/runtime/precheck-rejected-2026-10-21.json)。该收据Process后缀曾手工誊写错误；保留原文件，按已观察UTC start换算ticks的更正及原hash见[precheck-correction-2026-10-21.json](../../evidence/spike-001a5/runtime/precheck-correction-2026-10-21.json)。没有导出raw log或其他槽payload。

依第7节在游戏关闭时直接修正配置：[preparation-5ede462279f247788dba92b91003cf67.json](../../evidence/spike-001a5/runtime/preparation-5ede462279f247788dba92b91003cf67.json)。实际需改的settings与modded selector均先create-new完整备份到private probe目录，两个backup hash逐一匹配源before hash；非mod设置及selector其他字段canonical不变。selector改为1；补回loader重新生成mod_list时省略的已安装Workshop disabled记录，唯一有效enabled仍是本地A5。旧probe文件不变，live r2身份不变，未改探针源码、未消耗harness修复或业务预算。

下一步是同一r2候选的fresh主菜单预检；启动前重新由用户确认Cloud关闭。本次只修配置，不重复Phase0验证，不复用任何历史precheck目录。

## 2026-10-21 fresh主菜单通过

用户再次报告“已到主菜单，Cloud关闭，保持运行，未做其他操作”。本新进程为70340:639281797648938670，StartUTC=2026-10-21T11:42:44.8938670Z。固定game/Harmony/live sealed r2身份一致；当前日志确认A5唯一完成初始化、actual Loaded 1 mods、runtime profile token=modded/profile1，存在PRECHECK_PASS READY_A_ARM，无PRECHECK_BLOCKED或terminal。

本进程自己的新目录为861af3a9e6d449acbd192b0a8d3c2f56；first-tick在monotonic443ms、ready-A-arm在459ms，schema/revision/candidate/process一致，无arm、terminal或active pointer。两份明确白名单event已create-new归档、hash逐份一致；未读取native checkpoint。证据：[本进程预检收据](../../evidence/spike-001a5/runtime/precheck-pass-861af3a9e6d449acbd192b0a8d3c2f56.json)。

仅解锁新建Standard单人Ironclad A0、完成Neow到Act1地图后停手的准备步骤；尚未解锁F8或节点点击。地图后需当前完整截图确认，后续仍按READY_A_NODE绑定唯一目标，不复用旧图位置。

## 2026-10-21 地图准备与Arm请求

用户回报“已到Act1地图，未点节点、未按F8，游戏保持运行”。本进程与候选/game hash不变，当前自有记录仍只有first-tick/ready-A-arm，无arm、active pointer、terminal或拒绝标记；未读取native payload。此项仅证明准备状态的用户报告与观察器仍等待Arm，不能代替native前驱校验。

已解锁只按一次F8后停手的动作，等探针产生READY_A_NODE和arm metadata后再结合当前完整截图给出唯一目标。截图尚未收到，未授权节点点击。证据：[地图前Arm门](../../evidence/spike-001a5/runtime/map-pre-arm-861af3a9e6d449acbd192b0a8d3c2f56.json)、[实际F8请求](../../evidence/spike-001a5/runtime/manual-request-A1-arm-2026-10-21.md)。

## F8成功后用户请求结束

用户回报仅F8一次、仍在地图、未点节点，并询问能否立即关闭游戏。已明确允许通过游戏内“保存并退出”回主菜单，再用主菜单“退出游戏”，不再进行F8、节点或其他业务输入；等待用户退出确认及进程=0。

停手请求时本进程仍在运行；probe自有arm.json与armed事件均存在，只有first-tick/ready-A-arm/armed三条事件，无seal、A.capture或A.complete。这证明Arm可达，不证明房间前checkpoint已经封存或A采集完成。只选择性归档了arm及明确白名单事件，没有导出native raw/path文件。

证据：[中止请求时记录](../../evidence/spike-001a5/runtime/interruption-request-861af3a9e6d449acbd192b0a8d3c2f56.json)。本次性质为用户要求在目标输入前结束，无有效A/B；即使退出链触发后续observer terminal，也须与此已声明退出窗口关联，不能把未执行目标的本次记录写成production reconstruction FAIL。退出后需完成有限收尾并交PM，不能拿本次未完成A配新B。

## 用户退出后要求继续与独立完整attempt

用户报告已退出并明确表示“现在我回来了，可以继续干了”。进程=0后核对旧attempt：只有arm和三条事件，无seal/A.capture/A.complete/terminal，active仍指向旧attempt。原active和arm保持原字节，另存公开pointer metadata作证；没有恢复或读取native。见[退出收据](../../evidence/spike-001a5/runtime/interrupted-closeout-861af3a9e6d449acbd192b0a8d3c2f56.json)。本次不是harness或production故障，用户中断未被改写为失败；后续不得将它拼成B。

为落实用户继续完整实验的指示，建立同一Mod ID的runtime-r1私有namespace及独立工作副本experiments/SPIKE-001A5-runtime-r1；原r2工程/候选/报告与全部331文件保持原hash。只改private revision、Harmony owner、manifest version，schema仍2。Cecil确认889个探针程序集方法及locals/EH/fields在两个identity字符串归一后完全一致，不增加业务逻辑、production调用、getter或数据范围。没有把身份隔离伪称成harness缺陷修复。

新验证：[validation-runtime-r1-f00fb3bf2ef04925981d2862e62c51fa](../../evidence/spike-001a5/validation-runtime-r1-f00fb3bf2ef04925981d2862e62c51fa/receipt.json)。Release 0 warning/error、125/125、4/4 patch/unpatch、dummy、138个原game/Godot引用与增量诊断审计通过。新增3项使用实际已公开旧pointer/arm metadata，在synthetic目录验证新旧namespace隔离、旧文件不改写以及旧revision拒绝；其他122项保持。源码副本来源见runtime/runtime-r1-source-origin.json，合同见[data-contract-runtime-r1](../../evidence/spike-001a5/runtime/data-contract-runtime-r1.md)。

新DLL 143360 bytes / SHA-256 `AB97852918D5291B5D38E4D80C199C38A71F154A94EC982F5CE70510ACB52596`；manifest363 bytes / SHA-256 `EF4B1D6FADA613C88CA850C9E4AC800C0376E181669335C6FC7CD87732A0A421`。在游戏关闭、原installed pair精确匹配且workspace旧pair可恢复时，仅替换本包两个文件，见[安装收据](../../evidence/spike-001a5/runtime/install-runtime-r1-2026-10-21.json)。原private active未改、未启动游戏。

### 当前配置门

继续配置前发现steam下有第二个数字账号目录；其创建时间早于本轮离线测试。最近游戏日志加载多份旧probe，与已验明only-A5/modded1的进程70340不同。本轮没有为另一账号读取settings/selector/payload，也没有据此宣称其存档安全或损坏。配置脚本在多账号门处停止，尚未写本轮配置。此前把日志中的user://逻辑路径当成完整绝对路径做prefix比较，false不能作为账号归属证据；这里据此修正“该日志属于另一账号”的机器证据说法，其账号归属未由那个旧比较单独证实。

Steam ActiveUser能唯一对应原A5账号目录，并与已知probe私有native-path的账号scope一致；但最近日志不代表该当前账号，故先向用户确认是否已切回原A5测试账号并保持不切换。待明确答复后才按三者一致性消除路径歧义：用户确认、ActiveUser唯一目录、既有probe私有scope；任一不一致仍不写、不启动。只导出布尔结果/数量，账号数字、原始配置和私有绝对路径不导出。新一轮还须完整配置回读、fresh主菜单identity/Ready、重新准备Standard地图，不能跳过前门。

### 原账号确认与配置通过

用户明确答复“已切回原A5测试账号，本轮保持不切换”。按该确认执行三重匹配：Steam ActiveUser唯一匹配目录、先前probe私有native-path的相同账号scope、用户当前确认，均通过。没有读取或修改另一账号的settings/selector/payload。

游戏关闭时完成准备及回读，见[preparation-f03bf31c6acf4b1f960fec6e00243ec1.json](../../evidence/spike-001a5/runtime/preparation-f03bf31c6acf4b1f960fec6e00243ec1.json)。原账号的settings仅在mod_settings内补齐已安装却被loader省略的disabled记录，原配置先create-new完整private备份且hash匹配；所有非mod值canonical不变。modded selector已为1，未写。唯一有效enabled为本地A5 runtime-r1，旧probe文件不变。新namespace active不存在，旧r2 pointer未改。

仅解锁fresh主菜单手动预检，仍须当前账号下由用户重新现场确认Cloud关闭并核对actual loaded only A5/modded1/新进程first-tick与Ready。证据：[A2启动前门](../../evidence/spike-001a5/runtime/prelaunch-A2-runtime-r1-2026-10-21.json)。下一完整A/B为第2个业务记录；目前尚未Arm。

## runtime-r1 新进程主菜单通过

用户报告已到主菜单、当前原A5账号、Cloud关闭且未做其他操作。新进程92576:639281931959340480，StartUTC=2026-10-21T15:26:35.9340480Z。game/Harmony/live runtime-r1 pair完全匹配；当前日志仅A5完成初始化、actual Loaded 1 mods、modded/profile1、PRECHECK_PASS READY_A_ARM，无拒绝或终态。

本次按日志实际user://steam/account/modded/profile1形式提取account token，在内存与当前ActiveUser相同；ActiveUser也与原probe私有native-path的账号scope相同。账号数字及私有路径未导出。初次绝对prefix比较导致的false已在发出业务请求前纠正；此处token比较才是当前日志账号门的证据，未改动probe或判定标准。

新attempt目录3e6aaa0160e441c293b5ac4a0030b6d0，只含本进程first-tick=685ms、ready-A-arm=707ms，schema2/revision/candidate/process一致，无arm、active或terminal。两份事件create-new归档并逐份hash核对，见[当前主菜单收据](../../evidence/spike-001a5/runtime/precheck-pass-3e6aaa0160e441c293b5ac4a0030b6d0.json)。只解锁重新新建Standard Ironclad A0/Neow到地图后停手，不复用上轮测试局或旧截图，不提前开放F8/节点。

### 新地图与截图的作用

用户报告已到Act1地图、未点节点/F8并保持运行，提供当前完整截图。截图显示80/80 HP、99金币、MODDED(1)，起点有两条直接通向深棕色普通Monster的路线；本图视觉上的左侧目标位于起点左上方。截图已create-new归档至runtime/map-A2-before-arm-2026-10-21.png，并以hash绑定到[新地图门](../../evidence/spike-001a5/runtime/map-pre-arm-3e6aaa0160e441c293b5ac4a0030b6d0.json)。它仅用于把探针target coord对应到明确UI节点，不作为semantic oracle或Arm/native前驱成立证据。

用户询问截图是否必须。已澄清不是每步硬性要求；只要能用文字唯一对应目标即可，本图已够，无需重复截图。仍保持先F8取得READY_A_NODE/arm，再确认唯一点击目标；“大概走左边”不替代探针目标绑定。本进程/候选/game未变，仍仅两条precheck事件、无arm/active/terminal；已解锁一次F8后停手，尚未开放节点。

### A2 Arm通过与唯一目标

用户回报仅F8一次、仍在地图、未点节点且保持运行。当前同进程出现armed事件（monotonic1151654ms），arm/active identity与candidate一致，无terminal。目标为Act0 row1 col2，前驱row0 col3；seed JFZS2H4V4XN2、CHARACTER.IRONCLAD，与当前截图相符。前驱checkpoint one player、Floor=1、RoomNull=false；live ArmFloor=1，READY_A_NODE row=1 col=2 floor=1精确一致。这里只绑定前驱，尚未生产seal，playable floor仍须后续按+1验证。

截图中该目标是起点沿左侧虚线直接连接的深棕色普通Monster，位于起点左上方，为两条起始路线中左边的一个。仅授权点击这个目标一次后停手并保持运行/聚焦，不出牌、EndTurn、暂停、领奖、下一节点或F8。期望由匹配production SaveRun封存checkpoint并在原30帧门后取得A_CAPTURED_STOP_INPUT；任何缺失/终态均不当成重建结论。

arm.json与armed事件已create-new归档、hash匹配；公开确认见[arm-target-confirmed](../../evidence/spike-001a5/runtime/arm-target-confirmed-3e6aaa0160e441c293b5ac4a0030b6d0.json)。计入第2个已Arm正式记录；第1个仍为用户主动中断、不算harness或production失败。

### A2 production seal与有效A提交

用户报告只点指定Monster一次，随后保持运行/聚焦、未出牌/EndTurn/暂停/领奖。本进程记录production-save-start=1306476ms、seal-start=1306507ms、checkpoint-sealed=1306517ms、A-capture-start=1306519ms。checkpoint hash为`BD4E2D804E5929224301BFE0AB564BC1322B3E7B2FFE8A987C8DE4A0D734B100`，与前驱hash不同；事实为one player、PreFinishedRoom=null、target row1 col2、checkpoint Floor=1，PlayableFloor=2，符合原floor绑定标准。

捕获79次sampler callback，2次有界heartbeat。初期phaseIsPlay/executorIsNotPaused尚False，之后自然进入20字段全True的30相邻Engine帧window：42433至42462、1308305至1309271ms、max gap34ms。capture-30-frames=1309318ms，A-stop-input=1309319ms，都在独立20s capture期限内。日志匹配保存wrapper与A_CAPTURED_STOP_INPUT标记均成立。

A.semantic为8088 bytes，SHA-256 `DF34448C39CCEBC0DAE1F1C76006656D11BF00766A0B3A1EDB2A15EA506B4717`；A.capture和A.complete均1167 bytes、相同hash `AE90EC683CDAD92E8F782C8F09488B2F0F493432DBA7E413E0290B4C151F83D6`。phase/process/schema/revision/candidate一致，20字段window与首末帧/间隔复核通过，无terminal。Position为Act0/row1/col2/TotalFloor2/round1/Monster；实际RoomModel与Encounter各自读取后均为ENCOUNTER.CORPSE_SLUGS_WEAK，不人为要求这两个合法语义ID字符串不同。

private checkpoint hash等于seal；在只针对当前账号modded1精确native路径的退出前双hash读中，native也保持同sealed bytes。只导出hash/semantic和公开metadata，raw checkpoint/path不出private。16份明确allowlist公开文件已不可覆盖归档、逐份hash匹配，见[A提交复核](../../evidence/spike-001a5/runtime/A-commit-3e6aaa0160e441c293b5ac4a0030b6d0.json)。这是实际A，不是完整A/B等价结论。

已解锁正常退出动作：游戏内“保存并退出”回主菜单，再主菜单“退出游戏”；继续禁止牌/EndTurn/领奖/下一节点/F8。退出是独立原生writer窗口，进程=0后必须确认native仍sealed exact bytes，才请求fresh B；不同则停止，不restore/rebaseline。

### A正常退出与B输入门通过

用户报告按保存并退出→主菜单退出正常关闭，未出牌/EndTurn/领奖/其他动作，退出无异常。进程=0；A.complete与A.semantic保持原hash，11条A事件未新增terminal，无terminal文件或B.claim。只读Test-SealedInput-runtime-r1核对native与private副本均仍为`BD4E2D804E5929224301BFE0AB564BC1322B3E7B2FFE8A987C8DE4A0D734B100`，退出没有改变本轮B输入。

当前Steam scope仍与本attempt私有路径一致，game和candidate/manifest未变。B启动配置脚本另版本化为Prepare-B-runtime-r1，仅接受同一active且已有A.complete、无terminal/B.claim；没有修改A/B候选、active或native。loader在退出时重建mod_list并省略未订阅下载项，因此按原授权在备份后补回明确disabled项，唯一有效enabled仍A5，非mod canonical不变，selector原1未写。配置后再次核对native仍sealed exact bytes，见[preparation-51d75397e5f94baba7fecd54f64f3b5d](../../evidence/spike-001a5/runtime/preparation-51d75397e5f94baba7fecd54f64f3b5d.json)。

已解锁用户fresh进程只到主菜单、不Continue的动作；需重新现场确认Cloud关闭，并先核对fresh process、only A5/modded1、一次B.claim和READY_B_CONTINUE。A进程是92576:639281931959340480，不接受同进程冒充B。退出及输入证据见[A-exit-input](../../evidence/spike-001a5/runtime/A-exit-input-3e6aaa0160e441c293b5ac4a0030b6d0.json)。没有restore/rebaseline，也没有将B的预声明NumReloads写入提前用于放宽输入门。

### 2026-10-22 fresh B主菜单通过

用户报告重新启动到主菜单、原A5账号、Cloud关闭，未Continue/新局/F8或其他动作。B进程105216:639281953241414060（UTC start 2026-10-21T16:02:04.1414060Z）与A进程不同；game、Harmony和runtime-r1 installed pair身份不变，actual only A5/modded1。日志logical account token、当前ActiveUser与原probe scope一致，没有拒绝/terminal。

同attempt产生一次B.claim，Process等于本B；first-tick=497ms、ready-B-continue=507ms，schema2/revision/candidate/process/attempt一致。B主菜单的生产观察器BInput gate已通过，另以只读工具复核native仍为sealed exact bytes（hash BD4E2D804E5929224301BFE0AB564BC1322B3E7B2FFE8A987C8DE4A0D734B100）。三份公开B记录已create-new归档并hash匹配，见[B预检收据](../../evidence/spike-001a5/runtime/B-precheck-3e6aaa0160e441c293b5ac4a0030b6d0.json)。

现在仅授权用户点主菜单正常Continue一次后停手，保持运行/聚焦；不F8、出牌、EndTurn、暂停、领奖或下一节点。probe不会自行setup/load。正常Continue后的NumReloads等预声明原生保存属于B业务窗口，不用于放宽先前B输入exact-byte要求。只有B capture/compare commit、无terminal、有效A/B窗口及同候选/CP均核对通过才形成一个有效答案；等价和差异均到此停测。

### 有效B capture与一个实际A/B答案

用户回报只点正常Continue一次，随即停手并聚焦，未F8/牌/EndTurn/暂停/奖励/其他动作。B-capture-start=209000ms，capture-30-frames=213460ms、compare-complete=213461ms，在原独立20s期限内。332次sampler callback、4次限频heartbeat；早期pcs/Phase.Play尚未ready，随后自然满足同一门。B window为Engine8137至8166、212461至213427ms，count30、20字段全True、max gap34ms。

B.semantic 8088 bytes，SHA-256 `DF34448C39CCEBC0DAE1F1C76006656D11BF00766A0B3A1EDB2A15EA506B4717`，与A完全相同。B.capture 1164 bytes、SHA-256 `DFA3ADD74B9975731F82BFB2F280CAF9D4776F30953A796F76B2DED622EA09C5`。compare.json 523 bytes、SHA-256 `9DAE756D23AE4312A1E306C7E02C6EFF01EED2C7EE66037BB8292AE688008330`，结果SEMANTIC_EQUIVALENT、Differences=[]；A/B进程、candidate、checkpoint、semantic hashes全部正确，A.complete仍匹配A.capture，无terminal，B_CAPTURED_STOP_INPUT SEMANTIC_EQUIVALENT与compare-complete都成立。

实现者另从两个实际snapshot计算SHA-256并以System.Text.Json.Nodes.JsonNode.DeepEquals独立复核，结果相同；没有使用游戏对象或shared mutable graph当expected，也没有放宽比较器。B claim/capture/semantic/compare和9条B event共13份明确公开allowlist记录归档，逐份hash一致，raw checkpoint/path保留private。汇总见[AB-result](../../evidence/spike-001a5/runtime/AB-result-3e6aaa0160e441c293b5ac4a0030b6d0.json)。

**Confirmed：** 固定v0.111.0/41cef1ea、原账号modded1、only A5 runtime-r1、Standard Ironclad A0、seed JFZS2H4V4XN2、Act1普通Monster ENCOUNTER.CORPSE_SLUGS_WEAK入口，在本合同规定首个稳定操作点的完整语义投影（含规定牌堆/有效卡属性/敌人/run-player-monster RNG范围）A/B等价。A直接入房与fresh正常Continue B均来自同一个PreFinishedRoom=null checkpoint，checkpoint Floor1→两端PlayableFloor2成立。

**Unknown/未外推：** 其他seed/角色/房间/版本/模组组合、未投影状态、之后操作轨迹、正式Replay、独立播放会话、native存档隔离、性能及crash recovery。本结论不重分类旧A2/A3/A4失败，不自动解锁SPIKE-001B。已得到包要求的一个有效答案，不追加第3轮或为追求更大范围继续跑。

下一步仅B正常退出与有限收尾；禁止继续游戏。B正常Continue已包含预声明NumReloads等生产写入，B退出后的native不再拿A sealed bytes恒等作为门；保留原private证据，不restore任何文件。

## B正常退出、最终收尾及交付

用户明确报告按保存并退出→主菜单退出正常结束，无牌/EndTurn/奖励/其他动作或异常，并要求收尾交回PM。复核进程=0、compare/A/B semantic原hash不变、没有terminal；29份明确allowlist业务公开文件（9份metadata/snapshot/compare加20条A/B event）与private源逐份hash匹配。加此前active pointer metadata，公开attempt目录共有30份文件；raw checkpoint/path与完整配置backup均保留probe private目录，没有全目录glob导出。

按第7节完成收尾：先确认runtime-r1 installed DLL/manifest精确hash与workspace sealed pair相同、workspace可恢复；对当前已确认原A5账号的settings做create-new完整private备份，范围仅mod_list：A5改disabled，保留全部原记录与其他值，补齐所有当前installed id/source的显式disabled记录，其他nonprobe本就disabled。非mod canonical值和mod_settings其他字段不变；modded selector未写。配置后原账号有效enabled集合为空。

在游戏关闭、目标绝对路径位于明确game mods目录且hash再次匹配时，只Remove-Item两个精确安装文件SPIKE001A5.dll/json，无递归删除；18个其他旧probe安装文件长度/hash不变。旧r2与本runtime-r1 active pointer、private checkpoint及workspace全部候选/源码不删除或改写。脚本：[Closeout-runtime-r1-2026-10-22.ps1](../../evidence/spike-001a5/runtime/Closeout-runtime-r1-2026-10-22.ps1)；收据：[closeout-runtime-r1-2026-10-22.json](../../evidence/spike-001a5/runtime/closeout-runtime-r1-2026-10-22.json)。收尾未读取当前native run或任何个人unmodded profile/另一账号payload，也没有恢复文件或启动游戏。

原r1交付24份、原r2交付331份、runtime-r1 build交付326份逐份长度/hash复核通过。新公开runtime JSON检查未发现NativePath/NetId/SteamId/AccountId原始字段。原r1/r2报告、草案、源码和索引指向文件保持原hash；runtime结果不回写旧报告。当前runtime报告和本次交付索引单独交回PM维护权威状态。

**下次启动配置事实：** 原测试账号mods_enabled全局值保留，所有当前installed local/Workshop id/source明确disabled，A5文件不存在；按当前配置预计无有效模组并进入unmodded存档树。没有为验证此预测再启动游戏，没有读取该树的selector/profile1，也不宣称已验证个人槽安全。下一次项目实验须按其包先建立有效modded配置。本次不启用其他模组或退役probe，不改另一份账号配置、不要求删除其目录。

本包已得到要求的一个实际有效语义答案，停止追加业务。将报告、build/attempt/退出/收尾索引交PM验收；SPIKE-001B及正式产品架构继续由PM决策。
