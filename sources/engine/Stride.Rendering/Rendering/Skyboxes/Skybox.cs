// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;

namespace Stride.Rendering.Skyboxes
{
    /// <summary>
    /// The Skybox at runtime.
    /// </summary>
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<Skybox>), Profile = "Content")]
    [ContentSerializer(typeof(DataContentSerializer<Skybox>))]
    [DataContract]
    public class Skybox
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Skybox"/> class.
        /// </summary>
        public Skybox()
        {
            Parameters = new ParameterCollection();
            DiffuseLightingParameters = new ParameterCollection();
            SpecularLightingParameters = new ParameterCollection();
        }

        /// <summary>
        /// Gets or sets the parameters compiled for the runtime for the skybox.
        /// </summary>
        /// <value>The parameters.</value>
        public ParameterCollection Parameters { get; set; }

        public ParameterCollection DiffuseLightingParameters { get; set; }

        public ParameterCollection SpecularLightingParameters { get; set; }
    }
}
