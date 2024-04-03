// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;

namespace Stride.Core.Assets.CompilerApp;
class Program
{
    private static int Main(string[] args)
    {
        try
        {
            var packageBuilder = new PackageBuilderApp();
            var returnValue =  packageBuilder.Run(args);

            return returnValue;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Unexpected exception in AssetCompiler: {0}", ex);
            return 1;
        }
    }
}
