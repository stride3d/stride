// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.BuildEngine;

namespace Stride.Editor.Build
{
    public class PrecompiledAssetBuildUnit : AssetBuildUnit
    {
        private readonly ListBuildStep buildStep;

        private readonly bool mergeInCommonDatabase;

        public PrecompiledAssetBuildUnit(AssetBuildUnitIdentifier identifier, ListBuildStep buildStep, bool mergeInCommonDatabase = false)
            : base(identifier)
        {
            this.buildStep = buildStep;
            this.mergeInCommonDatabase = mergeInCommonDatabase;
        }

        protected override ListBuildStep Prepare()
        {
            return buildStep;
        }

        protected override void PostBuild()
        {
            base.PostBuild();
            if (mergeInCommonDatabase)
            {
                MicrothreadLocalDatabases.AddToSharedGroup(buildStep.OutputObjects);
            }
        }
    }
}
