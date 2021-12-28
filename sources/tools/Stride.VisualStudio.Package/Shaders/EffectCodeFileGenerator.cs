// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Stride.VisualStudio.CodeGenerator;
using Stride.VisualStudio.Commands;

namespace Stride.VisualStudio.Shaders
{
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid(GuidList.guidStride_VisualStudio_EffectCodeFileGenerator)]
    [ProvideObject(typeof(EffectCodeFileGenerator), RegisterUsing = RegistrationMethod.CodeBase)]
    public class EffectCodeFileGenerator : BaseCodeGeneratorWithSite
    {
        public const string DisplayName = "Stride Effect C# Code Generator";
        public const string InternalName = "StrideEffectCodeGenerator";

        protected override string GetDefaultExtension()
        {
            return ".sdfx.cs";
        }

        protected override byte[] GenerateCode(string inputFileName, string inputFileContent)
        {

            try
            {
                return System.Threading.Tasks.Task.Run(() =>
                {
                    var remoteCommands = StrideCommandsProxy.GetProxy();
                    return remoteCommands.GenerateShaderKeys(inputFileName, inputFileContent);
                }).Result;
            }
            catch (Exception ex)
            {
                GeneratorError(4, ex.ToString(), 0, 0);

                return null;
            }
        }
    }
}
