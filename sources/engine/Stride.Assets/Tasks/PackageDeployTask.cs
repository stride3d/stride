// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Renci.SshNet;
using Renci.SshNet.Common;
using Stride.Core.IO;

namespace Stride.Assets.Tasks
{
    /// <summary>
    /// Task in charge of syncing a <see cref="Directory"/> to a different <see cref="Machine"/> using a <see cref="Username"/> and
    /// <see cref="Password"/> in a <see cref="Location"/>
    /// </summary>
    public class PackageDeployTask : Task
    {

        /// <summary>
        /// Execute deploy task
        /// </summary>
        /// <returns>True on success, false otherwise</returns>
        public override bool Execute()
        {
            var t1 = DateTime.Now;
            bool isDeployed = false;
            Log.LogMessage("Deploying {0} on {1}:{2}...", Directory, Machine, Location);

            try
            {
                isDeployed = SyncTo(Directory.ItemSpec);
            }
            catch (SshAuthenticationException)
            {
                Log.LogError("Invalid username or password");
            }
            catch (SshException e)
            {
                Log.LogError(e.ToString());
            }
            catch (SocketException)
            {
                Log.LogError("Connection error when accessing to " + Machine + " on port " + Port);
            }

            if (isDeployed)
            {
                Log.LogMessage("Deployed.");
            }
            else
            {
                Log.LogError("Could not deploy. Check log for more details.");
            }

            var t2 = DateTime.Now;
            Log.LogMessage(nameof(PackageDeployTask) + " took " + (t2 - t1));
            return isDeployed;
        }

        /// <summary>
        /// Sync output of current compilation to <paramref name="dir"/>
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        private bool SyncTo(string dir)
        {
            // Copy files over
            using (var sftp = new SftpClient(Machine, Port, Username, Password))
            {
                sftp.Connect();
                if (!sftp.IsConnected)
                {
                    return false;
                }
                // Perform recursive copy of all the folders under `dir`. This is required
                // as the sftp client only synchronize directories at their level only, no
                // subdirectory.
                var dirs = new Queue<DirectoryInfo>();
                dirs.Enqueue(new DirectoryInfo(dir));
                var parentPath = new UDirectory(dir);
                while (dirs.Count != 0)
                {
                    var currentDir = dirs.Dequeue();
                    var currentPath = new UDirectory(currentDir.FullName);
                    foreach (var subdir in currentDir.EnumerateDirectories())
                    {
                        dirs.Enqueue(subdir);
                    }
                    // Get the destination path by adding to `Location` the relative path of `dir` to `currentDir`.
                    var destination = UPath.Combine(new UDirectory(Location.ItemSpec), currentPath.MakeRelative(parentPath));
                    Log.LogMessage("Synchronizing " + currentPath + " with " + destination.FullPath);
                    // Try to create a remote directory. If it throws an exception, we will assume
                    // for now that the directory already exists. See https://github.com/sshnet/SSH.NET/issues/25
                    try
                    {
                        sftp.CreateDirectory(destination.FullPath);
                        Log.LogMessage("Creating remote directory " + destination.FullPath);
                    }
                    catch (SshException)
                    {
                        // Do nothing, as this is when the directory already exists
                    }
                    // Synchronize files.
                    foreach (var file in sftp.SynchronizeDirectories(currentPath.FullPath, destination.FullPath, "*"))
                    {
                        Log.LogMessage("Updating " + file.Name);
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Directory to copy over to <see cref="Machine"/>.
        /// </summary>
        [Required]
        public ITaskItem Directory { get; set; }

        /// <summary>
        /// Machine where <see cref="Directory"/> will be copied.
        /// </summary>
        [Required]
        public string Machine { get; set; }

        /// <summary>
        /// Username used to log onto <see cref="Machine"/>.
        /// </summary>
        [Required]
        public string Username { get; set; }

        /// <summary>
        /// Password used to log onto <see cref="Machine"/> with <see cref="Username"/>.
        /// </summary>
        [Required]
        public string Password { get; set; }

        [Required]
        public ITaskItem Location { get; set; }

        /// <summary>
        /// Port on which we will connect, by default 22
        /// </summary>
        public int Port
        {
            get { return (_port <= 0 ? 22 : _port); }
            set { _port = value; }
        }

        private int _port = -1;
    }
}
