// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.VisualTree;
using Stride.Assets.Editor.Avalonia.Views.Preview;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Audio.Editor;
using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Diagnostics;
using Stride.Editor.Avalonia.Preview.Views;
using Stride.SpriteStudio.Editor;

namespace Stride.Assets.Editor.Avalonia.Tests;

/// <summary>
/// The Avalonia companions of the plugins: their plugin classes register the views of their assembly, and their themes
/// join the application when they initialize.
/// </summary>
public sealed class CompanionTests
{
    [Fact]
    public void TheAudioCompanionRegistersTheSoundPreviewView()
    {
        var views = PluginRegistrations.Of(new AudioEditorAvaloniaPlugin(), typeof(IAssetEditorView)).AssetPreviewViewTypes;

        Assert.Equal(typeof(SoundPreviewView), Assert.Single(views).Value);
        Assert.Equal(typeof(SoundPreview), Assert.Single(views).Key);
    }

    [Fact]
    public void TheSpriteStudioCompanionRegistersTheSheetPreviewView()
    {
        var views = PluginRegistrations.Of(new SpriteStudioEditorAvaloniaPlugin(), typeof(IAssetEditorView)).AssetPreviewViewTypes;

        Assert.Equal(typeof(SpriteStudioSheetPreviewView), views[typeof(SpriteStudioSheetPreview)]);
        Assert.Single(views);
    }

    [AvaloniaFact]
    public void TheSoundPreviewViewGetsItsThemeFromThePlugin()
    {
        PluginRegistrations.Of(new AudioEditorAvaloniaPlugin(), typeof(IAssetEditorView));
        var view = new SoundPreviewView();
        var window = new Window { Content = view };

        window.Show();

        // The sound view has its own template (play, pause, volume, position) instead of the game view
        Assert.Contains(Application.Current!.Resources.MergedDictionaries, x => x is ResourceInclude include && include.Source == AudioEditorAvaloniaPlugin.ThemesUri);
        var buttons = view.GetVisualDescendants().OfType<Button>().ToList();
        Assert.Contains(buttons, x => x.Name == "PART_Play");
        Assert.Contains(buttons, x => x.Name == "PART_Pause");
        Assert.Equal(2, view.GetVisualDescendants().OfType<Slider>().Count());
        Assert.DoesNotContain(view.GetVisualDescendants().OfType<ContentPresenter>(), x => x.Name == StridePreviewView.PartStrideView);
    }

    [AvaloniaFact]
    public void InitializingTheAudioPluginTwiceMergesItsThemeOnce()
    {
        var plugin = new AudioEditorAvaloniaPlugin();

        PluginRegistrations.Of(plugin, typeof(IAssetEditorView));
        PluginRegistrations.Of(plugin, typeof(IAssetEditorView));

        Assert.Single(Application.Current!.Resources.MergedDictionaries, x => x is ResourceInclude include && include.Source == AudioEditorAvaloniaPlugin.ThemesUri);
    }

    [AvaloniaFact]
    public void TheSheetPreviewViewSharesTheModelTemplate()
    {
        var view = new SpriteStudioSheetPreviewView();
        var window = new Window { Content = view };

        window.Show();

        Assert.IsAssignableFrom<ModelPreviewView>(view);
        Assert.Single(view.GetVisualDescendants().OfType<ContentPresenter>(), x => x.Name == StridePreviewView.PartStrideView);
    }
}
