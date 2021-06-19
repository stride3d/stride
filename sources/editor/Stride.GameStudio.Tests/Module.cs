// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Reflection;

namespace Stride.GameStudio.Tests
{
    public class Module
    {
        [Core.ModuleInitializer]
        internal static void Initialize()
        {
            PackageSessionPublicHelper.FindAndSetMSBuildVersion();
        }
    }
}
