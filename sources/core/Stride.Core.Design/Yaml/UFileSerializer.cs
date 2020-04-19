// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Annotations;
using Stride.Core.IO;
using Stride.Core.Yaml.Events;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Yaml
{
    /// <summary>
    /// A Yaml serializer for <see cref="UFile"/>.
    /// </summary>
    [YamlSerializerFactory(YamlSerializerFactoryAttribute.Default)]
    internal class UFileSerializer : AssetScalarSerializerBase
    {
        public override bool CanVisit(Type type)
        {
            return typeof(UFile) == type;
        }

        [NotNull]
        public override object ConvertFrom(ref ObjectContext context, [NotNull] Scalar fromScalar)
        {
            return new UFile(fromScalar.Value);
        }

        [NotNull]
        public override string ConvertTo(ref ObjectContext objectContext)
        {
            var path = ((UFile)objectContext.Instance);
            return path.FullPath;
        }

        protected override void WriteScalar(ref ObjectContext objectContext, ScalarEventInfo scalar)
        {
            // Force tag when writing back
            scalar.Tag = objectContext.SerializerContext.TagFromType(typeof(UFile));
            scalar.IsPlainImplicit = false;
            base.WriteScalar(ref objectContext, scalar);
        }
    }
}
