# TOOLING-001 — Alchyr ModTemplate-StS2 适用性检查

日期：2026-09-13  
状态：`COMPLETE — ADOPT_AS_SCAFFOLD_REFERENCE`

## 问题

检查 `https://github.com/Alchyr/ModTemplate-StS2` 是否对 Replay Mode 有用，以及它是否应改变当前技术路线。

## 固定上游身份

- 仓库：`Alchyr/ModTemplate-StS2`
- 分支：`master`
- 检查 commit：`55ca2c606e6c78dd39689a5cf979b243a49652e7`
- 模板 NuGet package version：`2.5.2`
- 访问日期：2026-09-13

该仓库归属 Alchyr，模板包作者也声明为 Alchyr。仓库 README 与 wiki 将 BaseLib 明确称为 community-developed library。因此，即使游戏内或官方模组入口链接/推荐了该模板，也不能把它记录成 Mega Crit 维护的官方代码；更准确的分类是社区模板或官方入口推荐的社区工具。

## Confirmed

- 包含 Empty Mod、Content Mod、Character Mod 三种 `.NET new` 模板；Replay Mode 对应 Empty Mod，而不是 content/character 模板。
- Empty Mod 使用 `Godot.NET.Sdk/4.5.1`、`net9.0`，引用游戏目录中的 `sts2.dll` 与 `0Harmony.dll`。
- 标准入口是 `[ModInitializer]`，初始化时对当前 assembly 执行 Harmony `PatchAll`；可选启用 Godot C# script discovery。
- 工程包含 STS2 安装路径发现、DLL/PDB/manifest 自动复制、本地 `.pck` export、manifest、Godot project 与 export preset。
- BaseLib 和 `Alchyr.Sts2.ModAnalyzers` 默认通过未固定版本的 NuGet `Version="*"` 引入；README 明确说明不需要 BaseLib 时可以移除。
- 模板 manifest 默认 `min_game_version: 0.107.0`、`has_pck: true`、`has_dll: true`、依赖 BaseLib、`affects_gameplay: true`。
- 默认 build 会把产物直接复制进 live game `mods` 目录；publish 的 Godot export 使用 `ContinueOnError="WarnAndContinue"`。
- 模板包 csproj 声明 MIT license expression；本次固定仓库 snapshot 根目录没有独立 `LICENSE` 文件，生成项目也不会自动得到项目许可证文件。

## 对 Replay Mode 有用的部分

1. **正式工程脚手架**：可省去 Godot/.NET 基础布局、游戏程序集引用、manifest 和构建部署目标的重复工作。
2. **模组入口参考**：`ModInitializer`、显式 `PatchAll(assembly)` 和 logger 适合作为最小启动方式。
3. **资源发布参考**：Replay 管理页将来很可能需要 Godot scene/asset；模板给出了 DLL + PCK 的基本打包链。
4. **路径发现参考**：Windows/Linux/macOS 的游戏目录发现可以借鉴，但仍需在 CI/测试中允许显式覆盖。
5. **静态检查候选**：ModAnalyzers 可能帮助发现 manifest/localization 等常规错误，采用前需固定版本并单独验证。

## 不解决的核心问题

模板不提供 replay capture/dispatch、权威状态序列化、确定性恢复、双向 seek、主菜单 peer-entry 注册、SaveManager session isolation、性能预算或不可信 replay 导入安全。它不会改变当前 `INV-006 → scope-safe timing gate → direct-write sentinel` 的证据路线。

BaseLib 默认依赖也不等于 Replay Mode 必须依赖 BaseLib。当前已确认主菜单并列入口没有现成注册 API，核心隔离亦不能由 BaseLib/RitsuLib 直接提供。

## 采用前必须修订

- 把 game/API 兼容目标明确固定为当前支持矩阵，不沿用 `0.107.0` 默认值。
- 将 BaseLib 改为显式能力决策；如果没有已验证用途则移除，不因模板默认值引入。
- 固定所有 NuGet/tool 版本，禁止 `Version="*"` 进入可复现构建。
- 分离 build、test、deploy；普通 build 不应默认写入 live game 目录。
- release/package gate 中不得允许 PCK export 以 warning 继续后仍被视为成功。
- `Publicize` 默认保持关闭；只有具体、审计过的私有 API 需求才能开启。
- 增加测试项目、CI、构建身份/依赖锁、版本元数据和 release packaging 校验。
- 建立本项目自己的许可证与 attribution 文件，不依赖模板包元数据替代。

## 结论

`ADOPT_AS_SCAFFOLD_REFERENCE`：在进入正式实现仓库搭建时，以固定 commit 的 Empty Mod 模板作为脚手架参考，并按上述要求生成/重写受控工程。现在不安装模板、不迁移既有 experiments、不把 BaseLib 设为必要依赖，也不因此提前决定 Replay Mode 架构。

## 仍然未知

- ModAnalyzers 对 v0.111 与未来版本的实际检查覆盖和误报情况。
- PCK 打包链在 Windows/Linux/macOS 的可复现性。
- Replay 管理 UI 是否最终需要 PCK；纯 code-built UI 仍是可能方案。
- 游戏内链接该仓库的精确官方措辞与背书范围；本轮只确认仓库自身的作者和内容。

## 来源

- `https://github.com/Alchyr/ModTemplate-StS2`
- `https://github.com/Alchyr/ModTemplate-StS2/wiki/Setup`
- 固定 commit 中的 `Alchyr.Sts2.Templates.csproj`
- 固定 commit 中的 `content/ModTemplate/ModTemplate.csproj`
- 固定 commit 中的 `content/ModTemplate/ModTemplate.json`
- 固定 commit 中的 `content/ModTemplate/ModTemplateCode/MainFile.cs`
- 固定 commit 中的 `content/ModTemplate/Sts2PathDiscovery.props`

