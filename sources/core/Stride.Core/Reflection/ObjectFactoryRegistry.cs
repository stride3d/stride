// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Annotations;

namespace Stride.Core.Reflection;

/// <summary>
/// A registry of <see cref="IObjectFactory"/> used to instantiate instances of types used at design-time.
/// </summary>
public static class ObjectFactoryRegistry
{
    private static readonly Dictionary<Type, IObjectFactory?> RegisteredFactories = [];

    static ObjectFactoryRegistry()
    {
        // Register factory for string
        // TODO: We should remove that as soon as we can register attributes in registry for "string" type
        RegisteredFactories.Add(typeof(string), new StringObjectFactory());
    }

    /// <summary>
    /// Gets the factory corresponding to the given object type, if available.
    /// </summary>
    /// <param name="objectType">The object type for which to retrieve the factory.</param>
    /// <returns>The factory corresponding to the given object type if available, <c>null</c> otherwise.</returns>
    /// <exception cref="ArgumentNullException">objectType</exception>
    public static IObjectFactory? GetFactory(Type objectType)
    {
        ArgumentNullException.ThrowIfNull(objectType);
        lock (RegisteredFactories)
        {
            RegisteredFactories.TryGetValue(objectType, out var factory);
            return factory;
        }
    }

    /// <summary>
    /// Creates a default instance for an object type.
    /// </summary>
    /// <typeparam name="T">Type of the object to create</typeparam>
    /// <returns>A new instance of T</returns>
    public static T NewInstance<T>()
    {
        return (T)NewInstance(typeof(T));
    }

    /// <summary>
    /// Returns true if the object of the specific type can be created.
    /// </summary>
    /// <param name="objectType">Type of the object.</param>
    /// <returns>True if it can be created, false otherwise.</returns>
    public static bool CanCreateInstance(Type objectType)
    {
        var factory = FindFactory(objectType);
        if (factory != null)
            return true;

        // No factory, check if there is a parameterless ctor for Activator.CreateInstance
        return objectType.GetConstructor(Type.EmptyTypes) != null;
    }

    /// <summary>
    /// Creates a default instance for an object type.
    /// </summary>
    /// <param name="objectType">Type of the object.</param>
    /// <returns>A new default instance of an object.</returns>
    public static object NewInstance(Type objectType)
    {
        var factory = FindFactory(objectType);

        // If no registered factory, creates directly the asset
        return factory != null ? factory.New(objectType) : Activator.CreateInstance(objectType)!;
    }

    private static IObjectFactory? FindFactory(Type objectType)
    {
        ArgumentNullException.ThrowIfNull(objectType);

        lock (RegisteredFactories)
        {
            if (!RegisteredFactories.TryGetValue(objectType, out var factory))
            {
                factory = RegisterFactory(objectType);
            }
            return factory;
        }
    }

    private static IObjectFactory? RegisterFactory(Type objectType)
    {
        ArgumentNullException.ThrowIfNull(objectType);

        lock (RegisteredFactories)
        {
            IObjectFactory? factory = null;
            var factoryAttribute = objectType.GetTypeInfo().GetCustomAttribute<ObjectFactoryAttribute>();
            if (factoryAttribute != null)
            {
                factory = (IObjectFactory?)Activator.CreateInstance(factoryAttribute.FactoryType);
            }

            RegisteredFactories[objectType] = factory;

            return factory;
        }
    }

    private class StringObjectFactory : IObjectFactory
    {
        public object New(Type type) => string.Empty;
    }
}
