## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector2_Length()
       add       rcx,0C
       vmovsd    xmm0,qword ptr [rcx]
       vinsertps xmm0,xmm0,xmm0,3C
       vdpps     xmm0,xmm0,xmm0,0FF
       vsqrtss   xmm0,xmm0,xmm0
       ret
; Total bytes of code 25
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector2_LengthSquared()
       add       rcx,0C
       vmovsd    xmm0,qword ptr [rcx]
       vinsertps xmm0,xmm0,xmm0,3C
       vdpps     xmm0,xmm0,xmm0,0FF
       ret
; Total bytes of code 21
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector2_Normalize()
       vmovsd    xmm0,qword ptr [rcx+0C]
       vinsertps xmm1,xmm0,xmm0,3C
       vdpps     xmm1,xmm1,xmm1,0FF
       vsqrtss   xmm1,xmm1,xmm1
       vbroadcastss xmm1,xmm1
       vdivps    xmm0,xmm0,xmm1
       vmovq     rax,xmm0
       ret
; Total bytes of code 36
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector2_Add()
       vmovsd    xmm0,qword ptr [rcx+0C]
       vmovsd    xmm1,qword ptr [rcx+14]
       vaddps    xmm0,xmm1,xmm0
       vmovq     rax,xmm0
       ret
; Total bytes of code 20
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector2_Subtract()
       vmovsd    xmm0,qword ptr [rcx+0C]
       vmovsd    xmm1,qword ptr [rcx+14]
       vsubps    xmm0,xmm0,xmm1
       vmovq     rax,xmm0
       ret
; Total bytes of code 20
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector2_Multiply()
       vmovsd    xmm0,qword ptr [rcx+0C]
       vbroadcastss xmm1,dword ptr [rcx+8]
       vmulps    xmm0,xmm1,xmm0
       vmovq     rax,xmm0
       ret
; Total bytes of code 21
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector2_Divide()
       vmovsd    xmm0,qword ptr [rcx+0C]
       vbroadcastss xmm1,dword ptr [rcx+8]
       vdivps    xmm0,xmm0,xmm1
       vmovq     rax,xmm0
       ret
; Total bytes of code 21
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector2_Negate()
       vmovsd    xmm0,qword ptr [rcx+0C]
       vxorps    xmm0,xmm0,[7FFC2BEEB880]
       vmovq     rax,xmm0
       ret
; Total bytes of code 19
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector2_Distance()
       vmovsd    xmm0,qword ptr [rcx+0C]
       vmovsd    xmm1,qword ptr [rcx+14]
       vsubps    xmm0,xmm0,xmm1
       vinsertps xmm0,xmm0,xmm0,3C
       vdpps     xmm0,xmm0,xmm0,0FF
       vsqrtss   xmm0,xmm0,xmm0
       ret
; Total bytes of code 31
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector2_DistanceSquared()
       vmovsd    xmm0,qword ptr [rcx+0C]
       vmovsd    xmm1,qword ptr [rcx+14]
       vsubps    xmm0,xmm0,xmm1
       vinsertps xmm0,xmm0,xmm0,3C
       vdpps     xmm0,xmm0,xmm0,0FF
       ret
; Total bytes of code 27
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector2_Dot()
       vmovsd    xmm0,qword ptr [rcx+0C]
       vinsertps xmm0,xmm0,xmm0,3C
       vmovsd    xmm1,qword ptr [rcx+14]
       vinsertps xmm1,xmm1,xmm1,3C
       vdpps     xmm0,xmm0,xmm1,0FF
       ret
; Total bytes of code 29
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector2_Lerp()
       vmovsd    xmm0,qword ptr [rcx+0C]
       vmovsd    xmm1,qword ptr [rcx+14]
       vbroadcastss xmm2,dword ptr [rcx+8]
       vbroadcastss xmm3,dword ptr [7FFC2BEFB980]
       vsubps    xmm3,xmm3,xmm2
       vmulps    xmm1,xmm2,xmm1
       vfmadd213ps xmm0,xmm3,xmm1
       vmovq     rax,xmm0
       ret
; Total bytes of code 44
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector2_Max()
       vmovsd    xmm0,qword ptr [rcx+0C]
       vmovsd    xmm1,qword ptr [rcx+14]
       vcmpeqps  xmm2,xmm0,xmm1
       vxorps    xmm3,xmm3,xmm3
       vpcmpgtd  xmm3,xmm3,xmm1
       vandps    xmm2,xmm3,xmm2
       vcmpneqps xmm3,xmm0,xmm0
       vorps     xmm2,xmm3,xmm2
       vcmpltps  xmm3,xmm1,xmm0
       vorps     xmm2,xmm3,xmm2
       vblendvps xmm0,xmm1,xmm0,xmm2
       vmovq     rax,xmm0
       ret
