// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable disable
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Presentation.ViewModels
{
    public interface IReorderItemViewModel : IInsertChildViewModel
    {
        void SetTargetNode(NodeViewModel node);
    }
}
