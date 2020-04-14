// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands
{
    /// <summary>
    /// Represents a type for <see cref="CreateNewInstanceCommand"/>.
    /// </summary>
    public sealed class AbstractNodeType : AbstractNodeEntry
    {
        public AbstractNodeType(Type type)
        {
            Type = type;
        }

        public Type Type { get; }

        /// <inheritdoc/>
        public override int Order => 0;

        /// <inheritdoc/>
        public override string DisplayValue => DisplayAttribute.GetDisplayName(Type);

        /// <inheritdoc/>
        public override object GenerateValue(object currentValue)
        {
            // Check if this type can be created first to avoid exceptions
            if (!ObjectFactoryRegistry.CanCreateInstance(Type))
                return null;

            return ObjectFactoryRegistry.NewInstance(Type);
        }

        /// <inheritdoc/>
        public override bool Equals(AbstractNodeEntry other)
        {
            var abstractNodeType = other as AbstractNodeType;
            if (abstractNodeType == null)
                return false;

            return Type == abstractNodeType.Type;
        }

        /// <inheritdoc/>
        public override bool IsMatchingValue(object value) => value?.GetType() == Type;

        public static IEnumerable<AbstractNodeType> GetInheritedInstantiableTypes(Type type)
        {
            return type.GetInheritedInstantiableTypes().Where(x => Attribute.GetCustomAttribute(x, typeof(NonInstantiableAttribute)) == null).Select(x => new AbstractNodeType(x));
        }

        public override string ToString() => DisplayValue;

        /// <inheritdoc/>
        protected override int ComputeHashCode()
        {
            return Type.GetHashCode();
        }
    }
}
