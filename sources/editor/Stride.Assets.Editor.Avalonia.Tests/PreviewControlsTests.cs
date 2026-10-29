// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Assets.Editor.Avalonia.Tests.Helpers;
using Stride.Assets.Editor.Avalonia.Views.Preview;
using Stride.Assets.Editor.Preview;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Editor.ViewModels.Preview;
using Stride.Core.Assets.Editor.Editors;
using Stride.Editor.Avalonia.Preview.Views;

namespace Stride.Assets.Editor.Avalonia.Tests;

/// <summary>
/// The preview views get the controls of their WPF templates from the themes of <see cref="StrideEditorViewPlugin"/>, around
/// the game view of the preview, and bind them to their view models.
/// </summary>
public sealed class PreviewControlsTests
{
    [AvaloniaFact]
    public void TheViewPluginMergesThePreviewThemesOnce()
    {
        var plugin = new StrideEditorViewPlugin();

        PluginRegistrations.Of(plugin, typeof(IAssetEditorView));
        PluginRegistrations.Of(plugin, typeof(IAssetEditorView));

        Assert.Single(Application.Current!.Resources.MergedDictionaries, x => x is ResourceInclude include && include.Source == StrideEditorViewPlugin.ThemesUri);
    }

    [AvaloniaFact]
    public void TheModelViewResetsTheCamera()
    {
        var viewModel = new ModelPreviewViewModel(PreviewSession.Create());
        var view = Show<ModelPreviewView>(viewModel);

        Assert.Same(viewModel.ResetModelCommand, Part<Button>(view, "PART_ResetCamera").Command);
    }

    [AvaloniaFact]
    public void TheModelViewBindsTheProceduralModelViewModelToo()
    {
        var viewModel = new ProceduralModelPreviewViewModel(PreviewSession.Create());
        var view = Show<ModelPreviewView>(viewModel);

        Assert.Same(viewModel.ResetModelCommand, Part<Button>(view, "PART_ResetCamera").Command);
    }

    [AvaloniaFact]
    public void TheSkyboxViewBindsTheMetalnessAndGlossiness()
    {
        var viewModel = new SkyboxPreviewViewModel(PreviewSession.Create());
        var view = Show<SkyboxPreviewView>(viewModel);
        var metalness = Part<Slider>(view, "PART_Metalness");
        var glossiness = Part<Slider>(view, "PART_Glossiness");

        Assert.Same(viewModel.ResetModelCommand, Part<Button>(view, "PART_ResetCamera").Command);
        Assert.Equal(1.0, metalness.Value, 3);
        Assert.Equal(0.6, glossiness.Value, 3);

        metalness.Value = 0.25;
        glossiness.Value = 0.75;

        Assert.Equal(0.25f, viewModel.Metalness);
        Assert.Equal(0.75f, viewModel.Glossiness);
    }

    [AvaloniaFact]
    public void TheMaterialViewChoosesThePrimitive()
    {
        var viewModel = new MaterialPreviewViewModel(PreviewSession.Create());
        var view = Show<MaterialPreviewView>(viewModel);
        var primitives = Part<ListBox>(view, "PART_Primitives");

        Assert.Equal(Enum.GetValues<MaterialPreviewPrimitive>().Cast<object>(), primitives.Items.Cast<object>());
        Assert.Equal(MaterialPreviewPrimitive.Sphere, primitives.SelectedItem);

        primitives.SelectedItem = MaterialPreviewPrimitive.Teapot;

        Assert.Equal(MaterialPreviewPrimitive.Teapot, viewModel.SelectedPrimitive);
    }

    [AvaloniaFact]
    public void TheViewPluginRegistersTheImagesOfTheMaterialPrimitives()
    {
        var enumImages = PluginRegistrations.Of(new StrideEditorViewPlugin(), typeof(IAssetEditorView)).EnumImages;

        Assert.All(Enum.GetValues<MaterialPreviewPrimitive>(), x => Assert.IsAssignableFrom<IImage>(enumImages[x]));
    }

    /// <summary>
    /// Shows a preview view with the given view model, after the view plugin merged its themes, as the editor does.
    /// </summary>
    internal static TView Show<TView>(object? viewModel) where TView : StridePreviewView, new()
    {
        PluginRegistrations.Of(new StrideEditorViewPlugin(), typeof(IAssetEditorView));
        var view = new TView { DataContext = viewModel };
        var window = new Window { Content = view, Width = 800, Height = 600 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        // The game view stays in the template, whatever the controls around it
        Assert.Single(view.GetVisualDescendants().OfType<ContentPresenter>(), x => x.Name == StridePreviewView.PartStrideView);
        return view;
    }

    internal static T Part<T>(StridePreviewView view, string name) where T : Control
    {
        return Assert.Single(view.GetVisualDescendants().OfType<T>(), x => x.Name == name);
    }
}
