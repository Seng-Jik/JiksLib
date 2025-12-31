# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

JiksLib is a Unity game development utility library with a dual-architecture design:
- **JiksLib.Core**: Unity-independent .NET library targeting `net10.0`, `net48`, and `netstandard2.1`
- **Unity Integration**: Assembly definition files for Unity 2022.3.62f2 with URP support
- **JiksLib.UniTask**: Optional UniTask integration requiring `JIKS_LIB_ENABLE_UNITASK` compile symbol

## Development Commands

### Building and Testing
```bash
# Run all .NET tests (requires .NET SDK 10.0+)
dotnet test JiksLib.Core.Test/

# Build the core library
dotnet build Assets/JiksLib/JiksLib.Core/Runtime/JiksLib.Core.csproj

# Open solution in VS Code (configured as default solution)
code .
```

### Unity Development
```bash
# Unity project is configured for Unity 2022.3.62f2
# Open in Unity Hub or use:
"D:\Program Files\Unity\Hub\Editor\2022.3.62f2\Editor\Unity.exe" -projectPath "D:\JiksLib"
```

## Architecture

### Dual-Target Structure
```
Assets/JiksLib/
├── JiksLib.Core/           # Unity-independent core library
│   └── Runtime/           # Multi-target .NET project (.csproj)
├── JiksLib.UniTask/       # UniTask integration (conditional)
└── package.json          # Unity package metadata

JiksLib.Core.Test/        # .NET test project (NUnit 4.3.2)
```

### Key Configuration Files
- `JiksLib.sln` - Visual Studio solution
- `Assets/JiksLib/JiksLib.Core/Runtime/JiksLib.Core.asmdef` - Unity assembly definition
- `Assets/JiksLib/JiksLib.UniTask/JiksLib.UniTask.asmdef` - UniTask assembly with define constraint
- `Packages/manifest.json` - Unity package dependencies including UniTask
- `.vscode/settings.json` - VS Code configuration with `dotnet.defaultSolution: "JiksLib.sln"`

### Conditional Compilation
- `JIKS_LIB_ENABLE_UNITASK`: Enables UniTask integration features
- Core library uses `LangVersion 9.0`, test project uses `LangVersion 10.0`
- Nullable reference types enabled (`<Nullable>enable</Nullable>`)

## Library Features

### Collections
- `Cell<T>` - Mutable container with change events
- `MultiDictionary` - Dictionary with multiple values per key
- `MultiHashSet` - HashSet with duplicate tracking
- `OnceCell` - Lazy-initialized container
- `IntRange` - Integer range utilities

### Control Flow
- `Disposable` pattern utilities
- `UnitType` - Void type representation

### Extensions
- `AnythingExtension` methods for common operations

### Text Utilities
- CSV reader/writer for data serialization

### UniTask Integration (Optional)
- Async disposable support
- Requires adding `JIKS_LIB_ENABLE_UNITASK` compile symbol
- Reference `JiksLib.UniTask.asmdef` in Unity projects

## Testing Strategy

### .NET Tests
- Located in `JiksLib.Core.Test/`
- Organized by feature area (Collections, Control, Extensions, Text)
- Uses NUnit 4.3.2 with NUnit3TestAdapter
- Code coverage with coverlet.collector

### Test Organization
```
JiksLib.Core.Test/
├── Collections/          # CellTests, MultiDictionaryTests, etc.
├── Control/             # DisposableTests, UnitTypeTests
├── Extensions/          # AnythingExtensionTests
└── Text/               # CsvReaderTests, CsvWriterTests
```

## Package Management

### Unity Package
```json
"com.sengjik.jikslib": "https://github.com/Seng-Jik/JiksLib.git?path=Assets/JiksLib"
```

### .NET NuGet Package
- Package name: `JiksLib.Core`
- Available on NuGet Gallery

## Development Notes

### Code Style
- C# 9.0/10.0 features used where appropriate
- Nullable reference types enabled throughout
- Clean separation between Unity-dependent and independent code

### Dependencies
- **Core**: No external dependencies (pure .NET)
- **Unity**: UniTask via Git URL, URP, and standard Unity packages
- **Testing**: NUnit 4.3.2, Microsoft.NET.Test.Sdk 17.14.0

### Build Targets
- `net10.0`, `net48`, `netstandard2.1` for maximum compatibility
- Unity assembly definitions for clean Unity integration