// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Assets.Serializers;
using Stride.Core.Reflection;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Events;
using Stride.Core.Yaml.Serialization;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Serializers
{
    /// <summary>
    /// A Yaml serializer for <see cref="ItemId"/> without associated data.
    /// </summary>
    [YamlSerializerFactory(YamlAssetProfile.Name)]
    internal class SceneCameraSlotIdSerializer : AssetScalarSerializerBase
    {
        public override bool CanVisit(Type type)
        {
            return type == typeof(SceneCameraSlotId);
        }

        /// <inheritdoc/>
        public override object ConvertFrom(ref ObjectContext context, Scalar fromScalar)
        {
            Guid id;
            Guid.TryParse(fromScalar.Value, out id);
            return new SceneCameraSlotId(id);
        }

        public override string ConvertTo(ref ObjectContext objectContext)
        {
            var slot = (SceneCameraSlotId)objectContext.Instance;
            return slot.Id.ToString();
        }
    }
}
