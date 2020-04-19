// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Diagnostics;
using Stride.Engine.Network;

namespace Stride.Shaders.Compiler
{
    // TODO: Make that private as soon as we stop signing assemblies (so that EffectCompilerServer can use it)
    public class RemoteEffectCompilerEffectAnswer : SocketMessage
    {
        // TODO: Support LoggerResult as well
        public EffectBytecode EffectBytecode { get; set; }

        public List<SerializableLogMessage> LogMessages { get; set; }

        public bool LogHasErrors { get; set; }
    }
}
