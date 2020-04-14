// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO;
using Stride.Core.Serialization;
using Stride.Core.Storage;

namespace Stride.Navigation
{
    public static class NavigationSettingsExtensions
    {
        /// <summary>
        /// Computes the hash of the <see cref="NavigationSettings.Groups"/> field
        /// </summary>
        public static ObjectId ComputeGroupsHash(this NavigationSettings settings)
        {
            using (DigestStream stream = new DigestStream(Stream.Null))
            {
                BinarySerializationWriter writer = new BinarySerializationWriter(stream);
                writer.Write(settings.Groups);
                return stream.CurrentHash;
            }
        }
    }
}
