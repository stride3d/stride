// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Audio;

public partial class AudioBuffer : IInitializable
{
    public short[] Pcm { get; internal set; }
    public int Size { get; internal set; }
    public int SampleRate { get; internal set; }
    public BufferType Type { get; internal set; }
    public bool Initialized { get; internal set; }
    public uint Value;
}