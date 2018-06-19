/*
Copyright (c) 2015 Giovanni Petrantoni

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

//
//  NativeMath.h
//  NativePath
//
//  Created by Giovanni Petrantoni on 11/16/15.
//  Copyright © 2015 Giovanni Petrantoni. All rights reserved.
//

#ifndef nativemath_h
#define nativemath_h

#include "NativePath.h"

#ifdef __cplusplus
extern "C" {
#endif

//ShaderFastMathLib

//
// Using 0 Newton Raphson iterations
// Relative error : ~3.4% over full
// Precise format : ~small float
// 2 ALU
//
extern float npFastRcpSqrtNR0(float inX);

//
// Using 1 Newton Raphson iterations
// Relative error : ~0.2% over full
// Precise format : ~half float
// 6 ALU
//
extern float npFastRcpSqrtNR1(float inX);

//
// Using 2 Newton Raphson iterations
// Relative error : ~4.6e-004%  over full
// Precise format : ~full float
// 9 ALU
//
extern float npFastRcpSqrtNR2(float inX);

//
// Using 0 Newton Raphson iterations
// Relative error : < 0.7% over full
// Precise format : ~small float
// 1 ALU
//
extern float npFastSqrtNR0(float inX);

//
// Use inverse Rcp Sqrt
// Using 1 Newton Raphson iterations
// Relative error : ~0.2% over full
// Precise format : ~half float
// 6 ALU
//
extern float npFastSqrtNR1(float inX);

//
// Use inverse Rcp Sqrt
// Using 2 Newton Raphson iterations
// Relative error : ~4.6e-004%  over full
// Precise format : ~full float
// 9 ALU
//
extern float npFastSqrtNR2(float inX);

//
// Using 0 Newton Raphson iterations
// Relative error : < 0.4% over full
// Precise format : ~small float
// 1 ALU
//
extern float npFastRcpNR0(float inX);

//
// Using 1 Newton Raphson iterations
// Relative error : < 0.02% over full
// Precise format : ~half float
// 3 ALU
//
extern float npFastRcpNR1(float inX);

//
// Using 2 Newton Raphson iterations
// Relative error : < 5.0e-005%  over full
// Precise format : ~full float
// 5 ALU
//
extern float npFastRcpNR2(float inX);

// 4th order polynomial approximation
// 4 VGRP, 16 ALU Full Rate
// 7 * 10^-5 radians precision
// Reference : Handbook of Mathematical Functions (chapter : Elementary Transcendental Functions), M. Abramowitz and I.A. Stegun, Ed.
extern float npAcosFast4(float inX);

// 4th order polynomial approximation
// 4 VGRP, 16 ALU Full Rate
// 7 * 10^-5 radians precision
extern float npAsinFast4(float inX);

// 4th order hyperbolical approximation
// 4 VGRP, 12 ALU Full Rate
// 7 * 10^-5 radians precision
// Reference : Efficient approximations for the arctangent function, Rajan, S. Sichun Wang Inkol, R. Joyal, A., May 2006
extern float npAtanFast4(float inX);

//LoL engine fast math

extern double npLolFabs(double x);
extern double npLolSin(double x);
extern double npLolCos(double x);
extern void npLolSincos(double x, double *sinx, double *cosx);
extern void npLolSincosf(float x, float *sinx, float *cosx);
extern double npLolTan(double x);

//Utility OpenCL vector goodies

static inline float4 npCrossProductF4(float4 left, float4 right)
{
	return left.yzxw * right.zxyw - left.zxyw * right.yzxw;
}

static inline float4 npTransformNormalF4(float4 normal, float4 matrix[4])
{
    return normal.xxxx * matrix[0].xyzw + normal.yyyy * matrix[1].xyzw + normal.zzzz * matrix[2].xyzw + normal.wwww * matrix[3].xyzw;
}

static void npMatrixIdentityF4(float4* outMatrix)
{
    outMatrix[0].yzw = 0.0f;
    outMatrix[1].xzw = 0.0f;
    outMatrix[2].xyw = 0.0f;
    outMatrix[3].xyz = 0.0f;
    outMatrix[0].x = 1.0f;
    outMatrix[1].y = 1.0f;
    outMatrix[2].z = 1.0f;
    outMatrix[3].w = 1.0f;
}

static float npLengthF4(float4 vec)
{
    return sqrtf(vec.x * vec.x + vec.y * vec.y + vec.z * vec.z + vec.w * vec.w);
}

#undef sqrtf
#define sqrtf sqrt

#ifdef __cplusplus
}
#endif

#endif /* nativemath_h */