; Total bytes of code 57
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector2_Min()
       vmovsd    xmm0,qword ptr [rcx+0C]
       vmovsd    xmm1,qword ptr [rcx+14]
       vcmpeqps  xmm2,xmm0,xmm1
       vxorps    xmm3,xmm3,xmm3
       vpcmpgtd  xmm3,xmm3,xmm0
       vandps    xmm2,xmm3,xmm2
       vcmpneqps xmm3,xmm0,xmm0
       vorps     xmm2,xmm3,xmm2
       vcmpltps  xmm3,xmm0,xmm1
       vorps     xmm2,xmm3,xmm2
       vblendvps xmm0,xmm1,xmm0,xmm2
       vmovq     rax,xmm0
       ret
; Total bytes of code 57
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector2_Transform()
       sub       rsp,18
       vmovaps   [rsp],xmm6
       vmovsd    xmm0,qword ptr [rcx+0C]
       vinsertps xmm0,xmm0,dword ptr [7FFC2BF0C010],34
       vmovups   xmm1,[7FFC2BF0C020]
       vpermilps xmm2,xmm1,4E
       vmovshdup xmm3,xmm0
       vbroadcastss xmm3,xmm3
       vmulps    xmm2,xmm3,xmm2
       vmovups   xmm3,[7FFC2BF0C030]
       vpermilps xmm4,xmm1,1B
       vmovaps   xmm5,xmm0
       vbroadcastss xmm5,xmm5
       vmulps    xmm4,xmm5,xmm4
       vmovddup  xmm5,qword ptr [7FFC2BF0C040]
       vshufps   xmm6,xmm0,xmm0,0FF
       vbroadcastss xmm6,xmm6
       vmulps    xmm6,xmm6,xmm1
       vfmadd213ps xmm4,xmm5,xmm6
       vfmadd213ps xmm2,xmm3,xmm4
       vpermilps xmm1,xmm1,0B1
       vunpckhps xmm0,xmm0,xmm0
       vbroadcastss xmm0,xmm0
       vmulps    xmm0,xmm0,xmm1
       vmovups   xmm1,[7FFC2BF0C050]
       vfmadd231ps xmm2,xmm1,xmm0
       vpermilps xmm0,xmm2,4E
       vxorps    xmm4,xmm4,xmm4
       vmulps    xmm0,xmm0,xmm4
       vpermilps xmm6,xmm2,1B
       vmulps    xmm6,xmm6,xmm4
       vfmadd213ps xmm6,xmm5,xmm2
       vfmadd213ps xmm0,xmm3,xmm6
       vpermilps xmm2,xmm2,0B1
       vmulps    xmm2,xmm2,xmm4
       vfmadd213ps xmm2,xmm1,xmm0
       vmovq     rax,xmm2
       vmovaps   xmm6,[rsp]
       add       rsp,18
       ret
; Total bytes of code 206
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector3_Length()
       add       rcx,1C
       vmovsd    xmm0,qword ptr [rcx]
       vinsertps xmm0,xmm0,dword ptr [rcx+8],28
       vinsertps xmm0,xmm0,xmm0,38
       vdpps     xmm0,xmm0,xmm0,0FF
       vsqrtss   xmm0,xmm0,xmm0
       ret
; Total bytes of code 32
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector3_LengthSquared()
       add       rcx,1C
       vmovsd    xmm0,qword ptr [rcx]
       vinsertps xmm0,xmm0,dword ptr [rcx+8],28
       vinsertps xmm0,xmm0,xmm0,38
       vdpps     xmm0,xmm0,xmm0,0FF
       ret
