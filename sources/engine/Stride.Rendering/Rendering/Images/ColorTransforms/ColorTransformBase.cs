// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Reflection;

using Stride.Core;

namespace Stride.Rendering.Images
{
    /// <summary>
    /// Base class for a <see cref="ColorTransformBase"/> used by <see cref="ColorTransform"/> and <see cref="ToneMapOperator"/>.
    /// </summary>
    [DataContract(Inherited = true)]
    public abstract class ColorTransformBase
    {
        private ParameterCollection.CompositionCopier parameterCompositionCopier;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorTransformBase" /> class.
        /// </summary>
        /// <param name="colorTransformShader">Name of the shader.</param>
        /// <exception cref="System.ArgumentNullException">shaderName</exception>
        protected ColorTransformBase(string colorTransformShader)
        {
            if (colorTransformShader == null) throw new ArgumentNullException("colorTransformShader");
            Parameters = new ParameterCollection();

            // Initialize all Parameters with values coming from each ParameterKey
            InitializeProperties();

            Shader = colorTransformShader;
        }

        /// <summary>
        /// Gets the group this <see cref="ColorTransformBase" /> is associated with.
        /// </summary>
        [DataMemberIgnore]
        public ColorTransformGroup Group { get; internal set; }

        /// <summary>
        /// Gets or sets the name of the shader.
        /// </summary>
        /// <value>The name of the shader.</value>
        [DataMemberIgnore]
        public string Shader
        {
            get
            {
                return Parameters.Get(ColorTransformKeys.Shader);
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                Parameters.Set(ColorTransformKeys.Shader, value);
                Group?.NotifyPermutationChange();
            }
        }

        /// <summary>
        /// Gets or sets the generic arguments used by the shader. Default is null.
        /// </summary>
        /// <value>The generic arguments used by the shader. Default is null</value>
        [DataMemberIgnore]
        public object[] GenericArguments
        {
            get
            {
                return Parameters.Get(ColorTransformKeys.GenericArguments);
            }
            set
            {
                Parameters.Set(ColorTransformKeys.GenericArguments, value);
                Group?.NotifyPermutationChange();
            }
        }

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <value>The parameters.</value>
        [DataMemberIgnore]
        public ParameterCollection Parameters { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ColorTransformBase"/> is enabled.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        [DataMember(5)]
        [DefaultValue(true)]
        public virtual bool Enabled
        {
            get
            {
                return Parameters.Get(ColorTransformKeys.Enabled);
            }
            set
            {
                Parameters.Set(ColorTransformKeys.Enabled, value);
            }
        }

        /// <summary>
        /// Prepare copy operations for parameters.
        /// </summary>
        public virtual void PrepareParameters(ColorTransformContext context, ParameterCollection parentCollection, string keyRoot)
        {
            // Save Group aside
            Group = context.Group;

            // Compute associated layout ranges
            parameterCompositionCopier = new ParameterCollection.CompositionCopier();
            parameterCompositionCopier.Compile(parentCollection, Parameters, keyRoot);
        }

        /// <summary>
        /// Updates the parameters for this transformation.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <remarks>This method is called just before rendering the ColorTransformGroup that is holding this ColorTransformBase</remarks>
        public virtual void UpdateParameters(ColorTransformContext context)
        {
            parameterCompositionCopier.Copy(Parameters);
        }

        private void InitializeProperties()
        {
            foreach (var property in GetType().GetRuntimeProperties())
            {
                if (property.CanRead && property.CanWrite)
                {
                    property.SetValue(this, property.GetValue(this));
                }
            }
        }
    }
}
