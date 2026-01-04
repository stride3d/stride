# MSBuild Built-in Variables for .props and .targets Files

This document lists the built-in MSBuild properties (variables) that can be used in `.props` and `.targets` files to identify the project and/or solution being built.

## Project-Related Properties

These properties provide information about the project file being built:

### `$(MSBuildProjectDirectory)`
- **Description**: The absolute path of the directory where the project file is located (without trailing slash)
- **Example**: `C:\Projects\Stride\Engine\stride\sources\core\Stride.Core`
- **Usage**: Useful for constructing paths relative to the project directory

### `$(MSBuildProjectFile)`
- **Description**: The complete file name of the project file, including the file extension
- **Example**: `Stride.Core.csproj`
- **Usage**: Useful when you need to reference the project file name

### `$(MSBuildProjectFullPath)`
- **Description**: The absolute path of the project file, including the complete file name
- **Example**: `C:\Projects\Stride\Engine\stride\sources\core\Stride.Core\Stride.Core.csproj`
- **Usage**: Complete reference to the project file location

### `$(MSBuildProjectName)`
- **Description**: The file name of the project file without the file extension
- **Example**: `Stride.Core`
- **Usage**: Commonly used for naming output directories or files based on the project name

### `$(MSBuildProjectExtension)`
- **Description**: The file extension of the project file, including the period
- **Example**: `.csproj`
- **Usage**: Can be used to determine the project type

## Import File Properties

These properties provide information about the current `.props` or `.targets` file being imported:

### `$(MSBuildThisFileDirectory)`
- **Description**: The absolute path of the directory containing the current `.props` or `.targets` file being imported (with trailing slash)
- **Example**: `C:\Projects\Stride\Engine\stride\sources\targets\`
- **Usage**: **Most commonly used** for constructing relative paths from the location of the `.props`/`.targets` file itself
- **Important**: This is different from `$(MSBuildProjectDirectory)` - it refers to the location of the current imported file, not the project

### `$(MSBuildThisFile)`
- **Description**: The file name of the current `.props` or `.targets` file being imported
- **Example**: `Stride.Core.props`
- **Usage**: Can be used for diagnostics or conditional logic based on the current file

### `$(MSBuildThisFileFullPath)`
- **Description**: The absolute path of the current `.props` or `.targets` file being imported
- **Example**: `C:\Projects\Stride\Engine\stride\sources\targets\Stride.Core.props`
- **Usage**: Complete reference to the current import file

### `$(MSBuildThisFileExtension)`
- **Description**: The file extension of the current `.props` or `.targets` file, including the period
- **Example**: `.props` or `.targets`
- **Usage**: Can be used in conditional logic

### `$(MSBuildThisFileName)`
- **Description**: The file name of the current `.props` or `.targets` file without extension
- **Example**: `Stride.Core`
- **Usage**: Base name of the current import file

## Solution-Related Properties

These properties provide information about the solution file:

### `$(SolutionDir)`
- **Description**: The absolute path of the directory containing the solution file (with trailing slash)
- **Example**: `C:\Projects\Stride\Engine\stride\build\`
- **Availability**: Only available when building through a solution file (`.sln`). Not available when building individual projects directly
- **Usage**: Useful for paths relative to the solution directory
- **Note**: When building without a solution, this property is empty or undefined

### `$(SolutionPath)`
- **Description**: The absolute path of the solution file
- **Example**: `C:\Projects\Stride\Engine\stride\build\Stride.sln`
- **Availability**: Only available when building through a solution file
- **Usage**: Complete reference to the solution file

### `$(SolutionName)`
- **Description**: The file name of the solution file without the file extension
- **Example**: `Stride`
- **Availability**: Only available when building through a solution file
- **Usage**: Commonly used for solution-wide settings or naming conventions
- **Note**: In Stride, this is sometimes set manually in `.props` files when it's not automatically available

### `$(SolutionFileName)`
- **Description**: The complete file name of the solution file, including the extension
- **Example**: `Stride.sln`
- **Availability**: Only available when building through a solution file
- **Usage**: Full solution file name reference

### `$(SolutionExt)`
- **Description**: The file extension of the solution file, including the period
- **Example**: `.sln`
- **Availability**: Only available when building through a solution file
- **Usage**: Can be used in conditional logic

## Common Usage Patterns

### Pattern 1: Relative Path from Import File
```xml
<!-- From the location of this .props file, reference a directory two levels up -->
<PropertyGroup>
  <StrideRootDir>$(MSBuildThisFileDirectory)..\..\</StrideRootDir>
  <StrideDepsDir>$(MSBuildThisFileDirectory)..\..\deps\</StrideDepsDir>
</PropertyGroup>
```

### Pattern 2: Conditional Import Based on Solution Name
```xml
<!-- Import solution-specific build properties if they exist -->
<Import Project="$(MSBuildThisFileDirectory)..\..\build\$(SolutionName).Build.props" 
        Condition="Exists('$(MSBuildThisFileDirectory)..\..\build\$(SolutionName).Build.props')" />
```

### Pattern 3: Fallback for Solution Properties
```xml
<!-- Provide a default when building without a solution -->
<PropertyGroup>
  <SolutionName Condition=" '$(SolutionName)' == '' ">Stride</SolutionName>
</PropertyGroup>
```

### Pattern 4: Project-Relative Output Path
```xml
<PropertyGroup>
  <OutputPath>$(MSBuildProjectDirectory)\bin\$(Configuration)\</OutputPath>
</PropertyGroup>
```

### Pattern 5: Reading Files Relative to Import Location
```xml
<PropertyGroup>
  <SharedAssemblyInfo>$([System.IO.File]::ReadAllText('$(MSBuildThisFileDirectory)..\shared\SharedAssemblyInfo.cs'))</SharedAssemblyInfo>
</PropertyGroup>
```

## Key Differences to Remember

1. **`$(MSBuildProjectDirectory)` vs `$(MSBuildThisFileDirectory)`**:
   - `MSBuildProjectDirectory`: Always points to the project file's directory
   - `MSBuildThisFileDirectory`: Points to the directory of the current `.props`/`.targets` file being evaluated
   - Use `MSBuildThisFileDirectory` in shared `.props`/`.targets` files to reference resources relative to that file's location

2. **Solution Properties Availability**:
   - Solution-related properties (`SolutionDir`, `SolutionName`, etc.) are **only** available when building through a solution
   - When building individual projects directly, these properties will be empty
   - Always provide fallbacks or conditional checks when using solution properties

3. **Trailing Slashes**:
   - `MSBuildThisFileDirectory` and `SolutionDir` **include** a trailing slash
   - `MSBuildProjectDirectory` **does not** include a trailing slash
   - Be consistent with path separators when concatenating paths

## Additional Resources

- [MSBuild Reserved and Well-known Properties](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-reserved-and-well-known-properties)
- [Common MSBuild Project Properties](https://learn.microsoft.com/en-us/visualstudio/msbuild/common-msbuild-project-properties)
- [MSBuild Special Characters](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-special-characters)

## Examples in Stride Codebase

For real-world examples of these properties in use, see:
- [sources/targets/Stride.Core.props](../../sources/targets/Stride.Core.props)
- [sources/targets/Stride.Core.targets](../../sources/targets/Stride.Core.targets)
