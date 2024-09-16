// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Audio.Layers.XAudio;

internal unsafe struct X3DAudioDistanceCurve
{
    public X3DAudioDistanceCurvePoint* pPoints;    // distance curve point array, must have at least PointCount elements with no duplicates and be sorted in ascending order with respect to Distance
	public uint PointCount; // number of distance curve points, must be >= 2 as all distance curves must have at least two endpoints, defining DSP settings at 0.0f and 1.0f normalized distance
} 