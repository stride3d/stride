// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Xenko.Core.Serialization.Contents
{
    /// <summary>
    /// Allows customization of IContentSerializer through an attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
    public class ContentSerializerAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContentSerializerAttribute"/> class.
        /// </summary>
        /// <param name="contentSerializerType">Type of the content serializer.</param>
        public ContentSerializerAttribute(Type contentSerializerType)
        {
            ContentSerializerType = contentSerializerType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentSerializerAttribute"/> class.
        /// </summary>
        public ContentSerializerAttribute()
        {
        }

        /// <summary>
        /// Gets the type of the content serializer.
        /// </summary>
        /// <value>
        /// The type of the content serializer.
        /// </value>
        public Type ContentSerializerType { get; private set; }
    }
}
