// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Assets.Models;
using Stride.Assets.SpriteFont;
using Stride.Core.Assets.Editor.Avalonia.Tests.Helpers;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Avalonia.Views;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Rendering.Compositing;
using Stride.Rendering.Materials.ComputeColors;
using Point = Avalonia.Point;

namespace Stride.Assets.Editor.Avalonia.Tests;

/// <summary>
/// The editors of asset members that have their own template in the property grid: each one is the template chosen for its
/// member among all the templates of the editor, and changes the member.
/// </summary>
public sealed class AssetPropertyTemplateTests
{
    private static readonly string[] Shaders = ["ComputeColorWave", "ComputeColorStripes", "ComputeTextureScroll"];

    [AvaloniaFact]
    public void TheShaderOfAComputeShaderClassIsPickedInTheEffectShadersOfThePackage()
    {
        var holder = new ShaderHolder { Color = new ComputeShaderClassColor { MixinReference = "ComputeColorWave" } };
        var node = new NodeViewModelContext(new Updater(x =>
        {
            if (x.Name == nameof(ComputeShaderClassColor.MixinReference))
                x.AttachedProperties.Add(MaterialData.Key, Shaders);
        })).MemberOf(holder, nameof(ShaderHolder.Color)).GetChild(nameof(ComputeShaderClassColor.MixinReference))!;

        var (window, content) = Show<ShaderClassNodeMixinReferenceTemplateProvider>(node, PropertyViewHelper.Category.PropertyEditor);
        var box = content.GetSelfAndVisualDescendants().OfType<AutoCompleteBox>().Single();
        Assert.Equal("ComputeColorWave", box.Text);
        Assert.Equal(Shaders, box.ItemsSource);

        box.Focus();
        Dispatcher.UIThread.RunJobs();
        box.Text = string.Empty;
        window.KeyTextInput("Stripes");
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("ComputeColorStripes", holder.Color.MixinReference);
        window.Close();
    }

    [AvaloniaFact]
    public void TheFontOfASpriteFontIsPickedInTheInstalledFonts()
    {
        var asset = new SpriteFontAsset { FontSource = new SystemFontProvider("Arial") };
        string[] fonts = ["Arial", "Courier New", "Liberation Sans"];
        var context = new NodeViewModelContext(new Updater(x =>
        {
            if (x.Type == typeof(SpriteFontAsset))
                x.AttachedProperties.Add(SpriteFontData.Key, fonts);
        }));
        var node = context.RootOf(asset).GetChild(nameof(SpriteFontAsset.FontSource))!.GetChild(nameof(SystemFontProvider.FontName))!;

        var (window, content) = Show<SpriteFontFontNameTemplateProvider>(node, PropertyViewHelper.Category.PropertyEditor);
        var comboBox = content.GetSelfAndVisualDescendants().OfType<ComboBox>().Single();
        Assert.Equal(fonts, comboBox.ItemsSource);
        Assert.Equal("Arial", comboBox.SelectedItem);

        comboBox.SelectedItem = "Courier New";
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("Courier New", ((SystemFontProvider)asset.FontSource).FontName);
        window.Close();
    }

    [AvaloniaFact]
    public void TheButtonsOfTheNodeListOfASkeletonKeepAllTheNodesOrNone()
    {
        var asset = new SkeletonAsset();
        asset.Nodes.Add(new NodeInformation("Root", 0, true));
        asset.Nodes.Add(new NodeInformation("Spine", 1, false));
        asset.Nodes.Add(new NodeInformation("Head", 2, true));
        var context = new NodeViewModelContext();
        context.Commands.Add(new SkeletonNodePreserveAllCommand());
        var node = context.RootOf(asset).GetChild(nameof(SkeletonAsset.Nodes))!;

        var (window, content) = Show<SkeletonNodeListTemplateProvider>(node, PropertyViewHelper.Category.PropertyHeader);
        var buttons = content.GetVisualDescendants().OfType<Button>().ToList();

        Click(window, buttons.Single(x => (string?)x.Content == "All"));
        Assert.All(asset.Nodes, x => Assert.True(x.Preserve));
        Click(window, buttons.Single(x => (string?)x.Content == "None"));
        Assert.All(asset.Nodes, x => Assert.False(x.Preserve));
        window.Close();
    }

