# CONTENT-006：持续录制、Continue、战斗列表与召援

2026-10-30。D-134同工程实施中。当前只交付A/B/C首个候选，不把构建/纯合同称为原生运行通过；D三鼠召援仍待实现。本包未完成。

## build-001：A/B/C首个可测增量，尚未游戏实测

移除正常录制的fresh/一次attempt/首排限制，格式9支持五角色A0首幕普通Monster地图房间逐场录制。每个原生`EnterMapCoordInternal`建立独立生命周期：正常旅行只接收退出旧房间后、创建新房间前的`ToSave(null)`；继续加载则使用`SetUpSavedSingleplayer`真实传入的SerializableRun，不假定saveGame:false会触发保存。等待该次entry Task及本场玩家开场Task成功、可操作且初始选择结束后才捕获Initial。非普通、已完成重进和范围外房间跳过，后续受支持房间可再次开始；不改正常房间/奖励逻辑。

胜利仍自动封存并生产精确读回；正常领奖后的牌组/遗物/药水/HP/金钱/RNG由下一场实际checkpoint承接，尚待实际多场核对。每场结束清理操作/选择/球身份、observer任务引用及STATE记录缓存，不保留上一场的录制任务。保留256操作和4MiB上限，命中明确报本场未封存，旧成功文件不覆盖。

格式9新增Battle元数据：Replay随机RunId、局内受支持战斗顺序、坐标新attempt、act/floor、原生start_time、map/continue-known/continue-unmatched入口、victory/interrupted。旧格式1–8无此字段，省略null保证旧载荷字节一致；仍只允许已有显式录制build，另精确兼容CONTENT-005/011。旧片列表显示未知归属，不补造运行关系。

Replay自有`recording-journal.json`记录新局GUID与实际观察到的存档指纹；Continue跨进程以完整存档的规范化内容匹配，忽略save_time/run_time/num_reloads三项正常变化，保留start_time、资源、历史/RNG等。新局总分配新GUID，不用seed/角色/坐标猜组；两个明确不同新局发生完全相同指纹碰撞时标ambiguous，Continue新建未匹配组，避免串局。没有过去的实际存档证据则continue-unmatched，不声称与旧片同局。普通存档不写Replay标签。

Continue重进未完成普通战斗时，验证传入存档PreFinishedRoom为空且最新坐标正确，使用其原生房间前起点开启新attempt；不串退出前操作。有已完成房间状态/缺少安全起点时跳过当前段，下一普通战斗恢复。是否所有实际保存形态规范化后均能匹配、以及重进同房间的开场数据/RNG一致，当前均待原生证据。

保存退出只封存已完成操作前缀及其完整选择。正在执行/待选的下一动作不强行完成、不回答选择，不写进文件；格式9允许没有EndTurn的真实中断前缀。尚无完整Initial或零完整操作时只记录中断诊断，不产生假胜利或不可播放零操作片。故中断文件不等于保存了退出瞬间的全部操作。

主菜单保留最近快捷打开，增加按RunId分组的逐场列表，显示场次/层数/attempt、角色、遭遇、时间和胜利/片段；每个条目直接使用自身checkpoint。旧固定样本和准备入口折叠到开发区域。目录扫描仅在菜单附着时执行；旧片首次读取建立轻量catalog，后续读取元数据，mtime变化或损坏元数据回读原片。坏片逐项报错/禁用，不妨碍好片。打开时仍执行完整生产完整性/版本/环境检查，catalog不是权威恢复数据。D-131/D-133与观看隔离/退出路径保持。

## 构建与有限验证

主工程和DirectContracts均0警告0错误。`evidence/content-006/checks/increment-abc/library.json`含5组直接合同：跨实例实际存档/可变时间字段识别；三场顺序及同房新attempt；重复seed新局/完全指纹歧义与资源不匹配不串组；后续层无EndTurn中断片生产保存/精确读回及假胜利拒绝；坏片隔离和好片元数据缓存复用。这些执行真正journal/Codec/ClipStore/library代码，但不执行游戏Recorder、Harmony入口、选择退出、房间创建或节点恢复，不当作原生生命周期通过。

已有工具箱开场政策7组通过，既有首次Osty格式8原片经生产读取/显式兼容通过，原文件未改；输出同目录`opening.json`/`first-osty-read.json`。没有重跑未变的旧整场矩阵，没有实际游戏运行结论。

用户本批确认Cloud关闭、无新进度。game0/原账号核对后，沿原完整恢复点补存已知51文件、3,953,223 bytes至`CONTENT006-pre-abc-current-20261030-194038`，逐hash读回；收据`evidence/content-006/pre-abc-current-files.json`。这是已知文件补存，不是新的完整账号快照；未扫描history/另一账号、未restore。

CONTENT-006/build-001已安装，DLL SHA256 `061EC2212B4E0A6B30D7153FA6FCBD8BC89E1D887935EE1823128BF46F572593`，manifest `69185E5024D560BC5C9B81FC64CC2F732745BCFBA31E64F069CAEF6AAEABE8FB`，收据`evidence/content-006/preparation/build-001-61568ba9.json`。保持仅ReplayMod/modded1，旧CONTENT-005/011 pair/config由安装脚本保留；没有启动游戏。任务间编号分开，不以本包001覆盖旧005/011历史。

## 下一次人工动作及未完成项

先以正常A0新局验证A/B/C，不开旧定向准备：至少三场普通战斗，第一、第二场正常领奖，第二场后保存退出并正常退出进程；重新启动Continue，进行第三场（至少两场首排后），第三场胜利后正常退出。下一自然停点查三份独立文件、奖励承接及Continue分组，再从列表验证较早/较晚独立原生播放、Back/胜利/Stop和切片隔离。出现入口/录制错误立即保留日志同包修，不追加五角色全矩阵。

D已定位固定原生`TwoTailedRatsNormal`：三只TwoTailedRat、五个位置，原生CALL_FOR_BACKUP及等待/数量/空slot约束。尚未修改其业务、spawn白名单或一般敌方roster恢复；当前build-001遇到敌方新生仍明确拒绝该段，不称完整支持。后续同包扩稳定CombatId/未来分配器、有序roster、同模型目标、场景/订阅和已访问点恢复，再取真实召援/跨生成Back/原生再召援/新怪目标及击杀证据。文件事实、运行比较、观看memento和任意文件冷恢复将分别报告。

当前仅源码/文件/纯合同和安装证明，A/B/C原生实测与D均Pending；CONTINUE新attempt、选择中退出及目录坏片均未实际游戏验证。精英/Boss/事件战斗、跨幕/难度、失败局、孵化/复活/多阶段、跨房间Seek/奖励语义播放/整局路线图、通用文件权威恢复仍非本包承诺。
