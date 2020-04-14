// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Annotations;
using Xenko.Core.Reflection;
using Xenko.Core.Yaml.Serialization;
using Xenko.Core.Yaml.Serialization.Serializers;

namespace Xenko.Core.Yaml
{
    public abstract class AssetScalarSerializerBase : ScalarSerializerBase, IYamlSerializableFactory, IDataCustomVisitor
    {
        [CanBeNull]
        public IYamlSerializable TryCreate(SerializerContext context, [NotNull] ITypeDescriptor typeDescriptor)
        {
            return CanVisit(typeDescriptor.Type) ? this : null;
        }

        public abstract bool CanVisit(Type type);

        public virtual void Visit(ref VisitorContext context)
        {
            // For a scalar object, we don't visit its members
            // But we do still visit the instance (either struct or class)
            context.Visitor.VisitObject(context.Instance, context.Descriptor, false);
        }
    }
}
