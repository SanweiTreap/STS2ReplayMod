# 当前项目状态

更新：2026-10-27。当前决策D-104，开发权限沿用D-092；开始工作先读本页，不必重读全部历史。

## 现在做什么

DEMO-001首个可用单房间闭环已验收：自动录下本轮Strike→Defend→EndTurn，从主菜单读文件并两次播放，初态/每步状态与RNG匹配，退出恢复原manager及所比较Continue/进度。结论ACCEPT_SINGLE_ROOM_CORE/CLOSED_WITH_LIMITATIONS；不是完整MVP。PERF已关闭，不再扩建测量设施。

DEMO-002/003/004/005及[STATE-001最小闭环](handoffs/STATE-001.md)均按固定样本验收关闭。STATE-001为ACCEPT_FIRST_DEFEND_FILE_RESTORE_AND_CORRECTION_FIXED_SAMPLE/CLOSED_WITH_LIMITATIONS：正常录制点1已在新进程零前缀恢复，并检出扰动后从文件纠偏/继续。原Implementation交回，当前无新包执行，不自动扩下一能力。

当前可用STATE-001/build-003（4283890C…76A92D）保留，PM核对sealed/installed一致。r4的158事件有2次文件直达1/14、5次独立应用（2冷恢复＋3纠偏），400绑定/35锚点在不同初态拓扑下匹配，历史11项/18RNG无重复。3次Energy/Block差异经私有原日志独立逐叶核对，只涉及两量的4个镜像路径；重新读文件恢复，不用memento/改expected，Play与Step保持。[验收](../evidence/state-001/project-lead-review-2026-10-27.md)。

r2为初态拓扑形状门失败，具体首差异节点未记录，不认定某字段就是实际根因；r3（Runtime6015f194f6cc4a9a8b46952903190e99/PID44112，11事件）两次均拒绝原文件node12的空ModifierModel[]。固定原生读存档将其表示为List，具体数组类型不必出现在新初态类型表，旧登记遗漏纯容器表示。两轮均initial匹配后应用前停0/0前缀、无扰动，退出原manager/所比较Continue/进度保持；失败保留，冷入口1→0仍不可用。

真实录像604d842f5c944fe781939b2d36b94624，实际HRQWK15DD6CE/col2双蛞蝓、14步胜利，首操作未升级Defend把能量3→2/格挡0→5。录制Runtime779ca0d36e714f6f84ca45f923e7f644、PID10832，正常退出；sidecar374480 bytes/409节点（400绑定+9新历史/共享数据）/18份RNG实际落盘。独立纯.NET文件读回/关联/历史引用/hash检查通过，不执行游戏冷恢复；见[文件检查](../evidence/state-001/recording-files-r1.json)。初态绑定约18.61ms/冻结1.55ms等仅一次路径观察，不认发布性能。

录制PID10832/startUTC04:19:59.2007908、观看PID43824/startUTC05:07:00.2624573不同；无Sequence1/前缀，20后续动作/4内存后退匹配。首场14胜利Stop，第二次重开/Step/纠偏后busy Stop到4，两次原manager/所比较Continue/进度保持。日志FileSHA256实际为payload570CDB…，物理state byte SHA为51D8B9…，原文件/expected不变，不混用。旧失败保留。

格式3原组合录像269735cd78054ec7b5002fd28928229a真实封存17步胜利，含燃烧、耸肩无视+、能量药水玩家目标/消耗/历史、第三回合小花2→0/加能量。修复后r2/r3两Runtime共298事件、36 forward/18 restore匹配，药水重做4次/花触发8次，槽位/计数/能量/显示读回通过。r2完整到17胜利Stop，r3新Runtime能力撤销重做短测到1 Stop；两个退出原manager及所比较Continue/进度保持。见[验收](../evidence/demo-005/project-lead-review-2026-10-27.md)，不认全内容/逐OS PID链。

