// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if WINDOWS
using Stride.Audio.Layers.XAudio;
using Stride.Core.Mathematics;

namespace Stride.Audio;

public sealed class Listener : IInitializable
{
    internal Device device;
    internal X3DAudioListener listener;
    internal Matrix worldTransform;
    public bool Initialized => true;
}
#endif