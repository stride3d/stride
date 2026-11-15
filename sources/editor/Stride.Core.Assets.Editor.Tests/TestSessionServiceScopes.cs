// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Commands;
using Stride.Core.Assets.Editor.Panels;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The three scopes of services in an open session: the editor's, the session's (a child of the editor's) and an asset
/// editor's tab (a child of the session's), and the constructor injection of the types the editor creates.
/// </summary>
public sealed class TestSessionServiceScopes
{
    public sealed class TabService : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }

    // An editor that uses the services of its tab, and asks for a service of the session
    public sealed class TabEditor : AssetEditorViewModel
    {
        public TabEditor(AssetViewModel asset, IViewModelServiceProvider services, IUndoRedoService undoRedo)
            : base(asset, services)
        {
            UndoRedo = undoRedo;
        }

        public IUndoRedoService UndoRedo { get; }
    }

    // An editor with the fixed signature only
    public sealed class PlainEditor(AssetViewModel asset) : AssetEditorViewModel(asset);

    public sealed class InjectedPanel(PanelContext context, IUndoRedoService undoRedo) : PanelViewModel(context)
    {
        public IUndoRedoService UndoRedo { get; } = undoRedo;
    }

    public sealed class InjectedWidget(IDialogService dialogs, string name)
    {
        public IDialogService Dialogs { get; } = dialogs;

        public string Name { get; } = name;
    }

    [Fact]
    public async Task TheSessionHasAScopeOfItsOwnUnderTheEditors()
    {
        await using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var sessionServices = Assert.IsType<ViewModelServiceProvider>(fixture.Session.ServiceProvider);

        Assert.Same(fixture.ServiceProvider, sessionServices.Parent);
        // The session's services are not the editor's
        Assert.NotNull(sessionServices.TryGet<IUndoRedoService>());
        Assert.Null(fixture.ServiceProvider.TryGet<IUndoRedoService>());
        Assert.Null(fixture.ServiceProvider.TryGet<SelectionService>());
        // The editor's are seen from the session
        Assert.Same(fixture.ServiceProvider.Get<IDialogService>(), sessionServices.Get<IDialogService>());
        // A service the editor gets after the session opened is seen too
        var late = new TabService();
        fixture.ServiceProvider.RegisterService(late);
        Assert.Same(late, sessionServices.Get<TabService>());
        fixture.ServiceProvider.UnregisterService(late);
    }

    [Fact]
    public async Task TheServicesOfTheSessionGoWhenItIsDestroyed()
    {
        await using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var sessionServices = fixture.Session.ServiceProvider;
        var made = sessionServices.Register(_ => new TabService());
        var instance = sessionServices.Get<TabService>();

        await fixture.DisposeAsync();

        Assert.Null(sessionServices.TryGet<IUndoRedoService>());
        Assert.Null(sessionServices.TryGet<UserDocumentationService>());
        Assert.Null(sessionServices.TryGet<TabService>());
        Assert.True(instance.IsDisposed);
        // The editor's stay
        Assert.NotNull(sessionServices.TryGet<IDialogService>());
        made.Dispose();
    }

    [Fact]
    public async Task AnEditorGetsTheServicesOfItsTabAndThoseItAsksFor()
    {
        await using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var wood = fixture.Session.AllAssets.Single();
        using var perScope = fixture.Session.ServiceProvider.Register(_ => new TabService(), ServiceLifetime.PerScope);

        var editor = fixture.OnUiThread(() => Assert.IsType<TabEditor>(AssetEditorViewModel.Create(typeof(TabEditor), wood)));

        Assert.Same(wood, editor.Asset);
        Assert.Same(fixture.Session.UndoRedoService, editor.UndoRedo);
        var tab = Assert.IsType<ViewModelServiceProvider>(editor.ServiceProvider);
        Assert.Same(fixture.Session.ServiceProvider, tab.Parent);
        // What the editor registers stays in its tab
        tab.RegisterService(new InjectedWidget(fixture.ServiceProvider.Get<IDialogService>(), "tab"));
        Assert.Null(fixture.Session.ServiceProvider.TryGet<InjectedWidget>());
        // A per-scope service of the session: the tab has its own, which goes with the editor
        var inTab = tab.Get<TabService>();
        Assert.NotSame(fixture.Session.ServiceProvider.Get<TabService>(), inTab);

        fixture.OnUiThread(editor.Destroy);

        Assert.True(inTab.IsDisposed);
        Assert.Null(tab.TryGet<InjectedWidget>());
    }

    [Fact]
    public async Task AnEditorWithTheFixedSignatureUsesTheSessionsServices()
    {
        await using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var wood = fixture.Session.AllAssets.Single();

        var editor = fixture.OnUiThread(() => Assert.IsType<PlainEditor>(AssetEditorViewModel.Create(typeof(PlainEditor), wood)));

        Assert.Same(fixture.Session.ServiceProvider, editor.ServiceProvider);
        Assert.Same(fixture.Session.UndoRedoService, editor.UndoRedoService);
        fixture.OnUiThread(editor.Destroy);
    }

#pragma warning disable STREXP003, STREXP006
    [Fact]
    public async Task ThePanelWidgetAndCommandContextsCreateByInjection()
    {
        await using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var descriptor = new PanelDescriptor { Id = "Test.Injected", Title = () => "Injected", CreateViewModel = context => context.CreateInstance<InjectedPanel>() };
        var panelContext = new PanelContext { Descriptor = descriptor, Session = fixture.Session, ServiceProvider = fixture.Session.ServiceProvider };

        var panel = fixture.OnUiThread(() => Assert.IsType<InjectedPanel>(descriptor.CreateViewModel(panelContext)));
        Assert.Same(fixture.Session.UndoRedoService, panel.UndoRedo);
        fixture.OnUiThread(panel.Destroy);

        // A constructor that doesn't take the context, with an argument that isn't a service
        var widgetContext = new ToolbarWidgetContext
        {
            Descriptor = new ToolbarWidgetDescriptor { Id = "Test.Widget", CreateViewModel = _ => throw new InvalidOperationException() },
            Session = fixture.Session,
            ServiceProvider = fixture.Session.ServiceProvider,
        };
        var widget = widgetContext.CreateInstance<InjectedWidget>("widget");
        Assert.Equal("widget", widget.Name);
        Assert.Same(fixture.ServiceProvider.Get<IDialogService>(), widget.Dialogs);

        var commandContext = new CommandContext { Session = fixture.Session, Location = CommandLocations.Shortcut };
        Assert.Equal("command", commandContext.CreateInstance<InjectedWidget>("command").Name);
        Assert.Throws<InvalidOperationException>(() => commandContext.CreateInstance<InjectedWidget>());
    }
#pragma warning restore STREXP003, STREXP006
}
