# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

JiksLib is a Unity game development utility library written in C#. It consists of:
- **JiksLib.Core**: A .NET library with platform-agnostic utilities (no Unity dependencies)
- **Unity Package**: Unity-specific integration and compiled DLL distribution
- **JiksLib.UniTask**: Optional UniTask integration for async operations

The core library supports multiple target frameworks: .NET 10.0, .NET Framework 4.8, and .NET Standard 2.1.

## Architecture

### Core Library Structure
- `JiksLib.Core/Collections/`: Observable containers (Cell, Deque, Range, MultiDictionary, MultiHashSet)
- `JiksLib.Core/Control/`: Disposable patterns and Unit type
- `JiksLib.Core/Extensions/`: Utility extension methods
- `JiksLib.Core/Text/`: CSV reader/writer utilities

### Unity Integration
- `Assets/JiksLib/`: Unity package containing compiled DLL and Unity-specific components
- `Assets/JiksLib/JiksLib.UniTask/`: UniTask integration (requires `JIKS_LIB_ENABLE_UNITASK` compile symbol)

### Key Design Patterns
1. **Cell Pattern**: Observable container with change events (`Cell<T>`)
2. **Extension Methods**: Utility methods for common operations
3. **Disposable Pattern**: Proper resource management
4. **Multi-targeting**: Support for Unity (.NET Framework) and modern .NET

## Development Commands

### Building
```bash
# Build core library for all target frameworks
dotnet build ./JiksLib.Core/JiksLib.Core.csproj

# Build and copy DLL to Unity Assets (for .NET Framework 4.8)
dotnet publish ./JiksLib.Core/JiksLib.Core.csproj -c Release -f net48
copy ./JiksLib.Core/bin/Release/net48/publish/JiksLib.Core.dll ./Assets/JiksLib/JiksLib.Core.dll

# Or use the PowerShell script
./copy-dll.ps1
```

### Testing
```bash
# Run all tests (requires .NET SDK 10 or higher)
dotnet test ./JiksLib.Core.Test/JiksLib.Core.Test.csproj

# Run tests for specific framework
dotnet test ./JiksLib.Core.Test/JiksLib.Core.Test.csproj -f net48
```

### Unity Development
- Unity version: 2022.3.62f2
- The Unity project is in the root directory
- Core library DLL is copied to `Assets/JiksLib/JiksLib.Core.dll`
- Unity package definition: `Assets/JiksLib/package.json`

## Project Configuration

### Solution Files
- `JiksLib.sln`: Main solution (includes all projects)
- `JiksLib.Core.sln`: Core library solution

### Key Configuration Files
- `JiksLib.Core/JiksLib.Core.csproj`: Core library project (multi-targets: net10.0, net48, netstandard2.1)
- `JiksLib.Core.Test/JiksLib.Core.Test.csproj`: Test project (targets: net10.0, net48)
- `Assets/JiksLib/package.json`: Unity package manifest
- `ProjectSettings/ProjectVersion.txt`: Unity version configuration

## Code Standards

- **Nullable Reference Types**: Enabled in all projects
- **Language Version**: C# 9.0 for core library, C# 10.0 for tests
- **Documentation**: XML documentation comments with Chinese/English documentation
- **Testing**: NUnit 4.3.2 with modern testing framework

## Important Notes

1. **Separation of Concerns**: Core library has no Unity dependencies; Unity integration is separate
2. **Multi-targeting**: Library supports both Unity (.NET Framework) and modern .NET
3. **Chinese Documentation**: Many comments and documentation are in Chinese
4. **UniTask Integration**: Optional feature requiring compile symbol `JIKS_LIB_ENABLE_UNITASK`

## Common Workflows

### Adding New Core Features
1. Add code to appropriate namespace in `JiksLib.Core/`
2. Write tests in `JiksLib.Core.Test/`
3. Run `dotnet test` to verify
4. Build and copy DLL to Unity Assets using `copy-dll.ps1`
5. Test in Unity project

### Unity Package Updates
1. Update core library
2. Run `copy-dll.ps1` to update DLL in Assets
3. Update `Assets/JiksLib/package.json` version if needed
4. Test Unity integration

### Testing Strategy
- Core library tests are in `JiksLib.Core.Test/`
- Unity-specific testing should be done in the Unity editor
- Tests use NUnit with modern testing patterns