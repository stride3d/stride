// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Serialization;

namespace Stride.Core
{
    [DataContract("PackageVersion")]
    [DataSerializer(typeof(PackageVersionDataSerializer))]
    public sealed partial class PackageVersion
    {
        internal class PackageVersionDataSerializer : DataSerializer<PackageVersion>
        {
            /// <inheritdoc/>
            public override bool IsBlittable => true;

            /// <inheritdoc/>
            public override void Serialize(ref PackageVersion obj, ArchiveMode mode, SerializationStream stream)
            {
                if (mode == ArchiveMode.Deserialize)
                {
                    string version = null;
                    stream.Serialize(ref version);
                    obj = Parse(version);
                }
                else
                {
                    string version = obj.ToString();
                    stream.Serialize(ref version);
                }
            }
        }
    }
}
