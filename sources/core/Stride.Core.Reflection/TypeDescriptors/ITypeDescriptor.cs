// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Stride.Core.Reflection
{
    /// <summary>
    /// Provides access members of a type.
    /// </summary>
    public interface ITypeDescriptor
    {
        /// <summary>
        /// Gets the type described by this instance.
        /// </summary>
        /// <value>The type.</value>
        Type Type { get; }

        /// <summary>
        /// Gets the list of attributes attached to this type.
        /// </summary>
        /// <value>The list of attributes.</value>
        List<Attribute> Attributes { get; }

        /// <summary>
        /// Gets the members of this type.
        /// </summary>
        /// <value>The members.</value>
        IEnumerable<IMemberDescriptor> Members { get; }

        /// <summary>
        /// Gets the member count.
        /// </summary>
        /// <value>The member count.</value>
        int Count { get; }

        /// <summary>
        /// Gets the category of this descriptor.
        /// </summary>
        /// <value>The category.</value>
        DescriptorCategory Category { get; }

        /// <summary>
        /// Gets a value indicating whether this instance has members.
        /// </summary>
        /// <value><c>true</c> if this instance has members; otherwise, <c>false</c>.</value>
        bool HasMembers { get; }

        /// <summary>
        /// Gets the <see cref="IMemberDescriptor"/> with the specified name.
        /// </summary>
        /// <param name="name">The name of the member.</param>
        /// <returns>The member.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when a member </exception>
        IMemberDescriptor this[string name] { get; }

        /// <summary>
        /// Tries to get a member with the specified name. If nothing could be found, returns null.
        /// </summary>
        /// <param name="name">The name of the member.</param>
        /// <returns>The member if found, otherwise [null].</returns>
        IMemberDescriptor TryGetMember(string name);

        /// <summary>
        /// Gets a value indicating whether this instance is a compiler generated type.
        /// </summary>
        /// <value><c>true</c> if this instance is a compiler generated type; otherwise, <c>false</c>.</value>
        bool IsCompilerGenerated { get; }

        /// <summary>
        /// Determines whether the named member is remmaped.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns><c>true</c> if the named member is remmaped; otherwise, <c>false</c>.</returns>
        bool IsMemberRemapped(string name);

        /// <summary>
        /// Gets the style.
        /// </summary>
        /// <value>The style.</value>
        DataStyle Style { get; }

        /// <summary>
        /// Initializes the type descriptor.
        /// </summary>
        /// <param name="keyComparer"></param>
        void Initialize(IComparer<object> keyComparer);

        /// <summary>
        /// Determines whether this instance contains a member with the specified member name.
        /// </summary>
        /// <param name="memberName">Name of the member.</param>
        /// <returns><c>true</c> if this instance contains a member with the specified member name; otherwise, <c>false</c>.</returns>
        bool Contains(string memberName);
    }
}
