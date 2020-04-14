// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_PLATFORM_IOS
namespace Stride.Games
{
    internal interface IAnimatedGameView
    {
        void StartAnimating();
        void StopAnimating();
    }
}
#endif
