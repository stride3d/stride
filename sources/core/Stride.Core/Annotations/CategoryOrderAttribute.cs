// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Annotations
{
    /// <summary>
    /// This attribute allows to associate an <see cref="Order"/> value to a category name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
    public sealed class CategoryOrderAttribute : Attribute
    {
        public CategoryOrderAttribute(int order, string name)
        {
            Order = order;
            Name = name;
        }

        /// <summary>
        /// Gets or sets the name of the category.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the order value of the category.
        /// </summary>
        public int Order { get; }

        /// <summary>
        /// Gets or sets whether to expand the category in the UI.
        /// </summary>
        public ExpandRule Expand { get; set; } = ExpandRule.Always;
    }
}
