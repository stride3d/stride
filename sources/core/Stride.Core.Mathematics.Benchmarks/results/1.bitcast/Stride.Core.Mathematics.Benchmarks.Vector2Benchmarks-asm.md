## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.IsNormalized()
       push      rax
       mov       rax,[rcx+18]
       mov       [rsp],rax
       vmovsd    xmm0,qword ptr [rsp]
       vinsertps xmm0,xmm0,xmm0,3C
       vdpps     xmm0,xmm0,xmm0,0FF
       vsubss    xmm0,xmm0,dword ptr [7FFC5B153DE0]
       vandps    xmm0,xmm0,[7FFC5B153DF0]
       vmovss    xmm1,dword ptr [7FFC5B153E00]
       vucomiss  xmm1,xmm0
       seta      al
       movzx     eax,al
       add       rsp,8
       ret
; Total bytes of code 65
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Length()
       push      rax
       mov       rax,[rcx+18]
       mov       [rsp],rax
       vmovsd    xmm0,qword ptr [rsp]
       vinsertps xmm0,xmm0,xmm0,3C
       vdpps     xmm0,xmm0,xmm0,0FF
       vsqrtss   xmm0,xmm0,xmm0
       add       rsp,8
       ret
; Total bytes of code 35
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.LengthSquared()
       push      rax
       mov       rax,[rcx+18]
       mov       [rsp],rax
       vmovsd    xmm0,qword ptr [rsp]
       vinsertps xmm0,xmm0,xmm0,3C
       vdpps     xmm0,xmm0,xmm0,0FF
       add       rsp,8
       ret
; Total bytes of code 31
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Normalize()
       sub       rsp,18
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    dword ptr [rsp+10],xmm0
       vmovss    dword ptr [rsp+14],xmm1
       vmovsd    xmm2,qword ptr [rsp+10]
       vinsertps xmm2,xmm2,xmm2,3C
       vdpps     xmm2,xmm2,xmm2,0FF
       vsqrtss   xmm2,xmm2,xmm2
       vucomiss  xmm2,dword ptr [7FFC5B174100]
       jbe       short M00_L00
       vmovss    dword ptr [rsp+8],xmm0
       vmovss    dword ptr [rsp+0C],xmm1
       vmovsd    xmm0,qword ptr [rsp+8]
       vbroadcastss xmm1,xmm2
       vdivps    xmm0,xmm0,xmm1
       vmovsd    qword ptr [rsp],xmm0
       vmovss    xmm0,dword ptr [rsp]
       vmovss    xmm1,dword ptr [rsp+4]
M00_L00:
       vmovd     eax,xmm0
       vmovd     ecx,xmm1
       shl       rcx,20
       or        rax,rcx
       add       rsp,18
       ret
; Total bytes of code 121
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.MoveTo()
       sub       rsp,48
       lea       rax,[rcx+18]
       lea       rdx,[rcx+20]
       vmovss    xmm0,dword ptr [rcx+10]
       mov       rcx,[rdx]
       mov       [rsp+38],rcx
       mov       rcx,[rax]
       mov       [rsp+30],rcx
       vmovsd    xmm1,qword ptr [rsp+38]
       vmovsd    xmm2,qword ptr [rsp+30]
       vsubps    xmm1,xmm1,xmm2
       vmovsd    qword ptr [rsp+28],xmm1
       vmovss    xmm1,dword ptr [rsp+28]
       vmovss    xmm2,dword ptr [rsp+2C]
       vmovss    dword ptr [rsp+20],xmm1
       vmovss    dword ptr [rsp+24],xmm2
       vmovsd    xmm3,qword ptr [rsp+20]
       vinsertps xmm3,xmm3,xmm3,3C
       vdpps     xmm3,xmm3,xmm3,0FF
       vsqrtss   xmm3,xmm3,xmm3
       vucomiss  xmm0,xmm3
       jae       near ptr M00_L02
       vxorps    xmm4,xmm4,xmm4
       vucomiss  xmm3,xmm4
       jp        short M00_L00
       je        short M00_L02
M00_L00:
       vdivss    xmm0,xmm0,xmm3
       vbroadcastss xmm0,xmm0
       vmovsd    qword ptr [rsp+18],xmm0
       vmovss    xmm0,dword ptr [rsp+18]
       vmovss    xmm3,dword ptr [rsp+1C]
       vmovss    dword ptr [rsp+10],xmm1
       vmovss    dword ptr [rsp+14],xmm2
       vmovsd    xmm1,qword ptr [rsp+10]
       vmovss    dword ptr [rsp+8],xmm0
       vmovss    dword ptr [rsp+0C],xmm3
       vmovsd    xmm0,qword ptr [rsp+8]
       mov       rdx,[rax]
       mov       [rsp],rdx
       vmovsd    xmm2,qword ptr [rsp]
       vfmadd231ps xmm2,xmm0,xmm1
       vmovsd    qword ptr [rsp+40],xmm2
       vmovss    xmm0,dword ptr [rsp+40]
       vmovss    xmm1,dword ptr [rsp+44]
M00_L01:
       vmovd     eax,xmm0
       vmovd     ecx,xmm1
       shl       rcx,20
       or        rax,rcx
       add       rsp,48
       ret
M00_L02:
       vmovss    xmm0,dword ptr [rdx]
       vmovss    xmm1,dword ptr [rdx+4]
       jmp       short M00_L01
; Total bytes of code 252
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Add()
       sub       rsp,18
       mov       rax,[rcx+18]
       mov       [rsp+10],rax
       mov       rax,[rcx+20]
       mov       [rsp+8],rax
       vmovsd    xmm0,qword ptr [rsp+10]
       vmovsd    xmm1,qword ptr [rsp+8]
       vaddps    xmm0,xmm1,xmm0
       vmovq     rax,xmm0
       add       rsp,18
       ret
