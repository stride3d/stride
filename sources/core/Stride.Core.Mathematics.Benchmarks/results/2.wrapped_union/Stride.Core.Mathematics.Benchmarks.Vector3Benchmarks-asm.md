## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.IsNormalized()
       add       rcx,18
       vmovsd    xmm0,qword ptr [rcx]
       vinsertps xmm0,xmm0,dword ptr [rcx+8],28
       vinsertps xmm0,xmm0,xmm0,38
       vdpps     xmm0,xmm0,xmm0,0FF
       vsubss    xmm0,xmm0,dword ptr [7FFC5B144050]
       vandps    xmm0,xmm0,[7FFC5B144060]
       vmovss    xmm1,dword ptr [7FFC5B144070]
       vucomiss  xmm1,xmm0
       seta      al
       movzx     eax,al
       ret
; Total bytes of code 62
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Length()
       add       rcx,18
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
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.LengthSquared()
       add       rcx,18
       vmovsd    xmm0,qword ptr [rcx]
       vinsertps xmm0,xmm0,dword ptr [rcx+8],28
       vinsertps xmm0,xmm0,xmm0,38
       vdpps     xmm0,xmm0,xmm0,0FF
       ret
; Total bytes of code 28
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Normalize()
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,dword ptr [rcx+20],28
       vmovaps   xmm1,xmm0
       vinsertps xmm2,xmm1,xmm1,38
       vdpps     xmm2,xmm2,xmm2,0FF
       vsqrtss   xmm2,xmm2,xmm2
       vucomiss  xmm2,dword ptr [7FFC5B174548]
       jbe       short M00_L00
       vbroadcastss xmm0,xmm2
       vdivps    xmm0,xmm1,xmm0
M00_L00:
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       ret
; Total bytes of code 66
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.MoveTo()
       push      rsi
       push      rbx
       sub       rsp,68
       vmovaps   [rsp+50],xmm6
       mov       rbx,rdx
       lea       rsi,[rcx+18]
       lea       rax,[rcx+24]
       vmovss    xmm1,dword ptr [rcx+10]
       xor       ecx,ecx
       mov       [rsp+30],rcx
       mov       [rsp+38],ecx
       vmovsd    xmm0,qword ptr [rax]
       vinsertps xmm0,xmm0,dword ptr [rax+8],28
       vmovsd    xmm2,qword ptr [rsi]
       vinsertps xmm2,xmm2,dword ptr [rsi+8],28
       vsubps    xmm6,xmm0,xmm2
       vmovaps   xmm0,xmm6
       vinsertps xmm0,xmm0,xmm0,38
       vdpps     xmm0,xmm0,xmm0,0FF
       vsqrtss   xmm0,xmm0,xmm0
       vucomiss  xmm1,xmm0
       jb        short M00_L02
M00_L00:
       mov       rcx,[rax]
       mov       [rsp+40],rcx
       mov       ecx,[rax+8]
       mov       [rsp+48],ecx
M00_L01:
       mov       rax,[rsp+40]
       mov       [rbx],rax
       mov       eax,[rsp+48]
       mov       [rbx+8],eax
       mov       rax,rbx
       vmovaps   xmm6,[rsp+50]
       add       rsp,68
       pop       rbx
       pop       rsi
       ret
M00_L02:
       vxorps    xmm2,xmm2,xmm2
       vucomiss  xmm0,xmm2
       jp        short M00_L03
       je        short M00_L00
M00_L03:
       vdivss    xmm1,xmm1,xmm0
       lea       rcx,[rsp+30]
       call      qword ptr [7FFC5B595230]
       vmovsd    xmm0,qword ptr [rsp+30]
       vinsertps xmm0,xmm0,dword ptr [rsp+38],28
       vmovsd    xmm1,qword ptr [rsi]
       vinsertps xmm1,xmm1,dword ptr [rsi+8],28
       vfmadd231ps xmm1,xmm0,xmm6
       vmovaps   [rsp+20],xmm1
       lea       rdx,[rsp+20]
       lea       rcx,[rsp+40]
       call      qword ptr [7FFC5B595278]; Stride.Core.Mathematics.Vector3.op_Implicit(System.Numerics.Vector3)
       jmp       short M00_L01
