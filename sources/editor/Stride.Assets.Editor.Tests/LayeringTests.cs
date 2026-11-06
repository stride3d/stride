// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The Presentation projects hold the asset and session view models: they reference neither the editor layer nor a UI
/// toolkit, and the Core one references no Stride engine assets project (see the projects' READMEs).
/// </summary>
public sealed class LayeringTests
{
    // The editor layer, above the Presentation projects
    private static readonly string[] EditorAssemblies = ["Stride.Core.Assets.Editor", "Stride.Editor", "Stride.Assets.Editor"];

    [Fact]
    public void TheAssetViewModelsDoNotReferenceTheEditorLayer()
    {
        var references = ReferencesOf(typeof(MaterialViewModel).Assembly);

        Assert.Contains("Stride.Core.Assets.Presentation", references);
        Assert.DoesNotContain(references, x => EditorAssemblies.Contains(x));
        Assert.DoesNotContain(references, IsToolkit);
    }

    [Fact]
    public void TheCoreAssetViewModelsDoNotReferenceTheEditorLayerNorTheEngineAssets()
    {
        var references = ReferencesOf(typeof(AssetViewModel).Assembly);

        Assert.DoesNotContain(references, x => EditorAssemblies.Contains(x));
        Assert.DoesNotContain(references, IsToolkit);
        Assert.DoesNotContain(references, x => x == "Stride.Assets" || x.StartsWith("Stride.Assets.", StringComparison.Ordinal));
    }

    private static List<string> ReferencesOf(Assembly assembly)
    {
        return assembly.GetReferencedAssemblies().Select(x => x.Name!).ToList();
    }

    private static bool IsToolkit(string name)
    {
        return name.StartsWith("Avalonia", StringComparison.Ordinal)
            || name is "PresentationCore" or "PresentationFramework" or "WindowsBase" or "System.Xaml"
            || name.EndsWith(".Wpf", StringComparison.Ordinal)
            || name.EndsWith(".Avalonia", StringComparison.Ordinal);
    }
}
