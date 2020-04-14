// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core
{
    /// <summary>
    /// Specify the way to store a property or field of some class or structure.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class DataMemberAttribute : Attribute
    {
        public const uint DefaultMask = 1;
        public const uint IgnoreMask = 0xF0000000;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataMemberAttribute"/> class.
        /// </summary>
        public DataMemberAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataMemberAttribute"/> class.
        /// </summary>
        /// <param name="order">The order.</param>
        public DataMemberAttribute(int order)
        {
            Order = order;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataMemberAttribute"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public DataMemberAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Specify the way to store a property or field of some class or structure.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="mode">The serialize method.</param>
        public DataMemberAttribute(string name, DataMemberMode mode)
        {
            Name = name;
            Mode = mode;
        }

        /// <summary>
        /// Specify the way to store a property or field of some class or structure.
        /// </summary>
        /// <param name="mode">The serialize method.</param>
        public DataMemberAttribute(DataMemberMode mode)
        {
            Mode = mode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataMemberAttribute"/> class.
        /// </summary>
        /// <param name="order">The order.</param>
        /// <param name="mode">The mode.</param>
        public DataMemberAttribute(int order, DataMemberMode mode)
        {
            Order = order;
            Mode = mode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataMemberAttribute"/> class.
        /// </summary>
        /// <param name="order">The order.</param>
        /// <param name="name">The name.</param>
        public DataMemberAttribute(int order, string name)
        {
            Order = order;
            Name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataMemberAttribute"/> class.
        /// </summary>
        /// <param name="order">The order.</param>
        /// <param name="name">The name.</param>
        /// <param name="mode">The mode.</param>
        public DataMemberAttribute(int order, string name, DataMemberMode mode)
        {
            Order = order;
            Name = name;
            Mode = mode;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; }

        /// <summary>
        /// Gets the serialize method1.
        /// </summary>
        /// <value>The serialize method1.</value>
        public DataMemberMode Mode { get; }

        /// <summary>
        /// Gets or sets the order. Default is -1 (default to alphabetical)
        /// </summary>
        /// <value>The order.</value>
        public int? Order { get; set; }

        /// <summary>
        /// Gets or sets the mask to filter out members.
        /// </summary>
        /// <value>The mask.</value>
        public uint Mask { get; set; } = DefaultMask;
    }
}
