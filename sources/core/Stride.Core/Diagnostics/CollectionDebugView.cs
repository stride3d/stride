// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single class
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xenko.Core.Annotations;

namespace Xenko.Core.Diagnostics
{
    /// <summary>
    /// Use this class to provide a debug output in Visual Studio debugger.
    /// </summary>
    public class CollectionDebugView
    {
        private readonly IEnumerable collection;

        public CollectionDebugView([NotNull] IEnumerable collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            this.collection = collection;
        }

        [NotNull]
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public object[] Items => collection.Cast<object>().ToArray();
    }

    /// <summary>
    /// Use this class to provide a debug output in Visual Studio debugger.
    /// </summary>
    public class CollectionDebugView<T>
    {
        private readonly ICollection<T> collection;

        public CollectionDebugView([NotNull] ICollection<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            this.collection = collection;
        }

        [NotNull]
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                var array = new T[collection.Count];
                collection.CopyTo(array, 0);
                return array;
            }
        }
    }
}
