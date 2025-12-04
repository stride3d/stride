// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Audio.Layers.XAudio;

using System.Runtime.InteropServices;

[Guid("15B3CD66-E9DE-4464-B6E6-2BC3CF63D455")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IXAPOHrtfParameters
{
    /// <summary>
    /// The position of the sound relative to the listener.
    /// </summary>
    /// <param name="position">Pointer to the HrtfPosition structure containing the position.</param>
    void SetSourcePosition(ref HrtfPosition position);

    /// <summary>
    /// The rotation matrix for the source orientation, with respect to the listener's frame of reference (the listener's coordinate system).
    /// </summary>
    /// <param name="orientation">Pointer to the HrtfOrientation structure containing the orientation.</param>
    void SetSourceOrientation(ref HrtfOrientation orientation);

    /// <summary>
    /// The custom direct path gain value for the current source position. Valid only for sounds played with the HrtfDistanceDecayType. Custom decay type.
    /// </summary>
    /// <param name="gain">The gain value.</param>
    void SetSourceGain(float gain);

    /// <summary>
    /// Selects the acoustic environment to simulate.
    /// </summary>
    /// <param name="environment">The environment to be simulated.</param>
    void SetEnvironment(HrtfEnvironment environment);
}

[StructLayout(LayoutKind.Sequential)]
public struct HrtfPosition
{
    public float x;
	public float y;
	public float z;
}

[StructLayout(LayoutKind.Sequential)]
public struct HrtfOrientation
{
    public float[] element;
}