; Total bytes of code 218
```
```assembly
; Stride.Core.Mathematics.Vector3.op_Implicit(System.Numerics.Vector3)
       vmovsd    xmm0,qword ptr [rdx]
       vinsertps xmm0,xmm0,dword ptr [rdx+8],28
       vmovsd    qword ptr [rcx],xmm0
       vextractps dword ptr [rcx+8],xmm0,2
       mov       rax,rcx
       ret
; Total bytes of code 26
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Add()
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,dword ptr [rcx+20],28
       vmovsd    xmm1,qword ptr [rcx+24]
       vinsertps xmm1,xmm1,dword ptr [rcx+2C],28
       vaddps    xmm0,xmm1,xmm0
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       ret
; Total bytes of code 43
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Subtract()
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,dword ptr [rcx+20],28
       vmovsd    xmm1,qword ptr [rcx+24]
       vinsertps xmm1,xmm1,dword ptr [rcx+2C],28
       vsubps    xmm0,xmm0,xmm1
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       ret
; Total bytes of code 43
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Multiply()
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,dword ptr [rcx+20],28
       vbroadcastss xmm1,dword ptr [rcx+10]
       vmulps    xmm0,xmm1,xmm0
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       ret
; Total bytes of code 37
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Modulate()
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,dword ptr [rcx+20],28
       vmovsd    xmm1,qword ptr [rcx+24]
       vinsertps xmm1,xmm1,dword ptr [rcx+2C],28
       vmulps    xmm0,xmm1,xmm0
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       ret
; Total bytes of code 43
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Divide()
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,dword ptr [rcx+20],28
       vbroadcastss xmm1,dword ptr [rcx+10]
       vdivps    xmm0,xmm0,xmm1
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       ret
; Total bytes of code 37
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Demodulate()
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,dword ptr [rcx+20],28
       vmovsd    xmm1,qword ptr [rcx+24]
       vinsertps xmm1,xmm1,dword ptr [rcx+2C],28
       vdivps    xmm0,xmm0,xmm1
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       ret
; Total bytes of code 43
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Negate()
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,dword ptr [rcx+20],28
       vxorps    xmm0,xmm0,[7FFC5B164330]
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       ret
; Total bytes of code 35
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Barycentric()
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,dword ptr [rcx+20],28
       vmovsd    xmm1,qword ptr [rcx+24]
       vinsertps xmm1,xmm1,dword ptr [rcx+2C],28
       vmovsd    xmm2,qword ptr [rcx+30]
       vinsertps xmm2,xmm2,dword ptr [rcx+38],28
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
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       ret
; Total bytes of code 95
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Clamp()
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,dword ptr [rcx+20],28
       vmovsd    xmm1,qword ptr [rcx+24]
       vinsertps xmm1,xmm1,dword ptr [rcx+2C],28
       vmovsd    xmm2,qword ptr [rcx+30]
       vinsertps xmm2,xmm2,dword ptr [rcx+38],28
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
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       ret
; Total bytes of code 133
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Distance()
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,dword ptr [rcx+20],28
       vmovsd    xmm1,qword ptr [rcx+24]
       vinsertps xmm1,xmm1,dword ptr [rcx+2C],28
       vsubps    xmm0,xmm0,xmm1
       vinsertps xmm0,xmm0,xmm0,38
       vdpps     xmm0,xmm0,xmm0,0FF
       vsqrtss   xmm0,xmm0,xmm0
       ret
; Total bytes of code 45
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.DistanceSquared()
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,dword ptr [rcx+20],28
       vmovsd    xmm1,qword ptr [rcx+24]
       vinsertps xmm1,xmm1,dword ptr [rcx+2C],28
       vsubps    xmm0,xmm0,xmm1
       vinsertps xmm0,xmm0,xmm0,38
       vdpps     xmm0,xmm0,xmm0,0FF
       ret
