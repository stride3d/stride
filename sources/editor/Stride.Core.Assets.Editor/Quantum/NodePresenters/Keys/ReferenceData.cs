// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys
{
    public static class ReferenceData
    {       
        public const string AddReferenceViewModel = nameof(AddReferenceViewModel);

        public static readonly PropertyKey<IAddReferenceViewModel> Key = new PropertyKey<IAddReferenceViewModel>(AddReferenceViewModel, typeof(ReferenceData));
    }
}
