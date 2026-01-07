// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Silk.NET.Assimp;
using Stride.Animations;
using Stride.Assets.Materials;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Graphics;
using Stride.Graphics.Data;
using Stride.Importer.Common;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Mesh = Stride.Rendering.Mesh;
using PrimitiveType = Stride.Graphics.PrimitiveType;
using Scene = Silk.NET.Assimp.Scene;

namespace Stride.Importer.ThreeD
{
    public class MeshConverter
    {
        static MeshConverter()
        {
            if (Platform.Type == PlatformType.Windows)
                NativeLibraryHelper.PreloadLibrary("Assimp64", typeof(MeshConverter));
            else if (Platform.Type == PlatformType.macOS || Platform.Type == PlatformType.Linux)
                NativeLibraryHelper.PreloadLibrary("libassimp", typeof(MeshConverter));
        }

        private const int NumberOfBonesPerVertex = 4;

        public Logger Logger { get; set; }

        private readonly Assimp assimp = Assimp.GetApi();

        private string vfsInputFilename;
        private string vfsOutputFilename;
        private string vfsInputPath;

        private Quaternion rootOrientation;
        private Quaternion rootOrientationInverse;
        private Matrix rootTransform;
        private Matrix rootTransformInverse;
        private Model modelData;

        private readonly List<ModelNodeDefinition> nodes = new();
        private readonly Dictionary<string, int> textureNameCount = new();

        public MeshConverter(Logger logger)
        {
            Logger = logger ?? GlobalLogger.GetLogger("Import Assimp");
        }

        private void ResetConversionData()
        {
            textureNameCount.Clear();
        }

        public unsafe EntityInfo ExtractEntity(string inputFilename, string outputFilename, bool extractTextureDependencies, bool deduplicateMaterials)
        {
            try
            {
                uint importFlags = 0;
                var postProcessFlags = aiPostProcessSteps.aiProcess_SortByPType;

                if (deduplicateMaterials)
                {
                    postProcessFlags |= aiPostProcessSteps.aiProcess_RemoveRedundantMaterials;
                }

                var scene = Initialize(inputFilename, outputFilename, importFlags, postProcessFlags);
                // If scene is null, something went wrong inside Assimp
                if (scene == null)
                {
                    var error = assimp.GetErrorStringS();
                    if (error.Length > 0)
                    {
                        Logger.Error($"Assimp: {error}");
                    }

                    return null;
                }

                ExtractEmbededTexture(scene, inputFilename);

                var materialNames = new Dictionary<IntPtr, string>();
                var meshNames = new Dictionary<IntPtr, string>();
                var animationNames = new Dictionary<IntPtr, string>();
                var nodeNames = new Dictionary<IntPtr, string>();

                GenerateNodeNames(scene, nodeNames);

                var entityInfo = new EntityInfo
                {
                    Materials = ExtractMaterials(scene, materialNames),
                    Models = ExtractModels(scene, meshNames, materialNames, nodeNames),
                    Nodes = ExtractNodeHierarchy(scene, nodeNames),
                    AnimationNodes = ExtractAnimations(scene, animationNames)
                };

                if (extractTextureDependencies)
                    entityInfo.TextureDependencies = ExtractTextureDependencies(scene);

                return entityInfo;
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception has occured during Entity extraction : {ex.Message}");
                throw;
            }
        }

        public unsafe Model Convert(string inputFilename, string outputFilename, bool deduplicateMaterials)
        {
            uint importFlags = 0;

            aiPostProcessSteps postProcessFlags = 0;
            if (deduplicateMaterials)
            {
                postProcessFlags |= aiPostProcessSteps.aiProcess_RemoveRedundantMaterials;
            }

            var scene = Initialize(inputFilename, outputFilename, importFlags, postProcessFlags);
            return ConvertAssimpScene(scene);
        }

        public unsafe AnimationInfo ConvertAnimation(string inputFilename, string outputFilename, int animationIndex)
        {
            uint importFlags = 0;
            var scene = Initialize(inputFilename, outputFilename, importFlags, 0);

            return ProcessAnimations(scene, animationIndex);
        }

        public unsafe Rendering.Skeleton ConvertSkeleton(string inputFilename, string outputFilename)
        {
            uint importFlags = 0;
            aiPostProcessSteps postProcessFlags = 0;

            var scene = Initialize(inputFilename, outputFilename, importFlags, postProcessFlags);

            return ProcessSkeleton(scene);
        }

        private unsafe Scene* Initialize(string inputFilename, string outputFilename, uint importFlags, aiPostProcessSteps postProcessFlags)
        {
            ResetConversionData();

            vfsInputFilename = inputFilename;
            vfsOutputFilename = outputFilename;
            vfsInputPath = VirtualFileSystem.GetParentFolder(inputFilename);

            var propStore = assimp.CreatePropertyStore();
            assimp.SetImportPropertyInteger(propStore, "IMPORT_FBX_PRESERVE_PIVOTS", 0); // Trade some issues for others, see: https://github.com/assimp/assimp/issues/894, https://github.com/assimp/assimp/issues/1974
            assimp.SetImportPropertyFloat(propStore, "APP_SCALE_FACTOR", .01f);
            var scene = assimp.ImportFileExWithProperties(inputFilename, importFlags, null, propStore);

            postProcessFlags |= aiPostProcessSteps.aiProcess_CalcTangentSpace
                               | aiPostProcessSteps.aiProcess_Triangulate
                               | aiPostProcessSteps.aiProcess_GenNormals
                               | aiPostProcessSteps.aiProcess_SortByPType
                               | aiPostProcessSteps.aiProcess_FlipWindingOrder
                               | aiPostProcessSteps.aiProcess_FlipUVs
                               | aiPostProcessSteps.aiProcess_GlobalScale;

            scene = assimp.ApplyPostProcessing(scene, (uint)postProcessFlags);
            assimp.ReleasePropertyStore(propStore);
            return scene;
        }

        private unsafe Model ConvertAssimpScene(Scene* scene)
        {
            modelData = new Model();

            var meshNames = new Dictionary<IntPtr, string>();
            GenerateMeshNames(scene, meshNames);

            var nodeNames = new Dictionary<IntPtr, string>();
            var duplicateNodeNameToNodePointers = new Dictionary<string, List<IntPtr>>();
            GenerateNodeNames(scene, nodeNames, duplicateNodeNameToNodePointers);

            // register the nodes and fill hierarchy
            var meshIndexToNodeIndex = new Dictionary<int, List<int>>();
            var nodePointerToNodeIndex = new Dictionary<IntPtr, int>();
            RegisterNodes(scene->MRootNode, -1, nodeNames, meshIndexToNodeIndex, nodePointerToNodeIndex);

            // Map the Bone pointers to their corresponding Node pointer
            var bonePointerToNodePointerMap = GenerateBoneToNodeMap(scene, nodePointerToNodeIndex, duplicateNodeNameToNodePointers);

            // meshes
            for (var i = 0; i < scene->MNumMeshes; ++i)
            {
                if (!meshIndexToNodeIndex.TryGetValue(i, out var value))
                {
                    continue;
                }

                var meshInfo = ProcessMesh(scene, scene->MMeshes[i], meshNames, nodePointerToNodeIndex, bonePointerToNodePointerMap);

                if (meshInfo == null)
                {
                    continue;
                }

                foreach (var nodeIndex in value)
                {
                    var nodeMeshData = new Mesh
                    {
                        Draw = meshInfo.Draw,
                        Name = meshInfo.Name,
                        MaterialIndex = meshInfo.MaterialIndex,
                        NodeIndex = nodeIndex,
                    };

                    if (meshInfo.Bones != null)
                    {
                        nodeMeshData.Skinning = new MeshSkinningDefinition
                        {
                            Bones = meshInfo.Bones.ToArray()
                        };
                    }

                    if (meshInfo.HasSkinningPosition && meshInfo.TotalClusterCount > 0)
                        nodeMeshData.Parameters.Set(MaterialKeys.HasSkinningPosition, true);

                    if (meshInfo.HasSkinningNormal && meshInfo.TotalClusterCount > 0)
                        nodeMeshData.Parameters.Set(MaterialKeys.HasSkinningNormal, true);


                    modelData.Meshes.Add(nodeMeshData);
                }
            }


            return modelData;
        }

