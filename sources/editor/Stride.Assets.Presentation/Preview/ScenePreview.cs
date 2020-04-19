// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Assets.Entities;
using Stride.Assets.Presentation.Preview.Views;
using Stride.Editor.Preview;

namespace Stride.Assets.Presentation.Preview
{
    [AssetPreview(typeof(SceneAsset), typeof(ScenePreviewView))]
    public class ScenePreview : AssetPreview<SceneAsset>
    {
    }
}
