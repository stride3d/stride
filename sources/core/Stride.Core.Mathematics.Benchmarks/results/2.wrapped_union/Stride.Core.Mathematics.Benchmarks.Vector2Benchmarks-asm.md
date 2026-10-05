## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.IsNormalized()
       add       rcx,18
       vmovsd    xmm0,qword ptr [rcx]
       vinsertps xmm0,xmm0,xmm0,3C
       vdpps     xmm0,xmm0,xmm0,0FF
       vsubss    xmm0,xmm0,dword ptr [7FFC5B133D10]
       vandps    xmm0,xmm0,[7FFC5B133D20]
       vmovss    xmm1,dword ptr [7FFC5B133D30]
       vucomiss  xmm1,xmm0
       seta      al
       movzx     eax,al
       ret
; Total bytes of code 55
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Length()
       add       rcx,18
       vmovsd    xmm0,qword ptr [rcx]
       vinsertps xmm0,xmm0,xmm0,3C
       vdpps     xmm0,xmm0,xmm0,0FF
       vsqrtss   xmm0,xmm0,xmm0
       ret
; Total bytes of code 25
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.LengthSquared()
       add       rcx,18
       vmovsd    xmm0,qword ptr [rcx]
       vinsertps xmm0,xmm0,xmm0,3C
       vdpps     xmm0,xmm0,xmm0,0FF
       ret
; Total bytes of code 21
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Normalize()
       vmovsd    xmm0,qword ptr [rcx+18]
       vmovaps   xmm1,xmm0
       vinsertps xmm2,xmm1,xmm1,3C
       vdpps     xmm2,xmm2,xmm2,0FF
       vsqrtss   xmm2,xmm2,xmm2
       vucomiss  xmm2,dword ptr [7FFC5B154078]
       jbe       short M00_L00
       vbroadcastss xmm0,xmm2
       vdivps    xmm0,xmm1,xmm0
M00_L00:
       vmovq     rax,xmm0
       ret
; Total bytes of code 50
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.MoveTo()
       lea       rax,[rcx+18]
       lea       rdx,[rcx+20]
       vmovss    xmm0,dword ptr [rcx+10]
       vmovsd    xmm1,qword ptr [rdx]
       vmovsd    xmm2,qword ptr [rax]
       vsubps    xmm1,xmm1,xmm2
       vinsertps xmm3,xmm1,xmm1,3C
       vdpps     xmm3,xmm3,xmm3,0FF
       vsqrtss   xmm3,xmm3,xmm3
       vucomiss  xmm0,xmm3
       jae       short M00_L02
       vxorps    xmm4,xmm4,xmm4
       vucomiss  xmm3,xmm4
       jp        short M00_L00
       je        short M00_L02
M00_L00:
       vdivss    xmm0,xmm0,xmm3
       vbroadcastss xmm0,xmm0
       vfmadd231ps xmm2,xmm0,xmm1
M00_L01:
       vmovq     rax,xmm2
       ret
M00_L02:
       vmovsd    xmm2,qword ptr [rdx]
       jmp       short M00_L01
; Total bytes of code 85
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Add()
       vmovsd    xmm0,qword ptr [rcx+18]
       vmovsd    xmm1,qword ptr [rcx+20]
       vaddps    xmm0,xmm1,xmm0
       vmovq     rax,xmm0
       ret
; Total bytes of code 20
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Subtract()
       vmovsd    xmm0,qword ptr [rcx+18]
       vmovsd    xmm1,qword ptr [rcx+20]
       vsubps    xmm0,xmm0,xmm1
       vmovq     rax,xmm0
       ret
; Total bytes of code 20
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Multiply()
       vmovsd    xmm0,qword ptr [rcx+18]
       vbroadcastss xmm1,dword ptr [rcx+10]
       vmulps    xmm0,xmm1,xmm0
       vmovq     rax,xmm0
       ret
; Total bytes of code 21
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Modulate()
       vmovsd    xmm0,qword ptr [rcx+18]
       vmovsd    xmm1,qword ptr [rcx+20]
       vmulps    xmm0,xmm1,xmm0
       vmovq     rax,xmm0
       ret
