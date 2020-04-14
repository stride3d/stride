// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#pragma once

/*
* Some platforms requires a special declaration before the function declaration to export them
* in the shared library. Defining NEED_DLL_EXPORT will define DLL_EXPORT_API to do the right thing
* for those platforms.
*
* To export void foo(int a), do:
*
*   DLL_EXPORT_API void foo (int a);
*/
#ifdef NEED_DLL_EXPORT
#define DLL_EXPORT_API __declspec(dllexport)
#else
#define DLL_EXPORT_API
#endif

#include "../../deps/NativePath/standard/math.h"

#ifdef __cplusplus
extern "C" {
#endif

#pragma pack(push, 4)

typedef struct FlatMatrix
{
	float M11; //0
	float M21; //1
	float M31; //2
	float M41; //3
	float M12; //4
	float M22; //5
	float M32; //6
	float M42; //7
	float M13; //8
	float M23; //9
	float M33; //10
	float M43; //11
	float M14; //12
	float M24; //13
	float M34; //14
	float M44; //15
} FlatMatrix;

typedef struct Matrix
{
	union
	{
		FlatMatrix Flat;

		float Array[16];
	};	
} Matrix;



inline void xnMatrixInvert(Matrix* m)
{
#ifdef __clang__
	typedef float float16 __attribute__((ext_vector_type(16)));
	//https://github.com/niswegmann/small-matrix-inverse/blob/master/invert4x4_llvm.h

	float16 rsrc, rdst;
	float16* src = &rsrc;
	float16* dst = &rdst;

	memcpy(src, m, sizeof(float16));

	float4 row0, row1, row2, row3;
	float4 col0, col1, col2, col3;
	float4 det, tmp1;

	/* Load matrix: */

	col0 = ((float4 *)src)[0];
	col1 = ((float4 *)src)[1];
	col2 = ((float4 *)src)[2];
	col3 = ((float4 *)src)[3];

	/* Transpose: */

	tmp1 = shufflevector(col0, col2, 0, 4, 1, 5);
	row1 = shufflevector(col1, col3, 0, 4, 1, 5);

	row0 = shufflevector(tmp1, row1, 0, 4, 1, 5);
	row1 = shufflevector(tmp1, row1, 2, 6, 3, 7);

	tmp1 = shufflevector(col0, col2, 2, 6, 3, 7);
	row3 = shufflevector(col1, col3, 2, 6, 3, 7);

	row2 = shufflevector(tmp1, row3, 0, 4, 1, 5);
	row3 = shufflevector(tmp1, row3, 2, 6, 3, 7);

	/* Compute adjoint: */

	row1 = shufflevector(row1, row1, 2, 3, 0, 1);
	row3 = shufflevector(row3, row3, 2, 3, 0, 1);

	tmp1 = row2 * row3;
	tmp1 = shufflevector(tmp1, tmp1, 1, 0, 7, 6);

	col0 = row1 * tmp1;
	col1 = row0 * tmp1;

	tmp1 = shufflevector(tmp1, tmp1, 2, 3, 4, 5);

	col0 = row1 * tmp1 - col0;
	col1 = row0 * tmp1 - col1;
	col1 = shufflevector(col1, col1, 2, 3, 4, 5);

	tmp1 = row1 * row2;
	tmp1 = shufflevector(tmp1, tmp1, 1, 0, 7, 6);

	col0 = row3 * tmp1 + col0;
	col3 = row0 * tmp1;

	tmp1 = shufflevector(tmp1, tmp1, 2, 3, 4, 5);

	col0 = col0 - row3 * tmp1;
	col3 = row0 * tmp1 - col3;
	col3 = shufflevector(col3, col3, 2, 3, 4, 5);

	tmp1 = shufflevector(row1, row1, 2, 3, 4, 5) * row3;
	tmp1 = shufflevector(tmp1, tmp1, 1, 0, 7, 6);
	row2 = shufflevector(row2, row2, 2, 3, 4, 5);

	col0 = row2 * tmp1 + col0;
	col2 = row0 * tmp1;

	tmp1 = shufflevector(tmp1, tmp1, 2, 3, 4, 5);

	col0 = col0 - row2 * tmp1;
	col2 = row0 * tmp1 - col2;
	col2 = shufflevector(col2, col2, 2, 3, 4, 5);

	tmp1 = row0 * row1;
	tmp1 = shufflevector(tmp1, tmp1, 1, 0, 7, 6);

	col2 = row3 * tmp1 + col2;
	col3 = row2 * tmp1 - col3;

	tmp1 = shufflevector(tmp1, tmp1, 2, 3, 4, 5);

	col2 = row3 * tmp1 - col2;
	col3 = col3 - row2 * tmp1;

	tmp1 = row0 * row3;
	tmp1 = shufflevector(tmp1, tmp1, 1, 0, 7, 6);

	col1 = col1 - row2 * tmp1;
	col2 = row1 * tmp1 + col2;

	tmp1 = shufflevector(tmp1, tmp1, 2, 3, 4, 5);

	col1 = row2 * tmp1 + col1;
	col2 = col2 - row1 * tmp1;

	tmp1 = row0 * row2;
	tmp1 = shufflevector(tmp1, tmp1, 1, 0, 7, 6);

	col1 = row3 * tmp1 + col1;
	col3 = col3 - row1 * tmp1;

	tmp1 = shufflevector(tmp1, tmp1, 2, 3, 4, 5);

	col1 = col1 - row3 * tmp1;
	col3 = row1 * tmp1 + col3;

	/* Compute determinant: */

	det = row0 * col0;
	det = shufflevector(det, det, 2, 3, 4, 5) + det;
	det = shufflevector(det, det, 1, 0, 7, 6) + det;

	/* Compute reciprocal of determinant: */

	det = 1.0f / det;

	/* Multiply matrix of cofactors with reciprocal of determinant: */

	col0 = col0 * det;
	col1 = col1 * det;
	col2 = col2 * det;
	col3 = col3 * det;

	/* Store inverted matrix: */

	((float4 *)dst)[0] = col0;
	((float4 *)dst)[1] = col1;
	((float4 *)dst)[2] = col2;
	((float4 *)dst)[3] = col3;

	memcpy(m, dst, sizeof(float16));
#endif
}

inline void xnMatrixTranspose(Matrix* m)
{
	float temp;
	temp = m->Flat.M21; m->Flat.M21 = m->Flat.M12; m->Flat.M12 = temp;
	temp = m->Flat.M31; m->Flat.M31 = m->Flat.M13; m->Flat.M13 = temp;
	temp = m->Flat.M41; m->Flat.M41 = m->Flat.M14; m->Flat.M14 = temp;

	temp = m->Flat.M32; m->Flat.M32 = m->Flat.M23; m->Flat.M23 = temp;
	temp = m->Flat.M42; m->Flat.M42 = m->Flat.M24; m->Flat.M24 = temp;

	temp = m->Flat.M43; m->Flat.M43 = m->Flat.M34; m->Flat.M34 = temp;
}

inline void xnMatrixMultiply(Matrix* left, Matrix* right, Matrix* out)
{
	xnMatrixTranspose(left);
	xnMatrixTranspose(right);
#ifdef __clang__
	float4 row1, row2, row3, row4;
	memcpy(&row1, &right->Array[0], sizeof(float4));
	memcpy(&row2, &right->Array[4], sizeof(float4));
	memcpy(&row3, &right->Array[8], sizeof(float4));
	memcpy(&row4, &right->Array[12], sizeof(float4));
	for (int i = 0; i < 4; i++)
	{
		float4 brod1, brod2, brod3, brod4;
		brod1.xyzw = left->Array[4 * i + 0];
		brod2.xyzw = left->Array[4 * i + 1];
		brod3.xyzw = left->Array[4 * i + 2];
		brod4.xyzw = left->Array[4 * i + 3];

		float4 b1xr1 = brod1 * row1;
		float4 b2xr2 = brod2 * row2;
		float4 b3xr3 = brod3 * row3;
		float4 b4xr4 = brod4 * row4;
		float4 b12pr12 = b1xr1 + b2xr2;
		float4 b34pr34 = b3xr3 + b4xr4;
		float4 row = b12pr12 + b34pr34;

		memcpy(&out->Array[4 * i], &row, sizeof(float4));
	}	
#endif
	xnMatrixTranspose(left);
	xnMatrixTranspose(right);
	xnMatrixTranspose(out);
}


typedef struct Vector2
{
	float X;
	float Y;
} Vector2;

typedef struct Vector3
{
	float X;
	float Y;
	float Z;
} Vector3;

typedef struct Vector4
{
	float X;
	float Y;
	float Z;
	float W;
} Vector4;

typedef struct Color4
{
	float R;
	float G;
	float B;
	float A;
} Color4;

typedef struct RectangleF
{
	float x;
	float y;
	float width;
	float height;
} RectangleF;

typedef struct BoundingBox
{
	Vector3 minimum;
	Vector3 maximum;
} BoundingBox;

typedef struct VertexPositionColorTextureSwizzle
{
	Vector4 Position;
	Color4 ColorScale;
	Color4 ColorAdd;
	Vector2 TextureCoordinate;
	float Swizzle;
} VertexPositionColorTextureSwizzle;

typedef struct VertexPositionNormalTexture
{
	Vector3 Position;
	Vector3 Normal;
	Vector2 TextureCoordinate;
} VertexPositionNormalTexture;
#pragma pack(pop)

#pragma pack(push, 8)
typedef struct SpriteDrawInfo
{
	RectangleF Source;
	RectangleF Destination;
	Vector2 Origin;
	float Rotation;
	float Depth;
	int SpriteEffects;
	Color4 ColorScale;
	Color4 ColorAdd;
	int Swizzle;
	Vector2 TextureSize;
	int Orientation;
} SpriteDrawInfo;
#pragma pack(pop)

#ifdef __cplusplus
}
#endif
