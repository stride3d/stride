// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#include "stdafx.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::IO;
using namespace Xenko::Core::Assets;
using namespace Xenko::Core::Diagnostics;
using namespace Xenko::Core::Mathematics;
using namespace Xenko::Core::Serialization;
using namespace Xenko::Animations;
using namespace Xenko::Rendering;
using namespace Xenko::Rendering::Materials;
using namespace Xenko::Rendering::Materials::ComputeColors;
using namespace Xenko::Assets::Materials;
using namespace Xenko::Engine;
using namespace Xenko::Graphics;
using namespace Xenko::Shaders;

namespace Xenko { namespace Importer { namespace Common {

public ref class AnimationInfo
{
public:
	AnimationInfo()
	{
		AnimationClips = gcnew Dictionary<System::String^, AnimationClip^>();
	}

	TimeSpan Duration;
	Dictionary<System::String^, AnimationClip^>^ AnimationClips;
};

public ref class MeshParameters
{
public:
	MeshParameters()
	{
	}

	String^ MaterialName;
	String^ MeshName;
	String^ NodeName;
	HashSet<String^>^ BoneNodes;
};

public ref class NodeInfo
{
public:
	String^ Name;
	int Depth;
	bool Preserve;
};

public ref class EntityInfo
{
public:
	List<String^>^ TextureDependencies;
	Dictionary<String^, MaterialAsset^>^ Materials;
	List<String^>^ AnimationNodes;
	List<MeshParameters^>^ Models;
	List<NodeInfo^>^ Nodes;
};

public ref class MeshMaterials
{
public:
	Dictionary<String^, MaterialAsset^>^ Materials;
	List<MeshParameters^>^ Models;
	List<String^>^ BoneNodes;
};

public ref class TextureLayerGenerator
{
public:

	static ShaderClassSource^ GenerateTextureLayer(String^ vfsOutputPath, String^ sourceTextureFile, int textureUVSetIndex, Vector2 textureUVscaling , 
										int& textureCount, ParameterKey<Texture^>^ surfaceMaterialKey, 
										Mesh^ meshData, Logger^ logger)
	{
		ParameterKey<Texture^>^ parameterKey;

		auto url = vfsOutputPath + "_" + Path::GetFileNameWithoutExtension(sourceTextureFile);

		if (File::Exists(sourceTextureFile))
		{
			if (logger != nullptr)
			{
				logger->Warning(String::Format("The texture '{0}' referenced in the mesh material can not be found on the system. Loading will probably fail at run time.", sourceTextureFile),
								nullptr, CallerInfo::Get(__FILEW__, __FUNCTIONW__, __LINE__));
			}
		}

		parameterKey = ParameterKeys::IndexedKey(surfaceMaterialKey, textureCount++);
		String^ uvSetName = "TEXCOORD";
		if (textureUVSetIndex != 0)
			uvSetName += textureUVSetIndex;
		//albedoMaterial->Add(gcnew ShaderClassSource("TextureStream", uvSetName, "TEXTEST" + uvSetIndex));
		auto uvScaling = textureUVscaling;
		auto textureName = parameterKey->Name;
		auto needScaling = uvScaling != Vector2::One;
		auto currentComposition = needScaling
			? gcnew ShaderClassSource("ComputeColorTextureRepeat", textureName, uvSetName, "float2(" + uvScaling.X + ", " + uvScaling.Y + ")")
			: gcnew ShaderClassSource("ComputeColorTexture", textureName, uvSetName);

		return currentComposition;
	}

	static ComputeTextureColor^ GenerateMaterialTextureNode(String^ vfsOutputPath, String^ sourceTextureFile, size_t textureUVSetIndex, Vector2 textureUVscaling, TextureAddressMode addressModeU, TextureAddressMode addressModeV, Logger^ logger)
	{
		auto textureFileName = Path::GetFileNameWithoutExtension(sourceTextureFile);
		auto url = vfsOutputPath + "_" + textureFileName;

		if (File::Exists(sourceTextureFile))
		{
			if (logger != nullptr)
			{
				logger->Warning(String::Format("The texture '{0}' referenced in the mesh material can not be found on the system. Loading will probably fail at run time.", sourceTextureFile),
								nullptr, CallerInfo::Get(__FILEW__, __FUNCTIONW__, __LINE__));
			}
		}

		auto uvScaling = textureUVscaling;
		auto textureName = textureFileName;
	
		auto texture = AttachedReferenceManager::CreateProxyObject<Texture^>(AssetId::Empty, textureName);

		auto currentTexture = gcnew ComputeTextureColor(texture, (TextureCoordinate)textureUVSetIndex, uvScaling, Vector2::Zero);
		currentTexture->AddressModeU = addressModeU;
		currentTexture->AddressModeV = addressModeV;
	
		return currentTexture;
	}
};
}}}
