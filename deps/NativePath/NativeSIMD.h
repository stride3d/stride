/*
Copyright (c) 2016 Giovanni Petrantoni

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
//  NativeSIMD.h
//  NativePath
//
//  Created by Giovanni Petrantoni on 05/14/16.
//  Copyright © 2016 Giovanni Petrantoni. All rights reserved.
//

#ifndef NativeSIMD_h
#define NativeSIMD_h

#include "NativePath.h"

#ifdef __cplusplus
extern "C" {
#endif

typedef float float32_t;
typedef int8_t poly8_t;
typedef int16_t poly16_t;

typedef __attribute__((__vector_size__(16))) int8_t int8x16_t;
typedef __attribute__((__vector_size__(16))) int16_t int16x8_t;
typedef __attribute__((__vector_size__(16))) int32_t int32x4_t;
typedef __attribute__((__vector_size__(16))) int64_t int64x2_t;
typedef __attribute__((__vector_size__(16))) uint8_t uint8x16_t;
typedef __attribute__((__vector_size__(16))) uint16_t uint16x8_t;
typedef __attribute__((__vector_size__(16))) uint32_t uint32x4_t;
typedef __attribute__((__vector_size__(16))) uint64_t uint64x2_t;
typedef __attribute__((__vector_size__(16))) float32_t float32x4_t;
typedef __attribute__((__vector_size__(16))) poly8_t poly8x16_t;
typedef __attribute__((__vector_size__(16))) poly16_t poly16x8_t;

typedef struct int8x16x2_t {
  int8x16_t val[2];
} int8x16x2_t;

typedef struct int16x8x2_t {
  int16x8_t val[2];
} int16x8x2_t;

typedef struct int32x4x2_t {
  int32x4_t val[2];
} int32x4x2_t;

typedef struct int64x2x2_t {
  int64x2_t val[2];
} int64x2x2_t;

typedef struct uint8x16x2_t {
  uint8x16_t val[2];
} uint8x16x2_t;

typedef struct uint16x8x2_t {
  uint16x8_t val[2];
} uint16x8x2_t;

typedef struct uint32x4x2_t {
  uint32x4_t val[2];
} uint32x4x2_t;

typedef struct uint64x2x2_t {
  uint64x2_t val[2];
} uint64x2x2_t;

typedef struct float32x4x2_t {
  float32x4_t val[2];
} float32x4x2_t;

typedef struct poly8x16x2_t {
  poly8x16_t val[2];
} poly8x16x2_t;

typedef struct poly16x8x2_t {
  poly16x8_t val[2];
} poly16x8x2_t;

typedef struct int8x16x3_t {
  int8x16_t val[3];
} int8x16x3_t;

typedef struct int16x8x3_t {
  int16x8_t val[3];
} int16x8x3_t;

typedef struct int32x4x3_t {
  int32x4_t val[3];
} int32x4x3_t;

typedef struct int64x2x3_t {
  int64x2_t val[3];
} int64x2x3_t;

typedef struct uint8x16x3_t {
  uint8x16_t val[3];
} uint8x16x3_t;

typedef struct uint16x8x3_t {
  uint16x8_t val[3];
} uint16x8x3_t;

typedef struct uint32x4x3_t {
  uint32x4_t val[3];
} uint32x4x3_t;

typedef struct uint64x2x3_t {
  uint64x2_t val[3];
} uint64x2x3_t;

typedef struct float32x4x3_t {
  float32x4_t val[3];
} float32x4x3_t;

typedef struct poly8x16x3_t {
  poly8x16_t val[3];
} poly8x16x3_t;

typedef struct poly16x8x3_t {
  poly16x8_t val[3];
} poly16x8x3_t;

typedef struct int8x16x4_t {
  int8x16_t val[4];
} int8x16x4_t;

typedef struct int16x8x4_t {
  int16x8_t val[4];
} int16x8x4_t;

typedef struct int32x4x4_t {
  int32x4_t val[4];
} int32x4x4_t;

typedef struct int64x2x4_t {
  int64x2_t val[4];
} int64x2x4_t;

typedef struct uint8x16x4_t {
  uint8x16_t val[4];
} uint8x16x4_t;

typedef struct uint16x8x4_t {
  uint16x8_t val[4];
} uint16x8x4_t;

typedef struct uint32x4x4_t {
  uint32x4_t val[4];
} uint32x4x4_t;

typedef struct uint64x2x4_t {
  uint64x2_t val[4];
} uint64x2x4_t;

typedef struct float32x4x4_t {
  float32x4_t val[4];
} float32x4x4_t;

typedef struct poly8x16x4_t {
  poly8x16_t val[4];
} poly8x16x4_t;

typedef struct poly16x8x4_t {
  poly16x8_t val[4];
} poly16x8x4_t;

extern int npCanUseSIMD();

extern int8x16_t np_vaddq_s8(int8x16_t a, int8x16_t b);
#define vaddq_s8 np_vaddq_s8
extern int16x8_t np_vaddq_s16(int16x8_t a, int16x8_t b);
#define vaddq_s16 np_vaddq_s16
extern int32x4_t np_vaddq_s32(int32x4_t a, int32x4_t b);
#define vaddq_s32 np_vaddq_s32
extern int64x2_t np_vaddq_s64(int64x2_t a, int64x2_t b);
#define vaddq_s64 np_vaddq_s64
extern float32x4_t np_vaddq_f32(float32x4_t a, float32x4_t b);
#define vaddq_f32 np_vaddq_f32
extern uint8x16_t np_vaddq_u8(uint8x16_t a, uint8x16_t b);
#define vaddq_u8 np_vaddq_u8
extern uint16x8_t np_vaddq_u16(uint16x8_t a, uint16x8_t b);
#define vaddq_u16 np_vaddq_u16
extern uint32x4_t np_vaddq_u32(uint32x4_t a, uint32x4_t b);
#define vaddq_u32 np_vaddq_u32
extern uint64x2_t np_vaddq_u64(uint64x2_t a, uint64x2_t b);
#define vaddq_u64 np_vaddq_u64
extern int8x16_t np_vhaddq_s8(int8x16_t a, int8x16_t b);
#define vhaddq_s8 np_vhaddq_s8
extern int16x8_t np_vhaddq_s16(int16x8_t a, int16x8_t b);
#define vhaddq_s16 np_vhaddq_s16
extern int32x4_t np_vhaddq_s32(int32x4_t a, int32x4_t b);
#define vhaddq_s32 np_vhaddq_s32
extern uint8x16_t np_vhaddq_u8(uint8x16_t a, uint8x16_t b);
#define vhaddq_u8 np_vhaddq_u8
extern uint16x8_t np_vhaddq_u16(uint16x8_t a, uint16x8_t b);
#define vhaddq_u16 np_vhaddq_u16
extern uint32x4_t np_vhaddq_u32(uint32x4_t a, uint32x4_t b);
#define vhaddq_u32 np_vhaddq_u32
extern int8x16_t np_vrhaddq_s8(int8x16_t a, int8x16_t b);
#define vrhaddq_s8 np_vrhaddq_s8
extern int16x8_t np_vrhaddq_s16(int16x8_t a, int16x8_t b);
#define vrhaddq_s16 np_vrhaddq_s16
extern int32x4_t np_vrhaddq_s32(int32x4_t a, int32x4_t b);
#define vrhaddq_s32 np_vrhaddq_s32
extern uint8x16_t np_vrhaddq_u8(uint8x16_t a, uint8x16_t b);
#define vrhaddq_u8 np_vrhaddq_u8
extern uint16x8_t np_vrhaddq_u16(uint16x8_t a, uint16x8_t b);
#define vrhaddq_u16 np_vrhaddq_u16
extern uint32x4_t np_vrhaddq_u32(uint32x4_t a, uint32x4_t b);
#define vrhaddq_u32 np_vrhaddq_u32
extern int8x16_t np_vqaddq_s8(int8x16_t a, int8x16_t b);
#define vqaddq_s8 np_vqaddq_s8
extern int16x8_t np_vqaddq_s16(int16x8_t a, int16x8_t b);
#define vqaddq_s16 np_vqaddq_s16
extern int32x4_t np_vqaddq_s32(int32x4_t a, int32x4_t b);
#define vqaddq_s32 np_vqaddq_s32
extern int64x2_t np_vqaddq_s64(int64x2_t a, int64x2_t b);
#define vqaddq_s64 np_vqaddq_s64
extern uint8x16_t np_vqaddq_u8(uint8x16_t a, uint8x16_t b);
#define vqaddq_u8 np_vqaddq_u8
extern uint16x8_t np_vqaddq_u16(uint16x8_t a, uint16x8_t b);
#define vqaddq_u16 np_vqaddq_u16
extern uint32x4_t np_vqaddq_u32(uint32x4_t a, uint32x4_t b);
#define vqaddq_u32 np_vqaddq_u32
extern uint64x2_t np_vqaddq_u64(uint64x2_t a, uint64x2_t b);
#define vqaddq_u64 np_vqaddq_u64
extern int8x16_t np_vmulq_s8(int8x16_t a, int8x16_t b);
#define vmulq_s8 np_vmulq_s8
extern int16x8_t np_vmulq_s16(int16x8_t a, int16x8_t b);
#define vmulq_s16 np_vmulq_s16
extern int32x4_t np_vmulq_s32(int32x4_t a, int32x4_t b);
#define vmulq_s32 np_vmulq_s32
extern float32x4_t np_vmulq_f32(float32x4_t a, float32x4_t b);
#define vmulq_f32 np_vmulq_f32
extern uint8x16_t np_vmulq_u8(uint8x16_t a, uint8x16_t b);
#define vmulq_u8 np_vmulq_u8
extern uint16x8_t np_vmulq_u16(uint16x8_t a, uint16x8_t b);
#define vmulq_u16 np_vmulq_u16
extern uint32x4_t np_vmulq_u32(uint32x4_t a, uint32x4_t b);
#define vmulq_u32 np_vmulq_u32
extern poly8x16_t np_vmulq_p8(poly8x16_t a, poly8x16_t b);
#define vmulq_p8 np_vmulq_p8
extern int8x16_t np_vmlaq_s8(int8x16_t a, int8x16_t b, int8x16_t c);
#define vmlaq_s8 np_vmlaq_s8
extern int16x8_t np_vmlaq_s16(int16x8_t a, int16x8_t b, int16x8_t c);
#define vmlaq_s16 np_vmlaq_s16
extern int32x4_t np_vmlaq_s32(int32x4_t a, int32x4_t b, int32x4_t c);
#define vmlaq_s32 np_vmlaq_s32
extern float32x4_t np_vmlaq_f32(float32x4_t a, float32x4_t b, float32x4_t c);
#define vmlaq_f32 np_vmlaq_f32
extern uint8x16_t np_vmlaq_u8(uint8x16_t a, uint8x16_t b, uint8x16_t c);
#define vmlaq_u8 np_vmlaq_u8
extern uint16x8_t np_vmlaq_u16(uint16x8_t a, uint16x8_t b, uint16x8_t c);
#define vmlaq_u16 np_vmlaq_u16
extern uint32x4_t np_vmlaq_u32(uint32x4_t a, uint32x4_t b, uint32x4_t c);
#define vmlaq_u32 np_vmlaq_u32
extern int8x16_t np_vmlsq_s8(int8x16_t a, int8x16_t b, int8x16_t c);
#define vmlsq_s8 np_vmlsq_s8
extern int16x8_t np_vmlsq_s16(int16x8_t a, int16x8_t b, int16x8_t c);
#define vmlsq_s16 np_vmlsq_s16
extern int32x4_t np_vmlsq_s32(int32x4_t a, int32x4_t b, int32x4_t c);
#define vmlsq_s32 np_vmlsq_s32
extern float32x4_t np_vmlsq_f32(float32x4_t a, float32x4_t b, float32x4_t c);
#define vmlsq_f32 np_vmlsq_f32
extern uint8x16_t np_vmlsq_u8(uint8x16_t a, uint8x16_t b, uint8x16_t c);
#define vmlsq_u8 np_vmlsq_u8
extern uint16x8_t np_vmlsq_u16(uint16x8_t a, uint16x8_t b, uint16x8_t c);
#define vmlsq_u16 np_vmlsq_u16
extern uint32x4_t np_vmlsq_u32(uint32x4_t a, uint32x4_t b, uint32x4_t c);
#define vmlsq_u32 np_vmlsq_u32
extern int16x8_t np_vqdmulhq_s16(int16x8_t a, int16x8_t b);
#define vqdmulhq_s16 np_vqdmulhq_s16
extern int32x4_t np_vqdmulhq_s32(int32x4_t a, int32x4_t b);
#define vqdmulhq_s32 np_vqdmulhq_s32
extern int16x8_t np_vqrdmulhq_s16(int16x8_t a, int16x8_t b);
#define vqrdmulhq_s16 np_vqrdmulhq_s16
extern int32x4_t np_vqrdmulhq_s32(int32x4_t a, int32x4_t b);
#define vqrdmulhq_s32 np_vqrdmulhq_s32
extern int8x16_t np_vsubq_s8(int8x16_t a, int8x16_t b);
#define vsubq_s8 np_vsubq_s8
extern int16x8_t np_vsubq_s16(int16x8_t a, int16x8_t b);
#define vsubq_s16 np_vsubq_s16
extern int32x4_t np_vsubq_s32(int32x4_t a, int32x4_t b);
#define vsubq_s32 np_vsubq_s32
extern int64x2_t np_vsubq_s64(int64x2_t a, int64x2_t b);
#define vsubq_s64 np_vsubq_s64
extern float32x4_t np_vsubq_f32(float32x4_t a, float32x4_t b);
#define vsubq_f32 np_vsubq_f32
extern uint8x16_t np_vsubq_u8(uint8x16_t a, uint8x16_t b);
#define vsubq_u8 np_vsubq_u8
extern uint16x8_t np_vsubq_u16(uint16x8_t a, uint16x8_t b);
#define vsubq_u16 np_vsubq_u16
extern uint32x4_t np_vsubq_u32(uint32x4_t a, uint32x4_t b);
#define vsubq_u32 np_vsubq_u32
extern uint64x2_t np_vsubq_u64(uint64x2_t a, uint64x2_t b);
#define vsubq_u64 np_vsubq_u64
extern int8x16_t np_vqsubq_s8(int8x16_t a, int8x16_t b);
#define vqsubq_s8 np_vqsubq_s8
extern int16x8_t np_vqsubq_s16(int16x8_t a, int16x8_t b);
#define vqsubq_s16 np_vqsubq_s16
extern int32x4_t np_vqsubq_s32(int32x4_t a, int32x4_t b);
#define vqsubq_s32 np_vqsubq_s32
extern int64x2_t np_vqsubq_s64(int64x2_t a, int64x2_t b);
#define vqsubq_s64 np_vqsubq_s64
extern uint8x16_t np_vqsubq_u8(uint8x16_t a, uint8x16_t b);
#define vqsubq_u8 np_vqsubq_u8
extern uint16x8_t np_vqsubq_u16(uint16x8_t a, uint16x8_t b);
#define vqsubq_u16 np_vqsubq_u16
extern uint32x4_t np_vqsubq_u32(uint32x4_t a, uint32x4_t b);
#define vqsubq_u32 np_vqsubq_u32
extern uint64x2_t np_vqsubq_u64(uint64x2_t a, uint64x2_t b);
#define vqsubq_u64 np_vqsubq_u64
extern int8x16_t np_vhsubq_s8(int8x16_t a, int8x16_t b);
#define vhsubq_s8 np_vhsubq_s8
extern int16x8_t np_vhsubq_s16(int16x8_t a, int16x8_t b);
#define vhsubq_s16 np_vhsubq_s16
extern int32x4_t np_vhsubq_s32(int32x4_t a, int32x4_t b);
#define vhsubq_s32 np_vhsubq_s32
extern uint8x16_t np_vhsubq_u8(uint8x16_t a, uint8x16_t b);
#define vhsubq_u8 np_vhsubq_u8
extern uint16x8_t np_vhsubq_u16(uint16x8_t a, uint16x8_t b);
#define vhsubq_u16 np_vhsubq_u16
extern uint32x4_t np_vhsubq_u32(uint32x4_t a, uint32x4_t b);
#define vhsubq_u32 np_vhsubq_u32
extern uint8x16_t np_vceqq_s8(int8x16_t a, int8x16_t b);
#define vceqq_s8 np_vceqq_s8
extern uint16x8_t np_vceqq_s16(int16x8_t a, int16x8_t b);
#define vceqq_s16 np_vceqq_s16
extern uint32x4_t np_vceqq_s32(int32x4_t a, int32x4_t b);
#define vceqq_s32 np_vceqq_s32
extern uint32x4_t np_vceqq_f32(float32x4_t a, float32x4_t b);
#define vceqq_f32 np_vceqq_f32
extern uint8x16_t np_vceqq_u8(uint8x16_t a, uint8x16_t b);
#define vceqq_u8 np_vceqq_u8
extern uint16x8_t np_vceqq_u16(uint16x8_t a, uint16x8_t b);
#define vceqq_u16 np_vceqq_u16
extern uint32x4_t np_vceqq_u32(uint32x4_t a, uint32x4_t b);
#define vceqq_u32 np_vceqq_u32
extern uint8x16_t np_vceqq_p8(poly8x16_t a, poly8x16_t b);
#define vceqq_p8 np_vceqq_p8
extern uint8x16_t np_vcgeq_s8(int8x16_t a, int8x16_t b);
#define vcgeq_s8 np_vcgeq_s8
extern uint16x8_t np_vcgeq_s16(int16x8_t a, int16x8_t b);
#define vcgeq_s16 np_vcgeq_s16
extern uint32x4_t np_vcgeq_s32(int32x4_t a, int32x4_t b);
#define vcgeq_s32 np_vcgeq_s32
extern uint32x4_t np_vcgeq_f32(float32x4_t a, float32x4_t b);
#define vcgeq_f32 np_vcgeq_f32
extern uint8x16_t np_vcgeq_u8(uint8x16_t a, uint8x16_t b);
#define vcgeq_u8 np_vcgeq_u8
extern uint16x8_t np_vcgeq_u16(uint16x8_t a, uint16x8_t b);
#define vcgeq_u16 np_vcgeq_u16
extern uint32x4_t np_vcgeq_u32(uint32x4_t a, uint32x4_t b);
#define vcgeq_u32 np_vcgeq_u32
extern uint8x16_t np_vcleq_s8(int8x16_t a, int8x16_t b);
#define vcleq_s8 np_vcleq_s8
extern uint16x8_t np_vcleq_s16(int16x8_t a, int16x8_t b);
#define vcleq_s16 np_vcleq_s16
extern uint32x4_t np_vcleq_s32(int32x4_t a, int32x4_t b);
#define vcleq_s32 np_vcleq_s32
extern uint32x4_t np_vcleq_f32(float32x4_t a, float32x4_t b);
#define vcleq_f32 np_vcleq_f32
extern uint8x16_t np_vcleq_u8(uint8x16_t a, uint8x16_t b);
#define vcleq_u8 np_vcleq_u8
extern uint16x8_t np_vcleq_u16(uint16x8_t a, uint16x8_t b);
#define vcleq_u16 np_vcleq_u16
extern uint32x4_t np_vcleq_u32(uint32x4_t a, uint32x4_t b);
#define vcleq_u32 np_vcleq_u32
extern uint8x16_t np_vcgtq_s8(int8x16_t a, int8x16_t b);
#define vcgtq_s8 np_vcgtq_s8
extern uint16x8_t np_vcgtq_s16(int16x8_t a, int16x8_t b);
#define vcgtq_s16 np_vcgtq_s16
extern uint32x4_t np_vcgtq_s32(int32x4_t a, int32x4_t b);
#define vcgtq_s32 np_vcgtq_s32
extern uint32x4_t np_vcgtq_f32(float32x4_t a, float32x4_t b);
#define vcgtq_f32 np_vcgtq_f32
extern uint8x16_t np_vcgtq_u8(uint8x16_t a, uint8x16_t b);
#define vcgtq_u8 np_vcgtq_u8
extern uint16x8_t np_vcgtq_u16(uint16x8_t a, uint16x8_t b);
#define vcgtq_u16 np_vcgtq_u16
extern uint32x4_t np_vcgtq_u32(uint32x4_t a, uint32x4_t b);
#define vcgtq_u32 np_vcgtq_u32
extern uint8x16_t np_vcltq_s8(int8x16_t a, int8x16_t b);
#define vcltq_s8 np_vcltq_s8
extern uint16x8_t np_vcltq_s16(int16x8_t a, int16x8_t b);
#define vcltq_s16 np_vcltq_s16
extern uint32x4_t np_vcltq_s32(int32x4_t a, int32x4_t b);
#define vcltq_s32 np_vcltq_s32
extern uint32x4_t np_vcltq_f32(float32x4_t a, float32x4_t b);
#define vcltq_f32 np_vcltq_f32
extern uint8x16_t np_vcltq_u8(uint8x16_t a, uint8x16_t b);
#define vcltq_u8 np_vcltq_u8
extern uint16x8_t np_vcltq_u16(uint16x8_t a, uint16x8_t b);
#define vcltq_u16 np_vcltq_u16
extern uint32x4_t np_vcltq_u32(uint32x4_t a, uint32x4_t b);
#define vcltq_u32 np_vcltq_u32
extern uint8x16_t np_vtstq_s8(int8x16_t a, int8x16_t b);
#define vtstq_s8 np_vtstq_s8
extern uint16x8_t np_vtstq_s16(int16x8_t a, int16x8_t b);
#define vtstq_s16 np_vtstq_s16
extern uint32x4_t np_vtstq_s32(int32x4_t a, int32x4_t b);
#define vtstq_s32 np_vtstq_s32
extern uint8x16_t np_vtstq_u8(uint8x16_t a, uint8x16_t b);
#define vtstq_u8 np_vtstq_u8
extern uint16x8_t np_vtstq_u16(uint16x8_t a, uint16x8_t b);
#define vtstq_u16 np_vtstq_u16
extern uint32x4_t np_vtstq_u32(uint32x4_t a, uint32x4_t b);
#define vtstq_u32 np_vtstq_u32
extern uint8x16_t np_vtstq_p8(poly8x16_t a, poly8x16_t b);
#define vtstq_p8 np_vtstq_p8
extern int8x16_t np_vabdq_s8(int8x16_t a, int8x16_t b);
#define vabdq_s8 np_vabdq_s8
extern int16x8_t np_vabdq_s16(int16x8_t a, int16x8_t b);
#define vabdq_s16 np_vabdq_s16
extern int32x4_t np_vabdq_s32(int32x4_t a, int32x4_t b);
#define vabdq_s32 np_vabdq_s32
extern uint8x16_t np_vabdq_u8(uint8x16_t a, uint8x16_t b);
#define vabdq_u8 np_vabdq_u8
extern uint16x8_t np_vabdq_u16(uint16x8_t a, uint16x8_t b);
#define vabdq_u16 np_vabdq_u16
extern uint32x4_t np_vabdq_u32(uint32x4_t a, uint32x4_t b);
#define vabdq_u32 np_vabdq_u32
extern float32x4_t np_vabdq_f32(float32x4_t a, float32x4_t b);
#define vabdq_f32 np_vabdq_f32
extern int8x16_t np_vabaq_s8(int8x16_t a, int8x16_t b, int8x16_t c);
#define vabaq_s8 np_vabaq_s8
extern int16x8_t np_vabaq_s16(int16x8_t a, int16x8_t b, int16x8_t c);
#define vabaq_s16 np_vabaq_s16
extern int32x4_t np_vabaq_s32(int32x4_t a, int32x4_t b, int32x4_t c);
#define vabaq_s32 np_vabaq_s32
extern uint8x16_t np_vabaq_u8(uint8x16_t a, uint8x16_t b, uint8x16_t c);
#define vabaq_u8 np_vabaq_u8
extern uint16x8_t np_vabaq_u16(uint16x8_t a, uint16x8_t b, uint16x8_t c);
#define vabaq_u16 np_vabaq_u16
extern uint32x4_t np_vabaq_u32(uint32x4_t a, uint32x4_t b, uint32x4_t c);
#define vabaq_u32 np_vabaq_u32
extern int8x16_t np_vmaxq_s8(int8x16_t a, int8x16_t b);
#define vmaxq_s8 np_vmaxq_s8
extern int16x8_t np_vmaxq_s16(int16x8_t a, int16x8_t b);
#define vmaxq_s16 np_vmaxq_s16
extern int32x4_t np_vmaxq_s32(int32x4_t a, int32x4_t b);
#define vmaxq_s32 np_vmaxq_s32
extern uint8x16_t np_vmaxq_u8(uint8x16_t a, uint8x16_t b);
#define vmaxq_u8 np_vmaxq_u8
extern uint16x8_t np_vmaxq_u16(uint16x8_t a, uint16x8_t b);
#define vmaxq_u16 np_vmaxq_u16
extern uint32x4_t np_vmaxq_u32(uint32x4_t a, uint32x4_t b);
#define vmaxq_u32 np_vmaxq_u32
extern float32x4_t np_vmaxq_f32(float32x4_t a, float32x4_t b);
#define vmaxq_f32 np_vmaxq_f32
extern int8x16_t np_vminq_s8(int8x16_t a, int8x16_t b);
#define vminq_s8 np_vminq_s8
extern int16x8_t np_vminq_s16(int16x8_t a, int16x8_t b);
#define vminq_s16 np_vminq_s16
extern int32x4_t np_vminq_s32(int32x4_t a, int32x4_t b);
#define vminq_s32 np_vminq_s32
extern uint8x16_t np_vminq_u8(uint8x16_t a, uint8x16_t b);
#define vminq_u8 np_vminq_u8
extern uint16x8_t np_vminq_u16(uint16x8_t a, uint16x8_t b);
#define vminq_u16 np_vminq_u16
extern uint32x4_t np_vminq_u32(uint32x4_t a, uint32x4_t b);
#define vminq_u32 np_vminq_u32
extern float32x4_t np_vminq_f32(float32x4_t a, float32x4_t b);
#define vminq_f32 np_vminq_f32
extern int16x8_t np_vpaddlq_s8(int8x16_t a);
#define vpaddlq_s8 np_vpaddlq_s8
extern int32x4_t np_vpaddlq_s16(int16x8_t a);
#define vpaddlq_s16 np_vpaddlq_s16
extern int64x2_t np_vpaddlq_s32(int32x4_t a);
#define vpaddlq_s32 np_vpaddlq_s32
extern uint16x8_t np_vpaddlq_u8(uint8x16_t a);
#define vpaddlq_u8 np_vpaddlq_u8
extern uint32x4_t np_vpaddlq_u16(uint16x8_t a);
#define vpaddlq_u16 np_vpaddlq_u16
extern uint64x2_t np_vpaddlq_u32(uint32x4_t a);
#define vpaddlq_u32 np_vpaddlq_u32
extern int16x8_t np_vpadalq_s8(int16x8_t a, int8x16_t b);
#define vpadalq_s8 np_vpadalq_s8
extern int32x4_t np_vpadalq_s16(int32x4_t a, int16x8_t b);
#define vpadalq_s16 np_vpadalq_s16
extern int64x2_t np_vpadalq_s32(int64x2_t a, int32x4_t b);
#define vpadalq_s32 np_vpadalq_s32
extern uint16x8_t np_vpadalq_u8(uint16x8_t a, uint8x16_t b);
#define vpadalq_u8 np_vpadalq_u8
extern uint32x4_t np_vpadalq_u16(uint32x4_t a, uint16x8_t b);
#define vpadalq_u16 np_vpadalq_u16
extern uint64x2_t np_vpadalq_u32(uint64x2_t a, uint32x4_t b);
#define vpadalq_u32 np_vpadalq_u32
extern float32x4_t np_vrecpsq_f32(float32x4_t a, float32x4_t b);
#define vrecpsq_f32 np_vrecpsq_f32
extern float32x4_t np_vrsqrtsq_f32(float32x4_t a, float32x4_t b);
#define vrsqrtsq_f32 np_vrsqrtsq_f32
extern int8x16_t np_vshlq_s8(int8x16_t a, int8x16_t b);
#define vshlq_s8 np_vshlq_s8
extern int16x8_t np_vshlq_s16(int16x8_t a, int16x8_t b);
#define vshlq_s16 np_vshlq_s16
extern int32x4_t np_vshlq_s32(int32x4_t a, int32x4_t b);
#define vshlq_s32 np_vshlq_s32
extern int64x2_t np_vshlq_s64(int64x2_t a, int64x2_t b);
#define vshlq_s64 np_vshlq_s64
extern uint8x16_t np_vshlq_u8(uint8x16_t a, int8x16_t b);
#define vshlq_u8 np_vshlq_u8
extern uint16x8_t np_vshlq_u16(uint16x8_t a, int16x8_t b);
#define vshlq_u16 np_vshlq_u16
extern uint32x4_t np_vshlq_u32(uint32x4_t a, int32x4_t b);
#define vshlq_u32 np_vshlq_u32
extern uint64x2_t np_vshlq_u64(uint64x2_t a, int64x2_t b);
#define vshlq_u64 np_vshlq_u64
extern int8x16_t np_vqshlq_s8(int8x16_t a, int8x16_t b);
#define vqshlq_s8 np_vqshlq_s8
extern int16x8_t np_vqshlq_s16(int16x8_t a, int16x8_t b);
#define vqshlq_s16 np_vqshlq_s16
extern int32x4_t np_vqshlq_s32(int32x4_t a, int32x4_t b);
#define vqshlq_s32 np_vqshlq_s32
extern int64x2_t np_vqshlq_s64(int64x2_t a, int64x2_t b);
#define vqshlq_s64 np_vqshlq_s64
extern uint8x16_t np_vqshlq_u8(uint8x16_t a, int8x16_t b);
#define vqshlq_u8 np_vqshlq_u8
extern uint16x8_t np_vqshlq_u16(uint16x8_t a, int16x8_t b);
#define vqshlq_u16 np_vqshlq_u16
extern uint32x4_t np_vqshlq_u32(uint32x4_t a, int32x4_t b);
#define vqshlq_u32 np_vqshlq_u32
extern uint64x2_t np_vqshlq_u64(uint64x2_t a, int64x2_t b);
#define vqshlq_u64 np_vqshlq_u64
extern int8x16_t np_vrshlq_s8(int8x16_t a, int8x16_t b);
#define vrshlq_s8 np_vrshlq_s8
extern int16x8_t np_vrshlq_s16(int16x8_t a, int16x8_t b);
#define vrshlq_s16 np_vrshlq_s16
extern int32x4_t np_vrshlq_s32(int32x4_t a, int32x4_t b);
#define vrshlq_s32 np_vrshlq_s32
extern int64x2_t np_vrshlq_s64(int64x2_t a, int64x2_t b);
#define vrshlq_s64 np_vrshlq_s64
extern uint8x16_t np_vrshlq_u8(uint8x16_t a, int8x16_t b);
#define vrshlq_u8 np_vrshlq_u8
extern uint16x8_t np_vrshlq_u16(uint16x8_t a, int16x8_t b);
#define vrshlq_u16 np_vrshlq_u16
extern uint32x4_t np_vrshlq_u32(uint32x4_t a, int32x4_t b);
#define vrshlq_u32 np_vrshlq_u32
extern uint64x2_t np_vrshlq_u64(uint64x2_t a, int64x2_t b);
#define vrshlq_u64 np_vrshlq_u64
extern int8x16_t np_vqrshlq_s8(int8x16_t a, int8x16_t b);
#define vqrshlq_s8 np_vqrshlq_s8
extern int16x8_t np_vqrshlq_s16(int16x8_t a, int16x8_t b);
#define vqrshlq_s16 np_vqrshlq_s16
extern int32x4_t np_vqrshlq_s32(int32x4_t a, int32x4_t b);
#define vqrshlq_s32 np_vqrshlq_s32
extern int64x2_t np_vqrshlq_s64(int64x2_t a, int64x2_t b);
#define vqrshlq_s64 np_vqrshlq_s64
extern uint8x16_t np_vqrshlq_u8(uint8x16_t a, int8x16_t b);
#define vqrshlq_u8 np_vqrshlq_u8
extern uint16x8_t np_vqrshlq_u16(uint16x8_t a, int16x8_t b);
#define vqrshlq_u16 np_vqrshlq_u16
extern uint32x4_t np_vqrshlq_u32(uint32x4_t a, int32x4_t b);
#define vqrshlq_u32 np_vqrshlq_u32
extern uint64x2_t np_vqrshlq_u64(uint64x2_t a, int64x2_t b);
#define vqrshlq_u64 np_vqrshlq_u64
extern int8x16_t np_vshrq_n_s8_1(int8x16_t a);
extern int8x16_t np_vshrq_n_s8_2(int8x16_t a);
extern int8x16_t np_vshrq_n_s8_3(int8x16_t a);
extern int8x16_t np_vshrq_n_s8_4(int8x16_t a);
extern int8x16_t np_vshrq_n_s8_5(int8x16_t a);
extern int8x16_t np_vshrq_n_s8_6(int8x16_t a);
extern int8x16_t np_vshrq_n_s8_7(int8x16_t a);
extern int8x16_t np_vshrq_n_s8_8(int8x16_t a);
#define vshrq_n_s8(__a, __b) np_vshrq_n_s8_##__b(__a)
extern int16x8_t np_vshrq_n_s16_1(int16x8_t a);
extern int16x8_t np_vshrq_n_s16_2(int16x8_t a);
extern int16x8_t np_vshrq_n_s16_3(int16x8_t a);
extern int16x8_t np_vshrq_n_s16_4(int16x8_t a);
extern int16x8_t np_vshrq_n_s16_5(int16x8_t a);
extern int16x8_t np_vshrq_n_s16_6(int16x8_t a);
extern int16x8_t np_vshrq_n_s16_7(int16x8_t a);
extern int16x8_t np_vshrq_n_s16_8(int16x8_t a);
extern int16x8_t np_vshrq_n_s16_9(int16x8_t a);
extern int16x8_t np_vshrq_n_s16_10(int16x8_t a);
extern int16x8_t np_vshrq_n_s16_11(int16x8_t a);
extern int16x8_t np_vshrq_n_s16_12(int16x8_t a);
extern int16x8_t np_vshrq_n_s16_13(int16x8_t a);
extern int16x8_t np_vshrq_n_s16_14(int16x8_t a);
extern int16x8_t np_vshrq_n_s16_15(int16x8_t a);
extern int16x8_t np_vshrq_n_s16_16(int16x8_t a);
#define vshrq_n_s16(__a, __b) np_vshrq_n_s16_##__b(__a)
extern int32x4_t np_vshrq_n_s32_1(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_2(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_3(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_4(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_5(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_6(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_7(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_8(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_9(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_10(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_11(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_12(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_13(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_14(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_15(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_16(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_17(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_18(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_19(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_20(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_21(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_22(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_23(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_24(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_25(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_26(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_27(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_28(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_29(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_30(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_31(int32x4_t a);
extern int32x4_t np_vshrq_n_s32_32(int32x4_t a);
#define vshrq_n_s32(__a, __b) np_vshrq_n_s32_##__b(__a)
extern int64x2_t np_vshrq_n_s64_1(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_2(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_3(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_4(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_5(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_6(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_7(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_8(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_9(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_10(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_11(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_12(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_13(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_14(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_15(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_16(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_17(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_18(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_19(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_20(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_21(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_22(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_23(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_24(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_25(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_26(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_27(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_28(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_29(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_30(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_31(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_32(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_33(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_34(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_35(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_36(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_37(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_38(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_39(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_40(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_41(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_42(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_43(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_44(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_45(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_46(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_47(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_48(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_49(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_50(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_51(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_52(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_53(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_54(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_55(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_56(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_57(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_58(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_59(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_60(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_61(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_62(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_63(int64x2_t a);
extern int64x2_t np_vshrq_n_s64_64(int64x2_t a);
#define vshrq_n_s64(__a, __b) np_vshrq_n_s64_##__b(__a)
extern uint8x16_t np_vshrq_n_u8_1(uint8x16_t a);
extern uint8x16_t np_vshrq_n_u8_2(uint8x16_t a);
extern uint8x16_t np_vshrq_n_u8_3(uint8x16_t a);
extern uint8x16_t np_vshrq_n_u8_4(uint8x16_t a);
extern uint8x16_t np_vshrq_n_u8_5(uint8x16_t a);
extern uint8x16_t np_vshrq_n_u8_6(uint8x16_t a);
extern uint8x16_t np_vshrq_n_u8_7(uint8x16_t a);
extern uint8x16_t np_vshrq_n_u8_8(uint8x16_t a);
#define vshrq_n_u8(__a, __b) np_vshrq_n_u8_##__b(__a)
extern uint16x8_t np_vshrq_n_u16_1(uint16x8_t a);
extern uint16x8_t np_vshrq_n_u16_2(uint16x8_t a);
extern uint16x8_t np_vshrq_n_u16_3(uint16x8_t a);
extern uint16x8_t np_vshrq_n_u16_4(uint16x8_t a);
extern uint16x8_t np_vshrq_n_u16_5(uint16x8_t a);
extern uint16x8_t np_vshrq_n_u16_6(uint16x8_t a);
extern uint16x8_t np_vshrq_n_u16_7(uint16x8_t a);
extern uint16x8_t np_vshrq_n_u16_8(uint16x8_t a);
extern uint16x8_t np_vshrq_n_u16_9(uint16x8_t a);
extern uint16x8_t np_vshrq_n_u16_10(uint16x8_t a);
extern uint16x8_t np_vshrq_n_u16_11(uint16x8_t a);
extern uint16x8_t np_vshrq_n_u16_12(uint16x8_t a);
extern uint16x8_t np_vshrq_n_u16_13(uint16x8_t a);
extern uint16x8_t np_vshrq_n_u16_14(uint16x8_t a);
extern uint16x8_t np_vshrq_n_u16_15(uint16x8_t a);
extern uint16x8_t np_vshrq_n_u16_16(uint16x8_t a);
#define vshrq_n_u16(__a, __b) np_vshrq_n_u16_##__b(__a)
extern uint32x4_t np_vshrq_n_u32_1(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_2(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_3(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_4(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_5(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_6(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_7(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_8(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_9(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_10(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_11(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_12(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_13(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_14(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_15(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_16(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_17(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_18(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_19(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_20(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_21(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_22(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_23(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_24(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_25(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_26(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_27(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_28(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_29(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_30(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_31(uint32x4_t a);
extern uint32x4_t np_vshrq_n_u32_32(uint32x4_t a);
#define vshrq_n_u32(__a, __b) np_vshrq_n_u32_##__b(__a)
extern uint64x2_t np_vshrq_n_u64_1(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_2(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_3(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_4(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_5(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_6(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_7(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_8(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_9(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_10(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_11(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_12(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_13(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_14(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_15(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_16(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_17(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_18(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_19(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_20(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_21(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_22(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_23(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_24(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_25(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_26(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_27(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_28(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_29(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_30(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_31(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_32(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_33(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_34(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_35(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_36(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_37(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_38(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_39(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_40(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_41(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_42(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_43(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_44(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_45(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_46(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_47(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_48(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_49(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_50(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_51(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_52(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_53(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_54(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_55(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_56(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_57(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_58(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_59(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_60(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_61(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_62(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_63(uint64x2_t a);
extern uint64x2_t np_vshrq_n_u64_64(uint64x2_t a);
#define vshrq_n_u64(__a, __b) np_vshrq_n_u64_##__b(__a)
extern int8x16_t np_vshlq_n_s8_0(int8x16_t a);
extern int8x16_t np_vshlq_n_s8_1(int8x16_t a);
extern int8x16_t np_vshlq_n_s8_2(int8x16_t a);
extern int8x16_t np_vshlq_n_s8_3(int8x16_t a);
extern int8x16_t np_vshlq_n_s8_4(int8x16_t a);
extern int8x16_t np_vshlq_n_s8_5(int8x16_t a);
extern int8x16_t np_vshlq_n_s8_6(int8x16_t a);
extern int8x16_t np_vshlq_n_s8_7(int8x16_t a);
#define vshlq_n_s8(__a, __b) np_vshlq_n_s8_##__b(__a)
extern int16x8_t np_vshlq_n_s16_0(int16x8_t a);
extern int16x8_t np_vshlq_n_s16_1(int16x8_t a);
extern int16x8_t np_vshlq_n_s16_2(int16x8_t a);
extern int16x8_t np_vshlq_n_s16_3(int16x8_t a);
extern int16x8_t np_vshlq_n_s16_4(int16x8_t a);
extern int16x8_t np_vshlq_n_s16_5(int16x8_t a);
extern int16x8_t np_vshlq_n_s16_6(int16x8_t a);
extern int16x8_t np_vshlq_n_s16_7(int16x8_t a);
extern int16x8_t np_vshlq_n_s16_8(int16x8_t a);
extern int16x8_t np_vshlq_n_s16_9(int16x8_t a);
extern int16x8_t np_vshlq_n_s16_10(int16x8_t a);
extern int16x8_t np_vshlq_n_s16_11(int16x8_t a);
extern int16x8_t np_vshlq_n_s16_12(int16x8_t a);
extern int16x8_t np_vshlq_n_s16_13(int16x8_t a);
extern int16x8_t np_vshlq_n_s16_14(int16x8_t a);
extern int16x8_t np_vshlq_n_s16_15(int16x8_t a);
#define vshlq_n_s16(__a, __b) np_vshlq_n_s16_##__b(__a)
extern int32x4_t np_vshlq_n_s32_0(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_1(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_2(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_3(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_4(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_5(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_6(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_7(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_8(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_9(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_10(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_11(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_12(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_13(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_14(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_15(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_16(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_17(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_18(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_19(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_20(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_21(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_22(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_23(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_24(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_25(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_26(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_27(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_28(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_29(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_30(int32x4_t a);
extern int32x4_t np_vshlq_n_s32_31(int32x4_t a);
#define vshlq_n_s32(__a, __b) np_vshlq_n_s32_##__b(__a)
extern int64x2_t np_vshlq_n_s64_0(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_1(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_2(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_3(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_4(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_5(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_6(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_7(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_8(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_9(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_10(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_11(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_12(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_13(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_14(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_15(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_16(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_17(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_18(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_19(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_20(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_21(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_22(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_23(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_24(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_25(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_26(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_27(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_28(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_29(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_30(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_31(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_32(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_33(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_34(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_35(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_36(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_37(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_38(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_39(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_40(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_41(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_42(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_43(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_44(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_45(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_46(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_47(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_48(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_49(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_50(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_51(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_52(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_53(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_54(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_55(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_56(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_57(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_58(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_59(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_60(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_61(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_62(int64x2_t a);
extern int64x2_t np_vshlq_n_s64_63(int64x2_t a);
#define vshlq_n_s64(__a, __b) np_vshlq_n_s64_##__b(__a)
extern uint8x16_t np_vshlq_n_u8_0(uint8x16_t a);
extern uint8x16_t np_vshlq_n_u8_1(uint8x16_t a);
extern uint8x16_t np_vshlq_n_u8_2(uint8x16_t a);
extern uint8x16_t np_vshlq_n_u8_3(uint8x16_t a);
extern uint8x16_t np_vshlq_n_u8_4(uint8x16_t a);
extern uint8x16_t np_vshlq_n_u8_5(uint8x16_t a);
extern uint8x16_t np_vshlq_n_u8_6(uint8x16_t a);
extern uint8x16_t np_vshlq_n_u8_7(uint8x16_t a);
#define vshlq_n_u8(__a, __b) np_vshlq_n_u8_##__b(__a)
extern uint16x8_t np_vshlq_n_u16_0(uint16x8_t a);
extern uint16x8_t np_vshlq_n_u16_1(uint16x8_t a);
extern uint16x8_t np_vshlq_n_u16_2(uint16x8_t a);
extern uint16x8_t np_vshlq_n_u16_3(uint16x8_t a);
extern uint16x8_t np_vshlq_n_u16_4(uint16x8_t a);
extern uint16x8_t np_vshlq_n_u16_5(uint16x8_t a);
extern uint16x8_t np_vshlq_n_u16_6(uint16x8_t a);
extern uint16x8_t np_vshlq_n_u16_7(uint16x8_t a);
extern uint16x8_t np_vshlq_n_u16_8(uint16x8_t a);
extern uint16x8_t np_vshlq_n_u16_9(uint16x8_t a);
extern uint16x8_t np_vshlq_n_u16_10(uint16x8_t a);
extern uint16x8_t np_vshlq_n_u16_11(uint16x8_t a);
extern uint16x8_t np_vshlq_n_u16_12(uint16x8_t a);
extern uint16x8_t np_vshlq_n_u16_13(uint16x8_t a);
extern uint16x8_t np_vshlq_n_u16_14(uint16x8_t a);
extern uint16x8_t np_vshlq_n_u16_15(uint16x8_t a);
#define vshlq_n_u16(__a, __b) np_vshlq_n_u16_##__b(__a)
extern uint32x4_t np_vshlq_n_u32_0(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_1(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_2(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_3(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_4(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_5(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_6(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_7(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_8(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_9(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_10(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_11(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_12(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_13(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_14(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_15(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_16(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_17(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_18(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_19(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_20(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_21(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_22(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_23(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_24(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_25(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_26(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_27(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_28(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_29(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_30(uint32x4_t a);
extern uint32x4_t np_vshlq_n_u32_31(uint32x4_t a);
#define vshlq_n_u32(__a, __b) np_vshlq_n_u32_##__b(__a)
extern uint64x2_t np_vshlq_n_u64_0(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_1(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_2(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_3(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_4(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_5(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_6(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_7(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_8(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_9(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_10(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_11(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_12(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_13(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_14(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_15(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_16(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_17(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_18(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_19(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_20(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_21(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_22(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_23(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_24(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_25(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_26(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_27(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_28(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_29(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_30(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_31(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_32(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_33(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_34(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_35(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_36(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_37(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_38(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_39(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_40(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_41(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_42(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_43(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_44(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_45(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_46(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_47(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_48(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_49(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_50(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_51(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_52(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_53(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_54(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_55(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_56(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_57(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_58(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_59(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_60(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_61(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_62(uint64x2_t a);
extern uint64x2_t np_vshlq_n_u64_63(uint64x2_t a);
#define vshlq_n_u64(__a, __b) np_vshlq_n_u64_##__b(__a)
extern int8x16_t np_vrshrq_n_s8_1(int8x16_t a);
extern int8x16_t np_vrshrq_n_s8_2(int8x16_t a);
extern int8x16_t np_vrshrq_n_s8_3(int8x16_t a);
extern int8x16_t np_vrshrq_n_s8_4(int8x16_t a);
extern int8x16_t np_vrshrq_n_s8_5(int8x16_t a);
extern int8x16_t np_vrshrq_n_s8_6(int8x16_t a);
extern int8x16_t np_vrshrq_n_s8_7(int8x16_t a);
extern int8x16_t np_vrshrq_n_s8_8(int8x16_t a);
#define vrshrq_n_s8(__a, __b) np_vrshrq_n_s8_##__b(__a)
extern int16x8_t np_vrshrq_n_s16_1(int16x8_t a);
extern int16x8_t np_vrshrq_n_s16_2(int16x8_t a);
extern int16x8_t np_vrshrq_n_s16_3(int16x8_t a);
extern int16x8_t np_vrshrq_n_s16_4(int16x8_t a);
extern int16x8_t np_vrshrq_n_s16_5(int16x8_t a);
extern int16x8_t np_vrshrq_n_s16_6(int16x8_t a);
extern int16x8_t np_vrshrq_n_s16_7(int16x8_t a);
extern int16x8_t np_vrshrq_n_s16_8(int16x8_t a);
extern int16x8_t np_vrshrq_n_s16_9(int16x8_t a);
extern int16x8_t np_vrshrq_n_s16_10(int16x8_t a);
extern int16x8_t np_vrshrq_n_s16_11(int16x8_t a);
extern int16x8_t np_vrshrq_n_s16_12(int16x8_t a);
extern int16x8_t np_vrshrq_n_s16_13(int16x8_t a);
extern int16x8_t np_vrshrq_n_s16_14(int16x8_t a);
extern int16x8_t np_vrshrq_n_s16_15(int16x8_t a);
extern int16x8_t np_vrshrq_n_s16_16(int16x8_t a);
#define vrshrq_n_s16(__a, __b) np_vrshrq_n_s16_##__b(__a)
extern int32x4_t np_vrshrq_n_s32_1(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_2(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_3(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_4(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_5(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_6(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_7(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_8(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_9(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_10(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_11(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_12(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_13(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_14(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_15(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_16(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_17(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_18(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_19(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_20(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_21(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_22(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_23(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_24(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_25(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_26(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_27(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_28(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_29(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_30(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_31(int32x4_t a);
extern int32x4_t np_vrshrq_n_s32_32(int32x4_t a);
#define vrshrq_n_s32(__a, __b) np_vrshrq_n_s32_##__b(__a)
extern int64x2_t np_vrshrq_n_s64_1(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_2(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_3(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_4(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_5(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_6(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_7(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_8(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_9(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_10(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_11(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_12(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_13(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_14(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_15(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_16(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_17(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_18(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_19(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_20(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_21(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_22(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_23(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_24(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_25(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_26(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_27(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_28(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_29(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_30(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_31(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_32(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_33(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_34(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_35(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_36(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_37(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_38(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_39(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_40(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_41(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_42(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_43(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_44(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_45(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_46(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_47(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_48(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_49(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_50(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_51(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_52(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_53(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_54(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_55(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_56(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_57(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_58(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_59(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_60(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_61(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_62(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_63(int64x2_t a);
extern int64x2_t np_vrshrq_n_s64_64(int64x2_t a);
#define vrshrq_n_s64(__a, __b) np_vrshrq_n_s64_##__b(__a)
extern uint8x16_t np_vrshrq_n_u8_1(uint8x16_t a);
extern uint8x16_t np_vrshrq_n_u8_2(uint8x16_t a);
extern uint8x16_t np_vrshrq_n_u8_3(uint8x16_t a);
extern uint8x16_t np_vrshrq_n_u8_4(uint8x16_t a);
extern uint8x16_t np_vrshrq_n_u8_5(uint8x16_t a);
extern uint8x16_t np_vrshrq_n_u8_6(uint8x16_t a);
extern uint8x16_t np_vrshrq_n_u8_7(uint8x16_t a);
extern uint8x16_t np_vrshrq_n_u8_8(uint8x16_t a);
#define vrshrq_n_u8(__a, __b) np_vrshrq_n_u8_##__b(__a)
extern uint16x8_t np_vrshrq_n_u16_1(uint16x8_t a);
extern uint16x8_t np_vrshrq_n_u16_2(uint16x8_t a);
extern uint16x8_t np_vrshrq_n_u16_3(uint16x8_t a);
extern uint16x8_t np_vrshrq_n_u16_4(uint16x8_t a);
extern uint16x8_t np_vrshrq_n_u16_5(uint16x8_t a);
extern uint16x8_t np_vrshrq_n_u16_6(uint16x8_t a);
extern uint16x8_t np_vrshrq_n_u16_7(uint16x8_t a);
extern uint16x8_t np_vrshrq_n_u16_8(uint16x8_t a);
extern uint16x8_t np_vrshrq_n_u16_9(uint16x8_t a);
extern uint16x8_t np_vrshrq_n_u16_10(uint16x8_t a);
extern uint16x8_t np_vrshrq_n_u16_11(uint16x8_t a);
extern uint16x8_t np_vrshrq_n_u16_12(uint16x8_t a);
extern uint16x8_t np_vrshrq_n_u16_13(uint16x8_t a);
extern uint16x8_t np_vrshrq_n_u16_14(uint16x8_t a);
extern uint16x8_t np_vrshrq_n_u16_15(uint16x8_t a);
extern uint16x8_t np_vrshrq_n_u16_16(uint16x8_t a);
#define vrshrq_n_u16(__a, __b) np_vrshrq_n_u16_##__b(__a)
extern uint32x4_t np_vrshrq_n_u32_1(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_2(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_3(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_4(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_5(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_6(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_7(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_8(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_9(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_10(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_11(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_12(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_13(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_14(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_15(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_16(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_17(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_18(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_19(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_20(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_21(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_22(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_23(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_24(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_25(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_26(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_27(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_28(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_29(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_30(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_31(uint32x4_t a);
extern uint32x4_t np_vrshrq_n_u32_32(uint32x4_t a);
#define vrshrq_n_u32(__a, __b) np_vrshrq_n_u32_##__b(__a)
extern uint64x2_t np_vrshrq_n_u64_1(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_2(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_3(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_4(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_5(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_6(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_7(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_8(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_9(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_10(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_11(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_12(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_13(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_14(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_15(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_16(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_17(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_18(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_19(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_20(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_21(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_22(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_23(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_24(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_25(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_26(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_27(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_28(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_29(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_30(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_31(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_32(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_33(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_34(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_35(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_36(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_37(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_38(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_39(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_40(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_41(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_42(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_43(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_44(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_45(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_46(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_47(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_48(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_49(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_50(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_51(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_52(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_53(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_54(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_55(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_56(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_57(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_58(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_59(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_60(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_61(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_62(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_63(uint64x2_t a);
extern uint64x2_t np_vrshrq_n_u64_64(uint64x2_t a);
#define vrshrq_n_u64(__a, __b) np_vrshrq_n_u64_##__b(__a)
extern int8x16_t np_vsraq_n_s8_1(int8x16_t a, int8x16_t b);
extern int8x16_t np_vsraq_n_s8_2(int8x16_t a, int8x16_t b);
extern int8x16_t np_vsraq_n_s8_3(int8x16_t a, int8x16_t b);
extern int8x16_t np_vsraq_n_s8_4(int8x16_t a, int8x16_t b);
extern int8x16_t np_vsraq_n_s8_5(int8x16_t a, int8x16_t b);
extern int8x16_t np_vsraq_n_s8_6(int8x16_t a, int8x16_t b);
extern int8x16_t np_vsraq_n_s8_7(int8x16_t a, int8x16_t b);
extern int8x16_t np_vsraq_n_s8_8(int8x16_t a, int8x16_t b);
#define vsraq_n_s8(__a, __b, __c) np_vsraq_n_s8_##__c(__a, __b)
extern int16x8_t np_vsraq_n_s16_1(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsraq_n_s16_2(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsraq_n_s16_3(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsraq_n_s16_4(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsraq_n_s16_5(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsraq_n_s16_6(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsraq_n_s16_7(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsraq_n_s16_8(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsraq_n_s16_9(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsraq_n_s16_10(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsraq_n_s16_11(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsraq_n_s16_12(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsraq_n_s16_13(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsraq_n_s16_14(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsraq_n_s16_15(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsraq_n_s16_16(int16x8_t a, int16x8_t b);
#define vsraq_n_s16(__a, __b, __c) np_vsraq_n_s16_##__c(__a, __b)
extern int32x4_t np_vsraq_n_s32_1(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_2(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_3(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_4(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_5(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_6(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_7(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_8(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_9(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_10(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_11(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_12(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_13(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_14(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_15(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_16(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_17(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_18(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_19(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_20(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_21(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_22(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_23(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_24(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_25(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_26(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_27(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_28(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_29(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_30(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_31(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsraq_n_s32_32(int32x4_t a, int32x4_t b);
#define vsraq_n_s32(__a, __b, __c) np_vsraq_n_s32_##__c(__a, __b)
extern int64x2_t np_vsraq_n_s64_1(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_2(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_3(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_4(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_5(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_6(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_7(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_8(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_9(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_10(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_11(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_12(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_13(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_14(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_15(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_16(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_17(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_18(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_19(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_20(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_21(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_22(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_23(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_24(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_25(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_26(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_27(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_28(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_29(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_30(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_31(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_32(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_33(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_34(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_35(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_36(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_37(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_38(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_39(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_40(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_41(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_42(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_43(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_44(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_45(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_46(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_47(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_48(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_49(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_50(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_51(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_52(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_53(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_54(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_55(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_56(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_57(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_58(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_59(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_60(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_61(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_62(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_63(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsraq_n_s64_64(int64x2_t a, int64x2_t b);
#define vsraq_n_s64(__a, __b, __c) np_vsraq_n_s64_##__c(__a, __b)
extern uint8x16_t np_vsraq_n_u8_1(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vsraq_n_u8_2(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vsraq_n_u8_3(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vsraq_n_u8_4(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vsraq_n_u8_5(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vsraq_n_u8_6(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vsraq_n_u8_7(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vsraq_n_u8_8(uint8x16_t a, uint8x16_t b);
#define vsraq_n_u8(__a, __b, __c) np_vsraq_n_u8_##__c(__a, __b)
extern uint16x8_t np_vsraq_n_u16_1(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsraq_n_u16_2(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsraq_n_u16_3(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsraq_n_u16_4(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsraq_n_u16_5(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsraq_n_u16_6(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsraq_n_u16_7(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsraq_n_u16_8(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsraq_n_u16_9(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsraq_n_u16_10(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsraq_n_u16_11(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsraq_n_u16_12(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsraq_n_u16_13(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsraq_n_u16_14(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsraq_n_u16_15(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsraq_n_u16_16(uint16x8_t a, uint16x8_t b);
#define vsraq_n_u16(__a, __b, __c) np_vsraq_n_u16_##__c(__a, __b)
extern uint32x4_t np_vsraq_n_u32_1(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_2(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_3(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_4(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_5(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_6(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_7(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_8(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_9(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_10(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_11(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_12(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_13(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_14(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_15(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_16(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_17(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_18(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_19(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_20(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_21(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_22(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_23(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_24(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_25(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_26(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_27(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_28(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_29(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_30(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_31(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsraq_n_u32_32(uint32x4_t a, uint32x4_t b);
#define vsraq_n_u32(__a, __b, __c) np_vsraq_n_u32_##__c(__a, __b)
extern uint64x2_t np_vsraq_n_u64_1(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_2(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_3(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_4(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_5(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_6(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_7(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_8(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_9(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_10(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_11(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_12(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_13(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_14(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_15(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_16(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_17(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_18(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_19(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_20(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_21(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_22(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_23(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_24(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_25(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_26(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_27(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_28(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_29(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_30(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_31(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_32(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_33(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_34(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_35(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_36(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_37(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_38(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_39(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_40(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_41(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_42(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_43(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_44(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_45(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_46(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_47(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_48(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_49(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_50(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_51(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_52(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_53(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_54(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_55(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_56(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_57(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_58(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_59(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_60(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_61(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_62(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_63(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsraq_n_u64_64(uint64x2_t a, uint64x2_t b);
#define vsraq_n_u64(__a, __b, __c) np_vsraq_n_u64_##__c(__a, __b)
extern int8x16_t np_vrsraq_n_s8_1(int8x16_t a, int8x16_t b);
extern int8x16_t np_vrsraq_n_s8_2(int8x16_t a, int8x16_t b);
extern int8x16_t np_vrsraq_n_s8_3(int8x16_t a, int8x16_t b);
extern int8x16_t np_vrsraq_n_s8_4(int8x16_t a, int8x16_t b);
extern int8x16_t np_vrsraq_n_s8_5(int8x16_t a, int8x16_t b);
extern int8x16_t np_vrsraq_n_s8_6(int8x16_t a, int8x16_t b);
extern int8x16_t np_vrsraq_n_s8_7(int8x16_t a, int8x16_t b);
extern int8x16_t np_vrsraq_n_s8_8(int8x16_t a, int8x16_t b);
#define vrsraq_n_s8(__a, __b, __c) np_vrsraq_n_s8_##__c(__a, __b)
extern int16x8_t np_vrsraq_n_s16_1(int16x8_t a, int16x8_t b);
extern int16x8_t np_vrsraq_n_s16_2(int16x8_t a, int16x8_t b);
extern int16x8_t np_vrsraq_n_s16_3(int16x8_t a, int16x8_t b);
extern int16x8_t np_vrsraq_n_s16_4(int16x8_t a, int16x8_t b);
extern int16x8_t np_vrsraq_n_s16_5(int16x8_t a, int16x8_t b);
extern int16x8_t np_vrsraq_n_s16_6(int16x8_t a, int16x8_t b);
extern int16x8_t np_vrsraq_n_s16_7(int16x8_t a, int16x8_t b);
extern int16x8_t np_vrsraq_n_s16_8(int16x8_t a, int16x8_t b);
extern int16x8_t np_vrsraq_n_s16_9(int16x8_t a, int16x8_t b);
extern int16x8_t np_vrsraq_n_s16_10(int16x8_t a, int16x8_t b);
extern int16x8_t np_vrsraq_n_s16_11(int16x8_t a, int16x8_t b);
extern int16x8_t np_vrsraq_n_s16_12(int16x8_t a, int16x8_t b);
extern int16x8_t np_vrsraq_n_s16_13(int16x8_t a, int16x8_t b);
extern int16x8_t np_vrsraq_n_s16_14(int16x8_t a, int16x8_t b);
extern int16x8_t np_vrsraq_n_s16_15(int16x8_t a, int16x8_t b);
extern int16x8_t np_vrsraq_n_s16_16(int16x8_t a, int16x8_t b);
#define vrsraq_n_s16(__a, __b, __c) np_vrsraq_n_s16_##__c(__a, __b)
extern int32x4_t np_vrsraq_n_s32_1(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_2(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_3(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_4(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_5(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_6(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_7(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_8(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_9(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_10(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_11(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_12(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_13(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_14(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_15(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_16(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_17(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_18(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_19(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_20(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_21(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_22(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_23(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_24(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_25(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_26(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_27(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_28(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_29(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_30(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_31(int32x4_t a, int32x4_t b);
extern int32x4_t np_vrsraq_n_s32_32(int32x4_t a, int32x4_t b);
#define vrsraq_n_s32(__a, __b, __c) np_vrsraq_n_s32_##__c(__a, __b)
extern int64x2_t np_vrsraq_n_s64_1(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_2(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_3(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_4(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_5(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_6(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_7(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_8(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_9(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_10(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_11(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_12(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_13(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_14(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_15(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_16(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_17(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_18(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_19(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_20(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_21(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_22(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_23(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_24(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_25(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_26(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_27(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_28(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_29(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_30(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_31(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_32(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_33(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_34(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_35(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_36(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_37(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_38(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_39(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_40(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_41(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_42(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_43(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_44(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_45(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_46(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_47(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_48(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_49(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_50(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_51(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_52(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_53(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_54(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_55(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_56(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_57(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_58(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_59(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_60(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_61(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_62(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_63(int64x2_t a, int64x2_t b);
extern int64x2_t np_vrsraq_n_s64_64(int64x2_t a, int64x2_t b);
#define vrsraq_n_s64(__a, __b, __c) np_vrsraq_n_s64_##__c(__a, __b)
extern uint8x16_t np_vrsraq_n_u8_1(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vrsraq_n_u8_2(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vrsraq_n_u8_3(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vrsraq_n_u8_4(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vrsraq_n_u8_5(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vrsraq_n_u8_6(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vrsraq_n_u8_7(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vrsraq_n_u8_8(uint8x16_t a, uint8x16_t b);
#define vrsraq_n_u8(__a, __b, __c) np_vrsraq_n_u8_##__c(__a, __b)
extern uint16x8_t np_vrsraq_n_u16_1(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vrsraq_n_u16_2(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vrsraq_n_u16_3(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vrsraq_n_u16_4(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vrsraq_n_u16_5(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vrsraq_n_u16_6(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vrsraq_n_u16_7(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vrsraq_n_u16_8(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vrsraq_n_u16_9(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vrsraq_n_u16_10(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vrsraq_n_u16_11(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vrsraq_n_u16_12(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vrsraq_n_u16_13(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vrsraq_n_u16_14(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vrsraq_n_u16_15(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vrsraq_n_u16_16(uint16x8_t a, uint16x8_t b);
#define vrsraq_n_u16(__a, __b, __c) np_vrsraq_n_u16_##__c(__a, __b)
extern uint32x4_t np_vrsraq_n_u32_1(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_2(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_3(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_4(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_5(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_6(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_7(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_8(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_9(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_10(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_11(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_12(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_13(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_14(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_15(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_16(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_17(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_18(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_19(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_20(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_21(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_22(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_23(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_24(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_25(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_26(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_27(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_28(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_29(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_30(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_31(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vrsraq_n_u32_32(uint32x4_t a, uint32x4_t b);
#define vrsraq_n_u32(__a, __b, __c) np_vrsraq_n_u32_##__c(__a, __b)
extern uint64x2_t np_vrsraq_n_u64_1(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_2(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_3(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_4(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_5(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_6(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_7(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_8(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_9(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_10(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_11(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_12(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_13(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_14(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_15(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_16(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_17(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_18(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_19(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_20(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_21(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_22(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_23(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_24(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_25(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_26(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_27(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_28(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_29(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_30(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_31(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_32(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_33(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_34(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_35(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_36(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_37(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_38(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_39(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_40(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_41(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_42(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_43(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_44(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_45(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_46(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_47(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_48(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_49(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_50(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_51(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_52(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_53(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_54(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_55(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_56(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_57(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_58(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_59(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_60(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_61(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_62(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_63(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vrsraq_n_u64_64(uint64x2_t a, uint64x2_t b);
#define vrsraq_n_u64(__a, __b, __c) np_vrsraq_n_u64_##__c(__a, __b)
extern int8x16_t np_vqshlq_n_s8_0(int8x16_t a);
extern int8x16_t np_vqshlq_n_s8_1(int8x16_t a);
extern int8x16_t np_vqshlq_n_s8_2(int8x16_t a);
extern int8x16_t np_vqshlq_n_s8_3(int8x16_t a);
extern int8x16_t np_vqshlq_n_s8_4(int8x16_t a);
extern int8x16_t np_vqshlq_n_s8_5(int8x16_t a);
extern int8x16_t np_vqshlq_n_s8_6(int8x16_t a);
extern int8x16_t np_vqshlq_n_s8_7(int8x16_t a);
#define vqshlq_n_s8(__a, __b) np_vqshlq_n_s8_##__b(__a)
extern int16x8_t np_vqshlq_n_s16_0(int16x8_t a);
extern int16x8_t np_vqshlq_n_s16_1(int16x8_t a);
extern int16x8_t np_vqshlq_n_s16_2(int16x8_t a);
extern int16x8_t np_vqshlq_n_s16_3(int16x8_t a);
extern int16x8_t np_vqshlq_n_s16_4(int16x8_t a);
extern int16x8_t np_vqshlq_n_s16_5(int16x8_t a);
extern int16x8_t np_vqshlq_n_s16_6(int16x8_t a);
extern int16x8_t np_vqshlq_n_s16_7(int16x8_t a);
extern int16x8_t np_vqshlq_n_s16_8(int16x8_t a);
extern int16x8_t np_vqshlq_n_s16_9(int16x8_t a);
extern int16x8_t np_vqshlq_n_s16_10(int16x8_t a);
extern int16x8_t np_vqshlq_n_s16_11(int16x8_t a);
extern int16x8_t np_vqshlq_n_s16_12(int16x8_t a);
extern int16x8_t np_vqshlq_n_s16_13(int16x8_t a);
extern int16x8_t np_vqshlq_n_s16_14(int16x8_t a);
extern int16x8_t np_vqshlq_n_s16_15(int16x8_t a);
#define vqshlq_n_s16(__a, __b) np_vqshlq_n_s16_##__b(__a)
extern int32x4_t np_vqshlq_n_s32_0(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_1(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_2(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_3(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_4(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_5(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_6(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_7(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_8(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_9(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_10(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_11(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_12(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_13(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_14(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_15(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_16(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_17(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_18(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_19(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_20(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_21(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_22(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_23(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_24(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_25(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_26(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_27(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_28(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_29(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_30(int32x4_t a);
extern int32x4_t np_vqshlq_n_s32_31(int32x4_t a);
#define vqshlq_n_s32(__a, __b) np_vqshlq_n_s32_##__b(__a)
extern int64x2_t np_vqshlq_n_s64_0(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_1(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_2(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_3(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_4(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_5(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_6(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_7(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_8(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_9(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_10(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_11(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_12(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_13(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_14(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_15(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_16(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_17(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_18(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_19(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_20(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_21(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_22(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_23(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_24(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_25(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_26(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_27(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_28(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_29(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_30(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_31(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_32(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_33(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_34(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_35(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_36(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_37(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_38(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_39(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_40(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_41(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_42(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_43(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_44(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_45(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_46(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_47(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_48(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_49(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_50(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_51(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_52(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_53(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_54(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_55(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_56(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_57(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_58(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_59(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_60(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_61(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_62(int64x2_t a);
extern int64x2_t np_vqshlq_n_s64_63(int64x2_t a);
#define vqshlq_n_s64(__a, __b) np_vqshlq_n_s64_##__b(__a)
extern uint8x16_t np_vqshlq_n_u8_0(uint8x16_t a);
extern uint8x16_t np_vqshlq_n_u8_1(uint8x16_t a);
extern uint8x16_t np_vqshlq_n_u8_2(uint8x16_t a);
extern uint8x16_t np_vqshlq_n_u8_3(uint8x16_t a);
extern uint8x16_t np_vqshlq_n_u8_4(uint8x16_t a);
extern uint8x16_t np_vqshlq_n_u8_5(uint8x16_t a);
extern uint8x16_t np_vqshlq_n_u8_6(uint8x16_t a);
extern uint8x16_t np_vqshlq_n_u8_7(uint8x16_t a);
#define vqshlq_n_u8(__a, __b) np_vqshlq_n_u8_##__b(__a)
extern uint16x8_t np_vqshlq_n_u16_0(uint16x8_t a);
extern uint16x8_t np_vqshlq_n_u16_1(uint16x8_t a);
extern uint16x8_t np_vqshlq_n_u16_2(uint16x8_t a);
extern uint16x8_t np_vqshlq_n_u16_3(uint16x8_t a);
extern uint16x8_t np_vqshlq_n_u16_4(uint16x8_t a);
extern uint16x8_t np_vqshlq_n_u16_5(uint16x8_t a);
extern uint16x8_t np_vqshlq_n_u16_6(uint16x8_t a);
extern uint16x8_t np_vqshlq_n_u16_7(uint16x8_t a);
extern uint16x8_t np_vqshlq_n_u16_8(uint16x8_t a);
extern uint16x8_t np_vqshlq_n_u16_9(uint16x8_t a);
extern uint16x8_t np_vqshlq_n_u16_10(uint16x8_t a);
extern uint16x8_t np_vqshlq_n_u16_11(uint16x8_t a);
extern uint16x8_t np_vqshlq_n_u16_12(uint16x8_t a);
extern uint16x8_t np_vqshlq_n_u16_13(uint16x8_t a);
extern uint16x8_t np_vqshlq_n_u16_14(uint16x8_t a);
extern uint16x8_t np_vqshlq_n_u16_15(uint16x8_t a);
#define vqshlq_n_u16(__a, __b) np_vqshlq_n_u16_##__b(__a)
extern uint32x4_t np_vqshlq_n_u32_0(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_1(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_2(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_3(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_4(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_5(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_6(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_7(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_8(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_9(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_10(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_11(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_12(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_13(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_14(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_15(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_16(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_17(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_18(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_19(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_20(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_21(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_22(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_23(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_24(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_25(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_26(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_27(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_28(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_29(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_30(uint32x4_t a);
extern uint32x4_t np_vqshlq_n_u32_31(uint32x4_t a);
#define vqshlq_n_u32(__a, __b) np_vqshlq_n_u32_##__b(__a)
extern uint64x2_t np_vqshlq_n_u64_0(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_1(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_2(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_3(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_4(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_5(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_6(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_7(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_8(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_9(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_10(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_11(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_12(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_13(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_14(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_15(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_16(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_17(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_18(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_19(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_20(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_21(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_22(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_23(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_24(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_25(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_26(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_27(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_28(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_29(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_30(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_31(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_32(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_33(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_34(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_35(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_36(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_37(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_38(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_39(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_40(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_41(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_42(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_43(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_44(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_45(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_46(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_47(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_48(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_49(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_50(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_51(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_52(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_53(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_54(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_55(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_56(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_57(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_58(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_59(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_60(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_61(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_62(uint64x2_t a);
extern uint64x2_t np_vqshlq_n_u64_63(uint64x2_t a);
#define vqshlq_n_u64(__a, __b) np_vqshlq_n_u64_##__b(__a)
extern uint8x16_t np_vqshluq_n_s8_0(int8x16_t a);
extern uint8x16_t np_vqshluq_n_s8_1(int8x16_t a);
extern uint8x16_t np_vqshluq_n_s8_2(int8x16_t a);
extern uint8x16_t np_vqshluq_n_s8_3(int8x16_t a);
extern uint8x16_t np_vqshluq_n_s8_4(int8x16_t a);
extern uint8x16_t np_vqshluq_n_s8_5(int8x16_t a);
extern uint8x16_t np_vqshluq_n_s8_6(int8x16_t a);
extern uint8x16_t np_vqshluq_n_s8_7(int8x16_t a);
#define vqshluq_n_s8(__a, __b) np_vqshluq_n_s8_##__b(__a)
extern uint16x8_t np_vqshluq_n_s16_0(int16x8_t a);
extern uint16x8_t np_vqshluq_n_s16_1(int16x8_t a);
extern uint16x8_t np_vqshluq_n_s16_2(int16x8_t a);
extern uint16x8_t np_vqshluq_n_s16_3(int16x8_t a);
extern uint16x8_t np_vqshluq_n_s16_4(int16x8_t a);
extern uint16x8_t np_vqshluq_n_s16_5(int16x8_t a);
extern uint16x8_t np_vqshluq_n_s16_6(int16x8_t a);
extern uint16x8_t np_vqshluq_n_s16_7(int16x8_t a);
extern uint16x8_t np_vqshluq_n_s16_8(int16x8_t a);
extern uint16x8_t np_vqshluq_n_s16_9(int16x8_t a);
extern uint16x8_t np_vqshluq_n_s16_10(int16x8_t a);
extern uint16x8_t np_vqshluq_n_s16_11(int16x8_t a);
extern uint16x8_t np_vqshluq_n_s16_12(int16x8_t a);
extern uint16x8_t np_vqshluq_n_s16_13(int16x8_t a);
extern uint16x8_t np_vqshluq_n_s16_14(int16x8_t a);
extern uint16x8_t np_vqshluq_n_s16_15(int16x8_t a);
#define vqshluq_n_s16(__a, __b) np_vqshluq_n_s16_##__b(__a)
extern uint32x4_t np_vqshluq_n_s32_0(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_1(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_2(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_3(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_4(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_5(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_6(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_7(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_8(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_9(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_10(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_11(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_12(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_13(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_14(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_15(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_16(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_17(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_18(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_19(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_20(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_21(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_22(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_23(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_24(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_25(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_26(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_27(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_28(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_29(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_30(int32x4_t a);
extern uint32x4_t np_vqshluq_n_s32_31(int32x4_t a);
#define vqshluq_n_s32(__a, __b) np_vqshluq_n_s32_##__b(__a)
extern uint64x2_t np_vqshluq_n_s64_0(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_1(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_2(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_3(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_4(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_5(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_6(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_7(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_8(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_9(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_10(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_11(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_12(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_13(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_14(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_15(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_16(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_17(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_18(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_19(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_20(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_21(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_22(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_23(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_24(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_25(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_26(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_27(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_28(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_29(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_30(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_31(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_32(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_33(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_34(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_35(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_36(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_37(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_38(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_39(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_40(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_41(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_42(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_43(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_44(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_45(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_46(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_47(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_48(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_49(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_50(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_51(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_52(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_53(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_54(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_55(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_56(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_57(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_58(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_59(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_60(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_61(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_62(int64x2_t a);
extern uint64x2_t np_vqshluq_n_s64_63(int64x2_t a);
#define vqshluq_n_s64(__a, __b) np_vqshluq_n_s64_##__b(__a)
extern int8x16_t np_vsriq_n_s8_1(int8x16_t a, int8x16_t b);
extern int8x16_t np_vsriq_n_s8_2(int8x16_t a, int8x16_t b);
extern int8x16_t np_vsriq_n_s8_3(int8x16_t a, int8x16_t b);
extern int8x16_t np_vsriq_n_s8_4(int8x16_t a, int8x16_t b);
extern int8x16_t np_vsriq_n_s8_5(int8x16_t a, int8x16_t b);
extern int8x16_t np_vsriq_n_s8_6(int8x16_t a, int8x16_t b);
extern int8x16_t np_vsriq_n_s8_7(int8x16_t a, int8x16_t b);
extern int8x16_t np_vsriq_n_s8_8(int8x16_t a, int8x16_t b);
#define vsriq_n_s8(__a, __b, __c) np_vsriq_n_s8_##__c(__a, __b)
extern int16x8_t np_vsriq_n_s16_1(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsriq_n_s16_2(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsriq_n_s16_3(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsriq_n_s16_4(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsriq_n_s16_5(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsriq_n_s16_6(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsriq_n_s16_7(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsriq_n_s16_8(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsriq_n_s16_9(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsriq_n_s16_10(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsriq_n_s16_11(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsriq_n_s16_12(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsriq_n_s16_13(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsriq_n_s16_14(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsriq_n_s16_15(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsriq_n_s16_16(int16x8_t a, int16x8_t b);
#define vsriq_n_s16(__a, __b, __c) np_vsriq_n_s16_##__c(__a, __b)
extern int32x4_t np_vsriq_n_s32_1(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_2(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_3(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_4(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_5(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_6(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_7(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_8(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_9(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_10(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_11(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_12(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_13(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_14(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_15(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_16(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_17(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_18(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_19(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_20(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_21(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_22(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_23(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_24(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_25(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_26(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_27(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_28(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_29(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_30(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_31(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsriq_n_s32_32(int32x4_t a, int32x4_t b);
#define vsriq_n_s32(__a, __b, __c) np_vsriq_n_s32_##__c(__a, __b)
extern int64x2_t np_vsriq_n_s64_1(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_2(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_3(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_4(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_5(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_6(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_7(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_8(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_9(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_10(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_11(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_12(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_13(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_14(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_15(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_16(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_17(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_18(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_19(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_20(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_21(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_22(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_23(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_24(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_25(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_26(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_27(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_28(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_29(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_30(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_31(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_32(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_33(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_34(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_35(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_36(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_37(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_38(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_39(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_40(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_41(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_42(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_43(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_44(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_45(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_46(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_47(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_48(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_49(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_50(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_51(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_52(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_53(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_54(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_55(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_56(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_57(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_58(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_59(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_60(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_61(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_62(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_63(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsriq_n_s64_64(int64x2_t a, int64x2_t b);
#define vsriq_n_s64(__a, __b, __c) np_vsriq_n_s64_##__c(__a, __b)
extern uint8x16_t np_vsriq_n_u8_1(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vsriq_n_u8_2(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vsriq_n_u8_3(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vsriq_n_u8_4(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vsriq_n_u8_5(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vsriq_n_u8_6(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vsriq_n_u8_7(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vsriq_n_u8_8(uint8x16_t a, uint8x16_t b);
#define vsriq_n_u8(__a, __b, __c) np_vsriq_n_u8_##__c(__a, __b)
extern uint16x8_t np_vsriq_n_u16_1(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsriq_n_u16_2(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsriq_n_u16_3(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsriq_n_u16_4(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsriq_n_u16_5(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsriq_n_u16_6(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsriq_n_u16_7(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsriq_n_u16_8(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsriq_n_u16_9(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsriq_n_u16_10(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsriq_n_u16_11(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsriq_n_u16_12(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsriq_n_u16_13(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsriq_n_u16_14(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsriq_n_u16_15(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsriq_n_u16_16(uint16x8_t a, uint16x8_t b);
#define vsriq_n_u16(__a, __b, __c) np_vsriq_n_u16_##__c(__a, __b)
extern uint32x4_t np_vsriq_n_u32_1(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_2(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_3(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_4(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_5(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_6(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_7(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_8(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_9(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_10(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_11(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_12(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_13(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_14(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_15(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_16(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_17(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_18(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_19(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_20(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_21(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_22(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_23(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_24(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_25(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_26(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_27(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_28(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_29(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_30(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_31(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsriq_n_u32_32(uint32x4_t a, uint32x4_t b);
#define vsriq_n_u32(__a, __b, __c) np_vsriq_n_u32_##__c(__a, __b)
extern uint64x2_t np_vsriq_n_u64_1(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_2(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_3(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_4(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_5(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_6(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_7(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_8(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_9(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_10(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_11(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_12(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_13(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_14(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_15(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_16(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_17(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_18(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_19(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_20(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_21(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_22(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_23(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_24(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_25(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_26(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_27(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_28(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_29(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_30(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_31(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_32(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_33(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_34(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_35(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_36(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_37(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_38(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_39(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_40(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_41(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_42(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_43(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_44(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_45(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_46(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_47(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_48(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_49(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_50(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_51(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_52(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_53(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_54(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_55(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_56(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_57(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_58(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_59(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_60(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_61(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_62(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_63(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsriq_n_u64_64(uint64x2_t a, uint64x2_t b);
#define vsriq_n_u64(__a, __b, __c) np_vsriq_n_u64_##__c(__a, __b)
extern poly8x16_t np_vsriq_n_p8_1(poly8x16_t a, poly8x16_t b);
extern poly8x16_t np_vsriq_n_p8_2(poly8x16_t a, poly8x16_t b);
extern poly8x16_t np_vsriq_n_p8_3(poly8x16_t a, poly8x16_t b);
extern poly8x16_t np_vsriq_n_p8_4(poly8x16_t a, poly8x16_t b);
extern poly8x16_t np_vsriq_n_p8_5(poly8x16_t a, poly8x16_t b);
extern poly8x16_t np_vsriq_n_p8_6(poly8x16_t a, poly8x16_t b);
extern poly8x16_t np_vsriq_n_p8_7(poly8x16_t a, poly8x16_t b);
extern poly8x16_t np_vsriq_n_p8_8(poly8x16_t a, poly8x16_t b);
#define vsriq_n_p8(__a, __b, __c) np_vsriq_n_p8_##__c(__a, __b)
extern poly16x8_t np_vsriq_n_p16_1(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vsriq_n_p16_2(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vsriq_n_p16_3(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vsriq_n_p16_4(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vsriq_n_p16_5(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vsriq_n_p16_6(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vsriq_n_p16_7(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vsriq_n_p16_8(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vsriq_n_p16_9(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vsriq_n_p16_10(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vsriq_n_p16_11(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vsriq_n_p16_12(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vsriq_n_p16_13(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vsriq_n_p16_14(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vsriq_n_p16_15(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vsriq_n_p16_16(poly16x8_t a, poly16x8_t b);
#define vsriq_n_p16(__a, __b, __c) np_vsriq_n_p16_##__c(__a, __b)
extern int8x16_t np_vsliq_n_s8_0(int8x16_t a, int8x16_t b);
extern int8x16_t np_vsliq_n_s8_1(int8x16_t a, int8x16_t b);
extern int8x16_t np_vsliq_n_s8_2(int8x16_t a, int8x16_t b);
extern int8x16_t np_vsliq_n_s8_3(int8x16_t a, int8x16_t b);
extern int8x16_t np_vsliq_n_s8_4(int8x16_t a, int8x16_t b);
extern int8x16_t np_vsliq_n_s8_5(int8x16_t a, int8x16_t b);
extern int8x16_t np_vsliq_n_s8_6(int8x16_t a, int8x16_t b);
extern int8x16_t np_vsliq_n_s8_7(int8x16_t a, int8x16_t b);
#define vsliq_n_s8(__a, __b, __c) np_vsliq_n_s8_##__c(__a, __b)
extern int16x8_t np_vsliq_n_s16_0(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsliq_n_s16_1(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsliq_n_s16_2(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsliq_n_s16_3(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsliq_n_s16_4(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsliq_n_s16_5(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsliq_n_s16_6(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsliq_n_s16_7(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsliq_n_s16_8(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsliq_n_s16_9(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsliq_n_s16_10(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsliq_n_s16_11(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsliq_n_s16_12(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsliq_n_s16_13(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsliq_n_s16_14(int16x8_t a, int16x8_t b);
extern int16x8_t np_vsliq_n_s16_15(int16x8_t a, int16x8_t b);
#define vsliq_n_s16(__a, __b, __c) np_vsliq_n_s16_##__c(__a, __b)
extern int32x4_t np_vsliq_n_s32_0(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_1(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_2(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_3(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_4(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_5(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_6(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_7(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_8(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_9(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_10(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_11(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_12(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_13(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_14(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_15(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_16(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_17(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_18(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_19(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_20(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_21(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_22(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_23(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_24(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_25(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_26(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_27(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_28(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_29(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_30(int32x4_t a, int32x4_t b);
extern int32x4_t np_vsliq_n_s32_31(int32x4_t a, int32x4_t b);
#define vsliq_n_s32(__a, __b, __c) np_vsliq_n_s32_##__c(__a, __b)
extern int64x2_t np_vsliq_n_s64_0(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_1(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_2(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_3(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_4(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_5(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_6(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_7(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_8(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_9(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_10(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_11(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_12(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_13(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_14(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_15(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_16(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_17(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_18(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_19(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_20(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_21(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_22(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_23(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_24(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_25(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_26(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_27(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_28(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_29(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_30(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_31(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_32(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_33(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_34(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_35(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_36(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_37(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_38(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_39(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_40(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_41(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_42(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_43(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_44(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_45(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_46(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_47(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_48(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_49(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_50(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_51(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_52(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_53(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_54(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_55(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_56(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_57(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_58(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_59(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_60(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_61(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_62(int64x2_t a, int64x2_t b);
extern int64x2_t np_vsliq_n_s64_63(int64x2_t a, int64x2_t b);
#define vsliq_n_s64(__a, __b, __c) np_vsliq_n_s64_##__c(__a, __b)
extern uint8x16_t np_vsliq_n_u8_0(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vsliq_n_u8_1(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vsliq_n_u8_2(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vsliq_n_u8_3(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vsliq_n_u8_4(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vsliq_n_u8_5(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vsliq_n_u8_6(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vsliq_n_u8_7(uint8x16_t a, uint8x16_t b);
#define vsliq_n_u8(__a, __b, __c) np_vsliq_n_u8_##__c(__a, __b)
extern uint16x8_t np_vsliq_n_u16_0(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsliq_n_u16_1(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsliq_n_u16_2(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsliq_n_u16_3(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsliq_n_u16_4(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsliq_n_u16_5(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsliq_n_u16_6(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsliq_n_u16_7(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsliq_n_u16_8(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsliq_n_u16_9(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsliq_n_u16_10(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsliq_n_u16_11(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsliq_n_u16_12(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsliq_n_u16_13(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsliq_n_u16_14(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vsliq_n_u16_15(uint16x8_t a, uint16x8_t b);
#define vsliq_n_u16(__a, __b, __c) np_vsliq_n_u16_##__c(__a, __b)
extern uint32x4_t np_vsliq_n_u32_0(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_1(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_2(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_3(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_4(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_5(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_6(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_7(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_8(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_9(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_10(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_11(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_12(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_13(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_14(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_15(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_16(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_17(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_18(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_19(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_20(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_21(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_22(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_23(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_24(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_25(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_26(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_27(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_28(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_29(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_30(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vsliq_n_u32_31(uint32x4_t a, uint32x4_t b);
#define vsliq_n_u32(__a, __b, __c) np_vsliq_n_u32_##__c(__a, __b)
extern uint64x2_t np_vsliq_n_u64_0(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_1(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_2(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_3(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_4(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_5(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_6(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_7(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_8(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_9(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_10(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_11(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_12(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_13(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_14(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_15(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_16(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_17(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_18(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_19(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_20(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_21(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_22(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_23(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_24(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_25(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_26(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_27(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_28(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_29(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_30(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_31(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_32(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_33(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_34(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_35(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_36(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_37(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_38(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_39(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_40(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_41(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_42(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_43(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_44(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_45(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_46(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_47(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_48(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_49(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_50(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_51(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_52(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_53(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_54(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_55(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_56(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_57(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_58(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_59(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_60(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_61(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_62(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vsliq_n_u64_63(uint64x2_t a, uint64x2_t b);
#define vsliq_n_u64(__a, __b, __c) np_vsliq_n_u64_##__c(__a, __b)
extern poly8x16_t np_vsliq_n_p8_0(poly8x16_t a, poly8x16_t b);
extern poly8x16_t np_vsliq_n_p8_1(poly8x16_t a, poly8x16_t b);
extern poly8x16_t np_vsliq_n_p8_2(poly8x16_t a, poly8x16_t b);
extern poly8x16_t np_vsliq_n_p8_3(poly8x16_t a, poly8x16_t b);
extern poly8x16_t np_vsliq_n_p8_4(poly8x16_t a, poly8x16_t b);
extern poly8x16_t np_vsliq_n_p8_5(poly8x16_t a, poly8x16_t b);
extern poly8x16_t np_vsliq_n_p8_6(poly8x16_t a, poly8x16_t b);
extern poly8x16_t np_vsliq_n_p8_7(poly8x16_t a, poly8x16_t b);
#define vsliq_n_p8(__a, __b, __c) np_vsliq_n_p8_##__c(__a, __b)
extern poly16x8_t np_vsliq_n_p16_0(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vsliq_n_p16_1(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vsliq_n_p16_2(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vsliq_n_p16_3(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vsliq_n_p16_4(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vsliq_n_p16_5(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vsliq_n_p16_6(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vsliq_n_p16_7(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vsliq_n_p16_8(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vsliq_n_p16_9(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vsliq_n_p16_10(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vsliq_n_p16_11(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vsliq_n_p16_12(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vsliq_n_p16_13(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vsliq_n_p16_14(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vsliq_n_p16_15(poly16x8_t a, poly16x8_t b);
#define vsliq_n_p16(__a, __b, __c) np_vsliq_n_p16_##__c(__a, __b)
extern uint8x16_t np_vld1q_u8(uint8_t const * ptr);
#define vld1q_u8 np_vld1q_u8
extern uint16x8_t np_vld1q_u16(uint16_t const * ptr);
#define vld1q_u16 np_vld1q_u16
extern uint32x4_t np_vld1q_u32(uint32_t const * ptr);
#define vld1q_u32 np_vld1q_u32
extern uint64x2_t np_vld1q_u64(uint64_t const * ptr);
#define vld1q_u64 np_vld1q_u64
extern int8x16_t np_vld1q_s8(int8_t const * ptr);
#define vld1q_s8 np_vld1q_s8
extern int16x8_t np_vld1q_s16(int16_t const * ptr);
#define vld1q_s16 np_vld1q_s16
extern int32x4_t np_vld1q_s32(int32_t const * ptr);
#define vld1q_s32 np_vld1q_s32
extern int64x2_t np_vld1q_s64(int64_t const * ptr);
#define vld1q_s64 np_vld1q_s64
extern float32x4_t np_vld1q_f32(float32_t const * ptr);
#define vld1q_f32 np_vld1q_f32
extern poly8x16_t np_vld1q_p8(poly8_t const * ptr);
#define vld1q_p8 np_vld1q_p8
extern poly16x8_t np_vld1q_p16(poly16_t const * ptr);
#define vld1q_p16 np_vld1q_p16
extern uint8x16_t np_vld1q_lane_u8_0(uint8_t const * ptr, uint8x16_t vec);
extern uint8x16_t np_vld1q_lane_u8_1(uint8_t const * ptr, uint8x16_t vec);
extern uint8x16_t np_vld1q_lane_u8_2(uint8_t const * ptr, uint8x16_t vec);
extern uint8x16_t np_vld1q_lane_u8_3(uint8_t const * ptr, uint8x16_t vec);
extern uint8x16_t np_vld1q_lane_u8_4(uint8_t const * ptr, uint8x16_t vec);
extern uint8x16_t np_vld1q_lane_u8_5(uint8_t const * ptr, uint8x16_t vec);
extern uint8x16_t np_vld1q_lane_u8_6(uint8_t const * ptr, uint8x16_t vec);
extern uint8x16_t np_vld1q_lane_u8_7(uint8_t const * ptr, uint8x16_t vec);
extern uint8x16_t np_vld1q_lane_u8_8(uint8_t const * ptr, uint8x16_t vec);
extern uint8x16_t np_vld1q_lane_u8_9(uint8_t const * ptr, uint8x16_t vec);
extern uint8x16_t np_vld1q_lane_u8_10(uint8_t const * ptr, uint8x16_t vec);
extern uint8x16_t np_vld1q_lane_u8_11(uint8_t const * ptr, uint8x16_t vec);
extern uint8x16_t np_vld1q_lane_u8_12(uint8_t const * ptr, uint8x16_t vec);
extern uint8x16_t np_vld1q_lane_u8_13(uint8_t const * ptr, uint8x16_t vec);
extern uint8x16_t np_vld1q_lane_u8_14(uint8_t const * ptr, uint8x16_t vec);
extern uint8x16_t np_vld1q_lane_u8_15(uint8_t const * ptr, uint8x16_t vec);
#define vld1q_lane_u8(__a, __b, __c) np_vld1q_lane_u8_##__c(__a, __b)
extern uint16x8_t np_vld1q_lane_u16_0(uint16_t const * ptr, uint16x8_t vec);
extern uint16x8_t np_vld1q_lane_u16_1(uint16_t const * ptr, uint16x8_t vec);
extern uint16x8_t np_vld1q_lane_u16_2(uint16_t const * ptr, uint16x8_t vec);
extern uint16x8_t np_vld1q_lane_u16_3(uint16_t const * ptr, uint16x8_t vec);
extern uint16x8_t np_vld1q_lane_u16_4(uint16_t const * ptr, uint16x8_t vec);
extern uint16x8_t np_vld1q_lane_u16_5(uint16_t const * ptr, uint16x8_t vec);
extern uint16x8_t np_vld1q_lane_u16_6(uint16_t const * ptr, uint16x8_t vec);
extern uint16x8_t np_vld1q_lane_u16_7(uint16_t const * ptr, uint16x8_t vec);
#define vld1q_lane_u16(__a, __b, __c) np_vld1q_lane_u16_##__c(__a, __b)
extern uint32x4_t np_vld1q_lane_u32_0(uint32_t const * ptr, uint32x4_t vec);
extern uint32x4_t np_vld1q_lane_u32_1(uint32_t const * ptr, uint32x4_t vec);
extern uint32x4_t np_vld1q_lane_u32_2(uint32_t const * ptr, uint32x4_t vec);
extern uint32x4_t np_vld1q_lane_u32_3(uint32_t const * ptr, uint32x4_t vec);
#define vld1q_lane_u32(__a, __b, __c) np_vld1q_lane_u32_##__c(__a, __b)
extern int8x16_t np_vld1q_lane_s8_0(int8_t const * ptr, int8x16_t vec);
extern int8x16_t np_vld1q_lane_s8_1(int8_t const * ptr, int8x16_t vec);
extern int8x16_t np_vld1q_lane_s8_2(int8_t const * ptr, int8x16_t vec);
extern int8x16_t np_vld1q_lane_s8_3(int8_t const * ptr, int8x16_t vec);
extern int8x16_t np_vld1q_lane_s8_4(int8_t const * ptr, int8x16_t vec);
extern int8x16_t np_vld1q_lane_s8_5(int8_t const * ptr, int8x16_t vec);
extern int8x16_t np_vld1q_lane_s8_6(int8_t const * ptr, int8x16_t vec);
extern int8x16_t np_vld1q_lane_s8_7(int8_t const * ptr, int8x16_t vec);
extern int8x16_t np_vld1q_lane_s8_8(int8_t const * ptr, int8x16_t vec);
extern int8x16_t np_vld1q_lane_s8_9(int8_t const * ptr, int8x16_t vec);
extern int8x16_t np_vld1q_lane_s8_10(int8_t const * ptr, int8x16_t vec);
extern int8x16_t np_vld1q_lane_s8_11(int8_t const * ptr, int8x16_t vec);
extern int8x16_t np_vld1q_lane_s8_12(int8_t const * ptr, int8x16_t vec);
extern int8x16_t np_vld1q_lane_s8_13(int8_t const * ptr, int8x16_t vec);
extern int8x16_t np_vld1q_lane_s8_14(int8_t const * ptr, int8x16_t vec);
extern int8x16_t np_vld1q_lane_s8_15(int8_t const * ptr, int8x16_t vec);
#define vld1q_lane_s8(__a, __b, __c) np_vld1q_lane_s8_##__c(__a, __b)
extern int16x8_t np_vld1q_lane_s16_0(int16_t const * ptr, int16x8_t vec);
extern int16x8_t np_vld1q_lane_s16_1(int16_t const * ptr, int16x8_t vec);
extern int16x8_t np_vld1q_lane_s16_2(int16_t const * ptr, int16x8_t vec);
extern int16x8_t np_vld1q_lane_s16_3(int16_t const * ptr, int16x8_t vec);
extern int16x8_t np_vld1q_lane_s16_4(int16_t const * ptr, int16x8_t vec);
extern int16x8_t np_vld1q_lane_s16_5(int16_t const * ptr, int16x8_t vec);
extern int16x8_t np_vld1q_lane_s16_6(int16_t const * ptr, int16x8_t vec);
extern int16x8_t np_vld1q_lane_s16_7(int16_t const * ptr, int16x8_t vec);
#define vld1q_lane_s16(__a, __b, __c) np_vld1q_lane_s16_##__c(__a, __b)
extern int32x4_t np_vld1q_lane_s32_0(int32_t const * ptr, int32x4_t vec);
extern int32x4_t np_vld1q_lane_s32_1(int32_t const * ptr, int32x4_t vec);
extern int32x4_t np_vld1q_lane_s32_2(int32_t const * ptr, int32x4_t vec);
extern int32x4_t np_vld1q_lane_s32_3(int32_t const * ptr, int32x4_t vec);
#define vld1q_lane_s32(__a, __b, __c) np_vld1q_lane_s32_##__c(__a, __b)
extern float32x4_t np_vld1q_lane_f32_0(float32_t const * ptr, float32x4_t vec);
extern float32x4_t np_vld1q_lane_f32_1(float32_t const * ptr, float32x4_t vec);
extern float32x4_t np_vld1q_lane_f32_2(float32_t const * ptr, float32x4_t vec);
extern float32x4_t np_vld1q_lane_f32_3(float32_t const * ptr, float32x4_t vec);
#define vld1q_lane_f32(__a, __b, __c) np_vld1q_lane_f32_##__c(__a, __b)
extern poly8x16_t np_vld1q_lane_p8_0(poly8_t const * ptr, poly8x16_t vec);
extern poly8x16_t np_vld1q_lane_p8_1(poly8_t const * ptr, poly8x16_t vec);
extern poly8x16_t np_vld1q_lane_p8_2(poly8_t const * ptr, poly8x16_t vec);
extern poly8x16_t np_vld1q_lane_p8_3(poly8_t const * ptr, poly8x16_t vec);
extern poly8x16_t np_vld1q_lane_p8_4(poly8_t const * ptr, poly8x16_t vec);
extern poly8x16_t np_vld1q_lane_p8_5(poly8_t const * ptr, poly8x16_t vec);
extern poly8x16_t np_vld1q_lane_p8_6(poly8_t const * ptr, poly8x16_t vec);
extern poly8x16_t np_vld1q_lane_p8_7(poly8_t const * ptr, poly8x16_t vec);
extern poly8x16_t np_vld1q_lane_p8_8(poly8_t const * ptr, poly8x16_t vec);
extern poly8x16_t np_vld1q_lane_p8_9(poly8_t const * ptr, poly8x16_t vec);
extern poly8x16_t np_vld1q_lane_p8_10(poly8_t const * ptr, poly8x16_t vec);
extern poly8x16_t np_vld1q_lane_p8_11(poly8_t const * ptr, poly8x16_t vec);
extern poly8x16_t np_vld1q_lane_p8_12(poly8_t const * ptr, poly8x16_t vec);
extern poly8x16_t np_vld1q_lane_p8_13(poly8_t const * ptr, poly8x16_t vec);
extern poly8x16_t np_vld1q_lane_p8_14(poly8_t const * ptr, poly8x16_t vec);
extern poly8x16_t np_vld1q_lane_p8_15(poly8_t const * ptr, poly8x16_t vec);
#define vld1q_lane_p8(__a, __b, __c) np_vld1q_lane_p8_##__c(__a, __b)
extern poly16x8_t np_vld1q_lane_p16_0(poly16_t const * ptr, poly16x8_t vec);
extern poly16x8_t np_vld1q_lane_p16_1(poly16_t const * ptr, poly16x8_t vec);
extern poly16x8_t np_vld1q_lane_p16_2(poly16_t const * ptr, poly16x8_t vec);
extern poly16x8_t np_vld1q_lane_p16_3(poly16_t const * ptr, poly16x8_t vec);
extern poly16x8_t np_vld1q_lane_p16_4(poly16_t const * ptr, poly16x8_t vec);
extern poly16x8_t np_vld1q_lane_p16_5(poly16_t const * ptr, poly16x8_t vec);
extern poly16x8_t np_vld1q_lane_p16_6(poly16_t const * ptr, poly16x8_t vec);
extern poly16x8_t np_vld1q_lane_p16_7(poly16_t const * ptr, poly16x8_t vec);
#define vld1q_lane_p16(__a, __b, __c) np_vld1q_lane_p16_##__c(__a, __b)
extern uint8x16_t np_vld1q_dup_u8(uint8_t const * ptr);
#define vld1q_dup_u8 np_vld1q_dup_u8
extern uint16x8_t np_vld1q_dup_u16(uint16_t const * ptr);
#define vld1q_dup_u16 np_vld1q_dup_u16
extern uint32x4_t np_vld1q_dup_u32(uint32_t const * ptr);
#define vld1q_dup_u32 np_vld1q_dup_u32
extern int8x16_t np_vld1q_dup_s8(int8_t const * ptr);
#define vld1q_dup_s8 np_vld1q_dup_s8
extern int16x8_t np_vld1q_dup_s16(int16_t const * ptr);
#define vld1q_dup_s16 np_vld1q_dup_s16
extern int32x4_t np_vld1q_dup_s32(int32_t const * ptr);
#define vld1q_dup_s32 np_vld1q_dup_s32
extern float32x4_t np_vld1q_dup_f32(float32_t const * ptr);
#define vld1q_dup_f32 np_vld1q_dup_f32
extern poly8x16_t np_vld1q_dup_p8(poly8_t const * ptr);
#define vld1q_dup_p8 np_vld1q_dup_p8
extern poly16x8_t np_vld1q_dup_p16(poly16_t const * ptr);
#define vld1q_dup_p16 np_vld1q_dup_p16
extern void np_vst1q_u8(uint8_t * ptr, uint8x16_t val);
#define vst1q_u8 np_vst1q_u8
extern void np_vst1q_u16(uint16_t * ptr, uint16x8_t val);
#define vst1q_u16 np_vst1q_u16
extern void np_vst1q_u32(uint32_t * ptr, uint32x4_t val);
#define vst1q_u32 np_vst1q_u32
extern void np_vst1q_u64(uint64_t * ptr, uint64x2_t val);
#define vst1q_u64 np_vst1q_u64
extern void np_vst1q_s8(int8_t * ptr, int8x16_t val);
#define vst1q_s8 np_vst1q_s8
extern void np_vst1q_s16(int16_t * ptr, int16x8_t val);
#define vst1q_s16 np_vst1q_s16
extern void np_vst1q_s32(int32_t * ptr, int32x4_t val);
#define vst1q_s32 np_vst1q_s32
extern void np_vst1q_s64(int64_t * ptr, int64x2_t val);
#define vst1q_s64 np_vst1q_s64
extern void np_vst1q_f32(float32_t * ptr, float32x4_t val);
#define vst1q_f32 np_vst1q_f32
extern void np_vst1q_p8(poly8_t * ptr, poly8x16_t val);
#define vst1q_p8 np_vst1q_p8
extern void np_vst1q_p16(poly16_t * ptr, poly16x8_t val);
#define vst1q_p16 np_vst1q_p16
extern uint8x16x2_t np_vld2q_u8(uint8_t const * ptr);
#define vld2q_u8 np_vld2q_u8
extern uint16x8x2_t np_vld2q_u16(uint16_t const * ptr);
#define vld2q_u16 np_vld2q_u16
extern uint32x4x2_t np_vld2q_u32(uint32_t const * ptr);
#define vld2q_u32 np_vld2q_u32
extern int8x16x2_t np_vld2q_s8(int8_t const * ptr);
#define vld2q_s8 np_vld2q_s8
extern int16x8x2_t np_vld2q_s16(int16_t const * ptr);
#define vld2q_s16 np_vld2q_s16
extern int32x4x2_t np_vld2q_s32(int32_t const * ptr);
#define vld2q_s32 np_vld2q_s32
extern float32x4x2_t np_vld2q_f32(float32_t const * ptr);
#define vld2q_f32 np_vld2q_f32
extern poly8x16x2_t np_vld2q_p8(poly8_t const * ptr);
#define vld2q_p8 np_vld2q_p8
extern poly16x8x2_t np_vld2q_p16(poly16_t const * ptr);
#define vld2q_p16 np_vld2q_p16
extern uint8x16x3_t np_vld3q_u8(uint8_t const * ptr);
#define vld3q_u8 np_vld3q_u8
extern uint16x8x3_t np_vld3q_u16(uint16_t const * ptr);
#define vld3q_u16 np_vld3q_u16
extern uint32x4x3_t np_vld3q_u32(uint32_t const * ptr);
#define vld3q_u32 np_vld3q_u32
extern int8x16x3_t np_vld3q_s8(int8_t const * ptr);
#define vld3q_s8 np_vld3q_s8
extern int16x8x3_t np_vld3q_s16(int16_t const * ptr);
#define vld3q_s16 np_vld3q_s16
extern int32x4x3_t np_vld3q_s32(int32_t const * ptr);
#define vld3q_s32 np_vld3q_s32
extern float32x4x3_t np_vld3q_f32(float32_t const * ptr);
#define vld3q_f32 np_vld3q_f32
extern poly8x16x3_t np_vld3q_p8(poly8_t const * ptr);
#define vld3q_p8 np_vld3q_p8
extern poly16x8x3_t np_vld3q_p16(poly16_t const * ptr);
#define vld3q_p16 np_vld3q_p16
extern uint8x16x4_t np_vld4q_u8(uint8_t const * ptr);
#define vld4q_u8 np_vld4q_u8
extern uint16x8x4_t np_vld4q_u16(uint16_t const * ptr);
#define vld4q_u16 np_vld4q_u16
extern uint32x4x4_t np_vld4q_u32(uint32_t const * ptr);
#define vld4q_u32 np_vld4q_u32
extern int8x16x4_t np_vld4q_s8(int8_t const * ptr);
#define vld4q_s8 np_vld4q_s8
extern int16x8x4_t np_vld4q_s16(int16_t const * ptr);
#define vld4q_s16 np_vld4q_s16
extern int32x4x4_t np_vld4q_s32(int32_t const * ptr);
#define vld4q_s32 np_vld4q_s32
extern float32x4x4_t np_vld4q_f32(float32_t const * ptr);
#define vld4q_f32 np_vld4q_f32
extern poly8x16x4_t np_vld4q_p8(poly8_t const * ptr);
#define vld4q_p8 np_vld4q_p8
extern poly16x8x4_t np_vld4q_p16(poly16_t const * ptr);
#define vld4q_p16 np_vld4q_p16
extern uint8_t np_vgetq_lane_u8_0(uint8x16_t vec);
extern uint8_t np_vgetq_lane_u8_1(uint8x16_t vec);
extern uint8_t np_vgetq_lane_u8_2(uint8x16_t vec);
extern uint8_t np_vgetq_lane_u8_3(uint8x16_t vec);
extern uint8_t np_vgetq_lane_u8_4(uint8x16_t vec);
extern uint8_t np_vgetq_lane_u8_5(uint8x16_t vec);
extern uint8_t np_vgetq_lane_u8_6(uint8x16_t vec);
extern uint8_t np_vgetq_lane_u8_7(uint8x16_t vec);
extern uint8_t np_vgetq_lane_u8_8(uint8x16_t vec);
extern uint8_t np_vgetq_lane_u8_9(uint8x16_t vec);
extern uint8_t np_vgetq_lane_u8_10(uint8x16_t vec);
extern uint8_t np_vgetq_lane_u8_11(uint8x16_t vec);
extern uint8_t np_vgetq_lane_u8_12(uint8x16_t vec);
extern uint8_t np_vgetq_lane_u8_13(uint8x16_t vec);
extern uint8_t np_vgetq_lane_u8_14(uint8x16_t vec);
extern uint8_t np_vgetq_lane_u8_15(uint8x16_t vec);
#define vgetq_lane_u8(__a, __b) np_vgetq_lane_u8_##__b(__a)
extern uint16_t np_vgetq_lane_u16_0(uint16x8_t vec);
extern uint16_t np_vgetq_lane_u16_1(uint16x8_t vec);
extern uint16_t np_vgetq_lane_u16_2(uint16x8_t vec);
extern uint16_t np_vgetq_lane_u16_3(uint16x8_t vec);
extern uint16_t np_vgetq_lane_u16_4(uint16x8_t vec);
extern uint16_t np_vgetq_lane_u16_5(uint16x8_t vec);
extern uint16_t np_vgetq_lane_u16_6(uint16x8_t vec);
extern uint16_t np_vgetq_lane_u16_7(uint16x8_t vec);
#define vgetq_lane_u16(__a, __b) np_vgetq_lane_u16_##__b(__a)
extern uint32_t np_vgetq_lane_u32_0(uint32x4_t vec);
extern uint32_t np_vgetq_lane_u32_1(uint32x4_t vec);
extern uint32_t np_vgetq_lane_u32_2(uint32x4_t vec);
extern uint32_t np_vgetq_lane_u32_3(uint32x4_t vec);
#define vgetq_lane_u32(__a, __b) np_vgetq_lane_u32_##__b(__a)
extern int8_t np_vgetq_lane_s8_0(int8x16_t vec);
extern int8_t np_vgetq_lane_s8_1(int8x16_t vec);
extern int8_t np_vgetq_lane_s8_2(int8x16_t vec);
extern int8_t np_vgetq_lane_s8_3(int8x16_t vec);
extern int8_t np_vgetq_lane_s8_4(int8x16_t vec);
extern int8_t np_vgetq_lane_s8_5(int8x16_t vec);
extern int8_t np_vgetq_lane_s8_6(int8x16_t vec);
extern int8_t np_vgetq_lane_s8_7(int8x16_t vec);
extern int8_t np_vgetq_lane_s8_8(int8x16_t vec);
extern int8_t np_vgetq_lane_s8_9(int8x16_t vec);
extern int8_t np_vgetq_lane_s8_10(int8x16_t vec);
extern int8_t np_vgetq_lane_s8_11(int8x16_t vec);
extern int8_t np_vgetq_lane_s8_12(int8x16_t vec);
extern int8_t np_vgetq_lane_s8_13(int8x16_t vec);
extern int8_t np_vgetq_lane_s8_14(int8x16_t vec);
extern int8_t np_vgetq_lane_s8_15(int8x16_t vec);
#define vgetq_lane_s8(__a, __b) np_vgetq_lane_s8_##__b(__a)
extern int16_t np_vgetq_lane_s16_0(int16x8_t vec);
extern int16_t np_vgetq_lane_s16_1(int16x8_t vec);
extern int16_t np_vgetq_lane_s16_2(int16x8_t vec);
extern int16_t np_vgetq_lane_s16_3(int16x8_t vec);
extern int16_t np_vgetq_lane_s16_4(int16x8_t vec);
extern int16_t np_vgetq_lane_s16_5(int16x8_t vec);
extern int16_t np_vgetq_lane_s16_6(int16x8_t vec);
extern int16_t np_vgetq_lane_s16_7(int16x8_t vec);
#define vgetq_lane_s16(__a, __b) np_vgetq_lane_s16_##__b(__a)
extern int32_t np_vgetq_lane_s32_0(int32x4_t vec);
extern int32_t np_vgetq_lane_s32_1(int32x4_t vec);
extern int32_t np_vgetq_lane_s32_2(int32x4_t vec);
extern int32_t np_vgetq_lane_s32_3(int32x4_t vec);
#define vgetq_lane_s32(__a, __b) np_vgetq_lane_s32_##__b(__a)
extern poly8_t np_vgetq_lane_p8_0(poly8x16_t vec);
extern poly8_t np_vgetq_lane_p8_1(poly8x16_t vec);
extern poly8_t np_vgetq_lane_p8_2(poly8x16_t vec);
extern poly8_t np_vgetq_lane_p8_3(poly8x16_t vec);
extern poly8_t np_vgetq_lane_p8_4(poly8x16_t vec);
extern poly8_t np_vgetq_lane_p8_5(poly8x16_t vec);
extern poly8_t np_vgetq_lane_p8_6(poly8x16_t vec);
extern poly8_t np_vgetq_lane_p8_7(poly8x16_t vec);
extern poly8_t np_vgetq_lane_p8_8(poly8x16_t vec);
extern poly8_t np_vgetq_lane_p8_9(poly8x16_t vec);
extern poly8_t np_vgetq_lane_p8_10(poly8x16_t vec);
extern poly8_t np_vgetq_lane_p8_11(poly8x16_t vec);
extern poly8_t np_vgetq_lane_p8_12(poly8x16_t vec);
extern poly8_t np_vgetq_lane_p8_13(poly8x16_t vec);
extern poly8_t np_vgetq_lane_p8_14(poly8x16_t vec);
extern poly8_t np_vgetq_lane_p8_15(poly8x16_t vec);
#define vgetq_lane_p8(__a, __b) np_vgetq_lane_p8_##__b(__a)
extern poly16_t np_vgetq_lane_p16_0(poly16x8_t vec);
extern poly16_t np_vgetq_lane_p16_1(poly16x8_t vec);
extern poly16_t np_vgetq_lane_p16_2(poly16x8_t vec);
extern poly16_t np_vgetq_lane_p16_3(poly16x8_t vec);
extern poly16_t np_vgetq_lane_p16_4(poly16x8_t vec);
extern poly16_t np_vgetq_lane_p16_5(poly16x8_t vec);
extern poly16_t np_vgetq_lane_p16_6(poly16x8_t vec);
extern poly16_t np_vgetq_lane_p16_7(poly16x8_t vec);
#define vgetq_lane_p16(__a, __b) np_vgetq_lane_p16_##__b(__a)
extern float32_t np_vgetq_lane_f32_0(float32x4_t vec);
extern float32_t np_vgetq_lane_f32_1(float32x4_t vec);
extern float32_t np_vgetq_lane_f32_2(float32x4_t vec);
extern float32_t np_vgetq_lane_f32_3(float32x4_t vec);
#define vgetq_lane_f32(__a, __b) np_vgetq_lane_f32_##__b(__a)
extern int64_t np_vgetq_lane_s64_0(int64x2_t vec);
extern int64_t np_vgetq_lane_s64_1(int64x2_t vec);
#define vgetq_lane_s64(__a, __b) np_vgetq_lane_s64_##__b(__a)
extern uint64_t np_vgetq_lane_u64_0(uint64x2_t vec);
extern uint64_t np_vgetq_lane_u64_1(uint64x2_t vec);
#define vgetq_lane_u64(__a, __b) np_vgetq_lane_u64_##__b(__a)
extern uint8x16_t np_vsetq_lane_u8_0(uint8_t value, uint8x16_t vec);
extern uint8x16_t np_vsetq_lane_u8_1(uint8_t value, uint8x16_t vec);
extern uint8x16_t np_vsetq_lane_u8_2(uint8_t value, uint8x16_t vec);
extern uint8x16_t np_vsetq_lane_u8_3(uint8_t value, uint8x16_t vec);
extern uint8x16_t np_vsetq_lane_u8_4(uint8_t value, uint8x16_t vec);
extern uint8x16_t np_vsetq_lane_u8_5(uint8_t value, uint8x16_t vec);
extern uint8x16_t np_vsetq_lane_u8_6(uint8_t value, uint8x16_t vec);
extern uint8x16_t np_vsetq_lane_u8_7(uint8_t value, uint8x16_t vec);
extern uint8x16_t np_vsetq_lane_u8_8(uint8_t value, uint8x16_t vec);
extern uint8x16_t np_vsetq_lane_u8_9(uint8_t value, uint8x16_t vec);
extern uint8x16_t np_vsetq_lane_u8_10(uint8_t value, uint8x16_t vec);
extern uint8x16_t np_vsetq_lane_u8_11(uint8_t value, uint8x16_t vec);
extern uint8x16_t np_vsetq_lane_u8_12(uint8_t value, uint8x16_t vec);
extern uint8x16_t np_vsetq_lane_u8_13(uint8_t value, uint8x16_t vec);
extern uint8x16_t np_vsetq_lane_u8_14(uint8_t value, uint8x16_t vec);
extern uint8x16_t np_vsetq_lane_u8_15(uint8_t value, uint8x16_t vec);
#define vsetq_lane_u8(__a, __b, __c) np_vsetq_lane_u8_##__c(__a, __b)
extern uint16x8_t np_vsetq_lane_u16_0(uint16_t value, uint16x8_t vec);
extern uint16x8_t np_vsetq_lane_u16_1(uint16_t value, uint16x8_t vec);
extern uint16x8_t np_vsetq_lane_u16_2(uint16_t value, uint16x8_t vec);
extern uint16x8_t np_vsetq_lane_u16_3(uint16_t value, uint16x8_t vec);
extern uint16x8_t np_vsetq_lane_u16_4(uint16_t value, uint16x8_t vec);
extern uint16x8_t np_vsetq_lane_u16_5(uint16_t value, uint16x8_t vec);
extern uint16x8_t np_vsetq_lane_u16_6(uint16_t value, uint16x8_t vec);
extern uint16x8_t np_vsetq_lane_u16_7(uint16_t value, uint16x8_t vec);
#define vsetq_lane_u16(__a, __b, __c) np_vsetq_lane_u16_##__c(__a, __b)
extern uint32x4_t np_vsetq_lane_u32_0(uint32_t value, uint32x4_t vec);
extern uint32x4_t np_vsetq_lane_u32_1(uint32_t value, uint32x4_t vec);
extern uint32x4_t np_vsetq_lane_u32_2(uint32_t value, uint32x4_t vec);
extern uint32x4_t np_vsetq_lane_u32_3(uint32_t value, uint32x4_t vec);
#define vsetq_lane_u32(__a, __b, __c) np_vsetq_lane_u32_##__c(__a, __b)
extern int8x16_t np_vsetq_lane_s8_0(int8_t value, int8x16_t vec);
extern int8x16_t np_vsetq_lane_s8_1(int8_t value, int8x16_t vec);
extern int8x16_t np_vsetq_lane_s8_2(int8_t value, int8x16_t vec);
extern int8x16_t np_vsetq_lane_s8_3(int8_t value, int8x16_t vec);
extern int8x16_t np_vsetq_lane_s8_4(int8_t value, int8x16_t vec);
extern int8x16_t np_vsetq_lane_s8_5(int8_t value, int8x16_t vec);
extern int8x16_t np_vsetq_lane_s8_6(int8_t value, int8x16_t vec);
extern int8x16_t np_vsetq_lane_s8_7(int8_t value, int8x16_t vec);
extern int8x16_t np_vsetq_lane_s8_8(int8_t value, int8x16_t vec);
extern int8x16_t np_vsetq_lane_s8_9(int8_t value, int8x16_t vec);
extern int8x16_t np_vsetq_lane_s8_10(int8_t value, int8x16_t vec);
extern int8x16_t np_vsetq_lane_s8_11(int8_t value, int8x16_t vec);
extern int8x16_t np_vsetq_lane_s8_12(int8_t value, int8x16_t vec);
extern int8x16_t np_vsetq_lane_s8_13(int8_t value, int8x16_t vec);
extern int8x16_t np_vsetq_lane_s8_14(int8_t value, int8x16_t vec);
extern int8x16_t np_vsetq_lane_s8_15(int8_t value, int8x16_t vec);
#define vsetq_lane_s8(__a, __b, __c) np_vsetq_lane_s8_##__c(__a, __b)
extern int16x8_t np_vsetq_lane_s16_0(int16_t value, int16x8_t vec);
extern int16x8_t np_vsetq_lane_s16_1(int16_t value, int16x8_t vec);
extern int16x8_t np_vsetq_lane_s16_2(int16_t value, int16x8_t vec);
extern int16x8_t np_vsetq_lane_s16_3(int16_t value, int16x8_t vec);
extern int16x8_t np_vsetq_lane_s16_4(int16_t value, int16x8_t vec);
extern int16x8_t np_vsetq_lane_s16_5(int16_t value, int16x8_t vec);
extern int16x8_t np_vsetq_lane_s16_6(int16_t value, int16x8_t vec);
extern int16x8_t np_vsetq_lane_s16_7(int16_t value, int16x8_t vec);
#define vsetq_lane_s16(__a, __b, __c) np_vsetq_lane_s16_##__c(__a, __b)
extern int32x4_t np_vsetq_lane_s32_0(int32_t value, int32x4_t vec);
extern int32x4_t np_vsetq_lane_s32_1(int32_t value, int32x4_t vec);
extern int32x4_t np_vsetq_lane_s32_2(int32_t value, int32x4_t vec);
extern int32x4_t np_vsetq_lane_s32_3(int32_t value, int32x4_t vec);
#define vsetq_lane_s32(__a, __b, __c) np_vsetq_lane_s32_##__c(__a, __b)
extern poly8x16_t np_vsetq_lane_p8_0(poly8_t value, poly8x16_t vec);
extern poly8x16_t np_vsetq_lane_p8_1(poly8_t value, poly8x16_t vec);
extern poly8x16_t np_vsetq_lane_p8_2(poly8_t value, poly8x16_t vec);
extern poly8x16_t np_vsetq_lane_p8_3(poly8_t value, poly8x16_t vec);
extern poly8x16_t np_vsetq_lane_p8_4(poly8_t value, poly8x16_t vec);
extern poly8x16_t np_vsetq_lane_p8_5(poly8_t value, poly8x16_t vec);
extern poly8x16_t np_vsetq_lane_p8_6(poly8_t value, poly8x16_t vec);
extern poly8x16_t np_vsetq_lane_p8_7(poly8_t value, poly8x16_t vec);
extern poly8x16_t np_vsetq_lane_p8_8(poly8_t value, poly8x16_t vec);
extern poly8x16_t np_vsetq_lane_p8_9(poly8_t value, poly8x16_t vec);
extern poly8x16_t np_vsetq_lane_p8_10(poly8_t value, poly8x16_t vec);
extern poly8x16_t np_vsetq_lane_p8_11(poly8_t value, poly8x16_t vec);
extern poly8x16_t np_vsetq_lane_p8_12(poly8_t value, poly8x16_t vec);
extern poly8x16_t np_vsetq_lane_p8_13(poly8_t value, poly8x16_t vec);
extern poly8x16_t np_vsetq_lane_p8_14(poly8_t value, poly8x16_t vec);
extern poly8x16_t np_vsetq_lane_p8_15(poly8_t value, poly8x16_t vec);
#define vsetq_lane_p8(__a, __b, __c) np_vsetq_lane_p8_##__c(__a, __b)
extern poly16x8_t np_vsetq_lane_p16_0(poly16_t value, poly16x8_t vec);
extern poly16x8_t np_vsetq_lane_p16_1(poly16_t value, poly16x8_t vec);
extern poly16x8_t np_vsetq_lane_p16_2(poly16_t value, poly16x8_t vec);
extern poly16x8_t np_vsetq_lane_p16_3(poly16_t value, poly16x8_t vec);
extern poly16x8_t np_vsetq_lane_p16_4(poly16_t value, poly16x8_t vec);
extern poly16x8_t np_vsetq_lane_p16_5(poly16_t value, poly16x8_t vec);
extern poly16x8_t np_vsetq_lane_p16_6(poly16_t value, poly16x8_t vec);
extern poly16x8_t np_vsetq_lane_p16_7(poly16_t value, poly16x8_t vec);
#define vsetq_lane_p16(__a, __b, __c) np_vsetq_lane_p16_##__c(__a, __b)
extern float32x4_t np_vsetq_lane_f32_0(float32_t value, float32x4_t vec);
extern float32x4_t np_vsetq_lane_f32_1(float32_t value, float32x4_t vec);
extern float32x4_t np_vsetq_lane_f32_2(float32_t value, float32x4_t vec);
extern float32x4_t np_vsetq_lane_f32_3(float32_t value, float32x4_t vec);
#define vsetq_lane_f32(__a, __b, __c) np_vsetq_lane_f32_##__c(__a, __b)
extern int64x2_t np_vsetq_lane_s64_0(int64_t value, int64x2_t vec);
extern int64x2_t np_vsetq_lane_s64_1(int64_t value, int64x2_t vec);
#define vsetq_lane_s64(__a, __b, __c) np_vsetq_lane_s64_##__c(__a, __b)
extern uint64x2_t np_vsetq_lane_u64_0(uint64_t value, uint64x2_t vec);
extern uint64x2_t np_vsetq_lane_u64_1(uint64_t value, uint64x2_t vec);
#define vsetq_lane_u64(__a, __b, __c) np_vsetq_lane_u64_##__c(__a, __b)
extern uint8x16_t np_vdupq_n_u8(uint8_t value);
#define vdupq_n_u8 np_vdupq_n_u8
extern uint16x8_t np_vdupq_n_u16(uint16_t value);
#define vdupq_n_u16 np_vdupq_n_u16
extern uint32x4_t np_vdupq_n_u32(uint32_t value);
#define vdupq_n_u32 np_vdupq_n_u32
extern int8x16_t np_vdupq_n_s8(int8_t value);
#define vdupq_n_s8 np_vdupq_n_s8
extern int16x8_t np_vdupq_n_s16(int16_t value);
#define vdupq_n_s16 np_vdupq_n_s16
extern int32x4_t np_vdupq_n_s32(int32_t value);
#define vdupq_n_s32 np_vdupq_n_s32
extern poly8x16_t np_vdupq_n_p8(poly8_t value);
#define vdupq_n_p8 np_vdupq_n_p8
extern poly16x8_t np_vdupq_n_p16(poly16_t value);
#define vdupq_n_p16 np_vdupq_n_p16
extern float32x4_t np_vdupq_n_f32(float32_t value);
#define vdupq_n_f32 np_vdupq_n_f32
extern int64x2_t np_vdupq_n_s64(int64_t value);
#define vdupq_n_s64 np_vdupq_n_s64
extern uint64x2_t np_vdupq_n_u64(uint64_t value);
#define vdupq_n_u64 np_vdupq_n_u64
extern uint8x16_t np_vmovq_n_u8(uint8_t value);
#define vmovq_n_u8 np_vmovq_n_u8
extern uint16x8_t np_vmovq_n_u16(uint16_t value);
#define vmovq_n_u16 np_vmovq_n_u16
extern uint32x4_t np_vmovq_n_u32(uint32_t value);
#define vmovq_n_u32 np_vmovq_n_u32
extern int8x16_t np_vmovq_n_s8(int8_t value);
#define vmovq_n_s8 np_vmovq_n_s8
extern int16x8_t np_vmovq_n_s16(int16_t value);
#define vmovq_n_s16 np_vmovq_n_s16
extern int32x4_t np_vmovq_n_s32(int32_t value);
#define vmovq_n_s32 np_vmovq_n_s32
extern poly8x16_t np_vmovq_n_p8(poly8_t value);
#define vmovq_n_p8 np_vmovq_n_p8
extern poly16x8_t np_vmovq_n_p16(poly16_t value);
#define vmovq_n_p16 np_vmovq_n_p16
extern float32x4_t np_vmovq_n_f32(float32_t value);
#define vmovq_n_f32 np_vmovq_n_f32
extern int64x2_t np_vmovq_n_s64(int64_t value);
#define vmovq_n_s64 np_vmovq_n_s64
extern uint64x2_t np_vmovq_n_u64(uint64_t value);
#define vmovq_n_u64 np_vmovq_n_u64
extern int32x4_t np_vcvtq_s32_f32(float32x4_t a);
#define vcvtq_s32_f32 np_vcvtq_s32_f32
extern uint32x4_t np_vcvtq_u32_f32(float32x4_t a);
#define vcvtq_u32_f32 np_vcvtq_u32_f32
extern int32x4_t np_vcvtq_n_s32_f32_1(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_2(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_3(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_4(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_5(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_6(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_7(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_8(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_9(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_10(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_11(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_12(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_13(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_14(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_15(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_16(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_17(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_18(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_19(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_20(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_21(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_22(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_23(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_24(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_25(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_26(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_27(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_28(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_29(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_30(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_31(float32x4_t a);
extern int32x4_t np_vcvtq_n_s32_f32_32(float32x4_t a);
#define vcvtq_n_s32_f32(__a, __b) np_vcvtq_n_s32_f32_##__b(__a)
extern uint32x4_t np_vcvtq_n_u32_f32_1(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_2(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_3(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_4(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_5(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_6(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_7(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_8(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_9(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_10(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_11(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_12(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_13(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_14(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_15(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_16(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_17(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_18(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_19(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_20(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_21(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_22(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_23(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_24(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_25(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_26(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_27(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_28(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_29(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_30(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_31(float32x4_t a);
extern uint32x4_t np_vcvtq_n_u32_f32_32(float32x4_t a);
#define vcvtq_n_u32_f32(__a, __b) np_vcvtq_n_u32_f32_##__b(__a)
extern float32x4_t np_vcvtq_f32_s32(int32x4_t a);
#define vcvtq_f32_s32 np_vcvtq_f32_s32
extern float32x4_t np_vcvtq_f32_u32(uint32x4_t a);
#define vcvtq_f32_u32 np_vcvtq_f32_u32
extern float32x4_t np_vcvtq_n_f32_s32_1(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_2(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_3(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_4(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_5(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_6(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_7(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_8(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_9(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_10(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_11(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_12(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_13(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_14(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_15(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_16(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_17(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_18(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_19(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_20(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_21(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_22(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_23(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_24(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_25(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_26(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_27(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_28(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_29(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_30(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_31(int32x4_t a);
extern float32x4_t np_vcvtq_n_f32_s32_32(int32x4_t a);
#define vcvtq_n_f32_s32(__a, __b) np_vcvtq_n_f32_s32_##__b(__a)
extern float32x4_t np_vcvtq_n_f32_u32_1(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_2(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_3(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_4(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_5(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_6(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_7(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_8(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_9(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_10(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_11(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_12(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_13(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_14(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_15(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_16(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_17(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_18(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_19(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_20(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_21(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_22(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_23(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_24(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_25(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_26(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_27(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_28(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_29(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_30(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_31(uint32x4_t a);
extern float32x4_t np_vcvtq_n_f32_u32_32(uint32x4_t a);
#define vcvtq_n_f32_u32(__a, __b) np_vcvtq_n_f32_u32_##__b(__a)
extern int16x8_t np_vmulq_n_s16(int16x8_t a, int16_t b);
#define vmulq_n_s16 np_vmulq_n_s16
extern int32x4_t np_vmulq_n_s32(int32x4_t a, int32_t b);
#define vmulq_n_s32 np_vmulq_n_s32
extern float32x4_t np_vmulq_n_f32(float32x4_t a, float32_t b);
#define vmulq_n_f32 np_vmulq_n_f32
extern uint16x8_t np_vmulq_n_u16(uint16x8_t a, uint16_t b);
#define vmulq_n_u16 np_vmulq_n_u16
extern uint32x4_t np_vmulq_n_u32(uint32x4_t a, uint32_t b);
#define vmulq_n_u32 np_vmulq_n_u32
extern int16x8_t np_vqdmulhq_n_s16(int16x8_t vec1, int16_t val2);
#define vqdmulhq_n_s16 np_vqdmulhq_n_s16
extern int32x4_t np_vqdmulhq_n_s32(int32x4_t vec1, int32_t val2);
#define vqdmulhq_n_s32 np_vqdmulhq_n_s32
extern int16x8_t np_vqrdmulhq_n_s16(int16x8_t vec1, int16_t val2);
#define vqrdmulhq_n_s16 np_vqrdmulhq_n_s16
extern int32x4_t np_vqrdmulhq_n_s32(int32x4_t vec1, int32_t val2);
#define vqrdmulhq_n_s32 np_vqrdmulhq_n_s32
extern int16x8_t np_vmlaq_n_s16(int16x8_t a, int16x8_t b, int16_t c);
#define vmlaq_n_s16 np_vmlaq_n_s16
extern int32x4_t np_vmlaq_n_s32(int32x4_t a, int32x4_t b, int32_t c);
#define vmlaq_n_s32 np_vmlaq_n_s32
extern uint16x8_t np_vmlaq_n_u16(uint16x8_t a, uint16x8_t b, uint16_t c);
#define vmlaq_n_u16 np_vmlaq_n_u16
extern uint32x4_t np_vmlaq_n_u32(uint32x4_t a, uint32x4_t b, uint32_t c);
#define vmlaq_n_u32 np_vmlaq_n_u32
extern float32x4_t np_vmlaq_n_f32(float32x4_t a, float32x4_t b, float32_t c);
#define vmlaq_n_f32 np_vmlaq_n_f32
extern int16x8_t np_vmlsq_n_s16(int16x8_t a, int16x8_t b, int16_t c);
#define vmlsq_n_s16 np_vmlsq_n_s16
extern int32x4_t np_vmlsq_n_s32(int32x4_t a, int32x4_t b, int32_t c);
#define vmlsq_n_s32 np_vmlsq_n_s32
extern uint16x8_t np_vmlsq_n_u16(uint16x8_t a, uint16x8_t b, uint16_t c);
#define vmlsq_n_u16 np_vmlsq_n_u16
extern uint32x4_t np_vmlsq_n_u32(uint32x4_t a, uint32x4_t b, uint32_t c);
#define vmlsq_n_u32 np_vmlsq_n_u32
extern float32x4_t np_vmlsq_n_f32(float32x4_t a, float32x4_t b, float32_t c);
#define vmlsq_n_f32 np_vmlsq_n_f32
extern int8x16_t np_vextq_s8_0(int8x16_t a, int8x16_t b);
extern int8x16_t np_vextq_s8_1(int8x16_t a, int8x16_t b);
extern int8x16_t np_vextq_s8_2(int8x16_t a, int8x16_t b);
extern int8x16_t np_vextq_s8_3(int8x16_t a, int8x16_t b);
extern int8x16_t np_vextq_s8_4(int8x16_t a, int8x16_t b);
extern int8x16_t np_vextq_s8_5(int8x16_t a, int8x16_t b);
extern int8x16_t np_vextq_s8_6(int8x16_t a, int8x16_t b);
extern int8x16_t np_vextq_s8_7(int8x16_t a, int8x16_t b);
extern int8x16_t np_vextq_s8_8(int8x16_t a, int8x16_t b);
extern int8x16_t np_vextq_s8_9(int8x16_t a, int8x16_t b);
extern int8x16_t np_vextq_s8_10(int8x16_t a, int8x16_t b);
extern int8x16_t np_vextq_s8_11(int8x16_t a, int8x16_t b);
extern int8x16_t np_vextq_s8_12(int8x16_t a, int8x16_t b);
extern int8x16_t np_vextq_s8_13(int8x16_t a, int8x16_t b);
extern int8x16_t np_vextq_s8_14(int8x16_t a, int8x16_t b);
extern int8x16_t np_vextq_s8_15(int8x16_t a, int8x16_t b);
#define vextq_s8(__a, __b, __c) np_vextq_s8_##__c(__a, __b)
extern uint8x16_t np_vextq_u8_0(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vextq_u8_1(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vextq_u8_2(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vextq_u8_3(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vextq_u8_4(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vextq_u8_5(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vextq_u8_6(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vextq_u8_7(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vextq_u8_8(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vextq_u8_9(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vextq_u8_10(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vextq_u8_11(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vextq_u8_12(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vextq_u8_13(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vextq_u8_14(uint8x16_t a, uint8x16_t b);
extern uint8x16_t np_vextq_u8_15(uint8x16_t a, uint8x16_t b);
#define vextq_u8(__a, __b, __c) np_vextq_u8_##__c(__a, __b)
extern poly8x16_t np_vextq_p8_0(poly8x16_t a, poly8x16_t b);
extern poly8x16_t np_vextq_p8_1(poly8x16_t a, poly8x16_t b);
extern poly8x16_t np_vextq_p8_2(poly8x16_t a, poly8x16_t b);
extern poly8x16_t np_vextq_p8_3(poly8x16_t a, poly8x16_t b);
extern poly8x16_t np_vextq_p8_4(poly8x16_t a, poly8x16_t b);
extern poly8x16_t np_vextq_p8_5(poly8x16_t a, poly8x16_t b);
extern poly8x16_t np_vextq_p8_6(poly8x16_t a, poly8x16_t b);
extern poly8x16_t np_vextq_p8_7(poly8x16_t a, poly8x16_t b);
extern poly8x16_t np_vextq_p8_8(poly8x16_t a, poly8x16_t b);
extern poly8x16_t np_vextq_p8_9(poly8x16_t a, poly8x16_t b);
extern poly8x16_t np_vextq_p8_10(poly8x16_t a, poly8x16_t b);
extern poly8x16_t np_vextq_p8_11(poly8x16_t a, poly8x16_t b);
extern poly8x16_t np_vextq_p8_12(poly8x16_t a, poly8x16_t b);
extern poly8x16_t np_vextq_p8_13(poly8x16_t a, poly8x16_t b);
extern poly8x16_t np_vextq_p8_14(poly8x16_t a, poly8x16_t b);
extern poly8x16_t np_vextq_p8_15(poly8x16_t a, poly8x16_t b);
#define vextq_p8(__a, __b, __c) np_vextq_p8_##__c(__a, __b)
extern int16x8_t np_vextq_s16_0(int16x8_t a, int16x8_t b);
extern int16x8_t np_vextq_s16_1(int16x8_t a, int16x8_t b);
extern int16x8_t np_vextq_s16_2(int16x8_t a, int16x8_t b);
extern int16x8_t np_vextq_s16_3(int16x8_t a, int16x8_t b);
extern int16x8_t np_vextq_s16_4(int16x8_t a, int16x8_t b);
extern int16x8_t np_vextq_s16_5(int16x8_t a, int16x8_t b);
extern int16x8_t np_vextq_s16_6(int16x8_t a, int16x8_t b);
extern int16x8_t np_vextq_s16_7(int16x8_t a, int16x8_t b);
#define vextq_s16(__a, __b, __c) np_vextq_s16_##__c(__a, __b)
extern uint16x8_t np_vextq_u16_0(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vextq_u16_1(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vextq_u16_2(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vextq_u16_3(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vextq_u16_4(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vextq_u16_5(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vextq_u16_6(uint16x8_t a, uint16x8_t b);
extern uint16x8_t np_vextq_u16_7(uint16x8_t a, uint16x8_t b);
#define vextq_u16(__a, __b, __c) np_vextq_u16_##__c(__a, __b)
extern poly16x8_t np_vextq_p16_0(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vextq_p16_1(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vextq_p16_2(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vextq_p16_3(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vextq_p16_4(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vextq_p16_5(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vextq_p16_6(poly16x8_t a, poly16x8_t b);
extern poly16x8_t np_vextq_p16_7(poly16x8_t a, poly16x8_t b);
#define vextq_p16(__a, __b, __c) np_vextq_p16_##__c(__a, __b)
extern int32x4_t np_vextq_s32_0(int32x4_t a, int32x4_t b);
extern int32x4_t np_vextq_s32_1(int32x4_t a, int32x4_t b);
extern int32x4_t np_vextq_s32_2(int32x4_t a, int32x4_t b);
extern int32x4_t np_vextq_s32_3(int32x4_t a, int32x4_t b);
#define vextq_s32(__a, __b, __c) np_vextq_s32_##__c(__a, __b)
extern uint32x4_t np_vextq_u32_0(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vextq_u32_1(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vextq_u32_2(uint32x4_t a, uint32x4_t b);
extern uint32x4_t np_vextq_u32_3(uint32x4_t a, uint32x4_t b);
#define vextq_u32(__a, __b, __c) np_vextq_u32_##__c(__a, __b)
extern int64x2_t np_vextq_s64_0(int64x2_t a, int64x2_t b);
extern int64x2_t np_vextq_s64_1(int64x2_t a, int64x2_t b);
#define vextq_s64(__a, __b, __c) np_vextq_s64_##__c(__a, __b)
extern uint64x2_t np_vextq_u64_0(uint64x2_t a, uint64x2_t b);
extern uint64x2_t np_vextq_u64_1(uint64x2_t a, uint64x2_t b);
#define vextq_u64(__a, __b, __c) np_vextq_u64_##__c(__a, __b)
extern float32x4_t np_vextq_f32_0(float32x4_t a, float32x4_t b);
extern float32x4_t np_vextq_f32_1(float32x4_t a, float32x4_t b);
extern float32x4_t np_vextq_f32_2(float32x4_t a, float32x4_t b);
extern float32x4_t np_vextq_f32_3(float32x4_t a, float32x4_t b);
#define vextq_f32(__a, __b, __c) np_vextq_f32_##__c(__a, __b)
extern int8x16_t np_vrev64q_s8(int8x16_t vec);
#define vrev64q_s8 np_vrev64q_s8
extern int16x8_t np_vrev64q_s16(int16x8_t vec);
#define vrev64q_s16 np_vrev64q_s16
extern int32x4_t np_vrev64q_s32(int32x4_t vec);
#define vrev64q_s32 np_vrev64q_s32
extern uint8x16_t np_vrev64q_u8(uint8x16_t vec);
#define vrev64q_u8 np_vrev64q_u8
extern uint16x8_t np_vrev64q_u16(uint16x8_t vec);
#define vrev64q_u16 np_vrev64q_u16
extern uint32x4_t np_vrev64q_u32(uint32x4_t vec);
#define vrev64q_u32 np_vrev64q_u32
extern poly8x16_t np_vrev64q_p8(poly8x16_t vec);
#define vrev64q_p8 np_vrev64q_p8
extern poly16x8_t np_vrev64q_p16(poly16x8_t vec);
#define vrev64q_p16 np_vrev64q_p16
extern float32x4_t np_vrev64q_f32(float32x4_t vec);
#define vrev64q_f32 np_vrev64q_f32
extern int8x16_t np_vrev32q_s8(int8x16_t vec);
#define vrev32q_s8 np_vrev32q_s8
extern int16x8_t np_vrev32q_s16(int16x8_t vec);
#define vrev32q_s16 np_vrev32q_s16
extern uint8x16_t np_vrev32q_u8(uint8x16_t vec);
#define vrev32q_u8 np_vrev32q_u8
extern uint16x8_t np_vrev32q_u16(uint16x8_t vec);
#define vrev32q_u16 np_vrev32q_u16
extern poly8x16_t np_vrev32q_p8(poly8x16_t vec);
#define vrev32q_p8 np_vrev32q_p8
extern poly16x8_t np_vrev32q_p16(poly16x8_t vec);
#define vrev32q_p16 np_vrev32q_p16
extern int8x16_t np_vrev16q_s8(int8x16_t vec);
#define vrev16q_s8 np_vrev16q_s8
extern uint8x16_t np_vrev16q_u8(uint8x16_t vec);
#define vrev16q_u8 np_vrev16q_u8
extern poly8x16_t np_vrev16q_p8(poly8x16_t vec);
#define vrev16q_p8 np_vrev16q_p8
extern int8x16_t np_vabsq_s8(int8x16_t a);
#define vabsq_s8 np_vabsq_s8
extern int16x8_t np_vabsq_s16(int16x8_t a);
#define vabsq_s16 np_vabsq_s16
extern int32x4_t np_vabsq_s32(int32x4_t a);
#define vabsq_s32 np_vabsq_s32
extern float32x4_t np_vabsq_f32(float32x4_t a);
#define vabsq_f32 np_vabsq_f32
extern int8x16_t np_vqabsq_s8(int8x16_t a);
#define vqabsq_s8 np_vqabsq_s8
extern int16x8_t np_vqabsq_s16(int16x8_t a);
#define vqabsq_s16 np_vqabsq_s16
extern int32x4_t np_vqabsq_s32(int32x4_t a);
#define vqabsq_s32 np_vqabsq_s32
extern int8x16_t np_vnegq_s8(int8x16_t a);
#define vnegq_s8 np_vnegq_s8
extern int16x8_t np_vnegq_s16(int16x8_t a);
#define vnegq_s16 np_vnegq_s16
extern int32x4_t np_vnegq_s32(int32x4_t a);
#define vnegq_s32 np_vnegq_s32
extern float32x4_t np_vnegq_f32(float32x4_t a);
#define vnegq_f32 np_vnegq_f32
extern int8x16_t np_vqnegq_s8(int8x16_t a);
#define vqnegq_s8 np_vqnegq_s8
extern int16x8_t np_vqnegq_s16(int16x8_t a);
#define vqnegq_s16 np_vqnegq_s16
extern int32x4_t np_vqnegq_s32(int32x4_t a);
#define vqnegq_s32 np_vqnegq_s32
extern int8x16_t np_vclsq_s8(int8x16_t a);
#define vclsq_s8 np_vclsq_s8
extern int16x8_t np_vclsq_s16(int16x8_t a);
#define vclsq_s16 np_vclsq_s16
extern int32x4_t np_vclsq_s32(int32x4_t a);
#define vclsq_s32 np_vclsq_s32
extern int8x16_t np_vclzq_s8(int8x16_t a);
#define vclzq_s8 np_vclzq_s8
extern int16x8_t np_vclzq_s16(int16x8_t a);
#define vclzq_s16 np_vclzq_s16
extern int32x4_t np_vclzq_s32(int32x4_t a);
#define vclzq_s32 np_vclzq_s32
extern uint8x16_t np_vclzq_u8(uint8x16_t a);
#define vclzq_u8 np_vclzq_u8
extern uint16x8_t np_vclzq_u16(uint16x8_t a);
#define vclzq_u16 np_vclzq_u16
extern uint32x4_t np_vclzq_u32(uint32x4_t a);
#define vclzq_u32 np_vclzq_u32
extern uint8x16_t np_vcntq_u8(uint8x16_t a);
#define vcntq_u8 np_vcntq_u8
extern int8x16_t np_vcntq_s8(int8x16_t a);
#define vcntq_s8 np_vcntq_s8
extern poly8x16_t np_vcntq_p8(poly8x16_t a);
#define vcntq_p8 np_vcntq_p8
extern float32x4_t np_vrecpeq_f32(float32x4_t a);
#define vrecpeq_f32 np_vrecpeq_f32
extern uint32x4_t np_vrecpeq_u32(uint32x4_t a);
#define vrecpeq_u32 np_vrecpeq_u32
extern float32x4_t np_vrsqrteq_f32(float32x4_t a);
#define vrsqrteq_f32 np_vrsqrteq_f32
extern uint32x4_t np_vrsqrteq_u32(uint32x4_t a);
#define vrsqrteq_u32 np_vrsqrteq_u32
extern int8x16_t np_vmvnq_s8(int8x16_t a);
#define vmvnq_s8 np_vmvnq_s8
extern int16x8_t np_vmvnq_s16(int16x8_t a);
#define vmvnq_s16 np_vmvnq_s16
extern int32x4_t np_vmvnq_s32(int32x4_t a);
#define vmvnq_s32 np_vmvnq_s32
extern uint8x16_t np_vmvnq_u8(uint8x16_t a);
#define vmvnq_u8 np_vmvnq_u8
extern uint16x8_t np_vmvnq_u16(uint16x8_t a);
#define vmvnq_u16 np_vmvnq_u16
extern uint32x4_t np_vmvnq_u32(uint32x4_t a);
#define vmvnq_u32 np_vmvnq_u32
extern poly8x16_t np_vmvnq_p8(poly8x16_t a);
#define vmvnq_p8 np_vmvnq_p8
extern int8x16_t np_vandq_s8(int8x16_t a, int8x16_t b);
#define vandq_s8 np_vandq_s8
extern int16x8_t np_vandq_s16(int16x8_t a, int16x8_t b);
#define vandq_s16 np_vandq_s16
extern int32x4_t np_vandq_s32(int32x4_t a, int32x4_t b);
#define vandq_s32 np_vandq_s32
extern int64x2_t np_vandq_s64(int64x2_t a, int64x2_t b);
#define vandq_s64 np_vandq_s64
extern uint8x16_t np_vandq_u8(uint8x16_t a, uint8x16_t b);
#define vandq_u8 np_vandq_u8
extern uint16x8_t np_vandq_u16(uint16x8_t a, uint16x8_t b);
#define vandq_u16 np_vandq_u16
extern uint32x4_t np_vandq_u32(uint32x4_t a, uint32x4_t b);
#define vandq_u32 np_vandq_u32
extern uint64x2_t np_vandq_u64(uint64x2_t a, uint64x2_t b);
#define vandq_u64 np_vandq_u64
extern int8x16_t np_vorrq_s8(int8x16_t a, int8x16_t b);
#define vorrq_s8 np_vorrq_s8
extern int16x8_t np_vorrq_s16(int16x8_t a, int16x8_t b);
#define vorrq_s16 np_vorrq_s16
extern int32x4_t np_vorrq_s32(int32x4_t a, int32x4_t b);
#define vorrq_s32 np_vorrq_s32
extern int64x2_t np_vorrq_s64(int64x2_t a, int64x2_t b);
#define vorrq_s64 np_vorrq_s64
extern uint8x16_t np_vorrq_u8(uint8x16_t a, uint8x16_t b);
#define vorrq_u8 np_vorrq_u8
extern uint16x8_t np_vorrq_u16(uint16x8_t a, uint16x8_t b);
#define vorrq_u16 np_vorrq_u16
extern uint32x4_t np_vorrq_u32(uint32x4_t a, uint32x4_t b);
#define vorrq_u32 np_vorrq_u32
extern uint64x2_t np_vorrq_u64(uint64x2_t a, uint64x2_t b);
#define vorrq_u64 np_vorrq_u64
extern int8x16_t np_veorq_s8(int8x16_t a, int8x16_t b);
#define veorq_s8 np_veorq_s8
extern int16x8_t np_veorq_s16(int16x8_t a, int16x8_t b);
#define veorq_s16 np_veorq_s16
extern int32x4_t np_veorq_s32(int32x4_t a, int32x4_t b);
#define veorq_s32 np_veorq_s32
extern int64x2_t np_veorq_s64(int64x2_t a, int64x2_t b);
#define veorq_s64 np_veorq_s64
extern uint8x16_t np_veorq_u8(uint8x16_t a, uint8x16_t b);
#define veorq_u8 np_veorq_u8
extern uint16x8_t np_veorq_u16(uint16x8_t a, uint16x8_t b);
#define veorq_u16 np_veorq_u16
extern uint32x4_t np_veorq_u32(uint32x4_t a, uint32x4_t b);
#define veorq_u32 np_veorq_u32
extern uint64x2_t np_veorq_u64(uint64x2_t a, uint64x2_t b);
#define veorq_u64 np_veorq_u64
extern int8x16_t np_vbicq_s8(int8x16_t a, int8x16_t b);
#define vbicq_s8 np_vbicq_s8
extern int16x8_t np_vbicq_s16(int16x8_t a, int16x8_t b);
#define vbicq_s16 np_vbicq_s16
extern int32x4_t np_vbicq_s32(int32x4_t a, int32x4_t b);
#define vbicq_s32 np_vbicq_s32
extern int64x2_t np_vbicq_s64(int64x2_t a, int64x2_t b);
#define vbicq_s64 np_vbicq_s64
extern uint8x16_t np_vbicq_u8(uint8x16_t a, uint8x16_t b);
#define vbicq_u8 np_vbicq_u8
extern uint16x8_t np_vbicq_u16(uint16x8_t a, uint16x8_t b);
#define vbicq_u16 np_vbicq_u16
extern uint32x4_t np_vbicq_u32(uint32x4_t a, uint32x4_t b);
#define vbicq_u32 np_vbicq_u32
extern uint64x2_t np_vbicq_u64(uint64x2_t a, uint64x2_t b);
#define vbicq_u64 np_vbicq_u64
extern int8x16_t np_vornq_s8(int8x16_t a, int8x16_t b);
#define vornq_s8 np_vornq_s8
extern int16x8_t np_vornq_s16(int16x8_t a, int16x8_t b);
#define vornq_s16 np_vornq_s16
extern int32x4_t np_vornq_s32(int32x4_t a, int32x4_t b);
#define vornq_s32 np_vornq_s32
extern int64x2_t np_vornq_s64(int64x2_t a, int64x2_t b);
#define vornq_s64 np_vornq_s64
extern uint8x16_t np_vornq_u8(uint8x16_t a, uint8x16_t b);
#define vornq_u8 np_vornq_u8
extern uint16x8_t np_vornq_u16(uint16x8_t a, uint16x8_t b);
#define vornq_u16 np_vornq_u16
extern uint32x4_t np_vornq_u32(uint32x4_t a, uint32x4_t b);
#define vornq_u32 np_vornq_u32
extern uint64x2_t np_vornq_u64(uint64x2_t a, uint64x2_t b);
#define vornq_u64 np_vornq_u64
extern int8x16_t np_vbslq_s8(uint8x16_t a, int8x16_t b, int8x16_t c);
#define vbslq_s8 np_vbslq_s8
extern int16x8_t np_vbslq_s16(uint16x8_t a, int16x8_t b, int16x8_t c);
#define vbslq_s16 np_vbslq_s16
extern int32x4_t np_vbslq_s32(uint32x4_t a, int32x4_t b, int32x4_t c);
#define vbslq_s32 np_vbslq_s32
extern int64x2_t np_vbslq_s64(uint64x2_t a, int64x2_t b, int64x2_t c);
#define vbslq_s64 np_vbslq_s64
extern uint8x16_t np_vbslq_u8(uint8x16_t a, uint8x16_t b, uint8x16_t c);
#define vbslq_u8 np_vbslq_u8
extern uint16x8_t np_vbslq_u16(uint16x8_t a, uint16x8_t b, uint16x8_t c);
#define vbslq_u16 np_vbslq_u16
extern uint32x4_t np_vbslq_u32(uint32x4_t a, uint32x4_t b, uint32x4_t c);
#define vbslq_u32 np_vbslq_u32
extern uint64x2_t np_vbslq_u64(uint64x2_t a, uint64x2_t b, uint64x2_t c);
#define vbslq_u64 np_vbslq_u64
extern float32x4_t np_vbslq_f32(uint32x4_t a, float32x4_t b, float32x4_t c);
#define vbslq_f32 np_vbslq_f32
extern poly8x16_t np_vbslq_p8(uint8x16_t a, poly8x16_t b, poly8x16_t c);
#define vbslq_p8 np_vbslq_p8
extern poly16x8_t np_vbslq_p16(uint16x8_t a, poly16x8_t b, poly16x8_t c);
#define vbslq_p16 np_vbslq_p16
extern int8x16x2_t np_vtrnq_s8(int8x16_t a, int8x16_t b);
#define vtrnq_s8 np_vtrnq_s8
extern int16x8x2_t np_vtrnq_s16(int16x8_t a, int16x8_t b);
#define vtrnq_s16 np_vtrnq_s16
extern int32x4x2_t np_vtrnq_s32(int32x4_t a, int32x4_t b);
#define vtrnq_s32 np_vtrnq_s32
extern uint8x16x2_t np_vtrnq_u8(uint8x16_t a, uint8x16_t b);
#define vtrnq_u8 np_vtrnq_u8
extern uint16x8x2_t np_vtrnq_u16(uint16x8_t a, uint16x8_t b);
#define vtrnq_u16 np_vtrnq_u16
extern uint32x4x2_t np_vtrnq_u32(uint32x4_t a, uint32x4_t b);
#define vtrnq_u32 np_vtrnq_u32
extern float32x4x2_t np_vtrnq_f32(float32x4_t a, float32x4_t b);
#define vtrnq_f32 np_vtrnq_f32
extern poly8x16x2_t np_vtrnq_p8(poly8x16_t a, poly8x16_t b);
#define vtrnq_p8 np_vtrnq_p8
extern poly16x8x2_t np_vtrnq_p16(poly16x8_t a, poly16x8_t b);
#define vtrnq_p16 np_vtrnq_p16
extern int8x16x2_t np_vzipq_s8(int8x16_t a, int8x16_t b);
#define vzipq_s8 np_vzipq_s8
extern int16x8x2_t np_vzipq_s16(int16x8_t a, int16x8_t b);
#define vzipq_s16 np_vzipq_s16
extern int32x4x2_t np_vzipq_s32(int32x4_t a, int32x4_t b);
#define vzipq_s32 np_vzipq_s32
extern uint8x16x2_t np_vzipq_u8(uint8x16_t a, uint8x16_t b);
#define vzipq_u8 np_vzipq_u8
extern uint16x8x2_t np_vzipq_u16(uint16x8_t a, uint16x8_t b);
#define vzipq_u16 np_vzipq_u16
extern uint32x4x2_t np_vzipq_u32(uint32x4_t a, uint32x4_t b);
#define vzipq_u32 np_vzipq_u32
extern float32x4x2_t np_vzipq_f32(float32x4_t a, float32x4_t b);
#define vzipq_f32 np_vzipq_f32
extern poly8x16x2_t np_vzipq_p8(poly8x16_t a, poly8x16_t b);
#define vzipq_p8 np_vzipq_p8
extern poly16x8x2_t np_vzipq_p16(poly16x8_t a, poly16x8_t b);
#define vzipq_p16 np_vzipq_p16
extern int8x16x2_t np_vuzpq_s8(int8x16_t a, int8x16_t b);
#define vuzpq_s8 np_vuzpq_s8
extern int16x8x2_t np_vuzpq_s16(int16x8_t a, int16x8_t b);
#define vuzpq_s16 np_vuzpq_s16
extern int32x4x2_t np_vuzpq_s32(int32x4_t a, int32x4_t b);
#define vuzpq_s32 np_vuzpq_s32
extern uint8x16x2_t np_vuzpq_u8(uint8x16_t a, uint8x16_t b);
#define vuzpq_u8 np_vuzpq_u8
extern uint16x8x2_t np_vuzpq_u16(uint16x8_t a, uint16x8_t b);
#define vuzpq_u16 np_vuzpq_u16
extern uint32x4x2_t np_vuzpq_u32(uint32x4_t a, uint32x4_t b);
#define vuzpq_u32 np_vuzpq_u32
extern float32x4x2_t np_vuzpq_f32(float32x4_t a, float32x4_t b);
#define vuzpq_f32 np_vuzpq_f32
extern poly8x16x2_t np_vuzpq_p8(poly8x16_t a, poly8x16_t b);
#define vuzpq_p8 np_vuzpq_p8
extern poly16x8x2_t np_vuzpq_p16(poly16x8_t a, poly16x8_t b);
#define vuzpq_p16 np_vuzpq_p16

#ifdef __cplusplus
}
#endif

#endif