# PERF-001 Phase 0 报告

日期：2026-10-26，Asia/Shanghai  
Implementation 结论：**READY_PERF_MEASUREMENT_CONTRACT**  
状态：离线交付完成，提交 Project Lead 独立验收；正式游戏运行 HOLD，attempt=0。此结论不是游戏性能或 G-02 发布验收。

## 范围与交付

本阶段按 `project-management/handoffs/PERF-001.md` 完成三模式测量合同、最小后续人工 pilot 草案、独立 .NET 9 Release 统计/有界管线代理和原始证据。只写本包源码、证据和本报告；PM 状态文档、DET 及旧 r5/r6 交付不由本包修改。不读取当前账号、配置、profile、存档、Cloud、游戏日志或游戏进程。静态游戏文件仅核对获准固定 DLL 的 SHA/窄 IL；未装载游戏 API 执行、构建/安装游戏候选或要求玩家进行游戏内测试。

主要入口：

- `evidence/perf-001/README.md`：复现与证据导航。
- `evidence/perf-001/measurement-contract.md`：模式、边界、统计、分配和 I/O 完成语义。
- `evidence/perf-001/pilot-plan.md`：下一阶段草案，不授权当前运行。
- `experiments/PERF-001/`：最小源码/测试/CLI 和只读复核脚本，无 NuGet 包/旧 probe 引用。
- `evidence/perf-001/runs/proxy-r1-20261026/`：唯一计时代理的原始 result、9 个 CSV、6 个 NDJSON。
- `evidence/perf-001/phase0-delivery-r1.json`：小型交付 hash 索引；仅本包交付/本报告和 5 个直接公开 DTO，排除 build cache 和旧全部索引，自身不递归入索引。

## Confirmed

### 功能和执行身份

1. SDK 9.0.311，Release 构建 0 warning/error；`build-functional-receipt-r1.json` 绑定 11 个当时 harness 源/配置/入口文件和已执行 DLL SHA256 `D2423592B26286770E85F8D4B29DC08C1304946E7D47A3DBAA35EBC8FE4E27FD`。后加 Verify-Delivery.ps1 是纯交付复核脚本，不改变该已执行程序集。
2. 最后一次功能验证 **23/23**，位于 `validation/functional-r1-final-20261026/result.json`。覆盖 nearest rank、ticks 频率、空窗、少样本、长尾保留、重复/逆序/负时间、无效数字、实际五 DTO 完整编码/冻结、oracle 隔离、实际 writer 正向排空、记录和字节溢出、未 drain、I/O 注入故障、写入预算耗尽、乱序/过大记录和错误 payload/order。
3. 两次功能目录和有意的失败/部分输出均保留。最后一次功能命令工具 wall 0.494 秒，构建 1.592 秒；其耗时不作性能观察。`cli-gates-r1.json` 另证实无人工计时窗确认时拒绝 Bench、不建目录，及 Verify 范围拒绝不写入。
4. 五个公开输入是已封存 DET A.point0..4；输入长度/SHA 在执行前和后均匹配。candidate 与 expected 的可变对象图/字节不共享；源输入保持只读。没有重新验证旧的数千文件/144 项测试。

### 唯一离线计时代理

用户先正常游戏，本包只做代码/功能测试；用户随后明确回复 **“已结束并退出，可以计时”**，才运行计时。没有用游戏进程/账号枚举替代确认。OS/后台竞争未被程序排除。

实测 runtime **.NET 9.0.13 / X64**、24 logical processors、workstation GC（ServerGC=false）/Interactive、Stopwatch 10,000,000 Hz/high resolution、Release=true、DebuggerAttached=false。外层 Bench 工具 wall **2.664 秒**，Verify **1.800 秒**，均<60秒；不是游戏运行。内部编排时钟 1731.4952 ms 包含事后校验/CSV，并非 timed blocks 的纯耗时和。

