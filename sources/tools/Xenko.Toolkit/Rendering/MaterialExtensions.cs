using System;
using Xenko.Rendering;

namespace Xenko.Toolkit.Rendering
{
    /// <summary>
    /// Extension methods for <see cref="Material"/>.
    /// </summary>
    public static class MaterialExtensions
    {
        /// <summary>
        /// Clone the <see cref="Material"/>.
        /// </summary>
        /// <param name="material">The material to clone.</param>
        /// <returns>The cloned material.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="material"/> is <see langword="null"/>.</exception>
        public static Material Clone(this Material material)
        {
            if (material == null)
            {
                throw new ArgumentNullException(nameof(material));
            }

            var clone = new Material();

            CopyProperties(material, clone);

            return clone;
        }

        internal static void CopyProperties(Material material, Material clone)
        {
            foreach (var pass in material.Passes)
            {
                clone.Passes.Add(new MaterialPass(new ParameterCollection(pass.Parameters))
                {
                    HasTransparency = pass.HasTransparency,
                    BlendState = pass.BlendState,
                    CullMode = pass.CullMode,
                    IsLightDependent = pass.IsLightDependent
                });
            }
        }
    }
}
