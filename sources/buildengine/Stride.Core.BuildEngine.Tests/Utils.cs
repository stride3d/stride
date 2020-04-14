// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Reflection;
using System.Text;

using Xunit;
using Stride.Core.BuildEngine.Tests.Commands;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.IO;

namespace Stride.Core.BuildEngine.Tests
{
    public static class Utils
    {
        private static bool loggerHandled;

        private const string FileSourceFolder = "source";

        public static string BuildPath => Path.Combine(PlatformFolders.ApplicationBinaryDirectory, "data/" + Assembly.GetEntryAssembly().GetName().Name);

        public static Logger CleanContext()
        {
            // delete previous build data
            if (Directory.Exists(BuildPath))
                Directory.Delete(BuildPath, true);

            // Create database directory
            ((FileSystemProvider)VirtualFileSystem.ApplicationData).ChangeBasePath(BuildPath);
            VirtualFileSystem.CreateDirectory(VirtualFileSystem.ApplicationDatabasePath);

            // Delete source folder if exists
            if (Directory.Exists(FileSourceFolder))
                Directory.Delete(FileSourceFolder, true);

            Builder.CloseObjectDatabase();

            TestCommand.ResetCounter();
            if (!loggerHandled)
            {
                GlobalLogger.GlobalMessageLogged += new ConsoleLogListener();
                loggerHandled = true;
            }

            return GlobalLogger.GetLogger("UnitTest");
        }

        public static Builder CreateBuilder(bool createIndexFile)
        {
            var logger = new LoggerResult();
            logger.ActivateLog(LogMessageType.Debug);
            var indexName = createIndexFile ? VirtualFileSystem.ApplicationDatabaseIndexName : null;
            var builder = new Builder(logger, BuildPath, indexName) { BuilderName = "TestBuilder" };
            return builder;
        }

        public static void GenerateSourceFile(string filename, string content, bool overwrite = false)
        {
            string filepath = GetSourcePath(filename);

            if (!Directory.Exists(FileSourceFolder))
                Directory.CreateDirectory(FileSourceFolder);

            if (!overwrite && File.Exists(filepath))
                throw new IOException("File already exists");

            File.WriteAllText(filepath, content);
        }

        public static string GetSourcePath(string filename)
        {
            // TODO: return a path in the temporary folder
            return Path.Combine(FileSourceFolder, filename);
        }
    }
}
