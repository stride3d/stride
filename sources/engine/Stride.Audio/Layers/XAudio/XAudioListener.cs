// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Audio.Layers.XAudio;
using Stride.Core.Mathematics;

namespace Stride.Audio;

internal sealed class XAudioListener
{
    internal XAudioDevice device;
    internal X3DAudioListener listener;
    internal Matrix worldTransform;
}