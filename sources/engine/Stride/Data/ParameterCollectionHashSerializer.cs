// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Serialization;

namespace Stride.Rendering.Data
{
    /// <summary>
    ///   Provides functionality to serialize and deserialize <see cref="ParameterCollection"/> objects
    ///   when used with the <see cref="DataSerializerGlobalAttribute.Profile"/> <c>"Hash"</c>.
    /// </summary>
    /// <remarks>
    ///   This serializer is specifically designed to handle the serialization of <c>CompilerParameters</c>,
    ///   which are a specialized form of <see cref="ParameterCollection"/> used in the context of Shader compilation.
    ///   Only permutation parameters are serialized, as CompilerParameters should not contain other types of parameters.
    /// </remarks>
    public class ParameterCollectionHashSerializer : ClassDataSerializer<ParameterCollection>
    {
        private DataSerializer<ParameterKey> parameterKeySerializer;


        /// <inheritdoc/>
        public override void Initialize(SerializerSelector serializerSelector)
        {
            parameterKeySerializer = serializerSelector.GetSerializer<ParameterKey>();
        }

        /// <summary>
        ///   Serializes or deserializes a <see cref="ParameterCollection"/> object.
        /// </summary>
        /// <param name="parameterCollection">The object to serialize or deserialize.</param>
        /// <inheritdoc/>
        public override void Serialize(ref ParameterCollection parameterCollection, ArchiveMode mode, SerializationStream stream)
        {
            foreach (var parameter in parameterCollection.ParameterKeyInfos)
            {
                // CompilerParameters should only contain permutation parameters
                if (parameter.Key.Type != ParameterKeyType.Permutation)
                    continue;

                parameterKeySerializer.Serialize(parameter.Key, stream);

                var value = parameterCollection.ObjectValues[parameter.BindingSlot];
                parameter.Key.Serialize(stream, value);
            }
        }
    }
}
