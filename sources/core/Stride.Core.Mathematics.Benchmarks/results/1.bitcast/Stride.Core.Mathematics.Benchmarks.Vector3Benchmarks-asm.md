## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.IsNormalized()
       sub       rsp,18
       mov       rax,[rcx+18]
       mov       [rsp+8],rax
       mov       eax,[rcx+20]
       mov       [rsp+10],eax
       vmovsd    xmm0,qword ptr [rsp+8]
       vinsertps xmm0,xmm0,dword ptr [rsp+10],28
       vinsertps xmm0,xmm0,xmm0,38
       vdpps     xmm0,xmm0,xmm0,0FF
       vsubss    xmm0,xmm0,dword ptr [7FFC5B154160]
       vandps    xmm0,xmm0,[7FFC5B154170]
       vmovss    xmm1,dword ptr [7FFC5B154180]
       vucomiss  xmm1,xmm0
       seta      al
       movzx     eax,al
       add       rsp,18
       ret
; Total bytes of code 85
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Length()
       sub       rsp,18
       mov       rax,[rcx+18]
       mov       [rsp+8],rax
       mov       eax,[rcx+20]
       mov       [rsp+10],eax
       vmovsd    xmm0,qword ptr [rsp+8]
       vinsertps xmm0,xmm0,dword ptr [rsp+10],28
       vinsertps xmm0,xmm0,xmm0,38
       vdpps     xmm0,xmm0,xmm0,0FF
       vsqrtss   xmm0,xmm0,xmm0
       add       rsp,18
       ret
; Total bytes of code 55
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.LengthSquared()
       sub       rsp,18
       mov       rax,[rcx+18]
       mov       [rsp+8],rax
       mov       eax,[rcx+20]
       mov       [rsp+10],eax
       vmovsd    xmm0,qword ptr [rsp+8]
       vinsertps xmm0,xmm0,dword ptr [rsp+10],28
       vinsertps xmm0,xmm0,xmm0,38
       vdpps     xmm0,xmm0,xmm0,0FF
       add       rsp,18
       ret
; Total bytes of code 51
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Normalize()
       sub       rsp,38
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    dword ptr [rsp+28],xmm0
       vmovss    dword ptr [rsp+2C],xmm1
       vmovss    dword ptr [rsp+30],xmm2
       vmovsd    xmm3,qword ptr [rsp+28]
       vinsertps xmm3,xmm3,dword ptr [rsp+30],28
       vinsertps xmm3,xmm3,xmm3,38
       vdpps     xmm3,xmm3,xmm3,0FF
       vsqrtss   xmm3,xmm3,xmm3
       vucomiss  xmm3,dword ptr [7FFC5B1345A8]
       jbe       short M00_L00
       vmovss    dword ptr [rsp],xmm0
       vmovss    dword ptr [rsp+4],xmm1
       vmovss    dword ptr [rsp+8],xmm2
       vmovsd    xmm0,qword ptr [rsp]
       vinsertps xmm0,xmm0,dword ptr [rsp+8],28
       vbroadcastss xmm1,xmm3
       vdivps    xmm2,xmm0,xmm1
       vmovaps   [rsp+10],xmm2
       vmovss    xmm0,dword ptr [rsp+10]
       vmovss    xmm1,dword ptr [rsp+14]
       vmovss    xmm2,dword ptr [rsp+18]
M00_L00:
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       mov       rax,rdx
       add       rsp,38
       ret
; Total bytes of code 162
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.MoveTo()
       push      rsi
       push      rbx
       sub       rsp,118
       vmovaps   [rsp+100],xmm6
       vmovaps   [rsp+0F0],xmm7
       vmovaps   [rsp+0E0],xmm8
       vmovaps   [rsp+0D0],xmm9
       vmovaps   [rsp+0C0],xmm10
       vmovaps   [rsp+0B0],xmm11
       mov       rbx,rdx
       lea       rsi,[rcx+18]
       lea       rax,[rcx+24]
       vmovss    xmm1,dword ptr [rcx+10]
       xor       ecx,ecx
       mov       [rsp+0A0],rcx
       mov       [rsp+0A8],ecx
       mov       rcx,[rax]
       mov       [rsp+70],rcx
       mov       ecx,[rax+8]
       mov       [rsp+78],ecx
       vmovsd    xmm0,qword ptr [rsp+70]
       vinsertps xmm0,xmm0,dword ptr [rsp+78],28
       mov       rcx,[rsi]
       mov       [rsp+60],rcx
       mov       ecx,[rsi+8]
       mov       [rsp+68],ecx
       vmovsd    xmm2,qword ptr [rsp+60]
       vinsertps xmm2,xmm2,dword ptr [rsp+68],28
       vsubps    xmm0,xmm0,xmm2
       vmovaps   [rsp+80],xmm0
       vmovss    xmm6,dword ptr [rsp+80]
       vmovss    xmm7,dword ptr [rsp+84]
       vmovss    xmm8,dword ptr [rsp+88]
       vmovss    dword ptr [rsp+50],xmm6
       vmovss    dword ptr [rsp+54],xmm7
       vmovss    dword ptr [rsp+58],xmm8
       vmovsd    xmm0,qword ptr [rsp+50]
       vinsertps xmm0,xmm0,dword ptr [rsp+58],28
       vinsertps xmm0,xmm0,xmm0,38
       vdpps     xmm0,xmm0,xmm0,0FF
       vsqrtss   xmm0,xmm0,xmm0
       vucomiss  xmm1,xmm0
       jb        short M00_L02
M00_L00:
       vmovss    xmm9,dword ptr [rax]
       vmovss    xmm10,dword ptr [rax+4]
       vmovss    xmm11,dword ptr [rax+8]
M00_L01:
       vmovss    dword ptr [rbx],xmm9
       vmovss    dword ptr [rbx+4],xmm10
       vmovss    dword ptr [rbx+8],xmm11
       mov       rax,rbx
       vmovaps   xmm6,[rsp+100]
       vmovaps   xmm7,[rsp+0F0]
       vmovaps   xmm8,[rsp+0E0]
       vmovaps   xmm9,[rsp+0D0]
       vmovaps   xmm10,[rsp+0C0]
       vmovaps   xmm11,[rsp+0B0]
       add       rsp,118
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
       lea       rcx,[rsp+0A0]
       call      qword ptr [7FFC5B575290]
       vmovss    dword ptr [rsp+40],xmm6
       vmovss    dword ptr [rsp+44],xmm7
       vmovss    dword ptr [rsp+48],xmm8
       vmovsd    xmm0,qword ptr [rsp+40]
       vinsertps xmm0,xmm0,dword ptr [rsp+48],28
       mov       rax,[rsp+0A0]
       mov       [rsp+30],rax
       mov       eax,[rsp+0A8]
       mov       [rsp+38],eax
       vmovsd    xmm1,qword ptr [rsp+30]
       vinsertps xmm1,xmm1,dword ptr [rsp+38],28
       mov       rax,[rsi]
       mov       [rsp+20],rax
       mov       eax,[rsi+8]
       mov       [rsp+28],eax
       vmovsd    xmm2,qword ptr [rsp+20]
       vinsertps xmm2,xmm2,dword ptr [rsp+28],28
       vfmadd231ps xmm2,xmm1,xmm0
       vmovaps   [rsp+90],xmm2
       vmovss    xmm9,dword ptr [rsp+90]
       vmovss    xmm10,dword ptr [rsp+94]
       vmovss    xmm11,dword ptr [rsp+98]
       jmp       near ptr M00_L01
; Total bytes of code 518
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Add()
       sub       rsp,28
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    dword ptr [rsp+18],xmm0
       vmovss    dword ptr [rsp+1C],xmm1
       vmovss    dword ptr [rsp+20],xmm2
       vmovsd    xmm0,qword ptr [rsp+18]
       vinsertps xmm0,xmm0,dword ptr [rsp+20],28
       vmovss    dword ptr [rsp+8],xmm3
       vmovss    dword ptr [rsp+0C],xmm4
       vmovss    dword ptr [rsp+10],xmm5
       vmovsd    xmm1,qword ptr [rsp+8]
       vinsertps xmm1,xmm1,dword ptr [rsp+10],28
       vaddps    xmm0,xmm1,xmm0
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       add       rsp,28
       ret
; Total bytes of code 121
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Subtract()
       sub       rsp,28
       lea       rax,[rcx+18]
       add       rcx,24
       mov       r8,[rax]
       mov       [rsp+18],r8
       mov       r8d,[rax+8]
       mov       [rsp+20],r8d
       vmovsd    xmm0,qword ptr [rsp+18]
       vinsertps xmm0,xmm0,dword ptr [rsp+20],28
       mov       rax,[rcx]
       mov       [rsp+8],rax
       mov       eax,[rcx+8]
       mov       [rsp+10],eax
       vmovsd    xmm1,qword ptr [rsp+8]
       vinsertps xmm1,xmm1,dword ptr [rsp+10],28
       vsubps    xmm0,xmm0,xmm1
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       add       rsp,28
       ret
; Total bytes of code 95
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Multiply()
       sub       rsp,18
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+10]
       vmovss    dword ptr [rsp+8],xmm0
       vmovss    dword ptr [rsp+0C],xmm1
       vmovss    dword ptr [rsp+10],xmm2
       vmovsd    xmm0,qword ptr [rsp+8]
       vinsertps xmm0,xmm0,dword ptr [rsp+10],28
       vbroadcastss xmm1,xmm3
       vmulps    xmm0,xmm1,xmm0
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       add       rsp,18
       ret
; Total bytes of code 84
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Modulate()
       sub       rsp,28
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    dword ptr [rsp+18],xmm0
       vmovss    dword ptr [rsp+1C],xmm1
       vmovss    dword ptr [rsp+20],xmm2
       vmovsd    xmm0,qword ptr [rsp+18]
       vinsertps xmm0,xmm0,dword ptr [rsp+20],28
       vmovss    dword ptr [rsp+8],xmm3
       vmovss    dword ptr [rsp+0C],xmm4
       vmovss    dword ptr [rsp+10],xmm5
       vmovsd    xmm1,qword ptr [rsp+8]
       vinsertps xmm1,xmm1,dword ptr [rsp+10],28
       vmulps    xmm0,xmm1,xmm0
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       add       rsp,28
       ret
