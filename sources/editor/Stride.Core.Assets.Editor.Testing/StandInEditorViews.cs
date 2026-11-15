// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Assets.Editor.Services;

namespace Stride.Core.Assets.Editor.Testing;

/// <summary>
/// An editor view that shows nothing, for a test that opens an editor: the session opens the editor of an asset only when a view is
/// registered for its view model.
/// </summary>
public sealed class StandInEditorView : IAssetEditorView;

/// <summary>
/// A plugin that registers <see cref="StandInEditorView"/> as the view of the given editor view models.
/// </summary>
public sealed class StandInEditorViewsPlugin : AssetsPlugin
{
    private readonly IReadOnlyList<Type> editorViewModelTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="StandInEditorViewsPlugin"/> class.
    /// </summary>
    /// <param name="editorViewModelTypes">The types of the editor view models that get the stand-in view.</param>
    public StandInEditorViewsPlugin(params IEnumerable<Type> editorViewModelTypes)
    {
        ArgumentNullException.ThrowIfNull(editorViewModelTypes);
        this.editorViewModelTypes = editorViewModelTypes.ToList();
    }

    /// <inheritdoc/>
    public override IReadOnlyList<Assembly> ScannedAssemblies => [];

    /// <inheritdoc/>
    public override void Initialize(IEditorPluginContext context)
    {
        foreach (var type in editorViewModelTypes)
            context.AssetEditorViewTypes.Register(type, typeof(StandInEditorView));
    }
}
