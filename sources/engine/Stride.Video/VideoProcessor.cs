// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Engine;
using Stride.Games;
using Stride.Rendering;

namespace Stride.Video.Rendering
{
    /// <summary>
    /// Processor in charge of updating the <see cref="VideoComponent"/>s.
    /// </summary>
    public class VideoProcessor : EntityProcessor<VideoComponent, VideoProcessor.AssociatedData>
    {
        /// <summary>
        /// The logger of the <see cref="VideoProcessor"/>.
        /// </summary>
        public static readonly Logger Logger = GlobalLogger.GetLogger(nameof(VideoProcessor));

        public class AssociatedData : IDisposable
        {
            private readonly VideoInstance instance;
            private TimeSpan timeSinceLastFrame = new TimeSpan(0);

            public AssociatedData([NotNull] VideoComponent component, [NotNull] IServiceRegistry services)
            {
                VideoComponent = component ?? throw new ArgumentNullException(nameof(component));
                if (services == null) throw new ArgumentNullException(nameof(services));

                instance = new VideoInstance(services, component);
            }

            [NotNull]
            public VideoComponent VideoComponent { get; }

            public bool Initialized { get; private set; }

            public void Dispose()
            {
                VideoComponent.DetachInstance();
                instance.Release();
                instance.Dispose();
            }

            public void Draw(TimeSpan elapsed)
            {
                if (!Initialized)
                    return;

                instance.Update(elapsed);
            }

            public void Initialize()
            {
                if (Initialized)
                    return;

                VideoComponent.AttachInstance(instance);
                instance.InitializeFromDataSource();

                Initialized = true;
            }
        }

        /// <inheritdoc />
        public override void Draw(RenderContext context)
        {
            foreach (var kv in ComponentDatas)
            {
                var associatedData = kv.Value;
                associatedData.Draw(context.Time.Elapsed);
            }
        }

        /// <inheritdoc />
        protected override AssociatedData GenerateComponentData(Entity entity, VideoComponent component)
        {
            return new AssociatedData(component, Services);
        }

        /// <inheritdoc />
        protected override bool IsAssociatedDataValid(Entity entity, VideoComponent component, AssociatedData associatedData)
        {
            return component == associatedData.VideoComponent;
        }

        /// <inheritdoc />
        protected override void OnEntityComponentAdding(Entity entity, VideoComponent component, AssociatedData data)
        {
            try
            {
                data.Initialize();
            }
            catch (Exception ex)
            {
                // FIXME log only certains exceptions, rethrow all the rest
                Logger.Error($"An error occurred while adding a {nameof(VideoComponent)}", ex);
                throw;
            }
        }

        /// <inheritdoc />
        protected override void OnEntityComponentRemoved(Entity entity, VideoComponent component, AssociatedData data)
        {
            try
            {
                data.Dispose();
            }
            catch (Exception ex)
            {
                // FIXME log only certains exceptions, rethrow all the rest
                Logger.Error($"An error occurred while removing a {nameof(VideoComponent)}", ex);
                throw;
            }
        }

        /// <inheritdoc />
        protected override void OnSystemAdd()
        {
            var videoSystem = Services.GetService<VideoSystem>();
            if (videoSystem == null)
            {
                videoSystem = new VideoSystem(Services);
                Services.AddService(videoSystem);
                var gameSystems = Services.GetSafeServiceAs<IGameSystemCollection>();
                gameSystems.Add(videoSystem);
            }
        }
    }
}
