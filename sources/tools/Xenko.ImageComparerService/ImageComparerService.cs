// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using Mono.Options;
using Xenko.Graphics;
using Xenko.Graphics.Regression;

namespace Xenko.ImageComparerService
{
    public class ImageComparerService : ServiceBase
    {
        public const string DefaultServiceName = "Xenko Image Comparer Service";

        private int port = 1832;
        private string baseOutputPath;
        private Dictionary<TcpClient, Thread> clients = new Dictionary<TcpClient, Thread>();
        private Thread listenerThread;
        private TcpListener server;

        public ImageComparerService()
        {
            ServiceName = DefaultServiceName;
        }

        /// <summary>
        /// Perform a comparison between the generated image and the base one.
        /// </summary>
        /// <param name="receivedImage">The received image.</param>
        /// <returns>True if the tests were correctly performed.</returns>
        private bool CompareImage(TestResultServerImage receivedImage, string resultTempFileName)
        {
            var comparer = new ImageComparator { SaveJson = true };

            return comparer.Compare_RMSE(receivedImage, resultTempFileName);
        }

        private void ProcessSendImage(ImageComparerClient imageComparerClient, BinaryReader reader, BinaryWriter binaryWriter)
        {
            var receivedImage = new TestResultServerImage(imageComparerClient);

            // Read image header
            receivedImage.ClientImage.Read(reader);

            Console.WriteLine(@"Receiving {0} from {1}", receivedImage.ClientImage.TestName, imageComparerClient.Connection.Serial);

            // Compute paths
            var goldPath = Path.Combine(baseOutputPath, receivedImage.GetGoldDirectory());
            var outputPath = Path.Combine(baseOutputPath, receivedImage.GetOutputDirectory());

            receivedImage.GoldPath = goldPath;
            receivedImage.OutputPath = outputPath;
            receivedImage.JsonPath = Path.Combine(baseOutputPath, "json");

            Directory.CreateDirectory(receivedImage.GoldPath);
            Directory.CreateDirectory(receivedImage.OutputPath);
            Directory.CreateDirectory(receivedImage.JsonPath);

            receivedImage.GoldFileName = Path.Combine(goldPath, receivedImage.GetFileName());
            receivedImage.ResultFileName = Path.Combine(outputPath, receivedImage.GetFileName());
            receivedImage.DiffFileName = Path.Combine(outputPath, receivedImage.GetDiffFileName());
            receivedImage.DiffNormFileName = Path.Combine(outputPath, receivedImage.GetNormDiffFileName());

            // Seems like image magick doesn't like to read from UNC path (size is OK but not data?)
            // Let's use temp path to do most of the work before copying
            var resultTempFileName = Path.GetTempPath() + Guid.NewGuid() + ".png";
            try
            {
                // Read image data
                using (var image = receivedImage.ClientImage.Image)
                using (var resultFileStream = File.OpenWrite(resultTempFileName))
                {
                    image.Save(resultFileStream, ImageFileType.Png);
                }

                CompareImage(receivedImage, resultTempFileName);
            }
            finally
            {
                File.Delete(resultTempFileName);
            }

            var receivedImages = imageComparerClient.Images;
            List<TestResultServerImage> receivedImageForThisTest;
            lock (receivedImages)
            {
                if (!receivedImages.TryGetValue(receivedImage.ClientImage.TestName, out receivedImageForThisTest))
                {
                    receivedImageForThisTest = new List<TestResultServerImage>();
                    receivedImage.FrameIndex = receivedImageForThisTest.Count;
                    receivedImages.Add(receivedImage.ClientImage.TestName, receivedImageForThisTest);
                }
            }

            receivedImageForThisTest.Add(receivedImage);

            // Send ack
            binaryWriter.Write(true);
        }

        /// <summary>
        /// Process a request for image comparison (client uses it to know if test succeeded or failed).
        /// </summary>
        /// <param name="binaryReader">The binary reader.</param>
        /// <param name="binaryWriter">The binary writer.</param>
        private void ProcessRequestImageComparisonStatus(ImageComparerClient imageComparerClient, BinaryReader binaryReader, BinaryWriter binaryWriter)
        {
            var testName = binaryReader.ReadString();

            var receivedImages = imageComparerClient.Images;
            List<TestResultServerImage> receivedImageForThisTest;
            lock (receivedImages)
            {
                if (!receivedImages.TryGetValue(testName, out receivedImageForThisTest))
                {
                    // No image, return false?!
                    binaryWriter.Write(true);
                    return;
                }
            }

            // perform comparisons
            var testSucceeded = true;
            foreach (var receivedImage in receivedImageForThisTest)
            {
                testSucceeded &= receivedImage.MeanSquareError == 0.0f;
            }

            binaryWriter.Write(testSucceeded);
            binaryWriter.Flush();
        }

