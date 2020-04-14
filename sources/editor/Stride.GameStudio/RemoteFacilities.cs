// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Renci.SshNet;
using Renci.SshNet.Common;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Translation;

namespace Stride.GameStudio
{
    /// <summary>
    /// Various feature for doing remote tasks: login, copying, executing, ...
    /// </summary>
    internal static class RemoteFacilities
    {
        /// <summary>
        /// Launch <paramref name="exePath"/> on remote host using credentials stored in EditorSettings.
        /// Before launching all the files requires by <paramref name="exePath"/> are copied over to host
        /// using the location specified in EditorSettings.Location. If <paramref name="isCoreCLR"/> is set
        /// all the Stride native libraries are copied over to the current directory of the game on the remote
        /// host via the `CoreCLRSetup` script.
        /// </summary>
        /// <param name="logger">Logger to show progress and any issues that may occur.</param>
        /// <param name="exePath">Path on the local machine where the executable was compiled.</param>
        /// <param name="isCoreCLR">Is <paramref name="exePath"/> executed against .NET Core?</param>
        /// <returns>True when launch was successful, false otherwise.</returns>
        internal static bool Launch([NotNull] LoggerResult logger, [NotNull] UFile exePath, bool isCoreCLR)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (exePath == null) throw new ArgumentNullException(nameof(exePath));

            var host = StrideEditorSettings.Host.GetValue();
            var username = StrideEditorSettings.Username.GetValue();
            var port = StrideEditorSettings.Port.GetValue();
            var password = Decrypt(StrideEditorSettings.Password.GetValue());
            var location = new UDirectory(StrideEditorSettings.Location.GetValue());
            var display = StrideEditorSettings.Display.GetValue();

            var connectInfo = NewConnectionInfo(host, port, username, password);
            if (SyncTo(connectInfo, exePath.GetFullDirectory(), UPath.Combine(location, new UDirectory(exePath.GetFileNameWithoutExtension())), logger))
            {
                var sshClient = new SshClient(connectInfo);
                try
                {
                    sshClient.Connect();
                    if (sshClient.IsConnected)
                    {
                        string cmdString;
                        SshCommand cmd;

                        // Due to lack of Dllmap config for CoreCLR, we have to ensure that our native libraries
                        // are copied next to the executable. The CoreCLRSetup script will check the 32-bit vs 64-bit
                        // of the `dotnet` runner and copy the .so files from the proper x86 or x64 directory.
                        if (isCoreCLR)
                        {
                            cmdString = "bash -c 'source /etc/profile ; cd " + location + "/" + exePath.GetFileNameWithoutExtension() + ";" + "sh ./CoreCLRSetup.sh'";
                            cmd = sshClient.CreateCommand(cmdString);
                            cmd.Execute();
                            var err = cmd.Error;
                            if (!string.IsNullOrEmpty(err))
                            {
                                logger.Error(err);
                                // We don't exit here in case of failure, we just print the error and continue
                                // Users can then try to fix the issue directly on the remote host.
                            }
                            else
                            {
                                err = cmd.Result;
                                if (!string.IsNullOrEmpty(err))
                                {
                                    logger.Info(err);
                                }
                            }
                        }
                        // Try to get the main IP of the machine
                        var ipv4 = GetAllLocalIPv4().FirstOrDefault();
                        var connectionRouter = string.Empty;
                        if (!string.IsNullOrEmpty(ipv4))
                        {
                            connectionRouter = " StrideConnectionRouterRemoteIP=" + ipv4;
                        }
                        var dotnetEngine = StrideEditorSettings.UseCoreCLR.GetValue() ? " dotnet " : " mono ";
                        if (!string.IsNullOrEmpty(display))
                        {
                            display = " DISPLAY=" + display;
                        }
                        else
                        {
                            display = " DISPLAY=:0.0";
                        }
                        cmdString = "bash -c 'source /etc/profile ; cd " + location + "/" + exePath.GetFileNameWithoutExtension() + ";" + display + connectionRouter + dotnetEngine + "./" + exePath.GetFileName() + "'";
                        cmd = sshClient.CreateCommand(cmdString);
                        cmd.BeginExecute((callback) =>
                        {
                            var res = cmd.Error;
                            if (!string.IsNullOrEmpty(res))
                            {
                                logger.Error(res);
                            }
                            else
                            {
                                res = cmd.Result;
                                if (!string.IsNullOrEmpty(res))
                                {
                                    logger.Info(res);
                                }
                            }

                            // Dispose of our resources as soon as we are done.
                            cmd.Dispose();
                            sshClient.Dispose();
                        });
                        return true;
                    }
                }
                catch (Exception)
                {
                    var message = Tr._p("Message", "Unable to launch {0} on host {1}");
                    logger.Error(string.Format(message, exePath, host));
                }
            }

            return false;
        }

