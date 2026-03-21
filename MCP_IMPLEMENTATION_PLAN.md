# Stride3D MCP Server - Complete Implementation Plan

## Project Vision

Build a Model Context Protocol (MCP) server integrated into Stride3D Game Studio that enables full programmatic control of the editor through LLM agents. The server will support multiple concurrent GameStudio instances and provide comprehensive read/write access to all editor state.

## Scope & Capabilities

### 1. State Reading
- **Assets**: Enumerate, inspect metadata, read content
- **Scenes**: List open scenes, read scene hierarchy
- **Entities**: Inspect entity tree, read transforms, read all components
- **Components**: Read all component properties and values
- **Projects**: Read project structure, dependencies, build configuration
- **UI/Sprites**: Read UI pages, sprite sheet definitions

### 2. Navigation & Visualization
- **Scene Navigation**: Open scenes, focus on entities, navigate hierarchy
- **Asset Navigation**: Open folders, filter assets, search
- **Viewport Control**: Pan, zoom, rotate camera
- **Screenshot Capture**: Capture viewports, UI panels, sprite sheets
- **Selection**: Select entities, components, assets

### 3. Modification
- **Entity Operations**: Create, delete, move, parent/unparent
- **Component Operations**: Add, remove, modify component properties
- **Transform Operations**: Move, rotate, scale entities
- **Asset Operations**: Import, delete, rename assets
- **UI Editing**: Modify UI pages, add/remove elements
- **Sprite Sheet Editing**: Edit sprite definitions, adjust frames

### 4. Lifecycle Management
- **Studio Control**: Launch, close, restart GameStudio
- **Project Management**: Open, create, close projects
- **Build Operations**: Build, clean, package projects
- **Multi-Instance**: Support multiple GameStudio instances simultaneously

## Architecture Overview

```
┌────────────────────────────────────────────────────────────┐
│                    MCP Client (Claude)                     │
└────────────────┬───────────────────────────────────────────┘
                 │ JSON-RPC over stdio
                 ▼
┌────────────────────────────────────────────────────────────┐
│              MCP Server Launcher Process                   │
│  - Manages multiple GameStudio instances                   │
│  - Routes requests to appropriate instance                 │
│  - Handles instance lifecycle                              │
└────────┬───────────────────────────┬───────────────────────┘
         │                           │
         ▼                           ▼
┌─────────────────────┐    ┌─────────────────────┐
│  GameStudio #1      │    │  GameStudio #2      │
│  ┌───────────────┐  │    │  ┌───────────────┐  │
│  │ MCP Plugin    │  │    │  │ MCP Plugin    │  │
│  │ - Tools       │  │    │  │ - Tools       │  │
│  │ - Services    │  │    │  │ - Services    │  │
│  │ - IPC Client  │◄─┼────┼─►│ - IPC Client  │  │
│  └───────────────┘  │    │  └───────────────┘  │
│  ┌───────────────┐  │    │  ┌───────────────┐  │
│  │ GameStudio VM │  │    │  │ GameStudio VM │  │
│  │ - Session     │  │    │  │ - Session     │  │
│  │ - Scenes      │  │    │  │ - Scenes      │  │
│  │ - Assets      │  │    │  │ - Assets      │  │
│  └───────────────┘  │    │  └───────────────┘  │
└─────────────────────┘    └─────────────────────┘
```

## Project Structure

