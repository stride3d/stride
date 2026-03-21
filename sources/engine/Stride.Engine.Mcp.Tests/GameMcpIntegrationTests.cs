// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Xunit;

namespace Stride.Engine.Mcp.Tests;

/// <summary>
/// Integration tests for the MCP server embedded in a running Stride game.
/// The <see cref="GameFixture"/> automatically launches a test game and waits
/// for the MCP server to become ready. Tests are skipped unless the environment
/// variable STRIDE_MCP_GAME_INTEGRATION_TESTS is set to "true".
/// </summary>
[Collection("GameMcpIntegration")]
public sealed class GameMcpIntegrationTests : IAsyncLifetime
{
    private readonly GameFixture _fixture;
    private McpClient? _client;

    public GameMcpIntegrationTests(GameFixture fixture)
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
            Name = "Stride Game MCP Integration Tests",
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

    private async Task<JsonElement> CallToolAsync(string toolName, Dictionary<string, object?>? args = null)
    {
        Assert.NotNull(_client);

        var response = await _client!.CallToolAsync(toolName, args);

        var textContent = response.Content.OfType<TextContentBlock>().FirstOrDefault();
        Assert.NotNull(textContent);
        return JsonDocument.Parse(textContent!.Text!).RootElement;
    }

    // ==================== Tool Discovery ====================

    [GameMcpIntegrationFact]
    public async Task ListTools_ReturnsAllExpectedTools()
    {
        Assert.NotNull(_client);
        var tools = await _client!.ListToolsAsync();
        var toolNames = tools.Select(t => t.Name).ToList();

        Assert.Contains("get_game_status", toolNames);
        Assert.Contains("get_scene_entities", toolNames);
        Assert.Contains("get_entity", toolNames);
        Assert.Contains("capture_screenshot", toolNames);
        Assert.Contains("get_logs", toolNames);
        Assert.Contains("simulate_input", toolNames);
        Assert.Contains("focus_element", toolNames);
        Assert.Contains("describe_viewport", toolNames);
    }

    // ==================== Status & Inspection ====================

    [GameMcpIntegrationFact]
    public async Task GetGameStatus_ReturnsRunningGame()
    {
        var result = await CallToolAsync("get_game_status");

        Assert.Equal("running", result.GetProperty("status").GetString());
        Assert.True(result.GetProperty("fps").GetDouble() >= 0, "FPS should be non-negative");
        Assert.True(result.GetProperty("entityCount").GetInt32() > 0);
        Assert.False(string.IsNullOrEmpty(result.GetProperty("activeSceneUrl").GetString()));
    }

    [GameMcpIntegrationFact]
    public async Task GetSceneEntities_ReturnsEntityTree()
    {
        var result = await CallToolAsync("get_scene_entities");

        var entities = result.GetProperty("entities");
        Assert.True(entities.GetArrayLength() > 0);

        var firstEntity = entities[0];
        Assert.True(firstEntity.TryGetProperty("id", out _));
        Assert.True(firstEntity.TryGetProperty("name", out _));
        Assert.True(firstEntity.TryGetProperty("children", out _) || firstEntity.TryGetProperty("childCount", out _));
    }

    [GameMcpIntegrationFact]
    public async Task GetSceneEntities_WithMaxDepth()
    {
        var result = await CallToolAsync("get_scene_entities", new Dictionary<string, object?>
        {
            ["maxDepth"] = 1,
        });

        var entities = result.GetProperty("entities");
        Assert.True(entities.GetArrayLength() > 0);

        // At depth 1, children should exist but their children should not go deeper
        var firstEntity = entities[0];
        if (firstEntity.TryGetProperty("children", out var children) && children.GetArrayLength() > 0)
        {
            var child = children[0];
            // At maxDepth=1, the children at depth 1 should not have their own children array
            // (they're at the limit)
            if (child.TryGetProperty("children", out var grandchildren))
            {
                // Grandchildren are at depth 2 which exceeds maxDepth 1 — so this should not have further expansion
                foreach (var gc in grandchildren.EnumerateArray())
                {
                    Assert.False(gc.TryGetProperty("children", out _),
                        "Hierarchy should be truncated at maxDepth");
                }
            }
        }
    }

