// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Stride.Core.AssemblyProcessor
{
    /// <summary>
    /// Execute a custom tasks that will log a simple message
    /// </summary>
    public class AssemblyProcessorTask : Task
    {
        // Required property indicated by Required attribute
        [Required]
        public string Arguments { get; set; }

        /// <summary>
        /// This method is called automatically when the task is run.
        /// </summary>
        /// <returns>Boolean to indicate if the task was sucessful.</returns>
        public override bool Execute()
        {
            // Override assembly resolve (cf comments in CurrentDomain_AssemblyResolve)
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            try
            {
                var args = ParseArguments(Arguments);
                var processor = new AssemblyProcessorProgram();
                var redirectLogger = new RedirectLogger(Log);
                var result = processor.Run(args.ToArray(), redirectLogger);

                if (result != 0)
                {
                    Log.LogError($"Failed to run assembly processor with parameters: {Arguments}");
                    Log.LogError("Check the previous logs");
                }

                return result == 0;
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
            }
        }

        private System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);

            // Note: Ideally we would just like to use AssemblyProcessor app.config temporarily
            // This is a workaround to get MSBuild/VS to load our dependencies properly
            // Try to resolve assemblies from current folder
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), assemblyName.Name + ".dll");
            if (File.Exists(path))
            {
                return Assembly.LoadFrom(path);
            }

            return null;
        }

        /// <summary>
        /// Recompose command line arguments as they were passed to a console app.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private static List<string> ParseArguments(string parameters)
        {
            var args = new List<string>();
            bool isInString = false;
            var builder = new StringBuilder();
            for (int i = 0; i < parameters.Length; i++)
            {
                var c = parameters[i];

                if (c == '"')
                {
                    if (isInString)
                    {
                        args.Add(builder.ToString());
                        builder.Clear();
                        isInString = false;
                    }
                    else
                    {
                        isInString = true;
                    }
                }
                else if (!char.IsWhiteSpace(c) || isInString)
                {
                    builder.Append(c);
                }
                else if (char.IsWhiteSpace(c) && builder.Length > 0)
                {
                    args.Add(builder.ToString());
                    builder.Clear();
                }
            }
            if (builder.Length > 0)
            {
                args.Add(builder.ToString());
            }
            return args;
        }

        /// <summary>
        /// Heler class to redirect logs
        /// </summary>
        private class RedirectLogger : TextWriter
        {
            private readonly TaskLoggingHelper taskLogger;
            private readonly StringBuilder content = new StringBuilder();

            public RedirectLogger(TaskLoggingHelper taskLogger)
            {
                this.taskLogger = taskLogger;
            }

            public override void Write(char value)
            {
                if (value == '\r')
                {
                    return;
                }
                if (value != '\n')
                {
                    content.Append(value);
                }
                else
                {
                    taskLogger.LogMessage(MessageImportance.High, content.ToString());
                    content.Clear();
                }
            }

            public override Encoding Encoding => Encoding.UTF8;
        }
    }
}
