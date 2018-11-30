// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xenko.Assets.Presentation;
using Xenko.Assets.Presentation.Templates;
using Xenko.Assets.Templates;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Templates;
using Xenko.Core.Diagnostics;
using Xenko.Core.IO;

namespace Xenko.Samples.Tests
{
    public class SampleTestFixture : IDisposable
    {
        public SampleTestFixture(UDirectory outputPath, Guid templateGuid)
        {
            // Setup MSBuild
            PackageSessionPublicHelper.FindAndSetMSBuildVersion();

            var logger = new LoggerResult();
            var sampleName = outputPath.GetDirectoryName();

            var session = GenerateSample(outputPath, templateGuid, sampleName, logger);
            CompileSample(logger, sampleName, session);
        }

        private static void CompileSample(LoggerResult logger, string sampleName, PackageSession session)
        {
            var project = session.Projects.OfType<SolutionProject>().First(x => x.Platform == Core.PlatformType.Windows);

            var buildResult = VSProjectHelper.CompileProjectAssemblyAsync(null, project.FullPath, logger, extraProperties: new Dictionary<string, string> { { "XenkoAutoTesting", "true" } }).BuildTask.Result;
            if (logger.HasErrors)
            {
                throw new InvalidOperationException($"Error compiling sample {sampleName}:\r\n{logger.ToText()}");
            }
        }

        private static PackageSession GenerateSample(UDirectory outputPath, Guid templateGuid, string sampleName, LoggerResult logger)
        {
            // Make output path absolute
            if (!outputPath.IsAbsolute)
                outputPath = UPath.Combine(Environment.CurrentDirectory, outputPath);

            Console.WriteLine(@"Bootstrapping: " + sampleName);

            var session = new PackageSession();
            var generator = TemplateSampleGenerator.Default;

            // Ensure progress is shown while it is happening.
            logger.MessageLogged += (sender, eventArgs) => Console.WriteLine(eventArgs.Message.Text);

            var parameters = new SessionTemplateGeneratorParameters { Session = session };
            parameters.Unattended = true;
            TemplateSampleGenerator.SetParameters(
                parameters,
                AssetRegistry.SupportedPlatforms.Where(x => x.Type == Core.PlatformType.Windows).Select(x => new SelectedSolutionPlatform(x, x.Templates.FirstOrDefault())).ToList(),
                addGamesTesting: true);

            session.SolutionPath = UPath.Combine<UFile>(outputPath, sampleName + ".sln");

            // Properly delete previous version
            if (Directory.Exists(outputPath))
            {
                try
                {
                    Directory.Delete(outputPath, true);
                }
                catch (Exception)
                {
                    logger.Warning($"Unable to delete directory [{outputPath}]");
                }
            }

            // Load templates
            XenkoDefaultAssetsPlugin.LoadDefaultTemplates();
            var xenkoTemplates = TemplateManager.FindTemplates(session);

            parameters.Description = xenkoTemplates.First(x => x.Id == templateGuid);
            parameters.Name = sampleName;
            parameters.Namespace = sampleName;
            parameters.OutputDirectory = outputPath;
            parameters.Logger = logger;

            if (!generator.PrepareForRun(parameters).Result)
                logger.Error("PrepareForRun returned false for the TemplateSampleGenerator");

            if (!generator.Run(parameters))
                logger.Error("Run returned false for the TemplateSampleGenerator");

            var updaterTemplate = xenkoTemplates.First(x => x.FullPath.ToString().EndsWith("UpdatePlatforms.xktpl"));
            parameters.Description = updaterTemplate;

            if (logger.HasErrors)
            {
                throw new InvalidOperationException($"Error generating sample {sampleName} from template:\r\n{logger.ToText()}");
            }

            return session;
        }

        public void Dispose()
        {

        }
    }
}
