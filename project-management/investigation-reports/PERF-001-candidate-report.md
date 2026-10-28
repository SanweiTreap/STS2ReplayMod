# PERF-001 三模式最小短测报告

2026-10-26，D-092。Implementation已完成，交Project Lead验收。

同一build-006的C1→C0→C2真实短测全部Complete，四个状态资格点完全一致。C2五条记录/1636 bytes逐条完整内容、顺序、字节数和SHA256独立复核通过，无拒绝、溢出或丢记录，writer已停止。最小采集/写入路径可运行；本轮单次有序样本不能给出可信的增量性能估计或G-02达标结论，不追加性能测试矩阵。

## 实际结果

同Custom seed `PERF001R1A`、Ironclad A0、无modifier、华美发束、初始10卡，5秒warmup＋10秒map idle。起点直连最左普通Monster为 `ENCOUNTER.NIBBITS_WEAK`，打最左Defend一次、EndTurn一次、敌方与抽牌结束后正常退出，不领奖。三模式实际only PERF001、modded profile1、原账号opaque binding与发布二进制身份一致；用户均报告Complete，收集时进程为0。

| 模式 | run | 存储帧记录 / 同段间隔 | 资格点 / 原Task成功观测 | 候选记录 |
|---|---|---:|---:|---:|
| C1 空候选hook | c1-20261026-r1-07 | 2346 / 2341 | 4 / 7 | 0 |
| C0 共同测量壳 | c0-20261026-r1-01 | 2352 / 2347 | 4 / 7 | 0 |
| C2 实际采集与writer | c2-20261026-r1-01 | 2353 / 2348 | 4 / 7 | 5 |

每轮分5个采样段，相邻engine frame和tick已独立复核。C0/C2的map/combat/card/turn四份SceneFacts逐文件hash与C1完全相同，五个共同语义事件顺序一致。采样、输入接受、原travel/EnterMapCoord/action/effect/StartTurn Task成功加playable及下一回合链完整，未把返回Task对象当完成。

以下均为本轮观测值，单位ms；动作墙钟从输入请求到首次观测成功加playable，包含正常游戏执行/动画，不等同录制器耗时。

| 指标 | C0 | C1 | C2 |
|---|---:|---:|---:|
| MapIdle帧间隔均值（各1200间隔） | 8.3330 | 8.3330 | 8.3330 |
| MapIdle p95 / 最大帧间隔 | 8.3828 / 9.7332 | 8.3753 / 8.7633 | 8.3648 / 8.9195 |
| 地图输入→房间可操作 | 2231.0994 | 2225.1623 | 2238.0275 |
| Defend请求→成功且idle | 178.7997 | 181.0342 | 178.4022 |
| EndTurn请求→下一玩家回合就绪 | 2215.9211 | 2209.5303 | 2217.1267 |
| drain＋独立记录核对 | 0.0190 | 0.0182 | 7.8578 |
| Loading内最大帧间隔 | 96.6735 | 118.6748 | 104.0534 |

C1−C0动作时长分别−5.9371、+2.2345、−6.3908ms；C2−C1分别+12.8652、−2.6320、+7.5964ms。正负混杂且各模式只有一轮，不能据此宣称空hook零成本或录制增加某个百分比。idle均值相近也不能排除帧率上限附近隐藏的CPU成本；本轮没有额外核对渲染设置，未改这些设置。

全部慢帧和CrossBoundary间隔保留。Defend段少于100间隔，只报告raw/range，不给p95/p99。进程CPU、分配、GC边界差值在comparison中：它们含游戏、观察器和其他进程线程，不能归因给候选；例如idle进程分配C0/C1/C2约4.71/7.84/4.63MB，说明单轮差值不能当作稳定录制开销。

## C2直接计时及记录完整性

| 事件 | 采集＋冻结 | 编码 | 入队 | 三阶段之和 | 同步线程分配bytes |
|---|---:|---:|---:|---:|---:|
| MapCommit（首次） | 0.4097 | 4.1766 | 0.3838 | 4.9701 | 159408 |
| CardAccepted | 0.0382 | 0.0158 | 0.0089 | 0.0629 | 2296 |
| CardResolved | 0.0090 | 0.0248 | 0.0077 | 0.0415 | 1880 |
| EndTurnAccepted | 0.0095 | 0.0159 | 0.0076 | 0.0330 | 1784 |
| NextPlayerTurnReady | 0.0099 | 0.0273 | 0.0089 | 0.0461 | 1960 |

