// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.ViewModels;
using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Assets.Editor.Services;

namespace Stride.Assets.Editor.Tests.Helpers;

/// <summary>
/// The views the editor of the tests has: the script editor's only. The editors open only when the editor has their view,
/// so the tests open scripts, and the other editors stay closed here (they have no view without a UI toolkit).
/// </summary>
public sealed class TestEditorViewsPlugin : AssetsPlugin
{
    /// <inheritdoc/>
    public override IReadOnlyList<System.Reflection.Assembly> ScannedAssemblies => [];

    /// <inheritdoc/>
    public override void Initialize(IEditorPluginContext context)
    {
        context.AssetEditorViewTypes.Register(typeof(ScriptSourceFileEditorViewModel), typeof(TestEditorView));
    }

    /// <summary>
    /// A view that shows nothing.
    /// </summary>
    public sealed class TestEditorView : IAssetEditorView;
}
