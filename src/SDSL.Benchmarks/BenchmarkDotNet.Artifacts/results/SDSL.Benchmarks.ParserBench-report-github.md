``` ini

BenchmarkDotNet=v0.13.3, OS=Windows 11 (10.0.22621.963)
AMD Ryzen 5 3500U with Radeon Vega Mobile Gfx, 1 CPU, 8 logical and 4 physical cores
.NET SDK=7.0.101
  [Host]     : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2


```
|      Method |     Mean |    Error |   StdDev |    Gen0 |    Gen1 | Allocated |
|------------ |---------:|---------:|---------:|--------:|--------:|----------:|
| StrideParse | 416.2 μs | 13.24 μs | 38.19 μs | 88.8672 | 26.8555 |  246.9 KB |
|    EtoParse | 418.3 μs | 11.48 μs | 33.13 μs | 79.5898 |       - | 163.28 KB |
