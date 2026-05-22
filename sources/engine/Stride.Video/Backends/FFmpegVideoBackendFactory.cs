// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_VIDEO_FFMPEG
using Stride.Core;
using Stride.Graphics;
using Stride.Video.FFmpeg;

namespace Stride.Video.Backends;

public sealed class FFmpegVideoBackendFactory : VideoBackendFactory
{
    public override string Name => "FFmpeg";
    public override int Priority => 100;
    public override bool IsSupported(GraphicsDevice device) => true;

    public override void InitializeSystem(VideoSystem system)
    {
        FFmpegUtils.PreloadLibraries();
        FFmpegUtils.Initialize();
    }

    public override VideoBackend CreateBackend(VideoInstance instance) => new FFmpegVideoBackend(instance);
}

internal static class FFmpegVideoBackendModule
{
    [ModuleInitializer]
    public static void Initialize() => VideoBackendRegistry.Register(new FFmpegVideoBackendFactory());
}
#endif
