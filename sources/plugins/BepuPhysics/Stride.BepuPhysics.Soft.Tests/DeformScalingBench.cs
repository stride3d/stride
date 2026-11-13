// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.BepuPhysics.Soft.Rendering;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Graphics.Regression;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Rendering.Lights;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.ProceduralModels;
using Xunit;

namespace Stride.BepuPhysics.Soft.Tests;

/// <summary>
/// GPU vs CPU soft body deformation scaling bench across three axes: body quantity, lattice density (particles),
/// and mesh tessellation (vertices). Skipped unless the environment variable <c>STRIDE_DEFORM_SCALING</c> is 1 (expensive, ~20 min).
/// </summary>
/// <remarks>
/// Writes a markdown report at <c>%TEMP%\Stride.BepuPhysics.Soft.Tests\bench_report.md</c>. Each scenario runs 100
/// warmup frames then 300 measured frames, three times, median reported. CPU deform = ElapsedTime of
/// <see cref="SoftBodyProcessor.DrawKey"/>. GPU frame time = backbuffer timestamp delta; the deform cost on GPU is the
/// difference between GPU mode and CPU mode frame times.
/// </remarks>
public class DeformScalingBench
{
    [BenchFact("STRIDE_DEFORM_SCALING")]
    public static void Scaling()
    {
        var folder = Path.Combine(Path.GetTempPath(), "Stride.BepuPhysics.Soft.Tests");
        Directory.CreateDirectory(folder);
        var report = Path.Combine(folder, "bench_report.md");

        var axis1 = new (int Count, int R, int Tess)[] { (1, 5, 16), (10, 5, 16), (50, 5, 16), (100, 5, 16) };
        var axis2 = new (int Count, int R, int Tess)[] { (1, 10, 24), (1, 20, 24), (1, 30, 24), (1, 40, 24) };
        var axis3 = new (int Count, int R, int Tess)[] { (1, 10, 16), (1, 10, 36), (1, 10, 72), (1, 10, 144), (1, 10, 288) };

        var axes = new[] { axis1, axis2, axis3 };
        var axisNames = new[]
        {
            "Axis 1 — Many small soft bodies (CPU parallelism)",
            "Axis 2 — Single soft body, growing lattice (particle count)",
            "Axis 3 — Single soft body, growing mesh tessellation (vertex count)",
        };

        var results = new List<List<Row>>();
        foreach (var axis in axes)
        {
            var rows = new List<Row>();
            foreach (var scn in axis)
            {
                var cpu = RunRepeated(scn, SoftBodyDeformationMode.Cpu);
                var gpu = RunRepeated(scn, SoftBodyDeformationMode.Gpu);
                rows.Add(new Row(scn, cpu, gpu));
            }
            results.Add(rows);
        }

        WriteReport(report, axisNames, results);
    }

    private record struct Measurement(double CpuDeformUs, double GpuFrameMs, int Particles, int Vertices);

    private record Row((int Count, int R, int Tess) Scenario, Measurement Cpu, Measurement Gpu);

    private static Measurement RunRepeated((int Count, int R, int Tess) scn, SoftBodyDeformationMode mode)
    {
        var samples = new List<Measurement>();
        for (int r = 0; r < 3; r++)
            samples.Add(RunOnce(scn, mode));
        // The median of each column: the run in the middle for one is not the one in the middle for the other
        return samples[0] with { CpuDeformUs = Median(samples.Select(s => s.CpuDeformUs)), GpuFrameMs = Median(samples.Select(s => s.GpuFrameMs)) };
    }

    private static double Median(IEnumerable<double> values)
    {
        var sorted = values.Order().ToArray();
        return sorted[sorted.Length / 2];
    }

    private static Measurement RunOnce((int Count, int R, int Tess) scn, SoftBodyDeformationMode mode)
    {
        var cpuTimes = new List<double>();
        var gpuTimes = new List<double>();
        int particles = 0, vertices = 0;
        using var game = new BenchGame(scn, mode, cpuTimes, gpuTimes, p => particles = p, v => vertices = v);
        GameTester.RunGameTest(game);
        // A run that timed out before measuring would put zeros in the report
        Assert.True(cpuTimes.Count > 0 && gpuTimes.Count > 0, $"{scn} in {mode} mode measured {cpuTimes.Count} deformations and {gpuTimes.Count} frames");
        return new Measurement(Median(cpuTimes) * 1000.0, Median(gpuTimes), particles, vertices);
    }

