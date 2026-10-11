# AgentFactory Backend Contracts

本仓库是《异星流体工厂》前十个后端阶段的团队交接包。它定义权威世界状态、静态配置、操作请求、模拟接口和验收边界，不包含 Unity 场景、动画、UI 或具体寻路算法实现。

## 交付方式

团队协作以 GitHub 仓库为唯一主版本。建议将本目录作为 Unity 项目仓库中的 `Packages/com.agentfactory.backend-contracts`，或作为独立仓库通过 Git Submodule 引入。每个可验收版本创建 Git Tag，并导出同版本 ZIP 作为冻结快照。

## 目录

```text
src/AgentFactory.Contracts/
  Core/          基础坐标和枚举
  Definitions/   不随单局改变的配置数据
  World/         权威运行状态
  Operations/    玩家或联机操作请求与结果
  Simulation/    模拟模块必须实现的接口
docs/
  HANDOFF.md               接入与交接说明
  DATA_DICTIONARY.md       类、字段和所有权说明
  SIMULATION_ACCEPTANCE.md 模拟验收测试
examples/
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

```bash
dotnet build AgentFactoryBackend.sln
```

Unity 若不支持项目当前 C# 语法，可在导入时将 `record` 请求 DTO 改为普通可序列化 class；字段语义和 JSON 名称保持不变。

## 交接入口

模拟负责人先阅读 `docs/HANDOFF.md`，再按 `docs/SIMULATION_ACCEPTANCE.md` 从 AF-SIM-01 开始逐项实现和留证。