; Total bytes of code 28
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector3_Normalize()
       vmovsd    xmm0,qword ptr [rcx+1C]
       vinsertps xmm0,xmm0,dword ptr [rcx+24],28
       vinsertps xmm1,xmm0,xmm0,38
       vdpps     xmm1,xmm1,xmm1,0FF
       vsqrtps   xmm1,xmm1
       vdivps    xmm0,xmm0,xmm1
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       ret
; Total bytes of code 47
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector3_Add()
       vmovsd    xmm0,qword ptr [rcx+1C]
       vinsertps xmm0,xmm0,dword ptr [rcx+24],28
       vmovsd    xmm1,qword ptr [rcx+28]
       vinsertps xmm1,xmm1,dword ptr [rcx+30],28
       vaddps    xmm0,xmm1,xmm0
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       ret
; Total bytes of code 43
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector3_Subtract()
       vmovsd    xmm0,qword ptr [rcx+1C]
       vinsertps xmm0,xmm0,dword ptr [rcx+24],28
       vmovsd    xmm1,qword ptr [rcx+28]
       vinsertps xmm1,xmm1,dword ptr [rcx+30],28
       vsubps    xmm0,xmm0,xmm1
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       ret
; Total bytes of code 43
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector3_Multiply()
       vmovsd    xmm0,qword ptr [rcx+1C]
       vinsertps xmm0,xmm0,dword ptr [rcx+24],28
       vbroadcastss xmm1,dword ptr [rcx+8]
       vmulps    xmm0,xmm1,xmm0
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       ret
; Total bytes of code 37
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector3_Divide()
       vmovsd    xmm0,qword ptr [rcx+1C]
       vinsertps xmm0,xmm0,dword ptr [rcx+24],28
       vbroadcastss xmm1,dword ptr [rcx+8]
       vdivps    xmm0,xmm0,xmm1
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       ret
; Total bytes of code 37
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector3_Negate()
       vmovsd    xmm0,qword ptr [rcx+1C]
       vinsertps xmm0,xmm0,dword ptr [rcx+24],28
       vxorps    xmm0,xmm0,[7FFC2BEDB980]
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       ret
; Total bytes of code 35
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector3_Distance()
       vmovsd    xmm0,qword ptr [rcx+1C]
       vinsertps xmm0,xmm0,dword ptr [rcx+24],28
       vmovsd    xmm1,qword ptr [rcx+28]
       vinsertps xmm1,xmm1,dword ptr [rcx+30],28
       vsubps    xmm0,xmm0,xmm1
       vinsertps xmm0,xmm0,xmm0,38
       vdpps     xmm0,xmm0,xmm0,0FF
       vsqrtss   xmm0,xmm0,xmm0
       ret
; Total bytes of code 45
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector3_DistanceSquared()
       vmovsd    xmm0,qword ptr [rcx+1C]
       vinsertps xmm0,xmm0,dword ptr [rcx+24],28
       vmovsd    xmm1,qword ptr [rcx+28]
       vinsertps xmm1,xmm1,dword ptr [rcx+30],28
       vsubps    xmm0,xmm0,xmm1
       vinsertps xmm0,xmm0,xmm0,38
       vdpps     xmm0,xmm0,xmm0,0FF
       ret
; Total bytes of code 41
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector3_Dot()
       vmovsd    xmm0,qword ptr [rcx+1C]
       vinsertps xmm0,xmm0,dword ptr [rcx+24],28
       vinsertps xmm0,xmm0,xmm0,38
       vmovsd    xmm1,qword ptr [rcx+28]
       vinsertps xmm1,xmm1,dword ptr [rcx+30],28
       vinsertps xmm1,xmm1,xmm1,38
       vdpps     xmm0,xmm0,xmm1,0FF
       ret
