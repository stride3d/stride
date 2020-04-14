// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Assets.Compiler
{
    /// <summary>
    /// Interface for compiling a package.
    /// </summary>
    public interface IPackageCompiler
    {
        /// <summary>
        /// Prepares a package with the specified compiler context.
        /// </summary>
        /// <param name="compilerContext">The compiler context.</param>
        /// <returns>Result of compilation.</returns>
        AssetCompilerResult Prepare(AssetCompilerContext compilerContext);
    }
}
