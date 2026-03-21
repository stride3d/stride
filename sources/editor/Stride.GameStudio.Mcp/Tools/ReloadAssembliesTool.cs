// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Commands;

namespace Stride.GameStudio.Mcp.Tools;

[McpServerToolType]
public sealed class ReloadAssembliesTool
{
    [McpServerTool(Name = "reload_assemblies"), Description("Reloads game assemblies after a build, making user-defined script types (e.g. PlayerController, EnemyAI) available for use in modify_component. This is the MCP equivalent of clicking the blinking 'Reload game assemblies' button in the toolbar. Call this after build_project completes successfully — without it, newly built script types won't be discoverable. Actions: 'status' checks if a reload is pending, 'reload' triggers the reload. After reload, user script types become available in modify_component's 'add' action.")]
    public static async Task<string> ReloadAssemblies(
        SessionViewModel session,
        DispatcherBridge dispatcher,
        [Description("The action to perform: 'status' (check if reload is pending) or 'reload' (trigger assembly reload)")] string action = "reload",
        CancellationToken cancellationToken = default)
    {
        var result = await dispatcher.InvokeTaskOnUIThread(async () =>
        {
            // Access DebuggingViewModel via reflection (it lives in Stride.GameStudio which we don't reference)
            var editorVm = EditorViewModel.Instance;
            if (editorVm == null)
            {
                return new { error = "EditorViewModel is not available.", result = (object?)null };
            }

            var debuggingProp = editorVm.GetType().GetProperty("Debugging");
            if (debuggingProp == null)
            {
                return new { error = "Debugging property not found on the editor view model.", result = (object?)null };
            }

            var debugging = debuggingProp.GetValue(editorVm);
            if (debugging == null)
            {
                return new { error = "DebuggingViewModel is null — editor may still be initializing.", result = (object?)null };
            }

            var reloadCommandProp = debugging.GetType().GetProperty("ReloadAssembliesCommand");
            if (reloadCommandProp == null)
            {
                return new { error = "ReloadAssembliesCommand not found on DebuggingViewModel.", result = (object?)null };
            }

            var reloadCommand = reloadCommandProp.GetValue(debugging) as ICommandBase;
            if (reloadCommand == null)
            {
                return new { error = "ReloadAssembliesCommand is not an ICommandBase.", result = (object?)null };
            }

            bool isPending = reloadCommand.IsEnabled;

            switch (action.ToLowerInvariant())
            {
                case "status":
                    return new
                    {
                        error = (string?)null,
                        result = (object)new
                        {
                            assemblyReloadPending = isPending,
                            message = isPending
                                ? "Game assemblies have changed. Call reload_assemblies with action 'reload' to load the updated scripts."
                                : "No assembly reload pending. Assemblies are up to date.",
                        },
                    };

                case "reload":
                    if (!isPending)
                    {
                        return new
                        {
                            error = (string?)null,
                            result = (object)new
                            {
                                assemblyReloadPending = false,
                                message = "No assembly reload pending. Assemblies are already up to date.",
                            },
                        };
                    }

                    // Execute the reload command — this is an async command that rebuilds if needed,
                    // then unloads old assemblies, loads new ones, and re-analyzes entity components.
                    reloadCommand.Execute();

                    // The command runs asynchronously via AnonymousTaskCommand.
                    // Wait briefly for it to start, then check if it's still enabled (pending).
                    // The reload itself may take several seconds for large projects.
                    await Task.Delay(500, cancellationToken);

                    // After execution, check if the reload completed
                    bool stillPending = reloadCommand.IsEnabled;

                    return new
                    {
                        error = (string?)null,
                        result = (object)new
                        {
                            assemblyReloadPending = stillPending,
                            message = stillPending
                                ? "Assembly reload initiated but still in progress. Check status again shortly."
                                : "Game assemblies reloaded successfully. User script types are now available.",
                        },
                    };

                default:
                    return new
                    {
                        error = $"Unknown action: '{action}'. Expected 'status' or 'reload'.",
                        result = (object?)null,
                    };
            }
        }, cancellationToken);

        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Helper to check if assembly reload is pending, usable from other tools.
    /// </summary>
    internal static bool IsReloadPending()
    {
        var editorVm = EditorViewModel.Instance;
        if (editorVm == null) return false;

        var debuggingProp = editorVm.GetType().GetProperty("Debugging");
        if (debuggingProp == null) return false;

        var debugging = debuggingProp.GetValue(editorVm);
        if (debugging == null) return false;

        var reloadCommandProp = debugging.GetType().GetProperty("ReloadAssembliesCommand");
        if (reloadCommandProp == null) return false;

        var reloadCommand = reloadCommandProp.GetValue(debugging) as ICommandBase;
        return reloadCommand?.IsEnabled ?? false;
    }
}