; Total bytes of code 121
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Divide()
       sub       rsp,18
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+10]
       vmovss    dword ptr [rsp+8],xmm0
       vmovss    dword ptr [rsp+0C],xmm1
       vmovss    dword ptr [rsp+10],xmm2
       vmovsd    xmm0,qword ptr [rsp+8]
       vinsertps xmm0,xmm0,dword ptr [rsp+10],28
       vbroadcastss xmm1,xmm3
       vdivps    xmm0,xmm0,xmm1
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       add       rsp,18
       ret
; Total bytes of code 84
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Demodulate()
       sub       rsp,28
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    dword ptr [rsp+18],xmm0
       vmovss    dword ptr [rsp+1C],xmm1
       vmovss    dword ptr [rsp+20],xmm2
       vmovsd    xmm0,qword ptr [rsp+18]
       vinsertps xmm0,xmm0,dword ptr [rsp+20],28
       vmovss    dword ptr [rsp+8],xmm3
       vmovss    dword ptr [rsp+0C],xmm4
       vmovss    dword ptr [rsp+10],xmm5
       vmovsd    xmm1,qword ptr [rsp+8]
       vinsertps xmm1,xmm1,dword ptr [rsp+10],28
       vdivps    xmm0,xmm0,xmm1
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       add       rsp,28
       ret
; Total bytes of code 121
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Negate()
       sub       rsp,18
       mov       rax,[rcx+18]
       mov       [rsp+8],rax
       mov       eax,[rcx+20]
       mov       [rsp+10],eax
       vmovsd    xmm0,qword ptr [rsp+8]
       vinsertps xmm0,xmm0,dword ptr [rsp+10],28
       vxorps    xmm0,xmm0,[7FFC5B1442D0]
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       add       rsp,18
       ret
; Total bytes of code 61
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Barycentric()
       sub       rsp,1C8
       vmovaps   [rsp+1B0],xmm6
       vmovaps   [rsp+1A0],xmm7
       vmovaps   [rsp+190],xmm8
       vmovaps   [rsp+180],xmm9
       vmovaps   [rsp+170],xmm10
       vmovaps   [rsp+160],xmm11
       vmovaps   [rsp+150],xmm12
       vmovaps   [rsp+140],xmm13
       vmovaps   [rsp+130],xmm14
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    xmm6,dword ptr [rcx+30]
       vmovss    xmm7,dword ptr [rcx+34]
       vmovss    xmm8,dword ptr [rcx+38]
       vmovss    xmm9,dword ptr [rcx+10]
       vmovss    xmm10,dword ptr [rcx+14]
       vbroadcastss xmm9,xmm9
       vmovaps   [rsp+120],xmm9
       vmovss    xmm9,dword ptr [rsp+120]
       vmovss    xmm11,dword ptr [rsp+124]
       vmovss    xmm12,dword ptr [rsp+128]
       vbroadcastss xmm10,xmm10
       vmovaps   [rsp+110],xmm10
       vmovss    xmm10,dword ptr [rsp+110]
       vmovss    xmm13,dword ptr [rsp+114]
       vmovss    xmm14,dword ptr [rsp+118]
       vmovss    dword ptr [rsp+0F0],xmm3
       vmovss    dword ptr [rsp+0F4],xmm4
       vmovss    dword ptr [rsp+0F8],xmm5
       vmovsd    xmm3,qword ptr [rsp+0F0]
       vinsertps xmm3,xmm3,dword ptr [rsp+0F8],28
       vmovss    dword ptr [rsp+0E0],xmm0
       vmovss    dword ptr [rsp+0E4],xmm1
       vmovss    dword ptr [rsp+0E8],xmm2
       vmovsd    xmm4,qword ptr [rsp+0E0]
       vinsertps xmm4,xmm4,dword ptr [rsp+0E8],28
       vsubps    xmm3,xmm3,xmm4
       vmovaps   [rsp+100],xmm3
       vmovss    xmm3,dword ptr [rsp+100]
       vmovss    xmm4,dword ptr [rsp+104]
       vmovss    xmm5,dword ptr [rsp+108]
       vmovss    dword ptr [rsp+0C0],xmm9
       vmovss    dword ptr [rsp+0C4],xmm11
       vmovss    dword ptr [rsp+0C8],xmm12
       vmovsd    xmm9,qword ptr [rsp+0C0]
       vinsertps xmm9,xmm9,dword ptr [rsp+0C8],28
       vmovss    dword ptr [rsp+0B0],xmm3
       vmovss    dword ptr [rsp+0B4],xmm4
       vmovss    dword ptr [rsp+0B8],xmm5
       vmovsd    xmm3,qword ptr [rsp+0B0]
       vinsertps xmm3,xmm3,dword ptr [rsp+0B8],28
       vmulps    xmm3,xmm3,xmm9
       vmovaps   [rsp+0D0],xmm3
       vmovss    xmm3,dword ptr [rsp+0D0]
       vmovss    xmm4,dword ptr [rsp+0D4]
       vmovss    xmm5,dword ptr [rsp+0D8]
       vmovss    dword ptr [rsp+90],xmm0
       vmovss    dword ptr [rsp+94],xmm1
       vmovss    dword ptr [rsp+98],xmm2
       vmovsd    xmm9,qword ptr [rsp+90]
       vinsertps xmm9,xmm9,dword ptr [rsp+98],28
       vmovss    dword ptr [rsp+80],xmm3
       vmovss    dword ptr [rsp+84],xmm4
       vmovss    dword ptr [rsp+88],xmm5
       vmovsd    xmm3,qword ptr [rsp+80]
       vinsertps xmm3,xmm3,dword ptr [rsp+88],28
       vaddps    xmm3,xmm3,xmm9
       vmovaps   [rsp+0A0],xmm3
       vmovss    xmm3,dword ptr [rsp+0A0]
       vmovss    xmm4,dword ptr [rsp+0A4]
       vmovss    xmm5,dword ptr [rsp+0A8]
       vmovss    dword ptr [rsp+60],xmm6
       vmovss    dword ptr [rsp+64],xmm7
       vmovss    dword ptr [rsp+68],xmm8
       vmovsd    xmm6,qword ptr [rsp+60]
       vinsertps xmm6,xmm6,dword ptr [rsp+68],28
       vmovss    dword ptr [rsp+50],xmm0
       vmovss    dword ptr [rsp+54],xmm1
       vmovss    dword ptr [rsp+58],xmm2
       vmovsd    xmm0,qword ptr [rsp+50]
       vinsertps xmm0,xmm0,dword ptr [rsp+58],28
       vsubps    xmm0,xmm6,xmm0
       vmovaps   [rsp+70],xmm0
       vmovss    xmm0,dword ptr [rsp+70]
       vmovss    xmm1,dword ptr [rsp+74]
       vmovss    xmm2,dword ptr [rsp+78]
       vmovss    dword ptr [rsp+30],xmm10
       vmovss    dword ptr [rsp+34],xmm13
       vmovss    dword ptr [rsp+38],xmm14
       vmovsd    xmm6,qword ptr [rsp+30]
       vinsertps xmm6,xmm6,dword ptr [rsp+38],28
       vmovss    dword ptr [rsp+20],xmm0
       vmovss    dword ptr [rsp+24],xmm1
       vmovss    dword ptr [rsp+28],xmm2
       vmovsd    xmm0,qword ptr [rsp+20]
       vinsertps xmm0,xmm0,dword ptr [rsp+28],28
       vmulps    xmm0,xmm0,xmm6
       vmovaps   [rsp+40],xmm0
       vmovss    xmm0,dword ptr [rsp+40]
       vmovss    xmm1,dword ptr [rsp+44]
       vmovss    xmm2,dword ptr [rsp+48]
       vmovss    dword ptr [rsp+10],xmm3
       vmovss    dword ptr [rsp+14],xmm4
       vmovss    dword ptr [rsp+18],xmm5
       vmovsd    xmm3,qword ptr [rsp+10]
       vinsertps xmm3,xmm3,dword ptr [rsp+18],28
       vmovss    dword ptr [rsp],xmm0
       vmovss    dword ptr [rsp+4],xmm1
       vmovss    dword ptr [rsp+8],xmm2
       vmovsd    xmm0,qword ptr [rsp]
       vinsertps xmm0,xmm0,dword ptr [rsp+8],28
       vaddps    xmm0,xmm0,xmm3
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       vmovaps   xmm6,[rsp+1B0]
       vmovaps   xmm7,[rsp+1A0]
       vmovaps   xmm8,[rsp+190]
       vmovaps   xmm9,[rsp+180]
       vmovaps   xmm10,[rsp+170]
       vmovaps   xmm11,[rsp+160]
       vmovaps   xmm12,[rsp+150]
       vmovaps   xmm13,[rsp+140]
       vmovaps   xmm14,[rsp+130]
       add       rsp,1C8
       ret
; Total bytes of code 982
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Clamp()
       sub       rsp,38
       mov       rax,[rcx+18]
       mov       [rsp+28],rax
       mov       eax,[rcx+20]
       mov       [rsp+30],eax
       mov       rax,[rcx+24]
       mov       [rsp+18],rax
       mov       eax,[rcx+2C]
       mov       [rsp+20],eax
       mov       rax,[rcx+30]
       mov       [rsp+8],rax
       mov       eax,[rcx+38]
       mov       [rsp+10],eax
       vmovsd    xmm0,qword ptr [rsp+28]
       vinsertps xmm0,xmm0,dword ptr [rsp+30],28
       vmovsd    xmm1,qword ptr [rsp+18]
       vinsertps xmm1,xmm1,dword ptr [rsp+20],28
       vmovsd    xmm2,qword ptr [rsp+8]
       vinsertps xmm2,xmm2,dword ptr [rsp+10],28
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
       add       rsp,38
       ret
; Total bytes of code 195
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Distance()
       sub       rsp,28
       mov       rax,[rcx+18]
       mov       [rsp+18],rax
       mov       eax,[rcx+20]
       mov       [rsp+20],eax
       mov       rax,[rcx+24]
       mov       [rsp+8],rax
       mov       eax,[rcx+2C]
       mov       [rsp+10],eax
       vmovsd    xmm0,qword ptr [rsp+18]
       vinsertps xmm0,xmm0,dword ptr [rsp+20],28
       vmovsd    xmm1,qword ptr [rsp+8]
       vinsertps xmm1,xmm1,dword ptr [rsp+10],28
       vsubps    xmm0,xmm0,xmm1
       vinsertps xmm0,xmm0,xmm0,38
       vdpps     xmm0,xmm0,xmm0,0FF
       vsqrtss   xmm0,xmm0,xmm0
       add       rsp,28
       ret
