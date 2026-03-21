// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Engine.Mcp.Tests;

/// <summary>
/// Defines the xUnit test collection for game MCP integration tests.
/// All test classes using [Collection("GameMcpIntegration")] share a single
/// <see cref="GameFixture"/> instance (one game process for all tests).
/// </summary>
[CollectionDefinition("GameMcpIntegration")]
public class GameMcpIntegrationCollection : ICollectionFixture<GameFixture>
{
}