; Total bytes of code 43
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector3_Lerp()
       vmovsd    xmm0,qword ptr [rcx+1C]
       vinsertps xmm0,xmm0,dword ptr [rcx+24],28
       vmovsd    xmm1,qword ptr [rcx+28]
       vinsertps xmm1,xmm1,dword ptr [rcx+30],28
       vbroadcastss xmm2,dword ptr [rcx+8]
       vbroadcastss xmm3,dword ptr [7FFC2BF1BA88]
       vsubps    xmm3,xmm3,xmm2
       vmulps    xmm1,xmm2,xmm1
       vfmadd213ps xmm0,xmm3,xmm1
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       ret
; Total bytes of code 67
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector3_Max()
       vmovsd    xmm0,qword ptr [rcx+1C]
       vinsertps xmm0,xmm0,dword ptr [rcx+24],28
       vmovsd    xmm1,qword ptr [rcx+28]
       vinsertps xmm1,xmm1,dword ptr [rcx+30],28
       vcmpeqps  xmm2,xmm0,xmm1
       vxorps    xmm3,xmm3,xmm3
       vpcmpgtd  xmm3,xmm3,xmm1
       vandps    xmm2,xmm3,xmm2
       vcmpneqps xmm3,xmm0,xmm0
       vorps     xmm2,xmm3,xmm2
       vcmpltps  xmm3,xmm1,xmm0
       vorps     xmm2,xmm3,xmm2
       vblendvps xmm0,xmm1,xmm0,xmm2
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       ret
; Total bytes of code 80
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector3_Min()
       vmovsd    xmm0,qword ptr [rcx+1C]
       vinsertps xmm0,xmm0,dword ptr [rcx+24],28
       vmovsd    xmm1,qword ptr [rcx+28]
       vinsertps xmm1,xmm1,dword ptr [rcx+30],28
       vcmpeqps  xmm2,xmm0,xmm1
       vxorps    xmm3,xmm3,xmm3
       vpcmpgtd  xmm3,xmm3,xmm0
       vandps    xmm2,xmm3,xmm2
       vcmpneqps xmm3,xmm0,xmm0
       vorps     xmm2,xmm3,xmm2
       vcmpltps  xmm3,xmm0,xmm1
       vorps     xmm2,xmm3,xmm2
       vblendvps xmm0,xmm1,xmm0,xmm2
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       ret
; Total bytes of code 80
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector3_Transform()
       sub       rsp,18
       vmovaps   [rsp],xmm6
       vmovsd    xmm0,qword ptr [rcx+1C]
       vinsertps xmm0,xmm0,dword ptr [rcx+24],28
       vinsertps xmm0,xmm0,dword ptr [7FFC2BEDC190],30
       vmovups   xmm1,[7FFC2BEDC1A0]
       vpermilps xmm2,xmm1,4E
       vmovshdup xmm3,xmm0
       vbroadcastss xmm3,xmm3
       vmulps    xmm2,xmm3,xmm2
       vmovups   xmm3,[7FFC2BEDC1B0]
       vpermilps xmm4,xmm1,1B
       vmovaps   xmm5,xmm0
       vbroadcastss xmm5,xmm5
       vmulps    xmm4,xmm5,xmm4
       vmovddup  xmm5,qword ptr [7FFC2BEDC1C0]
       vshufps   xmm6,xmm0,xmm0,0FF
       vbroadcastss xmm6,xmm6
       vmulps    xmm6,xmm6,xmm1
       vfmadd213ps xmm4,xmm5,xmm6
       vfmadd213ps xmm2,xmm3,xmm4
       vpermilps xmm1,xmm1,0B1
       vunpckhps xmm0,xmm0,xmm0
       vbroadcastss xmm0,xmm0
       vmulps    xmm0,xmm0,xmm1
       vmovups   xmm1,[7FFC2BEDC1D0]
       vfmadd231ps xmm2,xmm1,xmm0
       vpermilps xmm0,xmm2,4E
       vxorps    xmm4,xmm4,xmm4
       vmulps    xmm0,xmm0,xmm4
       vpermilps xmm6,xmm2,1B
       vmulps    xmm6,xmm6,xmm4
       vfmadd213ps xmm6,xmm5,xmm2
       vfmadd213ps xmm0,xmm3,xmm6
       vpermilps xmm2,xmm2,0B1
       vmulps    xmm2,xmm2,xmm4
       vfmadd213ps xmm2,xmm1,xmm0
       vmovsd    qword ptr [rdx],xmm2
       vextractps dword ptr [rdx+8],xmm2,2
       mov       rax,rdx
       vmovaps   xmm6,[rsp]
       add       rsp,18
       ret
; Total bytes of code 222
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector4_Length()
       add       rcx,34
       vmovups   xmm0,[rcx]
       vdpps     xmm0,xmm0,xmm0,0FF
       vsqrtss   xmm0,xmm0,xmm0
       ret
; Total bytes of code 19
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector4_LengthSquared()
       add       rcx,34
       vmovups   xmm0,[rcx]
       vdpps     xmm0,xmm0,xmm0,0FF
       ret
; Total bytes of code 15
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector4_Normalize()
       vmovups   xmm0,[rcx+34]
       vdpps     xmm1,xmm0,xmm0,0FF
       vsqrtps   xmm1,xmm1
       vdivps    xmm0,xmm0,xmm1
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 27
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector4_Add()
       vmovups   xmm0,[rcx+34]
       vaddps    xmm0,xmm0,[rcx+44]
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 18
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector4_Subtract()
       vmovups   xmm0,[rcx+34]
       vsubps    xmm0,xmm0,[rcx+44]
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 18
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector4_Multiply()
       vmovups   xmm0,[rcx+34]
       vbroadcastss xmm1,dword ptr [rcx+8]
       vmulps    xmm0,xmm1,xmm0
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 23
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector4_Divide()
       vmovups   xmm0,[rcx+34]
       vbroadcastss xmm1,dword ptr [rcx+8]
       vdivps    xmm0,xmm0,xmm1
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 23
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector4_Negate()
       vmovups   xmm0,[rcx+34]
       vxorps    xmm0,xmm0,[7FFC2BF0B930]
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 21
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector4_Distance()
       vmovups   xmm0,[rcx+34]
       vsubps    xmm0,xmm0,[rcx+44]
       vdpps     xmm0,xmm0,xmm0,0FF
       vsqrtss   xmm0,xmm0,xmm0
       ret