; Total bytes of code 89
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.DistanceSquared()
       sub       rsp,28
       mov       rax,[rcx+18]
       mov       [rsp+18],rax
       mov       eax,[rcx+20]
       mov       [rsp+20],eax
       mov       rax,[rcx+24]
       mov       [rsp+8],rax
       mov       eax,[rcx+2C]
       mov       [rsp+10],eax
       vmovsd    xmm0,qword ptr [rsp+18]
       vinsertps xmm0,xmm0,dword ptr [rsp+20],28
       vmovsd    xmm1,qword ptr [rsp+8]
       vinsertps xmm1,xmm1,dword ptr [rsp+10],28
       vsubps    xmm0,xmm0,xmm1
       vinsertps xmm0,xmm0,xmm0,38
       vdpps     xmm0,xmm0,xmm0,0FF
       add       rsp,28
       ret
; Total bytes of code 85
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Dot()
       sub       rsp,28
       mov       rax,[rcx+18]
       mov       [rsp+18],rax
       mov       eax,[rcx+20]
       mov       [rsp+20],eax
       mov       rax,[rcx+24]
       mov       [rsp+8],rax
       mov       eax,[rcx+2C]
       mov       [rsp+10],eax
       vmovsd    xmm0,qword ptr [rsp+18]
       vinsertps xmm0,xmm0,dword ptr [rsp+20],28
       vinsertps xmm0,xmm0,xmm0,38
       vmovsd    xmm1,qword ptr [rsp+8]
       vinsertps xmm1,xmm1,dword ptr [rsp+10],28
       vinsertps xmm1,xmm1,xmm1,38
       vdpps     xmm0,xmm0,xmm1,0FF
       add       rsp,28
       ret
; Total bytes of code 87
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Lerp()
       sub       rsp,28
       mov       rax,[rcx+18]
       mov       [rsp+18],rax
       mov       eax,[rcx+20]
       mov       [rsp+20],eax
       mov       rax,[rcx+24]
       mov       [rsp+8],rax
       mov       eax,[rcx+2C]
       mov       [rsp+10],eax
       vmovss    xmm0,dword ptr [rcx+10]
       vmovsd    xmm1,qword ptr [rsp+18]
       vinsertps xmm1,xmm1,dword ptr [rsp+20],28
       vmovsd    xmm2,qword ptr [rsp+8]
       vinsertps xmm2,xmm2,dword ptr [rsp+10],28
       vbroadcastss xmm0,xmm0
       vbroadcastss xmm3,dword ptr [7FFC5B164438]
       vsubps    xmm3,xmm3,xmm0
       vmulps    xmm0,xmm0,xmm2
       vfmadd213ps xmm1,xmm3,xmm0
       vmovsd    qword ptr [rdx],xmm1
       vextractps dword ptr [rdx+8],xmm1,2
       mov       rax,rdx
       add       rsp,28
       ret
; Total bytes of code 115
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.SmoothStep()
       sub       rsp,28
       mov       rax,[rcx+18]
       mov       [rsp+18],rax
       mov       eax,[rcx+20]
       mov       [rsp+20],eax
       mov       rax,[rcx+24]
       mov       [rsp+8],rax
       mov       eax,[rcx+2C]
       mov       [rsp+10],eax
       vmovss    xmm0,dword ptr [rcx+10]
       vxorps    xmm1,xmm1,xmm1
       vcmpneqps xmm2,xmm0,xmm0
       vorps     xmm1,xmm2,xmm1
       vxorps    xmm2,xmm2,xmm2
       vcmpgtps  xmm2,xmm0,xmm2
       vorps     xmm1,xmm2,xmm1
       vandps    xmm0,xmm1,xmm0
       vbroadcastss xmm1,dword ptr [7FFC5B1346F0]
       vminss    xmm0,xmm1,xmm0
       vmulss    xmm2,xmm0,xmm0
       vaddss    xmm0,xmm0,xmm0
       vmovss    xmm3,dword ptr [7FFC5B1346F4]
       vsubss    xmm0,xmm3,xmm0
       vmulss    xmm0,xmm2,xmm0
       vmovsd    xmm2,qword ptr [rsp+18]
       vinsertps xmm2,xmm2,dword ptr [rsp+20],28
       vmovsd    xmm3,qword ptr [rsp+8]
       vinsertps xmm3,xmm3,dword ptr [rsp+10],28
       vbroadcastss xmm0,xmm0
       vsubps    xmm1,xmm1,xmm0
       vmulps    xmm0,xmm0,xmm3
       vfmadd213ps xmm2,xmm1,xmm0
       vmovsd    qword ptr [rdx],xmm2
       vextractps dword ptr [rdx+8],xmm2,2
       mov       rax,rdx
       add       rsp,28
       ret
; Total bytes of code 173
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Hermite()
       sub       rsp,1B8
       vmovaps   [rsp+1A0],xmm6
       vmovaps   [rsp+190],xmm7
       vmovaps   [rsp+180],xmm8
       vmovaps   [rsp+170],xmm9
       vmovaps   [rsp+160],xmm10
       vmovaps   [rsp+150],xmm11
       vmovaps   [rsp+140],xmm12
       vmovaps   [rsp+130],xmm13
       vmovaps   [rsp+120],xmm14
       vmovaps   [rsp+110],xmm15
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    xmm6,dword ptr [rcx+30]
       vmovss    xmm7,dword ptr [rcx+34]
       vmovss    xmm8,dword ptr [rcx+38]
       vmovss    xmm9,dword ptr [rcx+3C]
       vmovss    xmm10,dword ptr [rcx+40]
       vmovss    xmm11,dword ptr [rcx+44]
       vmovss    dword ptr [rsp+0C],xmm11
       vmovss    xmm12,dword ptr [rcx+10]
       vmulss    xmm13,xmm12,xmm12
       vmulss    xmm14,xmm13,xmm12
       vaddss    xmm15,xmm13,xmm13
       vsubss    xmm15,xmm14,xmm15
       vaddss    xmm12,xmm15,xmm12
       vmulss    xmm15,xmm14,dword ptr [7FFC5B144D20]
       vmulss    xmm11,xmm13,dword ptr [7FFC5B144D24]
       vaddss    xmm15,xmm15,xmm11
       vsubss    xmm13,xmm14,xmm13
       vmovss    dword ptr [rsp+0F0],xmm0
       vmovss    dword ptr [rsp+0F4],xmm1
       vmovss    dword ptr [rsp+0F8],xmm2
       vaddss    xmm0,xmm14,xmm14
       vsubss    xmm0,xmm0,xmm11
       vaddss    xmm0,xmm0,dword ptr [7FFC5B144D28]
       vbroadcastss xmm0,xmm0
       vmovsd    xmm1,qword ptr [rsp+0F0]
       vinsertps xmm1,xmm1,dword ptr [rsp+0F8],28
       vmulps    xmm0,xmm0,xmm1
       vmovaps   [rsp+100],xmm0
       vmovss    xmm0,dword ptr [rsp+100]
       vmovss    xmm1,dword ptr [rsp+104]
       vmovss    xmm2,dword ptr [rsp+108]
       vmovss    dword ptr [rsp+0D0],xmm3
       vmovss    dword ptr [rsp+0D4],xmm4
       vmovss    dword ptr [rsp+0D8],xmm5
       vmovsd    xmm3,qword ptr [rsp+0D0]
       vinsertps xmm3,xmm3,dword ptr [rsp+0D8],28
       vbroadcastss xmm4,xmm12
       vmulps    xmm3,xmm4,xmm3
       vmovaps   [rsp+0E0],xmm3
       vmovss    xmm3,dword ptr [rsp+0E0]
       vmovss    xmm4,dword ptr [rsp+0E4]
       vmovss    xmm5,dword ptr [rsp+0E8]
       vmovss    dword ptr [rsp+0B0],xmm0
       vmovss    dword ptr [rsp+0B4],xmm1
       vmovss    dword ptr [rsp+0B8],xmm2
       vmovsd    xmm0,qword ptr [rsp+0B0]
       vinsertps xmm0,xmm0,dword ptr [rsp+0B8],28
       vmovss    dword ptr [rsp+0A0],xmm3
       vmovss    dword ptr [rsp+0A4],xmm4
       vmovss    dword ptr [rsp+0A8],xmm5
       vmovsd    xmm1,qword ptr [rsp+0A0]
       vinsertps xmm1,xmm1,dword ptr [rsp+0A8],28
       vaddps    xmm0,xmm1,xmm0
       vmovaps   [rsp+0C0],xmm0
       vmovss    xmm0,dword ptr [rsp+0C0]
       vmovss    xmm1,dword ptr [rsp+0C4]
       vmovss    xmm2,dword ptr [rsp+0C8]
       vmovss    dword ptr [rsp+80],xmm6
       vmovss    dword ptr [rsp+84],xmm7
       vmovss    dword ptr [rsp+88],xmm8
       vmovsd    xmm3,qword ptr [rsp+80]
       vinsertps xmm3,xmm3,dword ptr [rsp+88],28
       vbroadcastss xmm4,xmm15
       vmulps    xmm3,xmm4,xmm3
       vmovaps   [rsp+90],xmm3
       vmovss    xmm3,dword ptr [rsp+90]
       vmovss    xmm4,dword ptr [rsp+94]
       vmovss    xmm5,dword ptr [rsp+98]
       vmovss    dword ptr [rsp+60],xmm0
       vmovss    dword ptr [rsp+64],xmm1
       vmovss    dword ptr [rsp+68],xmm2
       vmovsd    xmm0,qword ptr [rsp+60]
       vinsertps xmm0,xmm0,dword ptr [rsp+68],28
       vmovss    dword ptr [rsp+50],xmm3
       vmovss    dword ptr [rsp+54],xmm4
       vmovss    dword ptr [rsp+58],xmm5
       vmovsd    xmm1,qword ptr [rsp+50]
       vinsertps xmm1,xmm1,dword ptr [rsp+58],28
       vaddps    xmm0,xmm1,xmm0
       vmovaps   [rsp+70],xmm0
       vmovss    xmm0,dword ptr [rsp+70]
       vmovss    xmm1,dword ptr [rsp+74]
       vmovss    xmm2,dword ptr [rsp+78]
       vmovss    dword ptr [rsp+30],xmm9
       vmovss    dword ptr [rsp+34],xmm10
       vmovss    xmm11,dword ptr [rsp+0C]
       vmovss    dword ptr [rsp+38],xmm11
       vmovsd    xmm3,qword ptr [rsp+30]
       vinsertps xmm3,xmm3,dword ptr [rsp+38],28
       vbroadcastss xmm4,xmm13
       vmulps    xmm3,xmm4,xmm3
       vmovaps   [rsp+40],xmm3
       vmovss    xmm3,dword ptr [rsp+40]
       vmovss    xmm4,dword ptr [rsp+44]
       vmovss    xmm5,dword ptr [rsp+48]
       vmovss    dword ptr [rsp+20],xmm0
       vmovss    dword ptr [rsp+24],xmm1
       vmovss    dword ptr [rsp+28],xmm2
       vmovsd    xmm0,qword ptr [rsp+20]
       vinsertps xmm0,xmm0,dword ptr [rsp+28],28
       vmovss    dword ptr [rsp+10],xmm3
       vmovss    dword ptr [rsp+14],xmm4
       vmovss    dword ptr [rsp+18],xmm5
       vmovsd    xmm1,qword ptr [rsp+10]
       vinsertps xmm1,xmm1,dword ptr [rsp+18],28
       vaddps    xmm0,xmm1,xmm0
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       vmovaps   xmm6,[rsp+1A0]
       vmovaps   xmm7,[rsp+190]
       vmovaps   xmm8,[rsp+180]
       vmovaps   xmm9,[rsp+170]
       vmovaps   xmm10,[rsp+160]
       vmovaps   xmm11,[rsp+150]
       vmovaps   xmm12,[rsp+140]
       vmovaps   xmm13,[rsp+130]
       vmovaps   xmm14,[rsp+120]
       vmovaps   xmm15,[rsp+110]
       add       rsp,1B8
       ret
