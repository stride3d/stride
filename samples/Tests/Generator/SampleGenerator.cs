// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using Stride.Assets.Presentation;
using Stride.Assets.Presentation.Templates;
using Stride.Assets.Templates;
using Stride.Core.Assets;
using Stride.Core.Assets.Templates;
using Stride.Core.Diagnostics;
using Stride.Core.IO;

namespace Stride.Samples.Generator;

/// <summary>
/// Regenerates a sample from its template GUID into <paramref name="outputPath"/>. Returns the
/// resulting <see cref="PackageSession"/> so callers can inspect or build the generated projects.
/// Caller is responsible for arranging the MSBuild environment via
/// <c>PackageSessionPublicHelper.FindAndSetMSBuildVersion()</c> before invoking this.
/// </summary>
public static class SampleGenerator
{
    public static PackageSession Generate(UDirectory outputPath, Guid templateGuid, string sampleName, LoggerResult logger)
    {
        if (!outputPath.IsAbsolute)
            outputPath = UPath.Combine(Environment.CurrentDirectory, outputPath);

        Console.WriteLine($"Bootstrapping: {sampleName}");

        var session = new PackageSession();
        var generator = TemplateSampleGenerator.Default;

        logger.MessageLogged += (sender, eventArgs) => Console.WriteLine(eventArgs.Message.Text);

        var parameters = new SessionTemplateGeneratorParameters { Session = session, Unattended = true };
        TemplateSampleGenerator.SetParameters(
            parameters,
            AssetRegistry.SupportedPlatforms
                .Where(x => x.Type == Core.PlatformType.Windows)
                .Select(x => new SelectedSolutionPlatform(x, x.Templates.FirstOrDefault()))
                .ToList());

        session.SolutionPath = UPath.Combine<UFile>(outputPath, sampleName + ".sln");

        if (Directory.Exists(outputPath))
        {
            try
            {
                Directory.Delete(outputPath, recursive: true);
            }
            catch (Exception)
            {
                logger.Warning($"Unable to delete directory [{outputPath}]");
            }
        }

        StrideDefaultAssetsPlugin.LoadDefaultTemplates();
        var strideTemplates = TemplateManager.FindTemplates(session);

        parameters.Description = strideTemplates.First(x => x.Id == templateGuid);
        parameters.Name = sampleName;
        parameters.Namespace = sampleName;
        parameters.OutputDirectory = outputPath;
        parameters.Logger = logger;

        if (!generator.PrepareForRun(parameters).Result)
            logger.Error("PrepareForRun returned false for the TemplateSampleGenerator");

        if (!generator.Run(parameters))
            logger.Error("Run returned false for the TemplateSampleGenerator");

        // Run the platforms updater so the generated csproj/template structure matches the latest sdtpl pass.
        var updaterTemplate = strideTemplates.First(x => x.FullPath.ToString().EndsWith("UpdatePlatforms.sdtpl", StringComparison.Ordinal));
        parameters.Description = updaterTemplate;

        if (logger.HasErrors)
            throw new InvalidOperationException($"Error generating sample {sampleName} from template:\r\n{logger.ToText()}");

        return session;
    }
}
