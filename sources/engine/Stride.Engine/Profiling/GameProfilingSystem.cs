// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using Color = Stride.Core.Mathematics.Color;

namespace Stride.Profiling
{
    public class GameProfilingSystem : GameSystemBase
    {
        private static readonly ProfilingKey UpdateStringsKey = new ProfilingKey($"{nameof(GameProfilingSystem)}.UpdateStrings");

        private readonly Point textDrawStartOffset = new Point(5, 10);
        private const int TextRowHeight = 16;
        private const int TopRowHeight = TextRowHeight + 2;

        private readonly GcProfiling gcProfiler;

        private readonly StringBuilder gcMemoryStringBuilder = new StringBuilder(64);
        private string gcMemoryString = string.Empty;

        private readonly StringBuilder gcCollectionsStringBuilder = new StringBuilder(64);
        private string gcCollectionsString = string.Empty;

        private readonly StringBuilder fpsStatStringBuilder = new StringBuilder(64);
        private string fpsStatString = string.Empty;

        private readonly StringBuilder gpuGeneralInfoStringBuilder = new StringBuilder();
        private string gpuGeneralInfoString = string.Empty;

        private readonly StringBuilder gpuInfoStringBuilder = new StringBuilder();
        private string gpuInfoString = string.Empty;

        private readonly StringBuilder profilersStringBuilder = new StringBuilder();
        private string profilersString = string.Empty;

        private FastTextRenderer fastTextRenderer;

        private readonly object stringLock = new object();

        private Color4 textColor = Color.LightGreen;

        private GameProfilingResults filteringMode;
        private Task asyncUpdateTask;
        private Size2 renderTargetSize;
        private ChannelReader<ProfilingEvent> profilerChannel;
        private PresentInterval? userPresentInterval;
        private bool userMinimizedState = true;

        private int lastFrame = -1;

        private float viewportHeight = 1000;

        private uint numberOfPages;

        private uint trianglesCount;
        private uint drawCallsCount;

        private CancellationTokenSource backgroundUpdateCancellationSource;

        /// <summary>
        /// The render target where the profiling results should be rendered into. If null, the <see cref="Game.GraphicsDevice.Presenter.BackBuffer"/> is used.
        /// </summary>
        public Texture RenderTarget { get; set; }

        private struct ProfilingResult : IComparer<ProfilingResult>
        {
            public TimeSpan AccumulatedTime;
            public TimeSpan MinTime;
            public TimeSpan MaxTime;
            public int Count;
            public int MarkCount;
            public ProfilingKey EventKey;
            public ProfilingEventMessage? EventMessage;

            public int Compare(ProfilingResult x, ProfilingResult y)
            {
                //Flip sign so we get descending order.
                return -TimeSpan.Compare(x.AccumulatedTime, y.AccumulatedTime);
            }
        }

        private readonly List<ProfilingResult> profilingResults = new List<ProfilingResult>(256);
        private readonly Dictionary<ProfilingKey, ProfilingResult> profilingResultsDictionary = new Dictionary<ProfilingKey, ProfilingResult>(256);

        /// <summary>
        /// Initializes a new instance of the <see cref="GameProfilingSystem"/> class.
        /// </summary>
        /// <param name="registry">The service registry.</param>
        public GameProfilingSystem(IServiceRegistry registry) : base(registry)
        {
            DrawOrder = 0xfffffe;

            gcProfiler = new GcProfiling();
        }

        private void UpdateInternalState(bool containsMarks)
        {
            CopyEventsForStringUpdateAndClear();

            //Advance any profiler that needs it
            gcProfiler.Tick();

            // update strings on another thread so that we don't block the event loop in UpdateWithEventsAsync
            _ = Task.Run(() => UpdateProfilingStrings(containsMarks));
        }

