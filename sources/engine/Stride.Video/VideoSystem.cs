// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Games;

namespace Stride.Video
{
    public partial class VideoSystem : GameSystemBase
    {
        public VideoSystem([NotNull] IServiceRegistry registry)
            : base(registry)
        {
        }
    }
}