    [AvaloniaFact]
    public void TheStartOfAClipIsEditedInFramesWithASliderOverTheAnimation()
    {
        var asset = new AnimationAsset
        {
            AnimationTimeMinimum = TimeSpan.Zero,
            AnimationTimeMaximum = TimeSpan.FromSeconds(2),
            ClipDuration = new AnimationAssetDuration { Enabled = true, StartAnimationTime = TimeSpan.FromSeconds(0.5), EndAnimationTime = TimeSpan.FromSeconds(1) },
        };
        var node = new NodeViewModelContext().RootOf(asset).GetChild(nameof(AnimationAsset.ClipDuration))!.GetChild(nameof(AnimationAssetDuration.StartAnimationTime))!;

        var (window, content) = Show<AnimationFrameTemplateProvider>(node, PropertyViewHelper.Category.PropertyEditor);
        var slider = content.GetVisualDescendants().OfType<Slider>().Single();
        var box = content.GetVisualDescendants().OfType<NumericTextBox>().Single();
        // 30 frames per second without a session
        Assert.Equal(60, slider.Maximum);
        Assert.Equal(15.0, box.Value);
        Assert.Equal(15.0, slider.Value);

        slider.Focus();
        window.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(TimeSpan.FromSeconds(16 / 30.0), asset.ClipDuration.StartAnimationTime);
        Assert.Equal(16.0, box.Value);

        var textBox = box.GetVisualDescendants().OfType<TextBox>().Single();
        textBox.Focus();
        textBox.SelectAll();
        window.KeyTextInput("45");
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(TimeSpan.FromSeconds(1.5), asset.ClipDuration.StartAnimationTime);
        Assert.Equal(45.0, slider.Value);
        window.Close();
    }

