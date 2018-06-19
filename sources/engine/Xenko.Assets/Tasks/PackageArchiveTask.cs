// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Xenko.Core.Assets;
using Xenko.Core.Diagnostics;

namespace Xenko.Assets.Tasks
{
    public class PackageArchiveTask : Task
    {
        /// <summary>
        /// Gets or sets the file.
        /// </summary>
        /// <value>The file.</value>
        [Required]
        public ITaskItem File { get; set; }

        public override bool Execute()
        {

            var result = new LoggerResult();
            var package = Package.Load(result, File.ItemSpec, new PackageLoadParameters()
                {
                    AutoCompileProjects = false,
                    LoadAssemblyReferences = false,
                    AutoLoadTemporaryAssets = false,
                });

            foreach (var message in result.Messages)
            {
                if (message.Type >= LogMessageType.Error)
                {
                    Log.LogError(message.ToString());
                }
                else if (message.Type == LogMessageType.Warning)
                {
                    Log.LogWarning(message.ToString());
                }
                else
                {
                    Log.LogMessage(message.ToString());
                }
            }

            // If we have errors loading the package, exit
            if (result.HasErrors)
            {
                return false;
            }

            Log.LogMessage(MessageImportance.High, "Packaging [{0}] version [{1}]", package.Meta.Name, package.Meta.Version);

            var log = new LoggerResult();

            // Build the package
            PackageArchive.Build(log, package);

            // Output log
            foreach (var message in log.Messages)
            {
                MessageImportance importance;
                switch (message.Type)
                {
                    case LogMessageType.Debug:
                    case LogMessageType.Verbose:
                        importance = MessageImportance.Low;
                        break;
                    case LogMessageType.Info:
                        importance = MessageImportance.Normal;
                        break;
                    case LogMessageType.Warning:
                    case LogMessageType.Error:
                    case LogMessageType.Fatal:
                        importance = MessageImportance.High;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                Log.LogMessage(importance, message.Text);
            }

            return true;
        }
    }
}
