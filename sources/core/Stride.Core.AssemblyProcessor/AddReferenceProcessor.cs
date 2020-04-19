// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace Stride.Core.AssemblyProcessor
{
    class AddReferenceProcessor : IAssemblyDefinitionProcessor
    {
        private readonly IList<string> referencesToAdd;

        public AddReferenceProcessor(IList<string> referencesToAdd)
        {
            this.referencesToAdd = referencesToAdd;
        }

        public bool Process(AssemblyProcessorContext context)
        {
            bool changed = false;

            // Make sure all assemblies in serializatonProjectReferencePaths are referenced (sometimes they might be optimized out if no direct references)
            foreach (var serializatonProjectReferencePath in referencesToAdd)
            {
                var shortAssemblyName = Path.GetFileNameWithoutExtension(serializatonProjectReferencePath);
            
                // Still in references (not optimized)
                if (context.Assembly.MainModule.AssemblyReferences.Any(x => x.Name == shortAssemblyName))
                    continue;
            
                if (!File.Exists(serializatonProjectReferencePath))
                    continue;
            
                // For now, use AssemblyDefinition.ReadAssembly to compute full name -- maybe not very efficient but it shouldn't happen often anyway)
                var referencedAssembly = AssemblyDefinition.ReadAssembly(serializatonProjectReferencePath, new ReaderParameters { AssemblyResolver = context.AssemblyResolver });

                context.Assembly.MainModule.AssemblyReferences.Add(AssemblyNameReference.Parse(referencedAssembly.FullName));
                changed = true;
            }

            return changed;
        }
    }
}