        private void UpdateProfilingStrings(bool containsMarks)
        {
            using var _ = Profiler.Begin(UpdateStringsKey);

            profilersStringBuilder.Clear();

            // calculate elaspsed frames
            var newDraw = Game.DrawTime.FrameCount;
            var elapsedFrames = newDraw - lastFrame;
            lastFrame = newDraw;

            var availableDisplayHeight = viewportHeight - 2 * TextRowHeight - 3 * TopRowHeight;
            var elementsPerPage = (int)Math.Floor(availableDisplayHeight / TextRowHeight);
            numberOfPages = (uint)Math.Ceiling(profilingResults.Count / (float)elementsPerPage);
            CurrentResultPage = Math.Min(CurrentResultPage, numberOfPages);

            char sortByTimeIndicator = SortingMode == GameProfilingSorting.ByTime ? 'v' : ' ';
            char sortByAvgTimeIndicator = SortingMode == GameProfilingSorting.ByAverageTime ? 'v' : ' ';

            profilersStringBuilder.AppendFormat("TOTAL    {0}| AVG/CALL {1}| MIN/CALL  | MAX/CALL  | CALLS  | ", sortByTimeIndicator, sortByAvgTimeIndicator);
            if (containsMarks)
                profilersStringBuilder.AppendFormat("MARKS | ");
            profilersStringBuilder.AppendFormat("PROFILING KEY / EXTRA INFO\n");

            for (int i = 0; i < Math.Min(profilingResults.Count - (CurrentResultPage - 1) * elementsPerPage, elementsPerPage); i++)
            {
                AppendEvent(profilingResults[((int)CurrentResultPage - 1) * elementsPerPage + i], elapsedFrames, containsMarks);
            }

            if (numberOfPages > 1)
                profilersStringBuilder.AppendFormat("PAGE {0} OF {1}", CurrentResultPage, numberOfPages);

            const float mb = 1 << 20;

            var gpuInfoStringNew = $"Drawn triangles: {trianglesCount / 1000f:0.0}k, Draw calls: {drawCallsCount}, Buffer memory: {GraphicsDevice.BuffersMemory / mb:0.00}[MB], Texture memory: {GraphicsDevice.TextureMemory / mb:0.00}[MB]";

            //Note: renderTargetSize gets set in Draw(), without synchronization, so might be temporarily incorrect.
            var gpuGeneralInfoStrinNew = $"Device: {GraphicsDevice.Adapter.Description}, Platform: {GraphicsDevice.Platform}, Profile: {GraphicsDevice.ShaderProfile}, Resolution: {renderTargetSize}";

            var fpsStatStringBuilderNew  = $"Displaying: {FilteringMode}, Frame: {Game.DrawTime.FrameCount}, Update: {Game.UpdateTime.TimePerFrame.TotalMilliseconds:0.00}ms, Draw: {Game.DrawTime.TimePerFrame.TotalMilliseconds:0.00}ms, FPS: {Game.DrawTime.FramePerSecond:0.00}";

            var gcCollectionsStringNew = gcCollectionsStringBuilder.ToString();
            var gcMemoryStringNew = gcMemoryStringBuilder.ToString();
            var profilersStringNew = profilersStringBuilder.ToString();

            lock (stringLock)
            {
                gcCollectionsString = gcCollectionsStringNew;
                gcMemoryString = gcMemoryStringNew;
                profilersString = profilersStringNew;
                fpsStatString = fpsStatStringBuilderNew;
                gpuInfoString = gpuInfoStringNew;
                gpuGeneralInfoString = gpuGeneralInfoStrinNew;
            }
        }

        private void CopyEventsForStringUpdateAndClear()
        {
            profilingResults.Clear();

            foreach (var profilingResult in profilingResultsDictionary)
            {
                if (profilingResult.Value.EventKey == null) continue;
                profilingResults.Add(profilingResult.Value);
            }
            profilingResultsDictionary.Clear();

            if (SortingMode == GameProfilingSorting.ByTime)
            {
                profilingResults.Sort((x, y) => x.Compare(x, y));
            }
            else if (SortingMode == GameProfilingSorting.ByAverageTime)
            {
                profilingResults.Sort((x, y) => -TimeSpan.Compare(x.AccumulatedTime / x.Count, y.AccumulatedTime / y.Count));
            }
            else
            {
                // Can't be null because we skip those events without values
                // ReSharper disable PossibleInvalidOperationException
                profilingResults.Sort((x1, x2) => string.Compare(x1.EventKey.Name, x2.EventKey.Name, StringComparison.Ordinal));
                // ReSharper restore PossibleInvalidOperationException
            }
        }

