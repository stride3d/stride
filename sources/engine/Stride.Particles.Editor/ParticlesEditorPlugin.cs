// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Editor;
using Stride.Particles.Components;
using Stride.Particles.Editor;

[assembly: TypeImage(typeof(ParticleSystemComponent), "ParticleSystemComponent.png")]
[assembly: EnumImage(StateControl.Play, "StateControlPlay.png")]
[assembly: EnumImage(StateControl.Pause, "StateControlPause.png")]
[assembly: EnumImage(StateControl.Stop, "StateControlStop.png")]

namespace Stride.Particles.Editor;

/// <summary>
/// The editor side of particles: the gizmo, the entity factories, the material change watcher and the render feature
/// of previews and thumbnails are found by their attributes; the property grid updater is registered here.
/// </summary>
public sealed class ParticlesEditorPlugin : StrideAssetsPlugin
{
    public override void InitializeSession(SessionViewModel session)
    {
        session.AssetViewProperties.RegisterNodePresenterUpdater(new ParticleSystemNodeUpdater());
    }

    /// <summary>
    /// Reads an embedded resource of this assembly by file name (the gizmo billboard).
    /// </summary>
    internal static byte[] LoadResource(string fileName)
    {
        var assembly = typeof(ParticlesEditorPlugin).Assembly;
        var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith("." + fileName, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded resource [{fileName}] not found in {assembly.GetName().Name}.");
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
