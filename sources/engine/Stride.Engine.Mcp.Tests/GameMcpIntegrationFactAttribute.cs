// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Engine.Mcp.Tests;

/// <summary>
/// A custom Fact attribute that skips the test unless the STRIDE_MCP_GAME_INTEGRATION_TESTS
/// environment variable is set to "true". This ensures integration tests that require
/// a running game instance with MCP enabled are disabled by default.
/// </summary>
public sealed class GameMcpIntegrationFactAttribute : FactAttribute
{
    private const string EnvVar = "STRIDE_MCP_GAME_INTEGRATION_TESTS";

    public GameMcpIntegrationFactAttribute()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(EnvVar), "true", StringComparison.OrdinalIgnoreCase))
        {
            Skip = $"Game MCP integration tests are disabled. Set {EnvVar}=true and ensure a game is running with MCP enabled.";
        }
    }
}
