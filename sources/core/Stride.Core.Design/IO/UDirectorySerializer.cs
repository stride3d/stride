// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Serialization;

namespace Stride.Core.IO
{
    [DataSerializerGlobal(typeof(UDirectorySerializer))]
    internal class UDirectorySerializer : DataSerializer<UDirectory>
    {
        /// <inheritdoc/>
        public override void Serialize(ref UDirectory obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Serialize)
            {
                var path = obj?.FullPath;
                stream.Serialize(ref path);
            }
            else if (mode == ArchiveMode.Deserialize)
            {
                string path = null;
                stream.Serialize(ref path);
                obj = new UDirectory(path);
            }
        }
    }
}
