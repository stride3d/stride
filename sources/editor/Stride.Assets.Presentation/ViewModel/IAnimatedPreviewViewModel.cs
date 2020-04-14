// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Presentation.Commands;

namespace Xenko.Assets.Presentation.ViewModel
{
    public interface IAnimatedPreviewViewModel
    {
        ICommandBase PlayCommand { get; }

        ICommandBase PauseCommand { get; }

        bool IsPlaying { get; }
    }
}
