// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Compiler;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.BuildEngine;

namespace Xenko.Editor.Build
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
