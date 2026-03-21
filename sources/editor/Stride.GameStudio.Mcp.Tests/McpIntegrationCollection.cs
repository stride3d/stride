// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.GameStudio.Mcp.Tests;

/// <summary>
/// Defines the xUnit test collection for MCP integration tests.
/// All test classes using [Collection("McpIntegration")] share a single
/// <see cref="GameStudioFixture"/> instance (one GameStudio process for all tests).
/// </summary>
[CollectionDefinition("McpIntegration")]
public class McpIntegrationCollection : ICollectionFixture<GameStudioFixture>
{
}
