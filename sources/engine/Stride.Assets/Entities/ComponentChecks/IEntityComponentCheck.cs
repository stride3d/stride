using System;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Engine;

namespace Stride.Assets.Entities.ComponentChecks
{
    /// <summary>
    /// Interface for component checks executed during asset compilation.
    /// </summary>
    public interface IEntityComponentCheck
    {
        /// <summary>
        /// A predicate determining if a component can be passed to <see cref="Check(EntityComponent)"/>.
        /// </summary>
        /// <param name="componentType">Type of the component to be checked.</param>
        /// <returns>Returns <c>true</c> if the component of <paramref name="componentType"/> can be passed to <see cref="Check(EntityComponent)"/>.</returns>
        bool AppliesTo(Type componentType);

        /// <summary>
        /// Checks if the component state is valid and reports appropriate errors/warnings.
        /// </summary>
        /// <param name="component">Component to check.</param>
        /// <param name="entity">Entity the <paramref name="component"/> is associated with.</param>
        /// <param name="assetItem">Asset item the <paramref name="entity"/> belongs to.</param>
        /// <param name="targetUrlInStorage">URL of the <paramref name="assetItem"/>.</param>
        /// <param name="result">Logger result to write information to.</param>
        void Check(EntityComponent component, Entity entity, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result);
    }
}