; Total bytes of code 41
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Dot()
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,dword ptr [rcx+20],28
       vinsertps xmm0,xmm0,xmm0,38
       vmovsd    xmm1,qword ptr [rcx+24]
       vinsertps xmm1,xmm1,dword ptr [rcx+2C],28
       vinsertps xmm1,xmm1,xmm1,38
       vdpps     xmm0,xmm0,xmm1,0FF
       ret
; Total bytes of code 43
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Lerp()
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,dword ptr [rcx+20],28
       vmovsd    xmm1,qword ptr [rcx+24]
       vinsertps xmm1,xmm1,dword ptr [rcx+2C],28
       vmovss    xmm2,dword ptr [rcx+10]
       vbroadcastss xmm2,xmm2
       vbroadcastss xmm3,dword ptr [7FFC5B164498]
       vsubps    xmm3,xmm3,xmm2
       vmulps    xmm1,xmm2,xmm1
       vfmadd213ps xmm0,xmm3,xmm1
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       ret
; Total bytes of code 71
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.SmoothStep()
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,dword ptr [rcx+20],28
       vmovsd    xmm1,qword ptr [rcx+24]
       vinsertps xmm1,xmm1,dword ptr [rcx+2C],28
       vmovss    xmm2,dword ptr [rcx+10]
       vxorps    xmm3,xmm3,xmm3
       vcmpneqps xmm4,xmm2,xmm2
       vorps     xmm3,xmm4,xmm3
       vxorps    xmm4,xmm4,xmm4
       vcmpgtps  xmm4,xmm2,xmm4
       vorps     xmm3,xmm4,xmm3
       vandps    xmm2,xmm3,xmm2
       vbroadcastss xmm3,dword ptr [7FFC5B154738]
       vminss    xmm2,xmm3,xmm2
       vmulss    xmm4,xmm2,xmm2
       vaddss    xmm2,xmm2,xmm2
       vmovss    xmm5,dword ptr [7FFC5B15473C]
       vsubss    xmm2,xmm5,xmm2
       vmulss    xmm2,xmm4,xmm2
       vbroadcastss xmm2,xmm2
       vsubps    xmm3,xmm3,xmm2
       vmulps    xmm1,xmm2,xmm1
       vfmadd213ps xmm0,xmm3,xmm1
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       ret
; Total bytes of code 129
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Hermite()
       sub       rsp,38
       vmovaps   [rsp+20],xmm6
       vmovaps   [rsp+10],xmm7
       vmovaps   [rsp],xmm8
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,dword ptr [rcx+20],28
       vmovsd    xmm1,qword ptr [rcx+24]
       vinsertps xmm1,xmm1,dword ptr [rcx+2C],28
       vmovsd    xmm2,qword ptr [rcx+30]
       vinsertps xmm2,xmm2,dword ptr [rcx+38],28
       vmovsd    xmm3,qword ptr [rcx+3C]
       vinsertps xmm3,xmm3,dword ptr [rcx+44],28
       vmovss    xmm4,dword ptr [rcx+10]
       vmulss    xmm5,xmm4,xmm4
       vmulss    xmm6,xmm5,xmm4
       vaddss    xmm7,xmm5,xmm5
       vsubss    xmm7,xmm6,xmm7
       vaddss    xmm4,xmm7,xmm4
       vmulss    xmm7,xmm6,dword ptr [7FFC5B154A70]
       vmulss    xmm8,xmm5,dword ptr [7FFC5B154A74]
       vaddss    xmm7,xmm7,xmm8
       vsubss    xmm5,xmm6,xmm5
       vaddss    xmm6,xmm6,xmm6
       vsubss    xmm6,xmm6,xmm8
       vaddss    xmm6,xmm6,dword ptr [7FFC5B154A78]
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
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       vmovaps   xmm6,[rsp+20]
       vmovaps   xmm7,[rsp+10]
       vmovaps   xmm8,[rsp]
       add       rsp,38
       ret
