// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Reflection;
using Stride.Core.Annotations;

namespace Stride.Core
{
    public enum ExpandRule
    {
        /// <summary>
        /// The control representing the associated object will use the default rule.
        /// </summary>
        Auto,

        /// <summary>
        /// The control representing the associated object will be expanded only the first time it is displayed.
        /// </summary>
        Once,

        /// <summary>
        /// The control representing the associated object will be collapsed.
        /// </summary>
        Never,

        /// <summary>
        /// The control representing the associated object will be expanded.
        /// </summary>
        Always,
    }
    
    /// <summary>
    /// Portable DisplayAttribute equivalent to <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute"/>.
    /// </summary>
    public class DisplayAttribute : Attribute
    {
        private static readonly Dictionary<MemberInfo, DisplayAttribute> RegisteredDisplayAttributes = new Dictionary<MemberInfo, DisplayAttribute>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DisplayAttribute"/> class.
        /// </summary>
        /// <param name="order">The order weight of the column.</param>
        /// <param name="name">A value that is used for display in the UI..</param>
        /// <param name="category">A value that is used to group fields in the UI..</param>
        public DisplayAttribute(int order, string name = null, string category = null)
            : this(name, category)
        {
            Order = order;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DisplayAttribute"/> class.
        /// </summary>
        /// <param name="name">A value that is used for display in the UI..</param>
        /// <param name="category">A value that is used to group fields in the UI..</param>
        public DisplayAttribute(string name = null, string category = null)
        {
            Name = name;
            Category = category;
        }

        /// <summary>
        /// Gets the order weight of the column.
        /// </summary>
        /// <value>The order.</value>
        public int? Order { get; }

        /// <summary>
        /// Gets a string that is used for display in the UI.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; }

        /// <summary>
        /// Gets a string that is used to group fields in the UI.
        /// </summary>
        /// <value>The category.</value>
        public string Category { get; }

        /// <summary>
        /// Gets the hue of a color that is used in the UI.
        /// </summary>
        /// <remarks>If not null, this value must be in the range [0, 360].</remarks>
        public float? CustomHue { get; set; }

        /// <summary>
        /// Gets or sets whether to expand the control representing the associated object in the UI.
        /// </summary>
        public ExpandRule Expand { get; set; }

        /// <summary>
        /// Gets or sets whether the related member is browsable when its class is exposed in the UI. 
        /// </summary>
        public bool Browsable { get; set; } = true;

        /// <summary>
        /// Gets the display attribute attached to the specified member info.
        /// </summary>
        /// <param name="memberInfo">Member type (Property, Field or Type).</param>
        /// <returns>DisplayAttribute.</returns>
        /// <exception cref="System.ArgumentNullException">memberInfo</exception>
        [Obsolete("Display attribute should be retrieved via an AttributeRegistry.")]
        public static DisplayAttribute GetDisplay([NotNull] MemberInfo memberInfo)
        {
            if (memberInfo == null) throw new ArgumentNullException(nameof(memberInfo));
            lock (RegisteredDisplayAttributes)
            {
                DisplayAttribute value;
                if (!RegisteredDisplayAttributes.TryGetValue(memberInfo, out value))
                {
                    value = memberInfo.GetCustomAttribute<DisplayAttribute>() ?? new DisplayAttribute(memberInfo.Name);
                    RegisteredDisplayAttributes.Add(memberInfo, value);
                }
                return value;
            }
        }

        /// <summary>
        /// Gets the display name of the given type. The display name is the name of the type, or, if the <see cref="DisplayAttribute"/> is
        /// applied on the type, value of the <see cref="DisplayAttribute.Name"/> property.
        /// </summary>
        /// <param name="type">The type for which to get the display name.</param>
        /// <returns>A string representing the display name of the type.</returns>
        [Obsolete("Display attribute should be retrieved via an AttributeRegistry.")]
        public static string GetDisplayName(Type type)
        {
            if (type == null)
                return null;

            return GetDisplay(type.GetTypeInfo())?.Name ?? type.Name;
        }

        [Obsolete("Display attribute should be retrieved via an AttributeRegistry.")]
        public static int? GetOrder([NotNull] MemberInfo memberInfo)
        {
            var display = GetDisplay(memberInfo);
            return display.Order;
        }
    }
}
