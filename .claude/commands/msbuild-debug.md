# MSBuild Debug Command

Debug MSBuild issues in the Stride build system.

## Usage

```
/msbuild-debug <project-or-target>
```

## Instructions

Help diagnose MSBuild build issues in Stride's complex build system.

### Diagnostic Commands

1. **Verbose build output:**
```bash
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" <project> /v:diag
```

2. **Binary log for detailed analysis:**
```bash
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" <project> /bl:build.binlog
```

3. **Show property values:**
```bash
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" <project> /p:Configuration=Debug /t:ShowProperties
```

4. **Preprocess to see effective project:**
```bash
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" <project> /pp:preprocessed.xml
```

### Key Build Files to Check

- `sources/Directory.Build.props` - Applied to all projects
- `sources/Directory.Build.targets` - Applied to all projects
- `sources/targets/Stride.props` - Main engine properties
- `sources/targets/Stride.targets` - Main engine targets
- `sources/targets/Stride.Core.props` - Core library props
- `sources/targets/Stride.GraphicsApi.*.targets` - Graphics API selection

### Common Issues

1. **Package restore failures** - Check NuGet.config and package sources
2. **Target framework issues** - Verify TargetFramework(s) in csproj
3. **Import order problems** - Check Sdk attribute and Import elements
4. **Property evaluation** - Properties evaluate differently in props vs targets
5. **Conditional compilation** - Check DefineConstants and Condition attributes

### SDK-Style vs Legacy

The SDK work aims to simplify:
- Legacy: Complex Directory.Build.* + targets/*.props files
- SDK: Clean Sdk="Stride.Sdk" with minimal configuration

Search for the specific error or target, trace through the import chain, and identify the root cause.
