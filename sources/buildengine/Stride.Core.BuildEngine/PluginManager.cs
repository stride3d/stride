// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Diagnostics;

namespace Stride.Core.BuildEngine
{
    public class PluginManager
    {
        public IEnumerable<string> PluginAssemblyLocations { get { return pluginAssemblyLocations; } }

        private readonly List<string> pluginAssemblyLocations = new List<string>();

        private readonly Logger logger;

        public PluginManager(Logger logger = null)
        {
            this.logger = logger;
        }

        public void Register()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, e) => LoadAssembly(new AssemblyName(e.Name));
        }

        public Assembly LoadAssembly(AssemblyName assemblyName)
        {
            return LoadAssembly(assemblyName.Name);
        }

        public Assembly LoadAssembly(string assemblyName)
        {
            foreach (string pluginLocation in pluginAssemblyLocations)
            {
                if (pluginLocation != assemblyName)
                {
                    string fileName = Path.GetFileNameWithoutExtension(pluginLocation);
                    if (fileName != assemblyName)
                        continue;
                }

                if (logger != null)
                    logger.Debug("Loading plugin: {0}", pluginLocation);
                return Assembly.LoadFrom(pluginLocation);
            }
            return null;
        }

        public string FindAssembly(string assemblyFileName)
        {
            foreach (string pluginLocation in pluginAssemblyLocations)
            {
                if (pluginLocation != assemblyFileName)
                {
                    string fileName = Path.GetFileName(pluginLocation);
                    if (fileName != assemblyFileName)
                        continue;
                }

                if (logger != null)
                    logger.Debug("Loading plugin: {0}", pluginLocation);
                return pluginLocation;
            }
            return null;
        }

        public void AddPlugin(string filePath)
        {
            pluginAssemblyLocations.Add(filePath);
        }

        public void AddPluginFolder(string folder)
        {
            if (!Directory.Exists(folder))
                return;

            foreach (string filePath in Directory.EnumerateFiles(folder, "*.dll"))
            {
                if (logger != null)
                    logger.Debug("Detected plugin: {0}", Path.GetFileNameWithoutExtension(filePath));
                pluginAssemblyLocations.Add(filePath);
            }
        }
    }
}