; Total bytes of code 220
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.CatmullRom()
       sub       rsp,58
       vmovaps   [rsp+40],xmm6
       vmovaps   [rsp+30],xmm7
       vmovaps   [rsp+20],xmm8
       vmovaps   [rsp+10],xmm9
       vmovaps   [rsp],xmm10
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,dword ptr [rcx+20],28
       vmovsd    xmm1,qword ptr [rcx+24]
       vinsertps xmm1,xmm1,dword ptr [rcx+2C],28
       vmovsd    xmm2,qword ptr [rcx+30]
       vinsertps xmm2,xmm2,dword ptr [rcx+38],28
       vmovsd    xmm3,qword ptr [rcx+3C]
       vinsertps xmm3,xmm3,dword ptr [rcx+44],28
       vmovss    xmm4,dword ptr [rcx+10]
       vmulss    xmm5,xmm4,xmm4
       vmulss    xmm6,xmm5,xmm4
       vbroadcastss xmm7,dword ptr [7FFC5B1552E0]
       vmulps    xmm8,xmm7,xmm1
       vmovaps   xmm9,xmm0
       vxorps    xmm9,xmm9,[7FFC5B1552F0]
       vmovaps   xmm10,xmm9
       vaddps    xmm10,xmm10,xmm2
       vbroadcastss xmm4,xmm4
       vmulps    xmm4,xmm4,xmm10
       vaddps    xmm4,xmm4,xmm8
       vmulps    xmm0,xmm0,xmm7
       vmulps    xmm7,xmm1,[7FFC5B155300]
       vsubps    xmm0,xmm0,xmm7
       vmulps    xmm7,xmm2,[7FFC5B155310]
       vaddps    xmm0,xmm7,xmm0
       vmovaps   xmm7,xmm3
       vsubps    xmm0,xmm0,xmm7
       vbroadcastss xmm5,xmm5
       vmulps    xmm0,xmm5,xmm0
       vaddps    xmm0,xmm0,xmm4
       vbroadcastss xmm4,dword ptr [7FFC5B155320]
       vmulps    xmm1,xmm4,xmm1
       vaddps    xmm1,xmm1,xmm9
       vmulps    xmm2,xmm4,xmm2
       vsubps    xmm1,xmm1,xmm2
       vaddps    xmm1,xmm3,xmm1
       vbroadcastss xmm2,xmm6
       vmulps    xmm1,xmm2,xmm1
       vaddps    xmm0,xmm1,xmm0
       vmulps    xmm0,xmm0,[7FFC5B155330]
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       vmovaps   xmm6,[rsp+40]
       vmovaps   xmm7,[rsp+30]
       vmovaps   xmm8,[rsp+20]
       vmovaps   xmm9,[rsp+10]
       vmovaps   xmm10,[rsp]
       add       rsp,58
       ret
; Total bytes of code 291
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Max()
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,dword ptr [rcx+20],28
       vmovsd    xmm1,qword ptr [rcx+24]
       vinsertps xmm1,xmm1,dword ptr [rcx+2C],28
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
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Min()
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,dword ptr [rcx+20],28
       vmovsd    xmm1,qword ptr [rcx+24]
       vinsertps xmm1,xmm1,dword ptr [rcx+2C],28
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
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Reflect()
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,dword ptr [rcx+20],28
       vmovsd    xmm1,qword ptr [rcx+24]
       vinsertps xmm1,xmm1,dword ptr [rcx+2C],28
       vinsertps xmm2,xmm0,xmm0,38
       vinsertps xmm3,xmm1,xmm1,38
       vdpps     xmm2,xmm2,xmm3,0FF
       vaddps    xmm2,xmm2,xmm2
       vxorps    xmm2,xmm2,[7FFC5B164490]
       vfmadd213ps xmm2,xmm1,xmm0
       vmovsd    qword ptr [rdx],xmm2
       vextractps dword ptr [rdx+8],xmm2,2
       mov       rax,rdx
       ret
