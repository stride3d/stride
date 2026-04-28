
cbuffer cb : register(b0)
{
    float4x4 WorldViewProj;
};

struct VS_INPUT
{
    float4 Pos : POSITION;
    float4 Color : COLOR;
};

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float4 Color : COLOR;
};

PS_INPUT VSMain(VS_INPUT input)
{
    PS_INPUT output = (PS_INPUT)0;
    output.Pos = mul(input.Pos, WorldViewProj);
    output.Color = input.Color;
    return output;
}

float4 main(PS_INPUT input) : SV_Target
{
    return input.Color;
}
