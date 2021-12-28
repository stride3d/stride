// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#include "stdafx.h"
#include "../Stride.Importer.Common/ImporterUtils.h"

#include "SceneMapping.h"
#include "AnimationConverter.h"

using namespace System;
using namespace System::IO;
using namespace System::Collections::Generic;
using namespace System::Runtime::InteropServices;
using namespace Stride::Core::BuildEngine;
using namespace Stride::Core::Diagnostics;
using namespace Stride::Core::IO;
using namespace Stride::Core::Mathematics;
using namespace Stride::Core::Serialization;
using namespace Stride::Core::Serialization::Contents;
using namespace Stride::Rendering::Materials;
using namespace Stride::Rendering::Materials::ComputeColors;
using namespace Stride::Assets::Materials;
using namespace Stride::Animations;
using namespace Stride::Engine;
using namespace Stride::Extensions;
using namespace Stride::Graphics;
using namespace Stride::Graphics::Data;
using namespace Stride::Shaders;

using namespace Stride::Importer::Common;

namespace Stride { namespace Importer { namespace FBX {

static const char* MappingModeName[] = { "None", "ByControlPoint", "ByPolygonVertex", "ByPolygon", "ByEdge", "AllSame" };
static const char* MappingModeSuggestion[] = { "", "", "", "", " Try using ByPolygon mapping instead.", "" };

public ref class MaterialInstantiation
{
public:
	MaterialInstantiation()
	{
	}

	FbxSurfaceMaterial* SourceMaterial;
	MaterialAsset^ Material;
	String^ MaterialName;
};


public ref class MeshConverter
{
public:
	property bool AllowUnsignedBlendIndices;

	Logger^ logger;

internal:
	FbxManager* lSdkManager;
	FbxImporter* lImporter;
	FbxScene* scene;

	String^ inputFilename;
	String^ vfsOutputFilename;
	String^ inputPath;

	Model^ modelData;

	SceneMapping^ sceneMapping;
	
	static array<Byte>^ currentBuffer;

	static bool WeightGreater(const std::pair<short, float>& elem1, const std::pair<short, float>& elem2)
	{
		return elem1.second > elem2.second;
	}

	bool IsGroupMappingModeByEdge(FbxLayerElement* layerElement)
	{
		return layerElement->GetMappingMode() == FbxLayerElement::eByEdge;
	}

	template <class T>
	int GetGroupIndexForLayerElementTemplate(FbxLayerElementTemplate<T>* layerElement, int controlPointIndex, int vertexIndex, int edgeIndex, int polygonIndex, String^ meshName, bool& firstTimeError)
	{
		int groupIndex = 0;
		if (layerElement->GetMappingMode() == FbxLayerElement::eByControlPoint)
		{
			groupIndex = (layerElement->GetReferenceMode() == FbxLayerElement::eIndexToDirect)
				? layerElement->GetIndexArray().GetAt(controlPointIndex)
				: controlPointIndex;
		}
		else if (layerElement->GetMappingMode() == FbxLayerElement::eByPolygonVertex)
		{
			groupIndex = (layerElement->GetReferenceMode() == FbxLayerElement::eIndexToDirect)
				? layerElement->GetIndexArray().GetAt(vertexIndex)
				: vertexIndex;
		}
		else if (layerElement->GetMappingMode() == FbxLayerElement::eByPolygon)
		{
			groupIndex = (layerElement->GetReferenceMode() == FbxLayerElement::eIndexToDirect)
				? layerElement->GetIndexArray().GetAt(polygonIndex)
				: polygonIndex;
		}
		else if (layerElement->GetMappingMode() == FbxLayerElement::eByEdge)
		{
			groupIndex = (layerElement->GetReferenceMode() == FbxLayerElement::eIndexToDirect)
				? layerElement->GetIndexArray().GetAt(edgeIndex)
				: edgeIndex;
		}
		else if (layerElement->GetMappingMode() == FbxLayerElement::eAllSame)
		{
			groupIndex = (layerElement->GetReferenceMode() == FbxLayerElement::eIndexToDirect)
				? layerElement->GetIndexArray().GetAt(0)
				: 0;
		}
		else if (firstTimeError)
		{
			firstTimeError = false;
			int mappingMode = layerElement->GetMappingMode();
			if (mappingMode > (int)FbxLayerElement::eAllSame)
				mappingMode = (int)FbxLayerElement::eAllSame;
			const char* layerName = layerElement->GetName();
			logger->Warning(String::Format("'{0}' mapping mode for layer '{1}' in mesh '{2}' is not supported by the FBX importer.{3}",
				gcnew String(MappingModeName[mappingMode]),
				strlen(layerName) > 0 ? gcnew String(layerName) : gcnew String("Unknown"),
				meshName,
				gcnew String(MappingModeSuggestion[mappingMode])), (CallerInfo^)nullptr);
		}

		return groupIndex;
	}


public:
	MeshConverter(Logger^ Logger)
	{
		if(Logger == nullptr)
			Logger = Core::Diagnostics::GlobalLogger::GetLogger("Importer FBX");

		logger = Logger;
		lSdkManager = NULL;
		lImporter = NULL;
	}

	void Destroy()
	{
		//Marshal::FreeHGlobal((IntPtr)lFilename);
		currentBuffer = nullptr;

		// The file has been imported; we can get rid of the importer.
		lImporter->Destroy();

		// Destroy the sdk manager and all other objects it was handling.
		lSdkManager->Destroy();

		// -----------------------------------------------------
		// TODO: Workaround with FBX SDK not being multithreaded. 
		// We protect the whole usage of this class with a monitor
		//
		// Lock the whole class between Initialize/Destroy
		// -----------------------------------------------------
		System::Threading::Monitor::Exit( globalLock );
		// -----------------------------------------------------
	}

	void ProcessMesh(FbxMesh* pMesh, std::map<FbxMesh*, std::string> meshNames, std::map<FbxSurfaceMaterial*, int> materials)
	{
		// Checks normals availability.
		bool has_normals = pMesh->GetElementNormalCount() > 0 && pMesh->GetElementNormal(0)->GetMappingMode() != FbxLayerElement::eNone;
		bool needEdgeIndexing = false;

		// Regenerate normals if necessary
		if (!has_normals)
		{
			pMesh->GenerateNormals(true, false, false);
		}

		FbxVector4* controlPoints = pMesh->GetControlPoints();
		FbxGeometryElementNormal* normalElement = pMesh->GetElementNormal();
		FbxGeometryElementTangent* tangentElement = pMesh->GetElementTangent();
		FbxGeometryElementBinormal* binormalElement = pMesh->GetElementBinormal();
		FbxGeometryElementSmoothing* smoothingElement = pMesh->GetElementSmoothing();

		// UV set name mapping
		std::map<std::string, int> uvElementMapping;
		std::vector<FbxGeometryElementUV*> uvElements;

		for (int i = 0; i < pMesh->GetElementUVCount(); ++i)
		{
			auto uvElement = pMesh->GetElementUV(i);
			uvElements.push_back(uvElement);
			needEdgeIndexing |= IsGroupMappingModeByEdge(uvElement);
		}

		auto meshName = gcnew String(meshNames[pMesh].c_str());

		bool hasSkinningPosition = false;
		bool hasSkinningNormal = false;
		int totalClusterCount = 0;
		std::vector<std::vector<std::pair<short, float> > > controlPointWeights;

		List<MeshBoneDefinition>^ bones = nullptr;

		// Dump skinning information
		int skinDeformerCount = pMesh->GetDeformerCount(FbxDeformer::eSkin);
		if (skinDeformerCount > 0)
		{
			bones = gcnew List<MeshBoneDefinition>();
			for (int deformerIndex = 0; deformerIndex < skinDeformerCount; deformerIndex++)
			{
				FbxSkin* skin = FbxCast<FbxSkin>(pMesh->GetDeformer(deformerIndex, FbxDeformer::eSkin));
				controlPointWeights.resize(pMesh->GetControlPointsCount());

				totalClusterCount = skin->GetClusterCount();
				for (int clusterIndex = 0 ; clusterIndex < totalClusterCount; ++clusterIndex)
				{
					FbxCluster* cluster = skin->GetCluster(clusterIndex);
					int indexCount = cluster->GetControlPointIndicesCount();
					if (indexCount == 0)
					{
						continue;
					}

					FbxNode* link = cluster->GetLink();
					const char* boneName = link->GetName();
					int *indices = cluster->GetControlPointIndices();
					double *weights = cluster->GetControlPointWeights();

					FbxAMatrix transformMatrix;
					FbxAMatrix transformLinkMatrix;

					cluster->GetTransformMatrix(transformMatrix);
					cluster->GetTransformLinkMatrix(transformLinkMatrix);
					auto globalBindposeInverseMatrix = transformLinkMatrix.Inverse() * transformMatrix;

					MeshBoneDefinition bone;
					int boneIndex = bones->Count;
					bone.NodeIndex = sceneMapping->FindNodeIndex(link);
					bone.LinkToMeshMatrix = sceneMapping->ConvertMatrixFromFbx(globalBindposeInverseMatrix);

					// Check if the bone was not already there, else update it
					// TODO: this is not the correct way to handle multiple deformers (additive...etc.)
					bool isBoneAlreadyFound = false;
					for (int i = 0; i < bones->Count; i++)
					{
						if (bones[i].NodeIndex == bone.NodeIndex)
						{
							bones[i] = bone;
							boneIndex = i;
							isBoneAlreadyFound = true;
							break;
						}
					}

					// Gather skin indices and weights
					for (int j = 0 ; j < indexCount; j++)
					{
						int controlPointIndex = indices[j];
						controlPointWeights[controlPointIndex].push_back(std::pair<short, float>((short)boneIndex, (float)weights[j]));
					}

					// Find an existing bone and update it
					// TODO: this is probably not correct to do this (we should handle cluster additive...etc. more correctly here)
					if (!isBoneAlreadyFound)
					{
						bones->Add(bone);
					}
				}

				// look for position/normals skinning
				if (pMesh->GetControlPointsCount() > 0)
				{
					hasSkinningPosition = true;
					hasSkinningNormal = (pMesh->GetElementNormal() != NULL);
				}

				for (int i = 0 ; i < pMesh->GetControlPointsCount(); i++)
				{
					std::sort(controlPointWeights[i].begin(), controlPointWeights[i].end(), WeightGreater);
					controlPointWeights[i].resize(4, std::pair<short, float>(0, 0.0f));
					float totalWeight = 0.0f;
					for (int j = 0; j < 4; ++j)
						totalWeight += controlPointWeights[i][j].second;
					if (totalWeight == 0.0f)
					{
						for (int j = 0; j < 4; ++j)
							controlPointWeights[i][j].second = (j == 0) ? 1.0f : 0.0f;
					}
					else
					{
						totalWeight = 1.0f / totalWeight;
						for (int j = 0; j < 4; ++j)
							controlPointWeights[i][j].second *= totalWeight;
					}
				}
			}
		}

		// *********************************************************************************
		// Build the vertex declaration
		// *********************************************************************************
		auto vertexElements = gcnew List<VertexElement>();

		// POSITION
		int vertexStride = 0;
		int positionOffset = vertexStride;
		vertexElements->Add(VertexElement::Position<Vector3>(0, vertexStride));
		vertexStride += 12;

		// NORMAL
		int normalOffset = vertexStride;
		if (normalElement != NULL)
		{
			vertexElements->Add(VertexElement::Normal<Vector3>(0, vertexStride));
			vertexStride += 12;

			needEdgeIndexing |= IsGroupMappingModeByEdge(normalElement);
		}

		int tangentOffset = vertexStride;
		if (tangentElement != NULL)
		{
			vertexElements->Add(VertexElement::Tangent<Vector4>(0, vertexStride));
			vertexStride += 16;

			needEdgeIndexing |= IsGroupMappingModeByEdge(tangentElement);
		}

		// TEXCOORD
		std::vector<int> uvOffsets;
		for (int i = 0; i < (int)uvElements.size(); ++i)
		{
			uvOffsets.push_back(vertexStride);
			vertexElements->Add(VertexElement::TextureCoordinate<Vector2>(i, vertexStride));
			vertexStride += 8;
			uvElementMapping[pMesh->GetElementUV(i)->GetName()] = i;
		}

		// BLENDINDICES
		int blendIndicesOffset = vertexStride;
		bool controlPointIndices16 = (AllowUnsignedBlendIndices && totalClusterCount > 256) || (!AllowUnsignedBlendIndices && totalClusterCount > 128);
		if (!controlPointWeights.empty())
		{
			if (controlPointIndices16)
			{
				if (AllowUnsignedBlendIndices)
				{
					vertexElements->Add(VertexElement("BLENDINDICES", 0, PixelFormat::R16G16B16A16_UInt, vertexStride));
					vertexStride += sizeof(unsigned short) * 4;
				}
				else
				{
					vertexElements->Add(VertexElement("BLENDINDICES", 0, PixelFormat::R16G16B16A16_SInt, vertexStride));
					vertexStride += sizeof(short) * 4;
				}
			}
			else
			{
				if (AllowUnsignedBlendIndices)
				{
					vertexElements->Add(VertexElement("BLENDINDICES", 0, PixelFormat::R8G8B8A8_UInt, vertexStride));
					vertexStride += sizeof(unsigned char) * 4;
				}
				else
				{
					vertexElements->Add(VertexElement("BLENDINDICES", 0, PixelFormat::R8G8B8A8_SInt, vertexStride));
					vertexStride += sizeof(char) * 4;
				}
			}
		}

		// BLENDWEIGHT
		int blendWeightOffset = vertexStride;
		if (!controlPointWeights.empty())
		{
			vertexElements->Add(VertexElement("BLENDWEIGHT", 0, PixelFormat::R32G32B32A32_Float, vertexStride));
			vertexStride += sizeof(float) * 4;
		}

		// COLOR
		auto elementVertexColorCount = pMesh->GetElementVertexColorCount();
		std::vector<FbxGeometryElementVertexColor*> vertexColorElements;
		int colorOffset = vertexStride;
		for (int i = 0; i < elementVertexColorCount; i++)
		{
			auto vertexColorElement = pMesh->GetElementVertexColor(i);
			vertexColorElements.push_back(vertexColorElement);
			vertexElements->Add(VertexElement::Color<Color>(i, vertexStride));
			vertexStride += sizeof(Color);
			needEdgeIndexing |= IsGroupMappingModeByEdge(vertexColorElement);
		}

		// USERDATA
		// TODO: USERData how to handle then?
		//auto userDataCount = pMesh->GetElementUserDataCount();
		//for (int i = 0; i < userDataCount; i++)
		//{
		//	auto userData = pMesh->GetElementUserData(i);
		//	auto dataType = userData->GetDataName(0);
		//	Console::WriteLine("DataName {0}", gcnew String(dataType));
		//}

		// Add the smoothing group information at the end of the vertex declaration
		// *************************************************************************
		// WARNING - DONT PUT ANY VertexElement after SMOOTHINGGROUP
		// *************************************************************************
		// Iit is important that to be the LAST ELEMENT of the declaration because it is dropped later in the process by partial memcopys
		// SMOOTHINGGROUP
		int smoothingOffset = vertexStride;
		if (smoothingElement != NULL)
		{
			vertexElements->Add(VertexElement("SMOOTHINGGROUP", 0, PixelFormat::R32_UInt, vertexStride));
			vertexStride += sizeof(int);

			needEdgeIndexing |= IsGroupMappingModeByEdge(smoothingElement);
		}

		int polygonCount = pMesh->GetPolygonCount();

		FbxGeometryElement::EMappingMode materialMappingMode = FbxGeometryElement::eNone;
		FbxLayerElementArrayTemplate<int>* materialIndices = NULL;

		if (pMesh->GetElementMaterial())
		{
			materialMappingMode = pMesh->GetElementMaterial()->GetMappingMode();
			materialIndices = &pMesh->GetElementMaterial()->GetIndexArray();
		}

		auto buildMeshes = gcnew List<BuildMesh^>();

		// Count polygon per materials
		for (int i = 0; i < polygonCount; i++)
		{
			int materialIndex = 0;
			if (materialMappingMode == FbxGeometryElement::eByPolygon)
			{
				materialIndex = materialIndices->GetAt(i);
			}

			// Equivalent to std::vector::resize()
			while (materialIndex >= buildMeshes->Count)
			{
				buildMeshes->Add(nullptr);
			}

			if (buildMeshes[materialIndex] == nullptr)
				buildMeshes[materialIndex] = gcnew BuildMesh();

			int polygonSize = pMesh->GetPolygonSize(i) - 2;
			if (polygonSize > 0)
				buildMeshes[materialIndex]->polygonCount += polygonSize;
		}

		// Create arrays
		for each(BuildMesh^ buildMesh in buildMeshes)
		{
			if (buildMesh == nullptr)
				continue;

			buildMesh->buffer = gcnew array<Byte>(vertexStride * buildMesh->polygonCount * 3);
		}

		bool layerIndexFirstTimeError = true;

		if (needEdgeIndexing)
			pMesh->BeginGetMeshEdgeIndexForPolygon();

		// Build polygons
		int polygonVertexStartIndex = 0;
		for (int i = 0; i < polygonCount; i++)
		{
			int materialIndex = 0;
			if (materialMappingMode == FbxGeometryElement::eByPolygon)
			{
				materialIndex = materialIndices->GetAt(i);
			}

			auto buildMesh = buildMeshes[materialIndex];
			auto buffer = buildMesh->buffer;

			int polygonSize = pMesh->GetPolygonSize(i);

			for (int polygonFanIndex = 2; polygonFanIndex < polygonSize; ++polygonFanIndex)
			{
				pin_ptr<Byte> vbPointer = &buffer[buildMesh->bufferOffset];
				buildMesh->bufferOffset += vertexStride * 3;

				int vertexInPolygon[3] = { 0, polygonFanIndex, polygonFanIndex - 1};
				int edgesInPolygon[3];

				if (needEdgeIndexing)
				{
					// Default case for polygon of size 3
					// Since our polygon order is 0,2,1, edge order is 2 (edge from 0 to 2),1 (edge from 2 to 1),0 (edge from 1 to 0)
					// Note: all that code computing edge should change if vertexInPolygon changes
					edgesInPolygon[0] = polygonFanIndex;
					edgesInPolygon[1] = polygonFanIndex - 1;
					edgesInPolygon[2] = 0;

					if (polygonSize > 3)
					{
						// Since we create non-existing edges inside the fan, we might have to use another edge in those cases
						// If edge doesn't exist, we have to use edge from (polygonFanIndex-1) to polygonFanIndex (only one that always exists)

						// Let's say polygon is 0,4,3,2,1

						// First polygons (except last): 0,2,1 (edge doesn't exist, use the one from 2 to 1 so edge 1)
						// Last polygon                : 0,4,3 (edge exists:4, from 0 to 4)
						if (polygonFanIndex != polygonSize - 1)
							edgesInPolygon[0] = polygonFanIndex - 1;

						// First polygon: 0,2,1 (edge exists:0, from 1 to 0)
						// Last polygons: 0,4,3 (edge doesn't exist, use the one from 4 to 3 so edge 3)
						if (polygonFanIndex != 2)
							edgesInPolygon[2] = polygonFanIndex - 1;
					}
				}

				//if (polygonSwap)
				//{
				//	int temp = vertexInPolygon[1];
				//	vertexInPolygon[1] = vertexInPolygon[2];
				//	vertexInPolygon[2] = temp;
				//}
				int controlPointIndices[3] = { pMesh->GetPolygonVertex(i, vertexInPolygon[0]), pMesh->GetPolygonVertex(i, vertexInPolygon[1]), pMesh->GetPolygonVertex(i, vertexInPolygon[2]) };

				for (int polygonFanVertex = 0; polygonFanVertex < 3; ++polygonFanVertex)
				{
					int j = vertexInPolygon[polygonFanVertex];
					int vertexIndex = polygonVertexStartIndex + j;
					int jNext = vertexInPolygon[(polygonFanVertex + 1) % 3];
					int vertexIndexNext = polygonVertexStartIndex + jNext;
					int controlPointIndex = controlPointIndices[polygonFanVertex];
					int edgeIndex = needEdgeIndexing ? pMesh->GetMeshEdgeIndexForPolygon(i, edgesInPolygon[polygonFanVertex]) : 0;

					// POSITION
					auto controlPoint = sceneMapping->ConvertPointFromFbx(controlPoints[controlPointIndex]);
					*(Vector3*)(vbPointer + positionOffset) = controlPoint;

					// NORMAL
					Vector3 normal = Vector3(1, 0, 0);
					if (normalElement != NULL)
					{
						int normalIndex = GetGroupIndexForLayerElementTemplate(normalElement, controlPointIndex, vertexIndex, edgeIndex, i, meshName, layerIndexFirstTimeError);
						auto src_normal = normalElement->GetDirectArray().GetAt(normalIndex);
						auto normalPointer = ((Vector3*)(vbPointer + normalOffset));
						normal = sceneMapping->ConvertNormalFromFbx(src_normal);
						if (isnan(normal.X) || isnan(normal.Y) || isnan(normal.Z))
							normal = Vector3(1, 0, 0);
						normal = Vector3::Normalize(normal);
						*normalPointer = normal;
					}

					// UV
					for (int uvGroupIndex = 0; uvGroupIndex < (int)uvElements.size(); ++uvGroupIndex)
					{
						auto uvElement = uvElements[uvGroupIndex];
						int uvIndex = GetGroupIndexForLayerElementTemplate(uvElement, controlPointIndex, vertexIndex, edgeIndex, i, meshName, layerIndexFirstTimeError);
						auto uv = uvElement->GetDirectArray().GetAt(uvIndex);

						((float*)(vbPointer + uvOffsets[uvGroupIndex]))[0] = (float)uv[0];
						((float*)(vbPointer + uvOffsets[uvGroupIndex]))[1] = 1.0f - (float)uv[1];
					}

					// TANGENT
					if (tangentElement != NULL)
					{
						int tangentIndex = GetGroupIndexForLayerElementTemplate(tangentElement, controlPointIndex, vertexIndex, edgeIndex, i, meshName, layerIndexFirstTimeError);
						auto src_tangent = tangentElement->GetDirectArray().GetAt(tangentIndex);
						auto tangentPointer = ((Vector4*)(vbPointer + tangentOffset));
						Vector3 tangent = sceneMapping->ConvertNormalFromFbx(src_tangent);
						if (isnan(tangent.X) || isnan(tangent.Y) || isnan(tangent.Z))
						{
							*tangentPointer = Vector4(1, 0, 0, 1);
						}
						else
						{
							tangent = Vector3::Normalize(tangent);

							int binormalIndex = GetGroupIndexForLayerElementTemplate(binormalElement, controlPointIndex, vertexIndex, edgeIndex, i, meshName, layerIndexFirstTimeError);
							auto src_binormal = binormalElement->GetDirectArray().GetAt(binormalIndex);
							Vector3 binormal = sceneMapping->ConvertNormalFromFbx(src_tangent);
							// See GenerateTangentBinormal()
							*tangentPointer = Vector4(tangent.X, tangent.Y, tangent.Z, Vector3::Dot(Vector3::Cross(normal, tangent), binormal) < 0.0f ? -1.0f : 1.0f);
						}
					}

					// BLENDINDICES and BLENDWEIGHT
					if (!controlPointWeights.empty())
					{
						const auto& blendWeights = controlPointWeights[controlPointIndex];
						for (int i = 0; i < 4; ++i)
						{
							if (controlPointIndices16)
							{
								if (AllowUnsignedBlendIndices)
									((unsigned short*)(vbPointer + blendIndicesOffset))[i] = (unsigned short)blendWeights[i].first;
								else
									((short*)(vbPointer + blendIndicesOffset))[i] = (short)blendWeights[i].first;
							}
							else
							{
								if (AllowUnsignedBlendIndices)
									((unsigned char*)(vbPointer + blendIndicesOffset))[i] = (unsigned char)blendWeights[i].first;
								else
									((char*)(vbPointer + blendIndicesOffset))[i] = (char)blendWeights[i].first;
							}
							((float*)(vbPointer + blendWeightOffset))[i] = blendWeights[i].second;
						}
					}

					// COLOR
					for (int elementColorIndex = 0; elementColorIndex < elementVertexColorCount; elementColorIndex++)
					{
						auto vertexColorElement = vertexColorElements[elementColorIndex];
						auto groupIndex = GetGroupIndexForLayerElementTemplate(vertexColorElement, controlPointIndex, vertexIndex, edgeIndex, i, meshName, layerIndexFirstTimeError);
						auto color = vertexColorElement->GetDirectArray().GetAt(groupIndex);
						((Color*)(vbPointer + colorOffset))[elementColorIndex] = Color((float)color.mRed, (float)color.mGreen, (float)color.mBlue, (float)color.mAlpha);
					}

					// USERDATA
					// TODO HANDLE USERDATA HERE

					// SMOOTHINGGROUP
					if (smoothingElement != NULL)
					{
						auto groupIndex = GetGroupIndexForLayerElementTemplate(smoothingElement, controlPointIndex, vertexIndex, edgeIndex, i, meshName, layerIndexFirstTimeError);
						auto group = smoothingElement->GetDirectArray().GetAt(groupIndex);
						((int*)(vbPointer + smoothingOffset))[0] = (int)group;
					}

					vbPointer += vertexStride;
				}
			}

			polygonVertexStartIndex += polygonSize;
		}

		if (needEdgeIndexing)
			pMesh->EndGetMeshEdgeIndexForPolygon();

		// Create submeshes
		for (int i = 0; i < buildMeshes->Count; ++i)
		{
			auto buildMesh = buildMeshes[i];
			if (buildMesh == nullptr)
				continue;

			auto buffer = buildMesh->buffer;
			auto vertexBufferBinding = VertexBufferBinding(GraphicsSerializerExtensions::ToSerializableVersion(gcnew BufferData(BufferFlags::VertexBuffer, buffer)), gcnew VertexDeclaration(vertexElements->ToArray()), buildMesh->polygonCount * 3, 0, 0);
			
			auto drawData = gcnew MeshDraw();
			auto vbb = gcnew List<VertexBufferBinding>();
			vbb->Add(vertexBufferBinding);
			drawData->VertexBuffers = vbb->ToArray();
			drawData->PrimitiveType = PrimitiveType::TriangleList;
			drawData->DrawCount = buildMesh->polygonCount * 3;

			// build the final VertexDeclaration removing the declaration element needed only for the buffer's correct construction
			auto finalVertexElements = gcnew List<VertexElement>();
			for each (VertexElement element in vertexElements)
			{
				if (element.SemanticName != "SMOOTHINGGROUP")
					finalVertexElements->Add(element);
			}
			auto finalDeclaration = gcnew VertexDeclaration(finalVertexElements->ToArray());

			// Generate index buffer
			// For now, if user requests 16 bits indices but it doesn't fit, it
			// won't generate an index buffer, but ideally it should just split it in multiple render calls
			IndexExtensions::GenerateIndexBuffer(drawData, finalDeclaration);
			/*if (drawData->DrawCount < 65536)
			{
				IndexExtensions::GenerateIndexBuffer(drawData);
			}
			else
			{
				logger->Warning("The index buffer could not be generated with --force-compact-indices because it would use more than 16 bits per index.", nullptr, CallerInfo::Get(__FILEW__, __FUNCTIONW__, __LINE__));
			}*/

			auto lMaterial = pMesh->GetNode()->GetMaterial(i);
		
			// Generate TNB
			if (tangentElement == NULL && normalElement != NULL && uvElements.size() > 0)
				TNBExtensions::GenerateTangentBinormal(drawData);

			auto meshData = gcnew Mesh();
			meshData->NodeIndex = sceneMapping->FindNodeIndex(pMesh->GetNode());
			meshData->Draw = drawData;
			if (!controlPointWeights.empty())
			{
				meshData->Skinning = gcnew MeshSkinningDefinition();
				meshData->Skinning->Bones = bones->ToArray();
			}

			auto materialIndex = materials.find(lMaterial);
			meshData->MaterialIndex = (materialIndex != materials.end()) ? materialIndex->second : 0;

			auto meshName = meshNames[pMesh];
			if (buildMeshes->Count > 1)
				meshName = meshName + "_" + std::to_string(i + 1);
			meshData->Name = gcnew String(meshName.c_str());
			
			if (hasSkinningPosition || hasSkinningNormal || totalClusterCount > 0)
			{
				if (hasSkinningPosition)
					meshData->Parameters->Set(MaterialKeys::HasSkinningPosition, true);
				if (hasSkinningNormal)
					meshData->Parameters->Set(MaterialKeys::HasSkinningNormal, true);
			}
			modelData->Meshes->Add(meshData);
		}
	}

	// return a boolean indicating whether the built material is transparent or not
	MaterialAsset^ ProcessMeshMaterialAsset(FbxSurfaceMaterial* lMaterial, std::map<std::string, size_t>& uvElementMapping)
	{
		auto uvEltMappingOverride = uvElementMapping;
		auto textureMap = gcnew Dictionary<IntPtr, ComputeTextureColor^>();
		std::map<std::string, int> textureNameCount;

		auto finalMaterial = gcnew Stride::Assets::Materials::MaterialAsset();
		
		auto phongSurface = FbxCast<FbxSurfacePhong>(lMaterial);
		auto lambertSurface = FbxCast<FbxSurfaceLambert>(lMaterial);

		{   // The diffuse color
			auto diffuseTree = (IComputeColor^)GenerateSurfaceTextureTree(lMaterial, uvEltMappingOverride, textureMap, textureNameCount, FbxSurfaceMaterial::sDiffuse, FbxSurfaceMaterial::sDiffuseFactor, finalMaterial);
			if(lambertSurface || diffuseTree != nullptr)
			{
				if(diffuseTree == nullptr)	
				{
					auto diffuseColor = lambertSurface->Diffuse.Get();
					auto diffuseFactor = lambertSurface->DiffuseFactor.Get();
					auto diffuseColorValue = diffuseFactor * diffuseColor;

					// Create diffuse value even if the color is black
					diffuseTree = gcnew ComputeColor(FbxDouble3ToColor4(diffuseColorValue));
				}

				if (diffuseTree != nullptr)
				{
					finalMaterial->Attributes->Diffuse = gcnew MaterialDiffuseMapFeature(diffuseTree);
					finalMaterial->Attributes->DiffuseModel = gcnew MaterialDiffuseLambertModelFeature();
				}
			}
		}
		{   // The emissive color
			auto emissiveTree = (IComputeColor^)GenerateSurfaceTextureTree(lMaterial, uvEltMappingOverride, textureMap, textureNameCount, FbxSurfaceMaterial::sEmissive, FbxSurfaceMaterial::sEmissiveFactor, finalMaterial);
			if(lambertSurface || emissiveTree != nullptr)
			{
				if(emissiveTree == nullptr)	
				{
					auto emissiveColor = lambertSurface->Emissive.Get();
					auto emissiveFactor = lambertSurface->EmissiveFactor.Get();
					auto emissiveColorValue = emissiveFactor * emissiveColor;

					// Do not create the node if the value has not been explicitly specified by the user.
					if(emissiveColorValue != FbxDouble3(0))
					{
						emissiveTree = gcnew ComputeColor(FbxDouble3ToColor4(emissiveColorValue));
					}
				}

				if (emissiveTree != nullptr)
				{
					finalMaterial->Attributes->Emissive = gcnew MaterialEmissiveMapFeature(emissiveTree);
				}
			}
		}
		// TODO: Check if we want to support Ambient Color
		//{   // The ambient color
		//	auto ambientTree = GenerateSurfaceTextureTree(lMaterial, uvEltMappingOverride, textureMap, textureNameCount, FbxSurfaceMaterial::sAmbient, FbxSurfaceMaterial::sAmbientFactor, finalMaterial);
		//	if(lambertSurface || ambientTree != nullptr)
		//	{
		//		if(ambientTree == nullptr)	
		//		{
		//			auto ambientColor = lambertSurface->Emissive.Get();
		//			auto ambientFactor = lambertSurface->EmissiveFactor.Get();
		//			auto ambientColorValue = ambientFactor * ambientColor;

		//			// Do not create the node if the value has not been explicitly specified by the user.
		//			if(ambientColorValue != FbxDouble3(0))
		//			{
		//				ambientTree = gcnew ComputeColor(FbxDouble3ToColor4(ambientColorValue));
		//			}
		//		}

		//		if(ambientTree != nullptr)
		//			finalMaterial->AddColorNode(MaterialParameters::AmbientMap, "ambient", ambientTree);
		//	}
		//}
		{   // The normal map
			auto normalMapTree = (IComputeColor^)GenerateSurfaceTextureTree(lMaterial, uvEltMappingOverride, textureMap, textureNameCount, FbxSurfaceMaterial::sNormalMap, NULL, finalMaterial);
			if(lambertSurface || normalMapTree != nullptr)
			{
				if(normalMapTree == nullptr)	
				{
					auto normalMapValue = lambertSurface->NormalMap.Get();

					// Do not create the node if the value has not been explicitly specified by the user.
					if(normalMapValue != FbxDouble3(0))
					{
						normalMapTree = gcnew ComputeFloat4(FbxDouble3ToVector4(normalMapValue));
					}
				}
				
				if (normalMapTree != nullptr)
				{
					finalMaterial->Attributes->Surface = gcnew MaterialNormalMapFeature(normalMapTree);
				}
			}
		}
		// TODO: Support for BumpMap
		//{   // The bump map
		//	auto bumpMapTree = GenerateSurfaceTextureTree(lMaterial, uvEltMappingOverride, textureMap, textureNameCount, FbxSurfaceMaterial::sBump, FbxSurfaceMaterial::sBumpFactor, finalMaterial);
		//	if(lambertSurface || bumpMapTree != nullptr)
		//	{
		//		if(bumpMapTree == nullptr)	
		//		{
		//			auto bumpValue = lambertSurface->Bump.Get();
		//			auto bumpFactor = lambertSurface->BumpFactor.Get();
		//			auto bumpMapValue = bumpFactor * bumpValue;

		//			// Do not create the node if the value has not been explicitly specified by the user.
		//			if(bumpMapValue != FbxDouble3(0))
		//			{
		//				bumpMapTree = gcnew MaterialFloat4ComputeColor(FbxDouble3ToVector4(bumpMapValue));
		//			}
		//		}
		//		
		//		if (bumpMapTree != nullptr)
		//		{
		//			finalMaterial->AddColorNode(MaterialParameters::BumpMap, "bumpMap", bumpMapTree);
		//		}
		//	}
		//}
		// TODO: Support for Transparency
		//{   // The transparency
		//	auto transparencyTree = GenerateSurfaceTextureTree(lMaterial, uvEltMappingOverride, textureMap, textureNameCount, FbxSurfaceMaterial::sTransparentColor, FbxSurfaceMaterial::sTransparencyFactor, finalMaterial);
		//	if(lambertSurface || transparencyTree != nullptr)
		//	{
		//		if(transparencyTree == nullptr)	
		//		{
		//			auto transparencyColor = lambertSurface->TransparentColor.Get();
		//			auto transparencyFactor = lambertSurface->TransparencyFactor.Get();
		//			auto transparencyValue = transparencyFactor * transparencyColor;
		//			auto opacityValue = std::min(1.0f, std::max(0.0f, 1-(float)transparencyValue[0]));

		//			// Do not create the node if the value has not been explicitly specified by the user.
		//			if(opacityValue < 1)
		//			{
		//				transparencyTree = gcnew MaterialFloatComputeColor(opacityValue);
		//			}
		//		}

		//		if(transparencyTree != nullptr)
		//			finalMaterial->AddColorNode(MaterialParameters::TransparencyMap, "transparencyMap", transparencyTree);
		//	}
		//}
		//// TODO: Support for displacement map
		//{   // The displacement map
		//	auto displacementColorTree = GenerateSurfaceTextureTree(lMaterial, uvEltMappingOverride, textureMap, textureNameCount, FbxSurfaceMaterial::sDisplacementColor, FbxSurfaceMaterial::sDisplacementFactor, finalMaterial);
		//	if(lambertSurface || displacementColorTree != nullptr)
		//	{
		//		if(displacementColorTree == nullptr)	
		//		{
		//			auto displacementColor = lambertSurface->DisplacementColor.Get();
		//			auto displacementFactor = lambertSurface->DisplacementFactor.Get();
		//			auto displacementValue = displacementFactor * displacementColor;

		//			// Do not create the node if the value has not been explicitly specified by the user.
		//			if(displacementValue != FbxDouble3(0))
		//			{
		//				displacementColorTree = gcnew MaterialFloat4ComputeColor(FbxDouble3ToVector4(displacementValue));
		//			}
		//		}
		//		
		//		if(displacementColorTree != nullptr)
		//			finalMaterial->AddColorNode(MaterialParameters::DisplacementMap, "displacementMap", displacementColorTree);
		//	}
		//}
		{	// The specular color
			auto specularTree = (IComputeColor^)GenerateSurfaceTextureTree(lMaterial, uvEltMappingOverride, textureMap, textureNameCount, FbxSurfaceMaterial::sSpecular, NULL, finalMaterial);
			if(phongSurface || specularTree != nullptr)
			{
				if(specularTree == nullptr)	
				{
					auto specularColor = phongSurface->Specular.Get();
		
					// Do not create the node if the value has not been explicitly specified by the user.
					if(specularColor != FbxDouble3(0))
					{
						specularTree = gcnew ComputeColor(FbxDouble3ToColor4(specularColor));
					}
				}
						
				if (specularTree != nullptr)
				{
					auto specularFeature = gcnew MaterialSpecularMapFeature();
					specularFeature->SpecularMap = specularTree;
					finalMaterial->Attributes->Specular = specularFeature;

					auto specularModel = gcnew MaterialSpecularMicrofacetModelFeature();
					specularModel->Fresnel = gcnew MaterialSpecularMicrofacetFresnelSchlick();
					specularModel->Visibility = gcnew MaterialSpecularMicrofacetVisibilityImplicit();
					specularModel->NormalDistribution = gcnew MaterialSpecularMicrofacetNormalDistributionBlinnPhong();

					finalMaterial->Attributes->SpecularModel = specularModel;
				}
			}
		}
		// TODO REPLUG SPECULAR INTENSITY
	//{	// The specular intensity map
	//		auto specularIntensityTree = (IComputeColor^)GenerateSurfaceTextureTree(lMaterial, uvEltMappingOverride, textureMap, textureNameCount, FbxSurfaceMaterial::sSpecularFactor, NULL, finalMaterial);
	//		if(phongSurface || specularIntensityTree != nullptr)
	//		{
	//			if(specularIntensityTree == nullptr)	
	//			{
	//				auto specularIntensity = phongSurface->SpecularFactor.Get();
	//	
	//				// Do not create the node if the value has not been explicitly specified by the user.
	//				if(specularIntensity > 0)
	//				{
	//					specularIntensityTree = gcnew MaterialFloatComputeNode((float)specularIntensity);
	//				}
	//			}
	//					
	//			if (specularIntensityTree != nullptr)
	//			{
	//				MaterialSpecularMapFeature^ specularFeature;
	//				if (finalMaterial->Attributes->Specular == nullptr || finalMaterial->Attributes->Specular->GetType() != MaterialSpecularMapFeature::typeid)
	//				{
	//					specularFeature = gcnew MaterialSpecularMapFeature();
	//				}
	//				else
	//				{
	//					specularFeature = (MaterialSpecularMapFeature^)finalMaterial->Attributes->Specular;
	//				}
	//				// TODO: Check Specular Intensity and Power
	//				specularFeature->Intensity = specularIntensityTree;
	//				finalMaterial->Attributes->Specular = specularFeature;
	//			}
	//		}
	//	}
	/*			{	// The specular power map
			auto specularPowerTree = GenerateSurfaceTextureTree(lMaterial, uvEltMappingOverride, textureMap, textureNameCount, FbxSurfaceMaterial::sShininess, NULL, finalMaterial);
			if(phongSurface || specularPowerTree != nullptr)
			{
				if(specularPowerTree == nullptr)	
				{
					auto specularPower = phongSurface->Shininess.Get();
		
					// Do not create the node if the value has not been explicitly specified by the user.
					if(specularPower > 0)
					{
						specularPowerTree = gcnew MaterialFloatComputeColor((float)specularPower);
					}
				}
						
				if (specularPowerTree != nullptr)		
				{
					MaterialSpecularMapFeature^ specularFeature;
					if (finalMaterial->Attributes->Specular == nullptr || finalMaterial->Attributes->Specular->GetType() != MaterialSpecularMapFeature::typeid)
					{
						specularFeature = gcnew MaterialSpecularMapFeature();
					}
					else
					{
						specularFeature = (MaterialSpecularMapFeature^)finalMaterial->Attributes->Specular;
					}
					// TODO: Check Specular Intensity and Power
					specularFeature->Intensity = specularPowerTree;
					finalMaterial->Attributes->Specular = specularFeature;
				}
			}
		}*/
		//// TODO: Support for reflection map
		//{   // The reflection map
		//	auto reflectionMapTree = GenerateSurfaceTextureTree(lMaterial, uvEltMappingOverride, textureMap, textureNameCount, FbxSurfaceMaterial::sReflection, FbxSurfaceMaterial::sReflectionFactor, finalMaterial);
		//	if(phongSurface || reflectionMapTree != nullptr)
		//	{
		//		if(reflectionMapTree == nullptr)	
		//		{
		//			auto reflectionColor = lambertSurface->DisplacementColor.Get();
		//			auto reflectionFactor = lambertSurface->DisplacementFactor.Get();
		//			auto reflectionValue = reflectionFactor * reflectionColor;

		//			// Do not create the node if the value has not been explicitly specified by the user.
		//			if(reflectionValue != FbxDouble3(0))
		//			{
		//				reflectionMapTree = gcnew ComputeColor(FbxDouble3ToColor4(reflectionValue));
		//			}
		//		}
		//		
		//		if(reflectionMapTree != nullptr)
		//			finalMaterial->AddColorNode(MaterialParameters::ReflectionMap, "reflectionMap", reflectionMapTree);
		//	}
		//}
		return finalMaterial;
	}

	bool IsTransparent(FbxSurfaceMaterial* lMaterial)
	{
		for (int i = 0; i < 2; ++i)
		{
			auto propertyName = i == 0 ? FbxSurfaceMaterial::sTransparentColor : FbxSurfaceMaterial::sTransparencyFactor;
			if (propertyName == NULL)
				continue;

			FbxProperty lProperty = lMaterial->FindProperty(propertyName);
			if (lProperty.IsValid())
			{
				const int lTextureCount = lProperty.GetSrcObjectCount<FbxTexture>();
				for (int j = 0; j < lTextureCount; ++j)
				{
					FbxLayeredTexture *lLayeredTexture = FbxCast<FbxLayeredTexture>(lProperty.GetSrcObject<FbxTexture>(j));
					FbxFileTexture *lFileTexture = FbxCast<FbxFileTexture>(lProperty.GetSrcObject<FbxTexture>(j));
					if (lLayeredTexture)
					{
						int lNbTextures = lLayeredTexture->GetSrcObjectCount<FbxFileTexture>();
						if (lNbTextures > 0)
							return true;
					}
					else if (lFileTexture)
						return true;
				}
				if (lTextureCount == 0)
				{
					auto val = FbxDouble3ToVector3(lProperty.Get<FbxDouble3>());
					if (val == Vector3::Zero || val != Vector3::One)
						return true;
				}
			}
		}
		return false;
	}

	IComputeNode^ GenerateSurfaceTextureTree(FbxSurfaceMaterial* lMaterial, std::map<std::string, size_t>& uvElementMapping, Dictionary<IntPtr, ComputeTextureColor^>^ textureMap,
												std::map<std::string, int>& textureNameCount, char const* surfaceMaterial, char const* surfaceMaterialFactor,
												Stride::Assets::Materials::MaterialAsset^ finalMaterial)
	{
		auto compositionTrees = gcnew cli::array<IComputeColor^>(2);

		for (int i = 0; i < 2; ++i)
		{
			// Scan first for component name, then its factor (i.e. sDiffuse, then sDiffuseFactor)
			auto propertyName = i == 0 ? surfaceMaterial : surfaceMaterialFactor;
			if (propertyName == NULL)
				continue;

			int compositionCount = 0;
			
			FbxProperty lProperty = lMaterial->FindProperty(propertyName);
			if (lProperty.IsValid())
			{
				IComputeColor^ previousNode = nullptr;
				const int lTextureCount = lProperty.GetSrcObjectCount<FbxTexture>();
				for (int j = 0; j < lTextureCount; ++j)
				{
					FbxLayeredTexture *lLayeredTexture = FbxCast<FbxLayeredTexture>(lProperty.GetSrcObject<FbxTexture>(j));
					FbxFileTexture *lFileTexture = FbxCast<FbxFileTexture>(lProperty.GetSrcObject<FbxTexture>(j));
					if (lLayeredTexture)
					{
						int lNbTextures = lLayeredTexture->GetSrcObjectCount<FbxFileTexture>();
						for (int k = 0; k < lNbTextures; ++k)
						{
							FbxFileTexture* lSubTexture = FbxCast<FbxFileTexture>(lLayeredTexture->GetSrcObject<FbxFileTexture>(k));

							auto uvName = std::string(lSubTexture->UVSet.Get());
							if (uvElementMapping.find(uvName) == uvElementMapping.end())
								uvElementMapping[uvName] = uvElementMapping.size();

							auto currentMaterialReference = GenerateMaterialTextureNodeFBX(lSubTexture, uvElementMapping, textureMap, textureNameCount, finalMaterial);
							
							if (lNbTextures == 1 || compositionCount == 0)
							{
								if (previousNode == nullptr)
									previousNode = currentMaterialReference;
								else
									previousNode = gcnew ComputeBinaryColor(previousNode, currentMaterialReference, BinaryOperator::Add); // not sure
							}
							else
							{
								auto newNode = gcnew ComputeBinaryColor(previousNode, currentMaterialReference, BinaryOperator::Add);
								previousNode = newNode;
								
								FbxLayeredTexture::EBlendMode blendMode;
								lLayeredTexture->GetTextureBlendMode(k, blendMode);
								newNode->Operator = BlendModeToBlendOperand(blendMode);								
							}

							compositionCount++;
						}
					}
					else if (lFileTexture)
					{
						compositionCount++;

						auto newMaterialReference = GenerateMaterialTextureNodeFBX(lFileTexture, uvElementMapping, textureMap, textureNameCount, finalMaterial);
						
						if (previousNode == nullptr)
							previousNode = newMaterialReference;
						else
							previousNode = gcnew ComputeBinaryColor(previousNode, newMaterialReference, BinaryOperator::Add); // not sure
					}
				}

				compositionTrees[i] = previousNode;
			}
		}

		// If we only have one of either Color or Factor, use directly, otherwise multiply them together
		IComputeColor^ compositionTree;
		if (compositionTrees[0] == nullptr) // TODO do we want only the factor??? -> delete
		{
			compositionTree = compositionTrees[1];
		}
		else if (compositionTrees[1] == nullptr)
		{
			compositionTree = compositionTrees[0];
		}
		else
		{
			compositionTree = gcnew ComputeBinaryColor(compositionTrees[0], compositionTrees[1], BinaryOperator::Multiply);
		}

		return compositionTree;
	}

	BinaryOperator BlendModeToBlendOperand(FbxLayeredTexture::EBlendMode blendMode)
	{
		switch (blendMode)
		{
		case FbxLayeredTexture::eOver:
			return BinaryOperator::Over;
		case FbxLayeredTexture::eAdditive:
			return BinaryOperator::Add;
		case FbxLayeredTexture::eModulate:
			return BinaryOperator::Multiply;
		//case FbxLayeredTexture::eTranslucent:
		//	return BinaryOperator::Multiply;
		//case FbxLayeredTexture::eModulate2:
		//	return BinaryOperator::Multiply;
		//case FbxLayeredTexture::eNormal:
		//	return BinaryOperator::Multiply;
		//case FbxLayeredTexture::eDissolve:
		//	return BinaryOperator::Multiply;
		case FbxLayeredTexture::eDarken:
			return BinaryOperator::Darken;
		case FbxLayeredTexture::eColorBurn:
			return BinaryOperator::ColorBurn;
		case FbxLayeredTexture::eLinearBurn:
			return BinaryOperator::LinearBurn;
		//case FbxLayeredTexture::eDarkerColor:
		//	return BinaryOperator::Multiply;
		case FbxLayeredTexture::eLighten:
			return BinaryOperator::Lighten;
		case FbxLayeredTexture::eScreen:
			return BinaryOperator::Screen;
		case FbxLayeredTexture::eColorDodge:
			return BinaryOperator::ColorDodge;
		case FbxLayeredTexture::eLinearDodge:
			return BinaryOperator::LinearDodge;
		//case FbxLayeredTexture::eLighterColor:
		//	return BinaryOperator::Multiply;
		case FbxLayeredTexture::eSoftLight:
			return BinaryOperator::SoftLight;
		case FbxLayeredTexture::eHardLight:
			return BinaryOperator::HardLight;
		//case FbxLayeredTexture::eVividLight:
		//	return BinaryOperator::Multiply;
		//case FbxLayeredTexture::eLinearLight:
		//	return BinaryOperator::Multiply;
		case FbxLayeredTexture::ePinLight:
			return BinaryOperator::PinLight;
		case FbxLayeredTexture::eHardMix:
			return BinaryOperator::HardMix;
		case FbxLayeredTexture::eDifference:
			return BinaryOperator::Difference;
		case FbxLayeredTexture::eExclusion:
			return BinaryOperator::Exclusion;
		case FbxLayeredTexture::eSubtract:
			return BinaryOperator::Subtract;
		case FbxLayeredTexture::eDivide:
			return BinaryOperator::Divide;
		case FbxLayeredTexture::eHue:
			return BinaryOperator::Hue;
		case FbxLayeredTexture::eSaturation:
			return BinaryOperator::Saturation;
		//case FbxLayeredTexture::eColor:
		//	return BinaryOperator::Multiply;
		//case FbxLayeredTexture::eLuminosity:
		//	return BinaryOperator::Multiply;
		case FbxLayeredTexture::eOverlay:
			return BinaryOperator::Overlay;
		default:
			logger->Error(String::Format("Material blending mode '{0}' is not supported yet. Multiplying blending mode will be used instead.", gcnew Int32(blendMode)), (CallerInfo^)nullptr);
			return BinaryOperator::Multiply;
		}
	}

	ShaderClassSource^ GenerateTextureLayerFBX(FbxFileTexture* lFileTexture, std::map<std::string, int>& uvElementMapping, Mesh^ meshData, int& textureCount, ParameterKey<Texture^>^ surfaceMaterialKey)
	{
		auto texScale = lFileTexture->GetUVScaling();
		auto texturePath = FindFilePath(lFileTexture);

		return TextureLayerGenerator::GenerateTextureLayer(vfsOutputFilename, texturePath, uvElementMapping[std::string(lFileTexture->UVSet.Get())], Vector2((float)texScale[0], (float)texScale[1]) , 
									textureCount, surfaceMaterialKey,
									meshData,
									nullptr);
	}

	String^ FindFilePath(FbxFileTexture* lFileTexture)
	{		
		auto relFileName = gcnew String(lFileTexture->GetRelativeFileName());
		auto absFileName = gcnew String(lFileTexture->GetFileName());

		// First try to get the texture filename by relative path, if not valid then use absolute path
		// (According to FBX doc, resolved first by absolute name, and relative name if absolute name is not valid)
		auto fileNameToUse = Path::Combine(inputPath, relFileName);
		if(fileNameToUse->StartsWith("\\\\"))
		{
			logger->Warning(String::Format("Importer detected a network address in referenced assets. This may temporary block the build if the file does not exist. [Address='{0}']", fileNameToUse), (CallerInfo^)nullptr);
		}
		if (!File::Exists(fileNameToUse) && !String::IsNullOrEmpty(absFileName))
		{
			fileNameToUse = absFileName;
		}

		// Make sure path is absolute
		if (!(gcnew UFile(fileNameToUse))->IsAbsolute)
		{
			fileNameToUse = Path::Combine(inputPath, fileNameToUse);
		}

		return fileNameToUse;
	}

	ComputeTextureColor^ GenerateMaterialTextureNodeFBX(FbxFileTexture* lFileTexture, std::map<std::string, size_t>& uvElementMapping, Dictionary<IntPtr, ComputeTextureColor^>^ textureMap, std::map<std::string, int>& textureNameCount, Stride::Assets::Materials::MaterialAsset^ finalMaterial)
	{
		auto texScale = lFileTexture->GetUVScaling();		
		auto texturePath = FindFilePath(lFileTexture);
		auto wrapModeU = lFileTexture->GetWrapModeU();
		auto wrapModeV = lFileTexture->GetWrapModeV();
		auto wrapTextureU = (wrapModeU == FbxTexture::EWrapMode::eRepeat) ? TextureAddressMode::Wrap : TextureAddressMode::Clamp;
		auto wrapTextureV = (wrapModeV == FbxTexture::EWrapMode::eRepeat) ? TextureAddressMode::Wrap : TextureAddressMode::Clamp;
		
		ComputeTextureColor^ textureValue;
		
		if (textureMap->TryGetValue(IntPtr(lFileTexture), textureValue))
		{
			return textureValue;
		}
		else
		{
			textureValue = TextureLayerGenerator::GenerateMaterialTextureNode(vfsOutputFilename, texturePath, uvElementMapping[std::string(lFileTexture->UVSet.Get())], Vector2((float)texScale[0], (float)texScale[1]), wrapTextureU, wrapTextureV, nullptr);

			auto attachedReference = AttachedReferenceManager::GetAttachedReference(textureValue->Texture);

			auto textureNamePtr = Marshal::StringToHGlobalAnsi(attachedReference->Url);
			std::string textureName = std::string((char*)textureNamePtr.ToPointer());
			Marshal:: FreeHGlobal(textureNamePtr);

			auto textureCount = GetTextureNameCount(textureNameCount, textureName);
			if (textureCount > 1)
				textureName = textureName + "_" + std::to_string(textureCount - 1);

			auto referenceName = gcnew String(textureName.c_str());
			//auto materialReference = gcnew MaterialReferenceNode(referenceName);
			//finalMaterial->AddNode(referenceName, textureValue);
			textureMap[IntPtr(lFileTexture)] = textureValue;
			return textureValue;
		}
		
		return nullptr;
	}

	int GetTextureNameCount(std::map<std::string, int>& textureNameCount, std::string textureName)
	{
		auto textureFound = textureNameCount.find(textureName);
		if (textureFound == textureNameCount.end())
			textureNameCount[textureName] = 1;
		else
			textureNameCount[textureName] = textureNameCount[textureName] + 1;
		return textureNameCount[textureName];
	}

	void ProcessAttribute(FbxNode* pNode, FbxNodeAttribute* pAttribute, std::map<FbxMesh*, std::string> meshNames, std::map<FbxSurfaceMaterial*, int> materials)
	{
		if(!pAttribute) return;
 
		if (pAttribute->GetAttributeType() == FbxNodeAttribute::eMesh)
		{
			ProcessMesh((FbxMesh*)pAttribute, meshNames, materials);
		}
	}

	void ProcessNodeTransformation(FbxNode* pNode)
	{
		auto nodeIndex = sceneMapping->FindNodeIndex(pNode);
		auto nodes = sceneMapping->Nodes;
		auto node = &nodes[nodeIndex];

		// Use GlobalTransform instead of LocalTransform

		auto fbxMatrix = pNode->EvaluateLocalTransform(FBXSDK_TIME_ZERO);
		auto matrix = sceneMapping->ConvertMatrixFromFbx(fbxMatrix);

		// Extract the translation and scaling
		Vector3 translation;
		Quaternion rotation;
		Vector3 scaling;
		matrix.Decompose(scaling, rotation, translation);

		// Apply rotation on top level nodes only
		if (node->ParentIndex == 0)
		{
			Vector3::TransformCoordinate(translation, sceneMapping->AxisSystemRotationMatrix, translation);
			rotation = Quaternion::Multiply(rotation, Quaternion::RotationMatrix(sceneMapping->AxisSystemRotationMatrix));
		}

		// Setup the transform for this node
		node->Transform.Position = translation;
		node->Transform.Rotation = rotation;
		node->Transform.Scale = scaling;

		// Recursively process the children nodes.
		for (int j = 0; j < pNode->GetChildCount(); j++)
		{
			ProcessNodeTransformation(pNode->GetChild(j));
		}
	}

	void ProcessNodeAttributes(FbxNode* pNode, std::map<FbxMesh*, std::string> meshNames, std::map<FbxSurfaceMaterial*, int> materials)
	{
		// Process the node's attributes.
		for(int i = 0; i < pNode->GetNodeAttributeCount(); i++)
			ProcessAttribute(pNode, pNode->GetNodeAttributeByIndex(i), meshNames, materials);

		// Recursively process the children nodes.
		for(int j = 0; j < pNode->GetChildCount(); j++)
		{
			ProcessNodeAttributes(pNode->GetChild(j), meshNames, materials);
		}
	}

	ref class BuildMesh
	{
	public:
		array<Byte>^ buffer;
		int bufferOffset;
		int polygonCount;
	};

	ref struct ImportConfiguration
	{
	public:
		property bool ImportTemplates;
		property bool ImportPivots;
		property bool ImportGlobalSettings;
		property bool ImportCharacters;
		property bool ImportConstraints;
		property bool ImportGobos;
		property bool ImportShapes;
		property bool ImportLinks;
		property bool ImportMaterials;
		property bool ImportTextures;
		property bool ImportModels;
		property bool ImportAnimations;
		property bool ExtractEmbeddedData;

	public:
		static ImportConfiguration^ ImportAll()
		{
			auto config = gcnew ImportConfiguration();

			config->ImportTemplates = true;
			config->ImportPivots = true;
			config->ImportGlobalSettings = true;
			config->ImportCharacters = true;
			config->ImportConstraints = true;
			config->ImportGobos = true;
			config->ImportShapes = true;
			config->ImportLinks = true;
			config->ImportMaterials = true;
			config->ImportTextures = true;
			config->ImportModels = true;
			config->ImportAnimations = true;
			config->ExtractEmbeddedData = true;

			return config;
		}

		static ImportConfiguration^ ImportModelOnly()
		{
			auto config = gcnew ImportConfiguration();

			config->ImportTemplates = false;
			config->ImportPivots = false;
			config->ImportGlobalSettings = true;
			config->ImportCharacters = false;
			config->ImportConstraints = false;
			config->ImportGobos = false;
			config->ImportShapes = false;
			config->ImportLinks = false;
			config->ImportMaterials = true;
			config->ImportTextures = false;
			config->ImportModels = true;
			config->ImportAnimations = false;
			config->ExtractEmbeddedData = false;

			return config;
		}

		static ImportConfiguration^ ImportMaterialsOnly()
		{
			auto config = gcnew ImportConfiguration();

			config->ImportTemplates = false;
			config->ImportPivots = false;
			config->ImportGlobalSettings = true;
			config->ImportCharacters = false;
			config->ImportConstraints = false;
			config->ImportGobos = false;
			config->ImportShapes = false;
			config->ImportLinks = false;
			config->ImportMaterials = true;
			config->ImportTextures = false;
			config->ImportModels = false;
			config->ImportAnimations = false;
			config->ExtractEmbeddedData = false;

			return config;
		}

		static ImportConfiguration^ ImportAnimationsOnly()
		{
			auto config = gcnew ImportConfiguration();

			config->ImportTemplates = false;
			config->ImportPivots = false;
			config->ImportGlobalSettings = true;
			config->ImportCharacters = false;
			config->ImportConstraints = false;
			config->ImportGobos = false;
			config->ImportShapes = false;
			config->ImportLinks = false;
			config->ImportMaterials = false;
			config->ImportTextures = false;
			config->ImportModels = false;
			config->ImportAnimations = true;
			config->ExtractEmbeddedData = false;

			return config;
		}

		static ImportConfiguration^ ImportSkeletonOnly()
		{
			auto config = gcnew ImportConfiguration();

			config->ImportTemplates = false;
			config->ImportPivots = false;
			config->ImportGlobalSettings = true;
			config->ImportCharacters = false;
			config->ImportConstraints = false;
			config->ImportGobos = false;
			config->ImportShapes = false;
			config->ImportLinks = false;
			config->ImportMaterials = false;
			config->ImportTextures = false;
			config->ImportModels = false;
			config->ImportAnimations = false;
			config->ExtractEmbeddedData = false;

			return config;
		}

		static ImportConfiguration^ ImportTexturesOnly()
		{
			auto config = gcnew ImportConfiguration();

			config->ImportTemplates = false;
			config->ImportPivots = false;
			config->ImportGlobalSettings = false;
			config->ImportCharacters = false;
			config->ImportConstraints = false;
			config->ImportGobos = false;
			config->ImportShapes = false;
			config->ImportLinks = false;
			config->ImportMaterials = false;
			config->ImportTextures = true;
			config->ImportModels = false;
			config->ImportAnimations = false;
			config->ExtractEmbeddedData = true;

			return config;
		}

		static ImportConfiguration^ ImportEntityConfig()
		{
			auto config = gcnew ImportConfiguration();

			config->ImportTemplates = false;
			config->ImportPivots = false;
			config->ImportGlobalSettings = true;
			config->ImportCharacters = false;
			config->ImportConstraints = false;
			config->ImportGobos = false;
			config->ImportShapes = false;
			config->ImportLinks = false;
			config->ImportMaterials = true;
			config->ImportTextures = true;
			config->ImportModels = true;
			config->ImportAnimations = true;
			config->ExtractEmbeddedData = true;

			return config;
		}

		static ImportConfiguration^ ImportGlobalSettingsOnly()
		{
			auto config = gcnew ImportConfiguration();

			config->ImportGlobalSettings = true;

			return config;
		}
	};

private:
	static System::Object^ globalLock = gcnew System::Object();

	void Initialize(String^ inputFilename, String^ vfsOutputFilename, ImportConfiguration^ importConfig)
	{
		// -----------------------------------------------------
		// TODO: Workaround with FBX SDK not being multithreaded. 
		// We protect the whole usage of this class with a monitor
		//
		// Lock the whole class between Initialize/Destroy
		// -----------------------------------------------------
		System::Threading::Monitor::Enter( globalLock );
		// -----------------------------------------------------

		this->inputFilename = inputFilename;
		this->vfsOutputFilename = vfsOutputFilename;
		this->inputPath = Path::GetDirectoryName(inputFilename);

		// Initialize the sdk manager. This object handles all our memory management.
		lSdkManager = FbxManager::Create();

		// Create the io settings object.
		FbxIOSettings *ios = FbxIOSettings::Create(lSdkManager, IOSROOT);
		ios->SetBoolProp(IMP_FBX_TEMPLATE, importConfig->ImportTemplates);
		ios->SetBoolProp(IMP_FBX_PIVOT, importConfig->ImportPivots);
		ios->SetBoolProp(IMP_FBX_GLOBAL_SETTINGS, importConfig->ImportGlobalSettings);
		ios->SetBoolProp(IMP_FBX_CHARACTER, importConfig->ImportCharacters);
		ios->SetBoolProp(IMP_FBX_CONSTRAINT, importConfig->ImportConstraints);
		ios->SetBoolProp(IMP_FBX_GOBO, importConfig->ImportGobos);
		ios->SetBoolProp(IMP_FBX_SHAPE, importConfig->ImportShapes);
		ios->SetBoolProp(IMP_FBX_LINK, importConfig->ImportLinks);
		ios->SetBoolProp(IMP_FBX_MATERIAL, importConfig->ImportMaterials);
		ios->SetBoolProp(IMP_FBX_TEXTURE, importConfig->ImportTextures);
		ios->SetBoolProp(IMP_FBX_MODEL, importConfig->ImportModels);
		ios->SetBoolProp(IMP_FBX_ANIMATION, importConfig->ImportAnimations);
		ios->SetBoolProp(IMP_FBX_EXTRACT_EMBEDDED_DATA, importConfig->ExtractEmbeddedData);
		lSdkManager->SetIOSettings(ios);

		// Create an importer using our sdk manager.
		lImporter = FbxImporter::Create(lSdkManager,"");
    
		auto inputFilenameUtf8 = System::Text::Encoding::UTF8->GetBytes(inputFilename);
		pin_ptr<Byte> inputFilenameUtf8Ptr = &inputFilenameUtf8[0];

		if(!lImporter->Initialize((const char*)inputFilenameUtf8Ptr, -1, lSdkManager->GetIOSettings()))
		{
			throw gcnew InvalidOperationException(String::Format("Call to FbxImporter::Initialize() failed.\n"
				"Error returned: {0}\n\n", gcnew String(lImporter->GetStatus().GetErrorString())));
		}

		// Create a new scene so it can be populated by the imported file.
		scene = FbxScene::Create(lSdkManager, "myScene");

		// Import the contents of the file into the scene.
		lImporter->Import(scene);

		const float framerate = static_cast<float>(FbxTime::GetFrameRate(scene->GetGlobalSettings().GetTimeMode()));
		scene->GetRootNode()->ResetPivotSetAndConvertAnimation(framerate, false, false);

		// Initialize the node mapping
		sceneMapping = gcnew SceneMapping(scene);
	}
	
	bool HasAnimationData(String^ inputFile)
	{
		try
		{
			Initialize(inputFile, nullptr, ImportConfiguration::ImportAnimationsOnly());
			auto animConverter = gcnew AnimationConverter(logger, sceneMapping);
			return animConverter->HasAnimationData();
		}
		finally
		{
			Destroy();
		}
	}
	
	void GenerateMaterialNames(std::map<FbxSurfaceMaterial*, std::string>& materialNames)
	{
		auto materials = gcnew List<MaterialAsset^>();
		std::map<std::string, int> materialNameTotalCount;
		std::map<std::string, int> materialNameCurrentCount;
		std::map<FbxSurfaceMaterial*, std::string> tempNames;
		auto materialCount = scene->GetMaterialCount();
		
		for (int i = 0;  i < materialCount; i++)
		{
			auto lMaterial = scene->GetMaterial(i);
			auto materialName = std::string(lMaterial->GetName());
			auto materialPart = std::string();

			size_t materialNameSplitPosition = materialName.find('#');
			if (materialNameSplitPosition != std::string::npos)
			{
				materialPart = materialName.substr(materialNameSplitPosition + 1);
				materialName = materialName.substr(0, materialNameSplitPosition);
			}

			materialNameSplitPosition = materialName.find("__");
			if (materialNameSplitPosition != std::string::npos)
			{
				materialPart = materialName.substr(materialNameSplitPosition + 2);
				materialName = materialName.substr(0, materialNameSplitPosition);
			}

			// remove all bad characters
			ReplaceCharacter(materialName, ':', '_');
			RemoveCharacter(materialName, ' ');
			tempNames[lMaterial] = materialName;
			
			if (materialNameTotalCount.count(materialName) == 0)
				materialNameTotalCount[materialName] = 1;
			else
				materialNameTotalCount[materialName] = materialNameTotalCount[materialName] + 1;
		}

		for (int i = 0;  i < materialCount; i++)
		{
			auto lMaterial = scene->GetMaterial(i);
			auto materialName = tempNames[lMaterial];
			int currentCount = 0;

			if (materialNameCurrentCount.count(materialName) == 0)
				materialNameCurrentCount[materialName] = 1;
			else
				materialNameCurrentCount[materialName] = materialNameCurrentCount[materialName] + 1;

			if(materialNameTotalCount[materialName] > 1)
				materialName = materialName + "_" + std::to_string(materialNameCurrentCount[materialName]);

			materialNames[lMaterial] = materialName;
		}
	}

	void GetMeshes(FbxNode* pNode, std::vector<FbxMesh*>& meshes)
	{
		// Process the node's attributes.
		for(int i = 0; i < pNode->GetNodeAttributeCount(); i++)
		{
			auto pAttribute = pNode->GetNodeAttributeByIndex(i);

			if(!pAttribute) return;
		
			if (pAttribute->GetAttributeType() == FbxNodeAttribute::eMesh)
			{
				auto pMesh = (FbxMesh*)pAttribute;
				meshes.push_back(pMesh);
			}
		}

		// Recursively process the children nodes.
		for(int j = 0; j < pNode->GetChildCount(); j++)
		{
			GetMeshes(pNode->GetChild(j), meshes);
		}
	}
	
	void GenerateMeshesName(std::map<FbxMesh*, std::string>& meshNames)
	{
		std::vector<FbxMesh*> meshes;
		GetMeshes(scene->GetRootNode(), meshes);

		std::map<std::string, int> meshNameTotalCount;
		std::map<std::string, int> meshNameCurrentCount;
		std::map<FbxMesh*, std::string> tempNames;

		for (auto iter = meshes.begin(); iter != meshes.end(); ++iter)
		{
			auto pMesh = *iter;
			auto meshName = std::string(pMesh->GetNode()->GetName());

			// remove all bad characters
			RemoveCharacter(meshName, ' ');
			tempNames[pMesh] = meshName;

			if (meshNameTotalCount.count(meshName) == 0)
				meshNameTotalCount[meshName] = 1;
			else
				meshNameTotalCount[meshName] = meshNameTotalCount[meshName] + 1;
		}

		for (auto iter = meshes.begin(); iter != meshes.end(); ++iter)
		{
			auto pMesh = *iter;
			auto meshName = tempNames[pMesh];
			int currentCount = 0;

			if (meshNameCurrentCount.count(meshName) == 0)
				meshNameCurrentCount[meshName] = 1;
			else
				meshNameCurrentCount[meshName] = meshNameCurrentCount[meshName] + 1;

			if(meshNameTotalCount[meshName] > 1)
				meshName = meshName + "_" + std::to_string(meshNameCurrentCount[meshName]);

			meshNames[pMesh] = meshName;
		}
	}

	MaterialInstantiation^ GetOrCreateMaterial(FbxSurfaceMaterial* lMaterial, List<String^>^ uvNames, List<MaterialInstantiation^>^ instances, std::map<std::string, size_t>& uvElements, std::map<FbxSurfaceMaterial*, std::string>& materialNames)
	{
		for (int i = 0; i < instances->Count; ++i)
		{
			if (lMaterial == instances[i]->SourceMaterial)
				return instances[i];
		}

		auto newMaterialInstantiation = gcnew MaterialInstantiation();
		newMaterialInstantiation->SourceMaterial = lMaterial;
		newMaterialInstantiation->MaterialName = gcnew String(materialNames[lMaterial].c_str());

		// TODO: We currently use UV mapping of first requesting mesh.
		//       However, we probably need to reverse everything: mesh describes what they have, materials what they need, and an appropriate input layout is created at runtime?
		//       Such a mechanism would also be able to handle missing streams gracefully.
		newMaterialInstantiation->Material = ProcessMeshMaterialAsset(lMaterial, uvElements);
		instances->Add(newMaterialInstantiation);
		return newMaterialInstantiation;
	}

	void SearchMeshInAttribute(FbxNode* pNode, FbxNodeAttribute* pAttribute, std::map<FbxSurfaceMaterial*, std::string> materialNames, std::map<FbxMesh*, std::string> meshNames, List<MeshParameters^>^ models, List<MaterialInstantiation^>^ materialInstantiations)
	{
		if(!pAttribute) return;
 
		if (pAttribute->GetAttributeType() == FbxNodeAttribute::eMesh)
		{
			auto pMesh = (FbxMesh*)pAttribute;
			int polygonCount = pMesh->GetPolygonCount();
			FbxGeometryElement::EMappingMode materialMappingMode = FbxGeometryElement::eNone;
			FbxLayerElementArrayTemplate<int>* materialIndices = NULL;
			
			if (pMesh->GetElementMaterial())
			{
				materialMappingMode = pMesh->GetElementMaterial()->GetMappingMode();
				materialIndices = &pMesh->GetElementMaterial()->GetIndexArray();
			}

			auto buildMeshes = gcnew List<BuildMesh^>();

			// Count polygon per materials
			for (int i = 0; i < polygonCount; i++)
			{
				int materialIndex = 0;
				if (materialMappingMode == FbxGeometryElement::eByPolygon)
				{
					materialIndex = materialIndices->GetAt(i);
				}
				else if (materialMappingMode == FbxGeometryElement::eAllSame)
				{
					materialIndex = materialIndices->GetAt(0);
				}

				// Equivalent to std::vector::resize()
				while (materialIndex >= buildMeshes->Count)
				{
					buildMeshes->Add(nullptr);
				}

				if (buildMeshes[materialIndex] == nullptr)
					buildMeshes[materialIndex] = gcnew BuildMesh();

				int polygonSize = pMesh->GetPolygonSize(i) - 2;
				if (polygonSize > 0)
					buildMeshes[materialIndex]->polygonCount += polygonSize;
			}

			for (int i = 0; i < buildMeshes->Count; ++i)
			{
				auto meshParams = gcnew MeshParameters();
				auto meshName = meshNames[pMesh];
				if (buildMeshes->Count > 1)
					meshName = meshName + "_" + std::to_string(i + 1);
				meshParams->MeshName = gcnew String(meshName.c_str());
				meshParams->NodeName = sceneMapping->FindNode(pNode).Name;

				// Collect bones
				int skinDeformerCount = pMesh->GetDeformerCount(FbxDeformer::eSkin);
				if (skinDeformerCount > 0)
				{
					meshParams->BoneNodes = gcnew HashSet<String^>();
					for (int deformerIndex = 0; deformerIndex < skinDeformerCount; deformerIndex++)
					{
						FbxSkin* skin = FbxCast<FbxSkin>(pMesh->GetDeformer(deformerIndex, FbxDeformer::eSkin));

						auto totalClusterCount = skin->GetClusterCount();
						for (int clusterIndex = 0; clusterIndex < totalClusterCount; ++clusterIndex)
						{
							FbxCluster* cluster = skin->GetCluster(clusterIndex);
							int indexCount = cluster->GetControlPointIndicesCount();
							if (indexCount == 0)
							{
								continue;
							}

							FbxNode* link = cluster->GetLink();

							MeshBoneDefinition bone;
							meshParams->BoneNodes->Add(sceneMapping->FindNode(link).Name);
						}
					}
				}

				FbxGeometryElementMaterial* lMaterialElement = pMesh->GetElementMaterial();
				FbxSurfaceMaterial* lMaterial = pNode->GetMaterial(i);
				if ((materialMappingMode == FbxGeometryElement::eByPolygon || materialMappingMode == FbxGeometryElement::eAllSame)
					&& lMaterialElement != NULL && lMaterial != NULL)
				{
					std::map<std::string, size_t> uvElements;
					auto uvNames = gcnew List<String^>();
					for (int j = 0; j < pMesh->GetElementUVCount(); ++j)
					{
						uvElements[pMesh->GetElementUV(j)->GetName()] = j;
						uvNames->Add(gcnew String(pMesh->GetElementUV(j)->GetName()));
					}

					auto material = GetOrCreateMaterial(lMaterial, uvNames, materialInstantiations, uvElements, materialNames);
					meshParams->MaterialName = material->MaterialName;
				}
				else
				{
					logger->Warning(String::Format("Mesh {0} does not have a material. It might not be displayed.", meshParams->MeshName), (CallerInfo^)nullptr);
				}

				models->Add(meshParams);
			}
		}
	}

	void SearchMesh(FbxNode* pNode, std::map<FbxSurfaceMaterial*, std::string> materialNames, std::map<FbxMesh*, std::string> meshNames, List<MeshParameters^>^ models, List<MaterialInstantiation^>^ materialInstantiations)
	{
		// Process the node's attributes.
		for(int i = 0; i < pNode->GetNodeAttributeCount(); i++)
			SearchMeshInAttribute(pNode, pNode->GetNodeAttributeByIndex(i), materialNames, meshNames, models, materialInstantiations);

		// Recursively process the children nodes.
		for(int j = 0; j < pNode->GetChildCount(); j++)
		{
			SearchMesh(pNode->GetChild(j), materialNames, meshNames, models, materialInstantiations);
		}
	}

	Dictionary<String^, MaterialAsset^>^ ExtractMaterialsNoInit()
	{
		std::map<FbxSurfaceMaterial*, std::string> materialNames;
		GenerateMaterialNames(materialNames);

		auto materials = gcnew Dictionary<String^, MaterialAsset^>();
		for (int i = 0;  i < scene->GetMaterialCount(); i++)
		{
			std::map<std::string, size_t> dict;
			auto lMaterial = scene->GetMaterial(i);
			auto materialName = materialNames[lMaterial];
			materials->Add(gcnew String(materialName.c_str()), ProcessMeshMaterialAsset(lMaterial, dict));
		}
		return materials;
	}

	MeshMaterials^ ExtractModelNoInit()
	{
		std::map<FbxSurfaceMaterial*, std::string> materialNames;
		GenerateMaterialNames(materialNames);

		std::map<FbxMesh*, std::string> meshNames;
		GenerateMeshesName(meshNames);
			
		std::map<std::string, FbxSurfaceMaterial*> materialPerMesh;
		auto models = gcnew List<MeshParameters^>();
		auto materialInstantiations = gcnew List<MaterialInstantiation^>();
		SearchMesh(scene->GetRootNode(), materialNames, meshNames, models, materialInstantiations);

		auto ret = gcnew MeshMaterials();
		ret->Models = models;
		ret->Materials = gcnew Dictionary<String^, MaterialAsset^>();
		for (int i = 0; i < materialInstantiations->Count; ++i)
		{
			ret->Materials->Add(materialInstantiations[i]->MaterialName, materialInstantiations[i]->Material);
		}
        
		return ret;
	}

	List<String^>^ ExtractTextureDependenciesNoInit()
	{
		auto textureNames = gcnew List<String^>();
			
		auto textureCount = scene->GetTextureCount();
		for(int i=0; i<textureCount; ++i)
		{
			auto texture  = FbxCast<FbxFileTexture>(scene->GetTexture(i));

			if(texture == nullptr)
				continue;
			
			auto texturePath = FindFilePath(texture);
			if (!String::IsNullOrEmpty(texturePath))
			{
				if (texturePath->Contains(".fbm\\"))
					logger->Info(String::Format("Importer detected an embedded texture. It has been extracted at address '{0}'.", texturePath), (CallerInfo^)nullptr);
				if (!File::Exists(texturePath))
					logger->Warning(String::Format("Importer detected a texture not available on disk at address '{0}'", texturePath), (CallerInfo^)nullptr);

				textureNames->Add(texturePath);
			}
		}

		return textureNames;
	}

	List<String^>^ ExtractTextureDependencies(String^ inputFile)
	{
		try
		{
			Initialize(inputFile, nullptr, ImportConfiguration::ImportTexturesOnly());
			return ExtractTextureDependenciesNoInit();
		}
		finally
		{
			Destroy();
		}
		return nullptr;
	}

	Dictionary<String^, MaterialAsset^>^ ExtractMaterials(String^ inputFilename)
	{
		try
		{
			Initialize(inputFilename, nullptr, ImportConfiguration::ImportMaterialsOnly());
			return ExtractMaterialsNoInit();
		}
		finally
		{
			Destroy();
		}
		return nullptr;
	}

	void GetNodes(FbxNode* node, int depth, List<NodeInfo^>^ allNodes)
	{
		auto newNodeInfo = gcnew NodeInfo();
		newNodeInfo->Name = sceneMapping->FindNode(node).Name;
		newNodeInfo->Depth = depth;
		newNodeInfo->Preserve = true;
		
		allNodes->Add(newNodeInfo);
		for (int i = 0; i < node->GetChildCount(); ++i)
			GetNodes(node->GetChild(i), depth + 1, allNodes);
	}

	List<NodeInfo^>^ ExtractNodeHierarchy()
	{
		auto allNodes = gcnew List<NodeInfo^>();
		GetNodes(scene->GetRootNode(), 0, allNodes);
		return allNodes;
	}

public:
	EntityInfo^ ExtractEntity(String^ inputFileName, bool extractTextureDependencies)
	{
		try
		{
			Initialize(inputFileName, nullptr, ImportConfiguration::ImportEntityConfig());
			
			auto animationConverter = gcnew AnimationConverter(logger, sceneMapping);
			
			auto entityInfo = gcnew EntityInfo();
			if (extractTextureDependencies)
				entityInfo->TextureDependencies = ExtractTextureDependenciesNoInit();
			entityInfo->AnimationNodes = animationConverter->ExtractAnimationNodesNoInit();
			auto models = ExtractModelNoInit();
			entityInfo->Models = models->Models;
			entityInfo->Materials = models->Materials;
			entityInfo->Nodes = ExtractNodeHierarchy();

			return entityInfo;
		}
		finally
		{
			Destroy();
		}
		return nullptr;
	}

	double GetAnimationDuration(String^ inputFileName)
	{
		try
		{
			Initialize(inputFileName, nullptr, ImportConfiguration::ImportEntityConfig());

			auto animationConverter = gcnew AnimationConverter(logger, sceneMapping);
			auto animationData = animationConverter->ProcessAnimation(inputFilename, "", true);

			return animationData->Duration.TotalSeconds;
		}
		finally
		{
			Destroy();
		}

		return 0;
	}

	Model^ Convert(String^ inputFilename, String^ vfsOutputFilename, Dictionary<System::String^, int>^ materialIndices)
	{
		try
		{
			Initialize(inputFilename, vfsOutputFilename, ImportConfiguration::ImportAll());

			// Create default ModelViewData
			modelData = gcnew Model();

			//auto sceneName = scene->GetName();
			//if (sceneName != NULL && strlen(sceneName) > 0)
			//{
			//	entity->Name = gcnew String(sceneName);
			//}
			//else
			//{
			//	// Build scene name from file name
			//	entity->Name = Path::GetFileName(this->inputFilename);
			//}

			std::map<FbxMesh*, std::string> meshNames;
			GenerateMeshesName(meshNames);

			std::map<FbxSurfaceMaterial*, std::string> materialNames;
			GenerateMaterialNames(materialNames);

			std::map<FbxSurfaceMaterial*, int> materials;
			for (auto it = materialNames.begin(); it != materialNames.end(); ++it)
			{
				auto materialName = gcnew String(it->second.c_str());
				int materialIndex;
				if (materialIndices->TryGetValue(materialName, materialIndex))
				{
					materials[it->first] = materialIndex;
				}
				else
				{
					logger->Warning(String::Format("Model references material '{0}', but it was not defined in the ModelAsset.", materialName), (CallerInfo^)nullptr);
				}
			}

			// Process and add root entity
			ProcessNodeTransformation(scene->GetRootNode());
			ProcessNodeAttributes(scene->GetRootNode(), meshNames, materials);

			return modelData;
		}
		finally
		{
			Destroy();
		}

		return nullptr;
	}

	AnimationInfo^ ConvertAnimation(String^ inputFilename, String^ vfsOutputFilename, bool importCustomAttributeAnimations)
	{
		try
		{
			Initialize(inputFilename, vfsOutputFilename, ImportConfiguration::ImportAnimationsOnly());

			auto animationConverter = gcnew AnimationConverter(logger, sceneMapping);
			return animationConverter->ProcessAnimation(inputFilename, vfsOutputFilename, importCustomAttributeAnimations);
		}
		finally
		{
			Destroy();
		}

		return nullptr;
	}

	Skeleton^ ConvertSkeleton(String^ inputFilename, String^ vfsOutputFilename)
	{
		try
		{
			Initialize(inputFilename, vfsOutputFilename, ImportConfiguration::ImportSkeletonOnly());
			ProcessNodeTransformation(scene->GetRootNode());

			auto skeleton = gcnew Skeleton();
			skeleton->Nodes = sceneMapping->Nodes;
			return skeleton;
		}
		finally
		{
			Destroy();
		}

		return nullptr;
	}
};

} } }
