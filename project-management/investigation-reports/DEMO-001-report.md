# DEMO-001 单房间录制与独立播放交付

2026-10-26，D-093。Implementation完成首个可用闭环，交Project Lead验收。用户总体反馈“测试结果符合预期”；真实日志确认同一新录片段两次打开并完整播放，初态及每个操作后状态均一致，Stop后恢复主菜单及原存档上下文。本轮不追加测试平台或扩大内容范围。

## 实际可用部分

源码 `src/ReplayMod/`；Mod ID/assembly ReplayMod，SemVer0.1.0、affects_gameplay=true。支持新局首场普通Monster、单人Ironclad A0、未升级初始10卡，自动开始录制，无需F8。记录来自本轮正常游戏的房间前ToSave、实际手牌/目标及原action/effect/StartTurn Tasks成功与playable边界，文件不含硬编码Defend轨迹。

主菜单右侧Replay入口显示最近片段的标识、时间和支持范围，从磁盘读取。打开后默认0步暂停；播放/暂停、Step和Stop按完整玩家语义操作边界工作，EndTurn包括敌方行动及下一回合抽牌。观看输入被阻止混入，不会反录自身。Stop返回主菜单，原文件可再次打开。

本轮实际片段 `6826d8ff0438476093386f7c3cc635a1`，seed `PERF001R1A`、华美发束、坐标row1/col1、`ENCOUNTER.NIBBITS_WEAK`。录下3个操作：手牌index1的Strike，目标index0 / MONSTER.NIBBIT / CombatId1；随后手牌index0的Defend；最后EndTurn。129735 bytes文件的envelope数据校验独立重算匹配，完整文件SHA256 `0B25C975FCD05E969465264EEE3C2E87D1233E3B18A884DC5C74C0E83AFC0128`。初态及三步后的投影都在文件内，不依赖旧fixture或内存操作计划。

会话 `a109a82817ad4422849ad1bdb2555276`（Asia/Shanghai）：

| 实际阶段 | 时间 | 结果 |
|---|---|---|
| 自动录制及封存 | 19:44:03–19:44:20 | 3操作，封存8.8586ms，原始投影hash与文件匹配 |
| 第一次打开并播放 | 19:45:03–19:45:32 | 初态＋3操作全部匹配；Stop后原manager及Continue保护检查通过 |
| 第二次重新打开并播放 | 19:45:55–19:46:05 | 再次从初态匹配，3操作全部匹配；Stop后同样恢复 |

状态比较实际覆盖玩家HP/maxHP/block/gold/energy/stars/turn/phase/powers/遗物/药水，初始牌组与hand/draw/discard/exhaust顺序及持久卡牌属性，敌人HP/block/powers/意图/移动状态，以及run/player/enemy RNG。

| 稳定点 | 玩家HP / block / energy / turn | 敌人HP |
|---|---|---:|
| 初态 | 80 / 0 / 3 / 1 | 44 |
| Strike后 | 80 / 0 / 2 / 1 | 38 |
| Defend后 | 80 / 5 / 1 / 1 | 38 |
| EndTurn及抽牌后 | 73 / 0 / 3 / 2 | 38 |

两次真实播放都比较完整投影，以上仅为可读摘录。无state-difference、observer-error、playback-failed或teardown-failed记录。实际loaded仅ReplayMod:0.1.0、profile1；游戏managed runtime实际为9.0.7。

## Continue隔离与证据边界

回放从原SaveManager序列化profile/settings/prefs/progress到独立内存store，使用新的SaveManager临时单例。从本片段SerializableRun重建房间，正常调用游戏出牌/EndTurn逻辑，关闭ShouldSave及原生CombatReplayWriter。退出顺序为保存等待→CleanUp→再次等待→恢复原SaveManager→重建主菜单。

本轮两次playback-closed均报告OriginalManagerRestored=true、NormalContinueAndOriginalProgressUnchanged=true：核对正常Continue主文件与其backup的hash，以及原manager进度序列化值。没有复制片段覆盖native save，也没有恢复备份冒充隔离通过。这仅证明本轮被比较的正常Continue文件和原manager进度，不代表全部个人存档/Cloud的隔离矩阵。本轮按要求未实际点击正常Continue恢复游戏。

用户总体确认了测试体验，但控件手势未单独写入日志；日志独立确认的是两次完整3操作播放与退出。实际只有一个进程会话，没有跨进程重启证据；两次都播放至末尾，没有实际中途Stop样本。代码每次打开均从磁盘读取并重建，离线游标合同验证过中途Stop语义；这些实现/离线检查不写成对应真实游戏场景已验。按工作包闭环跑通后收束，不为这些边界追加一轮测试。

## 版本、备份与当前安装

build-001封存在 `evidence/demo-001/builds/build-001/`。DLL SHA256 `8ED2AD3FD6B6BCC3E67D425601A29867978FDE6DF8AD453AF59E0D3ED6C5B22E`，manifest `69185E5024D560BC5C9B81FC64CC2F732745BCFBA31E64F069CAEF6AAEABE8FB`，固定game SHA `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`。Release 0 warning/error，六项相关离线核心合同通过。最终源hash和实际安装pair与实测封存版本一致，没有为收尾重build或重跑未变测试，没有改旧实验来源/证据。

当前游戏进程0，ReplayMod仍安装且是唯一enabled mod，modded profile1，作为可用原型保留；没有卸载可用demo或替换成未测二进制。用户可在mod列表关闭ReplayMod停止录制，clips保留。默认不整包恢复旧存档；正常最新测试存档保留，未修改游戏本体。

本轮另建完整本地恢复点 `C:/Projects/STS2ReplayMod-local/backups/DEMO001-20261026-180851-5ee9771d51/`，1268文件/131969299 bytes逐文件读回匹配、源复制期间稳定；含PERF结束后的状态和精确私有恢复说明。另一账号未读，Cloud沿用户本批关闭确认、未自动修改，未执行恢复。用户随后若有新游玩进度须另备份；本地备份不保证云状态。

公开运行证据只保存本包事件、去掉原生checkpoint的片段摘要和必要战斗投影：`evidence/demo-001/runtime/a109a82817ad4422849ad1bdb2555276/`。完整clip（含47876 bytes原生checkpoint和本地标识）及完整game日志只在私有恢复目录runtime子目录保留，不公开原始账号/存档payload。

## 未支持与下一步

目前只展示“最近片段”，实验格式1限定精确game/mod构建和scope；最多32操作，必须有一次EndTurn，录制中尚未封存的数据在崩溃时可能丢失。不支持复杂选择/生成牌、药水、奖励/胜负、下一房间、多人、跨版本、分享导入、任意动画冻结、后退或正式录制可靠性。单次封存耗时不是性能发布结论。

按D-093交回当前可用闭环，下一轮优先最小后退：重建本房间并重演到前一个操作边界，复用现有文件、会话和状态检查；不先扩建完整时间轴、测量平台或泛化格式。使用、构建、安装及恢复步骤见 `src/ReplayMod/README.md`。