// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using Mono.Options;
using Stride.ConnectionRouter;
using Stride.Engine.Network;
using Stride.Graphics.Regression;
using static System.String;

namespace Stride.TestRunner
{
    class TestServerHost : RouterServiceServer
    {
        /// <summary>
        /// The name of the branch the test is done on;
        /// </summary>
        private readonly string branchName;

        /// <summary>
        /// The current buildNumber.
        /// </summary>
        private readonly int buildNumber;

        private string resultFile;

        private bool testFailed = true;
        private bool testFinished = false;

        private readonly AutoResetEvent clientResultsEvent = new AutoResetEvent(false);

        public TestServerHost(int bn, string branch) : base("/task/Stride.TestRunner.exe")
        {
            buildNumber = bn;
            branchName = branch;
        }

        public int RunAndroidTest(ConnectedDevice device, bool reinstall, string packageName, string packageFile, string resultFilename)
        {
            resultFile = resultFilename;

            try
            {
                ProcessOutputs adbOutputs;

                var adbPath = AndroidDeviceEnumerator.GetAdbPath();
                if (adbPath == null)
                    throw new InvalidOperationException("Can't find adb");

                // force stop - only works for Android 3.0 and above.
                ShellHelper.RunProcessAndGetOutput(adbPath, $@"-s {device.Serial} shell am force-stop {packageName}");

                if (reinstall)
                {
                    // uninstall
                    ShellHelper.RunProcessAndGetOutput(adbPath, $@"-s {device.Serial} uninstall {packageName}");

                    // install
                    adbOutputs = ShellHelper.RunProcessAndGetOutput(adbPath, $@"-s {device.Serial} install {packageFile}");
                    Console.WriteLine("adb install: exitcode {0}\nOutput: {1}\nErrors: {2}", adbOutputs.ExitCode, adbOutputs.OutputAsString, adbOutputs.ErrorsAsString);
                    if (adbOutputs.ExitCode != 0)
                        throw new InvalidOperationException("Invalid error code from adb install.\n Shell log: {0}");
                }

                // run
                var parameters = new StringBuilder();
                parameters.Append("-s "); parameters.Append(device.Serial);
                parameters.Append(@" shell am start -a android.intent.action.MAIN -n " + packageName + "/nunitlite.tests.MainActivity");
                AddAndroidParameter(parameters, Graphics.Regression.TestRunner.StrideVersion, StrideVersion.NuGetVersion);
                AddAndroidParameter(parameters, Graphics.Regression.TestRunner.StrideBuildNumber, buildNumber.ToString());
                if (!IsNullOrEmpty(branchName))
                    AddAndroidParameter(parameters, Graphics.Regression.TestRunner.StrideBranchName, branchName);
                Console.WriteLine(parameters.ToString());

                adbOutputs = ShellHelper.RunProcessAndGetOutput(adbPath, parameters.ToString());
                Console.WriteLine("adb shell am start: exitcode {0}\nOutput: {1}\nErrors: {2}", adbOutputs.ExitCode, adbOutputs.OutputAsString, adbOutputs.ErrorsAsString);
                if (adbOutputs.ExitCode != 0)
                    throw new InvalidOperationException("Invalid error code from adb shell am start.");

                if (!clientResultsEvent.WaitOne(TimeSpan.FromSeconds(300))) //wait 30 seconds for client connection
                {
                    Console.WriteLine(@"Device failed to connect.");
                    return -1;
                }

                Console.WriteLine(@"Device client connected, waiting for test results...");

                // if we receive no events during more than 5 minutes, something is wrong
                // we also check that test session is not finished as well
                while (clientResultsEvent.WaitOne(TimeSpan.FromMinutes(5)) && !testFinished)
                {
                }

                return testFailed ? -1 : 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(@"An error was thrown when running the test on Android: {0}", e);
                return -1;
            }
        }

        /// <summary>
        /// Add the parameter as an extra in an Android launch command line
        /// </summary>
        /// <param name="builder">The string builder.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="parameterValue">The value of the parameter.</param>
        private static void AddAndroidParameter(StringBuilder builder, string parameterName, string parameterValue)
        {
            builder.Append(@" -e ");
            builder.Append(parameterName);
            builder.Append(@" ");
            builder.Append(parameterValue);
        }

        /// <summary>
        /// A structure to store information about the connected test devices.
        /// </summary>
        public struct ConnectedDevice
        {
            public string Serial;
            public string Name;
            public TestPlatform Platform;

            public override string ToString()
            {
                return Name + " " + Serial + " " + PlatformPermutator.GetPlatformName(Platform);
            }
        }