        private unsafe Dictionary<IntPtr, IntPtr> GenerateBoneToNodeMap(Scene* scene, Dictionary<IntPtr, int> nodePointerToNodeIndex, Dictionary<string, List<IntPtr>> duplicateNodeNameToNodePointers)
        {
            // Get the all bones in the scene
            var allBones = new List<(IntPtr NodePointer, string BoneName)>();
            var uniqueBoneNames = new HashSet<string>();
            for (int meshIdx = 0; meshIdx < scene->MNumMeshes; meshIdx++)
            {
                var mesh = scene->MMeshes[meshIdx];
                if (mesh->MNumBones == 0)
                {
                    continue;
                }

                for (int boneIdx = 0; boneIdx < mesh->MNumBones; boneIdx++)
                {
                    var bone = mesh->MBones[boneIdx];
                    string boneName = bone->MName.AsString.CleanNodeName();
                    allBones.Add(((IntPtr)bone, boneName));

                    // Note that bones may appear in multiple meshes
                    uniqueBoneNames.Add(boneName);
                }
            }

            // Find the node each bone corresponds to
            // Unfortunately, Assimp has a fundamental flaw where it does not properly track object types
            // and the only link we're given is the name, but names are *not* unique
            var bonePointerToNodePointerMap = new Dictionary<IntPtr, IntPtr>();
            for (int i = allBones.Count - 1; i >= 0; i--)
            {
                var (bonePtr, boneName) = allBones[i];
                var bone = (Bone*)bonePtr;
                // While bone names are expected to be unique, *Node* names are NOT unique because
                // nodes represent any object, eg. armature, mesh, bone, empty, etc.
                // We iterate the bone pointer list in reverse because bones usually have a hierarchy
                // such that we can test the parent or child node names as a best guess
                IntPtr? boneNodePointer = null;
                if (duplicateNodeNameToNodePointers.TryGetValue(boneName, out var dupNodePointers))
                {
                    // Case 1: If a child bone/node is connected to one of the duplicate node,
                    // then it is most likely that node is the bone node
                    for (int j = dupNodePointers.Count - 1; j >= 0; j--)
                    {
                        var nodePtr = dupNodePointers[j];
                        bool isConnectedToBoneNode = false;
                        foreach (var kv in bonePointerToNodePointerMap)
                        {
                            var otherBoneNodePtr = kv.Value;
                            var otherBoneNode = (Node*)otherBoneNodePtr;
                            var otherNodeParent = otherBoneNode->MParent;
                            if (nodePtr == (IntPtr)otherNodeParent)
                            {
                                isConnectedToBoneNode = true;
                                break;
                            }
                        }
                        if (isConnectedToBoneNode)
                        {
                            boneNodePointer = nodePtr;
                            break;
                        }
                    }
                    if (boneNodePointer is null)
                    {
                        // Case 2: This bone is a leaf bone part of a parent bone node
                        for (int j = dupNodePointers.Count - 1; j >= 0; j--)
                        {
                            var nodePtr = dupNodePointers[j];
                            var node = (Node*)nodePtr;
                            if (node->MNumMeshes > 0)
                            {
                                continue;   // Mesh object node, so we can ignore it
                            }
                            var parentNode = node->MParent;
                            if (parentNode is not null)
                            {
                                var parentNodeName = parentNode->MName.AsString.CleanNodeName();
                                if (parentNodeName == boneName)
                                {
                                    continue;   // This might occur when this bone is connected to an armature node with the same name (ie. the parent is NOT a bone node)
                                }
                                if (uniqueBoneNames.Contains(parentNodeName))
                                {
                                    boneNodePointer = nodePtr;
                                    break;
                                }
                            }
                        }
                    }
                    if (boneNodePointer is null)
                    {
                        // Case 3: This bone is a bone with a (confirmed) sibling bone
                        for (int j = dupNodePointers.Count - 1; j >= 0; j--)
                        {
                            var nodePtr = dupNodePointers[j];
                            var node = (Node*)nodePtr;
                            if (node->MNumMeshes > 0)
                            {
                                continue;   // Mesh object node, so we can ignore it
                            }
                            var parentNode = node->MParent;
                            if (parentNode is not null)
                            {
                                bool hasBoneSibling = false;
                                foreach (var kv in bonePointerToNodePointerMap)
                                {
                                    var otherBoneNodePtr = kv.Value;
                                    var otherBoneNode = (Node*)otherBoneNodePtr;
                                    if (otherBoneNode->MParent == parentNode)
                                    {
                                        hasBoneSibling = true;
                                        break;
                                    }
                                }
                                if (hasBoneSibling)
                                {
                                    boneNodePointer = nodePtr;
                                    break;
                                }
                            }
                        }
                    }
                    if (boneNodePointer is null)
                    {
                        // Final case: We can only make a best guess
                        // This might occur if you just have a single bone, or we didn't detect any related bone because we are the first node being processed
                        Logger.Warning($"Unable to properly determine bone node due to duplicate name: {boneName}. The name must be unique to ensure the animation plays correctly.");
                        for (int j = dupNodePointers.Count - 1; j >= 0; j--)
                        {
                            var nodePtr = dupNodePointers[j];
                            var node = (Node*)nodePtr;
                            if (node->MNumMeshes > 0)
                            {
                                continue;   // Mesh object node, so we can ignore it
                            }
                            boneNodePointer = nodePtr;
                            break;
                        }
                    }
                }
                else
                {
                    // Only one node with the same name so this must be the bone's node
                    for (int nodeIdx = 0; nodeIdx < nodes.Count; nodeIdx++)
                    {
                        if (boneName == nodes[nodeIdx].Name)
                        {
                            if (TryGetNodePointerFromNodeIndex(nodePointerToNodeIndex, nodeIdx, out var ptr))
                            {
                                boneNodePointer = ptr;
                            }
                            else
                            {
                                System.Diagnostics.Debug.Fail($"Node index was not mapped to a node pointer.");
                            }
                            break;
                        }
                    }
                }
                if (boneNodePointer is IntPtr foundNodePointer)
                {
                    System.Diagnostics.Debug.Assert(!bonePointerToNodePointerMap.ContainsKey(bonePtr));
                    bonePointerToNodePointerMap[bonePtr] = foundNodePointer;
                }
            }

            return bonePointerToNodePointerMap;

            static bool TryGetNodePointerFromNodeIndex(Dictionary<IntPtr, int> nodePointerToNodeIndex, int nodeIndex, out IntPtr nodePointer)
            {
                foreach (var kv in nodePointerToNodeIndex)
                {
                    if (kv.Value == nodeIndex)
                    {
                        nodePointer = kv.Key;
                        return true;
                    }
                }

                nodePointer = default;
                return false;
            }
        }

        private unsafe Rendering.Skeleton ProcessSkeleton(Scene* scene)
        {
            var nodeNames = new Dictionary<IntPtr, string>();
            var duplicateNodeNameToNodePointers = new Dictionary<string, List<IntPtr>>();
            GenerateNodeNames(scene, nodeNames, duplicateNodeNameToNodePointers);

            // register the nodes and fill hierarchy
            var meshIndexToNodeIndex = new Dictionary<int, List<int>>();
            RegisterNodes(scene->MRootNode, -1, nodeNames, meshIndexToNodeIndex);

            return new Rendering.Skeleton
            {
                Nodes = nodes.ToArray()
            };
        }

