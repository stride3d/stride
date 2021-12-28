// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma once
#include "stdafx.h"

using namespace System;
using namespace System::IO;
using namespace System::Collections::Generic;
using namespace System::Runtime::InteropServices;
using namespace Stride::Core::Diagnostics;
using namespace Stride::Animations;
using namespace Stride::Rendering;
using namespace Stride::Engine;
using namespace Stride::Core::Mathematics;

namespace Stride {
	namespace Importer {
		namespace FBX {
			/// <summary>
			/// Contains mapping between FBX nodes and Stride ModelNodeDefinition
			/// </summary>
			ref class SceneMapping
			{
			private:
				FbxScene* scene;
				Dictionary<IntPtr, int>^ nodeMapping;
				array<ModelNodeDefinition>^ nodes;

				Matrix convertMatrix;
				Matrix inverseConvertMatrix;
				Matrix normalConvertMatrix;
			public:
				/// <summary>
				/// Initializes a new instance of the <see cref="NodeMapping"/> class.
				/// </summary>
				/// <param name="sceneArg">The scene argument.</param>
				SceneMapping(FbxScene* scene) : scene(scene)
				{
					if (scene == nullptr)
					{
						throw gcnew ArgumentNullException("scene");
					}
					nodeMapping = gcnew Dictionary<IntPtr, int>();

					// Generate names for all nodes
					std::map<FbxNode*, std::string> nodeNames;
					GenerateNodesName(scene, nodeNames);

					// Generate all ModelNodeDefinition
					auto nodeList = gcnew List<ModelNodeDefinition>();
					RegisterNode(scene->GetRootNode(), -1, nodeNames, nodeMapping, nodeList);
					nodes = nodeList->ToArray();

					// Setup the convertion
					FbxGlobalSettings& settings = scene->GetGlobalSettings();
					InitializeMatrix(settings.GetAxisSystem(), settings.GetSystemUnit());
				}

				/// <summary>
				/// Gets all the nodes.
				/// </summary>
				property array<ModelNodeDefinition>^ Nodes
				{
					array<ModelNodeDefinition>^ get()
					{
						return nodes;
					}
				}

				/// <summary>
				/// Gets the associated FbxScene.
				/// </summary>
				property FbxScene* Scene
				{
					FbxScene* get()
					{
						return scene;
					}
				}

				property Matrix MatrixModifier
				{
					Matrix get()
					{
						return convertMatrix;
					}
				}

				property float ScaleToMeters;

				property Matrix AxisSystemRotationMatrix;

				/// <summary>
				/// Finds the index of the FBX node in the <see cref="ModelNodeDefinition"/> from a FBX node.
				/// </summary>
				/// <param name="node">The node.</param>
				/// <returns>Stride.Rendering.ModelNodeDefinition.</returns>
				int FindNodeIndex(FbxNode* node)
				{
					int nodeIndex;
					if (!nodeMapping->TryGetValue((IntPtr)node, nodeIndex))
					{
						throw gcnew ArgumentException("Invalid node not found", "node");
					}

					return nodeIndex;
				}


				/// <summary>
				/// Finds a <see cref="ModelNodeDefinition"/> from a FBX node.
				/// </summary>
				/// <param name="node">The node.</param>
				/// <returns>Stride.Rendering.ModelNodeDefinition.</returns>
				ModelNodeDefinition FindNode(FbxNode* node)
				{
					int nodeIndex;
					if (!nodeMapping->TryGetValue((IntPtr)node, nodeIndex))
					{
						throw gcnew ArgumentException("Invalid node not found", "node");
					}

					return nodes[nodeIndex];
				}

				Matrix ConvertMatrixFromFbx(FbxAMatrix& _m) 
				{
					auto result = FBXMatrixToMatrix(_m);
					// Adjust translation
					result.M41 *= ScaleToMeters;
					result.M42 *= ScaleToMeters;
					result.M43 *= ScaleToMeters;
					return result;
				}

				Vector3 ConvertPointFromFbx(const FbxVector4& _p)
				{
					return (Vector3)FbxDouble4ToVector4(_p) * ScaleToMeters;
				}

				Vector3 ConvertNormalFromFbx(const FbxVector4& _p)
				{
					return (Vector3)FbxDouble4ToVector4(_p);
				}
			private:
				static void GetNodes(FbxNode* pNode, std::vector<FbxNode*>& nodes)
				{
					nodes.push_back(pNode);

					// Recursively process the children nodes.
					for (int j = 0; j < pNode->GetChildCount(); j++)
						GetNodes(pNode->GetChild(j), nodes);
				}

				static void GenerateNodesName(FbxScene* scene, std::map<FbxNode*, std::string>& nodeNames)
				{
					std::vector<FbxNode*> nodes;
					GetNodes(scene->GetRootNode(), nodes);

					std::map<std::string, int> nodeNameTotalCount;
					std::map<std::string, int> nodeNameCurrentCount;
					std::map<FbxNode*, std::string> tempNames;

					for (auto iter = nodes.begin(); iter != nodes.end(); ++iter)
					{
						auto pNode = *iter;
						auto nodeName = std::string(pNode->GetName());
						auto subBegin = nodeName.find_last_of(':');
						if (subBegin != std::string::npos)
							nodeName = nodeName.substr(subBegin + 1);
						tempNames[pNode] = nodeName;

						if (nodeNameTotalCount.count(nodeName) == 0)
							nodeNameTotalCount[nodeName] = 1;
						else
							nodeNameTotalCount[nodeName] = nodeNameTotalCount[nodeName] + 1;
					}

					for (auto iter = nodes.begin(); iter != nodes.end(); ++iter)
					{
						auto pNode = *iter;
						auto nodeName = tempNames[pNode];
						int currentCount = 0;

						if (nodeNameCurrentCount.count(nodeName) == 0)
							nodeNameCurrentCount[nodeName] = 1;
						else
							nodeNameCurrentCount[nodeName] = nodeNameCurrentCount[nodeName] + 1;

						if (nodeNameTotalCount[nodeName] > 1)
							nodeName = nodeName + "_" + std::to_string(nodeNameCurrentCount[nodeName]);

						nodeNames[pNode] = nodeName;
					}
				}

				static void RegisterNode(FbxNode* pNode, int parentIndex, std::map<FbxNode*, std::string>& nodeNames, Dictionary<IntPtr, int>^ nodeMapping, List<ModelNodeDefinition>^ nodes)
				{
					int currentIndex = nodes->Count;

					nodeMapping[(IntPtr)pNode] = currentIndex;

					// Create node
					ModelNodeDefinition modelNodeDefinition;
					modelNodeDefinition.ParentIndex = parentIndex;
					modelNodeDefinition.Transform.Scale = Vector3::One;
					modelNodeDefinition.Name = ConvertToUTF8(nodeNames[pNode]);
					modelNodeDefinition.Flags = ModelNodeFlags::Default;
					nodes->Add(modelNodeDefinition);

					// Recursively process the children nodes.
					for (int j = 0; j < pNode->GetChildCount(); j++)
					{
						RegisterNode(pNode->GetChild(j), currentIndex, nodeNames, nodeMapping, nodes);
					}
				}

				void InitializeMatrix(const FbxAxisSystem& axisSystem, const FbxSystemUnit& unitSystem)
				{
					auto fromMatrix = BuildAxisSystemMatrix(axisSystem);
					fromMatrix.Invert();
					//auto fromMatrix = Matrix::Identity;

					// Finds unit conversion ratio to ScaleImport (usually 0.01 so 1 meter). GetScaleFactor() is in cm.
					ScaleToMeters = (float)unitSystem.GetScaleFactor() * 0.01f;

					// Builds conversion matrices.
					AxisSystemRotationMatrix = fromMatrix;
				}

				static Matrix BuildAxisSystemMatrix(const FbxAxisSystem& axisSystem) {

					int signUp;
					int signFront;
					Vector3 up = Vector3::UnitY;
					Vector3 at = Vector3::UnitZ;

					const auto upAxis = axisSystem.GetUpVector(signUp);
					const auto frontAxisParityEven = axisSystem.GetFrontVector(signFront) == FbxAxisSystem::eParityEven;
					switch (upAxis)
					{
					case FbxAxisSystem::eXAxis:
					{
						up = Vector3::UnitX;
						at = frontAxisParityEven ? Vector3::UnitY : Vector3::UnitZ;
						break;
					}

					case FbxAxisSystem::eYAxis:
					{
						up = Vector3::UnitY;
						at = frontAxisParityEven ? Vector3::UnitX : Vector3::UnitZ;
						break;
					}

					case FbxAxisSystem::eZAxis:
					{
						up = Vector3::UnitZ;
						at = frontAxisParityEven ? Vector3::UnitX : Vector3::UnitY;
						break;
					}
					}
					up *= (float)signUp;
					at *= (float)signFront;

					auto right = axisSystem.GetCoorSystem() == FbxAxisSystem::eRightHanded ? Vector3::Cross(up, at) : Vector3::Cross(at, up);

					auto matrix = Matrix::Identity;
					matrix.Right = right;
					matrix.Up = up;
					matrix.Backward = at;

					return matrix;
				}
			};
		}
	}
}