; Total bytes of code 990
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.CatmullRom()
       sub       rsp,448
       vmovaps   [rsp+430],xmm6
       vmovaps   [rsp+420],xmm7
       vmovaps   [rsp+410],xmm8
       vmovaps   [rsp+400],xmm9
       vmovaps   [rsp+3F0],xmm10
       vmovaps   [rsp+3E0],xmm11
       vmovaps   [rsp+3D0],xmm12
       vmovaps   [rsp+3C0],xmm13
       vmovaps   [rsp+3B0],xmm14
       vmovaps   [rsp+3A0],xmm15
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    xmm6,dword ptr [rcx+30]
       vmovss    xmm7,dword ptr [rcx+34]
       vmovss    xmm8,dword ptr [rcx+38]
       vmovss    xmm9,dword ptr [rcx+3C]
       vmovss    xmm10,dword ptr [rcx+40]
       vmovss    xmm11,dword ptr [rcx+44]
       vmovss    xmm12,dword ptr [rcx+10]
       vmulss    xmm13,xmm12,xmm12
       vmovss    dword ptr [rsp+39C],xmm13
       vmulss    xmm14,xmm13,xmm12
       vmovss    dword ptr [rsp+398],xmm14
       vmovss    dword ptr [rsp+370],xmm3
       vmovss    dword ptr [rsp+374],xmm4
       vmovss    dword ptr [rsp+378],xmm5
       vmovsd    xmm15,qword ptr [rsp+370]
       vinsertps xmm15,xmm15,dword ptr [rsp+378],28
       vbroadcastss xmm14,dword ptr [7FFC5B145C10]
       vmulps    xmm15,xmm15,xmm14
       vmovaps   [rsp+380],xmm15
       vmovss    xmm15,dword ptr [rsp+380]
       vmovss    dword ptr [rsp+3C],xmm15
       vmovss    xmm15,dword ptr [rsp+384]
       vmovss    dword ptr [rsp+38],xmm15
       vmovss    xmm15,dword ptr [rsp+388]
       vmovss    dword ptr [rsp+34],xmm15
       vmovss    dword ptr [rsp+350],xmm0
       vmovss    dword ptr [rsp+354],xmm1
       vmovss    dword ptr [rsp+358],xmm2
       vmovsd    xmm15,qword ptr [rsp+350]
       vinsertps xmm15,xmm15,dword ptr [rsp+358],28
       vbroadcastss xmm13,dword ptr [7FFC5B145C14]
       vmovaps   [rsp],xmm13
       vxorps    xmm15,xmm15,xmm13
       vmovaps   [rsp+360],xmm15
       mov       rax,[rsp+360]
       mov       [rsp+330],rax
       mov       eax,[rsp+368]
       mov       [rsp+338],eax
       vmovsd    xmm15,qword ptr [rsp+330]
       vinsertps xmm15,xmm15,dword ptr [rsp+338],28
       vmovss    dword ptr [rsp+320],xmm6
       vmovss    dword ptr [rsp+324],xmm7
       vmovss    dword ptr [rsp+328],xmm8
       vmovsd    xmm13,qword ptr [rsp+320]
       vinsertps xmm13,xmm13,dword ptr [rsp+328],28
       vaddps    xmm13,xmm13,xmm15
       vmovaps   [rsp+340],xmm13
       mov       rax,[rsp+340]
       mov       [rsp+300],rax
       mov       eax,[rsp+348]
       mov       [rsp+308],eax
       vmovsd    xmm13,qword ptr [rsp+300]
       vinsertps xmm13,xmm13,dword ptr [rsp+308],28
       vbroadcastss xmm12,xmm12
       vmulps    xmm12,xmm12,xmm13
       vmovaps   [rsp+310],xmm12
       vmovss    xmm12,dword ptr [rsp+310]
       vmovss    xmm13,dword ptr [rsp+314]
       vmovss    xmm15,dword ptr [rsp+318]
       vmovss    dword ptr [rsp+30],xmm15
       vmovss    xmm15,dword ptr [rsp+3C]
       vmovss    dword ptr [rsp+2E0],xmm15
       vmovss    xmm15,dword ptr [rsp+38]
       vmovss    dword ptr [rsp+2E4],xmm15
       vmovss    xmm15,dword ptr [rsp+34]
       vmovss    dword ptr [rsp+2E8],xmm15
       vmovsd    xmm15,qword ptr [rsp+2E0]
       vinsertps xmm15,xmm15,dword ptr [rsp+2E8],28
       vmovss    dword ptr [rsp+2D0],xmm12
       vmovss    dword ptr [rsp+2D4],xmm13
       vmovss    xmm12,dword ptr [rsp+30]
       vmovss    dword ptr [rsp+2D8],xmm12
       vmovsd    xmm12,qword ptr [rsp+2D0]
       vinsertps xmm12,xmm12,dword ptr [rsp+2D8],28
       vaddps    xmm12,xmm12,xmm15
       vmovaps   [rsp+2F0],xmm12
       vmovss    xmm12,dword ptr [rsp+2F0]
       vmovss    dword ptr [rsp+2C],xmm12
       vmovss    xmm13,dword ptr [rsp+2F4]
       vmovss    dword ptr [rsp+28],xmm13
       vmovss    xmm15,dword ptr [rsp+2F8]
       vmovss    dword ptr [rsp+24],xmm15
       vmovss    dword ptr [rsp+2B0],xmm0
       vmovss    dword ptr [rsp+2B4],xmm1
       vmovss    dword ptr [rsp+2B8],xmm2
       vmovsd    xmm15,qword ptr [rsp+2B0]
       vinsertps xmm15,xmm15,dword ptr [rsp+2B8],28
       vmulps    xmm14,xmm15,xmm14
       vmovaps   [rsp+2C0],xmm14
       vmovss    xmm14,dword ptr [rsp+2C0]
       vmovss    xmm15,dword ptr [rsp+2C4]
       vmovss    xmm13,dword ptr [rsp+2C8]
       vmovss    dword ptr [rsp+290],xmm3
       vmovss    dword ptr [rsp+294],xmm4
       vmovss    dword ptr [rsp+298],xmm5
       vmovsd    xmm12,qword ptr [rsp+290]
       vinsertps xmm12,xmm12,dword ptr [rsp+298],28
       vmulps    xmm12,xmm12,[7FFC5B145C20]
       vmovaps   [rsp+2A0],xmm12
       vmovss    xmm12,dword ptr [rsp+2A0]
       vmovss    dword ptr [rsp+20],xmm12
       vmovss    xmm12,dword ptr [rsp+2A4]
       vmovss    dword ptr [rsp+1C],xmm12
       vmovss    xmm12,dword ptr [rsp+2A8]
       vmovss    dword ptr [rsp+270],xmm14
       vmovss    dword ptr [rsp+274],xmm15
       vmovss    dword ptr [rsp+278],xmm13
       vmovsd    xmm13,qword ptr [rsp+270]
       vinsertps xmm13,xmm13,dword ptr [rsp+278],28
       vmovss    xmm14,dword ptr [rsp+20]
       vmovss    dword ptr [rsp+260],xmm14
       vmovss    xmm14,dword ptr [rsp+1C]
       vmovss    dword ptr [rsp+264],xmm14
       vmovss    dword ptr [rsp+268],xmm12
       vmovsd    xmm12,qword ptr [rsp+260]
       vinsertps xmm12,xmm12,dword ptr [rsp+268],28
       vsubps    xmm12,xmm13,xmm12
       vmovaps   [rsp+280],xmm12
       vmovss    xmm12,dword ptr [rsp+280]
       vmovss    xmm13,dword ptr [rsp+284]
       vmovss    xmm14,dword ptr [rsp+288]
       vmovss    dword ptr [rsp+240],xmm6
       vmovss    dword ptr [rsp+244],xmm7
       vmovss    dword ptr [rsp+248],xmm8
       vmovsd    xmm15,qword ptr [rsp+240]
       vinsertps xmm15,xmm15,dword ptr [rsp+248],28
       vmulps    xmm15,xmm15,[7FFC5B145C30]
       vmovaps   [rsp+250],xmm15
       vmovss    xmm15,dword ptr [rsp+250]
       vmovss    dword ptr [rsp+18],xmm15
       vmovss    xmm15,dword ptr [rsp+254]
       vmovss    dword ptr [rsp+14],xmm15
       vmovss    xmm15,dword ptr [rsp+258]
       vmovss    dword ptr [rsp+220],xmm12
       vmovss    dword ptr [rsp+224],xmm13
       vmovss    dword ptr [rsp+228],xmm14
       vmovsd    xmm12,qword ptr [rsp+220]
       vinsertps xmm12,xmm12,dword ptr [rsp+228],28
       vmovss    xmm13,dword ptr [rsp+18]
       vmovss    dword ptr [rsp+210],xmm13
       vmovss    xmm13,dword ptr [rsp+14]
       vmovss    dword ptr [rsp+214],xmm13
       vmovss    dword ptr [rsp+218],xmm15
       vmovsd    xmm13,qword ptr [rsp+210]
       vinsertps xmm13,xmm13,dword ptr [rsp+218],28
       vaddps    xmm12,xmm13,xmm12
       vmovaps   [rsp+230],xmm12
       mov       rax,[rsp+230]
       mov       [rsp+1F0],rax
       mov       eax,[rsp+238]
       mov       [rsp+1F8],eax
       vmovsd    xmm12,qword ptr [rsp+1F0]
       vinsertps xmm12,xmm12,dword ptr [rsp+1F8],28
       vmovss    dword ptr [rsp+1E0],xmm9
       vmovss    dword ptr [rsp+1E4],xmm10
       vmovss    dword ptr [rsp+1E8],xmm11
       vmovsd    xmm13,qword ptr [rsp+1E0]
       vinsertps xmm13,xmm13,dword ptr [rsp+1E8],28
       vsubps    xmm12,xmm12,xmm13
       vmovaps   [rsp+200],xmm12
       mov       rax,[rsp+200]
       mov       [rsp+1C0],rax
       mov       eax,[rsp+208]
       mov       [rsp+1C8],eax
       vmovsd    xmm12,qword ptr [rsp+1C0]
       vinsertps xmm12,xmm12,dword ptr [rsp+1C8],28
       vbroadcastss xmm13,dword ptr [rsp+39C]
       vmulps    xmm12,xmm13,xmm12
       vmovaps   [rsp+1D0],xmm12
       vmovss    xmm12,dword ptr [rsp+1D0]
       vmovss    xmm13,dword ptr [rsp+1D4]
       vmovss    xmm14,dword ptr [rsp+1D8]
       vmovss    xmm15,dword ptr [rsp+2C]
       vmovss    dword ptr [rsp+1A0],xmm15
       vmovss    xmm15,dword ptr [rsp+28]
       vmovss    dword ptr [rsp+1A4],xmm15
       vmovss    xmm15,dword ptr [rsp+24]
       vmovss    dword ptr [rsp+1A8],xmm15
       vmovsd    xmm15,qword ptr [rsp+1A0]
       vinsertps xmm15,xmm15,dword ptr [rsp+1A8],28
       vmovss    dword ptr [rsp+190],xmm12
       vmovss    dword ptr [rsp+194],xmm13
       vmovss    dword ptr [rsp+198],xmm14
       vmovsd    xmm12,qword ptr [rsp+190]
       vinsertps xmm12,xmm12,dword ptr [rsp+198],28
       vaddps    xmm12,xmm12,xmm15
       vmovaps   [rsp+1B0],xmm12
       vmovss    xmm12,dword ptr [rsp+1B0]
       vmovss    xmm13,dword ptr [rsp+1B4]
       vmovss    xmm14,dword ptr [rsp+1B8]
       vmovss    dword ptr [rsp+170],xmm0
       vmovss    dword ptr [rsp+174],xmm1
       vmovss    dword ptr [rsp+178],xmm2
       vmovsd    xmm0,qword ptr [rsp+170]
       vinsertps xmm0,xmm0,dword ptr [rsp+178],28
       vxorps    xmm0,xmm0,[rsp]
       vmovaps   [rsp+180],xmm0
       vmovss    xmm0,dword ptr [rsp+180]
       vmovss    xmm1,dword ptr [rsp+184]
       vmovss    xmm2,dword ptr [rsp+188]
       vmovss    dword ptr [rsp+150],xmm3
       vmovss    dword ptr [rsp+154],xmm4
       vmovss    dword ptr [rsp+158],xmm5
       vmovsd    xmm3,qword ptr [rsp+150]
       vinsertps xmm3,xmm3,dword ptr [rsp+158],28
       vbroadcastss xmm4,dword ptr [7FFC5B145C40]
       vmulps    xmm3,xmm3,xmm4
       vmovaps   [rsp+160],xmm3
       vmovss    xmm3,dword ptr [rsp+160]
       vmovss    xmm5,dword ptr [rsp+164]
       vmovss    xmm15,dword ptr [rsp+168]
       vmovss    dword ptr [rsp+130],xmm0
       vmovss    dword ptr [rsp+134],xmm1
       vmovss    dword ptr [rsp+138],xmm2
       vmovsd    xmm0,qword ptr [rsp+130]
       vinsertps xmm0,xmm0,dword ptr [rsp+138],28
       vmovss    dword ptr [rsp+120],xmm3
       vmovss    dword ptr [rsp+124],xmm5
       vmovss    dword ptr [rsp+128],xmm15
       vmovsd    xmm1,qword ptr [rsp+120]
       vinsertps xmm1,xmm1,dword ptr [rsp+128],28
       vaddps    xmm0,xmm1,xmm0
       vmovaps   [rsp+140],xmm0
       vmovss    xmm0,dword ptr [rsp+140]
       vmovss    xmm1,dword ptr [rsp+144]
       vmovss    xmm2,dword ptr [rsp+148]
       vmovss    dword ptr [rsp+100],xmm6
       vmovss    dword ptr [rsp+104],xmm7
       vmovss    dword ptr [rsp+108],xmm8
       vmovsd    xmm3,qword ptr [rsp+100]
       vinsertps xmm3,xmm3,dword ptr [rsp+108],28
       vmulps    xmm3,xmm3,xmm4
       vmovaps   [rsp+110],xmm3
       vmovss    xmm3,dword ptr [rsp+110]
       vmovss    xmm4,dword ptr [rsp+114]
       vmovss    xmm5,dword ptr [rsp+118]
       vmovss    dword ptr [rsp+0E0],xmm0
       vmovss    dword ptr [rsp+0E4],xmm1
       vmovss    dword ptr [rsp+0E8],xmm2
       vmovsd    xmm0,qword ptr [rsp+0E0]
       vinsertps xmm0,xmm0,dword ptr [rsp+0E8],28
       vmovss    dword ptr [rsp+0D0],xmm3
       vmovss    dword ptr [rsp+0D4],xmm4
       vmovss    dword ptr [rsp+0D8],xmm5
       vmovsd    xmm1,qword ptr [rsp+0D0]
       vinsertps xmm1,xmm1,dword ptr [rsp+0D8],28
       vsubps    xmm0,xmm0,xmm1
       vmovaps   [rsp+0F0],xmm0
       mov       rax,[rsp+0F0]
       mov       [rsp+0B0],rax
       mov       eax,[rsp+0F8]
       mov       [rsp+0B8],eax
       vmovsd    xmm0,qword ptr [rsp+0B0]
       vinsertps xmm0,xmm0,dword ptr [rsp+0B8],28
       vmovss    dword ptr [rsp+0A0],xmm9
       vmovss    dword ptr [rsp+0A4],xmm10
       vmovss    dword ptr [rsp+0A8],xmm11
       vmovsd    xmm1,qword ptr [rsp+0A0]
       vinsertps xmm1,xmm1,dword ptr [rsp+0A8],28
       vaddps    xmm0,xmm1,xmm0
       vmovaps   [rsp+0C0],xmm0
       mov       rax,[rsp+0C0]
       mov       [rsp+80],rax
       mov       eax,[rsp+0C8]
       mov       [rsp+88],eax
       vmovsd    xmm0,qword ptr [rsp+80]
       vinsertps xmm0,xmm0,dword ptr [rsp+88],28
       vbroadcastss xmm1,dword ptr [rsp+398]
       vmulps    xmm0,xmm1,xmm0
       vmovaps   [rsp+90],xmm0
       vmovss    xmm0,dword ptr [rsp+90]
       vmovss    xmm1,dword ptr [rsp+94]
       vmovss    xmm2,dword ptr [rsp+98]
       vmovss    dword ptr [rsp+60],xmm12
       vmovss    dword ptr [rsp+64],xmm13
       vmovss    dword ptr [rsp+68],xmm14
       vmovsd    xmm3,qword ptr [rsp+60]
       vinsertps xmm3,xmm3,dword ptr [rsp+68],28
       vmovss    dword ptr [rsp+50],xmm0
       vmovss    dword ptr [rsp+54],xmm1
       vmovss    dword ptr [rsp+58],xmm2
       vmovsd    xmm0,qword ptr [rsp+50]
       vinsertps xmm0,xmm0,dword ptr [rsp+58],28
       vaddps    xmm0,xmm0,xmm3
       vmovaps   [rsp+70],xmm0
       mov       rax,[rsp+70]
       mov       [rsp+40],rax
       mov       eax,[rsp+78]
       mov       [rsp+48],eax
       vmovsd    xmm0,qword ptr [rsp+40]
       vinsertps xmm0,xmm0,dword ptr [rsp+48],28
       vmulps    xmm0,xmm0,[7FFC5B145C50]
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       vmovaps   xmm6,[rsp+430]
       vmovaps   xmm7,[rsp+420]
       vmovaps   xmm8,[rsp+410]
       vmovaps   xmm9,[rsp+400]
       vmovaps   xmm10,[rsp+3F0]
       vmovaps   xmm11,[rsp+3E0]
       vmovaps   xmm12,[rsp+3D0]
       vmovaps   xmm13,[rsp+3C0]
       vmovaps   xmm14,[rsp+3B0]
       vmovaps   xmm15,[rsp+3A0]
       add       rsp,448
       ret