; Total bytes of code 48
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Subtract()
       sub       rsp,18
       lea       rax,[rcx+18]
       add       rcx,20
       mov       rax,[rax]
       mov       [rsp+10],rax
       mov       rax,[rcx]
       mov       [rsp+8],rax
       vmovsd    xmm0,qword ptr [rsp+10]
       vmovsd    xmm1,qword ptr [rsp+8]
       vsubps    xmm0,xmm0,xmm1
       vmovq     rax,xmm0
       add       rsp,18
       ret
; Total bytes of code 54
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Multiply()
       push      rax
       mov       rax,[rcx+18]
       mov       [rsp],rax
       vmovsd    xmm0,qword ptr [rsp]
       vbroadcastss xmm1,dword ptr [rcx+10]
       vmulps    xmm0,xmm1,xmm0
       vmovq     rax,xmm0
       add       rsp,8
       ret
; Total bytes of code 34
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Modulate()
       sub       rsp,18
       mov       rax,[rcx+18]
       mov       [rsp+10],rax
       mov       rax,[rcx+20]
       mov       [rsp+8],rax
       vmovsd    xmm0,qword ptr [rsp+10]
       vmovsd    xmm1,qword ptr [rsp+8]
       vmulps    xmm0,xmm1,xmm0
       vmovq     rax,xmm0
       add       rsp,18
       ret
; Total bytes of code 48
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Divide()
       push      rax
       mov       rax,[rcx+18]
       mov       [rsp],rax
       vmovsd    xmm0,qword ptr [rsp]
       vbroadcastss xmm1,dword ptr [rcx+10]
       vdivps    xmm0,xmm0,xmm1
       vmovq     rax,xmm0
       add       rsp,8
       ret
; Total bytes of code 34
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Demodulate()
       sub       rsp,18
       mov       rax,[rcx+18]
       mov       [rsp+10],rax
       mov       rax,[rcx+20]
       mov       [rsp+8],rax
       vmovsd    xmm0,qword ptr [rsp+10]
       vmovsd    xmm1,qword ptr [rsp+8]
       vdivps    xmm0,xmm0,xmm1
       vmovq     rax,xmm0
       add       rsp,18
       ret
; Total bytes of code 48
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Negate()
       push      rax
       mov       rax,[rcx+18]
       mov       [rsp],rax
       vmovsd    xmm0,qword ptr [rsp]
       vxorps    xmm0,xmm0,[7FFC5B153EB0]
       vmovq     rax,xmm0
       add       rsp,8
       ret
; Total bytes of code 32
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Barycentric()
       sub       rsp,88
       mov       rax,[rcx+18]
       mov       [rsp+80],rax
       mov       rax,[rcx+20]
       mov       [rsp+78],rax
       mov       rax,[rcx+28]
       mov       [rsp+58],rax
       vmovss    xmm0,dword ptr [rcx+10]
       vmovss    xmm1,dword ptr [rcx+14]
       vbroadcastss xmm0,xmm0
       vmovsd    qword ptr [rsp+30],xmm0
       vmovss    xmm0,dword ptr [rsp+30]
       vmovss    xmm2,dword ptr [rsp+34]
       vbroadcastss xmm1,xmm1
       vmovsd    qword ptr [rsp+28],xmm1
       vmovss    xmm1,dword ptr [rsp+28]
       vmovss    xmm3,dword ptr [rsp+2C]
       vmovss    dword ptr [rsp+70],xmm0
       vmovss    dword ptr [rsp+74],xmm2
       vmovsd    xmm0,qword ptr [rsp+78]
       vmovsd    xmm2,qword ptr [rsp+80]
       vsubps    xmm0,xmm0,xmm2
       vmovsd    qword ptr [rsp+20],xmm0
       mov       rax,[rsp+20]
       mov       [rsp+68],rax
       vmovsd    xmm0,qword ptr [rsp+70]
       vmovsd    xmm2,qword ptr [rsp+68]
       vmulps    xmm0,xmm2,xmm0
       vmovsd    qword ptr [rsp+18],xmm0
       mov       rax,[rsp+18]
       mov       [rsp+60],rax
       vmovsd    xmm0,qword ptr [rsp+80]
       vmovsd    xmm2,qword ptr [rsp+60]
       vaddps    xmm0,xmm2,xmm0
       vmovsd    qword ptr [rsp+10],xmm0
       mov       rax,[rsp+10]
       mov       [rsp+50],rax
       vmovss    dword ptr [rsp+48],xmm1
       vmovss    dword ptr [rsp+4C],xmm3
       vmovsd    xmm0,qword ptr [rsp+58]
       vmovsd    xmm1,qword ptr [rsp+80]
       vsubps    xmm0,xmm0,xmm1
       vmovsd    qword ptr [rsp+8],xmm0
       mov       rax,[rsp+8]
       mov       [rsp+40],rax
       vmovsd    xmm0,qword ptr [rsp+48]
       vmovsd    xmm1,qword ptr [rsp+40]
       vmulps    xmm0,xmm1,xmm0
       vmovsd    qword ptr [rsp],xmm0
       mov       rax,[rsp]
       mov       [rsp+38],rax
       vmovsd    xmm0,qword ptr [rsp+50]
       vmovsd    xmm1,qword ptr [rsp+38]
       vaddps    xmm0,xmm1,xmm0
       vmovq     rax,xmm0
       add       rsp,88
       ret
