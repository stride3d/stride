// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Xunit;

namespace Stride.GameStudio.Mcp.Tests;

/// <summary>
/// Integration tests for the MCP server embedded in Game Studio.
/// These tests require a running Game Studio instance with a project loaded and MCP enabled.
///
/// To run these tests:
/// 1. Build and launch Game Studio with a sample project (e.g., FirstPersonShooter)
/// 2. Verify MCP server is running on the expected port (default: 5271)
/// 3. Set environment variable: STRIDE_MCP_INTEGRATION_TESTS=true
/// 4. Optionally set STRIDE_MCP_PORT if using a non-default port
/// 5. Run: dotnet test sources/editor/Stride.GameStudio.Mcp.Tests
/// </summary>
[Collection("McpIntegration")]
public sealed class McpIntegrationTests : IAsyncLifetime
{
    private McpClient? _client;

    private static bool IsEnabled =>
        string.Equals(
            Environment.GetEnvironmentVariable("STRIDE_MCP_INTEGRATION_TESTS"),
            "true",
            StringComparison.OrdinalIgnoreCase);

    private static int Port
    {
        get
        {
            var portStr = Environment.GetEnvironmentVariable("STRIDE_MCP_PORT");
            return int.TryParse(portStr, out var port) ? port : 5271;
        }
    }

