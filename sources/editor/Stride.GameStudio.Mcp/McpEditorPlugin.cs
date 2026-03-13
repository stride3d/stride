// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Diagnostics;
using Stride.Editor;

namespace Stride.GameStudio.Mcp;

/// <summary>
/// MCP (Model Context Protocol) plugin for Stride Game Studio.
/// Hosts an HTTP/SSE MCP server that exposes editor functionality as tools
/// for LLM agents to interact with.
/// </summary>
public sealed class McpEditorPlugin : StrideAssetsPlugin
{
    private static readonly Logger Log = GlobalLogger.GetLogger("McpPlugin");

    protected override void Initialize(ILogger logger)
    {
        // No static initialization needed
    }

    public override void InitializeSession(SessionViewModel session)
    {
        try
        {
            var mcpService = new McpServerService(session);
            session.ServiceProvider.RegisterService(mcpService);
            mcpService.StartAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Log.Error("Failed to start MCP server", t.Exception?.InnerException ?? t.Exception);
                }
            });
        }
        catch (Exception ex)
        {
            Log.Error("Failed to initialize MCP plugin", ex);
        }
    }

    protected override void SessionDisposed(SessionViewModel session)
    {
        try
        {
            var mcpService = session.ServiceProvider.TryGet<McpServerService>();
            mcpService?.StopAsync().Wait(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            Log.Error("Error stopping MCP server", ex);
        }

        base.SessionDisposed(session);
    }

    public override void RegisterAssetPreviewViewTypes(IDictionary<Type, Type> assetPreviewViewTypes)
    {
        // No preview types to register
    }
}
