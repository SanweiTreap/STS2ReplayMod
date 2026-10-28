# SPIKE-001B Phase0-r2

状态：`PHASE0_R2_CANDIDATE / RUNTIME_HOLD`。同一 Mod ID SPIKE001B 的动作诊断窄修，独立源码副本保留 r1 的全部951份封存目标。private revision `phase0-r2`、schema2、owner `STS2ReplayMod.SPIKE001B.TwoActions.phase0-r2`、manifest0.1.0-phase0-r2；不和r1私有输入混用。

复用原7个patch及原有动作回调，仅增加去引用的最后观察DTO、受控site/code和有限阶段事件。timer不读live Action/card/player/Task。accepted、Round1/1/2、floor、20字段30连续帧250ms、独立deadline、两次B dispatch、完整投影/Task/ReadyB门保持。

```powershell
& 'C:/Projects/STS2ReplayMod/experiments/SPIKE-001B-r2/Invoke-OfflineValidation.ps1'
```

使用已有.NET9；88个r1合同及新增持久回读/冻结/写失败/竞态/非干扰回归在独立synthetic目录运行。无STS2进程、无engine生产目标调用、无当前玩家状态/A5 private raw读取、无配置/安装。固定API不重查，只补PlayerTurn构造初值、Increment及SwitchSides的窄IL。

原业务/人工方案见r1已验收合同；r2格式增量在 `evidence/spike-001b/r2/diagnostic-contract.md`，首回合解释在 `player-turn-contract.md`。最终独立报告、候选和交付索引交PM后等待验收，业务attempt仍0。