固定 N=1000/模式/正式块，预热 N=100；三次块顺序 Noop→Memory→Queue、Memory→Queue→Noop、Queue→Noop→Memory。共正式 9000 操作、预热 900 操作；同一进程的 3 个旋转块，不是 3 台机器或 3 个独立游戏实验。Noop 也含共用时钟/增量 SHA/消费操作，不能解释为零测量成本。

正式每块观察/处理的独立 expected 字节均为 **7,610,893**，checksum 均为 `29F4E8B07C12FED03D510CAB01F2B4C4916E1BD75280B4DA17A50F54D68FF07D`。原始计时/mean/p50/p95/p99/max/阈值计数、分配、进程 CPU/各代 GC 均在 result.json；下表保留各块，单位 ms，不合并尾分位：

| rotation | 模式 | producer loop | total completion | 每操作 mean | 每操作 p99 | 每操作 max |
|---:|---|---:|---:|---:|---:|---:|
| 0 | Noop | 2.6392 | 2.6468 | 0.0024571 | 0.0029 | 0.0125 |
| 0 | Memory | 104.1316 | 104.1400 | 0.1039624 | 0.1525 | 0.6499 |
| 0 | Queue | 103.2906 | 104.0394 | 0.1030728 | 0.1728 | 0.4143 |
| 1 | Memory | 41.8360 | 41.8469 | 0.0416646 | 0.0782 | 0.1820 |
| 1 | Queue | 40.4505 | 41.5919 | 0.0402331 | 0.0744 | 0.2377 |
| 1 | Noop | 2.4457 | 2.4486 | 0.0022698 | 0.0026 | 0.0076 |
| 2 | Queue | 38.6286 | 39.4134 | 0.0384192 | 0.0724 | 0.2816 |
| 2 | Noop | 2.6189 | 2.6233 | 0.0024362 | 0.0030 | 0.0134 |
| 2 | Memory | 37.9804 | 37.9915 | 0.0378138 | 0.0762 | 0.2115 |

Memory 主线程每个 1000 操作块分配 15,351,792 bytes；Queue 为 15,351,848 / 15,351,936 / 15,351,848 bytes，writer 每块另有 66,016 bytes（含文件 buffer 和线程内工作）。Noop producer loop 的线程分配计数为 0，但共同数组/摘要器在窗外分配、进程窗内另有分配，绝不代表整个工具或未来游戏零分配。进程 GC 实际计数保留；CPU counter 的短窗 0 是观测粒度下的读数，不证明零 CPU。GC pause、单线程 CPU、GPU/呈现/显存未取得，Unavailable。

Queue 正式三块的 writer write wall 合计分别 5.2697 / 5.0561 / 4.5408 ms，显式 flush 0.0097 / 0.0080 / 0.0082 ms；writer 总 wall 103.7342 / 41.0964 / 39.0844 ms 包含等待 producer，不能与 producer 或 total 相加。队列峰值记录 1 / 5 / 2，峰值字节 7,627 / 38,055 / 15,254；只是本输入/调度下观测，不是未来峰值保证。total 减 producer 还包含 writer 启动、hash 收尾等，不能称为独立 drain 耗时。

### 完整性与 I/O

- 预热/正式共 6 个排队块，accepted=written=**3300 records / 25,115,655 bytes**；3300 次成功返回的 Write、6 次显式 Flush，每块 completion requested、writer stopped、Complete=true，overflow/rejection=0。
- 上限是 128 queued records 且 queued bytes≤2 MiB、单条≤16 KiB；另外可各有一个 writer in-flight/producer 记录。拒绝是粘性失败，无静默成功丢弃、无阻塞背压。负向测试证明溢出后即使已接收部分后来排空也不能通过。
- 每个 NDJSON 逐条校验 sequence、input index、完整原始 payload，加文件长度和独立 expected SHA；不只验证能 parse 或 enqueue 快。文件/字节全齐且 writer 已 drain 后才完成。
- 只读 Verify 实际通过：18 blocks、6 files、9000 个正式 CSV 行重算统计，五输入仍匹配；Writes=0、额外计时 run=0。收据 `proxy-execution-receipt-r1.json` 绑定 result SHA256 `63A172727107AFAB99047694C5FF1AA3918D363FC18AEFF02F6E328D459A3E0F`。
- Write/Flush 是托管缓冲 API 的成功调用，不是物理 syscall、durable fsync 或断电安全保证。正式/预热 NDJSON 共约 23.95 MiB，本包全部交付保持<64 MiB，最后字节清单以 delivery index 为据。

