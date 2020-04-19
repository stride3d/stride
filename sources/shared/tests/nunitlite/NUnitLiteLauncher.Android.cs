// ***********************************************************************
// Copyright (c) 2009 Charlie Poole
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Android.App;
using Android.NUnitLite.UI;
using Android.OS;
using Java.IO;
using NUnit.Framework.Api;
using NUnit.Framework.Internal;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Engine.Network;
using Stride.Graphics.Regression;

using Console = System.Console;
using File = System.IO.File;
using StringWriter = System.IO.StringWriter;
using TextUI = Stride.Graphics.Regression.TextUI;
using Stride;
using static System.Int32;

namespace NUnitLite.Tests
{
    [Activity(MainLauncher = true, Name = "nunitlite.tests.MainActivity")]
    public class MainActivity : Activity, ITestListener
    {
        private const char IpAddressesSplitCharacter = '%';

        public static Logger Logger = GlobalLogger.GetLogger("NUnitLiteLauncher");
        private readonly ConsoleLogListener logAction = new ConsoleLogListener();
        private string resultFile;
        private StringBuilder stringBuilder;
        private SimpleSocket socketContext;
        private BinaryWriter socketBinaryWriter;

        protected TcpClient Connect(string serverAddresses, int serverPort)
        {
            // Connect back to server
            var client = new TcpClient();
            var possibleIpAddresses = serverAddresses.Split(IpAddressesSplitCharacter);
            foreach (var possibleIpAddress in possibleIpAddresses)
            {
                if (String.IsNullOrEmpty(possibleIpAddress))
                    continue;
                try
                {
                    Logger.Debug($@"Trying to connect to the server {possibleIpAddress}:{serverPort}.");
                    client.Connect(possibleIpAddress, serverPort);

                    Logger.Debug($@"Client connected with ip {possibleIpAddress}... sending data");
                    return client;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error when trying to connect to the server IP {possibleIpAddress}.\n{ex}", ex);
                }

                Logger.Debug($@"Client connected with ip {possibleIpAddress}... sending data");

                return client;
            }

            Logger.Fatal(@"Could not connect to server. Quitting the application.");
            OnDestroy();
            Finish();

            throw new InvalidObjectException("Could not connect to server.");
        }

        protected override void OnCreate(Bundle bundle)
        {
            GlobalLogger.GlobalMessageLogged += logAction;
            Logger.ActivateLog(LogMessageType.Debug);
            logAction.LogMode = ConsoleLogMode.Always;

            base.OnCreate(bundle);

            // Set the android global context
            if (PlatformAndroid.Context == null)
                PlatformAndroid.Context = this;

            var strideVersion = Intent.GetStringExtra(TestRunner.StrideVersion);
            if (strideVersion == null)
            {
                // Connect to image server in the background
                Task.Run(() => ConnectToImageServer());

                // No explicit intent, switch to UI activity
                StartActivity(typeof(StrideTestSuiteActivity));
                return;
            }

            Task.Run(() => RunTests());
        }

        private static void ConnectToImageServer()
        {
            // Use connection router to connect back to image tester
            // Connect during startup, so that first test timing is not affected by initial connection
            try
            {
                var imageServerSocket = RouterClient.RequestServer($"/redirect/{ImageTester.StrideImageServerHost}/{ImageTester.StrideImageServerPort}").Result;
                ImageTester.Connect(imageServerSocket);
            }
            catch (Exception e)
            {
                Logger.Error($"Error connecting to image tester server: {e}", e);
            }
        }

