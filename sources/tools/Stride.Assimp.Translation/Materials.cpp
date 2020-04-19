// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma once

#include <assimp/scene.h>
#include "Extension.h"

#pragma make_public(aiMaterial)
#pragma make_public(aiTextureType)

using namespace Stride::Core::Mathematics;
using namespace Stride::AssimpNet;
namespace Stride {
	namespace AssimpNet {
		namespace NetTranslation {
/// <summary>
/// Class used to translate Assimp's cpp materials datas to c#.
/// </summary>
public ref class Materials {
public:
	/// <summary>
	/// Converts an Assimp's material stack operation From c++ to c#.
	/// </summary>
	static array<Material::Operation>^ convertAssimpStackOperationCppToCs = {
		Material::Operation::Add,			//aiStackOperation_Add
		Material::Operation::Add3ds,		//aiStackOperation_Add3ds
		Material::Operation::AddMaya,		//aiStackOperation_AddMaya
		Material::Operation::Average,		//aiStackOperation_Average
		Material::Operation::Color,			//aiStackOperation_Color
		Material::Operation::ColorBurn,		//aiStackOperation_ColorBurn
		Material::Operation::ColorDodge,	//aiStackOperation_ColorDodge
		Material::Operation::Darken3ds,		//aiStackOperation_Darken3ds
		Material::Operation::DarkenMaya,	//aiStackOperation_DarkenMaya
		Material::Operation::Desaturate,	//aiStackOperation_Desaturate
		Material::Operation::Difference3ds,	//aiStackOperation_Difference3ds
		Material::Operation::DifferenceMaya,//aiStackOperation_DifferenceMaya
		Material::Operation::Divide,		//aiStackOperation_Divide
		Material::Operation::Exclusion,		//aiStackOperation_Exclusion
		Material::Operation::HardLight,		//aiStackOperation_HardLight
		Material::Operation::HardMix,		//aiStackOperation_HardMix
		Material::Operation::Hue,			//aiStackOperation_Hue
		Material::Operation::Illuminate,	//aiStackOperation_Illuminate
		Material::Operation::In,			//aiStackOperation_In
		Material::Operation::Lighten3ds,	//aiStackOperation_Lighten3ds
		Material::Operation::LightenMaya,	//aiStackOperation_LightenMaya
		Material::Operation::LinearBurn,	//aiStackOperation_LinearBurn
		Material::Operation::LinearDodge,	//aiStackOperation_LinearDodge
		Material::Operation::Multiply3ds,	//aiStackOperation_Multiply3ds
		Material::Operation::MultiplyMaya,	//aiStackOperation_MultiplyMaya
		Material::Operation::None,			//aiStackOperation_None
		Material::Operation::Out,			//aiStackOperation_Out
		Material::Operation::Over3ds,		//aiStackOperation_Over3ds
		Material::Operation::Overlay3ds,	//aiStackOperation_Overlay3ds
		Material::Operation::OverMaya,		//aiStackOperation_OverMaya
		Material::Operation::PinLight,		//aiStackOperation_PinLight
		Material::Operation::Saturate,		//aiStackOperation_Saturate
		Material::Operation::Saturation,	//aiStackOperation_Saturation
		Material::Operation::Screen,		//aiStackOperation_Screen
		Material::Operation::SoftLight,		//aiStackOperation_SoftLight
		Material::Operation::Substract3ds,	//aiStackOperation_Substract3ds
		Material::Operation::SubstractMaya,	//aiStackOperation_SubstractMaya
		Material::Operation::Value,			//aiStackOperation_Value
		Material::Operation::Mask			//aiStackOperation_Mask
	};
	/// <summary>
	/// Converts an Assimp's material stack node type From c++ to c#.
	/// </summary>
	static array<Material::StackType>^ convertAssimpStackTypeCppToCs = {
		Material::StackType::Color,			// aiStackType_ColorType
		Material::StackType::Texture,		// aiStackType_TextureType
		Material::StackType::Operation		// aiStackType_BlemdOpType
	};
	static array<Material::MappingMode>^ convertAssimpMappingModeCppToCs = {
		Material::MappingMode::Wrap,		// aiTextureMapMode_Wrap
		Material::MappingMode::Clamp,		// aiTextureMapMode_Clamp
		Material::MappingMode::Mirror,		// aiTextureMapMode_Mirror
		Material::MappingMode::Decal		// aiTextureMapMode_Decal
	};
	/// <summary>
	/// Converts an Assimp's material stack From c++ to c#.
	/// </summary>
	/// <param name="material">The material contaning the stack to convert.</param>
	/// <param name="type">The type of the stack to convert.</param>
	/// <returns></returns>
	static Material::Stack^ convertAssimpStackCppToCs(aiMaterial *material, const aiTextureType type) {
		Material::Stack^ ret = gcnew Material::Stack;
		unsigned int count = material->GetTextureCount(type);
		// As it is represented now, the base color is at the bottom of the stack

		/*aiString path;
		aiTextureMapping mapping;
		unsigned int index;
		float blend;
		aiTextureOp textureOp;
		aiTextureMapMode mapMode;
		material->GetTexture(type, 0, &path, &mapping, &index, &blend, &textureOp, &mapMode);*/

		/*
		if (type == aiTextureType_AMBIENT) {
			aiColor3D ambient;
			material->Get(AI_MATKEY_COLOR_AMBIENT, ambient);
			ret->Push(gcnew Material::StackColor(Color3(ambient.r, ambient.g, ambient.b), 1.f, 1.f, 0));
		}
		if (type == aiTextureType_DIFFUSE) {
			aiColor3D diffuse;
			material->Get(AI_MATKEY_COLOR_DIFFUSE, diffuse);
			ret->Push(gcnew Material::StackColor(Color3(diffuse.r, diffuse.g, diffuse.b), 1.f, 1.f, 0));
		}
		if (type == aiTextureType_SPECULAR) {
			aiColor3D specular;
			material->Get(AI_MATKEY_COLOR_SPECULAR, specular);
			ret->Push(gcnew Material::StackColor(Color3(specular.r, specular.g, specular.b), 1.f, 1.f, 0));
		}
		if (type == aiTextureType_EMISSIVE) {
			aiColor3D emissive;
			material->Get(AI_MATKEY_COLOR_EMISSIVE, emissive);
			ret->Push(gcnew Material::StackColor(Color3(emissive.r, emissive.g, emissive.b), 1.f, 1.f, 0));
		}
		*/
		// Process the material stack
		for (int iEl = count-1; iEl >= 0; --iEl) {
			Material::StackElement^ el;
			// Common properties
			int elType, elFlags;
			float elAlpha, elBlend;
			// Operation-specific properties
			int elOp;
			// Color-specific properties
			aiColor3D elColor;
			// Texture-specific properties
			aiString elTexPath;
			int elTexChannel, elMappingModeU, elMappingModeV;
			if (AI_SUCCESS != material->Get(AI_MATKEY_TEXALPHA(type, iEl), elAlpha))
				elAlpha = 1.f; // default alpha
			if (AI_SUCCESS != material->Get(AI_MATKEY_TEXBLEND(type, iEl), elBlend))
				elBlend = 1.f; // default blend
			if (AI_SUCCESS != material->Get(AI_MATKEY_TEXFLAGS(type, iEl), elFlags))
				elFlags = 0; // default flags (no flags)
			if (AI_SUCCESS != material->Get(AI_MATKEY_TEXTYPE(type, iEl), elType))
				elType = aiStackType_TextureType; //continue; // error !
			switch (elType) {
			case aiStackType_BlendOpType:
				if (AI_SUCCESS != material->Get(AI_MATKEY_TEXOP(type, iEl), elOp))
					continue; // error !
				el = gcnew Material::StackOperation(convertAssimpStackOperationCppToCs[elOp], elAlpha, elBlend, elFlags);
				break;
			case aiStackType_ColorType:
				if (AI_SUCCESS != material->Get(AI_MATKEY_TEXCOLOR(type, iEl), elColor))
					continue; // error !
				el = gcnew Material::StackColor(Color3(elColor.r, elColor.g, elColor.b), elAlpha, elBlend, elFlags);
				break;
			case aiStackType_TextureType:
				if (AI_SUCCESS != material->Get(AI_MATKEY_TEXTURE(type, iEl), elTexPath))
					continue; // error !
				if (AI_SUCCESS != material->Get(AI_MATKEY_UVWSRC(type, iEl), elTexChannel))
					elTexChannel = 0; // default channel
				if (AI_SUCCESS != material->Get(AI_MATKEY_MAPPINGMODE_U(type, iEl), elMappingModeU))
					elMappingModeU = aiTextureMapMode_Wrap; // default mapping mode
				if (AI_SUCCESS != material->Get(AI_MATKEY_MAPPINGMODE_V(type, iEl), elMappingModeV))
					elMappingModeV = aiTextureMapMode_Wrap; // default mapping mode
				el = gcnew Material::StackTexture(gcnew System::String(elTexPath.C_Str()),
					elTexChannel,
					convertAssimpMappingModeCppToCs[elMappingModeU],
					convertAssimpMappingModeCppToCs[elMappingModeV],
					elAlpha,
					elBlend,
					elFlags);
			}
			ret->Push(el);
		}
		return ret;
	}
};
		}
	}
}
