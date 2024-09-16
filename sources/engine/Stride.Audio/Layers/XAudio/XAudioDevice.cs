// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Silk.NET.XAudio;

namespace Stride.Audio;

public sealed unsafe class XAudioDevice
{
    internal IXAudio2* xAudio;
    internal X3DAUDIO_HANDLE x3_audio;
    internal IXAudio2MasteringVoice* masteringVoice;
    internal bool hrtf;
}