// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Reflection;

namespace Xenko.Core.Assets.Editor.Services
{
    public class UserDocumentationService
    {
        private readonly Dictionary<string, string> cachedDocumentations = new Dictionary<string, string>();
        private readonly HashSet<string> undocumentedAssemblies = new HashSet<string>();
        private readonly HashSet<string> documentedAssemblies = new HashSet<string>();
        private readonly object lockObj = new object();

        [CanBeNull]
        public string GetMemberDocumentation([NotNull] IMemberDescriptor member, Type rootType)
        {
            if (member == null) throw new ArgumentNullException(nameof(member));
            lock (lockObj)
            {
                string result;
                string key;

                var prefix = member is FieldDescriptor ? 'F' : 'P';
                if (rootType != null)
                {
                    if (CacheAssemblyDocumentation(rootType.Assembly))
                    {

                        // Remove generic type arguments specifications
                        key = $"{prefix}:{rootType.FullName}.{member.Name}";

                        if (cachedDocumentations.TryGetValue(key, out result))
                            return result;
                    }
                }

                if (!CacheAssemblyDocumentation(member.DeclaringType.Assembly))
                    return null;

                var declaringType = Regex.Replace(member.DeclaringType.FullName, @"\[.*\]", "");
                // Remove generic type arguments specifications
                key = $"{prefix}:{declaringType}.{member.Name}";

                return cachedDocumentations.TryGetValue(key, out result) ? result : null;
            }
        }

        [CanBeNull]
        public string GetPropertyKeyDocumentation([NotNull] PropertyKey propertyKey)
        {
            if (propertyKey == null) throw new ArgumentNullException(nameof(propertyKey));
            lock (lockObj)
            {
                var ownerType = propertyKey.OwnerType;
                if (ownerType == null)
                    return null;

                if (!CacheAssemblyDocumentation(ownerType.Assembly))
                    return null;

                var declaringType = ownerType.FullName;
                var suffix = propertyKey.Name.Split('.').Last();
                var key = $"F:{declaringType}.{suffix}";
                return cachedDocumentations.TryGetValue(key, out string result) ? result : null;
            }
        }

        private bool CacheAssemblyDocumentation([NotNull] Assembly assembly)
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
                if (string.IsNullOrEmpty(location))
                {
                    //Try to find the assembly in the loaded assemblies, since Location won't be populated in the case of User assemblies
                    if (ViewModel.SessionViewModel.Instance.CurrentProject?.Package != null)
                    {
                        var package = ViewModel.SessionViewModel.Instance.CurrentProject.Package;
                        foreach (var asm in package.LoadedAssemblies)
                        {
                            var name = asm.Assembly.GetName();
                            if (name.Name == assemblyName)
                            {
                                location = asm.Path;
                                break;
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(location))
                {
                    return false;
                }

                var basePath = Path.Combine(Path.GetDirectoryName(location) ?? "", Path.GetFileNameWithoutExtension(location));
                var docFile = basePath + ".usrdoc";
                if (!File.Exists(docFile))
                {
                    undocumentedAssemblies.Add(assemblyName);
                    return false;
                }
                try
                {
                    using (var reader = new StreamReader(docFile))
                    {
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();
                            if (string.IsNullOrWhiteSpace(line))
                                continue;

                            var separator = line.IndexOf('=');
                            // TODO: Emit a warning here.
                            if (separator < 0 || separator >= line.Length - 1)
                                continue;

                            var key = line.Substring(0, separator);
                            var documentation = line.Substring(separator + 1);
                            cachedDocumentations[key] = documentation;
                        }
                    }
                    documentedAssemblies.Add(assemblyName);
                }
                catch
                {
                    undocumentedAssemblies.Add(assemblyName);
                }
            }

            return documentedAssemblies.Contains(assemblyName);
        }
    }
}
