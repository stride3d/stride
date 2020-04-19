// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Core.Quantum
{
    internal interface IGraphNodeInternal : IInitializingGraphNode
    {
        event EventHandler<INodeChangeEventArgs> PrepareChange;
        event EventHandler<INodeChangeEventArgs> FinalizeChange;
    }
}
