## Plan: Login Service Project and MySQL Foundation

在当前 solution 中新增独立 ASP.NET Core Web API 项目 ThreeBooks.BookBackend.LoginService，作为统一登录服务的宿主；初版实现账号密码登录、注册、修改密码、角色权限、JWT Access Token + Refresh Token，并为后续微信扫码、微信公众号、短信登录预留统一认证扩展点。数据库采用 MySQL + EF Core + Pomelo，通过 Migration 管理表结构。推荐把新项目先做成常规 Web API，不启用 Native AOT，避免在认证、EF Core、JWT、未来第三方 SDK 上过早承担 AOT 兼容成本；现有 BookBackend 保持不动或只保留最小集成。

**Steps**
1. Phase 1 - Solution and architecture baseline
2. 在 BookBackend.sln 中新增独立 Web API 项目 ThreeBooks.BookBackend.LoginService，目标框架使用 net8.0；该步骤独立，不依赖现有业务代码。
3. 为新项目建立清晰分层目录：Api、Application、Domain、Infrastructure、Contracts、Options、Extensions；避免继续沿用当前 BookBackend 的单文件 Program.cs 风格。该步骤依赖步骤 2。
4. 明确项目边界：ThreeBooks.BookBackend.LoginService 负责认证、账户、凭据、角色、令牌、登录审计；现有 BookBackend 不承载登录业务逻辑，只在需要时调用认证结果或消费 token。该步骤依赖步骤 2，可与步骤 3 同步完成。
5. Phase 2 - Authentication domain design
6. 设计核心实体与关系：User、UserCredential、Role、UserRole、RefreshToken、LoginAttempt、AuditLog；为未来外部登录补充 ExternalIdentity、VerificationCode、AuthProvider 枚举。该步骤依赖步骤 3。
7. 设计认证能力接口：IPasswordAuthService、ITokenService、IRefreshTokenService、IUserService、IRoleService，并预留 IExternalAuthProvider / IAuthProviderFactory 作为后续微信扫码、公众号、短信登录的统一扩展入口。该步骤依赖步骤 6。
8. 设计 API 边界：/auth/register、/auth/login/password、/auth/refresh、/auth/logout、/auth/change-password、/users/me、/roles、/users/{id}/roles；其中第三方登录接口先定义契约和占位端点，不接真实供应商。该步骤依赖步骤 7。
9. Phase 3 - Database and persistence
10. 选用 EF Core 8 + Pomelo.EntityFrameworkCore.MySql，建立 LoginDbContext 和实体映射；统一采用 utf8mb4 字符集、UTC 时间、显式唯一索引。该步骤依赖步骤 6，可与步骤 8 并行推进。
11. 设计首版数据库表：users、user_credentials、roles、user_roles、refresh_tokens、login_attempts、audit_logs，可选 external_identities、verification_codes 先建空能力或延后到二期。该步骤依赖步骤 10。
12. 在新项目中配置 Migrations 目录与设计时 DbContext 工厂，保证可以独立执行 dotnet ef migrations add / database update。该步骤依赖步骤 10。
13. 配置数据库初始化流程：开发环境允许显式执行 migration 和 seed；生产环境不建议自动迁移，改为发布流程执行。该步骤依赖步骤 12。
14. Phase 4 - Security implementation details
15. 密码方案采用 ASP.NET Core PasswordHasher 或等价 PBKDF2/Argon2 安全散列；严禁自定义明文/可逆加密。该步骤依赖步骤 6。
16. Token 方案采用短期 Access Token + 持久化 Refresh Token；Refresh Token 需入库、可吊销、支持轮换，便于多端登录和未来第三方登录统一续期。该步骤依赖步骤 7 和步骤 11。
17. 增加登录安全控制：失败次数限制、临时锁定、审计日志、IP/UA 记录、密码复杂度规则；短信验证码和微信登录在同一风控框架下扩展。该步骤依赖步骤 11，可与步骤 16 并行。
18. 角色权限初版建议使用 RBAC：Role、Permission、UserRole；如果首版权限不复杂，可先只落表 Role/UserRole，并把 Permission 设计留在二期。该步骤依赖步骤 6。
19. Phase 5 - Configuration and integration
20. 新项目配置 appsettings.json / appsettings.Development.json：ConnectionStrings:LoginDb、Jwt、PasswordPolicy、Lockout、SeedAdmin、FeatureFlags。该步骤依赖步骤 2。
21. 开发环境使用 dotnet user-secrets 或环境变量保存数据库密码、JWT Secret、微信/SMS 凭据；禁止提交真实密钥到仓库。该步骤依赖步骤 20。
22. 现有 BookBackend 的处理方式二选一，但推荐先不强耦合：A. 暂不接入，只让 LoginService 独立运行并验证；B. 后续再由 BookBackend 通过 JWT Bearer 校验对接。该步骤依赖步骤 2。
23. 考虑到现有 BookBackend.csproj 已启用 PublishAot=true，新登录服务项目建议初期关闭 AOT，待认证链路、EF Core 和未来第三方 SDK 稳定后再单独评估裁剪/AOT。该步骤依赖步骤 2。
24. Phase 6 - Verification and rollout
25. 编写基础集成验证：注册、密码登录、刷新 token、修改密码、角色分配、锁定策略、重复用户名/手机号约束。该步骤依赖步骤 16、17、18。
26. 使用本地 MySQL 建库并执行初始 Migration，再插入一个管理员种子账号，验证 Swagger 或 HTTP 文件调用链路。该步骤依赖步骤 12、20、21。
27. 对未来能力做架构验收：确认新增 WeChat QR / WeChat OA / SMS 时，只需增加 provider 实现、配置项、数据表补充，而不重写主认证流程。该步骤依赖步骤 7、17。