    public async Task InitializeAsync()
    {
        if (!IsEnabled)
            return;

        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri($"http://localhost:{Port}/sse"),
            Name = "Stride MCP Integration Tests",
        });

        _client = await McpClient.CreateAsync(transport);
    }

    public async Task DisposeAsync()
    {
        if (_client != null)
        {
            await ((IAsyncDisposable)_client).DisposeAsync();
        }
    }

    [McpIntegrationFact]
    public async Task GetEditorStatus_ReturnsProjectInfo()
    {
        var root = await CallToolAndParseJsonAsync("get_editor_status");

        Assert.Equal("connected", root.GetProperty("status").GetString());
        Assert.False(string.IsNullOrEmpty(root.GetProperty("currentProject").GetString()));
        Assert.False(string.IsNullOrEmpty(root.GetProperty("solutionPath").GetString()));
        Assert.True(root.GetProperty("assetCount").GetInt32() > 0);
        Assert.True(root.GetProperty("scenes").GetArrayLength() > 0);
    }

    [McpIntegrationFact]
    public async Task QueryAssets_ReturnsAssets()
    {
        var root = await CallToolAndParseJsonAsync("query_assets", new Dictionary<string, object?>
        {
            ["maxResults"] = 10,
        });

        Assert.True(root.GetProperty("totalCount").GetInt32() > 0);
        Assert.True(root.GetProperty("returnedCount").GetInt32() > 0);

        var assets = root.GetProperty("assets");
        Assert.True(assets.GetArrayLength() > 0);

        var first = assets[0];
        Assert.False(string.IsNullOrEmpty(first.GetProperty("id").GetString()));
        Assert.False(string.IsNullOrEmpty(first.GetProperty("name").GetString()));
        Assert.False(string.IsNullOrEmpty(first.GetProperty("type").GetString()));
    }

    [McpIntegrationFact]
    public async Task QueryAssets_WithTypeFilter_ReturnsOnlyMatchingType()
    {
        var root = await CallToolAndParseJsonAsync("query_assets", new Dictionary<string, object?>
        {
            ["type"] = "SceneAsset",
        });

        var assets = root.GetProperty("assets");
        Assert.True(assets.GetArrayLength() > 0);
        foreach (var asset in assets.EnumerateArray())
        {
            Assert.Equal("SceneAsset", asset.GetProperty("type").GetString());
        }
    }

    [McpIntegrationFact]
    public async Task GetSceneTree_ReturnsEntityHierarchy()
    {
        var sceneId = await GetFirstSceneIdAsync();

        var root = await CallToolAndParseJsonAsync("get_scene_tree", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
        });

        Assert.Null(root.GetProperty("error").GetString());

        var scene = root.GetProperty("scene");
        Assert.Equal(sceneId, scene.GetProperty("id").GetString());
        Assert.False(string.IsNullOrEmpty(scene.GetProperty("name").GetString()));
        Assert.True(scene.GetProperty("entityCount").GetInt32() > 0);

        var entities = scene.GetProperty("entities");
        Assert.True(entities.GetArrayLength() > 0);

        // Verify entity node structure
        var firstEntity = entities[0];
        Assert.False(string.IsNullOrEmpty(firstEntity.GetProperty("id").GetString()));
        Assert.False(string.IsNullOrEmpty(firstEntity.GetProperty("name").GetString()));
        Assert.True(firstEntity.TryGetProperty("components", out _));
        Assert.True(firstEntity.TryGetProperty("children", out _));
    }

    [McpIntegrationFact]
    public async Task GetSceneTree_WithInvalidId_ReturnsError()
    {
        var root = await CallToolAndParseJsonAsync("get_scene_tree", new Dictionary<string, object?>
        {
            ["sceneId"] = "00000000-0000-0000-0000-000000000000",
        });

        Assert.False(string.IsNullOrEmpty(root.GetProperty("error").GetString()));
    }

    [McpIntegrationFact]
    public async Task GetEntity_ReturnsComponentDetails()
    {
        var sceneId = await GetFirstSceneIdAsync();
        var entityId = await GetFirstEntityIdAsync(sceneId);

        var root = await CallToolAndParseJsonAsync("get_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["entityId"] = entityId,
        });

        Assert.Null(root.GetProperty("error").GetString());

        var entity = root.GetProperty("entity");
        Assert.Equal(entityId, entity.GetProperty("id").GetString());
        Assert.False(string.IsNullOrEmpty(entity.GetProperty("name").GetString()));
        Assert.True(entity.TryGetProperty("components", out var components));
        Assert.True(components.GetArrayLength() > 0, "Entity should have at least one component");

        // Every entity has at least a TransformComponent
        var hasTransform = false;
        foreach (var comp in components.EnumerateArray())
        {
            Assert.False(string.IsNullOrEmpty(comp.GetProperty("type").GetString()));
            Assert.True(comp.TryGetProperty("properties", out _));
            if (comp.GetProperty("type").GetString() == "TransformComponent")
                hasTransform = true;
        }
        Assert.True(hasTransform, "Entity should have a TransformComponent");
    }

    [McpIntegrationFact]
    public async Task GetEntity_WithInvalidEntityId_ReturnsError()
    {
        var sceneId = await GetFirstSceneIdAsync();

        var root = await CallToolAndParseJsonAsync("get_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["entityId"] = "00000000-0000-0000-0000-000000000000",
        });

        Assert.False(string.IsNullOrEmpty(root.GetProperty("error").GetString()));
    }

    [McpIntegrationFact]
    public async Task ListTools_ReturnsAllExpectedTools()
    {
        var tools = await _client!.ListToolsAsync();
        var toolNames = tools.Select(t => t.Name).ToHashSet();

        Assert.Contains("get_editor_status", toolNames);
        Assert.Contains("query_assets", toolNames);
        Assert.Contains("get_scene_tree", toolNames);
        Assert.Contains("get_entity", toolNames);
    }

    private async Task<JsonElement> CallToolAndParseJsonAsync(
        string toolName,
        Dictionary<string, object?>? arguments = null)
    {
        var result = await _client!.CallToolAsync(toolName, arguments);
        var textBlock = result.Content.OfType<TextContentBlock>().First();
        var doc = JsonDocument.Parse(textBlock.Text!);
        return doc.RootElement;
    }

    private async Task<string> GetFirstSceneIdAsync()
    {
        var root = await CallToolAndParseJsonAsync("get_editor_status");
        var scenes = root.GetProperty("scenes");
        Assert.True(scenes.GetArrayLength() > 0, "No scenes found in project");
        return scenes[0].GetProperty("id").GetString()!;
    }

    private async Task<string> GetFirstEntityIdAsync(string sceneId)
    {
        var root = await CallToolAndParseJsonAsync("get_scene_tree", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
        });
        var entities = root.GetProperty("scene").GetProperty("entities");
        Assert.True(entities.GetArrayLength() > 0, "No entities found in scene");
        return entities[0].GetProperty("id").GetString()!;
    }
}
