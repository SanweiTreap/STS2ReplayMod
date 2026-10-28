# A5 runtime-r1

同一SPIKE001A5的身份隔离修订，用于用户在r2 Arm后主动退出、随后要求继续的fresh完整A/B。没有声称或修复production/harness故障。

原工程、sealed r1/r2和331份r2文件均保持原hash。工作副本只改private revision、Harmony owner、manifest version；新schema仍为2。原20字段/30帧/250ms/deadline/floor/Task/语义标准不变。

```powershell
& 'C:/Projects/STS2ReplayMod/experiments/SPIKE-001A5-runtime-r1/Invoke-OfflineValidation.ps1'
```

125项合同、4/4 patch/unpatch、dummy与增量审计通过；889个探针方法、locals/EH/fields在两个identity字符串归一后与sealed r2一致。3项新增合同使用旧attempt公开pointer/arm metadata作为前驱，在synthetic目录证明新旧namespace隔离。没有读取当前native payload或启动游戏。

合同见[evidence runtime-r1](../../evidence/spike-001a5/runtime/data-contract-runtime-r1.md)，实际阶段见[运行报告](../../project-management/investigation-reports/SPIKE-001A5-runtime-report.md)。游戏操作仍只由用户手动完成。
