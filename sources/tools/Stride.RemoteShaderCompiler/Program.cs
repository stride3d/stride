// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Engine.Network;
using Stride.Games;

namespace Stride.RemoteShaderCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            var shaderCompilerServer = new ShaderCompilerServer();
            shaderCompilerServer.Listen(13335);
        }
    }
}
