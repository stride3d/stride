```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
Intel Core Ultra 7 165H 1.40GHz, 1 CPU, 22 logical and 16 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=5  LaunchCount=1  
WarmupCount=5  

```
| Method               | Mean       | Code Size |
|--------------------- |-----------:|----------:|
| IsIdentity           |  0.2951 ns |      46 B |
| IsNormalized         |  0.5050 ns |      49 B |
| Angle                |  3.5440 ns |      84 B |
| Axis                 |  1.3408 ns |     137 B |
| YawPitchRoll         | 15.8445 ns |     503 B |
| Conjugate            |  0.2582 ns |      21 B |
| Invert               |  0.6400 ns |      44 B |
| Length               |  0.3028 ns |      19 B |
| LengthSquared        |  0.1613 ns |      15 B |
| Normalize            |  0.5034 ns |      41 B |
| Add                  |  0.2337 ns |      18 B |
| Subtract             |  0.2923 ns |      18 B |
| Multiply             |  0.5973 ns |     122 B |
| Negate               |  0.0859 ns |      21 B |
| Barycentric          | 69.8998 ns |     839 B |
| Dot                  |  0.2419 ns |      13 B |
| AngleBetween         |  4.2476 ns |      58 B |
| Exponential          |  5.4265 ns |     144 B |
| Lerp                 |  2.0198 ns |      97 B |
| Logarithm            | 10.9028 ns |     144 B |
| RotationX            |  4.9225 ns |      77 B |
| RotationY            |  4.9839 ns |      77 B |
| RotationZ            |  4.9068 ns |      77 B |
| RotationYawPitchRoll |  7.5387 ns |     794 B |
| Slerp                | 19.1986 ns |     350 B |
| RotateTowards        | 24.0134 ns |     431 B |
| Squad                | 70.7930 ns |     817 B |
| Operator_Add         |  0.1997 ns |      18 B |
| Operator_Subtract    |  0.2082 ns |      18 B |
| Operator_Negate      |  0.2569 ns |      21 B |
| Operator_Multiply    |  0.5542 ns |     116 B |
| Operator_Equals      |  0.2170 ns |      43 B |
| Operator_NotEquals   |  0.2491 ns |      43 B |
