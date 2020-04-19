// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Reflection;

namespace Stride.Core.Yaml.Serialization
{
    /// <summary>
    /// An interface representing a serializer factory selector.
    /// </summary>
    public interface ISerializerFactorySelector
    {
        /// <summary>
        /// Tries to register the given factory to this selector. The factory might be ignored if it doesn't match the criteria of the selector.
        /// </summary>
        /// <param name="factory">The factory to register.</param>
        void TryAddFactory(IYamlSerializableFactory factory);

        /// <summary>
        /// Seals the selector. No factory can be added to the selector after it has been sealed. The selector cannot be used to retrieve serializers until it's sealed.
        /// </summary>
        void Seal();

        /// <summary>
        /// Retrieves the serializer corresponding to the given type descriptor,
        /// </summary>
        /// <param name="context">The serializer context.</param>
        /// <param name="typeDescriptor">The type descriptor for which to retrieve a serializer.</param>
        /// <returns>A serializer that can serializer the given type in the given context.</returns>
        IYamlSerializable GetSerializer(SerializerContext context, ITypeDescriptor typeDescriptor);
    }
}
