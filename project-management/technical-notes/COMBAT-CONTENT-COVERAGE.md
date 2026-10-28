# 战斗内容覆盖：当前缺口与推进方式

更新：2026-10-27，D-107。下文“已确认的实现”保留D-105实施前源码调查，不能作为当前限制表：CONTENT-001已接通通用CardSelect路径，并实测初始/药水/连续/多选/回合开始选择、生成/变化与金纸的结果级录播和稳定点后退。当前能力以CURRENT_STATUS及CONTENT-001报告为准；原生窗口/逐点击过程未实现，用户提议另有记录。旧OOP与原生入口调查保留，不改写为当时已支持。

## 用户要求

下一段先推进原版战斗内容覆盖，不再用用户偏好问卷排列最终都要支持的卡牌/遗物/药水。全内容是最终范围，不把每张牌做成一个独立工作包；按共用机制实现，再找代表内容实测及补遗漏。

用户特别要求包含：药水开牌、储君印牌、无色卡印牌、许愿、秘密武器、知识恶魔提供的选择；烘焙手套、必备工具、暴政、熵等回合开始选择；猎人弃牌、储君变化牌；以及金纸等回合内触发遗物。名称按用户描述记录，本次未逐项核实中文名与模型映射，不为未查内容补造具体调用路径。

## 已确认的实现

- Clip.cs的OperationKind只有Card/EndTurn/Potion。ActionMonitor记录实际排队动作：手牌有序索引、模型ID、战斗牌实例ID、升级、状态指纹和目标；药水另记槽位、模型/状态与目标。
- PlayerLoop.Execute调用原生CardModel.TryManualPlay、PotionModel.EnqueueManualUse和EndPlayerTurnAction，牌效及其遗物/能力hook由游戏本体执行，没有逐卡重写伤害、抽牌或遗物效果。
- LiveState.Supported仍锁定单人铁甲A0首排普通战斗；普通牌输入已使用基类/目标机制检查，不是三张初始牌白名单。通用执行不等于所有牌池已覆盖。
- Patches在ReserveChoiceId处主动报incomplete-unsupported-player-choice；自动出牌链、AddGeneratedCardsToCombat、新单位生成与遗物成员变化也主动报不支持。它们是实际缺失的路径，不只是尚未逐牌测试。
- 遗物没有仅允许HappyFlower触发的白名单，原生触发会自然执行。HappyFlower另有回放期视觉适配，避免延迟计数覆盖；目前通过的可变遗物样本不足以认定全部遗物支持。遗物逻辑前向触发、状态记录完整、后退恢复、文件恢复需分别说明。

## 原生可复用入口

缓存根：C:/Projects/STS2ReplayMod-local/decompiled/v0.111.0-41cef1ea-0861BFA1/src。

- MegaCrit.Sts2.Core.GameActions.Multiplayer/PlayerChoiceSynchronizer.cs：ReserveChoiceId、SyncLocalChoice、PlayerChoiceReceived、WaitForRemoteChoice、ReceiveReplayChoice。它专门处理结算中玩家选择；源码注释例举Survivor弃牌、Discovery和Toolbox。
- MegaCrit.Sts2.Core.Commands/CardSelectCmd.cs：覆盖少量候选、牌堆、手牌、弃牌、升级/变化等入口；回放模式走等待结果分支，部分确定/自动选择或测试selector会绕开同步，不能只挂一个事件便宣称全选择覆盖。
- MegaCrit.Sts2.Core.GameActions/PlayerChoiceResult.cs与Entities.Multiplayer/NetPlayerChoiceResult.cs：区分选项索引、战斗牌、牌组牌等结果形态。
- MegaCrit.Sts2.Core.Multiplayer.Replay/CombatReplayWriter.cs：订阅PlayerChoiceReceived并RecordPlayerChoice。说明已有可参考的选择结果录制通路，不证明我们的录播/后退已兼容。
- Models.Powers/ToolsOfTheTradePower.cs、TyrannyPower.cs、EntropyPower.cs：回合开始分别经CardSelectCmd进行弃牌、消耗和变化选择，不能将全部选择只归到上一张手动出牌。
- Models.Relics/JossPaper.cs：AfterCardExhausted及AfterSideTurnEnd经阈值调用抽牌；DoActivateVisuals通过RunSafely启动并在等待后修改IsActivating。由此确认本体有回合内触发和独立延迟状态路径；本次未实测金纸，也未认定它已经造成回放错误。

## 下一段的机制目标

1. 先接通统一选择记录与重放：实际候选及顺序/状态、选择实例或选项、数量、顺序、跳过/确认结果与所属结算位置；覆盖出牌中、用药中、战斗/回合开始以及连续多次选择。回放按原流程提供结果，不让人重新选，也不能先等整张牌Task结束再投递其内部选择。是否显示选择界面、停留与时间轴粒度随实现保留必要数据，未在本次定稿。
2. 补生成/复制/变化与自动出牌的共用通路，保持新旧牌实例身份、引用、牌区和RNG对应；派生动作由本体执行，避免又作为新手动输入重放一次。选择通过不等于生成牌及恢复自动通过。
3. 按机制解除角色/目标等旧演示限制，覆盖各牌池、遗物、药水及联动。遗物要涵盖回合开始、回合内和结束触发/计数/持续状态，不能只检查是否闪光。新增对象/订阅/显示/异步回调纳入对应恢复路径。
4. 已有后退、独立会话和退出作为实际回归；新增内容所需的状态/文件扩展随功能做，不先铺设更多单点强记录实验。完整强记录仍是目标，但不阻塞内容扩展为无限前置条件。

以上是实施方向，不是已运行结论。本轮未签发新工作包；普通bug、相关hook/格式/状态改动沿D-092同任务迭代，不逐牌走INV。测试按机制与真实失败组织，不以每张牌一套完整矩阵代替可用功能。
