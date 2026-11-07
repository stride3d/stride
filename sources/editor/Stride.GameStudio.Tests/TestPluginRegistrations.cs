// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.ExceptionServices;
using System.Windows;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Presentation;
using Stride.Assets.Templates;
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
    [SkippableFact]
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
                // The WPF plugins take their template providers from resource dictionaries, whose order changes between runs
                var registrations = PluginRegistrations.Of(plugin, typeof(IEditorView)).Describe(sortTemplateProviders: true);
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
        yield return CreateDefaultAssetsPlugin();
        yield return Create(typeof(PluginService).Assembly, "Stride.GameStudio.Plugin.StrideEditorPlugin");
        yield return new UIEditorPlugin();
        yield return new UIEditorWpfPlugin();
        yield return new AudioEditorWpfPlugin();
        yield return new SpriteStudioEditorWpfPlugin();
    }

    private static StrideDefaultAssetsWpfPlugin CreateDefaultAssetsPlugin()
    {
        try
        {
            return new StrideDefaultAssetsWpfPlugin();
        }
        catch (InvalidOperationException e) when (IsMissingTemplatesPackage(e))
        {
            // The plugin loads the asset templates from the package store, which a CI machine doesn't have: the templates then come
            // from the repository's package file, and the plugin keeps them
            var packageFile = FindRepositoryTemplatesPackage()
                ?? throw new SkipException($"The {StrideDefaultTemplates.AssetTemplatesPackageName} package is neither in the package store nor in a repository above this test: {e.Message}");
            StrideDefaultTemplates.LoadAssetTemplates(packageFile, loadAssemblyReferences: false);
            return new StrideDefaultAssetsWpfPlugin();
        }
    }

    private static bool IsMissingTemplatesPackage(InvalidOperationException e)
    {
        return e.Message.StartsWith($"Could not find package {StrideDefaultTemplates.AssetTemplatesPackageName} ", StringComparison.Ordinal);
    }

    private static string? FindRepositoryTemplatesPackage()
    {
        var relativePath = Path.Combine("sources", "editor", StrideDefaultTemplates.AssetTemplatesPackageName, $"{StrideDefaultTemplates.AssetTemplatesPackageName}.sdpkg");
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var packageFile = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(packageFile))
                return packageFile;
        }
        return null;
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
