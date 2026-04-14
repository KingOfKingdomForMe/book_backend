## Plan: Unified Modular MVC Architecture

推荐采用“统一对外 API 宿主 + Application + Infrastructure + Domain + Contracts”的模块化单体架构，并且对新业务模块从一开始就采用存储过程友好的数据访问边界。控制器保持类 MVC 形式，业务逻辑集中在 Application Service，数据库访问统一通过 Application 定义的 Repository / Query / Command 接口进入 Infrastructure。鉴于你已经明确未来核心接口大概率最终都要走存储过程，建议新主业务 API 不再让业务层直接依赖 DbContext；相反，使用 Dapper + ADO.NET 作为主数据访问实现，EF Core 仅在现有 LoginService 过渡期和少量结构管理场景保留。

**Steps**
1. Phase 1 - 明确最终宿主与过渡宿主。新增统一对外宿主 `ThreeBooks.BookBackend.Api`，作为未来相册模板、相册书架、订单、晒单评论、个人数据等模块的统一入口；现有 `ThreeBooks.BookBackend.LoginService` 继续作为独立认证宿主保留一段时间，直到共享层稳定后再决定是否并入。不要继续把现有 `BookBackend` 作为最终主宿主演进，因为它当前是极简 Web 项目且启用了 PublishAot，不适合作为长期承载大量外部接口的核心宿主。该步骤阻塞后续所有拆分。
2. Phase 2 - 建立 5 个核心项目，先把解决方案边界定清。推荐项目清单为：`ThreeBooks.BookBackend.Api`，`ThreeBooks.BookBackend.Application`，`ThreeBooks.BookBackend.Infrastructure`，`ThreeBooks.BookBackend.Domain`，`ThreeBooks.BookBackend.Contracts`。这是第一阶段的固定粒度，不要一开始就按 10 个业务域继续拆出更多工程。该步骤依赖步骤 1。
3. 明确项目引用方向，避免以后出现循环依赖。推荐依赖方向为：`Api -> Application, Infrastructure, Contracts`；`Application -> Domain, Contracts`；`Infrastructure -> Application, Domain, Contracts`；`Domain` 不引用任何项目；`Contracts` 不引用任何项目。如果某个 DTO 需要枚举，优先把枚举复制为 Contracts 内部枚举或字符串常量，而不是让 Contracts 依赖 Domain。该步骤依赖步骤 2。
4. Phase 3 - 确定每个项目的职责边界。`ThreeBooks.BookBackend.Api` 只放 Controller、鉴权、中间件、异常处理、模型绑定、Swagger、DI 组合与宿主启动；`ThreeBooks.BookBackend.Application` 只放业务服务、接口定义、业务规则、验证、模块内协调与业务模型；`ThreeBooks.BookBackend.Infrastructure` 只放数据库访问实现、外部依赖调用、SQL 执行器、连接工厂、事务封装、种子与持久化脚本管理；`ThreeBooks.BookBackend.Domain` 放纯领域实体、值对象、业务枚举；`ThreeBooks.BookBackend.Contracts` 放 Request、Response、分页模型、错误响应与对外契约。该步骤依赖步骤 2，可与步骤 5 并行。
5. 把现有 LoginService 的已有分层映射到未来结构。当前的 `Api/Controllers`、`Application/Abstractions`、`Application/Services`、`Infrastructure/Persistence`、`Domain`、`Contracts`、`Extensions` 已经提供了拆分模板。具体上：`AuthController` 和 `UsersController` 代表未来薄控制器风格；`ServiceCollectionExtensions.AddLoginService()` 代表未来宿主组合入口；`ApplicationInitializationExtensions.InitializeLoginServiceAsync()` 代表启动初始化流程；`ServiceContracts.cs` 中的 `RequestContext` 与服务接口代表 Application 的典型边界。该步骤依赖步骤 2，可与步骤 4 并行。
6. Phase 4 - 在 Application 中采用“按业务域组织”的结构，而不是继续只有技术分层。推荐模块目录为 `Modules/Auth`、`Modules/AlbumTemplates`、`Modules/AlbumShelf`、`Modules/Orders`、`Modules/Reviews`、`Modules/Profiles`。每个模块内部再按职责分成 `Interfaces`、`Services`、`Models`、`Validators`、`Mappings`，但保持轻量，不要泛滥拆文件。该步骤依赖步骤 4。
7. 对 Api 宿主采用按模块分目录的控制器布局。推荐在 `ThreeBooks.BookBackend.Api` 中使用 `Controllers/Auth`、`Controllers/AlbumTemplates`、`Controllers/AlbumShelf`、`Controllers/Orders`、`Controllers/Reviews`、`Controllers/Profiles` 这样的目录组织，让所有对外接口仍然集中在一个宿主，但在文件结构上具备明确模块边界。当前先不引入版本号前缀，路径继续保持 `/api/...`；当 2 到 3 个模块稳定后，再统一切到 `/api/v1/...`。该步骤依赖步骤 4 和步骤 6。
8. Phase 5 - 数据访问边界必须按“业务语义”定义，而不是按 EF 细节定义。Application 中不要暴露 `DbContext`、`DbSet`、`IQueryable`、`FromSql`、事务对象等基础设施细节。为每个模块定义面向业务的接口，例如 `IAlbumTemplateRepository`、`IAlbumTemplateQueryStore`、`IAlbumTemplateCommandStore`、`IOrderRepository`、`IProfileQueryStore`。简单聚合的增删改可用 Repository 风格；复杂分页、聚合查询、统计和列表检索优先用 QueryStore 风格；需要明确状态变更的写操作可用 CommandStore 风格。该步骤依赖步骤 6。
9. 避免引入泛型仓储作为总入口。不要设计 `IRepository<T>` 来覆盖所有模块，因为你已经明确未来核心接口大概率要走存储过程，这种抽象通常既遮不住 SQL 复杂性，也会让调用层继续依赖通用查询表达式。建议按模块定义专用接口，让每个模块可以独立从 EF 过渡到 Dapper 存储过程实现。该步骤依赖步骤 8。
10. Phase 6 - Infrastructure 采用 Dapper + ADO.NET 作为新模块的主实现路线。新增的统一主 API 模块建议从第一天开始就用 Dapper + ADO.NET 访问 MySQL 存储过程，而不是先用 EF 再整体切换。Infrastructure 内建议至少划分为 `Persistence/Connections`、`Persistence/Executors`、`Persistence/Repositories`、`Persistence/Queries`、`Persistence/Commands`、`Persistence/Transactions`、`External`、`Sql`。其中连接工厂负责打开连接，执行器负责统一封装存储过程调用，模块仓储或 QueryStore 只关心入参、结果映射与事务边界。该步骤依赖步骤 8 和步骤 9。
11. 现有 LoginService 则采用过渡策略。现有认证宿主可以继续使用 EF Core 与 `LoginDbContext`，因为它已经成型并包含迁移、种子、Windows Service 部署等能力；但如果后续要把 Auth 也并入统一主 API，届时也应先在 Application 层补齐仓储与查询接口，再考虑替换基础实现。该步骤依赖步骤 5，可与步骤 10 并行。
12. Phase 7 - 为 SQL 脚本建立强版本管理规则。建议在 `ThreeBooks.BookBackend.Infrastructure` 内建立 `Sql` 目录，按模块再细分为 `AlbumTemplates`、`AlbumShelf`、`Orders`、`Reviews`、`Profiles`，每个模块下再按 `Procedures`、`Views`、`Seeds` 分类。命名上统一采用清晰的过程名，例如基于模块和动作命名；仓库中必须能从一个 Application 接口一路追踪到对应实现类和对应 SQL 文件。该步骤依赖步骤 10。
13. 明确结构变更职责。若某模块彻底走存储过程优先，推荐数据库表结构和索引变更也通过仓库 SQL 脚本管理，而不是继续完全依赖 EF Migration；如果某些共享基础表暂时仍由 EF 管理，必须在文档中明确“哪些对象由 EF Migration 负责，哪些对象由 SQL 脚本负责”，禁止双轨同时修改同一对象。该步骤依赖步骤 12。
14. Phase 8 - 统一 DI 组装方式，保持宿主简洁。参考现有 `ServiceCollectionExtensions.AddLoginService()` 的思路，在新主宿主中建立 `AddBookBackendApplication()` 和 `AddBookBackendInfrastructure()` 这样的组合入口；Api 项目只负责调用这些扩展并挂接鉴权、中间件、异常处理、Swagger 与模块路由。不要把数据库连接字符串解析、SQL 执行器组装、外部 HTTP 客户端注册散落到控制器附近。该步骤依赖步骤 4、步骤 6、步骤 10。
15. 对横切关注点做统一收口。`RequestContext` 建议保留在 Application 的公共模型中，因为控制器要构建它，服务要消费它；`ClaimsPrincipalExtensions`、鉴权策略、JWT 配置、中间件、异常处理则保留在 Api 宿主。`PasswordPolicy`、锁定策略等纯业务规则配置保留在 Application；纯宿主鉴权配置如 JWT Bearer 验证则放 Api。该步骤依赖步骤 14。
16. Phase 9 - 首个样板模块选择“相册模板”。这是一个合适的中等复杂度模块，足够覆盖列表查询、详情查询、创建、更新、状态变更、分页筛选等典型场景，但又不会像订单那样过早引入复杂事务和支付回调。先用它走通完整模板：Controller、Service、Repository/QueryStore、Dapper 存储过程实现、SQL 脚本、Swagger、鉴权与验证。该步骤依赖步骤 7、步骤 8、步骤 10。
17. 为“相册模板”定义第一版模块模板。Api 层放 `AlbumTemplatesController`；Application 层放 `IAlbumTemplateService`、`IAlbumTemplateRepository`、`IAlbumTemplateQueryStore`、`AlbumTemplateService`、筛选模型与验证器；Infrastructure 层放 `AlbumTemplateRepository`、`AlbumTemplateQueryStore`、SQL 参数映射与结果映射；Contracts 层放 `CreateAlbumTemplateRequest`、`UpdateAlbumTemplateRequest`、`AlbumTemplateListItemResponse`、`AlbumTemplateDetailResponse`、分页请求和分页响应；Domain 层放 `AlbumTemplate` 以及必要枚举和值对象。该步骤依赖步骤 16。
18. Phase 10 - 再按业务复杂度扩展其他模块。推荐顺序为：`相册模板` -> `用户个人数据` -> `相册书架` -> `晒单评论` -> `用户订单`。订单模块放在较后位置，因为它更可能需要更严格的事务设计、状态机、支付或库存相关逻辑，适合在模块模板稳定后再进入。该步骤依赖步骤 16。
19. 为认证集成预留两种后续路径。路径 A：继续保持 `ThreeBooks.BookBackend.LoginService` 为独立认证宿主，新主业务 API 通过 JWT 验证与内部调用集成；路径 B：待共享层成熟后，把 Auth 模块迁入 `ThreeBooks.BookBackend.Api`，并保留单独的认证数据访问实现。当前推荐先按路径 A 规划，避免在主业务架构尚未稳定时同时做宿主合并。该步骤依赖步骤 11、步骤 14。
20. Phase 11 - 约定未来继续拆工程的阈值。只有当某个模块同时满足独立发布、独立数据库、独立团队维护、构建显著拖慢或安全边界必须隔离中的至少两项时，才考虑把它升级为自己的 `*.Application` 与 `*.Infrastructure` 工程。否则当前 5 项目结构已经足够承载 10 组左右业务域。该步骤依赖步骤 18。