; Total bytes of code 74
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Orthogonalize()
       push      rsi
       push      rbx
       sub       rsp,0A8
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+30],ymm4
       vmovdqa   xmmword ptr [rsp+50],xmm4
       mov       [rsp+60],rax
       mov       rax,[rcx+8]
       mov       rdx,offset MT_Stride.Core.Mathematics.Vector3[]
       mov       [rsp+28],rdx
       lea       rdx,[rsp+28]
       mov       dword ptr [rdx+8],4
       lea       rdx,[rsp+28]
       mov       r8,[rcx+18]
       mov       [rsp+98],r8
       mov       r8d,[rcx+20]
       mov       [rsp+0A0],r8d
       mov       r8,[rsp+98]
       mov       [rdx+10],r8
       mov       r8d,[rsp+0A0]
       mov       [rdx+18],r8d
       mov       r8,[rcx+24]
       mov       [rsp+88],r8
       mov       r8d,[rcx+2C]
       mov       [rsp+90],r8d
       mov       r8,[rsp+88]
       mov       [rdx+1C],r8
       mov       r8d,[rsp+90]
       mov       [rdx+24],r8d
       mov       r8,[rcx+30]
       mov       [rsp+78],r8
       mov       r8d,[rcx+38]
       mov       [rsp+80],r8d
       mov       r8,[rsp+78]
       mov       [rdx+28],r8
       mov       r8d,[rsp+80]
       mov       [rdx+30],r8d
       mov       r8,[rcx+3C]
       mov       [rsp+68],r8
       mov       r8d,[rcx+44]
       mov       [rsp+70],r8d
       mov       rcx,[rsp+68]
       mov       [rdx+34],rcx
       mov       ecx,[rsp+70]
       mov       [rdx+3C],ecx
       test      rax,rax
       je        near ptr M00_L05
       mov       ecx,[rax+8]
       cmp       ecx,4
       jl        near ptr M00_L06
       xor       r8d,r8d
       cmp       r8d,4
       jl        short M00_L03
M00_L00:
       add       rsp,0A8
       pop       rbx
       pop       rsi
       ret
M00_L01:
       cmp       r9d,ecx
       jae       near ptr M00_L07
       mov       r11d,r9d
       lea       r11,[r11+r11*2]
       vmovsd    xmm1,qword ptr [rax+r11*4+10]
       vinsertps xmm1,xmm1,dword ptr [rax+r11*4+18],28
       vinsertps xmm2,xmm1,xmm1,38
       vmovaps   xmm3,xmm0
       vinsertps xmm3,xmm3,xmm3,38
       vdpps     xmm3,xmm2,xmm3,0FF
       vdpps     xmm2,xmm2,xmm2,0FF
       vdivss    xmm2,xmm3,xmm2
       vbroadcastss xmm2,xmm2
       vmulps    xmm1,xmm2,xmm1
       vsubps    xmm0,xmm0,xmm1
       inc       r9d
       cmp       r9d,r8d
       jl        short M00_L01
M00_L02:
       vmovsd    qword ptr [rax+r10*4+10],xmm0
       vextractps dword ptr [rax+r10*4+18],xmm0,2
       inc       r8d
       cmp       r8d,4
       jge       short M00_L00
M00_L03:
       lea       r10,[r8+r8*2]
       vmovsd    xmm0,qword ptr [rdx+r10*4+10]
       vinsertps xmm0,xmm0,dword ptr [rdx+r10*4+18],28
       xor       r9d,r9d
       test      r8d,r8d
       jle       short M00_L02
       cmp       ecx,r8d
       jl        near ptr M00_L01
