// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Xenko.Core.Quantum
{
    internal interface IGraphNodeInternal : IInitializingGraphNode
    {
        event EventHandler<INodeChangeEventArgs> PrepareChange;
        event EventHandler<INodeChangeEventArgs> FinalizeChange;
    }
}
