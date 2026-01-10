# Build Command

Build the Stride solution or a specific project.

## Usage

```
/build [project-name]
```

## Instructions

Use MSBuild directly (not dotnet CLI) due to C++/CLI projects.

**MSBuild path:** `C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe`

### Full Solution Build

1. First restore NuGet packages:
```bash
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" /t:Restore build\Stride.sln
```

2. Then build:
```bash
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" build\Stride.sln /p:Configuration=Debug /p:Platform="Mixed Platforms"
```

### Specific Project Build

If a project name is provided, find the .csproj file and build it:
```bash
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" <path-to-project>.csproj /p:Configuration=Debug
```

### Build Targets via Stride.build

For advanced scenarios, use the Stride.build file:
- `/t:Build` - Full build
- `/t:BuildWindows` - Windows platform
- `/t:BuildAndroid` - Android platform
- `/t:BuildiOS` - iOS platform
- `/t:BuildLinux` - Linux platform
- `/t:Package` - Create NuGet packages

Report build errors clearly and suggest fixes when possible.
