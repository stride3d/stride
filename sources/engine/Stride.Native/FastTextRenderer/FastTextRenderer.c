// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#include "../../../deps/NativePath/NativePath.h"
#include "../../../deps/NativePath/NativeMath.h"
#include "../../../deps/NativePath/standard/math.h"
#include "../StrideNative.h"

#ifdef __cplusplus
extern "C" {
#endif
	static VertexPositionNormalTexture BaseVertexBufferData[4] =
	{
		// Position		Normal		UV Coordinates
		{ { -1, 1, 0 },{ 0, 0, 1 },{ 0, 0 } },
		{ { 1, 1, 0 },{ 0, 0, 1 },{ 1, 0 } },
		{ { -1, -1, 0 },{ 0, 0, 1 },{ 0, 1 } },
		{ { 1, -1, 0 },{ 0, 0, 1 },{ 1, 1 } },
	};

	DLL_EXPORT_API void xnGraphicsFastTextRendererGenerateVertices(RectangleF constantInfos, RectangleF renderInfos, const char* textPointer, int** textLength, VertexPositionNormalTexture** vertexBufferPointer)
	{
		const float fX = renderInfos.x / renderInfos.width;
		const float fY = renderInfos.y / renderInfos.height;
		const float fW = constantInfos.x / renderInfos.width;
		const float fH = constantInfos.y / renderInfos.height;

		RectangleF destination = { fX, fY, fW, fH };
		RectangleF source = { 0.0f, 0.0f, constantInfos.x, constantInfos.y };

		// Copy the array length (since it may change during an iteration)
		const int textCharCount = **textLength;

		float scaledDestinationX = 0.0f;
		float scaledDestinationY = -(destination.y * 2.0f - 1.0f);

		const float invertedWidth = 1.0f / constantInfos.width;
		const float invertedHeight = 1.0f / constantInfos.height;

		for (int i = 0; i < textCharCount; i++)
		{
			char currentChar = textPointer[i];

			if (currentChar == 11)
			{
				// Tabulation
				destination.x += 8 * fX;
				--**textLength;
				continue;
			}
			else if (currentChar >= 10 && currentChar <= 13)
			{
				// New Line
				destination.x = fX;
				destination.y += fH;
				scaledDestinationY = -(destination.y * 2.0f - 1.0f);
				--**textLength;
				continue;
			}
			else if (currentChar < 32 || currentChar > 126)
			{
				currentChar = 32;
			}

			source.x = ((float)(currentChar % 32)) * constantInfos.x;
			source.y = ((float)((currentChar / 32) % 4)) * constantInfos.y;

			scaledDestinationX = (destination.x * 2.0f - 1.0f);

			// 0
			(*vertexBufferPointer)->Position.X = scaledDestinationX + BaseVertexBufferData[0].Position.X * destination.width;
			(*vertexBufferPointer)->Position.Y = scaledDestinationY + BaseVertexBufferData[0].Position.Y * destination.height;

			(*vertexBufferPointer)->TextureCoordinate.X = (source.x + BaseVertexBufferData[0].TextureCoordinate.X * source.width) * invertedWidth;
			(*vertexBufferPointer)->TextureCoordinate.Y = (source.y + BaseVertexBufferData[0].TextureCoordinate.Y * source.height) * invertedHeight;

			++(*vertexBufferPointer);

			// 1
			(*vertexBufferPointer)->Position.X = scaledDestinationX + BaseVertexBufferData[1].Position.X * destination.width;
			(*vertexBufferPointer)->Position.Y = scaledDestinationY + BaseVertexBufferData[1].Position.Y * destination.height;

			(*vertexBufferPointer)->TextureCoordinate.X = (source.x + BaseVertexBufferData[1].TextureCoordinate.X * source.width) * invertedWidth;
			(*vertexBufferPointer)->TextureCoordinate.Y = (source.y + BaseVertexBufferData[1].TextureCoordinate.Y * source.height) * invertedHeight;

			++(*vertexBufferPointer);

			// 2
			(*vertexBufferPointer)->Position.X = scaledDestinationX + BaseVertexBufferData[2].Position.X * destination.width;
			(*vertexBufferPointer)->Position.Y = scaledDestinationY + BaseVertexBufferData[2].Position.Y * destination.height;

			(*vertexBufferPointer)->TextureCoordinate.X = (source.x + BaseVertexBufferData[2].TextureCoordinate.X * source.width) * invertedWidth;
			(*vertexBufferPointer)->TextureCoordinate.Y = (source.y + BaseVertexBufferData[2].TextureCoordinate.Y * source.height) * invertedHeight;

			++(*vertexBufferPointer);

			// 3
			(*vertexBufferPointer)->Position.X = scaledDestinationX + BaseVertexBufferData[3].Position.X * destination.width;
			(*vertexBufferPointer)->Position.Y = scaledDestinationY + BaseVertexBufferData[3].Position.Y * destination.height;

			(*vertexBufferPointer)->TextureCoordinate.X = (source.x + BaseVertexBufferData[3].TextureCoordinate.X * source.width) * invertedWidth;
			(*vertexBufferPointer)->TextureCoordinate.Y = (source.y + BaseVertexBufferData[3].TextureCoordinate.Y * source.height) * invertedHeight;

			++(*vertexBufferPointer);

			destination.x += destination.width;
		}
	}

#ifdef __cplusplus
}
#endif
