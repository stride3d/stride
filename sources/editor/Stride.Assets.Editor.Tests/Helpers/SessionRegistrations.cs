// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using System.Text;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

#nullable enable

namespace Stride.Assets.Editor.Tests.Helpers;

/// <summary>
/// What a plugin adds to a session when the session's editor initializes, through its <see cref="ISessionPluginContext"/> or
/// directly (the legacy <see cref="AssetsPlugin.InitializeSession(ISessionViewModel)"/>): the services of the session and
/// the commands and updaters of its property grid. Both ways end up in the session, where they are read.
/// </summary>
public sealed class SessionRegistrations : ISessionPluginContext
{
    private readonly List<object> services = [];
    private readonly List<Action> closeActions = [];
    private readonly GraphViewModelService? graphService;

    private SessionRegistrations(AssetsPlugin plugin, ISessionViewModel session)
    {
        Plugin = plugin;
        Session = session;
        graphService = (session.AssetViewProperties as ViewModelBase)?.ServiceProvider.TryGet<GraphViewModelService>();
        Services = new Registry<object>(session.ServiceProvider.RegisterService);
        NodePresenterCommands = new Registry<INodePresenterCommand>(command => GraphService.AvailableCommands.Add(command));
        NodePresenterUpdaters = new Registry<INodePresenterUpdater>(updater => session.AssetViewProperties.RegisterNodePresenterUpdater(updater));
    }

    public AssetsPlugin Plugin { get; }

    public ISessionViewModel Session { get; }

    public ILogger Logger { get; } = new LoggerResult();

    public IPluginRegistry<object> Services { get; }

    public IPluginRegistry<INodePresenterCommand> NodePresenterCommands { get; }

    public IPluginRegistry<INodePresenterUpdater> NodePresenterUpdaters { get; }

    /// <summary>
    /// The services the plugin added to the session, in order.
    /// </summary>
    public IReadOnlyList<object> AddedServices => services;

    /// <summary>
    /// The commands the plugin added to the property grid, in order.
    /// </summary>
    public IReadOnlyList<INodePresenterCommand> AddedCommands { get; private set; } = [];

    /// <summary>
    /// The updaters the plugin added to the property grid, in order.
    /// </summary>
    public IReadOnlyList<INodePresenterUpdater> AddedUpdaters { get; private set; } = [];

    /// <summary>
    /// The actions the plugin asked to run when it leaves the session.
    /// </summary>
    public IReadOnlyList<Action> CloseActions => closeActions;

    private GraphViewModelService GraphService => graphService ?? throw new InvalidOperationException("The session has no property grid.");

    // The updaters of the session's property grid, or of the recording one of a session made by CreateSession
    private IEnumerable<INodePresenterUpdater> Updaters => graphService?.AvailableUpdaters ?? (Session.AssetViewProperties as RecordingProperties)?.Updaters ?? [];

    /// <summary>
    /// A session that has only services and a property grid that records its updaters, for the plugins that need no more of
    /// a session to initialize it; any other member throws.
    /// </summary>
    public static ISessionViewModel CreateSession()
    {
        var session = DispatchProxy.Create<ISessionViewModel, SessionProxy>();
        ((SessionProxy)(object)session).ServiceProvider = new ViewModelServiceProvider([new NullDispatcherService()]);
        return session;
    }

    public void OnClose(Action action) => closeActions.Add(action);

    /// <summary>
    /// Initializes <paramref name="plugin"/> for <paramref name="session"/>, as the editor does once its views are ready, and
    /// records what it adds to the session.
    /// </summary>
    public static SessionRegistrations Of(AssetsPlugin plugin, ISessionViewModel session)
    {
        var registrations = new SessionRegistrations(plugin, session);
        var commands = registrations.graphService?.AvailableCommands.ToList() ?? [];
        var updaters = registrations.Updaters.ToList();
        void ServiceRegistered(object? sender, ServiceRegistrationEventArgs e) => registrations.services.Add(e.Service);
        session.ServiceProvider.ServiceRegistered += ServiceRegistered;
        try
        {
            plugin.InitializeSession(registrations);
        }
        finally
        {
            session.ServiceProvider.ServiceRegistered -= ServiceRegistered;
        }
        registrations.AddedCommands = registrations.graphService?.AvailableCommands.Except(commands).ToList() ?? [];
        registrations.AddedUpdaters = registrations.Updaters.Except(updaters).ToList();
        return registrations;
    }

    /// <summary>
    /// The registrations as text, one per line, in registration order.
    /// </summary>
    public string Describe()
    {
        var builder = new StringBuilder();
        builder.Append("Plugin: ").AppendLine(Plugin.GetType().FullName);
        PluginRegistrations.Section(builder, nameof(Services), services.Select(x => PluginRegistrations.Name(x.GetType())));
        PluginRegistrations.Section(builder, nameof(NodePresenterCommands), AddedCommands.Select(x => PluginRegistrations.Name(x.GetType())));
        PluginRegistrations.Section(builder, nameof(NodePresenterUpdaters), AddedUpdaters.Select(x => PluginRegistrations.Name(x.GetType())));
        return builder.ToString();
    }

    /// <summary>
    /// Takes back what the plugin added, so that another plugin can be recorded on the same session.
    /// </summary>
    public void Undo()
    {
        foreach (var action in closeActions.AsEnumerable().Reverse())
            action();
        foreach (var updater in AddedUpdaters)
            Session.AssetViewProperties.UnregisterNodePresenterUpdater(updater);
        foreach (var command in AddedCommands)
            GraphService.AvailableCommands.Remove(command);
        foreach (var service in services.AsEnumerable().Reverse())
            Session.ServiceProvider.UnregisterService(service);
    }

    public class SessionProxy : DispatchProxy
    {
        private readonly RecordingProperties properties = new();

        internal IViewModelServiceProvider? ServiceProvider { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                "get_" + nameof(ISessionViewModel.ServiceProvider) => ServiceProvider,
                "get_" + nameof(ISessionViewModel.AssetViewProperties) => properties,
                "get_" + nameof(ISessionViewModel.Dispatcher) => ServiceProvider?.Get<IDispatcherService>(),
                _ => throw new NotSupportedException($"The recording session has no {targetMethod?.Name}."),
            };
        }
    }

    private sealed class RecordingProperties : ISessionObjectPropertiesViewModel
    {
        public List<INodePresenterUpdater> Updaters { get; } = [];

        public Task<GraphViewModel> GenerateSelectionPropertiesAsync(IEnumerable<IPropertyProviderViewModel> selectedObjects) => throw new NotSupportedException();

        public Task RefreshSelectedPropertiesAsync() => throw new NotSupportedException();

        public void RegisterNodePresenterUpdater(INodePresenterUpdater nodeUpdater) => Updaters.Add(nodeUpdater);

        public void UnregisterNodePresenterUpdater(INodePresenterUpdater nodeUpdater) => Updaters.Remove(nodeUpdater);
    }

    private sealed class Registry<T>(Action<T> register) : IPluginRegistry<T>
        where T : notnull
    {
        public void Register(T item) => register(item);
    }
}
