// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xenko.Core;
using Xenko.Assets.Models;

namespace Xenko.Assets.Presentation.NodePresenters.Keys
{
    public static class ModelNodeLinkData
    {
        public const string AvailableNodes = nameof(AvailableNodes);
        public static readonly PropertyKey<IEnumerable<NodeInformation>> Key = new PropertyKey<IEnumerable<NodeInformation>>(AvailableNodes, typeof(ModelNodeLinkData));
    }
}