        private async Task UpdateFpsAsync(CancellationToken cancellationToken)
        {
            while (!backgroundUpdateCancellationSource.IsCancellationRequested)
            {
                //In Fps filtering mode wait until it is time to update the output.
                await Task.Delay((int)RefreshTime, cancellationToken).ConfigureAwait(false);

                UpdateInternalState(false);
            }
        }

        //If we're subscribed to the Profiler (Cpu/Gpu mode) start receiving events.
        //Control flow will only return here once the profilerChannel is closed.
        private async Task UpdateWithEventsAsync(CancellationToken cancellationToken)
        {
            var containsMarks = false;
            Task delayTask = Task.Delay((int)RefreshTime, cancellationToken);

            await foreach (var e in profilerChannel.ReadAllAsync(cancellationToken))
            {
                if (delayTask.IsCompleted)
                {
                    UpdateInternalState(containsMarks);
                    delayTask = Task.Delay((int)RefreshTime);
                    containsMarks = false;
                }

                if (cancellationToken.IsCancellationRequested)
                    continue; // skip events until they run out

                if (FilteringMode == GameProfilingResults.Fps)
                    continue;

                if (e.IsGPUEvent() && FilteringMode != GameProfilingResults.GpuEvents)
                    continue;
                if (!e.IsGPUEvent() && FilteringMode != GameProfilingResults.CpuEvents)
                    continue;

                //gc profiling is a special case
                if (e.Key == GcProfiling.GcMemoryKey)
                {
                    lock (stringLock) // lock because string update can happen in parallel
                    {
                        gcMemoryStringBuilder.Clear();
                        e.Message?.ToString(gcMemoryStringBuilder);
                    }
                    continue;
                }

                if (e.Key == GcProfiling.GcCollectionCountKey)
                {
                    lock (stringLock) // lock because string update can happen in parallel
                    {
                        gcCollectionsStringBuilder.Clear();
                        e.Message?.ToString(gcCollectionsStringBuilder);
                    }
                    continue;
                }

                if (e.Key == GameProfilingKeys.GameDrawFPS && e.Type == ProfilingMessageType.End)
                    continue;

                ProfilingResult profilingResult;
                if (!profilingResultsDictionary.TryGetValue(e.Key, out profilingResult))
                {
                    profilingResult.MinTime = TimeSpan.MaxValue;
                }

                if (e.Type == ProfilingMessageType.End)
                {
                    ++profilingResult.Count;
                    profilingResult.AccumulatedTime += e.ElapsedTime;

                    if (e.ElapsedTime < profilingResult.MinTime)
                        profilingResult.MinTime = e.ElapsedTime;
                    if (e.ElapsedTime > profilingResult.MaxTime)
                        profilingResult.MaxTime = e.ElapsedTime;

                    profilingResult.EventKey = e.Key;
                    profilingResult.EventMessage = e.Message;
                }
                else if (e.Type == ProfilingMessageType.Mark)
                {
                    profilingResult.MarkCount++;
                    containsMarks = true;
                }

                profilingResultsDictionary[e.Key] = profilingResult;
            }
        }

        private void RunBackgroundUpdate(bool restart)
        {
            if (restart || asyncUpdateTask == null || asyncUpdateTask.IsCompleted)
            {
                if (backgroundUpdateCancellationSource != null)
                {
                    backgroundUpdateCancellationSource.Cancel();
                    backgroundUpdateCancellationSource.Dispose();
                }
                backgroundUpdateCancellationSource = new CancellationTokenSource();

                if (FilteringMode == GameProfilingResults.Fps)
                {
                    asyncUpdateTask = Task.Run(() => UpdateFpsAsync(backgroundUpdateCancellationSource.Token));
                }
                else
                {
                    asyncUpdateTask = Task.Run(() => UpdateWithEventsAsync(backgroundUpdateCancellationSource.Token));
                }
            }
        }   