        private void RunTests()
        {
            AppDomain.CurrentDomain.UnhandledException += (a, e) =>
            {
                var exception = e.ExceptionObject as Exception;
                if (exception != null)
                {
                    var exceptionText = exception.ToString();
                    stringBuilder.Append($"Tests fatal failure: {exceptionText}");
                    Logger.Debug($"Unhandled fatal exception: {exception.ToString()}");
                    EndTesting(true);
                }
            };

            var strideVersion = Intent.GetStringExtra(TestRunner.StrideVersion);
            var buildNumber = Parse(Intent.GetStringExtra(TestRunner.StrideBuildNumber) ?? "-1");
            var branchName = Intent.GetStringExtra(TestRunner.StrideBranchName) ?? "";

            // Remove extra (if activity is recreated)
            Intent.RemoveExtra(TestRunner.StrideVersion);
            Intent.RemoveExtra(TestRunner.StrideBuildNumber);
            Intent.RemoveExtra(TestRunner.StrideBranchName);

            Logger.Info(@"*******************************************************************************************************************************");
            Logger.Info(@"date: " + DateTime.Now);
            Logger.Info(@"*******************************************************************************************************************************");

            // Connect to server right away to let it know we're alive
            //var client = Connect(serverAddresses, serverPort);

            var url = "/task/Stride.TestRunner.exe";

            socketContext = RouterClient.RequestServer(url).Result;
            socketBinaryWriter = new BinaryWriter(socketContext.WriteStream);

            // Update build number (if available)
            ImageTester.ImageTestResultConnection.BuildNumber = buildNumber;
            ImageTester.ImageTestResultConnection.BranchName = branchName ?? "";

            ConnectToImageServer();

            // Start unit test
            var cachePath = CacheDir.AbsolutePath;
            var timeNow = DateTime.Now;

            // Generate result file name
            resultFile = Path.Combine(cachePath, $"TestResult-{timeNow:yyyy-MM-dd_hh-mm-ss-tt}.xml");

            Logger.Debug(@"Execute tests");

            stringBuilder = new StringBuilder();
            var stringWriter = new StringWriter(stringBuilder);

            try
            {
                new TextUI(stringWriter)
                {
                    TestListener = this
                }.Execute(new[] { "-format:nunit2", $"-result:{resultFile}" });
            }
            catch (Exception ex)
            {
                stringBuilder.Append($"Tests fatal failure: {ex}");
                Logger.Error($"Tests fatal failure: {ex}");
            }           

            EndTesting(false);
        }

        private void EndTesting(bool failure)
        {
            Logger.Debug(@"Execute tests done");

            // Read result file
            var result = File.Exists(resultFile) ? File.ReadAllText(resultFile) : "";

            // Delete result file
            if(File.Exists(resultFile)) File.Delete(resultFile);

            // Display some useful info
            var output = stringBuilder.ToString();
            Console.WriteLine(output);

            Logger.Debug(@"Sending results to server");

            // Send back result
            socketBinaryWriter.Write((int)(failure ? TestRunnerMessageType.SessionFailure : TestRunnerMessageType.SessionSuccess));
            socketBinaryWriter.Write(output);
            socketBinaryWriter.Write(result);
            socketBinaryWriter.Flush();

            Logger.Debug(@"Close connection");

            ImageTester.Disconnect();

            socketContext.WriteStream.Flush();

            socketContext.Dispose();

            Finish();
        }

        public void TestStarted(ITest test)
        {
            socketBinaryWriter.Write((int)TestRunnerMessageType.TestStarted);
            socketBinaryWriter.Write(test.FullName);
            socketBinaryWriter.Flush();
        }

        public void TestFinished(ITestResult result)
        {
            socketBinaryWriter.Write((int)TestRunnerMessageType.TestFinished);
            socketBinaryWriter.Write(result.FullName);
            socketBinaryWriter.Write(result.ResultState.Status.ToString());
            socketBinaryWriter.Flush();
        }

        public void TestOutput(TestOutput testOutput)
        {
            socketBinaryWriter.Write((int)TestRunnerMessageType.TestOutput);
            socketBinaryWriter.Write(testOutput.Type.ToString());
            socketBinaryWriter.Write(testOutput.Text);
            socketBinaryWriter.Flush();
        }

        protected override void OnDestroy()
        {
            GlobalLogger.GlobalMessageLogged -= logAction;

            base.OnDestroy();
        }
    }

    [Activity]
    public class StrideTestSuiteActivity : RunnerActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            // Set the android global context
            if (PlatformAndroid.Context == null)
                PlatformAndroid.Context = this;

            ImageTester.ImageTestResultConnection.BuildNumber = -1;

            // Test current assembly
            Add(Assembly.GetExecutingAssembly());

            base.OnCreate(bundle);
        }

        protected override void OnDestroy()
        {
            ImageTester.Disconnect();

            base.OnDestroy();
        }
    }
}