        private unsafe AnimationInfo ProcessAnimations(Scene* scene, int animationIndex)
        {
            var animationData = new AnimationInfo();
            var visitedNodeNames = new HashSet<string>();

            var nodeNames = new Dictionary<IntPtr, string>();
            var duplicateNodeNameToNodePointers = new Dictionary<string, List<IntPtr>>();
            GenerateNodeNames(scene, nodeNames, duplicateNodeNameToNodePointers);

            // register the nodes and fill hierarchy
            var meshIndexToNodeIndex = new Dictionary<int, List<int>>();
            var nodePointerToNodeIndex = new Dictionary<IntPtr, int>();
            var duplicateNodeNameToNodeIndices = new Dictionary<string, List<int>>();
            RegisterNodes(scene->MRootNode, -1, nodeNames, meshIndexToNodeIndex, nodePointerToNodeIndex, duplicateNodeNameToNodeIndices);

            // Map the Bone pointers to their corresponding Node pointer
            var bonePointerToNodePointerMap = GenerateBoneToNodeMap(scene, nodePointerToNodeIndex, duplicateNodeNameToNodePointers);
            // Map the Bone name to the index of our internal 'nodes' list
            var boneNameToNodeIndex = new Dictionary<string, int>();
            foreach (var kv in bonePointerToNodePointerMap)
            {
                var bone = (Bone*)kv.Key;
                var boneName = bone->MName.AsString.CleanNodeName();
                if (boneNameToNodeIndex.ContainsKey(boneName))
                {
                    // A bone may appear on multple meshes, so only deal with the first encountered one.
                    // Also note we cannot handle the case of multiple armatures in the same 'scene' with the same bone names
                    // (this error is logged further down the code).
                    continue;
                }
                if (TryFindNodeIndexFromBone(bone, bonePointerToNodePointerMap, nodePointerToNodeIndex, out int nodeIndex))
                {
                    boneNameToNodeIndex[boneName] = nodeIndex;
                }
                else
                {
                    Logger.Error($"Failed to find node with the same bone name '{boneName}'");
                }
            }

            if (scene->MNumAnimations > 0)
            {
                if (animationIndex < 0)
                {
                    animationIndex = 0;
                    Logger.Warning("Specified animation index less than zero, using the first animation.");
                }

                if (animationIndex >= scene->MNumAnimations)
                {
                    animationIndex = (int)scene->MNumAnimations - 1;
                    Logger.Warning("There less animations in the file than the animation index, using the last animation.");
                }

                var aiAnim = scene->MAnimations[animationIndex];

                // animation speed
                var ticksPerSec = aiAnim->MTicksPerSecond;

                animationData.Duration = Utils.AiTimeToStrideTimeSpan(aiAnim->MDuration, ticksPerSec);

                // Assimp animations have two different channels of animations ((1) on Nodes, (2) on Meshes).
                // Nevertheless the second one do not seems to be usable in assimp 3.0 so it will be ignored here.

                // name of the animation (dropped)
                var animName = aiAnim->MName.AsString.CleanNodeName(); // used only be the logger

                // animation using meshes (not supported)
                for (uint meshAnimId = 0; meshAnimId < aiAnim->MNumMeshChannels; ++meshAnimId)
                {
                    var meshName = aiAnim->MMeshChannels[meshAnimId]->MName.AsString;
                    Logger.Warning($"Mesh animations are not currently supported. Animation '{animName}' on mesh {meshName} will be ignored");
                }

                // animation on nodes
                for (uint nodeAnimId = 0; nodeAnimId < aiAnim->MNumChannels; ++nodeAnimId)
                {
                    var nodeAnim = aiAnim->MChannels[nodeAnimId];
                    var nodeName = nodeAnim->MNodeName.AsString.CleanNodeName();

                    // TODO: Need to resample the animation created by the pivot chain into a single animation, have a look at the file hierarchy in Assimp's viewer to get a better clue
                    // See: 'IMPORT_FBX_PRESERVE_PIVOTS' above and https://github.com/assimp/assimp/discussions/4966
                    if (nodeAnim->MNodeName.AsString.Contains("$AssimpFbx$"))
                        Logger.Warning($"Animation '{animName}' contains a pivot bone ({nodeAnim->MNodeName.AsString}), we currently do not handle these. This animation may not resolve properly.");

                    if (visitedNodeNames.Add(nodeName))
                    {
                        ProcessNodeAnimation(animationData.AnimationClips, nodeAnim, ticksPerSec, duplicateNodeNameToNodeIndices, boneNameToNodeIndex);
                    }
                    else
                    {
                        Logger.Error($"Animation '{animName}' uses two nodes with the same name ({nodeAnim->MNodeName.AsString}). The animation cannot be resolved.");
                        return null;
                    }
                }
            }

            return animationData;
        }

        private unsafe void ProcessNodeAnimation(Dictionary<string, AnimationClip> animationClips, NodeAnim* nodeAnim, double ticksPerSec, Dictionary<string, List<int>> duplicateNodeNameToNodeIndices, Dictionary<string, int> boneNameToNodeIndex)
        {
            // Find the nodes on which the animation is performed
            var nodeName = nodeAnim->MNodeName.AsString.CleanNodeName();

            var animationClip = new AnimationClip();

            // The translation
            ProcessAnimationCurveVector(animationClip, nodeAnim->MPositionKeys, nodeAnim->MNumPositionKeys, "Transform.Position", ticksPerSec, true);
            // The rotation
            ProcessAnimationCurveQuaternion(animationClip, nodeAnim->MRotationKeys, nodeAnim->MNumRotationKeys, "Transform.Rotation", ticksPerSec);
            // The scales
            ProcessAnimationCurveVector(animationClip, nodeAnim->MScalingKeys, nodeAnim->MNumScalingKeys, "Transform.Scale", ticksPerSec, false);

            string targetNodeName = null;
            if (duplicateNodeNameToNodeIndices.TryGetValue(nodeName, out var nodeIndices))
            {
                // Node name has remapped to be unique
                // For animation we assume the target is most likely a bone so we choose the bone node as a higher priority
                if (!boneNameToNodeIndex.TryGetValue(nodeName, out int nodeIndex))
                {
                    nodeIndex = nodeIndices[0];    // Just select the first duplicate node, and hope this is what animation was targeting
                    Logger.Warning($"Unable to properly determine target node for animation due to duplicate name: {nodeName}. The name must be unique to ensure the animation plays correctly.");
                }
                targetNodeName = nodes[nodeIndex].Name;
            }
            else
            {
                targetNodeName = nodeName;
            }

            if (animationClip.Curves.Count > 0)
                animationClips.Add(targetNodeName, animationClip);
        }

        private unsafe void ProcessAnimationCurveVector(AnimationClip animationClip, VectorKey* keys, uint nbKeys, string partialTargetName, double ticksPerSec, bool isTranslation)
        {
            var animationCurve = new AnimationCurve<Vector3>();

            // Switch to cubic implicit interpolation mode for Vector3
            animationCurve.InterpolationType = AnimationCurveInterpolationType.Cubic;

            var lastKeyTime = new CompressedTimeSpan();

            for (uint keyId = 0; keyId < nbKeys; ++keyId)
            {
                var aiKey = keys[keyId];

                var key = new KeyFrameData<Vector3>
                {
                    Time = lastKeyTime = Utils.AiTimeToStrideTimeSpan(aiKey.MTime, ticksPerSec),
                    Value = aiKey.MValue.ToStrideVector3()
                };

                if (isTranslation)
                {
                    // Change of basis: key.Value = (rootTransformInverse * Matrix::Translation(key.Value) * rootTransform).TranslationVector;
                    Vector3.TransformCoordinate(ref key.Value, ref rootTransform, out key.Value);
                }
                else
                {
                    // Change of basis: key.Value = (rootTransformInverse * Matrix::Scaling(key.Value) * rootTransform).ScaleVector;
                    var scale = Vector3.One;
                    Vector3.TransformNormal(ref scale, ref rootTransformInverse, out scale);
                    scale *= key.Value;
                    Vector3.TransformNormal(ref scale, ref rootTransform, out key.Value);
                }

                animationCurve.KeyFrames.Add(key);
                if (keyId == 0 || keyId == nbKeys - 1) // discontinuity at animation first and last frame
                    animationCurve.KeyFrames.Add(key); // add 2 times the same frame at discontinuities to have null gradient
            }

            animationClip.AddCurve(partialTargetName, animationCurve, false);

            if (nbKeys > 0 && animationClip.Duration < lastKeyTime)
            {
                animationClip.Duration = lastKeyTime;
            }
        }

        private unsafe void ProcessAnimationCurveQuaternion(AnimationClip animationClip, QuatKey* keys, uint nbKeys, string partialTargetName, double ticksPerSec)
        {
            var animationCurve = new AnimationCurve<Quaternion>();

            var lastKeyTime = new CompressedTimeSpan();

            for (uint keyId = 0; keyId < nbKeys; ++keyId)
            {
                var aiKey = keys[keyId];
                var key = new KeyFrameData<Quaternion>
                {
                    Time = lastKeyTime = Utils.AiTimeToStrideTimeSpan(aiKey.MTime, ticksPerSec),
                    Value = aiKey.MValue.ToStrideQuaternion()
                };

                key.Value = rootOrientationInverse * key.Value * rootOrientation;

                animationCurve.KeyFrames.Add(key);
            }

            animationClip.AddCurve(partialTargetName, animationCurve, false);

            if (nbKeys > 0 && animationClip.Duration < lastKeyTime)
            {
                animationClip.Duration = lastKeyTime;
            }
        }

