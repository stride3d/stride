// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders.Parsing;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// A composition or a stream declared inside a cbuffer or rgroup (Stride 4.3 moved them out of the block) gets a clear parse error (SDSL0050).
/// </summary>
public class ComposeInBufferTests
{
    [Theory]
    // VL.Addons' TextureControlTriangle_ShaderFX
    [InlineData("shader S { rgroup PerMaterial { stage Texture2D ShrinkTexture; stage compose ComputeFloat ShrinkStrength; } };", "Composition ShrinkStrength", "rgroup PerMaterial")]
    [InlineData("shader S { cbuffer PerDraw { float Scale; compose ComputeColor Color; } };", "Composition Color", "cbuffer PerDraw")]
    [InlineData("shader S { cbuffer PerDraw { float Scale; [Link(\"X\")] compose ComputeColor Color; } };", "Composition Color", "cbuffer PerDraw")]
    // VL.Addons' Sprite_ShaderFX
    [InlineData("shader S { cbuffer PerDraw { stage float2 GridSize; stage stream float2 ObjectUV; } };", "Stream ObjectUV", "cbuffer PerDraw")]
    public void MemberThatIsNotABufferMemberSaysSo(string code, string member, string block)
    {
        var error = SDSLParser.Parse(code).Errors[0];
        var blockKind = block.Split(' ')[0];
        Assert.True(error.Message.StartsWith($"SDSL0050: {member} is declared inside {block}, but it is not a member of a {blockKind}. Move it out of the block"), error.ToString());
    }

    [Fact]
    public void OtherWrongMembersKeepTheGeneralError()
    {
        var error = SDSLParser.Parse("shader S { cbuffer PerDraw { float Scale; 42; } };").Errors[0];
        Assert.StartsWith("SDSL0001", error.Message);
    }

    [Fact]
    public void OutsideTheBlockIsFine()
    {
        var result = SDSLParser.Parse("shader S { rgroup PerMaterial { stage Texture2D ShrinkTexture; } stage compose ComputeFloat ShrinkStrength; stage stream float2 ObjectUV; };");
        Assert.True(result.Errors.Count == 0, string.Join("\n", result.Errors.Select(x => x.ToString())));
    }
}
