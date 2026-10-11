# AgentFactory Backend Contracts

本包是《异星流体工厂》的后端模拟核心。它包含权威世界状态、静态配置、操作请求、确定性模拟算法、存档服务、不变量校验和验收边界；不包含 Unity 场景、动画或 UI 表现。

## 交付方式

团队协作以 GitHub 仓库为唯一主版本。本包作为 Unity 工程中的嵌入式 UPM 包位于 `Packages/com.agentfactory.backend-contracts`。每个可验收版本创建 Git Tag；ZIP 只能作为同版本的冻结快照，不能代替仓库源代码。

## 目录

```text
Runtime/
  Core/          基础坐标和枚举
  Definitions/   不随单局改变的配置数据
  World/         权威运行状态
  Operations/    玩家或联机操作请求与结果
  Simulation/    模拟接口与核心实现
Documentation~/
  HANDOFF.md               接入与交接说明
  DATA_DICTIONARY.md       类、字段和所有权说明
  SIMULATION_ACCEPTANCE.md 模拟验收测试
  SIMULATION_IMPLEMENTATION.md 实现结构和集成说明
Samples~/MinimalWorld/
  minimal-world.json       最小世界快照示例
```

## 关键约束

1. `WorldState` 是唯一结算权威，Unity View 只读取它。
2. `Definitions` 是配置，`World` 是单局运行事实，两者不得混用。
3. 所有关键对象 ID 在整个世界内唯一。
4. 资金使用 `decimal`，流体数量和时间使用 `double`。
5. 模拟采用固定步长，默认建议 `0.1` 个仿真秒。
6. 字典处理前必须按稳定 ID 排序，不能依赖字典遍历顺序。
7. 操作以 `requestId` 去重；成功修改后才增加 `WorldVersion`。
8. 未完工对象占格，但不参与生产、缓存和管网。
9. 两个厂区共用资金和剧情，其他运行数据按厂区隔离。
10. 模拟方不得直接改 UI 或动画数据，所有结果写回世界状态。

## 构建

使用 Unity 6000.3.24f1 打开项目即可由 `AgentFactory.Contracts.asmdef` 编译。JSON 使用 `com.unity.nuget.newtonsoft-json`。当前源代码保持 C# 9 兼容。

## 交接入口

模拟负责人先阅读 `Documentation~/HANDOFF.md` 和 `Documentation~/SIMULATION_IMPLEMENTATION.md`，再按 `Documentation~/SIMULATION_ACCEPTANCE.md` 从 AF-SIM-01 开始逐项运行和留证。
