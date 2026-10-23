// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Games;
using Stride.Graphics;

namespace Stride.Video
{
    public class VideoSystem : GameSystemBase, IService
    {
        public VideoSystem([NotNull] IServiceRegistry registry) : base(registry)
        {
        }

        public static IService NewInstance(IServiceRegistry services)
        {
            var instance = new VideoSystem(services);
            var gameSystems = services.GetSafeServiceAs<IGameSystemCollection>();
            gameSystems.Add(instance);
            return instance;
        }

        /// <summary>The backend selected at <see cref="Initialize"/> time, used to construct
        /// per-instance <see cref="VideoBackend"/>s. Null on platforms with no registered or
        /// supported video backend.</summary>
        internal VideoBackendFactory ActiveBackendFactory { get; private set; }

        public override void Initialize()
        {
            base.Initialize();

            var graphicsDevice = Services.GetService<IGame>()?.GraphicsDevice;
            ActiveBackendFactory = VideoBackendRegistry.SelectFactory(graphicsDevice);
            ActiveBackendFactory?.InitializeSystem(this);
        }

        protected override void Destroy()
        {
            ActiveBackendFactory?.DestroySystem(this);
            ActiveBackendFactory = null;
            base.Destroy();
        }
    }
}
