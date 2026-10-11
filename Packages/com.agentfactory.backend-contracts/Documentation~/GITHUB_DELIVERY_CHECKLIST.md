# GitHub 交付清单

## 提交到现有 Unity 仓库

```bash
git switch main
git pull --ff-only
git switch -c backend/contracts-v0.1
git add Packages/com.agentfactory.backend-contracts
git commit -m "contract: add backend data contracts and handoff docs"
git push -u origin backend/contracts-v0.1
```

推送后创建以 `main` 为目标分支的 Draft Pull Request。不要直接推送或合并到 `main`，也不要只上传 ZIP。Unity 编译和序列化兼容检查通过、评审批准并合并后，再在合并提交上创建 `v0.1.0-contracts` Tag；ZIP 仅作为可选的 Release 快照。

## Pull Request 必填内容

```text
变更范围：
修改的数据类或接口：
是否影响旧 JSON：是/否
是否需要提高 SaveVersion：是/否
新增验收测试：
测试初始快照：
实际结果证据：
未解决问题：
```

## 交付模拟负责人时

- [ ] 告知 GitHub 仓库 URL、分支和 Tag
- [ ] 指定 `docs/HANDOFF.md` 为第一阅读入口
- [ ] 确认她能本地编译 Contracts 项目
- [ ] 共同确定 JSON 序列化库和字段命名策略
- [ ] 共同确认固定仿真步长
- [ ] 共同确认错误码和日志格式
- [ ] 共同运行 AF-SIM-01 至 AF-SIM-03
- [ ] 将疑问开为 Issue，不通过聊天临时改变字段语义

## 每次阶段验收

- [ ] 源代码已推送
- [ ] 测试已推送并能重复运行
- [ ] 配置版本已记录
- [ ] 初始与输出 JSON 已保存
- [ ] 资金守恒通过
- [ ] 流体守恒通过
- [ ] 引用一致性通过
- [ ] 确定性回放通过
- [ ] 创建对应版本 Tag 和 ZIP 快照
