// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.VisualStudio.Commands.Shaders;

namespace Stride.VisualStudio.Commands
{
    /// <summary>
    /// Describes stride commands accessed by VS Package to current stride package (so that VSPackage doesn't depend on Stride assemblies).
    /// </summary>
    /// <remarks>
    /// WARNING: Removing any of those methods will likely break backwards compatibility!
    /// </remarks>
    public interface IStrideCommands
    {
        byte[] GenerateShaderKeys(string inputFileName, string inputFileContent);

        RawShaderNavigationResult AnalyzeAndGoToDefinition(string projectPath, string sourceCode, RawSourceSpan span);
    }
}