        private unsafe void GenerateUniqueNames(Dictionary<IntPtr, string> finalNames, List<string> baseNames, Func<int, IntPtr> objectToName, Dictionary<string, List<IntPtr>> duplicateNodeNameToNodePointers = null)
        {
            var itemNameTotalCount = new Dictionary<string, int>();
            var itemNameCurrentCount = new Dictionary<string, int>();
            var tempNames = new List<string>();

            for (var i = 0; i < baseNames.Count; ++i)
            {
                // Clean the name by removing unwanted characters
                var itemName = baseNames[i].CleanNodeName();

                tempNames.Add(itemName);

                // count the occurrences of this name
                if (!itemNameTotalCount.TryAdd(itemName, 1))
                    itemNameTotalCount[itemName]++;
            }

            for (var i = 0; i < baseNames.Count; ++i)
            {
                var lItem = objectToName(i);
                var itemName = tempNames[i];

                if (itemNameTotalCount[itemName] > 1)
                {
                    if (!itemNameCurrentCount.TryAdd(itemName, 1))
                    {
                        itemNameCurrentCount[itemName]++;
                    }
                    if (duplicateNodeNameToNodePointers is not null)
                    {
                        var nodePointers = duplicateNodeNameToNodePointers.GetOrCreateValue(itemName);
                        nodePointers.Add(lItem);
                    }

                    itemName = itemName + "_" + itemNameCurrentCount[itemName].ToString(CultureInfo.InvariantCulture);
                }

                finalNames.Add(lItem, itemName);
            }
        }

        private unsafe void GenerateMeshNames(Scene* scene, Dictionary<IntPtr, string> meshNames)
        {
            var baseNames = new List<string>();
            for (uint i = 0; i < scene->MNumMeshes; i++)
            {
                var lMesh = scene->MMeshes[i];
                baseNames.Add(lMesh->MName.AsString.CleanNodeName());
            }
            GenerateUniqueNames(meshNames, baseNames, i => (IntPtr)scene->MMeshes[i]);
        }

        private unsafe void GenerateAnimationNames(Scene* scene, Dictionary<IntPtr, string> animationNames)
        {
            var baseNames = new List<string>();
            for (uint i = 0; i < scene->MNumAnimations; i++)
            {
                var lAnimation = scene->MAnimations[i];
                var animationName = lAnimation->MName.AsString.CleanNodeName();
                baseNames.Add(animationName);
            }

            GenerateUniqueNames(animationNames, baseNames, i => (IntPtr)scene->MAnimations[i]);
        }

        private unsafe void GenerateNodeNames(Scene* scene, Dictionary<IntPtr, string> nodeNames, Dictionary<string, List<IntPtr>> duplicateNodeNameToNodePointers = null)
        {
            var baseNames = new List<string>();
            var orderedNodes = new List<IntPtr>();

            GetNodeNames(scene->MRootNode, baseNames, orderedNodes);
            GenerateUniqueNames(nodeNames, baseNames, i => orderedNodes[i], duplicateNodeNameToNodePointers);
        }

        private unsafe void GetNodeNames(Node* node, List<string> nodeNames, List<IntPtr> orderedNodes)
        {
            nodeNames.Add(node->MName.AsString.CleanNodeName());
            orderedNodes.Add((IntPtr)node);

            for (uint i = 0; i < node->MNumChildren; ++i)
            {
                GetNodeNames(node->MChildren[i], nodeNames, orderedNodes);
            }
        }

        private unsafe void RegisterNodes(
            Node* fromNode, int parentIndex,
            Dictionary<IntPtr, string> nodeNames, Dictionary<int, List<int>> meshIndexToNodeIndex,
            Dictionary<IntPtr, int> nodePointerToNodeIndex = null, Dictionary<string, List<int>> duplicateNodeNameToNodeIndices = null)
        {
            var nodeIndex = nodes.Count;

            // assign the index of the node to the index of the mesh
            for (uint m = 0; m < fromNode->MNumMeshes; ++m)
            {
                var meshIndex = fromNode->MMeshes[m];

                if (!meshIndexToNodeIndex.TryGetValue((int)meshIndex, out var nodeIndices))
                {
                    nodeIndices = new List<int>();
                    meshIndexToNodeIndex.Add((int)meshIndex, nodeIndices);
                }

                nodeIndices.Add(nodeIndex);
            }

            // Create node
            var modelNodeDefinition = new ModelNodeDefinition
            {
                ParentIndex = parentIndex,
                Name = nodeNames[(IntPtr)fromNode],
                Flags = ModelNodeFlags.Default
            };

            // Extract scene scaling and rotation from the root node.
            // Bake scaling into all node's positions and rotation into the 1st-level nodes.

            if (parentIndex == -1)
            {
                rootTransform = fromNode->MTransformation.ToStrideMatrix();

                rootTransform.Decompose(out var rootScaling, out rootOrientation, out var rootTranslation);

                rootTransformInverse = Matrix.Invert(rootTransform);
                rootOrientationInverse = Quaternion.Invert(rootOrientation);

                modelNodeDefinition.Transform.Rotation = Quaternion.Identity;
                modelNodeDefinition.Transform.Scale = Vector3.One;
            }
            else
            {
                var transform = fromNode->MTransformation.ToStrideMatrix();
                transform.Decompose(out modelNodeDefinition.Transform.Scale, out modelNodeDefinition.Transform.Rotation, out modelNodeDefinition.Transform.Position);
            }

            nodes.Add(modelNodeDefinition);

            if (nodePointerToNodeIndex is not null)
            {
                nodePointerToNodeIndex.Add((IntPtr)fromNode, nodeIndex);
            }
            if (duplicateNodeNameToNodeIndices is not null)
            {
                string originalNodeName = fromNode->MName.AsString.CleanNodeName();
                if (!string.IsNullOrWhiteSpace(originalNodeName) && !string.Equals(originalNodeName, modelNodeDefinition.Name))
                {
                    if (!duplicateNodeNameToNodeIndices.TryGetValue(originalNodeName, out var nodeIndices))
                    {
                        nodeIndices = new();
                        duplicateNodeNameToNodeIndices[originalNodeName] = nodeIndices;
                        Logger.Info($"Duplicate node name found in model: {originalNodeName}. Model might not appear correctly if this node is a bone, or animations may not play correctly if this is a target node.");
                    }
                    nodeIndices.Add(nodeIndex);
                }
            }

            // register the children
            for (uint child = 0; child < fromNode->MNumChildren; ++child)
            {
                RegisterNodes(fromNode->MChildren[child], nodeIndex, nodeNames, meshIndexToNodeIndex, nodePointerToNodeIndex, duplicateNodeNameToNodeIndices);
            }
        }