```
sources/
├── editor/
│   ├── Stride.Assets.Presentation.MCP/           # NEW: MCP Plugin for GameStudio
│   │   ├── Stride.Assets.Presentation.MCP.csproj
│   │   ├── Plugin/
│   │   │   ├── McpPlugin.cs                      # Plugin entry point
│   │   │   ├── McpPluginSettings.cs              # Plugin configuration
│   │   │   └── IpcClient.cs                      # IPC with launcher
│   │   ├── Tools/
│   │   │   ├── Base/
│   │   │   │   ├── IStrideTool.cs                # Tool interface
│   │   │   │   ├── StrideToolBase.cs             # Base implementation
│   │   │   │   └── ToolResponse.cs               # Common response types
│   │   │   ├── State/                            # Read-only operations
│   │   │   │   ├── Assets/
│   │   │   │   │   ├── ListAssetsTool.cs
│   │   │   │   │   ├── GetAssetMetadataTool.cs
│   │   │   │   │   └── SearchAssetsTool.cs
│   │   │   │   ├── Scenes/
│   │   │   │   │   ├── ListScenesTool.cs
│   │   │   │   │   ├── GetSceneTreeTool.cs
│   │   │   │   │   └── GetEntityDetailsTool.cs
│   │   │   │   ├── Components/
│   │   │   │   │   ├── GetComponentsTool.cs
│   │   │   │   │   └── GetComponentDataTool.cs
│   │   │   │   ├── Project/
│   │   │   │   │   ├── GetProjectInfoTool.cs
│   │   │   │   │   └── GetBuildConfigTool.cs
│   │   │   │   └── UI/
│   │   │   │       ├── GetUIPageTool.cs
│   │   │   │       └── GetSpriteSheetTool.cs
│   │   │   ├── Navigation/                       # Navigation operations
│   │   │   │   ├── OpenSceneTool.cs
│   │   │   │   ├── SelectEntityTool.cs
│   │   │   │   ├── FocusEntityTool.cs
│   │   │   │   ├── OpenAssetFolderTool.cs
│   │   │   │   └── CaptureScreenshotTool.cs
│   │   │   ├── Modification/                     # Write operations
│   │   │   │   ├── Entities/
│   │   │   │   │   ├── CreateEntityTool.cs
│   │   │   │   │   ├── DeleteEntityTool.cs
│   │   │   │   │   ├── MoveEntityTool.cs
│   │   │   │   │   └── ReparentEntityTool.cs
│   │   │   │   ├── Components/
│   │   │   │   │   ├── AddComponentTool.cs
│   │   │   │   │   ├── RemoveComponentTool.cs
│   │   │   │   │   └── UpdateComponentTool.cs
│   │   │   │   ├── Transforms/
│   │   │   │   │   ├── SetPositionTool.cs
│   │   │   │   │   ├── SetRotationTool.cs
│   │   │   │   │   └── SetScaleTool.cs
│   │   │   │   ├── Assets/
│   │   │   │   │   ├── ImportAssetTool.cs
│   │   │   │   │   ├── DeleteAssetTool.cs
│   │   │   │   │   └── RenameAssetTool.cs
│   │   │   │   └── UI/
│   │   │   │       ├── EditUIPageTool.cs
│   │   │   │       └── EditSpriteSheetTool.cs
│   │   │   └── Lifecycle/                        # Lifecycle operations
│   │   │       ├── OpenProjectTool.cs
│   │   │       ├── CreateProjectTool.cs
│   │   │       ├── CloseProjectTool.cs
│   │   │       ├── BuildProjectTool.cs
│   │   │       └── GetBuildStatusTool.cs
│   │   ├── Services/
│   │   │   ├── SceneAccessService.cs             # Thread-safe scene access
│   │   │   ├── AssetAccessService.cs             # Thread-safe asset access
│   │   │   ├── EditorStateService.cs             # Track editor state
│   │   │   ├── SelectionService.cs               # Manage selections
│   │   │   ├── ScreenshotService.cs              # Capture screenshots
│   │   │   └── UndoRedoService.cs                # Undo/redo integration
│   │   └── Models/
│   │       ├── EntityData.cs                     # Serializable entity data
│   │       ├── ComponentData.cs                  # Serializable component data
│   │       ├── AssetData.cs                      # Serializable asset data
│   │       └── InstanceInfo.cs                   # GameStudio instance info
│   │
│   ├── Stride.GameStudio.McpLauncher/            # NEW: MCP Launcher
│   │   ├── Stride.GameStudio.McpLauncher.csproj
│   │   ├── Program.cs                            # Main entry point
│   │   ├── McpServer/
│   │   │   ├── McpServerService.cs               # MCP protocol handler
│   │   │   ├── StdioTransport.cs                 # stdio transport
│   │   │   └── ToolRouter.cs                     # Route to instances
│   │   ├── InstanceManager/
│   │   │   ├── GameStudioInstanceManager.cs      # Manage instances
│   │   │   ├── GameStudioInstance.cs             # Instance wrapper
│   │   │   └── IpcServer.cs                      # IPC with plugins
│   │   └── Configuration/
│   │       └── LauncherConfig.cs                 # Configuration
│   │
│   └── Stride.GameStudio/
│       └── Stride.GameStudio.csproj              # MODIFIED: Add plugin reference
│
└── tests/
    ├── Stride.Assets.Presentation.MCP.Tests/     # NEW: Unit tests
    │   ├── Stride.Assets.Presentation.MCP.Tests.csproj
    │   ├── Tools/
    │   │   ├── State/
    │   │   │   ├── ListAssetsToolTests.cs
    │   │   │   ├── GetSceneTreeToolTests.cs
    │   │   │   └── GetComponentDataToolTests.cs
    │   │   ├── Navigation/
    │   │   │   ├── OpenSceneToolTests.cs
    │   │   │   └── SelectEntityToolTests.cs
    │   │   └── Modification/
    │   │       ├── CreateEntityToolTests.cs
    │   │       └── UpdateComponentToolTests.cs
    │   ├── Services/
    │   │   ├── SceneAccessServiceTests.cs
    │   │   └── ScreenshotServiceTests.cs
    │   └── Mocks/
    │       ├── MockSessionViewModel.cs
    │       ├── MockSceneViewModel.cs
    │       └── MockAssetViewModel.cs
    │
    └── Stride.GameStudio.McpLauncher.IntegrationTests/  # NEW: E2E tests
        ├── Stride.GameStudio.McpLauncher.IntegrationTests.csproj
        ├── Fixtures/
        │   ├── GameStudioFixture.cs              # Launch GameStudio for tests
        │   └── TestProjectFixture.cs             # Create test projects
        ├── EndToEnd/
        │   ├── BasicWorkflowTests.cs             # Basic read/write operations
        │   ├── MultiInstanceTests.cs             # Multi-instance scenarios
        │   ├── NavigationTests.cs                # Navigation scenarios
        │   ├── ModificationTests.cs              # Modification scenarios
        │   └── LifecycleTests.cs                 # Launch/close scenarios
        └── TestProjects/                         # Sample projects for testing
            ├── EmptyProject/
            ├── SimpleScene/
            └── ComplexScene/
```