; Total bytes of code 21
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector4_DistanceSquared()
       vmovups   xmm0,[rcx+34]
       vsubps    xmm0,xmm0,[rcx+44]
       vdpps     xmm0,xmm0,xmm0,0FF
       ret
; Total bytes of code 17
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector4_Dot()
       vmovups   xmm0,[rcx+34]
       vdpps     xmm0,xmm0,[rcx+44],0FF
       ret
; Total bytes of code 13
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector4_Lerp()
       vmovups   xmm0,[rcx+34]
       vmovups   xmm1,[rcx+44]
       vbroadcastss xmm2,dword ptr [rcx+8]
       vbroadcastss xmm3,dword ptr [7FFC2BEEBA20]
       vsubps    xmm3,xmm3,xmm2
       vmulps    xmm1,xmm2,xmm1
       vfmadd213ps xmm0,xmm3,xmm1
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 46
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector4_Max()
       vmovups   xmm0,[rcx+34]
       vmovups   xmm1,[rcx+44]
       vcmpeqps  xmm2,xmm0,xmm1
       vxorps    xmm3,xmm3,xmm3
       vpcmpgtd  xmm3,xmm3,xmm1
       vandps    xmm2,xmm3,xmm2
       vcmpneqps xmm3,xmm0,xmm0
       vorps     xmm2,xmm3,xmm2
       vcmpltps  xmm3,xmm1,xmm0
       vorps     xmm2,xmm3,xmm2
       vblendvps xmm0,xmm1,xmm0,xmm2
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 59
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector4_Min()
       vmovups   xmm0,[rcx+34]
       vmovups   xmm1,[rcx+44]
       vcmpeqps  xmm2,xmm0,xmm1
       vxorps    xmm3,xmm3,xmm3
       vpcmpgtd  xmm3,xmm3,xmm0
       vandps    xmm2,xmm3,xmm2
       vcmpneqps xmm3,xmm0,xmm0
       vorps     xmm2,xmm3,xmm2
       vcmpltps  xmm3,xmm0,xmm1
       vorps     xmm2,xmm3,xmm2
       vblendvps xmm0,xmm1,xmm0,xmm2
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 59
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.NumericsBenchmarks.Vector4_Transform()
       sub       rsp,18
       vmovaps   [rsp],xmm6
       vmovups   xmm0,[rcx+34]
       vmovups   xmm1,[7FFC2BF0BEB0]
       vpermilps xmm2,xmm1,4E
       vmovshdup xmm3,xmm0
       vbroadcastss xmm3,xmm3
       vmulps    xmm2,xmm3,xmm2
       vmovups   xmm3,[7FFC2BF0BEC0]
       vpermilps xmm4,xmm1,1B
       vmovaps   xmm5,xmm0
       vbroadcastss xmm5,xmm5
       vmulps    xmm4,xmm5,xmm4
       vmovddup  xmm5,qword ptr [7FFC2BF0BED0]
       vshufps   xmm6,xmm0,xmm0,0FF
       vbroadcastss xmm6,xmm6
       vmulps    xmm6,xmm6,xmm1
       vfmadd213ps xmm4,xmm5,xmm6
       vfmadd213ps xmm2,xmm3,xmm4
       vpermilps xmm1,xmm1,0B1
       vunpckhps xmm0,xmm0,xmm0
       vbroadcastss xmm0,xmm0
       vmulps    xmm0,xmm0,xmm1
       vmovups   xmm1,[7FFC2BF0BEE0]
       vfmadd231ps xmm2,xmm1,xmm0
       vpermilps xmm0,xmm2,4E
       vxorps    xmm4,xmm4,xmm4
       vmulps    xmm0,xmm0,xmm4
       vpermilps xmm6,xmm2,1B
       vmulps    xmm6,xmm6,xmm4
       vfmadd213ps xmm6,xmm5,xmm2
       vfmadd213ps xmm0,xmm3,xmm6
       vpermilps xmm2,xmm2,0B1
       vmulps    xmm2,xmm2,xmm4
       vfmadd213ps xmm2,xmm1,xmm0
       vmovups   [rdx],xmm2
       mov       rax,rdx
       vmovaps   xmm6,[rsp]
       add       rsp,18
       ret
; Total bytes of code 198
```