        protected override async void HandleClient(SimpleSocket clientSocket, string url)
        {
            clientResultsEvent.Set();

            await AcceptConnection(clientSocket);

            try
            {
                var binaryReader = new BinaryReader(clientSocket.ReadStream);

                //Read events
                TestRunnerMessageType messageType;
                do
                {
                    messageType = (TestRunnerMessageType)binaryReader.ReadInt32();
                    switch (messageType)
                    {
                        case TestRunnerMessageType.TestStarted:
                        {
                            var testFullName = binaryReader.ReadString();
                            Console.WriteLine($"Test Started: {testFullName}");
                            clientResultsEvent.Set();
                            break;
                        }
                        case TestRunnerMessageType.TestFinished:
                        {
                            var testFullName = binaryReader.ReadString();
                            var status = binaryReader.ReadString();
                            Console.WriteLine($"Test {status}: {testFullName}");
                            clientResultsEvent.Set();
                            break;
                        }
                        case TestRunnerMessageType.TestOutput:
                        {
                            var outputType = binaryReader.ReadString();
                            var outputText = binaryReader.ReadString();
                            Console.WriteLine($"  {outputType}: {outputText}");
                            clientResultsEvent.Set();
                            break;
                        }
                        case TestRunnerMessageType.SessionSuccess:
                            testFailed = false;
                            break;
                        case TestRunnerMessageType.SessionFailure:
                            testFailed = true;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                } while (messageType != TestRunnerMessageType.SessionFailure && messageType != TestRunnerMessageType.SessionSuccess);

                // Mark test session as finished
                testFinished = true;

                //Read output
                var output = binaryReader.ReadString();
                Console.WriteLine(output);

                // Read XML result
                var result = binaryReader.ReadString();
                Console.WriteLine(result);

                // Write XML result to disk
                File.WriteAllText(resultFile, result);

                clientResultsEvent.Set();
            }
            catch (Exception)
            {
                clientResultsEvent.Set();
                Console.WriteLine(@"Client disconnected before sending results, a fatal crash might have occurred.");
            }
        }
    }

    class Program
    {
        static int Main(string[] args)
        {
            var exeName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
            var showHelp = false;
            int exitCode;
            string resultPath = "TestResults";
            bool reinstall = true;

            var p = new OptionSet
            {
                "Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp) All Rights Reserved", "Stride Test Suite Tool - Version: " + Format("{0}.{1}.{2}", typeof(Program).Assembly.GetName().Version.Major, typeof(Program).Assembly.GetName().Version.Minor, typeof(Program).Assembly.GetName().Version.Build) + Empty, Format("Usage: {0} [assemblies|apk] -option1 -option2:a", exeName), Empty, "=== Options ===", Empty, { "h|help", "Show this message and exit", v => showHelp = v != null }, { "result-path:", "Result .XML output path", v => resultPath = v }, { "no-reinstall-apk", "Do not reinstall APK", v => reinstall = false },
            };

            try
            {
                var commandArgs = p.Parse(args);
                if (showHelp)
                {
                    p.WriteOptionDescriptions(Console.Out);
                    return 0;
                }

                // Make sure path exists
                Directory.CreateDirectory(resultPath);
                exitCode = BuildAndRunAndroidTests(commandArgs, reinstall, resultPath);
            }
            catch (Exception e)
            {
                Console.WriteLine(@"{0}: {1}", exeName, e);
                if (e is OptionException)
                    p.WriteOptionDescriptions(Console.Out);
                exitCode = 1;
            }

            return exitCode;
        }

        private static int BuildAndRunAndroidTests(List<string> commandArgs, bool reinstall, string resultPath)
        {
            if (commandArgs.Count == 0)
                throw new OptionException("One APK should be provided", "apk");

            // get build number
            int buildNumber;
            if (!Int32.TryParse(Environment.GetEnvironmentVariable("STRIDE_BUILD_NUMBER"), out buildNumber))
                buildNumber = -1;

            // get branch name
            var branchName = Environment.GetEnvironmentVariable("STRIDE_BRANCH_NAME");

            var exitCode = 0;

            foreach (var packageFile in commandArgs)
            {
                if (!packageFile.EndsWith("-Signed.apk"))
                    throw new OptionException("APK should end up with \"-Signed.apk\"", "apk");

                // Remove -Signed.apk suffix
                var packageName = Path.GetFileName(packageFile);
                packageName = packageName.Replace("-Signed.apk", Empty);

                var androidDevices = AndroidDeviceEnumerator.ListAndroidDevices();
                if (androidDevices.Length == 0)
                    throw new InvalidOperationException("Could not find any Android device connected.");

                foreach (var device in androidDevices)
                {
                    var testServerHost = new TestServerHost(buildNumber, branchName);
                    testServerHost.TryConnect("127.0.0.1", RouterClient.DefaultPort, true).Wait();
                    Directory.CreateDirectory(resultPath);
                    var deviceResultFile = Path.Combine(resultPath, "TestResult_" + packageName + "_Android_" + device.Name + "_" + device.Serial + ".xml");

                    var currentExitCode = testServerHost.RunAndroidTest(new TestServerHost.ConnectedDevice
                    {
                        Name = device.Name, Serial = device.Serial, Platform = TestPlatform.Android,
                    }, reinstall, packageName, packageFile, deviceResultFile);

                    var adbPath = AndroidDeviceEnumerator.GetAdbPath();

                    // force stop - only works for Android 3.0 and above.
                    ShellHelper.RunProcessAndGetOutput(adbPath, $@"-s {device.Serial} shell am force-stop {packageName}");

                    if (currentExitCode != 0)
                        exitCode = currentExitCode;
                }
            }

            return exitCode;
        }
    }
}