## Implementation Phases

### Phase 1: Foundation (Week 1-2)

#### Milestone 1.1: MCP Launcher + Basic IPC
**Deliverables:**
- `Stride.GameStudio.McpLauncher` project created
- stdio transport implementation
- Basic MCP protocol handler (initialize, tools/list)
- IPC infrastructure (named pipes or gRPC)
- Can launch single GameStudio instance
- Basic health check tool

**Tests:**
- Launcher starts and accepts MCP connections
- Can communicate with launched GameStudio
- Health check returns instance info

#### Milestone 1.2: MCP Plugin + Core Services
**Deliverables:**
- `Stride.Assets.Presentation.MCP` plugin project
- Plugin loads in GameStudio
- IPC client connects to launcher
- Core services: SceneAccessService, AssetAccessService, EditorStateService
- Thread-safe access patterns established

**Tests:**
- Plugin loads without errors
- Services can access editor state safely
- No deadlocks under load

#### Milestone 1.3: First Read Tools
**Deliverables:**
- `ListAssetsTool` - enumerate all assets
- `ListScenesTool` - list all scenes
- `GetSceneTreeTool` - get entity hierarchy
- `GetComponentDataTool` - read component properties

**Tests:**
- Each tool returns correct data
- Tools handle missing/invalid inputs gracefully
- Performance benchmarks (< 100ms for typical queries)

### Phase 2: Navigation & Visualization (Week 3-4)

