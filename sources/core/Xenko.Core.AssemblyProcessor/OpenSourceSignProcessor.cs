// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Mono.Cecil;

namespace Xenko.Core.AssemblyProcessor
{
    internal class OpenSourceSignProcessor : IAssemblyDefinitionProcessor
    {
        public bool Process(AssemblyProcessorContext context)
        {
            var assembly = context.Assembly;

            // Only process if there is a public key
            if (!assembly.Name.HasPublicKey)
                return false;

            // Check if already strong signed
            if ((assembly.MainModule.Attributes & ModuleAttributes.StrongNameSigned) == ModuleAttributes.StrongNameSigned)
                return false;

            // We have a delay signed assembly that is not strong name signed yet.
            // Let's strong sign it now (a.k.a. OSS, OpenSourceSign)
            // Note: Maybe we should make sure it's actually Xenko key?
            assembly.MainModule.Attributes |= ModuleAttributes.StrongNameSigned;

            return true;
        }
    }
}