    private static void WriteReport(string path, string[] axisNames, List<List<Row>> axes)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Soft body deformation CPU vs GPU scaling");
        sb.AppendLine();
        sb.AppendLine($"Hardware: see `wmic cpu get name` / `dxdiag`. Backend: {GraphicsDevice.Platform} Level_11_0.");
        sb.AppendLine("Method: 100 warmup frames + 300 measured frames per (scenario, mode), three runs, median reported.");
        sb.AppendLine("- CPU column = `SoftBodyProcessor.DrawKey` ElapsedTime, µs/frame.");
        sb.AppendLine("- GPU column = full frame GPU time (ms) measured via timestamp query; the deform cost on GPU is the delta");
        sb.AppendLine("  between GPU mode and CPU mode frame times (CPU mode has zero GPU deform cost).");
        sb.AppendLine();
        sb.AppendLine("The two columns of a mode are not to be added: the CPU work of a frame may run while the GPU draws it.");
        sb.AppendLine();
        for (int i = 0; i < axes.Count; i++)
        {
            sb.AppendLine($"### {axisNames[i]}");
            sb.AppendLine();
            sb.AppendLine("| Bodies | R (lattice) | Particles | Vertices | CPU mode: deform µs | CPU mode: GPU frame ms | GPU mode: CPU-side µs | GPU mode: GPU frame ms |");
            sb.AppendLine("|--------|-------------|-----------|----------|---------------------|------------------------|-----------------------|------------------------|");
            foreach (var row in axes[i])
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "| {0} | {1} | {2} | {3} | {4:0} | {5:0.00} | {6:0} | {7:0.00} |",
                    row.Scenario.Count, row.Scenario.R, row.Cpu.Particles, row.Cpu.Vertices,
                    row.Cpu.CpuDeformUs, row.Cpu.GpuFrameMs,
                    row.Gpu.CpuDeformUs, row.Gpu.GpuFrameMs));
            }
            sb.AppendLine();
        }
        File.WriteAllText(path, sb.ToString());
    }

    private sealed class BenchGame : GameTestBase
    {
        private readonly (int Count, int R, int Tess) _scn;
        private readonly SoftBodyDeformationMode _mode;
        private readonly List<double> _cpuTimes;
        private readonly List<double> _gpuTimes;
        private readonly Action<int> _reportParticles;
        private readonly Action<int> _reportVertices;

        private int _frame;
        private QueryPool?[] _gpuTimers = new QueryPool?[3];
        private readonly long[] _gpuTicks = new long[2];
        private int _gpuFrame;
        private System.Threading.Channels.ChannelReader<ProfilingEvent>? _profilingEvents;
        private bool _started;

        private const int WarmupFrames = 100;
        private const int MeasuredFrames = 300;

        public BenchGame((int Count, int R, int Tess) scn, SoftBodyDeformationMode mode, List<double> cpuTimes, List<double> gpuTimes, Action<int> reportParticles, Action<int> reportVertices)
        {
            _scn = scn;
            _mode = mode;
            _cpuTimes = cpuTimes;
            _gpuTimes = gpuTimes;
            _reportParticles = reportParticles;
            _reportVertices = reportVertices;

            GraphicsDeviceManager.PreferredGraphicsProfile = [GraphicsProfile.Level_11_0];
            GraphicsDeviceManager.ShaderProfile = GraphicsProfile.Level_11_0;
            GraphicsDeviceManager.PreferredBackBufferWidth = 640;
            GraphicsDeviceManager.PreferredBackBufferHeight = 360;
            IsFixedTimeStep = true;
            ForceOneUpdatePerDraw = true;
            IsDrawDesynchronized = false;
            TargetElapsedTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60);
            ScreenShotAutomationEnabled = false;
        }

        protected override Task LoadContent()
        {
            SetUpScene();
            return base.LoadContent();
        }

        private void SetUpScene()
        {
            var compositor = GraphicsCompositorHelper.CreateDefault(true, clearColor: new Color4(0.6f, 0.7f, 0.85f, 1f), graphicsProfile: GraphicsProfile.Level_11_0);
            SceneSystem.GraphicsCompositor = compositor;
            var root = SceneSystem.SceneInstance.RootScene.Entities;
            root.Clear();

            var sun = new Entity { new LightComponent { Type = new LightDirectional(), Intensity = 2f } };
            sun.Transform.Rotation = Quaternion.RotationYawPitchRoll(0.6f, -0.9f, 0f);
            root.Add(sun);
            root.Add(new Entity { new LightComponent { Type = new LightAmbient(), Intensity = 0.3f } });
            var camera = new Entity { new CameraComponent { Slot = compositor.Cameras[0].ToSlotId(), NearClipPlane = 0.05f, FarClipPlane = 100f } };
            camera.Transform.Position = new Vector3(0f, 2.5f, 6f);
            camera.Transform.Rotation = Quaternion.RotationX(-0.3f);
            root.Add(camera);

            var floor = new Entity
            {
                new ModelComponent(MakeModel(new PlaneProceduralModel { Size = new Vector2(20f) }, new Color(150, 146, 140))),
                new StaticComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider { Size = new Vector3(20f, 1f, 20f), PositionLocal = new Vector3(0f, -0.5f, 0f) } } } },
            };
            root.Add(floor);

            Services.GetOrCreate<SoftBodyConfiguration>().DeformationMode = _mode;
            int totalParticles = 0, totalVertices = 0;
            float spacing = 1.2f;
            int side = (int)Math.Ceiling(Math.Sqrt(_scn.Count));
            for (int i = 0; i < _scn.Count; i++)
            {
                int cx = i % side, cz = i / side;
                var soft = new VolumetricSoftBodyComponent { Resolution = _scn.R };
                var model = MakeModel(new SphereProceduralModel { Radius = 0.35f, Tessellation = _scn.Tess }, new Color(255, 111, 89));
                var entity = new Entity { new ModelComponent(model), soft };
                entity.Transform.Position = new Vector3((cx - side * 0.5f) * spacing, 1.5f + (i * 0.02f), (cz - side * 0.5f) * spacing);
                root.Add(entity);

                if (i == 0)
                {
                    totalVertices = EstimateVertexCount(model) * _scn.Count;
                    totalParticles = (_scn.R + 1) * (_scn.R + 1) * (_scn.R + 1) * _scn.Count;
                }
            }
            _reportParticles(totalParticles);
            _reportVertices(totalVertices);
        }

        private static int EstimateVertexCount(Model model)
        {
            int total = 0;
            foreach (var mesh in model.Meshes)
                total += mesh.Draw.VertexBuffers[0].Count;
            return total;
        }

        protected override void Draw(GameTime gameTime)
        {
            var commandList = GraphicsContext.CommandList;
            ref var pool = ref _gpuTimers[_gpuFrame % _gpuTimers.Length];
            pool ??= QueryPool.New(GraphicsDevice, QueryType.Timestamp, 2);
            if (_gpuFrame >= _gpuTimers.Length && pool.TryGetData(_gpuTicks) && _started)
            {
                double ms = (_gpuTicks[1] - _gpuTicks[0]) * 1000.0 / GraphicsDevice.TimestampFrequency;
                if (_frame >= WarmupFrames && _gpuTimes.Count < MeasuredFrames)
                    _gpuTimes.Add(ms);
            }
            commandList.WriteTimestamp(pool, 0);
            base.Draw(gameTime);
            commandList.WriteTimestamp(pool, 1);
            _gpuFrame++;

            if (_profilingEvents is null)
            {
                Profiler.Enable(SoftBodyProcessor.DrawKey);
                _profilingEvents = Profiler.Subscribe();
            }
            while (_profilingEvents.TryRead(out var pe))
            {
                if (pe.Key == SoftBodyProcessor.DrawKey && pe.Type == ProfilingMessageType.End && _started)
                {
                    if (_frame >= WarmupFrames && _cpuTimes.Count < MeasuredFrames)
                        _cpuTimes.Add(pe.ElapsedTime.TotalMilliseconds);
                }
            }
            _started = true;
            _frame++;
            if (_cpuTimes.Count >= MeasuredFrames && _gpuTimes.Count >= MeasuredFrames)
                Exit();
            else if (_frame > WarmupFrames + MeasuredFrames * 3)
                Exit();
        }

        protected override void Destroy()
        {
            if (_profilingEvents is not null)
            {
                Profiler.Unsubscribe(_profilingEvents);
                Profiler.Disable(SoftBodyProcessor.DrawKey);
            }
            foreach (var pool in _gpuTimers)
                pool?.Dispose();
            base.Destroy();
        }

        private Model MakeModel(PrimitiveProceduralModelBase shape, Color color)
        {
            var model = new ProceduralModelDescriptor(shape).GenerateModel(Services);
            var mat = Stride.Rendering.Material.New(GraphicsDevice, new MaterialDescriptor
            {
                Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(color.ToColor4())),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(0.5f)),
                    Specular = new MaterialMetalnessMapFeature(new ComputeFloat(0f)),
                    SpecularModel = new MaterialSpecularMicrofacetModelFeature { Environment = new MaterialSpecularMicrofacetEnvironmentGGXPolynomial() },
                },
            });
            model.Materials.Clear();
            model.Materials.Add(new MaterialInstance(mat));
            return model;
        }
    }
}
