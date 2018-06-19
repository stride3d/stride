// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;

namespace Xenko.Rendering.Skyboxes
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
