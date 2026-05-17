## Plan: Windows 本机部署前后端并通过域名访问

推荐方案：继续沿用仓库现有的 Windows Service 形态部署后端，把 book_frontend 构建为静态站点，用 Windows 上的 Nginx 作为统一入口。Nginx 负责同域名下的静态文件、SPA 路由回退、以及按路径把 `/api/auth/*` 转发到 LoginService:5279，把其余 `/api/*` 转发到 Api:5280。外网可达性先通过“域名 + 路由器 80/443 端口映射”实现；如果后续确认公网条件不足，再切到“域名 + Cloudflare Tunnel”备选路径。当前范围先以 HTTP 打通为目标，HTTPS 作为下一阶段。

**Steps**
1. Phase 0 - 确认外网入口前提。购买并控制一个域名；确认家庭/办公路由器可以把 80/443 转发到当前 Windows 机器；确认运营商未封禁入站 80/443；如果公网 IP 是动态的，同时准备 DDNS 或后续改用 Cloudflare Tunnel。这个步骤完成前，不要开始做正式 DNS 切换。
2. Phase 1 - 固化后端运行前置。梳理 ThreeBooks.BookBackend.Api 与 ThreeBooks.BookBackend.LoginService 的生产配置：数据库连接串、JWT 密钥、对象存储地址、是否开放 Swagger。优先把机器专属覆盖项落在各自的 appsettings.Local.json 或发布目录中的 appsettings.Service.json，而不是修改源码里的默认占位值。此阶段还要确认 LoginService 与 Api 使用的 JWT 发行者/签名密钥一致。
3. Phase 2 - 发布并安装两个后端服务。分别对 Api 与 LoginService 执行 net8.0 发布，发布到固定目录，例如 `D:\services\threebooks\api` 与 `D:\services\threebooks\login`。优先复用仓库自带的 Deploy-LocalWindowsService.ps1 完成服务注册、重装、启动和本机防火墙开放。发布后先只在本机验证 `http://127.0.0.1:5280/swagger` 与 `http://127.0.0.1:5279/swagger`、以及至少一个业务接口和一个认证接口可用。*依赖 1,2*
4. Phase 3 - 构建前端静态站点。执行前端依赖安装与生产构建，确认产物在 book_frontend/dist。由于前端请求基址固定为 `/api`，不需要先改环境变量，只要反向代理规则正确即可。把 `dist` 复制到 Nginx 站点目录，例如 `D:\nginx\html\book_frontend`。*可与 2 并行到“构建完成”为止；正式联调依赖 2*
5. Phase 4 - 配置 Nginx 统一入口。创建一个 server 监听 80，`root` 指向前端 dist；首页和前端路由使用 `try_files $uri $uri/ /index.html`；把 `/api/auth/` 反向代理到 `http://127.0.0.1:5279`；把其余 `/api/` 反向代理到 `http://127.0.0.1:5280`；代理头至少补齐 Host、X-Real-IP、X-Forwarded-For、X-Forwarded-Proto。路径顺序必须先写更具体的 `/api/auth/`，再写通配的 `/api/`，否则认证流量会被错误打到 Api。*依赖 2,3*
6. Phase 5 - 打通域名与公网。将域名 A 记录指向当前网络的公网 IP；在路由器上把 80/443 转发到这台 Windows 机器；在 Windows 防火墙中开放 Nginx 的 80/443 入站；完成后用手机蜂窝网络而不是局域网验证域名访问。如果发现公网 IP 不稳定或运营商限制入站，改走 Cloudflare Tunnel，把域名指到 tunnel，Nginx 继续作为本机源站。*依赖 4*
7. Phase 6 - 上线前联调与回滚准备。验证首页、前端 SPA 路由、账号注册/登录、业务接口、文件访问链路、Swagger 是否按预期暴露。保留 Nginx 配置备份、两个服务的发布目录备份、以及回滚到上一版发布目录的步骤说明。*依赖 5*

