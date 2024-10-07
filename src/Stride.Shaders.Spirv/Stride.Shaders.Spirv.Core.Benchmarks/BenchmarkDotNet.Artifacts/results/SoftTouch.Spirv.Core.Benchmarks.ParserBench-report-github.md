``` ini

BenchmarkDotNet=v0.13.5, OS=Windows 10 (10.0.19045.2846/22H2/2022Update)
Intel Core i5-8265U CPU 1.60GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
.NET SDK=7.0.100
  [Host]     : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2


```
|      Method |           Mean |         Error |        StdDev |         Median | Allocated |
|------------ |---------------:|--------------:|--------------:|---------------:|----------:|
| MemorySlice |      0.6161 ns |     0.0977 ns |     0.2803 ns |      0.5250 ns |         - |
|       Count |     11.0984 ns |     0.4045 ns |     1.1606 ns |     10.9187 ns |         - |
|       Parse | 36,396.4577 ns | 1,022.9391 ns | 2,851.5453 ns | 35,373.0682 ns |         - |
| ParseToList | 46,043.3036 ns |   732.9096 ns |   649.7052 ns | 45,818.8660 ns |         - |
