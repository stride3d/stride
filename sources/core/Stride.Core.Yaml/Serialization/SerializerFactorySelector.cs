// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading;
using Stride.Core.Reflection;

namespace Stride.Core.Yaml.Serialization
{
    /// <summary>
    /// Base class that implements <see cref="ISerializerFactorySelector"/>.
    /// </summary>
    public abstract class SerializerFactorySelector : ISerializerFactorySelector
    {
        private readonly Dictionary<Type, IYamlSerializable> serializers = new Dictionary<Type, IYamlSerializable>();
        private readonly List<IYamlSerializableFactory> factories = new List<IYamlSerializableFactory>();
        private readonly ReaderWriterLockSlim serializerLock = new ReaderWriterLockSlim();
        private bool isSealed;

        /// <inheritdoc/>
        public void TryAddFactory(IYamlSerializableFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (isSealed) throw new InvalidOperationException("Cannot add a factory to a serializer factory selector once it is sealed.");
            if (CanAddSerializerFactory(factory))
            {
                factories.Add(factory);
            }
        }

        /// <inheritdoc/>
        public void Seal()
        {
            isSealed = true;
        }

        /// <inheritdoc/>
        public IYamlSerializable GetSerializer(SerializerContext context, ITypeDescriptor typeDescriptor)
        {
            if (!isSealed) throw new InvalidOperationException("A serializer factory selector must be sealed before being used.");
            IYamlSerializable serializer;

            // First try, with just a read lock
            serializerLock.EnterReadLock();
            var found = serializers.TryGetValue(typeDescriptor.Type, out serializer);
            serializerLock.ExitReadLock();

            if (!found)
            {
                // Not found, let's take exclusive lock and try again
                serializerLock.EnterWriteLock();
                if (!serializers.TryGetValue(typeDescriptor.Type, out serializer))
                {
                    foreach (var factory in factories)
                    {
                        serializer = factory.TryCreate(context, typeDescriptor);
                        if (serializer != null)
                        {
                            serializers.Add(typeDescriptor.Type, serializer);
                            break;
                        }
                    }
                }
                serializerLock.ExitWriteLock();
            }

            if (serializer == null)
            {
                throw new InvalidOperationException($"Unable to find a serializer for the type [{typeDescriptor.Type}]");
            }

            return serializer;
        }

        /// <summary>
        /// Indicates whether the given factory is supported by this selector.
        /// </summary>
        /// <param name="factory">The factory to evaluate.</param>
        /// <returns>True if the factory can be added to this selector, False otherwise.</returns>
        protected abstract bool CanAddSerializerFactory(IYamlSerializableFactory factory);
    }
}
