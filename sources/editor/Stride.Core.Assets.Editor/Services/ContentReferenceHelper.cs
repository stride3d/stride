// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Extensions;
using Stride.Core.Reflection;
using Stride.Core.Serialization;

namespace Stride.Core.Assets.Editor.Services
{
    public class ContentReferenceHelper
    {
        public const string BrokenReference = "(Broken reference)";
        public const string EmptyReference = "(No asset selected)";

        /// <summary>
        /// Creates a reference to the given asset that matches the given reference type.
        /// </summary>
        /// <param name="asset">The target asset of the reference to create.</param>
        /// <returns>A reference to the given asset if it's not null and <typeparamref name="TReferenceType"/> is a valid reference type, null otherwise.</returns>
        /// <remarks>A reference type is either an <see cref="AssetReference"/> or a content type registered in the <see cref="AssetRegistry"/>.</remarks>
        public static TReferenceType CreateReference<TReferenceType>(AssetViewModel asset) where TReferenceType : class
        {
            return CreateReference(asset, typeof(TReferenceType)) as TReferenceType;
        }

        /// <summary>
        /// Creates a reference to the given asset that matches the given reference type.
        /// </summary>
        /// <param name="asset">The target asset of the reference to create.</param>
        /// <param name="referenceType">The type of reference to create.</param>
        /// <returns>A reference to the given asset if it's not null and <paramref name="referenceType"/> is a valid reference type, null otherwise.</returns>
        /// <remarks>A reference type is either an <see cref="AssetReference"/> or a content type registered in the <see cref="AssetRegistry"/>.</remarks>
        public static object CreateReference(AssetViewModel asset, Type referenceType)
        {
            if (asset != null)
            {
                if (UrlReferenceHelper.IsUrlReferenceType(referenceType))
                {
                    return AttachedReferenceManager.CreateProxyObject(referenceType, asset.Id, asset.Url);
                }

                if (AssetRegistry.IsContentType(referenceType))
                {
                    var assetType = asset.AssetItem.Asset.GetType();
                    var contentType = AssetRegistry.GetContentType(assetType);
                    return referenceType.IsAssignableFrom(contentType) ? AttachedReferenceManager.CreateProxyObject(contentType, asset.Id, asset.Url) : null;
                }

                if (typeof(AssetReference).IsAssignableFrom(referenceType))
                    return new AssetReference(asset.AssetItem.Id, asset.AssetItem.Location);
            }

            return null;
        }

        /// <summary>
        /// Retrieves the view model corresponding to the asset referenced by the <paramref name="source"/> parameter.
        /// </summary>
        /// <param name="session">The session view model to use to retrieve the asset view model.</param>
        /// <param name="source">The source of the reference.</param>
        /// <returns>The view model corresponding to the referenced asset if found, null otherwise.</returns>
        /// <remarks>The <paramref name="source"/> parameter must either be an <see cref="AssetReference"/>, or a proxy object of an <see cref="AttachedReference"/>.</remarks>
        public static AssetViewModel GetReferenceTarget(SessionViewModel session, object source)
        {
            var assetReference = source as AssetReference;
            if (assetReference != null)
            {
                return session.GetAssetById(assetReference.Id);
            }
            var reference = AttachedReferenceManager.GetAttachedReference(source);
            return reference != null ? session.GetAssetById(reference.Id) : null;
        }

        /// <summary>
        /// Indicates if the given type descriptor represents a reference type, or a collection of reference types.
        /// </summary>
        /// <param name="typeDescriptor">The type descriptor to analyze.</param>
        /// <returns>True if the type descriptor represents a reference type, false otherwise.</returns>
        /// <remarks>A reference type is either an <see cref="AssetReference"/> or a content type registered in the <see cref="AssetRegistry"/>.</remarks>
        public static bool ContainsReferenceType(ITypeDescriptor typeDescriptor)
        {
            var type = typeDescriptor.GetInnerCollectionType();
            return typeof(AssetReference).IsAssignableFrom(type) || AssetRegistry.IsContentType(type);
        }
    }
}
