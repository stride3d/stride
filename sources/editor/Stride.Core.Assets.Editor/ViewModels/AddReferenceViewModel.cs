// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable
using System.Collections.Generic;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels
{
    public abstract class AddReferenceViewModel : IAddReferenceViewModel
    {
        protected NodeViewModel TargetNode;

        public void SetTargetNode(NodeViewModel node)
        {
            TargetNode = node;
        }

        public abstract bool CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message);

        public abstract void AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers);
    }
}
