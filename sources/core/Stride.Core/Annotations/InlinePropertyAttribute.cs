// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Core.Annotations
{
    /// <summary>
    /// This attribute indicates that the associated property should be inlined in its container presentation
    /// when displayed in a property grid.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class InlinePropertyAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets whether to expand the inline property in the UI. The default is <see cref="ExpandRule.Never"/>.
        /// </summary>
        public ExpandRule Expand { get; set; } = ExpandRule.Never;
    }
}
