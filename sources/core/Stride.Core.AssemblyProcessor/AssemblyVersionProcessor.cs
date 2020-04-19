// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Mono.Cecil;

namespace Stride.Core.AssemblyProcessor
{
    internal class AssemblyVersionProcessor : IAssemblyDefinitionProcessor
    {
        public bool Process(AssemblyProcessorContext context)
        {
            var assembly = context.Assembly;
            var mscorlibAssembly = CecilExtensions.FindCorlibAssembly(assembly);
            if (mscorlibAssembly == null)
                throw new InvalidOperationException("Missing mscorlib.dll from assembly");


            // Resolve mscorlib types
            var assemblyFileVersionAttributeType = mscorlibAssembly.MainModule.GetTypeResolved(typeof(AssemblyFileVersionAttribute).FullName);
            var assemblyMethodConstructor = assembly.MainModule.ImportReference(assemblyFileVersionAttributeType.Methods.FirstOrDefault(method => method.IsConstructor && method.Parameters.Count == 1));
            var stringType = assembly.MainModule.TypeSystem.String;

            // TODO: Git Commit SHA
            var gitCommitShortId = "0";

            // Use epoch time to get a "unique" build number (different each time)
            var build = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds;

            // Get current AssemblyVersion and clone it
            var version = assembly.Name.Version;
            var fileVersion = string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, build, gitCommitShortId);

            // Copy build/revision to the AssemblyFileVersion
            bool fileVersionUpdated = false;
            for (int i = 0; i < assembly.CustomAttributes.Count; i++)
            {
                var customAttribute = assembly.CustomAttributes[i];
                if (customAttribute.AttributeType.FullName == typeof(AssemblyFileVersionAttribute).FullName)
                {
                    customAttribute.ConstructorArguments.Clear();
                    customAttribute.ConstructorArguments.Add(new CustomAttributeArgument(stringType, fileVersion));
                    fileVersionUpdated = true;
                    break;
                }
            }

            if (!fileVersionUpdated)
            {
                var assemblyFileVersion = new CustomAttribute(assemblyMethodConstructor);
                assemblyFileVersion.ConstructorArguments.Add(new CustomAttributeArgument(stringType, fileVersion));
                assembly.CustomAttributes.Add(assemblyFileVersion);
            }

            return true;
        }
    }
}