; Total bytes of code 313
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Clamp()
       sub       rsp,18
       mov       rax,[rcx+18]
       mov       [rsp+10],rax
       mov       rax,[rcx+20]
       mov       [rsp+8],rax
       mov       rax,[rcx+28]
       mov       [rsp],rax
       vmovsd    xmm0,qword ptr [rsp+10]
       vmovsd    xmm1,qword ptr [rsp+8]
       vmovsd    xmm2,qword ptr [rsp]
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
       add       rsp,18
       ret
; Total bytes of code 139
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Distance()
       sub       rsp,18
       mov       rax,[rcx+18]
       mov       [rsp+10],rax
       mov       rax,[rcx+20]
       mov       [rsp+8],rax
       vmovsd    xmm0,qword ptr [rsp+10]
       vmovsd    xmm1,qword ptr [rsp+8]
       vsubps    xmm0,xmm0,xmm1
       vinsertps xmm0,xmm0,xmm0,3C
       vdpps     xmm0,xmm0,xmm0,0FF
       vsqrtss   xmm0,xmm0,xmm0
       add       rsp,18
       ret
; Total bytes of code 59
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.DistanceSquared()
       sub       rsp,18
       mov       rax,[rcx+18]
       mov       [rsp+10],rax
       mov       rax,[rcx+20]
       mov       [rsp+8],rax
       vmovsd    xmm0,qword ptr [rsp+10]
       vmovsd    xmm1,qword ptr [rsp+8]
       vsubps    xmm0,xmm0,xmm1
       vinsertps xmm0,xmm0,xmm0,3C
       vdpps     xmm0,xmm0,xmm0,0FF
       add       rsp,18
       ret
; Total bytes of code 55
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Dot()
       sub       rsp,18
       mov       rax,[rcx+18]
       mov       [rsp+10],rax
       mov       rax,[rcx+20]
       mov       [rsp+8],rax
       vmovsd    xmm0,qword ptr [rsp+10]
       vinsertps xmm0,xmm0,xmm0,3C
       vmovsd    xmm1,qword ptr [rsp+8]
       vinsertps xmm1,xmm1,xmm1,3C
       vdpps     xmm0,xmm0,xmm1,0FF
       add       rsp,18
       ret
; Total bytes of code 57
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Lerp()
       sub       rsp,18
       mov       rax,[rcx+18]
       mov       [rsp+10],rax
       mov       rax,[rcx+20]
       mov       [rsp+8],rax
       vmovss    xmm0,dword ptr [rcx+10]
       vmovsd    xmm1,qword ptr [rsp+10]
       vmovsd    xmm2,qword ptr [rsp+8]
       vbroadcastss xmm0,xmm0
       vbroadcastss xmm3,dword ptr [7FFC5B173F80]
       vsubps    xmm3,xmm3,xmm0
       vmulps    xmm0,xmm0,xmm2
       vfmadd213ps xmm1,xmm3,xmm0
       vmovq     rax,xmm1
       add       rsp,18
       ret
; Total bytes of code 76
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.SmoothStep()
       sub       rsp,18
       mov       rax,[rcx+18]
       mov       [rsp+10],rax
       mov       rax,[rcx+20]
       mov       [rsp+8],rax
       vmovss    xmm0,dword ptr [rcx+10]
       vxorps    xmm1,xmm1,xmm1
       vcmpneqps xmm2,xmm0,xmm0
       vorps     xmm1,xmm2,xmm1
       vxorps    xmm2,xmm2,xmm2
       vcmpgtps  xmm2,xmm0,xmm2
       vorps     xmm1,xmm2,xmm1
       vandps    xmm0,xmm1,xmm0
       vbroadcastss xmm1,dword ptr [7FFC5B1441C8]
       vminss    xmm0,xmm1,xmm0
       vmulss    xmm2,xmm0,xmm0
       vaddss    xmm0,xmm0,xmm0
       vmovss    xmm3,dword ptr [7FFC5B1441CC]
       vsubss    xmm0,xmm3,xmm0
       vmulss    xmm0,xmm2,xmm0
       vmovsd    xmm2,qword ptr [rsp+10]
       vmovsd    xmm3,qword ptr [rsp+8]
       vbroadcastss xmm0,xmm0
       vsubps    xmm1,xmm1,xmm0
       vmulps    xmm0,xmm0,xmm3
       vfmadd213ps xmm2,xmm1,xmm0
       vmovq     rax,xmm2
       add       rsp,18
       ret
