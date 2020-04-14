// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stride.Core.Annotations;

namespace Stride.Core.Reflection
{
    /// <summary>
    /// A registry for all attributes.
    /// </summary>
    public interface IAttributeRegistry
    {
        // TODO: move this in a different location
        Action<ObjectDescriptor, List<IMemberDescriptor>> PrepareMembersCallback { get; set; }

        /// <summary>
        /// Gets the attributes associated with the specified member.
        /// </summary>
        /// <param name="memberInfo">The reflection member.</param>
        /// <param name="inherit">if set to <c>true</c> includes inherited attributes.</param>
        /// <returns>An enumeration of <see cref="Attribute"/>.</returns>
        List<Attribute> GetAttributes([NotNull] MemberInfo memberInfo, bool inherit = true);

        /// <summary>
        /// Registers an attribute for the specified member. Restriction: Attributes registered this way cannot be listed in inherited attributes.
        /// </summary>
        /// <param name="memberInfo">The member information.</param>
        /// <param name="attribute">The attribute.</param>
        void Register(MemberInfo memberInfo, Attribute attribute);
    }

    /// <summary>
    /// Extension methods for attribute registry.
    /// </summary>
    public static class AttributeRegistryExtensions
    {
        /// <summary>
        /// Gets the attributes associated with the specified member.
        /// </summary>
        /// <typeparam name="T">Type of the attribute</typeparam>
        /// <param name="attributeRegistry">The attribute registry.</param>
        /// <param name="memberInfo">The member information.</param>
        /// <param name="inherit">if set to <c>true</c> [inherit].</param>
        /// <returns>An enumeration of <see cref="Attribute" />.</returns>
        public static IEnumerable<T> GetAttributes<T>(this IAttributeRegistry attributeRegistry, MemberInfo memberInfo, bool inherit = true) where T : Attribute
        {
            return attributeRegistry.GetAttributes(memberInfo, inherit).OfType<T>();
        }

        /// <summary>
        /// Gets an attribute associated with the specified member.
        /// </summary>
        /// <typeparam name="T">Type of the attribute</typeparam>
        /// <param name="attributeRegistry">The attribute registry.</param>
        /// <param name="memberInfo">The member information.</param>
        /// <param name="inherit">if set to <c>true</c> [inherit].</param>
        /// <returns>An attribute of type {T} if it was found; otherwise <c>null</c></returns>
        public static T GetAttribute<T>(this IAttributeRegistry attributeRegistry, [NotNull] MemberInfo memberInfo, bool inherit = true) where T : Attribute
        {
            var list = attributeRegistry.GetAttributes(memberInfo, inherit);
            return list.OfType<T>().FirstOrDefault();
        }
    }
}