### 未来测量方案的静态依据

固定 sts2.dll 9757184 bytes / SHA256 `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9` / MVID `73b63ee0-6c0a-47bb-b0d1-b21f6d94222e`，只做 metadata/窄 IL。`seed-flow-il-v2.txt` 显示 Standard.SeedChanged 抛异常，Custom 的正常 seed 输入→Lobby.SetSeed，Custom 正常新局调用传 shouldSave=true。故草案用同一个**新 Custom seed、无 modifier**三模式新建，不承诺 Standard UI 能改 seed，不恢复旧档或关闭正常存盘。

## Inferred

1. 统计器/完整性和后半段代理满足本阶段验收；合同能区分共同测量壳、额外空 hook 和候选记录，C0 始终保留同一个新模组/modded 上下文。C0 不是未插桩游戏；条件差值含缓存、GC、调度等交互，不作独立 p99 相减或 wall 相加。
2. 本次早期 Memory producer 104.13 ms、后两块 41.84/37.98 ms，Queue 也从 103.29 ms 降到 40.45/38.63 ms。即使每块做了固定预热，**不能宣称稳态已建立**。可能涉及 JIT/tiering、缓存、GC或调度，现有证据不能区分，不追加优化/跑到满意为止。
3. 这些数值只给约 7.6 KB 公开 JsonElement envelope 的编码/拥有字节/队列线索。候选最值得正式观察的缺口是**游戏同步线程 live 采集/冻结与地图加载、卡牌结算/GC 重叠时的间隔和分配**；本包根本没有该路径，不凭代理快慢选正式架构。
4. 草案下一阶段首轮 3 模式各一新进程/一局，C1→C0→C2；共同 10 秒预热+30 秒 map idle，一次首排普通 Monster、一次实际 Defend、一次 EndTurn，然后收齐指标/drain并正常保存退出。合计 3 次进房、6 个战斗动作；覆盖加载和敌方/抽牌，**不刻意覆盖耗尽后的自然重洗**。同内容、当前手牌/目标资格不符就停止，不 roll、不引用旧坐标。

## Unknown / 后续门

- 今日真实版本、实际 loaded set、Steam Cloud、账号/profile、Custom UI 解锁/回显、同 seed 在本轮进度下是否保持同一 Neow/手牌/敌人；本离线包没有读取或现场验证，不能用旧收尾收据替代。下一阶段逐项门禁，不满足交 PM。
- 未来 CommonBoundary/Candidate 的精确生产 patch 方法、effect 成功/idle 边界及 Process 回调首帧，须下一阶段构建前窄 IL 和之后受控 runtime 验证。当前未构建新模组，不能声称 Godot 生命周期或空 hook 的真实游戏开销已通过。
- live 采集/冻结、真实帧间隔/呈现、玩家体感、GC pause、最坏队列 burst、最终记录体积、恢复/seek/跨机器等仍未证明。公开 DTO 不是已批准权威状态快照。
- 初次同一进程 3 个代理旋转块及未来每模式 1 局不足以建立稳态、尾延迟或统计因果保证；正式预热策略、重复数、G-02 毫秒/分配/体积预算须在游戏基线上另批。

## 复现及交回

入口和执行命令见 README；已完成的代理无需再计时，优先：

```powershell
& './experiments/PERF-001/Verify-Delivery.ps1' -RecheckProxy
```

它仅验证小型索引并复核本包原始结果，不 build、不再次计时、不访问当前玩家环境。下一阶段仍须 PM 新签发候选/安装/配置/人工运行范围。Implementation 在本报告/索引提交后停止 Phase 0；不自行启动 pilot。