; Total bytes of code 134
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Hermite()
       sub       rsp,88
       mov       rax,[rcx+18]
       mov       [rsp+80],rax
       mov       rax,[rcx+20]
       mov       [rsp+78],rax
       mov       rax,[rcx+28]
       mov       [rsp+60],rax
       mov       rax,[rcx+30]
       mov       [rsp+48],rax
       vmovss    xmm0,dword ptr [rcx+10]
       vmulss    xmm1,xmm0,xmm0
       vmulss    xmm2,xmm1,xmm0
       vaddss    xmm3,xmm1,xmm1
       vsubss    xmm3,xmm2,xmm3
       vaddss    xmm0,xmm3,xmm0
       vmulss    xmm3,xmm2,dword ptr [7FFC5B1444A0]
       vmulss    xmm4,xmm1,dword ptr [7FFC5B1444A4]
       vaddss    xmm3,xmm3,xmm4
       vsubss    xmm1,xmm2,xmm1
       vaddss    xmm2,xmm2,xmm2
       vsubss    xmm2,xmm2,xmm4
       vaddss    xmm2,xmm2,dword ptr [7FFC5B1444A8]
       vbroadcastss xmm2,xmm2
       vmovsd    xmm4,qword ptr [rsp+80]
       vmulps    xmm2,xmm2,xmm4
       vmovsd    qword ptr [rsp+30],xmm2
       mov       rax,[rsp+30]
       mov       [rsp+70],rax
       vmovsd    xmm2,qword ptr [rsp+78]
       vbroadcastss xmm0,xmm0
       vmulps    xmm0,xmm0,xmm2
       vmovsd    qword ptr [rsp+28],xmm0
       mov       rax,[rsp+28]
       mov       [rsp+68],rax
       vmovsd    xmm0,qword ptr [rsp+70]
       vmovsd    xmm2,qword ptr [rsp+68]
       vaddps    xmm0,xmm2,xmm0
       vmovsd    qword ptr [rsp+20],xmm0
       mov       rax,[rsp+20]
       mov       [rsp+58],rax
       vmovsd    xmm0,qword ptr [rsp+60]
       vbroadcastss xmm2,xmm3
       vmulps    xmm0,xmm2,xmm0
       vmovsd    qword ptr [rsp+18],xmm0
       mov       rax,[rsp+18]
       mov       [rsp+50],rax
       vmovsd    xmm0,qword ptr [rsp+58]
       vmovsd    xmm2,qword ptr [rsp+50]
       vaddps    xmm0,xmm2,xmm0
       vmovsd    qword ptr [rsp+10],xmm0
       mov       rax,[rsp+10]
       mov       [rsp+40],rax
       vmovsd    xmm0,qword ptr [rsp+48]
       vbroadcastss xmm1,xmm1
       vmulps    xmm0,xmm1,xmm0
       vmovsd    qword ptr [rsp+8],xmm0
       mov       rax,[rsp+8]
       mov       [rsp+38],rax
       vmovsd    xmm0,qword ptr [rsp+40]
       vmovsd    xmm1,qword ptr [rsp+38]
       vaddps    xmm0,xmm1,xmm0
       vmovq     rax,xmm0
       add       rsp,88
       ret
; Total bytes of code 331
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.CatmullRom()
       sub       rsp,188
       vmovaps   [rsp+170],xmm6
       mov       rax,[rcx+18]
       mov       [rsp+160],rax
       mov       rax,[rcx+20]
       mov       [rsp+168],rax
       mov       rax,[rcx+28]
       mov       [rsp+150],rax
       mov       rax,[rcx+30]
       mov       [rsp+108],rax
       vmovss    xmm0,dword ptr [rcx+10]
       vmulss    xmm1,xmm0,xmm0
       vmulss    xmm2,xmm1,xmm0
       vmovsd    xmm3,qword ptr [rsp+168]
       vbroadcastss xmm4,dword ptr [7FFC5B154CA0]
       vmulps    xmm3,xmm3,xmm4
       vmovsd    qword ptr [rsp+0A0],xmm3
       mov       rax,[rsp+0A0]
       mov       [rsp+158],rax
       vmovsd    xmm3,qword ptr [rsp+160]
       vbroadcastss xmm5,dword ptr [7FFC5B154CA4]
       vxorps    xmm3,xmm3,xmm5
       vmovsd    qword ptr [rsp+98],xmm3
       mov       rax,[rsp+98]
       mov       [rsp+148],rax
       vmovsd    xmm3,qword ptr [rsp+148]
       vmovsd    xmm6,qword ptr [rsp+150]
       vaddps    xmm3,xmm6,xmm3
       vmovsd    qword ptr [rsp+90],xmm3
       mov       rax,[rsp+90]
       mov       [rsp+140],rax
       vmovsd    xmm3,qword ptr [rsp+140]
       vbroadcastss xmm0,xmm0
       vmulps    xmm0,xmm0,xmm3
       vmovsd    qword ptr [rsp+88],xmm0
       mov       rax,[rsp+88]
       mov       [rsp+138],rax
       vmovsd    xmm0,qword ptr [rsp+158]
       vmovsd    xmm3,qword ptr [rsp+138]
       vaddps    xmm0,xmm3,xmm0
       vmovsd    qword ptr [rsp+80],xmm0
       mov       rax,[rsp+80]
       mov       [rsp+130],rax
       vmovsd    xmm0,qword ptr [rsp+160]
       vmulps    xmm0,xmm0,xmm4
       vmovsd    qword ptr [rsp+78],xmm0
       mov       rax,[rsp+78]
       mov       [rsp+128],rax
       vmovsd    xmm0,qword ptr [rsp+168]
       vmulps    xmm0,xmm0,[7FFC5B154CB0]
       vmovsd    qword ptr [rsp+70],xmm0
       mov       rax,[rsp+70]
       mov       [rsp+120],rax
       vmovsd    xmm0,qword ptr [rsp+128]
       vmovsd    xmm3,qword ptr [rsp+120]
       vsubps    xmm0,xmm0,xmm3
       vmovsd    qword ptr [rsp+68],xmm0
       mov       rax,[rsp+68]
       mov       [rsp+118],rax
       vmovsd    xmm0,qword ptr [rsp+150]
       vmulps    xmm0,xmm0,[7FFC5B154CC0]
       vmovsd    qword ptr [rsp+60],xmm0
       mov       rax,[rsp+60]
       mov       [rsp+110],rax
       vmovsd    xmm0,qword ptr [rsp+118]
       vmovsd    xmm3,qword ptr [rsp+110]
       vaddps    xmm0,xmm3,xmm0
       vmovsd    qword ptr [rsp+58],xmm0
       mov       rax,[rsp+58]
       mov       [rsp+100],rax
       vmovsd    xmm0,qword ptr [rsp+100]
       vmovsd    xmm3,qword ptr [rsp+108]
       vsubps    xmm0,xmm0,xmm3
       vmovsd    qword ptr [rsp+50],xmm0
       mov       rax,[rsp+50]
       mov       [rsp+0F8],rax
       vmovsd    xmm0,qword ptr [rsp+0F8]
       vbroadcastss xmm1,xmm1
       vmulps    xmm0,xmm1,xmm0
       vmovsd    qword ptr [rsp+48],xmm0
       mov       rax,[rsp+48]
       mov       [rsp+0F0],rax
       vmovsd    xmm0,qword ptr [rsp+130]
       vmovsd    xmm1,qword ptr [rsp+0F0]
       vaddps    xmm0,xmm1,xmm0
       vmovsd    qword ptr [rsp+40],xmm0
       mov       rax,[rsp+40]
       mov       [rsp+0E8],rax
       vmovsd    xmm0,qword ptr [rsp+160]
       vxorps    xmm0,xmm0,xmm5
       vmovsd    qword ptr [rsp+38],xmm0
       mov       rax,[rsp+38]
       mov       [rsp+0E0],rax
       vmovsd    xmm0,qword ptr [rsp+168]
       vbroadcastss xmm1,dword ptr [7FFC5B154CD0]
       vmulps    xmm0,xmm0,xmm1
       vmovsd    qword ptr [rsp+30],xmm0
       mov       rax,[rsp+30]
       mov       [rsp+0D8],rax
       vmovsd    xmm0,qword ptr [rsp+0E0]
       vmovsd    xmm3,qword ptr [rsp+0D8]
       vaddps    xmm0,xmm3,xmm0
       vmovsd    qword ptr [rsp+28],xmm0
       mov       rax,[rsp+28]
       mov       [rsp+0D0],rax
       vmovsd    xmm0,qword ptr [rsp+150]
       vmulps    xmm0,xmm0,xmm1
       vmovsd    qword ptr [rsp+20],xmm0
       mov       rax,[rsp+20]
       mov       [rsp+0C8],rax
       vmovsd    xmm0,qword ptr [rsp+0D0]
       vmovsd    xmm1,qword ptr [rsp+0C8]
       vsubps    xmm0,xmm0,xmm1
       vmovsd    qword ptr [rsp+18],xmm0
       mov       rax,[rsp+18]
       mov       [rsp+0C0],rax
       vmovsd    xmm0,qword ptr [rsp+0C0]
       vmovsd    xmm1,qword ptr [rsp+108]
       vaddps    xmm0,xmm1,xmm0
       vmovsd    qword ptr [rsp+10],xmm0
       mov       rax,[rsp+10]
       mov       [rsp+0B8],rax
       vmovsd    xmm0,qword ptr [rsp+0B8]
       vbroadcastss xmm1,xmm2
       vmulps    xmm0,xmm1,xmm0
       vmovsd    qword ptr [rsp+8],xmm0
       mov       rax,[rsp+8]
       mov       [rsp+0B0],rax
       vmovsd    xmm0,qword ptr [rsp+0E8]
       vmovsd    xmm1,qword ptr [rsp+0B0]
       vaddps    xmm0,xmm1,xmm0
       vmovsd    qword ptr [rsp],xmm0
       mov       rax,[rsp]
       mov       [rsp+0A8],rax
       vmovsd    xmm0,qword ptr [rsp+0A8]
       vmulps    xmm0,xmm0,[7FFC5B154CE0]
       vmovq     rax,xmm0
       vmovaps   xmm6,[rsp+170]
       add       rsp,188
       ret
