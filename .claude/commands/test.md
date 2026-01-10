---
name: test
description: Run Stride tests by category or all tests
---

# Test Command

Run Stride tests.

## Usage

```
/test [filter]
```

## Instructions

### Run All Tests

Use the Stride.build file to run the full test suite:
```bash
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" build\Stride.build /t:RunTestsWindows
```

### Test Categories

The test system supports three categories:
- `Simple` - Core library unit tests (fast, no GPU required)
- `Game` - Graphics and engine tests (requires GPU)
- `VSPackage` - Visual Studio integration tests

To run specific categories:
```bash
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" build\Stride.build /t:RunTestsWindows /p:StrideTestCategories=Simple
```

### Solution Filters

Tests are organized into solution filters in the `build/` directory:
- `Stride.Tests.Simple.slnf` - Core/asset unit tests
- `Stride.Tests.Game.slnf` - Graphics/engine tests
- `Stride.Tests.VSPackage.slnf` - VS integration tests

### Running Individual Test Projects

For faster iteration, build and run a specific test project:
```bash
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" sources\core\Stride.Core.Tests\Stride.Core.Tests.csproj
dotnet test sources\core\Stride.Core.Tests\Stride.Core.Tests.csproj --no-build
```

Report test failures with file locations and suggest fixes when applicable.
