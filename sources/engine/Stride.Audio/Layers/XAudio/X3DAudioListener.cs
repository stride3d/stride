// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;

namespace Stride.Audio.Layers.XAudio;

internal class X3DAudioListener
{
    public Vector3 Position { get; internal set; }
    public Vector3 Velocity { get; internal set; }
    public Vector3 OrientFront { get; internal set; }
    public Vector3 OrientTop { get; internal set; }
}