// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using NUnit.Framework;

using Xenko.Games;

namespace Xenko.Graphics.Regression
{
    [TestFixture]
    public class GraphicsTestBase : TestGameBase
    {
        #region Constant strings

        private const string SaveDirectory = @"";

        private const string AndroidReferenceName = @"SC-02C";

        private const string PcReferenceName = @"";

        private const string IosReferenceName = @"";

        private const char IpAddressesSplitCharacter = '%';

        #endregion

        #region Private members

        /// <summary>
        /// A flag stating if the program is run from a unit test session.
        /// </summary>
        private bool isServer;

        /// <summary>
        /// A flag to enable local save.
        /// </summary>
        private bool saveLocally;

        /// <summary>
        /// The current buildNumber.
        /// </summary>
        private int buildNumber;

        /// <summary>
        /// The address of the server.
        /// </summary>
        private string serverAddresses;
        
        /// <summary>
        /// The port used to communicate with the server.
        /// </summary>
        private int serverPort;

        /// <summary>
        /// The server used to get results from the test.
        /// </summary>
        private TcpListener server;

        /// <summary>
        /// A boolean stating if the server is up.
        /// </summary>
        private bool serverUp;

        /// <summary>
        /// Reset event used to correctly terminate the listening thread.
        /// </summary>
        private ManualResetEvent dataReceivedEvent;

        /// <summary>
        /// The client used to send data.
        /// </summary>
        private TcpClient client;

        /// <summary>
        /// The current device.
        /// </summary>
        private ConnectedDevice currentDevice;

        /// <summary>
        /// A flag stating if the image was received.
        /// </summary>
        private bool imageReceived;

        /// <summary>
        /// A flag stating if the test was performed.
        /// </summary>
        private bool testPerformed;

        /// <summary>
        /// A flag stating if the test is executed on bamboo.
        /// </summary>
        private bool onBamboo;

        /// <summary>
        /// The directory containing the Xenko SDK.
        /// </summary>
        private string xenkoSdkDir;

        /// <summary>
        /// A flag stating if the client is connected.
        /// </summary>
        private bool isClientConnected;
        
        /// <summary>
        /// Name of the assembly.
        /// </summary>
        private string assemblyNameForAndroid = "Xenko.Graphics.Regression";

        /// <summary>
        /// The name of the branch the test is done on;
        /// </summary>
        private string branchName;
        
        /// <summary>
        /// A flag that can be used to tell the program to run tests. Can be used to switch from visual check to actual unit test. Is handled automatically.
        /// </summary>
        private bool runTests;

        #endregion

        #region Public properties

        public FrameGameSystem FrameGameSystem { get; private set; }

        #endregion

        #region Public members

        /// <summary>
        /// The version to compare to.
        /// </summary>
        public int BaseVersionNumber;

        /// <summary>
        /// The current version of the test
        /// </summary>
        public int CurrentVersionNumber;

        /// <summary>
        /// The timeout of the server in milliseconds.
        /// </summary>
        public int Timeout;

        /// <summary>
        /// Forced name of the base version.
        /// </summary>
        public string BaseVersionFileName;

        /// <summary>
        /// Location of the csproj.
        /// </summary>
        public string CsprojLocationForAndroid = @"sources\engine\Xenko.Graphics.Regression\Xenko.Graphics.Regression.Android.csproj";

        #endregion

        #region Constructors

        public GraphicsTestBase()
        {
            isServer = false;
            saveLocally = false;
            BaseVersionNumber = 0;
            CurrentVersionNumber = 0;
            Timeout = 120000;
            server = null;
            client = null;
            serverUp = false;
            runTests = false;
            isClientConnected = false;

            onBamboo = Environment.GetEnvironmentVariable("XENKO_BAMBOO_TEST") != null;
            if (!Int32.TryParse(Environment.GetEnvironmentVariable(@"BAMBOO_BUILD_NUMBER"), out buildNumber))
                buildNumber = 0;
            
            xenkoSdkDir = Environment.GetEnvironmentVariable(@"XenkoSdkDir");

            if (onBamboo)
            {
                branchName = Environment.GetEnvironmentVariable("XENKO_BAMBOO_BRANCH_NAME");
                if (branchName != null)
                    branchName = branchName.Trim();
            }
            else
                branchName = null;

            assemblyNameForAndroid = this.GetType().Assembly.ManifestModule.ScopeName;

            FrameGameSystem = new FrameGameSystem(this.Services);
            FrameGameSystem.Visible = true;
            FrameGameSystem.Enabled = true;
            this.GameSystems.Add(FrameGameSystem);
        }

