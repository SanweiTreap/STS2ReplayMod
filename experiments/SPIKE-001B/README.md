# SPIKE-001B — 两条 accepted 语义操作

当前为 `PHASE0_CANDIDATE / RUNTIME_HOLD`。独立 Mod ID `SPIKE001B`，owner `STS2ReplayMod.SPIKE001B.TwoActions.phase0-r1`，private revision `phase0-r1`，schema 1。这是开发者单房间 control。

复用 A5 已验收的 room-before/floor/ProcessFrame/完整语义投影/SaveRun Task 观察器，只增加一张明确定位的普通 Defend 和一次 EndTurn。A 的三个稳定点为 Round 1 / 1 / 2；B 正常 Continue 后先比较初始状态，再各分派一次并逐点比较。任何差异立即停止后续分派。实际 accepted 证据必须同时满足生产 Action 执行完成、未取消/无选择/无故障及对应效果边界成功。

## 离线复现

```powershell
& 'C:/Projects/STS2ReplayMod/experiments/SPIKE-001B/Invoke-OfflineValidation.ps1'
```

使用已有 .NET 9 工具。测试仅读取固定游戏 DLL、A5 的公开初始投影和 synthetic 数据；patch smoke 不调用生产目标，dummy 是独立 .NET 测试进程。不会启动 STS2，也不读取 A5 private raw checkpoint 或当前玩家状态。

## 文件

- `Core/Trajectory.cs`：两操作顺序、完整手牌 hash＋精确 slot/有效属性指纹定位、每点比较和独立 oracle。
- `ActionBridge.cs`：RequestEnqueue 观察、生产 Action 生命周期、效果 Task 与 EndTurn 接受转换；不改变生产 Task/参数/结果。
- `Core/Flow.cs`：20 字段、30 相邻 Engine 帧、250ms gap、每阶段 deadline 和有界诊断。
- `PrivateStore.cs`：不可覆盖 attempt 文件和追加式 activation journal；中断可 fresh A，不删除旧记录、不靠改 binary 重开。
- `AttemptValidation.cs`：完整 A 日志/三快照 commit＋成功 ready proof 才能 ReadyB；same candidate/checkpoint 和 fresh process 门。

详细合同在 `evidence/spike-001b/data-contract.md`；固定 API 解释在 `new-api-contract.md`；未来人工草案在 `manual-plan.md`。候选、最终报告及索引交 Project Lead 验收后，才可放行配置、安装或游戏运行。当前未安装/启用，没有新的人工测试请求。A5 已关闭，保持只读。