#### Milestone 2.1: Scene Navigation
**Deliverables:**
- `OpenSceneTool` - open a scene in editor
- `SelectEntityTool` - select entity in scene
- `FocusEntityTool` - focus camera on entity
- `CaptureScreenshotTool` - capture viewport as base64 PNG

**Tests:**
- Can open scenes and verify they're loaded
- Selection updates correctly in editor
- Screenshots match viewport content

#### Milestone 2.2: Asset Navigation
**Deliverables:**
- `OpenAssetFolderTool` - navigate to folder in asset view
- `SearchAssetsTool` - search assets by name/type
- `GetAssetMetadataTool` - get detailed asset info

**Tests:**
- Folder navigation works correctly
- Search returns relevant results
- Metadata is complete and accurate

#### Milestone 2.3: Extended Visualization
**Deliverables:**
- `GetUIPageTool` - read UI page structure
- `GetSpriteSheetTool` - read sprite sheet data
- Enhanced screenshot capture (UI panels, specific viewports)

**Tests:**
- UI pages parsed correctly
- Sprite sheets include all frames
- Screenshot capture works for all viewport types

### Phase 3: Modification Operations (Week 5-6)

#### Milestone 3.1: Entity Operations
**Deliverables:**
- `CreateEntityTool` - create new entities
- `DeleteEntityTool` - remove entities
- `MoveEntityTool` - change position/rotation/scale
- `ReparentEntityTool` - change parent hierarchy
- Undo/redo integration

**Tests:**
- Entity operations execute correctly
- Undo/redo works for all operations
- Scene dirty state updates

#### Milestone 3.2: Component Operations
**Deliverables:**
- `AddComponentTool` - add components to entities
- `RemoveComponentTool` - remove components
- `UpdateComponentTool` - modify component properties
- Type-safe component data serialization

**Tests:**
- All component types supported
- Property updates persist correctly
- Type validation works

#### Milestone 3.3: Asset Operations
**Deliverables:**
- `ImportAssetTool` - import files as assets
- `DeleteAssetTool` - remove assets
- `RenameAssetTool` - rename assets
- `EditUIPageTool` - modify UI pages
- `EditSpriteSheetTool` - edit sprite sheets

**Tests:**
- Asset import handles all formats
- Deletions cascade correctly
- UI/sprite edits persist

### Phase 4: Lifecycle & Multi-Instance (Week 7-8)

#### Milestone 4.1: Project Lifecycle
**Deliverables:**
- `OpenProjectTool` - open existing project
- `CreateProjectTool` - create new project from template
- `CloseProjectTool` - close current project
- `BuildProjectTool` - trigger build
- `GetBuildStatusTool` - monitor build progress

**Tests:**
- Project operations work end-to-end
- Build triggers correctly
- Build status updates in real-time

#### Milestone 4.2: Multi-Instance Support
**Deliverables:**
- Instance manager tracks multiple GameStudios
- Tool requests include instance ID
- Launcher routes to correct instance
- Instance lifecycle management (launch, monitor, cleanup)

**Tests:**
- Can manage 3+ simultaneous instances
- Requests route correctly
- Instance crashes handled gracefully

#### Milestone 4.3: Studio Lifecycle
**Deliverables:**
- Launcher can start GameStudio instances
- Launcher can stop/restart instances
- Automatic crash detection and restart
- Instance affinity (route related operations to same instance)

**Tests:**
- Launch/close cycles work reliably
- Crash recovery works
- Affinity rules respected

### Phase 5: Polish & Performance (Week 9-10)

#### Milestone 5.1: Error Handling & Validation
**Deliverables:**
- Comprehensive input validation
- Detailed error messages
- Error recovery strategies
- Rate limiting and throttling

**Tests:**
- All error cases covered
- Error messages are actionable
- Rate limiting prevents abuse

#### Milestone 5.2: Performance Optimization
**Deliverables:**
- Response time < 100ms for reads
- Response time < 500ms for writes
- Batch operation support
- Caching for frequently accessed data

