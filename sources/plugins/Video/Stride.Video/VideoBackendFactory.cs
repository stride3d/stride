// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Graphics;

namespace Stride.Video;

/// <summary>
/// Factory for a <see cref="VideoBackend"/> implementation. One factory instance is registered
/// per-backend (typically via <see cref="VideoBackendRegistry.Register"/> from a module
/// initializer). At <see cref="VideoSystem.Initialize"/> time, one factory is selected based on
/// platform support and priority and bound to the system; <see cref="CreateBackend"/> is then
/// called per <see cref="VideoInstance"/>.
/// </summary>
public abstract class VideoBackendFactory
{
    /// <summary>Stable identifier (e.g. "FFmpeg", "MediaEngine", "MediaCodec"). Used for
    /// <see cref="VideoBackendRegistry.PreferredBackendName"/> matching.</summary>
    public abstract string Name { get; }

    /// <summary>Higher value wins when multiple supported backends are registered and no
    /// preferred backend is set.</summary>
    public abstract int Priority { get; }

    /// <summary>Whether this backend can run on the current device/platform.</summary>
    public abstract bool IsSupported(GraphicsDevice device);

    /// <summary>Per-process / per-<see cref="VideoSystem"/> initialization (e.g. MFStartup,
    /// FFmpeg library preload). Called once when this factory is selected as the active
    /// backend. Default: no-op.</summary>
    public virtual void InitializeSystem(VideoSystem system) { }

    /// <summary>Counterpart to <see cref="InitializeSystem"/>. Default: no-op.</summary>
    public virtual void DestroySystem(VideoSystem system) { }

    /// <summary>Construct a backend bound to <paramref name="instance"/>. Called once per
    /// <see cref="VideoInstance"/>.</summary>
    public abstract VideoBackend CreateBackend(VideoInstance instance);
}
