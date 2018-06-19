// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Text;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NUnit.Framework;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Templates;
using Xenko.Core.Diagnostics;
using Xenko.Core.IO;
using Xenko.Core.Mathematics;
using Xenko.Core.VisualStudio;
using Xenko.Assets.Templates;
using Xenko.Assets.Presentation.Templates;
using Xenko.Rendering;
using VSLangProj;
using Debugger = System.Diagnostics.Debugger;
using Process = System.Diagnostics.Process;
using Thread = System.Threading.Thread;

namespace Xenko.VisualStudio.Package.Tests
{
    /// <summary>
    /// Test class that runs experimental instance of Visual Studio to check our plugin works well.
    /// </summary>
    /// <remarks>
    /// Right now it only has a test for .xksl C# code generator, but it also tests a lot of things along the way: VSPackage properly have all dependencies (no missing .dll), IXenkoCommands can be properly found, etc...
    /// Also, it works against a dev version of Xenko, but it could eventually be improved to test against package version as well.
    /// </remarks>
    [TestFixture, Apartment(System.Threading.ApartmentState.STA)]
    public class IntegrationTests
    {
        private const string StartArguments = @"/RootSuffix Xenko /resetsettings Profiles\General.vssettings";
        private STAContext context = new STAContext();
        private DTE dte;
        private Process process;
        private bool killVisualStudioProcessDuringTearDown;

        /// <summary>
        /// Start experimental instance of Visual Studio. This will be killed on cleanup, except if a debugger is attached (in which case it can reuse the existing instance).
        /// </summary>
        [OneTimeSetUp]
        public void Init()
        {
            var visualStudioPath = VSLocator.VisualStudioPath;

            dte = context.Execute(() =>
            {
                // First, we try to check if an existing instance was launched
                var searcher = new ManagementObjectSearcher($"select CommandLine,ProcessId from Win32_Process where ExecutablePath='{visualStudioPath.Replace(@"\", @"\\")}' and CommandLine like '% /RootSuffix Exp'");
                var retObjectCollection = searcher.Get();
                var result = retObjectCollection.Cast<ManagementObject>().FirstOrDefault();
                if (result != null)
                {
                    var processId = (uint)result["ProcessId"];
                    process = Process.GetProcessById((int)processId);
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
                    
                    // Override Xenko dir with path relative to test directory
                    psi.EnvironmentVariables["XenkoDir"] = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..");

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
                    Thread.Sleep(1000);

                    if (process.HasExited)
                        throw new InvalidOperationException($"Visual Studio process {process.Id} exited before we could connect to it");

                    var matchingDte = VisualStudioDTE.GetDTEByProcess(process.Id);
                    if (matchingDte != null)
                        return matchingDte;
                }

                throw new InvalidOperationException($"Could not find the Visual Studio DTE for process {process.Id}, or it didn't start in time");
            });
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            if (killVisualStudioProcessDuringTearDown)
            {
                if (dte != null)
                {
                    dte.Quit();
                    process.WaitForExit();
                }
                else
                {
                    process?.Kill();
                }
            }
            context?.Dispose();
            context = null;
        }

        [SetUp]
        public void InitEachTest()
        {
            // Make sure solution is closed (i.e. previous test failure)
            var solution = (Solution2)dte.Solution;
            solution.Close();
        }

        [Test]
        public void TestXkslGeneration()
        {
            PackageSessionPublicHelper.FindAndSetMSBuildVersion();

            // Create temporary folder
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);

            // Create project
            var solution = (Solution2)dte.Solution;
            try
            {
                // Create new game
                var session = GenerateNewGame(tempDirectory);

                solution.Open(session.SolutionPath);

                // Find NewGame.Game project
                var newGameFolder = solution.Projects.OfType<EnvDTE.Project>().First();
                var newGameProject = newGameFolder.ProjectItems.OfType<ProjectItem>().Select(x => x.SubProject).First(x => x.Name == $"{session.Packages.First().Meta.Name}.Game");

                // Add xksl file
                var xkslItem = newGameProject.ProjectItems.AddFromFileCopy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestGenerator.xksl"));

                // Make sure custom tool is properly set
                Assert.AreEqual("XenkoShaderKeyGenerator", xkslItem.Properties.Item("CustomTool").Value);

                // Get generated cs file
                Assert.AreEqual(1, xkslItem.ProjectItems.Count);
                var shaderGeneratedCsharpItem = xkslItem.ProjectItems.OfType<ProjectItem>().First();
                var shaderGeneratedCsharpFile = shaderGeneratedCsharpItem.FileNames[0];

                // Check content
                // Note: we could do more advanced code analysis, but just check a few expected stuff is probably good enough to detect if there was no crash generating it
                var shaderGeneratedCsharpContent = File.ReadAllText(shaderGeneratedCsharpFile);
                Assert.That(shaderGeneratedCsharpContent.Contains($"{nameof(ValueParameterKey<float>)}<float> TestFloat"));
                Assert.That(shaderGeneratedCsharpContent.Contains($"{nameof(ValueParameterKey<Color3>)}<{nameof(Color3)}> TestColor"));
            }
            finally
            {
                solution.Close();
                Directory.Delete(tempDirectory, true);
            }
        }

        private static PackageSession GenerateNewGame(string outputFolder)
        {
            // Find the game template description for a new game
            var template = TemplateManager.FindTemplates().FirstOrDefault(matchTemplate => matchTemplate.Id == NewGameTemplateGenerator.TemplateId);
            Assert.IsNotNull(template);

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
