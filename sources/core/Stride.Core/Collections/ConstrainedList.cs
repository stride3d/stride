// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Serializers;

namespace Stride.Core.Collections
{
    /// <summary>
    /// Represent a collection associated with a constraint. When an item is added to this collection, it is tested against the constraint.
    /// If the test fails, the item can either be discarded, or an exception can be thrown. The desired behavior can be defined with <see cref="ThrowException"/>.
    /// </summary>
    [DataSerializer(typeof(ListAllSerializer<,>), Mode = DataSerializerGenericMode.TypeAndGenericArguments)]
    public class ConstrainedList<T> : IList<T>, IReadOnlyList<T>
    {
        private readonly List<T> innerList = new List<T>();

        private readonly string errorMessage;

        public ConstrainedList(Func<ConstrainedList<T>, T, bool> constraint = null, bool throwException = true, string errorMessage = null)
        {
            Constraint = constraint;
            ThrowException = throwException;
            this.errorMessage = errorMessage;
        }

        public ConstrainedList()
        {
            ThrowException = true;
        }

        /// <summary>
        /// Gets or sets whether the collection should throw an <see cref="ArgumentException"/> when an item to add or insert doesn't pass the constraint.
        /// </summary>
        [DataMemberIgnore]
        public bool ThrowException { get; set; }

        /// <summary>
        /// Gets or sets the constraint for items added to the collection. If <c>null</c>, this collection behaves like a <see cref="List{T}"/>.
        /// </summary>
        [DataMemberIgnore]
        public Func<ConstrainedList<T>, T, bool> Constraint { get; set; }

        public List<T>.Enumerator GetEnumerator()
        {
            return innerList.GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc/>
        public void Add(T item)
        {
            if (CheckConstraint(item))
                innerList.Add(item);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            innerList.Clear();
        }

        /// <inheritdoc/>
        public bool Contains(T item)
        {
            return innerList.Contains(item);
        }

        /// <inheritdoc/>
        public void CopyTo(T[] array, int arrayIndex)
        {
            innerList.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc/>
        public bool Remove(T item)
        {
            return innerList.Remove(item);
        }

        /// <inheritdoc/>
        public int Count => innerList.Count;

        /// <inheritdoc/>
        public bool IsReadOnly => false;

        /// <inheritdoc/>
        public int IndexOf(T item)
        {
            return innerList.IndexOf(item);
        }

        /// <inheritdoc/>
        public void Insert(int index, T item)
        {
            if (CheckConstraint(item))
                innerList.Insert(index, item);
        }

        /// <inheritdoc/>
        public void RemoveAt(int index)
        {
            innerList.RemoveAt(index);
        }

        /// <inheritdoc/>
        public T this[int index] { get { return innerList[index]; } set { if (CheckConstraint(value)) innerList[index] = value; } }

        private bool CheckConstraint(T item)
        {
            var result = true;
            if (Constraint != null)
            {
                result = Constraint(this, item);
                if (!result && ThrowException)
                    throw new ArgumentException(errorMessage ?? "The given item does not validate the collection constraint.");
            }

            return result;
        }
    }
}