        /// <summary>
        /// Initialize the game with the corresponding values.
        /// </summary>
        /// <param name="ipString">THe IP address of the server.</param>
        /// <param name="port">The listening port.</param>
        /// <param name="bNumber">The build number.</param>
        /// <param name="serial">The serial of the device.</param>
        public void Init(string ipString, int port, int bNumber, string serial)
        {
            serverAddresses = ipString;
            serverPort = port;
            buildNumber = bNumber;
        }

        #endregion

        #region public methods

        [TestFixtureSetUp]
        public void SetUpServerFlag()
        {
            isServer = true;
            FrameGameSystem.Visible = false;
            FrameGameSystem.Enabled = false;
        }

        [TestFixtureTearDown]
        public void StopServer()
        {
            if (server != null)
            {
                server.Stop();
                server = null;
            }
        }

        /// <summary>
        /// Run the test.
        /// </summary>
        /// <param name="device">The device where to run the test.</param>
        /// <param name="gameContext">The game context.</param>
        public void RunTest(ConnectedDevice device, GameContext gameContext = null)
        {
            currentDevice = device;
            runTests = true;
            Console.WriteLine(@"Running test " + this.GetType().Name + @" on device " + device.Name + @" (" + device.Platform + @")");

            // TODO: should be executed after LoadContent for client - or at the first frame?
            RegisterTests();
            
            if (isServer)
            {
                Console.WriteLine(@"Running server" + (onBamboo ? @" on Bamboo" : @"" ));
                // Launch server
                SetUpServer();

                if (!serverUp)
                {
                    Assert.Fail("Unable to create a server.");
                    return;
                }

                // Reset some variables
                imageReceived = false;
                testPerformed = false;
                dataReceivedEvent = new ManualResetEvent(false);

                // Launch remote test
                Console.WriteLine(@"Waiting for a connection... ");
                //server.BeginAcceptTcpClient(GetClientResultCallback, server);
                RunTestOnDevice(device);
                
                // Wait until data is received or timeout
                dataReceivedEvent.WaitOne(Timeout);

                try
                {
                    Console.WriteLine(@"Stopping the server.");
                    serverUp = false;
                    StopServer();
                }
                catch (Exception)
                {
                    Console.WriteLine(@"Stopping the server threw an error.");
                }
                finally
                {
                    // Some tests
                    Assert.IsTrue(imageReceived, "The image was not received.");
                    Assert.IsTrue(testPerformed, "The tests were not correctly performed");
                }
            }
            else
            {
                Console.WriteLine(@"Running test client");
                // 1. Create client
                SetUpClient();

                // 2. Run game
                Run(gameContext);
            }
        }

        /// <summary>
        /// Get the name of the base image to compare the generated images to.
        /// </summary>
        /// <param name="platform">The platform the test is performed on.</param>
        /// <param name="deviceName">The name of the device.</param>
        /// <returns>The name of the file.</returns>
        public string GenerateBaseFileNameFromVersion(TestPlatform platform, string deviceName)
        {
            return this.GetType().Name + @"_" + PlatformPermutator.GetPlatformName(platform) + @"_" + deviceName + @"_v" + BaseVersionNumber + @".png";
        }

        /// <summary>
        /// Save the image locally or on the server.
        /// </summary>
        /// <param name="textureToSave">The texture to save.</param>
        /// <param name="frameIndex">The index of the frame.</param>
        public void SaveImage(Texture textureToSave, int frameIndex)
        {
            if (textureToSave == null)
                return;

            Console.WriteLine(@"Saving non null image");
            var testName = NUnit.Framework.TestContext.CurrentContext.Test.FullName;
            if (saveLocally)
            {
                Console.WriteLine(@"saving locally.");
                using (var image = textureToSave.GetDataAsImage())
                {
                    var textureName = testName + "_" + PlatformPermutator.GetCurrentPlatformName();
                    using (var resultFileStream = File.OpenWrite(FileNameGenerator.GetFileName(textureName)))
                    {
                        image.Save(resultFileStream, ImageFileType.Png);
                    }
                }
            }
            else if (client != null)
            {
                Console.WriteLine(@"saving remotely.");
                using (var image = textureToSave.GetDataAsImage())
                {
                    try
                    {
                        SendImage(image, currentDevice.Serial, testName, frameIndex);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(@"An error occurred when trying to send the data to the server.");
                    }
                }
            }
        }

