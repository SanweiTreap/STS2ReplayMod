# TOOL-001 — Local Reproducible Decompilation Cache

状态：Ready — Non-blocking  
发出日期：2026-09-17  
任务类型：本地开发工具；可与主线静态/Phase 0工作并行，不解锁任何技术Gate

## 目标

建立一套可重复、hash-gated、只保存在workspace之外的STS2 managed assembly反编译缓存，用于全文搜索和IDE导航。反编译C#只作为`navigation cache / non-authoritative`；原始DLL和可复现IL仍是正式技术结论的权威证据。

## 固定输入

- 默认assembly：`C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll`
- 版本：`v0.111.0`，commit `41cef1ea`
- length：9,757,184 bytes
- SHA-256：`0861BFA1DF347538D932F22D580E75420F08082792EB914E53B4882764ACDBE9`
- 默认cache root：`C:\Projects\STS2ReplayMod-local\decompiled`
- 固定cache key：`v0.111.0-41cef1ea-0861BFA1`

identity不符立即停止，不得为“当前版本”生成缓存。

## 实现要求

在workspace内只创建可再生工具与说明，例如：

- `tools/decompilation/Invoke-DecompileSts2.ps1`
- `tools/decompilation/README.md`
- 如需ILSpy CLI，使用repo-local `.NET tool manifest`并固定**精确版本**；先检查本机已有工具，禁止依赖未记录的global mutable版本。

生成内容只能位于：

`C:\Projects\STS2ReplayMod-local\decompiled\v0.111.0-41cef1ea-0861BFA1\`

目录至少包含：

- `src/`：反编译输出；
- `manifest.json`：game version/commit、source DLL absolute path、length/hash、decompiler package/version、完整参数、生成UTC、output inventory摘要与`nonAuthoritative=true`；
- `README.txt`：明确“仅导航缓存；结论须回到固定DLL/IL复核；不得公开分发或复制大段进模组源码”。

脚本必须：

- 支持显式`-AssemblyPath`与`-OutputRoot`，但始终先hash gate；
- create-new version目录；目标已存在时只验证manifest与关键identity并幂等返回，不能静默覆盖；
- 不使用递归删除，不修改游戏安装目录，不启动游戏，不读取profile/save/cloud；
- 不把decompiled source、tool cache、NuGet package或临时文件写入`evidence/`、`references/`或正式模组源码；
- 不反编译所有依赖；第一版只处理固定`sts2.dll`。有源码或官方文档的第三方库继续使用其源码/文档；
- 工具失败留下明确诊断，但不得留下看似complete的manifest；用临时sibling目录生成并在完整验证后原子rename为最终cache key。

Windows目录创建/rename必须遵守精确路径检查，确认所有目标均位于固定cache root。不得触碰其他version目录。

## 验证

至少验证：

- 同一输入连续两次运行第二次为幂等验证，不重写src；
- 错误hash/错误assembly负向测试在生成前停止；
- output不位于workspace roots内；
- manifest字段完整且source identity匹配；
- `rg`能在cache中找到代表性类型/方法：`RunManager`、`SaveManager.SaveRun`、`CombatManager`、`ActionExecutor`；
- 反编译输出可读，但README明确其不作为方法体级验收证据；
- 不包含当前用户profile/save/cloud路径或payload。

## 非目标

- 不把反编译结果提交/复制到项目仓库。
- 不修改、编译或执行反编译源码。
- 不建立游戏源码派生工程，不尝试替换游戏DLL。
- 不对许可作确定法律结论；只执行本地、非分发、最小范围策略。
- 不改变INV/SPIKE结论，不阻塞或解锁`SPIKE-001A4`。

## 交付物

- workspace内的脚本与README；
- `project-management/investigation-reports/TOOL-001-report.md`，只记录工具identity、命令、验证结果和cache manifest路径，不嵌入反编译源码；
- workspace外的cache目录及manifest。

不得修改`CURRENT_STATUS.md`、`TODO.md`、`DECISIONS.md`、`EXPERIMENT_PLAN.md`、`MVP.md`或其他受保护项目管理状态文件。完成后交回Project Lead验收。
