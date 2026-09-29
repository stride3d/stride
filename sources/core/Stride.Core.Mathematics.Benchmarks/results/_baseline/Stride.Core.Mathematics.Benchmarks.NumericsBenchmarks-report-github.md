```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 6900HX with Radeon Graphics 3.30GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=5  LaunchCount=1  
WarmupCount=5  

```
| Method                  | Mean      | Code Size |
|------------------------ |----------:|----------:|
| Vector2_Length          | 0.5899 ns |      25 B |
| Vector2_LengthSquared   | 0.5392 ns |      21 B |
| Vector2_Normalize       | 1.0693 ns |      36 B |
| Vector2_Add             | 0.0464 ns |      20 B |
| Vector2_Subtract        | 0.4264 ns |      20 B |
| Vector2_Multiply        | 0.0221 ns |      21 B |
| Vector2_Divide          | 0.0994 ns |      21 B |
| Vector2_Negate          | 0.0339 ns |      19 B |
| Vector2_Distance        | 0.5364 ns |      31 B |
| Vector2_DistanceSquared | 0.5373 ns |      27 B |
| Vector2_Dot             | 0.5602 ns |      29 B |
| Vector2_Lerp            | 0.0441 ns |      44 B |
| Vector2_Max             | 0.0393 ns |      57 B |
| Vector2_Min             | 0.0488 ns |      57 B |
| Vector2_Transform       | 2.8772 ns |     206 B |
| Vector3_Length          | 0.5455 ns |      32 B |
| Vector3_LengthSquared   | 0.6001 ns |      28 B |
| Vector3_Normalize       | 1.3436 ns |      47 B |
| Vector3_Add             | 0.4179 ns |      43 B |
| Vector3_Subtract        | 0.4870 ns |      43 B |
| Vector3_Multiply        | 0.2297 ns |      37 B |
| Vector3_Divide          | 0.2597 ns |      37 B |
| Vector3_Negate          | 0.2157 ns |      35 B |
| Vector3_Distance        | 0.8402 ns |      45 B |
| Vector3_DistanceSquared | 0.8075 ns |      41 B |
| Vector3_Dot             | 0.8221 ns |      43 B |
| Vector3_Lerp            | 0.7419 ns |      67 B |
| Vector3_Max             | 0.5367 ns |      80 B |
| Vector3_Min             | 0.5557 ns |      80 B |
| Vector3_Transform       | 3.6468 ns |     222 B |
| Vector4_Length          | 0.5392 ns |      19 B |
| Vector4_LengthSquared   | 0.5375 ns |      15 B |
| Vector4_Normalize       | 1.0955 ns |      27 B |
| Vector4_Add             | 0.4218 ns |      18 B |
| Vector4_Subtract        | 0.4263 ns |      18 B |
| Vector4_Multiply        | 0.4109 ns |      23 B |
| Vector4_Divide          | 0.4100 ns |      23 B |
| Vector4_Negate          | 0.1391 ns |      21 B |
| Vector4_Distance        | 0.5083 ns |      21 B |
| Vector4_DistanceSquared | 0.5014 ns |      17 B |
| Vector4_Dot             | 0.5078 ns |      13 B |
| Vector4_Lerp            | 0.5721 ns |      46 B |
| Vector4_Max             | 0.4681 ns |      59 B |
| Vector4_Min             | 0.4271 ns |      59 B |
| Vector4_Transform       | 2.8364 ns |     198 B |
