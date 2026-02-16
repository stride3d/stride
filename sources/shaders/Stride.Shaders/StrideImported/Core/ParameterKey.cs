// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single type
using Stride.Core;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace Stride.Rendering
{
    /// <summary>
    /// Key of an effect parameter.
    /// </summary>
    public abstract class ParameterKey
    {
        protected string name;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterKey" /> class.
        /// </summary>
        /// <param name="propertyType">Type of the property.</param>
        /// <param name="name">The name.</param>
        /// <param name="length">The length.</param>
        /// <param name="metadatas">The metadatas.</param>
        protected ParameterKey(Type propertyType, string name, int length)
        {
            Length = length;
        }

        public string Name { get => name; init => name = value; }

        /// <summary>
        /// Gets the number of elements for this key.
        /// </summary>
        public int Length { get; private set; }

        public ParameterKeyType Type { get; protected set; }

        public abstract int Size { get; }

        internal void SetName(string nameParam)
        {
            if (nameParam == null) throw new ArgumentNullException(nameof(nameParam));

            name = string.Intern(nameParam);
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            //return ReferenceEquals(this, obj);
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            var against = obj as ParameterKey;
            if (against == null) return false;
            return (Equals(against.Name, Name));
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(ParameterKey left, ParameterKey right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(ParameterKey left, ParameterKey right)
        {
            return !Equals(left, right);
        }
    }

    public enum ParameterKeyType
    {
        Value,
        Object,
        Permutation,
    }

    /// <summary>
    /// Key of an gereric effect parameter.
    /// </summary>
    /// <typeparam name="T">Type of the parameter key.</typeparam>
    public abstract class ParameterKey<T> : ParameterKey
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterKey{T}"/> class.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name">The name.</param>
        /// <param name="length">The length.</param>
        /// <param name="metadatas">The metadatas.</param>
        protected ParameterKey(ParameterKeyType type, string name, int length = 1)
            : base(typeof(T), name, length)
        {
            Type = type;
        }

        public override int Size => Unsafe.SizeOf<T>();

        public override string ToString()
        {
            return string.Format("{0}", Name);
        }
    }
}
