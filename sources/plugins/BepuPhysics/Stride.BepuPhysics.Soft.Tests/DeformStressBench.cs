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
/// Visible stress bench: one high-tessellation soft body (R=10, tess=288, ~166k verts) falls on a floor,
/// run once in CPU mode and once in GPU mode. Opens a 1280×720 window so you can watch; title bar shows
/// mode + median CPU/GPU frame times. Skipped unless the environment variable <c>STRIDE_DEFORM_STRESS</c> is 1.
/// </summary>
public class DeformStressBench
{
    [BenchFact("STRIDE_DEFORM_STRESS")]
    public static void Stress()
    {
        var folder = Path.Combine(Path.GetTempPath(), "Stride.BepuPhysics.Soft.Tests");
        Directory.CreateDirectory(folder);
        var report = Path.Combine(folder, "stress_report.md");

        var cpuTimes = new List<double>();
        var cpuGpuTimes = new List<double>();
        var gpuTimes = new List<double>();
        var gpuGpuTimes = new List<double>();

        // Shown in a window, for this bench only: the tests that run after it in the process go back to what they had
        bool interactive = GameTestBase.ForceInteractiveMode;
        GameTestBase.ForceInteractiveMode = true;
        try
        {
            Console.WriteLine("=== CPU run ===");
            using (var g = new StressGame(SoftBodyDeformationMode.Cpu, cpuTimes, cpuGpuTimes))
                g.Run();

            Console.WriteLine("=== GPU run ===");
            using (var g = new StressGame(SoftBodyDeformationMode.Gpu, gpuTimes, gpuGpuTimes))
                g.Run();
        }
        finally
        {
            GameTestBase.ForceInteractiveMode = interactive;
        }

        double CpuMedian(List<double> l) => l.Count == 0 ? double.NaN : l.OrderBy(x => x).ElementAt(l.Count / 2);

        var apiName = GraphicsDevice.Platform;
        var sb = new StringBuilder();
        sb.AppendLine($"# Deform stress — {apiName}");
        sb.AppendLine();
        sb.AppendLine("Scenario: 1 body, lattice R=10, tessellation 288 (~166k verts).");
        sb.AppendLine("200 warmup frames + 600 measured frames (10 s @ 60 fps), median reported.");
        sb.AppendLine();
        sb.AppendLine("| Mode | CPU deform µs | GPU frame ms |");
        sb.AppendLine("|------|---------------|--------------|");
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
            "| CPU  | {0:0} | {1:0.00} |", CpuMedian(cpuTimes) * 1000.0, CpuMedian(cpuGpuTimes)));
        sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
            "| GPU  | {0:0} | {1:0.00} |", CpuMedian(gpuTimes) * 1000.0, CpuMedian(gpuGpuTimes)));
        File.WriteAllText(report, sb.ToString());
        Console.WriteLine(sb.ToString());
    }

    private sealed class StressGame : GameTestBase
    {
        private readonly SoftBodyDeformationMode _mode;
        private readonly List<double> _cpuTimes;
        private readonly List<double> _gpuTimes;
        private int _frame;
        private QueryPool?[] _gpuTimers = new QueryPool?[3];
        private readonly long[] _gpuTicks = new long[2];
        private int _gpuFrame;
        private System.Threading.Channels.ChannelReader<ProfilingEvent>? _profilingEvents;

        private const int WarmupFrames = 200;
        private const int MeasuredFrames = 600;

        public StressGame(SoftBodyDeformationMode mode, List<double> cpuTimes, List<double> gpuTimes)
        {
            _mode = mode;
            _cpuTimes = cpuTimes;
            _gpuTimes = gpuTimes;
            GraphicsDeviceManager.PreferredGraphicsProfile = [GraphicsProfile.Level_11_0];
            GraphicsDeviceManager.ShaderProfile = GraphicsProfile.Level_11_0;
            GraphicsDeviceManager.PreferredBackBufferWidth = 1280;
            GraphicsDeviceManager.PreferredBackBufferHeight = 720;
            IsFixedTimeStep = true;
            ForceOneUpdatePerDraw = true;
            IsDrawDesynchronized = false;
            TargetElapsedTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60);
            ScreenShotAutomationEnabled = false;
        }

        protected override Task LoadContent()
        {
            SetUpScene();
            Window.Title = $"Stride soft body stress — {_mode} — warmup";
            Window.Position = new Int2(100, 100);
            Window.Visible = true;
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
            camera.Transform.Position = new Vector3(0f, 1.2f, 3f);
            camera.Transform.Rotation = Quaternion.RotationX(-0.25f);
            root.Add(camera);

            var floor = new Entity
            {
                new ModelComponent(MakeModel(new PlaneProceduralModel { Size = new Vector2(20f) }, new Color(150, 146, 140))),
                new StaticComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider { Size = new Vector3(20f, 1f, 20f), PositionLocal = new Vector3(0f, -0.5f, 0f) } } } },
            };
            root.Add(floor);

            Services.GetOrCreate<SoftBodyConfiguration>().DeformationMode = _mode;
            var soft = new VolumetricSoftBodyComponent { Resolution = 10 };
            var model = MakeModel(new SphereProceduralModel { Radius = 0.5f, Tessellation = 288 }, new Color(255, 111, 89));
            var entity = new Entity { new ModelComponent(model), soft };
            entity.Transform.Position = new Vector3(0f, 2f, 0f);
            root.Add(entity);
        }

        protected override void Draw(GameTime gameTime)
        {
            var commandList = GraphicsContext.CommandList;
            ref var pool = ref _gpuTimers[_gpuFrame % _gpuTimers.Length];
            pool ??= QueryPool.New(GraphicsDevice, QueryType.Timestamp, 2);
            if (_gpuFrame >= _gpuTimers.Length && pool.TryGetData(_gpuTicks))
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
                if (pe.Key == SoftBodyProcessor.DrawKey && pe.Type == ProfilingMessageType.End)
                {
                    if (_frame >= WarmupFrames && _cpuTimes.Count < MeasuredFrames)
                        _cpuTimes.Add(pe.ElapsedTime.TotalMilliseconds);
                }
            }
            _frame++;

            if (_frame % 30 == 0)
            {
                string phase = _frame < WarmupFrames ? "warmup" : $"{_cpuTimes.Count}/{MeasuredFrames}";
                double lastCpu = _cpuTimes.Count > 0 ? _cpuTimes[^1] * 1000.0 : 0;
                double lastGpu = _gpuTimes.Count > 0 ? _gpuTimes[^1] : 0;
                Window.Title = $"Stride soft body stress — {_mode} — {phase} — CPU deform {lastCpu:0}µs | GPU frame {lastGpu:0.0}ms";
            }

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
