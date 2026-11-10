// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using Stride.Assets.Skyboxes;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Presentation.Annotations;

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// View model for <see cref="SkyboxAsset"/>.
/// </summary>
[AssetViewModel<SkyboxAsset>]
public sealed class SkyboxViewModel : AssetViewModel<SkyboxAsset>
{
    public SkyboxViewModel(AssetViewModelConstructionParameters parameters)
        : base(parameters)
    {
    }
}
