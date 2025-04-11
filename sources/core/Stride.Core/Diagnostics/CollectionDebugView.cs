// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#pragma warning disable SA1402 // File may only contain a single class

using System.Collections;
using System.Diagnostics;

namespace Stride.Core.Diagnostics;

/// <summary>
/// Use this class to provide a debug output in Visual Studio debugger.
/// </summary>
public class CollectionDebugView
{
    private readonly IEnumerable collection;

    public CollectionDebugView(
        IEnumerable collection)
    {
#if NET7_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(collection);
#else
        if (collection is null) throw new ArgumentNullException(nameof(collection));
#endif // NET7_0_OR_GREATER
        this.collection = collection;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public object[] Items => collection.Cast<object>().ToArray();
}

/// <summary>
/// Use this class to provide a debug output in Visual Studio debugger.
/// </summary>
public class CollectionDebugView<T>
{
    private readonly ICollection<T> collection;

    public CollectionDebugView(ICollection<T> collection)
    {
#if NET7_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(collection);
#else
        if (collection is null) throw new ArgumentNullException(nameof(collection));
#endif // NET7_0_OR_GREATER
        this.collection = collection;
    }

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
