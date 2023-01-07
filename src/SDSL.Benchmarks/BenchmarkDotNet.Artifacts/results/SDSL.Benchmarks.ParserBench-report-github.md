``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 11 (10.0.22621.963)
AMD Ryzen 5 3500U with Radeon Vega Mobile Gfx, 1 CPU, 8 logical and 4 physical cores
.NET SDK=7.0.101
  [Host] : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2

Job=MediumRun  Toolchain=InProcessNoEmitToolchain  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
|      Method |     Mean |    Error |   StdDev |     Gen0 |    Gen1 | Allocated |
|------------ |---------:|---------:|---------:|---------:|--------:|----------:|
| StrideParse | 535.8 μs | 48.59 μs | 69.68 μs | 102.0508 | 33.6914 | 306.85 KB |
|    EtoParse | 430.2 μs | 18.54 μs | 27.17 μs |  78.1250 |       - | 161.17 KB |
