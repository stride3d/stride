// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using Stride.Audio.Assets;
using Stride.Audio.Editor;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Editor;
using Stride.Editor.Annotations;
using Stride.Engine;

[assembly: TypeImage(typeof(AudioEmitterComponent), "AudioEmitterComponent.png")]
[assembly: TypeImage(typeof(AudioListenerComponent), "AudioListenerComponent.png")]
[assembly: StaticThumbnail(typeof(SoundAsset), "SoundThumbnail.png")]

namespace Stride.Audio.Editor;

/// <summary>
/// The editor side of audio: the emitter and listener gizmos and entity factories, the sound preview and its view,
/// the thumbnail and the editor-game compiler are found by their attributes.
/// </summary>
public sealed class AudioEditorPlugin : StrideAssetsPlugin
{
    /// <summary>
    /// Reads an embedded resource of this assembly by file name (the gizmo billboards).
    /// </summary>
    internal static byte[] LoadResource(string fileName)
    {
        var assembly = typeof(AudioEditorPlugin).Assembly;
        var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith("." + fileName, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded resource [{fileName}] not found in {assembly.GetName().Name}.");
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