**Relevant files**
- `d:\Codes\book_backend\BookBackend\BookBackend.sln` — 当前 solution 边界；后续需要新增 `ThreeBooks.BookBackend.Api`、`ThreeBooks.BookBackend.Application`、`ThreeBooks.BookBackend.Infrastructure`、`ThreeBooks.BookBackend.Domain`、`ThreeBooks.BookBackend.Contracts`。
- `d:\Codes\book_backend\BookBackend\BookBackend\BookBackend.csproj` — 当前极简 Web 项目且启用 AOT，建议不要直接作为未来主宿主继续演进。
- `d:\Codes\book_backend\BookBackend\ThreeBooks.BookBackend.LoginService\Program.cs` — 现有宿主启动顺序参考，包括配置叠加、鉴权、中间件和初始化调用。
- `d:\Codes\book_backend\BookBackend\ThreeBooks.BookBackend.LoginService\Extensions\ServiceCollectionExtensions.cs` — `AddLoginService()` 是未来统一宿主组合 DI 的直接参考模式。
- `d:\Codes\book_backend\BookBackend\ThreeBooks.BookBackend.LoginService\Extensions\ApplicationInitializationExtensions.cs` — `InitializeLoginServiceAsync()` 是初始化、迁移、seed 流程收口的参考。
- `d:\Codes\book_backend\BookBackend\ThreeBooks.BookBackend.LoginService\Api\Controllers\AuthController.cs` — 薄控制器风格的现有模板。
- `d:\Codes\book_backend\BookBackend\ThreeBooks.BookBackend.LoginService\Api\Controllers\UsersController.cs` — 受权接口与管理类控制器的现有模板。
- `d:\Codes\book_backend\BookBackend\ThreeBooks.BookBackend.LoginService\Application\Abstractions\ServiceContracts.cs` — 现有 `RequestContext`、`AccessTokenDescriptor` 和服务接口定义，适合作为 Application 公共边界的出发点。
- `d:\Codes\book_backend\BookBackend\ThreeBooks.BookBackend.LoginService\Application\Services\AuthService.cs` — 当前业务服务直接依赖 `LoginDbContext` 的典型例子，也是未来需要避免复制到新模块中的模式。
- `d:\Codes\book_backend\BookBackend\ThreeBooks.BookBackend.LoginService\Infrastructure\Persistence\LoginDbContext.cs` — 现有 EF Core 持久化集中点，适合作为认证宿主过渡实现参考，而不是新主业务模块的长期默认模式。
- `d:\Codes\book_backend\BookBackend\ThreeBooks.BookBackend.LoginService\Options\LoginServiceOptions.cs` — 当前业务规则配置与宿主配置混放，后续可作为拆分 Application 配置与 Api 配置的依据。
- `d:\Codes\book_backend\BookBackend\ThreeBooks.BookBackend.LoginService\scripts\Deploy-LocalWindowsService.ps1` — 认证宿主若继续独立保留，需要继续沿用当前发布与部署链路。

