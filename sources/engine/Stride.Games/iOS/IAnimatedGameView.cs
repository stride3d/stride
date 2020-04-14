// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_PLATFORM_IOS
namespace Xenko.Games
{
    internal interface IAnimatedGameView
    {
        void StartAnimating();
        void StopAnimating();
    }
}
#endif
