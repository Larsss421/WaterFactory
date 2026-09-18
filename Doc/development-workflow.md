# WaterFactory 开发与合并流程

## 开始任务

```bash
git switch main
git pull --ff-only
git switch -c feat/任务名称
git lfs install --local
```

一个功能一个短生命周期分支。修复使用 `fix/`，文档使用 `docs/`；提交信息使用 `feat:`、`fix:`、`docs:`、`refactor:`。不要直接向 main 推送。

## 提交与评审

只暂存本次任务涉及的文件，提交后使用 `git push -u origin HEAD` 发布分支，向 main 提 PR。PR 描述填写“改了什么、为什么改、怎么测试”。固定审核与人工测试负责人为 **@Larsss421**；CODEOWNERS 合入 main 后会自动向其请求评审。Larsss421 本人提交的 PR 必须另请一名有写入权限的协作者审核，不能自行批准。

本项目不使用 CI，由专人进行人工测试。main 要求至少一名非作者评审者批准，所有评审讨论已解决。新增提交会使旧批准失效；测试人员需要重新检查新增修改。提交 PR 前同步 main 并解决冲突，测试通过后才能批准和合并。

审核通过后使用 Squash merge，提交标题保持规范。main 禁止强推和删除，管理员同样遵守 PR 评审要求。GitHub 能强制“有人批准”，但无法确认测试是否真的执行；测试人员必须如实填写结果，不能只勾选清单。

模块通过公开接口、事件或 ScriptableObject 通信。设备参数与点位映射使用配置数据。涉及他人模块时先沟通。场景、预制体尽量分工，资源的 .meta 必须一起提交。

## 人工测试清单

测试人员切换到 PR 分支，在 Unity 6000.3.24f1 中完成以下检查，并在 PR 中填写测试人、场景、步骤、实际结果与已知问题：

- 项目能正常打开，Console 没有新增编译错误。
- 受影响场景能进入 Play Mode，核心功能和异常操作符合预期。
- 资源没有丢失引用，新增或移动资源与 .meta 一起提交，LFS 资源下载完整。
- 没有误提交 Library、Temp、UserSettings、IDE 工程等生成文件。
- 模块改动边界明确，涉及其他负责人代码的修改已经沟通。

纯文档和流程配置修改可以注明 Unity 运行测试不适用，但仍须人工检查内容准确性。无法完成测试的 PR 保持未批准状态；不要为了合并而填写未经执行的测试结果。

## VS Code 与 Unity

安装 GitLens、GitHub Pull Requests and Issues、Microsoft Unity 扩展。Unity 扩展会安装 C# 与 C# Dev Kit。Unity 使用 Visual Studio Editor 包，不使用已停止维护的 Visual Studio Code Editor 包。

Unity → Edit → Preferences → External Tools：将 External Script Editor 选为 Visual Studio Code，再 Regenerate project files。双击脚本验证补全，在运行场景中按 F5 附加调试并命中断点。无业务脚本时，空 solution 不代表已验证补全或调试。

## 合并以后

```bash
git switch main
git pull --ff-only
git branch -d 已合并的分支
```

GitHub 可以自动删除已合并的远程分支。main 分支保护不会替代人工测试，也不会自动部署；发布平台、构建目标和授权未确定前不启用自动部署。
