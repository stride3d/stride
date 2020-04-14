// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.BuildEngine;
using Stride.Core.Serialization;

namespace Stride.Core.Assets.Compiler
{
    /// <summary>
    /// A command processing an <see cref="Asset"/>.
    /// </summary>
    public abstract class AssetCommand : IndexFileCommand
    {
        public string Url { get; set; }

        protected AssetCommand(string url)
        {
            Url = url;
        }
    }

    public abstract class AssetCommand<T> : AssetCommand
    {
        protected readonly IAssetFinder AssetFinder;

        /// <summary>
        /// This is useful if the asset binary format has changed and we want to bump the version to force re-evaluation/compilation of the command
        /// </summary>
        protected int Version;

        protected AssetCommand(string url, T parameters, IAssetFinder assetFinder)
            : base (url)
        {
            Parameters = parameters;
            AssetFinder = assetFinder;
        }

        public T Parameters { get; set; }
        
        public override string Title => $"Asset command processing {Url}";

        protected override void ComputeParameterHash(BinarySerializationWriter writer)
        {
            base.ComputeParameterHash(writer);

            writer.Serialize(ref Version);
            
            var url = Url;
            var assetParameters = Parameters;
            writer.SerializeExtended(ref assetParameters, ArchiveMode.Serialize);
            writer.Serialize(ref url, ArchiveMode.Serialize);
        }

        public override string ToString()
        {
            // TODO provide automatic asset to string via YAML
            return $"[{Url}] {Parameters}";
        }
    }
}
