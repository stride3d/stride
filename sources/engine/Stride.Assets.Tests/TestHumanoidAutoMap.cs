// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Assets.Models;
using Stride.Assets.Models.Humanoid;
using Xunit;

namespace Stride.Assets.Tests;

public class TestHumanoidAutoMap
{
    // Node lists, one node per line, indented by two spaces per level

    private const string Mixamo = """
        RootNode
          Hips
            Spine
              Spine1
                Spine2
                  Neck
                    Head
                      HeadTop_End
                      LeftEye
                      RightEye
                  LeftShoulder
                    LeftArm
                      LeftForeArm
                        LeftHand
                          LeftHandThumb1
                            LeftHandThumb2
                              LeftHandThumb3
                          LeftHandIndex1
                            LeftHandIndex2
                              LeftHandIndex3
                  RightShoulder
                    RightArm
                      RightForeArm
                        RightHand
                          RightHandIndex1
            LeftUpLeg
              LeftLeg
                LeftFoot
                  LeftToeBase
                    LeftToe_End
            RightUpLeg
              RightLeg
                RightFoot
                  RightToeBase
        """;

    // Names as 3ds Max writes them; the importer removes the spaces, which the mapper also reads (see SingleNames)
    private const string Biped = """
        Bip01
          Bip01 Pelvis
            Bip01 Spine
              Bip01 Spine1
                Bip01 Neck
                  Bip01 Head
                  Bip01 L Clavicle
                    Bip01 L UpperArm
                      Bip01 L Forearm
                        Bip01 L Hand
                          Bip01 L Finger0
                            Bip01 L Finger01
                  Bip01 R Clavicle
                    Bip01 R UpperArm
                      Bip01 R Forearm
                        Bip01 R Hand
            Bip01 L Thigh
              Bip01 L Calf
                Bip01 L Foot
                  Bip01 L Toe0
            Bip01 R Thigh
              Bip01 R Calf
                Bip01 R Foot
                  Bip01 R Toe0
        """;

    private const string Unreal = """
        root
          pelvis
            spine_01
              spine_02
                spine_03
                  clavicle_l
                    upperarm_l
                      lowerarm_l
                        hand_l
                          thumb_01_l
                          index_01_l
                            index_02_l
                      upperarm_twist_01_l
                  clavicle_r
                    upperarm_r
                      lowerarm_r
                        hand_r
                  neck_01
                    head
            thigh_l
              calf_l
                foot_l
                  ball_l
            thigh_r
              calf_r
                foot_r
                  ball_r
          ik_hand_root
            ik_hand_l
        """;

    private const string SmplX = """
        pelvis
          left_hip
            left_knee
              left_ankle
                left_foot
          right_hip
            right_knee
              right_ankle
                right_foot
          spine1
            spine2
              spine3
                neck
                  head
                    jaw
                    left_eye_smplhf
                    right_eye_smplhf
                left_collar
                  left_shoulder
                    left_elbow
                      left_wrist
                        left_index1
                          left_index2
                        left_thumb1
                right_collar
                  right_shoulder
                    right_elbow
                      right_wrist
        """;

    private const string Vrm = """
        Root
          hips
            spine
              chest
                upperChest
                  neck
                    head
                  leftShoulder
                    leftUpperArm
                      leftLowerArm
                        leftHand
                          leftIndexProximal
                  rightShoulder
                    rightUpperArm
                      rightLowerArm
                        rightHand
            leftUpperLeg
              leftLowerLeg
                leftFoot
                  leftToes
            rightUpperLeg
              rightLowerLeg
                rightFoot
                  rightToes
        """;

