// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.IO;
using Xenko.Core.Presentation.Services;

namespace Xenko.Core.Assets.Editor.Services
{
    public interface INewProjectDialog : IModalDialog
    {
        UDirectory DefaultOutputDirectory { get; set; }

        NewPackageParameters Parameters { get; }
    }
}
