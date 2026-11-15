// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Views;

namespace Stride.Assets.Editor.Testing;

/// <summary>
/// The plugin host of the editors, for the plugins it is given only: it resolves the types they register the way the plugin host of
/// Game Studio does, the view model of an asset type (or of a base type) and the view model of a preview type (or of its generic
/// definition). It takes no template provider, since there is no view.
/// </summary>
public sealed class TestPluginHost : SceneEditorPluginHost
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestPluginHost"/> class.
    /// </summary>
    /// <param name="plugins">The plugins.</param>
    public TestPluginHost(params AssetsPlugin[] plugins)
        : base(plugins)
    {
    }

    /// <inheritdoc/>
    protected override Type AssetEditorViewType => typeof(IAssetEditorView);

    /// <inheritdoc/>
    protected override bool AddTemplateProvider(ISessionViewModel session, ITemplateProvider provider) => false;

    /// <inheritdoc/>
    protected override void RemoveTemplateProvider(ISessionViewModel session, ITemplateProvider provider)
    {
    }
}
