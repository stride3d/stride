// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Serialization.Contents
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ContentSerializerExtensionAttribute : Attribute
    {
        public ContentSerializerExtensionAttribute(string supportedExtension)
        {
            SupportedExtension = supportedExtension;
        }

        public string SupportedExtension { get; private set; }
    }
}
