# TOOL-001 — Local Reproducible Decompilation Cache Report

## 任务与状态

- Task ID：`TOOL-001`
- 状态：**Complete — 待 Project Lead 验收**
- 完成日期：2026-09-17
- 范围：为固定 `sts2.dll` 建立 hash-gated、版本隔离、workspace 外的本地反编译导航缓存。
- 边界：未启动游戏；未读取或修改任何游戏 profile/save/cloud；未修改游戏安装目录；未把反编译源码、工具包或临时文件写入项目 workspace、`evidence/` 或 `references/`。

## 交付物

### Workspace 内（仅可再生工具与说明）

- `.config/dotnet-tools.json`
- `tools/decompilation/Invoke-DecompileSts2.ps1`
- `tools/decompilation/README.md`
- `project-management/investigation-reports/TOOL-001-report.md`

### Workspace 外（本地导航缓存）

- Cache directory：`C:\Projects\STS2ReplayMod-local\decompiled\v0.111.0-41cef1ea-0861BFA1`
- Manifest：`C:\Projects\STS2ReplayMod-local\decompiled\v0.111.0-41cef1ea-0861BFA1\manifest.json`
- Navigation source：`C:\Projects\STS2ReplayMod-local\decompiled\v0.111.0-41cef1ea-0861BFA1\src`
- Cache warning：`C:\Projects\STS2ReplayMod-local\decompiled\v0.111.0-41cef1ea-0861BFA1\README.txt`

## 固定身份

| 项目 | 验证值 |
| --- | --- |
| Game version | `v0.111.0` |
| Commit | `41cef1ea` |
| Source DLL | `C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll` |
| Length | `9,757,184` bytes |
| SHA-256 | `0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9` |
| Cache key | `v0.111.0-41cef1ea-0861BFA1` |
| Decompiler package | `ilspycmd` |
| Pinned version | `9.1.0.7988`，`rollForward=false` |
| Tool-reported version | `ilspycmd: 9.1.0.7988`; `ICSharpCode.Decompiler: 9.1.0.7988` |
| Generated UTC | `2026-09-17T15:15:30.2972126+00:00` |
| Authority flag | `nonAuthoritative=true` |

工具由 repo-local `.config/dotnet-tools.json` 固定精确版本。NuGet package 使用标准 .NET 全局缓存，没有写入本项目的 `evidence/`、`references/` 或正式源码目录。

## 复现命令

默认生成或验证：

```powershell
& '.\tools\decompilation\Invoke-DecompileSts2.ps1'
```

显式参数形式：

```powershell
& '.\tools\decompilation\Invoke-DecompileSts2.ps1' `
  -AssemblyPath 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll' `
  -OutputRoot 'C:\Projects\STS2ReplayMod-local\decompiled'
```

脚本在创建目录、恢复工具或处理既有 cache key 前先验证 source length 与 SHA-256。首次生成位于 cache root 下唯一临时 sibling；在源码 inventory、README 和 manifest 全部验证通过后，才原子 rename 到固定 cache key。脚本不递归删除目录，也不覆盖既有版本目录。

## 验证结果

| 验证项 | 结果 | 证据摘要 |
| --- | --- | --- |
| 固定 DLL identity | **PASS** | length 与 SHA-256 均与 handoff 完全一致。 |
| 首次生成 | **PASS** | 返回 `CACHE_CREATED C:\Projects\STS2ReplayMod-local\decompiled\v0.111.0-41cef1ea-0861BFA1`。 |
| 幂等第二次运行 | **PASS** | 返回 `CACHE_VALID ...`；未执行反编译或重写。 |
| `src` 未重写 | **PASS** | 全部 `src` 文件的相对路径、长度和 `LastWriteTimeUtc` 元数据指纹在第二次运行前后均为 `E222D7F93523BE9FD3DF7969579E9B3BAAF00CE6349C175E954D9993A4D0D11C`。 |
| Manifest 未重写 | **PASS** | `LastWriteTimeUtc` 前后均为 `2026-09-17T15:15:30.3119443Z`。 |
| 不存在 assembly 负向测试 | **PASS** | `C:\definitely-missing\sts2.dll` 在任何 cache root/version 目录创建前报错。 |
| 错误 assembly 负向测试 | **PASS** | 系统 `System.Private.CoreLib.dll` 在 length gate 报错；未生成 cache。 |
| 同长度错误 hash 负向测试 | **PASS** | 9,757,184-byte 临时输入的 SHA-256 为 `5D77AABE9236DBAE1AB768B3CD4398AE7DEDBE82340B8D4E1295D86B9BE4E426`；在 SHA gate 报错，指定的输出根未创建；临时测试文件已按精确路径删除。 |
| Workspace 输出拒绝 | **PASS** | 指定 workspace 内 `-OutputRoot` 时在创建前报错，测试路径未创建。 |
| 输出边界 | **PASS** | 最终 cache path 不位于 `C:\Projects\STS2ReplayMod` 内；无 incomplete 临时 sibling。 |
| Manifest 完整性 | **PASS** | 含 game/version/commit、source absolute path/length/hash、decompiler package/version、逻辑参数、实际 generation 参数、generated UTC、inventory 与 `nonAuthoritative=true`。 |
| Output inventory | **PASS** | `3,539` files；其中 `3,538` C#；`223` directories；`19,276,847` bytes。 |
| `rg` 导航 | **PASS** | 在外部 cache 中分别检出 `RunManager`、`SaveManager.SaveRun`、`CombatManager`、`ActionExecutor`。 |
| README 权威边界 | **PASS** | 明确写明“仅导航缓存”“不作为方法体级验收证据”，技术结论须回到固定 DLL / 可复现 IL 复核，并禁止公开分发或复制大段源码进项目。 |
| Profile/save/cloud 隔离 | **PASS** | cache 中当前用户绝对路径命中数 `0`；`.save/.sav/.run/.cloud` payload 文件数 `0`；除 `manifest.json` 外 JSON 文件数 `0`。验证过程未枚举或读取游戏 profile/save/cloud 目录。 |

代表性搜索命令：

```powershell
rg -n -m 3 --glob '*.cs' 'RunManager' 'C:\Projects\STS2ReplayMod-local\decompiled\v0.111.0-41cef1ea-0861BFA1\src'
rg -n -m 3 --glob '*.cs' 'SaveManager\.SaveRun' 'C:\Projects\STS2ReplayMod-local\decompiled\v0.111.0-41cef1ea-0861BFA1\src'
rg -n -m 3 --glob '*.cs' 'CombatManager' 'C:\Projects\STS2ReplayMod-local\decompiled\v0.111.0-41cef1ea-0861BFA1\src'
rg -n -m 3 --glob '*.cs' 'ActionExecutor' 'C:\Projects\STS2ReplayMod-local\decompiled\v0.111.0-41cef1ea-0861BFA1\src'
```

## 结论

TOOL-001 的本地缓存已按固定 source identity 与 cache key 建立。缓存可用于全文搜索和 IDE 导航，但属于 `navigation cache / non-authoritative`；原始 DLL 与可复现 IL 仍是正式技术结论的权威证据。本任务不改变任何 INV/SPIKE 结论，也不阻塞或解锁 `SPIKE-001A4`。
