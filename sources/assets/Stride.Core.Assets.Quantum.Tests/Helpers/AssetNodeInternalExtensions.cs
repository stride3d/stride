// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Quantum.Internal;
using Stride.Core.Reflection;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Quantum.Tests.Helpers
{
    public static class AssetNodeInternalExtensions
    {
        public static OverrideType GetItemOverride(this IAssetNode node, NodeIndex index)
        {
            return ((IAssetObjectNodeInternal)node).GetItemOverride(index);
        }

        public static OverrideType GetKeyOverride(this IAssetNode node, NodeIndex index)
        {
            return ((IAssetObjectNodeInternal)node).GetKeyOverride(index);
        }
    }
}
