// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Xenko.Core.Assets
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
