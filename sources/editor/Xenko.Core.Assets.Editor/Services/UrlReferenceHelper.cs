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
        /// <remarks>A reference type is either an <see cref="AssetReference"/> or a content type registered in the <see cref="AssetRegistry"/>.</remarks>
        public static bool ContainsUrlReferenceType(ITypeDescriptor typeDescriptor)
        {
            var type = typeDescriptor.GetInnerCollectionType();
            return IsUrlReferenceType(type);
        }

        public static object CreateReference(AssetViewModel asset, Type referenceType)
        {
            if (asset != null && IsUrlReferenceType(referenceType))
            {
                return Activator.CreateInstance(referenceType, asset.Id, asset.Url);
            }

            return null;
        }


        public static bool IsUrlReferenceType(Type type)
            => type != null && typeof(UrlReference).IsAssignableFrom(type);

        public static Type GetTargetType(Type type)
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
        /// <remarks>The <paramref name="source"/> parameter must either be an <see cref="UrlReference"/>, or a proxy object of an <see cref="UrlReference{T}"/>.</remarks>
        public static AssetViewModel GetReferenceTarget(SessionViewModel session, object source)
        {
            if (source is UrlReference urlReference)
            {
                return session.GetAssetById(urlReference.Id);
            }

            return null;
        }

        private static readonly Type GenericType = typeof(UrlReference<>);

        //TODO: this should probably be put in one of the Xenko.Core.Reflection helper classes.
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