        private unsafe MeshInfo ProcessMesh(Scene* scene, Silk.NET.Assimp.Mesh* mesh, Dictionary<IntPtr, string> meshNames, Dictionary<IntPtr, int> nodePointerToNodeIndex, Dictionary<IntPtr, IntPtr> bonePointerToNodePointerMap)
        {
            List<MeshBoneDefinition> bones = null;
            var hasSkinningPosition = false;
            var hasSkinningNormal = false;
            var totalClusterCount = 0;
            var drawData = new MeshDraw();
            // Build the bone's indices/weights and attach bones to NodeData
            //(bones info are present in the mesh so that is why we have to perform that here)

            var vertexIndexToBoneIdWeight = new List<List<(short boneIndex, float weight)>>();
            if (mesh->MNumBones > 0)
            {
                bones = new List<MeshBoneDefinition>();

                // TODO: change this to support shared meshes across nodes

                // size of the array is already known
                vertexIndexToBoneIdWeight.Capacity = (int)mesh->MNumVertices;
                for (var i = 0; i < (int)mesh->MNumVertices; i++)
                {
                    vertexIndexToBoneIdWeight.Add(new List<(short, float)>());
                }

                // Build skinning clusters and fill controls points data structure
                for (uint boneId = 0; boneId < mesh->MNumBones; ++boneId)
                {
                    var bone = mesh->MBones[boneId];

                    // Fill controlPts with bone controls on the mesh
                    for (uint vtxWeightId = 0; vtxWeightId < bone->MNumWeights; ++vtxWeightId)
                    {
                        var vtxWeight = bone->MWeights[vtxWeightId];
                        vertexIndexToBoneIdWeight[(int)vtxWeight.MVertexId].Add(((short)boneId, vtxWeight.MWeight));
                    }

                    // Find the node where the bone is mapped
                    if (!TryFindNodeIndexFromBone(bone, bonePointerToNodePointerMap, nodePointerToNodeIndex, out int nodeIndex))
                    {
                        var boneName = bone->MName.AsString.CleanNodeName();
                        Logger.Error($"No node found for name {boneId}:{boneName}");
                        nodeIndex = 0;
                    }

                    bones.Add(new MeshBoneDefinition
                    {
                        NodeIndex = nodeIndex,
                        LinkToMeshMatrix = rootTransformInverse * bone->MOffsetMatrix.ToStrideMatrix() * rootTransform
                    });
                }

                NormalizeVertexWeights(vertexIndexToBoneIdWeight, NumberOfBonesPerVertex);

                totalClusterCount = (int)mesh->MNumBones;
                if (totalClusterCount > 0)
                {
                    hasSkinningPosition = true;
                    hasSkinningNormal = mesh->MNormals != null;
                }
            }

            // Build the vertex declaration
            var vertexElements = new List<VertexElement>();
            var vertexStride = 0;

            var positionOffset = vertexStride;
            vertexElements.Add(VertexElement.Position<Vector3>(0, vertexStride));
            vertexStride += sizeof(Vector3);

            var normalOffset = vertexStride;
            if (mesh->MNormals != null)
            {
                vertexElements.Add(VertexElement.Normal<Vector3>(0, vertexStride));
                vertexStride += sizeof(Vector3);
            }

            var uvOffset = vertexStride;
            var sizeUV = sizeof(Vector2); // 3D uv not supported
            for (uint uvChannel = 0; uvChannel < Utils.GetNumUVChannels(mesh); ++uvChannel)
            {
                vertexElements.Add(VertexElement.TextureCoordinate<Vector2>((int)uvChannel, vertexStride));
                vertexStride += sizeUV;
            }

            var colorOffset = vertexStride;
            var sizeColor = sizeof(Color);
            for (uint colorChannel = 0; colorChannel < Utils.GetNumColorChannels(mesh); ++colorChannel)
            {
                vertexElements.Add(VertexElement.Color<Color>((int)colorChannel, vertexStride));
                vertexStride += sizeColor;
            }

            var tangentOffset = vertexStride;
            if (mesh->MTangents != null)
            {
                vertexElements.Add(VertexElement.Tangent<Vector3>(0, vertexStride));
                vertexStride += sizeof(Vector3);
            }

            var bitangentOffset = vertexStride;
            if (mesh->MTangents != null)
            {
                vertexElements.Add(VertexElement.BiTangent<Vector3>(0, vertexStride));
                vertexStride += sizeof(Vector3);
            }

            var blendIndicesOffset = vertexStride;
            var controlPointIndices16 = totalClusterCount > 256;
            if (vertexIndexToBoneIdWeight.Count > 0)
            {
                if (controlPointIndices16)
                {
                    vertexElements.Add(new VertexElement("BLENDINDICES", 0, PixelFormat.R16G16B16A16_UInt, vertexStride));
                    vertexStride += sizeof(ushort) * 4;
                }
                else
                {
                    vertexElements.Add(new VertexElement("BLENDINDICES", 0, PixelFormat.R8G8B8A8_UInt, vertexStride));
                    vertexStride += sizeof(byte) * 4;
                }
            }

            var blendWeightOffset = vertexStride;
            if (vertexIndexToBoneIdWeight.Count > 0)
            {
                vertexElements.Add(new VertexElement("BLENDWEIGHT", 0, PixelFormat.R32G32B32A32_Float, vertexStride));
                vertexStride += sizeof(float) * 4;
            }

            // Build the vertices data buffer
            var vertexBuffer = new byte[vertexStride * mesh->MNumVertices];
            fixed (byte* vertexBufferPtr = &vertexBuffer[0])
            {
                var vbPointer = vertexBufferPtr;
                for (uint i = 0; i < mesh->MNumVertices; i++)
                {
                    var positionPointer = (Vector3*)(vbPointer + positionOffset);
                    *positionPointer = mesh->MVertices[i].ToStrideVector3();

                    Vector3.TransformCoordinate(ref *positionPointer, ref rootTransform, out *positionPointer);

                    if (mesh->MNormals != null)
                    {
                        var normalPointer = (Vector3*)(vbPointer + normalOffset);
                        *normalPointer = mesh->MNormals[i].ToStrideVector3();

                        Vector3.TransformNormal(ref *normalPointer, ref rootTransform, out *normalPointer);

                        if (float.IsNaN(normalPointer->X) || float.IsNaN(normalPointer->Y) || float.IsNaN(normalPointer->Z))
                            *normalPointer = new Vector3(1, 0, 0);
                        else
                            normalPointer->Normalize();
                    }

                    for (uint uvChannel = 0; uvChannel < Utils.GetNumUVChannels(mesh); ++uvChannel)
                    {
                        var textureCoord = mesh->MTextureCoords[(int)uvChannel][i];
                        *((Vector2*)(vbPointer + uvOffset + sizeUV * uvChannel)) = new Vector2(textureCoord.X, textureCoord.Y); // 3D uv not supported
                    }

                    for (uint colorChannel = 0; colorChannel < Utils.GetNumColorChannels(mesh); ++colorChannel)
                    {
                        var color = mesh->MColors[(int)colorChannel][i].ToStrideColor();
                        *((Color*)(vbPointer + colorOffset + sizeColor * colorChannel)) = color;
                    }

                    if (mesh->MTangents != null)
                    {
                        var tangentPointer = (Vector3*)(vbPointer + tangentOffset);
                        var bitangentPointer = (Vector3*)(vbPointer + bitangentOffset);
                        *tangentPointer = mesh->MTangents[i].ToStrideVector3();
                        *bitangentPointer = mesh->MBitangents[i].ToStrideVector3();
                        if (float.IsNaN(tangentPointer->X) || float.IsNaN(tangentPointer->Y) || float.IsNaN(tangentPointer->Z) ||
                            float.IsNaN(bitangentPointer->X) || float.IsNaN(bitangentPointer->Y) || float.IsNaN(bitangentPointer->Z))
                        {
                            var normalPointer = ((Vector3*)(vbPointer + normalOffset));
                            Vector3 c1 = Vector3.Cross(*normalPointer, new Vector3(0.0f, 0.0f, 1.0f));
                            Vector3 c2 = Vector3.Cross(*normalPointer, new Vector3(0.0f, 1.0f, 0.0f));

                            if (c1.LengthSquared() > c2.LengthSquared())
                                *tangentPointer = c1;
                            else
                                *tangentPointer = c2;
                            *bitangentPointer = Vector3.Cross(*normalPointer, *tangentPointer);
                        }
                        tangentPointer->Normalize();
                        bitangentPointer->Normalize();
                    }

                    if (vertexIndexToBoneIdWeight.Count > 0)
                    {
                        for (var bone = 0; bone < NumberOfBonesPerVertex; ++bone)
                        {
                            if (controlPointIndices16)
                            {
                                ((ushort*)(vbPointer + blendIndicesOffset))[bone] = (ushort)vertexIndexToBoneIdWeight[(int)i][bone].Item1;
                            }
                            else
                            {
                                (vbPointer + blendIndicesOffset)[bone] = (byte)vertexIndexToBoneIdWeight[(int)i][bone].Item1;
                            }

                            ((float*)(vbPointer + blendWeightOffset))[bone] = vertexIndexToBoneIdWeight[(int)i][bone].Item2;
                        }
                    }

                    vbPointer += vertexStride;
                }
            }

            // Build the indices data buffer
            var nbIndices = (int)(3 * mesh->MNumFaces);
            var is32BitIndex = mesh->MNumVertices > 65535;
            int arraySize = is32BitIndex ? sizeof(uint) * nbIndices : sizeof(ushort) * nbIndices;

            //Mesh has no vertices
            if(arraySize < 1)
            {
                return null;
            }

            byte[] indexBuffer = new byte[arraySize];

            fixed (byte* indexBufferPtr = &indexBuffer[0])
            {
                var ibPointer = indexBufferPtr;

                for (uint i = 0; i < mesh->MNumFaces; i++)
                {
                    if (is32BitIndex)
                    {
                        for (int j = 0; j < 3; ++j)
                        {
                            *((uint*)ibPointer) = mesh->MFaces[(int)i].MIndices[j];
                            ibPointer += sizeof(uint);
                        }
                    }
                    else
                    {
                        for (int j = 0; j < 3; ++j)
                        {
                            *((ushort*)ibPointer) = (ushort)(mesh->MFaces[(int)i].MIndices[j]);
                            ibPointer += sizeof(ushort);
                        }
                    }
                }
            }

            // Build the mesh data
            var vertexDeclaration = new VertexDeclaration(vertexElements.ToArray());
            var vertexBufferBinding = new VertexBufferBinding(GraphicsSerializerExtensions.ToSerializableVersion(new BufferData(BufferFlags.VertexBuffer, vertexBuffer)), vertexDeclaration, (int)mesh->MNumVertices, vertexDeclaration.VertexStride, 0);
            var indexBufferBinding = new IndexBufferBinding(GraphicsSerializerExtensions.ToSerializableVersion(new BufferData(BufferFlags.IndexBuffer, indexBuffer)), is32BitIndex, (int)nbIndices, 0);


            drawData.VertexBuffers = new VertexBufferBinding[] { vertexBufferBinding };
            drawData.IndexBuffer = indexBufferBinding;
            drawData.PrimitiveType = PrimitiveType.TriangleList;
            drawData.DrawCount = (int)nbIndices;

            return new MeshInfo
            {
                Draw = drawData,
                Name = meshNames[(IntPtr)mesh],
                Bones = bones,
                MaterialIndex = (int)mesh->MMaterialIndex,
                HasSkinningPosition = hasSkinningPosition,
                HasSkinningNormal = hasSkinningNormal,
                TotalClusterCount = totalClusterCount
            };
        }

