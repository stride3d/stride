# Stride Game Studio MCP Server

An embedded [Model Context Protocol](https://modelcontextprotocol.io/) (MCP) server that exposes Stride Game Studio editor functionality as tools for AI agents. This allows LLM-powered coding assistants to query project structure, browse scenes, and inspect entities directly from a running editor instance.

## How It Works

When Game Studio launches and opens a project, the MCP plugin automatically starts an HTTP server on `localhost:5271` using Server-Sent Events (SSE) transport. Any MCP-compatible client can connect to this endpoint and call tools to interact with the editor.

## Available Tools

### State Reading
| Tool | Description |
|------|-------------|
| `get_editor_status` | Returns project name, solution path, packages, projects (with type/platform/active status), asset count, and scene listing |
| `query_assets` | Search and filter assets by name, type, or folder path |
| `get_scene_tree` | Returns the full entity hierarchy for a scene |
| `get_entity` | Returns detailed component and property data for an entity |

### Navigation & Selection
| Tool | Description |
|------|-------------|
| `open_scene` | Opens a scene asset in the editor (or activates if already open) |
| `open_ui_page` | Opens a UI page asset in the editor (or activates if already open) |
| `select_entity` | Selects entities in the scene editor hierarchy (supports multi-select) |
| `focus_entity` | Centers the viewport camera on an entity (also selects it) |

### Modification
| Tool | Description |
|------|-------------|
| `create_entity` | Creates a new entity in a scene (with optional parent) |
| `delete_entity` | Deletes an entity and its children from a scene |
| `reparent_entity` | Moves an entity to a new parent (or to root level) |
| `set_transform` | Sets position, rotation (Euler degrees), and/or scale of an entity |
| `modify_component` | Adds, removes, or updates a component on an entity |

### Asset Management
| Tool | Description |
|------|-------------|
| `get_asset_details` | Returns detailed properties, source file, archetype, and tags for an asset |
| `get_asset_dependencies` | Shows what references an asset (inbound) and what it references (outbound) |
| `create_asset` | Creates a new asset (material, prefab, scene, etc.) with defaults |
| `manage_asset` | Renames, moves, or deletes an asset (with reference safety checks) |
| `set_asset_property` | Sets a property on an asset via dot-notation path through the property graph |
| `add_sprite_frame` | Adds a new sprite frame to a SpriteSheetAsset |
| `remove_sprite_frame` | Removes a sprite frame from a SpriteSheetAsset by index |

### UI Pages
| Tool | Description |
|------|-------------|
| `get_ui_tree` | Returns the full UI element hierarchy for a UIPageAsset |
| `get_ui_element` | Returns detailed properties for a specific UI element |
| `add_ui_element` | Creates a new UI element (Button, TextBlock, StackPanel, etc.) in a page |
| `remove_ui_element` | Removes a UI element and its descendants from a page |
| `set_ui_element_property` | Sets a property on a UI element via dot-notation path |

### Project
| Tool | Description |
|------|-------------|
| `save_project` | Saves all changes (scenes, entities, assets, etc.) to disk |
| `reload_scene` | Closes and reopens a scene editor tab to refresh its state |
| `reload_project` | Triggers a full GameStudio restart to reload the project from disk |
| `set_active_project` | Changes which project is active (determines build target and asset root) |

### Viewport
| Tool | Description |
|------|-------------|
| `capture_viewport` | Captures a PNG screenshot of the viewport for an open scene or UI page |

### Build
| Tool | Description |
|------|-------------|
| `build_project` | Triggers an async build of the current game project |
| `get_build_status` | Returns current build status, errors, and warnings |

## Configuration

| Environment Variable | Default | Description |
|---------------------|---------|-------------|
| `STRIDE_MCP_PORT` | `5271` | Port for the MCP HTTP/SSE server |
| `STRIDE_MCP_ENABLED` | `true` | Set to `false` to disable the MCP server |

## Connecting AI Agents

### Claude Code (CLI)

Add to your project's `.mcp.json` file or `~/.claude.json`:

```json
{
  "mcpServers": {
    "stride-editor": {
      "type": "sse",
      "url": "http://localhost:5271/sse"
    }
  }
}
```

Then start Claude Code. It will automatically connect to Game Studio when it's running.

### Claude Desktop

Add to your Claude Desktop config file (`%APPDATA%\Claude\claude_desktop_config.json` on Windows):

```json
{
  "mcpServers": {
    "stride-editor": {
      "type": "sse",
      "url": "http://localhost:5271/sse"
    }
  }
}
```

Restart Claude Desktop after editing the config.

### Continue (VS Code / JetBrains)

Add to your Continue config file (`.continue/config.yaml`):

```yaml
mcpServers:
  - name: stride-editor
    url: http://localhost:5271/sse
```

See [Continue MCP docs](https://docs.continue.dev/customize/context-providers#mcp) for details.

### JetBrains Junie

Add to your project's `.junie/mcp.json`:

```json
{
  "mcpServers": {
    "stride-editor": {
      "type": "sse",
      "url": "http://localhost:5271/sse"
    }
  }
}
```

See [Junie MCP docs](https://www.jetbrains.com/help/junie/mcp-servers.html) for details.

### Cursor

Add to your project's `.cursor/mcp.json`:

```json
{
  "mcpServers": {
    "stride-editor": {
      "type": "sse",
      "url": "http://localhost:5271/sse"
    }
  }
}
```

### Generic MCP Client

Any MCP-compatible client can connect using:
- **Transport**: SSE (Server-Sent Events)
- **Endpoint**: `http://localhost:5271/sse`

## Working with Asset References

Many component properties (e.g. `ModelComponent.Model`, `BackgroundComponent.Texture`, `UIComponent.Page`) are asset references that point to other assets in the project. These can be set using `modify_component update` or `set_asset_property`.

### JSON Format

Asset reference values accept the following JSON formats:

```json
// Object with assetId (preferred)
{"Model": {"assetId": "12345678-1234-1234-1234-123456789abc"}}

// Object with assetRef (matches serialization output, for round-tripping)
{"Model": {"assetRef": "12345678-1234-1234-1234-123456789abc"}}

// String shorthand
{"Model": "12345678-1234-1234-1234-123456789abc"}

// Clear the reference
{"Model": null}
```

Use `query_assets` to find the asset ID for the asset you want to reference.

### Example: Setting a Model on an Entity

1. Find a model asset: `query_assets` with `type: "ModelAsset"`
2. Add a ModelComponent: `modify_component` with `action: "add"`, `componentType: "ModelComponent"`
3. Set the model: `modify_component` with `action: "update"`, `properties: '{"Model":{"assetId":"<model-asset-id>"}}'`
4. Verify: `capture_viewport` to see the model rendered in the scene

## Project Reload Behavior

### save_project
Writes the editor's in-memory state to disk. This **overwrites** any external changes made to scene/asset YAML files. If you have modified project files externally, use `reload_project` first to load those changes into the editor.

### reload_scene
Closes and reopens a single scene editor tab. Useful after `build_project` completes (to pick up new script component types) or when the scene editor appears stale. Unsaved changes to that scene are discarded.

### reload_project
Triggers a full GameStudio restart (equivalent to File > Reload project). The MCP connection will be lost — the client must wait for the new GameStudio instance to start and reconnect to the new MCP server. If there are unsaved changes, the user will see a Save/Don't Save/Cancel dialog.

## Integration Tests

See [`Stride.GameStudio.Mcp.Tests/README.md`](../Stride.GameStudio.Mcp.Tests/README.md) for integration test setup and instructions.