; Total bytes of code 2644
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Max()
       sub       rsp,28
       mov       rax,[rcx+18]
       mov       [rsp+18],rax
       mov       eax,[rcx+20]
       mov       [rsp+20],eax
       mov       rax,[rcx+24]
       mov       [rsp+8],rax
       mov       eax,[rcx+2C]
       mov       [rsp+10],eax
       vmovsd    xmm0,qword ptr [rsp+18]
       vinsertps xmm0,xmm0,dword ptr [rsp+20],28
       vmovsd    xmm1,qword ptr [rsp+8]
       vinsertps xmm1,xmm1,dword ptr [rsp+10],28
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
       add       rsp,28
       ret
; Total bytes of code 124
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Min()
       sub       rsp,28
       mov       rax,[rcx+18]
       mov       [rsp+18],rax
       mov       eax,[rcx+20]
       mov       [rsp+20],eax
       mov       rax,[rcx+24]
       mov       [rsp+8],rax
       mov       eax,[rcx+2C]
       mov       [rsp+10],eax
       vmovsd    xmm0,qword ptr [rsp+18]
       vinsertps xmm0,xmm0,dword ptr [rsp+20],28
       vmovsd    xmm1,qword ptr [rsp+8]
       vinsertps xmm1,xmm1,dword ptr [rsp+10],28
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
       add       rsp,28
       ret
