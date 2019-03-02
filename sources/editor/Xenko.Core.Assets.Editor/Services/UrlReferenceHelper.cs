using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Extensions;
using Xenko.Core.Reflection;
using Xenko.Core.Serialization;

namespace Xenko.Core.Assets.Editor.Services
{
    public static class UrlReferenceHelper
    {
        /// <summary>
        /// Indicates if the given type descriptor represents a reference type, or a collection of reference types.
        /// </summary>
        /// <param name="typeDescriptor">The type descriptor to analyze.</param>
        /// <returns>True if the type descriptor represents a url reference type, false otherwise.</returns>
        /// <remarks>A reference type is either an <see cref="UrlReference"/> or a <see cref="UrlReference{T}"/>.</remarks>
        public static bool ContainsUrlReferenceType(ITypeDescriptor typeDescriptor)
        {
            var type = typeDescriptor.GetInnerCollectionType();
            return IsUrlReferenceType(type);
        }

        /// <summary>
        /// Creates a url reference to the given asset that matches the given reference type.
        /// </summary>
        /// <param name="asset">The target asset of the reference url to create.</param>
        /// <param name="referenceType">The type of reference to create.</param>
        /// <returns>A url reference to the given asset if it's not null and <paramref name="referenceType"/> is a valid reference url type, null otherwise.</returns>
        /// <remarks>A reference type is either an <see cref="UrlReference"/> or a <see cref="UrlReference{T}"/>.</remarks>
        public static object CreateReference(AssetViewModel asset, Type referenceType)
        {
            if (asset != null && IsUrlReferenceType(referenceType))
            {
                return Activator.CreateInstance(referenceType, asset.Id, asset.Url);
            }

            return null;
        }

        /// <summary>
        /// Checks if the given type is either an <see cref="UrlReference"/> or a <see cref="UrlReference{T}"/>
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns></returns>
        public static bool IsUrlReferenceType(Type type)
            => type != null && typeof(UrlReference).IsAssignableFrom(type);

        /// <summary>
        /// Checks if the given type is a <see cref="UrlReference{T}"/>
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns></returns>
        public static bool IsGenericUrlReferenceType(Type type)
            => type != null && IsSubclassOfRawGeneric(GenericType,type);

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

        /// <summary>
        /// Retrieves the view model corresponding to the asset referenced by the <paramref name="source"/> parameter.
        /// </summary>
        /// <param name="session">The session view model to use to retrieve the asset view model.</param>
        /// <param name="source">The source of the reference.</param>
        /// <returns>The view model corresponding to the referenced asset if found, null otherwise.</returns>
        /// <remarks>The <paramref name="source"/> parameter must either be an <see cref="UrlReference"/>, or a <see cref="UrlReference{T}"/>.</remarks>
        public static AssetViewModel GetReferenceTarget(SessionViewModel session, object source)
        {
            if (source is UrlReference urlReference)
            {
                return session.GetAssetById(urlReference.Id);
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
