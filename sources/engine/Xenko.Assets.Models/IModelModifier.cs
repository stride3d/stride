using Xenko.Core.BuildEngine;
using Xenko.Rendering;

namespace Xenko.Assets.Models
{
    /// <summary>
    /// Apply various modification to a <see cref="Model"/> during compilation of a <see cref="ModelAsset"/>.
    /// </summary>
    public interface IModelModifier
    {
        /// <summary>
        /// Used for hashing.
        /// </summary>
        int Version { get; }

        /// <summary>
        /// Apply the modifications to the model.
        /// </summary>
        /// <param name="commandContext"></param>
        /// <param name="model"></param>
        void Apply(ICommandContext commandContext, Model model);
    }
}