; Total bytes of code 124
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Reflect()
       sub       rsp,28
       mov       rax,[rcx+18]
       mov       [rsp+18],rax
       mov       eax,[rcx+20]
       mov       [rsp+20],eax
       mov       rax,[rcx+24]
       mov       [rsp+8],rax
       mov       eax,[rcx+2C]
       mov       [rsp+10],eax
       vmovsd    xmm0,qword ptr [rsp+18]
       vinsertps xmm0,xmm0,dword ptr [rsp+20],28
       vmovsd    xmm1,qword ptr [rsp+8]
       vinsertps xmm1,xmm1,dword ptr [rsp+10],28
       vinsertps xmm2,xmm0,xmm0,38
       vinsertps xmm3,xmm1,xmm1,38
       vdpps     xmm2,xmm2,xmm3,0FF
       vaddps    xmm2,xmm2,xmm2
       vxorps    xmm2,xmm2,[7FFC5B174450]
       vfmadd213ps xmm2,xmm1,xmm0
       vmovsd    qword ptr [rdx],xmm2
       vextractps dword ptr [rdx+8],xmm2,2
       mov       rax,rdx
       add       rsp,28
       ret
; Total bytes of code 118
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Orthogonalize()
       push      rsi
       push      rbx
       sub       rsp,0F8
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+20],ymm4
       vmovdqu   ymmword ptr [rsp+40],ymm4
       mov       rax,[rcx+8]
       mov       rdx,offset MT_Stride.Core.Mathematics.Vector3[]
       mov       [rsp+20],rdx
       lea       rdx,[rsp+20]
       mov       dword ptr [rdx+8],4
       lea       rdx,[rsp+20]
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       lea       r8,[rdx+10]
       vmovss    dword ptr [r8],xmm0
       vmovss    dword ptr [r8+4],xmm1
       vmovss    dword ptr [r8+8],xmm2
       vmovss    xmm0,dword ptr [rcx+24]
       vmovss    xmm1,dword ptr [rcx+28]
       vmovss    xmm2,dword ptr [rcx+2C]
       lea       r8,[rdx+1C]
       vmovss    dword ptr [r8],xmm0
       vmovss    dword ptr [r8+4],xmm1
       vmovss    dword ptr [r8+8],xmm2
       vmovss    xmm0,dword ptr [rcx+30]
       vmovss    xmm1,dword ptr [rcx+34]
       vmovss    xmm2,dword ptr [rcx+38]
       lea       r8,[rdx+28]
       vmovss    dword ptr [r8],xmm0
       vmovss    dword ptr [r8+4],xmm1
       vmovss    dword ptr [r8+8],xmm2
       vmovss    xmm0,dword ptr [rcx+3C]
       vmovss    xmm1,dword ptr [rcx+40]
       vmovss    xmm2,dword ptr [rcx+44]
       lea       rcx,[rdx+34]
       vmovss    dword ptr [rcx],xmm0
       vmovss    dword ptr [rcx+4],xmm1
       vmovss    dword ptr [rcx+8],xmm2
       test      rax,rax
       je        near ptr M00_L04
       mov       ecx,[rax+8]
       cmp       ecx,4
       jl        near ptr M00_L05
       xor       r8d,r8d
       cmp       r8d,4
       jl        near ptr M00_L03
M00_L00:
       add       rsp,0F8
       pop       rbx
       pop       rsi
       ret
M00_L01:
       cmp       r9d,ecx
       jae       near ptr M00_L06
       lea       r11,[r9+r9*2]
       lea       r11,[rax+r11*4+10]
       mov       rbx,[r11]
       mov       [rsp+0E8],rbx
       mov       ebx,[r11+8]
       mov       [rsp+0F0],ebx
       vmovss    dword ptr [rsp+0D8],xmm0
       vmovss    dword ptr [rsp+0DC],xmm1
       vmovss    dword ptr [rsp+0E0],xmm2
       vmovsd    xmm3,qword ptr [rsp+0E8]
       vinsertps xmm3,xmm3,dword ptr [rsp+0F0],28
       vinsertps xmm3,xmm3,xmm3,38
       vmovsd    xmm4,qword ptr [rsp+0D8]
       vinsertps xmm4,xmm4,dword ptr [rsp+0E0],28
       vinsertps xmm4,xmm4,xmm4,38
       vdpps     xmm3,xmm3,xmm4,0FF
       mov       rbx,[r11]
       mov       [rsp+0C8],rbx
       mov       ebx,[r11+8]
       mov       [rsp+0D0],ebx
       mov       rbx,[r11]
       mov       [rsp+0B8],rbx
       mov       ebx,[r11+8]
       mov       [rsp+0C0],ebx
       vmovsd    xmm4,qword ptr [rsp+0C8]
       vinsertps xmm4,xmm4,dword ptr [rsp+0D0],28
       vinsertps xmm4,xmm4,xmm4,38
       vmovsd    xmm5,qword ptr [rsp+0B8]
       vinsertps xmm5,xmm5,dword ptr [rsp+0C0],28
       vinsertps xmm5,xmm5,xmm5,38
       vdpps     xmm4,xmm4,xmm5,0FF
       vdivss    xmm3,xmm3,xmm4
       mov       rbx,[r11]
       mov       [rsp+90],rbx
       mov       ebx,[r11+8]
       mov       [rsp+98],ebx
       vmovsd    xmm4,qword ptr [rsp+90]
       vinsertps xmm4,xmm4,dword ptr [rsp+98],28
       vbroadcastss xmm3,xmm3
       vmulps    xmm3,xmm3,xmm4
       vmovaps   [rsp+0A0],xmm3
       vmovss    xmm3,dword ptr [rsp+0A0]
       vmovss    xmm4,dword ptr [rsp+0A4]
       vmovss    xmm5,dword ptr [rsp+0A8]
       vmovss    dword ptr [rsp+70],xmm0
       vmovss    dword ptr [rsp+74],xmm1
       vmovss    dword ptr [rsp+78],xmm2
       vmovsd    xmm0,qword ptr [rsp+70]
       vinsertps xmm0,xmm0,dword ptr [rsp+78],28
       vmovss    dword ptr [rsp+60],xmm3
       vmovss    dword ptr [rsp+64],xmm4
       vmovss    dword ptr [rsp+68],xmm5
       vmovsd    xmm1,qword ptr [rsp+60]
       vinsertps xmm1,xmm1,dword ptr [rsp+68],28
       vsubps    xmm2,xmm0,xmm1
       vmovaps   [rsp+80],xmm2
       vmovss    xmm0,dword ptr [rsp+80]
       vmovss    xmm1,dword ptr [rsp+84]
       vmovss    xmm2,dword ptr [rsp+88]
       inc       r9d
M00_L02:
       cmp       r9d,r8d
       jl        near ptr M00_L01
       lea       r10,[rax+r10*4+10]
       vmovss    dword ptr [r10],xmm0
       vmovss    dword ptr [r10+4],xmm1
       vmovss    dword ptr [r10+8],xmm2
       inc       r8d
       cmp       r8d,4
       jge       near ptr M00_L00
M00_L03:
       lea       r10,[r8+r8*2]
       lea       r9,[rdx+r10*4+10]
       vmovss    xmm0,dword ptr [r9]
       vmovss    xmm1,dword ptr [r9+4]
       vmovss    xmm2,dword ptr [r9+8]
       xor       r9d,r9d
       jmp       short M00_L02
M00_L04:
       mov       ecx,611
       mov       rdx,7FFC5B3B36E0
       call      qword ptr [7FFC5B2F7798]
       mov       rcx,rax
       call      qword ptr [7FFC5B597618]
       int       3
M00_L05:
       mov       rcx,offset MT_System.ArgumentOutOfRangeException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       ecx,611
       mov       rdx,7FFC5B3B36E0
       call      qword ptr [7FFC5B2F7798]
       mov       rsi,rax
       mov       ecx,629
       mov       rdx,7FFC5B3B36E0
       call      qword ptr [7FFC5B2F7798]
       mov       r8,rax
       mov       rdx,rsi
       mov       rcx,rbx
       call      qword ptr [7FFC5B4FE5B0]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
M00_L06:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 870
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Orthonormalize()
       push      rsi
       push      rbx
       sub       rsp,118
       vmovaps   [rsp+100],xmm6
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+20],ymm4
       vmovdqu   ymmword ptr [rsp+40],ymm4
       mov       rax,[rcx+8]
       mov       rdx,offset MT_Stride.Core.Mathematics.Vector3[]
       mov       [rsp+20],rdx
       lea       rdx,[rsp+20]
       mov       dword ptr [rdx+8],4
       lea       rdx,[rsp+20]
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       lea       r8,[rdx+10]
       vmovss    dword ptr [r8],xmm0
       vmovss    dword ptr [r8+4],xmm1
       vmovss    dword ptr [r8+8],xmm2
       vmovss    xmm0,dword ptr [rcx+24]
       vmovss    xmm1,dword ptr [rcx+28]
       vmovss    xmm2,dword ptr [rcx+2C]
       lea       r8,[rdx+1C]
       vmovss    dword ptr [r8],xmm0
       vmovss    dword ptr [r8+4],xmm1
       vmovss    dword ptr [r8+8],xmm2
       vmovss    xmm0,dword ptr [rcx+30]
       vmovss    xmm1,dword ptr [rcx+34]
       vmovss    xmm2,dword ptr [rcx+38]
       lea       r8,[rdx+28]
       vmovss    dword ptr [r8],xmm0
       vmovss    dword ptr [r8+4],xmm1
       vmovss    dword ptr [r8+8],xmm2
       vmovss    xmm0,dword ptr [rcx+3C]
       vmovss    xmm1,dword ptr [rcx+40]
       vmovss    xmm2,dword ptr [rcx+44]
       lea       rcx,[rdx+34]
       vmovss    dword ptr [rcx],xmm0
       vmovss    dword ptr [rcx+4],xmm1
       vmovss    dword ptr [rcx+8],xmm2
       test      rax,rax
       je        near ptr M00_L06
       mov       ecx,[rax+8]
       cmp       ecx,4
       jl        near ptr M00_L07
       xor       r8d,r8d
       cmp       r8d,4
       jl        near ptr M00_L04
