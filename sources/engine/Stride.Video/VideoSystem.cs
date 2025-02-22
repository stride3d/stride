// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Games;

namespace Stride.Video
{
    public partial class VideoSystem : GameSystemBase, IService
    {
        public VideoSystem([NotNull] IServiceRegistry registry)
            : base(registry)
        {
        }

        public static IService NewInstance(IServiceRegistry services)
        {
            var instance = new VideoSystem(services);
            var gameSystems = services.GetSafeServiceAs<IGameSystemCollection>();
            gameSystems.Add(instance);
            return instance;
        }
    }
}
