// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xenko.Core.Assets;
using Xenko.Core.Serialization.Contents;

namespace Xenko.Core.Serialization
{
    /// <summary>
    /// Manage object reference information externally, not stored in the object but in a separate <see cref="AttachedReference"/> object.
    /// </summary>
    // TODO: this should be ideally moved back in Core.Design, but there are a few runtime usages to clean first
    public static class AttachedReferenceManager
    {
        private static readonly object[] EmptyObjectArray = new object[0];
        private static readonly Dictionary<Type, ConstructorInfo> EmptyCtorCache = new Dictionary<Type, ConstructorInfo>();
        private static readonly ConditionalWeakTable<object, AttachedReference> AttachedReferences = new ConditionalWeakTable<object, AttachedReference>();

        /// <summary>
        /// Gets the URL of a given object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>The URL.</returns>
        public static string GetUrl(object obj)
        {
            AttachedReference attachedReference;
            return AttachedReferences.TryGetValue(obj, out attachedReference) ? attachedReference.Url : null;
        }

        /// <summary>
        /// Sets the URL of a given object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="url">The URL.</param>
        public static void SetUrl(object obj, string url)
        {
            var attachedReference = AttachedReferences.GetValue(obj, x => new AttachedReference());
            attachedReference.Url = url;
        }

        /// <summary>
        /// Gets the reference info of attached to a given object, if it exists.
        /// </summary>
        /// <param name="obj">The object for which to get the attached reference. Can be null, in this case this method returns null.</param>
        /// <returns>The <see cref="AttachedReference"/> attached to the given object if available, <c>null</c> otherwise.</returns>
        public static AttachedReference GetAttachedReference(object obj)
        {
            if (obj == null)
                return null;

            AttachedReference attachedReference;
            AttachedReferences.TryGetValue(obj, out attachedReference);
            return attachedReference;
        }

        /// <summary>
        /// Gets or creates the object reference info of a given object.
        /// </summary>
        /// <param name="obj">The object.</param>
        public static AttachedReference GetOrCreateAttachedReference(object obj)
        {
            return AttachedReferences.GetValue(obj, x => new AttachedReference());
        }

        /// <summary>
        /// Creates a proxy object with <see cref="AttachedReference" /> designing it as a proxy with a given id and location (that can be used with <see cref="ContentManager" />). This allows to construct and save object references without actually loading them.
        /// </summary>
        /// <param name="reference">The content reference.</param>
        /// <returns>T.</returns>
        /// <exception cref="System.ArgumentNullException">reference</exception>
        public static T CreateProxyObject<T>(IReference reference) where T : class, new()
        {
            if (reference == null) throw new ArgumentNullException(nameof(reference));
            return CreateProxyObject<T>(reference.Id, reference.Location);
        }

        /// <summary>
        /// Creates a proxy object with <see cref="AttachedReference"/> designing it as a proxy with a given id and location (that can be used with <see cref="ContentManager"/>). This allows to construct and save object references without actually loading them.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="location">The location.</param>
        public static T CreateProxyObject<T>(AssetId id, string location) where T : class, new()
        {
            var result = new T();
            var attachedReference = GetOrCreateAttachedReference(result);
            attachedReference.Id = id;
            attachedReference.Url = location;
            attachedReference.IsProxy = true;
            return result;
        }

        /// <summary>
        /// Creates a proxy object with <see cref="AttachedReference"/> designing it as a proxy with a given id and location (that can be used with <see cref="ContentManager"/>). This allows to construct and save object references without actually loading them.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="location">The location.</param>
        public static object CreateProxyObject(Type type, AssetId id, string location)
        {
            ConstructorInfo emptyCtor;
            lock (EmptyCtorCache)
            {
                if (!EmptyCtorCache.TryGetValue(type, out emptyCtor))
                {
                    foreach (var ctor in type.GetTypeInfo().DeclaredConstructors)
                    {
                        if (!ctor.IsStatic && ctor.GetParameters().Length == 0)
                        {
                            emptyCtor = ctor;
                            break;
                        }
                    }
                    if (emptyCtor == null)
                    {
                        throw new InvalidOperationException($"Type {type} has no empty ctor");
                    }
                    EmptyCtorCache.Add(type, emptyCtor);
                }
            }
            var result = emptyCtor.Invoke(EmptyObjectArray);
            var attachedReference = GetOrCreateAttachedReference(result);
            attachedReference.Id = id;
            attachedReference.Url = location;
            attachedReference.IsProxy = true;
            return result;
        }
    }
}
