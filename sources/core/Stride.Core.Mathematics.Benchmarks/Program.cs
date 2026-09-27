using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Stride.Core.Mathematics.Benchmarks;

// dotnet run -c Release -f net10.0 --filter "*"

var config = DefaultConfig.Instance
    .AddJob(Job.ShortRun
        .WithWarmupCount(5)
        .WithIterationCount(5)); // Job.ShortRun provides a fast feedback loop

BenchmarkSwitcher.FromAssembly(typeof(QuaternionBenchmarks).Assembly).Run(args, config);
