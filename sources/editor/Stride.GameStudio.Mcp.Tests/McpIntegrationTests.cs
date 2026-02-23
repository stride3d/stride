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

    // =====================
    // Phase 3: Modification
    // =====================

    [McpIntegrationFact]
    public async Task CreateEntity_CreatesNewEntity()
    {
        var sceneId = await GetFirstSceneIdAsync();
        await CallToolAndParseJsonAsync("open_scene", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
        });

        var root = await CallToolAndParseJsonAsync("create_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["name"] = "McpTestEntity",
        });

        Assert.Null(root.GetProperty("error").GetString());
        var entity = root.GetProperty("entity");
        Assert.False(string.IsNullOrEmpty(entity.GetProperty("id").GetString()));
        Assert.Equal("McpTestEntity", entity.GetProperty("name").GetString());

        // Clean up: delete the created entity
        var entityId = entity.GetProperty("id").GetString()!;
        await CallToolAndParseJsonAsync("delete_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["entityId"] = entityId,
        });
    }

    [McpIntegrationFact]
    public async Task CreateEntity_WithParent_CreatesChildEntity()
    {
        var sceneId = await GetFirstSceneIdAsync();
        await CallToolAndParseJsonAsync("open_scene", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
        });

        var parentId = await GetFirstEntityIdAsync(sceneId);

        var root = await CallToolAndParseJsonAsync("create_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["name"] = "McpChildEntity",
            ["parentId"] = parentId,
        });

        Assert.Null(root.GetProperty("error").GetString());
        var entity = root.GetProperty("entity");
        Assert.Equal("McpChildEntity", entity.GetProperty("name").GetString());
        Assert.Equal(parentId, entity.GetProperty("parentId").GetString());

        // Clean up
        await CallToolAndParseJsonAsync("delete_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["entityId"] = entity.GetProperty("id").GetString()!,
        });
    }

    [McpIntegrationFact]
    public async Task CreateEntity_WithSceneNotOpen_ReturnsError()
    {
        var root = await CallToolAndParseJsonAsync("create_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = "00000000-0000-0000-0000-000000000001",
            ["name"] = "ShouldFail",
        });

        Assert.False(string.IsNullOrEmpty(root.GetProperty("error").GetString()));
    }

    [McpIntegrationFact]
    public async Task DeleteEntity_DeletesCreatedEntity()
    {
        var sceneId = await GetFirstSceneIdAsync();
        await CallToolAndParseJsonAsync("open_scene", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
        });

        // Create an entity to delete
        var createResult = await CallToolAndParseJsonAsync("create_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["name"] = "McpDeleteMe",
        });
        var entityId = createResult.GetProperty("entity").GetProperty("id").GetString()!;

        // Delete it
        var root = await CallToolAndParseJsonAsync("delete_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["entityId"] = entityId,
        });

        Assert.Null(root.GetProperty("error").GetString());
        var deleted = root.GetProperty("deleted");
        Assert.Equal(entityId, deleted.GetProperty("id").GetString());
        Assert.Equal("McpDeleteMe", deleted.GetProperty("name").GetString());
    }

    [McpIntegrationFact]
    public async Task DeleteEntity_WithInvalidEntityId_ReturnsError()
    {
        var sceneId = await GetFirstSceneIdAsync();
        await CallToolAndParseJsonAsync("open_scene", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
        });

        var root = await CallToolAndParseJsonAsync("delete_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["entityId"] = "00000000-0000-0000-0000-000000000000",
        });

        Assert.False(string.IsNullOrEmpty(root.GetProperty("error").GetString()));
    }

    [McpIntegrationFact]
    public async Task ReparentEntity_MovesToNewParent()
    {
        var sceneId = await GetFirstSceneIdAsync();
        await CallToolAndParseJsonAsync("open_scene", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
        });

        // Create two entities: one to reparent, one as target parent
        var parentResult = await CallToolAndParseJsonAsync("create_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["name"] = "McpNewParent",
        });
        var newParentId = parentResult.GetProperty("entity").GetProperty("id").GetString()!;

        var childResult = await CallToolAndParseJsonAsync("create_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["name"] = "McpChildToMove",
        });
        var childId = childResult.GetProperty("entity").GetProperty("id").GetString()!;

        // Reparent
        var root = await CallToolAndParseJsonAsync("reparent_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["entityId"] = childId,
            ["newParentId"] = newParentId,
        });

        Assert.Null(root.GetProperty("error").GetString());
        var reparented = root.GetProperty("reparented");
        Assert.Equal(childId, reparented.GetProperty("id").GetString());
        Assert.Equal(newParentId, reparented.GetProperty("newParentId").GetString());

        // Clean up (delete parent which also deletes child)
        await CallToolAndParseJsonAsync("delete_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["entityId"] = newParentId,
        });
    }

    [McpIntegrationFact]
    public async Task ReparentEntity_MoveToRoot()
    {
        var sceneId = await GetFirstSceneIdAsync();
        await CallToolAndParseJsonAsync("open_scene", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
        });

        var parentId = await GetFirstEntityIdAsync(sceneId);

        // Create a child entity under the first entity
        var childResult = await CallToolAndParseJsonAsync("create_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["name"] = "McpMoveToRoot",
            ["parentId"] = parentId,
        });
        var childId = childResult.GetProperty("entity").GetProperty("id").GetString()!;

        // Reparent to root (no newParentId)
        var root = await CallToolAndParseJsonAsync("reparent_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["entityId"] = childId,
        });

        Assert.Null(root.GetProperty("error").GetString());
        var reparented = root.GetProperty("reparented");
        Assert.Equal(childId, reparented.GetProperty("id").GetString());

        // Clean up
        await CallToolAndParseJsonAsync("delete_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["entityId"] = childId,
        });
    }

    [McpIntegrationFact]
    public async Task SetTransform_SetsPosition()
    {
        var sceneId = await GetFirstSceneIdAsync();
        await CallToolAndParseJsonAsync("open_scene", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
        });

        // Create a test entity
        var createResult = await CallToolAndParseJsonAsync("create_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["name"] = "McpTransformTest",
        });
        var entityId = createResult.GetProperty("entity").GetProperty("id").GetString()!;

        // Set its position
        var root = await CallToolAndParseJsonAsync("set_transform", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["entityId"] = entityId,
            ["positionX"] = 1.0f,
            ["positionY"] = 2.0f,
            ["positionZ"] = 3.0f,
        });

        Assert.Null(root.GetProperty("error").GetString());
        var transform = root.GetProperty("transform");
        Assert.Equal(entityId, transform.GetProperty("id").GetString());

        var position = transform.GetProperty("position");
        Assert.Equal(1.0f, position.GetProperty("x").GetSingle(), 0.01f);
        Assert.Equal(2.0f, position.GetProperty("y").GetSingle(), 0.01f);
        Assert.Equal(3.0f, position.GetProperty("z").GetSingle(), 0.01f);

        // Clean up
        await CallToolAndParseJsonAsync("delete_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["entityId"] = entityId,
        });
    }

    [McpIntegrationFact]
    public async Task SetTransform_SetsRotationAndScale()
    {
        var sceneId = await GetFirstSceneIdAsync();
        await CallToolAndParseJsonAsync("open_scene", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
        });

        // Create a test entity
        var createResult = await CallToolAndParseJsonAsync("create_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["name"] = "McpRotScaleTest",
        });
        var entityId = createResult.GetProperty("entity").GetProperty("id").GetString()!;

        // Set rotation and scale
        var root = await CallToolAndParseJsonAsync("set_transform", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["entityId"] = entityId,
            ["rotationX"] = 45.0f,
            ["rotationY"] = 90.0f,
            ["rotationZ"] = 0.0f,
            ["scaleX"] = 2.0f,
            ["scaleY"] = 2.0f,
            ["scaleZ"] = 2.0f,
        });

        Assert.Null(root.GetProperty("error").GetString());
        var transform = root.GetProperty("transform");

        var rotation = transform.GetProperty("rotation");
        Assert.Equal(45.0f, rotation.GetProperty("x").GetSingle(), 0.5f);
        Assert.Equal(90.0f, rotation.GetProperty("y").GetSingle(), 0.5f);
        Assert.Equal(0.0f, rotation.GetProperty("z").GetSingle(), 0.5f);

        var scale = transform.GetProperty("scale");
        Assert.Equal(2.0f, scale.GetProperty("x").GetSingle(), 0.01f);
        Assert.Equal(2.0f, scale.GetProperty("y").GetSingle(), 0.01f);
        Assert.Equal(2.0f, scale.GetProperty("z").GetSingle(), 0.01f);

        // Clean up
        await CallToolAndParseJsonAsync("delete_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["entityId"] = entityId,
        });
    }

    [McpIntegrationFact]
    public async Task SetTransform_WithInvalidEntityId_ReturnsError()
    {
        var sceneId = await GetFirstSceneIdAsync();
        await CallToolAndParseJsonAsync("open_scene", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
        });

        var root = await CallToolAndParseJsonAsync("set_transform", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["entityId"] = "00000000-0000-0000-0000-000000000000",
            ["positionX"] = 1.0f,
        });

        Assert.False(string.IsNullOrEmpty(root.GetProperty("error").GetString()));
    }

    // =============================
    // Phase 3.2: Component Modification
    // =============================

    [McpIntegrationFact]
    public async Task ModifyComponent_AddComponent()
    {
        var sceneId = await GetFirstSceneIdAsync();
        await CallToolAndParseJsonAsync("open_scene", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
        });

        // Create a test entity
        var createResult = await CallToolAndParseJsonAsync("create_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["name"] = "McpComponentTest",
        });
        var entityId = createResult.GetProperty("entity").GetProperty("id").GetString()!;

        // Add a ModelComponent
        var root = await CallToolAndParseJsonAsync("modify_component", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["entityId"] = entityId,
            ["action"] = "add",
            ["componentType"] = "ModelComponent",
        });

        Assert.Null(root.GetProperty("error").GetString());
        var component = root.GetProperty("component");
        Assert.Equal("added", component.GetProperty("action").GetString());
        Assert.Equal("ModelComponent", component.GetProperty("type").GetString());

        // Clean up
        await CallToolAndParseJsonAsync("delete_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["entityId"] = entityId,
        });
    }

    [McpIntegrationFact]
    public async Task ModifyComponent_RemoveComponent()
    {
        var sceneId = await GetFirstSceneIdAsync();
        await CallToolAndParseJsonAsync("open_scene", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
        });

        // Create a test entity and add a component to remove
        var createResult = await CallToolAndParseJsonAsync("create_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["name"] = "McpRemoveComponentTest",
        });
        var entityId = createResult.GetProperty("entity").GetProperty("id").GetString()!;

        await CallToolAndParseJsonAsync("modify_component", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["entityId"] = entityId,
            ["action"] = "add",
            ["componentType"] = "ModelComponent",
        });

        // Remove the added component (index 1, since 0 is TransformComponent)
        var root = await CallToolAndParseJsonAsync("modify_component", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["entityId"] = entityId,
            ["action"] = "remove",
            ["componentIndex"] = 1,
        });

        Assert.Null(root.GetProperty("error").GetString());
        var component = root.GetProperty("component");
        Assert.Equal("removed", component.GetProperty("action").GetString());
        Assert.Equal("ModelComponent", component.GetProperty("type").GetString());

        // Clean up
        await CallToolAndParseJsonAsync("delete_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["entityId"] = entityId,
        });
    }

    [McpIntegrationFact]
    public async Task ModifyComponent_RemoveTransform_ReturnsError()
    {
        var sceneId = await GetFirstSceneIdAsync();
        await CallToolAndParseJsonAsync("open_scene", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
        });

        var entityId = await GetFirstEntityIdAsync(sceneId);

        var root = await CallToolAndParseJsonAsync("modify_component", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["entityId"] = entityId,
            ["action"] = "remove",
            ["componentIndex"] = 0,
        });

        Assert.False(string.IsNullOrEmpty(root.GetProperty("error").GetString()));
    }

    [McpIntegrationFact]
    public async Task ModifyComponent_InvalidAction_ReturnsError()
    {
        var sceneId = await GetFirstSceneIdAsync();
        await CallToolAndParseJsonAsync("open_scene", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
        });

        var entityId = await GetFirstEntityIdAsync(sceneId);

        var root = await CallToolAndParseJsonAsync("modify_component", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["entityId"] = entityId,
            ["action"] = "invalid",
        });

        Assert.False(string.IsNullOrEmpty(root.GetProperty("error").GetString()));
    }

    // =====================
    // Viewport
    // =====================

    [McpIntegrationFact]
    public async Task CaptureViewport_WithSceneNotOpen_ReturnsError()
    {
        var result = await _client!.CallToolAsync("capture_viewport", new Dictionary<string, object?>
        {
            ["assetId"] = "00000000-0000-0000-0000-000000000001",
        });

        var textBlock = result.Content.OfType<TextContentBlock>().FirstOrDefault();
        Assert.NotNull(textBlock);
        Assert.Contains("not found", textBlock.Text!, StringComparison.OrdinalIgnoreCase);
    }

    // =====================
    // Phase 5: Asset Management
    // =====================

    [McpIntegrationFact]
    public async Task GetAssetDetails_ReturnsAssetProperties()
    {
        // Get a known asset ID from query_assets
        var queryRoot = await CallToolAndParseJsonAsync("query_assets", new Dictionary<string, object?>
        {
            ["maxResults"] = 1,
        });
        var firstAsset = queryRoot.GetProperty("assets")[0];
        var assetId = firstAsset.GetProperty("id").GetString()!;

        var root = await CallToolAndParseJsonAsync("get_asset_details", new Dictionary<string, object?>
        {
            ["assetId"] = assetId,
        });

        Assert.Null(root.GetProperty("error").GetString());

        var asset = root.GetProperty("asset");
        Assert.Equal(assetId, asset.GetProperty("id").GetString());
        Assert.False(string.IsNullOrEmpty(asset.GetProperty("name").GetString()));
        Assert.False(string.IsNullOrEmpty(asset.GetProperty("type").GetString()));
        Assert.True(asset.TryGetProperty("properties", out _));
    }

    [McpIntegrationFact]
    public async Task GetAssetDetails_WithInvalidId_ReturnsError()
    {
        var root = await CallToolAndParseJsonAsync("get_asset_details", new Dictionary<string, object?>
        {
            ["assetId"] = "00000000-0000-0000-0000-000000000000",
        });

        Assert.False(string.IsNullOrEmpty(root.GetProperty("error").GetString()));
    }

    [McpIntegrationFact]
    public async Task GetAssetDependencies_ReturnsDependencyInfo()
    {
        // Get a known asset ID
        var queryRoot = await CallToolAndParseJsonAsync("query_assets", new Dictionary<string, object?>
        {
            ["maxResults"] = 1,
        });
        var assetId = queryRoot.GetProperty("assets")[0].GetProperty("id").GetString()!;

        var root = await CallToolAndParseJsonAsync("get_asset_dependencies", new Dictionary<string, object?>
        {
            ["assetId"] = assetId,
        });

        Assert.Null(root.GetProperty("error").GetString());

        var deps = root.GetProperty("dependencies");
        Assert.Equal(assetId, deps.GetProperty("assetId").GetString());
        Assert.True(deps.TryGetProperty("referencedBy", out _));
        Assert.True(deps.TryGetProperty("references", out _));
        Assert.True(deps.TryGetProperty("brokenReferences", out _));
    }

    [McpIntegrationFact]
    public async Task GetAssetDependencies_WithInvalidId_ReturnsError()
    {
        var root = await CallToolAndParseJsonAsync("get_asset_dependencies", new Dictionary<string, object?>
        {
            ["assetId"] = "00000000-0000-0000-0000-000000000000",
        });

        Assert.False(string.IsNullOrEmpty(root.GetProperty("error").GetString()));
    }

    [McpIntegrationFact]
    public async Task CreateAsset_CreatesAndDeletesMaterial()
    {
        var root = await CallToolAndParseJsonAsync("create_asset", new Dictionary<string, object?>
        {
            ["assetType"] = "MaterialAsset",
            ["name"] = "McpTestMaterial",
        });

        Assert.Null(root.GetProperty("error").GetString());
        var asset = root.GetProperty("asset");
        Assert.False(string.IsNullOrEmpty(asset.GetProperty("id").GetString()));
        Assert.Equal("McpTestMaterial", asset.GetProperty("name").GetString());
        Assert.Equal("MaterialAsset", asset.GetProperty("type").GetString());

        // Clean up: delete the asset
        var assetId = asset.GetProperty("id").GetString()!;
        await CallToolAndParseJsonAsync("manage_asset", new Dictionary<string, object?>
        {
            ["assetId"] = assetId,
            ["action"] = "delete",
        });
    }

    [McpIntegrationFact]
    public async Task CreateAsset_WithInvalidType_ReturnsError()
    {
        var root = await CallToolAndParseJsonAsync("create_asset", new Dictionary<string, object?>
        {
            ["assetType"] = "NonExistentAssetType",
            ["name"] = "ShouldFail",
        });

        Assert.False(string.IsNullOrEmpty(root.GetProperty("error").GetString()));
    }

    [McpIntegrationFact]
    public async Task ManageAsset_RenameAsset()
    {
        // Create an asset to rename
        var createRoot = await CallToolAndParseJsonAsync("create_asset", new Dictionary<string, object?>
        {
            ["assetType"] = "MaterialAsset",
            ["name"] = "McpRenameMe",
        });
        var assetId = createRoot.GetProperty("asset").GetProperty("id").GetString()!;

        // Rename it
        var root = await CallToolAndParseJsonAsync("manage_asset", new Dictionary<string, object?>
        {
            ["assetId"] = assetId,
            ["action"] = "rename",
            ["newName"] = "McpRenamed",
        });

        Assert.Null(root.GetProperty("error").GetString());
        var result = root.GetProperty("result");
        Assert.Equal("renamed", result.GetProperty("action").GetString());
        Assert.Equal("McpRenameMe", result.GetProperty("oldName").GetString());
        Assert.Equal("McpRenamed", result.GetProperty("newName").GetString());

        // Clean up
        await CallToolAndParseJsonAsync("manage_asset", new Dictionary<string, object?>
        {
            ["assetId"] = assetId,
            ["action"] = "delete",
        });
    }

    [McpIntegrationFact]
    public async Task ManageAsset_InvalidAction_ReturnsError()
    {
        var queryRoot = await CallToolAndParseJsonAsync("query_assets", new Dictionary<string, object?>
        {
            ["maxResults"] = 1,
        });
        var assetId = queryRoot.GetProperty("assets")[0].GetProperty("id").GetString()!;

        var root = await CallToolAndParseJsonAsync("manage_asset", new Dictionary<string, object?>
        {
            ["assetId"] = assetId,
            ["action"] = "invalid_action",
        });

        Assert.False(string.IsNullOrEmpty(root.GetProperty("error").GetString()));
    }

    [McpIntegrationFact]
    public async Task SetAssetProperty_WithInvalidPath_ReturnsError()
    {
        // Use a MaterialAsset — it always has a property graph, unlike some source-file assets
        var queryRoot = await CallToolAndParseJsonAsync("query_assets", new Dictionary<string, object?>
        {
            ["type"] = "MaterialAsset",
            ["maxResults"] = 1,
        });
        var assetId = queryRoot.GetProperty("assets")[0].GetProperty("id").GetString()!;

        var root = await CallToolAndParseJsonAsync("set_asset_property", new Dictionary<string, object?>
        {
            ["assetId"] = assetId,
            ["propertyPath"] = "NonExistentProperty",
            ["value"] = "42",
        });

        Assert.False(string.IsNullOrEmpty(root.GetProperty("error").GetString()));
        // The error should include available property names
        Assert.Contains("Available properties", root.GetProperty("error").GetString()!);
    }

    [McpIntegrationFact]
    public async Task SaveProject_ReturnsSaveResult()
    {
        var root = await CallToolAndParseJsonAsync("save_project");

        // save_project should either succeed or return a meaningful error
        var result = root.GetProperty("result");
        Assert.True(result.TryGetProperty("status", out var status));
        Assert.False(string.IsNullOrEmpty(status.GetString()));
    }

    // =====================
    // Phase 4: Build Tools
    // =====================

    [McpIntegrationFact]
    public async Task GetBuildStatus_WhenIdle_ReturnsIdle()
    {
        var root = await CallToolAndParseJsonAsync("get_build_status");
        // Status should be idle or reflect a prior build
        Assert.True(root.TryGetProperty("status", out var status));
        Assert.False(string.IsNullOrEmpty(status.GetString()));
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

        // Phase 3 tools (Modification)
        Assert.Contains("create_entity", toolNames);
        Assert.Contains("delete_entity", toolNames);
        Assert.Contains("reparent_entity", toolNames);
        Assert.Contains("set_transform", toolNames);
        Assert.Contains("modify_component", toolNames);

        // Viewport tools
        Assert.Contains("capture_viewport", toolNames);

        // Phase 4 tools (Build)
        Assert.Contains("build_project", toolNames);
        Assert.Contains("get_build_status", toolNames);

        // Phase 5 tools (Asset Management)
        Assert.Contains("get_asset_details", toolNames);
        Assert.Contains("get_asset_dependencies", toolNames);
        Assert.Contains("create_asset", toolNames);
        Assert.Contains("manage_asset", toolNames);
        Assert.Contains("set_asset_property", toolNames);
        Assert.Contains("save_project", toolNames);

        // Reload tools
        Assert.Contains("reload_scene", toolNames);
        Assert.Contains("reload_project", toolNames);

        // UI Page tools
        Assert.Contains("get_ui_tree", toolNames);
        Assert.Contains("get_ui_element", toolNames);
        Assert.Contains("add_ui_element", toolNames);
        Assert.Contains("remove_ui_element", toolNames);
        Assert.Contains("set_ui_element_property", toolNames);

        // Sprite tools
        Assert.Contains("add_sprite_frame", toolNames);
        Assert.Contains("remove_sprite_frame", toolNames);

        // Project tools
        Assert.Contains("set_active_project", toolNames);

        // UI navigation
        Assert.Contains("open_ui_page", toolNames);
    }

    // =====================
    // Reload Tools
    // =====================

    [McpIntegrationFact]
    public async Task ReloadScene_ReloadsOpenScene()
    {
        var sceneId = await GetFirstSceneIdAsync();
        await CallToolAndParseJsonAsync("open_scene", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
        });

        var root = await CallToolAndParseJsonAsync("reload_scene", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
        });

        Assert.Null(root.GetProperty("error").GetString());
        var result = root.GetProperty("result");
        Assert.Equal("reloaded", result.GetProperty("status").GetString());
        Assert.Equal(sceneId, result.GetProperty("sceneId").GetString());
    }

    [McpIntegrationFact]
    public async Task ReloadScene_WithInvalidId_ReturnsError()
    {
        var root = await CallToolAndParseJsonAsync("reload_scene", new Dictionary<string, object?>
        {
            ["sceneId"] = "00000000-0000-0000-0000-000000000000",
        });

        Assert.False(string.IsNullOrEmpty(root.GetProperty("error").GetString()));
    }

    // =====================
    // Asset Reference Update
    // =====================

    [McpIntegrationFact]
    public async Task ModifyComponent_UpdateWithAssetReference()
    {
        var sceneId = await GetFirstSceneIdAsync();
        await CallToolAndParseJsonAsync("open_scene", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
        });

        // Create a test entity with a ModelComponent
        var createResult = await CallToolAndParseJsonAsync("create_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["name"] = "McpAssetRefTest",
        });
        var entityId = createResult.GetProperty("entity").GetProperty("id").GetString()!;

        await CallToolAndParseJsonAsync("modify_component", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["entityId"] = entityId,
            ["action"] = "add",
            ["componentType"] = "ModelComponent",
        });

        // Find a model asset to reference
        var queryRoot = await CallToolAndParseJsonAsync("query_assets", new Dictionary<string, object?>
        {
            ["type"] = "ModelAsset",
            ["maxResults"] = 1,
        });

        var assets = queryRoot.GetProperty("assets");
        if (assets.GetArrayLength() > 0)
        {
            var modelAssetId = assets[0].GetProperty("id").GetString()!;

            // Update ModelComponent.Model with asset reference
            var updateRoot = await CallToolAndParseJsonAsync("modify_component", new Dictionary<string, object?>
            {
                ["sceneId"] = sceneId,
                ["entityId"] = entityId,
                ["action"] = "update",
                ["componentIndex"] = 1,
                ["properties"] = JsonSerializer.Serialize(new { Model = new { assetId = modelAssetId } }),
            });

            Assert.Null(updateRoot.GetProperty("error").GetString());
            var component = updateRoot.GetProperty("component");
            Assert.Contains("Model", component.GetProperty("updatedProperties").EnumerateArray().Select(e => e.GetString()!));
        }

        // Clean up
        await CallToolAndParseJsonAsync("delete_entity", new Dictionary<string, object?>
        {
            ["sceneId"] = sceneId,
            ["entityId"] = entityId,
        });
    }

    // =====================
    // UI Page Tools
    // =====================

    [McpIntegrationFact]
    public async Task GetUITree_WithInvalidId_ReturnsError()
    {
        var root = await CallToolAndParseJsonAsync("get_ui_tree", new Dictionary<string, object?>
        {
            ["assetId"] = "00000000-0000-0000-0000-000000000000",
        });

        Assert.False(string.IsNullOrEmpty(root.GetProperty("error").GetString()));
    }

    [McpIntegrationFact]
    public async Task AddUIElement_CreatesAndRemovesElement()
    {
        // Create a UIPageAsset
        var createAssetRoot = await CallToolAndParseJsonAsync("create_asset", new Dictionary<string, object?>
        {
            ["assetType"] = "UIPageAsset",
            ["name"] = "McpTestUIPage",
        });

        Assert.Null(createAssetRoot.GetProperty("error").GetString());
        var uiPageId = createAssetRoot.GetProperty("asset").GetProperty("id").GetString()!;

        // Verify tree is accessible (new UIPageAsset may have a default root element)
        var treeRoot = await CallToolAndParseJsonAsync("get_ui_tree", new Dictionary<string, object?>
        {
            ["assetId"] = uiPageId,
        });

        Assert.Null(treeRoot.GetProperty("error").GetString());
        var uiPage = treeRoot.GetProperty("uiPage");
        var initialCount = uiPage.GetProperty("elementCount").GetInt32();

        // Add a StackPanel as root
        var addPanelRoot = await CallToolAndParseJsonAsync("add_ui_element", new Dictionary<string, object?>
        {
            ["assetId"] = uiPageId,
            ["elementType"] = "StackPanel",
            ["name"] = "MainPanel",
        });

        Assert.Null(addPanelRoot.GetProperty("error").GetString());
        var panelElement = addPanelRoot.GetProperty("element");
        Assert.Equal("StackPanel", panelElement.GetProperty("type").GetString());
        Assert.Equal("MainPanel", panelElement.GetProperty("name").GetString());
        var panelId = panelElement.GetProperty("id").GetString()!;

        // Add a TextBlock under the StackPanel
        var addTextRoot = await CallToolAndParseJsonAsync("add_ui_element", new Dictionary<string, object?>
        {
            ["assetId"] = uiPageId,
            ["elementType"] = "TextBlock",
            ["name"] = "MyText",
            ["parentId"] = panelId,
        });

        Assert.Null(addTextRoot.GetProperty("error").GetString());
        var textElement = addTextRoot.GetProperty("element");
        Assert.Equal("TextBlock", textElement.GetProperty("type").GetString());
        Assert.Equal(panelId, textElement.GetProperty("parentId").GetString());
        var textId = textElement.GetProperty("id").GetString()!;

        // Verify hierarchy via get_ui_tree
        var treeAfterAdd = await CallToolAndParseJsonAsync("get_ui_tree", new Dictionary<string, object?>
        {
            ["assetId"] = uiPageId,
        });

        Assert.Null(treeAfterAdd.GetProperty("error").GetString());
        var pageAfterAdd = treeAfterAdd.GetProperty("uiPage");
        Assert.Equal(initialCount + 2, pageAfterAdd.GetProperty("elementCount").GetInt32());

        // Find our StackPanel in the root elements and verify it has the TextBlock as a child
        var rootElements = pageAfterAdd.GetProperty("elements");
        var panelNode = rootElements.EnumerateArray()
            .FirstOrDefault(e => e.GetProperty("id").GetString() == panelId);
        Assert.NotEqual(default, panelNode);
        Assert.True(panelNode.GetProperty("children").GetArrayLength() > 0);

        // Inspect element via get_ui_element
        var getElementRoot = await CallToolAndParseJsonAsync("get_ui_element", new Dictionary<string, object?>
        {
            ["assetId"] = uiPageId,
            ["elementId"] = textId,
        });

        Assert.Null(getElementRoot.GetProperty("error").GetString());
        var detail = getElementRoot.GetProperty("element");
        Assert.Equal("TextBlock", detail.GetProperty("type").GetString());
        Assert.Equal(panelId, detail.GetProperty("parentId").GetString());
        Assert.True(detail.TryGetProperty("properties", out _));

        // Remove the TextBlock
        var removeRoot = await CallToolAndParseJsonAsync("remove_ui_element", new Dictionary<string, object?>
        {
            ["assetId"] = uiPageId,
            ["elementId"] = textId,
        });

        Assert.Null(removeRoot.GetProperty("error").GetString());
        var removed = removeRoot.GetProperty("removed");
        Assert.Equal(textId, removed.GetProperty("id").GetString());

        // Clean up: delete the UI page asset
        await CallToolAndParseJsonAsync("manage_asset", new Dictionary<string, object?>
        {
            ["assetId"] = uiPageId,
            ["action"] = "delete",
        });
    }

    [McpIntegrationFact]
    public async Task SetUIElementProperty_SetsPropertyOnElement()
    {
        // Create a UIPageAsset
        var createAssetRoot = await CallToolAndParseJsonAsync("create_asset", new Dictionary<string, object?>
        {
            ["assetType"] = "UIPageAsset",
            ["name"] = "McpTestUIPageProp",
        });
        var uiPageId = createAssetRoot.GetProperty("asset").GetProperty("id").GetString()!;

        // Add a TextBlock
        var addTextRoot = await CallToolAndParseJsonAsync("add_ui_element", new Dictionary<string, object?>
        {
            ["assetId"] = uiPageId,
            ["elementType"] = "TextBlock",
            ["name"] = "PropTest",
        });
        var textId = addTextRoot.GetProperty("element").GetProperty("id").GetString()!;

        // Set width property
        var setRoot = await CallToolAndParseJsonAsync("set_ui_element_property", new Dictionary<string, object?>
        {
            ["assetId"] = uiPageId,
            ["elementId"] = textId,
            ["propertyPath"] = "Width",
            ["value"] = "200",
        });

        Assert.Null(setRoot.GetProperty("error").GetString());
        var setResult = setRoot.GetProperty("result");
        Assert.Equal("Width", setResult.GetProperty("propertyPath").GetString());

        // Clean up
        await CallToolAndParseJsonAsync("manage_asset", new Dictionary<string, object?>
        {
            ["assetId"] = uiPageId,
            ["action"] = "delete",
        });
    }

    [McpIntegrationFact]
    public async Task OpenUIPage_OpensAndCapturesViewport()
    {
        // Create a UIPageAsset
        var createAssetRoot = await CallToolAndParseJsonAsync("create_asset", new Dictionary<string, object?>
        {
            ["assetType"] = "UIPageAsset",
            ["name"] = "McpTestUIPageViewport",
        });

        Assert.Null(createAssetRoot.GetProperty("error").GetString());
        var uiPageId = createAssetRoot.GetProperty("asset").GetProperty("id").GetString()!;

        // Open the UI page in the editor
        var openRoot = await CallToolAndParseJsonAsync("open_ui_page", new Dictionary<string, object?>
        {
            ["assetId"] = uiPageId,
        });

        Assert.Equal("opened", openRoot.GetProperty("status").GetString());
        Assert.Equal("McpTestUIPageViewport", openRoot.GetProperty("name").GetString());

        // Wait for the editor to initialize
        await Task.Delay(3000);

        // Capture the viewport
        var captureResult = await _client!.CallToolAsync("capture_viewport", new Dictionary<string, object?>
        {
            ["assetId"] = uiPageId,
        });

        // The result should contain either an image or a text error (editor may not be fully initialized)
        Assert.NotNull(captureResult.Content);
        Assert.True(captureResult.Content.Count > 0);

        // Clean up
        await CallToolAndParseJsonAsync("manage_asset", new Dictionary<string, object?>
        {
            ["assetId"] = uiPageId,
            ["action"] = "delete",
        });
    }

    [McpIntegrationFact]
    public async Task OpenUIPage_WithInvalidId_ReturnsError()
    {
        var root = await CallToolAndParseJsonAsync("open_ui_page", new Dictionary<string, object?>
        {
            ["assetId"] = "00000000-0000-0000-0000-000000000000",
        });

        Assert.False(string.IsNullOrEmpty(root.GetProperty("error").GetString()));
    }

    // =====================
    // Sprite Frame Tools
    // =====================

    [McpIntegrationFact]
    public async Task AddSpriteFrame_AddsAndRemovesFrame()
    {
        // Create a SpriteSheetAsset
        var createAssetRoot = await CallToolAndParseJsonAsync("create_asset", new Dictionary<string, object?>
        {
            ["assetType"] = "SpriteSheetAsset",
            ["name"] = "McpTestSpriteSheet",
        });

        Assert.Null(createAssetRoot.GetProperty("error").GetString());
        var spriteSheetId = createAssetRoot.GetProperty("asset").GetProperty("id").GetString()!;

        // Add a sprite frame
        var addRoot = await CallToolAndParseJsonAsync("add_sprite_frame", new Dictionary<string, object?>
        {
            ["assetId"] = spriteSheetId,
            ["name"] = "Frame1",
        });

        Assert.Null(addRoot.GetProperty("error").GetString());
        var frame = addRoot.GetProperty("frame");
        Assert.Equal("Frame1", frame.GetProperty("name").GetString());
        Assert.Equal(0, frame.GetProperty("index").GetInt32());
        Assert.Equal(1, frame.GetProperty("totalFrames").GetInt32());

        // Add another frame
        var addRoot2 = await CallToolAndParseJsonAsync("add_sprite_frame", new Dictionary<string, object?>
        {
            ["assetId"] = spriteSheetId,
            ["name"] = "Frame2",
            ["textureRegionX"] = 0,
            ["textureRegionY"] = 0,
            ["textureRegionWidth"] = 64,
            ["textureRegionHeight"] = 64,
        });

        Assert.Null(addRoot2.GetProperty("error").GetString());
        Assert.Equal(1, addRoot2.GetProperty("frame").GetProperty("index").GetInt32());
        Assert.Equal(2, addRoot2.GetProperty("frame").GetProperty("totalFrames").GetInt32());

        // Remove the first frame
        var removeRoot = await CallToolAndParseJsonAsync("remove_sprite_frame", new Dictionary<string, object?>
        {
            ["assetId"] = spriteSheetId,
            ["index"] = 0,
        });

        Assert.Null(removeRoot.GetProperty("error").GetString());
        var removed = removeRoot.GetProperty("removed");
        Assert.Equal("Frame1", removed.GetProperty("name").GetString());
        Assert.Equal(1, removed.GetProperty("remainingCount").GetInt32());

        // Clean up
        await CallToolAndParseJsonAsync("manage_asset", new Dictionary<string, object?>
        {
            ["assetId"] = spriteSheetId,
            ["action"] = "delete",
        });
    }

    [McpIntegrationFact]
    public async Task RemoveSpriteFrame_WithInvalidIndex_ReturnsError()
    {
        // Create a SpriteSheetAsset
        var createAssetRoot = await CallToolAndParseJsonAsync("create_asset", new Dictionary<string, object?>
        {
            ["assetType"] = "SpriteSheetAsset",
            ["name"] = "McpTestSpriteSheetErr",
        });
        var spriteSheetId = createAssetRoot.GetProperty("asset").GetProperty("id").GetString()!;

        // Try to remove from empty sprite sheet
        var root = await CallToolAndParseJsonAsync("remove_sprite_frame", new Dictionary<string, object?>
        {
            ["assetId"] = spriteSheetId,
            ["index"] = 0,
        });

        Assert.False(string.IsNullOrEmpty(root.GetProperty("error").GetString()));

        // Clean up
        await CallToolAndParseJsonAsync("manage_asset", new Dictionary<string, object?>
        {
            ["assetId"] = spriteSheetId,
            ["action"] = "delete",
        });
    }

    // =====================
    // Project Tools
    // =====================

    [McpIntegrationFact]
    public async Task GetEditorStatus_IncludesProjects()
    {
        var root = await CallToolAndParseJsonAsync("get_editor_status");

        Assert.True(root.TryGetProperty("projects", out var projects));
        Assert.True(projects.GetArrayLength() > 0, "Should have at least one project");

        var firstProject = projects[0];
        Assert.False(string.IsNullOrEmpty(firstProject.GetProperty("name").GetString()));
        Assert.False(string.IsNullOrEmpty(firstProject.GetProperty("type").GetString()));
        Assert.False(string.IsNullOrEmpty(firstProject.GetProperty("platform").GetString()));
        Assert.True(firstProject.TryGetProperty("isCurrentProject", out _));
    }

    [McpIntegrationFact]
    public async Task SetActiveProject_WithInvalidName_ReturnsError()
    {
        var root = await CallToolAndParseJsonAsync("set_active_project", new Dictionary<string, object?>
        {
            ["projectName"] = "NonExistentProject_99999",
        });

        Assert.False(string.IsNullOrEmpty(root.GetProperty("error").GetString()));
        Assert.Contains("Available projects", root.GetProperty("error").GetString()!);
    }

    [McpIntegrationFact]
    public async Task SetActiveProject_ChangesActiveProject()
    {
        // Get current projects
        var statusRoot = await CallToolAndParseJsonAsync("get_editor_status");
        var projects = statusRoot.GetProperty("projects");

        if (projects.GetArrayLength() < 1)
        {
            // Skip if no projects available
            return;
        }

        // Set the first project as active (may already be active, but validates the command works)
        var projectName = projects[0].GetProperty("name").GetString()!;
        var root = await CallToolAndParseJsonAsync("set_active_project", new Dictionary<string, object?>
        {
            ["projectName"] = projectName,
        });

        Assert.Null(root.GetProperty("error").GetString());
        var project = root.GetProperty("project");
        Assert.Equal(projectName, project.GetProperty("name").GetString());
        Assert.True(project.GetProperty("isCurrentProject").GetBoolean());
    }

    private async Task<JsonElement> CallToolAndParseJsonAsync(
        string toolName,
        Dictionary<string, object?>? arguments = null)
    {
        var result = await _client!.CallToolAsync(toolName, arguments);
        var textBlock = result.Content.OfType<TextContentBlock>().First();
        var text = textBlock.Text!;
        try
        {
            var doc = JsonDocument.Parse(text);
            return doc.RootElement;
        }
        catch (JsonException ex)
        {
            throw new JsonException($"Failed to parse response from '{toolName}' as JSON: {text}", ex);
        }
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
