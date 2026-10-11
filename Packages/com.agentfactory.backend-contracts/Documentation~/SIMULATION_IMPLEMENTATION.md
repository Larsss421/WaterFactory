# 模拟核心实现说明

## 已实现

- `WorldInitializer`：根据配置建立世界、网格、地块、初始 Agent 与剧情状态。
- `WorldRequestHandler`：处理建造、取消施工、拆除、升级、切换配方、购买土地/厂区、雇佣 Agent 和接受订单，并提供请求幂等与世界版本检查。
- `GridPathfinder` / `TaskDispatcher`：确定性 A* 寻路与按优先级、路径成本、创建顺序和 ID 派发任务。
- `NetworkTopologySystem`：按稳定顺序建立管道连接和连通分量。
- `BufferService` / `DeviceProcessingSystem`：容量检查、配方输入预留、生产周期、产物生成和电费结算。
- `PressureSystem`：根据管内占用计算压力、累计损伤、爆裂、泄漏状态和唯一维修任务。
- `EconomySystem`：所有资金变化写入幂等账本，并维护余额、累计收入和累计支出。
- `StoryProgressSystem`：消费权威事件、累计目标并只发放一次解锁奖励。
- `WorldSerializer`：使用 Newtonsoft.Json、camelCase 和字符串枚举进行 JSON 往返，提供逐版本迁移接口。
- `WorldInvariantValidator`：检查资金、ID、跨厂区引用、Agent/任务双向绑定、占格、容量和订单范围。
- `WorldSimulation`：按固定顺序推进任务、Agent、生产、订单、压力、剧情、破产检查与仿真时钟。

## 设计原则

1. 所有集合按稳定 ID 或序列排序后处理，确保相同输入得到相同输出。
2. 资金变化只能通过 `EconomySystem`，请求和交易都必须有稳定 ID。
3. Unity 视图不直接修改模拟状态，只提交操作请求并读取 `WorldState`。
4. 模拟使用固定 `deltaTime`；暂停时不推进生产、订单和仿真时钟。
5. 管道模型采用可解释的容量/流量/压力规则，不宣称模拟真实流体力学。

## 后续集成工作

- 将游戏配置 ScriptableObject 转换为 `GameDefinitions`。
- 将 Unity 输入和 UI 操作转换为 `OperationRequest`。
- 将 `WorldState` 投影到场景对象、动画和 UI。
- 根据正式游戏数值补齐订单生成、进口释放和流体包在连接上的逐段移动表现。
- 运行 `SIMULATION_ACCEPTANCE.md` 中的完整场景测试并保存证据。

## 编译与测试边界

当前包以 Unity 6000.3.24f1 / C# 9 为目标。测试源位于 `Tests/Runtime`，只有在启用 `UNITY_INCLUDE_TESTS` 后才由 Unity Test Framework 编译和执行。