    [GameMcpIntegrationFact]
    public async Task GetEntity_ByName_ReturnsComponents()
    {
        // First get the entity list to find a known entity name
        var sceneResult = await CallToolAsync("get_scene_entities");
        var entities = sceneResult.GetProperty("entities");
        Assert.True(entities.GetArrayLength() > 0);

        var entityName = entities[0].GetProperty("name").GetString()!;

        var result = await CallToolAsync("get_entity", new Dictionary<string, object?>
        {
            ["entityName"] = entityName,
        });

        Assert.True(result.TryGetProperty("components", out var components));
        Assert.True(components.GetArrayLength() > 0);

        // Should have at least TransformComponent
        var hasTransform = false;
        foreach (var comp in components.EnumerateArray())
        {
            if (comp.GetProperty("type").GetString() == "TransformComponent")
            {
                hasTransform = true;
                break;
            }
        }
        Assert.True(hasTransform, "Entity should have a TransformComponent");
    }

    [GameMcpIntegrationFact]
    public async Task GetEntity_ByInvalidId_ReturnsError()
    {
        var result = await CallToolAsync("get_entity", new Dictionary<string, object?>
        {
            ["entityId"] = "00000000-0000-0000-0000-000000000000",
        });

        Assert.True(result.TryGetProperty("error", out _));
    }

    [GameMcpIntegrationFact]
    public async Task GetEntity_ComponentSerialization()
    {
        // Get any entity with a TransformComponent
        var sceneResult = await CallToolAsync("get_scene_entities");
        var entities = sceneResult.GetProperty("entities");
        var entityName = entities[0].GetProperty("name").GetString()!;

        var result = await CallToolAsync("get_entity", new Dictionary<string, object?>
        {
            ["entityName"] = entityName,
        });

        var components = result.GetProperty("components");
        foreach (var comp in components.EnumerateArray())
        {
            if (comp.GetProperty("type").GetString() == "TransformComponent")
            {
                var props = comp.GetProperty("properties");
                // TransformComponent should have Position with x/y/z
                Assert.True(props.TryGetProperty("Position", out var position));
                Assert.True(position.TryGetProperty("x", out _));
                Assert.True(position.TryGetProperty("y", out _));
                Assert.True(position.TryGetProperty("z", out _));
                return;
            }
        }

        Assert.Fail("No TransformComponent found to verify serialization");
    }

    // ==================== Screenshot ====================

    [GameMcpIntegrationFact]
    public async Task CaptureScreenshot_ReturnsBase64Png()
    {
        Assert.NotNull(_client);
        var response = await _client!.CallToolAsync("capture_screenshot");

        var imageContent = response.Content.OfType<ImageContentBlock>().FirstOrDefault();
        Assert.NotNull(imageContent);
        Assert.Equal("image/png", imageContent!.MimeType);

        // Decode and verify PNG header
        var bytes = Convert.FromBase64String(imageContent.Data!);
        Assert.True(bytes.Length > 8, "Image should have content");
        // PNG magic bytes: 0x89 0x50 0x4E 0x47
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal(0x50, bytes[1]);
        Assert.Equal(0x4E, bytes[2]);
        Assert.Equal(0x47, bytes[3]);
    }

    // ==================== Logs ====================

    [GameMcpIntegrationFact]
    public async Task GetLogs_ReturnsEntries()
    {
        var result = await CallToolAsync("get_logs");

        Assert.True(result.ValueKind == JsonValueKind.Array);
        if (result.GetArrayLength() > 0)
        {
            var entry = result[0];
            Assert.True(entry.TryGetProperty("timestamp", out _));
            Assert.True(entry.TryGetProperty("module", out _));
            Assert.True(entry.TryGetProperty("level", out _));
            Assert.True(entry.TryGetProperty("message", out _));
        }
    }

