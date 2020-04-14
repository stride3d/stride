// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_EFFECT_COMPILER
using System;

namespace Xenko.Shaders.Parser.Analysis
{
    [Flags]
    internal enum AssignmentOperatorStatus
    {
        None = 0,
        Read = 1,
        Write = 2,
        ReadWrite = Read | Write,
    }
}
#endif
