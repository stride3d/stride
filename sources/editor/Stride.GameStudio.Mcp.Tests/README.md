# MCP Integration Tests

Integration tests for the MCP server embedded in Stride Game Studio. These tests verify all MCP tools work correctly by connecting to a live Game Studio instance via the MCP client SDK.

Tests are **disabled by default** and must be explicitly opted in, because they require a desktop environment and a pre-built Game Studio.

## Prerequisites

Game Studio must be built before running the tests. The build process requires Visual Studio MSBuild (not `dotnet build`) because of transitive native C++ dependencies, and a NuGet packaging workaround for .NET 10.

A bootstrap script automates all of this:

```powershell
.\sources\editor\Stride.GameStudio.Mcp.Tests\bootstrap.ps1
```

The script will:
1. Locate Visual Studio MSBuild via `vswhere`
2. Build Game Studio with `StrideSkipAutoPack=true`
3. Copy pruned framework DLLs to the build output (workaround for a .NET 10 issue where `Microsoft.Extensions.FileProviders.Abstractions.dll` and related DLLs are missing)
4. Pack the `Stride.GameStudio` NuGet package to the local dev feed (`%LOCALAPPDATA%\Stride\NugetDev`)
5. Build the integration test project

## Running the Tests

```powershell
$env:STRIDE_MCP_INTEGRATION_TESTS = "true"
dotnet test sources\editor\Stride.GameStudio.Mcp.Tests
```

The test fixture automatically:
1. Launches Game Studio with the FirstPersonShooter sample project
2. Polls the MCP SSE endpoint until the server is ready (up to 120 seconds)
3. Runs all 8 integration tests
4. Kills the Game Studio process

## Configuration

| Environment Variable | Default | Description |
|---------------------|---------|-------------|
| `STRIDE_MCP_INTEGRATION_TESTS` | *(unset)* | Set to `true` to enable tests. When unset, all tests are skipped. |
| `STRIDE_MCP_PORT` | `5271` | Port for the MCP server |
| `STRIDE_GAMESTUDIO_EXE` | *(auto-detected)* | Override path to `Stride.GameStudio.exe` |
| `STRIDE_TEST_PROJECT` | *(auto-detected)* | Override path to the `.sln` file to open |

## What the Tests Cover

| Test | Description |
|------|-------------|
| `ListTools_ReturnsAllExpectedTools` | All 4 tools are registered |
| `GetEditorStatus_ReturnsProjectInfo` | Returns project name, solution path, asset count, scenes |
| `QueryAssets_ReturnsAssets` | Returns asset list with id/name/type metadata |
| `QueryAssets_WithTypeFilter_ReturnsOnlyMatchingType` | Type filter works correctly |
| `GetSceneTree_ReturnsEntityHierarchy` | Returns entity hierarchy with components and children |
| `GetSceneTree_WithInvalidId_ReturnsError` | Graceful error for invalid scene ID |
| `GetEntity_ReturnsComponentDetails` | Returns component properties including TransformComponent |
| `GetEntity_WithInvalidEntityId_ReturnsError` | Graceful error for invalid entity ID |

## Troubleshooting

**Tests skip with "Set STRIDE_MCP_INTEGRATION_TESTS=true"**
- Set the environment variable: `$env:STRIDE_MCP_INTEGRATION_TESTS = "true"`

**"Stride.GameStudio.exe not found"**
- Run `bootstrap.ps1` to build Game Studio, or set `STRIDE_GAMESTUDIO_EXE` to an existing build

**"GameStudio exited prematurely"**
- Check that the `Stride.GameStudio` NuGet package exists in `%LOCALAPPDATA%\Stride\NugetDev\`
- Re-run `bootstrap.ps1` to rebuild and repack

**"MCP server did not become ready within 120s"**
- Game Studio may be stuck during project loading. Check the captured stdout/stderr in the error message.
- Ensure no other Game Studio instance is already using port 5271