    [GameMcpIntegrationFact]
    public async Task GetLogs_WithMinLevel()
    {
        var result = await CallToolAsync("get_logs", new Dictionary<string, object?>
        {
            ["minLevel"] = "Warning",
        });

        Assert.True(result.ValueKind == JsonValueKind.Array);
        foreach (var entry in result.EnumerateArray())
        {
            var level = entry.GetProperty("level").GetString();
            Assert.True(level == "Warning" || level == "Error" || level == "Fatal",
                $"Expected Warning or higher, got {level}");
        }
    }

    [GameMcpIntegrationFact]
    public async Task GetLogs_WithMaxCount()
    {
        var result = await CallToolAsync("get_logs", new Dictionary<string, object?>
        {
            ["maxCount"] = 5,
        });

        Assert.True(result.ValueKind == JsonValueKind.Array);
        Assert.True(result.GetArrayLength() <= 5);
    }

    // ==================== Input Injection ====================

    [GameMcpIntegrationFact]
    public async Task SimulateInput_KeyPress()
    {
        var result = await CallToolAsync("simulate_input", new Dictionary<string, object?>
        {
            ["action"] = "key_press",
            ["key"] = "Space",
        });

        Assert.True(result.GetProperty("success").GetBoolean());
    }

    [GameMcpIntegrationFact]
    public async Task SimulateInput_MouseMove()
    {
        var result = await CallToolAsync("simulate_input", new Dictionary<string, object?>
        {
            ["action"] = "mouse_move",
            ["position"] = "0.5,0.5",
        });

        Assert.True(result.GetProperty("success").GetBoolean());
    }

    [GameMcpIntegrationFact]
    public async Task SimulateInput_MouseClick()
    {
        var result = await CallToolAsync("simulate_input", new Dictionary<string, object?>
        {
            ["action"] = "mouse_click",
            ["button"] = "Left",
        });

        Assert.True(result.GetProperty("success").GetBoolean());
    }

    [GameMcpIntegrationFact]
    public async Task SimulateInput_GamepadButton()
    {
        var result = await CallToolAsync("simulate_input", new Dictionary<string, object?>
        {
            ["action"] = "gamepad_button_press",
            ["gamepadButton"] = "A",
        });

        Assert.True(result.GetProperty("success").GetBoolean());
    }

    [GameMcpIntegrationFact]
    public async Task SimulateInput_GamepadAxis()
    {
        var result = await CallToolAsync("simulate_input", new Dictionary<string, object?>
        {
            ["action"] = "gamepad_axis",
            ["gamepadAxis"] = "LeftThumbX",
            ["axisValue"] = 0.75,
        });

        Assert.True(result.GetProperty("success").GetBoolean());
    }

    [GameMcpIntegrationFact]
    public async Task SimulateInput_InvalidKey_ReturnsError()
    {
        var result = await CallToolAsync("simulate_input", new Dictionary<string, object?>
        {
            ["action"] = "key_press",
            ["key"] = "NotAKey",
        });

        Assert.True(result.TryGetProperty("error", out _));
    }

    // ==================== Focus Element ====================

    [GameMcpIntegrationFact]
    public async Task FocusElement_Entity_ReturnsScreenPosition()
    {
        // Get a known entity name
        var sceneResult = await CallToolAsync("get_scene_entities");
        var entities = sceneResult.GetProperty("entities");
        Assert.True(entities.GetArrayLength() > 0);
        var entityName = entities[0].GetProperty("name").GetString()!;

        var result = await CallToolAsync("focus_element", new Dictionary<string, object?>
        {
            ["target"] = "entity",
            ["entityName"] = entityName,
        });

        // Entity might be off-screen, so check for either success or expected error
        if (result.TryGetProperty("success", out var success) && success.GetBoolean())
        {
            var nx = result.GetProperty("normalizedX").GetDouble();
            var ny = result.GetProperty("normalizedY").GetDouble();
            Assert.InRange(nx, 0.0, 1.0);
            Assert.InRange(ny, 0.0, 1.0);
        }
        else
        {
            // Acceptable if entity is off-screen
            Assert.True(result.TryGetProperty("error", out _));
        }
    }

