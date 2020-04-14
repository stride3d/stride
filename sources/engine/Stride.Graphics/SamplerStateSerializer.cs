// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;

namespace Xenko.Graphics
{
    /// <summary>
    /// Serializer for <see cref="SamplerState"/>.
    /// </summary>
    [DataSerializerGlobal(typeof(SamplerStateSerializer))]
    public class SamplerStateSerializer : DataSerializer<SamplerState>
    {
        public override void Serialize(ref SamplerState samplerState, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Serialize)
            {
                var samplerStateDescription = samplerState.Description;
                stream.Serialize(ref samplerStateDescription, mode);
            }
            else
            {
                // If we have a graphics context, we will instantiate GPU state, otherwise a CPU one.
                var services = stream.Context.Tags.Get(ServiceRegistry.ServiceRegistryKey);
                var graphicsDeviceService = services != null ? services.GetSafeServiceAs<IGraphicsDeviceService>() : null;

                var samplerStateDescription = SamplerStateDescription.Default;
                stream.Serialize(ref samplerStateDescription, mode);
                samplerState = graphicsDeviceService != null
                    ? SamplerState.New(graphicsDeviceService.GraphicsDevice, samplerStateDescription)
                    : SamplerState.NewFake(samplerStateDescription);
            }
        }
    }
}
