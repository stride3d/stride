## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.IsIdentity()
       vmovups   xmm0,[rcx+14]
       vsubps    xmm0,xmm0,[7FFC5B1346D0]
       vandps    xmm0,xmm0,[7FFC5B1346E0]
       vcmpltps  xmm0,xmm0,[7FFC5B1346F0]
       vpcmpeqd  xmm1,xmm1,xmm1
       vptest    xmm0,xmm1
       setb      al
       movzx     eax,al
       ret
; Total bytes of code 46
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.IsNormalized()
       add       rcx,14
       vmovups   xmm0,[rcx]
       vdpps     xmm0,xmm0,xmm0,0FF
       vsubss    xmm0,xmm0,dword ptr [7FFC5B1640C0]
       vandps    xmm0,xmm0,[7FFC5B1640D0]
       vmovss    xmm1,dword ptr [7FFC5B1640E0]
       vucomiss  xmm1,xmm0
       seta      al
       movzx     eax,al
       ret
; Total bytes of code 49
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Angle()
       sub       rsp,28
       vzeroupper
       add       rcx,14
       vmovss    xmm0,dword ptr [rcx]
       vinsertps xmm0,xmm0,dword ptr [rcx+4],10
       vinsertps xmm0,xmm0,dword ptr [rcx+8],28
       vinsertps xmm0,xmm0,xmm0,38
       vdpps     xmm0,xmm0,xmm0,0FF
       vmovss    xmm1,dword ptr [7FFC5B144158]
       vucomiss  xmm1,xmm0
       ja        short M00_L01
       vmovss    xmm0,dword ptr [rcx+0C]
       call      00007FFCBAD606C0
       vmulss    xmm0,xmm0,dword ptr [7FFC5B14415C]
M00_L00:
       add       rsp,28
       ret
M00_L01:
       vxorps    xmm0,xmm0,xmm0
       jmp       short M00_L00
; Total bytes of code 84
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Axis()
       cmp       [rcx],cl
       add       rcx,14
       jmp       qword ptr [7FFC5B575170]; Stride.Core.Mathematics.Quaternion.get_Axis()
; Total bytes of code 12
```
```assembly
; Stride.Core.Mathematics.Quaternion.get_Axis()
       push      rbx
       sub       rsp,20
       mov       rbx,rdx
       vmovss    xmm0,dword ptr [rcx]
       vinsertps xmm0,xmm0,dword ptr [rcx+4],10
       vinsertps xmm0,xmm0,dword ptr [rcx+8],28
       vinsertps xmm1,xmm0,xmm0,38
       vdpps     xmm1,xmm1,xmm1,0FF
       vmovss    xmm2,dword ptr [7FFC5B144400]
       vucomiss  xmm2,xmm1
       ja        short M01_L01
       vsqrtss   xmm1,xmm1,xmm1
       vbroadcastss xmm1,xmm1
       vdivps    xmm0,xmm0,xmm1
       vmovsd    qword ptr [rbx],xmm0
       vextractps dword ptr [rbx+8],xmm0,2
M01_L00:
       mov       rax,rbx
       add       rsp,20
       pop       rbx
       ret
