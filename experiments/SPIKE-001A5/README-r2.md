# SPIKE-001A5 Phase0-r2

2026-10-09：capture 诊断修订已完成离线交付，等待 Project Lead 验收。未安装、未改配置、未读取当前 production checkpoint、未启动游戏。实际 A/B=0，重建等价性仍 Unknown。

r1 的 README、脚本、报告、候选及24文件交付索引保持原 hash；它们描述历史 r1。当前源码是 phase0-r2/schema 2，候选独立保存在 [artifacts-phase0-r2](artifacts-phase0-r2/)。修改前保存的10份 r1源码与manifest在 [r1-source-before-r2](../../evidence/spike-001a5/r1-source-before-r2/)。

## 复现离线验证

```powershell
& 'C:/Projects/STS2ReplayMod/experiments/SPIKE-001A5/Invoke-OfflineValidation-r2.ps1' -Dotnet 'C:/Users/sanwe/.dotnet/dotnet.exe'
```

每次产生新的 `evidence/spike-001a5/validation-r2-<GUID>/`，不覆盖旧证据。验证包含 r1保留检查、Release、122项合同、4个 patch/unpatch、真实 Harmony dummy Task非干扰，以及增量白名单/诊断审计。测试 fixture 全为 synthetic，不执行任何 production目标或启动游戏。

诊断修订保留失败帧的20字段、callback计数与真实frame/time；quiet reset独立，terminal保留最后诊断。无样本显式null/NoSample。capture heartbeat每秒最多一次，20次/163840 bytes上限；timer仅读Flow，Tick不逐帧写盘。30相邻Engine帧、250ms、deadline、floor、Task及语义范围保持原标准。

## 交付

- [r2增量报告](../../project-management/investigation-reports/SPIKE-001A5-Phase0-R2-report.md)
- [r2数据合同](../../evidence/spike-001a5/data-contract-r2.md)
- [r2人工请求草案（未授权）](../../evidence/spike-001a5/manual-request-draft-r2.md)
- [r2只读seal核对工具（本轮未执行）](../../evidence/spike-001a5/Test-SealedInput-r2.ps1)
- [r2交付索引](../../evidence/spike-001a5/phase0-r2-delivery-2026-10-09.json)

当前在离线交付后停手；配置、安装和人工A/B均须由PM在同包验收后安排。
