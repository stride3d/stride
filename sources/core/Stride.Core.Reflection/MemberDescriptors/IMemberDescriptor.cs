// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Stride.Core.Reflection
{
    public delegate bool ShouldSerializePredicate(object value, IMemberDescriptor parentTypeMemberDescriptor);

    /// <summary>
    /// Describe a member of an object.
    /// </summary>
    public interface IMemberDescriptor
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string OriginalName { get; }

        /// <summary>
        /// Gets the default name comparer.
        /// </summary>
        /// <value>The default name comparer.</value>
        StringComparer DefaultNameComparer { get; }

        /// <summary>
        /// Gets the type of the member.
        /// </summary>
        /// <value>The type.</value>
        Type Type { get; }

        /// <summary>
        /// Gets the type that is declaring this member.
        /// </summary>
        /// <value>The type that is declaring this member.</value>
        Type DeclaringType { get; }

        /// <summary>
        /// Gets the type descriptor of the member.
        /// </summary>
        ITypeDescriptor TypeDescriptor { get; }

        /// <summary>
        /// Gets the order of this member.
        /// Default is -1, meaning that it is using the alphabetical order
        /// based on the name of this property.
        /// </summary>
        /// <value>The order.</value>
        int? Order { get; }

        /// <summary>
        /// Gets the mode of serialization for this member.
        /// </summary>
        /// <value>The mode.</value>
        DataMemberMode Mode { get; }

        /// <summary>
        /// Gets a value indicating whether this member is public.
        /// </summary>
        bool IsPublic { get; }

        /// <summary>
        /// Gets a value indicating whether this instance has set method.
        /// </summary>
        bool HasSet { get; }

        IEnumerable<T> GetCustomAttributes<T>(bool inherit) where T : Attribute;

        /// <summary>
        /// Gets a value indicating whether this member should be serialized.
        /// </summary>
        ShouldSerializePredicate ShouldSerialize { get; }

        bool HasDefaultValue { get; }
        object DefaultValue { get; }

        /// <summary>
        /// Gets the alternative names that will map back to this member (may be null).
        /// </summary>
        List<string> AlternativeNames { get; }

        /// <summary>
        /// Gets or sets a custom tag to associate with this object.
        /// </summary>
        object Tag { get; set; }

        /// <summary>
        /// Gets the serialization mask, that will be checked against the context to know if this field needs to be serialized.
        /// </summary>
        uint Mask { get; set; }

        /// <summary>
        /// Gets the default style attached to this member.
        /// </summary>
        DataStyle Style { get; set; }

        /// <summary>
        /// Gets the default scalar style attached to this member.
        /// </summary>
        ScalarStyle ScalarStyle { get; set; }

        /// <summary>
        /// Gets the member information.
        /// </summary>
        /// <value>The member information.</value>
        MemberInfo MemberInfo { get; }

        /// <summary>
        /// Gets the value of this member for the specified instance.
        /// </summary>
        /// <param name="thisObject">The this object to get the value from.</param>
        /// <returns>Value of the member.</returns>
        object Get(object thisObject);

        /// <summary>
        /// Sets a value of this member for the specified instance.
        /// </summary>
        /// <param name="thisObject">The this object.</param>
        /// <param name="value">The value.</param>
        void Set(object thisObject, object value);
    }
}
