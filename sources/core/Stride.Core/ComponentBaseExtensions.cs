// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core;

/// <summary>
/// Extensions for <see cref="IComponent"/>.
/// </summary>
public static class ComponentBaseExtensions
{
    /// <summary>
    ///   Keeps a disposable object alive by adding it to a container.
    /// </summary>
    /// <typeparam name="T">The type of the Component.</typeparam>
    /// <param name="component">The Component to keep alive.</param>
    /// <param name="container">The container that will keep a reference to the Component.</param>
    /// <returns>The same Component instance.</returns>
    public static T? DisposeBy<T>(this T component, ICollectorHolder container)
        where T : IDisposable
    {
        if (component is null)
            return default;

        return container.Collector.Add(component);
    }

    /// <summary>
    ///   Removes a disposable object from a container that keeping it alive.
    /// </summary>
    /// <typeparam name="T">The type of the Component.</typeparam>
    /// <param name="component">The Component to remove.</param>
    /// <param name="container">The container that kept a reference to the Component.</param>
    public static void RemoveDisposeBy<T>(this T component, ICollectorHolder container)
        where T : IDisposable
    {
        if (component is null)
            return;

        container.Collector.Remove(component);
    }

    /// <summary>
    ///   Keeps a referenceable object alive by adding it to a container.
    /// </summary>
    /// <typeparam name="T">The type of the Component.</typeparam>
    /// <param name="component">The Component to keep alive.</param>
    /// <param name="container">The container that will keep a reference to the Component.</param>
    /// <returns>The same Component instance.</returns>
    public static T? ReleaseBy<T>(this T component, ICollectorHolder container)
        where T : IReferencable
    {
        if (component is null)
            return default;

        return container.Collector.Add(component);
    }

    /// <summary>
    ///   Removes a referenceable object from a container that keeping it alive.
    /// </summary>
    /// <typeparam name="T">The type of the Component.</typeparam>
    /// <param name="component">The Component to remove.</param>
    /// <param name="container">The container that kept a reference to the Component.</param>
    public static void RemoveReleaseBy<T>(this T component, ICollectorHolder container)
        where T : IReferencable
    {
        if (component is null)
            return;

        container.Collector.Remove(component);
    }

    /// <summary>
    ///   Pins this component as a new reference.
    /// </summary>
    /// <typeparam name="T">The type of the Component.</typeparam>
    /// <param name="component">The Component to add a reference to.</param>
    /// <returns>The same Component instance.</returns>
    /// <remarks>
    ///   This method is equivalent to calling <see cref="IReferencable.AddReference"/> and returning the
    ///   same <paramref name="component"/> instance.
    /// </remarks>
    public static T? KeepReference<T>(this T component)
        where T : IReferencable
    {
        if (component is null)
            return default;

        component.AddReference();
        return component;
    }

    /// <summary>
    ///   Sets a tag in a Component and removes it after using it.
    /// </summary>
    /// <typeparam name="T">The type of the tag.</typeparam>
    /// <param name="component">The Component to set a tag for.</param>
    /// <param name="key">The key to identify the tag to set.</param>
    /// <param name="value">The value of the tag.</param>
    /// <returns>
    ///   A <see cref="PropertyTagRestore{T}"/> structure that can be used with a <see langword="using"/> clause (or
    ///   manually disposed) to set a tag, perform some action, and remove the tag once finished.
    /// </returns>
    /// <remarks>
    ///   <para>
    ///     This method is used to set a property value in <see cref="ComponentBase.Tags"/>, perform some actions, and restore
    ///     the previous value after finishing.
    ///   </para>
    ///   <para>
    ///     The returned object must be disposed once the original value must be restored. It can be done manually
    ///     (by calling <see cref="PropertyTagRestore{T}.Dispose"/>) or automatically with a <see langword="using"/> clause.
    ///   </para>
    /// </remarks>
    public static PropertyTagRestore<T> PushTagAndRestore<T>(this ComponentBase component, PropertyKey<T?> key, T value)
    {
        // TODO: Not fully satisfied with the name and the extension point (on ComponentBase). We need to review this a bit more
        var restorer = new PropertyTagRestore<T>(component, key);
        component.Tags.Set(key, value);
        return restorer;
    }

    /// <summary>
    ///   A structure returned by <see cref="PushTagAndRestore{T}(ComponentBase, PropertyKey{T}, T)"/> that saves the value of a
    ///   tag property of an object and, once finished, can restore its value to the previous one.
    /// </summary>
    /// <typeparam name="T">The type of the tag.</typeparam>
    public readonly struct PropertyTagRestore<T> : IDisposable
    {
        private readonly ComponentBase container;

        private readonly PropertyKey<T?> key;

        private readonly T? previousValue;


        /// <summary>
        ///   Initializes a new instance of the <see cref="PropertyTagRestore{T}"/> structure.
        /// </summary>
        /// <param name="container">The Component that contains the tag to set and restore.</param>
        /// <param name="key">The key that identifies the tag to set and restore.</param>
        /// <exception cref="ArgumentNullException"><paramref name="container"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        public PropertyTagRestore(ComponentBase container, PropertyKey<T?> key)
            : this()
        {
            ArgumentNullException.ThrowIfNull(container);
            ArgumentNullException.ThrowIfNull(key);

            this.container = container;
            this.key = key;
            previousValue = container.Tags.Get(key);
        }

        /// <inheritdoc/>
        public readonly void Dispose()
        {
            // Restore the value
            container.Tags.Set(key, previousValue);
        }
    }
}
