// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if LINUX || OSX 
using System.Collections.Generic;

namespace Stride.Audio;

public struct Listener() : IInitializable
{
    public List<Source> Sources { get; internal set; } = [];
    public Device Device { get; internal set; } = new();
    public unsafe Silk.NET.OpenAL.Context* Context { get; internal set; }
    public Dictionary<uint, AudioBuffer> Buffers { get; internal set; } = [];
    public bool Initialized { get; internal set; }
}
#endif