        /// <summary>
        /// Returns true if <paramref name="nodeIndex"/> on <see cref="nodes"/> corresponding to this <paramref name="bone"/> was found.
        /// </summary>
        private unsafe bool TryFindNodeIndexFromBone(Bone* bone, Dictionary<IntPtr, IntPtr> bonePointerToNodePointerMap, Dictionary<IntPtr, int> nodePointerToNodeIndex, out int nodeIndex)
        {
            if (bonePointerToNodePointerMap.TryGetValue((IntPtr)bone, out var nodePointer))
            {
                return nodePointerToNodeIndex.TryGetValue(nodePointer, out nodeIndex);
            }
            nodeIndex = -1;
            return false;
        }

        private void NormalizeVertexWeights(List<List<(short, float)>> controlPts, int nbBoneByVertex)
        {
            for (var vertexId = 0; vertexId < controlPts.Count; ++vertexId)
            {
                var curVertexWeights = controlPts[vertexId];

                // check that one vertex has not more than 'nbBoneByVertex' associated bones
                if (curVertexWeights.Count > nbBoneByVertex)
                {
                    Logger.Warning(
                        $"The input file contains vertices that are associated to more than {curVertexWeights.Count} bones. In current version of the system, a single vertex can only be associated to {nbBoneByVertex} bones. Extra bones will be ignored",
                        new ArgumentOutOfRangeException("To much bones influencing a single vertex"));
                }

                // resize the weights so that they contains exactly the number of bone weights required
                while (curVertexWeights.Count < nbBoneByVertex)
                {
                    curVertexWeights.Add((0, 0));
                }

                var totalWeight = 0.0f;
                for (var boneId = 0; boneId < nbBoneByVertex; ++boneId)
                    totalWeight += curVertexWeights[boneId].Item2;

                if (totalWeight <= float.Epsilon) // Assimp weights are positive, so in this case all weights are nulls
                    continue;

                for (var boneId = 0; boneId < nbBoneByVertex; ++boneId)
                    curVertexWeights[boneId] = (curVertexWeights[boneId].Item1, curVertexWeights[boneId].Item2 / totalWeight);
            }
        }


        private unsafe void ExtractEmbededTexture(Scene* scene, string importFieName)
        {
            string dir = Path.GetDirectoryName(importFieName);
            for (uint i = 0; i < scene->MNumTextures; ++i)
            {
                var texture = scene->MTextures[i];
                if (!Material.Materials.TryGetTextureFileName(texture, out var texFileName, out var errorMessage))
                {
                    Logger.Error(errorMessage);
                    continue;
                }
                string fullName = Path.Combine(dir, texFileName);
                CreateTextureFile(texture, fullName);
            }
        }

        private unsafe void CreateTextureFile(Silk.NET.Assimp.Texture* texture, string path)
        {
            var texel = texture->PcData;
            var arraySize = texture->MWidth;
            byte[] buffer = new byte[texture->MWidth];
            fixed (byte* bufferPointer = buffer)
            {
                var sourcePointer = (byte*)texture->PcData;
                MemoryUtilities.CopyWithAlignmentFallback(bufferPointer, sourcePointer, arraySize);
            }
            System.IO.File.WriteAllBytes(path, buffer);
        }

        private unsafe Dictionary<string, MaterialAsset> ExtractMaterials(Scene* scene, Dictionary<IntPtr, string> materialNames)
        {
            GenerateMaterialNames(scene, materialNames);

            var materials = new Dictionary<string, MaterialAsset>();
            for (uint i = 0; i < scene->MNumMaterials; i++)
            {
                var lMaterial = scene->MMaterials[i];
                // Replace slashes with an underscore to indicate it is not absolute to Strides asset system.
                var materialName = materialNames[(IntPtr)lMaterial].Replace('/', '_');

                materials.Add(materialName, ProcessMeshMaterial(scene, lMaterial));
            }
            return materials;
        }

        private unsafe void GenerateMaterialNames(Scene* scene, Dictionary<IntPtr, string> materialNames)
        {
            var baseNames = new List<string>();
            for (uint i = 0; i < scene->MNumMaterials; i++)
            {
                var lMaterial = scene->MMaterials[i];

                var aiMaterial = new AssimpString();
                var materialName = assimp.GetMaterialString(lMaterial, Assimp.MaterialNameBase, 0, 0, ref aiMaterial) == Return.Success ? aiMaterial.AsString : "Material";
                baseNames.Add(materialName);
            }

            GenerateUniqueNames(materialNames, baseNames, i => (IntPtr)scene->MMaterials[i]);
        }