    [GameMcpIntegrationFact]
    public async Task FocusElement_InvalidEntity_ReturnsError()
    {
        var result = await CallToolAsync("focus_element", new Dictionary<string, object?>
        {
            ["target"] = "entity",
            ["entityName"] = "NonExistentEntity_12345",
        });

        Assert.True(result.TryGetProperty("error", out _));
    }

    [GameMcpIntegrationFact]
    public async Task FocusElement_Entity_ThenMouseClick()
    {
        // Get a known entity name
        var sceneResult = await CallToolAsync("get_scene_entities");
        var entities = sceneResult.GetProperty("entities");
        Assert.True(entities.GetArrayLength() > 0);
        var entityName = entities[0].GetProperty("name").GetString()!;

        // Focus on entity
        var focusResult = await CallToolAsync("focus_element", new Dictionary<string, object?>
        {
            ["target"] = "entity",
            ["entityName"] = entityName,
        });

        // Mouse click (should succeed regardless of focus result)
        var clickResult = await CallToolAsync("simulate_input", new Dictionary<string, object?>
        {
            ["action"] = "mouse_click",
            ["button"] = "Left",
        });

        Assert.True(clickResult.GetProperty("success").GetBoolean());
    }

    // ==================== Focus UI Element ====================

    /// <summary>
    /// Helper to focus a UI element and assert its normalized position is near the expected value.
    /// The TestGame has a Grid root filling the 800x600 virtual resolution with three named elements:
    ///   - TopLeftButton: 100x40, Left/Top aligned, Margin(Left:50, Top:30) → center at virtual (100, 50) → normalized (0.125, 0.083)
    ///   - CenterButton: 200x50, Center/Center aligned → center at virtual (400, 300) → normalized (0.5, 0.5)
    ///   - BottomRightButton: 150x60, Right/Bottom aligned, Margin(Right:50, Bottom:30) → center at virtual (675, 540) → normalized (0.84375, 0.9)
    /// </summary>
    private static void AssertNormalizedPosition(JsonElement result, double expectedNX, double expectedNY, double tolerance = 0.03)
    {
        Assert.True(result.TryGetProperty("success", out var success), $"Expected success but got: {result}");
        Assert.True(success.GetBoolean());

        var nx = result.GetProperty("normalizedX").GetDouble();
        var ny = result.GetProperty("normalizedY").GetDouble();

        Assert.True(Math.Abs(nx - expectedNX) < tolerance,
            $"normalizedX: expected ~{expectedNX:F3} but got {nx:F3} (diff={Math.Abs(nx - expectedNX):F4}). Full response: {result}");
        Assert.True(Math.Abs(ny - expectedNY) < tolerance,
            $"normalizedY: expected ~{expectedNY:F3} but got {ny:F3} (diff={Math.Abs(ny - expectedNY):F4}). Full response: {result}");
    }

    [GameMcpIntegrationFact]
    public async Task FocusElement_UI_TopLeftButton()
    {
        // TopLeftButton: 100x40, HorizontalAlignment=Left, VerticalAlignment=Top, Margin(Left:50, Top:30)
        // Center at virtual (100, 50) in 800x600 → normalized (0.125, 0.0833)
        var result = await CallToolAsync("focus_element", new Dictionary<string, object?>
        {
            ["target"] = "ui",
            ["elementName"] = "TopLeftButton",
        });

        AssertNormalizedPosition(result, 0.125, 0.0833);
    }

