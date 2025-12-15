// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Audio.Layers.XAudio;

internal struct X3DAudioDistanceCurvePoint
{
    public float Distance;   // normalized distance, must be within [0.0f, 1.0f]
	public float DSPSetting; // DSP setting
}