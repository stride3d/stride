// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Testing;

/// <summary>
/// A session over a package written for the test in a temporary folder, opened the way the editor opens one, with an
/// <see cref="ExclusiveDispatcherService"/> as the UI thread.
/// </summary>
/// <remarks>
/// <para>The session opens in the dispatcher, and a test that awaits <see cref="OpenAsync"/> goes on there
/// (<see cref="DispatcherTask{TResult}"/>): it changes the session and disposes it there, as the editor does on its UI thread.
/// Disposing destroys the session, waits for its background work and deletes the folder.</para>
/// <para>The package loads without the NuGet restore and the MSBuild evaluation of its project, which have nothing to give for it
/// (<see cref="TestPackageLoad"/>).</para>
/// </remarks>
public sealed class EditorTestSession : IAsyncDisposable
{
    /// <summary>
    /// The default name of the package.
    /// </summary>
    public const string DefaultPackageName = "TestGame";

    private bool disposed;

    private EditorTestSession(string directory, string packageName, UFile packagePath, IViewModelServiceProvider serviceProvider)
    {
        Directory = directory;
        PackageName = packageName;
        PackagePath = packagePath;
        ServiceProvider = serviceProvider;
    }

    /// <summary>
    /// The temporary folder of the test, which holds the folder of the package.
    /// </summary>
    public string Directory { get; }

    /// <summary>
    /// The name of the package.
    /// </summary>
    public string PackageName { get; }

    /// <summary>
    /// The path of the package file.
    /// </summary>
    public UFile PackagePath { get; }

    /// <summary>
    /// The services the session was opened with.
    /// </summary>
    public IViewModelServiceProvider ServiceProvider { get; }

    /// <summary>
    /// The result of the loading of the package.
    /// </summary>
    public PackageSessionResult Result { get; } = new();

    /// <summary>
    /// The session.
    /// </summary>
    public SessionViewModel Session { get; private set; } = null!;

    /// <summary>
    /// The messages of the loading, for the messages of failed assertions.
    /// </summary>
    public string Log => string.Join(Environment.NewLine, Result.Messages.Select(x => x.ToString()));

    /// <summary>
    /// Writes a package with the given assets and opens it.
    /// </summary>
    /// <param name="plugins">The plugin host of the session, which knows the plugins of the assets and their editors.</param>
    /// <param name="assets">The assets, each with its location in the package, without extension.</param>
    /// <returns>The open session, which continues its caller in the dispatcher.</returns>
    public static DispatcherTask<EditorTestSession> OpenAsync(IAssetsPluginService plugins, params (string Location, Asset Asset)[] assets)
        => OpenAsync(new EditorTestSessionOptions { Plugins = plugins }, assets);