**Tests:**
- Performance benchmarks pass
- No memory leaks
- Batch operations faster than sequential

#### Milestone 5.3: Documentation & Examples
**Deliverables:**
- API documentation for all tools
- Usage examples for common workflows
- Claude Desktop integration guide
- Troubleshooting guide

### Phase 6: Advanced Features (Week 11-12)

#### Milestone 6.1: Advanced Navigation
**Deliverables:**
- Viewport camera control
- Animation playback control
- Timeline navigation
- Visual debugging overlays

#### Milestone 6.2: Advanced Modification
**Deliverables:**
- Prefab instantiation
- Script generation
- Material editing
- Animation editing

#### Milestone 6.3: Monitoring & Events
**Deliverables:**
- MCP Resources for state changes
- Event notifications (entity created, scene loaded, etc.)
- Real-time state synchronization

## Testing Strategy

### Unit Tests (Target: 80% coverage)

**Framework:** xUnit + NSubstitute for mocking

**Test Categories:**
1. **Tool Tests** - Each tool has comprehensive tests
   - Valid inputs return correct results
   - Invalid inputs return proper errors
   - Edge cases handled
   - Performance benchmarks

2. **Service Tests** - Core services tested in isolation
   - Thread safety verified
   - Error handling validated
   - Resource cleanup verified

3. **Serialization Tests** - Data models serialize correctly
   - All component types supported
   - Null handling correct
   - Circular references prevented

**Example Test Structure:**
```csharp
public class GetSceneTreeToolTests
{
    [Fact]
    public async Task Execute_ValidSceneId_ReturnsEntityTree()
    {
        // Arrange
        var mockSession = CreateMockSession();
        var tool = new GetSceneTreeTool(mockSession);
        var input = new { SceneId = "test-scene" };
        
        // Act
        var result = await tool.ExecuteAsync(input);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("RootEntity", result.Name);
        Assert.NotEmpty(result.Children);
    }
    
    [Fact]
    public async Task Execute_InvalidSceneId_ReturnsError()
    {
        // ... test error handling
    }
    
    [Fact]
    public async Task Execute_PerformanceBenchmark()
    {
        // ... verify < 100ms response time
    }
}
```

### Integration Tests (E2E)

**Framework:** xUnit + real GameStudio instances

**Test Infrastructure:**
- `GameStudioFixture` - Launches GameStudio for tests
- `TestProjectFixture` - Creates temporary test projects
- Cleanup between tests

**Test Scenarios:**

1. **Basic Workflow** (10 tests)
   - Launch GameStudio
   - List assets
   - Open scene
   - Read entity tree
   - Close project
   - Shutdown GameStudio

2. **Multi-Instance** (5 tests)
   - Launch 2 instances
   - Open different projects
   - Execute tools on each
   - Verify correct routing
   - Clean shutdown

3. **Modification Workflow** (15 tests)
   - Create entity
   - Add components
   - Modify properties
   - Save scene
   - Reload scene
   - Verify persistence

4. **Navigation Workflow** (8 tests)
   - Open scene
   - Select entity
   - Focus camera
   - Capture screenshot
   - Verify screenshot content

5. **Lifecycle Workflow** (7 tests)
   - Create new project
   - Add assets
   - Create scene
   - Build project
   - Verify build output

**Example E2E Test:**
```csharp
public class BasicWorkflowTests : IClassFixture<GameStudioFixture>
{
    private readonly GameStudioFixture _fixture;
    
    public BasicWorkflowTests(GameStudioFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task CompleteWorkflow_CreateAndModifyEntity()
    {
        // Arrange - project already opened by fixture
        var mcpClient = _fixture.McpClient;
        
        // Act 1: Create entity
        var createResult = await mcpClient.CallToolAsync(
            "create_entity",
            new { Name = "TestEntity", SceneId = _fixture.MainSceneId }
        );
        var entityId = createResult.EntityId;
        
        // Act 2: Add transform component
        await mcpClient.CallToolAsync(
            "add_component",
            new { EntityId = entityId, ComponentType = "Transform" }
        );
        
        // Act 3: Update position
        await mcpClient.CallToolAsync(
            "update_component",
            new { 
                EntityId = entityId,
                ComponentType = "Transform",
                Properties = new { Position = new { X = 1, Y = 2, Z = 3 } }
            }
        );
        
        // Act 4: Read back entity
        var entityData = await mcpClient.CallToolAsync(
            "get_entity_details",
            new { EntityId = entityId }
        );
        
        // Assert
        Assert.Equal("TestEntity", entityData.Name);
        Assert.Single(entityData.Components);
        Assert.Equal(1, entityData.Components[0].Properties["Position"].X);
    }
}
```