    // mannequinModel Skeleton.sdskel of the mannequin pack, as recorded
    private const string Mannequin = """
        RootNode
          mannequinModel
            upper
            lower
            arm
            hand
          CATRigHub001
            CATRigLLeg1
              CATRigLLeg2
                CATRigLLegAnkle
                  CATRigAnkleleft
                  left_additional
                    left_extra_bone
                CATRig1Boneside01
                CATRig1Boneside324
              CATRig1Boneupleft
                CATRig1Bonelowleft
            backplateleftup
              backplatelieftdown
            CATRigSpine1
              CATRigSpine2
                CATRigSpine3
                  CATRigHub002
                    CATRigLArmCollarbone
                      CATRigLArm1
                        CATRigLArm2
                          CATRigLArmPalm
                            CATRigLArmDigit11
                              CATRigLArmDigit12
                                CATRigLArmDigit13
                            CATRigLArmDigit21
                              CATRigLArmDigit22
                                CATRigLArmDigit23
                            CATRigLArmDigit31
                              CATRigLArmDigit32
                                CATRigLArmDigit33
                            CATRigLArmDigit41
                              CATRigLArmDigit42
                                CATRigLArmDigit43
                            CATRigLArmDigit51
                              CATRigLArmDigit52
                                CATRigLArmDigit53
                            wepon_bone_L
                    CATRigHub002Bone001
                    CATRigHub002Bone002
                    CATRigHub002Bone003
                    CATRigSpin4435f
                      CATRigSpine3294
                        CATRigSpineneck043
                          CATRigRibcage0rtee
                    CATRigHub002Bone004
                    CATRigRArmCollarbone
                      CATRigRArm1
                        CATRigRArm2
                          CATRigRArmPalm
                            CATRigRArmDigit11
                              CATRigRArmDigit12
                                CATRigRArmDigit13
                            CATRigRArmDigit21
                              CATRigRArmDigit22
                                CATRigRArmDigit23
                            CATRigRArmDigit31
                              CATRigRArmDigit32
                                CATRigRArmDigit33
                            CATRigRArmDigit41
                              CATRigRArmDigit42
                                CATRigRArmDigit43
                            CATRigRArmDigit51
                              CATRigRArmDigit52
                                CATRigRArmDigit53
                            wepon_bone_R
                    gun_bone
                      gun_barrel
                      gun_bone_magazine_offset
                        gun_bone_magazine
            CATRigHub001Bone002
            backplaterightup
              back platerightdown
            CATRigRLeg1
              CATRigRLeg2
                CATRigRLegAnkle
                  CATRigAnkleright
                  right_additional
                    right_extra_bone
                CATRig1Boeer643
                CATRig1Bone43840
              CATRig1Bonesiderightup
                CATRig1Bonerightlow
          CATRigLLegPlatform
          CATRigRLegPlatform
          CATRigRArmIKTarget
          CATRigLArmIKTarget
        """;

    public static TheoryData<string, string, string> Presets => new()
    {
        { "Mixamo", Mixamo, "LeftForeArm" },
        { "Biped", Biped, "Bip01 L Forearm" },
        { "Unreal", Unreal, "lowerarm_l" },
        { "SMPL", SmplX, "left_elbow" },
        { "VRM", Vrm, "leftLowerArm" },
    };

    [Theory]
    [MemberData(nameof(Presets))]
    public void PresetMapsAllRequiredBones(string preset, string nodeList, string leftLowerArm)
    {
        var mapping = HumanoidAutoMapper.Map(ParseNodes(nodeList), out var matched);

        Assert.Equal(preset, matched);
        Assert.Empty(HumanoidBones.GetMissingRequired(ToMappings(mapping)));
        Assert.Equal(leftLowerArm, mapping[HumanoidBone.LeftLowerArm]);
    }

    [Fact]
    public void PresetsMapSpineAndFingers()
    {
        var mixamo = HumanoidAutoMapper.Map(ParseNodes(Mixamo), out _);
        Assert.Equal("Spine1", mixamo[HumanoidBone.Chest]);
        Assert.Equal("Spine2", mixamo[HumanoidBone.UpperChest]);
        Assert.Equal("LeftToeBase", mixamo[HumanoidBone.LeftToes]);
        Assert.Equal("LeftHandThumb1", mixamo[HumanoidBone.LeftThumbMetacarpal]);
        Assert.Equal("LeftHandIndex3", mixamo[HumanoidBone.LeftIndexDistal]);
        Assert.Equal("LeftEye", mixamo[HumanoidBone.LeftEye]);
        Assert.DoesNotContain("HeadTop_End", mixamo.Values);

        var unreal = HumanoidAutoMapper.Map(ParseNodes(Unreal), out _);
        Assert.Equal("upperarm_l", unreal[HumanoidBone.LeftUpperArm]);
        Assert.Equal("ball_r", unreal[HumanoidBone.RightToes]);
        Assert.Equal("index_02_l", unreal[HumanoidBone.LeftIndexIntermediate]);
        Assert.DoesNotContain("upperarm_twist_01_l", unreal.Values);
        Assert.DoesNotContain("ik_hand_l", unreal.Values);

        var smpl = HumanoidAutoMapper.Map(ParseNodes(SmplX), out _);
        Assert.Equal("left_shoulder", smpl[HumanoidBone.LeftUpperArm]);
        Assert.Equal("left_foot", smpl[HumanoidBone.LeftToes]);
        Assert.Equal("jaw", smpl[HumanoidBone.Jaw]);
        Assert.Equal("right_eye_smplhf", smpl[HumanoidBone.RightEye]);
    }