; Total bytes of code 956
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Max()
       sub       rsp,18
       mov       rax,[rcx+18]
       mov       [rsp+10],rax
       mov       rax,[rcx+20]
       mov       [rsp+8],rax
       vmovsd    xmm0,qword ptr [rsp+10]
       vmovsd    xmm1,qword ptr [rsp+8]
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
       add       rsp,18
       ret
; Total bytes of code 85
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Min()
       sub       rsp,18
       mov       rax,[rcx+18]
       mov       [rsp+10],rax
       mov       rax,[rcx+20]
       mov       [rsp+8],rax
       vmovsd    xmm0,qword ptr [rsp+10]
       vmovsd    xmm1,qword ptr [rsp+8]
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
       add       rsp,18
       ret
; Total bytes of code 85
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Reflect()
       sub       rsp,18
       mov       rax,[rcx+18]
       mov       [rsp+10],rax
       mov       rax,[rcx+20]
       mov       [rsp+8],rax
       vmovsd    xmm0,qword ptr [rsp+10]
       vmovsd    xmm1,qword ptr [rsp+8]
       vinsertps xmm2,xmm0,xmm0,3C
       vinsertps xmm3,xmm1,xmm1,3C
       vdpps     xmm2,xmm2,xmm3,0FF
       vaddps    xmm2,xmm2,xmm2
       vxorps    xmm2,xmm2,[7FFC5B163F80]
       vfmadd213ps xmm2,xmm1,xmm0
       vmovq     rax,xmm2
       add       rsp,18
       ret
; Total bytes of code 79
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Orthogonalize()
       push      rsi
       push      rbx
       sub       rsp,98
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+30],ymm4
       vmovdqu   ymmword ptr [rsp+50],ymm4
       vmovdqu   ymmword ptr [rsp+70],ymm4
       mov       [rsp+90],rax
       mov       rax,[rcx+8]
       mov       rdx,offset MT_Stride.Core.Mathematics.Vector2[]
       mov       [rsp+28],rdx
       lea       rdx,[rsp+28]
       mov       dword ptr [rdx+8],4
       lea       rdx,[rsp+28]
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       lea       r8,[rdx+10]
       vmovss    dword ptr [r8],xmm0
       vmovss    dword ptr [r8+4],xmm1
       vmovss    xmm0,dword ptr [rcx+20]
       vmovss    xmm1,dword ptr [rcx+24]
       lea       r8,[rdx+18]
       vmovss    dword ptr [r8],xmm0
       vmovss    dword ptr [r8+4],xmm1
       vmovss    xmm0,dword ptr [rcx+28]
       vmovss    xmm1,dword ptr [rcx+2C]
       lea       r8,[rdx+20]
       vmovss    dword ptr [r8],xmm0
       vmovss    dword ptr [r8+4],xmm1
       vmovss    xmm0,dword ptr [rcx+30]
       vmovss    xmm1,dword ptr [rcx+34]
       lea       rcx,[rdx+28]
       vmovss    dword ptr [rcx],xmm0
       vmovss    dword ptr [rcx+4],xmm1
       test      rax,rax
       je        near ptr M00_L04
       mov       ecx,[rax+8]
       cmp       ecx,4
       jl        near ptr M00_L05
       xor       r8d,r8d
       cmp       r8d,4
       jge       near ptr M00_L03
