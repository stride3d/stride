// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Audio.Layers.XAudio;

public unsafe class X3DAudioDSPSettings
{
    public float* pMatrixCoefficients; // [inout] matrix coefficient table, receives an array representing the volume level used to send from source channel S to destination channel D, stored as pMatrixCoefficients[SrcChannelCount * D + S], must have at least SrcChannelCount*DstChannelCount elements
    public float* pDelayTimes;         // [inout] delay time array, receives delays for each destination channel in milliseconds, must have at least DstChannelCount elements (stereo final mix only)
    public uint SrcChannelCount;       // [in] number of source channels, must equal number of channels in respective emitter
    public uint DstChannelCount;       // [in] number of destination channels, must equal number of channels of the final mix

    public float LPFDirectCoefficient; // [out] LPF direct-path coefficient
    float LPFReverbCoefficient; // [out] LPF reverb-path coefficient
    float ReverbLevel; // [out] reverb send level
    public float DopplerFactor; // [out] doppler shift factor, scales resampler ratio for doppler shift effect, where the effective frequency = DopplerFactor * original frequency
    float EmitterToListenerAngle; // [out] emitter-to-listener interior angle, expressed in radians with respect to the emitter's front orientation

    float EmitterToListenerDistance; // [out] distance in user-defined world units from the emitter base to listener position, always calculated
    float EmitterVelocityComponent; // [out] component of emitter velocity vector projected onto emitter->listener vector in user-defined world units/second, calculated only for doppler
    float ListenerVelocityComponent; // [out] component of listener velocity vector projected onto emitter->listener vector in user-defined world units/second, calculated only for doppler
}