// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if LINUX || OSX 
using System.Collections.Generic;

namespace Stride.Audio;

public class Device()
{
    public SpinLock DeviceLock { get; set; } = new();
    public List<Listener> Listeners { get; internal set; } = [];
    public unsafe Silk.NET.OpenAL.Device* Value { get; internal set; }
}
#endif
