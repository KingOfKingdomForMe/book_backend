---
name: new-api-endpoint
description: "用于新增 REST API 接口、controller、request/response DTO、状态码、鉴权和实现清单。Use when adding endpoints to ThreeBooks.BookBackend.LoginService."
argument-hint: "描述要新增的接口、资源、调用方和鉴权要求"
user-invocable: true
---

# 新增接口清单

## 何时使用
- 新增认证、账户、角色或外部登录相关接口。
- 重构现有接口契约、状态码或 DTO。
- 需要在实现前先跑一遍一致性清单。

## 关键位置
- `ThreeBooks.BookBackend.LoginService/Api/Controllers/`
- `ThreeBooks.BookBackend.LoginService/Application/Services/`
- `ThreeBooks.BookBackend.LoginService/Application/Abstractions/`
- `ThreeBooks.BookBackend.LoginService/Contracts/Requests/`
- `ThreeBooks.BookBackend.LoginService/Contracts/Responses/`

## 操作步骤
1. 先定义路由、HTTP 方法、鉴权要求、请求体、响应体和错误响应。
2. 对照已有控制器模式，决定是否落在 Auth、Users、Roles 或新的聚合边界下。
3. 为请求和响应创建 Contracts，避免让控制器直接暴露领域实体。
4. 在 Application 层定义或扩展服务接口与实现，把控制器保持为编排层。
5. 如有新增依赖或选项，更新 Extensions/ServiceCollectionExtensions.cs。
6. 补充 `ProducesResponseType`、参数校验和 CancellationToken 传递。

## 检查清单
- 路由前缀是否与现有 `api/auth`、`api/users`、`api/roles` 风格一致。
- 是否明确区分 400、401、403、404、409、422 等语义。
- 是否避免在控制器中堆积业务逻辑或直接操作 DbContext。
- 是否同步考虑 Swagger、鉴权策略和请求上下文构建。