    /// <summary>
    /// Writes a package with the given assets and opens it, with the given options.
    /// </summary>
    /// <param name="options">The plugin host, the services and the name of the package.</param>
    /// <param name="assets">The assets, each with its location in the package, without extension.</param>
    /// <returns>The open session, which continues its caller in the dispatcher.</returns>
    /// <exception cref="InvalidOperationException">The session could not be opened; the message has the log of the loading.</exception>
    public static DispatcherTask<EditorTestSession> OpenAsync(EditorTestSessionOptions options, params (string Location, Asset Asset)[] assets)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Plugins);
        ArgumentNullException.ThrowIfNull(assets);
        return DispatcherTask.InDispatcher(() => OpenCoreAsync(options, assets));
    }

    /// <summary>
    /// The location of an asset of the package as the session names it, prefixed with the name of the package.
    /// </summary>
    /// <param name="location">The location in the package.</param>
    public string Url(string location) => "/" + PackageName + "/" + location;

    /// <summary>
    /// Gets the view model of the asset at a location of the package.
    /// </summary>
    /// <typeparam name="T">The type of the view model.</typeparam>
    /// <param name="location">The location in the package.</param>
    /// <exception cref="InvalidOperationException">No asset of that type is at that location.</exception>
    public T Asset<T>(string location) where T : AssetViewModel
        => Session.GetAssetByUrl(Url(location)) as T ?? throw new InvalidOperationException($"No {typeof(T).Name} at {location}.");

    /// <summary>
    /// Runs the action as the UI thread of the editor would: in the dispatcher, one at a time with the work the session dispatches.
    /// </summary>
    public void OnUiThread(Action action) => Session.Dispatcher.Invoke(action);

    /// <inheritdoc cref="OnUiThread(Action)"/>
    public T OnUiThread<T>(Func<T> action) => Session.Dispatcher.Invoke(action);

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (disposed)
            return;
        disposed = true;
        if (Session is not null)
            await SessionTeardown.DestroyAsync(Session);
        try
        {
            System.IO.Directory.Delete(Directory, true);
        }
        catch (IOException)
        {
            // Best effort: a file the session opened last can still be closing
        }
    }

    private static async Task<EditorTestSession> OpenCoreAsync(EditorTestSessionOptions options, (string Location, Asset Asset)[] assets)
    {
        var dispatcher = new ExclusiveDispatcherService();
        dispatcher.EnsureAccess();
        var directory = Path.Combine(Path.GetTempPath(), "StrideEditorTestSessions", Guid.NewGuid().ToString("N"));
        var packageName = options.PackageName ?? DefaultPackageName;
        IEnumerable<object> services =
        [
            dispatcher,
            options.Plugins!,
            .. options.Services,
        ];
        // A service the test doesn't give fails when it is used
        if (!options.Services.OfType<IEditorDialogService>().Any())
            services = services.Append(NotSupportedService<IEditorDialogService>.Create());
        if (!options.Services.OfType<IClipboardService>().Any())
            services = services.Append(NotSupportedService<IClipboardService>.Create());
        var serviceProvider = new ViewModelServiceProvider(services);

        UFile packagePath;
        try
        {
            packagePath = TestPackage.Write(directory, packageName, assets);
        }
        catch
        {
            // An asset the package can't hold leaves no folder behind
            if (System.IO.Directory.Exists(directory))
                System.IO.Directory.Delete(directory, true);
            throw;
        }
        var fixture = new EditorTestSession(directory, packageName, packagePath, serviceProvider);
        try
        {
            var session = await SessionViewModel.OpenSessionAsync(packagePath, loadParameters => TestPackageLoad.WithoutMSBuild(packagePath, null, fixture.Result, loadParameters),
                fixture.Result, new TestMainViewModel(serviceProvider), serviceProvider);
            fixture.Session = session ?? throw new InvalidOperationException("The session could not be opened:" + Environment.NewLine + fixture.Log);
            options.SessionOpened?.Invoke(session);
            // What the session does in the background once open, so that the test starts from a session at rest
            await session.Initialization;
            return fixture;
        }
        catch
        {
            await fixture.DisposeAsync();
            throw;
        }
    }
}

/// <summary>
/// The options of <see cref="EditorTestSession.OpenAsync(EditorTestSessionOptions, (string Location, Asset Asset)[])"/>.
/// </summary>
public sealed class EditorTestSessionOptions
{
    /// <summary>
    /// The plugin host of the session, which knows the plugins of the assets and their editors.
    /// </summary>
    public IAssetsPluginService? Plugins { get; init; }

    /// <summary>
    /// Other services of the session, such as fakes of the dialog and clipboard services; without them, these two services fail when
    /// they are used (<see cref="NotSupportedService{T}"/>).
    /// </summary>
    public IReadOnlyList<object> Services { get; init; } = [];

    /// <summary>
    /// The name of the package; <see cref="EditorTestSession.DefaultPackageName"/> when <c>null</c>.
    /// </summary>
    public string? PackageName { get; init; }

    /// <summary>
    /// Runs once the session is open, in the dispatcher, before its initialization ends: for example to initialize the plugins of
    /// the session.
    /// </summary>
    public Action<SessionViewModel>? SessionOpened { get; init; }
}
