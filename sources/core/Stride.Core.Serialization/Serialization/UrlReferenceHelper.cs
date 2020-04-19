// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Assets;

namespace Stride.Core.Serialization
{
    /// <summary>
    /// A Helper class for <see cref="UrlReference"/> and <see cref="UrlReference{T}"/>.
    /// </summary>
    public static class UrlReferenceHelper
    {
        /// <summary>
        /// Creates a url reference to the given asset that matches the given reference type.
        /// </summary>
        /// <param name="referenceType">The type of reference to create.</param>
        /// <param name="assetId">The target asset id to create.</param>
        /// <param name="assetUrl">The target asset url to create.</param>
        /// <returns>A url reference to the given asset if it's not null and <paramref name="referenceType"/> is a valid reference url type, null otherwise.</returns>
        /// <remarks>A reference type is either an <see cref="UrlReference"/> or a <see cref="UrlReference{T}"/>.</remarks>
        public static object CreateReference(Type referenceType, AssetId assetId, string assetUrl)
        {
            if (assetId != null && assetUrl != null && IsUrlReferenceType(referenceType))
            {
                var urlReference = (UrlReferenceBase)AttachedReferenceManager.CreateProxyObject(referenceType, assetId, assetUrl);

                urlReference.Url = assetUrl;
                
                return urlReference;
            }

            return null;
        }

        /// <summary>
        /// Checks if the given type is either an <see cref="UrlReference"/> or a <see cref="UrlReference{T}"/>
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns></returns>
        public static bool IsUrlReferenceType(Type type)
            => type != null && typeof(UrlReference).IsAssignableFrom(type) || IsGenericUrlReferenceType(type);

        /// <summary>
        /// Checks if the given type is a <see cref="UrlReference{T}"/>
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns></returns>
        public static bool IsGenericUrlReferenceType(Type type)
            => type != null && IsSubclassOfRawGeneric(GenericType, type);

        /// <summary>
        /// Gets the asset content type for a given url reference type.
        /// </summary>
        /// <param name="type">The type is an url reference type, either an <see cref="UrlReference"/> or a <see cref="UrlReference{T}"/></param>
        /// <returns>The target content type or null.</returns>
        public static Type GetTargetContentType(Type type)
        {
            if (!IsUrlReferenceType(type)) return null;

            if (IsSubclassOfRawGeneric(GenericType, type))
            {
                return type.GetGenericArguments()[0];
            }

            return null;
        }

        private static readonly Type GenericType = typeof(UrlReference<>);

        //TODO: this should probably be put in one of the Reflection helper classes.
        static bool IsSubclassOfRawGeneric(Type type, Type c)
        {
            while (c != null && c != typeof(object))
            {
                var cur = c.IsGenericType ? c.GetGenericTypeDefinition() : c;
                if (type == cur)
                {
                    return true;
                }
                c = c.BaseType;
            }
            return false;
        }
    }
}