旧r1的381事件/两次10→9异常保留，不追认成功。build-003同任务修RefreshUi误处理退场死亡节点，修复后10→9七次通过，四次清理真实残留节点/淡出，0前缀/无room重载且不等装饰动画。r1局部5→4升级抽牌撤销沿原证据，修复版未单列重复未变输入路径；所选显示值/体验不等于全部视觉/隐藏状态完备。

已验收DEMO-004/build-001（ECC7F251…D97987）的pair在替换前保留。定向双蛞蝓11步胜利真实封存；42 forward/31 restore匹配，7次5→4恢复同一Id1/顺序并回滚Id2强化/意图，重做与剩余目标正确，0前缀/无房间重载。两次Stop原manager/所比较Continue/进度保持。见[验收](../evidence/demo-004/project-lead-review-2026-10-27.md)，不把旧结果当新候选通过。

DEMO-003/build-002已实际解决多余死亡动画等待，用户确认；旧10步录像完整播放，又新录9步Beetle胜利并自动封存/完整播放/重开。PM核对runtime-r2的152事件：3次open、34次forward、9次restore、2个胜利终点；三个退出均无Error、原manager及所比较Continue/进度保持。胜利观察到逻辑ready约5–15ms且死亡动画仍待播放，不是端到端发布延迟。早期Sludge Spinner未封存失败保留，不追认为通过。

DEMO-003/build-003已实测修复Shrink回退体型和手牌入场：同一9步文件3次完整播放/退出、47次forward和20次直接恢复匹配，20次缩放/手牌布局读回通过，用户体验确认。结论ACCEPT_COMPLETE_FIRST_COMBAT_FIXED_SAMPLE/CLOSED_WITH_LIMITATIONS，见[验收](../evidence/demo-003/project-lead-review-2026-10-26.md)，旧pair保留。所选显示值和用户体验不是任意场景/像素级恢复。

最新完整恢复点按收据为 `C:/Projects/STS2ReplayMod-local/backups/STATE001-20261027-122340-b30c90322d/`，第一段正常新增进度后建立，1278文件/132037841 bytes来源/副本读回匹配；原clip/state/session私有归档其runtime/state-001-r1。旧D02可用pair及DEMO005/004所有恢复点、原录像/失败保留，无整存档恢复/换DLL/本体改动。PM只核对进展报告/文件检查摘要，不重复整包审计、构建/测试或读取当前原生存档。

## 最新用户授权

STATE-001安装按收据复用DEMO005-20261027-013423-1f0ea24e59：51个profile1 saves仅progress.save.backup不同，差异副本单独读回保存；旧D02 pair/settings/selector存preparations/build-001-5448a922。安装时game0/固定本体/原账号/modded1仅ReplayMod核对通过，无自动游戏操作或整存档恢复。PM本次只核对进展报告，不重复构建/测试/全账号审计。

用户认为旧存档保护过严，允许存档备份后做更大胆的mod/hook实验，接受测试失败、游戏文件重装/验证及手动恢复；unmodded profile1不再禁止读取/复制。默认使用modded1，另一账号不纳入。备份和恢复办法见[TEST_PROTOCOL](TEST_PROTOCOL.md)。

用户先前报告modded侧profile1、所有mod关闭，仅为口述准备信息，不代表今日已现场核对。执行安装时确认游戏正常退出；实际游戏仍由用户手动启动/操作，不用Computer Use或替代GUI自动化。

## 已经确认

