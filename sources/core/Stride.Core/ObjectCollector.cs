// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Stride.Core;

/// <summary>
///   A struct to collect objects implementing the <see cref="IDisposable"/>, or <see cref="IReferencable"/> interfaces, or
///   pointers to memory allocated with <see cref="MemoryUtilities.Allocate"/>, so they can be disposed in bulk at a later time.
/// </summary>
[DebuggerDisplay("Instances: {Count}")]
public struct ObjectCollector : IDisposable
{
    private List<object> disposables;

    /// <summary>
    ///   Gets the number of elements collected to be disposed.
    /// </summary>
    public readonly int Count => disposables?.Count ?? 0;

    /// <summary>
    ///   Disposes all the objects collected by this collector and clears the list. The collector can still be used for collecting.
    /// </summary>
    public readonly void Dispose()
    {
        if (disposables is null)
            return;

        for (var i = disposables.Count - 1; i >= 0; i--)
        {
            var objectToDispose = disposables[i];
            DisposeObject(objectToDispose);
            disposables.RemoveAt(i);
        }
        disposables.Clear();
    }

    /// <summary>
    ///   Ensures this collector is ready to be used for collecting object instances.
    /// </summary>
    public void EnsureValid()
    {
        disposables ??= [];
    }

    /// <summary>
    ///   Adds an object implementing the <see cref="IDisposable"/> or <see cref="IReferencable"/> interfaces,
    ///   or a <see cref="IntPtr"/> to an object allocated using <see cref="MemoryUtilities.Allocate"/>
    ///   to the list of the objects to dispose.
    /// </summary>
    /// <typeparam name="T">The type of the object to add.</typeparam>
    /// <param name="objectToDispose">The object to add to the collector to be disposed at a later time.</param>
    /// <exception cref="ArgumentException">
    ///   <paramref name="objectToDispose"/> does not implement the interface <see cref="IDisposable"/>, <see cref="IReferencable"/>,
    ///   and is not a valid memory pointer allocated by <see cref="MemoryUtilities.Allocate"/>.
    /// </exception>
    public T Add<T>(T objectToDispose) where T : notnull
    {
        if (objectToDispose is not (IDisposable or IReferencable or IntPtr))
            throw new ArgumentException("The object must be IDisposable, IReferenceable, or IntPtr", nameof(objectToDispose));

        // Check memory alignment
        if (objectToDispose is IntPtr memoryPtr && !MemoryUtilities.IsAligned(memoryPtr))
            throw new ArgumentException("The memory pointer is invalid. Memory must have been allocated with MemoryUtilities.Allocate", nameof(objectToDispose));

        EnsureValid();

        if (!disposables.Contains(objectToDispose))
            disposables.Add(objectToDispose);

        return objectToDispose;
    }

    /// <summary>
    ///   Removes a disposable object from the list of the objects to dispose.
    /// </summary>
    /// <typeparam name="T">The type of the object to remove.</typeparam>
    /// <param name="objectToDispose">The object to be removed from the list of objects to dispose.</param>
    public readonly void Remove<T>(T objectToDispose) where T : notnull
    {
        disposables?.Remove(objectToDispose);
    }

    /// <summary>
    ///   Removes an object from this collector and disposes it immediately, setting the reference to <see langword="null"/>.
    /// </summary>
    /// <param name="objectToDispose">The object to remove and dispose.</param>
    public readonly void RemoveAndDispose<T>([MaybeNull] ref T objectToDispose) where T : notnull
    {
        if (disposables is not null)
        {
            Remove(objectToDispose);
            DisposeObject(objectToDispose);
            objectToDispose = default;
        }
    }

    /// <summary>
    ///   Disposes an object that implements <see cref="IReferencable"/>, or <see cref="IDisposable"/>, or
    ///   a pointer to allocated memory.
    /// </summary>
    private static void DisposeObject(object objectToDispose)
    {
        switch (objectToDispose)
        {
            case null:
                return;

            case IReferencable referenceableObject:
                referenceableObject.Release();
                break;

            case IDisposable disposableObject:
                disposableObject.Dispose();
                break;

            default:
                var dataPointer = (nint) objectToDispose;
                MemoryUtilities.Free(dataPointer);
                break;
        }
    }
}
