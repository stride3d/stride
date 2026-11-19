// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Editor.Avalonia.Tests.Helpers;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Translation;
using Stride.Core.Translation.Providers;
using Stride.UI;
using Stride.UI.Editor.Avalonia.Views;
using Thickness = Stride.UI.Thickness;

namespace Stride.Assets.Editor.Avalonia.Tests;

/// <summary>
/// The Avalonia companion of the UI editor: the property templates of the UI elements (thickness, strip definition,
/// rotation) and the thickness editor.
/// </summary>
public sealed class UICompanionTests
{
    private readonly NodeViewModelContext context = new();

    [Fact]
    public void TheUIEditorRegistersItsTranslations()
    {
        // Tr._p looks the catalog up by the calling assembly: without its own provider, the UI editor's questions (its
        // "Don't ask again" among them) stay in English in a translated editor
        var assembly = typeof(Stride.UI.Editor.UIEditorPlugin).Assembly;
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);

        // The manager takes one provider per assembly, so a second one is refused
        Assert.Throws<ArgumentException>(() => TranslationManager.Instance.RegisterProvider(new GettextTranslationProvider(assembly)));
    }

    public sealed class Holder
    {
        public Thickness Margin { get; set; } = new(1, 2, 3, 4);

        public StripDefinition Strip { get; set; } = new(StripType.Fixed, 10);

        public float Rotation { get; set; }

        public float Opacity { get; set; }
    }

    [AvaloniaFact]
    public void TheCompanionRegistersTheUIPropertyTemplatesAndItsTheme()
    {
        var registered = PluginRegistrations.Of(new UIEditorAvaloniaPlugin(), typeof(IAssetEditorView)).TemplateProviders;

        Assert.Equal(3, registered.Count);
        Assert.Single(registered, x => x is TypeAndPropertyNameMatchTemplateProvider { PropertyName: "Rotation" });
        Assert.Single(registered, x => x is TypeMatchTemplateProvider { Type: var type } && type == typeof(Thickness));
        Assert.Single(registered, x => x is TypeMatchTemplateProvider { Type: var type } && type == typeof(StripDefinition));
        Assert.All(registered, x => Assert.Equal(PropertyViewHelper.Category.PropertyEditor, PropertyViewHelper.GetTemplateCategory((AvaloniaObject)x)));
        Assert.Contains(Application.Current!.Resources.MergedDictionaries, x => x is ResourceInclude include && include.Source == UIEditorAvaloniaPlugin.ThemesUri);
    }

    [AvaloniaFact]
    public void TheTemplatesMatchTheirNodesOnly()
    {
        var providers = new UIPropertyTemplates().Values.OfType<TypeMatchTemplateProvider>().ToList();
        var holder = new Holder();
        var rotation = providers.OfType<TypeAndPropertyNameMatchTemplateProvider>().Single();
        var thickness = providers.Single(x => x.Type == typeof(Thickness));

        // The rotation is a float: only the node of that name is edited in degrees
        Assert.True(rotation.Match(context.MemberOf(holder, nameof(Holder.Rotation))));
        Assert.False(rotation.Match(context.MemberOf(holder, nameof(Holder.Opacity))));
        Assert.True(thickness.Match(context.MemberOf(holder, nameof(Holder.Margin))));
        Assert.False(thickness.Match(context.MemberOf(holder, nameof(Holder.Strip))));
    }

    [AvaloniaFact]
    public void TheThicknessEditorShowsOneBoxPerSide()
    {
        PluginRegistrations.Of(new UIEditorAvaloniaPlugin(), typeof(IAssetEditorView));
        var editor = new ThicknessEditor { Value = new Thickness(1, 2, 3, 4, 5, 6) };
        var window = new Window { Content = editor };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        var boxes = editor.GetVisualDescendants().OfType<NumericTextBox>().ToDictionary(x => x.Name!, x => x.Value);
        Assert.Equal(1.0, boxes["PART_Left"]);
        Assert.Equal(2.0, boxes["PART_Top"]);
        Assert.Equal(3.0, boxes["PART_Back"]);
        Assert.Equal(4.0, boxes["PART_Right"]);
        Assert.Equal(5.0, boxes["PART_Bottom"]);
        Assert.Equal(6.0, boxes["PART_Front"]);

        editor.Bottom = 8;
        Assert.Equal(new Thickness(1, 2, 3, 4, 8, 6), editor.Value);
        window.Close();
    }

    [AvaloniaFact]
    public async Task AHeldSpinButtonOfTheThicknessEditorWritesTheThicknessOnce()
    {
        PluginRegistrations.Of(new UIEditorAvaloniaPlugin(), typeof(IAssetEditorView));
        var source = new ThicknessSource { Value = new Thickness(1, 2, 3, 4, 5, 6) };
        source.Reset();
        var editor = new ThicknessEditor();
        // The binding of the property grid: written only when the editor commits
        editor.Bind(ThicknessEditor.ValueProperty, new Binding(nameof(ThicknessSource.Value)) { Source = source, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.Explicit });
        var window = new Window { Content = editor, Width = 400, Height = 200 };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var left = editor.GetVisualDescendants().OfType<NumericTextBox>().Single(x => x.Name == "PART_Left");
        // The sides of a thickness show their spin buttons
        var up = left.GetVisualDescendants().OfType<RepeatButton>().Single(x => x.Name == "PART_IncreaseButton");
        Assert.True(up.IsEffectivelyVisible);
        var center = up.TranslatePoint(new Point(up.Bounds.Width / 2, up.Bounds.Height / 2), window)!.Value;

        window.MouseDown(center, MouseButton.Left);
        for (var i = 0; i < 100 && left.Value < 4; ++i)
        {
            await Task.Delay(20);
            Dispatcher.UIThread.RunJobs();
        }
        Assert.Equal(0, source.Writes);
        window.MouseUp(center, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1, source.Writes);
        Assert.Equal(left.Value, source.Value.Left);
        Assert.True(source.Value.Left >= 4);
        window.Close();
    }

    private sealed class ThicknessSource : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public int Writes { get; private set; }

        public Thickness Value
        {
            get;
            set
            {
                Writes++;
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }

        /// <summary>
        /// Forgets the writes so far: the initial value is not one.
        /// </summary>
        public void Reset() => Writes = 0;
    }
}
