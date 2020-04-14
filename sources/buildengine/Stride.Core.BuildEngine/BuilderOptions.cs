// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;

using Mono.Options;
using Stride.Core.Diagnostics;

namespace Stride.Core.BuildEngine
{
    public class BuilderOptions
    {
        public readonly PluginResolver Plugins;
        public readonly Logger Logger;

        public bool Verbose = false;
        public bool Debug = false;
        // This should not be a list
        public List<string> InputFiles = new List<string>();
        public Builder.Mode BuilderMode;
        public string BuildDirectory;
        public List<string> MonitorPipeNames = new List<string>();
        public string OutputDirectory;
        public string Configuration;
        public bool EnableFileLogging;
        public bool Append;
        public string CustomLogFileName;
        public string SourceBaseDirectory;
        public string MetadataDatabaseDirectory;
        public string SlavePipe;

        public int ThreadCount = Environment.ProcessorCount;

        public string TestName;

        public BuilderOptions(Logger logger)
        {
            Logger = logger;
            Plugins = new PluginResolver(logger);
            BuilderMode = Builder.Mode.Build;
        }

        /// <summary>
        /// This function indicate if the current builder options mean to execute a master session
        /// </summary>
        /// <returns>true if the options mean to execute a master session</returns>
        public bool IsValidForMaster()
        {
            return InputFiles.Count == 1;
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
        public void ValidateOptionsForMaster()
        {
            if (InputFiles.Count != 1)
                throw new OptionException("This tool requires one input file.", "filename");

            if (SourceBaseDirectory != null)
            {
                if (!Directory.Exists(SourceBaseDirectory))
                    throw new OptionException("The given working directory does not exist.", "workingdir");
            }
        }
    }
}
