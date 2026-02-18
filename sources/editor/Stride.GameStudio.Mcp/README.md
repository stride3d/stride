# Stride Game Studio MCP Server

An embedded [Model Context Protocol](https://modelcontextprotocol.io/) (MCP) server that exposes Stride Game Studio editor functionality as tools for AI agents. This allows LLM-powered coding assistants to query project structure, browse scenes, and inspect entities directly from a running editor instance.

## How It Works

When Game Studio launches and opens a project, the MCP plugin automatically starts an HTTP server on `localhost:5271` using Server-Sent Events (SSE) transport. Any MCP-compatible client can connect to this endpoint and call tools to interact with the editor.

## Available Tools

| Tool | Description |
|------|-------------|
| `get_editor_status` | Returns project name, solution path, asset count, and scene listing |
| `query_assets` | Search and filter assets by name, type, or folder path |
| `get_scene_tree` | Returns the full entity hierarchy for a scene |
| `get_entity` | Returns detailed component and property data for an entity |

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
