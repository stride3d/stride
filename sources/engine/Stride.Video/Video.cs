// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Stride.Core;
using Stride.Core.IO;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;

namespace Stride.Video
{
    /// <summary>
    /// Video content.
    /// </summary>
    [DebuggerDisplay("{" + nameof(Name) + "}")]
    [ContentSerializer(typeof(DataContentSerializer<Video>))]
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<Video>), Profile = "Content")]
    [DataSerializer(typeof(VideoSerializer))]

    public sealed class Video : ComponentBase
    {
        internal DatabaseFileProvider FileProvider;

        public string CompressedDataUrl { get; set; }
    }
}
