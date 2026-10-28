# Git仓库与协作

更新：2026-10-28。用户要求给现有项目建立Git并上传GitHub；最终可见性要求为私有。

- 仓库：https://github.com/SanweiTreap/STS2ReplayMod
- 所有者：SanweiTreap；可见性：private。
- 本地目录：C:/Projects/STS2ReplayMod；主分支：main。
- origin：https://github.com/SanweiTreap/STS2ReplayMod.git

首次导入是现有工作目录的开发快照，PLAYBACK-001仍在进行，不是对应构建已全部通过的发布标签。Implementation可同时继续代码工作；暂存之后的新改动保留在工作树，不能为追求clean状态reset/clean或覆盖它们。

源码、项目管理文档、历史实验源码与开发工具进入版本控制。`.gitignore`排除原始evidence、运行日志、存档录像、恢复备份、下载的第三方源码/归档、编译输出和游戏反编译缓存；references来源说明与许可证副本、evidence说明页保留。历史文档中的本地证据链接不保证在clone后可用。Git远程不替代原测试账号的存档备份。

凭据由本机Git Credential Manager管理，不写入文件、remote URL或项目文档。现有Git作者配置沿用，不改全局配置。后续若考虑公开，需明确改变可见性并重新检查分享范围；不得因旧的“公开”选择覆盖用户最新的私有要求。

日常在任务自然停点提交相关变更，避免把未完成源码描述为实测发布。同一时间仅一个对话进行stage/commit/push；Git初始化与首次推送由PM协调完成后，Implementation可正常提交其任务变更。普通代码迭代不因此增加阶段审批；不force-push、不重写别人历史或清理他人工作树。
