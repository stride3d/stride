# Stride Game Studio MCP Server

An embedded [Model Context Protocol](https://modelcontextprotocol.io/) (MCP) server that exposes Stride Game Studio editor functionality as tools for AI agents. This allows LLM-powered coding assistants to query project structure, browse scenes, and inspect entities directly from a running editor instance.

## How It Works

When Game Studio launches and opens a project, the MCP plugin automatically starts an HTTP server on `localhost:5271` using Server-Sent Events (SSE) transport. Any MCP-compatible client can connect to this endpoint and call tools to interact with the editor.

## Available Tools

### State Reading
| Tool | Description |
|------|-------------|
| `get_editor_status` | Returns project name, solution path, asset count, and scene listing |
| `query_assets` | Search and filter assets by name, type, or folder path |
| `get_scene_tree` | Returns the full entity hierarchy for a scene |
| `get_entity` | Returns detailed component and property data for an entity |

### Navigation & Selection
| Tool | Description |
|------|-------------|
| `open_scene` | Opens a scene asset in the editor (or activates if already open) |
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

### Project
| Tool | Description |
|------|-------------|
| `save_project` | Saves all changes (scenes, entities, assets, etc.) to disk |

### Viewport
| Tool | Description |
|------|-------------|
| `capture_viewport` | Captures a PNG screenshot of the 3D viewport for an open scene |

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

## Integration Tests

See [`Stride.GameStudio.Mcp.Tests/README.md`](../Stride.GameStudio.Mcp.Tests/README.md) for integration test setup and instructions.
