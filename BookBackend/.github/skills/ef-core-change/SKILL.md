---
name: ef-core-change
description: "用于 EF Core 与 MySQL 实体变更、DbContext 映射、迁移生成、database update、索引和约束检查。Use for entity changes, migrations, and schema review in ThreeBooks.BookBackend.LoginService."
argument-hint: "描述本次实体、字段、索引、关系或迁移目标"
user-invocable: true
---

# EF Core 变更流程

## 何时使用
- 新增或修改实体、字段、索引、唯一约束、外键关系。
- 需要生成或审查 EF Core migration。
- 需要确认 MySQL schema 变更对登录服务的影响。

## 关键位置
- `ThreeBooks.BookBackend.LoginService/Domain/Entities/`
- `ThreeBooks.BookBackend.LoginService/Infrastructure/Persistence/LoginDbContext.cs`
- `ThreeBooks.BookBackend.LoginService/Infrastructure/Persistence/LoginDbContextFactory.cs`
- `ThreeBooks.BookBackend.LoginService/Infrastructure/Persistence/Migrations/`
- `.config/dotnet-tools.json`

## 操作步骤
1. 先确认领域模型变化是否同时影响 Contracts、Application 服务逻辑与初始化种子数据。
2. 更新实体与 Fluent API 映射，保持 utf8mb4、显式索引、唯一约束和 UTC 时间约定。
3. 运行本地工具恢复与迁移命令：

```powershell
dotnet tool restore
dotnet ef migrations add <MigrationName> -p ThreeBooks.BookBackend.LoginService -s ThreeBooks.BookBackend.LoginService
dotnet ef database update -p ThreeBooks.BookBackend.LoginService -s ThreeBooks.BookBackend.LoginService
```

4. 审查生成的 migration，重点看列类型、索引名、唯一性、删除行为、时间精度和数据迁移逻辑。
5. 检查启动迁移或 seed 逻辑是否受影响，避免把开发环境假设带进生产发布流程。

## 检查清单
- 是否为登录名、外部身份、token 等关键字段定义了明确索引和唯一约束。
- 是否避免了破坏性改动而没有补充数据迁移方案。
- 是否保持 LoginDbContextFactory 可支持 design-time migration。
- 是否需要同步更新 sql 目录下的参考脚本或发布说明。