// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Assets
{
    /// <summary>
    /// An asset that generates another file.
    /// </summary>
    public interface IProjectFileGeneratorAsset : IProjectAsset
    {
        string Generator { get; }

        void SaveGeneratedAsset(AssetItem assetItem);
    }
}