### Performance Testing

**Benchmarks:**
- Read operations: < 100ms
- Write operations: < 500ms
- Screenshot capture: < 2s (1920x1080)
- Multi-instance overhead: < 50ms

**Load Testing:**
- 100 sequential requests/second sustained
- 10 concurrent requests without deadlock
- Memory usage stable over 1000 operations

## Configuration

### Launcher Configuration

**File:** `stride-mcp-launcher.json`
```json
{
  "mcp": {
    "transport": "stdio",
    "maxInstances": 5,
    "instanceTimeout": 300000,
    "enableLogging": true,
    "logLevel": "Information"
  },
  "gameStudio": {
    "executablePath": "C:\\Program Files\\Stride\\GameStudio\\Stride.GameStudio.exe",
    "startupArgs": ["--mcp-plugin"],
    "workingDirectory": null,
    "environmentVariables": {
      "STRIDE_MCP_ENABLED": "true"
    }
  },
  "ipc": {
    "transport": "namedPipes",
    "pipeName": "stride-mcp-{instanceId}"
  },
  "rateLimiting": {
    "enabled": true,
    "maxRequestsPerSecond": 100,
    "maxConcurrentRequests": 10
  }
}
```

### Plugin Configuration

**File:** Embedded in GameStudio settings
```json
{
  "mcpPlugin": {
    "enabled": true,
    "autoConnect": true,
    "launcherPipeName": "stride-mcp-{processId}",
    "tools": {
      "readOperations": { "enabled": true },
      "writeOperations": { "enabled": true, "requireConfirmation": false },
      "lifecycleOperations": { "enabled": true }
    }
  }
}
```

## Development Workflow

### Setting Up Development Environment

1. **Clone Fork:**
   ```bash
   git clone https://github.com/madsiberian/stride.git
   cd stride
   ```

2. **Create Feature Branch:**
   ```bash
   git checkout -b feature/mcp-integration
   ```

3. **Build Prerequisites:**
   - .NET 10.0 SDK
   - Visual Studio 2026
   - All Stride prerequisites (see main README)

4. **Build Solution:**
   ```bash
   cd build
   msbuild /t:Restore Stride.sln
   msbuild Stride.sln
   ```

### Development Loop

1. **Implement Feature** (1-2 days)
   - Create/modify source files
   - Follow existing Stride code style

2. **Write Unit Tests** (0.5 days)
   - Achieve 80% coverage
   - All edge cases covered

3. **Write Integration Tests** (0.5 days)
   - At least one E2E scenario

4. **Manual Testing** (0.5 days)
   - Test with Claude Desktop
   - Verify in real GameStudio

5. **Code Review** (0.5 days)
   - Self-review with checklist
   - Performance profiling

6. **Commit & Push** (0.1 days)
   - Descriptive commit message
   - Reference issue/milestone

### Testing Before Commit

```bash
# Run unit tests
dotnet test sources/editor/Stride.Assets.Presentation.MCP.Tests/

# Run integration tests (slower)
dotnet test tests/Stride.GameStudio.McpLauncher.IntegrationTests/

# Run performance benchmarks
dotnet run --project tests/PerformanceBenchmarks/ --configuration Release
```

## Success Criteria