; Total bytes of code 20
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Divide()
       vmovsd    xmm0,qword ptr [rcx+18]
       vbroadcastss xmm1,dword ptr [rcx+10]
       vdivps    xmm0,xmm0,xmm1
       vmovq     rax,xmm0
       ret
; Total bytes of code 21
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Demodulate()
       vmovsd    xmm0,qword ptr [rcx+18]
       vmovsd    xmm1,qword ptr [rcx+20]
       vdivps    xmm0,xmm0,xmm1
       vmovq     rax,xmm0
       ret
; Total bytes of code 20
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Negate()
       vmovsd    xmm0,qword ptr [rcx+18]
       vxorps    xmm0,xmm0,[7FFC5B143EA0]
       vmovq     rax,xmm0
       ret
; Total bytes of code 19
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Barycentric()
       vmovsd    xmm0,qword ptr [rcx+18]
       vmovsd    xmm1,qword ptr [rcx+20]
       vmovsd    xmm2,qword ptr [rcx+28]
       vmovss    xmm3,dword ptr [rcx+10]
       vmovss    xmm4,dword ptr [rcx+14]
       vbroadcastss xmm3,xmm3
       vbroadcastss xmm4,xmm4
       vsubps    xmm1,xmm1,xmm0
       vmulps    xmm1,xmm1,xmm3
       vaddps    xmm1,xmm1,xmm0
       vsubps    xmm0,xmm2,xmm0
       vmulps    xmm0,xmm0,xmm4
       vaddps    xmm0,xmm0,xmm1
       vmovq     rax,xmm0
       ret
; Total bytes of code 65
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Clamp()
       vmovsd    xmm0,qword ptr [rcx+18]
       vmovsd    xmm1,qword ptr [rcx+20]
       vmovsd    xmm2,qword ptr [rcx+28]
       vcmpeqps  xmm3,xmm0,xmm1
       vxorps    xmm4,xmm4,xmm4
       vpcmpgtd  xmm4,xmm4,xmm1
       vandps    xmm3,xmm4,xmm3
       vcmpneqps xmm4,xmm0,xmm0
       vorps     xmm3,xmm4,xmm3
       vcmpltps  xmm4,xmm1,xmm0
       vorps     xmm3,xmm4,xmm3
       vblendvps xmm0,xmm1,xmm0,xmm3
       vcmpeqps  xmm1,xmm0,xmm2
       vxorps    xmm3,xmm3,xmm3
       vpcmpgtd  xmm3,xmm3,xmm0
       vandps    xmm1,xmm3,xmm1
       vcmpneqps xmm3,xmm0,xmm0
       vorps     xmm1,xmm3,xmm1
       vcmpltps  xmm3,xmm0,xmm2
       vorps     xmm1,xmm3,xmm1
       vblendvps xmm0,xmm2,xmm0,xmm1
       vmovq     rax,xmm0
       ret
; Total bytes of code 103
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Distance()
       vmovsd    xmm0,qword ptr [rcx+18]
       vmovsd    xmm1,qword ptr [rcx+20]
       vsubps    xmm0,xmm0,xmm1
       vinsertps xmm0,xmm0,xmm0,3C
       vdpps     xmm0,xmm0,xmm0,0FF
       vsqrtss   xmm0,xmm0,xmm0
       ret
; Total bytes of code 31
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.DistanceSquared()
       vmovsd    xmm0,qword ptr [rcx+18]
       vmovsd    xmm1,qword ptr [rcx+20]
       vsubps    xmm0,xmm0,xmm1
       vinsertps xmm0,xmm0,xmm0,3C
       vdpps     xmm0,xmm0,xmm0,0FF
       ret
; Total bytes of code 27
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Dot()
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,xmm0,3C
       vmovsd    xmm1,qword ptr [rcx+20]
       vinsertps xmm1,xmm1,xmm1,3C
       vdpps     xmm0,xmm0,xmm1,0FF
       ret