    [GameMcpIntegrationFact]
    public async Task FocusElement_UI_CenterButton()
    {
        // CenterButton: 200x50, HorizontalAlignment=Center, VerticalAlignment=Center
        // Center at virtual (400, 300) in 800x600 → normalized (0.5, 0.5)
        var result = await CallToolAsync("focus_element", new Dictionary<string, object?>
        {
            ["target"] = "ui",
            ["elementName"] = "CenterButton",
        });

        AssertNormalizedPosition(result, 0.5, 0.5);
    }

    [GameMcpIntegrationFact]
    public async Task FocusElement_UI_BottomRightButton()
    {
        // BottomRightButton: 150x60, HorizontalAlignment=Right, VerticalAlignment=Bottom, Margin(Right:50, Bottom:30)
        // Center at virtual (675, 540) in 800x600 → normalized (0.84375, 0.9)
        var result = await CallToolAsync("focus_element", new Dictionary<string, object?>
        {
            ["target"] = "ui",
            ["elementName"] = "BottomRightButton",
        });

        AssertNormalizedPosition(result, 0.84375, 0.9);
    }

    [GameMcpIntegrationFact]
    public async Task FocusElement_UI_PositionsAreDifferent()
    {
        // Verify all three buttons return distinct positions (guards against all returning 0,0)
        var topLeft = await CallToolAsync("focus_element", new Dictionary<string, object?>
        {
            ["target"] = "ui",
            ["elementName"] = "TopLeftButton",
        });
        var center = await CallToolAsync("focus_element", new Dictionary<string, object?>
        {
            ["target"] = "ui",
            ["elementName"] = "CenterButton",
        });
        var bottomRight = await CallToolAsync("focus_element", new Dictionary<string, object?>
        {
            ["target"] = "ui",
            ["elementName"] = "BottomRightButton",
        });

        Assert.True(topLeft.GetProperty("success").GetBoolean());
        Assert.True(center.GetProperty("success").GetBoolean());
        Assert.True(bottomRight.GetProperty("success").GetBoolean());

        var tlX = topLeft.GetProperty("normalizedX").GetDouble();
        var cX = center.GetProperty("normalizedX").GetDouble();
        var brX = bottomRight.GetProperty("normalizedX").GetDouble();

        // TopLeft.X < Center.X < BottomRight.X
        Assert.True(tlX < cX, $"TopLeft.X ({tlX:F3}) should be less than Center.X ({cX:F3})");
        Assert.True(cX < brX, $"Center.X ({cX:F3}) should be less than BottomRight.X ({brX:F3})");

        var tlY = topLeft.GetProperty("normalizedY").GetDouble();
        var cY = center.GetProperty("normalizedY").GetDouble();
        var brY = bottomRight.GetProperty("normalizedY").GetDouble();

        // TopLeft.Y < Center.Y < BottomRight.Y
        Assert.True(tlY < cY, $"TopLeft.Y ({tlY:F3}) should be less than Center.Y ({cY:F3})");
        Assert.True(cY < brY, $"Center.Y ({cY:F3}) should be less than BottomRight.Y ({brY:F3})");
    }

    [GameMcpIntegrationFact]
    public async Task FocusElement_InvalidUIElement_ReturnsError()
    {
        var result = await CallToolAsync("focus_element", new Dictionary<string, object?>
        {
            ["target"] = "ui",
            ["elementName"] = "NonExistentElement_12345",
        });

        Assert.True(result.TryGetProperty("error", out _));
    }

    [GameMcpIntegrationFact]
    public async Task FocusElement_UI_ThenMouseClick()
    {
        // Focus on the top-left button, then click
        var focusResult = await CallToolAsync("focus_element", new Dictionary<string, object?>
        {
            ["target"] = "ui",
            ["elementName"] = "TopLeftButton",
        });

        Assert.True(focusResult.TryGetProperty("success", out var success), $"Expected success but got: {focusResult}");
        Assert.True(success.GetBoolean());

        // Mouse click at the focused position (should succeed)
        var clickResult = await CallToolAsync("simulate_input", new Dictionary<string, object?>
        {
            ["action"] = "mouse_click",
            ["button"] = "Left",
        });

        Assert.True(clickResult.GetProperty("success").GetBoolean());
    }

