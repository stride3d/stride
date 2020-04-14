using Xenko.Core;
using Xenko.Graphics;

namespace Xenko.Rendering
{
    /// <summary>
    /// Describes a single rendering pass of a <see cref="Rendering.Material"/>.
    /// </summary>
    [DataContract]
    public class MaterialPass
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialPass"/> class.
        /// </summary>
        public MaterialPass()
        {
            Parameters = new ParameterCollection();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialPass"/> class.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        public MaterialPass(ParameterCollection parameters)
        {
            Parameters = parameters;
        }

        /// <summary>
        /// The material that contains this pass.
        /// </summary>
        [DataMemberIgnore]
        public Material Material { get; internal set; }

        /// <summary>
        /// Gets or sets the parameters.
        /// </summary>
        /// <value>The parameters.</value>
        public ParameterCollection Parameters { get; set; }

        /// <summary>
        /// Overrides the cullmode for this material.
        /// </summary>
        public CullMode? CullMode;

        /// <summary>
        /// Overrides the blend state for this material.
        /// </summary>
        public BlendStateDescription? BlendState;

        /// <summary>
        /// The tessellation method used by the material.
        /// </summary>
        public XenkoTessellationMethod TessellationMethod;

        /// <summary>
        /// Gets or sets a value indicating whether this instance has transparent.
        /// </summary>
        /// <value><c>true</c> if this instance has transparent; otherwise, <c>false</c>.</value>
        public bool HasTransparency { get; set; }

        /// <summary>
        /// Determines if this material is affected by lighting.
        /// </summary>
        /// <value><c>true</c> if this instance affects lighting; otherwise, <c>false</c>.</value>
        public bool IsLightDependent { get; set; }

        /// <summary>
        /// The index of this pass as part of its containing <see cref="Material"/>.
        /// </summary>
        /// <remarks>
        /// Used for state sorting.
        /// </remarks>
        public int PassIndex { get; set; }
    }
}