M00_L00:
       mov       r10d,r8d
       mov       r9,[rdx+r10*8+10]
       mov       [rsp+90],r9
       xor       r9d,r9d
       cmp       r9d,r8d
       jge       near ptr M00_L02
M00_L01:
       cmp       r9d,ecx
       jae       near ptr M00_L06
       lea       r11,[rax+r9*8+10]
       mov       rbx,[r11]
       mov       [rsp+80],rbx
       vmovsd    xmm0,qword ptr [rsp+80]
       vinsertps xmm0,xmm0,xmm0,3C
       vmovsd    xmm1,qword ptr [rsp+90]
       vinsertps xmm1,xmm1,xmm1,3C
       vdpps     xmm0,xmm0,xmm1,0FF
       mov       rbx,[r11]
       mov       [rsp+78],rbx
       mov       rbx,[r11]
       mov       [rsp+70],rbx
       vmovsd    xmm1,qword ptr [rsp+78]
       vinsertps xmm1,xmm1,xmm1,3C
       vmovsd    xmm2,qword ptr [rsp+70]
       vinsertps xmm2,xmm2,xmm2,3C
       vdpps     xmm1,xmm1,xmm2,0FF
       vdivss    xmm0,xmm0,xmm1
       mov       r11,[r11]
       mov       [rsp+68],r11
       vmovsd    xmm1,qword ptr [rsp+68]
       vbroadcastss xmm0,xmm0
       vmulps    xmm0,xmm0,xmm1
       vmovsd    qword ptr [rsp+60],xmm0
       mov       r11,[rsp+60]
       mov       [rsp+88],r11
       vmovsd    xmm0,qword ptr [rsp+90]
       vmovsd    xmm1,qword ptr [rsp+88]
       vsubps    xmm0,xmm0,xmm1
       vmovsd    qword ptr [rsp+58],xmm0
       mov       r11,[rsp+58]
       mov       [rsp+90],r11
       inc       r9d
       cmp       r9d,r8d
       jl        near ptr M00_L01
M00_L02:
       mov       r9,[rsp+90]
       mov       [rax+r10*8+10],r9
       inc       r8d
       cmp       r8d,4
       jl        near ptr M00_L00
M00_L03:
       add       rsp,98
       pop       rbx
       pop       rsi
       ret
M00_L04:
       mov       ecx,611
       mov       rdx,7FFC5B3A36E0
       call      qword ptr [7FFC5B2E7798]
       mov       rcx,rax
       call      qword ptr [7FFC5B5876C0]
       int       3
M00_L05:
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
       call      qword ptr [7FFC5B4EE5B0]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
M00_L06:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 608
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Orthonormalize()
       push      rsi
       push      rbx
       sub       rsp,0A8
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
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       lea       r8,[rdx+10]
       vmovss    dword ptr [r8],xmm0
       vmovss    dword ptr [r8+4],xmm1
       vmovss    xmm0,dword ptr [rcx+20]
       vmovss    xmm1,dword ptr [rcx+24]
       lea       r8,[rdx+18]
       vmovss    dword ptr [r8],xmm0
       vmovss    dword ptr [r8+4],xmm1
       vmovss    xmm0,dword ptr [rcx+28]
       vmovss    xmm1,dword ptr [rcx+2C]
       lea       r8,[rdx+20]
       vmovss    dword ptr [r8],xmm0
       vmovss    dword ptr [r8+4],xmm1
       vmovss    xmm0,dword ptr [rcx+30]
       vmovss    xmm1,dword ptr [rcx+34]
       lea       rcx,[rdx+28]
       vmovss    dword ptr [rcx],xmm0
       vmovss    dword ptr [rcx+4],xmm1
       test      rax,rax
       je        near ptr M00_L06
       mov       ecx,[rax+8]
       cmp       ecx,4
       jl        near ptr M00_L07
       xor       r8d,r8d
       cmp       r8d,4
       jl        near ptr M00_L03
M00_L00:
       add       rsp,0A8
       pop       rbx
       pop       rsi
       ret
M00_L01:
       vmovss    dword ptr [rsp+0A0],xmm0
       vmovss    dword ptr [rsp+0A4],xmm1
       cmp       r9d,ecx
       jae       near ptr M00_L08
       mov       r11d,r9d
       lea       r11,[rax+r11*8+10]
       mov       rbx,[r11]
       mov       [rsp+90],rbx
       vmovss    dword ptr [rsp+88],xmm0
       vmovss    dword ptr [rsp+8C],xmm1
       vmovsd    xmm2,qword ptr [rsp+90]
       vmovsd    xmm3,qword ptr [rsp+88]
       mov       r11,[r11]
       mov       [rsp+80],r11
       vmovaps   xmm0,xmm2
       vinsertps xmm1,xmm0,xmm0,3C
       vmovaps   xmm0,xmm3
       vinsertps xmm0,xmm0,xmm0,3C
       vdpps     xmm0,xmm1,xmm0,0FF
       vmovsd    xmm1,qword ptr [rsp+80]
       vmulps    xmm0,xmm0,xmm1
       vmovsd    qword ptr [rsp+78],xmm0
       mov       r11,[rsp+78]
       mov       [rsp+98],r11
       vmovsd    xmm0,qword ptr [rsp+0A0]
       vmovsd    xmm1,qword ptr [rsp+98]
       vsubps    xmm0,xmm0,xmm1
       vmovsd    qword ptr [rsp+70],xmm0
       vmovss    xmm0,dword ptr [rsp+70]
       vmovss    xmm1,dword ptr [rsp+74]
       inc       r9d
       cmp       r9d,r8d
       jl        near ptr M00_L01
       jmp       near ptr M00_L05
