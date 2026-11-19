// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Testing;
using Stride.Core.Presentation.Services;

namespace Stride.Assets.Editor.Testing;

/// <summary>
/// Opens an <see cref="EditorTestSession"/> with Stride's assets and editors: <see cref="StrideDefaultAssetsPlugin"/>, the plugins of
/// the test, and the stand-in views of the editors the test opens, in a <see cref="TestPluginHost"/>.
/// </summary>
/// <remarks>
/// The assemblies of the assets of the test must be registered first, as the session does for the packages of a game:
/// <c>AssemblyRegistry.Register(typeof(MyAsset).Assembly, AssemblyCommonCategories.Assets)</c>, for example in the static constructor
/// or the module initializer of the tests.
/// </remarks>
public static class StrideEditorTestSession
{
    private static readonly Lazy<bool> MSBuildLocated = new(() =>
    {
        // The session evaluates projects with the MSBuild of the .NET SDK, as the editor does
        PackageSessionPublicHelper.FindAndSetMSBuildVersion();
        return true;
    });

    /// <summary>
    /// Writes a package with the given assets and opens it with Stride's default assets plugin and the given plugins.
    /// </summary>
    /// <param name="plugins">The plugins of the test, such as the editor plugin of an extension.</param>
    /// <param name="assets">The assets, each with its location in the package, without extension.</param>
    /// <returns>The open session, which continues its caller in the dispatcher.</returns>
    public static DispatcherTask<EditorTestSession> OpenAsync(IEnumerable<AssetsPlugin> plugins, params (string Location, Asset Asset)[] assets)
        => OpenAsync(new StrideEditorTestSessionOptions { Plugins = [.. plugins] }, assets);

    /// <summary>
    /// Writes a package with the given assets and opens it with the given options.
    /// </summary>
    /// <param name="options">The plugins, the stand-in views, the services and the name of the package.</param>
    /// <param name="assets">The assets, each with its location in the package, without extension.</param>
    /// <returns>The open session, which continues its caller in the dispatcher.</returns>
    public static DispatcherTask<EditorTestSession> OpenAsync(StrideEditorTestSessionOptions options, params (string Location, Asset Asset)[] assets)
    {
        ArgumentNullException.ThrowIfNull(options);
        _ = MSBuildLocated.Value;
        List<AssetsPlugin> plugins = [];
        if (options.IncludeDefaultAssetsPlugin)
            plugins.Add(new StrideDefaultAssetsPlugin());
        plugins.AddRange(options.Plugins);
        if (options.StandInEditors.Count > 0)
            plugins.Add(new StandInEditorViewsPlugin(options.StandInEditors));
        var host = new TestPluginHost([.. plugins]);
        return EditorTestSession.OpenAsync(new EditorTestSessionOptions
        {
            Plugins = host,
            Services = options.Services,
            PackageName = options.PackageName,
            Dispatcher = options.Dispatcher,
            SessionOpened = options.InitializeSession ? host.InitializeSession : null,
        }, assets);
    }
}

/// <summary>
/// The options of <see cref="StrideEditorTestSession.OpenAsync(StrideEditorTestSessionOptions, (string Location, Asset Asset)[])"/>.
/// </summary>
public sealed class StrideEditorTestSessionOptions
{
    /// <summary>
    /// The plugins of the test, such as the editor plugin of an extension.
    /// </summary>
    public IReadOnlyList<AssetsPlugin> Plugins { get; init; } = [];

    /// <summary>
    /// Whether the session has <see cref="StrideDefaultAssetsPlugin"/>, which Stride's assets need; <c>true</c> by default.
    /// </summary>
    public bool IncludeDefaultAssetsPlugin { get; init; } = true;

    /// <summary>
    /// The editor view models that get a <see cref="StandInEditorView"/>, so that the test can open their editors: the session opens
    /// the editor of an asset only when a view is registered for it.
    /// </summary>
    public IReadOnlyList<Type> StandInEditors { get; init; } = [];

    /// <summary>
    /// Whether to initialize the plugins for the session, as the editor does once its views are ready: they then register their
    /// session services, commands and property grid updaters. <c>false</c> by default.
    /// </summary>
    public bool InitializeSession { get; init; }

    /// <summary>
    /// Other services of the session (<see cref="EditorTestSessionOptions.Services"/>).
    /// </summary>
    public IReadOnlyList<object> Services { get; init; } = [];

    /// <summary>
    /// The name of the package; <see cref="EditorTestSession.DefaultPackageName"/> when <c>null</c>.
    /// </summary>
    public string? PackageName { get; init; }

    /// <summary>
    /// The UI thread of the session (<see cref="EditorTestSessionOptions.Dispatcher"/>): the dispatcher of the toolkit's UI thread for
    /// a test of views; a new <see cref="ExclusiveDispatcherService"/> when <c>null</c>.
    /// </summary>
    public IDispatcherService? Dispatcher { get; init; }
}
