// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Xunit;

namespace Stride.GameStudio.Mcp.Tests;

/// <summary>
/// Integration tests for the MCP server embedded in Game Studio.
/// The <see cref="GameStudioFixture"/> automatically launches Game Studio and waits
/// for the MCP server to become ready. Tests are skipped unless the environment
/// variable STRIDE_MCP_INTEGRATION_TESTS is set to "true".
///
/// See README.md in this project for setup instructions.
/// </summary>
[Collection("McpIntegration")]
public sealed class McpIntegrationTests : IAsyncLifetime
{
    private readonly GameStudioFixture _fixture;
    private McpClient? _client;

    public McpIntegrationTests(GameStudioFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        if (!_fixture.IsReady)
            return;

        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri($"http://localhost:{_fixture.Port}/sse"),
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
    public async Task OpenScene_OpensSceneSuccessfully()
    {
        var sceneId = await GetFirstSceneIdAsync();

        var root = await CallToolAndParseJsonAsync("open_scene", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
        });

        Assert.Equal("opened", root.GetProperty("status").GetString());
        Assert.Equal(sceneId, root.GetProperty("sceneId").GetString());
        Assert.False(string.IsNullOrEmpty(root.GetProperty("sceneName").GetString()));
    }

    [McpIntegrationFact]
    public async Task OpenScene_WithInvalidId_ReturnsError()
    {
        var root = await CallToolAndParseJsonAsync("open_scene", new Dictionary<string, object?>
        {
            ["sceneId"] = "00000000-0000-0000-0000-000000000000",
        });

        Assert.False(string.IsNullOrEmpty(root.GetProperty("error").GetString()));
    }

    [McpIntegrationFact]
    public async Task SelectEntity_SelectsEntityInEditor()
    {
        var sceneId = await GetFirstSceneIdAsync();
        // Ensure the scene is open first
        await CallToolAndParseJsonAsync("open_scene", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
        });

        var entityId = await GetFirstEntityIdAsync(sceneId);

        var root = await CallToolAndParseJsonAsync("select_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["entityId"] = entityId,
        });

        Assert.Null(root.GetProperty("error").GetString());
        var selected = root.GetProperty("selected");
        Assert.Equal(1, selected.GetProperty("count").GetInt32());
        var entities = selected.GetProperty("entities");
        Assert.Equal(entityId, entities[0].GetProperty("id").GetString());
    }

    [McpIntegrationFact]
    public async Task SelectEntity_WithInvalidEntityId_ReturnsError()
    {
        var sceneId = await GetFirstSceneIdAsync();
        // Ensure the scene is open first
        await CallToolAndParseJsonAsync("open_scene", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
        });

        var root = await CallToolAndParseJsonAsync("select_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["entityId"] = "00000000-0000-0000-0000-000000000000",
        });

        Assert.False(string.IsNullOrEmpty(root.GetProperty("error").GetString()));
    }

    [McpIntegrationFact]
    public async Task SelectEntity_WithSceneNotOpen_ReturnsError()
    {
        // Use a scene ID but don't open it — we just call select_entity directly
        // Note: this test depends on the editor not having the scene open already from a prior test
        // Since open_scene is called in other tests, this may need a fresh scene ID
        var root = await CallToolAndParseJsonAsync("select_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = "00000000-0000-0000-0000-000000000001",
            ["entityId"] = "00000000-0000-0000-0000-000000000002",
        });

        Assert.False(string.IsNullOrEmpty(root.GetProperty("error").GetString()));
    }

    [McpIntegrationFact]
    public async Task FocusEntity_FocusesOnEntity()
    {
        var sceneId = await GetFirstSceneIdAsync();
        // Ensure the scene is open first
        await CallToolAndParseJsonAsync("open_scene", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
        });

        var entityId = await GetFirstEntityIdAsync(sceneId);

        var root = await CallToolAndParseJsonAsync("focus_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["entityId"] = entityId,
        });

        Assert.Null(root.GetProperty("error").GetString());
        var focused = root.GetProperty("focused");
        Assert.Equal(entityId, focused.GetProperty("id").GetString());
        Assert.False(string.IsNullOrEmpty(focused.GetProperty("name").GetString()));
    }

    [McpIntegrationFact]
    public async Task FocusEntity_WithInvalidEntityId_ReturnsError()
    {
        var sceneId = await GetFirstSceneIdAsync();
        // Ensure the scene is open first
        await CallToolAndParseJsonAsync("open_scene", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
        });

        var root = await CallToolAndParseJsonAsync("focus_entity", new Dictionary<string, object?>
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

        // Phase 1 tools
        Assert.Contains("get_editor_status", toolNames);
        Assert.Contains("query_assets", toolNames);
        Assert.Contains("get_scene_tree", toolNames);
        Assert.Contains("get_entity", toolNames);

        // Phase 2 tools (Navigation & Selection)
        Assert.Contains("open_scene", toolNames);
        Assert.Contains("select_entity", toolNames);
        Assert.Contains("focus_entity", toolNames);
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
