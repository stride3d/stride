// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Quantum
{
    /// <summary>
    /// A class defining how an <see cref="AssetPropertyGraph"/> should behave for a given <see cref="Asset"/> type.
    /// </summary>
    [AssetPropertyGraphDefinition(typeof(Asset))]
    // ReSharper disable once RequiredBaseTypesIsNotInherited - due to a limitation on how ReSharper checks this requirement (see https://youtrack.jetbrains.com/issue/RSRP-462598)
    public class AssetPropertyGraphDefinition
    {
        public bool IsObjectReference(NodeAccessor nodeAccessor, object value)
        {
            if (nodeAccessor.IsMember)
                return IsMemberTargetObjectReference((IMemberNode)nodeAccessor.Node, value);
            if (nodeAccessor.IsItem)
                return IsTargetItemObjectReference((IObjectNode)nodeAccessor.Node, nodeAccessor.Index, value);

            return false;
        }

        public virtual bool IsMemberTargetObjectReference(IMemberNode member, object value)
        {
            return false;
        }

        public virtual bool IsTargetItemObjectReference(IObjectNode collection, NodeIndex itemIndex, object value)
        {
            return false;
        }
    }
}