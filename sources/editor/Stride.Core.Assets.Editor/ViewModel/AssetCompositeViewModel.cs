// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Quantum;
using Stride.Core.Annotations;

namespace Stride.Core.Assets.Editor.ViewModel
{
    public abstract class AssetCompositeViewModel<TAsset> : AssetViewModel<TAsset> where TAsset : AssetComposite
    {
        public AssetCompositePropertyGraph AssetCompositePropertyGraph => (AssetCompositePropertyGraph)PropertyGraph;

        protected AssetCompositeViewModel([NotNull] AssetViewModelConstructionParameters parameters) : base(parameters)
        {
        }
    }
}
