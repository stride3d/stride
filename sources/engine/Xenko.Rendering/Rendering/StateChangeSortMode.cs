// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;

namespace Xenko.Rendering
{
    /// <summary>
    /// Sort elements according to the pattern: [RenderFeature Sort Key 8 bits] RenderObject states 32 bits] [Distance front to back 16 bits]
    /// </summary>
    [DataContract("SortModeStateChange")]
    public class StateChangeSortMode : SortModeDistance
    {
        public StateChangeSortMode() : base(false)
        {
            statePosition = 32;
            distancePosition = 0;
        }
    }
}
