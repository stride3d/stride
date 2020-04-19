// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Reflection;
using System.Text;
using Stride.Core.Assets.Serializers;
using Stride.Core;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Events;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Assets.Tests
{
    /// <summary>
    /// We are copying the PropertyKey serializer from Stride.Assets assembly to here in order 
    /// to validate our tests.
    /// </summary>
    [YamlSerializerFactory(YamlSerializerFactoryAttribute.Default)]
    internal class PropertyKeySerializerTest : AssetScalarSerializerBase
    {
        public override bool CanVisit(Type type)
        {
            return (typeof(PropertyKey).IsAssignableFrom(type)); // && (!typeof(ParameterKey).IsAssignableFrom(type));
        }

        public override object ConvertFrom(ref ObjectContext objectContext, Scalar fromScalar)
        {
            var lastDot = fromScalar.Value.LastIndexOf('.');
            if (lastDot == -1)
                return null;

            var className = fromScalar.Value.Substring(0, lastDot);

            bool alias;
            var containingClass = objectContext.SerializerContext.TypeFromTag("!" + className, out alias); // Readd initial '!'
            if (containingClass == null)
            {
                throw new YamlException(fromScalar.Start, fromScalar.End, "Unable to find class from tag [{0}]".ToFormat(className));
            }

            var propertyName = fromScalar.Value.Substring(lastDot + 1);
            var propertyField = containingClass.GetField(propertyName, BindingFlags.Public | BindingFlags.Static);
            if (propertyField == null)
            {
                throw new YamlException(fromScalar.Start, fromScalar.End, "Unable to find property [{0}] in class [{1}]".ToFormat(propertyName, containingClass.Name));
            }

            return propertyField.GetValue(null);
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
            var propertyKey = (PropertyKey)objectContext.Instance;

            var className = objectContext.SerializerContext.TagFromType(propertyKey.OwnerType);
            var sb = new StringBuilder(className.Length + 1 + propertyKey.Name.Length);

            sb.Append(className, 1, className.Length - 1); // Ignore initial '!'
            sb.Append('.');
            sb.Append(propertyKey.Name);

            return sb.ToString();
        }
    }
}
