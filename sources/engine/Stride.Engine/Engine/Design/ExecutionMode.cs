// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Engine.Design
{
    /// <summary>
    /// Describes the different execution mode of the engine.
    /// </summary>
    [Flags]
    public enum ExecutionMode
    {
        None = 0,
        Runtime = 1,
        Editor = 2,
        Thumbnail = 4,
        Preview = 8,
        EffectCompile = 16,
        All = Runtime | Editor | Thumbnail | Preview | EffectCompile,
    }
}
