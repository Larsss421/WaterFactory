# 后端数据字典

## 1 配置层

| 类 | 作用 | 主要使用者 |
|---|---|---|
| `GameDefinitions` | 所有静态配置的总入口，并携带配置版本 | 全部模拟模块 |
| `FluidDefinition` | 流体价格、买卖能力和可选温度相态 | 进口、出口、混合 |
| `RecipeDefinition` | 配方输入、输出、周期、电费和适用设备 | 设备加工 |
| `DeviceDefinition` | 设备价格、工时、占地、端口、缓存和支持配方 | 建造、设备模拟 |
| `PipeDefinition` | 管道容量、流量、压力、健康和维修参数 | 管网、压力、维修 |
| `UpgradeDefinition` | Lv1 到 Lv2 的费用、工时和速度倍率 | 升级系统 |
| `FactoryDefinition` | 厂区网格、地皮、障碍、出口、原料和 Agent 上限 | 厂区初始化 |
| `StoryDefinition` | 章节、合同、目标和解锁配置 | 剧情系统 |

配置对象在一局游戏运行期间只读。平衡数值不得写入 `WorldState`，但实际支付金额要写入流水和对应运行状态，保证配置变更后仍能解释旧存档。

## 2 世界根对象

| 字段 | 含义 |
|---|---|
| `SaveVersion` | 存档结构版本 |
| `ConfigVersion` | 创建或保存快照时使用的配置版本 |
| `WorldVersion` | 每次合法权威修改后递增，用于请求竞争和联机 |
| `SimulationStepId` | 固定仿真步编号，用于稳定操作 ID 和复现 |
| `SimTimeSeconds` | 权威仿真时间，暂停时不增加 |
| `Factories` | 已创建厂区的全部运行状态 |
| `Economy` | 两个厂区共享的资金账户 |
| `Story` | 全局剧情与解锁状态 |
| `Ledger` | 所有资金事件，余额必须能由其核对 |
| `EventLog` | 剧情、统计和故障说明使用的权威事件 |
| `ProcessedRequests` | 请求幂等记录，防止重复扣费和重复创建 |

## 3 厂区与空间

`FactoryState` 是单厂区运行容器。两个厂区的设备、流体包、管网、Agent、任务和订单不得相互引用，除非未来明确实现跨厂区接口。

`GridRuntimeState` 负责快速查询地皮、占用、可建造和可通行信息。`LandParcelState` 负责记录购买事实和实际价格。

`PlaceableState` 是设备与管道共有的身份和空间字段。`InstanceId` 表示世界中的具体对象，`DefinitionId` 表示对象类型。

## 4 设备与管道

`DeviceState` 保存设备等级、健康、工作状态、配方、端口、缓存、施工和升级状态。`WorkState` 是运行原因，`LifeState` 是生命周期，两者不要合并。

`PipeState` 保存管内流体包引用、压力、流入流出、连接和损坏状态。`Damaged` 阶段仍保持原流通能力；`Ruptured` 后停止输送并断开网络。

`PortState` 表示世界中的实际端口坐标、方向和连接。端口是否相邻不等于真正连接。

`ConnectionState` 表示两个端点的直接连接。`PipeNetworkState` 表示连接图中的一个连通分量。设备输入端和输出端通常属于不同网络，配方在设备内部完成转换。

## 5 流体与配方

`FluidBufferState` 表示设备内部容量受限的库存。`FluidStack` 表示一种流体及其数量。

`FluidPacketState` 是管内运动的权威流体批次；Unity 流光仅是其视觉表现。新创建流体包不得在同一步再次推进。

`RecipeRuntimeState` 保存周期进度和已预留输入。输出空间不足时不能消耗输入。

## 6 Agent 与任务

`TaskState` 是后端任务状态机。有效主线为 `Created → Queued → Claimed → Moving → Executing → Completed`。

`AgentState` 保存归属厂区、位置、当前任务和路径。任务与 Agent 的绑定必须同时修改，并始终双向一致。

## 7 经济与订单

`EconomyState` 是唯一余额。`MoneyTransaction` 保存金额、前后余额和关联对象。收入为正，支出为负。

`OrderState` 保存真实交付进度和截止时间。每单位交付时结算收入，订单完成时不再次发放整笔收入。

## 8 操作请求

所有请求都携带 `RequestId`、`PlayerId` 和 `ExpectedWorldVersion`。服务端必须重新计算价格、占格和合法性，不能相信客户端提交的派生值。

`OperationResult` 必须说明成功与否、错误码、世界版本、资金变化和受影响对象。

## 9 剧情

`StoryState` 保存全局解锁 ID。`StoryContractState` 绑定真实订单，`StoryObjectiveState` 保存事件累计或状态检查结果。`CompletionRewardsGranted` 防止重复解锁。

## 10 必须长期保持的不变量

1. `InitialBalance + Σ Ledger.Amount == Economy.Balance`。
2. 同一关键 ID 在整个世界唯一。
3. 每个任务至多绑定一个 Agent，每个 Agent 至多绑定一个任务。
4. 占用格中的对象 ID 必须存在且覆盖该格。
5. 连接的两个端点必须存在、相邻、相对且介质兼容。
6. 流体包的 holder 必须存在，或状态已经是泄漏、消耗、销售、移除。
7. `Removed` 对象不得参与生产、管网、健康扫描和任务调度。
8. 厂区对象只能引用同一 `FactoryId` 内的普通对象。
9. 订单交付量不得超过需求量。
10. 缓存总量不得超过容量，也不得出现负数。