- A5：一个房间前checkpoint的初始重建A/B相同。
- 001B：一个Defend＋EndTurn轨迹的三稳定点相同。
- DET-001：一个真实首次自然重洗样本，四操作/五稳定点及实际RNG状态重放一致；已收尾关闭，不再重复运行。
- 部分独立会话写入隔离、受控异常退出/正常重启有局部证据，不等于完整产品会话已完成。
- PERF：同build-006真实C1/C0/C2各一轮，四场景资格点一致、实际事件链完整；C2五条/1636 bytes完整写出且writer停止。首次已计时阶段约4.97ms，后四次约0.033–0.063ms；只确认最小路径可运行，不认稳定增量或G-02。
- DEMO-001：真实新录三操作文件，两次同会话独立打开、各初态＋3步匹配并退出；有主菜单入口与播放控制。暂停/Step手势由用户总体确认，未逐次日志化；跨进程重开/实际中途Stop未证。可用源码在 `src/ReplayMod/`。
- DEMO-002直接恢复：同一个旧12操作SEAPUNK文件在新build播放，30次恢复/38次forward全匹配；每Runtime只首次加载一次房间，0前缀，恢复记录12–52ms。实际重做第4/8步EndTurn、到12后退至0并续播、中途Stop均成功。两次Runtime会话不等于完整OS PID链。仅回放期内存checkpoint，After比较JSON不是恢复快照。
- DEMO-003：真实首战胜利自动封存、最后操作/终点完整播放、Stop/重开已验证；build-003实际缩放/手牌修复与20次直接恢复通过。仅固定支持范围，已关闭；报告[DEMO-003](investigation-reports/DEMO-003-report.md)。
- DEMO-004：定向暗港双蛞蝓的中途死亡直接后退/同伴强化和目标恢复、重做及完整胜利/退出有真实证据；42次forward/31次restore含7次跨击杀，固定样本收束。[报告](investigation-reports/DEMO-004-report.md)。
- DEMO-005：12牌组合中的升级非初始牌/能力、小花自动触发计数、能量药水/用药历史录播和所测恢复边界通过；修复后36 forward/18 restore、胜利Stop/新Runtime重开短测。[报告](investigation-reports/DEMO-005-report.md)。
- STATE-001：正常录制首Defend后1点落盘、新进程零前缀文件恢复、三次Energy/Block差异检出/文件纠偏和后续胜利退出通过；仅该首回合单点。[报告](investigation-reports/STATE-001-report.md)。
- 已有本地反编译C#缓存，可直接用来定位和编写hook；普通实现不逐方法要求IL复证。

证据入口：[直接后退验收](../evidence/demo-002/direct-r1/project-lead-review-2026-10-26.md)、[首个闭环](../evidence/demo-001/project-lead-review-2026-10-26.md)、[调查台账](INVESTIGATIONS.md)。

## 仍然不知道

用户新增原生动画/取消/抽牌竞态经验已记录为[技术风险](technical-notes/NATIVE-ANIMATION-TIMING.md)：官方历史修复及本地源码证实相关生命周期/索引/异步依赖，用户具体凋萎互卡根因未复现。后续强记录要保留实际结果与必要因果顺序；DEMO-005已收束，不补该风险复现矩阵。

首回合首Defend点的文件恢复/纠偏已证；更多点/新增能力或用药状态、多回合/跨死亡生成的文件恢复、原生取消/卡死、完整强记录/任意seek仍未证明。冷入口1→0不可用，当前权威入口要求与录制不同进程/Runtime，不认同进程文件纠偏。其他群怪/更多内容/房间、玩家死亡/宠物/终局、任意形态、发布性能及跨版本仍待推进；旧失败不追认，显示读回/体验不等于任意视觉完备。

可演进原型在 `src/ReplayMod/`，实验格式1/2/3有精确身份限制及明确已知前驱build兼容，不是一般跨版本支持。不预先锁最终架构，性能沿真实路径观察。

## 下一段方向

本轮收束，不追加测试/矩阵。下一优先建议在已测组合内选能力或用药后的新关键点，扩权威状态字段/取点和恢复边界；具体目标待用户讨论，未派新包。全部内容/整局仍按[TODO](TODO.md)推进，不把这个单点闭环称通用强记录。

MVP仍要求自动录制、尽可能全单人场景、独立Replay入口、不覆盖玩家正常Continue、双向导航和配置化节奏。多人为MVP后第一优先，跨版本随后；接管不是MVP。详见[MVP](MVP.md)和[TODO](TODO.md)。

开发流程按[WORKFLOW](WORKFLOW.md)：源码优先、备份可恢复、允许相关改动成批迭代、针对真实问题测试。旧失败/原始报告保留，不追认；大段历史状态快照已移至`archive/2026-10-26-before-D092/`，决策沿革见[DECISIONS](DECISIONS.md)。