M01_L01:
       mov       rcx,offset MT_Stride.Core.Mathematics.Vector3
       call      qword ptr [7FFC5B0C5740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,2F1A340D4F0
       mov       rcx,[rax]
       mov       [rbx],rcx
       mov       ecx,[rax+8]
       mov       [rbx+8],ecx
       jmp       short M01_L00
; Total bytes of code 125
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.YawPitchRoll()
       push      rbx
       sub       rsp,0A0
       vzeroupper
       vmovaps   [rsp+90],xmm6
       vmovaps   [rsp+80],xmm7
       vmovaps   [rsp+70],xmm8
       vmovaps   [rsp+60],xmm9
       vmovaps   [rsp+50],xmm10
       vmovaps   [rsp+40],xmm11
       vmovaps   [rsp+30],xmm12
       mov       rbx,rdx
       add       rcx,14
       vmovss    xmm0,dword ptr [rcx]
       vmulss    xmm1,xmm0,xmm0
       vmovss    xmm2,dword ptr [rcx+4]
       vmulss    xmm3,xmm2,xmm2
       vmovss    xmm4,dword ptr [rcx+8]
       vmulss    xmm5,xmm4,xmm4
       vmulss    xmm6,xmm0,xmm2
       vmovss    xmm7,dword ptr [rcx+0C]
       vmulss    xmm8,xmm4,xmm7
       vmulss    xmm9,xmm4,xmm0
       vmulss    xmm10,xmm2,xmm7
       vmulss    xmm2,xmm2,xmm4
       vmulss    xmm0,xmm0,xmm7
       vaddss    xmm4,xmm3,xmm5
       vmovss    xmm7,dword ptr [7FFC5B144AA0]
       vmulss    xmm4,xmm4,xmm7
       vmovss    xmm11,dword ptr [7FFC5B144AA4]
       vsubss    xmm4,xmm11,xmm4
       vaddss    xmm12,xmm6,xmm8
       vmulss    xmm12,xmm12,xmm7
       vsubss    xmm6,xmm6,xmm8
       vmulss    xmm6,xmm6,xmm7
       vaddss    xmm5,xmm5,xmm1
       vmulss    xmm5,xmm5,xmm7
       vsubss    xmm5,xmm11,xmm5
       vmovss    dword ptr [rsp+2C],xmm5
       vaddss    xmm8,xmm9,xmm10
       vmulss    xmm8,xmm8,xmm7
       vsubss    xmm0,xmm2,xmm0
       vmulss    xmm9,xmm0,xmm7
       vaddss    xmm0,xmm3,xmm1
       vmulss    xmm0,xmm0,xmm7
       vsubss    xmm1,xmm11,xmm0
       vmovss    dword ptr [rsp+28],xmm1
       vandps    xmm0,xmm9,[7FFC5B144AB0]
       vsubss    xmm0,xmm0,xmm11
       vandps    xmm0,xmm0,[7FFC5B144AB0]
       vmovss    xmm2,dword ptr [7FFC5B144AC0]
       vucomiss  xmm2,xmm0
       ja        near ptr M00_L01
       vxorps    xmm0,xmm9,[7FFC5B144AD0]
       call      00007FFCBAD606D0
       vmovss    dword ptr [rsp+20],xmm0
       vmovaps   xmm0,xmm8
       vmovss    xmm1,dword ptr [rsp+28]
       call      00007FFCBAD606E0
       vmovss    dword ptr [rsp+24],xmm0
       vmovaps   xmm0,xmm12
       vmovss    xmm1,dword ptr [rsp+2C]
       call      00007FFCBAD606E0
M00_L00:
       vmovss    xmm1,dword ptr [rsp+24]
       vmovss    dword ptr [rbx],xmm1
       vmovss    xmm1,dword ptr [rsp+20]
       vmovss    dword ptr [rbx+4],xmm1
       vmovss    dword ptr [rbx+8],xmm0
       mov       rax,rbx
       vmovaps   xmm6,[rsp+90]
       vmovaps   xmm7,[rsp+80]
       vmovaps   xmm8,[rsp+70]
       vmovaps   xmm9,[rsp+60]
       vmovaps   xmm10,[rsp+50]
       vmovaps   xmm11,[rsp+40]
       vmovaps   xmm12,[rsp+30]
       add       rsp,0A0
       pop       rbx
       ret
M00_L01:
       vxorps    xmm0,xmm0,xmm0
       vucomiss  xmm9,xmm0
       jb        short M00_L03
       vmovss    xmm0,dword ptr [7FFC5B144AE0]
       vmovss    dword ptr [rsp+20],xmm0
       vxorps    xmm0,xmm6,[7FFC5B144AD0]
       vmovaps   xmm1,xmm4
       call      00007FFCBAD606E0
M00_L02:
       vxorps    xmm1,xmm1,xmm1
       vmovss    dword ptr [rsp+24],xmm0
       vmovaps   xmm0,xmm1
       jmp       near ptr M00_L00
M00_L03:
       vmovss    xmm0,dword ptr [7FFC5B144AE4]
       vmovss    dword ptr [rsp+20],xmm0
       vxorps    xmm0,xmm6,[7FFC5B144AD0]
       vmovaps   xmm1,xmm4
       call      00007FFCBAD606E0
       vxorps    xmm0,xmm0,[7FFC5B144AD0]
       jmp       short M00_L02
; Total bytes of code 503
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Conjugate()
       vmovups   xmm0,[rcx+14]
       vmulps    xmm0,xmm0,[7FFC5B1542D0]
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 21
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Invert()
       vmovups   xmm0,[rcx+14]
       vdpps     xmm1,xmm0,xmm0,0FF
       vmulps    xmm0,xmm0,[7FFC5B144310]
       vdivps    xmm0,xmm0,xmm1
       vcmpnleps xmm1,xmm1,[7FFC5B144320]
       vandps    xmm0,xmm1,xmm0
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 44
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Length()
       add       rcx,14
       vmovups   xmm0,[rcx]
       vdpps     xmm0,xmm0,xmm0,0FF
       vsqrtss   xmm0,xmm0,xmm0
       ret
; Total bytes of code 19
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.LengthSquared()
       add       rcx,14
       vmovups   xmm0,[rcx]
       vdpps     xmm0,xmm0,xmm0,0FF
       ret
; Total bytes of code 15
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Normalize()
       vmovups   xmm0,[rcx+14]
       vdpps     xmm1,xmm0,xmm0,0FF
       vmovaps   xmm2,xmm1
       vucomiss  xmm2,dword ptr [7FFC5B144480]
       jbe       short M00_L00
       vsqrtps   xmm1,xmm1
       vdivps    xmm0,xmm0,xmm1
M00_L00:
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 41
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Add()
       vmovups   xmm0,[rcx+14]
       vaddps    xmm0,xmm0,[rcx+24]
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 18
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Subtract()
       vmovups   xmm0,[rcx+14]
       vsubps    xmm0,xmm0,[rcx+24]
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 18
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Multiply()
       lea       rax,[rcx+14]
       add       rcx,24
       vmovups   xmm0,[rax]
       vmovups   xmm1,[rcx]
       vpermilps xmm2,xmm0,4E
       vmovshdup xmm3,xmm1
       vbroadcastss xmm3,xmm3
       vmulps    xmm2,xmm3,xmm2
       vpermilps xmm3,xmm0,1B
       vmovaps   xmm4,xmm1
       vbroadcastss xmm4,xmm4
       vmulps    xmm3,xmm4,xmm3
       vshufps   xmm4,xmm1,xmm1,0FF
       vbroadcastss xmm4,xmm4
       vmulps    xmm4,xmm4,xmm0
       vfmadd132ps xmm3,xmm4,[7FFC5B1445E0]
       vfmadd132ps xmm2,xmm3,[7FFC5B1445F0]
       vpermilps xmm0,xmm0,0B1
       vunpckhps xmm1,xmm1,xmm1
       vbroadcastss xmm1,xmm1
       vmulps    xmm0,xmm1,xmm0
       vfmadd231ps xmm2,xmm0,[7FFC5B144600]
       vmovups   [rdx],xmm2
       mov       rax,rdx
       ret
; Total bytes of code 122
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Negate()
       vmovups   xmm0,[rcx+14]
       vxorps    xmm0,xmm0,[7FFC5B164380]
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 21
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Barycentric()
       push      rbx
       sub       rsp,0E0
       vzeroupper
       vmovaps   [rsp+0D0],xmm6
       vmovaps   [rsp+0C0],xmm7
       vmovaps   [rsp+0B0],xmm8
       vmovaps   [rsp+0A0],xmm9
       vmovaps   [rsp+90],xmm10
       vmovaps   [rsp+80],xmm11
       vmovaps   [rsp+70],xmm12
       vmovaps   [rsp+60],xmm13
       vmovaps   [rsp+50],xmm14
       vmovaps   [rsp+40],xmm15
       mov       rbx,rdx
       vmovups   xmm6,[rcx+14]
       vmovups   xmm7,[rcx+24]
       vmovups   xmm8,[rcx+34]
       vmovss    xmm0,dword ptr [rcx+8]
       vmovss    xmm9,dword ptr [rcx+0C]
       vaddss    xmm10,xmm0,xmm9
       vmovaps   xmm11,xmm10
       vdpps     xmm0,xmm6,xmm7,0FF
       vmovss    xmm12,dword ptr [7FFC5B144CB0]
       vmovaps   xmm1,xmm12
       vxorps    xmm2,xmm2,xmm2
       vucomiss  xmm2,xmm0
       ja        near ptr M00_L06
M00_L00:
       vmovss    xmm13,dword ptr [7FFC5B144CB4]
       vucomiss  xmm0,xmm13
       ja        near ptr M00_L07
       vmovss    dword ptr [rsp+3C],xmm1
       call      qword ptr [7FFC5B5852C0]; System.Single.Acos(Single)
       vmovss    dword ptr [rsp+38],xmm0
       call      qword ptr [7FFC5B5852F0]; System.Single.Sin(Single)
       vdivss    xmm14,xmm12,xmm0
       vsubss    xmm15,xmm12,xmm11
       vmovss    xmm0,dword ptr [rsp+38]
       vmulss    xmm0,xmm15,xmm0
       call      qword ptr [7FFC5B5852F0]; System.Single.Sin(Single)
       vmulss    xmm0,xmm0,xmm14
       vmovss    dword ptr [rsp+34],xmm0
       vmulss    xmm0,xmm11,dword ptr [rsp+38]
       call      qword ptr [7FFC5B5852F0]; System.Single.Sin(Single)
       vmulss    xmm0,xmm0,xmm14
       vmulss    xmm0,xmm0,dword ptr [rsp+3C]
M00_L01:
       vbroadcastss xmm1,dword ptr [rsp+34]
       vmulps    xmm1,xmm1,xmm6
       vbroadcastss xmm0,xmm0
       vmulps    xmm0,xmm0,xmm7
       vaddps    xmm7,xmm0,xmm1
       vmovaps   xmm11,xmm10
       vdpps     xmm0,xmm6,xmm8,0FF
       vmovaps   xmm1,xmm12
       vxorps    xmm2,xmm2,xmm2
       vucomiss  xmm2,xmm0
       ja        near ptr M00_L08
M00_L02:
       vucomiss  xmm0,xmm13
       ja        near ptr M00_L09
       vmovss    dword ptr [rsp+30],xmm1
       call      qword ptr [7FFC5B5852C0]; System.Single.Acos(Single)
       vmovss    dword ptr [rsp+2C],xmm0
       call      qword ptr [7FFC5B5852F0]; System.Single.Sin(Single)
       vdivss    xmm14,xmm12,xmm0
       vmovss    xmm0,dword ptr [rsp+2C]
       vmulss    xmm0,xmm15,xmm0
       call      qword ptr [7FFC5B5852F0]; System.Single.Sin(Single)
       vmulss    xmm15,xmm0,xmm14
       vmulss    xmm0,xmm11,dword ptr [rsp+2C]
       call      qword ptr [7FFC5B5852F0]; System.Single.Sin(Single)
       vmulss    xmm0,xmm0,xmm14
       vmulss    xmm0,xmm0,dword ptr [rsp+30]
M00_L03:
       vbroadcastss xmm1,xmm15
       vmulps    xmm1,xmm1,xmm6
       vbroadcastss xmm0,xmm0
       vmulps    xmm0,xmm0,xmm8
       vaddps    xmm6,xmm0,xmm1
       vdivss    xmm8,xmm9,xmm10
       vdpps     xmm0,xmm7,xmm6,0FF
       vmovaps   xmm1,xmm12
       vxorps    xmm2,xmm2,xmm2
       vucomiss  xmm2,xmm0
       ja        near ptr M00_L10
M00_L04:
       vucomiss  xmm0,xmm13
       ja        near ptr M00_L11
       vmovss    dword ptr [rsp+28],xmm1
       call      qword ptr [7FFC5B5852C0]; System.Single.Acos(Single)
       vmovss    dword ptr [rsp+24],xmm0
       call      qword ptr [7FFC5B5852F0]; System.Single.Sin(Single)
       vdivss    xmm9,xmm12,xmm0
       vsubss    xmm0,xmm12,xmm8
       vmulss    xmm0,xmm0,dword ptr [rsp+24]
       call      qword ptr [7FFC5B5852F0]; System.Single.Sin(Single)
       vmulss    xmm10,xmm0,xmm9
       vmulss    xmm0,xmm8,dword ptr [rsp+24]
       call      qword ptr [7FFC5B5852F0]; System.Single.Sin(Single)
       vmulss    xmm0,xmm0,xmm9
       vmulss    xmm0,xmm0,dword ptr [rsp+28]
M00_L05:
       vbroadcastss xmm1,xmm10
       vmulps    xmm1,xmm1,xmm7
       vbroadcastss xmm0,xmm0
       vmulps    xmm0,xmm0,xmm6
       vaddps    xmm0,xmm0,xmm1
       vmovups   [rbx],xmm0
       mov       rax,rbx
       vmovaps   xmm6,[rsp+0D0]
       vmovaps   xmm7,[rsp+0C0]
       vmovaps   xmm8,[rsp+0B0]
       vmovaps   xmm9,[rsp+0A0]
       vmovaps   xmm10,[rsp+90]
       vmovaps   xmm11,[rsp+80]
       vmovaps   xmm12,[rsp+70]
       vmovaps   xmm13,[rsp+60]
       vmovaps   xmm14,[rsp+50]
       vmovaps   xmm15,[rsp+40]
       add       rsp,0E0
       pop       rbx
       ret
M00_L06:
       vxorps    xmm0,xmm0,[7FFC5B144CC0]
       vmovss    xmm1,dword ptr [7FFC5B144CD0]
       vmovss    dword ptr [rsp+3C],xmm1
       vmovss    xmm1,dword ptr [rsp+3C]
       jmp       near ptr M00_L00
M00_L07:
       vsubss    xmm15,xmm12,xmm11
       vmovaps   xmm0,xmm15
       vmulss    xmm1,xmm11,xmm1
       vmovss    dword ptr [rsp+34],xmm0
       vmovaps   xmm0,xmm1
       jmp       near ptr M00_L01
M00_L08:
       vxorps    xmm0,xmm0,[7FFC5B144CC0]
       vmovss    xmm1,dword ptr [7FFC5B144CD0]
       vmovss    dword ptr [rsp+30],xmm1
       vmovss    xmm1,dword ptr [rsp+30]
       jmp       near ptr M00_L02
M00_L09:
       vmulss    xmm0,xmm11,xmm1
       jmp       near ptr M00_L03
M00_L10:
       vxorps    xmm0,xmm0,[7FFC5B144CC0]
       vmovss    xmm1,dword ptr [7FFC5B144CD0]
       vmovss    dword ptr [rsp+28],xmm1
       vmovss    xmm1,dword ptr [rsp+28]
       jmp       near ptr M00_L04
M00_L11:
       vsubss    xmm10,xmm12,xmm8
       vmulss    xmm0,xmm8,xmm1
       jmp       near ptr M00_L05
; Total bytes of code 803
```
```assembly
; System.Single.Acos(Single)
       sub       rsp,28
       vzeroupper
       call      00007FFCBAD606C0
       nop
       add       rsp,28
       ret
; Total bytes of code 18
```
```assembly
; System.Single.Sin(Single)
       sub       rsp,28
       vzeroupper
       call      00007FFCBAD607C0
       nop
       add       rsp,28
       ret
; Total bytes of code 18
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Dot()
       vmovups   xmm0,[rcx+14]
       vdpps     xmm0,xmm0,[rcx+24],0FF
       ret
; Total bytes of code 13
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.AngleBetween()
       sub       rsp,28
       vzeroupper
       vmovups   xmm0,[rcx+14]
       vdpps     xmm0,xmm0,[rcx+24],0FF
       vandps    xmm0,xmm0,[7FFC5B134290]
       vbroadcastss xmm1,dword ptr [7FFC5B1342A0]
       vminss    xmm0,xmm1,xmm0
       call      00007FFCBAD606C0
       vmulss    xmm0,xmm0,dword ptr [7FFC5B1342A4]
       add       rsp,28
       ret
; Total bytes of code 58
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Exponential()
       push      rbx
       sub       rsp,50
       vzeroupper
       vmovaps   [rsp+40],xmm6
       vmovaps   [rsp+30],xmm7
       mov       rbx,rdx
       vmovups   xmm6,[rcx+14]
       vmovaps   xmm7,xmm6
       vinsertps xmm0,xmm7,xmm7,38
       vdpps     xmm0,xmm0,xmm0,0FF
       vsqrtss   xmm0,xmm0,xmm0
       vmovss    dword ptr [rsp+2C],xmm0
       call      00007FFCBAD607C0
       vandps    xmm1,xmm0,[7FFC5B164600]
       vucomiss  xmm1,dword ptr [7FFC5B164610]
       jb        short M00_L01
       vmovss    xmm1,dword ptr [rsp+2C]
       vdivss    xmm0,xmm0,xmm1
       vbroadcastss xmm0,xmm0
       vmulps    xmm6,xmm0,xmm7
M00_L00:
       vmovaps   xmm0,xmm1
       call      00007FFCBAD60710
       vinsertps xmm0,xmm6,xmm0,30
       vmovups   [rbx],xmm0
       mov       rax,rbx
       vmovaps   xmm6,[rsp+40]
       vmovaps   xmm7,[rsp+30]
       add       rsp,50
       pop       rbx
       ret
M00_L01:
       vmovss    xmm1,dword ptr [rsp+2C]
       jmp       short M00_L00
; Total bytes of code 144
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Lerp()
       vmovups   xmm0,[rcx+14]
       vmovups   xmm1,[rcx+24]
       vmovss    xmm2,dword ptr [rcx+8]
       vmovss    xmm3,dword ptr [7FFC5B154500]
       vsubss    xmm3,xmm3,xmm2
       vbroadcastss xmm3,xmm3
       vdpps     xmm4,xmm0,xmm1,0FF
       vxorps    xmm5,xmm5,xmm5
       vcmpgeps  xmm4,xmm4,xmm5
       vxorps    xmm5,xmm1,[7FFC5B154510]
       vblendvps xmm1,xmm5,xmm1,xmm4
       vbroadcastss xmm2,xmm2
       vmulps    xmm1,xmm2,xmm1
       vfmadd231ps xmm1,xmm3,xmm0
       vdpps     xmm0,xmm1,xmm1,0FF
       vsqrtps   xmm0,xmm0
       vdivps    xmm0,xmm1,xmm0
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 97
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Logarithm()
       push      rbx
       sub       rsp,50
       vzeroupper
       vmovaps   [rsp+40],xmm6
       mov       rbx,rdx
       vmovups   xmm0,[rcx+14]
       vmovups   [rsp+30],xmm0
       vmovups   xmm6,[rsp+30]
       vmovss    xmm0,dword ptr [rsp+3C]
       vandps    xmm1,xmm0,[7FFC5B1545F0]
       vmovss    xmm2,dword ptr [7FFC5B154600]
       vucomiss  xmm2,xmm1
       jbe       short M00_L00
       call      00007FFCBAD606C0
       vmovss    dword ptr [rsp+2C],xmm0
       call      00007FFCBAD607C0
       vandps    xmm1,xmm0,[7FFC5B1545F0]
       vucomiss  xmm1,dword ptr [7FFC5B154604]
       jb        short M00_L00
       vmovss    xmm1,dword ptr [rsp+2C]
       vdivss    xmm0,xmm1,xmm0
       vbroadcastss xmm0,xmm0
       vmulps    xmm6,xmm0,xmm6
M00_L00:
       vmovaps   xmm0,xmm6
       vinsertps xmm0,xmm0,xmm0,38
       vmovups   [rbx],xmm0
       mov       rax,rbx
       vmovaps   xmm6,[rsp+40]
       add       rsp,50
       pop       rbx
       ret
; Total bytes of code 144
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.RotationX()
       push      rbx
       sub       rsp,40
       vzeroupper
       mov       rbx,rdx
       vmovss    xmm0,dword ptr [rcx+8]
       vmulss    xmm0,xmm0,dword ptr [7FFC5B144300]
       vmovss    dword ptr [rsp+2C],xmm0
       call      00007FFCBAD607C0
       vmovups   [rsp+30],xmm0
       vmovss    xmm0,dword ptr [rsp+2C]
       call      00007FFCBAD60710
       vmovups   xmm1,[rsp+30]
       vinsertps xmm0,xmm1,xmm0,36
       vmovups   [rbx],xmm0
       mov       rax,rbx
       add       rsp,40
       pop       rbx
       ret
; Total bytes of code 77
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.RotationY()
       push      rbx
       sub       rsp,40
       vzeroupper
       mov       rbx,rdx
       vmovss    xmm0,dword ptr [rcx+0C]
       vmulss    xmm0,xmm0,dword ptr [7FFC5B154300]
       vmovss    dword ptr [rsp+2C],xmm0
       call      00007FFCBAD607C0
       vmovups   [rsp+30],xmm0
       vmovss    xmm0,dword ptr [rsp+2C]
       call      00007FFCBAD60710
       vmovups   xmm1,[rsp+30]
       vinsertps xmm0,xmm1,xmm0,36
       vmovups   [rbx],xmm0
       mov       rax,rbx
       add       rsp,40
       pop       rbx
       ret
; Total bytes of code 77
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.RotationZ()
       push      rbx
       sub       rsp,40
       vzeroupper
       mov       rbx,rdx
       vmovss    xmm0,dword ptr [rcx+10]
       vmulss    xmm0,xmm0,dword ptr [7FFC5B154300]
       vmovss    dword ptr [rsp+2C],xmm0
       call      00007FFCBAD607C0
       vmovups   [rsp+30],xmm0
       vmovss    xmm0,dword ptr [rsp+2C]
       call      00007FFCBAD60710
       vmovups   xmm1,[rsp+30]
       vinsertps xmm0,xmm1,xmm0,36
       vmovups   [rbx],xmm0
       mov       rax,rbx
       add       rsp,40
       pop       rbx
       ret
; Total bytes of code 77
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.RotationYawPitchRoll()
       push      rbx
       sub       rsp,0A0
       vmovaps   [rsp+90],xmm6
       vmovaps   [rsp+80],xmm7
       vmovaps   [rsp+70],xmm8
       vmovaps   [rsp+60],xmm9
       mov       rbx,rdx
       vmovss    xmm0,dword ptr [rcx+10]
       vinsertps xmm0,xmm0,dword ptr [rcx+0C],10
       vinsertps xmm0,xmm0,dword ptr [rcx+8],28
       vmulps    xmm0,xmm0,[7FFC5B135450]
       vinsertps xmm0,xmm0,xmm0,38
       vmovaps   [rsp+20],xmm0
       lea       rcx,[rsp+30]
       lea       rdx,[rsp+20]
       call      qword ptr [7FFC5B577960]; System.Runtime.Intrinsics.VectorMath.SinCosSingle[[System.Runtime.Intrinsics.Vector128`1[[System.Single, System.Private.CoreLib]], System.Private.CoreLib],[System.Runtime.Intrinsics.Vector128`1[[System.Int32, System.Private.CoreLib]], System.Private.CoreLib],[System.Runtime.Intrinsics.Vector256`1[[System.Double, System.Private.CoreLib]], System.Private.CoreLib],[System.Runtime.Intrinsics.Vector256`1[[System.Int64, System.Private.CoreLib]], System.Private.CoreLib]](System.Runtime.Intrinsics.Vector128`1<Single>)
       vmovups   xmm0,[rsp+30]
       vmovups   xmm1,[rsp+40]
       vmovaps   xmm2,xmm0
       vmovaps   xmm3,xmm1
       vmovshdup xmm4,xmm1
       vmovshdup xmm5,xmm0
       vunpckhps xmm1,xmm1,xmm1
       vunpckhps xmm0,xmm0,xmm0
       vmulss    xmm6,xmm1,xmm5
       vmulss    xmm7,xmm6,xmm3
       vmulss    xmm8,xmm0,xmm4
       vmulss    xmm9,xmm8,xmm2
       vaddss    xmm7,xmm7,xmm9
       vmovaps   xmm9,[rsp+50]
       vinsertps xmm9,xmm9,xmm7,0
       vmulss    xmm7,xmm8,xmm3
       vmulss    xmm6,xmm6,xmm2
       vsubss    xmm6,xmm7,xmm6
       vinsertps xmm9,xmm9,xmm6,10
       vmulss    xmm1,xmm1,xmm4
       vmulss    xmm4,xmm1,xmm2
       vmulss    xmm0,xmm0,xmm5
       vmulss    xmm5,xmm0,xmm3
       vsubss    xmm4,xmm4,xmm5
       vinsertps xmm4,xmm9,xmm4,20
       vmulss    xmm1,xmm1,xmm3
       vmulss    xmm0,xmm0,xmm2
       vaddss    xmm0,xmm1,xmm0
       vinsertps xmm9,xmm4,xmm0,30
       vmovups   [rbx],xmm9
       mov       rax,rbx
       vmovaps   xmm6,[rsp+90]
       vmovaps   xmm7,[rsp+80]
       vmovaps   xmm8,[rsp+70]
       vmovaps   xmm9,[rsp+60]
       add       rsp,0A0
       pop       rbx
       ret
; Total bytes of code 273
```
```assembly
; System.Runtime.Intrinsics.VectorMath.SinCosSingle[[System.Runtime.Intrinsics.Vector128`1[[System.Single, System.Private.CoreLib]], System.Private.CoreLib],[System.Runtime.Intrinsics.Vector128`1[[System.Int32, System.Private.CoreLib]], System.Private.CoreLib],[System.Runtime.Intrinsics.Vector256`1[[System.Double, System.Private.CoreLib]], System.Private.CoreLib],[System.Runtime.Intrinsics.Vector256`1[[System.Int64, System.Private.CoreLib]], System.Private.CoreLib]](System.Runtime.Intrinsics.Vector128`1<Single>)
       push      rsi
       push      rbx
       sub       rsp,0C8
       vmovaps   [rsp+0B0],xmm6
       vmovaps   [rsp+0A0],xmm7
       vmovaps   [rsp+90],xmm8
       mov       rsi,rcx
       mov       rbx,rdx
       vmovups   xmm0,[rbx]
       vandps    xmm6,xmm0,[7FFC5B136920]
       vbroadcastss xmm1,dword ptr [7FFC5B136930]
       vpcmpgtd  xmm1,xmm1,xmm6
       vpcmpeqd  xmm2,xmm2,xmm2
       vptest    xmm1,xmm2
       jae       near ptr M01_L02
       vpcmpgtd  xmm1,xmm6,[7FFC5B136940]
       vptest    xmm1,xmm1
       je        near ptr M01_L01
       vcvtps2pd ymm0,xmm0
       vmulpd    ymm1,ymm0,ymm0
       vmovaps   ymm2,ymm1
       vbroadcastsd ymm3,qword ptr [7FFC5B136950]
       vfmadd213pd ymm3,ymm2,[7FFC5B136960]
       vmulpd    ymm4,ymm2,ymm2
       vbroadcastsd ymm5,qword ptr [7FFC5B136980]
       vfmadd213pd ymm5,ymm2,[7FFC5B1369A0]
       vfmadd213pd ymm3,ymm4,ymm5
       vmulpd    ymm2,ymm0,ymm2
       vfmadd213pd ymm3,ymm2,ymm0
       vcvtpd2ps xmm7,ymm3
       vmovaps   ymm0,ymm1
       vmovaps   ymm2,ymm4
       vmulpd    ymm0,ymm0,[7FFC5B1369C0]
       vbroadcastsd ymm3,qword ptr [7FFC5B1369E0]
       vsubpd    ymm5,ymm3,ymm0
       vsubpd    ymm3,ymm3,ymm5
       vsubpd    ymm0,ymm3,ymm0
       vaddpd    ymm0,ymm0,ymm5
       vbroadcastsd ymm3,qword ptr [7FFC5B1369E8]
       vfmadd213pd ymm3,ymm1,[7FFC5B136A00]
       vbroadcastsd ymm5,qword ptr [7FFC5B136A20]
       vfmadd213pd ymm1,ymm5,[7FFC5B136A40]
       vfmadd213pd ymm3,ymm4,ymm1
       vfmadd213pd ymm2,ymm3,ymm0
       vcvtpd2ps xmm8,ymm2
M01_L00:
       vpcmpgtd  xmm0,xmm6,[7FFC5B136A60]
       vandps    xmm1,xmm7,xmm0
       vandnps   xmm2,xmm0,[rbx]
       vorps     xmm7,xmm2,xmm1
       vandps    xmm1,xmm8,xmm0
       vandnps   xmm0,xmm0,[7FFC5B136A70]
       vorps     xmm8,xmm0,xmm1
       vmovups   [rsi],xmm7
       vmovups   [rsi+10],xmm8
       mov       rax,rsi
       vzeroupper
       vmovaps   xmm6,[rsp+0B0]
       vmovaps   xmm7,[rsp+0A0]
       vmovaps   xmm8,[rsp+90]
       add       rsp,0C8
       pop       rbx
       pop       rsi
       ret
M01_L01:
       vmulps    xmm8,xmm0,xmm0
       vmulps    xmm1,xmm0,xmm8
       vfmadd231ps xmm0,xmm1,[7FFC5B136A80]
       vmovaps   xmm7,xmm0
       vbroadcastss xmm0,dword ptr [7FFC5B136A90]
       vfmadd213ps xmm8,xmm0,[7FFC5B136A70]
       jmp       near ptr M01_L00
M01_L02:
       vbroadcastss xmm1,dword ptr [7FFC5B136A94]
       vpcmpgtd  xmm1,xmm1,xmm6
       vpcmpeqd  xmm2,xmm2,xmm2
       vptest    xmm1,xmm2
       jae       short M01_L03
       vcvtps2pd ymm0,xmm0
       vmovups   [rsp+20],ymm0
       lea       rdx,[rsp+20]
       lea       rcx,[rsp+50]
       call      qword ptr [7FFC5B577A38]
       vmovups   ymm0,[rsp+50]
       vcvtpd2ps xmm7,ymm0
       vmovups   ymm0,[rsp+70]
       vcvtpd2ps xmm8,ymm0
       jmp       near ptr M01_L00
M01_L03:
       mov       rcx,rsi
       mov       rdx,rbx
       vzeroupper
       vmovaps   xmm6,[rsp+0B0]
       vmovaps   xmm7,[rsp+0A0]
       vmovaps   xmm8,[rsp+90]
       add       rsp,0C8
       pop       rbx
       pop       rsi
       jmp       qword ptr [7FFC5B5779A8]
; Total bytes of code 521
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Slerp()
       push      rbx
       sub       rsp,90
       vzeroupper
       vmovaps   [rsp+80],xmm6
       vmovaps   [rsp+70],xmm7
       vmovaps   [rsp+60],xmm8
       vmovaps   [rsp+50],xmm9
       vmovaps   [rsp+40],xmm10
       vmovaps   [rsp+30],xmm11
       mov       rbx,rdx
       lea       rax,[rcx+14]
       lea       rdx,[rcx+24]
       vmovss    xmm6,dword ptr [rcx+8]
       vmovups   xmm7,[rax]
       vmovups   xmm8,[rdx]
       vdpps     xmm0,xmm7,xmm8,0FF
       vmovss    xmm9,dword ptr [7FFC5B1546B0]
       vmovaps   xmm1,xmm9
       vxorps    xmm2,xmm2,xmm2
       vucomiss  xmm2,xmm0
       ja        near ptr M00_L02
M00_L00:
       vucomiss  xmm0,dword ptr [7FFC5B1546B4]
       ja        near ptr M00_L03
       vmovss    dword ptr [rsp+2C],xmm1
       call      qword ptr [7FFC5B5951D0]; System.Single.Acos(Single)
       vmovss    dword ptr [rsp+28],xmm0
       call      qword ptr [7FFC5B595200]; System.Single.Sin(Single)
       vdivss    xmm10,xmm9,xmm0
       vsubss    xmm0,xmm9,xmm6
       vmulss    xmm0,xmm0,dword ptr [rsp+28]
       call      qword ptr [7FFC5B595200]; System.Single.Sin(Single)
       vmulss    xmm11,xmm0,xmm10
       vmulss    xmm0,xmm6,dword ptr [rsp+28]
       call      qword ptr [7FFC5B595200]; System.Single.Sin(Single)
       vmulss    xmm0,xmm0,xmm10
       vmulss    xmm0,xmm0,dword ptr [rsp+2C]
M00_L01:
       vbroadcastss xmm1,xmm11
       vmulps    xmm1,xmm1,xmm7
       vbroadcastss xmm0,xmm0
       vmulps    xmm0,xmm0,xmm8
       vaddps    xmm0,xmm0,xmm1
       vmovups   [rbx],xmm0
       mov       rax,rbx
       vmovaps   xmm6,[rsp+80]
       vmovaps   xmm7,[rsp+70]
       vmovaps   xmm8,[rsp+60]
       vmovaps   xmm9,[rsp+50]
       vmovaps   xmm10,[rsp+40]
       vmovaps   xmm11,[rsp+30]
       add       rsp,90
       pop       rbx
       ret
M00_L02:
       vxorps    xmm0,xmm0,[7FFC5B1546C0]
       vmovss    xmm1,dword ptr [7FFC5B1546D0]
       vmovss    dword ptr [rsp+2C],xmm1
       vmovss    xmm1,dword ptr [rsp+2C]
       jmp       near ptr M00_L00
M00_L03:
       vsubss    xmm11,xmm9,xmm6
       vmulss    xmm0,xmm6,xmm1
       jmp       short M00_L01
; Total bytes of code 314
```
```assembly
; System.Single.Acos(Single)
       sub       rsp,28
       vzeroupper
       call      00007FFCBAD606C0
       nop
       add       rsp,28
       ret
; Total bytes of code 18
```
```assembly
; System.Single.Sin(Single)
       sub       rsp,28
       vzeroupper
       call      00007FFCBAD607C0
       nop
       add       rsp,28
       ret
; Total bytes of code 18
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.RotateTowards()
       push      rbx
       sub       rsp,90
       vzeroupper
       vmovaps   [rsp+80],xmm6
       vmovaps   [rsp+70],xmm7
       vmovaps   [rsp+60],xmm8
       vmovaps   [rsp+50],xmm9
       vmovaps   [rsp+40],xmm10
       vmovaps   [rsp+30],xmm11
       mov       rbx,rdx
       lea       rax,[rcx+14]
       lea       rdx,[rcx+24]
       vmovss    xmm6,dword ptr [rcx+8]
       vmovups   xmm7,[rax]
       vmovups   xmm8,[rdx]
       vdpps     xmm0,xmm7,xmm8,0FF
       vmovss    dword ptr [rsp+24],xmm0
       vandps    xmm1,xmm0,[7FFC5B164AD0]
       vbroadcastss xmm9,dword ptr [7FFC5B164AE0]
       vminss    xmm1,xmm9,xmm1
       vmovaps   xmm0,xmm1
       call      00007FFCBAD606C0
       vmulss    xmm0,xmm0,dword ptr [7FFC5B164AE4]
       vxorps    xmm1,xmm1,xmm1
       vucomiss  xmm0,xmm1
       jp        short M00_L00
       je        near ptr M00_L06
M00_L00:
       vdivss    xmm0,xmm6,xmm0
       vminss    xmm6,xmm9,xmm0
       vmovss    xmm0,dword ptr [rsp+24]
       vmovss    xmm9,dword ptr [7FFC5B164AE0]
       vmovaps   xmm1,xmm9
       vxorps    xmm2,xmm2,xmm2
       vucomiss  xmm2,xmm0
       ja        near ptr M00_L04
M00_L01:
       vucomiss  xmm0,dword ptr [7FFC5B164AE8]
       ja        near ptr M00_L05
       vmovss    dword ptr [rsp+2C],xmm1
       call      qword ptr [7FFC5B5A5230]; System.Single.Acos(Single)
       vmovss    dword ptr [rsp+28],xmm0
       call      qword ptr [7FFC5B5A5260]; System.Single.Sin(Single)
       vdivss    xmm10,xmm9,xmm0
       vsubss    xmm0,xmm9,xmm6
       vmulss    xmm0,xmm0,dword ptr [rsp+28]
       call      qword ptr [7FFC5B5A5260]; System.Single.Sin(Single)
       vmulss    xmm11,xmm0,xmm10
       vmulss    xmm0,xmm6,dword ptr [rsp+28]
       call      qword ptr [7FFC5B5A5260]; System.Single.Sin(Single)
       vmulss    xmm0,xmm0,xmm10
       vmulss    xmm0,xmm0,dword ptr [rsp+2C]
M00_L02:
       vbroadcastss xmm1,xmm11
       vmulps    xmm1,xmm1,xmm7
       vbroadcastss xmm0,xmm0
       vmulps    xmm0,xmm0,xmm8
       vaddps    xmm0,xmm0,xmm1
M00_L03:
       vmovups   [rbx],xmm0
       mov       rax,rbx
       vmovaps   xmm6,[rsp+80]
       vmovaps   xmm7,[rsp+70]
       vmovaps   xmm8,[rsp+60]
       vmovaps   xmm9,[rsp+50]
       vmovaps   xmm10,[rsp+40]
       vmovaps   xmm11,[rsp+30]
       add       rsp,90
       pop       rbx
       ret
M00_L04:
       vxorps    xmm0,xmm0,[7FFC5B164AF0]
       vmovss    xmm1,dword ptr [7FFC5B164B00]
       vmovss    dword ptr [rsp+2C],xmm1
       vmovss    xmm1,dword ptr [rsp+2C]
       jmp       near ptr M00_L01
M00_L05:
       vsubss    xmm11,xmm9,xmm6
       vmulss    xmm0,xmm6,xmm1
       jmp       short M00_L02
M00_L06:
       vmovaps   xmm0,xmm8
       jmp       short M00_L03
; Total bytes of code 395
```
```assembly
; System.Single.Acos(Single)
       sub       rsp,28
       vzeroupper
       call      00007FFCBAD606C0
       nop
       add       rsp,28
       ret
; Total bytes of code 18
```
```assembly
; System.Single.Sin(Single)
       sub       rsp,28
       vzeroupper
       call      00007FFCBAD607C0
       nop
       add       rsp,28
       ret
; Total bytes of code 18
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Squad()
       push      rbx
       sub       rsp,0E0
       vzeroupper
       vmovaps   [rsp+0D0],xmm6
       vmovaps   [rsp+0C0],xmm7
       vmovaps   [rsp+0B0],xmm8
       vmovaps   [rsp+0A0],xmm9
       vmovaps   [rsp+90],xmm10
       vmovaps   [rsp+80],xmm11
       vmovaps   [rsp+70],xmm12
       vmovaps   [rsp+60],xmm13
       vmovaps   [rsp+50],xmm14
       vmovaps   [rsp+40],xmm15
       mov       rbx,rdx
       vmovups   xmm6,[rcx+14]
       vmovups   xmm7,[rcx+24]
       vmovups   xmm8,[rcx+34]
       vmovups   xmm9,[rcx+44]
       vmovss    xmm10,dword ptr [rcx+8]
       vdpps     xmm0,xmm6,xmm9,0FF
       vmovss    xmm11,dword ptr [7FFC5B144CD0]
       vmovaps   xmm1,xmm11
       vxorps    xmm2,xmm2,xmm2
       vucomiss  xmm2,xmm0
       ja        near ptr M00_L06
M00_L00:
       vmovss    xmm12,dword ptr [7FFC5B144CD4]
       vucomiss  xmm0,xmm12
       ja        near ptr M00_L07
       vmovss    dword ptr [rsp+3C],xmm1
       call      qword ptr [7FFC5B5851D0]; System.Single.Acos(Single)
       vmovss    dword ptr [rsp+38],xmm0
       call      qword ptr [7FFC5B585200]; System.Single.Sin(Single)
       vdivss    xmm13,xmm11,xmm0
       vsubss    xmm14,xmm11,xmm10
       vmovss    xmm0,dword ptr [rsp+38]
       vmulss    xmm0,xmm14,xmm0
       call      qword ptr [7FFC5B585200]; System.Single.Sin(Single)
       vmulss    xmm15,xmm0,xmm13
       vmulss    xmm0,xmm10,dword ptr [rsp+38]
       call      qword ptr [7FFC5B585200]; System.Single.Sin(Single)
       vmulss    xmm0,xmm0,xmm13
       vmulss    xmm0,xmm0,dword ptr [rsp+3C]
M00_L01:
       vbroadcastss xmm1,xmm15
       vmulps    xmm1,xmm1,xmm6
       vbroadcastss xmm0,xmm0
       vmulps    xmm0,xmm0,xmm9
       vaddps    xmm6,xmm0,xmm1
       vdpps     xmm0,xmm7,xmm8,0FF
       vmovaps   xmm1,xmm11
       vxorps    xmm2,xmm2,xmm2
       vucomiss  xmm2,xmm0
       ja        near ptr M00_L08
M00_L02:
       vucomiss  xmm0,xmm12
       ja        near ptr M00_L09
       vmovss    dword ptr [rsp+34],xmm1
       call      qword ptr [7FFC5B5851D0]; System.Single.Acos(Single)
       vmovss    dword ptr [rsp+30],xmm0
       call      qword ptr [7FFC5B585200]; System.Single.Sin(Single)
       vdivss    xmm9,xmm11,xmm0
       vmovss    xmm0,dword ptr [rsp+30]
       vmulss    xmm0,xmm14,xmm0
       call      qword ptr [7FFC5B585200]; System.Single.Sin(Single)
       vmulss    xmm13,xmm0,xmm9
       vmulss    xmm0,xmm10,dword ptr [rsp+30]
       call      qword ptr [7FFC5B585200]; System.Single.Sin(Single)
       vmulss    xmm0,xmm0,xmm9
       vmulss    xmm0,xmm0,dword ptr [rsp+34]
M00_L03:
       vbroadcastss xmm1,xmm13
       vmulps    xmm1,xmm1,xmm7
       vbroadcastss xmm0,xmm0
       vmulps    xmm0,xmm0,xmm8
       vaddps    xmm7,xmm0,xmm1
       vaddss    xmm0,xmm10,xmm10
       vmulss    xmm8,xmm0,xmm14
       vdpps     xmm0,xmm6,xmm7,0FF
       vmovaps   xmm1,xmm11
       vxorps    xmm2,xmm2,xmm2
       vucomiss  xmm2,xmm0
       ja        near ptr M00_L10
M00_L04:
       vucomiss  xmm0,xmm12
       ja        near ptr M00_L11
       vmovss    dword ptr [rsp+2C],xmm1
       call      qword ptr [7FFC5B5851D0]; System.Single.Acos(Single)
       vmovss    dword ptr [rsp+28],xmm0
       call      qword ptr [7FFC5B585200]; System.Single.Sin(Single)
       vdivss    xmm9,xmm11,xmm0
       vsubss    xmm0,xmm11,xmm8
       vmulss    xmm0,xmm0,dword ptr [rsp+28]
       call      qword ptr [7FFC5B585200]; System.Single.Sin(Single)
       vmulss    xmm10,xmm0,xmm9
       vmulss    xmm0,xmm8,dword ptr [rsp+28]
       call      qword ptr [7FFC5B585200]; System.Single.Sin(Single)
       vmulss    xmm0,xmm0,xmm9
       vmulss    xmm0,xmm0,dword ptr [rsp+2C]
M00_L05:
       vbroadcastss xmm1,xmm10
       vmulps    xmm1,xmm1,xmm6
       vbroadcastss xmm0,xmm0
       vmulps    xmm0,xmm0,xmm7
       vaddps    xmm0,xmm0,xmm1
       vmovups   [rbx],xmm0
       mov       rax,rbx
       vmovaps   xmm6,[rsp+0D0]
       vmovaps   xmm7,[rsp+0C0]
       vmovaps   xmm8,[rsp+0B0]
       vmovaps   xmm9,[rsp+0A0]
       vmovaps   xmm10,[rsp+90]
       vmovaps   xmm11,[rsp+80]
       vmovaps   xmm12,[rsp+70]
       vmovaps   xmm13,[rsp+60]
       vmovaps   xmm14,[rsp+50]
       vmovaps   xmm15,[rsp+40]
       add       rsp,0E0
       pop       rbx
       ret
M00_L06:
       vxorps    xmm0,xmm0,[7FFC5B144CE0]
       vmovss    xmm1,dword ptr [7FFC5B144CF0]
       vmovss    dword ptr [rsp+3C],xmm1
       vmovss    xmm1,dword ptr [rsp+3C]
       jmp       near ptr M00_L00
M00_L07:
       vsubss    xmm14,xmm11,xmm10
       vmovaps   xmm15,xmm14
       vmulss    xmm0,xmm10,xmm1
       jmp       near ptr M00_L01
M00_L08:
       vxorps    xmm0,xmm0,[7FFC5B144CE0]
       vmovss    xmm1,dword ptr [7FFC5B144CF0]
       vmovss    dword ptr [rsp+34],xmm1
       vmovss    xmm1,dword ptr [rsp+34]
       jmp       near ptr M00_L02
M00_L09:
       vmovaps   xmm13,xmm14
       vmulss    xmm0,xmm10,xmm1
       jmp       near ptr M00_L03
M00_L10:
       vxorps    xmm0,xmm0,[7FFC5B144CE0]
       vmovss    xmm1,dword ptr [7FFC5B144CF0]
       vmovss    dword ptr [rsp+2C],xmm1
       vmovss    xmm1,dword ptr [rsp+2C]
       jmp       near ptr M00_L04
M00_L11:
       vsubss    xmm10,xmm11,xmm8
       vmulss    xmm0,xmm8,xmm1
       jmp       near ptr M00_L05
; Total bytes of code 781
```
```assembly
; System.Single.Acos(Single)
       sub       rsp,28
       vzeroupper
       call      00007FFCBAD606C0
       nop
       add       rsp,28
       ret
; Total bytes of code 18
```
```assembly
; System.Single.Sin(Single)
       sub       rsp,28
       vzeroupper
       call      00007FFCBAD607C0
       nop
       add       rsp,28
       ret
; Total bytes of code 18
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Operator_Add()
       vmovups   xmm0,[rcx+14]
       vaddps    xmm0,xmm0,[rcx+24]
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 18
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Operator_Subtract()
       vmovups   xmm0,[rcx+14]
       vsubps    xmm0,xmm0,[rcx+24]
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 18
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Operator_Negate()
       vmovups   xmm0,[rcx+14]
       vxorps    xmm0,xmm0,[7FFC5B1442D0]
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 21
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Operator_Multiply()
       vmovups   xmm0,[rcx+14]
       vmovups   xmm1,[rcx+24]
       vpermilps xmm2,xmm0,4E
       vmovshdup xmm3,xmm1
       vbroadcastss xmm3,xmm3
       vmulps    xmm2,xmm3,xmm2
       vpermilps xmm3,xmm0,1B
       vmovaps   xmm4,xmm1
       vbroadcastss xmm4,xmm4
       vmulps    xmm3,xmm4,xmm3
       vshufps   xmm4,xmm1,xmm1,0FF
       vbroadcastss xmm4,xmm4
       vmulps    xmm4,xmm4,xmm0
       vfmadd132ps xmm3,xmm4,[7FFC5B154520]
       vfmadd132ps xmm2,xmm3,[7FFC5B154530]
       vpermilps xmm0,xmm0,0B1
       vunpckhps xmm1,xmm1,xmm1
       vbroadcastss xmm1,xmm1
       vmulps    xmm0,xmm1,xmm0
       vfmadd231ps xmm2,xmm0,[7FFC5B154540]
       vmovups   [rdx],xmm2
       mov       rax,rdx
       ret
; Total bytes of code 116
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Operator_Equals()
       vmovups   xmm0,[rcx+14]
       vsubps    xmm0,xmm0,[rcx+24]
       vandps    xmm0,xmm0,[7FFC5B164460]
       vcmpltps  xmm0,xmm0,[7FFC5B164470]
       vpcmpeqd  xmm1,xmm1,xmm1
       vptest    xmm0,xmm1
       setb      al
       movzx     eax,al
       ret
; Total bytes of code 43
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Operator_NotEquals()
       vmovups   xmm0,[rcx+14]
       vsubps    xmm0,xmm0,[rcx+24]
       vandps    xmm0,xmm0,[7FFC5B164460]
       vcmpltps  xmm0,xmm0,[7FFC5B164470]
       vpcmpeqd  xmm1,xmm1,xmm1
       vptest    xmm0,xmm1
       setae     al
       movzx     eax,al
       ret
; Total bytes of code 43
```

