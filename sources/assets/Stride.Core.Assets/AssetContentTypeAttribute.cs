// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.Assets
{
    /// <summary>
    /// Describes which runtime-type, loadable through the <see cref="ContentManager"/>, corresponds to the associated asset type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AssetContentTypeAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetContentTypeAttribute"/> class.
        /// </summary>
        /// <param name="contentType">The content type corresponding to the associated asset type.</param>
        public AssetContentTypeAttribute(Type contentType)
        {
            ContentType = contentType;
        }

        /// <summary>
        /// The content type corresponding to the associated asset type.
        /// </summary>
        public Type ContentType { get; }
    }
}
