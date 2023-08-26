// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.LauncherApp
{
    static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            PrerequisitesValidator.Validate(args);
            Launcher.Main(args);
        }
    }
}