    [AvaloniaFact]
    public void TheStartOfAReferenceClipIsEditedInFramesInABox()
    {
        var asset = new AnimationAsset
        {
            Type = new DifferenceAnimationAssetType
            {
                ClipDuration = new AnimationAssetDurationUnchecked { Enabled = true, StartAnimationTimeBox = TimeSpan.FromSeconds(1) },
            },
        };
        var node = new NodeViewModelContext().RootOf(asset).GetChild(nameof(AnimationAsset.Type))!
            .GetChild(nameof(DifferenceAnimationAssetType.ClipDuration))!.GetChild(nameof(AnimationAssetDurationUnchecked.StartAnimationTimeBox))!;

        var (window, content) = Show<AnimationFrameBoxTemplateProvider>(node, PropertyViewHelper.Category.PropertyEditor);
        var box = content.GetSelfAndVisualDescendants().OfType<NumericTextBox>().Single();
        Assert.Equal(30.0, box.Value);

        box.GetVisualDescendants().OfType<TextBox>().Single().Focus();
        window.KeyPressQwerty(PhysicalKey.ArrowUp, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        var type = (DifferenceAnimationAssetType)asset.Type;
        Assert.Equal(TimeSpan.FromSeconds(31 / 30.0), type.ClipDuration.StartAnimationTimeBox);
        window.Close();
    }

    [AvaloniaFact]
    public void TheCameraSlotShowsTheNameOfTheSlotAndIsPickedInTheSlotsOfTheCompositor()
    {
        var main = new SceneCameraSlotId(Guid.NewGuid());
        var editor = new SceneCameraSlotId(Guid.NewGuid());
        AbstractNodeEntry[] entries =
        [
            new AbstractNodeValue(new SceneCameraSlotId(), AbstractNodeValue.Null.DisplayValue, AbstractNodeValue.Null.Order),
            new AbstractNodeValue(main, "GraphicsCompositor > Main", 0),
            new AbstractNodeValue(editor, "GraphicsCompositor > Editor", 1),
        ];
        var holder = new CameraHolder { Slot = main };
        // What CameraSlotNodeUpdater gives the slot of a camera component
        var node = new NodeViewModelContext(new Updater(x =>
        {
            if (x.Type != typeof(SceneCameraSlotId))
                return;

            x.AttachedProperties.Add(AbstractNodeEntryData.Key, entries);
            x.Commands.Add(new SyncAnonymousNodePresenterCommand(CameraSlotData.UpdateCameraSlotIndex, (n, id) => n.UpdateValue(id)));
        })).MemberOf(holder, nameof(CameraHolder.Slot));

        var (window, content) = Show<TypeMatchTemplateProvider>(node, PropertyViewHelper.Category.PropertyEditor);
        var name = content.GetVisualDescendants().OfType<TextBlock>().First(x => x.Name is null && x.Text?.StartsWith("Graphics") == true);
        Assert.Equal("GraphicsCompositor > Main", name.Text);

        var comboBox = content.GetVisualDescendants().OfType<ComboBox>().Single();
        Assert.True(comboBox.IsVisible);
        comboBox.IsDropDownOpen = true;
        Dispatcher.UIThread.RunJobs();
        comboBox.SelectedItem = entries[2];
        comboBox.IsDropDownOpen = false;
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(editor.Id, holder.Slot.Id);
        Assert.Equal("GraphicsCompositor > Editor", name.Text);
        Assert.Null(comboBox.SelectedItem);
        window.Close();
    }

    /// <summary>
    /// Checks that the template of the node is the one of the given provider among all the providers of the editor, and shows it.
    /// </summary>
    private static (Window Window, Control Content) Show<TProvider>(NodeViewModel node, PropertyViewHelper.Category category)
        where TProvider : TemplateProviderBase
    {
        var expected = new AssetPropertyTemplateProviders().Values.OfType<TProvider>().Single();
        var selector = new Selector(category);
        var chosen = selector.Chosen(node);
        Assert.NotNull(chosen);
        Assert.Equal(expected.Name, chosen.Name);

        var content = selector.Build(node)!;
        content.DataContext = node;
        // Another control to give the focus to
        var window = new Window { Width = 800, Height = 300, Content = new StackPanel { Children = { content, new TextBox() } } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, content);
    }

    private static void Click(Window window, Control control)
    {
        var center = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
        window.MouseDown(center, MouseButton.Left);
        window.MouseUp(center, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>
    /// The templates of one category from all the dictionaries of the editor, as the property grid selects them.
    /// </summary>
    private sealed class Selector : TemplateProviderSelector
    {
        public Selector(PropertyViewHelper.Category category)
        {
            var providers = new DefaultPropertyTemplateProviders().Values
                .Concat(new EntityPropertyTemplateProviders().Values)
                .Concat(new AssetPropertyTemplateProviders().Values)
                .OfType<TemplateProviderBase>()
                .Where(x => PropertyViewHelper.GetTemplateCategory(x) == category);
            foreach (var provider in providers)
                RegisterTemplateProvider(provider);
        }

        public TemplateProviderBase? Chosen(object item) => templateProviders.FirstOrDefault(x => x.Match(item));
    }

    private sealed class Updater(Action<INodePresenter> update) : INodePresenterUpdater
    {
        public void UpdateNode(INodePresenter node) => update(node);

        public void FinalizeTree(INodePresenter root)
        {
        }
    }
}

public sealed class ShaderHolder
{
    public ComputeShaderClassColor Color { get; set; } = new();
}

public sealed class CameraHolder
{
    public SceneCameraSlotId Slot { get; set; }
}
