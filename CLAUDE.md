# CLAUDE.md - EasyNetQ

EasyNetQ is a .NET client API for RabbitMQ. It provides a high-level abstraction over RabbitMQ.Client with pub/sub, RPC, send/receive, scheduling, and more.

## Project Structure

```
EasyNetQ/
├── Assets/                          # Package icon, strong-name key (EasyNetQ.snk)
├── Source/
│   ├── EasyNetQ/                    # Core library (netstandard2.0;net8.0;net9.0;net10.0)
│   ├── EasyNetQ.Serialization.NewtonsoftJson/  # Optional Newtonsoft.Json serializer
│   ├── EasyNetQ.Hosepipe/           # CLI tool for dead-letter message replay
│   ├── EasyNetQ.Tests/              # Unit tests (xUnit, net8.0)
│   ├── EasyNetQ.Serialization.Tests/
│   ├── EasyNetQ.IntegrationTests/   # Docker-based RabbitMQ integration tests
│   ├── EasyNetQ.ApprovalTests/      # Public API surface snapshot tests
│   ├── EasyNetQ.Hosepipe.Tests/
│   ├── EasyNetQ.Examples.*/         # Example projects
│   ├── Directory.Build.props        # Shared MSBuild properties
│   ├── Directory.Packages.props     # Central Package Management (CPM)
│   └── EasyNetQ.slnx               # Solution file (modern .slnx format)
├── .editorconfig                    # Code style rules (enforced in CI)
└── .github/workflows/ci.yml        # CI/CD pipeline
```

## Build & Test

```bash
# Restore
dotnet restore Source/EasyNetQ.slnx

# Build
dotnet build Source/EasyNetQ.slnx --configuration Release

# Run unit tests
dotnet test Source/EasyNetQ.Tests --configuration Release

# Run all tests (integration tests require Docker)
dotnet test Source/EasyNetQ.slnx --configuration Release

# Check formatting (CI enforces this)
dotnet format --verify-no-changes --severity warn Source/EasyNetQ.slnx
```

## Code Style & Conventions

- **Formatting**: Enforced by `.editorconfig` and `dotnet format` in CI
- **Namespaces**: File-scoped (`namespace Foo;`)
- **Usings**: Outside namespace, implicit usings enabled
- **Braces**: New line for all (`csharp_new_line_before_open_brace = all`)
- **var**: Use when type is apparent; use explicit type for built-in types
- **Accessibility**: Always explicit (`public`, `private`, etc.)
- **Readonly**: Enforce `readonly` on fields where possible
- **Naming**: PascalCase for public members, camelCase for private fields (no underscore prefix), `I` prefix for interfaces
- **Null guards**: The core `EasyNetQ` project uses Fody NullGuard for automatic null checks
- **XML docs**: Generated for all public APIs (`GenerateDocumentationFile=true`)

## Architecture

- **DI-first**: All services registered via `IServiceCollection`. Entry point: `services.AddEasyNetQ("host=...")`
- **Builder pattern**: `AddEasyNetQ()` returns `IEasyNetQBuilder` with `Use*()` extension methods
- **Interface-based**: Every service has a corresponding interface for testability/replaceability
- **Middleware pipelines**: Produce and consume paths use ASP.NET Core-style middleware
- **Dual connections**: Separate `IProducerConnection` and `IConsumerConnection` (RabbitMQ best practice)
- **Conventions system**: Delegates for exchange/queue/routing key naming (`IConventions`)
- **Extension methods**: Narrow interfaces extended via static extension method classes

## Dependencies & Versioning

- **Central Package Management**: All versions in `Source/Directory.Packages.props`
- **Versioning**: MinVer (version derived from git tags)
- **Assembly signing**: Strong-named with `Assets/EasyNetQ.snk`
- **Key deps**: RabbitMQ.Client 7.x, Microsoft.Extensions.DI/Logging abstractions

## Testing Conventions

- **Framework**: xUnit 2.x + FluentAssertions + NSubstitute
- **Naming**: Classes `When_<scenario>`, methods `Should_<expected>`
- **MockBuilder**: Central test helper in `EasyNetQ.Tests/Mocking/MockBuilder.cs` wires DI with substituted RabbitMQ infrastructure
- **Integration tests**: Use Docker (RabbitMQ container via `docker.dotnet`)
- **Approval tests**: `PublicApiGenerator` + `Shouldly` to snapshot public API surface
- **Global usings**: Test projects use `GlobalUsings.cs` importing xUnit, FluentAssertions, NSubstitute

## CI/CD

- GitHub Actions (`.github/workflows/ci.yml`)
- Runs on: push to `master`/`N.x`, PRs to `master`/`develop`, version tags
- Steps: restore → format check → build → test (unit + serialization + hosepipe + integration + approval)
- Publish: tag push triggers `dotnet pack` + `dotnet nuget push` to nuget.org
- .NET SDK: 8.x in CI
