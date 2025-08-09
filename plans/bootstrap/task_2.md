Establish Clean Architecture folder organization

This task creates the internal folder structure within each project following the Clean Architecture principles and naming conventions defined in CLAUDE.md. The folder organization enforces separation of concerns and maintains clear boundaries between architectural layers.

## Work to be Done

### SocialAnimal.Core Folder Structure
The Core project contains the business domain and defines contracts without implementation details. Create folders that align with the patterns in CLAUDE.md:

```
SocialAnimal.Core/
├── Domain/           # Domain models (*Record types) and value objects
├── Services/         # Business logic services (*Service classes)
├── Portals/          # Interface definitions for external dependencies
├── Messages/         # Message types (*Message classes) for async processing
├── Repositories/     # Repository interface contracts (*Repo interfaces)
└── Stubs/           # DTOs for creation/updates (*Stub types)
```

Execute from SocialAnimal.Core directory:
```bash
mkdir Domain Services Portals Messages Repositories Stubs
```

The Core folders serve specific purposes according to CLAUDE.md patterns:
- **Domain**: Houses `*Record` types using record syntax with required properties
- **Services**: Contains `*Service` classes implementing business logic
- **Portals**: Defines interfaces following the Portals Pattern for dependency inversion
- **Messages**: Strongly-typed `*Message` classes for message-driven architecture
- **Repositories**: `I*Repo` interfaces including `ICrudRepo<T>` and domain-specific repos
- **Stubs**: `*Stub` types for data transfer in creation/update operations

### SocialAnimal.Infrastructure Folder Structure
Infrastructure implements the interfaces defined in Core and handles all I/O operations:

```
SocialAnimal.Infrastructure/
├── Db/                      # Database layer following CLAUDE.md pattern
│   ├── Context/            # ApplicationContext and configuration
│   ├── Entities/           # Database entity classes
│   ├── Migrations/         # Entity Framework migrations
│   └── Converters/         # IInto<T> and IFrom<T> implementations
├── Repositories/            # Repository pattern implementations
├── CloudEnvironment/        # External service integrations (future)
├── Messaging/              # Message publisher/subscriber implementations
│   ├── Publishers/         # IMessagePublisher implementations
│   └── Subscribers/        # IMessageSubscriber<T> implementations
└── Portals/                # Portal interface implementations
```

Execute from SocialAnimal.Infrastructure directory:
```bash
mkdir -p Db/Context Db/Entities Db/Migrations Db/Converters
mkdir -p Repositories 
mkdir -p CloudEnvironment
mkdir -p Messaging/Publishers Messaging/Subscribers
mkdir -p Portals
```

### SocialAnimal.Web Folder Structure
The Web project serves as the presentation layer and composition root:

```
SocialAnimal.Web/
├── Controllers/         # *Controller classes following REST conventions
├── Configuration/       # DI setup and service registration modules
│   └── Modules/        # Static module classes for DI registration
├── Middleware/         # Custom ASP.NET Core middleware
└── Models/            # API-specific view models and DTOs
```

Execute from SocialAnimal.Web directory:
```bash
mkdir -p Controllers
mkdir -p Configuration/Modules
mkdir -p Middleware
mkdir -p Models
```

### SocialAnimal.Tests Folder Structure
Organize tests following the patterns for integration and unit testing:

```
SocialAnimal.Tests/
├── Integration/              # Integration tests with real database
│   ├── Controllers/         # API endpoint tests
│   ├── Repositories/        # Repository integration tests
│   └── Services/           # Service integration tests
├── Unit/                    # Unit tests with mocks
│   ├── Services/           # Service unit tests
│   ├── Repositories/       # Repository unit tests
│   └── Converters/         # Mapping unit tests
├── Utilities/              # Test utilities and builders
│   ├── Builders/           # Test data builders
│   └── Fixtures/           # Test fixtures and scenarios
└── TestData/               # Reusable test data
```

Execute from SocialAnimal.Tests directory:
```bash
mkdir -p Integration/Controllers Integration/Repositories Integration/Services
mkdir -p Unit/Services Unit/Repositories Unit/Converters
mkdir -p Utilities/Builders Utilities/Fixtures
mkdir -p TestData
```

## Relevant Patterns from CLAUDE.md

- **Feature-Based Structure**: Organize code by business capabilities within each layer
- **Suffix-Based Classification**: Use consistent suffixes (*Record, *Service, *Repo, *Controller)
- **Clean Architecture**: Maintain clear separation between Core, Infrastructure, and Web layers
- **Repository Pattern**: Separate interfaces (Core) from implementations (Infrastructure)
- **Entity-Record Mapping**: Use IInto<T> and IFrom<T> patterns in Db/Converters

## Deliverables

1. Core project with Domain, Services, Portals, Messages, Repositories, and Stubs folders
2. Infrastructure project with Db, Repositories, CloudEnvironment, Messaging, and Portals folders
3. Web project with Controllers, Configuration/Modules, Middleware, and Models folders
4. Tests project with Integration, Unit, Utilities, and TestData folders
5. All folders properly nested according to the architecture patterns

## Acceptance Criteria

- All specified folders exist in their respective projects
- Folder structure matches CLAUDE.md architectural patterns
- No default template files remain (Class1.cs, UnitTest1.cs removed)
- Directory structure supports Clean Architecture dependency flow
- Folder organization enables feature-based development within each layer
- Structure supports both generic CRUD operations and domain-specific implementations