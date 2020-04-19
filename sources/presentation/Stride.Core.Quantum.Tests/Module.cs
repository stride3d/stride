// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Reflection;

namespace Stride.Core.Quantum.Tests
{
    public class Module
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ShadowObject.Enable = true;
        }
    }
}
