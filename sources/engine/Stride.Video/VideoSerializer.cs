// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;
using Xenko.Core.IO;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;

namespace Xenko.Video
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