    // ==================== Describe Viewport ====================

    [GameMcpIntegrationFact]
    public async Task DescribeViewport_ReturnsEntities()
    {
        var result = await CallToolAsync("describe_viewport");

        Assert.False(result.TryGetProperty("error", out var err) && err.ValueKind == JsonValueKind.String && err.GetString() != null,
            $"Unexpected error: {result}");

        // Camera info should be present
        var camera = result.GetProperty("camera");
        Assert.True(camera.TryGetProperty("position", out _));
        Assert.True(camera.TryGetProperty("projection", out _));
        Assert.True(camera.TryGetProperty("fieldOfViewDegrees", out _));

        // Should have entities
        var totalEntities = result.GetProperty("totalEntitiesInScene").GetInt32();
        Assert.True(totalEntities > 0, "Expected at least one entity in the scene");

        var entities = result.GetProperty("entities");
        Assert.True(entities.GetArrayLength() > 0, "Expected at least one entity in describe_viewport result");

        // Each entity should have required fields
        var first = entities[0];
        Assert.True(first.TryGetProperty("id", out _));
        Assert.True(first.TryGetProperty("name", out _));
        Assert.True(first.TryGetProperty("worldPosition", out _));
        Assert.True(first.TryGetProperty("distanceToCamera", out _));
        Assert.True(first.TryGetProperty("isVisible", out _));
        Assert.True(first.TryGetProperty("components", out _));
    }

    [GameMcpIntegrationFact]
    public async Task DescribeViewport_VisibleEntitiesHaveScreenPosition()
    {
        var result = await CallToolAsync("describe_viewport");

        var entities = result.GetProperty("entities");
        bool foundVisible = false;

        for (int i = 0; i < entities.GetArrayLength(); i++)
        {
            var entity = entities[i];
            if (entity.GetProperty("isVisible").GetBoolean())
            {
                foundVisible = true;
                Assert.True(entity.TryGetProperty("screenPosition", out var screenPos));
                Assert.NotEqual(JsonValueKind.Null, screenPos.ValueKind);
                var sx = screenPos.GetProperty("x").GetDouble();
                var sy = screenPos.GetProperty("y").GetDouble();
                Assert.InRange(sx, -0.5, 1.5);
                Assert.InRange(sy, -0.5, 1.5);
                break;
            }
        }

        Assert.True(foundVisible, "Expected at least one visible entity in the viewport");
    }

    [GameMcpIntegrationFact]
    public async Task DescribeViewport_MaxEntities_LimitsResults()
    {
        var result = await CallToolAsync("describe_viewport", new Dictionary<string, object?>
        {
            ["maxEntities"] = 2,
        });

        var entities = result.GetProperty("entities");
        Assert.True(entities.GetArrayLength() <= 2, $"Expected at most 2 entities, got {entities.GetArrayLength()}");
    }

    [GameMcpIntegrationFact]
    public async Task DescribeViewport_CameraHasReasonableValues()
    {
        var result = await CallToolAsync("describe_viewport");

        var camera = result.GetProperty("camera");
        var fov = camera.GetProperty("fieldOfViewDegrees").GetDouble();
        Assert.InRange(fov, 1, 179); // Reasonable FOV range

        var nearPlane = camera.GetProperty("nearPlane").GetDouble();
        var farPlane = camera.GetProperty("farPlane").GetDouble();
        Assert.True(nearPlane > 0, "Near plane should be positive");
        Assert.True(farPlane > nearPlane, "Far plane should be greater than near plane");
    }
}
