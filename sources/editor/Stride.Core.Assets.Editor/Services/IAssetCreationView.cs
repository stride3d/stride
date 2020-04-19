// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.IO;

namespace Stride.Core.Assets.Editor.Services
{
    public interface IAssetCreationView
    {
        Task<AssetItem> Create(UFile defaultUrl, SessionViewModel sessionViewModel, DirectoryBaseViewModel targetDirectoryViewModel);
    }
}
