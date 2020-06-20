// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;

using Stride.Core;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.CompilerApp
{
    public class PackageBuilderOptions
    {
        public readonly LoggerResult Logger;

        public bool Verbose = false;
        public bool Debug = false;
        // This should not be a list
        public bool DisableAutoCompileProjects { get; set; }
        public string ProjectConfiguration { get; set; }
        public string OutputDirectory { get; set; }
        public string BuildDirectory { get; set; }
        public string SolutionFile { get; set; }
        public Guid PackageId { get; set; }
        public PlatformType Platform { get; set; }
        public string PackageFile { get; set; }
        public string MSBuildUpToDateCheckFileBase { get; set; }
        public List<string> MonitorPipeNames = new List<string>();
        public bool EnableFileLogging;
        public string CustomLogFileName;
        public string SlavePipe;
        public Dictionary<string, string> Properties = new Dictionary<string, string>();
        public Dictionary<string, string> ExtraCompileProperties;


        public int ThreadCount = Environment.ProcessorCount;

        public string TestName;

        public PackageBuilderOptions(LoggerResult logger)
        {
            if (logger == null) throw new ArgumentNullException("logger");
            Logger = logger;
        }

        /// <summary>
        /// Gets the logger messages type depending on the current options
        /// </summary>
        public LogMessageType LoggerType
        {
            get { return Debug ? LogMessageType.Debug : (Verbose ? LogMessageType.Verbose : LogMessageType.Info); }
        }

        /// <summary>
        /// This function indicate if the current builder options mean to execute a slave session
        /// </summary>
        /// <returns>true if the options mean to execute a slave session</returns>
        public bool IsValidForSlave()
        {
            return !string.IsNullOrEmpty(SlavePipe) && !string.IsNullOrEmpty(BuildDirectory);
        }

        /// <summary>
        /// Ensure every parameter is correct for a master execution. Throw an OptionException if a parameter is wrong
        /// </summary>
        /// <exception cref="Mono.Options.OptionException">This tool requires one input file.;filename
        /// or
        /// The given working directory \ + workingDir + \ does not exist.;workingdir</exception>
        public void ValidateOptions()
        {
            if (string.IsNullOrWhiteSpace(BuildDirectory))
                throw new ArgumentException("This tool requires a build path.", "build-path");

            try
            {
                BuildDirectory = Path.GetFullPath(BuildDirectory);
            }
            catch (Exception)
            {
                throw new ArgumentException("The provided path is not a valid path name.", "build-path");
            }

            if (SlavePipe == null)
            {
                if (string.IsNullOrWhiteSpace(PackageFile))
                {
                    if (string.IsNullOrWhiteSpace(SolutionFile) || PackageId == Guid.Empty)
                    {
                        throw new ArgumentException("This tool requires either a --package-file, or a --solution-file and --package-id.", "inputPackageFile");
                    }
                }
                else if (!File.Exists(PackageFile))
                {
                    throw new ArgumentException("Package file [{0}] doesn't exist".ToFormat(PackageFile), "inputPackageFile");
                }
            }
        }
    }
}
