// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.BuildEngine;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.Assets.Editor.Services
{
    public struct AssetBuildUnitIdentifier
    {
        public static AssetBuildUnitIdentifier Default = new AssetBuildUnitIdentifier(Guid.Empty, AssetId.Empty);
        
        public AssetBuildUnitIdentifier(Guid contextIdentifier, AssetId assetIdentifier)
        {
            ContextIdentifier = contextIdentifier;
            AssetIdentifier = assetIdentifier;
        }

        public Guid ContextIdentifier { get; }

        public AssetId AssetIdentifier { get; }
    }

    public abstract class AssetBuildUnit : IComparable<AssetBuildUnit>
    {
        private readonly TaskCompletionSource<ResultStatus> taskCompletionSource = new TaskCompletionSource<ResultStatus>();
        private ListBuildStep buildStep;

        protected AssetBuildUnit(AssetBuildUnitIdentifier identifier)
        {
            Identifier = identifier;
        }

        public int PriorityMajor { get; set; }

        public int PriorityMinor { get; set; }

        public AssetBuildUnitIdentifier Identifier { get; private set; }

        public bool Processed => buildStep.Processed;

        public bool Succeeded => buildStep.Succeeded;

        public bool Failed => buildStep.Failed;

        public IReadOnlyDictionary<ObjectUrl, OutputObject> OutputObjects => buildStep.OutputObjects;

        public ListBuildStep GetBuildStep()
        {
            try
            {
                buildStep = Prepare();
            }
            catch (Exception)
            {
                // TODO: properly log errors
                //Builder.Logger.Error("An exception was triggered during the compilation of the preview items '{0}':\n" + e.Message, AssetItem.Location);
                return null;
            }
            if (buildStep != null)
            {
                buildStep.StepProcessed += StepProcessed;
            }
            return buildStep;
        }

        public async Task<ResultStatus> Wait()
        {
            return await taskCompletionSource.Task;
        }

        protected abstract ListBuildStep Prepare();

        protected virtual void PostBuild()
        {
            // Intentionally does nothing by default.
        }

        private void StepProcessed(object sender, BuildStepEventArgs e)
        {
            e.Step.StepProcessed -= StepProcessed;
            PostBuild();
            taskCompletionSource.SetResult(e.Step.Status);
        }

        public int CompareTo(AssetBuildUnit other)
        {
            var priorityMajorDiff = PriorityMajor.CompareTo(other.PriorityMajor);
            if (priorityMajorDiff != 0)
                return priorityMajorDiff;

            return PriorityMinor.CompareTo(other.PriorityMinor);
        }
    }
}