已计时三阶段合计5.1536ms，同步线程分配合计167328 bytes；不包含所有hook guard/bookkeeping，不能称完整callback总开销。首次明显高于后四次，未通过额外测量将其归因到JIT或证明稳态。

writer接受5/写入5，队列峰值1条/369 bytes，上限128条/2MiB、单条16KiB；overflow/rejection/aborted均0。5次buffered Write合计0.0188ms，1次Flush 0.0815ms，writer线程分配67816 bytes并正常停止。线程生命周期89676.4233ms包含等待队列/人工等待，不是磁盘忙碌耗时，不与主线程墙钟相加。

独立从expected-events重建每条原始JSON及换行字节，逐条比较完整payload、sequence和kind，再核对1636 bytes和SHA256 `E45736A41F951FEEC2D2A56F5BD0558A5A057E39CD2080980E505B23F51185A2`。这验证本次五条稀疏记录完整，不证明持续写盘压力、长局峰值、断电持久性或正式录制内容已充分。

## 共同测量成本与适用范围

共同资格查询单列在原始动作结束之后。map/combat/card/turn查询耗时：C0 22.1311/5.8036/1.7016/0.8841ms；C1 15.6607/5.4769/1.5789/0.8651ms；C2 14.1350/5.2279/0.8511/0.9076ms。共同壳本身有检查、分配、计数器及输出成本，C0不是零观察基线。

同一SceneTree.ProcessFrame来源、monotonic ticks/engine frame id；仅在显式Segment内生成相邻间隔。主菜单/涅奥/Ready人工等待不存逐帧轨迹，只留时长/回调计数；不跨挂机间隙连接，不声称全会话连续raw FPS。地图准备失焦可保留无效中断窗、同局重计；本次三条成功run均首个idle有效且未污染。真实Loading/CardEffect/EndTurn失焦/暂停仍使本次动作测量失败。

原Task引用只在主线程读取状态，不替换、包装、await/cancel或改变原异常。完成是首次观测上界，有pending观测时才有下界；playable不是所有VFX结束。process间隔不是GPU/present，GC pause不可用。最后真实帧覆盖资格/drain尾部；后续metadata/commit I/O在采样窗外。实际游戏managed runtime版本未单独采集，不把离线SDK版本冒充它。

## 版本、失败保留与收尾

封存pair：`evidence/perf-001/candidate-r1/builds/build-006/`。DLL SHA256 `B378EA7ACAE62957A9868780FCA1360D45BDC4DCE2059E1462E6CFF79A82EE5F`，manifest `598D0A1F5A1BA9D3B792B3C73FA2BBBEC365420829D6717630D4F490B09BB8B6`。固定game SHA `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`，Harmony2.4.2.0。Release 0 warning/error，27项相关合同及13 common/16 C1/C2描述的Harmony同形回归通过；源hash和验证指针在build receipt。不重跑旧23/144、Phase0代理或全量审计。

C1-01..06及build-001..005保留：正常Neow起点误分类、JSON resolver缺口、准备焦点处理、正常地图Vote链误拒、idle失焦、主菜单挂机填满65536帧数组。build-006分段采样并允许同局idle重新准备；主菜单挂机问题是候选设计缺陷。旧10/30部分窗不混入新5/10成功比较，未恢复存档抹掉失败。

原账号完整本地备份1258文件/131932740 bytes，逐文件读回匹配：`C:/Projects/STS2ReplayMod-local/backups/PERF001-20261026-154425-9c3b1c897d/`，含私有恢复说明；源复制期间未变，另一账号未读。用户确认Cloud关闭，本地备份不保证云状态，未自动改Cloud。

全部三轮本包输出已归档，完整game日志仅私有保存。游戏退出后已移除本轮安装的PERF001 DLL/manifest和本包活动configuration（批次前均不存在），仅还原本包mod-list条目；所有mod均disabled，global mods_enabled仍为原来的true。未改其他设置或游戏本体；正常最新Custom战斗存档保留，未整包恢复旧存档。精确清理前文件仍在私有备份，见cleanup-receipt.json。

## 交付与下一步

必要原始结果及逐轮分析在 `evidence/perf-001/candidate-r1/runtime/`；汇总为 `pilot-comparison.json`，入口为同目录README。候选真实最小路径与有界writer已运行，增量/长局/稳态开销仍未知。建议按D-092收束PERF短pilot，下一段推进单房间录制→主菜单独立播放→暂停/单步/退出，再做最小后退，将性能测量嵌入真实产品路径。