// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;

namespace Xenko.Rendering
{
    /// <summary>
    /// Sort elements according to the pattern: [RenderFeature Sort Key 8 bits] [Distance back to front 32 bits] [RenderObject states 24 bits]
    /// </summary>
    [DataContract("BackToFrontSortMode")]
    public class BackToFrontSortMode : SortModeDistance
    {
        public BackToFrontSortMode() : base(true)
        {
            distancePrecision = 32;
            distancePosition = 24;

            statePrecision = 24;
        }
    }
}
