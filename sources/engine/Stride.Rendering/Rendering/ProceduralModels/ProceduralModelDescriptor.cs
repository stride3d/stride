// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Serialization.Contents;
using Stride.Rendering.ProceduralModels;

namespace Stride.Rendering.ProceduralModels
{
    /// <summary>
    /// A descriptor for a procedural geometry.
    /// </summary>
    [DataContract("ProceduralModelDescriptor")]
    [ContentSerializer(typeof(ProceduralModelDescriptorContentSerializer))]
    [ContentSerializer(typeof(DataContentSerializer<ProceduralModelDescriptor>))]    
    public class ProceduralModelDescriptor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProceduralModelDescriptor"/> class.
        /// </summary>
        public ProceduralModelDescriptor()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProceduralModelDescriptor"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        public ProceduralModelDescriptor(IProceduralModel type)
        {
            Type = type;
        }

        /// <summary>
        /// Gets or sets the type of geometric primitive.
        /// </summary>
        /// <value>The type of geometric primitive.</value>
        [DataMember(10)]
        [NotNull]
        [Display("Type", Expand = ExpandRule.Always)]
        public IProceduralModel Type { get; set; }

        public Model GenerateModel(IServiceRegistry services)
        {
            var model = new Model();
            GenerateModel(services, model);
            return model;
        }

        public void GenerateModel(IServiceRegistry services, Model model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            if (Type == null)
            {
                throw new InvalidOperationException("Invalid GeometricPrimitive [{0}]. Expecting a non-null Type");
            }

            Type.Generate(services, model);
        }
    }
}
