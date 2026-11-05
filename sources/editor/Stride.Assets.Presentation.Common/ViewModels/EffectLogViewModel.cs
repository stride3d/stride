// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using Stride.Assets.Effect;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Presentation.Annotations;

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// View model for <see cref="EffectLogAsset"/>.
/// </summary>
[AssetViewModel<EffectLogAsset>]
public class EffectLogViewModel : AssetViewModel<EffectLogAsset>
{
    public EffectLogViewModel(AssetViewModelConstructionParameters parameters)
        : base(parameters)
    {
    }

    public string Text
    {
        get => Asset.Text;
        set => SetValue(Asset.Text != value, () => Asset.Text = value);
    }
}
