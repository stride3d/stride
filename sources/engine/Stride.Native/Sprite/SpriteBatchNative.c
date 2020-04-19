// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#include "../../../deps/NativePath/NativePath.h"
#include "../../../deps/NativePath/NativeMath.h"
#include "../../../deps/NativePath/standard/math.h"
#include "../StrideNative.h"

#ifdef __cplusplus
extern "C" {
#endif

DLL_EXPORT_API void UpdateBufferValuesFromElementInfo(SpriteDrawInfo* drawInfo, VertexPositionColorTextureSwizzle* vertexPointer, void* indexPointer, int vertexStartOffset)
{
	float deltaX = 1.0f / drawInfo->TextureSize.X;
	float deltaY = 1.0f / drawInfo->TextureSize.Y;

	Vector2 rotation;
	rotation.X = 1;
	rotation.Y = 0;

	if (fabs(drawInfo->Rotation) > 1e-6f)
	{
		npLolSincosf(drawInfo->Rotation, &rotation.Y, &rotation.X);
	}

	Vector2 origin = drawInfo->Origin;
	origin.X /= fmax(1e-6f, drawInfo->Source.width);
	origin.Y /= fmax(1e-6f, drawInfo->Source.height);

	const Vector2 cornerOffsets[] =
	{
		{0, 0 },
		{1, 0 },
		{1, 1 },
		{0, 1 }
	};

	for (int j = 0; j < 4; j++)
	{
		Vector2 corner = cornerOffsets[j];
		Vector2 position;
		position.X = (corner.X - origin.X) * drawInfo->Destination.width;
		position.Y = (corner.Y - origin.Y) * drawInfo->Destination.height;

		vertexPointer->Position.X = drawInfo->Destination.x + (position.X * rotation.X) - (position.Y * rotation.Y);
		vertexPointer->Position.Y = drawInfo->Destination.y + (position.X * rotation.Y) + (position.Y * rotation.X);
		vertexPointer->Position.Z = drawInfo->Depth;
		vertexPointer->Position.W = 1.0f;
		vertexPointer->ColorScale = drawInfo->ColorScale;
		vertexPointer->ColorAdd = drawInfo->ColorAdd;

		corner = cornerOffsets[((j ^ (int)drawInfo->SpriteEffects) + (int)drawInfo->Orientation) % 4];
		vertexPointer->TextureCoordinate.X = (drawInfo->Source.x + corner.X * drawInfo->Source.width) * deltaX;
		vertexPointer->TextureCoordinate.Y = (drawInfo->Source.y + corner.Y * drawInfo->Source.height) * deltaY;

		vertexPointer->Swizzle = (int)drawInfo->Swizzle;
		
		vertexPointer++;
	}
}

#ifdef __cplusplus
}
#endif