; Total bytes of code 29
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Lerp()
       vmovsd    xmm0,qword ptr [rcx+18]
       vmovsd    xmm1,qword ptr [rcx+20]
       vmovss    xmm2,dword ptr [rcx+10]
       vbroadcastss xmm2,xmm2
       vbroadcastss xmm3,dword ptr [7FFC5B163F50]
       vsubps    xmm3,xmm3,xmm2
       vmulps    xmm1,xmm2,xmm1
       vfmadd213ps xmm0,xmm3,xmm1
       vmovq     rax,xmm0
       ret
; Total bytes of code 48
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.SmoothStep()
       vmovsd    xmm0,qword ptr [rcx+18]
       vmovsd    xmm1,qword ptr [rcx+20]
       vmovss    xmm2,dword ptr [rcx+10]
       vxorps    xmm3,xmm3,xmm3
       vcmpneqps xmm4,xmm2,xmm2
       vorps     xmm3,xmm4,xmm3
       vxorps    xmm4,xmm4,xmm4
       vcmpgtps  xmm4,xmm2,xmm4
       vorps     xmm3,xmm4,xmm3
       vandps    xmm2,xmm3,xmm2
       vbroadcastss xmm3,dword ptr [7FFC5B1441A0]
       vminss    xmm2,xmm3,xmm2
       vmulss    xmm4,xmm2,xmm2
       vaddss    xmm2,xmm2,xmm2
       vmovss    xmm5,dword ptr [7FFC5B1441A4]
       vsubss    xmm2,xmm5,xmm2
       vmulss    xmm2,xmm4,xmm2
       vbroadcastss xmm2,xmm2
       vsubps    xmm3,xmm3,xmm2
       vmulps    xmm1,xmm2,xmm1
       vfmadd213ps xmm0,xmm3,xmm1
       vmovq     rax,xmm0
       ret
; Total bytes of code 106
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Hermite()
       sub       rsp,38
       vmovaps   [rsp+20],xmm6
       vmovaps   [rsp+10],xmm7
       vmovaps   [rsp],xmm8
       vmovsd    xmm0,qword ptr [rcx+18]
       vmovsd    xmm1,qword ptr [rcx+20]
       vmovsd    xmm2,qword ptr [rcx+28]
       vmovsd    xmm3,qword ptr [rcx+30]
       vmovss    xmm4,dword ptr [rcx+10]
       vmulss    xmm5,xmm4,xmm4
       vmulss    xmm6,xmm5,xmm4
       vaddss    xmm7,xmm5,xmm5
       vsubss    xmm7,xmm6,xmm7
       vaddss    xmm4,xmm7,xmm4
       vmulss    xmm7,xmm6,dword ptr [7FFC5B134418]
       vmulss    xmm8,xmm5,dword ptr [7FFC5B13441C]
       vaddss    xmm7,xmm7,xmm8
       vsubss    xmm5,xmm6,xmm5
       vaddss    xmm6,xmm6,xmm6
       vsubss    xmm6,xmm6,xmm8
       vaddss    xmm6,xmm6,dword ptr [7FFC5B134420]
       vbroadcastss xmm6,xmm6
       vmulps    xmm0,xmm6,xmm0
       vbroadcastss xmm4,xmm4
       vmulps    xmm1,xmm4,xmm1
       vaddps    xmm0,xmm1,xmm0
       vbroadcastss xmm1,xmm7
       vmulps    xmm1,xmm1,xmm2
       vaddps    xmm0,xmm1,xmm0
       vbroadcastss xmm1,xmm5
       vmulps    xmm1,xmm1,xmm3
       vaddps    xmm0,xmm1,xmm0
       vmovq     rax,xmm0
       vmovaps   xmm6,[rsp+20]
       vmovaps   xmm7,[rsp+10]
       vmovaps   xmm8,[rsp]
       add       rsp,38
       ret
