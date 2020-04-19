// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.UI
{
    /// <summary>
    /// Describes how scaling applies to content and restricts scaling to named axis types.
    /// </summary>
    public enum StretchDirection
    {
        /// <summary>
        /// The content stretches to fit the parent according to the Stretch mode.
        /// </summary>
        /// <userdoc>The content stretches to fit the parent according to the Stretch mode.</userdoc>
        Both,
        /// <summary>
        /// The content scales downward only when it is larger than the parent. If the content is smaller, no scaling upward is performed.
        /// </summary>
        /// <userdoc>The content scales downward only when it is larger than the parent. If the content is smaller, no scaling upward is performed.</userdoc>
        DownOnly,
        /// <summary>
        /// The content scales upward only when it is smaller than the parent. If the content is larger, no scaling downward is performed.
        /// </summary>
        /// <userdoc>The content scales upward only when it is smaller than the parent. If the content is larger, no scaling downward is performed.</userdoc>
        UpOnly,
    }
}
