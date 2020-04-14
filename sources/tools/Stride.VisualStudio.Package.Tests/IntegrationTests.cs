// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Text;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Xunit;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Templates;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.VisualStudio;
using Stride.Assets.Templates;
using Stride.Assets.Presentation.Templates;
using Stride.Rendering;
using VSLangProj;
using Debugger = System.Diagnostics.Debugger;
using Process = System.Diagnostics.Process;
using Thread = System.Threading.Thread;

namespace Stride.VisualStudio.Package.Tests
{
    /// <summary>
    /// Test class that runs experimental instance of Visual Studio to check our plugin works well.
    /// </summary>
    /// <remarks>
    /// Right now it only has a test for .sdsl C# code generator, but it also tests a lot of things along the way: VSPackage properly have all dependencies (no missing .dll), IStrideCommands can be properly found, etc...
    /// Also, it works against a dev version of Stride, but it could eventually be improved to test against package version as well.
    /// </remarks>
    public class IntegrationTests : IDisposable
    {
        private const string StartArguments = @"/RootSuffix Stride /resetsettings Profiles\General.vssettings";
        private DTE dte;
        private Process process;
        private bool killVisualStudioProcessDuringTearDown;

        public IntegrationTests()
        {
            (dte, process, killVisualStudioProcessDuringTearDown) = InitDTE();
        }

        /// <summary>
        /// Start experimental instance of Visual Studio. This will be killed on cleanup, except if a debugger is attached (in which case it can reuse the existing instance).
        /// </summary>
        private static (DTE, Process, bool) InitDTE()
        {
            bool killVisualStudioProcessDuringTearDown;
            var visualStudioPath = VSLocator.VisualStudioPath;
            Process process;

            // First, we try to check if an existing instance was launched
            var searcher = new ManagementObjectSearcher($"select CommandLine,ProcessId from Win32_Process where ExecutablePath='{visualStudioPath.Replace(@"\", @"\\")}' and CommandLine like '% /RootSuffix Exp'");
            var retObjectCollection = searcher.Get();
            var result = retObjectCollection.Cast<ManagementObject>().FirstOrDefault();
            if (result != null)
            {
                var processId = (uint)result["ProcessId"];
                process = Process.GetProcessById((int)processId);
                killVisualStudioProcessDuringTearDown = false;
            }
            else
            {
                var psi = new ProcessStartInfo
                {
                    FileName = visualStudioPath,
                    WorkingDirectory = Path.GetDirectoryName(visualStudioPath),
                    Arguments = StartArguments,
                    UseShellExecute = false,
                };

                process = Process.Start(psi);
                if (process == null)
                    throw new InvalidOperationException("Could not start Visual Studio instance");

                // Since we are the one starting it, let's close it when we are done
                // (except if a debugger is attached, we assume developer want to iterate several time on it and will exit Visual Studio himself)
                killVisualStudioProcessDuringTearDown = !Debugger.IsAttached;
            }

            // Wait for 60 sec
            for (int i = 0; i < 60; ++i)
            {
                if (process.HasExited)
                    throw new InvalidOperationException($"Visual Studio process {process.Id} exited before we could connect to it");

                var matchingDte = VisualStudioDTE.GetDTEByProcess(process.Id);
                if (matchingDte != null)
                    return (matchingDte, process, killVisualStudioProcessDuringTearDown);

                Thread.Sleep(1000);
            }

            throw new InvalidOperationException($"Could not find the Visual Studio DTE for process {process.Id}, or it didn't start in time");
        }

        private static void CloseDTE(DTE dte, Process process)
        {
            if (dte != null)
            {
                dte.Quit();
                process?.WaitForExit();
            }
            else
            {
                process?.Kill();
            }
        }

        public void Dispose()
        {
            if (killVisualStudioProcessDuringTearDown)
            {
                CloseDTE(dte, process);
            }
        }

        [StaFact]
        public void TestXkslGeneration()
        {
            PackageSessionPublicHelper.FindAndSetMSBuildVersion();

            // Create temporary folder
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);

            // Make sure solution is closed (i.e. previous test failure)
            var solution = (Solution2)dte.Solution;
            solution.Close();

            // Create project
            try
            {
                // Create new game
                var session = GenerateNewGame(tempDirectory);

                solution.Open(session.SolutionPath);

                // Find NewGame.Game project
                var newGameFolder = solution.Projects.OfType<EnvDTE.Project>().First();
                var newGameProject = newGameFolder.ProjectItems.OfType<ProjectItem>().Select(x => x.SubProject).First(x => x.Name == $"{session.Packages.First().Meta.Name}.Game");

                // Add sdsl file
                var sdslItem = newGameProject.ProjectItems.AddFromFileCopy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestGenerator.sdsl"));

                // Make sure custom tool is properly set
                Assert.Equal("StrideShaderKeyGenerator", sdslItem.Properties.Item("CustomTool").Value);

                // Wait for cs file to be generated (up to 5 seconds)
                // TODO: Is there a better way to wait for it?
                for (int i = 0; i < 50; ++i)
                {
                    if (sdslItem.ProjectItems.Count > 0)
                        break;
                    Thread.Sleep(100);
                }

                // Get generated cs file
                Assert.Equal(1, sdslItem.ProjectItems.Count);
                var shaderGeneratedCsharpItem = sdslItem.ProjectItems.OfType<ProjectItem>().First();
                var shaderGeneratedCsharpFile = shaderGeneratedCsharpItem.FileNames[0];

                // Check content
                // Note: we could do more advanced code analysis, but just check a few expected stuff is probably good enough to detect if there was no crash generating it
                var shaderGeneratedCsharpContent = File.ReadAllText(shaderGeneratedCsharpFile);
                Assert.Contains($"{nameof(ValueParameterKey<float>)}<float> TestFloat", shaderGeneratedCsharpContent);
                Assert.Contains($"{nameof(ValueParameterKey<Color3>)}<{nameof(Color3)}> TestColor", shaderGeneratedCsharpContent);
            }
            finally
            {
                solution.Close();

                try
                {
                    Directory.Delete(tempDirectory, true);
                }
                catch
                {
                }
            }
        }

        private static PackageSession GenerateNewGame(string outputFolder)
        {
            // Find the game template description for a new game
            var template = TemplateManager.FindTemplates().FirstOrDefault(matchTemplate => matchTemplate.Id == NewGameTemplateGenerator.TemplateId);
            Assert.NotNull(template);

            var result = new LoggerResult();

            var session = new PackageSession();
            session.SolutionPath = Path.Combine(outputFolder, @"NewGame.sln");
            var parameters = new SessionTemplateGeneratorParameters
            {
                Description = template,
                Logger = result,
                Name = "NewGame",
                OutputDirectory = outputFolder,
                Session = session,
                Unattended = true,
            };

            NewGameTemplateGenerator.SetParameters(parameters, AssetRegistry.SupportedPlatforms.Where(x => x.Type == Core.PlatformType.Windows).Select(x => new SelectedSolutionPlatform(x, null)));

            var templateGenerator = TemplateManager.FindTemplateGenerator(parameters);
            templateGenerator.PrepareForRun(parameters);
            templateGenerator.Run(parameters);

            return session;
        }
    }
}
