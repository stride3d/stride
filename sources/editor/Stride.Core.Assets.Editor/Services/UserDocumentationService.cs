// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Editor.Services
{
    public class UserDocumentationService
    {
        private static readonly Logger Log = GlobalLogger.GetLogger(nameof(UserDocumentationService));

        private readonly Dictionary<string, string> cachedDocumentations = new Dictionary<string, string>();
        private readonly HashSet<string> undocumentedAssemblies = new HashSet<string>();
        private readonly HashSet<string> documentedAssemblies = new HashSet<string>();
        private readonly object lockObj = new object();

        [CanBeNull]
        public string GetMemberDocumentation([NotNull] IMemberDescriptor member, Type rootType)
        {
            if (member == null) throw new ArgumentNullException(nameof(member));
            string result;
            string key;

            var prefix = member is FieldDescriptor ? 'F' : 'P';
            if (rootType != null && CacheAssemblyDocumentation(rootType.Assembly))
            {
                // Remove generic type arguments specifications
                key = $"{prefix}:{rootType.FullName}.{member.Name}";

                lock (lockObj)
                {
                    if (cachedDocumentations.TryGetValue(key, out result))
                        return result;
                }
            }

            if (!CacheAssemblyDocumentation(member.DeclaringType.Assembly))
                return null;

            var declaringType = Regex.Replace(member.DeclaringType.FullName, @"\[.*\]", "").Replace('+', '.');

            // Remove generic type arguments specifications
            key = $"{prefix}:{declaringType}.{member.Name}";

            lock (lockObj)
            {
                return cachedDocumentations.TryGetValue(key, out result) ? result : null;
            }            
        }

        [CanBeNull]
        public string GetPropertyKeyDocumentation([NotNull] PropertyKey propertyKey)
        {
            if (propertyKey == null) throw new ArgumentNullException(nameof(propertyKey));
            var ownerType = propertyKey.OwnerType;
            if (ownerType == null)
                return null;

            if (!CacheAssemblyDocumentation(ownerType.Assembly))
                return null;

            var declaringType = ownerType.FullName.Replace('+', '.');
            var suffix = propertyKey.Name.Split('.').Last();
            var key = $"F:{declaringType}.{suffix}";

            lock (lockObj)
            {
                return cachedDocumentations.TryGetValue(key, out string result) ? result : null;
            }            
        }

        public void ClearCachedAssemblyDocumentation([NotNull] Assembly assembly)
        {
            var assemblyName = assembly.GetName().Name;

            lock (lockObj)
            {
                documentedAssemblies.Remove(assemblyName);
                undocumentedAssemblies.Remove(assemblyName);
            }
        }

        public bool CacheAssemblyDocumentation([NotNull] Assembly assembly)
        {
            // Can't process dynamic assemblies (they don't have a location)
            if (assembly.IsDynamic)
                return false;

            var assemblyName = assembly.GetName().Name;

            // Check if the assembly has already been marked as undocumented
            if (undocumentedAssemblies.Contains(assemblyName))
                return false;

            // If no documentation has been loaded yet, attempt to load it now
            if (!documentedAssemblies.Contains(assemblyName))
            {
                var location = assembly.Location;
                if (string.IsNullOrEmpty(location) && ViewModel.SessionViewModel.Instance.CurrentProject?.Package != null)
                {
                    //Try to find the assembly in the loaded assemblies, since Location won't be populated in the case of User assemblies
                    var package = ViewModel.SessionViewModel.Instance.CurrentProject.Package;

                    if (package.Container is SolutionProject solutionProject && solutionProject.Type == ProjectType.Executable)
                    {
                        Log.Info($"Package {solutionProject.Name} is a solution project. Attempting to cache documentation for dependencies.");
                        foreach (var dep in solutionProject.DirectDependencies)
                        {
                            var docPath = Path.Combine(Path.GetDirectoryName(solutionProject.TargetPath) ?? "", dep.Name + ".xml");

                            CacheAssemblyDocumentationFromPath(dep.Name, docPath);
                        }
                    }
                    else
                    {
                        foreach (var asm in package.LoadedAssemblies)
                        {
                            var name = asm.Assembly.GetName();
                            if (name.Name == assemblyName)
                            {
                                CacheAssemblyDocumentationFromPath(assemblyName, asm.Path);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    CacheAssemblyDocumentationFromPath(assemblyName, location);
                }

            }

            return documentedAssemblies.Contains(assemblyName);
        }

        private void CacheAssemblyDocumentationFromPath(string assemblyName, string location)
        {
            if (string.IsNullOrEmpty(location) || documentedAssemblies.Contains(assemblyName))
            {
                return;
            }


            var basePath = Path.Combine(Path.GetDirectoryName(location) ?? "", Path.GetFileNameWithoutExtension(location));
            if (!CacheCustomDocumentation(basePath + ".usrdoc"))
            {
                Log.Info($"Could not cache from {basePath + ".usrdoc"}. Attempting to read from {basePath + ".xml"}");
                // Fallback to XML assembly.
                if (!CacheXmlDocumentation(basePath + ".xml"))
                {
                    lock (lockObj)
                    {
                        undocumentedAssemblies.Add(assemblyName);
                    }
                    return;
                }
            }

            lock (lockObj)
            {
                documentedAssemblies.Add(assemblyName);
            }
        }

        /// <summary>
        /// Attempts to cache the contents of a file formatted in the style of the standard .xml documentation file.
        /// </summary>
        /// <param name="filePath">The file location.</param>
        /// <returns>Returns true if the file was successfully cached or empty.</returns>
        private bool CacheXmlDocumentation(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            try
            {
                using (var reader = new StreamReader(filePath))
                {
                    var doc = new XmlDocument();
                    doc.Load(reader);

                    foreach (XmlNode node in doc.GetElementsByTagName("userdoc"))
                    {
                        var key = node.ParentNode.Attributes["name"]?.Value;
                        var documentation = node.InnerText.Trim();

                        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(documentation))
                        {
                            continue;
                        }

                        lock (lockObj)
                        {
                            cachedDocumentations[key] = documentation;
                        }
                    }
                }
            }
            catch
            {
                Log.Error("Failed while caching from XML documentation.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Attempts to cache the contents of a file formatted in the .usrdoc style.
        /// </summary>
        /// <param name="filePath">The file location.</param>
        /// <returns>Returns true if the file was successfully cached or empty.</returns>
        private bool CacheCustomDocumentation(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            try
            {
                using (var reader = new StreamReader(filePath))
                {
                    int lineNumber = 0;
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        lineNumber++;

                        var separator = line.IndexOf('=');
                        if (separator < 0 || separator >= line.Length - 1)
                        {
                            Log.Warning($"Invalid doc format. File: {filePath}, Line {lineNumber}");
                            continue;
                        }

                        var key = line.Substring(0, separator);
                        var documentation = line.Substring(separator + 1);

                        lock (lockObj)
                        {
                            cachedDocumentations[key] = documentation;
                        }
                    }
                }
            }
            catch
            {
                Log.Error("Failed while caching documentation.");
                return false;
            }

            return true;
        }
    }
}