; Total bytes of code 183
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.CatmullRom()
       sub       rsp,58
       vmovaps   [rsp+40],xmm6
       vmovaps   [rsp+30],xmm7
       vmovaps   [rsp+20],xmm8
       vmovaps   [rsp+10],xmm9
       vmovaps   [rsp],xmm10
       vmovsd    xmm0,qword ptr [rcx+18]
       vmovsd    xmm1,qword ptr [rcx+20]
       vmovsd    xmm2,qword ptr [rcx+28]
       vmovsd    xmm3,qword ptr [rcx+30]
       vmovss    xmm4,dword ptr [rcx+10]
       vmulss    xmm5,xmm4,xmm4
       vmulss    xmm6,xmm5,xmm4
       vbroadcastss xmm7,dword ptr [7FFC5B1749F0]
       vmulps    xmm8,xmm7,xmm1
       vmovaps   xmm9,xmm0
       vxorps    xmm9,xmm9,[7FFC5B174A00]
       vmovaps   xmm10,xmm9
       vaddps    xmm10,xmm10,xmm2
       vbroadcastss xmm4,xmm4
       vmulps    xmm4,xmm4,xmm10
       vaddps    xmm4,xmm4,xmm8
       vmulps    xmm0,xmm0,xmm7
       vmulps    xmm7,xmm1,[7FFC5B174A10]
       vsubps    xmm0,xmm0,xmm7
       vmulps    xmm7,xmm2,[7FFC5B174A20]
       vaddps    xmm0,xmm7,xmm0
       vmovaps   xmm7,xmm3
       vsubps    xmm0,xmm0,xmm7
       vbroadcastss xmm5,xmm5
       vmulps    xmm0,xmm5,xmm0
       vaddps    xmm0,xmm0,xmm4
       vbroadcastss xmm4,dword ptr [7FFC5B174A30]
       vmulps    xmm1,xmm4,xmm1
       vaddps    xmm1,xmm1,xmm9
       vmulps    xmm2,xmm4,xmm2
       vsubps    xmm1,xmm1,xmm2
       vaddps    xmm1,xmm3,xmm1
       vbroadcastss xmm2,xmm6
       vmulps    xmm1,xmm2,xmm1
       vaddps    xmm0,xmm1,xmm0
       vmulps    xmm0,xmm0,[7FFC5B174A40]
       vmovq     rax,xmm0
       vmovaps   xmm6,[rsp+40]
       vmovaps   xmm7,[rsp+30]
       vmovaps   xmm8,[rsp+20]
       vmovaps   xmm9,[rsp+10]
       vmovaps   xmm10,[rsp]
       add       rsp,58
       ret
; Total bytes of code 254
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Max()
       vmovsd    xmm0,qword ptr [rcx+18]
       vmovsd    xmm1,qword ptr [rcx+20]
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
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Min()
       vmovsd    xmm0,qword ptr [rcx+18]
       vmovsd    xmm1,qword ptr [rcx+20]
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
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Reflect()
       vmovsd    xmm0,qword ptr [rcx+18]
       vmovsd    xmm1,qword ptr [rcx+20]
       vinsertps xmm2,xmm0,xmm0,3C
       vinsertps xmm3,xmm1,xmm1,3C
       vdpps     xmm2,xmm2,xmm3,0FF
       vaddps    xmm2,xmm2,xmm2
       vxorps    xmm2,xmm2,[7FFC5B173F60]
       vfmadd213ps xmm2,xmm1,xmm0
       vmovq     rax,xmm2
       ret
; Total bytes of code 51
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Orthogonalize()
       push      rsi
       push      rbx
       sub       rsp,58
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+30],ymm4
       mov       [rsp+50],rax
       mov       rax,[rcx+8]
       mov       rdx,offset MT_Stride.Core.Mathematics.Vector2[]
       mov       [rsp+28],rdx
       lea       rdx,[rsp+28]
       mov       dword ptr [rdx+8],4
       lea       rdx,[rsp+28]
       mov       r8,[rcx+18]
       mov       [rdx+10],r8
       mov       r8,[rcx+20]
       mov       [rdx+18],r8
       mov       r8,[rcx+28]
       mov       [rdx+20],r8
       mov       rcx,[rcx+30]
       mov       [rdx+28],rcx
       test      rax,rax
       je        near ptr M00_L05
       mov       ecx,[rax+8]
       cmp       ecx,4
       jl        near ptr M00_L06
       xor       r8d,r8d
       cmp       r8d,4
       jge       short M00_L03