M00_L02:
       lea       r10,[rax+r10*8+10]
       vmovss    dword ptr [r10],xmm0
       vmovss    dword ptr [r10+4],xmm1
       inc       r8d
       cmp       r8d,4
       jge       near ptr M00_L00
M00_L03:
       mov       r10d,r8d
       lea       r9,[rdx+r10*8+10]
       vmovss    xmm0,dword ptr [r9]
       vmovss    xmm1,dword ptr [r9+4]
       xor       r9d,r9d
       test      r8d,r8d
       jle       near ptr M00_L05
       cmp       ecx,r8d
       jl        near ptr M00_L01
       lea       r9,[rax+10]
       mov       r11d,r8d
M00_L04:
       vmovss    dword ptr [rsp+0A0],xmm0
       vmovss    dword ptr [rsp+0A4],xmm1
       mov       rbx,[r9]
       mov       [rsp+90],rbx
       vmovss    dword ptr [rsp+88],xmm0
       vmovss    dword ptr [rsp+8C],xmm1
       vmovsd    xmm2,qword ptr [rsp+90]
       vmovsd    xmm3,qword ptr [rsp+88]
       mov       rbx,[r9]
       mov       [rsp+80],rbx
       vinsertps xmm0,xmm2,xmm2,3C
       vinsertps xmm1,xmm3,xmm3,3C
       vdpps     xmm0,xmm0,xmm1,0FF
       vmovsd    xmm1,qword ptr [rsp+80]
       vmulps    xmm0,xmm0,xmm1
       vmovsd    qword ptr [rsp+78],xmm0
       mov       rbx,[rsp+78]
       mov       [rsp+98],rbx
       vmovsd    xmm0,qword ptr [rsp+0A0]
       vmovsd    xmm1,qword ptr [rsp+98]
       vsubps    xmm0,xmm0,xmm1
       vmovsd    qword ptr [rsp+70],xmm0
       vmovss    xmm0,dword ptr [rsp+70]
       vmovss    xmm1,dword ptr [rsp+74]
       add       r9,8
       dec       r11d
       jne       near ptr M00_L04
M00_L05:
       vmovss    dword ptr [rsp+68],xmm0
       vmovss    dword ptr [rsp+6C],xmm1
       vmovsd    xmm2,qword ptr [rsp+68]
       vinsertps xmm2,xmm2,xmm2,3C
       vdpps     xmm2,xmm2,xmm2,0FF
       vsqrtss   xmm2,xmm2,xmm2
       vucomiss  xmm2,dword ptr [7FFC5B144CE0]
       jbe       near ptr M00_L02
       vmovss    dword ptr [rsp+60],xmm0
       vmovss    dword ptr [rsp+64],xmm1
       vmovsd    xmm0,qword ptr [rsp+60]
       vbroadcastss xmm1,xmm2
       vdivps    xmm0,xmm0,xmm1
       vmovsd    qword ptr [rsp+58],xmm0
       vmovss    xmm0,dword ptr [rsp+58]
       vmovss    xmm1,dword ptr [rsp+5C]
       jmp       near ptr M00_L02
M00_L06:
       mov       ecx,611
       mov       rdx,7FFC5B3A36E0
       call      qword ptr [7FFC5B2E7798]
       mov       rcx,rax
       call      qword ptr [7FFC5B587690]
       int       3
M00_L07:
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
       call      qword ptr [7FFC5B4EE5B0]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
M00_L08:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 894
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Transform()
       sub       rsp,38
       vmovaps   [rsp+20],xmm6
       vmovaps   [rsp+10],xmm7
       mov       rax,[rcx+18]
       mov       [rsp+8],rax
       vmovsd    xmm0,qword ptr [rsp+8]
       vinsertps xmm0,xmm0,dword ptr [7FFC5B154FA0],34
       vmovups   xmm1,[7FFC5B154FB0]
       vpermilps xmm2,xmm1,4E
       vmovshdup xmm3,xmm0
       vbroadcastss xmm3,xmm3
       vmulps    xmm2,xmm3,xmm2
       vmovups   xmm3,[7FFC5B154FC0]
       vpermilps xmm4,xmm1,1B
       vmovaps   xmm5,xmm0
       vbroadcastss xmm5,xmm5
       vmulps    xmm4,xmm5,xmm4
       vmovddup  xmm5,qword ptr [7FFC5B154FD0]
       vshufps   xmm6,xmm0,xmm0,0FF
       vbroadcastss xmm6,xmm6
       vmulps    xmm6,xmm6,xmm1
       vfmadd213ps xmm4,xmm5,xmm6
       vfmadd213ps xmm2,xmm3,xmm4
       vpermilps xmm1,xmm1,0B1
       vunpckhps xmm0,xmm0,xmm0
       vbroadcastss xmm0,xmm0
       vmulps    xmm0,xmm0,xmm1
       vmovups   xmm1,[7FFC5B154FE0]
       vfmadd231ps xmm2,xmm1,xmm0
       vpermilps xmm0,xmm2,4E
       vbroadcastss xmm4,dword ptr [7FFC5B154FA0]
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
       vmovaps   xmm6,[rsp+20]
       vmovaps   xmm7,[rsp+10]
       add       rsp,38
       ret
