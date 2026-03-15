// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core.Settings;

namespace Stride.GameStudio.Mcp;

/// <summary>
/// Per-project MCP server settings, stored in the .sdpkg.user file.
/// </summary>
public static class McpProjectSettings
{
    public static SettingsKey<bool> McpServerEnabled = new SettingsKey<bool>("Package/Mcp/Enabled", PackageUserSettings.SettingsContainer, false)
    {
        DisplayName = "MCP server enabled (experimental)",
        Description = "Hosts an MCP (Model Context Protocol) server that allows AI agents to interact with the editor. Requires restart.",
    };

    public static SettingsKey<int> McpServerPort = new SettingsKey<int>("Package/Mcp/Port", PackageUserSettings.SettingsContainer, 0)
    {
        DisplayName = "MCP server port",
        Description = "TCP port for the MCP server. Set to 0 for automatic port selection (recommended — supports multiple instances). Requires restart.",
    };
}