M00_L00:
       mov       r10d,r8d
       vmovsd    xmm0,qword ptr [rdx+r10*8+10]
       xor       r9d,r9d
       test      r8d,r8d
       jle       short M00_L02
       cmp       ecx,r8d
       jl        short M00_L04
       lea       r9,[rax+10]
       mov       r11d,r8d
       nop       dword ptr [rax]
M00_L01:
       vmovsd    xmm1,qword ptr [r9]
       vinsertps xmm2,xmm1,xmm1,3C
       vinsertps xmm3,xmm0,xmm0,3C
       vdpps     xmm3,xmm2,xmm3,0FF
       vdpps     xmm2,xmm2,xmm2,0FF
       vdivss    xmm2,xmm3,xmm2
       vbroadcastss xmm3,xmm2
       vmulps    xmm2,xmm3,xmm1
       vsubps    xmm0,xmm0,xmm2
       add       r9,8
       dec       r11d
       jne       short M00_L01
M00_L02:
       vmovsd    qword ptr [rax+r10*8+10],xmm0
       inc       r8d
       cmp       r8d,4
       jl        short M00_L00
M00_L03:
       add       rsp,58
       pop       rbx
       pop       rsi
       ret
M00_L04:
       cmp       r9d,ecx
       jae       near ptr M00_L07
       mov       r11d,r9d
       vmovsd    xmm1,qword ptr [rax+r11*8+10]
       vinsertps xmm2,xmm1,xmm1,3C
       vmovaps   xmm3,xmm0
       vinsertps xmm3,xmm3,xmm3,3C
       vdpps     xmm3,xmm2,xmm3,0FF
       vdpps     xmm2,xmm2,xmm2,0FF
       vdivss    xmm2,xmm3,xmm2
       vbroadcastss xmm2,xmm2
       vmulps    xmm2,xmm2,xmm1
       vmovaps   xmm1,xmm2
       vsubps    xmm0,xmm0,xmm1
       inc       r9d
       cmp       r9d,r8d
       jl        short M00_L04
       jmp       short M00_L02
M00_L05:
       mov       ecx,611
       mov       rdx,7FFC5B3D36E0
       call      qword ptr [7FFC5B317798]
       mov       rcx,rax
       call      qword ptr [7FFC5B5A75D0]
       int       3
M00_L06:
       mov       rcx,offset MT_System.ArgumentOutOfRangeException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       ecx,611
       mov       rdx,7FFC5B3D36E0
       call      qword ptr [7FFC5B317798]
       mov       rsi,rax
       mov       ecx,629
       mov       rdx,7FFC5B3D36E0
       call      qword ptr [7FFC5B317798]
       mov       r8,rax
       mov       rdx,rsi
       mov       rcx,rbx
       call      qword ptr [7FFC5B51E538]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
M00_L07:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 440
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Orthonormalize()
       push      rsi
       push      rbx
       sub       rsp,58
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+30],ymm4
       mov       [rsp+50],rax
       mov       rax,[rcx+8]
       mov       rdx,offset MT_Stride.Core.Mathematics.Vector2[]
       mov       [rsp+28],rdx
       lea       rdx,[rsp+28]
       mov       dword ptr [rdx+8],4
       lea       rdx,[rsp+28]
       mov       r8,[rcx+18]
       mov       [rdx+10],r8
       mov       r8,[rcx+20]
       mov       [rdx+18],r8
       mov       r8,[rcx+28]
       mov       [rdx+20],r8
       mov       rcx,[rcx+30]
       mov       [rdx+28],rcx
       test      rax,rax
       je        near ptr M00_L07
       mov       ecx,[rax+8]
       cmp       ecx,4
       jl        near ptr M00_L08
       xor       r8d,r8d
       cmp       r8d,4
       jl        short M00_L04
M00_L00:
       add       rsp,58
       pop       rbx
       pop       rsi
       ret
