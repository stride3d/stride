// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_VIDEO_AVFOUNDATION
using Stride.Core;
using Stride.Graphics;

namespace Stride.Video.Backends;

public sealed class AVFoundationVideoBackendFactory : VideoBackendFactory
{
    public override string Name => "AVFoundation";
    public override int Priority => 200; // platform-native HW decode on iOS/macOS
    public override bool IsSupported(GraphicsDevice device) => true;

    public override VideoBackend CreateBackend(VideoInstance instance) => new AVFoundationVideoBackend(instance);
}

internal static class AVFoundationVideoBackendModule
{
    [ModuleInitializer]
    public static void Initialize() => VideoBackendRegistry.Register(new AVFoundationVideoBackendFactory());
}
#endif