        /// <summary>
        /// Save the image locally or on the server.
        /// </summary>
        /// <param name="frameIndex">The index of the frame.</param>
        public void SaveBackBuffer(int frameIndex)
        {
            Console.WriteLine(@"Saving the backbuffer");
            SaveImage(GraphicsDevice.BackBuffer.Texture, frameIndex);
        }

        /// <summary>
        /// Lists all the devices accessible from the computer.
        /// </summary>
        /// <returns>The list of all the available devices.</returns>
        public ConnectedDevice[] ListDevices()
        {
            var devices = new List<ConnectedDevice>();

            var addPcD3DDevices = Environment.GetEnvironmentVariable("XENKO_PC_DIRECT3D_DEVICES") != null;
            var addPcOglDevices = Environment.GetEnvironmentVariable("XENKO_PC_OPENGL_DEVICES") != null;
            var addPcOglEsDevices = Environment.GetEnvironmentVariable("XENKO_PC_OPENGLES_DEVICES") != null;
            var addAndroidDevices = Environment.GetEnvironmentVariable("XENKO_ANDROID_DEVICES") != null;
            var addiOsDevices = Environment.GetEnvironmentVariable("XENKO_IOS_DEVICES") != null;
            
            if (!(addPcD3DDevices || addPcOglDevices || addPcOglEsDevices || addAndroidDevices || addiOsDevices))
            {
                devices.AddRange(ListPcD3DDevices());
                devices.AddRange(ListPcOglDevices());
                devices.AddRange(ListPcOglEsDevices());
                devices.AddRange(ListAndroidDevices());
                devices.AddRange(ListIOsDevices());
            }
            else
            {
                if (addPcD3DDevices)
                    devices.AddRange(ListPcD3DDevices());
                if (addPcOglDevices)
                    devices.AddRange(ListPcOglDevices());
                if (addPcOglEsDevices)
                    devices.AddRange(ListPcOglEsDevices());
                if (addAndroidDevices)
                    devices.AddRange(ListAndroidDevices());
                if (addiOsDevices)
                    devices.AddRange(ListIOsDevices());
            }
            
            return devices.ToArray();
        }

        /// <summary>
        /// Lists all the Android devices accessible from the computer.
        /// </summary>
        /// <returns>The list of all the available Android devices.</returns>
        public ConnectedDevice[] ListAndroidDevices()
        {
            var devices = new List<ConnectedDevice>();

            var devicesOutputs = ShellHelper.RunProcessAndGetOutput(AndroidDeviceEnumerator.GetAdbPath(), @"devices");
            var whitespace = new[] { ' ', '\t' };
            for (var i = 1; i < devicesOutputs.OutputLines.Count; ++i) // from the second line
            {
                var line = devicesOutputs.OutputLines[i];
                if (line != null)
                {
                    var res = line.Split(whitespace);
                    if (res.Length == 2)
                    {
                        ConnectedDevice device;
                        device.Serial = res[0];
                        device.Name = res[1];
                        device.Platform = TestPlatform.Android;
                        devices.Add(device);
                    }
                }
            }

            // Set the real name of the Android device.
            for (var i = 0; i < devices.Count; ++i)
            {
                var device = devices[i];
                //TODO: doing a grep instead will be better
                var deviceNameOutputs = ShellHelper.RunProcessAndGetOutput(AndroidDeviceEnumerator.GetAdbPath(), @"-s " + device.Serial + @" shell cat /system/build.prop");
                foreach (var line in deviceNameOutputs.OutputLines)
                {
                    if (line != null && line.StartsWith(@"ro.product.model")) // correct line
                    {
                        var parts = line.Split('=');

                        if (parts.Length > 1)
                        {
                            device.Name = parts[1];
                            devices[i] = device;
                        }

                        break; // no need to search further
                    }
                }
            }

            // get the name of the base device.
            foreach (var device in devices)
            {
                if (device.Name.Equals(AndroidReferenceName))
                {
                    //device = device.Serial;
                    break;
                }
            }

            return devices.ToArray();
        }

        /// <summary>
        /// Lists all the Android devices accessible from the computer.
        /// </summary>
        /// <returns>The list of all the available Android devices.</returns>
        public ConnectedDevice[] ListIOsDevices()
        {
            var devices = new List<ConnectedDevice>();
            return devices.ToArray();
        }

