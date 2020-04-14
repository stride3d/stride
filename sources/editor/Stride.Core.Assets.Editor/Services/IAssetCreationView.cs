// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.IO;

namespace Xenko.Core.Assets.Editor.Services
{
    public interface IAssetCreationView
    {
        Task<AssetItem> Create(UFile defaultUrl, SessionViewModel sessionViewModel, DirectoryBaseViewModel targetDirectoryViewModel);
    }
}