**Relevant files**
- d:\Codes\book_backend\BookBackend\BookBackend.sln — 新增并纳入 ThreeBooks.BookBackend.LoginService 项目，决定 solution 组织方式。
- d:\Codes\book_backend\BookBackend\BookBackend\BookBackend.csproj — 当前项目已启用 PublishAot=true，需要作为新项目是否启用 AOT 的对照约束。
- d:\Codes\book_backend\BookBackend\BookBackend\Program.cs — 当前仅有 Minimal API 示例，新项目不建议复制其单文件结构，而应拆分为独立分层 Web API。
- d:\Codes\book_backend\BookBackend\BookBackend\appsettings.json — 参考现有配置入口，后续新项目需要加入 MySQL、JWT、锁定策略等配置。
- d:\Codes\book_backend\BookBackend\BookBackend\appsettings.Development.json — 参考开发环境配置方式，但敏感项应改用 user-secrets / 环境变量。

**Verification**
1. 执行 solution restore/build，确认新项目可独立编译，且不会破坏现有 BookBackend。
2. 启动本地 MySQL 8.x，执行 EF Core Migration，确认生成的表、索引、外键、字符集均符合设计。
3. 手工验证接口流程：注册成功、错误密码拒绝、达到失败阈值后锁定、Refresh Token 可轮换、退出登录后旧 Refresh Token 失效。
4. 验证角色权限：普通用户访问受限接口被拒绝，管理员角色可通过。
5. 验证配置安全：仓库中不存在真实数据库密码、JWT Secret、微信/SMS 凭据。
6. 预留能力验证：在不改主登录流程的前提下，能为微信/短信新增 provider 接口和配置节。

**Decisions**
- 已确认新增项目类型为独立 ASP.NET Core Web API 项目，而不是类库。
- 已确认初版令牌方案为 JWT Access Token + Refresh Token。
- 已确认初版范围包含账号密码登录、注册、修改密码、角色权限。
- 已确认数据库方案采用 MySQL，并由 EF Core Migration 管理 schema。
- 推荐新登录服务从一开始就设计 provider 抽象，但微信扫码、公众号、短信登录只做扩展点和数据模型预留，不在首版接真实接口。
- 推荐生产环境不自动执行数据库迁移；迁移应纳入发布流程。
- 推荐初期不为新登录服务启用 Native AOT，以降低认证和外部 SDK 兼容风险。

**Further Considerations**
1. 用户唯一标识建议优先明确：是用户名、手机号、邮箱三选一，还是支持多种登录名并存；推荐首版至少支持 username + mobile 两类唯一键设计。
2. 如果未来会做单点登录或多系统统一鉴权，建议尽早把 Role 之外的 Permission / Scope 模型纳入设计，否则后续迁移成本会上升。
3. 如果后续微信/短信接入节奏较快，建议二期引入 Redis，用于验证码、扫码状态、风控计数和短时 token 缓存。

## Plan: Login Service Windows Service Hosting

为 ThreeBooks.BookBackend.LoginService 增加原生 Windows Service 托管支持，并在服务模式下通过显式配置启用 Swagger，使发布后的服务可以直接通过浏览器访问 Swagger UI。发布流程采用独立的本地文件系统 publish profile，再用 PowerShell 脚本注册或更新系统服务，避免依赖额外的服务包装器。

**Steps**
1. 在 Program.cs 中启用 `UseWindowsService()`，并额外加载 `appsettings.Service.json` 作为服务环境专用配置。
2. 将 Swagger 启用条件从 `IsDevelopment()` 扩展为 `Development` 或 `Swagger:Enabled=true`。
3. 新增 `appsettings.Service.json`，为服务模式提供固定监听地址和 Swagger 开关。
4. 新增 `LocalWindowsService.pubxml`，统一将发布产物输出到 solution 根目录下的 `publish/ThreeBooks.BookBackend.LoginService/local-service/`。
5. 新增 `scripts/Deploy-LocalWindowsService.ps1`，通过原生 Windows Service 注册方式安装、启动、停止、重建服务。

**Verification**
1. 发布到 `publish/ThreeBooks.BookBackend.LoginService/local-service/` 后，目录中应包含 exe、runtimeconfig、`appsettings.Service.json` 与部署脚本。
2. 以管理员身份运行部署脚本执行安装后，Windows 服务列表中应出现 `ThreeBooks.BookBackend.LoginService`。
3. 启动服务后，访问 `http://localhost:5279/swagger` 应能打开 Swagger UI。