        private void AppendEvent(ProfilingResult profilingResult, int elapsedFrames, bool displayMarkCount)
        {
            Span<char> formatBuffer = stackalloc char[8];
            int formatBufferUse = 0;

            elapsedFrames = Math.Max(elapsedFrames, 1);

            Profiler.AppendTime(profilersStringBuilder, profilingResult.AccumulatedTime / elapsedFrames);
            profilersStringBuilder.Append(" | ");
            Profiler.AppendTime(profilersStringBuilder, profilingResult.AccumulatedTime / profilingResult.Count);
            profilersStringBuilder.Append(" | ");
            Profiler.AppendTime(profilersStringBuilder, profilingResult.MinTime);
            profilersStringBuilder.Append(" | ");
            Profiler.AppendTime(profilersStringBuilder, profilingResult.MaxTime);
            profilersStringBuilder.Append(" | ");

            var callsPerFrame = profilingResult.Count / (double)elapsedFrames;
            callsPerFrame.TryFormat(formatBuffer, out formatBufferUse, "{0,6:#00.00}");
            profilersStringBuilder.Append(formatBuffer[..formatBufferUse]);
            profilersStringBuilder.Append(" | ");

            if (displayMarkCount)
            {
                var marksPerFrame = profilingResult.MarkCount / (double)elapsedFrames;
                marksPerFrame.TryFormat(formatBuffer, out formatBufferUse, "{0:00.00}");
                profilersStringBuilder.Append(formatBuffer[..formatBufferUse]);
                profilersStringBuilder.Append(" | ");
            }

            profilersStringBuilder.Append(profilingResult.EventKey);
            if (profilingResult.EventMessage.HasValue)
            {
                profilersStringBuilder.Append(" / ");
                profilingResult.EventMessage.Value.ToString(profilersStringBuilder);
            }

            profilersStringBuilder.Append("\n");
        }

        /// <inheritdoc/>
        protected override void Destroy()
        {
            Enabled = false;
            Visible = false;

            if (profilerChannel != null)
                Profiler.Unsubscribe(profilerChannel);

            backgroundUpdateCancellationSource?.Cancel();
            backgroundUpdateCancellationSource?.Dispose();

            if (asyncUpdateTask != null && !asyncUpdateTask.IsCompleted)
            {
                try
                {
                    asyncUpdateTask.Wait();
                }
                catch { } // likely throws cancellation exception
            }

            gcProfiler.Dispose();
        }

        /// <inheritdoc/>
        public override void Draw(GameTime gameTime)
        {
            // Where to render the result?
            var renderTarget = RenderTarget ?? Game.GraphicsDevice.Presenter.BackBuffer;

            // copy those values before fast text render not to influence the game stats
            drawCallsCount = GraphicsDevice.FrameDrawCalls;
            trianglesCount = GraphicsDevice.FrameTriangleCount;

            if (FilteringMode == GameProfilingResults.GpuEvents && renderTargetSize != new Size2(renderTarget.Width, renderTarget.Height))
            {
                renderTargetSize = new Size2(renderTarget.Width, renderTarget.Height);
            }

            var renderContext = RenderContext.GetShared(Services);
            var renderDrawContext = renderContext.GetThreadContext();

            if (fastTextRenderer == null)
            {
                fastTextRenderer = new FastTextRenderer(renderDrawContext.GraphicsContext)
                {
                    DebugSpriteFont = Content.Load<Texture>("StrideDebugSpriteFont"),
                    TextColor = TextColor,
                };
            }

            using (renderDrawContext.PushRenderTargetsAndRestore())
            {
                renderDrawContext.CommandList.SetRenderTargetAndViewport(null, renderTarget);
                viewportHeight = renderDrawContext.CommandList.Viewport.Height;
                fastTextRenderer.Begin(renderDrawContext.GraphicsContext);
                lock (stringLock)
                {
                    var currentHeight = textDrawStartOffset.Y;
                    fastTextRenderer.DrawString(renderDrawContext.GraphicsContext, fpsStatString, textDrawStartOffset.X, currentHeight);
                    currentHeight += TopRowHeight;

                    if (FilteringMode == GameProfilingResults.CpuEvents)
                    {
                        fastTextRenderer.DrawString(renderDrawContext.GraphicsContext, gcMemoryString, textDrawStartOffset.X, currentHeight);
                        currentHeight += TopRowHeight;
                        fastTextRenderer.DrawString(renderDrawContext.GraphicsContext, gcCollectionsString, textDrawStartOffset.X, currentHeight);
                        currentHeight += TopRowHeight;
                    }
                    else if (FilteringMode == GameProfilingResults.GpuEvents)
                    {
                        fastTextRenderer.DrawString(renderDrawContext.GraphicsContext, gpuGeneralInfoString, textDrawStartOffset.X, currentHeight);
                        currentHeight += TopRowHeight;
                        fastTextRenderer.DrawString(renderDrawContext.GraphicsContext, gpuInfoString, textDrawStartOffset.X, currentHeight);
                        currentHeight += TopRowHeight;
                    }

                    if (FilteringMode != GameProfilingResults.Fps)
                        fastTextRenderer.DrawString(renderDrawContext.GraphicsContext, profilersString, textDrawStartOffset.X, currentHeight);
                }

                fastTextRenderer.End(renderDrawContext.GraphicsContext);
            }
        }

