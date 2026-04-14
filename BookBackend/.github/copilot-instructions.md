# Project Guidelines

## Scope
- Default to working in ThreeBooks.BookBackend.LoginService for authentication, account, JWT, role, permission, external auth, EF Core, and Windows Service tasks.
- Treat BookBackend as a separate ASP.NET Core project unless the request explicitly targets it.

## Architecture
- Preserve the layered structure: Api, Application, Contracts, Domain, Infrastructure, Options, Extensions.
- Keep dependency injection and framework wiring in Extensions/ServiceCollectionExtensions.cs.
- Keep startup initialization and migration or seed bootstrapping in Extensions/ApplicationInitializationExtensions.cs.
- Prefer service-layer changes over controller-heavy business logic.

## API Conventions
- Follow the existing controller pattern under Api/Controllers with ApiControllerBase, explicit route prefixes, ProducesResponseType attributes, request or response DTOs, and CancellationToken on async methods.
- Put request and response contracts under Contracts.
- Reuse the existing request context and service orchestration patterns before introducing new abstractions.

## Configuration
- Respect the existing settings layering: appsettings.json, appsettings.Development.json, appsettings.Service.json, appsettings.Local.json, then user secrets or environment variables.
- Never commit real secrets. Treat ConnectionStrings:LoginDb, Jwt, WeChat, and similar sensitive values as external configuration.

## Database And Hosting
- For persistence changes, update LoginDbContext mappings and create EF Core migrations with the local dotnet-ef tool manifest.
- Windows Service publishing uses LocalWindowsService.pubxml, the publish output under publish/ThreeBooks.BookBackend.LoginService/local-service/, and scripts/Deploy-LocalWindowsService.ps1.
- Validate changes with focused commands when practical, usually dotnet build or the relevant dotnet ef or publish command.

## Customization
- Prefer the custom agents under .github/agents for focused tasks such as implementation, review, debugging, API design, and documentation.
- Prefer the slash skills under .github/skills for repeatable workflows such as EF changes, endpoint checklists, publish steps, troubleshooting, and documentation output.