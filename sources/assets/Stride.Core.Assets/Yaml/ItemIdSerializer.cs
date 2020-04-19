// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Reflection;
using Stride.Core.Storage;
using Stride.Core.Yaml.Events;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Yaml
{
    /// <summary>
    /// A Yaml serializer for <see cref="ItemId"/> without associated data.
    /// </summary>
    [YamlSerializerFactory("Assets")] // TODO: use YamlAssetProfile.Name
    internal class ItemIdSerializer : ItemIdSerializerBase
    {
        /// <inheritdoc/>
        public override bool CanVisit(Type type)
        {
            return type == typeof(ItemId);
        }

        /// <inheritdoc/>
        public override object ConvertFrom(ref ObjectContext context, Scalar fromScalar)
        {
            ObjectId id;
            ObjectId.TryParse(fromScalar.Value, out id);
            return new ItemId(id);
        }
    }
}
