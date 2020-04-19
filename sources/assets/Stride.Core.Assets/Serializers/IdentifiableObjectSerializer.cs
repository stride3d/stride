// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Assets.Yaml;
using Stride.Core;
using Stride.Core.Reflection;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Events;
using Stride.Core.Yaml.Serialization;
using Stride.Core.Yaml.Serialization.Serializers;

namespace Stride.Core.Assets.Serializers
{
    /// <summary>
    /// A serializer for <see cref="IIdentifiable"/> instances, that can either serialize them directly or as an object reference.
    /// </summary>
    public sealed class IdentifiableObjectSerializer : ChainedSerializer
    {
        public const string Prefix = "ref!! ";
        private readonly IdentifiableObjectReferenceSerializer scalarRedirectSerializer = new IdentifiableObjectReferenceSerializer();

        public void Visit(ref VisitorContext context)
        {
            // For a scalar object, we don't visit its members
            // But we do still visit the instance (either struct or class)
            context.Visitor.VisitObject(context.Instance, context.Descriptor, false);
        }

        public override object ReadYaml(ref ObjectContext objectContext)
        {
            if (objectContext.Reader.Accept<Scalar>())
            {
                var next = objectContext.Reader.Peek<Scalar>();
                if (next.Value.StartsWith(Prefix))
                {
                    return scalarRedirectSerializer.ReadYaml(ref objectContext);
                }
            }
            return base.ReadYaml(ref objectContext);
        }

        public override void WriteYaml(ref ObjectContext objectContext)
        {
            if (ShouldSerializeAsScalar(ref objectContext))
            {
                scalarRedirectSerializer.WriteYaml(ref objectContext);
            }
            else
            {
                base.WriteYaml(ref objectContext);
            }
        }

        public IYamlSerializable TryCreate(SerializerContext context, ITypeDescriptor typeDescriptor) => typeof(IIdentifiable).IsAssignableFrom(typeDescriptor.Type) ? this : null;

        private static bool ShouldSerializeAsScalar(ref ObjectContext objectContext)
        {
            YamlAssetMetadata<Guid> objectReferences;
            if (!objectContext.SerializerContext.Properties.TryGetValue(AssetObjectSerializerBackend.ObjectReferencesKey, out objectReferences))
                return false;

            var path = AssetObjectSerializerBackend.GetCurrentPath(ref objectContext, true);
            return objectReferences.TryGet(path) != Guid.Empty;
        }

        private static bool TryParse(string text, out Guid identifier)
        {
            if (!text.StartsWith(Prefix))
            {
                identifier = Guid.Empty;
                return false;
            }
            return Guid.TryParse(text.Substring(Prefix.Length), out identifier);
        }

        private class IdentifiableObjectReferenceSerializer : ScalarSerializerBase
        {
            public override object ConvertFrom(ref ObjectContext context, Scalar fromScalar)
            {
                Guid identifier;
                if (!TryParse(fromScalar.Value, out identifier))
                {
                    throw new YamlException($"Unable to deserialize reference: [{fromScalar.Value}]");
                }

                // Add the path to the currently deserialized object to the list of object references
                YamlAssetMetadata<Guid> objectReferences;
                if (!context.SerializerContext.Properties.TryGetValue(AssetObjectSerializerBackend.ObjectReferencesKey, out objectReferences))
                {
                    objectReferences = new YamlAssetMetadata<Guid>();
                    context.SerializerContext.Properties.Add(AssetObjectSerializerBackend.ObjectReferencesKey, objectReferences);
                }
                var path = AssetObjectSerializerBackend.GetCurrentPath(ref context, true);
                objectReferences.Set(path, identifier);

                // Return default(T)
                //return !context.Descriptor.Type.IsValueType ? null : Activator.CreateInstance(context.Descriptor.Type);
                // Return temporary proxy instance
                var proxy = (IIdentifiable)AbstractObjectInstantiator.CreateConcreteInstance(context.Descriptor.Type);
                proxy.Id = identifier;
                return proxy;
            }

            public override string ConvertTo(ref ObjectContext objectContext)
            {
                var identifiable = (IIdentifiable)objectContext.Instance;
                return $"{Prefix}{identifiable.Id}";
            }

            protected override void WriteScalar(ref ObjectContext objectContext, ScalarEventInfo scalar)
            {
                // Remove the tag if one was added, which might happen if the concrete type is different from the container type.
                // NOTE: disabled for now, although it doesn't seem necessary anymore. To re-enable removing, just uncomment these two lines
                //scalar.Tag = null;
                //scalar.IsPlainImplicit = true;

                // Emit the scalar
                objectContext.SerializerContext.Writer.Emit(scalar);
            }
        }
    }
}
