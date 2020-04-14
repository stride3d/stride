// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma once

#include <assimp/scene.h>

#define _AI_MATKEY_TEXTYPE_BASE "$tex.type"
#define _AI_MATKEY_TEXCOLOR_BASE "$tex.color"
#define _AI_MATKEY_TEXALPHA_BASE "$tex.alpha"

#define AI_MATKEY_TEXTYPE(type, N) _AI_MATKEY_TEXTYPE_BASE,type,N
#define AI_MATKEY_TEXCOLOR(type,N) _AI_MATKEY_TEXCOLOR_BASE,type,N
#define AI_MATKEY_TEXALPHA(type,N) _AI_MATKEY_TEXALPHA_BASE,type,N

/// <summary>
/// Enumeration of the different types of node in the new Assimp's material stack.
/// Don't forget to update the dictionnary in Materials.cpp when modifying this enum.
/// </summary>
enum aiStackType {
	aiStackType_ColorType,
	aiStackType_TextureType,
	aiStackType_BlendOpType,
	aiStackType_NumberTypes
};
/// <summary>
/// Enumeration of the new Assimp's flags.
/// </summary>
enum aiStackFlags {
	aiStackFlags_Invert = 1,
	aiStackFlags_ReplaceAlpha = 2
};
#define aiStackFlags_NumbeFlags 2
/// <summary>
/// Enumeration of the different operations in the new Assimp's material stack.
/// Don't forget to update the dictionnary in Materials.cpp when modifying this enum.
/// </summary>
enum aiStackOperation {
	aiStackOperation_Add = 0,
	aiStackOperation_Add3ds,
	aiStackOperation_AddMaya,
	aiStackOperation_Average,
	aiStackOperation_Color,
	aiStackOperation_ColorBurn,
	aiStackOperation_ColorDodge,
	aiStackOperation_Darken3ds,
	aiStackOperation_DarkenMaya,
	aiStackOperation_Desaturate,
	aiStackOperation_Difference3ds,
	aiStackOperation_DifferenceMaya,
	aiStackOperation_Divide,
	aiStackOperation_Exclusion,
	aiStackOperation_HardLight,
	aiStackOperation_HardMix,
	aiStackOperation_Hue,
	aiStackOperation_Illuminate,
	aiStackOperation_In,
	aiStackOperation_Lighten3ds,
	aiStackOperation_LightenMaya,
	aiStackOperation_LinearBurn,
	aiStackOperation_LinearDodge,
	aiStackOperation_Multiply3ds,
	aiStackOperation_MultiplyMaya,
	aiStackOperation_None,
	aiStackOperation_Out,
	aiStackOperation_Over3ds,
	aiStackOperation_Overlay3ds,
	aiStackOperation_OverMaya,
	aiStackOperation_PinLight,
	aiStackOperation_Saturate,
	aiStackOperation_Saturation,
	aiStackOperation_Screen,
	aiStackOperation_SoftLight,
	aiStackOperation_Substract3ds,
	aiStackOperation_SubstractMaya,
	aiStackOperation_Value,
	aiStackOperation_Mask,
	aiStackOperation_Unknown,
	aiStackOperation_NumberOperations
};
