// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Xenko.Core;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;

namespace Xenko.Video
{
    /// <summary>
    /// Video content.
    /// </summary>
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    [ContentSerializer(typeof(DataContentSerializer<Video>))]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<Video>), Profile = "Content")]
    [DataContract]
    public sealed class Video : ComponentBase
    {
        public string CompressedDataUrl { get; set; }
    }
}
