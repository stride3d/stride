// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Assets;
using Xenko.Core.Assets.Templates;
using Xenko.Core.Diagnostics;
using Xenko.Core.IO;
using Xenko.Assets.Presentation.Templates;
using System;
using System.Linq;
using Xenko.Assets.Templates;
using System.IO;

namespace Xenko.SamplesBootstrapper
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Invalid number of arguments.\n");
                Console.WriteLine("Usage: Xenko.SamplesBootstrapper.exe SampleName SampleGuid");
                return 1;
            }

            PackageSessionPublicHelper.FindAndSetMSBuildVersion();

            Console.WriteLine(@"Bootstrapping: " + args[0]);

            var session = new PackageSession();
            var xenkoPkg = PackageStore.Instance.DefaultPackage;
            Console.WriteLine("Using Xenko from " + xenkoPkg.FullPath + "...");
            var xenkoDir = Path.GetDirectoryName(xenkoPkg.FullPath);

            var generator = TemplateSampleGenerator.Default;

            var logger = new LoggerResult();
            // Ensure progress is shown while it is happening.
            logger.MessageLogged += (sender, eventArgs) => Console.WriteLine(eventArgs.Message.Text);

            var parameters = new SessionTemplateGeneratorParameters { Session = session };
            parameters.Unattended = true;
            TemplateSampleGenerator.SetParameters(parameters, AssetRegistry.SupportedPlatforms.Select(x => new SelectedSolutionPlatform(x, x.Templates.FirstOrDefault())).ToList());

            var outputPath = UPath.Combine(new UDirectory(xenkoDir), new UDirectory("samplesGenerated"));
            outputPath = UPath.Combine(outputPath, new UDirectory(args[0]));
            session.SolutionPath = UPath.Combine<UFile>(outputPath, args[0] + ".sln");

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

            var xenkoTemplates = xenkoPkg.Templates;
            parameters.Description = xenkoTemplates.First(x => x.Id == new Guid(args[1]));
            parameters.Name = args[0];
            parameters.Namespace = args[0];
            parameters.OutputDirectory = outputPath;
            parameters.Logger = logger;

            if (!generator.PrepareForRun(parameters).Result)
                logger.Error("PrepareForRun returned false for the TemplateSampleGenerator");

            if (!generator.Run(parameters))
                logger.Error("Run returned false for the TemplateSampleGenerator");

            var updaterTemplate = xenkoTemplates.First(x => x.FullPath.ToString().EndsWith("UpdatePlatforms.xktpl"));
            parameters.Description = updaterTemplate;

            return logger.HasErrors ? 1 : 0;
        }
    }
}