        private void ClientThread(object res)
        {
            var client = (TcpClient)res;

            var imageComparerClient = new ImageComparerClient();

            lock (clients)
            {
                clients[client] = Thread.CurrentThread;
            }

            try
            {
                using (var networkStream = client.GetStream())
                {
                    var binaryReader = new BinaryReader(networkStream);
                    var binaryWriter = new BinaryWriter(networkStream);

                    // Read common information to all tests (device, branch, etc...)
                    imageComparerClient.Connection.Read(binaryReader);

                    // Process requests
                    while (true)
                    {
                        var messageType = (ImageServerMessageType)binaryReader.ReadInt32();
                        if (messageType == ImageServerMessageType.ConnectionFinished)
                            break;

                        switch (messageType)
                        {
                            case ImageServerMessageType.SendImage:
                                // Receives an image
                                ProcessSendImage(imageComparerClient, binaryReader, binaryWriter);
                                break;
                            case ImageServerMessageType.RequestImageComparisonStatus:
                                // Returns comparison status
                                ProcessRequestImageComparisonStatus(imageComparerClient, binaryReader, binaryWriter);
                                break;
                        }
                    }
                }

                client.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Client thread exception: {0}", e);
            }

            lock (clients)
            {
                clients.Remove(client);
            }
        }

        public void ListenBase()
        {
            server = new TcpListener(IPAddress.Any, port);
            server.Start();

            try
            {
                while (true)
                {
                    var client = server.AcceptTcpClient();

                    Console.WriteLine(@"Accepted a new connection from client '{0}'.", client.Client.RemoteEndPoint);

                    new Thread(ClientThread).Start(client);
                }
            }
            catch (SocketException)
            {
                // Probably OnStop closing socket
            }
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);

            var exeName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
            var showHelp = false;
            baseOutputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Xenko", "ImageComparerResults");

            var p = new OptionSet
                {
                    "Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp) All Rights Reserved",
                    "Xenko Test Suite Tool - Version: "
                    +
                    String.Format(
                        "{0}.{1}.{2}",
                        typeof(ImageComparerService).Assembly.GetName().Version.Major,
                        typeof(ImageComparerService).Assembly.GetName().Version.Minor,
                        typeof(ImageComparerService).Assembly.GetName().Version.Build) + string.Empty,
                    string.Format("Usage: {0} [--port=port] [--output=folder", exeName),
                    string.Empty,
                    "=== Options ===",
                    string.Empty,
                    { "h|help", @"Show this message and exit", v => showHelp = v != null },
                    { "p|port=", @"Port (default: 1832)", v => port = int.Parse(v) },
                    { "o|output=", @"Output folder (default: %APPDATA%\Xenko\ImageComparerResults)", v => baseOutputPath = v },
                };

            try
            {
                var commandArgs = p.Parse(args);
                if (commandArgs.Count > 0)
                    throw new OptionException();

                if (showHelp)
                {
                    p.WriteOptionDescriptions(Console.Out);
                    Stop();
                    return;
                }

                Directory.CreateDirectory(baseOutputPath);

                listenerThread = new Thread(ListenBase);
                listenerThread.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("{0}: {1}", exeName, e);
                if (e is OptionException)
                    p.WriteOptionDescriptions(Console.Out);
                Stop();
            }
        }

        protected override void OnStop()
        {
            // Stop main socket & thread
            if (server != null)
            {
                server.Stop();
                server = null;
            }

            if (listenerThread != null)
            {
                listenerThread.Join();
                listenerThread = null;
            }

            // Stop client threads
            KeyValuePair<TcpClient, Thread>[] currentClients;
            lock (clients)
            {
                currentClients = clients.ToArray();
                clients.Clear();
            }

            foreach (var client in currentClients)
            {
                client.Key.Close();
                client.Value.Join();
            }
        }

        /// <summary>
        /// The Main Thread: This is where your Service is Run.
        /// </summary>
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--console")
            {
                var imageComparerService = new ImageComparerService();
                imageComparerService.OnStart(args.Skip(1).ToArray());
                imageComparerService.listenerThread.Join();
                imageComparerService.OnStop();
            }
            else if (args.Length > 0 && args[0] == "--install")
            {
                ManagedInstallerClass.InstallHelper(new[] { Assembly.GetExecutingAssembly().Location });
            }
            else if (args.Length > 0 && args[0] == "--uninstall")
            {
                ManagedInstallerClass.InstallHelper(new[] { "/u", Assembly.GetExecutingAssembly().Location });
            }
            else
            {
                ServiceBase.Run(new ImageComparerService());
            }
        }
    }

    [RunInstaller(true)]
    public class MyWindowsServiceInstaller : Installer
    {
        public MyWindowsServiceInstaller()
        {
            var processInstaller = new ServiceProcessInstaller();
            var serviceInstaller = new ServiceInstaller();

            //set the privileges
            processInstaller.Account = ServiceAccount.LocalSystem;

            serviceInstaller.DisplayName = ImageComparerService.DefaultServiceName;
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            //must be the same as what was set in Program's constructor
            serviceInstaller.ServiceName = ImageComparerService.DefaultServiceName;
            this.Installers.Add(processInstaller);
            this.Installers.Add(serviceInstaller);
        }
    }
}
