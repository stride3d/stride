// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Assets.Models;

namespace Stride.Assets.Presentation.NodePresenters.Keys
{
    public static class ModelNodeLinkData
    {
        public const string AvailableNodes = nameof(AvailableNodes);
        public static readonly PropertyKey<IEnumerable<NodeInformation>> Key = new PropertyKey<IEnumerable<NodeInformation>>(AvailableNodes, typeof(ModelNodeLinkData));
    }
}
