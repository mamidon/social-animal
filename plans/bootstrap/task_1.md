Create solution structure with Clean Architecture layers

This task establishes the foundational .NET solution structure following Clean Architecture principles as outlined in CLAUDE.md. The architecture enforces proper dependency direction with the core domain at the center, free from external dependencies.

## Work to be Done

### Solution and Project Creation
Create the .NET 8.0 solution with four projects representing distinct architectural layers. Each project serves a specific purpose in the Clean Architecture pattern where dependencies flow inward toward the core domain.

Execute the following commands from the project root:
```bash
# Create solution file
dotnet new sln -n SocialAnimal

# Create Core domain library (innermost layer - no external dependencies)
dotnet new classlib -n SocialAnimal.Core -f net8.0

# Create Infrastructure library (implements Core interfaces)
dotnet new classlib -n SocialAnimal.Infrastructure -f net8.0

# Create Web API project (presentation layer and composition root)
dotnet new webapi -n SocialAnimal.Web -f net8.0

# Create Test project (tests all layers)
dotnet new xunit -n SocialAnimal.Tests -f net8.0
```

### Add Projects to Solution
Register all projects with the solution file:
```bash
dotnet sln add SocialAnimal.Core/SocialAnimal.Core.csproj
dotnet sln add SocialAnimal.Infrastructure/SocialAnimal.Infrastructure.csproj  
dotnet sln add SocialAnimal.Web/SocialAnimal.Web.csproj
dotnet sln add SocialAnimal.Tests/SocialAnimal.Tests.csproj
```

### Configure Project References
Establish the dependency hierarchy following Clean Architecture principles. The Core project must remain free of dependencies on other projects in the solution:

```bash
# Infrastructure implements Core interfaces (depends on Core)
dotnet add SocialAnimal.Infrastructure reference SocialAnimal.Core

# Web is the composition root (depends on both Core and Infrastructure)
dotnet add SocialAnimal.Web reference SocialAnimal.Core
dotnet add SocialAnimal.Web reference SocialAnimal.Infrastructure

# Tests can reference all projects for comprehensive testing
dotnet add SocialAnimal.Tests reference SocialAnimal.Core
dotnet add SocialAnimal.Tests reference SocialAnimal.Infrastructure
dotnet add SocialAnimal.Tests reference SocialAnimal.Web
```

### Clean Default Files
Remove the default template files that won't be used:
```bash
rm SocialAnimal.Core/Class1.cs
rm SocialAnimal.Infrastructure/Class1.cs
rm SocialAnimal.Tests/UnitTest1.cs
```

## Relevant Patterns from CLAUDE.md

- **Clean Architecture / Hexagonal Architecture**: Core domain separation with infrastructure layer, ensuring business logic resides in Core layer separated from infrastructure concerns
- **Dependency Direction**: Dependencies point inward toward the core domain - Core has no dependencies, Infrastructure depends on Core, Web depends on both
- **Portals Pattern**: Core will define interfaces (in `Core/Portals/`) that Infrastructure implements, maintaining dependency inversion

## Deliverables

1. **SocialAnimal.sln**: Solution file linking all projects
2. **SocialAnimal.Core**: Class library with no external project dependencies
3. **SocialAnimal.Infrastructure**: Class library referencing only Core
4. **SocialAnimal.Web**: ASP.NET Core Web API referencing Core and Infrastructure
5. **SocialAnimal.Tests**: xUnit test project referencing all other projects
6. **Clean project structure**: No default template files remaining

## Acceptance Criteria

- Solution builds successfully with `dotnet build` command
- Dependency graph shows Core has zero project dependencies
- Infrastructure only depends on Core
- Web depends on both Core and Infrastructure (acting as composition root)
- Tests can access all projects for comprehensive testing
- No compilation errors or warnings
- Project structure enforces Clean Architecture principles through reference configuration