M00_L04:
       mov       r11d,r9d
       lea       r11,[r11+r11*2]
       vmovsd    xmm1,qword ptr [rax+r11*4+10]
       vinsertps xmm1,xmm1,dword ptr [rax+r11*4+18],28
       vinsertps xmm2,xmm1,xmm1,38
       vinsertps xmm3,xmm0,xmm0,38
       vdpps     xmm3,xmm2,xmm3,0FF
       vdpps     xmm2,xmm2,xmm2,0FF
       vdivss    xmm2,xmm3,xmm2
       vbroadcastss xmm2,xmm2
       vmulps    xmm1,xmm2,xmm1
       vsubps    xmm0,xmm0,xmm1
       inc       r9d
       cmp       r9d,r8d
       jl        short M00_L04
       jmp       near ptr M00_L02
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
       call      qword ptr [7FFC5B51E550]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
M00_L07:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 629
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Orthonormalize()
       push      rsi
       push      rbx
       sub       rsp,0A8
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+30],ymm4
       vmovdqa   xmmword ptr [rsp+50],xmm4
       mov       [rsp+60],rax
       mov       rax,[rcx+8]
       mov       rdx,offset MT_Stride.Core.Mathematics.Vector3[]
       mov       [rsp+28],rdx
       lea       rdx,[rsp+28]
       mov       dword ptr [rdx+8],4
       lea       rdx,[rsp+28]
       mov       r8,[rcx+18]
       mov       [rsp+98],r8
       mov       r8d,[rcx+20]
       mov       [rsp+0A0],r8d
       mov       r8,[rsp+98]
       mov       [rdx+10],r8
       mov       r8d,[rsp+0A0]
       mov       [rdx+18],r8d
       mov       r8,[rcx+24]
       mov       [rsp+88],r8
       mov       r8d,[rcx+2C]
       mov       [rsp+90],r8d
       mov       r8,[rsp+88]
       mov       [rdx+1C],r8
       mov       r8d,[rsp+90]
       mov       [rdx+24],r8d
       mov       r8,[rcx+30]
       mov       [rsp+78],r8
       mov       r8d,[rcx+38]
       mov       [rsp+80],r8d
       mov       r8,[rsp+78]
       mov       [rdx+28],r8
       mov       r8d,[rsp+80]
       mov       [rdx+30],r8d
       mov       r8,[rcx+3C]
       mov       [rsp+68],r8
       mov       r8d,[rcx+44]
       mov       [rsp+70],r8d
       mov       rcx,[rsp+68]
       mov       [rdx+34],rcx
       mov       ecx,[rsp+70]
       mov       [rdx+3C],ecx
       test      rax,rax
       je        near ptr M00_L06
       mov       ecx,[rax+8]
       cmp       ecx,4
       jl        near ptr M00_L07
       xor       r8d,r8d
       cmp       r8d,4
       jl        near ptr M00_L04
M00_L00:
       add       rsp,0A8
       pop       rbx
       pop       rsi
       ret
M00_L01:
       cmp       r9d,ecx
       jae       near ptr M00_L08
       mov       r11d,r9d
       lea       r11,[r11+r11*2]
       vmovsd    xmm1,qword ptr [rax+r11*4+10]
       vinsertps xmm1,xmm1,dword ptr [rax+r11*4+18],28
       vinsertps xmm2,xmm1,xmm1,38
       vmovaps   xmm3,xmm0
       vinsertps xmm3,xmm3,xmm3,38
       vdpps     xmm2,xmm2,xmm3,0FF
       vmulps    xmm1,xmm2,xmm1
       vsubps    xmm0,xmm0,xmm1
       inc       r9d
       cmp       r9d,r8d
       jl        short M00_L01
M00_L02:
       vmovaps   xmm1,xmm0
       vinsertps xmm1,xmm1,xmm1,38
       vdpps     xmm1,xmm1,xmm1,0FF
       vsqrtss   xmm1,xmm1,xmm1
       vucomiss  xmm1,dword ptr [7FFC5B165130]
       jbe       short M00_L03
       vbroadcastss xmm1,xmm1
       vdivps    xmm0,xmm0,xmm1
