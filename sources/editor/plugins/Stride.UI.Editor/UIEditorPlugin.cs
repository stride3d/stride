// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Editor;
using Stride.Editor.Annotations;
using Stride.Engine;
using Stride.UI.Assets;
using Stride.UI.Editor;

[assembly: TypeImage(typeof(UIComponent), "UIComponent.png")]
[assembly: TypeImage(typeof(UIElementLinkComponent), "UIElementLinkComponent.png")]
[assembly: StaticThumbnail(typeof(UILibraryAsset), "UILibraryThumbnail.png")]

#pragma warning disable 436 // Stride.PublicKeys is defined in multiple assemblies
// The WPF views of these view models
[assembly: InternalsVisibleTo("Stride.UI.Editor.Wpf" + Stride.PublicKeys.Default)]

namespace Stride.UI.Editor;

/// <summary>
/// Registers the UI property grid updater and paste processor; the other UI editor parts are found by their attributes.
/// </summary>
public sealed class UIEditorPlugin : StrideAssetsPlugin
{
    public override void InitializeSession(SessionViewModel session)
    {
        session.AssetViewProperties.RegisterNodePresenterUpdater(new UIAssetNodeUpdater());
    }

    /// <inheritdoc />
    public override void RegisterPasteProcessors(ICollection<IPasteProcessor> pasteProcessors, SessionViewModel session)
    {
        pasteProcessors.Add(new UIHierarchyPasteProcessor());
    }

    /// <summary>
    /// Reads an embedded resource of this assembly by file name (the gizmo billboard).
    /// </summary>
    internal static byte[] LoadResource(string fileName)
    {
        var assembly = typeof(UIEditorPlugin).Assembly;
        var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith("." + fileName, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded resource [{fileName}] not found in {assembly.GetName().Name}.");
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
