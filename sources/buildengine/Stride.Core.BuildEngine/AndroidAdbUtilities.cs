// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Stride.Framework.Diagnostics;
using Stride.Framework.Serialization;

namespace Stride.BuildEngine
{
    public static class AndroidAdbUtilities
    {
        private static readonly string AdbExecutable = FindAdbExecutable();

        public static string GetExternalStoragePath(string device)
        {
            var sdcardPath = RunAdb(device, "shell \"echo $EXTERNAL_STORAGE\"");
            return sdcardPath[0];
        }

        public static string[] GetDevices()
        {
            var devices = RunAdb(null, "devices");

            // Skip first line "List of devices attached",
            // and then take first part of string (device id).
            return devices.Skip(1).Select(x => x.Split(' ', '\t').First()).ToArray();
        }

        /// <summary>
        /// Synchronizes files to an android device.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="device">The device.</param>
        /// <param name="fileMapping">The file mapping (relative target path, source HDD filename).</param>
        /// <param name="androidPath">The android path.</param>
        /// <param name="cacheFile">The cache file.</param>
        public static void Synchronize(Logger logger, string device, Dictionary<string, string> fileMapping, string androidPath, string cacheFile)
        {
            // Ensure android path ends up with directory separator
            if (!androidPath.EndsWith("/"))
                androidPath = androidPath + "/";

            // Search files
            var currentVersions = fileMapping
                .ToDictionary(x => x.Key, x => new FileVersion(x.Value));

            // Try to read previous cache file
            var previousVersions = new Dictionary<string, FileVersion>();
            try
            {
                using (var file = File.OpenRead(cacheFile))
                {
                    var binaryReader = new BinarySerializationReader(file);
                    binaryReader.Serialize(ref previousVersions, ArchiveMode.Deserialize);
                }
            }
            catch (IOException)
            {
            }

            var filesToRemove = new List<string>();
            var filesToUpload = new List<string>();

            // Remove unecessary files (in previousVersions but not in currentVersions)
            foreach (var file in previousVersions.Where(x => !currentVersions.ContainsKey(x.Key)))
            {
                filesToRemove.Add(file.Key);
            }

            // Upload files that are either not uploaded yet, or not up to date
            foreach (var file in currentVersions)
            {
                FileVersion fileVersion;
                if (!previousVersions.TryGetValue(file.Key, out fileVersion)
                    || fileVersion.FileSize != file.Value.FileSize
                    || fileVersion.LastModifiedDate != file.Value.LastModifiedDate)
                {
                    filesToUpload.Add(file.Key);
                }
            }

            // Upload files
            foreach (var file in filesToUpload)
            {
                if (logger != null)
                    logger.Verbose("Copying file {0}", file);
                RunAdb(device, string.Format("push \"{0}\" \"{1}\"", fileMapping[file], androidPath + file.Replace('\\', '/')));
            }

            // Remove files
            foreach (var file in filesToRemove)
            {
                if (logger != null)
                    logger.Verbose("Deleting file {0}", file);
                RunAdb(device, string.Format("shell \"rm {0}\"", androidPath + file.Replace('\\', '/')));
            }

            // Write new cache file
            using (var file = File.Create(cacheFile))
            {
                var binaryWriter = new BinarySerializationWriter(file);
                binaryWriter.Write(currentVersions);
            }
        }

        private static string FindAdbExecutable()
        {
            var androidSdkDir = Environment.GetEnvironmentVariable("ANDROID_SDK");
            var androidAdbExecutable = androidSdkDir != null ? androidSdkDir + @"\platform-tools\adb" : "adb";

            return androidAdbExecutable;
        }

        private static string[] RunAdb(string device, string arguments)
        {
            // Add device to argument list if necessary
            if (device != null)
                arguments = "-s " + device + ' ' + arguments;

            var processStartInfo = new ProcessStartInfo()
                {
                    FileName = AdbExecutable,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                };

            var lines = new List<string>();

            var adbProcess = Process.Start(processStartInfo);
            adbProcess.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        lock (adbProcess)
                        {
                            lines.Add(args.Data);
                        }
                    }
                };
            adbProcess.BeginOutputReadLine();
            adbProcess.WaitForExit();

            return lines.ToArray();
        }

        [Serializable]
        public struct FileVersion
        {
            public DateTime LastModifiedDate;
            public long FileSize;

            public FileVersion(string fileName)
            {
                LastModifiedDate = DateTime.MinValue;
                FileSize = -1;

                if (File.Exists(fileName))
                {
                    var fileInfo = new FileInfo(fileName);
                    LastModifiedDate = fileInfo.LastWriteTime;
                    FileSize = fileInfo.Length;
                }
            }
        }
    }
}
