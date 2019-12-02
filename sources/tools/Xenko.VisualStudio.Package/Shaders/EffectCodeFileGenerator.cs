// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Xenko.VisualStudio.CodeGenerator;
using Xenko.VisualStudio.Commands;

namespace Xenko.VisualStudio.Shaders
{
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid(GuidList.guidXenko_VisualStudio_EffectCodeFileGenerator)]
    [ProvideObject(typeof(EffectCodeFileGenerator), RegisterUsing = RegistrationMethod.CodeBase)]
    public class EffectCodeFileGenerator : BaseCodeGeneratorWithSite
    {
        public const string DisplayName = "Xenko Effect C# Code Generator";
        public const string InternalName = "XenkoEffectCodeGenerator";

        protected override string GetDefaultExtension()
        {
            return ".xkfx.cs";
        }

        protected override byte[] GenerateCode(string inputFileName, string inputFileContent)
        {

            try
            {
                return System.Threading.Tasks.Task.Run(() =>
                {
                    var remoteCommands = XenkoCommandsProxy.GetProxy();
                    return remoteCommands.GenerateShaderKeys(inputFileName, inputFileContent);
                }).Result;
            }
            catch (Exception ex)
            {
                GeneratorError(4, ex.ToString(), 0, 0);

                return new byte[0];
            }
        }
    }
}
