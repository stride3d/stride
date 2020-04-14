// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core.Serialization;

namespace Xenko.Rendering.Data
{
    public class ParameterCollectionHashSerializer : ClassDataSerializer<ParameterCollection>
    {
        private DataSerializer<ParameterKey> parameterKeySerializer;

        public override void Initialize(SerializerSelector serializerSelector)
        {
            parameterKeySerializer = serializerSelector.GetSerializer<ParameterKey>();
        }

        public override void Serialize(ref ParameterCollection parameterCollection, ArchiveMode mode, SerializationStream stream)
        {
            foreach (var parameter in parameterCollection.ParameterKeyInfos)
            {
                // CompilerParameters should only contain permutation parameters
                if (parameter.Key.Type != ParameterKeyType.Permutation)
                    continue;

                parameterKeySerializer.Serialize(parameter.Key, stream);

                var value = parameterCollection.ObjectValues[parameter.BindingSlot];
                parameter.Key.SerializeHash(stream, value);
            }
        }
    }
}
