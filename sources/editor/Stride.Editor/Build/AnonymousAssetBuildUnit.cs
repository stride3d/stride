// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.BuildEngine;

namespace Stride.Editor.Build
{
    public class AnonymousAssetBuildUnit : AssetBuildUnit
    {
        private readonly Func<ListBuildStep> compile;

        public AnonymousAssetBuildUnit(AssetBuildUnitIdentifier identifier, Func<ListBuildStep> compile)
            : base(identifier)
        {
            this.compile = compile;
        }

        protected override ListBuildStep Prepare()
        {
            return compile();
        }
    }
}
