// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using Mono.Cecil.Rocks;

namespace Stride.Core.AssemblyProcessor
{
    internal class InitLocalsProcessor : IAssemblyDefinitionProcessor
    {
        public bool Process(AssemblyProcessorContext context)
        {
            bool changed = false;
            foreach (var type in context.Assembly.MainModule.GetAllTypes())
            {
                foreach (var method in type.Methods)
                {
                    if (method.CustomAttributes.Any(x => x.AttributeType.FullName == "Stride.Core.IL.RemoveInitLocalsAttribute"))
                    {
                        if (method.Body == null)
                        {
                            throw new InvalidOperationException($"Trying to remove initlocals from method {method.FullName} without body.");
                        }

                        method.Body.InitLocals = false;
                        changed = true;
                    }
                }
            }

            return changed;
        }
    }
}
