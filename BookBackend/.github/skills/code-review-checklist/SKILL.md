---
name: code-review-checklist
description: "用于统一代码评审清单、findings 输出格式、行为回归检查、配置与安全风险检查。Use for structured review of backend changes in this repository."
argument-hint: "描述要 review 的功能、文件或改动范围"
user-invocable: true
---

# 代码评审清单

## 何时使用
- 评审后端功能改动、配置改动、数据库改动或发布脚本改动。
- 需要稳定输出 findings 而不是宽泛总结。

## 评审维度
1. 正确性：是否存在直接 bug、空值路径、状态流错误、权限遗漏或返回值不一致。
2. 回归风险：是否改变了原有接口语义、配置读取顺序、DI 注册或启动顺序。
3. 安全性：是否暴露密钥、弱化 JWT 校验、放宽授权、记录敏感信息或允许危险默认值。
4. 持久化：是否破坏 migration、索引、唯一约束、软删除或 token 生命周期。
5. 运维性：是否影响 Windows Service 发布、appsettings.Service.json、本地部署脚本或 Swagger 访问。
6. 验证缺口：是否缺少最必要的构建、运行、迁移或手工验证说明。

## 输出要求
- 先列 findings，并按严重程度排序。
- 每个 finding 要说明影响、触发条件和相关文件。
- 如果没有发现问题，明确写出未发现 findings，并补充剩余风险或未验证部分。