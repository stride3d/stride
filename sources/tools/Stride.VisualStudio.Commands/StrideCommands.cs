// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Stride.Core;
using Stride.Core.Assets;
using Stride.VisualStudio.Commands.Shaders;

namespace Stride.VisualStudio.Commands
{
    public class StrideCommands : IStrideCommands
    {
        public StrideCommands()
        {
            PackageSessionPublicHelper.FindAndSetMSBuildVersion();
        }

        public byte[] GenerateShaderKeys(string inputFileName, string inputFileContent)
        {
            var data = Encoding.UTF8.GetBytes("""
                // New shader system use C# analyzer to generate code instead of custom tools.
                // - If using Visual Studio: right-click file, Properties, set "Build Action" to "C# analyzer additional file" and clear value in "Custom Tool".
                //   Also delete the already generated .sdfx.cs and .sdfx.cs files
                // - If editing .csproj manually: switch your .sdsl/.sdfx files in ItemGroup from None to AdditionalFiles in .csproj and remove CodeGenerator
                //   Also delete the already generated .sdfx.cs and .sdfx.cs files and remove them from the .csproj
                #error Shader or Effect file is using old build system. Please use ItemType AdditionalFiles instead of None and remove the Generator metadata
                """);
            return [.. Encoding.UTF8.GetPreamble(), ..data];
        }

        public RawShaderNavigationResult AnalyzeAndGoToDefinition(string projectPath, string sourceCode, RawSourceSpan span)
        {
            return new RawShaderNavigationResult();
        }
    }
}
