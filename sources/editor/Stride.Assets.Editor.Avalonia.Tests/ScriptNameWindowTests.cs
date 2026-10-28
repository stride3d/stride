// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Stride.Assets.Editor.Avalonia.Templates;
using Stride.Assets.Editor.Templates;
using Stride.Core.Assets.Templates;

namespace Stride.Assets.Editor.Avalonia.Tests;

/// <summary>
/// The dialog the script template shows before it runs: the class name, the namespace and the template of the new script.
/// </summary>
public sealed class ScriptNameWindowTests
{
    private static readonly TemplateAssetDescription SyncScript = new() { Id = Guid.NewGuid(), Name = "Sync script", AssetTypeName = "ScriptSourceFileAsset", Scope = TemplateScope.Asset };
    private static readonly TemplateAssetDescription AsyncScript = new() { Id = Guid.NewGuid(), Name = "Async script", AssetTypeName = "ScriptSourceFileAsset", Scope = TemplateScope.Asset };

    [AvaloniaFact]
    public async Task TheWindowShowsTheNamesAndTheTemplatesToPickFrom()
    {
        var viewModel = new ScriptNameViewModel("Script", "MyGame", SyncScript, enableTemplateSelect: true, templates: [SyncScript, AsyncScript]);
        var window = new ScriptNameWindow { DataContext = viewModel };
        window.Show();
        try
        {
            await PumpAsync();
            Assert.True(window.IsVisible);
            Assert.Equal("New script", window.Title);
            Assert.Same(viewModel, window.Script);
            var className = window.FindControl<TextBox>("ClassNameBox")!;
            var ns = window.FindControl<TextBox>("NamespaceBox")!;
            var templates = window.FindControl<ComboBox>("TemplateBox")!;
            Assert.Equal("Script", className.Text);
            Assert.Equal("MyGame", ns.Text);
            // The class name has the focus, selected, so that typing replaces it
            Assert.True(className.IsFocused);
            Assert.Equal("Script", className.SelectedText);
            Assert.True(templates.IsVisible);
            Assert.Equal(2, templates.ItemCount);
            Assert.Same(SyncScript, templates.SelectedItem);

            // Another template
            templates.SelectedItem = AsyncScript;
            await PumpAsync();
            Assert.Same(AsyncScript, viewModel.SelectedTemplate);
            Assert.Same(AsyncScript, viewModel.ScriptTemplate);

            // Typing the names, and accepting: the names are validated and the window closes
            window.KeyTextInput("PlayerController");
            ns.Focus();
            ns.SelectAll();
            window.KeyTextInput("My Game.Scripts");
            await PumpAsync();
            Click(window, window.FindControl<Button>("OkButton")!);
            await WaitUntilAsync(() => !window.IsVisible, "the window to close");
            Assert.Equal("PlayerController", viewModel.ClassName);
            // The names are made valid identifiers
            Assert.Equal("My_Game.Scripts", viewModel.Namespace);
        }
        finally
        {
            await CloseAsync(window);
        }
    }

    [AvaloniaFact]
    public async Task AnEmptyNameIsRefusedWithAMessageAndCancelCloses()
    {
        var viewModel = new ScriptNameViewModel("Script", "MyGame", SyncScript);
        var window = new ScriptNameWindow { DataContext = viewModel };
        window.Show();
        try
        {
            await PumpAsync();
            // Without the choice, the template list is hidden
            Assert.False(window.FindControl<ComboBox>("TemplateBox")!.IsVisible);
            var error = window.FindControl<TextBlock>("ErrorLabel")!;
            Assert.False(error.IsVisible);

            viewModel.ClassName = "";
            await PumpAsync();
            Click(window, window.FindControl<Button>("OkButton")!);
            await PumpAsync();
            Assert.True(window.IsVisible);
            Assert.True(error.IsVisible);
            Assert.Equal("The names you entered are invalid or empty.", error.Text);

            Click(window, window.FindControl<Button>("CancelButton")!);
            await WaitUntilAsync(() => !window.IsVisible, "the window to close");
        }
        finally
        {
            await CloseAsync(window);
        }
    }

    private static void Click(Window window, Control control)
    {
        var center = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)
            ?? throw new InvalidOperationException("The control is not in the window.");
        window.MouseMove(center);
        window.MouseDown(center, MouseButton.Left);
        window.MouseUp(center, MouseButton.Left);
    }

    private static async Task PumpAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, string what, int timeoutMilliseconds = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException($"Timed out waiting for {what}.");
            await Task.Delay(25);
            await PumpAsync();
        }
    }

    private static async Task CloseAsync(Window window)
    {
        window.Close();
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
    }
}