M00_L03:
       vmovsd    qword ptr [rax+r10*4+10],xmm0
       vextractps dword ptr [rax+r10*4+18],xmm0,2
       inc       r8d
       cmp       r8d,4
       jge       near ptr M00_L00
M00_L04:
       lea       r10,[r8+r8*2]
       vmovsd    xmm0,qword ptr [rdx+r10*4+10]
       vinsertps xmm0,xmm0,dword ptr [rdx+r10*4+18],28
       xor       r9d,r9d
       test      r8d,r8d
       jle       short M00_L02
       cmp       ecx,r8d
       jl        near ptr M00_L01
       nop       dword ptr [rax]
M00_L05:
       mov       r11d,r9d
       lea       r11,[r11+r11*2]
       vmovsd    xmm1,qword ptr [rax+r11*4+10]
       vinsertps xmm1,xmm1,dword ptr [rax+r11*4+18],28
       vinsertps xmm2,xmm1,xmm1,38
       vinsertps xmm3,xmm0,xmm0,38
       vdpps     xmm2,xmm2,xmm3,0FF
       vmulps    xmm1,xmm2,xmm1
       vsubps    xmm0,xmm0,xmm1
       inc       r9d
       cmp       r9d,r8d
       jl        short M00_L05
       jmp       near ptr M00_L02
M00_L06:
       mov       ecx,611
       mov       rdx,7FFC5B3C36E0
       call      qword ptr [7FFC5B307798]
       mov       rcx,rax
       call      qword ptr [7FFC5B597648]
       int       3
M00_L07:
       mov       rcx,offset MT_System.ArgumentOutOfRangeException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       ecx,611
       mov       rdx,7FFC5B3C36E0
       call      qword ptr [7FFC5B307798]
       mov       rsi,rax
       mov       ecx,629
       mov       rdx,7FFC5B3C36E0
       call      qword ptr [7FFC5B307798]
       mov       r8,rax
       mov       rdx,rsi
       mov       rcx,rbx
       call      qword ptr [7FFC5B50E550]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
M00_L08:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 649
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Transform()
       sub       rsp,28
       vmovaps   [rsp+10],xmm6
       vmovaps   [rsp],xmm7
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,dword ptr [rcx+20],28
       vinsertps xmm0,xmm0,dword ptr [7FFC5B1454F0],30
       vmovups   xmm1,[7FFC5B145500]
       vpermilps xmm2,xmm1,4E
       vmovshdup xmm3,xmm0
       vbroadcastss xmm3,xmm3
       vmulps    xmm2,xmm3,xmm2
       vmovups   xmm3,[7FFC5B145510]
       vpermilps xmm4,xmm1,1B
       vmovaps   xmm5,xmm0
       vbroadcastss xmm5,xmm5
       vmulps    xmm4,xmm5,xmm4
       vmovddup  xmm5,qword ptr [7FFC5B145520]
       vshufps   xmm6,xmm0,xmm0,0FF
       vbroadcastss xmm6,xmm6
       vmulps    xmm6,xmm6,xmm1
       vfmadd213ps xmm4,xmm5,xmm6
       vfmadd213ps xmm2,xmm3,xmm4
       vpermilps xmm1,xmm1,0B1
       vunpckhps xmm0,xmm0,xmm0
       vbroadcastss xmm0,xmm0
       vmulps    xmm0,xmm0,xmm1
       vmovups   xmm1,[7FFC5B145530]
       vfmadd231ps xmm2,xmm1,xmm0
       vpermilps xmm0,xmm2,4E
       vbroadcastss xmm4,dword ptr [7FFC5B1454F0]
       vmulps    xmm0,xmm0,xmm4
       vpermilps xmm6,xmm2,1B
       vmulps    xmm6,xmm6,xmm4
       vmulps    xmm7,xmm4,xmm2
       vfmadd213ps xmm6,xmm5,xmm7
       vfmadd213ps xmm0,xmm3,xmm6
       vpermilps xmm2,xmm2,0B1
       vmulps    xmm2,xmm2,xmm4
       vfmadd213ps xmm2,xmm1,xmm0
       vmovsd    qword ptr [rdx],xmm2
       vextractps dword ptr [rdx+8],xmm2,2
       mov       rax,rdx
       vmovaps   xmm6,[rsp+10]
       vmovaps   xmm7,[rsp]
       add       rsp,28
       ret
