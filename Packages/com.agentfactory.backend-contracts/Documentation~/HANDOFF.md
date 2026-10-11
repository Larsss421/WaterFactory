# 模拟模块交接说明

## 1 交付范围

本包覆盖前十个后端阶段的数据契约：世界状态、建造请求、Agent 任务、网格与管网、流体包、配方、压力故障、经济订单、拆除升级、双厂区和剧情解锁。

本包交付“数据含义和模块边界”，模拟负责人交付“按固定步长改变这些数据的实现”。接口不是空泛建议，而是两人协作时必须共同遵守的契约。

## 2 GitHub 协作方式

流程：

1. 后端数据结构负责人建立 `backend/contracts-v0.1` 分支，提交本包。
2. 模拟负责人从该分支建立 `simulation/core-loop`。
3. 公共数据结构的变更单独提交，Commit 使用 `contract:` 前缀。
4. 模拟实现使用 `sim:` 前缀，测试使用 `test:` 前缀。
5. Pull Request 必须说明修改了哪些字段、是否破坏旧存档、增加了哪些验收测试。
6. 数据结构冻结后创建 Tag `v0.1.0-contracts`，同时保留 ZIP。

不要将 `Library/`、`Temp/`、`Logs/`、`obj/`、`bin/` 提交到仓库。

## 3 模拟负责人要实现的模块

建议建立独立程序集 `AgentFactory.Simulation`，引用 `AgentFactory.Contracts`：

```text
Simulation/
  WorldSimulation.cs
  Requests/
    WorldRequestHandler.cs
    BlueprintHandler.cs
    UpgradeHandler.cs
  Tasks/
    TaskDispatcher.cs
    TaskStateMachine.cs
  Grid/
    GridFootprintCalculator.cs
    NetworkTopologySystem.cs
  Fluids/
    BufferService.cs
    PacketMovementSystem.cs
    DeviceProcessingSystem.cs
  Pressure/
    PressureSystem.cs
    MaintenanceScanner.cs
  Economy/
    EconomySystem.cs
    OrderSystem.cs
  Story/
    StoryProgressSystem.cs
  Validation/
    WorldInvariantValidator.cs
```

## 4 固定仿真顺序

每个固定步必须使用相同顺序：

```text
确认并处理外部请求
→ Agent 和任务
→ 进口实际采购
→ 缓存向管道释放
→ 流体包移动和抵达
→ 设备配方加工
→ 订单优先交付和固定价销售
→ 压力、损伤、炸裂和维修扫描
→ 剧情事件消费
→ 统计与一致性检查
→ SimTime 和 SimulationStepId 增加
```

世界暂停时不得推进生产、订单时间和运行费用。恢复类操作按产品决定是否允许推进。

## 5 输入输出责任

### 模拟输入

- `WorldState`：本步开始前的权威状态。
- `GameDefinitions`：流体、配方、设备、管道、厂区和剧情规则。
- `deltaTime`：固定步长，建议 `0.1`。
- 已排序的操作请求队列。

### 模拟输出

- 原地更新后的 `WorldState`，或后续改造成明确的状态差异集。
- 新建或更新的任务、流体包、资金流水和事件。
- 可解释的失败原因码。
- 一致性检查结果。

### 模拟方不得修改

- Unity Transform 和动画状态。
- 相机位置与当前查看厂区。
- 本地 UI 选择状态。
- `Definition` 中的配置值。

## 6 接口解释

`IWorldRequestHandler`：接收请求，检查请求去重、世界版本、资金、占格和冲突，然后以原子操作改变世界。

`IFactorySimulation`：运行单个厂区的一个固定仿真步。上层负责以稳定 `FactoryId` 顺序遍历厂区。

`ITaskDispatcher`：在同一厂区内按照优先级、实际路径代价、创建顺序和任务 ID 选择 Agent。

`IPathfinder`：返回路径、选中的工作格和代价。不可达必须返回原因，不能返回空路径并假装成功。

`INetworkTopologySystem`：在建造、拆除、炸裂、维修或端口变化时重建连接和连通分量；不负责移动流体。

`IEconomySystem`：唯一允许修改余额的服务，同时写入资金流水并防止重复操作。

`IStoryProgressSystem`：消费权威事件、更新目标和解锁内容；不重复结算订单收入。

`IWorldInvariantValidator`：在测试、存档前和调试构建中验证跨模块不变量。

## 7 变更契约的规则

以下改动属于破坏性变更，必须通知后端负责人并提高主版本或存档版本：

- 删除或重命名公开字段。
- 改变枚举已有成员的数值顺序。
- 改变金额正负含义。
- 改变 ID 的唯一范围。
- 改变流体包位置语义。
- 改变订单收入结算时点。

允许的兼容性改动：

- 新增可空字段。
- 新增带默认值的配置字段。
- 新增错误码。
- 新增枚举成员，但必须验证旧存档读取策略。

## 8 交付完成条件

模拟负责人不能只提交能运行的视频。每次交付必须包含：

1. 对应源代码和单元测试。
2. 使用的配置版本与 Git Commit SHA。
3. 初始世界 JSON。
4. 操作步骤和期望结果。
5. 实际输出世界 JSON或关键字段快照。
6. 资金守恒、流体守恒和引用一致性报告。
7. 失败测试的原因及复现步骤。
