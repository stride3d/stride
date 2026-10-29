// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using Stride.Assets.Editor.Avalonia.Tests.Helpers;
using Stride.Assets.Editor.Avalonia.Views.Preview;
using Stride.Assets.Editor.Preview;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Editor.ViewModels.Preview;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
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

    [AvaloniaFact]
    public void TheAnimationViewBindsThePlaybackControls()
    {
        var viewModel = new AnimationPreviewViewModel(PreviewSession.Create());
        var view = Show<AnimationPreviewView>(viewModel);

        Assert.Same(viewModel.PlayCommand, Part<Button>(view, "PART_Play").Command);
        Assert.Same(viewModel.PauseCommand, Part<Button>(view, "PART_Pause").Command);
        Assert.Equal(1.0, Part<NumericTextBox>(view, "PART_TimeScale").Value);

        Part<NumericTextBox>(view, "PART_TimeScale").Value = 0.5;

        Assert.Equal(0.5f, viewModel.TimeScale);
    }

    [AvaloniaFact]
    public void TheAnimationTimeSliderShowsOnceThePreviewHasATime()
    {
        var viewModel = new AnimationPreviewViewModel(PreviewSession.Create());
        var view = Show<AnimationPreviewView>(viewModel);

        // The view model has no time until the preview's game reports one
        Assert.False(Part<Slider>(view, "PART_TimeSlider").IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void DraggingTheTimeSliderPausesAPlayingAnimation()
    {
        var animation = new FakeAnimatedPreview { IsPlaying = true };
        var slider = new Slider { Maximum = 1, Width = 200 };
        Interaction.GetBehaviors(slider).Add(new SuspendAnimationDuringSliderDragBehavior { AnimatedPreviewViewModel = animation });
        var window = new Window { Content = slider, Width = 300, Height = 100 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var center = slider.TranslatePoint(new Point(slider.Bounds.Width / 2, slider.Bounds.Height / 2), window)!.Value;

        window.MouseDown(center, MouseButton.Left);

        Assert.Equal(["Pause"], animation.Calls);

        window.MouseMove(center + new Point(20, 0));
        window.MouseUp(center + new Point(20, 0), MouseButton.Left);

        Assert.Equal(["Pause", "Play"], animation.Calls);
    }

    [AvaloniaFact]
    public void DraggingTheTimeSliderOfAPausedAnimationLeavesItPaused()
    {
        var animation = new FakeAnimatedPreview { IsPlaying = false };
        var slider = new Slider { Maximum = 1, Width = 200 };
        Interaction.GetBehaviors(slider).Add(new SuspendAnimationDuringSliderDragBehavior { AnimatedPreviewViewModel = animation });
        var window = new Window { Content = slider, Width = 300, Height = 100 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var center = slider.TranslatePoint(new Point(slider.Bounds.Width / 2, slider.Bounds.Height / 2), window)!.Value;

        window.MouseDown(center, MouseButton.Left);
        window.MouseUp(center, MouseButton.Left);

        Assert.Empty(animation.Calls);
    }

    [AvaloniaFact]
    public void TheTimeRulerLeavesRoomForItsLabels()
    {
        var ruler = new AnimationTimeRuler { Maximum = 10, Inset = 10 };
        var window = new Window { Content = ruler, Width = 520, Height = 100 };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        // 500 pixels for 10 seconds: a label every second (50 pixels)
        Assert.Equal(1, ruler.MajorStep);

        ruler.Maximum = 0.5;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(0.05, ruler.MajorStep, 6);

        ruler.Maximum = 0;

        Assert.Equal(0, ruler.MajorStep);
    }

    [AvaloniaFact]
    public void TheTextureViewBindsTheZoomTheMipLevelAndTheSize()
    {
        var viewModel = new TexturePreviewViewModel(PreviewSession.Create());
        viewModel.PreviewAvailableMipMaps.AddRange(["Level 0", "Level 1", "Level 2"]);
        var view = Show<TexturePreviewView>(viewModel);

        AssertZoomCommands(view, viewModel);
        var mipMaps = Part<ComboBox>(view, "PART_MipMaps");
        Assert.Equal(3, mipMaps.ItemCount);
        Assert.Equal("Level 0", mipMaps.SelectedItem);
        mipMaps.SelectedItem = "Level 2";
        Assert.Equal("Level 2", viewModel.PreviewSelectedMipMap);
        Assert.True(Part<Grid>(view, "PART_StatusBar").IsVisible);
        Assert.Equal("W: 0", Part<TextBlock>(view, "PART_Width").Inlines!.Text);

        // A 2D texture has neither a face nor a slice to choose
        Assert.False(Part<ComboBox>(view, "PART_CubeMode").IsVisible);
        Assert.Equal(Enum.GetValues<TextureCubePreviewMode>().Length, Part<ComboBox>(view, "PART_CubeMode").ItemCount);
        Assert.Equal(TextureCubePreviewMode.Full, Part<ComboBox>(view, "PART_CubeMode").SelectedItem);
        Assert.False(Part<DockPanel>(view, "PART_Depth").IsVisible);
    }

    [AvaloniaFact]
    public void TheSpriteViewBindsTheZoomAndTheSize()
    {
        var viewModel = new SpritePreviewViewModel(PreviewSession.Create());
        var view = Show<SpritePreviewView>(viewModel);

        AssertZoomCommands(view, viewModel);
        Assert.True(Part<Grid>(view, "PART_StatusBar").IsVisible);
        Assert.Equal("H: 0", Part<TextBlock>(view, "PART_Height").Inlines!.Text);
    }

    [AvaloniaFact]
    public void TheSpriteSheetViewBindsTheZoomTheDisplayModeAndTheSprite()
    {
        var viewModel = new SpriteSheetPreviewViewModel(PreviewSession.Create());
        var view = Show<SpriteSheetPreviewView>(viewModel);

        AssertZoomCommands(view, viewModel);
        Assert.Same(viewModel.PreviewPreviousFrameCommand, Part<Button>(view, "PART_PreviousFrame").Command);
        Assert.Same(viewModel.PreviewNextFrameCommand, Part<Button>(view, "PART_NextFrame").Command);
        var displayMode = Part<ComboBox>(view, "PART_DisplayMode");
        Assert.Equal(Enum.GetValues<SpriteSheetDisplayMode>().Length, displayMode.ItemCount);

        displayMode.SelectedItem = SpriteSheetDisplayMode.SpriteTextures;

        Assert.Equal(SpriteSheetDisplayMode.SpriteTextures, viewModel.DisplayMode);
        Assert.Equal("Sprite: 0/0", Part<TextBlock>(view, "PART_Frame").Inlines!.Text);
        Assert.NotNull(Part<Slider>(view, "PART_FrameSlider"));
    }

    [AvaloniaFact]
    public async Task TheSpriteFontViewSendsItsTextAfterADelay()
    {
        var viewModel = new SpriteFontPreviewViewModel(PreviewSession.Create());
        var view = Show<SpriteFontPreviewView>(viewModel);
        var text = Part<TextBox>(view, "PART_PreviewText");

        text.Text = "Hello";

        Assert.Null(viewModel.PreviewString);
        await Task.Delay(700, TestContext.Current.CancellationToken);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("Hello", viewModel.PreviewString);

        Part<Button>(view, "PART_ClearText").Command!.Execute(null);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(string.Empty, text.Text);
    }

    private static void AssertZoomCommands<TPreview>(StridePreviewView view, TextureBasePreviewViewModel<TPreview> viewModel)
        where TPreview : Stride.Editor.Preview.IAssetPreview
    {
        Assert.Same(viewModel.PreviewZoomOutCommand, Part<Button>(view, "PART_ZoomOut").Command);
        Assert.Same(viewModel.PreviewZoomInCommand, Part<Button>(view, "PART_ZoomIn").Command);
        Assert.Same(viewModel.PreviewScaleToRealSizeCommand, Part<Button>(view, "PART_RealSize").Command);
        Assert.Same(viewModel.PreviewFitOnScreenCommand, Part<Button>(view, "PART_FitOnScreen").Command);
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

    private sealed class FakeAnimatedPreview : IAnimatedPreviewViewModel
    {
        private static readonly IViewModelServiceProvider ServiceProvider = new ViewModelServiceProvider([new NullDispatcherService()]);

        public FakeAnimatedPreview()
        {
            PlayCommand = new AnonymousCommand(ServiceProvider, () => Calls.Add("Play"));
            PauseCommand = new AnonymousCommand(ServiceProvider, () => Calls.Add("Pause"));
        }

        public List<string> Calls { get; } = [];

        public ICommandBase PlayCommand { get; }

        public ICommandBase PauseCommand { get; }

        public bool IsPlaying { get; set; }
    }

    internal static T Part<T>(StridePreviewView view, string name) where T : Control
    {
        return Assert.Single(view.GetVisualDescendants().OfType<T>(), x => x.Name == name);
    }
}
