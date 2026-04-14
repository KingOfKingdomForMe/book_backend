---
name: incident-troubleshooting
description: "用于启动失败、DI 注册错误、配置缺失、JWT 配置错误、数据库连接问题、Windows Service 运行异常排查。Use for troubleshooting runtime and startup incidents in this repository."
argument-hint: "描述症状、报错、最近改动或当前运行命令"
user-invocable: true
---

# 故障排查流程

## 何时使用
- `dotnet run` 启动失败。
- 依赖注入、配置、数据库连接、JWT 或服务部署出现异常。
- Windows Service 启动后不可访问或 Swagger 打不开。

## 关键排查点
- `ThreeBooks.BookBackend.LoginService/Program.cs`
- `ThreeBooks.BookBackend.LoginService/Extensions/ServiceCollectionExtensions.cs`
- `ThreeBooks.BookBackend.LoginService/appsettings.json`
- `ThreeBooks.BookBackend.LoginService/appsettings.Service.json`
- `ThreeBooks.BookBackend.LoginService/appsettings.Local.json`
- `ThreeBooks.BookBackend.LoginService/scripts/Deploy-LocalWindowsService.ps1`

## 排查步骤
1. 明确失败发生在 build、startup、runtime、migration 还是 service deployment 阶段。
2. 检查 `ConnectionStrings:LoginDb` 是否存在，数据库是否可达。
3. 检查 `Jwt:SigningKey` 是否至少 32 个字符，以及 issuer、audience 是否为空。
4. 检查 `Program.cs` 中配置加载顺序、Swagger 启用条件、中间件顺序和初始化调用。
5. 检查 `ServiceCollectionExtensions.cs` 中选项绑定、DI 注册、MySQL 版本与授权策略。
6. 如果是 Windows Service，检查管理员权限、发布目录、exe 与 runtimeconfig 是否齐全、防火墙规则与端口绑定是否正确。

## 输出要求
- 先给阶段定位与最可能的根因。
- 再给证据和最小验证动作。
- 最后给下一步修复顺序，避免同时改太多变量。