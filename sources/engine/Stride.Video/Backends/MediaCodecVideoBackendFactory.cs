// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_ANDROID && STRIDE_VIDEO_MEDIACODEC
using Stride.Core;
using Stride.Graphics;

namespace Stride.Video.Backends;

public sealed class MediaCodecVideoBackendFactory : VideoBackendFactory
{
    public override string Name => "MediaCodec";
    public override int Priority => 200; // platform-native HW decode on Android
    public override bool IsSupported(GraphicsDevice device) => true;

    public override VideoBackend CreateBackend(VideoInstance instance) => new MediaCodecVideoBackend(instance);
}

internal static class MediaCodecVideoBackendModule
{
    [ModuleInitializer]
    public static void Initialize() => VideoBackendRegistry.Register(new MediaCodecVideoBackendFactory());
}
#endif
