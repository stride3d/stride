// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Templates;
using Stride.Core.Assets.Tracking;
using Stride.Core;
using Stride.Core.Reflection;
using Stride.Core.Yaml;

namespace Stride.Core.Assets
{
    internal class Module
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            // Shadow object is always enabled when we are using assets, so we force it here
            ShadowObject.Enable = true;

            // Make sure that this assembly is registered
            AssemblyRegistry.Register(typeof(Module).Assembly, AssemblyCommonCategories.Assets);

            AssetYamlSerializer.Default.PrepareMembers += SourceHashesHelper.AddSourceHashesMember;
        }
    }
}