M00_L01:
       cmp       r9d,ecx
       jae       near ptr M00_L09
       mov       r11d,r9d
       vmovsd    xmm1,qword ptr [rax+r11*8+10]
       vinsertps xmm2,xmm1,xmm1,3C
       vmovaps   xmm3,xmm0
       vinsertps xmm3,xmm3,xmm3,3C
       vdpps     xmm2,xmm2,xmm3,0FF
       vmulps    xmm1,xmm2,xmm1
       vsubps    xmm0,xmm0,xmm1
       inc       r9d
       cmp       r9d,r8d
       jl        short M00_L01
       jmp       short M00_L06
M00_L02:
       vbroadcastss xmm1,xmm1
       vdivps    xmm0,xmm0,xmm1
M00_L03:
       vmovsd    qword ptr [rax+r10*8+10],xmm0
       inc       r8d
       cmp       r8d,4
       jge       short M00_L00
M00_L04:
       mov       r10d,r8d
       vmovsd    xmm0,qword ptr [rdx+r10*8+10]
       xor       r9d,r9d
       test      r8d,r8d
       jle       short M00_L06
       cmp       ecx,r8d
       jl        short M00_L01
       lea       r9,[rax+10]
       mov       r11d,r8d
M00_L05:
       vmovsd    xmm1,qword ptr [r9]
       vinsertps xmm2,xmm1,xmm1,3C
       vinsertps xmm3,xmm0,xmm0,3C
       vdpps     xmm2,xmm2,xmm3,0FF
       vmulps    xmm1,xmm2,xmm1
       vsubps    xmm0,xmm0,xmm1
       add       r9,8
       dec       r11d
       jne       short M00_L05
M00_L06:
       vmovaps   xmm1,xmm0
       vinsertps xmm1,xmm1,xmm1,3C
       vdpps     xmm1,xmm1,xmm1,0FF
       vsqrtss   xmm1,xmm1,xmm1
       vucomiss  xmm1,dword ptr [7FFC5B144AC8]
       jbe       short M00_L03
       jmp       short M00_L02
M00_L07:
       mov       ecx,611
       mov       rdx,7FFC5B3A36E0
       call      qword ptr [7FFC5B2E7798]
       mov       rcx,rax
       call      qword ptr [7FFC5B577630]
       int       3
M00_L08:
       mov       rcx,offset MT_System.ArgumentOutOfRangeException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       ecx,611
       mov       rdx,7FFC5B3A36E0
       call      qword ptr [7FFC5B2E7798]
       mov       rsi,rax
       mov       ecx,629
       mov       rdx,7FFC5B3A36E0
       call      qword ptr [7FFC5B2E7798]
       mov       r8,rax
       mov       rdx,rsi
       mov       rcx,rbx
       call      qword ptr [7FFC5B4EE538]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
M00_L09:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 443
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Transform()
       sub       rsp,28
       vmovaps   [rsp+10],xmm6
       vmovaps   [rsp],xmm7
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,dword ptr [7FFC5B164F70],34
       vmovups   xmm1,[7FFC5B164F80]
       vpermilps xmm2,xmm1,4E
       vmovshdup xmm3,xmm0
       vbroadcastss xmm3,xmm3
       vmulps    xmm2,xmm3,xmm2
       vmovups   xmm3,[7FFC5B164F90]
       vpermilps xmm4,xmm1,1B
       vmovaps   xmm5,xmm0
       vbroadcastss xmm5,xmm5
       vmulps    xmm4,xmm5,xmm4
       vmovddup  xmm5,qword ptr [7FFC5B164FA0]
       vshufps   xmm6,xmm0,xmm0,0FF
       vbroadcastss xmm6,xmm6
       vmulps    xmm6,xmm6,xmm1
       vfmadd213ps xmm4,xmm5,xmm6
       vfmadd213ps xmm2,xmm3,xmm4
       vpermilps xmm1,xmm1,0B1
       vunpckhps xmm0,xmm0,xmm0
       vbroadcastss xmm0,xmm0
       vmulps    xmm0,xmm0,xmm1
       vmovups   xmm1,[7FFC5B164FB0]
       vfmadd231ps xmm2,xmm1,xmm0
       vpermilps xmm0,xmm2,4E
       vbroadcastss xmm4,dword ptr [7FFC5B164F70]
       vmulps    xmm0,xmm0,xmm4
       vpermilps xmm6,xmm2,1B
       vmulps    xmm6,xmm6,xmm4
       vmulps    xmm7,xmm4,xmm2
       vfmadd213ps xmm6,xmm5,xmm7
       vfmadd213ps xmm0,xmm3,xmm6
       vpermilps xmm2,xmm2,0B1
       vmulps    xmm2,xmm2,xmm4
       vfmadd213ps xmm2,xmm1,xmm0
       vmovq     rax,xmm2
       vmovaps   xmm6,[rsp+10]
       vmovaps   xmm7,[rsp]
       add       rsp,28
       ret