### Phase 1 Success
- ✅ Launcher starts and manages 1 GameStudio instance
- ✅ 4 read tools implemented and tested
- ✅ Unit tests passing with 80% coverage
- ✅ Basic E2E test passes

### Phase 2 Success
- ✅ All navigation tools working
- ✅ Screenshots captured successfully
- ✅ Navigation E2E tests passing

### Phase 3 Success
- ✅ Entity/component CRUD operations working
- ✅ Undo/redo integration complete
- ✅ Modification E2E tests passing

### Phase 4 Success
- ✅ Multi-instance support (3+ instances)
- ✅ Project lifecycle tools working
- ✅ Build integration complete

### Phase 5 Success
- ✅ All performance benchmarks met
- ✅ Comprehensive error handling
- ✅ Documentation complete

### Phase 6 Success
- ✅ Advanced features implemented
- ✅ Event system working
- ✅ Production-ready quality

## Risk Management

### Technical Risks

1. **Threading Issues**
   - Risk: Deadlocks when accessing editor state
   - Mitigation: All editor access via Dispatcher, strict locking strategy
   - Test: Stress tests with concurrent requests

2. **Memory Leaks**
   - Risk: Long-running instances consume excessive memory
   - Mitigation: Proper disposal patterns, periodic GC
   - Test: Memory profiling over 1000+ operations

3. **IPC Reliability**
   - Risk: IPC connection drops
   - Mitigation: Automatic reconnection, heartbeat
   - Test: Network fault injection

4. **Serialization Performance**
   - Risk: Large scene trees slow to serialize
   - Mitigation: Streaming serialization, pagination
   - Test: Benchmark with 10k+ entity scenes

### Schedule Risks

1. **Underestimated Complexity**
   - Mitigation: Phase 1-2 establish patterns, later phases replicate
   - Buffer: 2-week contingency after Phase 3

2. **Stride API Changes**
   - Mitigation: Work with Stride maintainers, document assumptions
   - Buffer: Fork can lag behind upstream

## Deliverables Checklist

### Code
- [ ] MCP Launcher project
- [ ] MCP Plugin project
- [ ] Unit test project (80% coverage)
- [ ] Integration test project (30+ E2E tests)
- [ ] All 60+ tools implemented

### Documentation
- [ ] README for launcher setup
- [ ] API documentation for all tools
- [ ] Architecture decision records
- [ ] Claude Desktop integration guide
- [ ] Troubleshooting guide

### Quality
- [ ] All tests passing
- [ ] Performance benchmarks met
- [ ] No memory leaks
- [ ] Code review completed
- [ ] Security review completed

## Next Steps - Starting Implementation

### Immediate Actions (Day 1)

1. **Create Launcher Project:**
   ```bash
   cd sources/editor
   dotnet new console -n Stride.GameStudio.McpLauncher
   cd Stride.GameStudio.McpLauncher
   dotnet add package ModelContextProtocol
   dotnet add package System.IO.Pipes  # For IPC
   ```

2. **Create Plugin Project:**
   ```bash
   cd sources/editor
   dotnet new classlib -n Stride.Assets.Presentation.MCP
   cd Stride.Assets.Presentation.MCP
   dotnet add package ModelContextProtocol
   dotnet add package System.IO.Pipes
   ```

3. **Create Test Projects:**
   ```bash
   cd tests
   dotnet new xunit -n Stride.Assets.Presentation.MCP.Tests
   dotnet new xunit -n Stride.GameStudio.McpLauncher.IntegrationTests
   ```

4. **Add Project References:**
   - Plugin → Stride core libraries
   - Tests → Plugin + xUnit + NSubstitute

### Week 1 Goals

- Complete Milestone 1.1 (Launcher + IPC)
- Complete Milestone 1.2 (Plugin + Services)
- Begin Milestone 1.3 (First tools)

---

**Document Version:** 1.0  
**Last Updated:** 2025-02-17  
**Author:** Development Team  
**Status:** APPROVED - Ready for Implementation  

**Estimated Timeline:** 12 weeks (3 months)  
**Team Size:** 1-2 developers  
**Priority:** High
