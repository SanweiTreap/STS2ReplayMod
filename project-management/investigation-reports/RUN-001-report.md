# RUN-001：整局外壳与第一条非战斗录播链

## 最新摘要 · 2026-10-31 · build-002 已安装，首轮录制待测

- 当前安装RUN-001/build-002，DLL `A0393172DD3254AC336762A544B11B7EAFC7CF2065E2B52B7915CABA35247DAC`，收据`evidence/run-001/preparation/build-002-08a8e213.json`。001在人工运行前修正准备地图的UI生成时点，不是用户实测FAIL；009与001 pair/config及恢复点均保持。无本包原生PASS。
- 用户确认Cloud仍关闭、没有上轮后的新进度。当前正常存档是未完三鼠测试局；新测试前补存，不自动恢复或自行启动游戏。
- 已实现run→visit→operation元数据及阶段保存、普通奖励/选牌与营火休息/升级原生选择记录、同会话路线播放器及下一战初态/RNG比较；目前只有构建/格式检查通过，不当原生链条通过。
- 原生入口已定位：RewardsSetSynchronizer.SelectLocalReward、NCardRewardSelectionScreen.OptionSelected、RestSiteSynchronizer.ChooseLocalOption及NDeckUpgradeSelectScreen、NMapScreen原生选路/TravelToMapCoord。单场播放器现有胜利奖励屏蔽保持；整局模式在观看者推进时才开放原生奖励，避免新奖励等待破坏战斗Back。
- 首个短样本拟默认关闭准备：铁甲A0暗港普通战斗→营火→普通战斗，使用原生牌/遭遇与实际相连地图节点，准备在checkpoint前明确记录。未支持房间留缺口，非战斗Back/跨房间Seek禁用。
- 下一动作：用户启用RUN-001准备、铁甲A0一路走最左可选普通→营火→普通；首战获胜领金币/选牌（若有药水也领取），营火升级痛击，下一战打一张防御+后存退关游戏。先核实际录像/路线/奖励/营火/后场锚点，再给整条路线观看步骤。休息/跳卡与合法保存点新进程Continue及选择中Stop尚待后续短样本，不要求重跑旧50步/角色矩阵。证据本地`evidence/run-001/`。

## 001→002：首个纵向实现，尚无原生运行结论

沿CONTENT-006的真实RunId与Continue观察，独立`runs/<RunId>.json`校验封装保存实际房间访问与语义操作，战斗仍引用原格式10片，不合并成巨型战斗JSON。幕/层/坐标/地图类型与实际RoomType/Model分开，重进增加entry计数、战斗attempt继承原规则，地图/奖励是操作阶段；未支持/嵌套房间留gap/subroom信息，旧片不伪造历史路线。未完战斗Continue另开尝试并留中断边界，菜单能从各个已录战斗起点顺播，不能跨缺口冒充完整整局。房间与步骤保存发生在阶段边界，菜单附着时读取，非每帧解析库。

奖励沿RewardsSetSynchronizer.SelectLocalReward记录实际索引/完整原生奖励身份，NCardRewardSelectionScreen.OptionSelected记录最终候选与决策；营火沿ChooseLocalOption，升级通过原生NDeckUpgradeSelectScreen选择/确认，休息走原生Heal/Hook；地图沿原生NMapScreen.OnMapPointSelectedLocally的投票GameAction及TravelToMapCoord/EnterMapCoord转换。没有按牌/药水/遗物手写资源效果。`RunResources`仅比较原生Players、RNG、Odds、共享遗物池、幕/难度/坐标历史、房间历史及不含saved_map的Act状态，明确不是通用地图/文件权威比较；原生地图省略bool读入差异不靠覆盖expected修补。下一战另用现有完整LiveState初态/RNG投影核对。

整局模式复用PlayerLoop/MemorySession一次安装与统一退出；只有合法第一战锚点载入原生checkpoint。锚点另记录当时原生Progress环境供解锁/奖励池等使用，放在隔离manager，不修改普通Progress；后续地图→营火→战斗继续同一RunState，不载入后场checkpoint、也不注入其expected。跨战斗退出清理旧roster/牌/单位订阅后交给原生房间退出，下一战只校验并复用NativeSeek/原生动作。Stop场景退出先取消原生窗口，再观察/收束未完战外Task，恢复原manager前再次drain；这些新退出路径尚未实测，不提前判隔离通过。

控制沿`>`细阶段、`>>`完成当前语义操作、Auto连续推进，无新Play/Pause。整局的战斗胜利先保留现有Back机会，观众推进后才原生显示奖励；单场打开仍屏蔽奖励、停在胜利。战外由共同门控驱动原生选择窗口、选中与确认，忽略录制时来回犹豫，只演示最终决策。战外Back/跨房间或开场重入Seek明示禁用；单场原有开场Back保持。新控制与奖励等待、地图投票、后战开场及异常清理均待人工，不以代码结构声称已可用。

默认关闭的RunRoute准备仅用于下一新铁甲A0：原生创建两BodySlam并登记Owner/Run，升级原三Defend及两BodySlam，五牌原生RoyallyApproved先天/保留，40HP；暗港前两普通遭遇为CorpseSlugsWeak。在地图交给原生NMapScreen.SetMap创建UI前，选第一排最左普通及它的最左子、孙节点，把同一真实相连链设为Monster→RestSite→Monster，明确日志。001先在GenerateMap结束后改类型，会晚于原生地图节点UI创建，人工运行前002移到SetMap前缀；不改正常未启用准备的地图/Continue/观看，不重掷RNG或播放时改expected。

主工程与DirectContracts构建均0警告0错误；4项RunData文件/归属/中断状态/校验破坏隔离检查通过，见`checks/run-data.json`。实际009旧一步片经原ClipStore显式兼容读回通过，见`checks/legacy-009-read.json`；只验证格式/生产读取，未运行Godot或Native奖励/营火/整链/旧角色回归。没有为新准备创建镜像Owner测试或重跑旧纯模型合同。

用户确认本包Cloud仍关闭、没有新进度；安装前固定原账号/game0、51当前已知文件与`CONTENT006-post-r10-current-20261031-021506`全部hash相同，复用该恢复点，收据`pre-001-recovery-check.json`。001准备build-001-66377d55保留009 pair；002准备build-002-08a8e213保留001 pair，均在本包evidence/run-001内独立seal构建编号，manifest保持原值。没有另扫账号/历史或全账号快照，无自动恢复/Cloud切换/自行正式或headless启动。首轮只先取实际短路线录制，普通bug在本包修，不向PM逐候选请求审批。
