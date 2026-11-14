// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Services;

namespace Stride.GameStudio.Avalonia.Commands;

/// <summary>
/// The plugin of this editor's shell: it registers the commands of the main menu and of the main window's shortcuts
/// (<see cref="GameStudioCommands"/>) in the command registry when a session opens. A plugin of the editor's own assembly,
/// so its commands are the editor's: their shortcuts come before the plugins'.
/// </summary>
[EditorDefaultPlugin]
internal sealed class GameStudioCommandsPlugin : AssetsPlugin
{
    private static readonly Lock RegistrationLock = new();
    private static bool registered;

    /// <inheritdoc />
    /// <remarks>None: it only registers commands.</remarks>
    public override IReadOnlyList<Assembly> ScannedAssemblies => [];

    /// <summary>
    /// Registers the plugin with the plugins of the process once, before the shell creates its plugin host.
    /// </summary>
    public static void EnsureRegistered()
    {
        lock (RegistrationLock)
        {
            if (registered)
                return;
            registered = true;
            RegisterPlugin(typeof(GameStudioCommandsPlugin));
        }
    }

    /// <inheritdoc />
    public override void Initialize(IEditorPluginContext context)
    {
        base.Initialize(context);
        foreach (var command in GameStudioCommands.Create())
            context.Commands.Register(command);
    }
}
