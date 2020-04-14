// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;

namespace Stride.Core.Assets.Templates
{
    public delegate bool RunGeneratorDelegate();

    /// <summary>
    /// The interface to represent a template generator.
    /// </summary>
    public interface ITemplateGenerator
    {
        /// <summary>
        /// Determines whether this generator is supporting the specified template
        /// </summary>
        /// <param name="templateDescription">The template description.</param>
        /// <returns><c>true</c> if this generator is supporting the specified template; otherwise, <c>false</c>.</returns>
        bool IsSupportingTemplate(TemplateDescription templateDescription);
    }

    /// <summary>
    /// The interface to represent a template generator.
    /// </summary>
    /// <typeparam name="TParameters">The type of parameters this generator uses.</typeparam>
    public interface ITemplateGenerator<in TParameters> : ITemplateGenerator where TParameters : TemplateGeneratorParameters
    {
        /// <summary>
        /// Prepares this generator with the specified parameters.
        /// </summary>
        /// <param name="parameters">The parameters for the template generator.</param>
        /// <remarks>This method should be used to verify that the parameters are correct, and to ask user for additional
        /// information before running the template.
        /// </remarks>
        /// <returns>A task completing when the preparation is finished, with the result <c>True</c> if the preparation was successful, <c>false</c> otherwise.</returns>
        Task<bool> PrepareForRun(TParameters parameters);

        /// <summary>
        /// Runs the generator with the given parameter.
        /// </summary>
        /// <param name="parameters">The parameters for the template generator.</param>
        /// <remarks>
        /// This method should work in unattended mode and should not ask user for information anymore.
        /// </remarks>
        /// <returns><c>True</c> if the generation was successful, <c>false</c> otherwise.</returns>
        bool Run(TParameters parameters);
    }
}
