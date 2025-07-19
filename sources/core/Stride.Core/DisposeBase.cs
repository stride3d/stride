// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.ReferenceCounting;

namespace Stride.Core;

/// <summary>
///   Base class for a <see cref="IDisposable"/> interface implementation with reference-counting semantics.
/// </summary>
/// <remarks>
///   A class inheriting from <see cref="DisposeBase"/> is an implementation of <see cref="IDisposable"/>
///   where calls to <see cref="Dispose"/> decrements an internal reference count (following also <see cref="IReferencable"/>).
///   <para>
///     Only when that reference count reaches zero, the object should be disposed and the <see cref="Destroy"/> method
///     is invoked. That method can be overriden by derived classes to release dependent resources.
///   </para>
/// </remarks>
[DataContract]
public abstract class DisposeBase : IDisposable, IReferencable
{
    private int refCount = 1;

    /// <summary>
    ///   Gets a value indicating whether the object has been disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }


    /// <summary>
    ///   Decrements the reference count of this object, disposing, releasing, and freeing associated
    ///   resources when the count reaches zero.
    /// </summary>
    /// <seealso cref="Destroy"/>
    public void Dispose()
    {
        if (!IsDisposed)
        {
            this.ReleaseInternal();
        }
    }

    /// <summary>
    ///   Disposes the object's resources.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Override in a derived class to implement disposal logic specific to it.
    ///   </para>
    ///   <para>
    ///     This method is automatically called whenever a call to <see cref="Dispose"/> (or to <see cref="IReferencable.Release"/>)
    ///     has decreased the internal reference count to zero, meaning no other objects (hopefully) hold a reference to this one
    ///     and its resources can be safely released.
    ///   </para>
    /// </remarks>
    protected virtual void Destroy() { }


    /// <inheritdoc/>
    int IReferencable.ReferenceCount => refCount;

    /// <inheritdoc/>
    int IReferencable.AddReference()
    {
        OnAddReference();

        var newCounter = Interlocked.Increment(ref refCount);
        if (newCounter <= 1)
            throw new InvalidOperationException(FrameworkResources.AddReferenceError);

        return newCounter;
    }

    /// <inheritdoc/>
    int IReferencable.Release()
    {
        OnReleaseReference();

        var newCounter = Interlocked.Decrement(ref refCount);
        if (newCounter == 0)
        {
            Destroy();
            IsDisposed = true;
        }
        else if (newCounter < 0)
        {
            throw new InvalidOperationException(FrameworkResources.ReleaseReferenceError);
        }
        return newCounter;
    }

    /// <summary>
    ///   Called when a new reference of this object has been counted (via a call to <see cref="IReferencable.AddReference"/>).
    /// </summary>
    protected virtual void OnAddReference() { }

    /// <summary>
    ///   Called when a call to <see cref="IReferencable.Release"/> has decremented the reference count of this object.
    /// </summary>
    protected virtual void OnReleaseReference() { }
}