; Total bytes of code 239
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Operator_Add()
       sub       rsp,18
       mov       rax,[rcx+18]
       mov       [rsp+10],rax
       mov       rax,[rcx+20]
       mov       [rsp+8],rax
       vmovsd    xmm0,qword ptr [rsp+10]
       vmovsd    xmm1,qword ptr [rsp+8]
       vaddps    xmm0,xmm1,xmm0
       vmovq     rax,xmm0
       add       rsp,18
       ret
; Total bytes of code 48
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Operator_Subtract()
       sub       rsp,18
       mov       rax,[rcx+18]
       mov       [rsp+10],rax
       mov       rax,[rcx+20]
       mov       [rsp+8],rax
       vmovsd    xmm0,qword ptr [rsp+10]
       vmovsd    xmm1,qword ptr [rsp+8]
       vsubps    xmm0,xmm0,xmm1
       vmovq     rax,xmm0
       add       rsp,18
       ret
; Total bytes of code 48
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Operator_Negate()
       push      rax
       mov       rax,[rcx+18]
       mov       [rsp],rax
       vmovsd    xmm0,qword ptr [rsp]
       vxorps    xmm0,xmm0,[7FFC5B173E30]
       vmovq     rax,xmm0
       add       rsp,8
       ret
; Total bytes of code 32
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Operator_Multiply()
       sub       rsp,18
       mov       rax,[rcx+18]
       mov       [rsp+10],rax
       mov       rax,[rcx+20]
       mov       [rsp+8],rax
       vmovsd    xmm0,qword ptr [rsp+10]
       vmovsd    xmm1,qword ptr [rsp+8]
       vmulps    xmm0,xmm1,xmm0
       vmovq     rax,xmm0
       add       rsp,18
       ret
; Total bytes of code 48
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Operator_Divide()
       sub       rsp,18
       mov       rax,[rcx+18]
       mov       [rsp+10],rax
       mov       rax,[rcx+20]
       mov       [rsp+8],rax
       vmovsd    xmm0,qword ptr [rsp+10]
       vmovsd    xmm1,qword ptr [rsp+8]
       vdivps    xmm0,xmm0,xmm1
       vmovq     rax,xmm0
       add       rsp,18
       ret
; Total bytes of code 48
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Operator_Equals()
       sub       rsp,38
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       mov       rax,[rcx+20]
       mov       [rsp+30],rax
       vmovss    dword ptr [rsp+18],xmm0
       vmovss    dword ptr [rsp+1C],xmm1
       vmovsd    xmm0,qword ptr [rsp+18]
       vmovsd    xmm1,qword ptr [rsp+30]
       vsubps    xmm0,xmm0,xmm1
       vmovsd    qword ptr [rsp+10],xmm0
       mov       rax,[rsp+10]
       mov       [rsp+28],rax
       vmovsd    xmm0,qword ptr [rsp+28]
       vandps    xmm0,xmm0,[7FFC5B174160]
       vmovsd    qword ptr [rsp+8],xmm0
       mov       rax,[rsp+8]
       mov       [rsp+20],rax
       vmovsd    xmm0,qword ptr [rsp+20]
       vcmpltps  xmm0,xmm0,[7FFC5B174170]
       vinsertps xmm0,xmm0,xmm0,3C
       vpcmpeqd  xmm0,xmm0,[7FFC5B174180]
       vpmovmskb eax,xmm0
       cmp       eax,0FFFF
       sete      al
       movzx     eax,al
       add       rsp,38
       ret
; Total bytes of code 146
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Operator_NotEquals()
       sub       rsp,38
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       mov       rax,[rcx+20]
       mov       [rsp+30],rax
       vmovss    dword ptr [rsp+18],xmm0
       vmovss    dword ptr [rsp+1C],xmm1
       vmovsd    xmm0,qword ptr [rsp+18]
       vmovsd    xmm1,qword ptr [rsp+30]
       vsubps    xmm0,xmm0,xmm1
       vmovsd    qword ptr [rsp+10],xmm0
       mov       rax,[rsp+10]
       mov       [rsp+28],rax
       vmovsd    xmm0,qword ptr [rsp+28]
       vandps    xmm0,xmm0,[7FFC5B134220]
       vmovsd    qword ptr [rsp+8],xmm0
       mov       rax,[rsp+8]
       mov       [rsp+20],rax
       vmovsd    xmm0,qword ptr [rsp+20]
       vcmpltps  xmm0,xmm0,[7FFC5B134230]
       vinsertps xmm0,xmm0,xmm0,3C
       vpcmpeqd  xmm0,xmm0,[7FFC5B134240]
       vpmovmskb eax,xmm0
       cmp       eax,0FFFF
       setne     al
       movzx     eax,al
       add       rsp,38
       ret
; Total bytes of code 146
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.EqualsStrict()
       sub       rsp,18
       lea       rax,[rcx+18]
       mov       rcx,[rcx+20]
       mov       [rsp+10],rcx
       mov       rax,[rax]
       mov       [rsp+8],rax
       vmovsd    xmm0,qword ptr [rsp+8]
       vmovsd    xmm1,qword ptr [rsp+10]
       vcmpeqps  xmm0,xmm1,xmm0
       vinsertps xmm0,xmm0,xmm0,3C
       vpcmpeqd  xmm0,xmm0,[7FFC5B143D70]
       vpmovmskb eax,xmm0
       cmp       eax,0FFFF
       sete      al
       movzx     eax,al
       add       rsp,18
       ret
; Total bytes of code 76
```