        private unsafe MaterialAsset ProcessMeshMaterial(Scene* scene, Silk.NET.Assimp.Material* pMaterial)
        {
            var finalMaterial = new MaterialAsset();

            float specPower = 0;
            float opacity = 0;

            bool hasDiffColor = false;
            bool hasSpecColor = false;
            bool hasAmbientColor = false;
            bool hasEmissiveColor = false;
            bool hasReflectiveColor = false;
            bool hasSpecPower = false;
            bool hasOpacity = false;

            var diffColor = System.Numerics.Vector4.Zero;
            var specColor = System.Numerics.Vector4.Zero;
            var ambientColor = System.Numerics.Vector4.Zero;
            var emissiveColor = System.Numerics.Vector4.Zero;
            var reflectiveColor = System.Numerics.Vector4.Zero;
            var dummyColor = System.Numerics.Vector4.Zero;

            SetMaterialColorFlag(pMaterial, Assimp.MaterialColorDiffuseBase, ref hasDiffColor, ref diffColor, true);// always keep black color for diffuse
            SetMaterialColorFlag(pMaterial, Assimp.MaterialColorSpecularBase, ref hasSpecColor, ref specColor, IsNotBlackColor(specColor));
            SetMaterialColorFlag(pMaterial, Assimp.MaterialColorAmbientBase, ref hasAmbientColor, ref ambientColor, IsNotBlackColor(specColor));
            SetMaterialColorFlag(pMaterial, Assimp.MaterialColorEmissiveBase, ref hasEmissiveColor, ref emissiveColor, IsNotBlackColor(emissiveColor));
            SetMaterialColorFlag(pMaterial, Assimp.MaterialColorReflectiveBase, ref hasReflectiveColor, ref reflectiveColor, IsNotBlackColor(reflectiveColor));
            SetMaterialFloatArrayFlag(pMaterial, Assimp.MaterialShininessBase, ref hasSpecPower, specPower, specPower > 0);
            SetMaterialFloatArrayFlag(pMaterial, Assimp.MaterialOpacityBase, ref hasOpacity, opacity, opacity < 1.0);
            if (hasDiffColor == false)
                SetMaterialColorFlag(pMaterial, Assimp.MatkeyBaseColor, ref hasDiffColor, ref diffColor, true);

            BuildLayeredSurface(scene, pMaterial, hasDiffColor, false, diffColor.ToStrideColor(), 0.0f, TextureType.Diffuse, finalMaterial);
            BuildLayeredSurface(scene, pMaterial, hasSpecColor, false, specColor.ToStrideColor(), 0.0f, TextureType.Specular, finalMaterial);
            BuildLayeredSurface(scene, pMaterial, false, false, dummyColor.ToStrideColor(), 0.0f, TextureType.Normals, finalMaterial);
            BuildLayeredSurface(scene, pMaterial, false, false, dummyColor.ToStrideColor(), 0.0f, TextureType.Displacement, finalMaterial);
            BuildLayeredSurface(scene, pMaterial, hasAmbientColor, false, ambientColor.ToStrideColor(), 0.0f, TextureType.Ambient, finalMaterial);
            BuildLayeredSurface(scene, pMaterial, false, hasOpacity, dummyColor.ToStrideColor(), opacity, TextureType.Opacity, finalMaterial);
            BuildLayeredSurface(scene, pMaterial, false, hasSpecPower, dummyColor.ToStrideColor(), specPower, TextureType.Shininess, finalMaterial);
            BuildLayeredSurface(scene, pMaterial, hasEmissiveColor, false, emissiveColor.ToStrideColor(), 0.0f, TextureType.Emissive, finalMaterial);
            BuildLayeredSurface(scene, pMaterial, false, false, dummyColor.ToStrideColor(), 0.0f, TextureType.Height, finalMaterial);
            BuildLayeredSurface(scene, pMaterial, hasReflectiveColor, false, reflectiveColor.ToStrideColor(), 0.0f, TextureType.Reflection, finalMaterial);

            return finalMaterial;
        }

        private unsafe void SetMaterialColorFlag(Silk.NET.Assimp.Material* pMaterial, string materialColorBase, ref bool hasMatColor, ref System.Numerics.Vector4 matColor, bool condition)
        {
            if (assimp.GetMaterialColor(pMaterial, materialColorBase, 0, 0, ref matColor) == Return.Success && condition)
            {
                hasMatColor = true;
            }
        }
        private unsafe void SetMaterialFloatArrayFlag(Silk.NET.Assimp.Material* pMaterial, string materialBase, ref bool hasMatProperty, float matColor, bool condition)
        {
            if (assimp.GetMaterialFloatArray(pMaterial, materialBase, 0, 0, &matColor, (uint*)0x0) == Return.Success && condition)
            {
                hasMatProperty = true;
            }
        }

        private bool IsNotBlackColor(System.Numerics.Vector4 diffColor)
        {
            return diffColor != System.Numerics.Vector4.Zero;
        }

        private unsafe void BuildLayeredSurface(Scene* scene, Silk.NET.Assimp.Material* pMat, bool hasBaseColor, bool hasBaseValue, Color4 baseColor, float baseValue, TextureType textureType, MaterialAsset finalMaterial)
        {
            var nbTextures = assimp.GetMaterialTextureCount(pMat, textureType);

            IComputeColor computeColorNode = null;
            int textureCount = 0;
            if (nbTextures == 0)
            {
                if (hasBaseColor)
                {
                    computeColorNode = new ComputeColor(baseColor);
                }
                //else if (hasBaseValue)
                //{
                //	computeColorNode = gcnew MaterialFloatComputeNode(baseValue);
                //}
            }
            else
            {
                computeColorNode = GenerateOneTextureTypeLayers(scene, pMat, textureType, textureCount, finalMaterial);
            }

            if (computeColorNode == null)
            {
                return;
            }

            if (textureType == TextureType.Diffuse)
            {
                if (assimp.GetMaterialTextureCount(pMat, TextureType.Lightmap) > 0)
                {
                    var lightMap = GenerateOneTextureTypeLayers(scene, pMat, TextureType.Lightmap, textureCount, finalMaterial);
                    if (lightMap != null)
                        computeColorNode = new ComputeBinaryColor(computeColorNode, lightMap, BinaryOperator.Add);
                }

                finalMaterial.Attributes.Diffuse = new MaterialDiffuseMapFeature(computeColorNode);

                // TODO TEMP: Set a default diffuse model
                finalMaterial.Attributes.DiffuseModel = new MaterialDiffuseLambertModelFeature();
            }
            else if (textureType == TextureType.Specular)
            {
                var specularFeature = new MaterialSpecularMapFeature
                {
                    SpecularMap = computeColorNode
                };
                finalMaterial.Attributes.Specular = specularFeature;

                // TODO TEMP: Set a default specular model
                var specularModel = new MaterialSpecularMicrofacetModelFeature
                {
                    Fresnel = new MaterialSpecularMicrofacetFresnelSchlick(),
                    Visibility = new MaterialSpecularMicrofacetVisibilityImplicit(),
                    NormalDistribution = new MaterialSpecularMicrofacetNormalDistributionBlinnPhong()
                };
                finalMaterial.Attributes.SpecularModel = specularModel;
            }
            else if (textureType == TextureType.Emissive)
            {
                // TODO: Add support
            }
            else if (textureType == TextureType.Ambient)
            {
                // TODO: Add support
            }
            else if (textureType == TextureType.Reflection)
            {
                // TODO: Add support
            }
            if (textureType == TextureType.Opacity)
            {
                // TODO: Add support
            }
            else if (textureType == TextureType.Shininess)
            {
                // TODO: Add support
            }
            if (textureType == TextureType.Specular)
            {
                // TODO: Add support
            }
            else if (textureType == TextureType.Normals)
            {
                finalMaterial.Attributes.Surface = new MaterialNormalMapFeature(computeColorNode);
            }
            else if (textureType == TextureType.Displacement)
            {
                // TODO: Add support
            }
            else if (textureType == TextureType.Height)
            {
                // TODO: Add support
            }
        }

        private unsafe IComputeColor GenerateOneTextureTypeLayers(Scene* scene, Silk.NET.Assimp.Material* pMat, TextureType textureType, int textureCount, MaterialAsset finalMaterial)
        {
            var stack = Material.Materials.ConvertAssimpStackCppToCs(assimp, scene, pMat, textureType, Logger);

            var compositionFathers = new Stack<IComputeColor>();

            var sets = new Stack<int>();
            sets.Push(0);

            var nbTextures = assimp.GetMaterialTextureCount(pMat, textureType);

            IComputeColor curComposition = null, newCompositionFather = null;

            var isRootElement = true;
            IComputeColor rootMaterial = null;

            while (!stack.IsEmpty)
            {
                var top = stack.Pop();

                IComputeColor curCompositionFather = null;
                if (!isRootElement)
                {
                    if (compositionFathers.Count == 0)
                        Logger.Error("Texture Stack Invalid : Operand without Operation.");

                    curCompositionFather = compositionFathers.Pop();
                }

                var type = top.Type;
                var strength = top.Blend;
                var alpha = top.Alpha;
                int set = sets.Peek();

                if (type == Material.StackElementType.Operation)
                {
                    set = sets.Pop();
                    var realTop = (Material.StackOperation)top;
                    var op = realTop.Operation;
                    var binNode = new ComputeBinaryColor(null, null, BinaryOperator.Add);

                    binNode.Operator = op switch
                    {
                        Material.Operation.Add3ds or Material.Operation.AddMaya => BinaryOperator.Add,
                        Material.Operation.Multiply3ds or Material.Operation.MultiplyMaya => BinaryOperator.Multiply,
                        _ => BinaryOperator.Add,
                    };
                    curComposition = binNode;
                }
                else if (type == Material.StackElementType.Color)
                {
                    var realTop = (Material.StackColor)top;
                    var ol = realTop.Color;
                    curComposition = new ComputeColor(new Color4(ol.R, ol.G, ol.B, alpha));
                }
                else if (type == Material.StackElementType.Texture)
                {
                    var realTop = (Material.StackTexture)top;
                    var texPath = realTop.TexturePath;
                    var indexUV = realTop.Channel;
                    curComposition = GetTextureReferenceNode(vfsOutputFilename, texPath, (uint)indexUV, Vector2.One, ConvertTextureMode(realTop.MappingModeU), ConvertTextureMode(realTop.MappingModeV), finalMaterial);
                }

                newCompositionFather = curComposition;

                if (strength != 1.0f)
                {
                    var strengthAlpha = strength;
                    if (type != Material.StackElementType.Color)
                        strengthAlpha *= alpha;

                    var factorComposition = new ComputeFloat4(new Vector4(strength, strength, strength, strengthAlpha));
                    curComposition = new ComputeBinaryColor(curComposition, factorComposition, BinaryOperator.Multiply);
                }
                else if (alpha != 1.0f && type != Material.StackElementType.Color)
                {
                    var factorComposition = new ComputeFloat4(new Vector4(1.0f, 1.0f, 1.0f, alpha));
                    curComposition = new ComputeBinaryColor(curComposition, factorComposition, BinaryOperator.Multiply);
                }

                if (isRootElement)
                {
                    rootMaterial = curComposition;
                    isRootElement = false;
                    compositionFathers.Push(curCompositionFather);
                }
                else
                {
                    if (set == 0)
                    {
                        ((ComputeBinaryColor)curCompositionFather).LeftChild = curComposition;
                        compositionFathers.Push(curCompositionFather);
                        sets.Push(1);
                    }
                    else if (set == 1)
                    {
                        ((ComputeBinaryColor)curCompositionFather).RightChild = curComposition;
                    }
                    else
                    {
                        Logger.Error($"Texture Stack Invalid : Invalid Operand Number {set}.");
                    }
                }

                if (type == Material.StackElementType.Operation)
                {
                    compositionFathers.Push(newCompositionFather);
                    sets.Push(0);
                }
            }

            return rootMaterial;
        }

