---
name: local-windows-service-publish
description: "用于本地 Windows Service 发布、pubxml 检查、publish 输出目录核对、Deploy-LocalWindowsService.ps1 使用与服务验证。Use for local Windows service publish and deployment in ThreeBooks.BookBackend.LoginService."
argument-hint: "描述发布动作、当前错误、目标目录或是否自动部署"
user-invocable: true
---

# 本地 Windows Service 发布

## 何时使用
- 发布 LoginService 到本地 Windows Service 目录。
- 检查 LocalWindowsService.pubxml、服务名、监听地址和 Swagger 可访问性。
- 使用或排查 Deploy-LocalWindowsService.ps1。

## 关键位置
- `ThreeBooks.BookBackend.LoginService/Properties/PublishProfiles/LocalWindowsService.pubxml`
- `ThreeBooks.BookBackend.LoginService/appsettings.Service.json`
- `ThreeBooks.BookBackend.LoginService/scripts/Deploy-LocalWindowsService.ps1`
- `publish/ThreeBooks.BookBackend.LoginService/local-service/`

## 操作步骤
1. 先确认服务配置与发布目录，特别是 `Urls`、`Swagger:Enabled`、服务名和可执行文件名称。
2. 按需使用发布命令：

```powershell
dotnet publish ThreeBooks.BookBackend.LoginService -c Release -p:EnableLocalWindowsServiceAutomation=true
dotnet publish ThreeBooks.BookBackend.LoginService -c Release -p:SkipLocalWindowsServiceAutomation=true
```

3. 如果需要手工部署或排错，使用脚本动作：

```powershell
pwsh .\ThreeBooks.BookBackend.LoginService\scripts\Deploy-LocalWindowsService.ps1 -Action Deploy -ServiceName "ThreeBooks.BookBackend.LoginService"
pwsh .\ThreeBooks.BookBackend.LoginService\scripts\Deploy-LocalWindowsService.ps1 -Action Restart -ServiceName "ThreeBooks.BookBackend.LoginService"
```

4. 验证发布目录包含 exe、runtimeconfig、appsettings.Service.json 与正确的脚本副本。
5. 验证管理员权限、防火墙规则、端口解析与 Swagger URL 是否符合 `appsettings.Service.json`。

## 检查清单
- 是否发布到了 `publish/ThreeBooks.BookBackend.LoginService/local-service/`。
- 是否误把本地 source scripts 目录当成 publish 目录。
- 是否需要管理员权限来安装、重建或删除服务。
- 是否因为 `Urls` 绑定、端口占用或防火墙规则导致服务不可访问。