    [Theory]
    [InlineData("mixamorig:LeftForeArm")]
    [InlineData("mixamorig1_LeftForeArm")]
    [InlineData("CATRigLArm2")]
    [InlineData("Bip01 L Forearm")]
    [InlineData("Bip01LForearm")]
    [InlineData("lowerarm_l")]
    [InlineData("left_elbow")]
    [InlineData("leftLowerArm")]
    public void SingleNames(string name)
    {
        var mapping = HumanoidAutoMapper.Map([new NodeInformation(name, 0, true)], out _);

        Assert.Equal(name, Assert.Single(mapping).Value);
        Assert.Equal(HumanoidBone.LeftLowerArm, mapping.Keys.Single());
    }

    [Fact]
    public void MannequinMapsAllButNeckAndHead()
    {
        var mapping = HumanoidAutoMapper.Map(ParseNodes(Mannequin), out var preset);

        Assert.Equal("CAT", preset);
        Assert.Equal([HumanoidBone.Head], HumanoidBones.GetMissingRequired(ToMappings(mapping)));
        Assert.DoesNotContain(HumanoidBone.Neck, mapping.Keys);
        Assert.Equal("CATRigHub001", mapping[HumanoidBone.Hips]);
        Assert.Equal("CATRigSpine3", mapping[HumanoidBone.UpperChest]);
        Assert.Equal("CATRigLArmCollarbone", mapping[HumanoidBone.LeftShoulder]);
        Assert.Equal("CATRigRArmPalm", mapping[HumanoidBone.RightHand]);
        Assert.Equal("CATRigLLegAnkle", mapping[HumanoidBone.LeftFoot]);
        Assert.Equal("CATRigLArmDigit11", mapping[HumanoidBone.LeftThumbMetacarpal]);
        Assert.Equal("CATRigRArmDigit53", mapping[HumanoidBone.RightLittleDistal]);
        Assert.DoesNotContain(mapping.Values, x => x.Contains("Platform") || x.Contains("IKTarget") || x.Contains("Bone"));
    }

    [Fact]
    public void BoneOutsideItsParentIsNotMapped()
    {
        // The left forearm hangs under the right arm
        var nodes = ParseNodes("""
            Hips
              LeftArm
              RightArm
                LeftForeArm
            """);

        var mapping = HumanoidAutoMapper.Map(nodes, out _);

        Assert.Equal("LeftArm", mapping[HumanoidBone.LeftUpperArm]);
        Assert.DoesNotContain(HumanoidBone.LeftLowerArm, mapping.Keys);
    }

    [Fact]
    public void HandSetEntriesAreKept()
    {
        var skeleton = new SkeletonAsset();
        skeleton.Nodes.AddRange(ParseNodes(Mannequin).Select(x => new NodeInformation(x.Name, x.Depth, false)));
        skeleton.HumanoidBones.Add(new HumanoidBoneMapping { Bone = HumanoidBone.Head, NodeName = "CATRigSpineneck043" });
        skeleton.HumanoidBones.Add(new HumanoidBoneMapping { Bone = HumanoidBone.Hips, NodeName = "CATRigHub002" });

        var added = HumanoidAutoMapper.Apply(skeleton);

        Assert.Equal("CATRigSpineneck043", skeleton.HumanoidBones[0].NodeName);
        Assert.Equal("CATRigHub002", skeleton.HumanoidBones[1].NodeName);
        Assert.DoesNotContain(added, x => x.Bone is HumanoidBone.Head or HumanoidBone.Hips);
        Assert.DoesNotContain(added, x => x.NodeName == "CATRigHub002");
        Assert.Contains(added, x => x.Bone == HumanoidBone.LeftUpperArm && x.NodeName == "CATRigLArm1");
        Assert.Equal(added.Count + 2, skeleton.HumanoidBones.Count);

        // Mapped nodes are preserved, so they stay in the compiled skeleton
        Assert.All(added, x => Assert.True(skeleton.Nodes.Single(n => n.Name == x.NodeName).Preserve));
        Assert.False(skeleton.Nodes.Single(n => n.Name == "gun_bone").Preserve);

        // A second run adds nothing
        Assert.Empty(HumanoidAutoMapper.Apply(skeleton));
    }

    private static List<HumanoidBoneMapping> ToMappings(Dictionary<HumanoidBone, string> mapping)
        => mapping.Select(x => new HumanoidBoneMapping { Bone = x.Key, NodeName = x.Value }).ToList();

    private static List<NodeInformation> ParseNodes(string nodeList)
    {
        return nodeList.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.TrimEnd('\r'))
            .Where(x => x.Trim().Length > 0)
            .Select(x => new NodeInformation(x.Trim(), (x.Length - x.TrimStart().Length) / 2, true))
            .ToList();
    }
}
