# STATE-001 续接记录

最终停点：用户确认build-003测试通过且已退出，r4已完整核对并归档。Runtimef8f18b96…/PID43824跨录制PID10832；158事件，2次冷开直达1/14、3次Energy/Block扰动检出并重新读文件纠偏（Play自动继续、Step保持暂停），真实胜利Stop/重开/中途Stop；20forward/4内存restore全匹配，无意外失败，两次原manager/所比较Continue/progress保持，退出后52个modded1存档SHA与STATE001恢复点一致，game0，原clip/state字节未变。报告、README、runtime-r4/verification-r4已更新，原始session私有归档STATE001恢复点runtime/state-001-r4。当前DLL仍4283890CF7FC01DEE08FAAD8FA46F2263F9CF4438F91BED5A09A3E9BDE76A92D，无待实施/待测试项；提交PM固定首点验收，不自动扩下一能力。下面是各阶段历史。

最新覆盖：build-002/r3 Runtime6015f194…两次冷入口仍失败，明确node12 ModifierModel[]未登记（原文件空数组，原生FromSerializable为List），均initial匹配后停0、0prefix、退出原manager/所比较Continue/progress保持；game0且modded1存档文件无变化。已归档r3失败并修build-003（4283890CF7FC01DEE08FAAD8FA46F2263F9CF4438F91BED5A09A3E9BDE76A92D），只从本地已知元素类型形成托管数组登记，不加载任意类型/构造模型；新增用例修复前复现同错误、修复后9项检查通过，Release及原文件读取通过。已安装，旧C031 pair在STATE001恢复点preparations/build-003-aa508491，原clip/state/expected/hash未改。当前等待原片冷入口重测，尚无任何修复版冷恢复/纠偏成功证据。下面保留历史。

最新：用户首次冷入口失败，r2/b414550e…停在0，原因state-initial-binding-shape-mismatch；初态已匹配、0前缀、失败退出比较原Continue/progress保持，现场game0且所有modded1存档文件仍与STATE001恢复点一致。已归档失败并同任务修复为build-002（C031E20C52FBF87DFD2DDB51380CE22FB52A4A97A5D866B9D9ECC0FC1D3614E5），从根/实体及牌逻辑ID、deck顺序到字段/字典键绑定，不再假设分配/别名拓扑相同；旧v1文件/期望/hash原样，字段/容器/RNG实际读回验证。8项纯数据回归、Release和原片精确前驱build读取通过，尚未有修复版游戏冷恢复。已安装game0，旧FCE pair保存在STATE001恢复点preparations/build-002-adeb0082。当前停点请求用户直接沿原片冷入口重测及扰动/胜利/Stop重开，不重录。下面为此前历史记录。

恢复后进展：STATE-001/build-001（DLL `FCE1E395BB83D5518F1B08B57FA5C09AFD46939A7D7C32A802C186918463CF10`）已真实新录clip604d842f…14步胜利，第一步未升级Defend，位置1侧文件374480 bytes/409节点/18RNG，真实文件校验通过；Runtime779ca0d…/PID10832，24事件无失败，game0。当前停点为请求用户新启动，STATE落盘入口直接1/14，启用扰动后Play到胜利Stop、重开Step/Stop并退出；冷恢复/纠偏尚无运行证据。新增正常进度完整恢复点 `C:/Projects/STS2ReplayMod-local/backups/STATE001-20261027-122340-b30c90322d/`（1278文件全部hash读回），原clip/state/session私有归档在其runtime/state-001-r1。报告和evidence已更新，未换DLL。此前Release/7项纯数据/6项memento/旧17步文件兼容检查通过；旧D02 pair仍在DEMO005恢复点preparations/build-001-5448a922。下面保留此前暂停时的分析历史，未实施的候选表述不再代表当前代码。

2026-10-27。用户曾明确“暂停一下可以吗，我要关电脑睡觉了”，期间暂停。PM现转达用户“我醒了。继续吧”，恢复本包实施；下述为暂停时的历史停点，尚不代表实现或测试结论。

当前可用安装仍为已验收DEMO-005/build-003，DLL `D02C7335EB39C0338C85CF2556687ABA0A1F4D35841FF944D4752EFEC61B1130`。最新完整恢复点仍`C:/Projects/STS2ReplayMod-local/backups/DEMO005-20261027-013423-1f0ea24e59/`。STATE-001本轮只有工作包/源码阅读，未写实现代码、构建、安装或运行游戏；本记录不是候选交付或运行结论。

已读CURRENT_STATUS、handoffs/STATE-001.md、现有Recorder/PlayerLoop/DirectCheckpoint/IdentityMemento/MemorySession，以及technical-notes/NATIVE-ANIMATION-TIMING.md。固定反编译缓存与游戏SHA沿当前项目，无需重做旧矩阵。

待实施的最小闭环仍是：正常录制一个非零稳定点落盘→录制进程退出后新启动→独立初始房间中零前缀应用文件状态→两独立逻辑量扰动被检出→重新读/独立解码文件恢复并校验→原Play继续到胜利/Stop/重开。旧After投影、观看期memento或原生SerializableRun均不能冒充权威中途状态。

初步倾向选首回合首张Defend完成后，缩小冷恢复边界；尚未锁定实现。讨论中的方案是纯数据引用图/逻辑绑定表：在初始上下文建立稳定根/牌ID/实体ID及数据子图绑定，正常录制冻结字段/容器/RNG与新增纯历史对象；新上下文重新绑定，排除Task/Tween/委托/信号和旧进程引用。需要审慎验证类型白名单、readonly历史字段、初始图稳定性、损坏数据拒绝及独立文件解码。也可按后续源码证据选择更窄DTO方案，不把这项分析当已实现架构。

已定位CombatHistory位于`MegaCrit.Sts2.Core.Combat.History/`，事件在`.Entries/`。首张Defend涉及EnergySpent、CardPlayStarted/Finished、BlockGained；CardPlay含Card/Player/Target/Resources等纯状态引用，历史基类还持有readonly玩家回合字典/Actor/History。CombatTurnState.Id是当前战斗生命周期ID，应保留/重绑定当前上下文，不能用文件中的旧生命周期编号覆盖。相关RNG需保存完整状态。当前DirectCheckpoint的数据/UI恢复可重构复用，但磁盘应用不得取其旧memento值。

恢复后先继续源码选择和实现，再做必要纯数据/坏payload检查、备份/安装及最短人工新录/新进程验证。当前没有STATE-001状态文件或可测试版本，管理文档由PM维护。