; Total bytes of code 227
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Operator_Add()
       vmovsd    xmm0,qword ptr [rcx+18]
       vmovsd    xmm1,qword ptr [rcx+20]
       vaddps    xmm0,xmm1,xmm0
       vmovq     rax,xmm0
       ret
; Total bytes of code 20
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Operator_Subtract()
       vmovsd    xmm0,qword ptr [rcx+18]
       vmovsd    xmm1,qword ptr [rcx+20]
       vsubps    xmm0,xmm0,xmm1
       vmovq     rax,xmm0
       ret
; Total bytes of code 20
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Operator_Negate()
       vmovsd    xmm0,qword ptr [rcx+18]
       vxorps    xmm0,xmm0,[7FFC5B163E20]
       vmovq     rax,xmm0
       ret
; Total bytes of code 19
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Operator_Multiply()
       vmovsd    xmm0,qword ptr [rcx+18]
       vmovsd    xmm1,qword ptr [rcx+20]
       vmulps    xmm0,xmm1,xmm0
       vmovq     rax,xmm0
       ret
; Total bytes of code 20
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Operator_Divide()
       vmovsd    xmm0,qword ptr [rcx+18]
       vmovsd    xmm1,qword ptr [rcx+20]
       vdivps    xmm0,xmm0,xmm1
       vmovq     rax,xmm0
       ret
; Total bytes of code 20
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Operator_Equals()
       vmovsd    xmm0,qword ptr [rcx+18]
       vmovsd    xmm1,qword ptr [rcx+20]
       vsubps    xmm0,xmm0,xmm1
       vandps    xmm0,xmm0,[7FFC5B154090]
       vcmpltps  xmm0,xmm0,[7FFC5B1540A0]
       vinsertps xmm0,xmm0,xmm0,3C
       vpcmpeqd  xmm0,xmm0,[7FFC5B1540B0]
       vpmovmskb eax,xmm0
       cmp       eax,0FFFF
       sete      al
       movzx     eax,al
       ret
; Total bytes of code 61
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Operator_NotEquals()
       vmovsd    xmm0,qword ptr [rcx+18]
       vmovsd    xmm1,qword ptr [rcx+20]
       vsubps    xmm0,xmm0,xmm1
       vandps    xmm0,xmm0,[7FFC5B1340C0]
       vcmpltps  xmm0,xmm0,[7FFC5B1340D0]
       vinsertps xmm0,xmm0,xmm0,3C
       vpcmpeqd  xmm0,xmm0,[7FFC5B1340E0]
       vpmovmskb eax,xmm0
       cmp       eax,0FFFF
       setne     al
       movzx     eax,al
       ret
; Total bytes of code 61
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.EqualsStrict()
       vmovsd    xmm0,qword ptr [rcx+18]
       vmovsd    xmm1,qword ptr [rcx+20]
       vcmpeqps  xmm0,xmm1,xmm0
       vinsertps xmm0,xmm0,xmm0,3C
       vpcmpeqd  xmm0,xmm0,[7FFC5B173D10]
       vpmovmskb eax,xmm0
       cmp       eax,0FFFF
       sete      al
       movzx     eax,al
       ret
; Total bytes of code 45
```

