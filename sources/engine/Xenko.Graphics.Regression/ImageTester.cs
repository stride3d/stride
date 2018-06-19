// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xenko.Engine.Network;
using Sockets.Plugin;

namespace Xenko.Graphics.Regression
{
    public static class ImageTester
    {
        public const string XenkoImageServerHost = "xenkobuild.xenko.com";
        public const int XenkoImageServerPort = 1832;

        public static ImageTestResultConnection ImageTestResultConnection = PlatformPermutator.GetDefaultImageTestResultConnection();

        private static TcpSocketClient ImageComparisonServer;

        public static bool Connect(SimpleSocket simpleSocket)
        {
            ImageComparisonServer = simpleSocket.Socket;
            return OpenConnection();
        }

        public static bool Connect()
        {
            if (ImageComparisonServer != null)
                return true;

            try
            {
                ImageComparisonServer = new TcpSocketClient();
                var t = Task.Run(async () => await ImageComparisonServer.ConnectAsync(XenkoImageServerHost, XenkoImageServerPort));
                t.Wait();
            }
            catch (Exception)
            {
                ImageComparisonServer = null;

                return false;
            }

            return OpenConnection();
        }

        private static bool OpenConnection()
        {
            try
            {
                // Send initial parameters
                var networkStream = ImageComparisonServer.WriteStream;
                var binaryWriter = new BinaryWriter(networkStream);
                ImageTestResultConnection.Write(binaryWriter);
                return true;
            }
            catch
            {
                ImageComparisonServer = null;
                return false;
            }
        }

        public static void Disconnect()
        {
            if (ImageComparisonServer != null)
            {
                try
                {
                    // Properly sends a message notifying we want to close the connection
                    var networkStream = ImageComparisonServer.WriteStream;
                    var binaryWriter = new BinaryWriter(networkStream);
                    binaryWriter.Write((int)ImageServerMessageType.ConnectionFinished);
                    binaryWriter.Flush();

                    ImageComparisonServer.Dispose();
                }
                catch (Exception)
                {
                    // Ignore failures on disconnect
                }
                ImageComparisonServer = null;
            }
        }

        public static bool RequestImageComparisonStatus(ref string testName)
        {
            if (!Connect())
                throw new InvalidOperationException("Could not connect to image comparer server");

            try
            {
                if (testName == null && NUnit.Framework.TestContext.CurrentContext != null)
                {
                    testName = NUnit.Framework.TestContext.CurrentContext.Test.FullName;
                }

                var binaryWriter = new BinaryWriter(ImageComparisonServer.WriteStream);
                var binaryReader = new BinaryReader(ImageComparisonServer.ReadStream);

                // Header
                binaryWriter.Write((int)ImageServerMessageType.RequestImageComparisonStatus);
                binaryWriter.Write(testName ?? "Unable to fetch test name");
                binaryWriter.Flush();

                return binaryReader.ReadBoolean();
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Send the data of the test to the server.
        /// </summary>
        /// <param name="testResultImage">The image to send.</param>
        public static bool SendImage(TestResultImage testResultImage)
        {
            if (!Connect())
                throw new InvalidOperationException("Could not connect to image comparer server");

            try
            {
                if (testResultImage.TestName == null && NUnit.Framework.TestContext.CurrentContext != null)
                {
                    testResultImage.TestName = NUnit.Framework.TestContext.CurrentContext.Test.FullName;
                }

                var binaryWriter = new BinaryWriter(ImageComparisonServer.WriteStream);
                var binaryReader = new BinaryReader(ImageComparisonServer.ReadStream);

                // Header
                binaryWriter.Write((int)ImageServerMessageType.SendImage);

                GameTestBase.TestGameLogger.Info(@"Sending image information...");

                var sw = new Stopwatch();
                sw.Start();

                testResultImage.Write(binaryWriter);

                sw.Stop();
                GameTestBase.TestGameLogger.Info($"Total calculation time: {sw.Elapsed}");

                return binaryReader.ReadBoolean();
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
