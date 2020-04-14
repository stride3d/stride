// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Reflection;
using Stride.Core.Reflection;

namespace Stride.Core.Annotations
{
    /// <summary>
    /// An attribute that defines a factory class implementing <see cref="IObjectFactory"/>, used to create instances of the related type in design-time scenarios.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class ObjectFactoryAttribute : Attribute
    {
        /// <summary>
        /// The type of the factory to use to create instance of the related type.
        /// </summary>
        [NotNull]
        public Type FactoryType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectFactoryAttribute"/> class.
        /// </summary>
        /// <param name="factoryType">The factory type that implements <see cref="IObjectFactory"/>.</param>
        public ObjectFactoryAttribute([NotNull] Type factoryType)
        {
            if (factoryType == null) throw new ArgumentNullException(nameof(factoryType));
#if STRIDE_PLATFORM_WINDOWS_DESKTOP
            if (!typeof(IObjectFactory).GetTypeInfo().IsAssignableFrom(factoryType.GetTypeInfo())) throw new ArgumentException($@"The given type does not implement {nameof(IObjectFactory)}/", nameof(factoryType));
            if (factoryType.GetTypeInfo().GetConstructor(Type.EmptyTypes) == null) throw new ArgumentException(@"The given type does have a public parameterless constructor.", nameof(factoryType));
#endif
            FactoryType = factoryType;
        }
    }
}
