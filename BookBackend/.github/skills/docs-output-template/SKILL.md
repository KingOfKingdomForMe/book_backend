---
name: docs-output-template
description: "用于输出 README、接口变更说明、发布说明、运维步骤、排障记录与中文优先文档模板。Use for writing repository documentation and release notes."
argument-hint: "描述文档类型、受众、场景和想覆盖的内容"
user-invocable: true
---

# 文档输出模板

## 何时使用
- 需要写 README、接口说明、发布说明、部署手册、排障记录或变更摘要。
- 需要把 plan.md、代码、脚本和配置整理成操作文档。

## 写作原则
- 先核实命令、路径、配置项和脚本参数，再写文档。
- 中文优先，但保留必要的英文技术术语与命令原文。
- 优先写操作步骤、前置条件、验证方式和常见失败点，不写空泛背景。

## 常用结构
1. 背景或目标。
2. 前置条件。
3. 操作步骤。
4. 验证方法。
5. 常见问题或回滚方式。

## 适用素材
- `plan.md`
- `ThreeBooks.BookBackend.LoginService/scripts/`
- `ThreeBooks.BookBackend.LoginService/Properties/PublishProfiles/`
- `ThreeBooks.BookBackend.LoginService/appsettings*.json`
- 相关 controller、service、contracts 文件

## 输出要求
- 明确文档类型与目标读者。
- 只写已验证或可从仓库明确推导的信息。
- 如果存在信息缺口，直接标注需要用户补充的部分。