// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        var generator = new DotNetNewTemplateGenerator();

        logger.MessageLogged += (sender, eventArgs) => Console.WriteLine(eventArgs.Message.Text);

        var parameters = new SessionTemplateGeneratorParameters { Session = session, Unattended = true };
        DotNetNewTemplateGenerator.SetParameters(parameters, new Dictionary<string, string>
        {
            ["platforms"] = "windows",
        });

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

        StrideDefaultTemplates.Load(loadAssemblyReferences: false);
        var strideTemplates = TemplateManager.FindTemplates(session);

        parameters.Description = strideTemplates.First(x => x.Id == templateGuid);
        parameters.Name = sampleName;
        parameters.Namespace = sampleName;
        parameters.OutputDirectory = outputPath;
        parameters.Logger = logger;

        if (!generator.PrepareForRun(parameters).Result)
            logger.Error("PrepareForRun returned false");

        // Headless: only the file output is needed (sample is built + run as a subprocess);
        // skipping IntegrateIntoSession avoids loading per-template Stride.Particles/etc. assemblies
        // into the AssemblyContainer, which would duplicate-register DataContract aliases across
        // sequential test cases.
        if (!generator.GenerateFilesOnly(parameters))
            logger.Error("GenerateFilesOnly returned false");

        if (logger.HasErrors)
            throw new InvalidOperationException($"Error generating sample {sampleName} from template:\r\n{logger.ToText()}");

        return session;
    }
}
