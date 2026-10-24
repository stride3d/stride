// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Graphics;

namespace Stride.Video;

/// <summary>
/// Process-wide registry of <see cref="VideoBackendFactory"/> implementations. Each backend
/// (typically via a module initializer) registers its factory here; <see cref="VideoSystem"/>
/// then selects one at initialization time.
/// </summary>
public static class VideoBackendRegistry
{
    private static readonly List<VideoBackendFactory> factories = new();

    public static void Register(VideoBackendFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        lock (factories)
            factories.Add(factory);
    }

    public static IReadOnlyList<VideoBackendFactory> Factories
    {
        get { lock (factories) return factories.ToArray(); }
    }

    /// <summary>Override default selection. When set, the first registered factory whose
    /// <see cref="VideoBackendFactory.Name"/> matches (case-insensitive) and which reports
    /// <see cref="VideoBackendFactory.IsSupported"/> is used. Tests can set this to force a
    /// specific deterministic backend (e.g. "FFmpeg").</summary>
    public static string PreferredBackendName { get; set; }

    internal static VideoBackendFactory SelectFactory(GraphicsDevice device)
    {
        var preferred = PreferredBackendName;
        var current = Factories;
        if (!string.IsNullOrEmpty(preferred))
        {
            var match = current.FirstOrDefault(f =>
                string.Equals(f.Name, preferred, StringComparison.OrdinalIgnoreCase) && f.IsSupported(device));
            if (match != null) return match;
        }
        return current.Where(f => f.IsSupported(device)).OrderByDescending(f => f.Priority).FirstOrDefault();
    }
}
