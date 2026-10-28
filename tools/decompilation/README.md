# STS2 本地反编译导航缓存

`Invoke-DecompileSts2.ps1` 为固定的 STS2 `v0.111.0` / commit `41cef1ea` / `sts2.dll` SHA-256 建立版本隔离的本地导航缓存。反编译输出只写到 workspace 外；默认位置为：

```text
C:\Projects\STS2ReplayMod-local\decompiled\v0.111.0-41cef1ea-0861BFA1\
```

## 运行

从项目根目录执行：

```powershell
& '.\tools\decompilation\Invoke-DecompileSts2.ps1'
```

可显式指定输入和外部输出根目录：

```powershell
& '.\tools\decompilation\Invoke-DecompileSts2.ps1' `
  -AssemblyPath 'C:\absolute\path\to\sts2.dll' `
  -OutputRoot 'C:\absolute\external\cache-root'
```

无论输入路径如何，脚本都会先校验固定 length 和 SHA-256。身份不符时，它会在创建输出目录、恢复工具或读取既有缓存之前停止。输出目录不得位于项目 workspace 或游戏安装目录内。

ILSpy CLI 由仓库根目录 `.config/dotnet-tools.json` 固定为精确版本 `9.1.0.7988`，并禁用 roll-forward；该版本与本机 .NET 8 工具运行环境兼容。包由标准 .NET 全局 NuGet 缓存提供，不写入项目的 `evidence/`、`references/` 或模组源码目录。

首次生成使用 cache root 下唯一的临时 sibling 目录。只有在 `src/`、`README.txt`、manifest 与 inventory 全部验证后，才把临时目录原子 rename 为固定 cache key。脚本不递归删除任何目录，也不覆盖已存在的版本目录。再次运行只验证 manifest、源身份和输出 inventory，成功时返回 `CACHE_VALID`，不会重写 `src/`。

## 使用边界

2026-10-26 D-092：生成的C#可直接用于日常理解调用、选择hook和编写mod，编译引用对应游戏DLL，尽早用实际运行验证。它不是官方原始工程；反编译含糊、异步/底层语义影响结论或行为不符时再查IL，不要求每个方法先输出IL证据。

缓存仍标记non-authoritative，不把还原源码单独当产品行为已验证；缓存原件保持只读，必要实现写入mod工程，不尝试将整个还原工程当官方源代码重新编译。不得公开分发缓存或把大量游戏源码混入本项目交付。旧工具报告/缓存README的“所有方法体必须IL复证”是旧流程，当前开发按D-092处理。