        /// <summary>
        /// New Connection info for password based identification.
        /// </summary>
        /// <param name="host">Host where to connect.</param>
        /// <param name="port">Port where to connect on <paramref name="host"/>.</param>
        /// <param name="username">Username used for connection.</param>
        /// <param name="password">Password used for connection.</param>
        /// <returns>A new connection info object.</returns>
        internal static ConnectionInfo NewConnectionInfo(string host, int port, string username, string password)
        {
            var simplePassword = new PasswordAuthenticationMethod(username, password);
            var keyboardInteractive = new KeyboardInteractiveAuthenticationMethod(username);
            keyboardInteractive.AuthenticationPrompt += (sender, args) =>
            {
                foreach (var prompt in args.Prompts)
                {
                    if (prompt.Request.IndexOf("Password:", StringComparison.InvariantCulture) >= 0)
                    {
                        prompt.Response = password;
                    }
                }
            };

            return new ConnectionInfo(host, port, username, simplePassword, keyboardInteractive);
        }

        /// <summary>
        /// Sync <paramref name="sourceDir"/> with <paramref name="destDir"/>
        /// </summary>
        /// <param name="connectInfo">Credentials to access host where synchronization takes place.</param>
        /// <param name="sourceDir">Source directory on the local host.</param>
        /// <param name="destDir">Destination directory on the remote host</param>
        /// <param name="logger">Logging facility</param>
        /// <returns><c>true</c> if successful; otherwise, <c>false</c>.</returns>
        private static bool SyncTo(ConnectionInfo connectInfo, UDirectory sourceDir, UDirectory destDir, LoggerResult logger)
        {
            // Copy files over
            using (var sftp = new SftpClient(connectInfo))
            {
                try
                {
                    sftp.Connect();
                    if (!sftp.IsConnected)
                    {
                        return false;
                    }
                }
                catch
                {
                    logger.Error("Cannot connect");
                    return false;
                }

                // Perform recursive copy of all the folders under `sourceDir`. This is required
                // as the sftp client only synchronize directories at their level only, no
                // subdirectory.
                var dirs = new Queue<DirectoryInfo>();
                dirs.Enqueue(new DirectoryInfo(sourceDir));
                var parentPath = sourceDir;
                while (dirs.Count != 0)
                {
                    var currentDir = dirs.Dequeue();
                    var currentPath = new UDirectory(currentDir.FullName);
                    foreach (var subdir in currentDir.EnumerateDirectories())
                    {
                        dirs.Enqueue(subdir);
                    }

                    // Get the destination path by adding to `Location` the relative path of `sourceDir` to `currentDir`.
                    var destination = UPath.Combine(destDir, currentPath.MakeRelative(parentPath));

                    logger.Info("Synchronizing " + currentPath + " with " + destination.FullPath);
                    // Try to create a remote directory. If it throws an exception, we will assume
                    // for now that the directory already exists. See https://github.com/sshnet/SSH.NET/issues/25
                    try
                    {
                        sftp.CreateDirectory(destination.FullPath);
                        logger.Info("Creating remote directory " + destination.FullPath);
                    }
                    catch (SshException)
                    {
                        // Do nothing, as this is when the directory already exists
                    }
                    // Synchronize files.
                    foreach (var file in sftp.SynchronizeDirectories(currentPath.FullPath, destination.FullPath, "*"))
                    {
                        logger.Info("Updating " + file.Name);
                        // Some of our files needs executable rights, however we do not know in advance which one
                        // need it. For now all files will be rwxr.xr.x (0755 in octal but written in decimal for readability).
                        sftp.ChangePermissions(destination.FullPath + "/" + file.Name, 755);
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Encrypts a given password and returns the encrypted data as a base64 string.
        /// </summary>
        /// <param name="plainText">An unencrypted string that needs to be secured.</param>
        /// <returns>A base64 encoded string that represents the encrypted binary data.</returns>
        public static string Encrypt(string plainText)
        {
            //encrypt data
            var data = Encoding.Unicode.GetBytes(plainText);
            byte[] encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);

            //return as base64 string
            return Convert.ToBase64String(encrypted);
        }

        /// <summary>
        /// Decrypts a given string.
        /// </summary>
        /// <param name="cipher">A base64 encoded string that was created through the <see cref="Encrypt(string)"/>.</param>
        /// <returns>The decrypted string.</returns>
        public static string Decrypt(string cipher)
        {
            try
            {
                //parse base64 string
                byte[] data = Convert.FromBase64String(cipher);

                //decrypt data
                byte[] decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
                return Encoding.Unicode.GetString(decrypted);
            }
            catch
            {
                // In case the password we had stored in irretrievable we discard it.
                return "";
            }
        }

        /// <summary>
        /// Get all IPv4 addresses for the current host using either Ethernet or Wireless that have been assigned via DHCP.
        /// </summary>
        /// <returns>All IPv4 addresses for current host</returns>
        private static string[] GetAllLocalIPv4()
        {
            var ipAddrList = new List<string>();
            foreach (var item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if ((item.OperationalStatus == OperationalStatus.Up) && !item.GetIPProperties().DhcpServerAddresses.IsNullOrEmpty() && (
                    (item.NetworkInterfaceType == NetworkInterfaceType.Ethernet) || (item.NetworkInterfaceType == NetworkInterfaceType.FastEthernetT)
                    || (item.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet) || (item.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)))
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            ipAddrList.Add(ip.Address.ToString());
                        }
                    }
                }
            }
            return ipAddrList.ToArray();
        }
    }
}
