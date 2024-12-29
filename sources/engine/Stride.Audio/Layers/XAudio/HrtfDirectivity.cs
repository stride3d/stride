// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Audio.Layers.XAudio;

internal class HrtfDirectivity
{
    public readonly HrtfDirectivityType omniDirectional;
    public readonly float hrtfDirectionFactor;

    public HrtfDirectivity(HrtfDirectivityType omniDirectional, float hrtfDirectionFactor)
    {
        this.omniDirectional = omniDirectional;
        this.hrtfDirectionFactor = hrtfDirectionFactor;
    }
}