// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Assets.Serializers;
using Xenko.Core.Reflection;
using Xenko.Core.Yaml;
using Xenko.Core.Yaml.Events;
using Xenko.Core.Yaml.Serialization;
using Xenko.Rendering.Compositing;

namespace Xenko.Assets.Serializers
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
