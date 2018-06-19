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

            Console.WriteLine(@"Bootstrapping: " + args[0]);

            // When running the SamplesBootstrapper from within Visual Studio and XenkoDir
            // points to your installation of Xenko and not the development checkout, make sure to override
            // the environment variable properly otherwise it will get the samples from the Xenko installation.
            var xenkoDir = Environment.GetEnvironmentVariable("XenkoDir");
            var xenkoPkgPath = UPath.Combine(xenkoDir, new UFile("Xenko.xkpkg"));

            Console.WriteLine("Loading " + xenkoPkgPath + "...");
            var session = new PackageSession();

            var xenkoPkg = PackageStore.Instance.DefaultPackage;

            var generator = TemplateSampleGenerator.Default;

            var logger = new LoggerResult();
            // Ensure progress is shown while it is happening.
            logger.MessageLogged += (sender, eventArgs) => Console.WriteLine(eventArgs.Message.Text);

            var parameters = new SessionTemplateGeneratorParameters { Session = session };
            parameters.Unattended = true;
            TemplateSampleGenerator.SetParameters(parameters, AssetRegistry.SupportedPlatforms.Select(x => new SelectedSolutionPlatform(x, x.Templates.FirstOrDefault())).ToList());

            var outputPath = UPath.Combine(new UDirectory(xenkoDir), new UDirectory("samplesGenerated"));
            outputPath = UPath.Combine(outputPath, new UDirectory(args[0]));

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
