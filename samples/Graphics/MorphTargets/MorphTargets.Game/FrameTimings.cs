using System.Reflection;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.UI.Controls;

namespace MorphTargets;

/// <summary>
/// Live CPU and GPU frame times for the sample scenes. The GPU frame is one timestamp pair, from before the model
/// processors (where deformation is dispatched) to the end of the scene, read back a few frames later without stalling.
/// Regions inside the frame are not timed: on the GPU timeline they also contain waits on the previous frame and idle
/// gaps between submissions, which would be attributed to the wrong work.
/// </summary>
public sealed class FrameTimings : IDisposable
{
    private const int Slots = 6;
    private const int AverageFrames = 30;
    // Vulkan pools must be reset before reuse; Direct3D 11 has no reset call.
    private static readonly MethodInfo ResetQueryPool = typeof(CommandList).GetMethod("ResetQueryPool", [typeof(QueryPool)]);

    private readonly ScriptComponent owner;
    private readonly CommandList commandList;
    private readonly QueryPool[] pools = new QueryPool[Slots];
    private readonly bool[] pending = new bool[Slots];
    private readonly long[] values = new long[2];
    private readonly Queue<double> gpu = new();
    private readonly Queue<double> cpu = new();
    private readonly BoundaryProcessor begin;
    private readonly TimedSceneRenderer sceneRenderer;
    private readonly ISceneRenderer originalRenderer;
    private int slot = -1;
    private int next;
    private bool disposed;

    public FrameTimings(ScriptComponent owner)
    {
        this.owner = owner;
        commandList = owner.Game.GraphicsContext.CommandList;
        for (int i = 0; i < Slots; i++) pools[i] = QueryPool.New(owner.GraphicsDevice, QueryType.Timestamp, values.Length);
        // Start the GPU frame before the model processors record their GPU work, where deformation is dispatched.
        begin = new BoundaryProcessor(this, -1);
        owner.SceneSystem.SceneInstance.Processors.Add(begin);
        var compositor = owner.SceneSystem.GraphicsCompositor;
        originalRenderer = compositor.Game;
        while (originalRenderer is TimedSceneRenderer timed)
            originalRenderer = timed.Child;
        compositor.Game = sceneRenderer = new TimedSceneRenderer { Child = originalRenderer, Timings = this };
    }

    private void BeginFrame()
    {
        if (disposed) return;
        Poll();
        slot = -1;
        if (pending[next]) return; // GPU is far behind; skip timing this frame rather than stall.
        slot = next;
        next = (next + 1) % Slots;
        ResetQueryPool?.Invoke(commandList, [pools[slot]]);
        commandList.WriteTimestamp(pools[slot], 0);
    }

    private void EndFrame()
    {
        if (disposed || slot < 0) return;
        commandList.WriteTimestamp(pools[slot], 1);
        pending[slot] = true;
        slot = -1;
    }

    private void Poll()
    {
        for (int i = 0; i < Slots; i++)
        {
            if (!pending[i] || !pools[i].TryGetData(values)) continue;
            pending[i] = false;
            double frequency = owner.GraphicsDevice.TimestampFrequency;
            if (frequency > 0) Add(gpu, (values[1] - values[0]) * 1000.0 / frequency);
        }
    }

    private static void Add(Queue<double> samples, double value)
    {
        samples.Enqueue(value);
        while (samples.Count > AverageFrames) samples.Dequeue();
    }

    private static string Format(Queue<double> samples) => samples.Count > 0 ? $"{samples.Average():0.00} ms" : "-";

    /// <summary>Row names for <see cref="Report"/>.</summary>
    public static readonly string[] Rows = ["Frame rate", "CPU frame", "GPU frame"];

    /// <summary>Call once per frame from the owning script's Update; fills one value per <see cref="Rows"/> entry.</summary>
    public void Report(TextBlock[] values)
    {
        Add(cpu, owner.Game.UpdateTime.Elapsed.TotalMilliseconds);
        values[0].Text = $"{owner.Game.UpdateTime.FramePerSecond:0} FPS";
        values[1].Text = Format(cpu);
        values[2].Text = Format(gpu);
    }

    public void Dispose()
    {
        disposed = true;
        owner.SceneSystem.SceneInstance?.Processors.Remove(begin);
        var compositor = owner.SceneSystem.GraphicsCompositor;
        if (compositor != null && compositor.Game == sceneRenderer) compositor.Game = originalRenderer;
        foreach (var pool in pools) pool?.Dispose();
    }

    private sealed class BoundaryProcessor : EntityProcessor<ModelComponent, ModelComponent>
    {
        private readonly FrameTimings timings;

        public BoundaryProcessor(FrameTimings timings, int order) { this.timings = timings; Order = order; }
        protected override ModelComponent GenerateComponentData(Entity entity, ModelComponent component) => component;
        protected override bool IsAssociatedDataValid(Entity entity, ModelComponent component, ModelComponent data) => component == data;
        public override void Draw(RenderContext context) => timings.BeginFrame();
    }

    private sealed class TimedSceneRenderer : SceneRendererBase
    {
        public ISceneRenderer Child;
        public FrameTimings Timings;

        protected override void CollectCore(RenderContext context)
        {
            base.CollectCore(context);
            Child?.Collect(context);
        }

        protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
        {
            Child?.Draw(drawContext);
            Timings.EndFrame();
        }
    }
}