        /// <summary>
        /// Enables the profiling system drawing.
        /// </summary>
        /// <param name="excludeKeys">If true the keys specified after are excluded from rendering, if false they will be exclusively included.</param>
        /// <param name="keys">The keys to exclude or include.</param>
        public void EnableProfiling(bool excludeKeys = false, params ProfilingKey[] keys)
        {
            Enabled = true;
            Visible = true;

            if (Game != null)
            {
                userMinimizedState = Game.TreatNotFocusedLikeMinimized;
                Game.TreatNotFocusedLikeMinimized = false;
            }

            // Backup current PresentInterval state
            userPresentInterval = GraphicsDevice.Tags.Get(GraphicsPresenter.ForcedPresentInterval);

            // Disable VSync (otherwise GPU results might be incorrect)
            GraphicsDevice.Tags.Set(GraphicsPresenter.ForcedPresentInterval, PresentInterval.Immediate);

            if (keys.Length == 0)
            {
                Profiler.EnableAll();
            }
            else
            {
                if (excludeKeys)
                {
                    Profiler.EnableAll();
                    foreach (var profilingKey in keys)
                    {
                        Profiler.Disable(profilingKey);
                    }
                }
                else
                {
                    foreach (var profilingKey in keys)
                    {
                        Profiler.Enable(profilingKey);
                    }
                }
            }

            gcProfiler.Enable();

            RunBackgroundUpdate(restart: false);
        }

        /// <summary>
        /// Disables the profiling system drawing.
        /// </summary>
        public void DisableProfiling()
        {
            Enabled = false;
            Visible = false;

            // Restore previous PresentInterval state
            GraphicsDevice.Tags.Set(GraphicsPresenter.ForcedPresentInterval, userPresentInterval);

            userPresentInterval = default;
            if (Game != null)
                Game.TreatNotFocusedLikeMinimized = userMinimizedState;

            Profiler.DisableAll();
            gcProfiler.Disable();

            FilteringMode = GameProfilingResults.Fps;
            backgroundUpdateCancellationSource.Cancel();
        }

        /// <summary>
        /// Sets or gets the color to use when drawing the profiling system fonts.
        /// </summary>
        public Color4 TextColor
        {
            get => textColor;
            set
            {
                textColor = value;
                if (fastTextRenderer != null)
                    fastTextRenderer.TextColor = value;
            }
        }

        /// <summary>
        /// Sets or gets the way the printed information will be sorted.
        /// </summary>
        public GameProfilingSorting SortingMode { get; set; } = GameProfilingSorting.ByTime;

        /// <summary>
        /// Sets or gets which data should be displayed on screen.
        /// </summary>
        public GameProfilingResults FilteringMode
        {
            get => filteringMode;
            set
            {
                // Only Cpu and Gpu modes need to subscribe to profiling events, so we
                // subscribe when switching away from Fps mode and unsubscribe when switching to it.
                if (filteringMode != value)
                {
                    if (filteringMode == GameProfilingResults.Fps)
                        profilerChannel = Profiler.Subscribe();
                    else if (value == GameProfilingResults.Fps)
                        Profiler.Unsubscribe(profilerChannel);

                    filteringMode = value;

                    RunBackgroundUpdate(restart: true);
                }
            }
        }

        /// <summary>
        /// Sets or gets the refreshing time of the profiling information in milliseconds.
        /// </summary>
        public double RefreshTime { get; set; } = 500;

        /// <summary>
        /// Sets or gets the profiling result page to display.
        /// </summary>
        public uint CurrentResultPage { get; set; } = 1;
    }
}
