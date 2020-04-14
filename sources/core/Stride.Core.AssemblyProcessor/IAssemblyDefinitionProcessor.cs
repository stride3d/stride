// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Mono.Cecil;

namespace Stride.Core.AssemblyProcessor
{
    internal interface IAssemblyDefinitionProcessor
    {
        bool Process(AssemblyProcessorContext context);
    }
}