; Total bytes of code 243
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Operator_Add()
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,dword ptr [rcx+20],28
       vmovsd    xmm1,qword ptr [rcx+24]
       vinsertps xmm1,xmm1,dword ptr [rcx+2C],28
       vaddps    xmm0,xmm1,xmm0
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       ret
; Total bytes of code 43
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Operator_Subtract()
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,dword ptr [rcx+20],28
       vmovsd    xmm1,qword ptr [rcx+24]
       vinsertps xmm1,xmm1,dword ptr [rcx+2C],28
       vsubps    xmm0,xmm0,xmm1
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       ret
; Total bytes of code 43
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Operator_Negate()
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,dword ptr [rcx+20],28
       vxorps    xmm0,xmm0,[7FFC5B1342B0]
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       ret
; Total bytes of code 35
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Operator_Multiply()
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,dword ptr [rcx+20],28
       vmovsd    xmm1,qword ptr [rcx+24]
       vinsertps xmm1,xmm1,dword ptr [rcx+2C],28
       vmulps    xmm0,xmm1,xmm0
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       ret
; Total bytes of code 43
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Operator_Divide()
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,dword ptr [rcx+20],28
       vmovsd    xmm1,qword ptr [rcx+24]
       vinsertps xmm1,xmm1,dword ptr [rcx+2C],28
       vdivps    xmm0,xmm0,xmm1
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       ret
; Total bytes of code 43
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Operator_Equals()
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,dword ptr [rcx+20],28
       vmovsd    xmm1,qword ptr [rcx+24]
       vinsertps xmm1,xmm1,dword ptr [rcx+2C],28
       vsubps    xmm0,xmm0,xmm1
       vandps    xmm0,xmm0,[7FFC5B154A50]
       vcmpltps  xmm0,xmm0,[7FFC5B154A60]
       vinsertps xmm0,xmm0,xmm0,38
       vpcmpeqd  xmm0,xmm0,[7FFC5B154A70]
       vpmovmskb eax,xmm0
       cmp       eax,0FFFF
       sete      al
       movzx     eax,al
       ret
; Total bytes of code 75
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Operator_NotEquals()
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,dword ptr [rcx+20],28
       vmovsd    xmm1,qword ptr [rcx+24]
       vinsertps xmm1,xmm1,dword ptr [rcx+2C],28
       vsubps    xmm0,xmm0,xmm1
       vandps    xmm0,xmm0,[7FFC5B154A60]
       vcmpltps  xmm0,xmm0,[7FFC5B154A70]
       vinsertps xmm0,xmm0,xmm0,38
       vpcmpeqd  xmm0,xmm0,[7FFC5B154A80]
       vpmovmskb eax,xmm0
       cmp       eax,0FFFF
       setne     al
       movzx     eax,al
       ret
; Total bytes of code 75
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.EqualsStrict()
       vmovsd    xmm0,qword ptr [rcx+18]
       vinsertps xmm0,xmm0,dword ptr [rcx+20],28
       vmovsd    xmm1,qword ptr [rcx+24]
       vinsertps xmm1,xmm1,dword ptr [rcx+2C],28
       vcmpeqps  xmm0,xmm1,xmm0
       vinsertps xmm0,xmm0,xmm0,38
       vpcmpeqd  xmm0,xmm0,[7FFC5B144180]
       vpmovmskb eax,xmm0
       cmp       eax,0FFFF
       sete      al
       movzx     eax,al
       ret
; Total bytes of code 59
```