**Relevant files**
- `d:\Codes\book_backend\BookBackend\ThreeBooks.BookBackend.Api\Program.cs` — Api 作为 Windows Service 启动，开发态默认 5281，服务态读 service/local 配置
- `d:\Codes\book_backend\BookBackend\ThreeBooks.BookBackend.Api\Extensions\ServiceCollectionExtensions.cs` — Api 的 JWT、控制器、业务模块注册；JWT 密钥缺失会直接阻塞启动
- `d:\Codes\book_backend\BookBackend\ThreeBooks.BookBackend.Api\appsettings.json` — 数据库、JWT、对象存储默认项与占位值
- `d:\Codes\book_backend\BookBackend\ThreeBooks.BookBackend.Api\appsettings.Service.json` — Api 服务态默认监听 `http://0.0.0.0:5280`
- `d:\Codes\book_backend\BookBackend\ThreeBooks.BookBackend.Api\ThreeBooks.BookBackend.Api.csproj` — 发布时复制配置和 SQL 脚本，并可触发本地 Windows Service 自动部署
- `d:\Codes\book_backend\BookBackend\ThreeBooks.BookBackend.Api\scripts\Deploy-LocalWindowsService.ps1` — Api 的服务安装、重装、启动和防火墙配置脚本
- `d:\Codes\book_backend\BookBackend\ThreeBooks.BookBackend.LoginService\Program.cs` — LoginService 作为独立 Windows Service 启动
- `d:\Codes\book_backend\BookBackend\ThreeBooks.BookBackend.LoginService\appsettings.Service.json` — LoginService 服务态默认监听 `http://0.0.0.0:5279`
- `d:\Codes\book_backend\BookBackend\ThreeBooks.BookBackend.LoginService\Api\Controllers\AuthController.cs` — 认证接口真实路由定义，供 Nginx 与前端联调核对
- `d:\Codes\book_frontend\package.json` — 前端构建入口，生产构建命令是 `npm run build`
- `d:\Codes\book_frontend\webpack.config.js` — 产物目录是 `dist`，生产环境仍假定通过 `/api` 访问后端
- `d:\Codes\book_frontend\utils\request.js` — Axios baseURL 固定为 `/api`，决定了必须走同域名反向代理
- `d:\Codes\book_frontend\src\pages\Signin\signin.api.ts` — 前端认证接口封装，需与 LoginService 路由逐项核对
- `d:\Codes\book_frontend\src\pages\Signin\SigninPage.tsx` — 微信登录当前仍是模拟流，账号登录走真实后端接口

**Verification**
1. 在本机验证两个 Windows Service 均为 Running，且 5279/5280 端口监听正常。
2. 直接访问 `http://127.0.0.1:5279/swagger` 与 `http://127.0.0.1:5280/swagger`，确认两个后端均可启动。
3. 在本机通过 Nginx 访问 `http://localhost/`，确认前端首页可用，刷新任意前端路由不会 404。
4. 在本机通过 Nginx 访问认证与业务接口，确认 `/api/auth/*` 命中 LoginService，其他 `/api/*` 命中 Api。
5. 用非局域网网络访问域名，确认首页、接口、登录流程和静态资源加载全部正常。
6. 如果计划后续上 HTTPS，再补做 80/443 证书签发、HTTP 到 HTTPS 跳转、以及浏览器 Mixed Content 检查。

**Decisions**
- 包含范围：book_frontend、ThreeBooks.BookBackend.Api、ThreeBooks.BookBackend.LoginService、Nginx、本机 Windows Service、域名解析、路由器端口映射、HTTP 首次上线验证。
- 不包含范围：数据库迁移/数据初始化细节、对象存储安装、CI/CD 自动化、HTTPS 自动签发细节、云服务器迁移。
- 路由策略采用单域名单入口，而不是让前端与后端分域名。原因是前端生产代码固定使用相对路径 `/api`，单域名可避免新增 CORS 配置。
- 外网方案优先使用路由器端口映射；Cloudflare Tunnel 作为公网条件不足时的备选，而不是第一选择。

**Further Considerations**
1. 认证路径存在潜在不一致：前端 signin.api.ts 当前账号登录走 `/api/auth/login`，而 LoginService AuthController 的密码登录是 `/api/auth/login/password`。上线前必须做一次实际登录联调；如果确实不一致，需要单独改代码后再上线。
2. 微信登录目前仍是页面内模拟流程，SigninPage.tsx 中真实请求被注释掉。首次外网上线应把微信登录视为未交付能力，避免把它纳入验收口径。
3. 如果公网 IP 是动态的，建议同时准备 DDNS 或直接改走 Cloudflare Tunnel，否则域名解析会在 IP 变化后失效。