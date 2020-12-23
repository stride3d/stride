// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Utility;
using Stride.Shaders.Parser;
using Stride.Shaders.Parser.Mixins;
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
            return ShaderKeyFileHelper.GenerateCode(inputFileName, inputFileContent);
        }

        public RawShaderNavigationResult AnalyzeAndGoToDefinition(string projectPath, string sourceCode, RawSourceSpan span)
        {
            var rawResult = new RawShaderNavigationResult();

            var navigation = new ShaderNavigation();

            var shaderDirectories = CollectShadersDirectories(projectPath);

            if (span.File != null)
            {
                var dirName = Path.GetDirectoryName(span.File);
                if (dirName != null)
                {
                    shaderDirectories.Add(dirName);
                }
            }

            var resultAnalysis = navigation.AnalyzeAndGoToDefinition(sourceCode, new Stride.Core.Shaders.Ast.SourceLocation(span.File, 0, span.Line, span.Column), shaderDirectories);

            if (resultAnalysis.DefinitionLocation.Location.FileSource != null)
            {
                rawResult.DefinitionSpan = ConvertToRawLocation(resultAnalysis.DefinitionLocation);
            }

            foreach (var message in resultAnalysis.Messages.Messages)
            {
                rawResult.Messages.Add(ConvertToRawMessage(message));
            }

            return rawResult;
        }

        private static RawSourceSpan ConvertToRawLocation(SourceSpan span)
        {
            return new RawSourceSpan()
            {
                File = span.Location.FileSource,
                Line = span.Location.Line,
                EndLine = span.Location.Line,
                Column = span.Location.Column,
                EndColumn = span.Location.Column + span.Length
            };
        }

        private static RawShaderAnalysisMessage ConvertToRawMessage(ReportMessage message)
        {
            return new RawShaderAnalysisMessage()
            {
                Span = ConvertToRawLocation(message.Span),
                Text = message.Text,
                Code = message.Code,
                Type = ConvertToStringLevel(message.Level)
            };
        }

        private static string ConvertToStringLevel(ReportMessageLevel level)
        {
            return level.ToString().ToLowerInvariant();
        }

        private List<string> CollectShadersDirectories(string packagePath)
        {
            if (packagePath == null)
            {
                packagePath = PackageStore.Instance.GetPackageFileName("Stride.Engine", new PackageVersionRange(new PackageVersion(StrideVersion.NuGetVersion)));
            }

            var defaultLoad = PackageLoadParameters.Default();
            defaultLoad.AutoCompileProjects = false;
            defaultLoad.AutoLoadTemporaryAssets = false;
            defaultLoad.GenerateNewAssetIds = false;
            defaultLoad.LoadAssemblyReferences = false;

            var sessionResult = PackageSession.Load(packagePath, defaultLoad);

            if (sessionResult.HasErrors)
            {
                // TODO: Throw an error
                return null;
            }

            var session = sessionResult.Session;

            var assetsPaths = new List<string>();
            foreach (var package in session.Packages)
            {
                foreach (var assetFolder in package.AssetFolders)
                {
                    var fullPath = assetFolder.Path.ToWindowsPath();
                    if (Directory.Exists(fullPath))
                    {
                        assetsPaths.Add(fullPath);
                        assetsPaths.AddRange(Directory.EnumerateDirectories(fullPath, "*.*", SearchOption.AllDirectories));
                    }
                }
            }
            return assetsPaths;
        }
    }
}