M00_L00:
       vmovaps   xmm6,[rsp+100]
       add       rsp,118
       pop       rbx
       pop       rsi
       ret
M00_L01:
       cmp       r9d,ecx
       jae       near ptr M00_L08
       mov       r11d,r9d
       lea       r11,[r11+r11*2]
       lea       r11,[rax+r11*4+10]
       mov       rbx,[r11]
       mov       [rsp+0F0],rbx
       mov       ebx,[r11+8]
       mov       [rsp+0F8],ebx
       vmovss    dword ptr [rsp+0E0],xmm0
       vmovss    dword ptr [rsp+0E4],xmm1
       vmovss    dword ptr [rsp+0E8],xmm2
       vmovsd    xmm3,qword ptr [rsp+0F0]
       vinsertps xmm3,xmm3,dword ptr [rsp+0F8],28
       vmovsd    xmm4,qword ptr [rsp+0E0]
       vinsertps xmm4,xmm4,dword ptr [rsp+0E8],28
       mov       rbx,[r11]
       mov       [rsp+0C0],rbx
       mov       ebx,[r11+8]
       mov       [rsp+0C8],ebx
       vmovaps   xmm5,xmm3
       vinsertps xmm3,xmm5,xmm5,38
       vinsertps xmm4,xmm4,xmm4,38
       vdpps     xmm3,xmm3,xmm4,0FF
       vmovsd    xmm4,qword ptr [rsp+0C0]
       vinsertps xmm4,xmm4,dword ptr [rsp+0C8],28
       vmulps    xmm3,xmm3,xmm4
       vmovaps   [rsp+0D0],xmm3
       vmovss    xmm3,dword ptr [rsp+0D0]
       vmovss    xmm4,dword ptr [rsp+0D4]
       vmovss    xmm5,dword ptr [rsp+0D8]
       vmovss    dword ptr [rsp+0A0],xmm0
       vmovss    dword ptr [rsp+0A4],xmm1
       vmovss    dword ptr [rsp+0A8],xmm2
       vmovsd    xmm6,qword ptr [rsp+0A0]
       vinsertps xmm6,xmm6,dword ptr [rsp+0A8],28
       vmovss    dword ptr [rsp+90],xmm3
       vmovss    dword ptr [rsp+94],xmm4
       vmovss    dword ptr [rsp+98],xmm5
       vmovaps   xmm0,xmm6
       vmovsd    xmm1,qword ptr [rsp+90]
       vinsertps xmm1,xmm1,dword ptr [rsp+98],28
       vsubps    xmm2,xmm0,xmm1
       vmovaps   [rsp+0B0],xmm2
       vmovss    xmm0,dword ptr [rsp+0B0]
       vmovss    xmm1,dword ptr [rsp+0B4]
       vmovss    xmm2,dword ptr [rsp+0B8]
       inc       r9d
       cmp       r9d,r8d
       jl        near ptr M00_L01
M00_L02:
       vmovss    dword ptr [rsp+80],xmm0
       vmovss    dword ptr [rsp+84],xmm1
       vmovss    dword ptr [rsp+88],xmm2
       vmovsd    xmm3,qword ptr [rsp+80]
       vinsertps xmm3,xmm3,dword ptr [rsp+88],28
       vinsertps xmm3,xmm3,xmm3,38
       vdpps     xmm3,xmm3,xmm3,0FF
       vsqrtss   xmm3,xmm3,xmm3
       vucomiss  xmm3,dword ptr [7FFC5B135430]
       jbe       short M00_L03
       vmovss    dword ptr [rsp+60],xmm0
       vmovss    dword ptr [rsp+64],xmm1
       vmovss    dword ptr [rsp+68],xmm2
       vmovsd    xmm0,qword ptr [rsp+60]
       vinsertps xmm0,xmm0,dword ptr [rsp+68],28
       vbroadcastss xmm1,xmm3
       vdivps    xmm2,xmm0,xmm1
       vmovaps   [rsp+70],xmm2
       vmovss    xmm0,dword ptr [rsp+70]
       vmovss    xmm1,dword ptr [rsp+74]
       vmovss    xmm2,dword ptr [rsp+78]
M00_L03:
       lea       r10,[rax+r10*4+10]
       vmovss    dword ptr [r10],xmm0
       vmovss    dword ptr [r10+4],xmm1
       vmovss    dword ptr [r10+8],xmm2
       inc       r8d
       cmp       r8d,4
       jge       near ptr M00_L00
M00_L04:
       lea       r10,[r8+r8*2]
       lea       r9,[rdx+r10*4+10]
       vmovss    xmm0,dword ptr [r9]
       vmovss    xmm1,dword ptr [r9+4]
       vmovss    xmm2,dword ptr [r9+8]
       xor       r9d,r9d
       test      r8d,r8d
       jle       near ptr M00_L02
       cmp       ecx,r8d
       jl        near ptr M00_L01
M00_L05:
       mov       r11d,r9d
       lea       r11,[r11+r11*2]
       mov       rbx,[rax+r11*4+10]
       mov       [rsp+0F0],rbx
       mov       ebx,[rax+r11*4+18]
       mov       [rsp+0F8],ebx
       vmovss    dword ptr [rsp+0E0],xmm0
       vmovss    dword ptr [rsp+0E4],xmm1
       vmovss    dword ptr [rsp+0E8],xmm2
       vmovsd    xmm3,qword ptr [rsp+0F0]
       vinsertps xmm3,xmm3,dword ptr [rsp+0F8],28
       vmovsd    xmm4,qword ptr [rsp+0E0]
       vinsertps xmm4,xmm4,dword ptr [rsp+0E8],28
       mov       rbx,[rax+r11*4+10]
       mov       [rsp+0C0],rbx
       mov       ebx,[rax+r11*4+18]
       mov       [rsp+0C8],ebx
       vinsertps xmm3,xmm3,xmm3,38
       vinsertps xmm4,xmm4,xmm4,38
       vdpps     xmm3,xmm3,xmm4,0FF
       vmovsd    xmm4,qword ptr [rsp+0C0]
       vinsertps xmm4,xmm4,dword ptr [rsp+0C8],28
       vmulps    xmm3,xmm3,xmm4
       vmovaps   [rsp+0D0],xmm3
       vmovss    xmm3,dword ptr [rsp+0D0]
       vmovss    xmm4,dword ptr [rsp+0D4]
       vmovss    xmm5,dword ptr [rsp+0D8]
       vmovss    dword ptr [rsp+0A0],xmm0
       vmovss    dword ptr [rsp+0A4],xmm1
       vmovss    dword ptr [rsp+0A8],xmm2
       vmovsd    xmm6,qword ptr [rsp+0A0]
       vinsertps xmm6,xmm6,dword ptr [rsp+0A8],28
       vmovss    dword ptr [rsp+90],xmm3
       vmovss    dword ptr [rsp+94],xmm4
       vmovss    dword ptr [rsp+98],xmm5
       vmovsd    xmm0,qword ptr [rsp+90]
       vinsertps xmm0,xmm0,dword ptr [rsp+98],28
       vsubps    xmm1,xmm6,xmm0
       vmovaps   [rsp+0B0],xmm1
       vmovss    xmm0,dword ptr [rsp+0B0]
       vmovss    xmm1,dword ptr [rsp+0B4]
       vmovss    xmm2,dword ptr [rsp+0B8]
       inc       r9d
       cmp       r9d,r8d
       jl        near ptr M00_L05
       jmp       near ptr M00_L02
M00_L06:
       mov       ecx,611
       mov       rdx,7FFC5B3936E0
       call      qword ptr [7FFC5B2D7798]
       mov       rcx,rax
       call      qword ptr [7FFC5B5776A8]
       int       3
M00_L07:
       mov       rcx,offset MT_System.ArgumentOutOfRangeException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       ecx,611
       mov       rdx,7FFC5B3936E0
       call      qword ptr [7FFC5B2D7798]
       mov       rsi,rax
       mov       ecx,629
       mov       rdx,7FFC5B3936E0
       call      qword ptr [7FFC5B2D7798]
       mov       r8,rax
       mov       rdx,rsi
       mov       rcx,rbx
       call      qword ptr [7FFC5B4DE5B0]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
M00_L08:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 1325
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Transform()
       sub       rsp,38
       vmovaps   [rsp+20],xmm6
       vmovaps   [rsp+10],xmm7
       mov       rax,[rcx+18]
       mov       [rsp],rax
       mov       eax,[rcx+20]
       mov       [rsp+8],eax
       vmovsd    xmm0,qword ptr [rsp]
       vinsertps xmm0,xmm0,dword ptr [rsp+8],28
       vinsertps xmm0,xmm0,dword ptr [7FFC5B165480],30
       vmovups   xmm1,[7FFC5B165490]
       vpermilps xmm2,xmm1,4E
       vmovshdup xmm3,xmm0
       vbroadcastss xmm3,xmm3
       vmulps    xmm2,xmm3,xmm2
       vmovups   xmm3,[7FFC5B1654A0]
       vpermilps xmm4,xmm1,1B
       vmovaps   xmm5,xmm0
       vbroadcastss xmm5,xmm5
       vmulps    xmm4,xmm5,xmm4
       vmovddup  xmm5,qword ptr [7FFC5B1654B0]
       vshufps   xmm6,xmm0,xmm0,0FF
       vbroadcastss xmm6,xmm6
       vmulps    xmm6,xmm6,xmm1
       vfmadd213ps xmm4,xmm5,xmm6
       vfmadd213ps xmm2,xmm3,xmm4
       vpermilps xmm1,xmm1,0B1
       vunpckhps xmm0,xmm0,xmm0
       vbroadcastss xmm0,xmm0
       vmulps    xmm0,xmm0,xmm1
       vmovups   xmm1,[7FFC5B1654C0]
       vfmadd231ps xmm2,xmm1,xmm0
       vpermilps xmm0,xmm2,4E
       vbroadcastss xmm4,dword ptr [7FFC5B165480]
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
       vmovaps   xmm6,[rsp+20]
       vmovaps   xmm7,[rsp+10]
       add       rsp,38
       ret
