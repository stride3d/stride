// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.


using System.Collections.Generic;

namespace Stride.Audio;

public partial class Source()
{
#if LINUX || OSX 
    public Listener Listener { get; internal set; }
    public List<AudioBuffer> FreeBuffers { get; internal set; } = [];
    public AudioBuffer SingleBuffer { get; internal set; }

    public volatile float DequeuedTime;
    public uint Sources;
    internal uint Value;

#endif
}
