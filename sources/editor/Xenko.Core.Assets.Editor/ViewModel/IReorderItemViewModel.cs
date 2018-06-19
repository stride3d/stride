// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Presentation.Quantum.ViewModels;

namespace Xenko.Core.Assets.Editor.ViewModel
{
    public interface IReorderItemViewModel : IInsertChildViewModel
    {
        void SetTargetNode(NodeViewModel node);
    }
}
