// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Assets.Serializers;
using Stride.Core;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Events;
using Stride.Core.Yaml.Serialization;
using Stride.Rendering;

namespace Stride.Assets.Serializers
{
    /// <summary>
    /// A Yaml serializer for <see cref="ParameterKey"/>
    /// </summary>
    [YamlSerializerFactory(YamlAssetProfile.Name)]
    internal class ParameterKeySerializer : AssetScalarSerializerBase
    {
        public override bool CanVisit(Type type)
        {
            return typeof(ParameterKey).IsAssignableFrom(type);
        }

        public override object ConvertFrom(ref ObjectContext objectContext, Scalar fromScalar)
        {
            var parameterKey = ParameterKeys.FindByName(fromScalar.Value);
            if (parameterKey == null)
            {
                throw new YamlException(fromScalar.Start, fromScalar.End, "Unable to find registered ParameterKey [{0}]".ToFormat(fromScalar.Value));
            }
            return parameterKey;
        }

        protected override void WriteScalar(ref ObjectContext objectContext, ScalarEventInfo scalar)
        {
            // TODO: if ParameterKey is written to an object, It will not serialized a tag
            scalar.Tag = null;
            scalar.IsPlainImplicit = true;
            base.WriteScalar(ref objectContext, scalar);
        }

        public override string ConvertTo(ref ObjectContext objectContext)
        {
            return ((ParameterKey)objectContext.Instance).Name;
        }
    }
}
