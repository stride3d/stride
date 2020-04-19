// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Stride.Core.Reflection
{
    /// <summary>
    /// Base class for <see cref="IMemberDescriptor"/> for a <see cref="MemberInfo"/>
    /// </summary>
    public abstract class MemberDescriptorBase : IMemberDescriptor
    {
        protected MemberDescriptorBase(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            Name = name;
            OriginalName = name;
        }

        protected MemberDescriptorBase(MemberInfo memberInfo, StringComparer defaultNameComparer)
        {
            if (memberInfo == null) throw new ArgumentNullException(nameof(memberInfo));

            MemberInfo = memberInfo;
            Name = MemberInfo.Name;
            OriginalName = Name;
            DeclaringType = memberInfo.DeclaringType;
            DefaultNameComparer = defaultNameComparer;
        }

        // TODO: turn the public setters internal or protected

        public string Name { get; set; }
        public string OriginalName { get; }
        public StringComparer DefaultNameComparer { get; }
        public abstract Type Type { get; }
        public int? Order { get; set; }

        /// <summary>
        /// Gets the type of the declaring this member.
        /// </summary>
        /// <value>The type of the declaring.</value>
        public Type DeclaringType { get; }

        public ITypeDescriptor TypeDescriptor { get; protected set; }

        public DataMemberMode Mode { get; set; }
        public abstract object Get(object thisObject);
        public abstract void Set(object thisObject, object value);
        /// <summary>
        /// Gets whether this member has a public getter.
        /// </summary>
        public abstract bool IsPublic { get; }
        public abstract bool HasSet { get; }
        public abstract IEnumerable<T> GetCustomAttributes<T>(bool inherit) where T : Attribute;

        /// <summary>
        /// Gets the serialization mask, that will be checked against the context to know if this field needs to be serialized.
        /// </summary>
        public uint Mask { get; set; }

        /// <summary>
        /// Gets the default style attached to this member.
        /// </summary>
        public DataStyle Style { get; set; }

        /// <summary>
        /// Gets the default style attached to this member.
        /// </summary>
        public ScalarStyle ScalarStyle { get; set; }

        /// <summary>
        /// Gets the member information.
        /// </summary>
        /// <value>The member information.</value>
        public MemberInfo MemberInfo { get; }

        public ShouldSerializePredicate ShouldSerialize { get; set; }

        public System.ComponentModel.DefaultValueAttribute DefaultValueAttribute { get; set; }
        public bool HasDefaultValue => DefaultValueAttribute != null;
        public object DefaultValue => DefaultValueAttribute.Value;

        public List<string> AlternativeNames { get; set; }

        public object Tag { get; set; }
    }
}