**Verification**
1. 先做样板工程验证：创建 5 项目结构后，只实现“相册模板”一个模块，确认 Controller 只依赖 Service，Service 只依赖接口，数据库调用只发生在 Infrastructure。
2. 验证引用方向：`Application` 不能引用 `Infrastructure`，`Contracts` 不能引用 `Domain`，`Domain` 保持纯净。
3. 验证存储过程闭环：从 `IAlbumTemplateQueryStore` 到 `AlbumTemplateQueryStore` 再到 SQL 脚本文件应当可以清晰追踪，且切换实现时不影响 Controller 与 Service 签名。
4. 验证统一宿主能力：新 `ThreeBooks.BookBackend.Api` 能同时承载至少一个业务模块，并保持 Swagger、异常处理、鉴权与配置加载清晰可维护。
5. 验证认证过渡兼容：现有 `ThreeBooks.BookBackend.LoginService` 继续可以独立构建、运行和发布，不因为主业务架构演进而立即受阻。
6. 验证 SQL 管理纪律：数据库对象脚本、应用层接口、基础设施实现三者版本同步，不允许只改数据库而不更新仓库接口定义。

**Decisions**
- 已确认未来统一主宿主命名采用 `ThreeBooks.BookBackend.Api`。
- 已确认当前首选方案为一个统一宿主加四个共享类库，而不是按业务域先拆多个独立 API 宿主。
- 已确认当前 `BookBackend` 项目会逐步弱化或废弃，不建议作为最终主架构核心继续扩展。
- 已确认外部接口预计约 10 组，适合先用模块化单体承载。
- 已确认你偏好的开发外观是 `Controller + Service + Repository`，因此方案采用 MVC 外观而不是过度 CQRS 化。
- 已确认未来核心接口大概率最终都要走存储过程，且查询与写入都可能切换。
- 已确认新主业务模块的数据访问技术推荐为 `Dapper + ADO.NET`。
- 已确认统一的请求响应契约需要抽到共享 `Contracts` 项目中。
- 已确认当前先不做 API 版本化，待模块稳定后再统一引入。
- 已确认首个样板模块选择 `相册模板`。
- 已确认认证模块当前先保持独立宿主，后续再决定是否并入统一主 API。

**Further Considerations**
1. 对新主业务 API，建议从一开始就把 SQL 结果映射和参数组装封装在 Infrastructure 中，避免 Service 层直接出现 Dapper 参数对象和 SQL 细节。
2. 当订单模块进入实现阶段时，应单独补一份事务与状态流设计计划，因为它很可能成为第一个超出普通模板边界的模块。
3. 如果未来认证最终并入统一主宿主，再单独做一次“鉴权与会话边界”审视，避免把认证敏感逻辑与普通业务模块耦合得过紧。