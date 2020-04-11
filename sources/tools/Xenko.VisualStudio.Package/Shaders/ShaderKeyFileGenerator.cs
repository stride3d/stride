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
using Xenko.Core;
using Xenko.VisualStudio.CodeGenerator;
using Xenko.VisualStudio.Commands;

namespace Xenko.VisualStudio.Shaders
{
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid(GuidList.guidXenko_VisualStudio_ShaderKeyFileGenerator)]
    [ProvideObject(typeof(ShaderKeyFileGenerator), RegisterUsing = RegistrationMethod.CodeBase)]
    public class ShaderKeyFileGenerator : BaseCodeGeneratorWithSite
    {
        public const string DisplayName = "Xenko Shader C# Key Generator";
        public const string InternalName = "XenkoShaderKeyGenerator";

        protected override string GetDefaultExtension()
        {
            // Figure out extension (different in case of versions before 3.1.0.2-beta01)
            if (XenkoCommandsProxy.CurrentPackageInfo.ExpectedVersion != null
                && XenkoCommandsProxy.CurrentPackageInfo.ExpectedVersion < new PackageVersion("3.2.0.1-beta02"))
            {
                return ".cs";
            }

            return ".xksl.cs";
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
