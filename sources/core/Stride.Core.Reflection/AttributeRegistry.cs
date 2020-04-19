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
    /// A default implementation for <see cref="IAttributeRegistry"/>. 
    /// This implementation allows to retrieve default attributes for a member or 
    /// to attach an attribute to a specific type/member.
    /// </summary>
    public class AttributeRegistry : IAttributeRegistry
    {
        private readonly object lockObject = new object();
        private readonly Dictionary<MemberInfoKey, List<Attribute>> cachedAttributes = new Dictionary<MemberInfoKey, List<Attribute>>();
        private readonly Dictionary<MemberInfo, List<Attribute>> registeredAttributes = new Dictionary<MemberInfo, List<Attribute>>();

        // TODO: move this in a different location
        public Action<ObjectDescriptor, List<IMemberDescriptor>> PrepareMembersCallback { get; set; }

        /// <summary>
        /// Gets the attributes associated with the specified member.
        /// </summary>
        /// <param name="memberInfo">The reflection member.</param>
        /// <param name="inherit">if set to <c>true</c> includes inherited attributes.</param>
        /// <returns>An enumeration of <see cref="Attribute"/>.</returns>
        public virtual List<Attribute> GetAttributes(MemberInfo memberInfo, bool inherit = true)
        {
            var key = new MemberInfoKey(memberInfo, inherit);

            // Use a cache of attributes
            List<Attribute> attributes;
            lock (lockObject)
            {
                if (cachedAttributes.TryGetValue(key, out attributes))
                {
                    return attributes;
                }

                // Else retrieve all default attributes
                var defaultAttributes = Attribute.GetCustomAttributes(memberInfo, inherit);
                IEnumerable<Attribute> attributesToCache = defaultAttributes;

                // And add registered attributes
                List<Attribute> registered;
                if (registeredAttributes.TryGetValue(memberInfo, out registered))
                {
                    // Remove "real" attributes overridden by manually registered attributes
                    attributesToCache = registered.Concat(defaultAttributes.Where(x => GetUsage(x).AllowMultiple || registered.All(y => y.GetType() != x.GetType())));
                }

                attributes = attributesToCache.ToList();

                // Add to the cache
                cachedAttributes.Add(key, attributes);
            }

            return attributes;
        }

        /// <summary>
        /// Registers an attribute for the specified member. Restriction: Attributes registered this way cannot be listed in inherited attributes.
        /// </summary>
        /// <param name="memberInfo">The member information.</param>
        /// <param name="attribute">The attribute.</param>
        public void Register(MemberInfo memberInfo, Attribute attribute)
        {
            lock (lockObject)
            {
                List<Attribute> attributes;
                if (!registeredAttributes.TryGetValue(memberInfo, out attributes))
                {
                    attributes = new List<Attribute>();
                    registeredAttributes.Add(memberInfo, attributes);
                }
                // Insert it in the first position to ensure it will override same attributes from base classes when using First
                attributes.Insert(0, attribute);

                cachedAttributes.Remove(new MemberInfoKey(memberInfo, true));
                cachedAttributes.Remove(new MemberInfoKey(memberInfo, false));
            }
        }

        private static AttributeUsageAttribute GetUsage(Attribute attribute)
        {
            return Attribute.GetCustomAttribute(attribute.GetType(), typeof(AttributeUsageAttribute)) as AttributeUsageAttribute;
        }

        private struct MemberInfoKey : IEquatable<MemberInfoKey>
        {
            private readonly MemberInfo memberInfo;

            private readonly bool inherit;

            public MemberInfoKey([NotNull] MemberInfo memberInfo, bool inherit)
            {
                if (memberInfo == null) throw new ArgumentNullException(nameof(memberInfo));
                this.memberInfo = memberInfo;
                this.inherit = inherit;
            }

            public bool Equals(MemberInfoKey other)
            {
                return memberInfo.Equals(other.memberInfo) && inherit.Equals(other.inherit);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is MemberInfoKey && Equals((MemberInfoKey) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (memberInfo.GetHashCode()*397) ^ inherit.GetHashCode();
                }
            }
        }
    }
}
