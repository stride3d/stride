// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using System.Text;

using Stride.Core.AssemblyProcessor.Serializers;

namespace Stride.Core.AssemblyProcessor
{
    internal class ProfileSerializerProcessor : ICecilSerializerProcessor
    {
        public void ProcessSerializers(CecilSerializerContext context)
        {
            var defaultProfile = context.SerializableTypes;

            foreach (var profile in context.SerializableTypesProfiles)
            {
                // Skip default profile
                if (profile.Value == defaultProfile)
                    continue;

                defaultProfile.IsFrozen = true;

                // For each profile, try to instantiate all types existing in default profile
                foreach (var type in defaultProfile.SerializableTypes)
                {
                    context.GenerateSerializer(type.Key, false, profile.Key);
                }
            }
        }
    }
}
