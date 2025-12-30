# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

JiksLib is a utility library for Unity game development, written in C# with Chinese documentation. It provides data structures, control flow utilities, and text processing tools with zero external dependencies in the main library.

## Build and Test Commands

### Building
```bash
# Build all projects in the solution
dotnet build JiksLib.sln

# Build specific project
dotnet build JiksLib/JiksLib.csproj
dotnet build JiksLib.Test/JiksLib.Test.csproj
dotnet build JiksLib.UniTask/JiksLib.UniTask.csproj
```

### Testing
```bash
# Run all tests
dotnet test JiksLib.Test/JiksLib.Test.csproj

# Run tests with filter (NUnit filter syntax)
dotnet test --filter "TestCategory=Collections"
dotnet test --filter "MultiDictionary"
dotnet test --filter "Disposable"

# Run specific test class
dotnet test --filter "FullyQualifiedName~JiksLib.Test.Collections.MultiDictionaryTests"
```

### Development Workflow
1. Build the solution: `dotnet build JiksLib.sln`
2. Run tests: `dotnet test JiksLib.Test/JiksLib.Test.csproj`
3. For UniTask integration, build the separate project: `dotnet build JiksLib.UniTask/JiksLib.UniTask.csproj`

## Architecture and Structure

### Solution Structure
- **JiksLib/** - Main library (multi-target: net10.0, net48, netstandard2.1)
- **JiksLib.Test/** - Test project (NUnit, net10.0, net48)
- **JiksLib.UniTask/** - UniTask integration (net10.0 only, depends on UniTask v2.5.10)

### Key Directories and Components

#### Collections (`JiksLib/Collections/`)
- **Cell.cs** - Mutable cell with change events (`OnSet`)
- **MultiDictionary.cs** - Dictionary where each key can have multiple values
- **MultiHashSet.cs** - Hash set that allows duplicate elements
- **OnceCell.cs** - Cell that can only be set once
- **IReadOnlyMultiDictionary.cs** - Read-only interface for MultiDictionary
- **IReadOnlyMultiSet.cs** - Read-only interface for MultiHashSet

#### Control (`JiksLib/Control/`)
- **Disposable.cs** - IDisposable utilities (scope management, merging)
- **UnitType.cs** - Unit type implementation (functional programming concept)

#### Extensions (`JiksLib/Extensions/`)
- **AnythingExtension.cs** - `ThrowIfNull()` extension method for null checking

#### Text (`JiksLib/Text/`)
- **CsvReader.cs** - CSV reading functionality
- **CsvWriter.cs** - CSV writing functionality

### Testing Structure
Test files mirror the main project structure with `Tests` suffix:
- `JiksLib.Test/Collections/` - Collection tests
- `JiksLib.Test/Control/` - Control flow tests
- `JiksLib.Test/Extensions/` - Extension method tests
- `JiksLib.Test/Text/` - Text processing tests

## Code Conventions and Patterns

### Null Safety
- Nullable reference types enabled (`<Nullable>enable</Nullable>`)
- Use `ThrowIfNull()` extension method for parameter validation
- Generic constraints: `where T : notnull` for non-nullable types

### Documentation
- XML documentation in Chinese
- English method and class names
- Comprehensive test documentation

### Design Patterns
1. **Reactive Programming**: `Cell<T>` with value change events
2. **Resource Management**: `Disposable` scope-based patterns
3. **Immutable Interfaces**: Read-only interfaces for collections
4. **Functional Programming**: `UnitType` as a functional concept
5. **Extension Methods**: Utility extensions like `ThrowIfNull()`

### Testing Patterns
- NUnit framework with `[TestFixture]` and `[Test]` attributes
- Arrange-Act-Assert pattern
- Descriptive test names (e.g., `Constructor_WithValue_SetsValue`)
- Chinese test method documentation with English assertions

## Development Notes

### Multi-targeting
The main library targets:
- `.NET 10.0` (modern .NET)
- `.NET Framework 4.8` (Windows compatibility)
- `.NET Standard 2.1` (cross-platform library)

### Dependencies
- **Main Library**: Zero external dependencies
- **Test Project**: NUnit, coverlet, Microsoft.NET.Test.Sdk
- **UniTask Project**: UniTask v2.5.10 (Unity async utilities)

### Code Quality Features
- Comprehensive unit test coverage
- Cloneable collections (`ICloneable` implementation)
- Exception handling with descriptive messages
- Strong typing with generic constraints

### Common Issues and Solutions
1. **Null Reference Exceptions**: Use `ThrowIfNull()` for parameter validation
2. **Multi-target Build Errors**: Ensure all target frameworks are compatible
3. **Test Filtering**: Use NUnit filter syntax for running specific tests
4. **UniTask Integration**: Separate project for Unity-specific async utilities

## Target Audience
- Unity game developers
- Chinese-speaking developers (documentation in Chinese)
- Multi-platform development (Windows, cross-platform via .NET Standard)

## File Reference
- `JiksLib.sln` - Visual Studio solution file
- `JiksLib/JiksLib.csproj` - Main library project file
- `JiksLib.Test/JiksLib.Test.csproj` - Test project file
- `JiksLib.UniTask/JiksLib.UniTask.csproj` - UniTask integration project file
- `README.md` - Project description in Chinese