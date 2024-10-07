// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet;
using BenchmarkDotNet.Running;
using Stride.Shaders.Spirv.Core.Benchmarks;

BenchmarkRunner.Run<ParserBench>();

Console.WriteLine("Hello, World!");


