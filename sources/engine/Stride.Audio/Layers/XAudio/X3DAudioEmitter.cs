// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Numerics;

namespace Stride.Audio.Layers.XAudio;

public unsafe class X3DAudioEmitter()
{
    internal X3DAudioCone* pCone; // sound cone, used only with single-channel emitters for matrix, LPF (both direct and reverb paths), and reverb calculations, NULL specifies omnidirectionality
    public Vector3 OrientFront; // orientation of front direction, used only for emitter angle calculations or with multi-channel emitters for matrix calculations or single-channel emitters with cones for matrix, LPF (both direct and reverb paths), and reverb calculations, must be normalized when used
    public Vector3 OrientTop;   // orientation of top direction, used only with multi-channel emitters for matrix calculations, must be orthonormal with OrientFront when used

    public Vector3 Position; // position in user-defined world units, does not affect Velocity
    public Vector3 Velocity; // velocity vector in user-defined world units/second, used only for doppler calculations, does not affect Position

    public float InnerRadius;      // inner radius, must be within [0.0f, FLT_MAX]
    public float InnerRadiusAngle; // inner radius angle, must be within [0.0f, X3DAUDIO_PI/4.0)

    public uint ChannelCount;       // number of sound channels, must be > 0
    float ChannelRadius;     // channel radius, used only with multi-channel emitters for matrix calculations, must be >= 0.0f when used
    float* pChannelAzimuths; // channel azimuth array, used only with multi-channel emitters for matrix calculations, contains positions of each channel expressed in radians along the channel radius with respect to the front orientation vector in the plane orthogonal to the top orientation vector, or X3DAUDIO_2PI to specify an LFE channel, must have at least ChannelCount elements, all within [0.0f, X3DAUDIO_2PI] when used

    internal X3DAudioDistanceCurve* pVolumeCurve;    // volume level distance curve, used only for matrix calculations, NULL specifies a default curve that conforms to the inverse square law, calculated in user-defined world units with distances <= CurveDistanceScaler clamped to no attenuation
    internal X3DAudioDistanceCurve* pLFECurve;       // LFE level distance curve, used only for matrix calculations, NULL specifies a default curve that conforms to the inverse square law, calculated in user-defined world units with distances <= CurveDistanceScaler clamped to no attenuation
    internal X3DAudioDistanceCurve* pLPFDirectCurve; // LPF direct-path coefficient distance curve, used only for LPF direct-path calculations, NULL specifies the default curve: [0.0f,1.0f], [1.0f,0.75f]
    internal X3DAudioDistanceCurve* pLPFReverbCurve; // LPF reverb-path coefficient distance curve, used only for LPF reverb-path calculations, NULL specifies the default curve: [0.0f,0.75f], [1.0f,0.75f]
    internal X3DAudioDistanceCurve* pReverbCurve;    // reverb send level distance curve, used only for reverb calculations, NULL specifies the default curve: [0.0f,1.0f], [1.0f,0.0f]

    public float CurveDistanceScaler; // curve distance scaler, used to scale normalized distance curves to user-defined world units and/or exaggerate their effect, used only for matrix, LPF (both direct and reverb paths), and reverb calculations, must be within [FLT_MIN, FLT_MAX] when used
    public float DopplerScaler;       // doppler shift scaler, used to exaggerate doppler shift effect, used only for doppler calculations, must be within [0.0f, FLT_MAX] when used
}