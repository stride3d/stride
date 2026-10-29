// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.ExceptionServices;
using System.Windows;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Presentation;
using Stride.Audio.Editor;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Presentation.Views;
using Stride.GameStudio.Services;
using Stride.SpriteStudio.Editor;
using Stride.UI.Editor;
using Stride.UI.Editor.Views;
using Xunit;

#nullable enable

namespace Stride.GameStudio.Tests;

/// <summary>
/// The built-in plugins of the WPF Game Studio register what they registered before they moved to the plugin context:
/// the recorded registrations are in the <c>Registrations</c> folder.
/// </summary>
public class TestPluginRegistrations
{
    [Fact]
    public void TheWpfPluginsRegisterTheSameAsBefore()
    {
        var differences = new List<string>();
        RunSta(() =>
        {
            // The plugins load their resource dictionaries into the application, as in the Game Studio
            if (Application.Current is null)
                _ = new Application();

            foreach (var plugin in CreatePlugins())
            {
                var registrations = PluginRegistrations.Of(plugin, typeof(IEditorView)).Describe();
                if (RegistrationsGolden.Compare(plugin.GetType().FullName!, registrations) is { } difference)
                    differences.Add(difference);
            }
        });

        Assert.True(differences.Count == 0, string.Join(Environment.NewLine + Environment.NewLine, differences));
    }

    private static IEnumerable<AssetsPlugin> CreatePlugins()
    {
        // The plugins of the core editor and of the Game Studio are internal
        yield return Create(typeof(Stride.Core.Assets.Editor.View.ValueConverters.TypeToResource).Assembly, "Stride.Core.Assets.Editor.CoreAssetsEditorPlugin");
        yield return new StrideDefaultAssetsPlugin();
        yield return Create(typeof(PluginService).Assembly, "Stride.GameStudio.Plugin.StrideEditorPlugin");
        yield return new UIEditorPlugin();
        yield return new UIEditorWpfPlugin();
        yield return new AudioEditorWpfPlugin();
        yield return new SpriteStudioEditorWpfPlugin();
    }

    private static AssetsPlugin Create(System.Reflection.Assembly assembly, string typeName)
    {
        return (AssetsPlugin)Activator.CreateInstance(assembly.GetType(typeName, throwOnError: true)!)!;
    }

    private static void RunSta(Action action)
    {
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                failure = ExceptionDispatchInfo.Capture(exception);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }
}
