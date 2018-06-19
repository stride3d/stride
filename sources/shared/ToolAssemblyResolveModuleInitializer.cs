// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Xenko.Core;

namespace Xenko
{
    /// <summary>
    /// Automatically copy Direct3D11 assemblies at the top level so that tools can find them.
    /// Note: we could use "probing" but it turns out to be slow (esp. on ExecServer where it slows down startup from almost 0 to 0.8 sec!)
    /// </summary>
    static class ToolAssemblyResolveModuleInitializer
    {
        // List of folders to copy
        private static readonly Dictionary<string, string> SearchPaths = new Dictionary<string, string>
        {
            { @"Direct3D11", @"." },
        };

        // Should execute before almost everything else
        [ModuleInitializer(-100000)]
        internal static void Setup()
        {
            foreach (var searchPath in SearchPaths)
            {
                var sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, searchPath.Key);
                var destPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, searchPath.Value);

                // Make sure output directory exist
                Directory.CreateDirectory(destPath);

                // Search source files
                foreach (var filename in Directory.EnumerateFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                {
                    var sourceFile = new FileInfo(filename);
                    var destFile = new FileInfo(filename.Replace(sourcePath, destPath));

                    // Only copy if doesn't exist or newer
                    if (!destFile.Exists || sourceFile.LastWriteTime > destFile.LastWriteTime)
                    {
                        try
                        {
                            // Out of safety, patch .ssdeps
                            // Note: shouldn't be used: unit tests should directly add all the graphics specific assemblies as primary project references, and same for actual games (automatically done through the Xenko.targets)
                            var extension = sourceFile.Extension;
                            if (extension?.ToLowerInvariant() == ".ssdeps")
                            {
                                var dependencies = File.ReadAllLines(sourceFile.FullName);
                                for (var index = 0; index < dependencies.Length; index++)
                                {
                                    var dependency = dependencies[index];

                                    // Patch the second item: build a new relative path from destination
                                    var dependencyEntries = dependency.Split(';');
                                    var fullPath = Path.Combine(sourceFile.DirectoryName, dependencyEntries[1]);
                                    dependencyEntries[1] = new Uri(destFile.FullName).MakeRelativeUri(new Uri(fullPath)).ToString().Replace('/', '\\');

                                    dependencies[index] = string.Join(";", dependencyEntries);
                                }

                                File.WriteAllLines(destFile.FullName, dependencies);
                            }
                            else
                            {
                                // now you can safely overwrite it
                                Directory.CreateDirectory(destFile.DirectoryName);
                                sourceFile.CopyTo(destFile.FullName, true);
                            }
                        }
                        catch
                        {
                            // Mute exceptions
                            // Not ideal, but better than crashing
                            // Let's see when it happens...
                            if (System.Diagnostics.Debugger.IsAttached)
                                System.Diagnostics.Debugger.Break();
                        }
                    }
                }
            }
        }
    }
}
