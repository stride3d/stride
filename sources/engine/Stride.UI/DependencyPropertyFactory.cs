// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Stride.Core;

namespace Stride.UI
{
    public static class DependencyPropertyFactory
    {
        /// <summary>
        /// Registers a dependency property.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The type that is registering the property.</param>
        /// <param name="defaultValue">The default value of the property.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PropertyKey<T> Register<T>(string name, Type ownerType, T defaultValue)
        {
            return Register(name, ownerType, defaultValue, null, null);
        }

        /// <summary>
        /// Registers a dependency property.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The type that is registering the property.</param>
        /// <param name="defaultValue">The default value of the property.</param>
        /// <param name="validateValueCallback">A callback for validation/coercision of the property's value.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PropertyKey<T> Register<T>(string name, Type ownerType, T defaultValue, ValidateValueCallback<T> validateValueCallback)
        {
            return Register(name, ownerType, defaultValue, validateValueCallback, null);
        }

        /// <summary>
        /// Registers a dependency property.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The type that is registering the property.</param>
        /// <param name="defaultValue">The default value of the property.</param>
        /// <param name="invalidationCallback">A callback to invalidate an object state after a modification of the property's value.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PropertyKey<T> Register<T>(string name, Type ownerType, T defaultValue, ObjectInvalidationCallback<T> invalidationCallback)
        {
            return Register(name, ownerType, defaultValue, null, invalidationCallback);
        }

        /// <summary>
        /// Registers a dependency property.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The type that is registering the property.</param>
        /// <param name="defaultValue">The default value of the property.</param>
        /// <param name="validateValueCallback">A callback for validation/coercision of the property's value.</param>
        /// <param name="invalidationCallback">A callback to invalidate an object state after a modification of the property's value.</param>
        /// <param name="metadatas">The metadatas.</param>
        /// <returns></returns>
        public static PropertyKey<T> Register<T>(string name, Type ownerType, T defaultValue, ValidateValueCallback<T> validateValueCallback, ObjectInvalidationCallback<T> invalidationCallback, params PropertyKeyMetadata[] metadatas)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (ownerType == null) throw new ArgumentNullException(nameof(ownerType));
            if (metadatas == null) throw new ArgumentNullException(nameof(metadatas));

            return RegisterCommon(DependencyPropertyKeyMetadata.Default, name, ownerType, defaultValue, validateValueCallback, invalidationCallback, metadatas);
        }

        /// <summary>
        /// Registers an attached dependency property.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The type that is registering the property.</param>
        /// <param name="defaultValue">The default value of the property.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PropertyKey<T> RegisterAttached<T>(string name, Type ownerType, T defaultValue)
        {
            return RegisterAttached(name, ownerType, defaultValue, null, null);
        }

        /// <summary>
        /// Registers an attached dependency property.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The type that is registering the property.</param>
        /// <param name="defaultValue">The default value of the property.</param>
        /// <param name="validateValueCallback">A callback for validation/coercision of the property's value.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PropertyKey<T> RegisterAttached<T>(string name, Type ownerType, T defaultValue, ValidateValueCallback<T> validateValueCallback)
        {
            return RegisterAttached(name, ownerType, defaultValue, validateValueCallback, null);
        }

        /// <summary>
        /// Registers an attached dependency property.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The type that is registering the property.</param>
        /// <param name="defaultValue">The default value of the property.</param>
        /// <param name="invalidationCallback">A callback to invalidate an object state after a modification of the property's value.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PropertyKey<T> RegisterAttached<T>(string name, Type ownerType, T defaultValue, ObjectInvalidationCallback<T> invalidationCallback)
        {
            return RegisterAttached(name, ownerType, defaultValue, null, invalidationCallback);
        }

        /// <summary>
        /// Registers an attached dependency property.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The type that is registering the property.</param>
        /// <param name="defaultValue">The default value of the property.</param>
        /// <param name="validateValueCallback">A callback for validation/coercision of the property's value.</param>
        /// <param name="invalidationCallback">A callback to invalidate an object state after a modification of the property's value.</param>
        /// <param name="metadatas">The metadatas.</param>
        /// <returns></returns>
        public static PropertyKey<T> RegisterAttached<T>(string name, Type ownerType, T defaultValue, ValidateValueCallback<T> validateValueCallback, ObjectInvalidationCallback<T> invalidationCallback, params PropertyKeyMetadata[] metadatas)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (ownerType == null) throw new ArgumentNullException(nameof(ownerType));
            if (metadatas == null) throw new ArgumentNullException(nameof(metadatas));

            return RegisterCommon(DependencyPropertyKeyMetadata.Attached, name, ownerType, defaultValue, validateValueCallback, invalidationCallback, metadatas);
        }

        // ReSharper disable once SuggestBaseTypeForParameter
        private static PropertyKey<T> RegisterCommon<T>(DependencyPropertyKeyMetadata dependencyPropertyMetadata, string name, Type ownerType, T defaultValue, ValidateValueCallback<T> validateValueCallback, ObjectInvalidationCallback<T> invalidationCallback, params PropertyKeyMetadata[] otherMetadatas)
        {
            var metadataList = new List<PropertyKeyMetadata> { dependencyPropertyMetadata, DefaultValueMetadata.Static(defaultValue) };
            if (validateValueCallback != null)
                metadataList.Add(ValidateValueMetadata.New(validateValueCallback));
            if (invalidationCallback != null)
                metadataList.Add(ObjectInvalidationMetadata.New(invalidationCallback));
            metadataList.AddRange(otherMetadatas);

            return new PropertyKey<T>(name, ownerType, metadataList.ToArray());
        }
    }
}
