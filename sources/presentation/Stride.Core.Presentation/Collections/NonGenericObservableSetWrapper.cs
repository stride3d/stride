// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Collections
{
    /// <summary>
    /// A class that wraps an instance of the <see cref="ObservableSet{T}"/> class and implement the <see cref="IList"/> interface.
    /// In some scenarii, <see cref="IList"/> does not support range changes on the collection (Especially when bound to a ListCollectionView).
    /// This is why the <see cref="ObservableSet{T}"/> class does not implement this interface directly. However this wrapper class can be used
    /// when the <see cref="IList"/> interface is required.
    /// </summary>
    /// <typeparam name="T">The type of item contained in the <see cref="ObservableSet{T}"/>.</typeparam>
    [Obsolete("This class is identical to NonGenericObservableListWrapper.")]
    public class NonGenericObservableSetWrapper<T> : NonGenericObservableCollectionWrapper<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NonGenericObservableSetWrapper{T}"/> class.
        /// </summary>
        /// <param name="list">The <see cref="ObservableSet{T}"/> to wrap.</param>
        public NonGenericObservableSetWrapper([NotNull] ObservableSet<T> list)
            : base(list)
        {
        }

        public void AddRange([NotNull] IEnumerable values)
        {
            ((ObservableSet<T>)List).AddRange(values.Cast<T>());
        }

        public void AddRange([NotNull] IEnumerable<T> values)
        {
            ((ObservableSet<T>)List).AddRange(values);
        }
    }
}
