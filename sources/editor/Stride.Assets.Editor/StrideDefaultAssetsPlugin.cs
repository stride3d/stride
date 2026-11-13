// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Assets.Presentation;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Reflection;
using Stride.Core.Serialization;
using Stride.Engine;

namespace Stride.Assets.Editor;

/// <summary>
/// The assets plugin of the Stride engine assets: the asset view models of <c>Stride.Assets.Presentation</c>, primitive types
/// and the entity component metadata that the property grids rely on.
/// </summary>
/// <remarks>
/// It covers <c>Stride.Assets.Presentation</c>, which holds view models and does not reference the editor layer, so it can
/// hold no plugin itself: the host registers what that assembly declares in this plugin's name (<see cref="ScannedAssemblies"/>),
/// and not what this plugin's own assembly declares, which <see cref="StrideEditorPlugin"/> covers.
/// The editor-level registrations (node presenter commands and updaters, previews, gizmos, entity factories...) are in
/// <see cref="StrideEditorPlugin"/>.
/// A default plugin of the cross-platform editor: its host registers it (<c>Stride.Assets.Editor.Avalonia</c>), the discovery skips it.
/// </remarks>
[EditorDefaultPlugin]
public sealed class StrideDefaultAssetsPlugin : AssetsPlugin
{
    private static bool initialized;

    /// <inheritdoc />
    /// <remarks>Only <c>Stride.Assets.Presentation</c>.</remarks>
    public override IReadOnlyList<Assembly> ScannedAssemblies { get; } = [typeof(EntityComponentMetadata).Assembly];

    /// <inheritdoc />
    public override void Initialize(IEditorPluginContext context)
    {
        base.Initialize(context);

        context.PrimitiveTypes.Register(typeof(AssetReference));
        context.PrimitiveTypes.Register(typeof(UrlReferenceBase));

        // The entity component metadata is process-wide: it is set once, by the first session
        if (initialized)
            return;
        initialized = true;

        AssemblyRegistry.AssemblyRegistered += (_, e) =>
        {
            EntityComponentMetadata.SetDisplayFallback(e.Assembly.GetTypes());
        };

        EntityComponentMetadata.SetDisplayFallback(typeof(EntityComponent).GetInheritedInstantiableTypes());
        EntityComponentMetadata.RegisterComponentOrders(context.Logger);
    }
}
