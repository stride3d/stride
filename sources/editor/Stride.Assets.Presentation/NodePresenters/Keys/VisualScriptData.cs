// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Assets.Presentation.AssetEditors.VisualScriptEditor;

namespace Stride.Assets.Presentation.NodePresenters.Keys
{
    public static class VisualScriptData
    {
        public const string OwnerBlock = nameof(OwnerBlock);

        public static readonly PropertyKey<VisualScriptBlockViewModel> OwnerBlockKey = new PropertyKey<VisualScriptBlockViewModel>(OwnerBlock, typeof(VisualScriptData));
    }
}
