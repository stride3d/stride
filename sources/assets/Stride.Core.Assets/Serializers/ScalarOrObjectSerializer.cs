// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Reflection;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Events;
using Stride.Core.Yaml.Serialization;
using Stride.Core.Yaml.Serialization.Serializers;

namespace Stride.Core.Assets.Serializers
{
    /// <summary>
    /// Serializer that works with scalar, but could still read older ObjectSerializer format.
    /// </summary>
    public abstract class ScalarOrObjectSerializer : IYamlSerializableFactory, IYamlSerializable, IDataCustomVisitor
    {
        private static readonly ObjectSerializer ObjectSerializer = new ObjectSerializer();
        private readonly YamlRedirectSerializer scalarRedirectSerializer;

        protected ScalarOrObjectSerializer()
        {
            scalarRedirectSerializer = new YamlRedirectSerializer(this);
        }

        public IYamlSerializable TryCreate(SerializerContext context, ITypeDescriptor typeDescriptor)
        {
            return CanVisit(typeDescriptor.Type) ? this : null;
        }

        public abstract bool CanVisit(Type type);

        public void Visit(ref VisitorContext context)
        {
            // For a scalar object, we don't visit its members
            // But we do still visit the instance (either struct or class)
            context.Visitor.VisitObject(context.Instance, context.Descriptor, false);
        }

        public object ReadYaml(ref ObjectContext objectContext)
        {
            if (!objectContext.Reader.Accept<Scalar>())
            {
                // Old format, fallback to ObjectSerializer
                return ObjectSerializer.ReadYaml(ref objectContext);
            }

            // If it's a scalar (new format), redirect to our scalar serializer
            return scalarRedirectSerializer.ReadYaml(ref objectContext);
        }

        public void WriteYaml(ref ObjectContext objectContext)
        {
            if (ShouldSerializeAsScalar(ref objectContext))
            {
                scalarRedirectSerializer.WriteYaml(ref objectContext);
            }
            else
            {
                ObjectSerializer.WriteYaml(ref objectContext);
            }
        }

        public abstract object ConvertFrom(ref ObjectContext context, Scalar fromScalar);

        public abstract string ConvertTo(ref ObjectContext objectContext);

        protected virtual void WriteScalar(ref ObjectContext objectContext, ScalarEventInfo scalar)
        {
            // Emit the scalar
            objectContext.SerializerContext.Writer.Emit(scalar);
        }

        protected virtual bool ShouldSerializeAsScalar(ref ObjectContext objectContext)
        {
            // Always write in the new format by default
            return true;
        }

        internal class YamlRedirectSerializer : AssetScalarSerializerBase
        {
            private readonly ScalarOrObjectSerializer realScalarSerializer;

            public YamlRedirectSerializer(ScalarOrObjectSerializer realScalarSerializer)
            {
                this.realScalarSerializer = realScalarSerializer;
            }

            public override object ConvertFrom(ref ObjectContext context, Scalar fromScalar)
            {
                return realScalarSerializer.ConvertFrom(ref context, fromScalar);
            }

            public override string ConvertTo(ref ObjectContext objectContext)
            {
                return realScalarSerializer.ConvertTo(ref objectContext);
            }

            protected override void WriteScalar(ref ObjectContext objectContext, ScalarEventInfo scalar)
            {
                realScalarSerializer.WriteScalar(ref objectContext, scalar);
            }

            public override bool CanVisit(Type type)
            {
                return realScalarSerializer.CanVisit(type);
            }
        }
    }
}