        /// <summary>
        /// Lists all the D3D PC devices accessible from the computer.
        /// </summary>
        /// <returns>The list of all the available D3D PC devices.</returns>
        public ConnectedDevice[] ListPcD3DDevices()
        {
            var devices = new List<ConnectedDevice>(); 
            
            ConnectedDevice pcDdevice;
            pcDdevice.Serial = "Local";
            pcDdevice.Name = "LocalPC";
            pcDdevice.Platform = TestPlatform.WindowsDx;
            devices.Add(pcDdevice);

            return devices.ToArray();
        }

        /// <summary>
        /// Lists all the OpenGL PC devices accessible from the computer.
        /// </summary>
        /// <returns>The list of all the available OpenGL PC devices.</returns>
        public ConnectedDevice[] ListPcOglDevices()
        {
            var devices = new List<ConnectedDevice>();

            ConnectedDevice pcDdevice;
            pcDdevice.Serial = "Local";
            pcDdevice.Name = "LocalPC";
            pcDdevice.Platform = TestPlatform.WindowsOgl;
            devices.Add(pcDdevice);

            return devices.ToArray();
        }

        /// <summary>
        /// Lists all the OpenGL ES PC devices accessible from the computer.
        /// </summary>
        /// <returns>The list of all the available OpenGL ES PC devices.</returns>
        public ConnectedDevice[] ListPcOglEsDevices()
        {
            var devices = new List<ConnectedDevice>();

            ConnectedDevice pcDdevice;
            pcDdevice.Serial = "Local";
            pcDdevice.Name = "LocalPC";
            pcDdevice.Platform = TestPlatform.WindowsOgles;
            //devices.Add(pcDdevice);

            return devices.ToArray();
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Method to register the tests.
        /// </summary>
        protected virtual void RegisterTests()
        {
        }

        /// <summary>
        /// Loop through all the tests and save the images.
        /// </summary>
        /// <param name="gameTime">the game time.</param>
        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (runTests && !isServer)
            {
                if (FrameGameSystem.AllTestsCompleted)
                {
                    CloseClient();
                    Exit();
#if XENKO_PLATFORM_ANDROID
                   Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
#endif
                }
                else if (FrameGameSystem.TakeSnapshot)
                    SaveBackBuffer(gameTime.FrameCount - 1); // because first draw happens at frame 1
            }
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Create the server and start it.
        /// </summary>
        private void SetUpServer()
        {
            //TODO: IPv6 ?
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            
            serverAddresses = "";

            foreach (var adapter in nics)
            {
                var ip = adapter.GetIPProperties();
                foreach (var addr in ip.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork && !String.IsNullOrEmpty(addr.Address.ToString()) && !addr.Address.ToString().Equals(@"127.0.0.1"))
                        serverAddresses = String.Join(IpAddressesSplitCharacter.ToString(), serverAddresses, addr.Address);
                }
            }

            if (serverAddresses.Equals(""))
            {
                Console.WriteLine(@"No IP address found.");
                return;
            }

            var rand = new Random();
            serverPort = 20000 + (int)(100.0 * rand.NextDouble()); // range [20000, 20100]
            server = new TcpListener(IPAddress.Any, serverPort);
            Console.WriteLine(@"Server listening to port {0}", serverPort);
            serverUp = true;
            server.Start();
        }

        /// <summary>
        /// Setup all needed for the client.
        /// </summary>
        public void SetUpClient()
        {
            client = new TcpClient();
        }

