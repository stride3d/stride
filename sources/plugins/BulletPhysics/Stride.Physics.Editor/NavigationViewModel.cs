// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Navigation.Assets;
using Stride.Core.Annotations;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Assets.Presentation.ViewModel;

namespace Stride.Physics.Editor
{
    [AssetViewModel<NavigationMeshAsset>]
    public class NavigationViewModel : AssetViewModel<NavigationMeshAsset>
    {
        public NavigationViewModel([NotNull] AssetViewModelConstructionParameters parameters) : base(parameters)
        {
        }
    }
}
