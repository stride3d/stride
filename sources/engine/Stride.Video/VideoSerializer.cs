// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.IO;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;

namespace Stride.Video
{
    /// <summary>
    /// Used internally to serialize Video
    /// </summary>
    internal sealed class VideoSerializer : DataSerializer<Video>
    {
        public override void Serialize(ref Video video, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                var services = stream.Context.Tags.Get(ServiceRegistry.ServiceRegistryKey);

                video.FileProvider = services.GetService<IDatabaseFileProviderService>()?.FileProvider;
                video.CompressedDataUrl = stream.ReadString();
            }
            else
            {
                stream.Write(video.CompressedDataUrl);
            }
        }
    }
}
