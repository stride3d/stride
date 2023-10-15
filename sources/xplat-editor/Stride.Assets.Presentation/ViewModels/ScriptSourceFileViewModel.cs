// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Scripts;
using Stride.Core.Assets;
using Stride.Core.Assets.Presentation.Annotations;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// View model for <see cref="ScriptSourceFileAsset"/>.
/// </summary>
[AssetViewModel<ScriptSourceFileAsset>]
public class ScriptSourceFileViewModel : SourceCodeViewModel<ScriptSourceFileAsset>
{
    public ScriptSourceFileViewModel(AssetItem assetItem, DirectoryBaseViewModel directory)
        : base(assetItem, directory)
    {
    }
}