; Total bytes of code 261
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Operator_Add()
       sub       rsp,28
       lea       rax,[rcx+18]
       add       rcx,24
       mov       r8,[rax]
       mov       [rsp+18],r8
       mov       r8d,[rax+8]
       mov       [rsp+20],r8d
       vmovsd    xmm0,qword ptr [rsp+18]
       vinsertps xmm0,xmm0,dword ptr [rsp+20],28
       mov       rax,[rcx]
       mov       [rsp+8],rax
       mov       eax,[rcx+8]
       mov       [rsp+10],eax
       vmovsd    xmm1,qword ptr [rsp+8]
       vinsertps xmm1,xmm1,dword ptr [rsp+10],28
       vaddps    xmm0,xmm1,xmm0
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       add       rsp,28
       ret
; Total bytes of code 95
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Operator_Subtract()
       sub       rsp,28
       lea       rax,[rcx+18]
       add       rcx,24
       mov       r8,[rax]
       mov       [rsp+18],r8
       mov       r8d,[rax+8]
       mov       [rsp+20],r8d
       vmovsd    xmm0,qword ptr [rsp+18]
       vinsertps xmm0,xmm0,dword ptr [rsp+20],28
       mov       rax,[rcx]
       mov       [rsp+8],rax
       mov       eax,[rcx+8]
       mov       [rsp+10],eax
       vmovsd    xmm1,qword ptr [rsp+8]
       vinsertps xmm1,xmm1,dword ptr [rsp+10],28
       vsubps    xmm0,xmm0,xmm1
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       add       rsp,28
       ret
; Total bytes of code 95
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Operator_Negate()
       sub       rsp,18
       mov       rax,[rcx+18]
       mov       [rsp+8],rax
       mov       eax,[rcx+20]
       mov       [rsp+10],eax
       vmovsd    xmm0,qword ptr [rsp+8]
       vinsertps xmm0,xmm0,dword ptr [rsp+10],28
       vxorps    xmm0,xmm0,[7FFC5B144250]
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       add       rsp,18
       ret
; Total bytes of code 61
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Operator_Multiply()
       sub       rsp,28
       lea       rax,[rcx+18]
       add       rcx,24
       mov       r8,[rax]
       mov       [rsp+18],r8
       mov       r8d,[rax+8]
       mov       [rsp+20],r8d
       vmovsd    xmm0,qword ptr [rsp+18]
       vinsertps xmm0,xmm0,dword ptr [rsp+20],28
       mov       rax,[rcx]
       mov       [rsp+8],rax
       mov       eax,[rcx+8]
       mov       [rsp+10],eax
       vmovsd    xmm1,qword ptr [rsp+8]
       vinsertps xmm1,xmm1,dword ptr [rsp+10],28
       vmulps    xmm0,xmm1,xmm0
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       add       rsp,28
       ret
; Total bytes of code 95
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Operator_Divide()
       sub       rsp,28
       lea       rax,[rcx+18]
       add       rcx,24
       mov       r8,[rax]
       mov       [rsp+18],r8
       mov       r8d,[rax+8]
       mov       [rsp+20],r8d
       vmovsd    xmm0,qword ptr [rsp+18]
       vinsertps xmm0,xmm0,dword ptr [rsp+20],28
       mov       rax,[rcx]
       mov       [rsp+8],rax
       mov       eax,[rcx+8]
       mov       [rsp+10],eax
       vmovsd    xmm1,qword ptr [rsp+8]
       vinsertps xmm1,xmm1,dword ptr [rsp+10],28
       vdivps    xmm0,xmm0,xmm1
       vmovsd    qword ptr [rdx],xmm0
       vextractps dword ptr [rdx+8],xmm0,2
       mov       rax,rdx
       add       rsp,28
       ret
; Total bytes of code 95
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Operator_Equals()
       sub       rsp,98
       vmovaps   [rsp+80],xmm6
       vmovaps   [rsp+70],xmm7
       vmovaps   [rsp+60],xmm8
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vbroadcastss xmm6,dword ptr [7FFC5B144A00]
       vmovaps   [rsp+50],xmm6
       vmovss    xmm6,dword ptr [rsp+50]
       vmovss    xmm7,dword ptr [rsp+54]
       vmovss    xmm8,dword ptr [rsp+58]
       vmovss    dword ptr [rsp+20],xmm0
       vmovss    dword ptr [rsp+24],xmm1
       vmovss    dword ptr [rsp+28],xmm2
       vmovsd    xmm0,qword ptr [rsp+20]
       vinsertps xmm0,xmm0,dword ptr [rsp+28],28
       vmovss    dword ptr [rsp+10],xmm3
       vmovss    dword ptr [rsp+14],xmm4
       vmovss    dword ptr [rsp+18],xmm5
       vmovsd    xmm1,qword ptr [rsp+10]
       vinsertps xmm1,xmm1,dword ptr [rsp+18],28
       vsubps    xmm0,xmm0,xmm1
       vmovaps   [rsp+30],xmm0
       mov       rax,[rsp+30]
       mov       [rsp+40],rax
       mov       eax,[rsp+38]
       mov       [rsp+48],eax
       vmovsd    xmm0,qword ptr [rsp+40]
       vinsertps xmm0,xmm0,dword ptr [rsp+48],28
       vandps    xmm0,xmm0,[7FFC5B144A10]
       vmovss    dword ptr [rsp],xmm6
       vmovss    dword ptr [rsp+4],xmm7
       vmovss    dword ptr [rsp+8],xmm8
       vmovsd    xmm1,qword ptr [rsp]
       vinsertps xmm1,xmm1,dword ptr [rsp+8],28
       vcmpltps  xmm0,xmm0,xmm1
       vinsertps xmm0,xmm0,xmm0,38
       vpcmpeqd  xmm0,xmm0,[7FFC5B144A20]
       vpmovmskb eax,xmm0
       cmp       eax,0FFFF
       sete      al
       movzx     eax,al
       vmovaps   xmm6,[rsp+80]
       vmovaps   xmm7,[rsp+70]
       vmovaps   xmm8,[rsp+60]
       add       rsp,98
       ret
; Total bytes of code 298
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Operator_NotEquals()
       sub       rsp,98
       vmovaps   [rsp+80],xmm6
       vmovaps   [rsp+70],xmm7
       vmovaps   [rsp+60],xmm8
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vbroadcastss xmm6,dword ptr [7FFC5B164A30]
       vmovaps   [rsp+50],xmm6
       vmovss    xmm6,dword ptr [rsp+50]
       vmovss    xmm7,dword ptr [rsp+54]
       vmovss    xmm8,dword ptr [rsp+58]
       vmovss    dword ptr [rsp+20],xmm0
       vmovss    dword ptr [rsp+24],xmm1
       vmovss    dword ptr [rsp+28],xmm2
       vmovsd    xmm0,qword ptr [rsp+20]
       vinsertps xmm0,xmm0,dword ptr [rsp+28],28
       vmovss    dword ptr [rsp+10],xmm3
       vmovss    dword ptr [rsp+14],xmm4
       vmovss    dword ptr [rsp+18],xmm5
       vmovsd    xmm1,qword ptr [rsp+10]
       vinsertps xmm1,xmm1,dword ptr [rsp+18],28
       vsubps    xmm0,xmm0,xmm1
       vmovaps   [rsp+30],xmm0
       mov       rax,[rsp+30]
       mov       [rsp+40],rax
       mov       eax,[rsp+38]
       mov       [rsp+48],eax
       vmovsd    xmm0,qword ptr [rsp+40]
       vinsertps xmm0,xmm0,dword ptr [rsp+48],28
       vandps    xmm0,xmm0,[7FFC5B164A40]
       vmovss    dword ptr [rsp],xmm6
       vmovss    dword ptr [rsp+4],xmm7
       vmovss    dword ptr [rsp+8],xmm8
       vmovsd    xmm1,qword ptr [rsp]
       vinsertps xmm1,xmm1,dword ptr [rsp+8],28
       vcmpltps  xmm0,xmm0,xmm1
       vinsertps xmm0,xmm0,xmm0,38
       vpcmpeqd  xmm0,xmm0,[7FFC5B164A50]
       vpmovmskb eax,xmm0
       cmp       eax,0FFFF
       setne     al
       movzx     eax,al
       vmovaps   xmm6,[rsp+80]
       vmovaps   xmm7,[rsp+70]
       vmovaps   xmm8,[rsp+60]
       add       rsp,98
       ret
; Total bytes of code 298
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.EqualsStrict()
       sub       rsp,28
       lea       rax,[rcx+18]
       mov       rdx,[rcx+24]
       mov       [rsp+18],rdx
       mov       edx,[rcx+2C]
       mov       [rsp+20],edx
       mov       rcx,[rax]
       mov       [rsp+8],rcx
       mov       ecx,[rax+8]
       mov       [rsp+10],ecx
       vmovsd    xmm0,qword ptr [rsp+8]
       vinsertps xmm0,xmm0,dword ptr [rsp+10],28
       vmovsd    xmm1,qword ptr [rsp+18]
       vinsertps xmm1,xmm1,dword ptr [rsp+20],28
       vcmpeqps  xmm0,xmm1,xmm0
       vinsertps xmm0,xmm0,xmm0,38
       vpcmpeqd  xmm0,xmm0,[7FFC5B1341B0]
       vpmovmskb eax,xmm0
       cmp       eax,0FFFF
       sete      al
       movzx     eax,al
       add       rsp,28
       ret
; Total bytes of code 106
```

