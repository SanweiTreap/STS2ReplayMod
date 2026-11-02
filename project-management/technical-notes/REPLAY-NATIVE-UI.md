# 原生UI接入与历史记录入口

2026-11-03，D-153/D-154。PM与用户讨论中的设计资料，未派发正式UI实现；RUN-005录制工作继续独立推进。固定v0.111.0。

## 用户已确认

- 当前主菜单“回放”只供开发临时使用。所有正式功能重新编排并融入STS2原生内容，不能把临时页面当最终布局。
- 已结束对局的观看入口采用：百科大全→历史记录→左右切换到目标局→从该局历史详情进入回放。原生页面本身已是一局详情，不需要先插入一个重复的自制整局列表。
- D-155：观看回放按钮参照原生分享按钮制作，放在其左侧；没有demo时采用与分享按钮相同的无法点击效果。此前是否占用分享位置/隐藏按钮的候选已排除，不默认增设额外必需提示交互。
- 未完成/中断局、外来录像、原生设置/默认项及观看内控件的使用路径尚待讨论。用户已要求考虑保存/分享/导入并听取方案，见[文件讨论](REPLAY-FILES.md)，尚未派实现。

## 静态确认

源码根：`C:/Example/STS2ReplayMod-local/decompiled/v0.111.0-41cef1ea-0861BFA1/src/`。以下均为定点C#阅读，无游戏启动/GUI控制/存档读取。

| 原生位置 | 确认事实 | 意义 |
| --- | --- | --- |
| `MegaCrit.Sts2.Core.Nodes.Screens.MainMenu/NCompendiumSubmenu.cs:288` | OpenRunHistory调用子菜单栈PushSubmenuType<NRunHistory> | 与用户提供百科大全路径一致，复用原生导航栈 |
| `MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen/NRunHistory.cs:349` | 左箭头RefreshAndSelectRun(index+1)，右箭头(index-1) | 原生左右翻阅，不另造列表作为必经入口 |
| 同文件`:405`、`:470` | 读取当前历史文件后DisplayRun(history)，更新_history/路线/玩家等 | 适合在实际选中历史变化后刷新回放关联与可用状态；接入尚未实现 |
| `MegaCrit.Sts2.Core.Nodes.Screens/NShareButton.cs:40` | _Ready调用ConnectSignals、Disable，再设置RUN_HISTORY.SHARE.title本地化文案 | 初始化明确禁用，不是缺少本模组录像才禁用 |
| 同文件全类 | 除Ready/生成注册包装外，无分享或导出业务处理 | 不能只Enable就认为分享功能可用；C#搜索未见历史页为它绑定分享业务 |
| `MegaCrit.Sts2.Core.Saves.Managers/RunHistorySaveManager.cs:53` | 原生历史按当前profile的saves/history/{history.StartTime}.run保存 | 可在真实原生保存身份与Replay RunId之间建立旁路关系；具体落点/唯一性仍需实现验证 |
| `MegaCrit.Sts2.Core.Runs/RunHistory.cs` | 有StartTime/Seed/Players/Acts/GameMode/Ascension/Win/WasAbandoned等 | 原生历史摘要不等于Replay记录，不凭相同seed/截图或最终牌组猜关联 |

分享调查边界：尚未核PCK场景的其他连接/资源样式，不声称已审计所有平台分享能力；NMapShareButton/NShareStatsButton是其他类，不能直接当作录像导出服务。当前NShareButton类无实际分享处理已确认。截图右下有“回放”字样，但固定NRunHistory C#中未发现原生Replay处理，来源未确认，不据截图认定本体已有可用Replay。

用户截图本地副本：`evidence/ui-native/20261103-native-history-reference.png`（Git忽略，不上传公开仓库）。仅作页面布局参考；其多人对局不扩大当前单人实现范围。

## PM候选交互，待用户确认

- 观看按钮左邻原生分享按钮、同风格且无demo同样禁用已由用户确认；具体尺寸/间距、其他不可播放原因与焦点表现仍待原生接入实测。
- 翻到别局时同步刷新，不能沿用上一局的录像绑定。不存在录像/只录一段/版本不支持时明确说明，不将历史摘要当可重放文件。
- 退出观看返回原生历史页，并恢复原先局/玩家/滚动位置等合理上下文；现开发版回主菜单路径不能直接当最终交互。
- 历史与回放匹配使用真实保存/结束生命周期身份及上下文，不仅seed或最终状态。RUN-005已有真实终局采集可复用相关事实，但本次不要求其做UI或扩分享测试。
- 角色切换/原生多人历史的呈现与回放能力分别处理。当前单人支持、历史页展示多人信息，不等于已支持多人录像。

尚未知：具体原生设置扩展方式、历史文件与ReplayId最终绑定实现及重复/删除情形、未完对局入口、样式/焦点/返回状态的动态表现。后续用原生草图与窄实测解决，不先开串行INV或界面平台。