        private static TextureAddressMode ConvertTextureMode(Material.MappingMode mappingMode)
        {
            return mappingMode switch
            {
                Material.MappingMode.Clamp => TextureAddressMode.Clamp,
                Material.MappingMode.Decal => TextureAddressMode.Border,
                Material.MappingMode.Mirror => TextureAddressMode.Mirror,
                _ => TextureAddressMode.Wrap,
            };
        }

        private ComputeTextureColor GetTextureReferenceNode(string vfsOutputPath, string sourceTextureFile, uint textureUVSetIndex, Vector2 textureUVscaling, TextureAddressMode addressModeU, TextureAddressMode addressModeV, MaterialAsset finalMaterial)
        {
            // TODO: compare with FBX importer - see if there could be some conflict between texture names
            var textureValue = TextureLayerGenerator.GenerateMaterialTextureNode(vfsOutputPath, sourceTextureFile, textureUVSetIndex, textureUVscaling, addressModeU, addressModeV, Logger);

            var attachedReference = AttachedReferenceManager.GetAttachedReference(textureValue.Texture);
            var referenceName = attachedReference.Url;

            // find a new and correctName
            if (!textureNameCount.TryAdd(referenceName, 1))
            {
                int count = textureNameCount[referenceName];
                textureNameCount[referenceName] = count + 1;
                referenceName = string.Concat(referenceName, "_", count);
            }

            return textureValue;
        }

        private unsafe List<MeshParameters> ExtractModels(Scene* scene, Dictionary<IntPtr, string> meshNames, Dictionary<IntPtr, string> materialNames, Dictionary<IntPtr, string> nodeNames)
        {
            GenerateMeshNames(scene, meshNames);
            var meshList = new List<MeshParameters>();
            for (uint i = 0; i < scene->MNumMeshes; ++i)
            {
                var mesh = scene->MMeshes[i];
                var lMaterial = scene->MMaterials[mesh->MMaterialIndex];

                var meshParams = new MeshParameters
                {
                    MeshName = meshNames[(IntPtr)mesh],
                    MaterialName = materialNames[(IntPtr)lMaterial],
                    NodeName = SearchMeshNode(scene->MRootNode, i, nodeNames)
                };

                meshList.Add(meshParams);
            }

            return meshList;
        }

        private unsafe string SearchMeshNode(Node* node, uint meshIndex, Dictionary<IntPtr, string> nodeNames)
        {
            for (uint i = 0; i < node->MNumMeshes; ++i)
            {
                if (node->MMeshes[i] == meshIndex)
                    return nodeNames[(IntPtr)node];
            }

            for (uint i = 0; i < node->MNumChildren; ++i)
            {
                var res = SearchMeshNode(node->MChildren[i], meshIndex, nodeNames);
                if (res != null)
                    return res;
            }

            return null;
        }

        private unsafe List<NodeInfo> ExtractNodeHierarchy(Scene* scene, Dictionary<IntPtr, string> nodeNames)
        {
            var allNodes = new List<NodeInfo>();
            GetNodes(scene->MRootNode, 0, nodeNames, allNodes);
            return allNodes;
        }

        private unsafe void GetNodes(Node* node, int depth, Dictionary<IntPtr, string> nodeNames, List<NodeInfo> allNodes)
        {
            var newNodeInfo = new NodeInfo
            {
                Name = nodeNames[(IntPtr)node],
                Depth = depth,
                Preserve = true
            };

            allNodes.Add(newNodeInfo);
            for (uint i = 0; i < node->MNumChildren; ++i)
                GetNodes(node->MChildren[i], depth + 1, nodeNames, allNodes);
        }

        private unsafe List<string> ExtractAnimations(Scene* scene, Dictionary<IntPtr, string> animationNames)
        {
            if (scene->MNumAnimations == 0)
                return new();

            GenerateAnimationNames(scene, animationNames);

            var animationList = new List<string>();
            foreach (var animationName in animationNames)
            {
                animationList.Add(animationName.Value);
            }

            return animationList;
        }

        private unsafe List<string> ExtractTextureDependencies(Scene* scene)
        {
            var textureNames = new List<string>();

            // texture search is done by type so we need to loop on them
            var allTextureTypes = new TextureType[]
            {
                TextureType.Diffuse,
                TextureType.Specular,
                TextureType.Ambient,
                TextureType.Emissive,
                TextureType.Height,
                TextureType.Normals,
                TextureType.Shininess,
                TextureType.Opacity,
                TextureType.Displacement,
                TextureType.Lightmap,
                TextureType.Reflection
            };

            for (uint i = 0; i < scene->MNumMaterials; i++)
            {
                foreach (var textureType in allTextureTypes)
                {
                    var lMaterial = scene->MMaterials[i];
                    var nbTextures = assimp.GetMaterialTextureCount(lMaterial, textureType);

                    for (uint j = 0; j < nbTextures; ++j)
                    {
                        var path = new AssimpString();
                        var mapping = TextureMapping.UV;
                        uint uvIndex = 0;
                        var blend = 0.0f;
                        var textureOp = TextureOp.Multiply;
                        var mapMode = TextureMapMode.Wrap;
                        uint flags = 0;

                        if (assimp.GetMaterialTexture(lMaterial, textureType, j, ref path, ref mapping, ref uvIndex, ref blend, ref textureOp, ref mapMode, ref flags) == Return.Success)
                        {
                            if (!Material.Materials.TryGetTextureFileName(path.AsString, scene, out var texFileName, out var errorMessage))
                            {
                                Logger.Error(errorMessage);
                                break;
                            }
                            var relFileName = texFileName;
                            var fileNameToUse = Path.Combine(vfsInputPath, relFileName);
                            textureNames.Add(fileNameToUse);
                            break;
                        }
                    }
                }
            }

            return textureNames;
        }
    }

    public class MeshInfo
    {
        public MeshDraw Draw;
        public List<MeshBoneDefinition> Bones;
        public string Name;
        public int MaterialIndex;
        public bool HasSkinningPosition = false;
        public bool HasSkinningNormal = false;
        public int TotalClusterCount = 0;
    }

    public class MaterialInstantiation
    {
        public List<string> Parameters;
        public MaterialAsset Material;
        public string MaterialName;
    }

    public unsafe class MaterialInstances
    {
        public Silk.NET.Assimp.Material* SourceMaterial;
        public List<MaterialInstantiation> Instances = new();
        public string MaterialsName;
    }

}