        /// <summary>
        /// Run the test on the specified device.
        /// </summary>
        /// <param name="device">The device.</param>
        private void RunTestOnDevice(ConnectedDevice device)
        {
            switch (device.Platform)
            {
                case TestPlatform.WindowsDx:
                case TestPlatform.WindowsOgl:
                case TestPlatform.WindowsOgles:
                    RunWindowsTest(device.Serial, device.Platform);
                    break;
                case TestPlatform.Android:
                    if (onBamboo)
                        RunAndroidTestOnBamboo(device.Serial);
                    else
                        RunAndroidTest(device.Serial);
                    break;
                case TestPlatform.Ios:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Run the test on Android through a bat file.
        /// </summary>
        private void RunAndroidTest(string serial)
        {
            try
            {
                var testName = this.GetType().Name;
                Process.Start(
                    new ProcessStartInfo(@"Scripts\RunAndroidUnitTest.bat", serverAddresses + " "
                                                                            + serverPort + " "
                                                                            + buildNumber + " "
                                                                            + serial + " "
                                                                            + testName + " "
                                                                            + assemblyNameForAndroid + " "
                                                                            + CsprojLocationForAndroid)
                    {
                        UseShellExecute = false,
                        CreateNoWindow = false,
                    });
            }
            catch
            {
                Console.WriteLine(@"An error was thrown when running the test on Android.");
                dataReceivedEvent.Set();
            }
        }

        /// <summary>
        /// Run the test on Android through a bat file.
        /// </summary>
        private void RunAndroidTestOnBamboo(string serial)
        {
            try
            {
                // force stop - only works for Android 3.0 and above.
                var o0 = ShellHelper.RunProcessAndGetOutput(AndroidDeviceEnumerator.GetAdbPath(), @"-s " + serial + @" am shell force-stop " + assemblyNameForAndroid);

                // install
                var o1 = ShellHelper.RunProcessAndGetOutput(AndroidDeviceEnumerator.GetAdbPath(), @"-s " + serial + @" -d install -r ..\..\Bin\Android-OpenGLES\" + assemblyNameForAndroid + "-Signed.apk");

                // run
                var parameters = new StringBuilder();
                parameters.Append("-s "); parameters.Append(serial);
                parameters.Append(@" shell am start -a android.intent.action.MAIN -n " + assemblyNameForAndroid + "/xenko.graphicstests.GraphicsTestRunner");
                AddAndroidParameter(parameters, TestRunner.XenkoServerIp, serverAddresses);
                AddAndroidParameter(parameters, TestRunner.XenkoServerPort, serverPort.ToString());
                AddAndroidParameter(parameters, TestRunner.XenkoBuildNumber, buildNumber.ToString());
                AddAndroidParameter(parameters, TestRunner.XenkoDeviceSerial, serial);
                AddAndroidParameter(parameters, TestRunner.XenkoTestName, this.GetType().Name);

                Console.WriteLine(parameters.ToString());

                ShellHelper.RunProcess(AndroidDeviceEnumerator.GetAdbPath(), parameters.ToString());
            }
            catch
            {
                Console.WriteLine(@"An error was thrown when running the test on Android.");
                dataReceivedEvent.Set();
            }
        }
        
        /// <summary>
        /// Run the test on Windows through a bat file.
        /// </summary>
        private void RunWindowsTest(string serial, TestPlatform platform)
        {
            try
            {
                var command = new StringBuilder();
                var parameters = new StringBuilder();
                command.Append(@"..\..\Bin\");
                switch (platform)
                {
                    case TestPlatform.WindowsDx:
                        command.Append(@"Windows-AnyCPU-Direct3D\");
                        break;
                    case TestPlatform.WindowsOgl:
                        command.Append(@"Windows-AnyCPU-OpenGL\");
                        break;
                    case TestPlatform.WindowsOgles:
                        command.Append(@"Windows-AnyCPU-OpenGLES\");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("platform");
                }
                command.Append(@"Xenko.Graphics.Regression.exe");

                parameters.Append(serverAddresses);
                parameters.Append(" "); parameters.Append(serverPort);
                parameters.Append(" "); parameters.Append(buildNumber);
                parameters.Append(" "); parameters.Append(serial);
                parameters.Append(" "); parameters.Append(this.GetType().Name);
                parameters.Append(" "); parameters.Append(this.GetType().Assembly.ManifestModule.Name);

                Console.WriteLine(@"Running: " + command.ToString() + @" " + parameters.ToString());

                var outputs = ShellHelper.RunProcessAndGetOutput(command.ToString(), parameters.ToString());
                foreach (var output in outputs.OutputLines)
                    Console.WriteLine(output);

                foreach (var output in outputs.OutputErrors)
                    Console.WriteLine(output);
            }
            catch (Exception e)
            {
                Console.WriteLine(@"An error was thrown when running the test on Windows.");
                dataReceivedEvent.Set();
            }
        }
        
        /// <summary>
        /// Send the data of the test to the server.
        /// </summary>
        /// <param name="image">The image to send.</param>
        /// <param name="serial">The serial of the device.</param>
        /// <param name="testName">The name of the test.</param>
        public void SendImage(Image image, string serial, string testName, int frameIndex)
        {
            ImageTester.SendImage(new TestResultImage { BaseVersionFileName = BaseVersionFileName, BaseVersion = BaseVersionNumber, CurrentVersion = CurrentVersionNumber, Image = image }, testName);
        }

        /// <summary>
        /// Closes the client.
        /// </summary>
        private void CloseClient()
        {
            if (isClientConnected)
            {
                Console.WriteLine(@"Close client.");
                client.GetStream().Close();
                client.Close();
                isClientConnected = false;
            }
        }

        #endregion

        #region Helper structures and classes

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

        #endregion
    }
}
