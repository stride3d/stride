using System.Reflection;

namespace Stride.Core.Shaders.Properties
{
	public class Resources
	{
        private static global::System.Resources.ResourceManager resourceMan;
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        private static global::System.Resources.ResourceManager ResourceManager 
		{
            get 
			{
                if (object.ReferenceEquals(resourceMan, null)) 
				{
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Stride.Core.Shaders.Properties.Resources", typeof(Resources).GetTypeInfo().Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
		
		public static System.String HlslDeclarations
		{
			get {
				 return "// ------------------------------------------------------------------------------" +
    "---------\r\n// Shader Model 4.0 / 4.1\r\n// ---------------------------------------" +
    "------------------------------------------------\r\n\r\n// CalculateLevelOfDetail (D" +
    "irectX HLSL Texture Object) \r\n// http://msdn.microsoft.com/en-us/library/windows" +
    "/desktop/bb944001%28v=vs.85%29.aspx\r\n\r\n// Gather (DirectX HLSL Texture Object) \r" +
    "\n// http://msdn.microsoft.com/en-us/library/windows/desktop/bb944003%28v=VS.85%2" +
    "9.aspx\r\n\r\n// GetDimensions (DirectX HLSL Texture Object)\r\n// http://msdn.microso" +
    "ft.com/en-us/library/bb509693%28v=VS.85%29.aspx\r\n\r\n// GetSamplePosition (DirectX" +
    " HLSL Texture Object)\r\n// http://msdn.microsoft.com/en-us/library/bb944004%28v=V" +
    "S.85%29.aspx\r\n\r\n// Load (DirectX HLSL Texture Object)\r\n// http://msdn.microsoft." +
    "com/en-us/library/bb509694%28v=VS.85%29.aspx\r\n\r\n// Sample (DirectX HLSL Texture " +
    "Object)\r\n// http://msdn.microsoft.com/en-us/library/bb509695%28v=VS.85%29.aspx\r\n" +
    "\r\n// SampleBias (DirectX HLSL Texture Object)\r\n// http://msdn.microsoft.com/en-u" +
    "s/library/bb944005%28v=VS.85%29.aspx\r\n\r\n// SampleCmp (DirectX HLSL Texture Objec" +
    "t)\r\n// http://msdn.microsoft.com/en-us/library/bb509696%28v=VS.85%29.aspx\r\n\r\n// " +
    "SampleGrad (DirectX HLSL Texture Object)\r\n// http://msdn.microsoft.com/en-us/lib" +
    "rary/bb509698%28v=VS.85%29.aspx\r\n\r\n// SampleLevel (DirectX HLSL Texture Object)\r" +
    "\n// http://msdn.microsoft.com/en-us/library/bb509699%28v=VS.85%29.aspx\r\n\r\nvoid G" +
    "roupMemoryBarrierWithGroupSync();\r\n\r\nclass __Texture1D<T> {\r\n\t// SM 4.0\r\n\tfloat " +
    "CalculateLevelOfDetail( sampler_state s, float1 x);\r\n\tvoid GetDimensions( uint M" +
    "ipLevel, out uint Width, out uint NumberOfLevels);\r\n\tvoid GetDimensions( out uin" +
    "t Width);\r\n\tvoid GetDimensions( uint MipLevel, out float Width, out float Number" +
    "OfLevels);\r\n\tvoid GetDimensions( out float Width);\r\n\tT Load(int2 Location);\r\n\tT " +
    "Load(int2 Location, int Offset);\r\n\tfloat4 Sample(sampler_state S, float Location" +
    ");\r\n\tfloat4 Sample(sampler_state S, float Location, int Offset);\r\n\tfloat4 Sample" +
    "Bias(sampler_state S, float Location, float Bias);\r\n\tfloat4 SampleBias(sampler_s" +
    "tate S, float Location, float Bias, int Offset);\r\n\tfloat SampleCmp(sampler_state" +
    " S, float Location, float CompareValue);\r\n\tfloat SampleCmp(sampler_state S, floa" +
    "t Location, float CompareValue, int Offset);\r\n\tfloat SampleCmpLevelZero(sampler_" +
    "state S, float Location, float CompareValue);\r\n\tfloat SampleCmpLevelZero(sampler" +
    "_state S, float Location, float CompareValue, int Offset);\r\n\tfloat4 SampleGrad(s" +
    "ampler_state S, float Location, float DDX, float DDY);\r\n\tfloat4 SampleGrad(sampl" +
    "er_state S, float Location, float DDX, float DDY, int Offset);\r\n\tfloat4 SampleLe" +
    "vel( sampler_state S, float Location, float LOD);\r\n\tfloat4 SampleLevel( sampler_" +
    "state S, float Location, float LOD, int Offset);\r\n\r\n\t// SM 5.0\r\n\tT mips.operator" +
    "[][](in uint mipSlice,in  uint pos);\t\r\n\t\r\n\tT operator[](in  uint pos);\r\n};\r\n\r\ncl" +
    "ass __Texture1DArray<T> {\r\n\t// SM 4.0\r\n\tfloat CalculateLevelOfDetail( sampler_st" +
    "ate s, float1 x);\r\n\tvoid GetDimensions( uint MipLevel, out uint Width, out uint " +
    "Elements, out uint NumberOfLevels);\r\n\tvoid GetDimensions( out uint Width, out ui" +
    "nt Elements);\r\n\tvoid GetDimensions( uint MipLevel, out float Width, out float El" +
    "ements, out float NumberOfLevels);\r\n\tvoid GetDimensions( out float Width, out fl" +
    "oat Elements);\r\n\tT Load(int3 Location);\r\n\tT Load(int3 Location, int Offset);\r\n\tf" +
    "loat4 Sample(sampler_state S, float2 Location);\r\n\tfloat4 Sample(sampler_state S," +
    " float2 Location, int Offset);\r\n\tfloat4 SampleBias(sampler_state S, float2 Locat" +
    "ion, float Bias);\r\n\tfloat4 SampleBias(sampler_state S, float2 Location, float Bi" +
    "as, int Offset);\r\n\tfloat SampleCmp(sampler_state S, float2 Location, float Compa" +
    "reValue);\r\n\tfloat SampleCmp(sampler_state S, float2 Location, float CompareValue" +
    ", int Offset);\r\n\tfloat SampleCmpLevelZero(sampler_state S, float2 Location, floa" +
    "t CompareValue);\r\n\tfloat SampleCmpLevelZero(sampler_state S, float2 Location, fl" +
    "oat CompareValue, int Offset);\r\n\tfloat4 SampleGrad(sampler_state S, float2 Locat" +
    "ion, float DDX, float DDY);\r\n\tfloat4 SampleGrad(sampler_state S, float2 Location" +
    ", float DDX, float DDY, int Offset);\r\n\tfloat4 SampleLevel( sampler_state S, floa" +
    "t2 Location, float LOD);\r\n\tfloat4 SampleLevel( sampler_state S, float2 Location," +
    " float LOD, int Offset);\r\n\r\n\t// SM 5.0\r\n\tT mips.operator[][](in  uint mipSlice,i" +
    "n  uint2 pos);\t\r\n\r\n    T operator[](in  uint2 pos);\r\n};\r\n\r\nclass __Texture2D<T> " +
    "{\r\n\t// SM 4.0\r\n\tfloat CalculateLevelOfDetail( sampler_state s, float2 x);\r\n\tvect" +
    "or<__T_base,4> Gather( sampler_state S, float2 Location);\r\n\tvector<__T_base,4> G" +
    "ather( sampler_state S, float2 Location, int2 Offset );\r\n\tvoid GetDimensions( ui" +
    "nt MipLevel, out uint Width, out uint Height, out uint NumberOfLevels);\r\n\tvoid G" +
    "etDimensions( out uint Width, out uint Height);\r\n\tvoid GetDimensions( uint MipLe" +
    "vel, out float Width, out float Height, out float NumberOfLevels);\r\n\tvoid GetDim" +
    "ensions( out float Width, out float Height);\r\n\tT Load(int3 Location);\r\n\tT Load(i" +
    "nt3 Location, int2 Offset);\r\n\tfloat4 Sample(sampler_state S, float2 Location);\r\n" +
    "\tfloat4 Sample(sampler_state S, float2 Location, int2 Offset);\r\n\tfloat4 SampleBi" +
    "as(sampler_state S, float2 Location, float Bias);\r\n\tfloat4 SampleBias(sampler_st" +
    "ate S, float2 Location, float Bias, int2 Offset);\r\n\tfloat SampleCmp(sampler_stat" +
    "e S, float2 Location, float CompareValue);\r\n\tfloat SampleCmp(sampler_state S, fl" +
    "oat2 Location, float CompareValue, int2 Offset);\r\n\tfloat SampleCmpLevelZero(samp" +
    "ler_state S, float2 Location, float CompareValue);\r\n\tfloat SampleCmpLevelZero(sa" +
    "mpler_state S, float2 Location, float CompareValue, int2 Offset);\r\n\tfloat4 Sampl" +
    "eGrad(sampler_state S, float2 Location, float2 DDX, float2 DDY);\r\n\tfloat4 Sample" +
    "Grad(sampler_state S, float2 Location, float2 DDX, float2 DDY, int2 Offset);\r\n\tf" +
    "loat4 SampleLevel( sampler_state S, float2 Location, float LOD);\r\n\tfloat4 Sample" +
    "Level( sampler_state S, float2 Location, float LOD, int2 Offset);\r\n\t\r\n\t// SM 5.0" +
    "\r\n\tT Gather(\r\n\t  in  sampler s,\r\n\t  in  float2 location,\r\n\t  in  int2 offset\r\n\t)" +
    ";\t\r\n\t\r\n\tT GatherRed(\r\n\t\tin  sampler s,\r\n\t\tin  float2 location\r\n\t\t);\r\n\r\n\tT Gather" +
    "Green(\r\n\t\tin  sampler s,\r\n\t\tin  float2 location\r\n\t\t);\r\n\r\n\tT GatherBlue(\r\n\t\tin  s" +
    "ampler s,\r\n\t\tin  float2 location\r\n\t\t);\r\n\r\n\tT GatherRed(\r\n\t  in  sampler s,\r\n\t  i" +
    "n  float2 location,\r\n\t  in  int2 offset\r\n\t);\r\n\r\n\tT GatherGreen(\r\n\t  in  sampler " +
    "s,\r\n\t  in  float2 location,\r\n\t  in  int2 offset\r\n\t);\r\n\t\r\n\tT GatherBlue(\r\n\t  in  " +
    "sampler s,\r\n\t  in  float2 location,\r\n\t  in  int2 offset\r\n\t);\t\r\n\r\n\tT GatherAlpha(" +
    "\r\n\t  in  sampler s,\r\n\t  in  float2 location,\r\n\t  in  int2 offset\r\n\t);\r\n\r\n\tT Gath" +
    "erRed(\r\n\t  in  sampler s,\r\n\t  in  float2 location,\r\n\t  in  int2 offset1,\r\n\t  in " +
    " int2 offset2,\r\n\t  in  int2 offset3,\r\n\t  in  int2 offset4\r\n\t);\r\n\r\n\tT GatherGreen" +
    "(\r\n\t  in  sampler s,\r\n\t  in  float2 location,\r\n\t  in  int2 offset1,\r\n\t  in  int2" +
    " offset2,\r\n\t  in  int2 offset3,\r\n\t  in  int2 offset4\r\n\t);\r\n\t\r\n\tT GatherBlue(\r\n\t " +
    " in  sampler s,\r\n\t  in  float2 location,\r\n\t  in  int2 offset1,\r\n\t  in  int2 offs" +
    "et2,\r\n\t  in  int2 offset3,\r\n\t  in  int2 offset4\r\n\t);\t\r\n\r\n\tT GatherAlpha(\r\n\t  in " +
    " sampler s,\r\n\t  in  float2 location,\r\n\t  in  int2 offset1,\r\n\t  in  int2 offset2," +
    "\r\n\t  in  int2 offset3,\r\n\t  in  int2 offset4\r\n\t);\r\n\r\n\tfloat4 GatherCmp(\r\n\t  in  S" +
    "amplerComparisonState s,\r\n\t  in  float2 location,\r\n\t  in  float compare_value,\r\n" +
    "\t  in  int2 offset\r\n\t);\t\r\n\t\r\n\tfloat4 GatherCmpRed(\r\n\t  in  SamplerComparisonStat" +
    "e s,\r\n\t  in  float2 location,\r\n\t  in  float compare_value,\r\n\t  in  int2 offset\r\n" +
    "\t);\t\r\n\t\r\n\tfloat4 GatherCmpGreen(\r\n\t  in  SamplerComparisonState s,\r\n\t  in  float" +
    "2 location,\r\n\t  in  float compare_value,\r\n\t  in  int2 offset\r\n\t);\t\r\n\t\r\n\tfloat4 G" +
    "atherCmpBlue(\r\n\t  in  SamplerComparisonState s,\r\n\t  in  float2 location,\r\n\t  in " +
    " float compare_value,\r\n\t  in  int2 offset\r\n\t);\r\n\t\r\n\tfloat4 GatherCmpAlpha(\r\n\t  i" +
    "n  SamplerComparisonState s,\r\n\t  in  float2 location,\r\n\t  in  float compare_valu" +
    "e,\r\n\t  in  int2 offset\r\n\t);\r\n\t\r\n\tT mips.operator[][](in uint mipSlice, in  uint2" +
    " pos);\r\n\t\t\r\n\tT operator[](in  uint2 pos);\r\n};\r\n\r\nclass __Texture2DArray<T> {\r\n\t/" +
    "/ SM 4.0\r\n\tfloat CalculateLevelOfDetail( sampler_state s, float2 x);\r\n\tvector<__" +
    "T_base,4> Gather( sampler_state S, float3 Location, int2 Offset );\r\n\tvoid GetDim" +
    "ensions( uint MipLevel, out uint Width, out uint Height, out uint Elements, out " +
    "uint NumberOfLevels);\r\n\tvoid GetDimensions( out uint Width, out uint Height, out" +
    " uint Elements);\r\n\tvoid GetDimensions( uint MipLevel, out float Width, out float" +
    " Height, out float Elements, out float NumberOfLevels);\r\n\tvoid GetDimensions( ou" +
    "t float Width, out float Height, out float Elements);\r\n\tT Load(int4 Location);\r\n" +
    "\tT Load(int4 Location, int2 Offset);\r\n\tT Load(int4 Location, int3 Offset);\r\n\tflo" +
    "at4 Sample(sampler_state S, float3 Location);\r\n\tfloat4 Sample(sampler_state S, f" +
    "loat3 Location, int2 Offset);\r\n\tfloat4 SampleBias(sampler_state S, float3 Locati" +
    "on, float Bias);\r\n\tfloat4 SampleBias(sampler_state S, float3 Location, float Bia" +
    "s, int2 Offset);\r\n\tfloat SampleCmp(sampler_state S, float3 Location, float Compa" +
    "reValue);\r\n\tfloat SampleCmp(sampler_state S, float3 Location, float CompareValue" +
    ", int2 Offset);\r\n\tfloat SampleCmpLevelZero(sampler_state S, float3 Location, flo" +
    "at CompareValue);\r\n\tfloat SampleCmpLevelZero(sampler_state S, float3 Location, f" +
    "loat CompareValue, int2 Offset);\r\n\tfloat4 SampleGrad(sampler_state S, float3 Loc" +
    "ation, float2 DDX, float2 DDY);\r\n\tfloat4 SampleGrad(sampler_state S, float3 Loca" +
    "tion, float2 DDX, float2 DDY, int2 Offset);\r\n\tfloat4 SampleLevel( sampler_state " +
    "S, float3 Location, float LOD);\r\n\tfloat4 SampleLevel( sampler_state S, float3 Lo" +
    "cation, float LOD, int2 Offset);\r\n\r\n\t\t// SM 5.0\r\n\tT Gather(\r\n\t  in  sampler s,\r\n" +
    "\t  in  float3 location,\r\n\t  in  int2 offset\r\n\t);\t\r\n\t\r\n\tT GatherRed(\r\n\t  in  samp" +
    "ler s,\r\n\t  in  float3 location,\r\n\t  in  int2 offset\r\n\t);\r\n\r\n\tT GatherGreen(\r\n\t  " +
    "in  sampler s,\r\n\t  in  float3 location,\r\n\t  in  int2 offset\r\n\t);\r\n\t\r\n\tT GatherBl" +
    "ue(\r\n\t  in  sampler s,\r\n\t  in  float3 location,\r\n\t  in  int2 offset\r\n\t);\t\r\n\r\n\tT " +
    "GatherAlpha(\r\n\t  in  sampler s,\r\n\t  in  float3 location,\r\n\t  in  int2 offset\r\n\t)" +
    ";\r\n\r\n\tfloat4 GatherCmp(\r\n\t  in  SamplerComparisonState s,\r\n\t  in  float3 locatio" +
    "n,\r\n\t  in  float compare_value,\r\n\t  in  int2 offset\r\n\t);\t\r\n\t\r\n\tfloat4 GatherCmpR" +
    "ed(\r\n\t  in  SamplerComparisonState s,\r\n\t  in  float3 location,\r\n\t  in  float com" +
    "pare_value,\r\n\t  in  int2 offset\r\n\t);\t\r\n\t\r\n\tfloat4 GatherCmpGreen(\r\n\t  in  Sample" +
    "rComparisonState s,\r\n\t  in  float3 location,\r\n\t  in  float compare_value,\r\n\t  in" +
    "  int2 offset\r\n\t);\t\r\n\t\r\n\tfloat4 GatherCmpBlue(\r\n\t  in  SamplerComparisonState s," +
    "\r\n\t  in  float3 location,\r\n\t  in  float compare_value,\r\n\t  in  int2 offset\r\n\t);\r" +
    "\n\t\r\n\tfloat4 GatherCmpAlpha(\r\n\t  in  SamplerComparisonState s,\r\n\t  in  float3 loc" +
    "ation,\r\n\t  in  float compare_value,\r\n\t  in  int2 offset\r\n\t);\r\n\t\r\n\tT mips.operato" +
    "r[][](in  uint mipSlice, in  uint3 pos);\r\n\t\t\r\n\tT operator[](in  uint3 pos);\r\n};\r" +
    "\n\r\n\r\nclass __Texture3D<T> {\r\n\t// SM 4.0\r\n\tfloat CalculateLevelOfDetail( sampler_" +
    "state s, float3 x);\r\n\tvoid GetDimensions( uint MipLevel, out uint Width, out uin" +
    "t Height, out uint Depth, out uint NumberOfLevels);\r\n\tvoid GetDimensions( out ui" +
    "nt Width, out uint Height, out uint Depth);\r\n\tvoid GetDimensions( uint MipLevel," +
    " out float Width, out float Height, out float Depth, out float NumberOfLevels);\r" +
    "\n\tvoid GetDimensions( out float Width, out float Height, out float Depth);\r\n\tT L" +
    "oad(int4 Location);\r\n\tT Load(int4 Location, int3 Offset);\r\n\tfloat4 Sample(sample" +
    "r_state S, float3 Location);\r\n\tfloat4 Sample(sampler_state S, float3 Location, i" +
    "nt3 Offset);\r\n\tfloat4 SampleBias(sampler_state S, float3 Location, float Bias);\r" +
    "\n\tfloat4 SampleBias(sampler_state S, float3 Location, float Bias, int3 Offset);\r" +
    "\n\tfloat SampleCmp(sampler_state S, float3 Location, float CompareValue);\r\n\tfloat" +
    " SampleCmp(sampler_state S, float3 Location, float CompareValue, int3 Offset);\r\n" +
    "\tfloat4 SampleGrad(sampler_state S, float3 Location, float3 DDX, float3 DDY);\r\n\t" +
    "float4 SampleGrad(sampler_state S, float3 Location, float3 DDX, float3 DDY, int3" +
    " Offset);\r\n\tfloat4 SampleLevel( sampler_state S, float3 Location, float LOD);\r\n\t" +
    "float4 SampleLevel( sampler_state S, float3 Location, float LOD, int3 Offset);\r\n" +
    "\t\r\n\t// SM 5.0\r\n\tT mips.operator[][](in uint mipSlice,in  uint3 pos);\r\n\t\t\r\n\tT ope" +
    "rator[](in  uint3 pos);\r\n};\r\n\r\nclass __TextureCube<T> {\r\n\t// SM 4.0\r\n\tfloat Calc" +
    "ulateLevelOfDetail( sampler_state s, float3 x);\r\n\tvector<__T_base,4> Gather( sam" +
    "pler_state S, float3 Location);\r\n\tvoid GetDimensions( uint MipLevel, out uint Wi" +
    "dth, out uint Height, out uint NumberOfLevels);\r\n\tvoid GetDimensions( out uint W" +
    "idth, out uint Height);\r\n\tvoid GetDimensions( uint MipLevel, out float Width, ou" +
    "t float Height, out uint NumberOfLevels);\r\n\tvoid GetDimensions( out float Width," +
    " out float Height);\r\n\tfloat4 Sample(sampler_state S, float3 Location);\r\n\tfloat4 " +
    "SampleBias(sampler_state S, float3 Location, float Bias);\r\n\tfloat SampleCmp(samp" +
    "ler_state S, float3 Location, float CompareValue);\r\n\tfloat SampleCmpLevelZero(sa" +
    "mpler_state S, float3 Location, float CompareValue);\r\n\tfloat4 SampleGrad(sampler" +
    "_state S, float3 Location, float3 DDX, float3 DDY);\r\n\tfloat4 SampleLevel( sample" +
    "r_state S, float3 Location, float LOD);\r\n};\r\n\r\nclass __TextureCubeArray<T> {\r\n\t/" +
    "/ SM 4.0\r\n\tfloat CalculateLevelOfDetail( sampler_state s, float3 x);\r\n\tvector<__" +
    "T_base,4> Gather( sampler_state S, float4 Location);\r\n\tvoid GetDimensions( uint " +
    "MipLevel, out uint Width, out uint Height, out uint Elements, out uint NumberOfL" +
    "evels);\r\n\tvoid GetDimensions( out uint Width, out uint Height, out uint Elements" +
    ");\r\n\tvoid GetDimensions( uint MipLevel, out float Width, out float Height, out f" +
    "loat Elements, out float NumberOfLevels);\r\n\tvoid GetDimensions( out float Width," +
    " out float Height, out float Elements);\r\n\tfloat4 Sample(sampler_state S, float4 " +
    "Location);\r\n\tfloat4 SampleBias(sampler_state S, float4 Location, float Bias);\r\n\t" +
    "float SampleCmp(sampler_state S, float4 Location, float CompareValue);\r\n\tfloat S" +
    "ampleCmpLevelZero(sampler_state S, float4 Location, float CompareValue);\r\n\tfloat" +
    "4 SampleGrad(sampler_state S, float4 Location, float3 DDX, float3 DDY);\r\n\tfloat4" +
    " SampleLevel( sampler_state S, float4 Location, float LOD);\r\n};\r\n\r\nclass __Textu" +
    "re2DMS<T> {\r\n\t// SM 4.0\r\n\tvoid GetDimensions( out uint Width, out uint Height, o" +
    "ut uint Samples);\r\n\tvoid GetDimensions( out float Width, out float Height, out f" +
    "loat Samples);\r\n\tfloat2 GetSamplePosition(int s);\r\n\tT Load(int2 Location);\r\n\tT L" +
    "oad(int2 Location, int2 Offset);\r\n\tT Load(int2 Location, int2 Offset, int Sample" +
    "Index);\r\n\t\r\n\t\r\n\t// SM 5.0\r\n\tfloat2 GetSamplePosition(\r\n\t  in  int sampleindex\r\n\t" +
    ");\t\r\n\t\r\n\tT Load(\r\n\t  in  int2 coord,\r\n\t  in  int sampleindex\r\n\t);\t\r\n\t\r\n\tT sample" +
    ".operator[][]( in  uint sampleSlice, in  uint3 pos);\t\r\n};\r\n\r\nclass __Texture2DMS" +
    "Array<T> {\r\n\t// SM 4.0\r\n\tvoid GetDimensions( out uint Width, out uint Height, ou" +
    "t uint Elements, out uint Samples);\r\n\tvoid GetDimensions( out float Width, out f" +
    "loat Height, out float Elements, out float Samples);\r\n\tfloat2 GetSamplePosition(" +
    "int s);\r\n\tT Load(int3 Location); \r\n\tT Load(int3 Location, int2 Offset); \r\n\tT Loa" +
    "d(int3 Location, int2 Offset, int SampleIndex); \r\n\r\n\t// SM 5.0\r\n\tfloat2 GetSampl" +
    "ePosition(\r\n\t  in  int sampleindex\r\n\t);\t\r\n\r\n\tT Load(\r\n\t  in  int3 coord,\r\n\t  in " +
    " int sampleindex\r\n\t);\t\r\n\r\n\tT sample.operator[][]( in  uint sampleSlice, in  uint" +
    "3 pos);\r\n};\r\n\r\nclass __Buffer<T> {\r\n\t// SM 4.0\r\n\tT Load(int Location);\r\n\r\n\tvoid " +
    "GetDimensions(out  uint dim);\t\r\n\t\r\n\tT operator[](in  uint pos);\r\n};\r\n\r\n// Stream" +
    "-Output Object (DirectX HLSL)\r\n// http://msdn.microsoft.com/en-us/library/bb5096" +
    "61%28v=VS.85%29.aspx\r\n// StreamOutputObject <T>   Name\r\n// StreamOutputObject: P" +
    "ointStream, LineStream, TriangleStream\r\nclass __PointStream<T> {\r\n\tvoid Append(T" +
    " StreamDataType);\r\n\tvoid RestartStrip();\r\n};\r\n\r\nclass __LineStream<T> {\r\n\tvoid A" +
    "ppend(T StreamDataType);\r\n\tvoid RestartStrip();\r\n};\r\n\r\nclass __TriangleStream<T>" +
    " {\r\n\tvoid Append(T StreamDataType);\r\n\tvoid RestartStrip();\r\n};\r\n\r\n// -----------" +
    "----------------------------------------------------------------------------\r\n//" +
    " Shader Model 5.0 \r\n// ---------------------------------------------------------" +
    "------------------------------\r\n\r\n// AppendStructuredBuffer<T>\r\n// http://msdn.m" +
    "icrosoft.com/en-us/library/ff471448%28v=VS.85%29.aspx\r\nclass __AppendStructuredB" +
    "uffer<T> {\r\n\tvoid Append(T value);\r\n\tvoid GetDimensions(out uint numStructs, out" +
    " uint stride);\r\n};\r\n\r\n// ByteAddressBuffer\r\n// http://msdn.microsoft.com/en-us/l" +
    "ibrary/ff471453%28v=VS.85%29.aspx\r\nclass __ByteAddressBuffer {\r\n\tvoid GetDimensi" +
    "ons(out  uint dim);\r\n\tuint Load(in  uint address);\r\n\tuint2 Load2(in  uint addres" +
    "s);\r\n\tuint3 Load3(in  uint address);\r\n\tuint4 Load4(in  uint address);\r\n};\r\n\r\n// " +
    "ConsumeStructuredBuffer<T>\r\n// http://msdn.microsoft.com/en-us/library/ff471459%" +
    "28v=VS.85%29.aspx\r\nclass __ConsumeStructuredBuffer<T> {\r\n\tT Consume(void);\r\n\tvoi" +
    "d GetDimensions(out  uint numStructs, out  uint stride);\r\n};\r\n\r\n// InputPatch<T," +
    "N>\r\n// http://msdn.microsoft.com/en-us/library/ff471462%28v=VS.85%29.aspx\r\nclass" +
    " __InputPatch<T,N> {\r\n\tuint Length;\r\n\tT operator[](in uint n);\r\n};\r\n\r\n// OutputP" +
    "atch<T,N>\r\n// http://msdn.microsoft.com/en-us/library/ff471464%28v=VS.85%29.aspx" +
    "\r\nclass __OutputPatch<T,N> {\r\n\tuint Length;\r\n\tT operator[](in uint n);\r\n};\r\n\r\n//" +
    " RWBuffer<T>\r\n// http://msdn.microsoft.com/en-us/library/ff471472%28v=VS.85%29.a" +
    "spx\r\nclass __RWBuffer<T> {\r\n\tvoid GetDimensions(out  uint dim);\r\n\tT operator[](i" +
    "n uint pos);\r\n};\r\n\r\n// RWByteAddressBuffer\r\n// http://msdn.microsoft.com/en-us/l" +
    "ibrary/ff471475%28v=VS.85%29.aspx\r\nclass __RWByteAddressBuffer {\r\n\tvoid GetDimen" +
    "sions(out  uint dim);\r\n\tvoid InterlockedAdd(in   uint dest, in   uint value, out" +
    "  uint original_value);\r\n\tvoid InterlockedAnd(\r\n\t\tin   uint dest,\r\n\t\tin   uint v" +
    "alue,\r\n\t\tout  uint original_value\r\n\t);\r\n\tvoid InterlockedCompareExchange(\r\n\t\tin " +
    "  uint dest,\r\n\t\tin   uint compare_value,\r\n\t\tin   uint value,\r\n\t\tout  uint origin" +
    "al_value\r\n\t);\r\n\tvoid InterlockedCompareStore(\r\n\t  in  uint dest,\r\n\t  in  uint co" +
    "mpare_value,\r\n\t  in  uint value\r\n\t);\r\n\tvoid InterlockedExchange(\r\n\t  in   uint d" +
    "est,\r\n\t  in   uint value,\r\n\t  out  uint original_value\r\n\t);\r\n\tvoid InterlockedMa" +
    "x(\r\n\t  in   uint dest,\r\n\t  in   uint value,\r\n\t  out  uint original_value\r\n\t);\t\r\n" +
    "\tvoid InterlockedMin(\r\n\t  in   uint dest,\r\n\t  in   uint value,\r\n\t  out  uint ori" +
    "ginal_value\r\n\t);\t\r\n\tvoid InterlockedOr(\r\n\t  in   uint dest,\r\n\t  in   uint value," +
    "\r\n\t  out  uint original_value\r\n\t);\t\r\n\tvoid InterlockedXor(\r\n\t  in   uint dest,\r\n" +
    "\t  in   uint value,\r\n\t  out  uint original_value\r\n\t);\t\r\n\tuint Load(\r\n\t  in  uint" +
    " address\r\n\t);\t\r\n\tuint2 Load2(\r\n\t  in  uint address\r\n\t);\t\r\n\tuint3 Load3(\r\n\t  in  " +
    "uint address\r\n\t);\t\r\n\tuint4 Load4(\r\n\t  in  uint address\r\n\t);\t\r\n\tvoid Store(\r\n\t  i" +
    "n  uint address,\r\n\t  in  uint value\r\n\t);\t\r\n\tvoid Store2(\r\n\t  in  uint address,\r\n" +
    "\t  in  uint2 values\r\n\t);\t\r\n\tvoid Store3(\r\n\t  in  uint address,\r\n\t  in  uint3 val" +
    "ues\r\n\t);\t\r\n\tvoid Store4(\r\n\t  in  uint address,\r\n\t  in  uint4 values\r\n\t);\t\r\n};\r\n\r" +
    "\n// RWStructuredBuffer<T>\r\n// http://msdn.microsoft.com/en-us/library/ff471494%2" +
    "8v=VS.85%29.aspx\r\nclass __RWStructuredBuffer<T> {\r\n\r\n\tuint DecrementCounter(void" +
    ");\r\n\r\n\tvoid GetDimensions(\r\n\t  out  uint numStructs,\r\n\t  out  uint stride\r\n\t);\r\n" +
    "\r\n\tuint IncrementCounter(void);\r\n\r\n\tT operator[](in uint pos);\r\n};\r\n\r\n// RWTextu" +
    "re1D<T>\r\n// http://msdn.microsoft.com/en-us/library/ff471499%28v=VS.85%29.aspx\r\n" +
    "class __RWTexture1D<T> {\r\n\tvoid GetDimensions(\r\n\t  out  uint Width\r\n\t);\r\n\tT oper" +
    "ator[](in  uint pos);\r\n};\r\n\r\n// RWTexture1DArray<T>\r\n// http://msdn.microsoft.co" +
    "m/en-us/library/ff471500%28v=VS.85%29.aspx\r\nclass __RWTexture1DArray<T> {\r\n\tvoid" +
    " GetDimensions(\r\n\t  out  uint Width,\r\n\t  out  uint Elements\r\n\t);\r\n\r\n\tT operator[" +
    "](in  uint2 pos);\r\n};\r\n\r\n// RWTexture2D<T>\r\n// http://msdn.microsoft.com/en-us/l" +
    "ibrary/ff471505%28v=VS.85%29.aspx\r\nclass __RWTexture2D<T> {\r\n\tvoid GetDimensions" +
    "(\r\n\t  out  uint Width,\r\n\t  out  uint Height\r\n\t);\r\n\r\n      T operator[](in  uint2" +
    " pos);\r\n};\r\n\r\n// RWTexture2DArray<T>\r\n// http://msdn.microsoft.com/en-us/library" +
    "/ff471506%28v=VS.85%29.aspx\r\nclass __RWTexture2DArray<T> {\r\n\tvoid GetDimensions(" +
    "\r\n\t  out  uint Width,\r\n\t  out  uint Height,\r\n\t  out  uint Elements\r\n\t);\r\n\tT oper" +
    "ator[](in  uint3 pos);\r\n};\r\n\r\n// RWTexture3D<T>\r\n// http://msdn.microsoft.com/en" +
    "-us/library/ff471511%28v=VS.85%29.aspx\r\nclass __RWTexture3D<T> {\r\n\tvoid GetDimen" +
    "sions(\r\n\t  out  uint Width,\r\n\t  out  uint Height,\r\n\t  out  uint Depth\r\n\t);\r\n\r\n\tT" +
    " operator[](in  uint3 pos);\r\n};\r\n\r\n// StructuredBuffer<T>\r\n// http://msdn.micros" +
    "oft.com/en-us/library/ff471514%28v=VS.85%29.aspx\r\nclass __StructuredBuffer<T> {\r" +
    "\n\tvoid GetDimensions(\r\n\t  out  uint numStructs,\r\n\t  out  uint stride\r\n\t);\r\n\r\n\tT " +
    "operator[](in  uint pos);\t\r\n};";
			}
		}

		public static System.Byte[] Keywords
		{
			get {
				 return new byte [] {
					239, 187, 191, 47, 47, 32, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 
					45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 
					45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 13, 10, 47, 47, 32, 76, 105, 115, 116, 32, 111, 102, 32, 71, 76, 83, 76, 32, 107, 101, 121, 
					119, 111, 114, 100, 115, 13, 10, 47, 47, 32, 65, 99, 99, 111, 114, 100, 105, 110, 103, 32, 116, 111, 32, 116, 104, 101, 32, 115, 112, 101, 99, 32, 104, 116, 116, 112, 58, 47, 47, 119, 119, 119, 46, 111, 112, 101, 110, 103, 108, 46, 
					111, 114, 103, 47, 114, 101, 103, 105, 115, 116, 114, 121, 47, 100, 111, 99, 47, 71, 76, 83, 76, 97, 110, 103, 83, 112, 101, 99, 46, 52, 46, 50, 48, 46, 54, 46, 99, 108, 101, 97, 110, 46, 112, 100, 102, 13, 10, 47, 47, 32, 
					83, 101, 99, 116, 105, 111, 110, 32, 34, 51, 46, 54, 32, 75, 101, 121, 119, 111, 114, 100, 115, 34, 44, 32, 112, 49, 54, 45, 49, 56, 13, 10, 47, 47, 32, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 
					45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 
					45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 45, 
					45, 45, 45, 45, 45, 45, 45, 45, 13, 10, 47, 47, 32, 84, 104, 101, 32, 102, 111, 108, 108, 111, 119, 105, 110, 103, 32, 97, 114, 101, 32, 116, 104, 101, 32, 107, 101, 121, 119, 111, 114, 100, 115, 32, 105, 110, 32, 116, 104, 101, 
					32, 108, 97, 110, 103, 117, 97, 103, 101, 44, 32, 97, 110, 100, 32, 99, 97, 110, 110, 111, 116, 32, 98, 101, 32, 117, 115, 101, 100, 32, 102, 111, 114, 32, 97, 110, 121, 32, 111, 116, 104, 101, 114, 32, 112, 117, 114, 112, 111, 115, 
					101, 32, 116, 104, 97, 110, 32, 116, 104, 97, 116, 32, 100, 101, 102, 105, 110, 101, 100, 32, 98, 121, 32, 116, 104, 105, 115, 32, 100, 111, 99, 117, 109, 101, 110, 116, 58, 13, 10, 13, 10, 97, 116, 116, 114, 105, 98, 117, 116, 101, 
					32, 99, 111, 110, 115, 116, 32, 117, 110, 105, 102, 111, 114, 109, 32, 118, 97, 114, 121, 105, 110, 103, 13, 10, 99, 111, 104, 101, 114, 101, 110, 116, 32, 118, 111, 108, 97, 116, 105, 108, 101, 32, 114, 101, 115, 116, 114, 105, 99, 116, 
					32, 114, 101, 97, 100, 111, 110, 108, 121, 32, 119, 114, 105, 116, 101, 111, 110, 108, 121, 13, 10, 97, 116, 111, 109, 105, 99, 95, 117, 105, 110, 116, 13, 10, 108, 97, 121, 111, 117, 116, 13, 10, 99, 101, 110, 116, 114, 111, 105, 100, 
					32, 102, 108, 97, 116, 32, 115, 109, 111, 111, 116, 104, 32, 110, 111, 112, 101, 114, 115, 112, 101, 99, 116, 105, 118, 101, 13, 10, 112, 97, 116, 99, 104, 32, 115, 97, 109, 112, 108, 101, 13, 10, 98, 114, 101, 97, 107, 32, 99, 111, 
					110, 116, 105, 110, 117, 101, 32, 100, 111, 32, 102, 111, 114, 32, 119, 104, 105, 108, 101, 32, 115, 119, 105, 116, 99, 104, 32, 99, 97, 115, 101, 32, 100, 101, 102, 97, 117, 108, 116, 13, 10, 105, 102, 32, 101, 108, 115, 101, 13, 10, 
					115, 117, 98, 114, 111, 117, 116, 105, 110, 101, 13, 10, 105, 110, 32, 111, 117, 116, 32, 105, 110, 111, 117, 116, 13, 10, 102, 108, 111, 97, 116, 32, 100, 111, 117, 98, 108, 101, 32, 105, 110, 116, 32, 118, 111, 105, 100, 32, 98, 111, 
					111, 108, 32, 116, 114, 117, 101, 32, 102, 97, 108, 115, 101, 13, 10, 105, 110, 118, 97, 114, 105, 97, 110, 116, 13, 10, 100, 105, 115, 99, 97, 114, 100, 32, 114, 101, 116, 117, 114, 110, 13, 10, 109, 97, 116, 50, 32, 109, 97, 116, 
					51, 32, 109, 97, 116, 52, 32, 100, 109, 97, 116, 50, 32, 100, 109, 97, 116, 51, 32, 100, 109, 97, 116, 52, 13, 10, 109, 97, 116, 50, 120, 50, 32, 109, 97, 116, 50, 120, 51, 32, 109, 97, 116, 50, 120, 52, 32, 100, 109, 97, 
					116, 50, 120, 50, 32, 100, 109, 97, 116, 50, 120, 51, 32, 100, 109, 97, 116, 50, 120, 52, 13, 10, 109, 97, 116, 51, 120, 50, 32, 109, 97, 116, 51, 120, 51, 32, 109, 97, 116, 51, 120, 52, 32, 100, 109, 97, 116, 51, 120, 50, 
					32, 100, 109, 97, 116, 51, 120, 51, 32, 100, 109, 97, 116, 51, 120, 52, 13, 10, 109, 97, 116, 52, 120, 50, 32, 109, 97, 116, 52, 120, 51, 32, 109, 97, 116, 52, 120, 52, 32, 100, 109, 97, 116, 52, 120, 50, 32, 100, 109, 97, 
					116, 52, 120, 51, 32, 100, 109, 97, 116, 52, 120, 52, 13, 10, 118, 101, 99, 50, 32, 118, 101, 99, 51, 32, 118, 101, 99, 52, 32, 105, 118, 101, 99, 50, 32, 105, 118, 101, 99, 51, 32, 105, 118, 101, 99, 52, 32, 98, 118, 101, 
					99, 50, 32, 98, 118, 101, 99, 51, 32, 98, 118, 101, 99, 52, 32, 100, 118, 101, 99, 50, 32, 100, 118, 101, 99, 51, 32, 100, 118, 101, 99, 52, 13, 10, 117, 105, 110, 116, 32, 117, 118, 101, 99, 50, 32, 117, 118, 101, 99, 51, 
					32, 117, 118, 101, 99, 52, 13, 10, 108, 111, 119, 112, 32, 109, 101, 100, 105, 117, 109, 112, 32, 104, 105, 103, 104, 112, 32, 112, 114, 101, 99, 105, 115, 105, 111, 110, 13, 10, 115, 97, 109, 112, 108, 101, 114, 49, 68, 32, 115, 97, 
					109, 112, 108, 101, 114, 50, 68, 32, 115, 97, 109, 112, 108, 101, 114, 51, 68, 32, 115, 97, 109, 112, 108, 101, 114, 67, 117, 98, 101, 13, 10, 115, 97, 109, 112, 108, 101, 114, 49, 68, 83, 104, 97, 100, 111, 119, 32, 115, 97, 109, 
					112, 108, 101, 114, 50, 68, 83, 104, 97, 100, 111, 119, 32, 115, 97, 109, 112, 108, 101, 114, 67, 117, 98, 101, 83, 104, 97, 100, 111, 119, 13, 10, 115, 97, 109, 112, 108, 101, 114, 49, 68, 65, 114, 114, 97, 121, 32, 115, 97, 109, 
					112, 108, 101, 114, 50, 68, 65, 114, 114, 97, 121, 13, 10, 115, 97, 109, 112, 108, 101, 114, 49, 68, 65, 114, 114, 97, 121, 83, 104, 97, 100, 111, 119, 32, 115, 97, 109, 112, 108, 101, 114, 50, 68, 65, 114, 114, 97, 121, 83, 104, 
					97, 100, 111, 119, 13, 10, 105, 115, 97, 109, 112, 108, 101, 114, 49, 68, 32, 105, 115, 97, 109, 112, 108, 101, 114, 50, 68, 32, 105, 115, 97, 109, 112, 108, 101, 114, 51, 68, 32, 105, 115, 97, 109, 112, 108, 101, 114, 67, 117, 98, 
					101, 13, 10, 105, 115, 97, 109, 112, 108, 101, 114, 49, 68, 65, 114, 114, 97, 121, 32, 105, 115, 97, 109, 112, 108, 101, 114, 50, 68, 65, 114, 114, 97, 121, 13, 10, 117, 115, 97, 109, 112, 108, 101, 114, 49, 68, 32, 117, 115, 97, 
					109, 112, 108, 101, 114, 50, 68, 32, 117, 115, 97, 109, 112, 108, 101, 114, 51, 68, 32, 117, 115, 97, 109, 112, 108, 101, 114, 67, 117, 98, 101, 13, 10, 117, 115, 97, 109, 112, 108, 101, 114, 49, 68, 65, 114, 114, 97, 121, 32, 117, 
					115, 97, 109, 112, 108, 101, 114, 50, 68, 65, 114, 114, 97, 121, 13, 10, 115, 97, 109, 112, 108, 101, 114, 50, 68, 82, 101, 99, 116, 32, 115, 97, 109, 112, 108, 101, 114, 50, 68, 82, 101, 99, 116, 83, 104, 97, 100, 111, 119, 32, 
					105, 115, 97, 109, 112, 108, 101, 114, 50, 68, 82, 101, 99, 116, 32, 117, 115, 97, 109, 112, 108, 101, 114, 50, 68, 82, 101, 99, 116, 13, 10, 115, 97, 109, 112, 108, 101, 114, 66, 117, 102, 102, 101, 114, 32, 105, 115, 97, 109, 112, 
					108, 101, 114, 66, 117, 102, 102, 101, 114, 32, 117, 115, 97, 109, 112, 108, 101, 114, 66, 117, 102, 102, 101, 114, 13, 10, 115, 97, 109, 112, 108, 101, 114, 50, 68, 77, 83, 32, 105, 115, 97, 109, 112, 108, 101, 114, 50, 68, 77, 83, 
					32, 117, 115, 97, 109, 112, 108, 101, 114, 50, 68, 77, 83, 13, 10, 115, 97, 109, 112, 108, 101, 114, 50, 68, 77, 83, 65, 114, 114, 97, 121, 32, 105, 115, 97, 109, 112, 108, 101, 114, 50, 68, 77, 83, 65, 114, 114, 97, 121, 32, 
					117, 115, 97, 109, 112, 108, 101, 114, 50, 68, 77, 83, 65, 114, 114, 97, 121, 13, 10, 115, 97, 109, 112, 108, 101, 114, 67, 117, 98, 101, 65, 114, 114, 97, 121, 32, 115, 97, 109, 112, 108, 101, 114, 67, 117, 98, 101, 65, 114, 114, 
					97, 121, 83, 104, 97, 100, 111, 119, 32, 105, 115, 97, 109, 112, 108, 101, 114, 67, 117, 98, 101, 65, 114, 114, 97, 121, 32, 117, 115, 97, 109, 112, 108, 101, 114, 67, 117, 98, 101, 65, 114, 114, 97, 121, 13, 10, 105, 109, 97, 103, 
					101, 49, 68, 32, 105, 105, 109, 97, 103, 101, 49, 68, 32, 117, 105, 109, 97, 103, 101, 49, 68, 13, 10, 105, 109, 97, 103, 101, 50, 68, 32, 105, 105, 109, 97, 103, 101, 50, 68, 32, 117, 105, 109, 97, 103, 101, 50, 68, 13, 10, 
					105, 109, 97, 103, 101, 51, 68, 32, 105, 105, 109, 97, 103, 101, 51, 68, 32, 117, 105, 109, 97, 103, 101, 51, 68, 13, 10, 105, 109, 97, 103, 101, 50, 68, 82, 101, 99, 116, 32, 105, 105, 109, 97, 103, 101, 50, 68, 82, 101, 99, 
					116, 32, 117, 105, 109, 97, 103, 101, 50, 68, 82, 101, 99, 116, 13, 10, 105, 109, 97, 103, 101, 67, 117, 98, 101, 32, 105, 105, 109, 97, 103, 101, 67, 117, 98, 101, 32, 117, 105, 109, 97, 103, 101, 67, 117, 98, 101, 13, 10, 105, 
					109, 97, 103, 101, 66, 117, 102, 102, 101, 114, 32, 105, 105, 109, 97, 103, 101, 66, 117, 102, 102, 101, 114, 32, 117, 105, 109, 97, 103, 101, 66, 117, 102, 102, 101, 114, 13, 10, 105, 109, 97, 103, 101, 49, 68, 65, 114, 114, 97, 121, 
					32, 105, 105, 109, 97, 103, 101, 49, 68, 65, 114, 114, 97, 121, 32, 117, 105, 109, 97, 103, 101, 49, 68, 65, 114, 114, 97, 121, 13, 10, 105, 109, 97, 103, 101, 50, 68, 65, 114, 114, 97, 121, 32, 105, 105, 109, 97, 103, 101, 50, 
					68, 65, 114, 114, 97, 121, 32, 117, 105, 109, 97, 103, 101, 50, 68, 65, 114, 114, 97, 121, 13, 10, 105, 109, 97, 103, 101, 67, 117, 98, 101, 65, 114, 114, 97, 121, 32, 105, 105, 109, 97, 103, 101, 67, 117, 98, 101, 65, 114, 114, 
					97, 121, 32, 117, 105, 109, 97, 103, 101, 67, 117, 98, 101, 65, 114, 114, 97, 121, 13, 10, 105, 109, 97, 103, 101, 50, 68, 77, 83, 32, 105, 105, 109, 97, 103, 101, 50, 68, 77, 83, 32, 117, 105, 109, 97, 103, 101, 50, 68, 77, 
					83, 13, 10, 105, 109, 97, 103, 101, 50, 68, 77, 83, 65, 114, 114, 97, 121, 32, 105, 105, 109, 97, 103, 101, 50, 68, 77, 83, 65, 114, 114, 97, 121, 32, 117, 105, 109, 97, 103, 101, 50, 68, 77, 83, 65, 114, 114, 97, 121, 13, 
					10, 115, 116, 114, 117, 99, 116, 13, 10, 13, 10, 47, 47, 32, 84, 104, 101, 32, 102, 111, 108, 108, 111, 119, 105, 110, 103, 32, 97, 114, 101, 32, 116, 104, 101, 32, 107, 101, 121, 119, 111, 114, 100, 115, 32, 114, 101, 115, 101, 114, 
					118, 101, 100, 32, 102, 111, 114, 32, 102, 117, 116, 117, 114, 101, 32, 117, 115, 101, 46, 32, 85, 115, 105, 110, 103, 32, 116, 104, 101, 109, 32, 119, 105, 108, 108, 32, 114, 101, 115, 117, 108, 116, 32, 105, 110, 32, 97, 110, 32, 101, 
					114, 114, 111, 114, 58, 13, 10, 99, 111, 109, 109, 111, 110, 32, 112, 97, 114, 116, 105, 116, 105, 111, 110, 32, 97, 99, 116, 105, 118, 101, 13, 10, 97, 115, 109, 13, 10, 99, 108, 97, 115, 115, 32, 117, 110, 105, 111, 110, 32, 101, 
					110, 117, 109, 32, 116, 121, 112, 101, 100, 101, 102, 32, 116, 101, 109, 112, 108, 97, 116, 101, 32, 116, 104, 105, 115, 32, 112, 97, 99, 107, 101, 100, 13, 10, 114, 101, 115, 111, 117, 114, 99, 101, 13, 10, 103, 111, 116, 111, 13, 10, 
					105, 110, 108, 105, 110, 101, 32, 110, 111, 105, 110, 108, 105, 110, 101, 32, 112, 117, 98, 108, 105, 99, 32, 115, 116, 97, 116, 105, 99, 32, 101, 120, 116, 101, 114, 110, 32, 101, 120, 116, 101, 114, 110, 97, 108, 32, 105, 110, 116, 101, 
					114, 102, 97, 99, 101, 13, 10, 108, 111, 110, 103, 32, 115, 104, 111, 114, 116, 32, 104, 97, 108, 102, 32, 102, 105, 120, 101, 100, 32, 117, 110, 115, 105, 103, 110, 101, 100, 32, 115, 117, 112, 101, 114, 112, 13, 10, 105, 110, 112, 117, 
					116, 32, 111, 117, 116, 112, 117, 116, 13, 10, 104, 118, 101, 99, 50, 32, 104, 118, 101, 99, 51, 32, 104, 118, 101, 99, 52, 32, 102, 118, 101, 99, 50, 32, 102, 118, 101, 99, 51, 32, 102, 118, 101, 99, 52, 13, 10, 115, 97, 109, 
					112, 108, 101, 114, 51, 68, 82, 101, 99, 116, 13, 10, 102, 105, 108, 116, 101, 114, 13, 10, 115, 105, 122, 101, 111, 102, 32, 99, 97, 115, 116, 13, 10, 110, 97, 109, 101, 115, 112, 97, 99, 101, 32, 117, 115, 105, 110, 103, 13, 10, 
					114, 111, 119, 95, 109, 97, 106, 111, 114, 13, 10, 13, 10, 47, 47, 73, 110, 32, 97, 100, 100, 105, 116, 105, 111, 110, 44, 32, 97, 108, 108, 32, 105, 100, 101, 110, 116, 105, 102, 105, 101, 114, 115, 32, 99, 111, 110, 116, 97, 105, 
					110, 105, 110, 103, 32, 116, 119, 111, 32, 99, 111, 110, 115, 101, 99, 117, 116, 105, 118, 101, 32, 117, 110, 100, 101, 114, 115, 99, 111, 114, 101, 115, 32, 40, 95, 95, 41, 32, 97, 114, 101, 32, 114, 101, 115, 101, 114, 118, 101, 100, 
					32, 97, 115, 32, 112, 111, 115, 115, 105, 98, 108, 101, 32, 102, 117, 116, 117, 114, 101, 32, 107, 101, 121, 119, 111, 114, 100, 115, 46
				};
			}
		}

		public static System.Byte[] Tokenizer
		{
			get {
				 return new byte [] {
					71, 0, 79, 0, 76, 0, 68, 0, 32, 0, 80, 0, 97, 0, 114, 0, 115, 0, 101, 0, 114, 0, 32, 0, 84, 0, 97, 0, 98, 0, 108, 0, 101, 0, 115, 0, 47, 0, 118, 0, 49, 0, 46, 0, 48, 0, 0, 0, 77, 7, 
					0, 98, 80, 83, 40, 0, 85, 0, 110, 0, 116, 0, 105, 0, 116, 0, 108, 0, 101, 0, 100, 0, 41, 0, 0, 0, 83, 40, 0, 78, 0, 111, 0, 116, 0, 32, 0, 83, 0, 112, 0, 101, 0, 99, 0, 105, 0, 102, 0, 105, 
					0, 101, 0, 100, 0, 41, 0, 0, 0, 83, 40, 0, 85, 0, 110, 0, 107, 0, 110, 0, 111, 0, 119, 0, 110, 0, 41, 0, 0, 0, 83, 0, 0, 66, 0, 73, 70, 0, 77, 6, 0, 98, 84, 73, 71, 0, 73, 52, 0, 73, 
					65, 0, 73, 104, 0, 73, 67, 0, 77, 3, 0, 98, 73, 73, 0, 0, 73, 0, 0, 77, 3, 0, 98, 67, 73, 0, 0, 83, 9, 0, 11, 0, 12, 0, 32, 0, 160, 0, 0, 0, 77, 3, 0, 98, 67, 73, 1, 0, 83, 64, 
					0, 0, 0, 77, 3, 0, 98, 67, 73, 2, 0, 83, 126, 0, 0, 0, 77, 3, 0, 98, 67, 73, 3, 0, 83, 44, 0, 0, 0, 77, 3, 0, 98, 67, 73, 4, 0, 83, 36, 0, 65, 0, 66, 0, 67, 0, 68, 0, 69, 0, 
					70, 0, 71, 0, 72, 0, 73, 0, 74, 0, 75, 0, 76, 0, 77, 0, 78, 0, 79, 0, 80, 0, 81, 0, 82, 0, 83, 0, 84, 0, 85, 0, 86, 0, 88, 0, 89, 0, 90, 0, 95, 0, 97, 0, 98, 0, 99, 0, 100, 0, 
					101, 0, 102, 0, 103, 0, 104, 0, 105, 0, 106, 0, 107, 0, 108, 0, 109, 0, 110, 0, 111, 0, 112, 0, 113, 0, 114, 0, 115, 0, 116, 0, 117, 0, 118, 0, 120, 0, 121, 0, 122, 0, 0, 0, 77, 3, 0, 98, 67, 73, 
					5, 0, 83, 91, 0, 0, 0, 77, 3, 0, 98, 67, 73, 6, 0, 83, 123, 0, 0, 0, 77, 3, 0, 98, 67, 73, 7, 0, 83, 40, 0, 0, 0, 77, 3, 0, 98, 67, 73, 8, 0, 83, 92, 0, 0, 0, 77, 3, 0, 98, 
					67, 73, 9, 0, 83, 10, 0, 0, 0, 77, 3, 0, 98, 67, 73, 10, 0, 83, 63, 0, 0, 0, 77, 3, 0, 98, 67, 73, 11, 0, 83, 93, 0, 0, 0, 77, 3, 0, 98, 67, 73, 12, 0, 83, 125, 0, 0, 0, 77, 3, 
					0, 98, 67, 73, 13, 0, 83, 41, 0, 0, 0, 77, 3, 0, 98, 67, 73, 14, 0, 83, 59, 0, 0, 0, 77, 3, 0, 98, 67, 73, 15, 0, 83, 34, 0, 0, 0, 77, 3, 0, 98, 67, 73, 16, 0, 83, 13, 0, 0, 0, 
					77, 3, 0, 98, 67, 73, 17, 0, 83, 33, 0, 0, 0, 77, 3, 0, 98, 67, 73, 18, 0, 83, 35, 0, 0, 0, 77, 3, 0, 98, 67, 73, 19, 0, 83, 37, 0, 0, 0, 77, 3, 0, 98, 67, 73, 20, 0, 83, 38, 0, 
					0, 0, 77, 3, 0, 98, 67, 73, 21, 0, 83, 39, 0, 0, 0, 77, 3, 0, 98, 67, 73, 22, 0, 83, 42, 0, 0, 0, 77, 3, 0, 98, 67, 73, 23, 0, 83, 43, 0, 0, 0, 77, 3, 0, 98, 67, 73, 24, 0, 83, 
					45, 0, 0, 0, 77, 3, 0, 98, 67, 73, 25, 0, 83, 46, 0, 0, 0, 77, 3, 0, 98, 67, 73, 26, 0, 83, 47, 0, 0, 0, 77, 3, 0, 98, 67, 73, 27, 0, 83, 48, 0, 0, 0, 77, 3, 0, 98, 67, 73, 28, 
					0, 83, 49, 0, 50, 0, 51, 0, 52, 0, 53, 0, 54, 0, 55, 0, 56, 0, 57, 0, 0, 0, 77, 3, 0, 98, 67, 73, 29, 0, 83, 58, 0, 0, 0, 77, 3, 0, 98, 67, 73, 30, 0, 83, 60, 0, 0, 0, 77, 3, 
					0, 98, 67, 73, 31, 0, 83, 61, 0, 0, 0, 77, 3, 0, 98, 67, 73, 32, 0, 83, 62, 0, 0, 0, 77, 3, 0, 98, 67, 73, 33, 0, 83, 87, 0, 119, 0, 0, 0, 77, 3, 0, 98, 67, 73, 34, 0, 83, 94, 0, 
					0, 0, 77, 3, 0, 98, 67, 73, 35, 0, 83, 124, 0, 0, 0, 77, 3, 0, 98, 67, 73, 36, 0, 83, 36, 0, 48, 0, 49, 0, 50, 0, 51, 0, 52, 0, 53, 0, 54, 0, 55, 0, 56, 0, 57, 0, 65, 0, 66, 0, 
					67, 0, 68, 0, 69, 0, 70, 0, 71, 0, 72, 0, 73, 0, 74, 0, 75, 0, 76, 0, 77, 0, 78, 0, 79, 0, 80, 0, 81, 0, 82, 0, 83, 0, 84, 0, 85, 0, 86, 0, 87, 0, 88, 0, 89, 0, 90, 0, 95, 0, 
					97, 0, 98, 0, 99, 0, 100, 0, 101, 0, 102, 0, 103, 0, 104, 0, 105, 0, 106, 0, 107, 0, 108, 0, 109, 0, 110, 0, 111, 0, 112, 0, 113, 0, 114, 0, 115, 0, 116, 0, 117, 0, 118, 0, 119, 0, 120, 0, 121, 0, 
					122, 0, 0, 0, 77, 3, 0, 98, 67, 73, 37, 0, 83, 32, 0, 33, 0, 35, 0, 36, 0, 37, 0, 38, 0, 39, 0, 40, 0, 41, 0, 42, 0, 43, 0, 44, 0, 45, 0, 46, 0, 47, 0, 48, 0, 49, 0, 50, 0, 51, 
					0, 52, 0, 53, 0, 54, 0, 55, 0, 56, 0, 57, 0, 58, 0, 59, 0, 60, 0, 61, 0, 62, 0, 63, 0, 64, 0, 65, 0, 66, 0, 67, 0, 68, 0, 69, 0, 70, 0, 71, 0, 72, 0, 73, 0, 74, 0, 75, 0, 76, 
					0, 77, 0, 78, 0, 79, 0, 80, 0, 81, 0, 82, 0, 83, 0, 84, 0, 85, 0, 86, 0, 87, 0, 88, 0, 89, 0, 90, 0, 91, 0, 92, 0, 93, 0, 94, 0, 95, 0, 96, 0, 97, 0, 98, 0, 99, 0, 100, 0, 101, 
					0, 102, 0, 103, 0, 104, 0, 105, 0, 106, 0, 107, 0, 108, 0, 109, 0, 110, 0, 111, 0, 112, 0, 113, 0, 114, 0, 115, 0, 116, 0, 117, 0, 118, 0, 119, 0, 120, 0, 121, 0, 122, 0, 123, 0, 124, 0, 125, 0, 126, 
					0, 160, 0, 0, 0, 77, 3, 0, 98, 67, 73, 38, 0, 83, 32, 0, 33, 0, 35, 0, 36, 0, 37, 0, 38, 0, 39, 0, 40, 0, 41, 0, 42, 0, 43, 0, 44, 0, 45, 0, 46, 0, 47, 0, 48, 0, 49, 0, 50, 0, 
					51, 0, 52, 0, 53, 0, 54, 0, 55, 0, 56, 0, 57, 0, 58, 0, 59, 0, 60, 0, 61, 0, 62, 0, 63, 0, 64, 0, 65, 0, 66, 0, 67, 0, 68, 0, 69, 0, 70, 0, 71, 0, 72, 0, 73, 0, 74, 0, 75, 0, 
					76, 0, 77, 0, 78, 0, 79, 0, 80, 0, 81, 0, 82, 0, 83, 0, 84, 0, 85, 0, 86, 0, 87, 0, 88, 0, 89, 0, 90, 0, 91, 0, 93, 0, 94, 0, 95, 0, 96, 0, 97, 0, 98, 0, 99, 0, 100, 0, 101, 0, 
					102, 0, 103, 0, 104, 0, 105, 0, 106, 0, 107, 0, 108, 0, 109, 0, 110, 0, 111, 0, 112, 0, 113, 0, 114, 0, 115, 0, 116, 0, 117, 0, 118, 0, 119, 0, 120, 0, 121, 0, 122, 0, 123, 0, 124, 0, 125, 0, 126, 0, 
					160, 0, 0, 0, 77, 3, 0, 98, 67, 73, 39, 0, 83, 85, 0, 88, 0, 117, 0, 120, 0, 0, 0, 77, 3, 0, 98, 67, 73, 40, 0, 83, 48, 0, 49, 0, 50, 0, 51, 0, 52, 0, 53, 0, 54, 0, 55, 0, 0, 0, 
					77, 3, 0, 98, 67, 73, 41, 0, 83, 34, 0, 39, 0, 65, 0, 66, 0, 70, 0, 78, 0, 82, 0, 84, 0, 92, 0, 97, 0, 98, 0, 102, 0, 110, 0, 114, 0, 116, 0, 0, 0, 77, 3, 0, 98, 67, 73, 42, 0, 83, 
					48, 0, 49, 0, 50, 0, 51, 0, 52, 0, 53, 0, 54, 0, 55, 0, 56, 0, 57, 0, 65, 0, 66, 0, 67, 0, 68, 0, 69, 0, 70, 0, 97, 0, 98, 0, 99, 0, 100, 0, 101, 0, 102, 0, 0, 0, 77, 3, 0, 98, 
					67, 73, 43, 0, 83, 48, 0, 49, 0, 50, 0, 51, 0, 52, 0, 53, 0, 54, 0, 55, 0, 56, 0, 57, 0, 0, 0, 77, 3, 0, 98, 67, 73, 44, 0, 83, 68, 0, 70, 0, 72, 0, 100, 0, 102, 0, 104, 0, 0, 0, 
					77, 3, 0, 98, 67, 73, 45, 0, 83, 69, 0, 101, 0, 0, 0, 77, 3, 0, 98, 67, 73, 46, 0, 83, 43, 0, 45, 0, 0, 0, 77, 3, 0, 98, 67, 73, 47, 0, 83, 76, 0, 108, 0, 0, 0, 77, 3, 0, 98, 67, 
					73, 48, 0, 83, 56, 0, 57, 0, 0, 0, 77, 3, 0, 98, 67, 73, 49, 0, 83, 88, 0, 120, 0, 0, 0, 77, 3, 0, 98, 67, 73, 50, 0, 83, 36, 0, 48, 0, 49, 0, 50, 0, 51, 0, 52, 0, 53, 0, 54, 0, 
					55, 0, 56, 0, 57, 0, 65, 0, 66, 0, 67, 0, 68, 0, 69, 0, 70, 0, 71, 0, 72, 0, 73, 0, 74, 0, 75, 0, 76, 0, 77, 0, 78, 0, 79, 0, 80, 0, 81, 0, 82, 0, 84, 0, 85, 0, 86, 0, 87, 0, 
					88, 0, 89, 0, 90, 0, 95, 0, 97, 0, 98, 0, 99, 0, 100, 0, 101, 0, 102, 0, 103, 0, 104, 0, 105, 0, 106, 0, 107, 0, 108, 0, 109, 0, 110, 0, 111, 0, 112, 0, 113, 0, 114, 0, 116, 0, 117, 0, 118, 0, 
					119, 0, 120, 0, 121, 0, 122, 0, 0, 0, 77, 3, 0, 98, 67, 73, 51, 0, 83, 83, 0, 115, 0, 0, 0, 77, 4, 0, 98, 83, 73, 0, 0, 83, 69, 0, 79, 0, 70, 0, 0, 0, 73, 3, 0, 77, 4, 0, 98, 83, 
					73, 1, 0, 83, 69, 0, 114, 0, 114, 0, 111, 0, 114, 0, 0, 0, 73, 7, 0, 77, 4, 0, 98, 83, 73, 2, 0, 83, 87, 0, 104, 0, 105, 0, 116, 0, 101, 0, 115, 0, 112, 0, 97, 0, 99, 0, 101, 0, 0, 0, 
					73, 2, 0, 77, 4, 0, 98, 83, 73, 3, 0, 83, 67, 0, 111, 0, 109, 0, 109, 0, 101, 0, 110, 0, 116, 0, 32, 0, 69, 0, 110, 0, 100, 0, 0, 0, 73, 5, 0, 77, 4, 0, 98, 83, 73, 4, 0, 83, 67, 0, 
					111, 0, 109, 0, 109, 0, 101, 0, 110, 0, 116, 0, 32, 0, 76, 0, 105, 0, 110, 0, 101, 0, 0, 0, 73, 6, 0, 77, 4, 0, 98, 83, 73, 5, 0, 83, 67, 0, 111, 0, 109, 0, 109, 0, 101, 0, 110, 0, 116, 0, 
					32, 0, 83, 0, 116, 0, 97, 0, 114, 0, 116, 0, 0, 0, 73, 4, 0, 77, 4, 0, 98, 83, 73, 6, 0, 83, 65, 0, 100, 0, 100, 0, 65, 0, 115, 0, 115, 0, 105, 0, 103, 0, 110, 0, 0, 0, 73, 1, 0, 77, 
					4, 0, 98, 83, 73, 7, 0, 83, 65, 0, 110, 0, 100, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 8, 0, 83, 65, 0, 114, 0, 114, 0, 111, 0, 98, 0, 97, 0, 115, 0, 0, 0, 73, 1, 0, 77, 4, 0, 
					98, 83, 73, 9, 0, 83, 65, 0, 115, 0, 115, 0, 105, 0, 103, 0, 110, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 10, 0, 83, 66, 0, 105, 0, 116, 0, 119, 0, 105, 0, 115, 0, 101, 0, 65, 0, 110, 0, 
					100, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 11, 0, 83, 66, 0, 105, 0, 116, 0, 119, 0, 105, 0, 115, 0, 101, 0, 65, 0, 110, 0, 100, 0, 65, 0, 115, 0, 115, 0, 105, 0, 103, 0, 110, 0, 0, 0, 
					73, 1, 0, 77, 4, 0, 98, 83, 73, 12, 0, 83, 66, 0, 105, 0, 116, 0, 119, 0, 105, 0, 115, 0, 101, 0, 78, 0, 111, 0, 116, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 13, 0, 83, 66, 0, 105, 0, 
					116, 0, 119, 0, 105, 0, 115, 0, 101, 0, 79, 0, 114, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 14, 0, 83, 66, 0, 105, 0, 116, 0, 119, 0, 105, 0, 115, 0, 101, 0, 79, 0, 114, 0, 65, 0, 115, 0, 
					115, 0, 105, 0, 103, 0, 110, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 15, 0, 83, 66, 0, 105, 0, 116, 0, 119, 0, 105, 0, 115, 0, 101, 0, 83, 0, 104, 0, 105, 0, 102, 0, 116, 0, 76, 0, 101, 0, 
					102, 0, 116, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 16, 0, 83, 66, 0, 105, 0, 116, 0, 119, 0, 105, 0, 115, 0, 101, 0, 83, 0, 104, 0, 105, 0, 102, 0, 116, 0, 76, 0, 101, 0, 102, 0, 116, 0, 
					65, 0, 115, 0, 115, 0, 105, 0, 103, 0, 110, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 17, 0, 83, 66, 0, 105, 0, 116, 0, 119, 0, 105, 0, 115, 0, 101, 0, 83, 0, 104, 0, 105, 0, 102, 0, 116, 0, 
					82, 0, 105, 0, 103, 0, 104, 0, 116, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 18, 0, 83, 66, 0, 105, 0, 116, 0, 119, 0, 105, 0, 115, 0, 101, 0, 83, 0, 104, 0, 105, 0, 102, 0, 116, 0, 82, 0, 
					105, 0, 103, 0, 104, 0, 116, 0, 65, 0, 115, 0, 115, 0, 105, 0, 103, 0, 110, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 19, 0, 83, 66, 0, 105, 0, 116, 0, 119, 0, 105, 0, 115, 0, 101, 0, 88, 0, 
					111, 0, 114, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 20, 0, 83, 66, 0, 105, 0, 116, 0, 119, 0, 105, 0, 115, 0, 101, 0, 88, 0, 111, 0, 114, 0, 65, 0, 115, 0, 115, 0, 105, 0, 103, 0, 110, 0, 
					0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 21, 0, 83, 67, 0, 111, 0, 108, 0, 111, 0, 110, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 22, 0, 83, 67, 0, 111, 0, 109, 0, 109, 0, 97, 0, 0, 0, 
					73, 1, 0, 77, 4, 0, 98, 83, 73, 23, 0, 83, 68, 0, 105, 0, 118, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 24, 0, 83, 68, 0, 105, 0, 118, 0, 65, 0, 115, 0, 115, 0, 105, 0, 103, 0, 110, 0, 
					0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 25, 0, 83, 68, 0, 111, 0, 116, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 26, 0, 83, 69, 0, 113, 0, 117, 0, 97, 0, 108, 0, 0, 0, 73, 1, 0, 77, 
					4, 0, 98, 83, 73, 27, 0, 83, 70, 0, 108, 0, 111, 0, 97, 0, 116, 0, 105, 0, 110, 0, 103, 0, 80, 0, 111, 0, 105, 0, 110, 0, 116, 0, 76, 0, 105, 0, 116, 0, 101, 0, 114, 0, 97, 0, 108, 0, 0, 0, 
					73, 1, 0, 77, 4, 0, 98, 83, 73, 28, 0, 83, 70, 0, 108, 0, 111, 0, 97, 0, 116, 0, 105, 0, 110, 0, 103, 0, 80, 0, 111, 0, 105, 0, 110, 0, 116, 0, 76, 0, 105, 0, 116, 0, 101, 0, 114, 0, 97, 0, 
					108, 0, 69, 0, 120, 0, 112, 0, 111, 0, 110, 0, 101, 0, 110, 0, 116, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 29, 0, 83, 71, 0, 114, 0, 101, 0, 97, 0, 116, 0, 101, 0, 114, 0, 84, 0, 104, 0, 
					97, 0, 110, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 30, 0, 83, 71, 0, 114, 0, 101, 0, 97, 0, 116, 0, 101, 0, 114, 0, 84, 0, 104, 0, 97, 0, 110, 0, 79, 0, 114, 0, 69, 0, 113, 0, 117, 0, 
					97, 0, 108, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 31, 0, 83, 72, 0, 101, 0, 120, 0, 69, 0, 115, 0, 99, 0, 97, 0, 112, 0, 101, 0, 67, 0, 104, 0, 97, 0, 114, 0, 76, 0, 105, 0, 116, 0, 
					101, 0, 114, 0, 97, 0, 108, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 32, 0, 83, 72, 0, 101, 0, 120, 0, 73, 0, 110, 0, 116, 0, 101, 0, 103, 0, 101, 0, 114, 0, 76, 0, 105, 0, 116, 0, 101, 0, 
					114, 0, 97, 0, 108, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 33, 0, 83, 73, 0, 100, 0, 101, 0, 110, 0, 116, 0, 105, 0, 102, 0, 105, 0, 101, 0, 114, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 
					73, 34, 0, 83, 73, 0, 100, 0, 101, 0, 110, 0, 116, 0, 105, 0, 102, 0, 105, 0, 101, 0, 114, 0, 83, 0, 101, 0, 112, 0, 97, 0, 114, 0, 97, 0, 116, 0, 111, 0, 114, 0, 0, 0, 73, 1, 0, 77, 4, 0, 
					98, 83, 73, 35, 0, 83, 73, 0, 110, 0, 100, 0, 105, 0, 114, 0, 101, 0, 99, 0, 116, 0, 67, 0, 104, 0, 97, 0, 114, 0, 76, 0, 105, 0, 116, 0, 101, 0, 114, 0, 97, 0, 108, 0, 0, 0, 73, 1, 0, 77, 
					4, 0, 98, 83, 73, 36, 0, 83, 76, 0, 101, 0, 102, 0, 116, 0, 66, 0, 114, 0, 97, 0, 99, 0, 107, 0, 101, 0, 116, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 37, 0, 83, 76, 0, 101, 0, 102, 0, 
					116, 0, 67, 0, 117, 0, 114, 0, 108, 0, 121, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 38, 0, 83, 76, 0, 101, 0, 102, 0, 116, 0, 80, 0, 97, 0, 114, 0, 101, 0, 110, 0, 0, 0, 73, 1, 0, 77, 
					4, 0, 98, 83, 73, 39, 0, 83, 76, 0, 101, 0, 115, 0, 115, 0, 84, 0, 104, 0, 97, 0, 110, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 40, 0, 83, 76, 0, 101, 0, 115, 0, 115, 0, 84, 0, 104, 0, 
					97, 0, 110, 0, 79, 0, 114, 0, 69, 0, 113, 0, 117, 0, 97, 0, 108, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 41, 0, 83, 76, 0, 105, 0, 110, 0, 101, 0, 67, 0, 111, 0, 110, 0, 116, 0, 105, 0, 
					110, 0, 117, 0, 97, 0, 116, 0, 105, 0, 111, 0, 110, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 42, 0, 83, 77, 0, 105, 0, 110, 0, 117, 0, 115, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 43, 
					0, 83, 77, 0, 105, 0, 110, 0, 117, 0, 115, 0, 77, 0, 105, 0, 110, 0, 117, 0, 115, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 44, 0, 83, 77, 0, 111, 0, 100, 0, 0, 0, 73, 1, 0, 77, 4, 0, 
					98, 83, 73, 45, 0, 83, 77, 0, 111, 0, 100, 0, 65, 0, 115, 0, 115, 0, 105, 0, 103, 0, 110, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 46, 0, 83, 77, 0, 117, 0, 108, 0, 0, 0, 73, 1, 0, 77, 
					4, 0, 98, 83, 73, 47, 0, 83, 77, 0, 117, 0, 108, 0, 65, 0, 115, 0, 115, 0, 105, 0, 103, 0, 110, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 48, 0, 83, 78, 0, 101, 0, 119, 0, 76, 0, 105, 0, 
					110, 0, 101, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 49, 0, 83, 78, 0, 111, 0, 116, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 50, 0, 83, 78, 0, 111, 0, 116, 0, 69, 0, 113, 0, 117, 0, 
					97, 0, 108, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 51, 0, 83, 79, 0, 99, 0, 116, 0, 97, 0, 108, 0, 69, 0, 115, 0, 99, 0, 97, 0, 112, 0, 101, 0, 67, 0, 104, 0, 97, 0, 114, 0, 76, 0, 
					105, 0, 116, 0, 101, 0, 114, 0, 97, 0, 108, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 52, 0, 83, 79, 0, 99, 0, 116, 0, 97, 0, 108, 0, 73, 0, 110, 0, 116, 0, 101, 0, 103, 0, 101, 0, 114, 0, 
					76, 0, 105, 0, 116, 0, 101, 0, 114, 0, 97, 0, 108, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 53, 0, 83, 79, 0, 114, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 54, 0, 83, 80, 0, 108, 0, 
					117, 0, 115, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 55, 0, 83, 80, 0, 108, 0, 117, 0, 115, 0, 80, 0, 108, 0, 117, 0, 115, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 56, 0, 83, 80, 0, 
					114, 0, 101, 0, 112, 0, 114, 0, 111, 0, 99, 0, 101, 0, 115, 0, 115, 0, 111, 0, 114, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 57, 0, 83, 81, 0, 117, 0, 101, 0, 115, 0, 116, 0, 105, 0, 111, 0, 
					110, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 58, 0, 83, 82, 0, 105, 0, 103, 0, 104, 0, 116, 0, 66, 0, 114, 0, 97, 0, 99, 0, 107, 0, 101, 0, 116, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 
					73, 59, 0, 83, 82, 0, 105, 0, 103, 0, 104, 0, 116, 0, 67, 0, 117, 0, 114, 0, 108, 0, 121, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 60, 0, 83, 82, 0, 105, 0, 103, 0, 104, 0, 116, 0, 80, 0, 
					97, 0, 114, 0, 101, 0, 110, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 61, 0, 83, 83, 0, 101, 0, 109, 0, 105, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 62, 0, 83, 83, 0, 116, 0, 97, 0, 
					110, 0, 100, 0, 97, 0, 114, 0, 100, 0, 69, 0, 115, 0, 99, 0, 97, 0, 112, 0, 101, 0, 67, 0, 104, 0, 97, 0, 114, 0, 76, 0, 105, 0, 116, 0, 101, 0, 114, 0, 97, 0, 108, 0, 0, 0, 73, 1, 0, 77, 
					4, 0, 98, 83, 73, 63, 0, 83, 83, 0, 116, 0, 97, 0, 114, 0, 116, 0, 87, 0, 105, 0, 116, 0, 104, 0, 78, 0, 111, 0, 90, 0, 101, 0, 114, 0, 111, 0, 68, 0, 101, 0, 99, 0, 105, 0, 109, 0, 97, 0, 
					108, 0, 73, 0, 110, 0, 116, 0, 101, 0, 103, 0, 101, 0, 114, 0, 76, 0, 105, 0, 116, 0, 101, 0, 114, 0, 97, 0, 108, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 64, 0, 83, 83, 0, 116, 0, 97, 0, 
					114, 0, 116, 0, 87, 0, 105, 0, 116, 0, 104, 0, 90, 0, 101, 0, 114, 0, 111, 0, 68, 0, 101, 0, 99, 0, 105, 0, 109, 0, 97, 0, 108, 0, 73, 0, 110, 0, 116, 0, 101, 0, 103, 0, 101, 0, 114, 0, 76, 0, 
					105, 0, 116, 0, 101, 0, 114, 0, 97, 0, 108, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 65, 0, 83, 83, 0, 116, 0, 114, 0, 105, 0, 110, 0, 103, 0, 76, 0, 105, 0, 116, 0, 101, 0, 114, 0, 97, 0, 
					108, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 66, 0, 83, 83, 0, 117, 0, 98, 0, 65, 0, 115, 0, 115, 0, 105, 0, 103, 0, 110, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 67, 0, 83, 84, 0, 
					111, 0, 107, 0, 101, 0, 110, 0, 80, 0, 97, 0, 115, 0, 116, 0, 105, 0, 110, 0, 103, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 73, 68, 0, 83, 87, 0, 83, 0, 0, 0, 73, 1, 0, 77, 4, 0, 98, 83, 
					73, 69, 0, 83, 86, 0, 97, 0, 108, 0, 117, 0, 101, 0, 0, 0, 73, 0, 0, 77, 4, 0, 98, 83, 73, 70, 0, 83, 86, 0, 97, 0, 108, 0, 117, 0, 101, 0, 115, 0, 0, 0, 73, 0, 0, 77, 5, 0, 98, 82, 
					73, 0, 0, 73, 69, 0, 69, 73, 68, 0, 77, 5, 0, 98, 82, 73, 1, 0, 73, 69, 0, 69, 73, 48, 0, 77, 5, 0, 98, 82, 73, 2, 0, 73, 69, 0, 69, 73, 35, 0, 77, 5, 0, 98, 82, 73, 3, 0, 73, 69, 
					0, 69, 73, 62, 0, 77, 5, 0, 98, 82, 73, 4, 0, 73, 69, 0, 69, 73, 51, 0, 77, 5, 0, 98, 82, 73, 5, 0, 73, 69, 0, 69, 73, 31, 0, 77, 5, 0, 98, 82, 73, 6, 0, 73, 69, 0, 69, 73, 64, 0, 
					77, 5, 0, 98, 82, 73, 7, 0, 73, 69, 0, 69, 73, 63, 0, 77, 5, 0, 98, 82, 73, 8, 0, 73, 69, 0, 69, 73, 27, 0, 77, 5, 0, 98, 82, 73, 9, 0, 73, 69, 0, 69, 73, 28, 0, 77, 5, 0, 98, 82, 
					73, 10, 0, 73, 69, 0, 69, 73, 32, 0, 77, 5, 0, 98, 82, 73, 11, 0, 73, 69, 0, 69, 73, 52, 0, 77, 5, 0, 98, 82, 73, 12, 0, 73, 69, 0, 69, 73, 65, 0, 77, 5, 0, 98, 82, 73, 13, 0, 73, 69, 
					0, 69, 73, 33, 0, 77, 5, 0, 98, 82, 73, 14, 0, 73, 69, 0, 69, 73, 41, 0, 77, 5, 0, 98, 82, 73, 15, 0, 73, 69, 0, 69, 73, 56, 0, 77, 5, 0, 98, 82, 73, 16, 0, 73, 69, 0, 69, 73, 67, 0, 
					77, 5, 0, 98, 82, 73, 17, 0, 73, 69, 0, 69, 73, 8, 0, 77, 5, 0, 98, 82, 73, 18, 0, 73, 69, 0, 69, 73, 49, 0, 77, 5, 0, 98, 82, 73, 19, 0, 73, 69, 0, 69, 73, 50, 0, 77, 5, 0, 98, 82, 
					73, 20, 0, 73, 69, 0, 69, 73, 7, 0, 77, 5, 0, 98, 82, 73, 21, 0, 73, 69, 0, 69, 73, 38, 0, 77, 5, 0, 98, 82, 73, 22, 0, 73, 69, 0, 69, 73, 60, 0, 77, 5, 0, 98, 82, 73, 23, 0, 73, 69, 
					0, 69, 73, 46, 0, 77, 5, 0, 98, 82, 73, 24, 0, 73, 69, 0, 69, 73, 47, 0, 77, 5, 0, 98, 82, 73, 25, 0, 73, 69, 0, 69, 73, 54, 0, 77, 5, 0, 98, 82, 73, 26, 0, 73, 69, 0, 69, 73, 55, 0, 
					77, 5, 0, 98, 82, 73, 27, 0, 73, 69, 0, 69, 73, 6, 0, 77, 5, 0, 98, 82, 73, 28, 0, 73, 69, 0, 69, 73, 22, 0, 77, 5, 0, 98, 82, 73, 29, 0, 73, 69, 0, 69, 73, 42, 0, 77, 5, 0, 98, 82, 
					73, 30, 0, 73, 69, 0, 69, 73, 43, 0, 77, 5, 0, 98, 82, 73, 31, 0, 73, 69, 0, 69, 73, 66, 0, 77, 5, 0, 98, 82, 73, 32, 0, 73, 69, 0, 69, 73, 23, 0, 77, 5, 0, 98, 82, 73, 33, 0, 73, 69, 
					0, 69, 73, 24, 0, 77, 5, 0, 98, 82, 73, 34, 0, 73, 69, 0, 69, 73, 44, 0, 77, 5, 0, 98, 82, 73, 35, 0, 73, 69, 0, 69, 73, 45, 0, 77, 5, 0, 98, 82, 73, 36, 0, 73, 69, 0, 69, 73, 21, 0, 
					77, 5, 0, 98, 82, 73, 37, 0, 73, 69, 0, 69, 73, 61, 0, 77, 5, 0, 98, 82, 73, 38, 0, 73, 69, 0, 69, 73, 39, 0, 77, 5, 0, 98, 82, 73, 39, 0, 73, 69, 0, 69, 73, 40, 0, 77, 5, 0, 98, 82, 
					73, 40, 0, 73, 69, 0, 69, 73, 9, 0, 77, 5, 0, 98, 82, 73, 41, 0, 73, 69, 0, 69, 73, 26, 0, 77, 5, 0, 98, 82, 73, 42, 0, 73, 69, 0, 69, 73, 29, 0, 77, 5, 0, 98, 82, 73, 43, 0, 73, 69, 
					0, 69, 73, 30, 0, 77, 5, 0, 98, 82, 73, 44, 0, 73, 69, 0, 69, 73, 57, 0, 77, 5, 0, 98, 82, 73, 45, 0, 73, 69, 0, 69, 73, 36, 0, 77, 5, 0, 98, 82, 73, 46, 0, 73, 69, 0, 69, 73, 58, 0, 
					77, 5, 0, 98, 82, 73, 47, 0, 73, 69, 0, 69, 73, 37, 0, 77, 5, 0, 98, 82, 73, 48, 0, 73, 69, 0, 69, 73, 53, 0, 77, 5, 0, 98, 82, 73, 49, 0, 73, 69, 0, 69, 73, 59, 0, 77, 5, 0, 98, 82, 
					73, 50, 0, 73, 69, 0, 69, 73, 25, 0, 77, 5, 0, 98, 82, 73, 51, 0, 73, 69, 0, 69, 73, 12, 0, 77, 5, 0, 98, 82, 73, 52, 0, 73, 69, 0, 69, 73, 15, 0, 77, 5, 0, 98, 82, 73, 53, 0, 73, 69, 
					0, 69, 73, 17, 0, 77, 5, 0, 98, 82, 73, 54, 0, 73, 69, 0, 69, 73, 10, 0, 77, 5, 0, 98, 82, 73, 55, 0, 73, 69, 0, 69, 73, 13, 0, 77, 5, 0, 98, 82, 73, 56, 0, 73, 69, 0, 69, 73, 19, 0, 
					77, 5, 0, 98, 82, 73, 57, 0, 73, 69, 0, 69, 73, 16, 0, 77, 5, 0, 98, 82, 73, 58, 0, 73, 69, 0, 69, 73, 18, 0, 77, 5, 0, 98, 82, 73, 59, 0, 73, 69, 0, 69, 73, 11, 0, 77, 5, 0, 98, 82, 
					73, 60, 0, 73, 69, 0, 69, 73, 14, 0, 77, 5, 0, 98, 82, 73, 61, 0, 73, 69, 0, 69, 73, 20, 0, 77, 5, 0, 98, 82, 73, 62, 0, 73, 69, 0, 69, 73, 34, 0, 77, 5, 0, 98, 82, 73, 63, 0, 73, 70, 
					0, 69, 73, 69, 0, 77, 6, 0, 98, 82, 73, 64, 0, 73, 70, 0, 69, 73, 70, 0, 73, 69, 0, 77, 113, 0, 98, 68, 73, 0, 0, 66, 0, 73, 255, 255, 69, 73, 0, 0, 73, 1, 0, 69, 73, 1, 0, 73, 2, 0, 
					69, 73, 2, 0, 73, 3, 0, 69, 73, 3, 0, 73, 4, 0, 69, 73, 4, 0, 73, 5, 0, 69, 73, 5, 0, 73, 7, 0, 69, 73, 6, 0, 73, 8, 0, 69, 73, 7, 0, 73, 9, 0, 69, 73, 8, 0, 73, 10, 0, 69, 
					73, 9, 0, 73, 11, 0, 69, 73, 10, 0, 73, 12, 0, 69, 73, 11, 0, 73, 13, 0, 69, 73, 12, 0, 73, 14, 0, 69, 73, 13, 0, 73, 15, 0, 69, 73, 14, 0, 73, 16, 0, 69, 73, 15, 0, 73, 17, 0, 69, 73, 
					16, 0, 73, 20, 0, 69, 73, 17, 0, 73, 22, 0, 69, 73, 18, 0, 73, 24, 0, 69, 73, 19, 0, 73, 26, 0, 69, 73, 20, 0, 73, 28, 0, 69, 73, 21, 0, 73, 31, 0, 69, 73, 22, 0, 73, 42, 0, 69, 73, 23, 
					0, 73, 45, 0, 69, 73, 24, 0, 73, 48, 0, 69, 73, 25, 0, 73, 51, 0, 69, 73, 26, 0, 73, 58, 0, 69, 73, 27, 0, 73, 62, 0, 69, 73, 28, 0, 73, 82, 0, 69, 73, 29, 0, 73, 85, 0, 69, 73, 30, 0, 
					73, 87, 0, 69, 73, 31, 0, 73, 91, 0, 69, 73, 32, 0, 73, 93, 0, 69, 73, 33, 0, 73, 97, 0, 69, 73, 34, 0, 73, 99, 0, 69, 73, 35, 0, 73, 101, 0, 69, 77, 8, 0, 98, 68, 73, 1, 0, 66, 1, 73, 
					2, 0, 69, 73, 0, 0, 73, 1, 0, 69, 77, 5, 0, 98, 68, 73, 2, 0, 66, 1, 73, 8, 0, 69, 77, 5, 0, 98, 68, 73, 3, 0, 66, 1, 73, 12, 0, 69, 77, 5, 0, 98, 68, 73, 4, 0, 66, 1, 73, 22, 
					0, 69, 77, 8, 0, 98, 68, 73, 5, 0, 66, 1, 73, 33, 0, 69, 73, 36, 0, 73, 6, 0, 69, 77, 8, 0, 98, 68, 73, 6, 0, 66, 1, 73, 33, 0, 69, 73, 36, 0, 73, 6, 0, 69, 77, 5, 0, 98, 68, 73, 
					7, 0, 66, 1, 73, 36, 0, 69, 77, 5, 0, 98, 68, 73, 8, 0, 66, 1, 73, 37, 0, 69, 77, 5, 0, 98, 68, 73, 9, 0, 66, 1, 73, 38, 0, 69, 77, 5, 0, 98, 68, 73, 10, 0, 66, 1, 73, 41, 0, 69, 
					77, 5, 0, 98, 68, 73, 11, 0, 66, 1, 73, 48, 0, 69, 77, 5, 0, 98, 68, 73, 12, 0, 66, 1, 73, 57, 0, 69, 77, 5, 0, 98, 68, 73, 13, 0, 66, 1, 73, 58, 0, 69, 77, 5, 0, 98, 68, 73, 14, 0, 
					66, 1, 73, 59, 0, 69, 77, 5, 0, 98, 68, 73, 15, 0, 66, 1, 73, 60, 0, 69, 77, 5, 0, 98, 68, 73, 16, 0, 66, 1, 73, 61, 0, 69, 77, 11, 0, 98, 68, 73, 17, 0, 66, 0, 73, 255, 255, 69, 73, 37, 
					0, 73, 18, 0, 69, 73, 15, 0, 73, 19, 0, 69, 77, 11, 0, 98, 68, 73, 18, 0, 66, 0, 73, 255, 255, 69, 73, 37, 0, 73, 18, 0, 69, 73, 15, 0, 73, 19, 0, 69, 77, 5, 0, 98, 68, 73, 19, 0, 66, 1, 
					73, 65, 0, 69, 77, 8, 0, 98, 68, 73, 20, 0, 66, 1, 73, 48, 0, 69, 73, 9, 0, 73, 21, 0, 69, 77, 5, 0, 98, 68, 73, 21, 0, 66, 1, 73, 48, 0, 69, 77, 8, 0, 98, 68, 73, 22, 0, 66, 1, 73, 
					49, 0, 69, 73, 31, 0, 73, 23, 0, 69, 77, 5, 0, 98, 68, 73, 23, 0, 66, 1, 73, 50, 0, 69, 77, 8, 0, 98, 68, 73, 24, 0, 66, 1, 73, 56, 0, 69, 73, 18, 0, 73, 25, 0, 69, 77, 5, 0, 98, 68, 
					73, 25, 0, 66, 1, 73, 67, 0, 69, 77, 8, 0, 98, 68, 73, 26, 0, 66, 1, 73, 44, 0, 69, 73, 31, 0, 73, 27, 0, 69, 77, 5, 0, 98, 68, 73, 27, 0, 66, 1, 73, 45, 0, 69, 77, 11, 0, 98, 68, 73, 
					28, 0, 66, 1, 73, 10, 0, 69, 73, 20, 0, 73, 29, 0, 69, 73, 31, 0, 73, 30, 0, 69, 77, 5, 0, 98, 68, 73, 29, 0, 66, 1, 73, 7, 0, 69, 77, 5, 0, 98, 68, 73, 30, 0, 66, 1, 73, 11, 0, 69, 
					77, 11, 0, 98, 68, 73, 31, 0, 66, 0, 73, 255, 255, 69, 73, 38, 0, 73, 32, 0, 69, 73, 8, 0, 73, 34, 0, 69, 77, 8, 0, 98, 68, 73, 32, 0, 66, 0, 73, 255, 255, 69, 73, 21, 0, 73, 33, 0, 69, 77, 
					5, 0, 98, 68, 73, 33, 0, 66, 1, 73, 35, 0, 69, 77, 14, 0, 98, 68, 73, 34, 0, 66, 0, 73, 255, 255, 69, 73, 39, 0, 73, 35, 0, 69, 73, 40, 0, 73, 38, 0, 69, 73, 41, 0, 73, 40, 0, 69, 77, 8, 
					0, 98, 68, 73, 35, 0, 66, 0, 73, 255, 255, 69, 73, 42, 0, 73, 36, 0, 69, 77, 11, 0, 98, 68, 73, 36, 0, 66, 0, 73, 255, 255, 69, 73, 42, 0, 73, 36, 0, 69, 73, 21, 0, 73, 37, 0, 69, 77, 5, 0, 
					98, 68, 73, 37, 0, 66, 1, 73, 31, 0, 69, 77, 11, 0, 98, 68, 73, 38, 0, 66, 0, 73, 255, 255, 69, 73, 40, 0, 73, 38, 0, 69, 73, 21, 0, 73, 39, 0, 69, 77, 5, 0, 98, 68, 73, 39, 0, 66, 1, 73, 
					51, 0, 69, 77, 8, 0, 98, 68, 73, 40, 0, 66, 0, 73, 255, 255, 69, 73, 21, 0, 73, 41, 0, 69, 77, 5, 0, 98, 68, 73, 41, 0, 66, 1, 73, 62, 0, 69, 77, 11, 0, 98, 68, 73, 42, 0, 66, 1, 73, 46, 
					0, 69, 73, 26, 0, 73, 43, 0, 69, 73, 31, 0, 73, 44, 0, 69, 77, 5, 0, 98, 68, 73, 43, 0, 66, 1, 73, 3, 0, 69, 77, 5, 0, 98, 68, 73, 44, 0, 66, 1, 73, 47, 0, 69, 77, 11, 0, 98, 68, 73, 
					45, 0, 66, 1, 73, 54, 0, 69, 73, 31, 0, 73, 46, 0, 69, 73, 23, 0, 73, 47, 0, 69, 77, 5, 0, 98, 68, 73, 46, 0, 66, 1, 73, 6, 0, 69, 77, 5, 0, 98, 68, 73, 47, 0, 66, 1, 73, 55, 0, 69, 
					77, 11, 0, 98, 68, 73, 48, 0, 66, 1, 73, 42, 0, 69, 73, 24, 0, 73, 49, 0, 69, 73, 31, 0, 73, 50, 0, 69, 77, 5, 0, 98, 68, 73, 49, 0, 66, 1, 73, 43, 0, 69, 77, 5, 0, 98, 68, 73, 50, 0, 
					66, 1, 73, 66, 0, 69, 77, 8, 0, 98, 68, 73, 51, 0, 66, 1, 73, 25, 0, 69, 73, 43, 0, 73, 52, 0, 69, 77, 14, 0, 98, 68, 73, 52, 0, 66, 1, 73, 27, 0, 69, 73, 44, 0, 73, 53, 0, 69, 73, 45, 
					0, 73, 54, 0, 69, 73, 43, 0, 73, 52, 0, 69, 77, 5, 0, 98, 68, 73, 53, 0, 66, 1, 73, 27, 0, 69, 77, 11, 0, 98, 68, 73, 54, 0, 66, 0, 73, 255, 255, 69, 73, 46, 0, 73, 55, 0, 69, 73, 43, 0, 
					73, 56, 0, 69, 77, 8, 0, 98, 68, 73, 55, 0, 66, 0, 73, 255, 255, 69, 73, 43, 0, 73, 56, 0, 69, 77, 11, 0, 98, 68, 73, 56, 0, 66, 1, 73, 28, 0, 69, 73, 43, 0, 73, 56, 0, 69, 73, 44, 0, 73, 
					57, 0, 69, 77, 5, 0, 98, 68, 73, 57, 0, 66, 1, 73, 28, 0, 69, 77, 14, 0, 98, 68, 73, 58, 0, 66, 1, 73, 23, 0, 69, 73, 26, 0, 73, 59, 0, 69, 73, 22, 0, 73, 60, 0, 69, 73, 31, 0, 73, 61, 
					0, 69, 77, 5, 0, 98, 68, 73, 59, 0, 66, 1, 73, 4, 0, 69, 77, 5, 0, 98, 68, 73, 60, 0, 66, 1, 73, 5, 0, 69, 77, 5, 0, 98, 68, 73, 61, 0, 66, 1, 73, 24, 0, 69, 77, 26, 0, 98, 68, 73, 
					62, 0, 66, 1, 73, 64, 0, 69, 73, 44, 0, 73, 63, 0, 69, 73, 45, 0, 73, 64, 0, 69, 73, 47, 0, 73, 68, 0, 69, 73, 25, 0, 73, 69, 0, 69, 73, 40, 0, 73, 76, 0, 69, 73, 48, 0, 73, 78, 0, 69, 
					73, 49, 0, 73, 79, 0, 69, 77, 5, 0, 98, 68, 73, 63, 0, 66, 1, 73, 27, 0, 69, 77, 11, 0, 98, 68, 73, 64, 0, 66, 0, 73, 255, 255, 69, 73, 46, 0, 73, 65, 0, 69, 73, 43, 0, 73, 66, 0, 69, 77, 
					8, 0, 98, 68, 73, 65, 0, 66, 0, 73, 255, 255, 69, 73, 43, 0, 73, 66, 0, 69, 77, 11, 0, 98, 68, 73, 66, 0, 66, 1, 73, 28, 0, 69, 73, 43, 0, 73, 66, 0, 69, 73, 44, 0, 73, 67, 0, 69, 77, 5, 
					0, 98, 68, 73, 67, 0, 66, 1, 73, 28, 0, 69, 77, 5, 0, 98, 68, 73, 68, 0, 66, 1, 73, 64, 0, 69, 77, 14, 0, 98, 68, 73, 69, 0, 66, 1, 73, 27, 0, 69, 73, 44, 0, 73, 70, 0, 69, 73, 45, 0, 
					73, 71, 0, 69, 73, 43, 0, 73, 75, 0, 69, 77, 5, 0, 98, 68, 73, 70, 0, 66, 1, 73, 27, 0, 69, 77, 11, 0, 98, 68, 73, 71, 0, 66, 0, 73, 255, 255, 69, 73, 46, 0, 73, 72, 0, 69, 73, 43, 0, 73, 
					73, 0, 69, 77, 8, 0, 98, 68, 73, 72, 0, 66, 0, 73, 255, 255, 69, 73, 43, 0, 73, 73, 0, 69, 77, 11, 0, 98, 68, 73, 73, 0, 66, 1, 73, 28, 0, 69, 73, 43, 0, 73, 73, 0, 69, 73, 44, 0, 73, 74, 
					0, 69, 77, 5, 0, 98, 68, 73, 74, 0, 66, 1, 73, 28, 0, 69, 77, 14, 0, 98, 68, 73, 75, 0, 66, 1, 73, 27, 0, 69, 73, 44, 0, 73, 70, 0, 69, 73, 45, 0, 73, 71, 0, 69, 73, 43, 0, 73, 75, 0, 
					69, 77, 23, 0, 98, 68, 73, 76, 0, 66, 1, 73, 52, 0, 69, 73, 44, 0, 73, 63, 0, 69, 73, 45, 0, 73, 64, 0, 69, 73, 47, 0, 73, 77, 0, 69, 73, 25, 0, 73, 69, 0, 69, 73, 40, 0, 73, 76, 0, 69, 
					73, 48, 0, 73, 78, 0, 69, 77, 5, 0, 98, 68, 73, 77, 0, 66, 1, 73, 52, 0, 69, 77, 17, 0, 98, 68, 73, 78, 0, 66, 0, 73, 255, 255, 69, 73, 44, 0, 73, 63, 0, 69, 73, 45, 0, 73, 64, 0, 69, 73, 
					25, 0, 73, 69, 0, 69, 73, 43, 0, 73, 78, 0, 69, 77, 8, 0, 98, 68, 73, 79, 0, 66, 0, 73, 255, 255, 69, 73, 42, 0, 73, 80, 0, 69, 77, 11, 0, 98, 68, 73, 80, 0, 66, 1, 73, 32, 0, 69, 73, 42, 
					0, 73, 80, 0, 69, 73, 47, 0, 73, 81, 0, 69, 77, 5, 0, 98, 68, 73, 81, 0, 66, 1, 73, 32, 0, 69, 77, 20, 0, 98, 68, 73, 82, 0, 66, 1, 73, 63, 0, 69, 73, 44, 0, 73, 63, 0, 69, 73, 45, 0, 
					73, 64, 0, 69, 73, 47, 0, 73, 83, 0, 69, 73, 25, 0, 73, 69, 0, 69, 73, 43, 0, 73, 84, 0, 69, 77, 5, 0, 98, 68, 73, 83, 0, 66, 1, 73, 63, 0, 69, 77, 20, 0, 98, 68, 73, 84, 0, 66, 1, 73, 
					63, 0, 69, 73, 44, 0, 73, 63, 0, 69, 73, 45, 0, 73, 64, 0, 69, 73, 47, 0, 73, 83, 0, 69, 73, 25, 0, 73, 69, 0, 69, 73, 43, 0, 73, 84, 0, 69, 77, 8, 0, 98, 68, 73, 85, 0, 66, 1, 73, 21, 
					0, 69, 73, 29, 0, 73, 86, 0, 69, 77, 5, 0, 98, 68, 73, 86, 0, 66, 1, 73, 34, 0, 69, 77, 11, 0, 98, 68, 73, 87, 0, 66, 1, 73, 39, 0, 69, 73, 31, 0, 73, 88, 0, 69, 73, 30, 0, 73, 89, 0, 
					69, 77, 5, 0, 98, 68, 73, 88, 0, 66, 1, 73, 40, 0, 69, 77, 8, 0, 98, 68, 73, 89, 0, 66, 1, 73, 15, 0, 69, 73, 31, 0, 73, 90, 0, 69, 77, 5, 0, 98, 68, 73, 90, 0, 66, 1, 73, 16, 0, 69, 
					77, 8, 0, 98, 68, 73, 91, 0, 66, 1, 73, 9, 0, 69, 73, 31, 0, 73, 92, 0, 69, 77, 5, 0, 98, 68, 73, 92, 0, 66, 1, 73, 26, 0, 69, 77, 11, 0, 98, 68, 73, 93, 0, 66, 1, 73, 29, 0, 69, 73, 
					31, 0, 73, 94, 0, 69, 73, 32, 0, 73, 95, 0, 69, 77, 5, 0, 98, 68, 73, 94, 0, 66, 1, 73, 30, 0, 69, 77, 8, 0, 98, 68, 73, 95, 0, 66, 1, 73, 17, 0, 69, 73, 31, 0, 73, 96, 0, 69, 77, 5, 
					0, 98, 68, 73, 96, 0, 66, 1, 73, 18, 0, 69, 77, 11, 0, 98, 68, 73, 97, 0, 66, 1, 73, 33, 0, 69, 73, 50, 0, 73, 6, 0, 69, 73, 51, 0, 73, 98, 0, 69, 77, 8, 0, 98, 68, 73, 98, 0, 66, 1, 
					73, 68, 0, 69, 73, 36, 0, 73, 6, 0, 69, 77, 8, 0, 98, 68, 73, 99, 0, 66, 1, 73, 19, 0, 69, 73, 31, 0, 73, 100, 0, 69, 77, 5, 0, 98, 68, 73, 100, 0, 66, 1, 73, 20, 0, 69, 77, 11, 0, 98, 
					68, 73, 101, 0, 66, 1, 73, 13, 0, 69, 73, 31, 0, 73, 102, 0, 69, 73, 35, 0, 73, 103, 0, 69, 77, 5, 0, 98, 68, 73, 102, 0, 66, 1, 73, 14, 0, 69, 77, 5, 0, 98, 68, 73, 103, 0, 66, 1, 73, 53, 
					0, 69, 77, 7, 1, 98, 76, 73, 0, 0, 69, 73, 6, 0, 73, 1, 0, 73, 1, 0, 69, 73, 7, 0, 73, 1, 0, 73, 2, 0, 69, 73, 8, 0, 73, 1, 0, 73, 3, 0, 69, 73, 9, 0, 73, 1, 0, 73, 4, 0, 
					69, 73, 10, 0, 73, 1, 0, 73, 5, 0, 69, 73, 11, 0, 73, 1, 0, 73, 6, 0, 69, 73, 12, 0, 73, 1, 0, 73, 7, 0, 69, 73, 13, 0, 73, 1, 0, 73, 8, 0, 69, 73, 14, 0, 73, 1, 0, 73, 9, 0, 
					69, 73, 15, 0, 73, 1, 0, 73, 10, 0, 69, 73, 16, 0, 73, 1, 0, 73, 11, 0, 69, 73, 17, 0, 73, 1, 0, 73, 12, 0, 69, 73, 18, 0, 73, 1, 0, 73, 13, 0, 69, 73, 19, 0, 73, 1, 0, 73, 14, 0, 
					69, 73, 20, 0, 73, 1, 0, 73, 15, 0, 69, 73, 21, 0, 73, 1, 0, 73, 16, 0, 69, 73, 22, 0, 73, 1, 0, 73, 17, 0, 69, 73, 23, 0, 73, 1, 0, 73, 18, 0, 69, 73, 24, 0, 73, 1, 0, 73, 19, 0, 
					69, 73, 25, 0, 73, 1, 0, 73, 20, 0, 69, 73, 26, 0, 73, 1, 0, 73, 21, 0, 69, 73, 27, 0, 73, 1, 0, 73, 22, 0, 69, 73, 28, 0, 73, 1, 0, 73, 23, 0, 69, 73, 29, 0, 73, 1, 0, 73, 24, 0, 
					69, 73, 30, 0, 73, 1, 0, 73, 25, 0, 69, 73, 31, 0, 73, 1, 0, 73, 26, 0, 69, 73, 32, 0, 73, 1, 0, 73, 27, 0, 69, 73, 33, 0, 73, 1, 0, 73, 28, 0, 69, 73, 34, 0, 73, 1, 0, 73, 29, 0, 
					69, 73, 35, 0, 73, 1, 0, 73, 30, 0, 69, 73, 36, 0, 73, 1, 0, 73, 31, 0, 69, 73, 37, 0, 73, 1, 0, 73, 32, 0, 69, 73, 38, 0, 73, 1, 0, 73, 33, 0, 69, 73, 39, 0, 73, 1, 0, 73, 34, 0, 
					69, 73, 40, 0, 73, 1, 0, 73, 35, 0, 69, 73, 41, 0, 73, 1, 0, 73, 36, 0, 69, 73, 42, 0, 73, 1, 0, 73, 37, 0, 69, 73, 43, 0, 73, 1, 0, 73, 38, 0, 69, 73, 44, 0, 73, 1, 0, 73, 39, 0, 
					69, 73, 45, 0, 73, 1, 0, 73, 40, 0, 69, 73, 46, 0, 73, 1, 0, 73, 41, 0, 69, 73, 47, 0, 73, 1, 0, 73, 42, 0, 69, 73, 48, 0, 73, 1, 0, 73, 43, 0, 69, 73, 49, 0, 73, 1, 0, 73, 44, 0, 
					69, 73, 50, 0, 73, 1, 0, 73, 45, 0, 69, 73, 51, 0, 73, 1, 0, 73, 46, 0, 69, 73, 52, 0, 73, 1, 0, 73, 47, 0, 69, 73, 53, 0, 73, 1, 0, 73, 48, 0, 69, 73, 54, 0, 73, 1, 0, 73, 49, 0, 
					69, 73, 55, 0, 73, 1, 0, 73, 50, 0, 69, 73, 56, 0, 73, 1, 0, 73, 51, 0, 69, 73, 57, 0, 73, 1, 0, 73, 52, 0, 69, 73, 58, 0, 73, 1, 0, 73, 53, 0, 69, 73, 59, 0, 73, 1, 0, 73, 54, 0, 
					69, 73, 60, 0, 73, 1, 0, 73, 55, 0, 69, 73, 61, 0, 73, 1, 0, 73, 56, 0, 69, 73, 62, 0, 73, 1, 0, 73, 57, 0, 69, 73, 63, 0, 73, 1, 0, 73, 58, 0, 69, 73, 64, 0, 73, 1, 0, 73, 59, 0, 
					69, 73, 65, 0, 73, 1, 0, 73, 60, 0, 69, 73, 66, 0, 73, 1, 0, 73, 61, 0, 69, 73, 67, 0, 73, 1, 0, 73, 62, 0, 69, 73, 68, 0, 73, 1, 0, 73, 63, 0, 69, 73, 69, 0, 73, 3, 0, 73, 64, 0, 
					69, 73, 70, 0, 73, 3, 0, 73, 65, 0, 69, 77, 3, 1, 98, 76, 73, 1, 0, 69, 73, 0, 0, 73, 2, 0, 73, 27, 0, 69, 73, 6, 0, 73, 2, 0, 73, 27, 0, 69, 73, 7, 0, 73, 2, 0, 73, 27, 0, 69, 
					73, 8, 0, 73, 2, 0, 73, 27, 0, 69, 73, 9, 0, 73, 2, 0, 73, 27, 0, 69, 73, 10, 0, 73, 2, 0, 73, 27, 0, 69, 73, 11, 0, 73, 2, 0, 73, 27, 0, 69, 73, 12, 0, 73, 2, 0, 73, 27, 0, 69, 
					73, 13, 0, 73, 2, 0, 73, 27, 0, 69, 73, 14, 0, 73, 2, 0, 73, 27, 0, 69, 73, 15, 0, 73, 2, 0, 73, 27, 0, 69, 73, 16, 0, 73, 2, 0, 73, 27, 0, 69, 73, 17, 0, 73, 2, 0, 73, 27, 0, 69, 
					73, 18, 0, 73, 2, 0, 73, 27, 0, 69, 73, 19, 0, 73, 2, 0, 73, 27, 0, 69, 73, 20, 0, 73, 2, 0, 73, 27, 0, 69, 73, 21, 0, 73, 2, 0, 73, 27, 0, 69, 73, 22, 0, 73, 2, 0, 73, 27, 0, 69, 
					73, 23, 0, 73, 2, 0, 73, 27, 0, 69, 73, 24, 0, 73, 2, 0, 73, 27, 0, 69, 73, 25, 0, 73, 2, 0, 73, 27, 0, 69, 73, 26, 0, 73, 2, 0, 73, 27, 0, 69, 73, 27, 0, 73, 2, 0, 73, 27, 0, 69, 
					73, 28, 0, 73, 2, 0, 73, 27, 0, 69, 73, 29, 0, 73, 2, 0, 73, 27, 0, 69, 73, 30, 0, 73, 2, 0, 73, 27, 0, 69, 73, 31, 0, 73, 2, 0, 73, 27, 0, 69, 73, 32, 0, 73, 2, 0, 73, 27, 0, 69, 
					73, 33, 0, 73, 2, 0, 73, 27, 0, 69, 73, 34, 0, 73, 2, 0, 73, 27, 0, 69, 73, 35, 0, 73, 2, 0, 73, 27, 0, 69, 73, 36, 0, 73, 2, 0, 73, 27, 0, 69, 73, 37, 0, 73, 2, 0, 73, 27, 0, 69, 
					73, 38, 0, 73, 2, 0, 73, 27, 0, 69, 73, 39, 0, 73, 2, 0, 73, 27, 0, 69, 73, 40, 0, 73, 2, 0, 73, 27, 0, 69, 73, 41, 0, 73, 2, 0, 73, 27, 0, 69, 73, 42, 0, 73, 2, 0, 73, 27, 0, 69, 
					73, 43, 0, 73, 2, 0, 73, 27, 0, 69, 73, 44, 0, 73, 2, 0, 73, 27, 0, 69, 73, 45, 0, 73, 2, 0, 73, 27, 0, 69, 73, 46, 0, 73, 2, 0, 73, 27, 0, 69, 73, 47, 0, 73, 2, 0, 73, 27, 0, 69, 
					73, 48, 0, 73, 2, 0, 73, 27, 0, 69, 73, 49, 0, 73, 2, 0, 73, 27, 0, 69, 73, 50, 0, 73, 2, 0, 73, 27, 0, 69, 73, 51, 0, 73, 2, 0, 73, 27, 0, 69, 73, 52, 0, 73, 2, 0, 73, 27, 0, 69, 
					73, 53, 0, 73, 2, 0, 73, 27, 0, 69, 73, 54, 0, 73, 2, 0, 73, 27, 0, 69, 73, 55, 0, 73, 2, 0, 73, 27, 0, 69, 73, 56, 0, 73, 2, 0, 73, 27, 0, 69, 73, 57, 0, 73, 2, 0, 73, 27, 0, 69, 
					73, 58, 0, 73, 2, 0, 73, 27, 0, 69, 73, 59, 0, 73, 2, 0, 73, 27, 0, 69, 73, 60, 0, 73, 2, 0, 73, 27, 0, 69, 73, 61, 0, 73, 2, 0, 73, 27, 0, 69, 73, 62, 0, 73, 2, 0, 73, 27, 0, 69, 
					73, 63, 0, 73, 2, 0, 73, 27, 0, 69, 73, 64, 0, 73, 2, 0, 73, 27, 0, 69, 73, 65, 0, 73, 2, 0, 73, 27, 0, 69, 73, 66, 0, 73, 2, 0, 73, 27, 0, 69, 73, 67, 0, 73, 2, 0, 73, 27, 0, 69, 
					73, 68, 0, 73, 2, 0, 73, 27, 0, 69, 77, 3, 1, 98, 76, 73, 2, 0, 69, 73, 0, 0, 73, 2, 0, 73, 20, 0, 69, 73, 6, 0, 73, 2, 0, 73, 20, 0, 69, 73, 7, 0, 73, 2, 0, 73, 20, 0, 69, 73, 
					8, 0, 73, 2, 0, 73, 20, 0, 69, 73, 9, 0, 73, 2, 0, 73, 20, 0, 69, 73, 10, 0, 73, 2, 0, 73, 20, 0, 69, 73, 11, 0, 73, 2, 0, 73, 20, 0, 69, 73, 12, 0, 73, 2, 0, 73, 20, 0, 69, 73, 
					13, 0, 73, 2, 0, 73, 20, 0, 69, 73, 14, 0, 73, 2, 0, 73, 20, 0, 69, 73, 15, 0, 73, 2, 0, 73, 20, 0, 69, 73, 16, 0, 73, 2, 0, 73, 20, 0, 69, 73, 17, 0, 73, 2, 0, 73, 20, 0, 69, 73, 
					18, 0, 73, 2, 0, 73, 20, 0, 69, 73, 19, 0, 73, 2, 0, 73, 20, 0, 69, 73, 20, 0, 73, 2, 0, 73, 20, 0, 69, 73, 21, 0, 73, 2, 0, 73, 20, 0, 69, 73, 22, 0, 73, 2, 0, 73, 20, 0, 69, 73, 
					23, 0, 73, 2, 0, 73, 20, 0, 69, 73, 24, 0, 73, 2, 0, 73, 20, 0, 69, 73, 25, 0, 73, 2, 0, 73, 20, 0, 69, 73, 26, 0, 73, 2, 0, 73, 20, 0, 69, 73, 27, 0, 73, 2, 0, 73, 20, 0, 69, 73, 
					28, 0, 73, 2, 0, 73, 20, 0, 69, 73, 29, 0, 73, 2, 0, 73, 20, 0, 69, 73, 30, 0, 73, 2, 0, 73, 20, 0, 69, 73, 31, 0, 73, 2, 0, 73, 20, 0, 69, 73, 32, 0, 73, 2, 0, 73, 20, 0, 69, 73, 
					33, 0, 73, 2, 0, 73, 20, 0, 69, 73, 34, 0, 73, 2, 0, 73, 20, 0, 69, 73, 35, 0, 73, 2, 0, 73, 20, 0, 69, 73, 36, 0, 73, 2, 0, 73, 20, 0, 69, 73, 37, 0, 73, 2, 0, 73, 20, 0, 69, 73, 
					38, 0, 73, 2, 0, 73, 20, 0, 69, 73, 39, 0, 73, 2, 0, 73, 20, 0, 69, 73, 40, 0, 73, 2, 0, 73, 20, 0, 69, 73, 41, 0, 73, 2, 0, 73, 20, 0, 69, 73, 42, 0, 73, 2, 0, 73, 20, 0, 69, 73, 
					43, 0, 73, 2, 0, 73, 20, 0, 69, 73, 44, 0, 73, 2, 0, 73, 20, 0, 69, 73, 45, 0, 73, 2, 0, 73, 20, 0, 69, 73, 46, 0, 73, 2, 0, 73, 20, 0, 69, 73, 47, 0, 73, 2, 0, 73, 20, 0, 69, 73, 
					48, 0, 73, 2, 0, 73, 20, 0, 69, 73, 49, 0, 73, 2, 0, 73, 20, 0, 69, 73, 50, 0, 73, 2, 0, 73, 20, 0, 69, 73, 51, 0, 73, 2, 0, 73, 20, 0, 69, 73, 52, 0, 73, 2, 0, 73, 20, 0, 69, 73, 
					53, 0, 73, 2, 0, 73, 20, 0, 69, 73, 54, 0, 73, 2, 0, 73, 20, 0, 69, 73, 55, 0, 73, 2, 0, 73, 20, 0, 69, 73, 56, 0, 73, 2, 0, 73, 20, 0, 69, 73, 57, 0, 73, 2, 0, 73, 20, 0, 69, 73, 
					58, 0, 73, 2, 0, 73, 20, 0, 69, 73, 59, 0, 73, 2, 0, 73, 20, 0, 69, 73, 60, 0, 73, 2, 0, 73, 20, 0, 69, 73, 61, 0, 73, 2, 0, 73, 20, 0, 69, 73, 62, 0, 73, 2, 0, 73, 20, 0, 69, 73, 
					63, 0, 73, 2, 0, 73, 20, 0, 69, 73, 64, 0, 73, 2, 0, 73, 20, 0, 69, 73, 65, 0, 73, 2, 0, 73, 20, 0, 69, 73, 66, 0, 73, 2, 0, 73, 20, 0, 69, 73, 67, 0, 73, 2, 0, 73, 20, 0, 69, 73, 
					68, 0, 73, 2, 0, 73, 20, 0, 69, 77, 3, 1, 98, 76, 73, 3, 0, 69, 73, 0, 0, 73, 2, 0, 73, 17, 0, 69, 73, 6, 0, 73, 2, 0, 73, 17, 0, 69, 73, 7, 0, 73, 2, 0, 73, 17, 0, 69, 73, 8, 
					0, 73, 2, 0, 73, 17, 0, 69, 73, 9, 0, 73, 2, 0, 73, 17, 0, 69, 73, 10, 0, 73, 2, 0, 73, 17, 0, 69, 73, 11, 0, 73, 2, 0, 73, 17, 0, 69, 73, 12, 0, 73, 2, 0, 73, 17, 0, 69, 73, 13, 
					0, 73, 2, 0, 73, 17, 0, 69, 73, 14, 0, 73, 2, 0, 73, 17, 0, 69, 73, 15, 0, 73, 2, 0, 73, 17, 0, 69, 73, 16, 0, 73, 2, 0, 73, 17, 0, 69, 73, 17, 0, 73, 2, 0, 73, 17, 0, 69, 73, 18, 
					0, 73, 2, 0, 73, 17, 0, 69, 73, 19, 0, 73, 2, 0, 73, 17, 0, 69, 73, 20, 0, 73, 2, 0, 73, 17, 0, 69, 73, 21, 0, 73, 2, 0, 73, 17, 0, 69, 73, 22, 0, 73, 2, 0, 73, 17, 0, 69, 73, 23, 
					0, 73, 2, 0, 73, 17, 0, 69, 73, 24, 0, 73, 2, 0, 73, 17, 0, 69, 73, 25, 0, 73, 2, 0, 73, 17, 0, 69, 73, 26, 0, 73, 2, 0, 73, 17, 0, 69, 73, 27, 0, 73, 2, 0, 73, 17, 0, 69, 73, 28, 
					0, 73, 2, 0, 73, 17, 0, 69, 73, 29, 0, 73, 2, 0, 73, 17, 0, 69, 73, 30, 0, 73, 2, 0, 73, 17, 0, 69, 73, 31, 0, 73, 2, 0, 73, 17, 0, 69, 73, 32, 0, 73, 2, 0, 73, 17, 0, 69, 73, 33, 
					0, 73, 2, 0, 73, 17, 0, 69, 73, 34, 0, 73, 2, 0, 73, 17, 0, 69, 73, 35, 0, 73, 2, 0, 73, 17, 0, 69, 73, 36, 0, 73, 2, 0, 73, 17, 0, 69, 73, 37, 0, 73, 2, 0, 73, 17, 0, 69, 73, 38, 
					0, 73, 2, 0, 73, 17, 0, 69, 73, 39, 0, 73, 2, 0, 73, 17, 0, 69, 73, 40, 0, 73, 2, 0, 73, 17, 0, 69, 73, 41, 0, 73, 2, 0, 73, 17, 0, 69, 73, 42, 0, 73, 2, 0, 73, 17, 0, 69, 73, 43, 
					0, 73, 2, 0, 73, 17, 0, 69, 73, 44, 0, 73, 2, 0, 73, 17, 0, 69, 73, 45, 0, 73, 2, 0, 73, 17, 0, 69, 73, 46, 0, 73, 2, 0, 73, 17, 0, 69, 73, 47, 0, 73, 2, 0, 73, 17, 0, 69, 73, 48, 
					0, 73, 2, 0, 73, 17, 0, 69, 73, 49, 0, 73, 2, 0, 73, 17, 0, 69, 73, 50, 0, 73, 2, 0, 73, 17, 0, 69, 73, 51, 0, 73, 2, 0, 73, 17, 0, 69, 73, 52, 0, 73, 2, 0, 73, 17, 0, 69, 73, 53, 
					0, 73, 2, 0, 73, 17, 0, 69, 73, 54, 0, 73, 2, 0, 73, 17, 0, 69, 73, 55, 0, 73, 2, 0, 73, 17, 0, 69, 73, 56, 0, 73, 2, 0, 73, 17, 0, 69, 73, 57, 0, 73, 2, 0, 73, 17, 0, 69, 73, 58, 
					0, 73, 2, 0, 73, 17, 0, 69, 73, 59, 0, 73, 2, 0, 73, 17, 0, 69, 73, 60, 0, 73, 2, 0, 73, 17, 0, 69, 73, 61, 0, 73, 2, 0, 73, 17, 0, 69, 73, 62, 0, 73, 2, 0, 73, 17, 0, 69, 73, 63, 
					0, 73, 2, 0, 73, 17, 0, 69, 73, 64, 0, 73, 2, 0, 73, 17, 0, 69, 73, 65, 0, 73, 2, 0, 73, 17, 0, 69, 73, 66, 0, 73, 2, 0, 73, 17, 0, 69, 73, 67, 0, 73, 2, 0, 73, 17, 0, 69, 73, 68, 
					0, 73, 2, 0, 73, 17, 0, 69, 77, 3, 1, 98, 76, 73, 4, 0, 69, 73, 0, 0, 73, 2, 0, 73, 40, 0, 69, 73, 6, 0, 73, 2, 0, 73, 40, 0, 69, 73, 7, 0, 73, 2, 0, 73, 40, 0, 69, 73, 8, 0, 
					73, 2, 0, 73, 40, 0, 69, 73, 9, 0, 73, 2, 0, 73, 40, 0, 69, 73, 10, 0, 73, 2, 0, 73, 40, 0, 69, 73, 11, 0, 73, 2, 0, 73, 40, 0, 69, 73, 12, 0, 73, 2, 0, 73, 40, 0, 69, 73, 13, 0, 
					73, 2, 0, 73, 40, 0, 69, 73, 14, 0, 73, 2, 0, 73, 40, 0, 69, 73, 15, 0, 73, 2, 0, 73, 40, 0, 69, 73, 16, 0, 73, 2, 0, 73, 40, 0, 69, 73, 17, 0, 73, 2, 0, 73, 40, 0, 69, 73, 18, 0, 
					73, 2, 0, 73, 40, 0, 69, 73, 19, 0, 73, 2, 0, 73, 40, 0, 69, 73, 20, 0, 73, 2, 0, 73, 40, 0, 69, 73, 21, 0, 73, 2, 0, 73, 40, 0, 69, 73, 22, 0, 73, 2, 0, 73, 40, 0, 69, 73, 23, 0, 
					73, 2, 0, 73, 40, 0, 69, 73, 24, 0, 73, 2, 0, 73, 40, 0, 69, 73, 25, 0, 73, 2, 0, 73, 40, 0, 69, 73, 26, 0, 73, 2, 0, 73, 40, 0, 69, 73, 27, 0, 73, 2, 0, 73, 40, 0, 69, 73, 28, 0, 
					73, 2, 0, 73, 40, 0, 69, 73, 29, 0, 73, 2, 0, 73, 40, 0, 69, 73, 30, 0, 73, 2, 0, 73, 40, 0, 69, 73, 31, 0, 73, 2, 0, 73, 40, 0, 69, 73, 32, 0, 73, 2, 0, 73, 40, 0, 69, 73, 33, 0, 
					73, 2, 0, 73, 40, 0, 69, 73, 34, 0, 73, 2, 0, 73, 40, 0, 69, 73, 35, 0, 73, 2, 0, 73, 40, 0, 69, 73, 36, 0, 73, 2, 0, 73, 40, 0, 69, 73, 37, 0, 73, 2, 0, 73, 40, 0, 69, 73, 38, 0, 
					73, 2, 0, 73, 40, 0, 69, 73, 39, 0, 73, 2, 0, 73, 40, 0, 69, 73, 40, 0, 73, 2, 0, 73, 40, 0, 69, 73, 41, 0, 73, 2, 0, 73, 40, 0, 69, 73, 42, 0, 73, 2, 0, 73, 40, 0, 69, 73, 43, 0, 
					73, 2, 0, 73, 40, 0, 69, 73, 44, 0, 73, 2, 0, 73, 40, 0, 69, 73, 45, 0, 73, 2, 0, 73, 40, 0, 69, 73, 46, 0, 73, 2, 0, 73, 40, 0, 69, 73, 47, 0, 73, 2, 0, 73, 40, 0, 69, 73, 48, 0, 
					73, 2, 0, 73, 40, 0, 69, 73, 49, 0, 73, 2, 0, 73, 40, 0, 69, 73, 50, 0, 73, 2, 0, 73, 40, 0, 69, 73, 51, 0, 73, 2, 0, 73, 40, 0, 69, 73, 52, 0, 73, 2, 0, 73, 40, 0, 69, 73, 53, 0, 
					73, 2, 0, 73, 40, 0, 69, 73, 54, 0, 73, 2, 0, 73, 40, 0, 69, 73, 55, 0, 73, 2, 0, 73, 40, 0, 69, 73, 56, 0, 73, 2, 0, 73, 40, 0, 69, 73, 57, 0, 73, 2, 0, 73, 40, 0, 69, 73, 58, 0, 
					73, 2, 0, 73, 40, 0, 69, 73, 59, 0, 73, 2, 0, 73, 40, 0, 69, 73, 60, 0, 73, 2, 0, 73, 40, 0, 69, 73, 61, 0, 73, 2, 0, 73, 40, 0, 69, 73, 62, 0, 73, 2, 0, 73, 40, 0, 69, 73, 63, 0, 
					73, 2, 0, 73, 40, 0, 69, 73, 64, 0, 73, 2, 0, 73, 40, 0, 69, 73, 65, 0, 73, 2, 0, 73, 40, 0, 69, 73, 66, 0, 73, 2, 0, 73, 40, 0, 69, 73, 67, 0, 73, 2, 0, 73, 40, 0, 69, 73, 68, 0, 
					73, 2, 0, 73, 40, 0, 69, 77, 3, 1, 98, 76, 73, 5, 0, 69, 73, 0, 0, 73, 2, 0, 73, 54, 0, 69, 73, 6, 0, 73, 2, 0, 73, 54, 0, 69, 73, 7, 0, 73, 2, 0, 73, 54, 0, 69, 73, 8, 0, 73, 
					2, 0, 73, 54, 0, 69, 73, 9, 0, 73, 2, 0, 73, 54, 0, 69, 73, 10, 0, 73, 2, 0, 73, 54, 0, 69, 73, 11, 0, 73, 2, 0, 73, 54, 0, 69, 73, 12, 0, 73, 2, 0, 73, 54, 0, 69, 73, 13, 0, 73, 
					2, 0, 73, 54, 0, 69, 73, 14, 0, 73, 2, 0, 73, 54, 0, 69, 73, 15, 0, 73, 2, 0, 73, 54, 0, 69, 73, 16, 0, 73, 2, 0, 73, 54, 0, 69, 73, 17, 0, 73, 2, 0, 73, 54, 0, 69, 73, 18, 0, 73, 
					2, 0, 73, 54, 0, 69, 73, 19, 0, 73, 2, 0, 73, 54, 0, 69, 73, 20, 0, 73, 2, 0, 73, 54, 0, 69, 73, 21, 0, 73, 2, 0, 73, 54, 0, 69, 73, 22, 0, 73, 2, 0, 73, 54, 0, 69, 73, 23, 0, 73, 
					2, 0, 73, 54, 0, 69, 73, 24, 0, 73, 2, 0, 73, 54, 0, 69, 73, 25, 0, 73, 2, 0, 73, 54, 0, 69, 73, 26, 0, 73, 2, 0, 73, 54, 0, 69, 73, 27, 0, 73, 2, 0, 73, 54, 0, 69, 73, 28, 0, 73, 
					2, 0, 73, 54, 0, 69, 73, 29, 0, 73, 2, 0, 73, 54, 0, 69, 73, 30, 0, 73, 2, 0, 73, 54, 0, 69, 73, 31, 0, 73, 2, 0, 73, 54, 0, 69, 73, 32, 0, 73, 2, 0, 73, 54, 0, 69, 73, 33, 0, 73, 
					2, 0, 73, 54, 0, 69, 73, 34, 0, 73, 2, 0, 73, 54, 0, 69, 73, 35, 0, 73, 2, 0, 73, 54, 0, 69, 73, 36, 0, 73, 2, 0, 73, 54, 0, 69, 73, 37, 0, 73, 2, 0, 73, 54, 0, 69, 73, 38, 0, 73, 
					2, 0, 73, 54, 0, 69, 73, 39, 0, 73, 2, 0, 73, 54, 0, 69, 73, 40, 0, 73, 2, 0, 73, 54, 0, 69, 73, 41, 0, 73, 2, 0, 73, 54, 0, 69, 73, 42, 0, 73, 2, 0, 73, 54, 0, 69, 73, 43, 0, 73, 
					2, 0, 73, 54, 0, 69, 73, 44, 0, 73, 2, 0, 73, 54, 0, 69, 73, 45, 0, 73, 2, 0, 73, 54, 0, 69, 73, 46, 0, 73, 2, 0, 73, 54, 0, 69, 73, 47, 0, 73, 2, 0, 73, 54, 0, 69, 73, 48, 0, 73, 
					2, 0, 73, 54, 0, 69, 73, 49, 0, 73, 2, 0, 73, 54, 0, 69, 73, 50, 0, 73, 2, 0, 73, 54, 0, 69, 73, 51, 0, 73, 2, 0, 73, 54, 0, 69, 73, 52, 0, 73, 2, 0, 73, 54, 0, 69, 73, 53, 0, 73, 
					2, 0, 73, 54, 0, 69, 73, 54, 0, 73, 2, 0, 73, 54, 0, 69, 73, 55, 0, 73, 2, 0, 73, 54, 0, 69, 73, 56, 0, 73, 2, 0, 73, 54, 0, 69, 73, 57, 0, 73, 2, 0, 73, 54, 0, 69, 73, 58, 0, 73, 
					2, 0, 73, 54, 0, 69, 73, 59, 0, 73, 2, 0, 73, 54, 0, 69, 73, 60, 0, 73, 2, 0, 73, 54, 0, 69, 73, 61, 0, 73, 2, 0, 73, 54, 0, 69, 73, 62, 0, 73, 2, 0, 73, 54, 0, 69, 73, 63, 0, 73, 
					2, 0, 73, 54, 0, 69, 73, 64, 0, 73, 2, 0, 73, 54, 0, 69, 73, 65, 0, 73, 2, 0, 73, 54, 0, 69, 73, 66, 0, 73, 2, 0, 73, 54, 0, 69, 73, 67, 0, 73, 2, 0, 73, 54, 0, 69, 73, 68, 0, 73, 
					2, 0, 73, 54, 0, 69, 77, 3, 1, 98, 76, 73, 6, 0, 69, 73, 0, 0, 73, 2, 0, 73, 59, 0, 69, 73, 6, 0, 73, 2, 0, 73, 59, 0, 69, 73, 7, 0, 73, 2, 0, 73, 59, 0, 69, 73, 8, 0, 73, 2, 
					0, 73, 59, 0, 69, 73, 9, 0, 73, 2, 0, 73, 59, 0, 69, 73, 10, 0, 73, 2, 0, 73, 59, 0, 69, 73, 11, 0, 73, 2, 0, 73, 59, 0, 69, 73, 12, 0, 73, 2, 0, 73, 59, 0, 69, 73, 13, 0, 73, 2, 
					0, 73, 59, 0, 69, 73, 14, 0, 73, 2, 0, 73, 59, 0, 69, 73, 15, 0, 73, 2, 0, 73, 59, 0, 69, 73, 16, 0, 73, 2, 0, 73, 59, 0, 69, 73, 17, 0, 73, 2, 0, 73, 59, 0, 69, 73, 18, 0, 73, 2, 
					0, 73, 59, 0, 69, 73, 19, 0, 73, 2, 0, 73, 59, 0, 69, 73, 20, 0, 73, 2, 0, 73, 59, 0, 69, 73, 21, 0, 73, 2, 0, 73, 59, 0, 69, 73, 22, 0, 73, 2, 0, 73, 59, 0, 69, 73, 23, 0, 73, 2, 
					0, 73, 59, 0, 69, 73, 24, 0, 73, 2, 0, 73, 59, 0, 69, 73, 25, 0, 73, 2, 0, 73, 59, 0, 69, 73, 26, 0, 73, 2, 0, 73, 59, 0, 69, 73, 27, 0, 73, 2, 0, 73, 59, 0, 69, 73, 28, 0, 73, 2, 
					0, 73, 59, 0, 69, 73, 29, 0, 73, 2, 0, 73, 59, 0, 69, 73, 30, 0, 73, 2, 0, 73, 59, 0, 69, 73, 31, 0, 73, 2, 0, 73, 59, 0, 69, 73, 32, 0, 73, 2, 0, 73, 59, 0, 69, 73, 33, 0, 73, 2, 
					0, 73, 59, 0, 69, 73, 34, 0, 73, 2, 0, 73, 59, 0, 69, 73, 35, 0, 73, 2, 0, 73, 59, 0, 69, 73, 36, 0, 73, 2, 0, 73, 59, 0, 69, 73, 37, 0, 73, 2, 0, 73, 59, 0, 69, 73, 38, 0, 73, 2, 
					0, 73, 59, 0, 69, 73, 39, 0, 73, 2, 0, 73, 59, 0, 69, 73, 40, 0, 73, 2, 0, 73, 59, 0, 69, 73, 41, 0, 73, 2, 0, 73, 59, 0, 69, 73, 42, 0, 73, 2, 0, 73, 59, 0, 69, 73, 43, 0, 73, 2, 
					0, 73, 59, 0, 69, 73, 44, 0, 73, 2, 0, 73, 59, 0, 69, 73, 45, 0, 73, 2, 0, 73, 59, 0, 69, 73, 46, 0, 73, 2, 0, 73, 59, 0, 69, 73, 47, 0, 73, 2, 0, 73, 59, 0, 69, 73, 48, 0, 73, 2, 
					0, 73, 59, 0, 69, 73, 49, 0, 73, 2, 0, 73, 59, 0, 69, 73, 50, 0, 73, 2, 0, 73, 59, 0, 69, 73, 51, 0, 73, 2, 0, 73, 59, 0, 69, 73, 52, 0, 73, 2, 0, 73, 59, 0, 69, 73, 53, 0, 73, 2, 
					0, 73, 59, 0, 69, 73, 54, 0, 73, 2, 0, 73, 59, 0, 69, 73, 55, 0, 73, 2, 0, 73, 59, 0, 69, 73, 56, 0, 73, 2, 0, 73, 59, 0, 69, 73, 57, 0, 73, 2, 0, 73, 59, 0, 69, 73, 58, 0, 73, 2, 
					0, 73, 59, 0, 69, 73, 59, 0, 73, 2, 0, 73, 59, 0, 69, 73, 60, 0, 73, 2, 0, 73, 59, 0, 69, 73, 61, 0, 73, 2, 0, 73, 59, 0, 69, 73, 62, 0, 73, 2, 0, 73, 59, 0, 69, 73, 63, 0, 73, 2, 
					0, 73, 59, 0, 69, 73, 64, 0, 73, 2, 0, 73, 59, 0, 69, 73, 65, 0, 73, 2, 0, 73, 59, 0, 69, 73, 66, 0, 73, 2, 0, 73, 59, 0, 69, 73, 67, 0, 73, 2, 0, 73, 59, 0, 69, 73, 68, 0, 73, 2, 
					0, 73, 59, 0, 69, 77, 3, 1, 98, 76, 73, 7, 0, 69, 73, 0, 0, 73, 2, 0, 73, 51, 0, 69, 73, 6, 0, 73, 2, 0, 73, 51, 0, 69, 73, 7, 0, 73, 2, 0, 73, 51, 0, 69, 73, 8, 0, 73, 2, 0, 
					73, 51, 0, 69, 73, 9, 0, 73, 2, 0, 73, 51, 0, 69, 73, 10, 0, 73, 2, 0, 73, 51, 0, 69, 73, 11, 0, 73, 2, 0, 73, 51, 0, 69, 73, 12, 0, 73, 2, 0, 73, 51, 0, 69, 73, 13, 0, 73, 2, 0, 
					73, 51, 0, 69, 73, 14, 0, 73, 2, 0, 73, 51, 0, 69, 73, 15, 0, 73, 2, 0, 73, 51, 0, 69, 73, 16, 0, 73, 2, 0, 73, 51, 0, 69, 73, 17, 0, 73, 2, 0, 73, 51, 0, 69, 73, 18, 0, 73, 2, 0, 
					73, 51, 0, 69, 73, 19, 0, 73, 2, 0, 73, 51, 0, 69, 73, 20, 0, 73, 2, 0, 73, 51, 0, 69, 73, 21, 0, 73, 2, 0, 73, 51, 0, 69, 73, 22, 0, 73, 2, 0, 73, 51, 0, 69, 73, 23, 0, 73, 2, 0, 
					73, 51, 0, 69, 73, 24, 0, 73, 2, 0, 73, 51, 0, 69, 73, 25, 0, 73, 2, 0, 73, 51, 0, 69, 73, 26, 0, 73, 2, 0, 73, 51, 0, 69, 73, 27, 0, 73, 2, 0, 73, 51, 0, 69, 73, 28, 0, 73, 2, 0, 
					73, 51, 0, 69, 73, 29, 0, 73, 2, 0, 73, 51, 0, 69, 73, 30, 0, 73, 2, 0, 73, 51, 0, 69, 73, 31, 0, 73, 2, 0, 73, 51, 0, 69, 73, 32, 0, 73, 2, 0, 73, 51, 0, 69, 73, 33, 0, 73, 2, 0, 
					73, 51, 0, 69, 73, 34, 0, 73, 2, 0, 73, 51, 0, 69, 73, 35, 0, 73, 2, 0, 73, 51, 0, 69, 73, 36, 0, 73, 2, 0, 73, 51, 0, 69, 73, 37, 0, 73, 2, 0, 73, 51, 0, 69, 73, 38, 0, 73, 2, 0, 
					73, 51, 0, 69, 73, 39, 0, 73, 2, 0, 73, 51, 0, 69, 73, 40, 0, 73, 2, 0, 73, 51, 0, 69, 73, 41, 0, 73, 2, 0, 73, 51, 0, 69, 73, 42, 0, 73, 2, 0, 73, 51, 0, 69, 73, 43, 0, 73, 2, 0, 
					73, 51, 0, 69, 73, 44, 0, 73, 2, 0, 73, 51, 0, 69, 73, 45, 0, 73, 2, 0, 73, 51, 0, 69, 73, 46, 0, 73, 2, 0, 73, 51, 0, 69, 73, 47, 0, 73, 2, 0, 73, 51, 0, 69, 73, 48, 0, 73, 2, 0, 
					73, 51, 0, 69, 73, 49, 0, 73, 2, 0, 73, 51, 0, 69, 73, 50, 0, 73, 2, 0, 73, 51, 0, 69, 73, 51, 0, 73, 2, 0, 73, 51, 0, 69, 73, 52, 0, 73, 2, 0, 73, 51, 0, 69, 73, 53, 0, 73, 2, 0, 
					73, 51, 0, 69, 73, 54, 0, 73, 2, 0, 73, 51, 0, 69, 73, 55, 0, 73, 2, 0, 73, 51, 0, 69, 73, 56, 0, 73, 2, 0, 73, 51, 0, 69, 73, 57, 0, 73, 2, 0, 73, 51, 0, 69, 73, 58, 0, 73, 2, 0, 
					73, 51, 0, 69, 73, 59, 0, 73, 2, 0, 73, 51, 0, 69, 73, 60, 0, 73, 2, 0, 73, 51, 0, 69, 73, 61, 0, 73, 2, 0, 73, 51, 0, 69, 73, 62, 0, 73, 2, 0, 73, 51, 0, 69, 73, 63, 0, 73, 2, 0, 
					73, 51, 0, 69, 73, 64, 0, 73, 2, 0, 73, 51, 0, 69, 73, 65, 0, 73, 2, 0, 73, 51, 0, 69, 73, 66, 0, 73, 2, 0, 73, 51, 0, 69, 73, 67, 0, 73, 2, 0, 73, 51, 0, 69, 73, 68, 0, 73, 2, 0, 
					73, 51, 0, 69, 77, 3, 1, 98, 76, 73, 8, 0, 69, 73, 0, 0, 73, 2, 0, 73, 55, 0, 69, 73, 6, 0, 73, 2, 0, 73, 55, 0, 69, 73, 7, 0, 73, 2, 0, 73, 55, 0, 69, 73, 8, 0, 73, 2, 0, 73, 
					55, 0, 69, 73, 9, 0, 73, 2, 0, 73, 55, 0, 69, 73, 10, 0, 73, 2, 0, 73, 55, 0, 69, 73, 11, 0, 73, 2, 0, 73, 55, 0, 69, 73, 12, 0, 73, 2, 0, 73, 55, 0, 69, 73, 13, 0, 73, 2, 0, 73, 
					55, 0, 69, 73, 14, 0, 73, 2, 0, 73, 55, 0, 69, 73, 15, 0, 73, 2, 0, 73, 55, 0, 69, 73, 16, 0, 73, 2, 0, 73, 55, 0, 69, 73, 17, 0, 73, 2, 0, 73, 55, 0, 69, 73, 18, 0, 73, 2, 0, 73, 
					55, 0, 69, 73, 19, 0, 73, 2, 0, 73, 55, 0, 69, 73, 20, 0, 73, 2, 0, 73, 55, 0, 69, 73, 21, 0, 73, 2, 0, 73, 55, 0, 69, 73, 22, 0, 73, 2, 0, 73, 55, 0, 69, 73, 23, 0, 73, 2, 0, 73, 
					55, 0, 69, 73, 24, 0, 73, 2, 0, 73, 55, 0, 69, 73, 25, 0, 73, 2, 0, 73, 55, 0, 69, 73, 26, 0, 73, 2, 0, 73, 55, 0, 69, 73, 27, 0, 73, 2, 0, 73, 55, 0, 69, 73, 28, 0, 73, 2, 0, 73, 
					55, 0, 69, 73, 29, 0, 73, 2, 0, 73, 55, 0, 69, 73, 30, 0, 73, 2, 0, 73, 55, 0, 69, 73, 31, 0, 73, 2, 0, 73, 55, 0, 69, 73, 32, 0, 73, 2, 0, 73, 55, 0, 69, 73, 33, 0, 73, 2, 0, 73, 
					55, 0, 69, 73, 34, 0, 73, 2, 0, 73, 55, 0, 69, 73, 35, 0, 73, 2, 0, 73, 55, 0, 69, 73, 36, 0, 73, 2, 0, 73, 55, 0, 69, 73, 37, 0, 73, 2, 0, 73, 55, 0, 69, 73, 38, 0, 73, 2, 0, 73, 
					55, 0, 69, 73, 39, 0, 73, 2, 0, 73, 55, 0, 69, 73, 40, 0, 73, 2, 0, 73, 55, 0, 69, 73, 41, 0, 73, 2, 0, 73, 55, 0, 69, 73, 42, 0, 73, 2, 0, 73, 55, 0, 69, 73, 43, 0, 73, 2, 0, 73, 
					55, 0, 69, 73, 44, 0, 73, 2, 0, 73, 55, 0, 69, 73, 45, 0, 73, 2, 0, 73, 55, 0, 69, 73, 46, 0, 73, 2, 0, 73, 55, 0, 69, 73, 47, 0, 73, 2, 0, 73, 55, 0, 69, 73, 48, 0, 73, 2, 0, 73, 
					55, 0, 69, 73, 49, 0, 73, 2, 0, 73, 55, 0, 69, 73, 50, 0, 73, 2, 0, 73, 55, 0, 69, 73, 51, 0, 73, 2, 0, 73, 55, 0, 69, 73, 52, 0, 73, 2, 0, 73, 55, 0, 69, 73, 53, 0, 73, 2, 0, 73, 
					55, 0, 69, 73, 54, 0, 73, 2, 0, 73, 55, 0, 69, 73, 55, 0, 73, 2, 0, 73, 55, 0, 69, 73, 56, 0, 73, 2, 0, 73, 55, 0, 69, 73, 57, 0, 73, 2, 0, 73, 55, 0, 69, 73, 58, 0, 73, 2, 0, 73, 
					55, 0, 69, 73, 59, 0, 73, 2, 0, 73, 55, 0, 69, 73, 60, 0, 73, 2, 0, 73, 55, 0, 69, 73, 61, 0, 73, 2, 0, 73, 55, 0, 69, 73, 62, 0, 73, 2, 0, 73, 55, 0, 69, 73, 63, 0, 73, 2, 0, 73, 
					55, 0, 69, 73, 64, 0, 73, 2, 0, 73, 55, 0, 69, 73, 65, 0, 73, 2, 0, 73, 55, 0, 69, 73, 66, 0, 73, 2, 0, 73, 55, 0, 69, 73, 67, 0, 73, 2, 0, 73, 55, 0, 69, 73, 68, 0, 73, 2, 0, 73, 
					55, 0, 69, 77, 3, 1, 98, 76, 73, 9, 0, 69, 73, 0, 0, 73, 2, 0, 73, 60, 0, 69, 73, 6, 0, 73, 2, 0, 73, 60, 0, 69, 73, 7, 0, 73, 2, 0, 73, 60, 0, 69, 73, 8, 0, 73, 2, 0, 73, 60, 
					0, 69, 73, 9, 0, 73, 2, 0, 73, 60, 0, 69, 73, 10, 0, 73, 2, 0, 73, 60, 0, 69, 73, 11, 0, 73, 2, 0, 73, 60, 0, 69, 73, 12, 0, 73, 2, 0, 73, 60, 0, 69, 73, 13, 0, 73, 2, 0, 73, 60, 
					0, 69, 73, 14, 0, 73, 2, 0, 73, 60, 0, 69, 73, 15, 0, 73, 2, 0, 73, 60, 0, 69, 73, 16, 0, 73, 2, 0, 73, 60, 0, 69, 73, 17, 0, 73, 2, 0, 73, 60, 0, 69, 73, 18, 0, 73, 2, 0, 73, 60, 
					0, 69, 73, 19, 0, 73, 2, 0, 73, 60, 0, 69, 73, 20, 0, 73, 2, 0, 73, 60, 0, 69, 73, 21, 0, 73, 2, 0, 73, 60, 0, 69, 73, 22, 0, 73, 2, 0, 73, 60, 0, 69, 73, 23, 0, 73, 2, 0, 73, 60, 
					0, 69, 73, 24, 0, 73, 2, 0, 73, 60, 0, 69, 73, 25, 0, 73, 2, 0, 73, 60, 0, 69, 73, 26, 0, 73, 2, 0, 73, 60, 0, 69, 73, 27, 0, 73, 2, 0, 73, 60, 0, 69, 73, 28, 0, 73, 2, 0, 73, 60, 
					0, 69, 73, 29, 0, 73, 2, 0, 73, 60, 0, 69, 73, 30, 0, 73, 2, 0, 73, 60, 0, 69, 73, 31, 0, 73, 2, 0, 73, 60, 0, 69, 73, 32, 0, 73, 2, 0, 73, 60, 0, 69, 73, 33, 0, 73, 2, 0, 73, 60, 
					0, 69, 73, 34, 0, 73, 2, 0, 73, 60, 0, 69, 73, 35, 0, 73, 2, 0, 73, 60, 0, 69, 73, 36, 0, 73, 2, 0, 73, 60, 0, 69, 73, 37, 0, 73, 2, 0, 73, 60, 0, 69, 73, 38, 0, 73, 2, 0, 73, 60, 
					0, 69, 73, 39, 0, 73, 2, 0, 73, 60, 0, 69, 73, 40, 0, 73, 2, 0, 73, 60, 0, 69, 73, 41, 0, 73, 2, 0, 73, 60, 0, 69, 73, 42, 0, 73, 2, 0, 73, 60, 0, 69, 73, 43, 0, 73, 2, 0, 73, 60, 
					0, 69, 73, 44, 0, 73, 2, 0, 73, 60, 0, 69, 73, 45, 0, 73, 2, 0, 73, 60, 0, 69, 73, 46, 0, 73, 2, 0, 73, 60, 0, 69, 73, 47, 0, 73, 2, 0, 73, 60, 0, 69, 73, 48, 0, 73, 2, 0, 73, 60, 
					0, 69, 73, 49, 0, 73, 2, 0, 73, 60, 0, 69, 73, 50, 0, 73, 2, 0, 73, 60, 0, 69, 73, 51, 0, 73, 2, 0, 73, 60, 0, 69, 73, 52, 0, 73, 2, 0, 73, 60, 0, 69, 73, 53, 0, 73, 2, 0, 73, 60, 
					0, 69, 73, 54, 0, 73, 2, 0, 73, 60, 0, 69, 73, 55, 0, 73, 2, 0, 73, 60, 0, 69, 73, 56, 0, 73, 2, 0, 73, 60, 0, 69, 73, 57, 0, 73, 2, 0, 73, 60, 0, 69, 73, 58, 0, 73, 2, 0, 73, 60, 
					0, 69, 73, 59, 0, 73, 2, 0, 73, 60, 0, 69, 73, 60, 0, 73, 2, 0, 73, 60, 0, 69, 73, 61, 0, 73, 2, 0, 73, 60, 0, 69, 73, 62, 0, 73, 2, 0, 73, 60, 0, 69, 73, 63, 0, 73, 2, 0, 73, 60, 
					0, 69, 73, 64, 0, 73, 2, 0, 73, 60, 0, 69, 73, 65, 0, 73, 2, 0, 73, 60, 0, 69, 73, 66, 0, 73, 2, 0, 73, 60, 0, 69, 73, 67, 0, 73, 2, 0, 73, 60, 0, 69, 73, 68, 0, 73, 2, 0, 73, 60, 
					0, 69, 77, 3, 1, 98, 76, 73, 10, 0, 69, 73, 0, 0, 73, 2, 0, 73, 52, 0, 69, 73, 6, 0, 73, 2, 0, 73, 52, 0, 69, 73, 7, 0, 73, 2, 0, 73, 52, 0, 69, 73, 8, 0, 73, 2, 0, 73, 52, 0, 
					69, 73, 9, 0, 73, 2, 0, 73, 52, 0, 69, 73, 10, 0, 73, 2, 0, 73, 52, 0, 69, 73, 11, 0, 73, 2, 0, 73, 52, 0, 69, 73, 12, 0, 73, 2, 0, 73, 52, 0, 69, 73, 13, 0, 73, 2, 0, 73, 52, 0, 
					69, 73, 14, 0, 73, 2, 0, 73, 52, 0, 69, 73, 15, 0, 73, 2, 0, 73, 52, 0, 69, 73, 16, 0, 73, 2, 0, 73, 52, 0, 69, 73, 17, 0, 73, 2, 0, 73, 52, 0, 69, 73, 18, 0, 73, 2, 0, 73, 52, 0, 
					69, 73, 19, 0, 73, 2, 0, 73, 52, 0, 69, 73, 20, 0, 73, 2, 0, 73, 52, 0, 69, 73, 21, 0, 73, 2, 0, 73, 52, 0, 69, 73, 22, 0, 73, 2, 0, 73, 52, 0, 69, 73, 23, 0, 73, 2, 0, 73, 52, 0, 
					69, 73, 24, 0, 73, 2, 0, 73, 52, 0, 69, 73, 25, 0, 73, 2, 0, 73, 52, 0, 69, 73, 26, 0, 73, 2, 0, 73, 52, 0, 69, 73, 27, 0, 73, 2, 0, 73, 52, 0, 69, 73, 28, 0, 73, 2, 0, 73, 52, 0, 
					69, 73, 29, 0, 73, 2, 0, 73, 52, 0, 69, 73, 30, 0, 73, 2, 0, 73, 52, 0, 69, 73, 31, 0, 73, 2, 0, 73, 52, 0, 69, 73, 32, 0, 73, 2, 0, 73, 52, 0, 69, 73, 33, 0, 73, 2, 0, 73, 52, 0, 
					69, 73, 34, 0, 73, 2, 0, 73, 52, 0, 69, 73, 35, 0, 73, 2, 0, 73, 52, 0, 69, 73, 36, 0, 73, 2, 0, 73, 52, 0, 69, 73, 37, 0, 73, 2, 0, 73, 52, 0, 69, 73, 38, 0, 73, 2, 0, 73, 52, 0, 
					69, 73, 39, 0, 73, 2, 0, 73, 52, 0, 69, 73, 40, 0, 73, 2, 0, 73, 52, 0, 69, 73, 41, 0, 73, 2, 0, 73, 52, 0, 69, 73, 42, 0, 73, 2, 0, 73, 52, 0, 69, 73, 43, 0, 73, 2, 0, 73, 52, 0, 
					69, 73, 44, 0, 73, 2, 0, 73, 52, 0, 69, 73, 45, 0, 73, 2, 0, 73, 52, 0, 69, 73, 46, 0, 73, 2, 0, 73, 52, 0, 69, 73, 47, 0, 73, 2, 0, 73, 52, 0, 69, 73, 48, 0, 73, 2, 0, 73, 52, 0, 
					69, 73, 49, 0, 73, 2, 0, 73, 52, 0, 69, 73, 50, 0, 73, 2, 0, 73, 52, 0, 69, 73, 51, 0, 73, 2, 0, 73, 52, 0, 69, 73, 52, 0, 73, 2, 0, 73, 52, 0, 69, 73, 53, 0, 73, 2, 0, 73, 52, 0, 
					69, 73, 54, 0, 73, 2, 0, 73, 52, 0, 69, 73, 55, 0, 73, 2, 0, 73, 52, 0, 69, 73, 56, 0, 73, 2, 0, 73, 52, 0, 69, 73, 57, 0, 73, 2, 0, 73, 52, 0, 69, 73, 58, 0, 73, 2, 0, 73, 52, 0, 
					69, 73, 59, 0, 73, 2, 0, 73, 52, 0, 69, 73, 60, 0, 73, 2, 0, 73, 52, 0, 69, 73, 61, 0, 73, 2, 0, 73, 52, 0, 69, 73, 62, 0, 73, 2, 0, 73, 52, 0, 69, 73, 63, 0, 73, 2, 0, 73, 52, 0, 
					69, 73, 64, 0, 73, 2, 0, 73, 52, 0, 69, 73, 65, 0, 73, 2, 0, 73, 52, 0, 69, 73, 66, 0, 73, 2, 0, 73, 52, 0, 69, 73, 67, 0, 73, 2, 0, 73, 52, 0, 69, 73, 68, 0, 73, 2, 0, 73, 52, 0, 
					69, 77, 3, 1, 98, 76, 73, 11, 0, 69, 73, 0, 0, 73, 2, 0, 73, 57, 0, 69, 73, 6, 0, 73, 2, 0, 73, 57, 0, 69, 73, 7, 0, 73, 2, 0, 73, 57, 0, 69, 73, 8, 0, 73, 2, 0, 73, 57, 0, 69, 
					73, 9, 0, 73, 2, 0, 73, 57, 0, 69, 73, 10, 0, 73, 2, 0, 73, 57, 0, 69, 73, 11, 0, 73, 2, 0, 73, 57, 0, 69, 73, 12, 0, 73, 2, 0, 73, 57, 0, 69, 73, 13, 0, 73, 2, 0, 73, 57, 0, 69, 
					73, 14, 0, 73, 2, 0, 73, 57, 0, 69, 73, 15, 0, 73, 2, 0, 73, 57, 0, 69, 73, 16, 0, 73, 2, 0, 73, 57, 0, 69, 73, 17, 0, 73, 2, 0, 73, 57, 0, 69, 73, 18, 0, 73, 2, 0, 73, 57, 0, 69, 
					73, 19, 0, 73, 2, 0, 73, 57, 0, 69, 73, 20, 0, 73, 2, 0, 73, 57, 0, 69, 73, 21, 0, 73, 2, 0, 73, 57, 0, 69, 73, 22, 0, 73, 2, 0, 73, 57, 0, 69, 73, 23, 0, 73, 2, 0, 73, 57, 0, 69, 
					73, 24, 0, 73, 2, 0, 73, 57, 0, 69, 73, 25, 0, 73, 2, 0, 73, 57, 0, 69, 73, 26, 0, 73, 2, 0, 73, 57, 0, 69, 73, 27, 0, 73, 2, 0, 73, 57, 0, 69, 73, 28, 0, 73, 2, 0, 73, 57, 0, 69, 
					73, 29, 0, 73, 2, 0, 73, 57, 0, 69, 73, 30, 0, 73, 2, 0, 73, 57, 0, 69, 73, 31, 0, 73, 2, 0, 73, 57, 0, 69, 73, 32, 0, 73, 2, 0, 73, 57, 0, 69, 73, 33, 0, 73, 2, 0, 73, 57, 0, 69, 
					73, 34, 0, 73, 2, 0, 73, 57, 0, 69, 73, 35, 0, 73, 2, 0, 73, 57, 0, 69, 73, 36, 0, 73, 2, 0, 73, 57, 0, 69, 73, 37, 0, 73, 2, 0, 73, 57, 0, 69, 73, 38, 0, 73, 2, 0, 73, 57, 0, 69, 
					73, 39, 0, 73, 2, 0, 73, 57, 0, 69, 73, 40, 0, 73, 2, 0, 73, 57, 0, 69, 73, 41, 0, 73, 2, 0, 73, 57, 0, 69, 73, 42, 0, 73, 2, 0, 73, 57, 0, 69, 73, 43, 0, 73, 2, 0, 73, 57, 0, 69, 
					73, 44, 0, 73, 2, 0, 73, 57, 0, 69, 73, 45, 0, 73, 2, 0, 73, 57, 0, 69, 73, 46, 0, 73, 2, 0, 73, 57, 0, 69, 73, 47, 0, 73, 2, 0, 73, 57, 0, 69, 73, 48, 0, 73, 2, 0, 73, 57, 0, 69, 
					73, 49, 0, 73, 2, 0, 73, 57, 0, 69, 73, 50, 0, 73, 2, 0, 73, 57, 0, 69, 73, 51, 0, 73, 2, 0, 73, 57, 0, 69, 73, 52, 0, 73, 2, 0, 73, 57, 0, 69, 73, 53, 0, 73, 2, 0, 73, 57, 0, 69, 
					73, 54, 0, 73, 2, 0, 73, 57, 0, 69, 73, 55, 0, 73, 2, 0, 73, 57, 0, 69, 73, 56, 0, 73, 2, 0, 73, 57, 0, 69, 73, 57, 0, 73, 2, 0, 73, 57, 0, 69, 73, 58, 0, 73, 2, 0, 73, 57, 0, 69, 
					73, 59, 0, 73, 2, 0, 73, 57, 0, 69, 73, 60, 0, 73, 2, 0, 73, 57, 0, 69, 73, 61, 0, 73, 2, 0, 73, 57, 0, 69, 73, 62, 0, 73, 2, 0, 73, 57, 0, 69, 73, 63, 0, 73, 2, 0, 73, 57, 0, 69, 
					73, 64, 0, 73, 2, 0, 73, 57, 0, 69, 73, 65, 0, 73, 2, 0, 73, 57, 0, 69, 73, 66, 0, 73, 2, 0, 73, 57, 0, 69, 73, 67, 0, 73, 2, 0, 73, 57, 0, 69, 73, 68, 0, 73, 2, 0, 73, 57, 0, 69, 
					77, 3, 1, 98, 76, 73, 12, 0, 69, 73, 0, 0, 73, 2, 0, 73, 53, 0, 69, 73, 6, 0, 73, 2, 0, 73, 53, 0, 69, 73, 7, 0, 73, 2, 0, 73, 53, 0, 69, 73, 8, 0, 73, 2, 0, 73, 53, 0, 69, 73, 
					9, 0, 73, 2, 0, 73, 53, 0, 69, 73, 10, 0, 73, 2, 0, 73, 53, 0, 69, 73, 11, 0, 73, 2, 0, 73, 53, 0, 69, 73, 12, 0, 73, 2, 0, 73, 53, 0, 69, 73, 13, 0, 73, 2, 0, 73, 53, 0, 69, 73, 
					14, 0, 73, 2, 0, 73, 53, 0, 69, 73, 15, 0, 73, 2, 0, 73, 53, 0, 69, 73, 16, 0, 73, 2, 0, 73, 53, 0, 69, 73, 17, 0, 73, 2, 0, 73, 53, 0, 69, 73, 18, 0, 73, 2, 0, 73, 53, 0, 69, 73, 
					19, 0, 73, 2, 0, 73, 53, 0, 69, 73, 20, 0, 73, 2, 0, 73, 53, 0, 69, 73, 21, 0, 73, 2, 0, 73, 53, 0, 69, 73, 22, 0, 73, 2, 0, 73, 53, 0, 69, 73, 23, 0, 73, 2, 0, 73, 53, 0, 69, 73, 
					24, 0, 73, 2, 0, 73, 53, 0, 69, 73, 25, 0, 73, 2, 0, 73, 53, 0, 69, 73, 26, 0, 73, 2, 0, 73, 53, 0, 69, 73, 27, 0, 73, 2, 0, 73, 53, 0, 69, 73, 28, 0, 73, 2, 0, 73, 53, 0, 69, 73, 
					29, 0, 73, 2, 0, 73, 53, 0, 69, 73, 30, 0, 73, 2, 0, 73, 53, 0, 69, 73, 31, 0, 73, 2, 0, 73, 53, 0, 69, 73, 32, 0, 73, 2, 0, 73, 53, 0, 69, 73, 33, 0, 73, 2, 0, 73, 53, 0, 69, 73, 
					34, 0, 73, 2, 0, 73, 53, 0, 69, 73, 35, 0, 73, 2, 0, 73, 53, 0, 69, 73, 36, 0, 73, 2, 0, 73, 53, 0, 69, 73, 37, 0, 73, 2, 0, 73, 53, 0, 69, 73, 38, 0, 73, 2, 0, 73, 53, 0, 69, 73, 
					39, 0, 73, 2, 0, 73, 53, 0, 69, 73, 40, 0, 73, 2, 0, 73, 53, 0, 69, 73, 41, 0, 73, 2, 0, 73, 53, 0, 69, 73, 42, 0, 73, 2, 0, 73, 53, 0, 69, 73, 43, 0, 73, 2, 0, 73, 53, 0, 69, 73, 
					44, 0, 73, 2, 0, 73, 53, 0, 69, 73, 45, 0, 73, 2, 0, 73, 53, 0, 69, 73, 46, 0, 73, 2, 0, 73, 53, 0, 69, 73, 47, 0, 73, 2, 0, 73, 53, 0, 69, 73, 48, 0, 73, 2, 0, 73, 53, 0, 69, 73, 
					49, 0, 73, 2, 0, 73, 53, 0, 69, 73, 50, 0, 73, 2, 0, 73, 53, 0, 69, 73, 51, 0, 73, 2, 0, 73, 53, 0, 69, 73, 52, 0, 73, 2, 0, 73, 53, 0, 69, 73, 53, 0, 73, 2, 0, 73, 53, 0, 69, 73, 
					54, 0, 73, 2, 0, 73, 53, 0, 69, 73, 55, 0, 73, 2, 0, 73, 53, 0, 69, 73, 56, 0, 73, 2, 0, 73, 53, 0, 69, 73, 57, 0, 73, 2, 0, 73, 53, 0, 69, 73, 58, 0, 73, 2, 0, 73, 53, 0, 69, 73, 
					59, 0, 73, 2, 0, 73, 53, 0, 69, 73, 60, 0, 73, 2, 0, 73, 53, 0, 69, 73, 61, 0, 73, 2, 0, 73, 53, 0, 69, 73, 62, 0, 73, 2, 0, 73, 53, 0, 69, 73, 63, 0, 73, 2, 0, 73, 53, 0, 69, 73, 
					64, 0, 73, 2, 0, 73, 53, 0, 69, 73, 65, 0, 73, 2, 0, 73, 53, 0, 69, 73, 66, 0, 73, 2, 0, 73, 53, 0, 69, 73, 67, 0, 73, 2, 0, 73, 53, 0, 69, 73, 68, 0, 73, 2, 0, 73, 53, 0, 69, 77, 
					3, 1, 98, 76, 73, 13, 0, 69, 73, 0, 0, 73, 2, 0, 73, 58, 0, 69, 73, 6, 0, 73, 2, 0, 73, 58, 0, 69, 73, 7, 0, 73, 2, 0, 73, 58, 0, 69, 73, 8, 0, 73, 2, 0, 73, 58, 0, 69, 73, 9, 
					0, 73, 2, 0, 73, 58, 0, 69, 73, 10, 0, 73, 2, 0, 73, 58, 0, 69, 73, 11, 0, 73, 2, 0, 73, 58, 0, 69, 73, 12, 0, 73, 2, 0, 73, 58, 0, 69, 73, 13, 0, 73, 2, 0, 73, 58, 0, 69, 73, 14, 
					0, 73, 2, 0, 73, 58, 0, 69, 73, 15, 0, 73, 2, 0, 73, 58, 0, 69, 73, 16, 0, 73, 2, 0, 73, 58, 0, 69, 73, 17, 0, 73, 2, 0, 73, 58, 0, 69, 73, 18, 0, 73, 2, 0, 73, 58, 0, 69, 73, 19, 
					0, 73, 2, 0, 73, 58, 0, 69, 73, 20, 0, 73, 2, 0, 73, 58, 0, 69, 73, 21, 0, 73, 2, 0, 73, 58, 0, 69, 73, 22, 0, 73, 2, 0, 73, 58, 0, 69, 73, 23, 0, 73, 2, 0, 73, 58, 0, 69, 73, 24, 
					0, 73, 2, 0, 73, 58, 0, 69, 73, 25, 0, 73, 2, 0, 73, 58, 0, 69, 73, 26, 0, 73, 2, 0, 73, 58, 0, 69, 73, 27, 0, 73, 2, 0, 73, 58, 0, 69, 73, 28, 0, 73, 2, 0, 73, 58, 0, 69, 73, 29, 
					0, 73, 2, 0, 73, 58, 0, 69, 73, 30, 0, 73, 2, 0, 73, 58, 0, 69, 73, 31, 0, 73, 2, 0, 73, 58, 0, 69, 73, 32, 0, 73, 2, 0, 73, 58, 0, 69, 73, 33, 0, 73, 2, 0, 73, 58, 0, 69, 73, 34, 
					0, 73, 2, 0, 73, 58, 0, 69, 73, 35, 0, 73, 2, 0, 73, 58, 0, 69, 73, 36, 0, 73, 2, 0, 73, 58, 0, 69, 73, 37, 0, 73, 2, 0, 73, 58, 0, 69, 73, 38, 0, 73, 2, 0, 73, 58, 0, 69, 73, 39, 
					0, 73, 2, 0, 73, 58, 0, 69, 73, 40, 0, 73, 2, 0, 73, 58, 0, 69, 73, 41, 0, 73, 2, 0, 73, 58, 0, 69, 73, 42, 0, 73, 2, 0, 73, 58, 0, 69, 73, 43, 0, 73, 2, 0, 73, 58, 0, 69, 73, 44, 
					0, 73, 2, 0, 73, 58, 0, 69, 73, 45, 0, 73, 2, 0, 73, 58, 0, 69, 73, 46, 0, 73, 2, 0, 73, 58, 0, 69, 73, 47, 0, 73, 2, 0, 73, 58, 0, 69, 73, 48, 0, 73, 2, 0, 73, 58, 0, 69, 73, 49, 
					0, 73, 2, 0, 73, 58, 0, 69, 73, 50, 0, 73, 2, 0, 73, 58, 0, 69, 73, 51, 0, 73, 2, 0, 73, 58, 0, 69, 73, 52, 0, 73, 2, 0, 73, 58, 0, 69, 73, 53, 0, 73, 2, 0, 73, 58, 0, 69, 73, 54, 
					0, 73, 2, 0, 73, 58, 0, 69, 73, 55, 0, 73, 2, 0, 73, 58, 0, 69, 73, 56, 0, 73, 2, 0, 73, 58, 0, 69, 73, 57, 0, 73, 2, 0, 73, 58, 0, 69, 73, 58, 0, 73, 2, 0, 73, 58, 0, 69, 73, 59, 
					0, 73, 2, 0, 73, 58, 0, 69, 73, 60, 0, 73, 2, 0, 73, 58, 0, 69, 73, 61, 0, 73, 2, 0, 73, 58, 0, 69, 73, 62, 0, 73, 2, 0, 73, 58, 0, 69, 73, 63, 0, 73, 2, 0, 73, 58, 0, 69, 73, 64, 
					0, 73, 2, 0, 73, 58, 0, 69, 73, 65, 0, 73, 2, 0, 73, 58, 0, 69, 73, 66, 0, 73, 2, 0, 73, 58, 0, 69, 73, 67, 0, 73, 2, 0, 73, 58, 0, 69, 73, 68, 0, 73, 2, 0, 73, 58, 0, 69, 77, 3, 
					1, 98, 76, 73, 14, 0, 69, 73, 0, 0, 73, 2, 0, 73, 56, 0, 69, 73, 6, 0, 73, 2, 0, 73, 56, 0, 69, 73, 7, 0, 73, 2, 0, 73, 56, 0, 69, 73, 8, 0, 73, 2, 0, 73, 56, 0, 69, 73, 9, 0, 
					73, 2, 0, 73, 56, 0, 69, 73, 10, 0, 73, 2, 0, 73, 56, 0, 69, 73, 11, 0, 73, 2, 0, 73, 56, 0, 69, 73, 12, 0, 73, 2, 0, 73, 56, 0, 69, 73, 13, 0, 73, 2, 0, 73, 56, 0, 69, 73, 14, 0, 
					73, 2, 0, 73, 56, 0, 69, 73, 15, 0, 73, 2, 0, 73, 56, 0, 69, 73, 16, 0, 73, 2, 0, 73, 56, 0, 69, 73, 17, 0, 73, 2, 0, 73, 56, 0, 69, 73, 18, 0, 73, 2, 0, 73, 56, 0, 69, 73, 19, 0, 
					73, 2, 0, 73, 56, 0, 69, 73, 20, 0, 73, 2, 0, 73, 56, 0, 69, 73, 21, 0, 73, 2, 0, 73, 56, 0, 69, 73, 22, 0, 73, 2, 0, 73, 56, 0, 69, 73, 23, 0, 73, 2, 0, 73, 56, 0, 69, 73, 24, 0, 
					73, 2, 0, 73, 56, 0, 69, 73, 25, 0, 73, 2, 0, 73, 56, 0, 69, 73, 26, 0, 73, 2, 0, 73, 56, 0, 69, 73, 27, 0, 73, 2, 0, 73, 56, 0, 69, 73, 28, 0, 73, 2, 0, 73, 56, 0, 69, 73, 29, 0, 
					73, 2, 0, 73, 56, 0, 69, 73, 30, 0, 73, 2, 0, 73, 56, 0, 69, 73, 31, 0, 73, 2, 0, 73, 56, 0, 69, 73, 32, 0, 73, 2, 0, 73, 56, 0, 69, 73, 33, 0, 73, 2, 0, 73, 56, 0, 69, 73, 34, 0, 
					73, 2, 0, 73, 56, 0, 69, 73, 35, 0, 73, 2, 0, 73, 56, 0, 69, 73, 36, 0, 73, 2, 0, 73, 56, 0, 69, 73, 37, 0, 73, 2, 0, 73, 56, 0, 69, 73, 38, 0, 73, 2, 0, 73, 56, 0, 69, 73, 39, 0, 
					73, 2, 0, 73, 56, 0, 69, 73, 40, 0, 73, 2, 0, 73, 56, 0, 69, 73, 41, 0, 73, 2, 0, 73, 56, 0, 69, 73, 42, 0, 73, 2, 0, 73, 56, 0, 69, 73, 43, 0, 73, 2, 0, 73, 56, 0, 69, 73, 44, 0, 
					73, 2, 0, 73, 56, 0, 69, 73, 45, 0, 73, 2, 0, 73, 56, 0, 69, 73, 46, 0, 73, 2, 0, 73, 56, 0, 69, 73, 47, 0, 73, 2, 0, 73, 56, 0, 69, 73, 48, 0, 73, 2, 0, 73, 56, 0, 69, 73, 49, 0, 
					73, 2, 0, 73, 56, 0, 69, 73, 50, 0, 73, 2, 0, 73, 56, 0, 69, 73, 51, 0, 73, 2, 0, 73, 56, 0, 69, 73, 52, 0, 73, 2, 0, 73, 56, 0, 69, 73, 53, 0, 73, 2, 0, 73, 56, 0, 69, 73, 54, 0, 
					73, 2, 0, 73, 56, 0, 69, 73, 55, 0, 73, 2, 0, 73, 56, 0, 69, 73, 56, 0, 73, 2, 0, 73, 56, 0, 69, 73, 57, 0, 73, 2, 0, 73, 56, 0, 69, 73, 58, 0, 73, 2, 0, 73, 56, 0, 69, 73, 59, 0, 
					73, 2, 0, 73, 56, 0, 69, 73, 60, 0, 73, 2, 0, 73, 56, 0, 69, 73, 61, 0, 73, 2, 0, 73, 56, 0, 69, 73, 62, 0, 73, 2, 0, 73, 56, 0, 69, 73, 63, 0, 73, 2, 0, 73, 56, 0, 69, 73, 64, 0, 
					73, 2, 0, 73, 56, 0, 69, 73, 65, 0, 73, 2, 0, 73, 56, 0, 69, 73, 66, 0, 73, 2, 0, 73, 56, 0, 69, 73, 67, 0, 73, 2, 0, 73, 56, 0, 69, 73, 68, 0, 73, 2, 0, 73, 56, 0, 69, 77, 3, 1, 
					98, 76, 73, 15, 0, 69, 73, 0, 0, 73, 2, 0, 73, 61, 0, 69, 73, 6, 0, 73, 2, 0, 73, 61, 0, 69, 73, 7, 0, 73, 2, 0, 73, 61, 0, 69, 73, 8, 0, 73, 2, 0, 73, 61, 0, 69, 73, 9, 0, 73, 
					2, 0, 73, 61, 0, 69, 73, 10, 0, 73, 2, 0, 73, 61, 0, 69, 73, 11, 0, 73, 2, 0, 73, 61, 0, 69, 73, 12, 0, 73, 2, 0, 73, 61, 0, 69, 73, 13, 0, 73, 2, 0, 73, 61, 0, 69, 73, 14, 0, 73, 
					2, 0, 73, 61, 0, 69, 73, 15, 0, 73, 2, 0, 73, 61, 0, 69, 73, 16, 0, 73, 2, 0, 73, 61, 0, 69, 73, 17, 0, 73, 2, 0, 73, 61, 0, 69, 73, 18, 0, 73, 2, 0, 73, 61, 0, 69, 73, 19, 0, 73, 
					2, 0, 73, 61, 0, 69, 73, 20, 0, 73, 2, 0, 73, 61, 0, 69, 73, 21, 0, 73, 2, 0, 73, 61, 0, 69, 73, 22, 0, 73, 2, 0, 73, 61, 0, 69, 73, 23, 0, 73, 2, 0, 73, 61, 0, 69, 73, 24, 0, 73, 
					2, 0, 73, 61, 0, 69, 73, 25, 0, 73, 2, 0, 73, 61, 0, 69, 73, 26, 0, 73, 2, 0, 73, 61, 0, 69, 73, 27, 0, 73, 2, 0, 73, 61, 0, 69, 73, 28, 0, 73, 2, 0, 73, 61, 0, 69, 73, 29, 0, 73, 
					2, 0, 73, 61, 0, 69, 73, 30, 0, 73, 2, 0, 73, 61, 0, 69, 73, 31, 0, 73, 2, 0, 73, 61, 0, 69, 73, 32, 0, 73, 2, 0, 73, 61, 0, 69, 73, 33, 0, 73, 2, 0, 73, 61, 0, 69, 73, 34, 0, 73, 
					2, 0, 73, 61, 0, 69, 73, 35, 0, 73, 2, 0, 73, 61, 0, 69, 73, 36, 0, 73, 2, 0, 73, 61, 0, 69, 73, 37, 0, 73, 2, 0, 73, 61, 0, 69, 73, 38, 0, 73, 2, 0, 73, 61, 0, 69, 73, 39, 0, 73, 
					2, 0, 73, 61, 0, 69, 73, 40, 0, 73, 2, 0, 73, 61, 0, 69, 73, 41, 0, 73, 2, 0, 73, 61, 0, 69, 73, 42, 0, 73, 2, 0, 73, 61, 0, 69, 73, 43, 0, 73, 2, 0, 73, 61, 0, 69, 73, 44, 0, 73, 
					2, 0, 73, 61, 0, 69, 73, 45, 0, 73, 2, 0, 73, 61, 0, 69, 73, 46, 0, 73, 2, 0, 73, 61, 0, 69, 73, 47, 0, 73, 2, 0, 73, 61, 0, 69, 73, 48, 0, 73, 2, 0, 73, 61, 0, 69, 73, 49, 0, 73, 
					2, 0, 73, 61, 0, 69, 73, 50, 0, 73, 2, 0, 73, 61, 0, 69, 73, 51, 0, 73, 2, 0, 73, 61, 0, 69, 73, 52, 0, 73, 2, 0, 73, 61, 0, 69, 73, 53, 0, 73, 2, 0, 73, 61, 0, 69, 73, 54, 0, 73, 
					2, 0, 73, 61, 0, 69, 73, 55, 0, 73, 2, 0, 73, 61, 0, 69, 73, 56, 0, 73, 2, 0, 73, 61, 0, 69, 73, 57, 0, 73, 2, 0, 73, 61, 0, 69, 73, 58, 0, 73, 2, 0, 73, 61, 0, 69, 73, 59, 0, 73, 
					2, 0, 73, 61, 0, 69, 73, 60, 0, 73, 2, 0, 73, 61, 0, 69, 73, 61, 0, 73, 2, 0, 73, 61, 0, 69, 73, 62, 0, 73, 2, 0, 73, 61, 0, 69, 73, 63, 0, 73, 2, 0, 73, 61, 0, 69, 73, 64, 0, 73, 
					2, 0, 73, 61, 0, 69, 73, 65, 0, 73, 2, 0, 73, 61, 0, 69, 73, 66, 0, 73, 2, 0, 73, 61, 0, 69, 73, 67, 0, 73, 2, 0, 73, 61, 0, 69, 73, 68, 0, 73, 2, 0, 73, 61, 0, 69, 77, 3, 1, 98, 
					76, 73, 16, 0, 69, 73, 0, 0, 73, 2, 0, 73, 36, 0, 69, 73, 6, 0, 73, 2, 0, 73, 36, 0, 69, 73, 7, 0, 73, 2, 0, 73, 36, 0, 69, 73, 8, 0, 73, 2, 0, 73, 36, 0, 69, 73, 9, 0, 73, 2, 
					0, 73, 36, 0, 69, 73, 10, 0, 73, 2, 0, 73, 36, 0, 69, 73, 11, 0, 73, 2, 0, 73, 36, 0, 69, 73, 12, 0, 73, 2, 0, 73, 36, 0, 69, 73, 13, 0, 73, 2, 0, 73, 36, 0, 69, 73, 14, 0, 73, 2, 
					0, 73, 36, 0, 69, 73, 15, 0, 73, 2, 0, 73, 36, 0, 69, 73, 16, 0, 73, 2, 0, 73, 36, 0, 69, 73, 17, 0, 73, 2, 0, 73, 36, 0, 69, 73, 18, 0, 73, 2, 0, 73, 36, 0, 69, 73, 19, 0, 73, 2, 
					0, 73, 36, 0, 69, 73, 20, 0, 73, 2, 0, 73, 36, 0, 69, 73, 21, 0, 73, 2, 0, 73, 36, 0, 69, 73, 22, 0, 73, 2, 0, 73, 36, 0, 69, 73, 23, 0, 73, 2, 0, 73, 36, 0, 69, 73, 24, 0, 73, 2, 
					0, 73, 36, 0, 69, 73, 25, 0, 73, 2, 0, 73, 36, 0, 69, 73, 26, 0, 73, 2, 0, 73, 36, 0, 69, 73, 27, 0, 73, 2, 0, 73, 36, 0, 69, 73, 28, 0, 73, 2, 0, 73, 36, 0, 69, 73, 29, 0, 73, 2, 
					0, 73, 36, 0, 69, 73, 30, 0, 73, 2, 0, 73, 36, 0, 69, 73, 31, 0, 73, 2, 0, 73, 36, 0, 69, 73, 32, 0, 73, 2, 0, 73, 36, 0, 69, 73, 33, 0, 73, 2, 0, 73, 36, 0, 69, 73, 34, 0, 73, 2, 
					0, 73, 36, 0, 69, 73, 35, 0, 73, 2, 0, 73, 36, 0, 69, 73, 36, 0, 73, 2, 0, 73, 36, 0, 69, 73, 37, 0, 73, 2, 0, 73, 36, 0, 69, 73, 38, 0, 73, 2, 0, 73, 36, 0, 69, 73, 39, 0, 73, 2, 
					0, 73, 36, 0, 69, 73, 40, 0, 73, 2, 0, 73, 36, 0, 69, 73, 41, 0, 73, 2, 0, 73, 36, 0, 69, 73, 42, 0, 73, 2, 0, 73, 36, 0, 69, 73, 43, 0, 73, 2, 0, 73, 36, 0, 69, 73, 44, 0, 73, 2, 
					0, 73, 36, 0, 69, 73, 45, 0, 73, 2, 0, 73, 36, 0, 69, 73, 46, 0, 73, 2, 0, 73, 36, 0, 69, 73, 47, 0, 73, 2, 0, 73, 36, 0, 69, 73, 48, 0, 73, 2, 0, 73, 36, 0, 69, 73, 49, 0, 73, 2, 
					0, 73, 36, 0, 69, 73, 50, 0, 73, 2, 0, 73, 36, 0, 69, 73, 51, 0, 73, 2, 0, 73, 36, 0, 69, 73, 52, 0, 73, 2, 0, 73, 36, 0, 69, 73, 53, 0, 73, 2, 0, 73, 36, 0, 69, 73, 54, 0, 73, 2, 
					0, 73, 36, 0, 69, 73, 55, 0, 73, 2, 0, 73, 36, 0, 69, 73, 56, 0, 73, 2, 0, 73, 36, 0, 69, 73, 57, 0, 73, 2, 0, 73, 36, 0, 69, 73, 58, 0, 73, 2, 0, 73, 36, 0, 69, 73, 59, 0, 73, 2, 
					0, 73, 36, 0, 69, 73, 60, 0, 73, 2, 0, 73, 36, 0, 69, 73, 61, 0, 73, 2, 0, 73, 36, 0, 69, 73, 62, 0, 73, 2, 0, 73, 36, 0, 69, 73, 63, 0, 73, 2, 0, 73, 36, 0, 69, 73, 64, 0, 73, 2, 
					0, 73, 36, 0, 69, 73, 65, 0, 73, 2, 0, 73, 36, 0, 69, 73, 66, 0, 73, 2, 0, 73, 36, 0, 69, 73, 67, 0, 73, 2, 0, 73, 36, 0, 69, 73, 68, 0, 73, 2, 0, 73, 36, 0, 69, 77, 3, 1, 98, 76, 
					73, 17, 0, 69, 73, 0, 0, 73, 2, 0, 73, 28, 0, 69, 73, 6, 0, 73, 2, 0, 73, 28, 0, 69, 73, 7, 0, 73, 2, 0, 73, 28, 0, 69, 73, 8, 0, 73, 2, 0, 73, 28, 0, 69, 73, 9, 0, 73, 2, 0, 
					73, 28, 0, 69, 73, 10, 0, 73, 2, 0, 73, 28, 0, 69, 73, 11, 0, 73, 2, 0, 73, 28, 0, 69, 73, 12, 0, 73, 2, 0, 73, 28, 0, 69, 73, 13, 0, 73, 2, 0, 73, 28, 0, 69, 73, 14, 0, 73, 2, 0, 
					73, 28, 0, 69, 73, 15, 0, 73, 2, 0, 73, 28, 0, 69, 73, 16, 0, 73, 2, 0, 73, 28, 0, 69, 73, 17, 0, 73, 2, 0, 73, 28, 0, 69, 73, 18, 0, 73, 2, 0, 73, 28, 0, 69, 73, 19, 0, 73, 2, 0, 
					73, 28, 0, 69, 73, 20, 0, 73, 2, 0, 73, 28, 0, 69, 73, 21, 0, 73, 2, 0, 73, 28, 0, 69, 73, 22, 0, 73, 2, 0, 73, 28, 0, 69, 73, 23, 0, 73, 2, 0, 73, 28, 0, 69, 73, 24, 0, 73, 2, 0, 
					73, 28, 0, 69, 73, 25, 0, 73, 2, 0, 73, 28, 0, 69, 73, 26, 0, 73, 2, 0, 73, 28, 0, 69, 73, 27, 0, 73, 2, 0, 73, 28, 0, 69, 73, 28, 0, 73, 2, 0, 73, 28, 0, 69, 73, 29, 0, 73, 2, 0, 
					73, 28, 0, 69, 73, 30, 0, 73, 2, 0, 73, 28, 0, 69, 73, 31, 0, 73, 2, 0, 73, 28, 0, 69, 73, 32, 0, 73, 2, 0, 73, 28, 0, 69, 73, 33, 0, 73, 2, 0, 73, 28, 0, 69, 73, 34, 0, 73, 2, 0, 
					73, 28, 0, 69, 73, 35, 0, 73, 2, 0, 73, 28, 0, 69, 73, 36, 0, 73, 2, 0, 73, 28, 0, 69, 73, 37, 0, 73, 2, 0, 73, 28, 0, 69, 73, 38, 0, 73, 2, 0, 73, 28, 0, 69, 73, 39, 0, 73, 2, 0, 
					73, 28, 0, 69, 73, 40, 0, 73, 2, 0, 73, 28, 0, 69, 73, 41, 0, 73, 2, 0, 73, 28, 0, 69, 73, 42, 0, 73, 2, 0, 73, 28, 0, 69, 73, 43, 0, 73, 2, 0, 73, 28, 0, 69, 73, 44, 0, 73, 2, 0, 
					73, 28, 0, 69, 73, 45, 0, 73, 2, 0, 73, 28, 0, 69, 73, 46, 0, 73, 2, 0, 73, 28, 0, 69, 73, 47, 0, 73, 2, 0, 73, 28, 0, 69, 73, 48, 0, 73, 2, 0, 73, 28, 0, 69, 73, 49, 0, 73, 2, 0, 
					73, 28, 0, 69, 73, 50, 0, 73, 2, 0, 73, 28, 0, 69, 73, 51, 0, 73, 2, 0, 73, 28, 0, 69, 73, 52, 0, 73, 2, 0, 73, 28, 0, 69, 73, 53, 0, 73, 2, 0, 73, 28, 0, 69, 73, 54, 0, 73, 2, 0, 
					73, 28, 0, 69, 73, 55, 0, 73, 2, 0, 73, 28, 0, 69, 73, 56, 0, 73, 2, 0, 73, 28, 0, 69, 73, 57, 0, 73, 2, 0, 73, 28, 0, 69, 73, 58, 0, 73, 2, 0, 73, 28, 0, 69, 73, 59, 0, 73, 2, 0, 
					73, 28, 0, 69, 73, 60, 0, 73, 2, 0, 73, 28, 0, 69, 73, 61, 0, 73, 2, 0, 73, 28, 0, 69, 73, 62, 0, 73, 2, 0, 73, 28, 0, 69, 73, 63, 0, 73, 2, 0, 73, 28, 0, 69, 73, 64, 0, 73, 2, 0, 
					73, 28, 0, 69, 73, 65, 0, 73, 2, 0, 73, 28, 0, 69, 73, 66, 0, 73, 2, 0, 73, 28, 0, 69, 73, 67, 0, 73, 2, 0, 73, 28, 0, 69, 73, 68, 0, 73, 2, 0, 73, 28, 0, 69, 77, 3, 1, 98, 76, 73, 
					18, 0, 69, 73, 0, 0, 73, 2, 0, 73, 32, 0, 69, 73, 6, 0, 73, 2, 0, 73, 32, 0, 69, 73, 7, 0, 73, 2, 0, 73, 32, 0, 69, 73, 8, 0, 73, 2, 0, 73, 32, 0, 69, 73, 9, 0, 73, 2, 0, 73, 
					32, 0, 69, 73, 10, 0, 73, 2, 0, 73, 32, 0, 69, 73, 11, 0, 73, 2, 0, 73, 32, 0, 69, 73, 12, 0, 73, 2, 0, 73, 32, 0, 69, 73, 13, 0, 73, 2, 0, 73, 32, 0, 69, 73, 14, 0, 73, 2, 0, 73, 
					32, 0, 69, 73, 15, 0, 73, 2, 0, 73, 32, 0, 69, 73, 16, 0, 73, 2, 0, 73, 32, 0, 69, 73, 17, 0, 73, 2, 0, 73, 32, 0, 69, 73, 18, 0, 73, 2, 0, 73, 32, 0, 69, 73, 19, 0, 73, 2, 0, 73, 
					32, 0, 69, 73, 20, 0, 73, 2, 0, 73, 32, 0, 69, 73, 21, 0, 73, 2, 0, 73, 32, 0, 69, 73, 22, 0, 73, 2, 0, 73, 32, 0, 69, 73, 23, 0, 73, 2, 0, 73, 32, 0, 69, 73, 24, 0, 73, 2, 0, 73, 
					32, 0, 69, 73, 25, 0, 73, 2, 0, 73, 32, 0, 69, 73, 26, 0, 73, 2, 0, 73, 32, 0, 69, 73, 27, 0, 73, 2, 0, 73, 32, 0, 69, 73, 28, 0, 73, 2, 0, 73, 32, 0, 69, 73, 29, 0, 73, 2, 0, 73, 
					32, 0, 69, 73, 30, 0, 73, 2, 0, 73, 32, 0, 69, 73, 31, 0, 73, 2, 0, 73, 32, 0, 69, 73, 32, 0, 73, 2, 0, 73, 32, 0, 69, 73, 33, 0, 73, 2, 0, 73, 32, 0, 69, 73, 34, 0, 73, 2, 0, 73, 
					32, 0, 69, 73, 35, 0, 73, 2, 0, 73, 32, 0, 69, 73, 36, 0, 73, 2, 0, 73, 32, 0, 69, 73, 37, 0, 73, 2, 0, 73, 32, 0, 69, 73, 38, 0, 73, 2, 0, 73, 32, 0, 69, 73, 39, 0, 73, 2, 0, 73, 
					32, 0, 69, 73, 40, 0, 73, 2, 0, 73, 32, 0, 69, 73, 41, 0, 73, 2, 0, 73, 32, 0, 69, 73, 42, 0, 73, 2, 0, 73, 32, 0, 69, 73, 43, 0, 73, 2, 0, 73, 32, 0, 69, 73, 44, 0, 73, 2, 0, 73, 
					32, 0, 69, 73, 45, 0, 73, 2, 0, 73, 32, 0, 69, 73, 46, 0, 73, 2, 0, 73, 32, 0, 69, 73, 47, 0, 73, 2, 0, 73, 32, 0, 69, 73, 48, 0, 73, 2, 0, 73, 32, 0, 69, 73, 49, 0, 73, 2, 0, 73, 
					32, 0, 69, 73, 50, 0, 73, 2, 0, 73, 32, 0, 69, 73, 51, 0, 73, 2, 0, 73, 32, 0, 69, 73, 52, 0, 73, 2, 0, 73, 32, 0, 69, 73, 53, 0, 73, 2, 0, 73, 32, 0, 69, 73, 54, 0, 73, 2, 0, 73, 
					32, 0, 69, 73, 55, 0, 73, 2, 0, 73, 32, 0, 69, 73, 56, 0, 73, 2, 0, 73, 32, 0, 69, 73, 57, 0, 73, 2, 0, 73, 32, 0, 69, 73, 58, 0, 73, 2, 0, 73, 32, 0, 69, 73, 59, 0, 73, 2, 0, 73, 
					32, 0, 69, 73, 60, 0, 73, 2, 0, 73, 32, 0, 69, 73, 61, 0, 73, 2, 0, 73, 32, 0, 69, 73, 62, 0, 73, 2, 0, 73, 32, 0, 69, 73, 63, 0, 73, 2, 0, 73, 32, 0, 69, 73, 64, 0, 73, 2, 0, 73, 
					32, 0, 69, 73, 65, 0, 73, 2, 0, 73, 32, 0, 69, 73, 66, 0, 73, 2, 0, 73, 32, 0, 69, 73, 67, 0, 73, 2, 0, 73, 32, 0, 69, 73, 68, 0, 73, 2, 0, 73, 32, 0, 69, 77, 3, 1, 98, 76, 73, 19, 
					0, 69, 73, 0, 0, 73, 2, 0, 73, 33, 0, 69, 73, 6, 0, 73, 2, 0, 73, 33, 0, 69, 73, 7, 0, 73, 2, 0, 73, 33, 0, 69, 73, 8, 0, 73, 2, 0, 73, 33, 0, 69, 73, 9, 0, 73, 2, 0, 73, 33, 
					0, 69, 73, 10, 0, 73, 2, 0, 73, 33, 0, 69, 73, 11, 0, 73, 2, 0, 73, 33, 0, 69, 73, 12, 0, 73, 2, 0, 73, 33, 0, 69, 73, 13, 0, 73, 2, 0, 73, 33, 0, 69, 73, 14, 0, 73, 2, 0, 73, 33, 
					0, 69, 73, 15, 0, 73, 2, 0, 73, 33, 0, 69, 73, 16, 0, 73, 2, 0, 73, 33, 0, 69, 73, 17, 0, 73, 2, 0, 73, 33, 0, 69, 73, 18, 0, 73, 2, 0, 73, 33, 0, 69, 73, 19, 0, 73, 2, 0, 73, 33, 
					0, 69, 73, 20, 0, 73, 2, 0, 73, 33, 0, 69, 73, 21, 0, 73, 2, 0, 73, 33, 0, 69, 73, 22, 0, 73, 2, 0, 73, 33, 0, 69, 73, 23, 0, 73, 2, 0, 73, 33, 0, 69, 73, 24, 0, 73, 2, 0, 73, 33, 
					0, 69, 73, 25, 0, 73, 2, 0, 73, 33, 0, 69, 73, 26, 0, 73, 2, 0, 73, 33, 0, 69, 73, 27, 0, 73, 2, 0, 73, 33, 0, 69, 73, 28, 0, 73, 2, 0, 73, 33, 0, 69, 73, 29, 0, 73, 2, 0, 73, 33, 
					0, 69, 73, 30, 0, 73, 2, 0, 73, 33, 0, 69, 73, 31, 0, 73, 2, 0, 73, 33, 0, 69, 73, 32, 0, 73, 2, 0, 73, 33, 0, 69, 73, 33, 0, 73, 2, 0, 73, 33, 0, 69, 73, 34, 0, 73, 2, 0, 73, 33, 
					0, 69, 73, 35, 0, 73, 2, 0, 73, 33, 0, 69, 73, 36, 0, 73, 2, 0, 73, 33, 0, 69, 73, 37, 0, 73, 2, 0, 73, 33, 0, 69, 73, 38, 0, 73, 2, 0, 73, 33, 0, 69, 73, 39, 0, 73, 2, 0, 73, 33, 
					0, 69, 73, 40, 0, 73, 2, 0, 73, 33, 0, 69, 73, 41, 0, 73, 2, 0, 73, 33, 0, 69, 73, 42, 0, 73, 2, 0, 73, 33, 0, 69, 73, 43, 0, 73, 2, 0, 73, 33, 0, 69, 73, 44, 0, 73, 2, 0, 73, 33, 
					0, 69, 73, 45, 0, 73, 2, 0, 73, 33, 0, 69, 73, 46, 0, 73, 2, 0, 73, 33, 0, 69, 73, 47, 0, 73, 2, 0, 73, 33, 0, 69, 73, 48, 0, 73, 2, 0, 73, 33, 0, 69, 73, 49, 0, 73, 2, 0, 73, 33, 
					0, 69, 73, 50, 0, 73, 2, 0, 73, 33, 0, 69, 73, 51, 0, 73, 2, 0, 73, 33, 0, 69, 73, 52, 0, 73, 2, 0, 73, 33, 0, 69, 73, 53, 0, 73, 2, 0, 73, 33, 0, 69, 73, 54, 0, 73, 2, 0, 73, 33, 
					0, 69, 73, 55, 0, 73, 2, 0, 73, 33, 0, 69, 73, 56, 0, 73, 2, 0, 73, 33, 0, 69, 73, 57, 0, 73, 2, 0, 73, 33, 0, 69, 73, 58, 0, 73, 2, 0, 73, 33, 0, 69, 73, 59, 0, 73, 2, 0, 73, 33, 
					0, 69, 73, 60, 0, 73, 2, 0, 73, 33, 0, 69, 73, 61, 0, 73, 2, 0, 73, 33, 0, 69, 73, 62, 0, 73, 2, 0, 73, 33, 0, 69, 73, 63, 0, 73, 2, 0, 73, 33, 0, 69, 73, 64, 0, 73, 2, 0, 73, 33, 
					0, 69, 73, 65, 0, 73, 2, 0, 73, 33, 0, 69, 73, 66, 0, 73, 2, 0, 73, 33, 0, 69, 73, 67, 0, 73, 2, 0, 73, 33, 0, 69, 73, 68, 0, 73, 2, 0, 73, 33, 0, 69, 77, 3, 1, 98, 76, 73, 20, 0, 
					69, 73, 0, 0, 73, 2, 0, 73, 50, 0, 69, 73, 6, 0, 73, 2, 0, 73, 50, 0, 69, 73, 7, 0, 73, 2, 0, 73, 50, 0, 69, 73, 8, 0, 73, 2, 0, 73, 50, 0, 69, 73, 9, 0, 73, 2, 0, 73, 50, 0, 
					69, 73, 10, 0, 73, 2, 0, 73, 50, 0, 69, 73, 11, 0, 73, 2, 0, 73, 50, 0, 69, 73, 12, 0, 73, 2, 0, 73, 50, 0, 69, 73, 13, 0, 73, 2, 0, 73, 50, 0, 69, 73, 14, 0, 73, 2, 0, 73, 50, 0, 
					69, 73, 15, 0, 73, 2, 0, 73, 50, 0, 69, 73, 16, 0, 73, 2, 0, 73, 50, 0, 69, 73, 17, 0, 73, 2, 0, 73, 50, 0, 69, 73, 18, 0, 73, 2, 0, 73, 50, 0, 69, 73, 19, 0, 73, 2, 0, 73, 50, 0, 
					69, 73, 20, 0, 73, 2, 0, 73, 50, 0, 69, 73, 21, 0, 73, 2, 0, 73, 50, 0, 69, 73, 22, 0, 73, 2, 0, 73, 50, 0, 69, 73, 23, 0, 73, 2, 0, 73, 50, 0, 69, 73, 24, 0, 73, 2, 0, 73, 50, 0, 
					69, 73, 25, 0, 73, 2, 0, 73, 50, 0, 69, 73, 26, 0, 73, 2, 0, 73, 50, 0, 69, 73, 27, 0, 73, 2, 0, 73, 50, 0, 69, 73, 28, 0, 73, 2, 0, 73, 50, 0, 69, 73, 29, 0, 73, 2, 0, 73, 50, 0, 
					69, 73, 30, 0, 73, 2, 0, 73, 50, 0, 69, 73, 31, 0, 73, 2, 0, 73, 50, 0, 69, 73, 32, 0, 73, 2, 0, 73, 50, 0, 69, 73, 33, 0, 73, 2, 0, 73, 50, 0, 69, 73, 34, 0, 73, 2, 0, 73, 50, 0, 
					69, 73, 35, 0, 73, 2, 0, 73, 50, 0, 69, 73, 36, 0, 73, 2, 0, 73, 50, 0, 69, 73, 37, 0, 73, 2, 0, 73, 50, 0, 69, 73, 38, 0, 73, 2, 0, 73, 50, 0, 69, 73, 39, 0, 73, 2, 0, 73, 50, 0, 
					69, 73, 40, 0, 73, 2, 0, 73, 50, 0, 69, 73, 41, 0, 73, 2, 0, 73, 50, 0, 69, 73, 42, 0, 73, 2, 0, 73, 50, 0, 69, 73, 43, 0, 73, 2, 0, 73, 50, 0, 69, 73, 44, 0, 73, 2, 0, 73, 50, 0, 
					69, 73, 45, 0, 73, 2, 0, 73, 50, 0, 69, 73, 46, 0, 73, 2, 0, 73, 50, 0, 69, 73, 47, 0, 73, 2, 0, 73, 50, 0, 69, 73, 48, 0, 73, 2, 0, 73, 50, 0, 69, 73, 49, 0, 73, 2, 0, 73, 50, 0, 
					69, 73, 50, 0, 73, 2, 0, 73, 50, 0, 69, 73, 51, 0, 73, 2, 0, 73, 50, 0, 69, 73, 52, 0, 73, 2, 0, 73, 50, 0, 69, 73, 53, 0, 73, 2, 0, 73, 50, 0, 69, 73, 54, 0, 73, 2, 0, 73, 50, 0, 
					69, 73, 55, 0, 73, 2, 0, 73, 50, 0, 69, 73, 56, 0, 73, 2, 0, 73, 50, 0, 69, 73, 57, 0, 73, 2, 0, 73, 50, 0, 69, 73, 58, 0, 73, 2, 0, 73, 50, 0, 69, 73, 59, 0, 73, 2, 0, 73, 50, 0, 
					69, 73, 60, 0, 73, 2, 0, 73, 50, 0, 69, 73, 61, 0, 73, 2, 0, 73, 50, 0, 69, 73, 62, 0, 73, 2, 0, 73, 50, 0, 69, 73, 63, 0, 73, 2, 0, 73, 50, 0, 69, 73, 64, 0, 73, 2, 0, 73, 50, 0, 
					69, 73, 65, 0, 73, 2, 0, 73, 50, 0, 69, 73, 66, 0, 73, 2, 0, 73, 50, 0, 69, 73, 67, 0, 73, 2, 0, 73, 50, 0, 69, 73, 68, 0, 73, 2, 0, 73, 50, 0, 69, 77, 3, 1, 98, 76, 73, 21, 0, 69, 
					73, 0, 0, 73, 2, 0, 73, 41, 0, 69, 73, 6, 0, 73, 2, 0, 73, 41, 0, 69, 73, 7, 0, 73, 2, 0, 73, 41, 0, 69, 73, 8, 0, 73, 2, 0, 73, 41, 0, 69, 73, 9, 0, 73, 2, 0, 73, 41, 0, 69, 
					73, 10, 0, 73, 2, 0, 73, 41, 0, 69, 73, 11, 0, 73, 2, 0, 73, 41, 0, 69, 73, 12, 0, 73, 2, 0, 73, 41, 0, 69, 73, 13, 0, 73, 2, 0, 73, 41, 0, 69, 73, 14, 0, 73, 2, 0, 73, 41, 0, 69, 
					73, 15, 0, 73, 2, 0, 73, 41, 0, 69, 73, 16, 0, 73, 2, 0, 73, 41, 0, 69, 73, 17, 0, 73, 2, 0, 73, 41, 0, 69, 73, 18, 0, 73, 2, 0, 73, 41, 0, 69, 73, 19, 0, 73, 2, 0, 73, 41, 0, 69, 
					73, 20, 0, 73, 2, 0, 73, 41, 0, 69, 73, 21, 0, 73, 2, 0, 73, 41, 0, 69, 73, 22, 0, 73, 2, 0, 73, 41, 0, 69, 73, 23, 0, 73, 2, 0, 73, 41, 0, 69, 73, 24, 0, 73, 2, 0, 73, 41, 0, 69, 
					73, 25, 0, 73, 2, 0, 73, 41, 0, 69, 73, 26, 0, 73, 2, 0, 73, 41, 0, 69, 73, 27, 0, 73, 2, 0, 73, 41, 0, 69, 73, 28, 0, 73, 2, 0, 73, 41, 0, 69, 73, 29, 0, 73, 2, 0, 73, 41, 0, 69, 
					73, 30, 0, 73, 2, 0, 73, 41, 0, 69, 73, 31, 0, 73, 2, 0, 73, 41, 0, 69, 73, 32, 0, 73, 2, 0, 73, 41, 0, 69, 73, 33, 0, 73, 2, 0, 73, 41, 0, 69, 73, 34, 0, 73, 2, 0, 73, 41, 0, 69, 
					73, 35, 0, 73, 2, 0, 73, 41, 0, 69, 73, 36, 0, 73, 2, 0, 73, 41, 0, 69, 73, 37, 0, 73, 2, 0, 73, 41, 0, 69, 73, 38, 0, 73, 2, 0, 73, 41, 0, 69, 73, 39, 0, 73, 2, 0, 73, 41, 0, 69, 
					73, 40, 0, 73, 2, 0, 73, 41, 0, 69, 73, 41, 0, 73, 2, 0, 73, 41, 0, 69, 73, 42, 0, 73, 2, 0, 73, 41, 0, 69, 73, 43, 0, 73, 2, 0, 73, 41, 0, 69, 73, 44, 0, 73, 2, 0, 73, 41, 0, 69, 
					73, 45, 0, 73, 2, 0, 73, 41, 0, 69, 73, 46, 0, 73, 2, 0, 73, 41, 0, 69, 73, 47, 0, 73, 2, 0, 73, 41, 0, 69, 73, 48, 0, 73, 2, 0, 73, 41, 0, 69, 73, 49, 0, 73, 2, 0, 73, 41, 0, 69, 
					73, 50, 0, 73, 2, 0, 73, 41, 0, 69, 73, 51, 0, 73, 2, 0, 73, 41, 0, 69, 73, 52, 0, 73, 2, 0, 73, 41, 0, 69, 73, 53, 0, 73, 2, 0, 73, 41, 0, 69, 73, 54, 0, 73, 2, 0, 73, 41, 0, 69, 
					73, 55, 0, 73, 2, 0, 73, 41, 0, 69, 73, 56, 0, 73, 2, 0, 73, 41, 0, 69, 73, 57, 0, 73, 2, 0, 73, 41, 0, 69, 73, 58, 0, 73, 2, 0, 73, 41, 0, 69, 73, 59, 0, 73, 2, 0, 73, 41, 0, 69, 
					73, 60, 0, 73, 2, 0, 73, 41, 0, 69, 73, 61, 0, 73, 2, 0, 73, 41, 0, 69, 73, 62, 0, 73, 2, 0, 73, 41, 0, 69, 73, 63, 0, 73, 2, 0, 73, 41, 0, 69, 73, 64, 0, 73, 2, 0, 73, 41, 0, 69, 
					73, 65, 0, 73, 2, 0, 73, 41, 0, 69, 73, 66, 0, 73, 2, 0, 73, 41, 0, 69, 73, 67, 0, 73, 2, 0, 73, 41, 0, 69, 73, 68, 0, 73, 2, 0, 73, 41, 0, 69, 77, 3, 1, 98, 76, 73, 22, 0, 69, 73, 
					0, 0, 73, 2, 0, 73, 8, 0, 69, 73, 6, 0, 73, 2, 0, 73, 8, 0, 69, 73, 7, 0, 73, 2, 0, 73, 8, 0, 69, 73, 8, 0, 73, 2, 0, 73, 8, 0, 69, 73, 9, 0, 73, 2, 0, 73, 8, 0, 69, 73, 
					10, 0, 73, 2, 0, 73, 8, 0, 69, 73, 11, 0, 73, 2, 0, 73, 8, 0, 69, 73, 12, 0, 73, 2, 0, 73, 8, 0, 69, 73, 13, 0, 73, 2, 0, 73, 8, 0, 69, 73, 14, 0, 73, 2, 0, 73, 8, 0, 69, 73, 
					15, 0, 73, 2, 0, 73, 8, 0, 69, 73, 16, 0, 73, 2, 0, 73, 8, 0, 69, 73, 17, 0, 73, 2, 0, 73, 8, 0, 69, 73, 18, 0, 73, 2, 0, 73, 8, 0, 69, 73, 19, 0, 73, 2, 0, 73, 8, 0, 69, 73, 
					20, 0, 73, 2, 0, 73, 8, 0, 69, 73, 21, 0, 73, 2, 0, 73, 8, 0, 69, 73, 22, 0, 73, 2, 0, 73, 8, 0, 69, 73, 23, 0, 73, 2, 0, 73, 8, 0, 69, 73, 24, 0, 73, 2, 0, 73, 8, 0, 69, 73, 
					25, 0, 73, 2, 0, 73, 8, 0, 69, 73, 26, 0, 73, 2, 0, 73, 8, 0, 69, 73, 27, 0, 73, 2, 0, 73, 8, 0, 69, 73, 28, 0, 73, 2, 0, 73, 8, 0, 69, 73, 29, 0, 73, 2, 0, 73, 8, 0, 69, 73, 
					30, 0, 73, 2, 0, 73, 8, 0, 69, 73, 31, 0, 73, 2, 0, 73, 8, 0, 69, 73, 32, 0, 73, 2, 0, 73, 8, 0, 69, 73, 33, 0, 73, 2, 0, 73, 8, 0, 69, 73, 34, 0, 73, 2, 0, 73, 8, 0, 69, 73, 
					35, 0, 73, 2, 0, 73, 8, 0, 69, 73, 36, 0, 73, 2, 0, 73, 8, 0, 69, 73, 37, 0, 73, 2, 0, 73, 8, 0, 69, 73, 38, 0, 73, 2, 0, 73, 8, 0, 69, 73, 39, 0, 73, 2, 0, 73, 8, 0, 69, 73, 
					40, 0, 73, 2, 0, 73, 8, 0, 69, 73, 41, 0, 73, 2, 0, 73, 8, 0, 69, 73, 42, 0, 73, 2, 0, 73, 8, 0, 69, 73, 43, 0, 73, 2, 0, 73, 8, 0, 69, 73, 44, 0, 73, 2, 0, 73, 8, 0, 69, 73, 
					45, 0, 73, 2, 0, 73, 8, 0, 69, 73, 46, 0, 73, 2, 0, 73, 8, 0, 69, 73, 47, 0, 73, 2, 0, 73, 8, 0, 69, 73, 48, 0, 73, 2, 0, 73, 8, 0, 69, 73, 49, 0, 73, 2, 0, 73, 8, 0, 69, 73, 
					50, 0, 73, 2, 0, 73, 8, 0, 69, 73, 51, 0, 73, 2, 0, 73, 8, 0, 69, 73, 52, 0, 73, 2, 0, 73, 8, 0, 69, 73, 53, 0, 73, 2, 0, 73, 8, 0, 69, 73, 54, 0, 73, 2, 0, 73, 8, 0, 69, 73, 
					55, 0, 73, 2, 0, 73, 8, 0, 69, 73, 56, 0, 73, 2, 0, 73, 8, 0, 69, 73, 57, 0, 73, 2, 0, 73, 8, 0, 69, 73, 58, 0, 73, 2, 0, 73, 8, 0, 69, 73, 59, 0, 73, 2, 0, 73, 8, 0, 69, 73, 
					60, 0, 73, 2, 0, 73, 8, 0, 69, 73, 61, 0, 73, 2, 0, 73, 8, 0, 69, 73, 62, 0, 73, 2, 0, 73, 8, 0, 69, 73, 63, 0, 73, 2, 0, 73, 8, 0, 69, 73, 64, 0, 73, 2, 0, 73, 8, 0, 69, 73, 
					65, 0, 73, 2, 0, 73, 8, 0, 69, 73, 66, 0, 73, 2, 0, 73, 8, 0, 69, 73, 67, 0, 73, 2, 0, 73, 8, 0, 69, 73, 68, 0, 73, 2, 0, 73, 8, 0, 69, 77, 3, 1, 98, 76, 73, 23, 0, 69, 73, 0, 
					0, 73, 2, 0, 73, 9, 0, 69, 73, 6, 0, 73, 2, 0, 73, 9, 0, 69, 73, 7, 0, 73, 2, 0, 73, 9, 0, 69, 73, 8, 0, 73, 2, 0, 73, 9, 0, 69, 73, 9, 0, 73, 2, 0, 73, 9, 0, 69, 73, 10, 
					0, 73, 2, 0, 73, 9, 0, 69, 73, 11, 0, 73, 2, 0, 73, 9, 0, 69, 73, 12, 0, 73, 2, 0, 73, 9, 0, 69, 73, 13, 0, 73, 2, 0, 73, 9, 0, 69, 73, 14, 0, 73, 2, 0, 73, 9, 0, 69, 73, 15, 
					0, 73, 2, 0, 73, 9, 0, 69, 73, 16, 0, 73, 2, 0, 73, 9, 0, 69, 73, 17, 0, 73, 2, 0, 73, 9, 0, 69, 73, 18, 0, 73, 2, 0, 73, 9, 0, 69, 73, 19, 0, 73, 2, 0, 73, 9, 0, 69, 73, 20, 
					0, 73, 2, 0, 73, 9, 0, 69, 73, 21, 0, 73, 2, 0, 73, 9, 0, 69, 73, 22, 0, 73, 2, 0, 73, 9, 0, 69, 73, 23, 0, 73, 2, 0, 73, 9, 0, 69, 73, 24, 0, 73, 2, 0, 73, 9, 0, 69, 73, 25, 
					0, 73, 2, 0, 73, 9, 0, 69, 73, 26, 0, 73, 2, 0, 73, 9, 0, 69, 73, 27, 0, 73, 2, 0, 73, 9, 0, 69, 73, 28, 0, 73, 2, 0, 73, 9, 0, 69, 73, 29, 0, 73, 2, 0, 73, 9, 0, 69, 73, 30, 
					0, 73, 2, 0, 73, 9, 0, 69, 73, 31, 0, 73, 2, 0, 73, 9, 0, 69, 73, 32, 0, 73, 2, 0, 73, 9, 0, 69, 73, 33, 0, 73, 2, 0, 73, 9, 0, 69, 73, 34, 0, 73, 2, 0, 73, 9, 0, 69, 73, 35, 
					0, 73, 2, 0, 73, 9, 0, 69, 73, 36, 0, 73, 2, 0, 73, 9, 0, 69, 73, 37, 0, 73, 2, 0, 73, 9, 0, 69, 73, 38, 0, 73, 2, 0, 73, 9, 0, 69, 73, 39, 0, 73, 2, 0, 73, 9, 0, 69, 73, 40, 
					0, 73, 2, 0, 73, 9, 0, 69, 73, 41, 0, 73, 2, 0, 73, 9, 0, 69, 73, 42, 0, 73, 2, 0, 73, 9, 0, 69, 73, 43, 0, 73, 2, 0, 73, 9, 0, 69, 73, 44, 0, 73, 2, 0, 73, 9, 0, 69, 73, 45, 
					0, 73, 2, 0, 73, 9, 0, 69, 73, 46, 0, 73, 2, 0, 73, 9, 0, 69, 73, 47, 0, 73, 2, 0, 73, 9, 0, 69, 73, 48, 0, 73, 2, 0, 73, 9, 0, 69, 73, 49, 0, 73, 2, 0, 73, 9, 0, 69, 73, 50, 
					0, 73, 2, 0, 73, 9, 0, 69, 73, 51, 0, 73, 2, 0, 73, 9, 0, 69, 73, 52, 0, 73, 2, 0, 73, 9, 0, 69, 73, 53, 0, 73, 2, 0, 73, 9, 0, 69, 73, 54, 0, 73, 2, 0, 73, 9, 0, 69, 73, 55, 
					0, 73, 2, 0, 73, 9, 0, 69, 73, 56, 0, 73, 2, 0, 73, 9, 0, 69, 73, 57, 0, 73, 2, 0, 73, 9, 0, 69, 73, 58, 0, 73, 2, 0, 73, 9, 0, 69, 73, 59, 0, 73, 2, 0, 73, 9, 0, 69, 73, 60, 
					0, 73, 2, 0, 73, 9, 0, 69, 73, 61, 0, 73, 2, 0, 73, 9, 0, 69, 73, 62, 0, 73, 2, 0, 73, 9, 0, 69, 73, 63, 0, 73, 2, 0, 73, 9, 0, 69, 73, 64, 0, 73, 2, 0, 73, 9, 0, 69, 73, 65, 
					0, 73, 2, 0, 73, 9, 0, 69, 73, 66, 0, 73, 2, 0, 73, 9, 0, 69, 73, 67, 0, 73, 2, 0, 73, 9, 0, 69, 73, 68, 0, 73, 2, 0, 73, 9, 0, 69, 77, 3, 1, 98, 76, 73, 24, 0, 69, 73, 0, 0, 
					73, 2, 0, 73, 42, 0, 69, 73, 6, 0, 73, 2, 0, 73, 42, 0, 69, 73, 7, 0, 73, 2, 0, 73, 42, 0, 69, 73, 8, 0, 73, 2, 0, 73, 42, 0, 69, 73, 9, 0, 73, 2, 0, 73, 42, 0, 69, 73, 10, 0, 
					73, 2, 0, 73, 42, 0, 69, 73, 11, 0, 73, 2, 0, 73, 42, 0, 69, 73, 12, 0, 73, 2, 0, 73, 42, 0, 69, 73, 13, 0, 73, 2, 0, 73, 42, 0, 69, 73, 14, 0, 73, 2, 0, 73, 42, 0, 69, 73, 15, 0, 
					73, 2, 0, 73, 42, 0, 69, 73, 16, 0, 73, 2, 0, 73, 42, 0, 69, 73, 17, 0, 73, 2, 0, 73, 42, 0, 69, 73, 18, 0, 73, 2, 0, 73, 42, 0, 69, 73, 19, 0, 73, 2, 0, 73, 42, 0, 69, 73, 20, 0, 
					73, 2, 0, 73, 42, 0, 69, 73, 21, 0, 73, 2, 0, 73, 42, 0, 69, 73, 22, 0, 73, 2, 0, 73, 42, 0, 69, 73, 23, 0, 73, 2, 0, 73, 42, 0, 69, 73, 24, 0, 73, 2, 0, 73, 42, 0, 69, 73, 25, 0, 
					73, 2, 0, 73, 42, 0, 69, 73, 26, 0, 73, 2, 0, 73, 42, 0, 69, 73, 27, 0, 73, 2, 0, 73, 42, 0, 69, 73, 28, 0, 73, 2, 0, 73, 42, 0, 69, 73, 29, 0, 73, 2, 0, 73, 42, 0, 69, 73, 30, 0, 
					73, 2, 0, 73, 42, 0, 69, 73, 31, 0, 73, 2, 0, 73, 42, 0, 69, 73, 32, 0, 73, 2, 0, 73, 42, 0, 69, 73, 33, 0, 73, 2, 0, 73, 42, 0, 69, 73, 34, 0, 73, 2, 0, 73, 42, 0, 69, 73, 35, 0, 
					73, 2, 0, 73, 42, 0, 69, 73, 36, 0, 73, 2, 0, 73, 42, 0, 69, 73, 37, 0, 73, 2, 0, 73, 42, 0, 69, 73, 38, 0, 73, 2, 0, 73, 42, 0, 69, 73, 39, 0, 73, 2, 0, 73, 42, 0, 69, 73, 40, 0, 
					73, 2, 0, 73, 42, 0, 69, 73, 41, 0, 73, 2, 0, 73, 42, 0, 69, 73, 42, 0, 73, 2, 0, 73, 42, 0, 69, 73, 43, 0, 73, 2, 0, 73, 42, 0, 69, 73, 44, 0, 73, 2, 0, 73, 42, 0, 69, 73, 45, 0, 
					73, 2, 0, 73, 42, 0, 69, 73, 46, 0, 73, 2, 0, 73, 42, 0, 69, 73, 47, 0, 73, 2, 0, 73, 42, 0, 69, 73, 48, 0, 73, 2, 0, 73, 42, 0, 69, 73, 49, 0, 73, 2, 0, 73, 42, 0, 69, 73, 50, 0, 
					73, 2, 0, 73, 42, 0, 69, 73, 51, 0, 73, 2, 0, 73, 42, 0, 69, 73, 52, 0, 73, 2, 0, 73, 42, 0, 69, 73, 53, 0, 73, 2, 0, 73, 42, 0, 69, 73, 54, 0, 73, 2, 0, 73, 42, 0, 69, 73, 55, 0, 
					73, 2, 0, 73, 42, 0, 69, 73, 56, 0, 73, 2, 0, 73, 42, 0, 69, 73, 57, 0, 73, 2, 0, 73, 42, 0, 69, 73, 58, 0, 73, 2, 0, 73, 42, 0, 69, 73, 59, 0, 73, 2, 0, 73, 42, 0, 69, 73, 60, 0, 
					73, 2, 0, 73, 42, 0, 69, 73, 61, 0, 73, 2, 0, 73, 42, 0, 69, 73, 62, 0, 73, 2, 0, 73, 42, 0, 69, 73, 63, 0, 73, 2, 0, 73, 42, 0, 69, 73, 64, 0, 73, 2, 0, 73, 42, 0, 69, 73, 65, 0, 
					73, 2, 0, 73, 42, 0, 69, 73, 66, 0, 73, 2, 0, 73, 42, 0, 69, 73, 67, 0, 73, 2, 0, 73, 42, 0, 69, 73, 68, 0, 73, 2, 0, 73, 42, 0, 69, 77, 3, 1, 98, 76, 73, 25, 0, 69, 73, 0, 0, 73, 
					2, 0, 73, 43, 0, 69, 73, 6, 0, 73, 2, 0, 73, 43, 0, 69, 73, 7, 0, 73, 2, 0, 73, 43, 0, 69, 73, 8, 0, 73, 2, 0, 73, 43, 0, 69, 73, 9, 0, 73, 2, 0, 73, 43, 0, 69, 73, 10, 0, 73, 
					2, 0, 73, 43, 0, 69, 73, 11, 0, 73, 2, 0, 73, 43, 0, 69, 73, 12, 0, 73, 2, 0, 73, 43, 0, 69, 73, 13, 0, 73, 2, 0, 73, 43, 0, 69, 73, 14, 0, 73, 2, 0, 73, 43, 0, 69, 73, 15, 0, 73, 
					2, 0, 73, 43, 0, 69, 73, 16, 0, 73, 2, 0, 73, 43, 0, 69, 73, 17, 0, 73, 2, 0, 73, 43, 0, 69, 73, 18, 0, 73, 2, 0, 73, 43, 0, 69, 73, 19, 0, 73, 2, 0, 73, 43, 0, 69, 73, 20, 0, 73, 
					2, 0, 73, 43, 0, 69, 73, 21, 0, 73, 2, 0, 73, 43, 0, 69, 73, 22, 0, 73, 2, 0, 73, 43, 0, 69, 73, 23, 0, 73, 2, 0, 73, 43, 0, 69, 73, 24, 0, 73, 2, 0, 73, 43, 0, 69, 73, 25, 0, 73, 
					2, 0, 73, 43, 0, 69, 73, 26, 0, 73, 2, 0, 73, 43, 0, 69, 73, 27, 0, 73, 2, 0, 73, 43, 0, 69, 73, 28, 0, 73, 2, 0, 73, 43, 0, 69, 73, 29, 0, 73, 2, 0, 73, 43, 0, 69, 73, 30, 0, 73, 
					2, 0, 73, 43, 0, 69, 73, 31, 0, 73, 2, 0, 73, 43, 0, 69, 73, 32, 0, 73, 2, 0, 73, 43, 0, 69, 73, 33, 0, 73, 2, 0, 73, 43, 0, 69, 73, 34, 0, 73, 2, 0, 73, 43, 0, 69, 73, 35, 0, 73, 
					2, 0, 73, 43, 0, 69, 73, 36, 0, 73, 2, 0, 73, 43, 0, 69, 73, 37, 0, 73, 2, 0, 73, 43, 0, 69, 73, 38, 0, 73, 2, 0, 73, 43, 0, 69, 73, 39, 0, 73, 2, 0, 73, 43, 0, 69, 73, 40, 0, 73, 
					2, 0, 73, 43, 0, 69, 73, 41, 0, 73, 2, 0, 73, 43, 0, 69, 73, 42, 0, 73, 2, 0, 73, 43, 0, 69, 73, 43, 0, 73, 2, 0, 73, 43, 0, 69, 73, 44, 0, 73, 2, 0, 73, 43, 0, 69, 73, 45, 0, 73, 
					2, 0, 73, 43, 0, 69, 73, 46, 0, 73, 2, 0, 73, 43, 0, 69, 73, 47, 0, 73, 2, 0, 73, 43, 0, 69, 73, 48, 0, 73, 2, 0, 73, 43, 0, 69, 73, 49, 0, 73, 2, 0, 73, 43, 0, 69, 73, 50, 0, 73, 
					2, 0, 73, 43, 0, 69, 73, 51, 0, 73, 2, 0, 73, 43, 0, 69, 73, 52, 0, 73, 2, 0, 73, 43, 0, 69, 73, 53, 0, 73, 2, 0, 73, 43, 0, 69, 73, 54, 0, 73, 2, 0, 73, 43, 0, 69, 73, 55, 0, 73, 
					2, 0, 73, 43, 0, 69, 73, 56, 0, 73, 2, 0, 73, 43, 0, 69, 73, 57, 0, 73, 2, 0, 73, 43, 0, 69, 73, 58, 0, 73, 2, 0, 73, 43, 0, 69, 73, 59, 0, 73, 2, 0, 73, 43, 0, 69, 73, 60, 0, 73, 
					2, 0, 73, 43, 0, 69, 73, 61, 0, 73, 2, 0, 73, 43, 0, 69, 73, 62, 0, 73, 2, 0, 73, 43, 0, 69, 73, 63, 0, 73, 2, 0, 73, 43, 0, 69, 73, 64, 0, 73, 2, 0, 73, 43, 0, 69, 73, 65, 0, 73, 
					2, 0, 73, 43, 0, 69, 73, 66, 0, 73, 2, 0, 73, 43, 0, 69, 73, 67, 0, 73, 2, 0, 73, 43, 0, 69, 73, 68, 0, 73, 2, 0, 73, 43, 0, 69, 77, 3, 1, 98, 76, 73, 26, 0, 69, 73, 0, 0, 73, 2, 
					0, 73, 5, 0, 69, 73, 6, 0, 73, 2, 0, 73, 5, 0, 69, 73, 7, 0, 73, 2, 0, 73, 5, 0, 69, 73, 8, 0, 73, 2, 0, 73, 5, 0, 69, 73, 9, 0, 73, 2, 0, 73, 5, 0, 69, 73, 10, 0, 73, 2, 
					0, 73, 5, 0, 69, 73, 11, 0, 73, 2, 0, 73, 5, 0, 69, 73, 12, 0, 73, 2, 0, 73, 5, 0, 69, 73, 13, 0, 73, 2, 0, 73, 5, 0, 69, 73, 14, 0, 73, 2, 0, 73, 5, 0, 69, 73, 15, 0, 73, 2, 
					0, 73, 5, 0, 69, 73, 16, 0, 73, 2, 0, 73, 5, 0, 69, 73, 17, 0, 73, 2, 0, 73, 5, 0, 69, 73, 18, 0, 73, 2, 0, 73, 5, 0, 69, 73, 19, 0, 73, 2, 0, 73, 5, 0, 69, 73, 20, 0, 73, 2, 
					0, 73, 5, 0, 69, 73, 21, 0, 73, 2, 0, 73, 5, 0, 69, 73, 22, 0, 73, 2, 0, 73, 5, 0, 69, 73, 23, 0, 73, 2, 0, 73, 5, 0, 69, 73, 24, 0, 73, 2, 0, 73, 5, 0, 69, 73, 25, 0, 73, 2, 
					0, 73, 5, 0, 69, 73, 26, 0, 73, 2, 0, 73, 5, 0, 69, 73, 27, 0, 73, 2, 0, 73, 5, 0, 69, 73, 28, 0, 73, 2, 0, 73, 5, 0, 69, 73, 29, 0, 73, 2, 0, 73, 5, 0, 69, 73, 30, 0, 73, 2, 
					0, 73, 5, 0, 69, 73, 31, 0, 73, 2, 0, 73, 5, 0, 69, 73, 32, 0, 73, 2, 0, 73, 5, 0, 69, 73, 33, 0, 73, 2, 0, 73, 5, 0, 69, 73, 34, 0, 73, 2, 0, 73, 5, 0, 69, 73, 35, 0, 73, 2, 
					0, 73, 5, 0, 69, 73, 36, 0, 73, 2, 0, 73, 5, 0, 69, 73, 37, 0, 73, 2, 0, 73, 5, 0, 69, 73, 38, 0, 73, 2, 0, 73, 5, 0, 69, 73, 39, 0, 73, 2, 0, 73, 5, 0, 69, 73, 40, 0, 73, 2, 
					0, 73, 5, 0, 69, 73, 41, 0, 73, 2, 0, 73, 5, 0, 69, 73, 42, 0, 73, 2, 0, 73, 5, 0, 69, 73, 43, 0, 73, 2, 0, 73, 5, 0, 69, 73, 44, 0, 73, 2, 0, 73, 5, 0, 69, 73, 45, 0, 73, 2, 
					0, 73, 5, 0, 69, 73, 46, 0, 73, 2, 0, 73, 5, 0, 69, 73, 47, 0, 73, 2, 0, 73, 5, 0, 69, 73, 48, 0, 73, 2, 0, 73, 5, 0, 69, 73, 49, 0, 73, 2, 0, 73, 5, 0, 69, 73, 50, 0, 73, 2, 
					0, 73, 5, 0, 69, 73, 51, 0, 73, 2, 0, 73, 5, 0, 69, 73, 52, 0, 73, 2, 0, 73, 5, 0, 69, 73, 53, 0, 73, 2, 0, 73, 5, 0, 69, 73, 54, 0, 73, 2, 0, 73, 5, 0, 69, 73, 55, 0, 73, 2, 
					0, 73, 5, 0, 69, 73, 56, 0, 73, 2, 0, 73, 5, 0, 69, 73, 57, 0, 73, 2, 0, 73, 5, 0, 69, 73, 58, 0, 73, 2, 0, 73, 5, 0, 69, 73, 59, 0, 73, 2, 0, 73, 5, 0, 69, 73, 60, 0, 73, 2, 
					0, 73, 5, 0, 69, 73, 61, 0, 73, 2, 0, 73, 5, 0, 69, 73, 62, 0, 73, 2, 0, 73, 5, 0, 69, 73, 63, 0, 73, 2, 0, 73, 5, 0, 69, 73, 64, 0, 73, 2, 0, 73, 5, 0, 69, 73, 65, 0, 73, 2, 
					0, 73, 5, 0, 69, 73, 66, 0, 73, 2, 0, 73, 5, 0, 69, 73, 67, 0, 73, 2, 0, 73, 5, 0, 69, 73, 68, 0, 73, 2, 0, 73, 5, 0, 69, 77, 3, 1, 98, 76, 73, 27, 0, 69, 73, 0, 0, 73, 2, 0, 
					73, 10, 0, 69, 73, 6, 0, 73, 2, 0, 73, 10, 0, 69, 73, 7, 0, 73, 2, 0, 73, 10, 0, 69, 73, 8, 0, 73, 2, 0, 73, 10, 0, 69, 73, 9, 0, 73, 2, 0, 73, 10, 0, 69, 73, 10, 0, 73, 2, 0, 
					73, 10, 0, 69, 73, 11, 0, 73, 2, 0, 73, 10, 0, 69, 73, 12, 0, 73, 2, 0, 73, 10, 0, 69, 73, 13, 0, 73, 2, 0, 73, 10, 0, 69, 73, 14, 0, 73, 2, 0, 73, 10, 0, 69, 73, 15, 0, 73, 2, 0, 
					73, 10, 0, 69, 73, 16, 0, 73, 2, 0, 73, 10, 0, 69, 73, 17, 0, 73, 2, 0, 73, 10, 0, 69, 73, 18, 0, 73, 2, 0, 73, 10, 0, 69, 73, 19, 0, 73, 2, 0, 73, 10, 0, 69, 73, 20, 0, 73, 2, 0, 
					73, 10, 0, 69, 73, 21, 0, 73, 2, 0, 73, 10, 0, 69, 73, 22, 0, 73, 2, 0, 73, 10, 0, 69, 73, 23, 0, 73, 2, 0, 73, 10, 0, 69, 73, 24, 0, 73, 2, 0, 73, 10, 0, 69, 73, 25, 0, 73, 2, 0, 
					73, 10, 0, 69, 73, 26, 0, 73, 2, 0, 73, 10, 0, 69, 73, 27, 0, 73, 2, 0, 73, 10, 0, 69, 73, 28, 0, 73, 2, 0, 73, 10, 0, 69, 73, 29, 0, 73, 2, 0, 73, 10, 0, 69, 73, 30, 0, 73, 2, 0, 
					73, 10, 0, 69, 73, 31, 0, 73, 2, 0, 73, 10, 0, 69, 73, 32, 0, 73, 2, 0, 73, 10, 0, 69, 73, 33, 0, 73, 2, 0, 73, 10, 0, 69, 73, 34, 0, 73, 2, 0, 73, 10, 0, 69, 73, 35, 0, 73, 2, 0, 
					73, 10, 0, 69, 73, 36, 0, 73, 2, 0, 73, 10, 0, 69, 73, 37, 0, 73, 2, 0, 73, 10, 0, 69, 73, 38, 0, 73, 2, 0, 73, 10, 0, 69, 73, 39, 0, 73, 2, 0, 73, 10, 0, 69, 73, 40, 0, 73, 2, 0, 
					73, 10, 0, 69, 73, 41, 0, 73, 2, 0, 73, 10, 0, 69, 73, 42, 0, 73, 2, 0, 73, 10, 0, 69, 73, 43, 0, 73, 2, 0, 73, 10, 0, 69, 73, 44, 0, 73, 2, 0, 73, 10, 0, 69, 73, 45, 0, 73, 2, 0, 
					73, 10, 0, 69, 73, 46, 0, 73, 2, 0, 73, 10, 0, 69, 73, 47, 0, 73, 2, 0, 73, 10, 0, 69, 73, 48, 0, 73, 2, 0, 73, 10, 0, 69, 73, 49, 0, 73, 2, 0, 73, 10, 0, 69, 73, 50, 0, 73, 2, 0, 
					73, 10, 0, 69, 73, 51, 0, 73, 2, 0, 73, 10, 0, 69, 73, 52, 0, 73, 2, 0, 73, 10, 0, 69, 73, 53, 0, 73, 2, 0, 73, 10, 0, 69, 73, 54, 0, 73, 2, 0, 73, 10, 0, 69, 73, 55, 0, 73, 2, 0, 
					73, 10, 0, 69, 73, 56, 0, 73, 2, 0, 73, 10, 0, 69, 73, 57, 0, 73, 2, 0, 73, 10, 0, 69, 73, 58, 0, 73, 2, 0, 73, 10, 0, 69, 73, 59, 0, 73, 2, 0, 73, 10, 0, 69, 73, 60, 0, 73, 2, 0, 
					73, 10, 0, 69, 73, 61, 0, 73, 2, 0, 73, 10, 0, 69, 73, 62, 0, 73, 2, 0, 73, 10, 0, 69, 73, 63, 0, 73, 2, 0, 73, 10, 0, 69, 73, 64, 0, 73, 2, 0, 73, 10, 0, 69, 73, 65, 0, 73, 2, 0, 
					73, 10, 0, 69, 73, 66, 0, 73, 2, 0, 73, 10, 0, 69, 73, 67, 0, 73, 2, 0, 73, 10, 0, 69, 73, 68, 0, 73, 2, 0, 73, 10, 0, 69, 77, 3, 1, 98, 76, 73, 28, 0, 69, 73, 0, 0, 73, 2, 0, 73, 
					13, 0, 69, 73, 6, 0, 73, 2, 0, 73, 13, 0, 69, 73, 7, 0, 73, 2, 0, 73, 13, 0, 69, 73, 8, 0, 73, 2, 0, 73, 13, 0, 69, 73, 9, 0, 73, 2, 0, 73, 13, 0, 69, 73, 10, 0, 73, 2, 0, 73, 
					13, 0, 69, 73, 11, 0, 73, 2, 0, 73, 13, 0, 69, 73, 12, 0, 73, 2, 0, 73, 13, 0, 69, 73, 13, 0, 73, 2, 0, 73, 13, 0, 69, 73, 14, 0, 73, 2, 0, 73, 13, 0, 69, 73, 15, 0, 73, 2, 0, 73, 
					13, 0, 69, 73, 16, 0, 73, 2, 0, 73, 13, 0, 69, 73, 17, 0, 73, 2, 0, 73, 13, 0, 69, 73, 18, 0, 73, 2, 0, 73, 13, 0, 69, 73, 19, 0, 73, 2, 0, 73, 13, 0, 69, 73, 20, 0, 73, 2, 0, 73, 
					13, 0, 69, 73, 21, 0, 73, 2, 0, 73, 13, 0, 69, 73, 22, 0, 73, 2, 0, 73, 13, 0, 69, 73, 23, 0, 73, 2, 0, 73, 13, 0, 69, 73, 24, 0, 73, 2, 0, 73, 13, 0, 69, 73, 25, 0, 73, 2, 0, 73, 
					13, 0, 69, 73, 26, 0, 73, 2, 0, 73, 13, 0, 69, 73, 27, 0, 73, 2, 0, 73, 13, 0, 69, 73, 28, 0, 73, 2, 0, 73, 13, 0, 69, 73, 29, 0, 73, 2, 0, 73, 13, 0, 69, 73, 30, 0, 73, 2, 0, 73, 
					13, 0, 69, 73, 31, 0, 73, 2, 0, 73, 13, 0, 69, 73, 32, 0, 73, 2, 0, 73, 13, 0, 69, 73, 33, 0, 73, 2, 0, 73, 13, 0, 69, 73, 34, 0, 73, 2, 0, 73, 13, 0, 69, 73, 35, 0, 73, 2, 0, 73, 
					13, 0, 69, 73, 36, 0, 73, 2, 0, 73, 13, 0, 69, 73, 37, 0, 73, 2, 0, 73, 13, 0, 69, 73, 38, 0, 73, 2, 0, 73, 13, 0, 69, 73, 39, 0, 73, 2, 0, 73, 13, 0, 69, 73, 40, 0, 73, 2, 0, 73, 
					13, 0, 69, 73, 41, 0, 73, 2, 0, 73, 13, 0, 69, 73, 42, 0, 73, 2, 0, 73, 13, 0, 69, 73, 43, 0, 73, 2, 0, 73, 13, 0, 69, 73, 44, 0, 73, 2, 0, 73, 13, 0, 69, 73, 45, 0, 73, 2, 0, 73, 
					13, 0, 69, 73, 46, 0, 73, 2, 0, 73, 13, 0, 69, 73, 47, 0, 73, 2, 0, 73, 13, 0, 69, 73, 48, 0, 73, 2, 0, 73, 13, 0, 69, 73, 49, 0, 73, 2, 0, 73, 13, 0, 69, 73, 50, 0, 73, 2, 0, 73, 
					13, 0, 69, 73, 51, 0, 73, 2, 0, 73, 13, 0, 69, 73, 52, 0, 73, 2, 0, 73, 13, 0, 69, 73, 53, 0, 73, 2, 0, 73, 13, 0, 69, 73, 54, 0, 73, 2, 0, 73, 13, 0, 69, 73, 55, 0, 73, 2, 0, 73, 
					13, 0, 69, 73, 56, 0, 73, 2, 0, 73, 13, 0, 69, 73, 57, 0, 73, 2, 0, 73, 13, 0, 69, 73, 58, 0, 73, 2, 0, 73, 13, 0, 69, 73, 59, 0, 73, 2, 0, 73, 13, 0, 69, 73, 60, 0, 73, 2, 0, 73, 
					13, 0, 69, 73, 61, 0, 73, 2, 0, 73, 13, 0, 69, 73, 62, 0, 73, 2, 0, 73, 13, 0, 69, 73, 63, 0, 73, 2, 0, 73, 13, 0, 69, 73, 64, 0, 73, 2, 0, 73, 13, 0, 69, 73, 65, 0, 73, 2, 0, 73, 
					13, 0, 69, 73, 66, 0, 73, 2, 0, 73, 13, 0, 69, 73, 67, 0, 73, 2, 0, 73, 13, 0, 69, 73, 68, 0, 73, 2, 0, 73, 13, 0, 69, 77, 3, 1, 98, 76, 73, 29, 0, 69, 73, 0, 0, 73, 2, 0, 73, 62, 
					0, 69, 73, 6, 0, 73, 2, 0, 73, 62, 0, 69, 73, 7, 0, 73, 2, 0, 73, 62, 0, 69, 73, 8, 0, 73, 2, 0, 73, 62, 0, 69, 73, 9, 0, 73, 2, 0, 73, 62, 0, 69, 73, 10, 0, 73, 2, 0, 73, 62, 
					0, 69, 73, 11, 0, 73, 2, 0, 73, 62, 0, 69, 73, 12, 0, 73, 2, 0, 73, 62, 0, 69, 73, 13, 0, 73, 2, 0, 73, 62, 0, 69, 73, 14, 0, 73, 2, 0, 73, 62, 0, 69, 73, 15, 0, 73, 2, 0, 73, 62, 
					0, 69, 73, 16, 0, 73, 2, 0, 73, 62, 0, 69, 73, 17, 0, 73, 2, 0, 73, 62, 0, 69, 73, 18, 0, 73, 2, 0, 73, 62, 0, 69, 73, 19, 0, 73, 2, 0, 73, 62, 0, 69, 73, 20, 0, 73, 2, 0, 73, 62, 
					0, 69, 73, 21, 0, 73, 2, 0, 73, 62, 0, 69, 73, 22, 0, 73, 2, 0, 73, 62, 0, 69, 73, 23, 0, 73, 2, 0, 73, 62, 0, 69, 73, 24, 0, 73, 2, 0, 73, 62, 0, 69, 73, 25, 0, 73, 2, 0, 73, 62, 
					0, 69, 73, 26, 0, 73, 2, 0, 73, 62, 0, 69, 73, 27, 0, 73, 2, 0, 73, 62, 0, 69, 73, 28, 0, 73, 2, 0, 73, 62, 0, 69, 73, 29, 0, 73, 2, 0, 73, 62, 0, 69, 73, 30, 0, 73, 2, 0, 73, 62, 
					0, 69, 73, 31, 0, 73, 2, 0, 73, 62, 0, 69, 73, 32, 0, 73, 2, 0, 73, 62, 0, 69, 73, 33, 0, 73, 2, 0, 73, 62, 0, 69, 73, 34, 0, 73, 2, 0, 73, 62, 0, 69, 73, 35, 0, 73, 2, 0, 73, 62, 
					0, 69, 73, 36, 0, 73, 2, 0, 73, 62, 0, 69, 73, 37, 0, 73, 2, 0, 73, 62, 0, 69, 73, 38, 0, 73, 2, 0, 73, 62, 0, 69, 73, 39, 0, 73, 2, 0, 73, 62, 0, 69, 73, 40, 0, 73, 2, 0, 73, 62, 
					0, 69, 73, 41, 0, 73, 2, 0, 73, 62, 0, 69, 73, 42, 0, 73, 2, 0, 73, 62, 0, 69, 73, 43, 0, 73, 2, 0, 73, 62, 0, 69, 73, 44, 0, 73, 2, 0, 73, 62, 0, 69, 73, 45, 0, 73, 2, 0, 73, 62, 
					0, 69, 73, 46, 0, 73, 2, 0, 73, 62, 0, 69, 73, 47, 0, 73, 2, 0, 73, 62, 0, 69, 73, 48, 0, 73, 2, 0, 73, 62, 0, 69, 73, 49, 0, 73, 2, 0, 73, 62, 0, 69, 73, 50, 0, 73, 2, 0, 73, 62, 
					0, 69, 73, 51, 0, 73, 2, 0, 73, 62, 0, 69, 73, 52, 0, 73, 2, 0, 73, 62, 0, 69, 73, 53, 0, 73, 2, 0, 73, 62, 0, 69, 73, 54, 0, 73, 2, 0, 73, 62, 0, 69, 73, 55, 0, 73, 2, 0, 73, 62, 
					0, 69, 73, 56, 0, 73, 2, 0, 73, 62, 0, 69, 73, 57, 0, 73, 2, 0, 73, 62, 0, 69, 73, 58, 0, 73, 2, 0, 73, 62, 0, 69, 73, 59, 0, 73, 2, 0, 73, 62, 0, 69, 73, 60, 0, 73, 2, 0, 73, 62, 
					0, 69, 73, 61, 0, 73, 2, 0, 73, 62, 0, 69, 73, 62, 0, 73, 2, 0, 73, 62, 0, 69, 73, 63, 0, 73, 2, 0, 73, 62, 0, 69, 73, 64, 0, 73, 2, 0, 73, 62, 0, 69, 73, 65, 0, 73, 2, 0, 73, 62, 
					0, 69, 73, 66, 0, 73, 2, 0, 73, 62, 0, 69, 73, 67, 0, 73, 2, 0, 73, 62, 0, 69, 73, 68, 0, 73, 2, 0, 73, 62, 0, 69, 77, 3, 1, 98, 76, 73, 30, 0, 69, 73, 0, 0, 73, 2, 0, 73, 2, 0, 
					69, 73, 6, 0, 73, 2, 0, 73, 2, 0, 69, 73, 7, 0, 73, 2, 0, 73, 2, 0, 69, 73, 8, 0, 73, 2, 0, 73, 2, 0, 69, 73, 9, 0, 73, 2, 0, 73, 2, 0, 69, 73, 10, 0, 73, 2, 0, 73, 2, 0, 
					69, 73, 11, 0, 73, 2, 0, 73, 2, 0, 69, 73, 12, 0, 73, 2, 0, 73, 2, 0, 69, 73, 13, 0, 73, 2, 0, 73, 2, 0, 69, 73, 14, 0, 73, 2, 0, 73, 2, 0, 69, 73, 15, 0, 73, 2, 0, 73, 2, 0, 
					69, 73, 16, 0, 73, 2, 0, 73, 2, 0, 69, 73, 17, 0, 73, 2, 0, 73, 2, 0, 69, 73, 18, 0, 73, 2, 0, 73, 2, 0, 69, 73, 19, 0, 73, 2, 0, 73, 2, 0, 69, 73, 20, 0, 73, 2, 0, 73, 2, 0, 
					69, 73, 21, 0, 73, 2, 0, 73, 2, 0, 69, 73, 22, 0, 73, 2, 0, 73, 2, 0, 69, 73, 23, 0, 73, 2, 0, 73, 2, 0, 69, 73, 24, 0, 73, 2, 0, 73, 2, 0, 69, 73, 25, 0, 73, 2, 0, 73, 2, 0, 
					69, 73, 26, 0, 73, 2, 0, 73, 2, 0, 69, 73, 27, 0, 73, 2, 0, 73, 2, 0, 69, 73, 28, 0, 73, 2, 0, 73, 2, 0, 69, 73, 29, 0, 73, 2, 0, 73, 2, 0, 69, 73, 30, 0, 73, 2, 0, 73, 2, 0, 
					69, 73, 31, 0, 73, 2, 0, 73, 2, 0, 69, 73, 32, 0, 73, 2, 0, 73, 2, 0, 69, 73, 33, 0, 73, 2, 0, 73, 2, 0, 69, 73, 34, 0, 73, 2, 0, 73, 2, 0, 69, 73, 35, 0, 73, 2, 0, 73, 2, 0, 
					69, 73, 36, 0, 73, 2, 0, 73, 2, 0, 69, 73, 37, 0, 73, 2, 0, 73, 2, 0, 69, 73, 38, 0, 73, 2, 0, 73, 2, 0, 69, 73, 39, 0, 73, 2, 0, 73, 2, 0, 69, 73, 40, 0, 73, 2, 0, 73, 2, 0, 
					69, 73, 41, 0, 73, 2, 0, 73, 2, 0, 69, 73, 42, 0, 73, 2, 0, 73, 2, 0, 69, 73, 43, 0, 73, 2, 0, 73, 2, 0, 69, 73, 44, 0, 73, 2, 0, 73, 2, 0, 69, 73, 45, 0, 73, 2, 0, 73, 2, 0, 
					69, 73, 46, 0, 73, 2, 0, 73, 2, 0, 69, 73, 47, 0, 73, 2, 0, 73, 2, 0, 69, 73, 48, 0, 73, 2, 0, 73, 2, 0, 69, 73, 49, 0, 73, 2, 0, 73, 2, 0, 69, 73, 50, 0, 73, 2, 0, 73, 2, 0, 
					69, 73, 51, 0, 73, 2, 0, 73, 2, 0, 69, 73, 52, 0, 73, 2, 0, 73, 2, 0, 69, 73, 53, 0, 73, 2, 0, 73, 2, 0, 69, 73, 54, 0, 73, 2, 0, 73, 2, 0, 69, 73, 55, 0, 73, 2, 0, 73, 2, 0, 
					69, 73, 56, 0, 73, 2, 0, 73, 2, 0, 69, 73, 57, 0, 73, 2, 0, 73, 2, 0, 69, 73, 58, 0, 73, 2, 0, 73, 2, 0, 69, 73, 59, 0, 73, 2, 0, 73, 2, 0, 69, 73, 60, 0, 73, 2, 0, 73, 2, 0, 
					69, 73, 61, 0, 73, 2, 0, 73, 2, 0, 69, 73, 62, 0, 73, 2, 0, 73, 2, 0, 69, 73, 63, 0, 73, 2, 0, 73, 2, 0, 69, 73, 64, 0, 73, 2, 0, 73, 2, 0, 69, 73, 65, 0, 73, 2, 0, 73, 2, 0, 
					69, 73, 66, 0, 73, 2, 0, 73, 2, 0, 69, 73, 67, 0, 73, 2, 0, 73, 2, 0, 69, 73, 68, 0, 73, 2, 0, 73, 2, 0, 69, 77, 3, 1, 98, 76, 73, 31, 0, 69, 73, 0, 0, 73, 2, 0, 73, 45, 0, 69, 
					73, 6, 0, 73, 2, 0, 73, 45, 0, 69, 73, 7, 0, 73, 2, 0, 73, 45, 0, 69, 73, 8, 0, 73, 2, 0, 73, 45, 0, 69, 73, 9, 0, 73, 2, 0, 73, 45, 0, 69, 73, 10, 0, 73, 2, 0, 73, 45, 0, 69, 
					73, 11, 0, 73, 2, 0, 73, 45, 0, 69, 73, 12, 0, 73, 2, 0, 73, 45, 0, 69, 73, 13, 0, 73, 2, 0, 73, 45, 0, 69, 73, 14, 0, 73, 2, 0, 73, 45, 0, 69, 73, 15, 0, 73, 2, 0, 73, 45, 0, 69, 
					73, 16, 0, 73, 2, 0, 73, 45, 0, 69, 73, 17, 0, 73, 2, 0, 73, 45, 0, 69, 73, 18, 0, 73, 2, 0, 73, 45, 0, 69, 73, 19, 0, 73, 2, 0, 73, 45, 0, 69, 73, 20, 0, 73, 2, 0, 73, 45, 0, 69, 
					73, 21, 0, 73, 2, 0, 73, 45, 0, 69, 73, 22, 0, 73, 2, 0, 73, 45, 0, 69, 73, 23, 0, 73, 2, 0, 73, 45, 0, 69, 73, 24, 0, 73, 2, 0, 73, 45, 0, 69, 73, 25, 0, 73, 2, 0, 73, 45, 0, 69, 
					73, 26, 0, 73, 2, 0, 73, 45, 0, 69, 73, 27, 0, 73, 2, 0, 73, 45, 0, 69, 73, 28, 0, 73, 2, 0, 73, 45, 0, 69, 73, 29, 0, 73, 2, 0, 73, 45, 0, 69, 73, 30, 0, 73, 2, 0, 73, 45, 0, 69, 
					73, 31, 0, 73, 2, 0, 73, 45, 0, 69, 73, 32, 0, 73, 2, 0, 73, 45, 0, 69, 73, 33, 0, 73, 2, 0, 73, 45, 0, 69, 73, 34, 0, 73, 2, 0, 73, 45, 0, 69, 73, 35, 0, 73, 2, 0, 73, 45, 0, 69, 
					73, 36, 0, 73, 2, 0, 73, 45, 0, 69, 73, 37, 0, 73, 2, 0, 73, 45, 0, 69, 73, 38, 0, 73, 2, 0, 73, 45, 0, 69, 73, 39, 0, 73, 2, 0, 73, 45, 0, 69, 73, 40, 0, 73, 2, 0, 73, 45, 0, 69, 
					73, 41, 0, 73, 2, 0, 73, 45, 0, 69, 73, 42, 0, 73, 2, 0, 73, 45, 0, 69, 73, 43, 0, 73, 2, 0, 73, 45, 0, 69, 73, 44, 0, 73, 2, 0, 73, 45, 0, 69, 73, 45, 0, 73, 2, 0, 73, 45, 0, 69, 
					73, 46, 0, 73, 2, 0, 73, 45, 0, 69, 73, 47, 0, 73, 2, 0, 73, 45, 0, 69, 73, 48, 0, 73, 2, 0, 73, 45, 0, 69, 73, 49, 0, 73, 2, 0, 73, 45, 0, 69, 73, 50, 0, 73, 2, 0, 73, 45, 0, 69, 
					73, 51, 0, 73, 2, 0, 73, 45, 0, 69, 73, 52, 0, 73, 2, 0, 73, 45, 0, 69, 73, 53, 0, 73, 2, 0, 73, 45, 0, 69, 73, 54, 0, 73, 2, 0, 73, 45, 0, 69, 73, 55, 0, 73, 2, 0, 73, 45, 0, 69, 
					73, 56, 0, 73, 2, 0, 73, 45, 0, 69, 73, 57, 0, 73, 2, 0, 73, 45, 0, 69, 73, 58, 0, 73, 2, 0, 73, 45, 0, 69, 73, 59, 0, 73, 2, 0, 73, 45, 0, 69, 73, 60, 0, 73, 2, 0, 73, 45, 0, 69, 
					73, 61, 0, 73, 2, 0, 73, 45, 0, 69, 73, 62, 0, 73, 2, 0, 73, 45, 0, 69, 73, 63, 0, 73, 2, 0, 73, 45, 0, 69, 73, 64, 0, 73, 2, 0, 73, 45, 0, 69, 73, 65, 0, 73, 2, 0, 73, 45, 0, 69, 
					73, 66, 0, 73, 2, 0, 73, 45, 0, 69, 73, 67, 0, 73, 2, 0, 73, 45, 0, 69, 73, 68, 0, 73, 2, 0, 73, 45, 0, 69, 77, 3, 1, 98, 76, 73, 32, 0, 69, 73, 0, 0, 73, 2, 0, 73, 47, 0, 69, 73, 
					6, 0, 73, 2, 0, 73, 47, 0, 69, 73, 7, 0, 73, 2, 0, 73, 47, 0, 69, 73, 8, 0, 73, 2, 0, 73, 47, 0, 69, 73, 9, 0, 73, 2, 0, 73, 47, 0, 69, 73, 10, 0, 73, 2, 0, 73, 47, 0, 69, 73, 
					11, 0, 73, 2, 0, 73, 47, 0, 69, 73, 12, 0, 73, 2, 0, 73, 47, 0, 69, 73, 13, 0, 73, 2, 0, 73, 47, 0, 69, 73, 14, 0, 73, 2, 0, 73, 47, 0, 69, 73, 15, 0, 73, 2, 0, 73, 47, 0, 69, 73, 
					16, 0, 73, 2, 0, 73, 47, 0, 69, 73, 17, 0, 73, 2, 0, 73, 47, 0, 69, 73, 18, 0, 73, 2, 0, 73, 47, 0, 69, 73, 19, 0, 73, 2, 0, 73, 47, 0, 69, 73, 20, 0, 73, 2, 0, 73, 47, 0, 69, 73, 
					21, 0, 73, 2, 0, 73, 47, 0, 69, 73, 22, 0, 73, 2, 0, 73, 47, 0, 69, 73, 23, 0, 73, 2, 0, 73, 47, 0, 69, 73, 24, 0, 73, 2, 0, 73, 47, 0, 69, 73, 25, 0, 73, 2, 0, 73, 47, 0, 69, 73, 
					26, 0, 73, 2, 0, 73, 47, 0, 69, 73, 27, 0, 73, 2, 0, 73, 47, 0, 69, 73, 28, 0, 73, 2, 0, 73, 47, 0, 69, 73, 29, 0, 73, 2, 0, 73, 47, 0, 69, 73, 30, 0, 73, 2, 0, 73, 47, 0, 69, 73, 
					31, 0, 73, 2, 0, 73, 47, 0, 69, 73, 32, 0, 73, 2, 0, 73, 47, 0, 69, 73, 33, 0, 73, 2, 0, 73, 47, 0, 69, 73, 34, 0, 73, 2, 0, 73, 47, 0, 69, 73, 35, 0, 73, 2, 0, 73, 47, 0, 69, 73, 
					36, 0, 73, 2, 0, 73, 47, 0, 69, 73, 37, 0, 73, 2, 0, 73, 47, 0, 69, 73, 38, 0, 73, 2, 0, 73, 47, 0, 69, 73, 39, 0, 73, 2, 0, 73, 47, 0, 69, 73, 40, 0, 73, 2, 0, 73, 47, 0, 69, 73, 
					41, 0, 73, 2, 0, 73, 47, 0, 69, 73, 42, 0, 73, 2, 0, 73, 47, 0, 69, 73, 43, 0, 73, 2, 0, 73, 47, 0, 69, 73, 44, 0, 73, 2, 0, 73, 47, 0, 69, 73, 45, 0, 73, 2, 0, 73, 47, 0, 69, 73, 
					46, 0, 73, 2, 0, 73, 47, 0, 69, 73, 47, 0, 73, 2, 0, 73, 47, 0, 69, 73, 48, 0, 73, 2, 0, 73, 47, 0, 69, 73, 49, 0, 73, 2, 0, 73, 47, 0, 69, 73, 50, 0, 73, 2, 0, 73, 47, 0, 69, 73, 
					51, 0, 73, 2, 0, 73, 47, 0, 69, 73, 52, 0, 73, 2, 0, 73, 47, 0, 69, 73, 53, 0, 73, 2, 0, 73, 47, 0, 69, 73, 54, 0, 73, 2, 0, 73, 47, 0, 69, 73, 55, 0, 73, 2, 0, 73, 47, 0, 69, 73, 
					56, 0, 73, 2, 0, 73, 47, 0, 69, 73, 57, 0, 73, 2, 0, 73, 47, 0, 69, 73, 58, 0, 73, 2, 0, 73, 47, 0, 69, 73, 59, 0, 73, 2, 0, 73, 47, 0, 69, 73, 60, 0, 73, 2, 0, 73, 47, 0, 69, 73, 
					61, 0, 73, 2, 0, 73, 47, 0, 69, 73, 62, 0, 73, 2, 0, 73, 47, 0, 69, 73, 63, 0, 73, 2, 0, 73, 47, 0, 69, 73, 64, 0, 73, 2, 0, 73, 47, 0, 69, 73, 65, 0, 73, 2, 0, 73, 47, 0, 69, 73, 
					66, 0, 73, 2, 0, 73, 47, 0, 69, 73, 67, 0, 73, 2, 0, 73, 47, 0, 69, 73, 68, 0, 73, 2, 0, 73, 47, 0, 69, 77, 3, 1, 98, 76, 73, 33, 0, 69, 73, 0, 0, 73, 2, 0, 73, 21, 0, 69, 73, 6, 
					0, 73, 2, 0, 73, 21, 0, 69, 73, 7, 0, 73, 2, 0, 73, 21, 0, 69, 73, 8, 0, 73, 2, 0, 73, 21, 0, 69, 73, 9, 0, 73, 2, 0, 73, 21, 0, 69, 73, 10, 0, 73, 2, 0, 73, 21, 0, 69, 73, 11, 
					0, 73, 2, 0, 73, 21, 0, 69, 73, 12, 0, 73, 2, 0, 73, 21, 0, 69, 73, 13, 0, 73, 2, 0, 73, 21, 0, 69, 73, 14, 0, 73, 2, 0, 73, 21, 0, 69, 73, 15, 0, 73, 2, 0, 73, 21, 0, 69, 73, 16, 
					0, 73, 2, 0, 73, 21, 0, 69, 73, 17, 0, 73, 2, 0, 73, 21, 0, 69, 73, 18, 0, 73, 2, 0, 73, 21, 0, 69, 73, 19, 0, 73, 2, 0, 73, 21, 0, 69, 73, 20, 0, 73, 2, 0, 73, 21, 0, 69, 73, 21, 
					0, 73, 2, 0, 73, 21, 0, 69, 73, 22, 0, 73, 2, 0, 73, 21, 0, 69, 73, 23, 0, 73, 2, 0, 73, 21, 0, 69, 73, 24, 0, 73, 2, 0, 73, 21, 0, 69, 73, 25, 0, 73, 2, 0, 73, 21, 0, 69, 73, 26, 
					0, 73, 2, 0, 73, 21, 0, 69, 73, 27, 0, 73, 2, 0, 73, 21, 0, 69, 73, 28, 0, 73, 2, 0, 73, 21, 0, 69, 73, 29, 0, 73, 2, 0, 73, 21, 0, 69, 73, 30, 0, 73, 2, 0, 73, 21, 0, 69, 73, 31, 
					0, 73, 2, 0, 73, 21, 0, 69, 73, 32, 0, 73, 2, 0, 73, 21, 0, 69, 73, 33, 0, 73, 2, 0, 73, 21, 0, 69, 73, 34, 0, 73, 2, 0, 73, 21, 0, 69, 73, 35, 0, 73, 2, 0, 73, 21, 0, 69, 73, 36, 
					0, 73, 2, 0, 73, 21, 0, 69, 73, 37, 0, 73, 2, 0, 73, 21, 0, 69, 73, 38, 0, 73, 2, 0, 73, 21, 0, 69, 73, 39, 0, 73, 2, 0, 73, 21, 0, 69, 73, 40, 0, 73, 2, 0, 73, 21, 0, 69, 73, 41, 
					0, 73, 2, 0, 73, 21, 0, 69, 73, 42, 0, 73, 2, 0, 73, 21, 0, 69, 73, 43, 0, 73, 2, 0, 73, 21, 0, 69, 73, 44, 0, 73, 2, 0, 73, 21, 0, 69, 73, 45, 0, 73, 2, 0, 73, 21, 0, 69, 73, 46, 
					0, 73, 2, 0, 73, 21, 0, 69, 73, 47, 0, 73, 2, 0, 73, 21, 0, 69, 73, 48, 0, 73, 2, 0, 73, 21, 0, 69, 73, 49, 0, 73, 2, 0, 73, 21, 0, 69, 73, 50, 0, 73, 2, 0, 73, 21, 0, 69, 73, 51, 
					0, 73, 2, 0, 73, 21, 0, 69, 73, 52, 0, 73, 2, 0, 73, 21, 0, 69, 73, 53, 0, 73, 2, 0, 73, 21, 0, 69, 73, 54, 0, 73, 2, 0, 73, 21, 0, 69, 73, 55, 0, 73, 2, 0, 73, 21, 0, 69, 73, 56, 
					0, 73, 2, 0, 73, 21, 0, 69, 73, 57, 0, 73, 2, 0, 73, 21, 0, 69, 73, 58, 0, 73, 2, 0, 73, 21, 0, 69, 73, 59, 0, 73, 2, 0, 73, 21, 0, 69, 73, 60, 0, 73, 2, 0, 73, 21, 0, 69, 73, 61, 
					0, 73, 2, 0, 73, 21, 0, 69, 73, 62, 0, 73, 2, 0, 73, 21, 0, 69, 73, 63, 0, 73, 2, 0, 73, 21, 0, 69, 73, 64, 0, 73, 2, 0, 73, 21, 0, 69, 73, 65, 0, 73, 2, 0, 73, 21, 0, 69, 73, 66, 
					0, 73, 2, 0, 73, 21, 0, 69, 73, 67, 0, 73, 2, 0, 73, 21, 0, 69, 73, 68, 0, 73, 2, 0, 73, 21, 0, 69, 77, 3, 1, 98, 76, 73, 34, 0, 69, 73, 0, 0, 73, 2, 0, 73, 38, 0, 69, 73, 6, 0, 
					73, 2, 0, 73, 38, 0, 69, 73, 7, 0, 73, 2, 0, 73, 38, 0, 69, 73, 8, 0, 73, 2, 0, 73, 38, 0, 69, 73, 9, 0, 73, 2, 0, 73, 38, 0, 69, 73, 10, 0, 73, 2, 0, 73, 38, 0, 69, 73, 11, 0, 
					73, 2, 0, 73, 38, 0, 69, 73, 12, 0, 73, 2, 0, 73, 38, 0, 69, 73, 13, 0, 73, 2, 0, 73, 38, 0, 69, 73, 14, 0, 73, 2, 0, 73, 38, 0, 69, 73, 15, 0, 73, 2, 0, 73, 38, 0, 69, 73, 16, 0, 
					73, 2, 0, 73, 38, 0, 69, 73, 17, 0, 73, 2, 0, 73, 38, 0, 69, 73, 18, 0, 73, 2, 0, 73, 38, 0, 69, 73, 19, 0, 73, 2, 0, 73, 38, 0, 69, 73, 20, 0, 73, 2, 0, 73, 38, 0, 69, 73, 21, 0, 
					73, 2, 0, 73, 38, 0, 69, 73, 22, 0, 73, 2, 0, 73, 38, 0, 69, 73, 23, 0, 73, 2, 0, 73, 38, 0, 69, 73, 24, 0, 73, 2, 0, 73, 38, 0, 69, 73, 25, 0, 73, 2, 0, 73, 38, 0, 69, 73, 26, 0, 
					73, 2, 0, 73, 38, 0, 69, 73, 27, 0, 73, 2, 0, 73, 38, 0, 69, 73, 28, 0, 73, 2, 0, 73, 38, 0, 69, 73, 29, 0, 73, 2, 0, 73, 38, 0, 69, 73, 30, 0, 73, 2, 0, 73, 38, 0, 69, 73, 31, 0, 
					73, 2, 0, 73, 38, 0, 69, 73, 32, 0, 73, 2, 0, 73, 38, 0, 69, 73, 33, 0, 73, 2, 0, 73, 38, 0, 69, 73, 34, 0, 73, 2, 0, 73, 38, 0, 69, 73, 35, 0, 73, 2, 0, 73, 38, 0, 69, 73, 36, 0, 
					73, 2, 0, 73, 38, 0, 69, 73, 37, 0, 73, 2, 0, 73, 38, 0, 69, 73, 38, 0, 73, 2, 0, 73, 38, 0, 69, 73, 39, 0, 73, 2, 0, 73, 38, 0, 69, 73, 40, 0, 73, 2, 0, 73, 38, 0, 69, 73, 41, 0, 
					73, 2, 0, 73, 38, 0, 69, 73, 42, 0, 73, 2, 0, 73, 38, 0, 69, 73, 43, 0, 73, 2, 0, 73, 38, 0, 69, 73, 44, 0, 73, 2, 0, 73, 38, 0, 69, 73, 45, 0, 73, 2, 0, 73, 38, 0, 69, 73, 46, 0, 
					73, 2, 0, 73, 38, 0, 69, 73, 47, 0, 73, 2, 0, 73, 38, 0, 69, 73, 48, 0, 73, 2, 0, 73, 38, 0, 69, 73, 49, 0, 73, 2, 0, 73, 38, 0, 69, 73, 50, 0, 73, 2, 0, 73, 38, 0, 69, 73, 51, 0, 
					73, 2, 0, 73, 38, 0, 69, 73, 52, 0, 73, 2, 0, 73, 38, 0, 69, 73, 53, 0, 73, 2, 0, 73, 38, 0, 69, 73, 54, 0, 73, 2, 0, 73, 38, 0, 69, 73, 55, 0, 73, 2, 0, 73, 38, 0, 69, 73, 56, 0, 
					73, 2, 0, 73, 38, 0, 69, 73, 57, 0, 73, 2, 0, 73, 38, 0, 69, 73, 58, 0, 73, 2, 0, 73, 38, 0, 69, 73, 59, 0, 73, 2, 0, 73, 38, 0, 69, 73, 60, 0, 73, 2, 0, 73, 38, 0, 69, 73, 61, 0, 
					73, 2, 0, 73, 38, 0, 69, 73, 62, 0, 73, 2, 0, 73, 38, 0, 69, 73, 63, 0, 73, 2, 0, 73, 38, 0, 69, 73, 64, 0, 73, 2, 0, 73, 38, 0, 69, 73, 65, 0, 73, 2, 0, 73, 38, 0, 69, 73, 66, 0, 
					73, 2, 0, 73, 38, 0, 69, 73, 67, 0, 73, 2, 0, 73, 38, 0, 69, 73, 68, 0, 73, 2, 0, 73, 38, 0, 69, 77, 3, 1, 98, 76, 73, 35, 0, 69, 73, 0, 0, 73, 2, 0, 73, 39, 0, 69, 73, 6, 0, 73, 
					2, 0, 73, 39, 0, 69, 73, 7, 0, 73, 2, 0, 73, 39, 0, 69, 73, 8, 0, 73, 2, 0, 73, 39, 0, 69, 73, 9, 0, 73, 2, 0, 73, 39, 0, 69, 73, 10, 0, 73, 2, 0, 73, 39, 0, 69, 73, 11, 0, 73, 
					2, 0, 73, 39, 0, 69, 73, 12, 0, 73, 2, 0, 73, 39, 0, 69, 73, 13, 0, 73, 2, 0, 73, 39, 0, 69, 73, 14, 0, 73, 2, 0, 73, 39, 0, 69, 73, 15, 0, 73, 2, 0, 73, 39, 0, 69, 73, 16, 0, 73, 
					2, 0, 73, 39, 0, 69, 73, 17, 0, 73, 2, 0, 73, 39, 0, 69, 73, 18, 0, 73, 2, 0, 73, 39, 0, 69, 73, 19, 0, 73, 2, 0, 73, 39, 0, 69, 73, 20, 0, 73, 2, 0, 73, 39, 0, 69, 73, 21, 0, 73, 
					2, 0, 73, 39, 0, 69, 73, 22, 0, 73, 2, 0, 73, 39, 0, 69, 73, 23, 0, 73, 2, 0, 73, 39, 0, 69, 73, 24, 0, 73, 2, 0, 73, 39, 0, 69, 73, 25, 0, 73, 2, 0, 73, 39, 0, 69, 73, 26, 0, 73, 
					2, 0, 73, 39, 0, 69, 73, 27, 0, 73, 2, 0, 73, 39, 0, 69, 73, 28, 0, 73, 2, 0, 73, 39, 0, 69, 73, 29, 0, 73, 2, 0, 73, 39, 0, 69, 73, 30, 0, 73, 2, 0, 73, 39, 0, 69, 73, 31, 0, 73, 
					2, 0, 73, 39, 0, 69, 73, 32, 0, 73, 2, 0, 73, 39, 0, 69, 73, 33, 0, 73, 2, 0, 73, 39, 0, 69, 73, 34, 0, 73, 2, 0, 73, 39, 0, 69, 73, 35, 0, 73, 2, 0, 73, 39, 0, 69, 73, 36, 0, 73, 
					2, 0, 73, 39, 0, 69, 73, 37, 0, 73, 2, 0, 73, 39, 0, 69, 73, 38, 0, 73, 2, 0, 73, 39, 0, 69, 73, 39, 0, 73, 2, 0, 73, 39, 0, 69, 73, 40, 0, 73, 2, 0, 73, 39, 0, 69, 73, 41, 0, 73, 
					2, 0, 73, 39, 0, 69, 73, 42, 0, 73, 2, 0, 73, 39, 0, 69, 73, 43, 0, 73, 2, 0, 73, 39, 0, 69, 73, 44, 0, 73, 2, 0, 73, 39, 0, 69, 73, 45, 0, 73, 2, 0, 73, 39, 0, 69, 73, 46, 0, 73, 
					2, 0, 73, 39, 0, 69, 73, 47, 0, 73, 2, 0, 73, 39, 0, 69, 73, 48, 0, 73, 2, 0, 73, 39, 0, 69, 73, 49, 0, 73, 2, 0, 73, 39, 0, 69, 73, 50, 0, 73, 2, 0, 73, 39, 0, 69, 73, 51, 0, 73, 
					2, 0, 73, 39, 0, 69, 73, 52, 0, 73, 2, 0, 73, 39, 0, 69, 73, 53, 0, 73, 2, 0, 73, 39, 0, 69, 73, 54, 0, 73, 2, 0, 73, 39, 0, 69, 73, 55, 0, 73, 2, 0, 73, 39, 0, 69, 73, 56, 0, 73, 
					2, 0, 73, 39, 0, 69, 73, 57, 0, 73, 2, 0, 73, 39, 0, 69, 73, 58, 0, 73, 2, 0, 73, 39, 0, 69, 73, 59, 0, 73, 2, 0, 73, 39, 0, 69, 73, 60, 0, 73, 2, 0, 73, 39, 0, 69, 73, 61, 0, 73, 
					2, 0, 73, 39, 0, 69, 73, 62, 0, 73, 2, 0, 73, 39, 0, 69, 73, 63, 0, 73, 2, 0, 73, 39, 0, 69, 73, 64, 0, 73, 2, 0, 73, 39, 0, 69, 73, 65, 0, 73, 2, 0, 73, 39, 0, 69, 73, 66, 0, 73, 
					2, 0, 73, 39, 0, 69, 73, 67, 0, 73, 2, 0, 73, 39, 0, 69, 73, 68, 0, 73, 2, 0, 73, 39, 0, 69, 77, 3, 1, 98, 76, 73, 36, 0, 69, 73, 0, 0, 73, 2, 0, 73, 14, 0, 69, 73, 6, 0, 73, 2, 
					0, 73, 14, 0, 69, 73, 7, 0, 73, 2, 0, 73, 14, 0, 69, 73, 8, 0, 73, 2, 0, 73, 14, 0, 69, 73, 9, 0, 73, 2, 0, 73, 14, 0, 69, 73, 10, 0, 73, 2, 0, 73, 14, 0, 69, 73, 11, 0, 73, 2, 
					0, 73, 14, 0, 69, 73, 12, 0, 73, 2, 0, 73, 14, 0, 69, 73, 13, 0, 73, 2, 0, 73, 14, 0, 69, 73, 14, 0, 73, 2, 0, 73, 14, 0, 69, 73, 15, 0, 73, 2, 0, 73, 14, 0, 69, 73, 16, 0, 73, 2, 
					0, 73, 14, 0, 69, 73, 17, 0, 73, 2, 0, 73, 14, 0, 69, 73, 18, 0, 73, 2, 0, 73, 14, 0, 69, 73, 19, 0, 73, 2, 0, 73, 14, 0, 69, 73, 20, 0, 73, 2, 0, 73, 14, 0, 69, 73, 21, 0, 73, 2, 
					0, 73, 14, 0, 69, 73, 22, 0, 73, 2, 0, 73, 14, 0, 69, 73, 23, 0, 73, 2, 0, 73, 14, 0, 69, 73, 24, 0, 73, 2, 0, 73, 14, 0, 69, 73, 25, 0, 73, 2, 0, 73, 14, 0, 69, 73, 26, 0, 73, 2, 
					0, 73, 14, 0, 69, 73, 27, 0, 73, 2, 0, 73, 14, 0, 69, 73, 28, 0, 73, 2, 0, 73, 14, 0, 69, 73, 29, 0, 73, 2, 0, 73, 14, 0, 69, 73, 30, 0, 73, 2, 0, 73, 14, 0, 69, 73, 31, 0, 73, 2, 
					0, 73, 14, 0, 69, 73, 32, 0, 73, 2, 0, 73, 14, 0, 69, 73, 33, 0, 73, 2, 0, 73, 14, 0, 69, 73, 34, 0, 73, 2, 0, 73, 14, 0, 69, 73, 35, 0, 73, 2, 0, 73, 14, 0, 69, 73, 36, 0, 73, 2, 
					0, 73, 14, 0, 69, 73, 37, 0, 73, 2, 0, 73, 14, 0, 69, 73, 38, 0, 73, 2, 0, 73, 14, 0, 69, 73, 39, 0, 73, 2, 0, 73, 14, 0, 69, 73, 40, 0, 73, 2, 0, 73, 14, 0, 69, 73, 41, 0, 73, 2, 
					0, 73, 14, 0, 69, 73, 42, 0, 73, 2, 0, 73, 14, 0, 69, 73, 43, 0, 73, 2, 0, 73, 14, 0, 69, 73, 44, 0, 73, 2, 0, 73, 14, 0, 69, 73, 45, 0, 73, 2, 0, 73, 14, 0, 69, 73, 46, 0, 73, 2, 
					0, 73, 14, 0, 69, 73, 47, 0, 73, 2, 0, 73, 14, 0, 69, 73, 48, 0, 73, 2, 0, 73, 14, 0, 69, 73, 49, 0, 73, 2, 0, 73, 14, 0, 69, 73, 50, 0, 73, 2, 0, 73, 14, 0, 69, 73, 51, 0, 73, 2, 
					0, 73, 14, 0, 69, 73, 52, 0, 73, 2, 0, 73, 14, 0, 69, 73, 53, 0, 73, 2, 0, 73, 14, 0, 69, 73, 54, 0, 73, 2, 0, 73, 14, 0, 69, 73, 55, 0, 73, 2, 0, 73, 14, 0, 69, 73, 56, 0, 73, 2, 
					0, 73, 14, 0, 69, 73, 57, 0, 73, 2, 0, 73, 14, 0, 69, 73, 58, 0, 73, 2, 0, 73, 14, 0, 69, 73, 59, 0, 73, 2, 0, 73, 14, 0, 69, 73, 60, 0, 73, 2, 0, 73, 14, 0, 69, 73, 61, 0, 73, 2, 
					0, 73, 14, 0, 69, 73, 62, 0, 73, 2, 0, 73, 14, 0, 69, 73, 63, 0, 73, 2, 0, 73, 14, 0, 69, 73, 64, 0, 73, 2, 0, 73, 14, 0, 69, 73, 65, 0, 73, 2, 0, 73, 14, 0, 69, 73, 66, 0, 73, 2, 
					0, 73, 14, 0, 69, 73, 67, 0, 73, 2, 0, 73, 14, 0, 69, 73, 68, 0, 73, 2, 0, 73, 14, 0, 69, 77, 3, 1, 98, 76, 73, 37, 0, 69, 73, 0, 0, 73, 2, 0, 73, 29, 0, 69, 73, 6, 0, 73, 2, 0, 
					73, 29, 0, 69, 73, 7, 0, 73, 2, 0, 73, 29, 0, 69, 73, 8, 0, 73, 2, 0, 73, 29, 0, 69, 73, 9, 0, 73, 2, 0, 73, 29, 0, 69, 73, 10, 0, 73, 2, 0, 73, 29, 0, 69, 73, 11, 0, 73, 2, 0, 
					73, 29, 0, 69, 73, 12, 0, 73, 2, 0, 73, 29, 0, 69, 73, 13, 0, 73, 2, 0, 73, 29, 0, 69, 73, 14, 0, 73, 2, 0, 73, 29, 0, 69, 73, 15, 0, 73, 2, 0, 73, 29, 0, 69, 73, 16, 0, 73, 2, 0, 
					73, 29, 0, 69, 73, 17, 0, 73, 2, 0, 73, 29, 0, 69, 73, 18, 0, 73, 2, 0, 73, 29, 0, 69, 73, 19, 0, 73, 2, 0, 73, 29, 0, 69, 73, 20, 0, 73, 2, 0, 73, 29, 0, 69, 73, 21, 0, 73, 2, 0, 
					73, 29, 0, 69, 73, 22, 0, 73, 2, 0, 73, 29, 0, 69, 73, 23, 0, 73, 2, 0, 73, 29, 0, 69, 73, 24, 0, 73, 2, 0, 73, 29, 0, 69, 73, 25, 0, 73, 2, 0, 73, 29, 0, 69, 73, 26, 0, 73, 2, 0, 
					73, 29, 0, 69, 73, 27, 0, 73, 2, 0, 73, 29, 0, 69, 73, 28, 0, 73, 2, 0, 73, 29, 0, 69, 73, 29, 0, 73, 2, 0, 73, 29, 0, 69, 73, 30, 0, 73, 2, 0, 73, 29, 0, 69, 73, 31, 0, 73, 2, 0, 
					73, 29, 0, 69, 73, 32, 0, 73, 2, 0, 73, 29, 0, 69, 73, 33, 0, 73, 2, 0, 73, 29, 0, 69, 73, 34, 0, 73, 2, 0, 73, 29, 0, 69, 73, 35, 0, 73, 2, 0, 73, 29, 0, 69, 73, 36, 0, 73, 2, 0, 
					73, 29, 0, 69, 73, 37, 0, 73, 2, 0, 73, 29, 0, 69, 73, 38, 0, 73, 2, 0, 73, 29, 0, 69, 73, 39, 0, 73, 2, 0, 73, 29, 0, 69, 73, 40, 0, 73, 2, 0, 73, 29, 0, 69, 73, 41, 0, 73, 2, 0, 
					73, 29, 0, 69, 73, 42, 0, 73, 2, 0, 73, 29, 0, 69, 73, 43, 0, 73, 2, 0, 73, 29, 0, 69, 73, 44, 0, 73, 2, 0, 73, 29, 0, 69, 73, 45, 0, 73, 2, 0, 73, 29, 0, 69, 73, 46, 0, 73, 2, 0, 
					73, 29, 0, 69, 73, 47, 0, 73, 2, 0, 73, 29, 0, 69, 73, 48, 0, 73, 2, 0, 73, 29, 0, 69, 73, 49, 0, 73, 2, 0, 73, 29, 0, 69, 73, 50, 0, 73, 2, 0, 73, 29, 0, 69, 73, 51, 0, 73, 2, 0, 
					73, 29, 0, 69, 73, 52, 0, 73, 2, 0, 73, 29, 0, 69, 73, 53, 0, 73, 2, 0, 73, 29, 0, 69, 73, 54, 0, 73, 2, 0, 73, 29, 0, 69, 73, 55, 0, 73, 2, 0, 73, 29, 0, 69, 73, 56, 0, 73, 2, 0, 
					73, 29, 0, 69, 73, 57, 0, 73, 2, 0, 73, 29, 0, 69, 73, 58, 0, 73, 2, 0, 73, 29, 0, 69, 73, 59, 0, 73, 2, 0, 73, 29, 0, 69, 73, 60, 0, 73, 2, 0, 73, 29, 0, 69, 73, 61, 0, 73, 2, 0, 
					73, 29, 0, 69, 73, 62, 0, 73, 2, 0, 73, 29, 0, 69, 73, 63, 0, 73, 2, 0, 73, 29, 0, 69, 73, 64, 0, 73, 2, 0, 73, 29, 0, 69, 73, 65, 0, 73, 2, 0, 73, 29, 0, 69, 73, 66, 0, 73, 2, 0, 
					73, 29, 0, 69, 73, 67, 0, 73, 2, 0, 73, 29, 0, 69, 73, 68, 0, 73, 2, 0, 73, 29, 0, 69, 77, 3, 1, 98, 76, 73, 38, 0, 69, 73, 0, 0, 73, 2, 0, 73, 30, 0, 69, 73, 6, 0, 73, 2, 0, 73, 
					30, 0, 69, 73, 7, 0, 73, 2, 0, 73, 30, 0, 69, 73, 8, 0, 73, 2, 0, 73, 30, 0, 69, 73, 9, 0, 73, 2, 0, 73, 30, 0, 69, 73, 10, 0, 73, 2, 0, 73, 30, 0, 69, 73, 11, 0, 73, 2, 0, 73, 
					30, 0, 69, 73, 12, 0, 73, 2, 0, 73, 30, 0, 69, 73, 13, 0, 73, 2, 0, 73, 30, 0, 69, 73, 14, 0, 73, 2, 0, 73, 30, 0, 69, 73, 15, 0, 73, 2, 0, 73, 30, 0, 69, 73, 16, 0, 73, 2, 0, 73, 
					30, 0, 69, 73, 17, 0, 73, 2, 0, 73, 30, 0, 69, 73, 18, 0, 73, 2, 0, 73, 30, 0, 69, 73, 19, 0, 73, 2, 0, 73, 30, 0, 69, 73, 20, 0, 73, 2, 0, 73, 30, 0, 69, 73, 21, 0, 73, 2, 0, 73, 
					30, 0, 69, 73, 22, 0, 73, 2, 0, 73, 30, 0, 69, 73, 23, 0, 73, 2, 0, 73, 30, 0, 69, 73, 24, 0, 73, 2, 0, 73, 30, 0, 69, 73, 25, 0, 73, 2, 0, 73, 30, 0, 69, 73, 26, 0, 73, 2, 0, 73, 
					30, 0, 69, 73, 27, 0, 73, 2, 0, 73, 30, 0, 69, 73, 28, 0, 73, 2, 0, 73, 30, 0, 69, 73, 29, 0, 73, 2, 0, 73, 30, 0, 69, 73, 30, 0, 73, 2, 0, 73, 30, 0, 69, 73, 31, 0, 73, 2, 0, 73, 
					30, 0, 69, 73, 32, 0, 73, 2, 0, 73, 30, 0, 69, 73, 33, 0, 73, 2, 0, 73, 30, 0, 69, 73, 34, 0, 73, 2, 0, 73, 30, 0, 69, 73, 35, 0, 73, 2, 0, 73, 30, 0, 69, 73, 36, 0, 73, 2, 0, 73, 
					30, 0, 69, 73, 37, 0, 73, 2, 0, 73, 30, 0, 69, 73, 38, 0, 73, 2, 0, 73, 30, 0, 69, 73, 39, 0, 73, 2, 0, 73, 30, 0, 69, 73, 40, 0, 73, 2, 0, 73, 30, 0, 69, 73, 41, 0, 73, 2, 0, 73, 
					30, 0, 69, 73, 42, 0, 73, 2, 0, 73, 30, 0, 69, 73, 43, 0, 73, 2, 0, 73, 30, 0, 69, 73, 44, 0, 73, 2, 0, 73, 30, 0, 69, 73, 45, 0, 73, 2, 0, 73, 30, 0, 69, 73, 46, 0, 73, 2, 0, 73, 
					30, 0, 69, 73, 47, 0, 73, 2, 0, 73, 30, 0, 69, 73, 48, 0, 73, 2, 0, 73, 30, 0, 69, 73, 49, 0, 73, 2, 0, 73, 30, 0, 69, 73, 50, 0, 73, 2, 0, 73, 30, 0, 69, 73, 51, 0, 73, 2, 0, 73, 
					30, 0, 69, 73, 52, 0, 73, 2, 0, 73, 30, 0, 69, 73, 53, 0, 73, 2, 0, 73, 30, 0, 69, 73, 54, 0, 73, 2, 0, 73, 30, 0, 69, 73, 55, 0, 73, 2, 0, 73, 30, 0, 69, 73, 56, 0, 73, 2, 0, 73, 
					30, 0, 69, 73, 57, 0, 73, 2, 0, 73, 30, 0, 69, 73, 58, 0, 73, 2, 0, 73, 30, 0, 69, 73, 59, 0, 73, 2, 0, 73, 30, 0, 69, 73, 60, 0, 73, 2, 0, 73, 30, 0, 69, 73, 61, 0, 73, 2, 0, 73, 
					30, 0, 69, 73, 62, 0, 73, 2, 0, 73, 30, 0, 69, 73, 63, 0, 73, 2, 0, 73, 30, 0, 69, 73, 64, 0, 73, 2, 0, 73, 30, 0, 69, 73, 65, 0, 73, 2, 0, 73, 30, 0, 69, 73, 66, 0, 73, 2, 0, 73, 
					30, 0, 69, 73, 67, 0, 73, 2, 0, 73, 30, 0, 69, 73, 68, 0, 73, 2, 0, 73, 30, 0, 69, 77, 3, 1, 98, 76, 73, 39, 0, 69, 73, 0, 0, 73, 2, 0, 73, 34, 0, 69, 73, 6, 0, 73, 2, 0, 73, 34, 
					0, 69, 73, 7, 0, 73, 2, 0, 73, 34, 0, 69, 73, 8, 0, 73, 2, 0, 73, 34, 0, 69, 73, 9, 0, 73, 2, 0, 73, 34, 0, 69, 73, 10, 0, 73, 2, 0, 73, 34, 0, 69, 73, 11, 0, 73, 2, 0, 73, 34, 
					0, 69, 73, 12, 0, 73, 2, 0, 73, 34, 0, 69, 73, 13, 0, 73, 2, 0, 73, 34, 0, 69, 73, 14, 0, 73, 2, 0, 73, 34, 0, 69, 73, 15, 0, 73, 2, 0, 73, 34, 0, 69, 73, 16, 0, 73, 2, 0, 73, 34, 
					0, 69, 73, 17, 0, 73, 2, 0, 73, 34, 0, 69, 73, 18, 0, 73, 2, 0, 73, 34, 0, 69, 73, 19, 0, 73, 2, 0, 73, 34, 0, 69, 73, 20, 0, 73, 2, 0, 73, 34, 0, 69, 73, 21, 0, 73, 2, 0, 73, 34, 
					0, 69, 73, 22, 0, 73, 2, 0, 73, 34, 0, 69, 73, 23, 0, 73, 2, 0, 73, 34, 0, 69, 73, 24, 0, 73, 2, 0, 73, 34, 0, 69, 73, 25, 0, 73, 2, 0, 73, 34, 0, 69, 73, 26, 0, 73, 2, 0, 73, 34, 
					0, 69, 73, 27, 0, 73, 2, 0, 73, 34, 0, 69, 73, 28, 0, 73, 2, 0, 73, 34, 0, 69, 73, 29, 0, 73, 2, 0, 73, 34, 0, 69, 73, 30, 0, 73, 2, 0, 73, 34, 0, 69, 73, 31, 0, 73, 2, 0, 73, 34, 
					0, 69, 73, 32, 0, 73, 2, 0, 73, 34, 0, 69, 73, 33, 0, 73, 2, 0, 73, 34, 0, 69, 73, 34, 0, 73, 2, 0, 73, 34, 0, 69, 73, 35, 0, 73, 2, 0, 73, 34, 0, 69, 73, 36, 0, 73, 2, 0, 73, 34, 
					0, 69, 73, 37, 0, 73, 2, 0, 73, 34, 0, 69, 73, 38, 0, 73, 2, 0, 73, 34, 0, 69, 73, 39, 0, 73, 2, 0, 73, 34, 0, 69, 73, 40, 0, 73, 2, 0, 73, 34, 0, 69, 73, 41, 0, 73, 2, 0, 73, 34, 
					0, 69, 73, 42, 0, 73, 2, 0, 73, 34, 0, 69, 73, 43, 0, 73, 2, 0, 73, 34, 0, 69, 73, 44, 0, 73, 2, 0, 73, 34, 0, 69, 73, 45, 0, 73, 2, 0, 73, 34, 0, 69, 73, 46, 0, 73, 2, 0, 73, 34, 
					0, 69, 73, 47, 0, 73, 2, 0, 73, 34, 0, 69, 73, 48, 0, 73, 2, 0, 73, 34, 0, 69, 73, 49, 0, 73, 2, 0, 73, 34, 0, 69, 73, 50, 0, 73, 2, 0, 73, 34, 0, 69, 73, 51, 0, 73, 2, 0, 73, 34, 
					0, 69, 73, 52, 0, 73, 2, 0, 73, 34, 0, 69, 73, 53, 0, 73, 2, 0, 73, 34, 0, 69, 73, 54, 0, 73, 2, 0, 73, 34, 0, 69, 73, 55, 0, 73, 2, 0, 73, 34, 0, 69, 73, 56, 0, 73, 2, 0, 73, 34, 
					0, 69, 73, 57, 0, 73, 2, 0, 73, 34, 0, 69, 73, 58, 0, 73, 2, 0, 73, 34, 0, 69, 73, 59, 0, 73, 2, 0, 73, 34, 0, 69, 73, 60, 0, 73, 2, 0, 73, 34, 0, 69, 73, 61, 0, 73, 2, 0, 73, 34, 
					0, 69, 73, 62, 0, 73, 2, 0, 73, 34, 0, 69, 73, 63, 0, 73, 2, 0, 73, 34, 0, 69, 73, 64, 0, 73, 2, 0, 73, 34, 0, 69, 73, 65, 0, 73, 2, 0, 73, 34, 0, 69, 73, 66, 0, 73, 2, 0, 73, 34, 
					0, 69, 73, 67, 0, 73, 2, 0, 73, 34, 0, 69, 73, 68, 0, 73, 2, 0, 73, 34, 0, 69, 77, 3, 1, 98, 76, 73, 40, 0, 69, 73, 0, 0, 73, 2, 0, 73, 35, 0, 69, 73, 6, 0, 73, 2, 0, 73, 35, 0, 
					69, 73, 7, 0, 73, 2, 0, 73, 35, 0, 69, 73, 8, 0, 73, 2, 0, 73, 35, 0, 69, 73, 9, 0, 73, 2, 0, 73, 35, 0, 69, 73, 10, 0, 73, 2, 0, 73, 35, 0, 69, 73, 11, 0, 73, 2, 0, 73, 35, 0, 
					69, 73, 12, 0, 73, 2, 0, 73, 35, 0, 69, 73, 13, 0, 73, 2, 0, 73, 35, 0, 69, 73, 14, 0, 73, 2, 0, 73, 35, 0, 69, 73, 15, 0, 73, 2, 0, 73, 35, 0, 69, 73, 16, 0, 73, 2, 0, 73, 35, 0, 
					69, 73, 17, 0, 73, 2, 0, 73, 35, 0, 69, 73, 18, 0, 73, 2, 0, 73, 35, 0, 69, 73, 19, 0, 73, 2, 0, 73, 35, 0, 69, 73, 20, 0, 73, 2, 0, 73, 35, 0, 69, 73, 21, 0, 73, 2, 0, 73, 35, 0, 
					69, 73, 22, 0, 73, 2, 0, 73, 35, 0, 69, 73, 23, 0, 73, 2, 0, 73, 35, 0, 69, 73, 24, 0, 73, 2, 0, 73, 35, 0, 69, 73, 25, 0, 73, 2, 0, 73, 35, 0, 69, 73, 26, 0, 73, 2, 0, 73, 35, 0, 
					69, 73, 27, 0, 73, 2, 0, 73, 35, 0, 69, 73, 28, 0, 73, 2, 0, 73, 35, 0, 69, 73, 29, 0, 73, 2, 0, 73, 35, 0, 69, 73, 30, 0, 73, 2, 0, 73, 35, 0, 69, 73, 31, 0, 73, 2, 0, 73, 35, 0, 
					69, 73, 32, 0, 73, 2, 0, 73, 35, 0, 69, 73, 33, 0, 73, 2, 0, 73, 35, 0, 69, 73, 34, 0, 73, 2, 0, 73, 35, 0, 69, 73, 35, 0, 73, 2, 0, 73, 35, 0, 69, 73, 36, 0, 73, 2, 0, 73, 35, 0, 
					69, 73, 37, 0, 73, 2, 0, 73, 35, 0, 69, 73, 38, 0, 73, 2, 0, 73, 35, 0, 69, 73, 39, 0, 73, 2, 0, 73, 35, 0, 69, 73, 40, 0, 73, 2, 0, 73, 35, 0, 69, 73, 41, 0, 73, 2, 0, 73, 35, 0, 
					69, 73, 42, 0, 73, 2, 0, 73, 35, 0, 69, 73, 43, 0, 73, 2, 0, 73, 35, 0, 69, 73, 44, 0, 73, 2, 0, 73, 35, 0, 69, 73, 45, 0, 73, 2, 0, 73, 35, 0, 69, 73, 46, 0, 73, 2, 0, 73, 35, 0, 
					69, 73, 47, 0, 73, 2, 0, 73, 35, 0, 69, 73, 48, 0, 73, 2, 0, 73, 35, 0, 69, 73, 49, 0, 73, 2, 0, 73, 35, 0, 69, 73, 50, 0, 73, 2, 0, 73, 35, 0, 69, 73, 51, 0, 73, 2, 0, 73, 35, 0, 
					69, 73, 52, 0, 73, 2, 0, 73, 35, 0, 69, 73, 53, 0, 73, 2, 0, 73, 35, 0, 69, 73, 54, 0, 73, 2, 0, 73, 35, 0, 69, 73, 55, 0, 73, 2, 0, 73, 35, 0, 69, 73, 56, 0, 73, 2, 0, 73, 35, 0, 
					69, 73, 57, 0, 73, 2, 0, 73, 35, 0, 69, 73, 58, 0, 73, 2, 0, 73, 35, 0, 69, 73, 59, 0, 73, 2, 0, 73, 35, 0, 69, 73, 60, 0, 73, 2, 0, 73, 35, 0, 69, 73, 61, 0, 73, 2, 0, 73, 35, 0, 
					69, 73, 62, 0, 73, 2, 0, 73, 35, 0, 69, 73, 63, 0, 73, 2, 0, 73, 35, 0, 69, 73, 64, 0, 73, 2, 0, 73, 35, 0, 69, 73, 65, 0, 73, 2, 0, 73, 35, 0, 69, 73, 66, 0, 73, 2, 0, 73, 35, 0, 
					69, 73, 67, 0, 73, 2, 0, 73, 35, 0, 69, 73, 68, 0, 73, 2, 0, 73, 35, 0, 69, 77, 3, 1, 98, 76, 73, 41, 0, 69, 73, 0, 0, 73, 2, 0, 73, 23, 0, 69, 73, 6, 0, 73, 2, 0, 73, 23, 0, 69, 
					73, 7, 0, 73, 2, 0, 73, 23, 0, 69, 73, 8, 0, 73, 2, 0, 73, 23, 0, 69, 73, 9, 0, 73, 2, 0, 73, 23, 0, 69, 73, 10, 0, 73, 2, 0, 73, 23, 0, 69, 73, 11, 0, 73, 2, 0, 73, 23, 0, 69, 
					73, 12, 0, 73, 2, 0, 73, 23, 0, 69, 73, 13, 0, 73, 2, 0, 73, 23, 0, 69, 73, 14, 0, 73, 2, 0, 73, 23, 0, 69, 73, 15, 0, 73, 2, 0, 73, 23, 0, 69, 73, 16, 0, 73, 2, 0, 73, 23, 0, 69, 
					73, 17, 0, 73, 2, 0, 73, 23, 0, 69, 73, 18, 0, 73, 2, 0, 73, 23, 0, 69, 73, 19, 0, 73, 2, 0, 73, 23, 0, 69, 73, 20, 0, 73, 2, 0, 73, 23, 0, 69, 73, 21, 0, 73, 2, 0, 73, 23, 0, 69, 
					73, 22, 0, 73, 2, 0, 73, 23, 0, 69, 73, 23, 0, 73, 2, 0, 73, 23, 0, 69, 73, 24, 0, 73, 2, 0, 73, 23, 0, 69, 73, 25, 0, 73, 2, 0, 73, 23, 0, 69, 73, 26, 0, 73, 2, 0, 73, 23, 0, 69, 
					73, 27, 0, 73, 2, 0, 73, 23, 0, 69, 73, 28, 0, 73, 2, 0, 73, 23, 0, 69, 73, 29, 0, 73, 2, 0, 73, 23, 0, 69, 73, 30, 0, 73, 2, 0, 73, 23, 0, 69, 73, 31, 0, 73, 2, 0, 73, 23, 0, 69, 
					73, 32, 0, 73, 2, 0, 73, 23, 0, 69, 73, 33, 0, 73, 2, 0, 73, 23, 0, 69, 73, 34, 0, 73, 2, 0, 73, 23, 0, 69, 73, 35, 0, 73, 2, 0, 73, 23, 0, 69, 73, 36, 0, 73, 2, 0, 73, 23, 0, 69, 
					73, 37, 0, 73, 2, 0, 73, 23, 0, 69, 73, 38, 0, 73, 2, 0, 73, 23, 0, 69, 73, 39, 0, 73, 2, 0, 73, 23, 0, 69, 73, 40, 0, 73, 2, 0, 73, 23, 0, 69, 73, 41, 0, 73, 2, 0, 73, 23, 0, 69, 
					73, 42, 0, 73, 2, 0, 73, 23, 0, 69, 73, 43, 0, 73, 2, 0, 73, 23, 0, 69, 73, 44, 0, 73, 2, 0, 73, 23, 0, 69, 73, 45, 0, 73, 2, 0, 73, 23, 0, 69, 73, 46, 0, 73, 2, 0, 73, 23, 0, 69, 
					73, 47, 0, 73, 2, 0, 73, 23, 0, 69, 73, 48, 0, 73, 2, 0, 73, 23, 0, 69, 73, 49, 0, 73, 2, 0, 73, 23, 0, 69, 73, 50, 0, 73, 2, 0, 73, 23, 0, 69, 73, 51, 0, 73, 2, 0, 73, 23, 0, 69, 
					73, 52, 0, 73, 2, 0, 73, 23, 0, 69, 73, 53, 0, 73, 2, 0, 73, 23, 0, 69, 73, 54, 0, 73, 2, 0, 73, 23, 0, 69, 73, 55, 0, 73, 2, 0, 73, 23, 0, 69, 73, 56, 0, 73, 2, 0, 73, 23, 0, 69, 
					73, 57, 0, 73, 2, 0, 73, 23, 0, 69, 73, 58, 0, 73, 2, 0, 73, 23, 0, 69, 73, 59, 0, 73, 2, 0, 73, 23, 0, 69, 73, 60, 0, 73, 2, 0, 73, 23, 0, 69, 73, 61, 0, 73, 2, 0, 73, 23, 0, 69, 
					73, 62, 0, 73, 2, 0, 73, 23, 0, 69, 73, 63, 0, 73, 2, 0, 73, 23, 0, 69, 73, 64, 0, 73, 2, 0, 73, 23, 0, 69, 73, 65, 0, 73, 2, 0, 73, 23, 0, 69, 73, 66, 0, 73, 2, 0, 73, 23, 0, 69, 
					73, 67, 0, 73, 2, 0, 73, 23, 0, 69, 73, 68, 0, 73, 2, 0, 73, 23, 0, 69, 77, 3, 1, 98, 76, 73, 42, 0, 69, 73, 0, 0, 73, 2, 0, 73, 24, 0, 69, 73, 6, 0, 73, 2, 0, 73, 24, 0, 69, 73, 
					7, 0, 73, 2, 0, 73, 24, 0, 69, 73, 8, 0, 73, 2, 0, 73, 24, 0, 69, 73, 9, 0, 73, 2, 0, 73, 24, 0, 69, 73, 10, 0, 73, 2, 0, 73, 24, 0, 69, 73, 11, 0, 73, 2, 0, 73, 24, 0, 69, 73, 
					12, 0, 73, 2, 0, 73, 24, 0, 69, 73, 13, 0, 73, 2, 0, 73, 24, 0, 69, 73, 14, 0, 73, 2, 0, 73, 24, 0, 69, 73, 15, 0, 73, 2, 0, 73, 24, 0, 69, 73, 16, 0, 73, 2, 0, 73, 24, 0, 69, 73, 
					17, 0, 73, 2, 0, 73, 24, 0, 69, 73, 18, 0, 73, 2, 0, 73, 24, 0, 69, 73, 19, 0, 73, 2, 0, 73, 24, 0, 69, 73, 20, 0, 73, 2, 0, 73, 24, 0, 69, 73, 21, 0, 73, 2, 0, 73, 24, 0, 69, 73, 
					22, 0, 73, 2, 0, 73, 24, 0, 69, 73, 23, 0, 73, 2, 0, 73, 24, 0, 69, 73, 24, 0, 73, 2, 0, 73, 24, 0, 69, 73, 25, 0, 73, 2, 0, 73, 24, 0, 69, 73, 26, 0, 73, 2, 0, 73, 24, 0, 69, 73, 
					27, 0, 73, 2, 0, 73, 24, 0, 69, 73, 28, 0, 73, 2, 0, 73, 24, 0, 69, 73, 29, 0, 73, 2, 0, 73, 24, 0, 69, 73, 30, 0, 73, 2, 0, 73, 24, 0, 69, 73, 31, 0, 73, 2, 0, 73, 24, 0, 69, 73, 
					32, 0, 73, 2, 0, 73, 24, 0, 69, 73, 33, 0, 73, 2, 0, 73, 24, 0, 69, 73, 34, 0, 73, 2, 0, 73, 24, 0, 69, 73, 35, 0, 73, 2, 0, 73, 24, 0, 69, 73, 36, 0, 73, 2, 0, 73, 24, 0, 69, 73, 
					37, 0, 73, 2, 0, 73, 24, 0, 69, 73, 38, 0, 73, 2, 0, 73, 24, 0, 69, 73, 39, 0, 73, 2, 0, 73, 24, 0, 69, 73, 40, 0, 73, 2, 0, 73, 24, 0, 69, 73, 41, 0, 73, 2, 0, 73, 24, 0, 69, 73, 
					42, 0, 73, 2, 0, 73, 24, 0, 69, 73, 43, 0, 73, 2, 0, 73, 24, 0, 69, 73, 44, 0, 73, 2, 0, 73, 24, 0, 69, 73, 45, 0, 73, 2, 0, 73, 24, 0, 69, 73, 46, 0, 73, 2, 0, 73, 24, 0, 69, 73, 
					47, 0, 73, 2, 0, 73, 24, 0, 69, 73, 48, 0, 73, 2, 0, 73, 24, 0, 69, 73, 49, 0, 73, 2, 0, 73, 24, 0, 69, 73, 50, 0, 73, 2, 0, 73, 24, 0, 69, 73, 51, 0, 73, 2, 0, 73, 24, 0, 69, 73, 
					52, 0, 73, 2, 0, 73, 24, 0, 69, 73, 53, 0, 73, 2, 0, 73, 24, 0, 69, 73, 54, 0, 73, 2, 0, 73, 24, 0, 69, 73, 55, 0, 73, 2, 0, 73, 24, 0, 69, 73, 56, 0, 73, 2, 0, 73, 24, 0, 69, 73, 
					57, 0, 73, 2, 0, 73, 24, 0, 69, 73, 58, 0, 73, 2, 0, 73, 24, 0, 69, 73, 59, 0, 73, 2, 0, 73, 24, 0, 69, 73, 60, 0, 73, 2, 0, 73, 24, 0, 69, 73, 61, 0, 73, 2, 0, 73, 24, 0, 69, 73, 
					62, 0, 73, 2, 0, 73, 24, 0, 69, 73, 63, 0, 73, 2, 0, 73, 24, 0, 69, 73, 64, 0, 73, 2, 0, 73, 24, 0, 69, 73, 65, 0, 73, 2, 0, 73, 24, 0, 69, 73, 66, 0, 73, 2, 0, 73, 24, 0, 69, 73, 
					67, 0, 73, 2, 0, 73, 24, 0, 69, 73, 68, 0, 73, 2, 0, 73, 24, 0, 69, 77, 3, 1, 98, 76, 73, 43, 0, 69, 73, 0, 0, 73, 2, 0, 73, 1, 0, 69, 73, 6, 0, 73, 2, 0, 73, 1, 0, 69, 73, 7, 
					0, 73, 2, 0, 73, 1, 0, 69, 73, 8, 0, 73, 2, 0, 73, 1, 0, 69, 73, 9, 0, 73, 2, 0, 73, 1, 0, 69, 73, 10, 0, 73, 2, 0, 73, 1, 0, 69, 73, 11, 0, 73, 2, 0, 73, 1, 0, 69, 73, 12, 
					0, 73, 2, 0, 73, 1, 0, 69, 73, 13, 0, 73, 2, 0, 73, 1, 0, 69, 73, 14, 0, 73, 2, 0, 73, 1, 0, 69, 73, 15, 0, 73, 2, 0, 73, 1, 0, 69, 73, 16, 0, 73, 2, 0, 73, 1, 0, 69, 73, 17, 
					0, 73, 2, 0, 73, 1, 0, 69, 73, 18, 0, 73, 2, 0, 73, 1, 0, 69, 73, 19, 0, 73, 2, 0, 73, 1, 0, 69, 73, 20, 0, 73, 2, 0, 73, 1, 0, 69, 73, 21, 0, 73, 2, 0, 73, 1, 0, 69, 73, 22, 
					0, 73, 2, 0, 73, 1, 0, 69, 73, 23, 0, 73, 2, 0, 73, 1, 0, 69, 73, 24, 0, 73, 2, 0, 73, 1, 0, 69, 73, 25, 0, 73, 2, 0, 73, 1, 0, 69, 73, 26, 0, 73, 2, 0, 73, 1, 0, 69, 73, 27, 
					0, 73, 2, 0, 73, 1, 0, 69, 73, 28, 0, 73, 2, 0, 73, 1, 0, 69, 73, 29, 0, 73, 2, 0, 73, 1, 0, 69, 73, 30, 0, 73, 2, 0, 73, 1, 0, 69, 73, 31, 0, 73, 2, 0, 73, 1, 0, 69, 73, 32, 
					0, 73, 2, 0, 73, 1, 0, 69, 73, 33, 0, 73, 2, 0, 73, 1, 0, 69, 73, 34, 0, 73, 2, 0, 73, 1, 0, 69, 73, 35, 0, 73, 2, 0, 73, 1, 0, 69, 73, 36, 0, 73, 2, 0, 73, 1, 0, 69, 73, 37, 
					0, 73, 2, 0, 73, 1, 0, 69, 73, 38, 0, 73, 2, 0, 73, 1, 0, 69, 73, 39, 0, 73, 2, 0, 73, 1, 0, 69, 73, 40, 0, 73, 2, 0, 73, 1, 0, 69, 73, 41, 0, 73, 2, 0, 73, 1, 0, 69, 73, 42, 
					0, 73, 2, 0, 73, 1, 0, 69, 73, 43, 0, 73, 2, 0, 73, 1, 0, 69, 73, 44, 0, 73, 2, 0, 73, 1, 0, 69, 73, 45, 0, 73, 2, 0, 73, 1, 0, 69, 73, 46, 0, 73, 2, 0, 73, 1, 0, 69, 73, 47, 
					0, 73, 2, 0, 73, 1, 0, 69, 73, 48, 0, 73, 2, 0, 73, 1, 0, 69, 73, 49, 0, 73, 2, 0, 73, 1, 0, 69, 73, 50, 0, 73, 2, 0, 73, 1, 0, 69, 73, 51, 0, 73, 2, 0, 73, 1, 0, 69, 73, 52, 
					0, 73, 2, 0, 73, 1, 0, 69, 73, 53, 0, 73, 2, 0, 73, 1, 0, 69, 73, 54, 0, 73, 2, 0, 73, 1, 0, 69, 73, 55, 0, 73, 2, 0, 73, 1, 0, 69, 73, 56, 0, 73, 2, 0, 73, 1, 0, 69, 73, 57, 
					0, 73, 2, 0, 73, 1, 0, 69, 73, 58, 0, 73, 2, 0, 73, 1, 0, 69, 73, 59, 0, 73, 2, 0, 73, 1, 0, 69, 73, 60, 0, 73, 2, 0, 73, 1, 0, 69, 73, 61, 0, 73, 2, 0, 73, 1, 0, 69, 73, 62, 
					0, 73, 2, 0, 73, 1, 0, 69, 73, 63, 0, 73, 2, 0, 73, 1, 0, 69, 73, 64, 0, 73, 2, 0, 73, 1, 0, 69, 73, 65, 0, 73, 2, 0, 73, 1, 0, 69, 73, 66, 0, 73, 2, 0, 73, 1, 0, 69, 73, 67, 
					0, 73, 2, 0, 73, 1, 0, 69, 73, 68, 0, 73, 2, 0, 73, 1, 0, 69, 77, 3, 1, 98, 76, 73, 44, 0, 69, 73, 0, 0, 73, 2, 0, 73, 18, 0, 69, 73, 6, 0, 73, 2, 0, 73, 18, 0, 69, 73, 7, 0, 
					73, 2, 0, 73, 18, 0, 69, 73, 8, 0, 73, 2, 0, 73, 18, 0, 69, 73, 9, 0, 73, 2, 0, 73, 18, 0, 69, 73, 10, 0, 73, 2, 0, 73, 18, 0, 69, 73, 11, 0, 73, 2, 0, 73, 18, 0, 69, 73, 12, 0, 
					73, 2, 0, 73, 18, 0, 69, 73, 13, 0, 73, 2, 0, 73, 18, 0, 69, 73, 14, 0, 73, 2, 0, 73, 18, 0, 69, 73, 15, 0, 73, 2, 0, 73, 18, 0, 69, 73, 16, 0, 73, 2, 0, 73, 18, 0, 69, 73, 17, 0, 
					73, 2, 0, 73, 18, 0, 69, 73, 18, 0, 73, 2, 0, 73, 18, 0, 69, 73, 19, 0, 73, 2, 0, 73, 18, 0, 69, 73, 20, 0, 73, 2, 0, 73, 18, 0, 69, 73, 21, 0, 73, 2, 0, 73, 18, 0, 69, 73, 22, 0, 
					73, 2, 0, 73, 18, 0, 69, 73, 23, 0, 73, 2, 0, 73, 18, 0, 69, 73, 24, 0, 73, 2, 0, 73, 18, 0, 69, 73, 25, 0, 73, 2, 0, 73, 18, 0, 69, 73, 26, 0, 73, 2, 0, 73, 18, 0, 69, 73, 27, 0, 
					73, 2, 0, 73, 18, 0, 69, 73, 28, 0, 73, 2, 0, 73, 18, 0, 69, 73, 29, 0, 73, 2, 0, 73, 18, 0, 69, 73, 30, 0, 73, 2, 0, 73, 18, 0, 69, 73, 31, 0, 73, 2, 0, 73, 18, 0, 69, 73, 32, 0, 
					73, 2, 0, 73, 18, 0, 69, 73, 33, 0, 73, 2, 0, 73, 18, 0, 69, 73, 34, 0, 73, 2, 0, 73, 18, 0, 69, 73, 35, 0, 73, 2, 0, 73, 18, 0, 69, 73, 36, 0, 73, 2, 0, 73, 18, 0, 69, 73, 37, 0, 
					73, 2, 0, 73, 18, 0, 69, 73, 38, 0, 73, 2, 0, 73, 18, 0, 69, 73, 39, 0, 73, 2, 0, 73, 18, 0, 69, 73, 40, 0, 73, 2, 0, 73, 18, 0, 69, 73, 41, 0, 73, 2, 0, 73, 18, 0, 69, 73, 42, 0, 
					73, 2, 0, 73, 18, 0, 69, 73, 43, 0, 73, 2, 0, 73, 18, 0, 69, 73, 44, 0, 73, 2, 0, 73, 18, 0, 69, 73, 45, 0, 73, 2, 0, 73, 18, 0, 69, 73, 46, 0, 73, 2, 0, 73, 18, 0, 69, 73, 47, 0, 
					73, 2, 0, 73, 18, 0, 69, 73, 48, 0, 73, 2, 0, 73, 18, 0, 69, 73, 49, 0, 73, 2, 0, 73, 18, 0, 69, 73, 50, 0, 73, 2, 0, 73, 18, 0, 69, 73, 51, 0, 73, 2, 0, 73, 18, 0, 69, 73, 52, 0, 
					73, 2, 0, 73, 18, 0, 69, 73, 53, 0, 73, 2, 0, 73, 18, 0, 69, 73, 54, 0, 73, 2, 0, 73, 18, 0, 69, 73, 55, 0, 73, 2, 0, 73, 18, 0, 69, 73, 56, 0, 73, 2, 0, 73, 18, 0, 69, 73, 57, 0, 
					73, 2, 0, 73, 18, 0, 69, 73, 58, 0, 73, 2, 0, 73, 18, 0, 69, 73, 59, 0, 73, 2, 0, 73, 18, 0, 69, 73, 60, 0, 73, 2, 0, 73, 18, 0, 69, 73, 61, 0, 73, 2, 0, 73, 18, 0, 69, 73, 62, 0, 
					73, 2, 0, 73, 18, 0, 69, 73, 63, 0, 73, 2, 0, 73, 18, 0, 69, 73, 64, 0, 73, 2, 0, 73, 18, 0, 69, 73, 65, 0, 73, 2, 0, 73, 18, 0, 69, 73, 66, 0, 73, 2, 0, 73, 18, 0, 69, 73, 67, 0, 
					73, 2, 0, 73, 18, 0, 69, 73, 68, 0, 73, 2, 0, 73, 18, 0, 69, 77, 3, 1, 98, 76, 73, 45, 0, 69, 73, 0, 0, 73, 2, 0, 73, 19, 0, 69, 73, 6, 0, 73, 2, 0, 73, 19, 0, 69, 73, 7, 0, 73, 
					2, 0, 73, 19, 0, 69, 73, 8, 0, 73, 2, 0, 73, 19, 0, 69, 73, 9, 0, 73, 2, 0, 73, 19, 0, 69, 73, 10, 0, 73, 2, 0, 73, 19, 0, 69, 73, 11, 0, 73, 2, 0, 73, 19, 0, 69, 73, 12, 0, 73, 
					2, 0, 73, 19, 0, 69, 73, 13, 0, 73, 2, 0, 73, 19, 0, 69, 73, 14, 0, 73, 2, 0, 73, 19, 0, 69, 73, 15, 0, 73, 2, 0, 73, 19, 0, 69, 73, 16, 0, 73, 2, 0, 73, 19, 0, 69, 73, 17, 0, 73, 
					2, 0, 73, 19, 0, 69, 73, 18, 0, 73, 2, 0, 73, 19, 0, 69, 73, 19, 0, 73, 2, 0, 73, 19, 0, 69, 73, 20, 0, 73, 2, 0, 73, 19, 0, 69, 73, 21, 0, 73, 2, 0, 73, 19, 0, 69, 73, 22, 0, 73, 
					2, 0, 73, 19, 0, 69, 73, 23, 0, 73, 2, 0, 73, 19, 0, 69, 73, 24, 0, 73, 2, 0, 73, 19, 0, 69, 73, 25, 0, 73, 2, 0, 73, 19, 0, 69, 73, 26, 0, 73, 2, 0, 73, 19, 0, 69, 73, 27, 0, 73, 
					2, 0, 73, 19, 0, 69, 73, 28, 0, 73, 2, 0, 73, 19, 0, 69, 73, 29, 0, 73, 2, 0, 73, 19, 0, 69, 73, 30, 0, 73, 2, 0, 73, 19, 0, 69, 73, 31, 0, 73, 2, 0, 73, 19, 0, 69, 73, 32, 0, 73, 
					2, 0, 73, 19, 0, 69, 73, 33, 0, 73, 2, 0, 73, 19, 0, 69, 73, 34, 0, 73, 2, 0, 73, 19, 0, 69, 73, 35, 0, 73, 2, 0, 73, 19, 0, 69, 73, 36, 0, 73, 2, 0, 73, 19, 0, 69, 73, 37, 0, 73, 
					2, 0, 73, 19, 0, 69, 73, 38, 0, 73, 2, 0, 73, 19, 0, 69, 73, 39, 0, 73, 2, 0, 73, 19, 0, 69, 73, 40, 0, 73, 2, 0, 73, 19, 0, 69, 73, 41, 0, 73, 2, 0, 73, 19, 0, 69, 73, 42, 0, 73, 
					2, 0, 73, 19, 0, 69, 73, 43, 0, 73, 2, 0, 73, 19, 0, 69, 73, 44, 0, 73, 2, 0, 73, 19, 0, 69, 73, 45, 0, 73, 2, 0, 73, 19, 0, 69, 73, 46, 0, 73, 2, 0, 73, 19, 0, 69, 73, 47, 0, 73, 
					2, 0, 73, 19, 0, 69, 73, 48, 0, 73, 2, 0, 73, 19, 0, 69, 73, 49, 0, 73, 2, 0, 73, 19, 0, 69, 73, 50, 0, 73, 2, 0, 73, 19, 0, 69, 73, 51, 0, 73, 2, 0, 73, 19, 0, 69, 73, 52, 0, 73, 
					2, 0, 73, 19, 0, 69, 73, 53, 0, 73, 2, 0, 73, 19, 0, 69, 73, 54, 0, 73, 2, 0, 73, 19, 0, 69, 73, 55, 0, 73, 2, 0, 73, 19, 0, 69, 73, 56, 0, 73, 2, 0, 73, 19, 0, 69, 73, 57, 0, 73, 
					2, 0, 73, 19, 0, 69, 73, 58, 0, 73, 2, 0, 73, 19, 0, 69, 73, 59, 0, 73, 2, 0, 73, 19, 0, 69, 73, 60, 0, 73, 2, 0, 73, 19, 0, 69, 73, 61, 0, 73, 2, 0, 73, 19, 0, 69, 73, 62, 0, 73, 
					2, 0, 73, 19, 0, 69, 73, 63, 0, 73, 2, 0, 73, 19, 0, 69, 73, 64, 0, 73, 2, 0, 73, 19, 0, 69, 73, 65, 0, 73, 2, 0, 73, 19, 0, 69, 73, 66, 0, 73, 2, 0, 73, 19, 0, 69, 73, 67, 0, 73, 
					2, 0, 73, 19, 0, 69, 73, 68, 0, 73, 2, 0, 73, 19, 0, 69, 77, 3, 1, 98, 76, 73, 46, 0, 69, 73, 0, 0, 73, 2, 0, 73, 4, 0, 69, 73, 6, 0, 73, 2, 0, 73, 4, 0, 69, 73, 7, 0, 73, 2, 
					0, 73, 4, 0, 69, 73, 8, 0, 73, 2, 0, 73, 4, 0, 69, 73, 9, 0, 73, 2, 0, 73, 4, 0, 69, 73, 10, 0, 73, 2, 0, 73, 4, 0, 69, 73, 11, 0, 73, 2, 0, 73, 4, 0, 69, 73, 12, 0, 73, 2, 
					0, 73, 4, 0, 69, 73, 13, 0, 73, 2, 0, 73, 4, 0, 69, 73, 14, 0, 73, 2, 0, 73, 4, 0, 69, 73, 15, 0, 73, 2, 0, 73, 4, 0, 69, 73, 16, 0, 73, 2, 0, 73, 4, 0, 69, 73, 17, 0, 73, 2, 
					0, 73, 4, 0, 69, 73, 18, 0, 73, 2, 0, 73, 4, 0, 69, 73, 19, 0, 73, 2, 0, 73, 4, 0, 69, 73, 20, 0, 73, 2, 0, 73, 4, 0, 69, 73, 21, 0, 73, 2, 0, 73, 4, 0, 69, 73, 22, 0, 73, 2, 
					0, 73, 4, 0, 69, 73, 23, 0, 73, 2, 0, 73, 4, 0, 69, 73, 24, 0, 73, 2, 0, 73, 4, 0, 69, 73, 25, 0, 73, 2, 0, 73, 4, 0, 69, 73, 26, 0, 73, 2, 0, 73, 4, 0, 69, 73, 27, 0, 73, 2, 
					0, 73, 4, 0, 69, 73, 28, 0, 73, 2, 0, 73, 4, 0, 69, 73, 29, 0, 73, 2, 0, 73, 4, 0, 69, 73, 30, 0, 73, 2, 0, 73, 4, 0, 69, 73, 31, 0, 73, 2, 0, 73, 4, 0, 69, 73, 32, 0, 73, 2, 
					0, 73, 4, 0, 69, 73, 33, 0, 73, 2, 0, 73, 4, 0, 69, 73, 34, 0, 73, 2, 0, 73, 4, 0, 69, 73, 35, 0, 73, 2, 0, 73, 4, 0, 69, 73, 36, 0, 73, 2, 0, 73, 4, 0, 69, 73, 37, 0, 73, 2, 
					0, 73, 4, 0, 69, 73, 38, 0, 73, 2, 0, 73, 4, 0, 69, 73, 39, 0, 73, 2, 0, 73, 4, 0, 69, 73, 40, 0, 73, 2, 0, 73, 4, 0, 69, 73, 41, 0, 73, 2, 0, 73, 4, 0, 69, 73, 42, 0, 73, 2, 
					0, 73, 4, 0, 69, 73, 43, 0, 73, 2, 0, 73, 4, 0, 69, 73, 44, 0, 73, 2, 0, 73, 4, 0, 69, 73, 45, 0, 73, 2, 0, 73, 4, 0, 69, 73, 46, 0, 73, 2, 0, 73, 4, 0, 69, 73, 47, 0, 73, 2, 
					0, 73, 4, 0, 69, 73, 48, 0, 73, 2, 0, 73, 4, 0, 69, 73, 49, 0, 73, 2, 0, 73, 4, 0, 69, 73, 50, 0, 73, 2, 0, 73, 4, 0, 69, 73, 51, 0, 73, 2, 0, 73, 4, 0, 69, 73, 52, 0, 73, 2, 
					0, 73, 4, 0, 69, 73, 53, 0, 73, 2, 0, 73, 4, 0, 69, 73, 54, 0, 73, 2, 0, 73, 4, 0, 69, 73, 55, 0, 73, 2, 0, 73, 4, 0, 69, 73, 56, 0, 73, 2, 0, 73, 4, 0, 69, 73, 57, 0, 73, 2, 
					0, 73, 4, 0, 69, 73, 58, 0, 73, 2, 0, 73, 4, 0, 69, 73, 59, 0, 73, 2, 0, 73, 4, 0, 69, 73, 60, 0, 73, 2, 0, 73, 4, 0, 69, 73, 61, 0, 73, 2, 0, 73, 4, 0, 69, 73, 62, 0, 73, 2, 
					0, 73, 4, 0, 69, 73, 63, 0, 73, 2, 0, 73, 4, 0, 69, 73, 64, 0, 73, 2, 0, 73, 4, 0, 69, 73, 65, 0, 73, 2, 0, 73, 4, 0, 69, 73, 66, 0, 73, 2, 0, 73, 4, 0, 69, 73, 67, 0, 73, 2, 
					0, 73, 4, 0, 69, 73, 68, 0, 73, 2, 0, 73, 4, 0, 69, 77, 3, 1, 98, 76, 73, 47, 0, 69, 73, 0, 0, 73, 2, 0, 73, 11, 0, 69, 73, 6, 0, 73, 2, 0, 73, 11, 0, 69, 73, 7, 0, 73, 2, 0, 
					73, 11, 0, 69, 73, 8, 0, 73, 2, 0, 73, 11, 0, 69, 73, 9, 0, 73, 2, 0, 73, 11, 0, 69, 73, 10, 0, 73, 2, 0, 73, 11, 0, 69, 73, 11, 0, 73, 2, 0, 73, 11, 0, 69, 73, 12, 0, 73, 2, 0, 
					73, 11, 0, 69, 73, 13, 0, 73, 2, 0, 73, 11, 0, 69, 73, 14, 0, 73, 2, 0, 73, 11, 0, 69, 73, 15, 0, 73, 2, 0, 73, 11, 0, 69, 73, 16, 0, 73, 2, 0, 73, 11, 0, 69, 73, 17, 0, 73, 2, 0, 
					73, 11, 0, 69, 73, 18, 0, 73, 2, 0, 73, 11, 0, 69, 73, 19, 0, 73, 2, 0, 73, 11, 0, 69, 73, 20, 0, 73, 2, 0, 73, 11, 0, 69, 73, 21, 0, 73, 2, 0, 73, 11, 0, 69, 73, 22, 0, 73, 2, 0, 
					73, 11, 0, 69, 73, 23, 0, 73, 2, 0, 73, 11, 0, 69, 73, 24, 0, 73, 2, 0, 73, 11, 0, 69, 73, 25, 0, 73, 2, 0, 73, 11, 0, 69, 73, 26, 0, 73, 2, 0, 73, 11, 0, 69, 73, 27, 0, 73, 2, 0, 
					73, 11, 0, 69, 73, 28, 0, 73, 2, 0, 73, 11, 0, 69, 73, 29, 0, 73, 2, 0, 73, 11, 0, 69, 73, 30, 0, 73, 2, 0, 73, 11, 0, 69, 73, 31, 0, 73, 2, 0, 73, 11, 0, 69, 73, 32, 0, 73, 2, 0, 
					73, 11, 0, 69, 73, 33, 0, 73, 2, 0, 73, 11, 0, 69, 73, 34, 0, 73, 2, 0, 73, 11, 0, 69, 73, 35, 0, 73, 2, 0, 73, 11, 0, 69, 73, 36, 0, 73, 2, 0, 73, 11, 0, 69, 73, 37, 0, 73, 2, 0, 
					73, 11, 0, 69, 73, 38, 0, 73, 2, 0, 73, 11, 0, 69, 73, 39, 0, 73, 2, 0, 73, 11, 0, 69, 73, 40, 0, 73, 2, 0, 73, 11, 0, 69, 73, 41, 0, 73, 2, 0, 73, 11, 0, 69, 73, 42, 0, 73, 2, 0, 
					73, 11, 0, 69, 73, 43, 0, 73, 2, 0, 73, 11, 0, 69, 73, 44, 0, 73, 2, 0, 73, 11, 0, 69, 73, 45, 0, 73, 2, 0, 73, 11, 0, 69, 73, 46, 0, 73, 2, 0, 73, 11, 0, 69, 73, 47, 0, 73, 2, 0, 
					73, 11, 0, 69, 73, 48, 0, 73, 2, 0, 73, 11, 0, 69, 73, 49, 0, 73, 2, 0, 73, 11, 0, 69, 73, 50, 0, 73, 2, 0, 73, 11, 0, 69, 73, 51, 0, 73, 2, 0, 73, 11, 0, 69, 73, 52, 0, 73, 2, 0, 
					73, 11, 0, 69, 73, 53, 0, 73, 2, 0, 73, 11, 0, 69, 73, 54, 0, 73, 2, 0, 73, 11, 0, 69, 73, 55, 0, 73, 2, 0, 73, 11, 0, 69, 73, 56, 0, 73, 2, 0, 73, 11, 0, 69, 73, 57, 0, 73, 2, 0, 
					73, 11, 0, 69, 73, 58, 0, 73, 2, 0, 73, 11, 0, 69, 73, 59, 0, 73, 2, 0, 73, 11, 0, 69, 73, 60, 0, 73, 2, 0, 73, 11, 0, 69, 73, 61, 0, 73, 2, 0, 73, 11, 0, 69, 73, 62, 0, 73, 2, 0, 
					73, 11, 0, 69, 73, 63, 0, 73, 2, 0, 73, 11, 0, 69, 73, 64, 0, 73, 2, 0, 73, 11, 0, 69, 73, 65, 0, 73, 2, 0, 73, 11, 0, 69, 73, 66, 0, 73, 2, 0, 73, 11, 0, 69, 73, 67, 0, 73, 2, 0, 
					73, 11, 0, 69, 73, 68, 0, 73, 2, 0, 73, 11, 0, 69, 77, 3, 1, 98, 76, 73, 48, 0, 69, 73, 0, 0, 73, 2, 0, 73, 48, 0, 69, 73, 6, 0, 73, 2, 0, 73, 48, 0, 69, 73, 7, 0, 73, 2, 0, 73, 
					48, 0, 69, 73, 8, 0, 73, 2, 0, 73, 48, 0, 69, 73, 9, 0, 73, 2, 0, 73, 48, 0, 69, 73, 10, 0, 73, 2, 0, 73, 48, 0, 69, 73, 11, 0, 73, 2, 0, 73, 48, 0, 69, 73, 12, 0, 73, 2, 0, 73, 
					48, 0, 69, 73, 13, 0, 73, 2, 0, 73, 48, 0, 69, 73, 14, 0, 73, 2, 0, 73, 48, 0, 69, 73, 15, 0, 73, 2, 0, 73, 48, 0, 69, 73, 16, 0, 73, 2, 0, 73, 48, 0, 69, 73, 17, 0, 73, 2, 0, 73, 
					48, 0, 69, 73, 18, 0, 73, 2, 0, 73, 48, 0, 69, 73, 19, 0, 73, 2, 0, 73, 48, 0, 69, 73, 20, 0, 73, 2, 0, 73, 48, 0, 69, 73, 21, 0, 73, 2, 0, 73, 48, 0, 69, 73, 22, 0, 73, 2, 0, 73, 
					48, 0, 69, 73, 23, 0, 73, 2, 0, 73, 48, 0, 69, 73, 24, 0, 73, 2, 0, 73, 48, 0, 69, 73, 25, 0, 73, 2, 0, 73, 48, 0, 69, 73, 26, 0, 73, 2, 0, 73, 48, 0, 69, 73, 27, 0, 73, 2, 0, 73, 
					48, 0, 69, 73, 28, 0, 73, 2, 0, 73, 48, 0, 69, 73, 29, 0, 73, 2, 0, 73, 48, 0, 69, 73, 30, 0, 73, 2, 0, 73, 48, 0, 69, 73, 31, 0, 73, 2, 0, 73, 48, 0, 69, 73, 32, 0, 73, 2, 0, 73, 
					48, 0, 69, 73, 33, 0, 73, 2, 0, 73, 48, 0, 69, 73, 34, 0, 73, 2, 0, 73, 48, 0, 69, 73, 35, 0, 73, 2, 0, 73, 48, 0, 69, 73, 36, 0, 73, 2, 0, 73, 48, 0, 69, 73, 37, 0, 73, 2, 0, 73, 
					48, 0, 69, 73, 38, 0, 73, 2, 0, 73, 48, 0, 69, 73, 39, 0, 73, 2, 0, 73, 48, 0, 69, 73, 40, 0, 73, 2, 0, 73, 48, 0, 69, 73, 41, 0, 73, 2, 0, 73, 48, 0, 69, 73, 42, 0, 73, 2, 0, 73, 
					48, 0, 69, 73, 43, 0, 73, 2, 0, 73, 48, 0, 69, 73, 44, 0, 73, 2, 0, 73, 48, 0, 69, 73, 45, 0, 73, 2, 0, 73, 48, 0, 69, 73, 46, 0, 73, 2, 0, 73, 48, 0, 69, 73, 47, 0, 73, 2, 0, 73, 
					48, 0, 69, 73, 48, 0, 73, 2, 0, 73, 48, 0, 69, 73, 49, 0, 73, 2, 0, 73, 48, 0, 69, 73, 50, 0, 73, 2, 0, 73, 48, 0, 69, 73, 51, 0, 73, 2, 0, 73, 48, 0, 69, 73, 52, 0, 73, 2, 0, 73, 
					48, 0, 69, 73, 53, 0, 73, 2, 0, 73, 48, 0, 69, 73, 54, 0, 73, 2, 0, 73, 48, 0, 69, 73, 55, 0, 73, 2, 0, 73, 48, 0, 69, 73, 56, 0, 73, 2, 0, 73, 48, 0, 69, 73, 57, 0, 73, 2, 0, 73, 
					48, 0, 69, 73, 58, 0, 73, 2, 0, 73, 48, 0, 69, 73, 59, 0, 73, 2, 0, 73, 48, 0, 69, 73, 60, 0, 73, 2, 0, 73, 48, 0, 69, 73, 61, 0, 73, 2, 0, 73, 48, 0, 69, 73, 62, 0, 73, 2, 0, 73, 
					48, 0, 69, 73, 63, 0, 73, 2, 0, 73, 48, 0, 69, 73, 64, 0, 73, 2, 0, 73, 48, 0, 69, 73, 65, 0, 73, 2, 0, 73, 48, 0, 69, 73, 66, 0, 73, 2, 0, 73, 48, 0, 69, 73, 67, 0, 73, 2, 0, 73, 
					48, 0, 69, 73, 68, 0, 73, 2, 0, 73, 48, 0, 69, 77, 3, 1, 98, 76, 73, 49, 0, 69, 73, 0, 0, 73, 2, 0, 73, 25, 0, 69, 73, 6, 0, 73, 2, 0, 73, 25, 0, 69, 73, 7, 0, 73, 2, 0, 73, 25, 
					0, 69, 73, 8, 0, 73, 2, 0, 73, 25, 0, 69, 73, 9, 0, 73, 2, 0, 73, 25, 0, 69, 73, 10, 0, 73, 2, 0, 73, 25, 0, 69, 73, 11, 0, 73, 2, 0, 73, 25, 0, 69, 73, 12, 0, 73, 2, 0, 73, 25, 
					0, 69, 73, 13, 0, 73, 2, 0, 73, 25, 0, 69, 73, 14, 0, 73, 2, 0, 73, 25, 0, 69, 73, 15, 0, 73, 2, 0, 73, 25, 0, 69, 73, 16, 0, 73, 2, 0, 73, 25, 0, 69, 73, 17, 0, 73, 2, 0, 73, 25, 
					0, 69, 73, 18, 0, 73, 2, 0, 73, 25, 0, 69, 73, 19, 0, 73, 2, 0, 73, 25, 0, 69, 73, 20, 0, 73, 2, 0, 73, 25, 0, 69, 73, 21, 0, 73, 2, 0, 73, 25, 0, 69, 73, 22, 0, 73, 2, 0, 73, 25, 
					0, 69, 73, 23, 0, 73, 2, 0, 73, 25, 0, 69, 73, 24, 0, 73, 2, 0, 73, 25, 0, 69, 73, 25, 0, 73, 2, 0, 73, 25, 0, 69, 73, 26, 0, 73, 2, 0, 73, 25, 0, 69, 73, 27, 0, 73, 2, 0, 73, 25, 
					0, 69, 73, 28, 0, 73, 2, 0, 73, 25, 0, 69, 73, 29, 0, 73, 2, 0, 73, 25, 0, 69, 73, 30, 0, 73, 2, 0, 73, 25, 0, 69, 73, 31, 0, 73, 2, 0, 73, 25, 0, 69, 73, 32, 0, 73, 2, 0, 73, 25, 
					0, 69, 73, 33, 0, 73, 2, 0, 73, 25, 0, 69, 73, 34, 0, 73, 2, 0, 73, 25, 0, 69, 73, 35, 0, 73, 2, 0, 73, 25, 0, 69, 73, 36, 0, 73, 2, 0, 73, 25, 0, 69, 73, 37, 0, 73, 2, 0, 73, 25, 
					0, 69, 73, 38, 0, 73, 2, 0, 73, 25, 0, 69, 73, 39, 0, 73, 2, 0, 73, 25, 0, 69, 73, 40, 0, 73, 2, 0, 73, 25, 0, 69, 73, 41, 0, 73, 2, 0, 73, 25, 0, 69, 73, 42, 0, 73, 2, 0, 73, 25, 
					0, 69, 73, 43, 0, 73, 2, 0, 73, 25, 0, 69, 73, 44, 0, 73, 2, 0, 73, 25, 0, 69, 73, 45, 0, 73, 2, 0, 73, 25, 0, 69, 73, 46, 0, 73, 2, 0, 73, 25, 0, 69, 73, 47, 0, 73, 2, 0, 73, 25, 
					0, 69, 73, 48, 0, 73, 2, 0, 73, 25, 0, 69, 73, 49, 0, 73, 2, 0, 73, 25, 0, 69, 73, 50, 0, 73, 2, 0, 73, 25, 0, 69, 73, 51, 0, 73, 2, 0, 73, 25, 0, 69, 73, 52, 0, 73, 2, 0, 73, 25, 
					0, 69, 73, 53, 0, 73, 2, 0, 73, 25, 0, 69, 73, 54, 0, 73, 2, 0, 73, 25, 0, 69, 73, 55, 0, 73, 2, 0, 73, 25, 0, 69, 73, 56, 0, 73, 2, 0, 73, 25, 0, 69, 73, 57, 0, 73, 2, 0, 73, 25, 
					0, 69, 73, 58, 0, 73, 2, 0, 73, 25, 0, 69, 73, 59, 0, 73, 2, 0, 73, 25, 0, 69, 73, 60, 0, 73, 2, 0, 73, 25, 0, 69, 73, 61, 0, 73, 2, 0, 73, 25, 0, 69, 73, 62, 0, 73, 2, 0, 73, 25, 
					0, 69, 73, 63, 0, 73, 2, 0, 73, 25, 0, 69, 73, 64, 0, 73, 2, 0, 73, 25, 0, 69, 73, 65, 0, 73, 2, 0, 73, 25, 0, 69, 73, 66, 0, 73, 2, 0, 73, 25, 0, 69, 73, 67, 0, 73, 2, 0, 73, 25, 
					0, 69, 73, 68, 0, 73, 2, 0, 73, 25, 0, 69, 77, 3, 1, 98, 76, 73, 50, 0, 69, 73, 0, 0, 73, 2, 0, 73, 26, 0, 69, 73, 6, 0, 73, 2, 0, 73, 26, 0, 69, 73, 7, 0, 73, 2, 0, 73, 26, 0, 
					69, 73, 8, 0, 73, 2, 0, 73, 26, 0, 69, 73, 9, 0, 73, 2, 0, 73, 26, 0, 69, 73, 10, 0, 73, 2, 0, 73, 26, 0, 69, 73, 11, 0, 73, 2, 0, 73, 26, 0, 69, 73, 12, 0, 73, 2, 0, 73, 26, 0, 
					69, 73, 13, 0, 73, 2, 0, 73, 26, 0, 69, 73, 14, 0, 73, 2, 0, 73, 26, 0, 69, 73, 15, 0, 73, 2, 0, 73, 26, 0, 69, 73, 16, 0, 73, 2, 0, 73, 26, 0, 69, 73, 17, 0, 73, 2, 0, 73, 26, 0, 
					69, 73, 18, 0, 73, 2, 0, 73, 26, 0, 69, 73, 19, 0, 73, 2, 0, 73, 26, 0, 69, 73, 20, 0, 73, 2, 0, 73, 26, 0, 69, 73, 21, 0, 73, 2, 0, 73, 26, 0, 69, 73, 22, 0, 73, 2, 0, 73, 26, 0, 
					69, 73, 23, 0, 73, 2, 0, 73, 26, 0, 69, 73, 24, 0, 73, 2, 0, 73, 26, 0, 69, 73, 25, 0, 73, 2, 0, 73, 26, 0, 69, 73, 26, 0, 73, 2, 0, 73, 26, 0, 69, 73, 27, 0, 73, 2, 0, 73, 26, 0, 
					69, 73, 28, 0, 73, 2, 0, 73, 26, 0, 69, 73, 29, 0, 73, 2, 0, 73, 26, 0, 69, 73, 30, 0, 73, 2, 0, 73, 26, 0, 69, 73, 31, 0, 73, 2, 0, 73, 26, 0, 69, 73, 32, 0, 73, 2, 0, 73, 26, 0, 
					69, 73, 33, 0, 73, 2, 0, 73, 26, 0, 69, 73, 34, 0, 73, 2, 0, 73, 26, 0, 69, 73, 35, 0, 73, 2, 0, 73, 26, 0, 69, 73, 36, 0, 73, 2, 0, 73, 26, 0, 69, 73, 37, 0, 73, 2, 0, 73, 26, 0, 
					69, 73, 38, 0, 73, 2, 0, 73, 26, 0, 69, 73, 39, 0, 73, 2, 0, 73, 26, 0, 69, 73, 40, 0, 73, 2, 0, 73, 26, 0, 69, 73, 41, 0, 73, 2, 0, 73, 26, 0, 69, 73, 42, 0, 73, 2, 0, 73, 26, 0, 
					69, 73, 43, 0, 73, 2, 0, 73, 26, 0, 69, 73, 44, 0, 73, 2, 0, 73, 26, 0, 69, 73, 45, 0, 73, 2, 0, 73, 26, 0, 69, 73, 46, 0, 73, 2, 0, 73, 26, 0, 69, 73, 47, 0, 73, 2, 0, 73, 26, 0, 
					69, 73, 48, 0, 73, 2, 0, 73, 26, 0, 69, 73, 49, 0, 73, 2, 0, 73, 26, 0, 69, 73, 50, 0, 73, 2, 0, 73, 26, 0, 69, 73, 51, 0, 73, 2, 0, 73, 26, 0, 69, 73, 52, 0, 73, 2, 0, 73, 26, 0, 
					69, 73, 53, 0, 73, 2, 0, 73, 26, 0, 69, 73, 54, 0, 73, 2, 0, 73, 26, 0, 69, 73, 55, 0, 73, 2, 0, 73, 26, 0, 69, 73, 56, 0, 73, 2, 0, 73, 26, 0, 69, 73, 57, 0, 73, 2, 0, 73, 26, 0, 
					69, 73, 58, 0, 73, 2, 0, 73, 26, 0, 69, 73, 59, 0, 73, 2, 0, 73, 26, 0, 69, 73, 60, 0, 73, 2, 0, 73, 26, 0, 69, 73, 61, 0, 73, 2, 0, 73, 26, 0, 69, 73, 62, 0, 73, 2, 0, 73, 26, 0, 
					69, 73, 63, 0, 73, 2, 0, 73, 26, 0, 69, 73, 64, 0, 73, 2, 0, 73, 26, 0, 69, 73, 65, 0, 73, 2, 0, 73, 26, 0, 69, 73, 66, 0, 73, 2, 0, 73, 26, 0, 69, 73, 67, 0, 73, 2, 0, 73, 26, 0, 
					69, 73, 68, 0, 73, 2, 0, 73, 26, 0, 69, 77, 3, 1, 98, 76, 73, 51, 0, 69, 73, 0, 0, 73, 2, 0, 73, 15, 0, 69, 73, 6, 0, 73, 2, 0, 73, 15, 0, 69, 73, 7, 0, 73, 2, 0, 73, 15, 0, 69, 
					73, 8, 0, 73, 2, 0, 73, 15, 0, 69, 73, 9, 0, 73, 2, 0, 73, 15, 0, 69, 73, 10, 0, 73, 2, 0, 73, 15, 0, 69, 73, 11, 0, 73, 2, 0, 73, 15, 0, 69, 73, 12, 0, 73, 2, 0, 73, 15, 0, 69, 
					73, 13, 0, 73, 2, 0, 73, 15, 0, 69, 73, 14, 0, 73, 2, 0, 73, 15, 0, 69, 73, 15, 0, 73, 2, 0, 73, 15, 0, 69, 73, 16, 0, 73, 2, 0, 73, 15, 0, 69, 73, 17, 0, 73, 2, 0, 73, 15, 0, 69, 
					73, 18, 0, 73, 2, 0, 73, 15, 0, 69, 73, 19, 0, 73, 2, 0, 73, 15, 0, 69, 73, 20, 0, 73, 2, 0, 73, 15, 0, 69, 73, 21, 0, 73, 2, 0, 73, 15, 0, 69, 73, 22, 0, 73, 2, 0, 73, 15, 0, 69, 
					73, 23, 0, 73, 2, 0, 73, 15, 0, 69, 73, 24, 0, 73, 2, 0, 73, 15, 0, 69, 73, 25, 0, 73, 2, 0, 73, 15, 0, 69, 73, 26, 0, 73, 2, 0, 73, 15, 0, 69, 73, 27, 0, 73, 2, 0, 73, 15, 0, 69, 
					73, 28, 0, 73, 2, 0, 73, 15, 0, 69, 73, 29, 0, 73, 2, 0, 73, 15, 0, 69, 73, 30, 0, 73, 2, 0, 73, 15, 0, 69, 73, 31, 0, 73, 2, 0, 73, 15, 0, 69, 73, 32, 0, 73, 2, 0, 73, 15, 0, 69, 
					73, 33, 0, 73, 2, 0, 73, 15, 0, 69, 73, 34, 0, 73, 2, 0, 73, 15, 0, 69, 73, 35, 0, 73, 2, 0, 73, 15, 0, 69, 73, 36, 0, 73, 2, 0, 73, 15, 0, 69, 73, 37, 0, 73, 2, 0, 73, 15, 0, 69, 
					73, 38, 0, 73, 2, 0, 73, 15, 0, 69, 73, 39, 0, 73, 2, 0, 73, 15, 0, 69, 73, 40, 0, 73, 2, 0, 73, 15, 0, 69, 73, 41, 0, 73, 2, 0, 73, 15, 0, 69, 73, 42, 0, 73, 2, 0, 73, 15, 0, 69, 
					73, 43, 0, 73, 2, 0, 73, 15, 0, 69, 73, 44, 0, 73, 2, 0, 73, 15, 0, 69, 73, 45, 0, 73, 2, 0, 73, 15, 0, 69, 73, 46, 0, 73, 2, 0, 73, 15, 0, 69, 73, 47, 0, 73, 2, 0, 73, 15, 0, 69, 
					73, 48, 0, 73, 2, 0, 73, 15, 0, 69, 73, 49, 0, 73, 2, 0, 73, 15, 0, 69, 73, 50, 0, 73, 2, 0, 73, 15, 0, 69, 73, 51, 0, 73, 2, 0, 73, 15, 0, 69, 73, 52, 0, 73, 2, 0, 73, 15, 0, 69, 
					73, 53, 0, 73, 2, 0, 73, 15, 0, 69, 73, 54, 0, 73, 2, 0, 73, 15, 0, 69, 73, 55, 0, 73, 2, 0, 73, 15, 0, 69, 73, 56, 0, 73, 2, 0, 73, 15, 0, 69, 73, 57, 0, 73, 2, 0, 73, 15, 0, 69, 
					73, 58, 0, 73, 2, 0, 73, 15, 0, 69, 73, 59, 0, 73, 2, 0, 73, 15, 0, 69, 73, 60, 0, 73, 2, 0, 73, 15, 0, 69, 73, 61, 0, 73, 2, 0, 73, 15, 0, 69, 73, 62, 0, 73, 2, 0, 73, 15, 0, 69, 
					73, 63, 0, 73, 2, 0, 73, 15, 0, 69, 73, 64, 0, 73, 2, 0, 73, 15, 0, 69, 73, 65, 0, 73, 2, 0, 73, 15, 0, 69, 73, 66, 0, 73, 2, 0, 73, 15, 0, 69, 73, 67, 0, 73, 2, 0, 73, 15, 0, 69, 
					73, 68, 0, 73, 2, 0, 73, 15, 0, 69, 77, 3, 1, 98, 76, 73, 52, 0, 69, 73, 0, 0, 73, 2, 0, 73, 44, 0, 69, 73, 6, 0, 73, 2, 0, 73, 44, 0, 69, 73, 7, 0, 73, 2, 0, 73, 44, 0, 69, 73, 
					8, 0, 73, 2, 0, 73, 44, 0, 69, 73, 9, 0, 73, 2, 0, 73, 44, 0, 69, 73, 10, 0, 73, 2, 0, 73, 44, 0, 69, 73, 11, 0, 73, 2, 0, 73, 44, 0, 69, 73, 12, 0, 73, 2, 0, 73, 44, 0, 69, 73, 
					13, 0, 73, 2, 0, 73, 44, 0, 69, 73, 14, 0, 73, 2, 0, 73, 44, 0, 69, 73, 15, 0, 73, 2, 0, 73, 44, 0, 69, 73, 16, 0, 73, 2, 0, 73, 44, 0, 69, 73, 17, 0, 73, 2, 0, 73, 44, 0, 69, 73, 
					18, 0, 73, 2, 0, 73, 44, 0, 69, 73, 19, 0, 73, 2, 0, 73, 44, 0, 69, 73, 20, 0, 73, 2, 0, 73, 44, 0, 69, 73, 21, 0, 73, 2, 0, 73, 44, 0, 69, 73, 22, 0, 73, 2, 0, 73, 44, 0, 69, 73, 
					23, 0, 73, 2, 0, 73, 44, 0, 69, 73, 24, 0, 73, 2, 0, 73, 44, 0, 69, 73, 25, 0, 73, 2, 0, 73, 44, 0, 69, 73, 26, 0, 73, 2, 0, 73, 44, 0, 69, 73, 27, 0, 73, 2, 0, 73, 44, 0, 69, 73, 
					28, 0, 73, 2, 0, 73, 44, 0, 69, 73, 29, 0, 73, 2, 0, 73, 44, 0, 69, 73, 30, 0, 73, 2, 0, 73, 44, 0, 69, 73, 31, 0, 73, 2, 0, 73, 44, 0, 69, 73, 32, 0, 73, 2, 0, 73, 44, 0, 69, 73, 
					33, 0, 73, 2, 0, 73, 44, 0, 69, 73, 34, 0, 73, 2, 0, 73, 44, 0, 69, 73, 35, 0, 73, 2, 0, 73, 44, 0, 69, 73, 36, 0, 73, 2, 0, 73, 44, 0, 69, 73, 37, 0, 73, 2, 0, 73, 44, 0, 69, 73, 
					38, 0, 73, 2, 0, 73, 44, 0, 69, 73, 39, 0, 73, 2, 0, 73, 44, 0, 69, 73, 40, 0, 73, 2, 0, 73, 44, 0, 69, 73, 41, 0, 73, 2, 0, 73, 44, 0, 69, 73, 42, 0, 73, 2, 0, 73, 44, 0, 69, 73, 
					43, 0, 73, 2, 0, 73, 44, 0, 69, 73, 44, 0, 73, 2, 0, 73, 44, 0, 69, 73, 45, 0, 73, 2, 0, 73, 44, 0, 69, 73, 46, 0, 73, 2, 0, 73, 44, 0, 69, 73, 47, 0, 73, 2, 0, 73, 44, 0, 69, 73, 
					48, 0, 73, 2, 0, 73, 44, 0, 69, 73, 49, 0, 73, 2, 0, 73, 44, 0, 69, 73, 50, 0, 73, 2, 0, 73, 44, 0, 69, 73, 51, 0, 73, 2, 0, 73, 44, 0, 69, 73, 52, 0, 73, 2, 0, 73, 44, 0, 69, 73, 
					53, 0, 73, 2, 0, 73, 44, 0, 69, 73, 54, 0, 73, 2, 0, 73, 44, 0, 69, 73, 55, 0, 73, 2, 0, 73, 44, 0, 69, 73, 56, 0, 73, 2, 0, 73, 44, 0, 69, 73, 57, 0, 73, 2, 0, 73, 44, 0, 69, 73, 
					58, 0, 73, 2, 0, 73, 44, 0, 69, 73, 59, 0, 73, 2, 0, 73, 44, 0, 69, 73, 60, 0, 73, 2, 0, 73, 44, 0, 69, 73, 61, 0, 73, 2, 0, 73, 44, 0, 69, 73, 62, 0, 73, 2, 0, 73, 44, 0, 69, 73, 
					63, 0, 73, 2, 0, 73, 44, 0, 69, 73, 64, 0, 73, 2, 0, 73, 44, 0, 69, 73, 65, 0, 73, 2, 0, 73, 44, 0, 69, 73, 66, 0, 73, 2, 0, 73, 44, 0, 69, 73, 67, 0, 73, 2, 0, 73, 44, 0, 69, 73, 
					68, 0, 73, 2, 0, 73, 44, 0, 69, 77, 3, 1, 98, 76, 73, 53, 0, 69, 73, 0, 0, 73, 2, 0, 73, 46, 0, 69, 73, 6, 0, 73, 2, 0, 73, 46, 0, 69, 73, 7, 0, 73, 2, 0, 73, 46, 0, 69, 73, 8, 
					0, 73, 2, 0, 73, 46, 0, 69, 73, 9, 0, 73, 2, 0, 73, 46, 0, 69, 73, 10, 0, 73, 2, 0, 73, 46, 0, 69, 73, 11, 0, 73, 2, 0, 73, 46, 0, 69, 73, 12, 0, 73, 2, 0, 73, 46, 0, 69, 73, 13, 
					0, 73, 2, 0, 73, 46, 0, 69, 73, 14, 0, 73, 2, 0, 73, 46, 0, 69, 73, 15, 0, 73, 2, 0, 73, 46, 0, 69, 73, 16, 0, 73, 2, 0, 73, 46, 0, 69, 73, 17, 0, 73, 2, 0, 73, 46, 0, 69, 73, 18, 
					0, 73, 2, 0, 73, 46, 0, 69, 73, 19, 0, 73, 2, 0, 73, 46, 0, 69, 73, 20, 0, 73, 2, 0, 73, 46, 0, 69, 73, 21, 0, 73, 2, 0, 73, 46, 0, 69, 73, 22, 0, 73, 2, 0, 73, 46, 0, 69, 73, 23, 
					0, 73, 2, 0, 73, 46, 0, 69, 73, 24, 0, 73, 2, 0, 73, 46, 0, 69, 73, 25, 0, 73, 2, 0, 73, 46, 0, 69, 73, 26, 0, 73, 2, 0, 73, 46, 0, 69, 73, 27, 0, 73, 2, 0, 73, 46, 0, 69, 73, 28, 
					0, 73, 2, 0, 73, 46, 0, 69, 73, 29, 0, 73, 2, 0, 73, 46, 0, 69, 73, 30, 0, 73, 2, 0, 73, 46, 0, 69, 73, 31, 0, 73, 2, 0, 73, 46, 0, 69, 73, 32, 0, 73, 2, 0, 73, 46, 0, 69, 73, 33, 
					0, 73, 2, 0, 73, 46, 0, 69, 73, 34, 0, 73, 2, 0, 73, 46, 0, 69, 73, 35, 0, 73, 2, 0, 73, 46, 0, 69, 73, 36, 0, 73, 2, 0, 73, 46, 0, 69, 73, 37, 0, 73, 2, 0, 73, 46, 0, 69, 73, 38, 
					0, 73, 2, 0, 73, 46, 0, 69, 73, 39, 0, 73, 2, 0, 73, 46, 0, 69, 73, 40, 0, 73, 2, 0, 73, 46, 0, 69, 73, 41, 0, 73, 2, 0, 73, 46, 0, 69, 73, 42, 0, 73, 2, 0, 73, 46, 0, 69, 73, 43, 
					0, 73, 2, 0, 73, 46, 0, 69, 73, 44, 0, 73, 2, 0, 73, 46, 0, 69, 73, 45, 0, 73, 2, 0, 73, 46, 0, 69, 73, 46, 0, 73, 2, 0, 73, 46, 0, 69, 73, 47, 0, 73, 2, 0, 73, 46, 0, 69, 73, 48, 
					0, 73, 2, 0, 73, 46, 0, 69, 73, 49, 0, 73, 2, 0, 73, 46, 0, 69, 73, 50, 0, 73, 2, 0, 73, 46, 0, 69, 73, 51, 0, 73, 2, 0, 73, 46, 0, 69, 73, 52, 0, 73, 2, 0, 73, 46, 0, 69, 73, 53, 
					0, 73, 2, 0, 73, 46, 0, 69, 73, 54, 0, 73, 2, 0, 73, 46, 0, 69, 73, 55, 0, 73, 2, 0, 73, 46, 0, 69, 73, 56, 0, 73, 2, 0, 73, 46, 0, 69, 73, 57, 0, 73, 2, 0, 73, 46, 0, 69, 73, 58, 
					0, 73, 2, 0, 73, 46, 0, 69, 73, 59, 0, 73, 2, 0, 73, 46, 0, 69, 73, 60, 0, 73, 2, 0, 73, 46, 0, 69, 73, 61, 0, 73, 2, 0, 73, 46, 0, 69, 73, 62, 0, 73, 2, 0, 73, 46, 0, 69, 73, 63, 
					0, 73, 2, 0, 73, 46, 0, 69, 73, 64, 0, 73, 2, 0, 73, 46, 0, 69, 73, 65, 0, 73, 2, 0, 73, 46, 0, 69, 73, 66, 0, 73, 2, 0, 73, 46, 0, 69, 73, 67, 0, 73, 2, 0, 73, 46, 0, 69, 73, 68, 
					0, 73, 2, 0, 73, 46, 0, 69, 77, 3, 1, 98, 76, 73, 54, 0, 69, 73, 0, 0, 73, 2, 0, 73, 49, 0, 69, 73, 6, 0, 73, 2, 0, 73, 49, 0, 69, 73, 7, 0, 73, 2, 0, 73, 49, 0, 69, 73, 8, 0, 
					73, 2, 0, 73, 49, 0, 69, 73, 9, 0, 73, 2, 0, 73, 49, 0, 69, 73, 10, 0, 73, 2, 0, 73, 49, 0, 69, 73, 11, 0, 73, 2, 0, 73, 49, 0, 69, 73, 12, 0, 73, 2, 0, 73, 49, 0, 69, 73, 13, 0, 
					73, 2, 0, 73, 49, 0, 69, 73, 14, 0, 73, 2, 0, 73, 49, 0, 69, 73, 15, 0, 73, 2, 0, 73, 49, 0, 69, 73, 16, 0, 73, 2, 0, 73, 49, 0, 69, 73, 17, 0, 73, 2, 0, 73, 49, 0, 69, 73, 18, 0, 
					73, 2, 0, 73, 49, 0, 69, 73, 19, 0, 73, 2, 0, 73, 49, 0, 69, 73, 20, 0, 73, 2, 0, 73, 49, 0, 69, 73, 21, 0, 73, 2, 0, 73, 49, 0, 69, 73, 22, 0, 73, 2, 0, 73, 49, 0, 69, 73, 23, 0, 
					73, 2, 0, 73, 49, 0, 69, 73, 24, 0, 73, 2, 0, 73, 49, 0, 69, 73, 25, 0, 73, 2, 0, 73, 49, 0, 69, 73, 26, 0, 73, 2, 0, 73, 49, 0, 69, 73, 27, 0, 73, 2, 0, 73, 49, 0, 69, 73, 28, 0, 
					73, 2, 0, 73, 49, 0, 69, 73, 29, 0, 73, 2, 0, 73, 49, 0, 69, 73, 30, 0, 73, 2, 0, 73, 49, 0, 69, 73, 31, 0, 73, 2, 0, 73, 49, 0, 69, 73, 32, 0, 73, 2, 0, 73, 49, 0, 69, 73, 33, 0, 
					73, 2, 0, 73, 49, 0, 69, 73, 34, 0, 73, 2, 0, 73, 49, 0, 69, 73, 35, 0, 73, 2, 0, 73, 49, 0, 69, 73, 36, 0, 73, 2, 0, 73, 49, 0, 69, 73, 37, 0, 73, 2, 0, 73, 49, 0, 69, 73, 38, 0, 
					73, 2, 0, 73, 49, 0, 69, 73, 39, 0, 73, 2, 0, 73, 49, 0, 69, 73, 40, 0, 73, 2, 0, 73, 49, 0, 69, 73, 41, 0, 73, 2, 0, 73, 49, 0, 69, 73, 42, 0, 73, 2, 0, 73, 49, 0, 69, 73, 43, 0, 
					73, 2, 0, 73, 49, 0, 69, 73, 44, 0, 73, 2, 0, 73, 49, 0, 69, 73, 45, 0, 73, 2, 0, 73, 49, 0, 69, 73, 46, 0, 73, 2, 0, 73, 49, 0, 69, 73, 47, 0, 73, 2, 0, 73, 49, 0, 69, 73, 48, 0, 
					73, 2, 0, 73, 49, 0, 69, 73, 49, 0, 73, 2, 0, 73, 49, 0, 69, 73, 50, 0, 73, 2, 0, 73, 49, 0, 69, 73, 51, 0, 73, 2, 0, 73, 49, 0, 69, 73, 52, 0, 73, 2, 0, 73, 49, 0, 69, 73, 53, 0, 
					73, 2, 0, 73, 49, 0, 69, 73, 54, 0, 73, 2, 0, 73, 49, 0, 69, 73, 55, 0, 73, 2, 0, 73, 49, 0, 69, 73, 56, 0, 73, 2, 0, 73, 49, 0, 69, 73, 57, 0, 73, 2, 0, 73, 49, 0, 69, 73, 58, 0, 
					73, 2, 0, 73, 49, 0, 69, 73, 59, 0, 73, 2, 0, 73, 49, 0, 69, 73, 60, 0, 73, 2, 0, 73, 49, 0, 69, 73, 61, 0, 73, 2, 0, 73, 49, 0, 69, 73, 62, 0, 73, 2, 0, 73, 49, 0, 69, 73, 63, 0, 
					73, 2, 0, 73, 49, 0, 69, 73, 64, 0, 73, 2, 0, 73, 49, 0, 69, 73, 65, 0, 73, 2, 0, 73, 49, 0, 69, 73, 66, 0, 73, 2, 0, 73, 49, 0, 69, 73, 67, 0, 73, 2, 0, 73, 49, 0, 69, 73, 68, 0, 
					73, 2, 0, 73, 49, 0, 69, 77, 3, 1, 98, 76, 73, 55, 0, 69, 73, 0, 0, 73, 2, 0, 73, 22, 0, 69, 73, 6, 0, 73, 2, 0, 73, 22, 0, 69, 73, 7, 0, 73, 2, 0, 73, 22, 0, 69, 73, 8, 0, 73, 
					2, 0, 73, 22, 0, 69, 73, 9, 0, 73, 2, 0, 73, 22, 0, 69, 73, 10, 0, 73, 2, 0, 73, 22, 0, 69, 73, 11, 0, 73, 2, 0, 73, 22, 0, 69, 73, 12, 0, 73, 2, 0, 73, 22, 0, 69, 73, 13, 0, 73, 
					2, 0, 73, 22, 0, 69, 73, 14, 0, 73, 2, 0, 73, 22, 0, 69, 73, 15, 0, 73, 2, 0, 73, 22, 0, 69, 73, 16, 0, 73, 2, 0, 73, 22, 0, 69, 73, 17, 0, 73, 2, 0, 73, 22, 0, 69, 73, 18, 0, 73, 
					2, 0, 73, 22, 0, 69, 73, 19, 0, 73, 2, 0, 73, 22, 0, 69, 73, 20, 0, 73, 2, 0, 73, 22, 0, 69, 73, 21, 0, 73, 2, 0, 73, 22, 0, 69, 73, 22, 0, 73, 2, 0, 73, 22, 0, 69, 73, 23, 0, 73, 
					2, 0, 73, 22, 0, 69, 73, 24, 0, 73, 2, 0, 73, 22, 0, 69, 73, 25, 0, 73, 2, 0, 73, 22, 0, 69, 73, 26, 0, 73, 2, 0, 73, 22, 0, 69, 73, 27, 0, 73, 2, 0, 73, 22, 0, 69, 73, 28, 0, 73, 
					2, 0, 73, 22, 0, 69, 73, 29, 0, 73, 2, 0, 73, 22, 0, 69, 73, 30, 0, 73, 2, 0, 73, 22, 0, 69, 73, 31, 0, 73, 2, 0, 73, 22, 0, 69, 73, 32, 0, 73, 2, 0, 73, 22, 0, 69, 73, 33, 0, 73, 
					2, 0, 73, 22, 0, 69, 73, 34, 0, 73, 2, 0, 73, 22, 0, 69, 73, 35, 0, 73, 2, 0, 73, 22, 0, 69, 73, 36, 0, 73, 2, 0, 73, 22, 0, 69, 73, 37, 0, 73, 2, 0, 73, 22, 0, 69, 73, 38, 0, 73, 
					2, 0, 73, 22, 0, 69, 73, 39, 0, 73, 2, 0, 73, 22, 0, 69, 73, 40, 0, 73, 2, 0, 73, 22, 0, 69, 73, 41, 0, 73, 2, 0, 73, 22, 0, 69, 73, 42, 0, 73, 2, 0, 73, 22, 0, 69, 73, 43, 0, 73, 
					2, 0, 73, 22, 0, 69, 73, 44, 0, 73, 2, 0, 73, 22, 0, 69, 73, 45, 0, 73, 2, 0, 73, 22, 0, 69, 73, 46, 0, 73, 2, 0, 73, 22, 0, 69, 73, 47, 0, 73, 2, 0, 73, 22, 0, 69, 73, 48, 0, 73, 
					2, 0, 73, 22, 0, 69, 73, 49, 0, 73, 2, 0, 73, 22, 0, 69, 73, 50, 0, 73, 2, 0, 73, 22, 0, 69, 73, 51, 0, 73, 2, 0, 73, 22, 0, 69, 73, 52, 0, 73, 2, 0, 73, 22, 0, 69, 73, 53, 0, 73, 
					2, 0, 73, 22, 0, 69, 73, 54, 0, 73, 2, 0, 73, 22, 0, 69, 73, 55, 0, 73, 2, 0, 73, 22, 0, 69, 73, 56, 0, 73, 2, 0, 73, 22, 0, 69, 73, 57, 0, 73, 2, 0, 73, 22, 0, 69, 73, 58, 0, 73, 
					2, 0, 73, 22, 0, 69, 73, 59, 0, 73, 2, 0, 73, 22, 0, 69, 73, 60, 0, 73, 2, 0, 73, 22, 0, 69, 73, 61, 0, 73, 2, 0, 73, 22, 0, 69, 73, 62, 0, 73, 2, 0, 73, 22, 0, 69, 73, 63, 0, 73, 
					2, 0, 73, 22, 0, 69, 73, 64, 0, 73, 2, 0, 73, 22, 0, 69, 73, 65, 0, 73, 2, 0, 73, 22, 0, 69, 73, 66, 0, 73, 2, 0, 73, 22, 0, 69, 73, 67, 0, 73, 2, 0, 73, 22, 0, 69, 73, 68, 0, 73, 
					2, 0, 73, 22, 0, 69, 77, 3, 1, 98, 76, 73, 56, 0, 69, 73, 0, 0, 73, 2, 0, 73, 37, 0, 69, 73, 6, 0, 73, 2, 0, 73, 37, 0, 69, 73, 7, 0, 73, 2, 0, 73, 37, 0, 69, 73, 8, 0, 73, 2, 
					0, 73, 37, 0, 69, 73, 9, 0, 73, 2, 0, 73, 37, 0, 69, 73, 10, 0, 73, 2, 0, 73, 37, 0, 69, 73, 11, 0, 73, 2, 0, 73, 37, 0, 69, 73, 12, 0, 73, 2, 0, 73, 37, 0, 69, 73, 13, 0, 73, 2, 
					0, 73, 37, 0, 69, 73, 14, 0, 73, 2, 0, 73, 37, 0, 69, 73, 15, 0, 73, 2, 0, 73, 37, 0, 69, 73, 16, 0, 73, 2, 0, 73, 37, 0, 69, 73, 17, 0, 73, 2, 0, 73, 37, 0, 69, 73, 18, 0, 73, 2, 
					0, 73, 37, 0, 69, 73, 19, 0, 73, 2, 0, 73, 37, 0, 69, 73, 20, 0, 73, 2, 0, 73, 37, 0, 69, 73, 21, 0, 73, 2, 0, 73, 37, 0, 69, 73, 22, 0, 73, 2, 0, 73, 37, 0, 69, 73, 23, 0, 73, 2, 
					0, 73, 37, 0, 69, 73, 24, 0, 73, 2, 0, 73, 37, 0, 69, 73, 25, 0, 73, 2, 0, 73, 37, 0, 69, 73, 26, 0, 73, 2, 0, 73, 37, 0, 69, 73, 27, 0, 73, 2, 0, 73, 37, 0, 69, 73, 28, 0, 73, 2, 
					0, 73, 37, 0, 69, 73, 29, 0, 73, 2, 0, 73, 37, 0, 69, 73, 30, 0, 73, 2, 0, 73, 37, 0, 69, 73, 31, 0, 73, 2, 0, 73, 37, 0, 69, 73, 32, 0, 73, 2, 0, 73, 37, 0, 69, 73, 33, 0, 73, 2, 
					0, 73, 37, 0, 69, 73, 34, 0, 73, 2, 0, 73, 37, 0, 69, 73, 35, 0, 73, 2, 0, 73, 37, 0, 69, 73, 36, 0, 73, 2, 0, 73, 37, 0, 69, 73, 37, 0, 73, 2, 0, 73, 37, 0, 69, 73, 38, 0, 73, 2, 
					0, 73, 37, 0, 69, 73, 39, 0, 73, 2, 0, 73, 37, 0, 69, 73, 40, 0, 73, 2, 0, 73, 37, 0, 69, 73, 41, 0, 73, 2, 0, 73, 37, 0, 69, 73, 42, 0, 73, 2, 0, 73, 37, 0, 69, 73, 43, 0, 73, 2, 
					0, 73, 37, 0, 69, 73, 44, 0, 73, 2, 0, 73, 37, 0, 69, 73, 45, 0, 73, 2, 0, 73, 37, 0, 69, 73, 46, 0, 73, 2, 0, 73, 37, 0, 69, 73, 47, 0, 73, 2, 0, 73, 37, 0, 69, 73, 48, 0, 73, 2, 
					0, 73, 37, 0, 69, 73, 49, 0, 73, 2, 0, 73, 37, 0, 69, 73, 50, 0, 73, 2, 0, 73, 37, 0, 69, 73, 51, 0, 73, 2, 0, 73, 37, 0, 69, 73, 52, 0, 73, 2, 0, 73, 37, 0, 69, 73, 53, 0, 73, 2, 
					0, 73, 37, 0, 69, 73, 54, 0, 73, 2, 0, 73, 37, 0, 69, 73, 55, 0, 73, 2, 0, 73, 37, 0, 69, 73, 56, 0, 73, 2, 0, 73, 37, 0, 69, 73, 57, 0, 73, 2, 0, 73, 37, 0, 69, 73, 58, 0, 73, 2, 
					0, 73, 37, 0, 69, 73, 59, 0, 73, 2, 0, 73, 37, 0, 69, 73, 60, 0, 73, 2, 0, 73, 37, 0, 69, 73, 61, 0, 73, 2, 0, 73, 37, 0, 69, 73, 62, 0, 73, 2, 0, 73, 37, 0, 69, 73, 63, 0, 73, 2, 
					0, 73, 37, 0, 69, 73, 64, 0, 73, 2, 0, 73, 37, 0, 69, 73, 65, 0, 73, 2, 0, 73, 37, 0, 69, 73, 66, 0, 73, 2, 0, 73, 37, 0, 69, 73, 67, 0, 73, 2, 0, 73, 37, 0, 69, 73, 68, 0, 73, 2, 
					0, 73, 37, 0, 69, 77, 3, 1, 98, 76, 73, 57, 0, 69, 73, 0, 0, 73, 2, 0, 73, 3, 0, 69, 73, 6, 0, 73, 2, 0, 73, 3, 0, 69, 73, 7, 0, 73, 2, 0, 73, 3, 0, 69, 73, 8, 0, 73, 2, 0, 
					73, 3, 0, 69, 73, 9, 0, 73, 2, 0, 73, 3, 0, 69, 73, 10, 0, 73, 2, 0, 73, 3, 0, 69, 73, 11, 0, 73, 2, 0, 73, 3, 0, 69, 73, 12, 0, 73, 2, 0, 73, 3, 0, 69, 73, 13, 0, 73, 2, 0, 
					73, 3, 0, 69, 73, 14, 0, 73, 2, 0, 73, 3, 0, 69, 73, 15, 0, 73, 2, 0, 73, 3, 0, 69, 73, 16, 0, 73, 2, 0, 73, 3, 0, 69, 73, 17, 0, 73, 2, 0, 73, 3, 0, 69, 73, 18, 0, 73, 2, 0, 
					73, 3, 0, 69, 73, 19, 0, 73, 2, 0, 73, 3, 0, 69, 73, 20, 0, 73, 2, 0, 73, 3, 0, 69, 73, 21, 0, 73, 2, 0, 73, 3, 0, 69, 73, 22, 0, 73, 2, 0, 73, 3, 0, 69, 73, 23, 0, 73, 2, 0, 
					73, 3, 0, 69, 73, 24, 0, 73, 2, 0, 73, 3, 0, 69, 73, 25, 0, 73, 2, 0, 73, 3, 0, 69, 73, 26, 0, 73, 2, 0, 73, 3, 0, 69, 73, 27, 0, 73, 2, 0, 73, 3, 0, 69, 73, 28, 0, 73, 2, 0, 
					73, 3, 0, 69, 73, 29, 0, 73, 2, 0, 73, 3, 0, 69, 73, 30, 0, 73, 2, 0, 73, 3, 0, 69, 73, 31, 0, 73, 2, 0, 73, 3, 0, 69, 73, 32, 0, 73, 2, 0, 73, 3, 0, 69, 73, 33, 0, 73, 2, 0, 
					73, 3, 0, 69, 73, 34, 0, 73, 2, 0, 73, 3, 0, 69, 73, 35, 0, 73, 2, 0, 73, 3, 0, 69, 73, 36, 0, 73, 2, 0, 73, 3, 0, 69, 73, 37, 0, 73, 2, 0, 73, 3, 0, 69, 73, 38, 0, 73, 2, 0, 
					73, 3, 0, 69, 73, 39, 0, 73, 2, 0, 73, 3, 0, 69, 73, 40, 0, 73, 2, 0, 73, 3, 0, 69, 73, 41, 0, 73, 2, 0, 73, 3, 0, 69, 73, 42, 0, 73, 2, 0, 73, 3, 0, 69, 73, 43, 0, 73, 2, 0, 
					73, 3, 0, 69, 73, 44, 0, 73, 2, 0, 73, 3, 0, 69, 73, 45, 0, 73, 2, 0, 73, 3, 0, 69, 73, 46, 0, 73, 2, 0, 73, 3, 0, 69, 73, 47, 0, 73, 2, 0, 73, 3, 0, 69, 73, 48, 0, 73, 2, 0, 
					73, 3, 0, 69, 73, 49, 0, 73, 2, 0, 73, 3, 0, 69, 73, 50, 0, 73, 2, 0, 73, 3, 0, 69, 73, 51, 0, 73, 2, 0, 73, 3, 0, 69, 73, 52, 0, 73, 2, 0, 73, 3, 0, 69, 73, 53, 0, 73, 2, 0, 
					73, 3, 0, 69, 73, 54, 0, 73, 2, 0, 73, 3, 0, 69, 73, 55, 0, 73, 2, 0, 73, 3, 0, 69, 73, 56, 0, 73, 2, 0, 73, 3, 0, 69, 73, 57, 0, 73, 2, 0, 73, 3, 0, 69, 73, 58, 0, 73, 2, 0, 
					73, 3, 0, 69, 73, 59, 0, 73, 2, 0, 73, 3, 0, 69, 73, 60, 0, 73, 2, 0, 73, 3, 0, 69, 73, 61, 0, 73, 2, 0, 73, 3, 0, 69, 73, 62, 0, 73, 2, 0, 73, 3, 0, 69, 73, 63, 0, 73, 2, 0, 
					73, 3, 0, 69, 73, 64, 0, 73, 2, 0, 73, 3, 0, 69, 73, 65, 0, 73, 2, 0, 73, 3, 0, 69, 73, 66, 0, 73, 2, 0, 73, 3, 0, 69, 73, 67, 0, 73, 2, 0, 73, 3, 0, 69, 73, 68, 0, 73, 2, 0, 
					73, 3, 0, 69, 77, 3, 1, 98, 76, 73, 58, 0, 69, 73, 0, 0, 73, 2, 0, 73, 7, 0, 69, 73, 6, 0, 73, 2, 0, 73, 7, 0, 69, 73, 7, 0, 73, 2, 0, 73, 7, 0, 69, 73, 8, 0, 73, 2, 0, 73, 
					7, 0, 69, 73, 9, 0, 73, 2, 0, 73, 7, 0, 69, 73, 10, 0, 73, 2, 0, 73, 7, 0, 69, 73, 11, 0, 73, 2, 0, 73, 7, 0, 69, 73, 12, 0, 73, 2, 0, 73, 7, 0, 69, 73, 13, 0, 73, 2, 0, 73, 
					7, 0, 69, 73, 14, 0, 73, 2, 0, 73, 7, 0, 69, 73, 15, 0, 73, 2, 0, 73, 7, 0, 69, 73, 16, 0, 73, 2, 0, 73, 7, 0, 69, 73, 17, 0, 73, 2, 0, 73, 7, 0, 69, 73, 18, 0, 73, 2, 0, 73, 
					7, 0, 69, 73, 19, 0, 73, 2, 0, 73, 7, 0, 69, 73, 20, 0, 73, 2, 0, 73, 7, 0, 69, 73, 21, 0, 73, 2, 0, 73, 7, 0, 69, 73, 22, 0, 73, 2, 0, 73, 7, 0, 69, 73, 23, 0, 73, 2, 0, 73, 
					7, 0, 69, 73, 24, 0, 73, 2, 0, 73, 7, 0, 69, 73, 25, 0, 73, 2, 0, 73, 7, 0, 69, 73, 26, 0, 73, 2, 0, 73, 7, 0, 69, 73, 27, 0, 73, 2, 0, 73, 7, 0, 69, 73, 28, 0, 73, 2, 0, 73, 
					7, 0, 69, 73, 29, 0, 73, 2, 0, 73, 7, 0, 69, 73, 30, 0, 73, 2, 0, 73, 7, 0, 69, 73, 31, 0, 73, 2, 0, 73, 7, 0, 69, 73, 32, 0, 73, 2, 0, 73, 7, 0, 69, 73, 33, 0, 73, 2, 0, 73, 
					7, 0, 69, 73, 34, 0, 73, 2, 0, 73, 7, 0, 69, 73, 35, 0, 73, 2, 0, 73, 7, 0, 69, 73, 36, 0, 73, 2, 0, 73, 7, 0, 69, 73, 37, 0, 73, 2, 0, 73, 7, 0, 69, 73, 38, 0, 73, 2, 0, 73, 
					7, 0, 69, 73, 39, 0, 73, 2, 0, 73, 7, 0, 69, 73, 40, 0, 73, 2, 0, 73, 7, 0, 69, 73, 41, 0, 73, 2, 0, 73, 7, 0, 69, 73, 42, 0, 73, 2, 0, 73, 7, 0, 69, 73, 43, 0, 73, 2, 0, 73, 
					7, 0, 69, 73, 44, 0, 73, 2, 0, 73, 7, 0, 69, 73, 45, 0, 73, 2, 0, 73, 7, 0, 69, 73, 46, 0, 73, 2, 0, 73, 7, 0, 69, 73, 47, 0, 73, 2, 0, 73, 7, 0, 69, 73, 48, 0, 73, 2, 0, 73, 
					7, 0, 69, 73, 49, 0, 73, 2, 0, 73, 7, 0, 69, 73, 50, 0, 73, 2, 0, 73, 7, 0, 69, 73, 51, 0, 73, 2, 0, 73, 7, 0, 69, 73, 52, 0, 73, 2, 0, 73, 7, 0, 69, 73, 53, 0, 73, 2, 0, 73, 
					7, 0, 69, 73, 54, 0, 73, 2, 0, 73, 7, 0, 69, 73, 55, 0, 73, 2, 0, 73, 7, 0, 69, 73, 56, 0, 73, 2, 0, 73, 7, 0, 69, 73, 57, 0, 73, 2, 0, 73, 7, 0, 69, 73, 58, 0, 73, 2, 0, 73, 
					7, 0, 69, 73, 59, 0, 73, 2, 0, 73, 7, 0, 69, 73, 60, 0, 73, 2, 0, 73, 7, 0, 69, 73, 61, 0, 73, 2, 0, 73, 7, 0, 69, 73, 62, 0, 73, 2, 0, 73, 7, 0, 69, 73, 63, 0, 73, 2, 0, 73, 
					7, 0, 69, 73, 64, 0, 73, 2, 0, 73, 7, 0, 69, 73, 65, 0, 73, 2, 0, 73, 7, 0, 69, 73, 66, 0, 73, 2, 0, 73, 7, 0, 69, 73, 67, 0, 73, 2, 0, 73, 7, 0, 69, 73, 68, 0, 73, 2, 0, 73, 
					7, 0, 69, 77, 3, 1, 98, 76, 73, 59, 0, 69, 73, 0, 0, 73, 2, 0, 73, 6, 0, 69, 73, 6, 0, 73, 2, 0, 73, 6, 0, 69, 73, 7, 0, 73, 2, 0, 73, 6, 0, 69, 73, 8, 0, 73, 2, 0, 73, 6, 
					0, 69, 73, 9, 0, 73, 2, 0, 73, 6, 0, 69, 73, 10, 0, 73, 2, 0, 73, 6, 0, 69, 73, 11, 0, 73, 2, 0, 73, 6, 0, 69, 73, 12, 0, 73, 2, 0, 73, 6, 0, 69, 73, 13, 0, 73, 2, 0, 73, 6, 
					0, 69, 73, 14, 0, 73, 2, 0, 73, 6, 0, 69, 73, 15, 0, 73, 2, 0, 73, 6, 0, 69, 73, 16, 0, 73, 2, 0, 73, 6, 0, 69, 73, 17, 0, 73, 2, 0, 73, 6, 0, 69, 73, 18, 0, 73, 2, 0, 73, 6, 
					0, 69, 73, 19, 0, 73, 2, 0, 73, 6, 0, 69, 73, 20, 0, 73, 2, 0, 73, 6, 0, 69, 73, 21, 0, 73, 2, 0, 73, 6, 0, 69, 73, 22, 0, 73, 2, 0, 73, 6, 0, 69, 73, 23, 0, 73, 2, 0, 73, 6, 
					0, 69, 73, 24, 0, 73, 2, 0, 73, 6, 0, 69, 73, 25, 0, 73, 2, 0, 73, 6, 0, 69, 73, 26, 0, 73, 2, 0, 73, 6, 0, 69, 73, 27, 0, 73, 2, 0, 73, 6, 0, 69, 73, 28, 0, 73, 2, 0, 73, 6, 
					0, 69, 73, 29, 0, 73, 2, 0, 73, 6, 0, 69, 73, 30, 0, 73, 2, 0, 73, 6, 0, 69, 73, 31, 0, 73, 2, 0, 73, 6, 0, 69, 73, 32, 0, 73, 2, 0, 73, 6, 0, 69, 73, 33, 0, 73, 2, 0, 73, 6, 
					0, 69, 73, 34, 0, 73, 2, 0, 73, 6, 0, 69, 73, 35, 0, 73, 2, 0, 73, 6, 0, 69, 73, 36, 0, 73, 2, 0, 73, 6, 0, 69, 73, 37, 0, 73, 2, 0, 73, 6, 0, 69, 73, 38, 0, 73, 2, 0, 73, 6, 
					0, 69, 73, 39, 0, 73, 2, 0, 73, 6, 0, 69, 73, 40, 0, 73, 2, 0, 73, 6, 0, 69, 73, 41, 0, 73, 2, 0, 73, 6, 0, 69, 73, 42, 0, 73, 2, 0, 73, 6, 0, 69, 73, 43, 0, 73, 2, 0, 73, 6, 
					0, 69, 73, 44, 0, 73, 2, 0, 73, 6, 0, 69, 73, 45, 0, 73, 2, 0, 73, 6, 0, 69, 73, 46, 0, 73, 2, 0, 73, 6, 0, 69, 73, 47, 0, 73, 2, 0, 73, 6, 0, 69, 73, 48, 0, 73, 2, 0, 73, 6, 
					0, 69, 73, 49, 0, 73, 2, 0, 73, 6, 0, 69, 73, 50, 0, 73, 2, 0, 73, 6, 0, 69, 73, 51, 0, 73, 2, 0, 73, 6, 0, 69, 73, 52, 0, 73, 2, 0, 73, 6, 0, 69, 73, 53, 0, 73, 2, 0, 73, 6, 
					0, 69, 73, 54, 0, 73, 2, 0, 73, 6, 0, 69, 73, 55, 0, 73, 2, 0, 73, 6, 0, 69, 73, 56, 0, 73, 2, 0, 73, 6, 0, 69, 73, 57, 0, 73, 2, 0, 73, 6, 0, 69, 73, 58, 0, 73, 2, 0, 73, 6, 
					0, 69, 73, 59, 0, 73, 2, 0, 73, 6, 0, 69, 73, 60, 0, 73, 2, 0, 73, 6, 0, 69, 73, 61, 0, 73, 2, 0, 73, 6, 0, 69, 73, 62, 0, 73, 2, 0, 73, 6, 0, 69, 73, 63, 0, 73, 2, 0, 73, 6, 
					0, 69, 73, 64, 0, 73, 2, 0, 73, 6, 0, 69, 73, 65, 0, 73, 2, 0, 73, 6, 0, 69, 73, 66, 0, 73, 2, 0, 73, 6, 0, 69, 73, 67, 0, 73, 2, 0, 73, 6, 0, 69, 73, 68, 0, 73, 2, 0, 73, 6, 
					0, 69, 77, 3, 1, 98, 76, 73, 60, 0, 69, 73, 0, 0, 73, 2, 0, 73, 12, 0, 69, 73, 6, 0, 73, 2, 0, 73, 12, 0, 69, 73, 7, 0, 73, 2, 0, 73, 12, 0, 69, 73, 8, 0, 73, 2, 0, 73, 12, 0, 
					69, 73, 9, 0, 73, 2, 0, 73, 12, 0, 69, 73, 10, 0, 73, 2, 0, 73, 12, 0, 69, 73, 11, 0, 73, 2, 0, 73, 12, 0, 69, 73, 12, 0, 73, 2, 0, 73, 12, 0, 69, 73, 13, 0, 73, 2, 0, 73, 12, 0, 
					69, 73, 14, 0, 73, 2, 0, 73, 12, 0, 69, 73, 15, 0, 73, 2, 0, 73, 12, 0, 69, 73, 16, 0, 73, 2, 0, 73, 12, 0, 69, 73, 17, 0, 73, 2, 0, 73, 12, 0, 69, 73, 18, 0, 73, 2, 0, 73, 12, 0, 
					69, 73, 19, 0, 73, 2, 0, 73, 12, 0, 69, 73, 20, 0, 73, 2, 0, 73, 12, 0, 69, 73, 21, 0, 73, 2, 0, 73, 12, 0, 69, 73, 22, 0, 73, 2, 0, 73, 12, 0, 69, 73, 23, 0, 73, 2, 0, 73, 12, 0, 
					69, 73, 24, 0, 73, 2, 0, 73, 12, 0, 69, 73, 25, 0, 73, 2, 0, 73, 12, 0, 69, 73, 26, 0, 73, 2, 0, 73, 12, 0, 69, 73, 27, 0, 73, 2, 0, 73, 12, 0, 69, 73, 28, 0, 73, 2, 0, 73, 12, 0, 
					69, 73, 29, 0, 73, 2, 0, 73, 12, 0, 69, 73, 30, 0, 73, 2, 0, 73, 12, 0, 69, 73, 31, 0, 73, 2, 0, 73, 12, 0, 69, 73, 32, 0, 73, 2, 0, 73, 12, 0, 69, 73, 33, 0, 73, 2, 0, 73, 12, 0, 
					69, 73, 34, 0, 73, 2, 0, 73, 12, 0, 69, 73, 35, 0, 73, 2, 0, 73, 12, 0, 69, 73, 36, 0, 73, 2, 0, 73, 12, 0, 69, 73, 37, 0, 73, 2, 0, 73, 12, 0, 69, 73, 38, 0, 73, 2, 0, 73, 12, 0, 
					69, 73, 39, 0, 73, 2, 0, 73, 12, 0, 69, 73, 40, 0, 73, 2, 0, 73, 12, 0, 69, 73, 41, 0, 73, 2, 0, 73, 12, 0, 69, 73, 42, 0, 73, 2, 0, 73, 12, 0, 69, 73, 43, 0, 73, 2, 0, 73, 12, 0, 
					69, 73, 44, 0, 73, 2, 0, 73, 12, 0, 69, 73, 45, 0, 73, 2, 0, 73, 12, 0, 69, 73, 46, 0, 73, 2, 0, 73, 12, 0, 69, 73, 47, 0, 73, 2, 0, 73, 12, 0, 69, 73, 48, 0, 73, 2, 0, 73, 12, 0, 
					69, 73, 49, 0, 73, 2, 0, 73, 12, 0, 69, 73, 50, 0, 73, 2, 0, 73, 12, 0, 69, 73, 51, 0, 73, 2, 0, 73, 12, 0, 69, 73, 52, 0, 73, 2, 0, 73, 12, 0, 69, 73, 53, 0, 73, 2, 0, 73, 12, 0, 
					69, 73, 54, 0, 73, 2, 0, 73, 12, 0, 69, 73, 55, 0, 73, 2, 0, 73, 12, 0, 69, 73, 56, 0, 73, 2, 0, 73, 12, 0, 69, 73, 57, 0, 73, 2, 0, 73, 12, 0, 69, 73, 58, 0, 73, 2, 0, 73, 12, 0, 
					69, 73, 59, 0, 73, 2, 0, 73, 12, 0, 69, 73, 60, 0, 73, 2, 0, 73, 12, 0, 69, 73, 61, 0, 73, 2, 0, 73, 12, 0, 69, 73, 62, 0, 73, 2, 0, 73, 12, 0, 69, 73, 63, 0, 73, 2, 0, 73, 12, 0, 
					69, 73, 64, 0, 73, 2, 0, 73, 12, 0, 69, 73, 65, 0, 73, 2, 0, 73, 12, 0, 69, 73, 66, 0, 73, 2, 0, 73, 12, 0, 69, 73, 67, 0, 73, 2, 0, 73, 12, 0, 69, 73, 68, 0, 73, 2, 0, 73, 12, 0, 
					69, 77, 3, 1, 98, 76, 73, 61, 0, 69, 73, 0, 0, 73, 2, 0, 73, 31, 0, 69, 73, 6, 0, 73, 2, 0, 73, 31, 0, 69, 73, 7, 0, 73, 2, 0, 73, 31, 0, 69, 73, 8, 0, 73, 2, 0, 73, 31, 0, 69, 
					73, 9, 0, 73, 2, 0, 73, 31, 0, 69, 73, 10, 0, 73, 2, 0, 73, 31, 0, 69, 73, 11, 0, 73, 2, 0, 73, 31, 0, 69, 73, 12, 0, 73, 2, 0, 73, 31, 0, 69, 73, 13, 0, 73, 2, 0, 73, 31, 0, 69, 
					73, 14, 0, 73, 2, 0, 73, 31, 0, 69, 73, 15, 0, 73, 2, 0, 73, 31, 0, 69, 73, 16, 0, 73, 2, 0, 73, 31, 0, 69, 73, 17, 0, 73, 2, 0, 73, 31, 0, 69, 73, 18, 0, 73, 2, 0, 73, 31, 0, 69, 
					73, 19, 0, 73, 2, 0, 73, 31, 0, 69, 73, 20, 0, 73, 2, 0, 73, 31, 0, 69, 73, 21, 0, 73, 2, 0, 73, 31, 0, 69, 73, 22, 0, 73, 2, 0, 73, 31, 0, 69, 73, 23, 0, 73, 2, 0, 73, 31, 0, 69, 
					73, 24, 0, 73, 2, 0, 73, 31, 0, 69, 73, 25, 0, 73, 2, 0, 73, 31, 0, 69, 73, 26, 0, 73, 2, 0, 73, 31, 0, 69, 73, 27, 0, 73, 2, 0, 73, 31, 0, 69, 73, 28, 0, 73, 2, 0, 73, 31, 0, 69, 
					73, 29, 0, 73, 2, 0, 73, 31, 0, 69, 73, 30, 0, 73, 2, 0, 73, 31, 0, 69, 73, 31, 0, 73, 2, 0, 73, 31, 0, 69, 73, 32, 0, 73, 2, 0, 73, 31, 0, 69, 73, 33, 0, 73, 2, 0, 73, 31, 0, 69, 
					73, 34, 0, 73, 2, 0, 73, 31, 0, 69, 73, 35, 0, 73, 2, 0, 73, 31, 0, 69, 73, 36, 0, 73, 2, 0, 73, 31, 0, 69, 73, 37, 0, 73, 2, 0, 73, 31, 0, 69, 73, 38, 0, 73, 2, 0, 73, 31, 0, 69, 
					73, 39, 0, 73, 2, 0, 73, 31, 0, 69, 73, 40, 0, 73, 2, 0, 73, 31, 0, 69, 73, 41, 0, 73, 2, 0, 73, 31, 0, 69, 73, 42, 0, 73, 2, 0, 73, 31, 0, 69, 73, 43, 0, 73, 2, 0, 73, 31, 0, 69, 
					73, 44, 0, 73, 2, 0, 73, 31, 0, 69, 73, 45, 0, 73, 2, 0, 73, 31, 0, 69, 73, 46, 0, 73, 2, 0, 73, 31, 0, 69, 73, 47, 0, 73, 2, 0, 73, 31, 0, 69, 73, 48, 0, 73, 2, 0, 73, 31, 0, 69, 
					73, 49, 0, 73, 2, 0, 73, 31, 0, 69, 73, 50, 0, 73, 2, 0, 73, 31, 0, 69, 73, 51, 0, 73, 2, 0, 73, 31, 0, 69, 73, 52, 0, 73, 2, 0, 73, 31, 0, 69, 73, 53, 0, 73, 2, 0, 73, 31, 0, 69, 
					73, 54, 0, 73, 2, 0, 73, 31, 0, 69, 73, 55, 0, 73, 2, 0, 73, 31, 0, 69, 73, 56, 0, 73, 2, 0, 73, 31, 0, 69, 73, 57, 0, 73, 2, 0, 73, 31, 0, 69, 73, 58, 0, 73, 2, 0, 73, 31, 0, 69, 
					73, 59, 0, 73, 2, 0, 73, 31, 0, 69, 73, 60, 0, 73, 2, 0, 73, 31, 0, 69, 73, 61, 0, 73, 2, 0, 73, 31, 0, 69, 73, 62, 0, 73, 2, 0, 73, 31, 0, 69, 73, 63, 0, 73, 2, 0, 73, 31, 0, 69, 
					73, 64, 0, 73, 2, 0, 73, 31, 0, 69, 73, 65, 0, 73, 2, 0, 73, 31, 0, 69, 73, 66, 0, 73, 2, 0, 73, 31, 0, 69, 73, 67, 0, 73, 2, 0, 73, 31, 0, 69, 73, 68, 0, 73, 2, 0, 73, 31, 0, 69, 
					77, 3, 1, 98, 76, 73, 62, 0, 69, 73, 0, 0, 73, 2, 0, 73, 16, 0, 69, 73, 6, 0, 73, 2, 0, 73, 16, 0, 69, 73, 7, 0, 73, 2, 0, 73, 16, 0, 69, 73, 8, 0, 73, 2, 0, 73, 16, 0, 69, 73, 
					9, 0, 73, 2, 0, 73, 16, 0, 69, 73, 10, 0, 73, 2, 0, 73, 16, 0, 69, 73, 11, 0, 73, 2, 0, 73, 16, 0, 69, 73, 12, 0, 73, 2, 0, 73, 16, 0, 69, 73, 13, 0, 73, 2, 0, 73, 16, 0, 69, 73, 
					14, 0, 73, 2, 0, 73, 16, 0, 69, 73, 15, 0, 73, 2, 0, 73, 16, 0, 69, 73, 16, 0, 73, 2, 0, 73, 16, 0, 69, 73, 17, 0, 73, 2, 0, 73, 16, 0, 69, 73, 18, 0, 73, 2, 0, 73, 16, 0, 69, 73, 
					19, 0, 73, 2, 0, 73, 16, 0, 69, 73, 20, 0, 73, 2, 0, 73, 16, 0, 69, 73, 21, 0, 73, 2, 0, 73, 16, 0, 69, 73, 22, 0, 73, 2, 0, 73, 16, 0, 69, 73, 23, 0, 73, 2, 0, 73, 16, 0, 69, 73, 
					24, 0, 73, 2, 0, 73, 16, 0, 69, 73, 25, 0, 73, 2, 0, 73, 16, 0, 69, 73, 26, 0, 73, 2, 0, 73, 16, 0, 69, 73, 27, 0, 73, 2, 0, 73, 16, 0, 69, 73, 28, 0, 73, 2, 0, 73, 16, 0, 69, 73, 
					29, 0, 73, 2, 0, 73, 16, 0, 69, 73, 30, 0, 73, 2, 0, 73, 16, 0, 69, 73, 31, 0, 73, 2, 0, 73, 16, 0, 69, 73, 32, 0, 73, 2, 0, 73, 16, 0, 69, 73, 33, 0, 73, 2, 0, 73, 16, 0, 69, 73, 
					34, 0, 73, 2, 0, 73, 16, 0, 69, 73, 35, 0, 73, 2, 0, 73, 16, 0, 69, 73, 36, 0, 73, 2, 0, 73, 16, 0, 69, 73, 37, 0, 73, 2, 0, 73, 16, 0, 69, 73, 38, 0, 73, 2, 0, 73, 16, 0, 69, 73, 
					39, 0, 73, 2, 0, 73, 16, 0, 69, 73, 40, 0, 73, 2, 0, 73, 16, 0, 69, 73, 41, 0, 73, 2, 0, 73, 16, 0, 69, 73, 42, 0, 73, 2, 0, 73, 16, 0, 69, 73, 43, 0, 73, 2, 0, 73, 16, 0, 69, 73, 
					44, 0, 73, 2, 0, 73, 16, 0, 69, 73, 45, 0, 73, 2, 0, 73, 16, 0, 69, 73, 46, 0, 73, 2, 0, 73, 16, 0, 69, 73, 47, 0, 73, 2, 0, 73, 16, 0, 69, 73, 48, 0, 73, 2, 0, 73, 16, 0, 69, 73, 
					49, 0, 73, 2, 0, 73, 16, 0, 69, 73, 50, 0, 73, 2, 0, 73, 16, 0, 69, 73, 51, 0, 73, 2, 0, 73, 16, 0, 69, 73, 52, 0, 73, 2, 0, 73, 16, 0, 69, 73, 53, 0, 73, 2, 0, 73, 16, 0, 69, 73, 
					54, 0, 73, 2, 0, 73, 16, 0, 69, 73, 55, 0, 73, 2, 0, 73, 16, 0, 69, 73, 56, 0, 73, 2, 0, 73, 16, 0, 69, 73, 57, 0, 73, 2, 0, 73, 16, 0, 69, 73, 58, 0, 73, 2, 0, 73, 16, 0, 69, 73, 
					59, 0, 73, 2, 0, 73, 16, 0, 69, 73, 60, 0, 73, 2, 0, 73, 16, 0, 69, 73, 61, 0, 73, 2, 0, 73, 16, 0, 69, 73, 62, 0, 73, 2, 0, 73, 16, 0, 69, 73, 63, 0, 73, 2, 0, 73, 16, 0, 69, 73, 
					64, 0, 73, 2, 0, 73, 16, 0, 69, 73, 65, 0, 73, 2, 0, 73, 16, 0, 69, 73, 66, 0, 73, 2, 0, 73, 16, 0, 69, 73, 67, 0, 73, 2, 0, 73, 16, 0, 69, 73, 68, 0, 73, 2, 0, 73, 16, 0, 69, 77, 
					3, 1, 98, 76, 73, 63, 0, 69, 73, 0, 0, 73, 2, 0, 73, 0, 0, 69, 73, 6, 0, 73, 2, 0, 73, 0, 0, 69, 73, 7, 0, 73, 2, 0, 73, 0, 0, 69, 73, 8, 0, 73, 2, 0, 73, 0, 0, 69, 73, 9, 
					0, 73, 2, 0, 73, 0, 0, 69, 73, 10, 0, 73, 2, 0, 73, 0, 0, 69, 73, 11, 0, 73, 2, 0, 73, 0, 0, 69, 73, 12, 0, 73, 2, 0, 73, 0, 0, 69, 73, 13, 0, 73, 2, 0, 73, 0, 0, 69, 73, 14, 
					0, 73, 2, 0, 73, 0, 0, 69, 73, 15, 0, 73, 2, 0, 73, 0, 0, 69, 73, 16, 0, 73, 2, 0, 73, 0, 0, 69, 73, 17, 0, 73, 2, 0, 73, 0, 0, 69, 73, 18, 0, 73, 2, 0, 73, 0, 0, 69, 73, 19, 
					0, 73, 2, 0, 73, 0, 0, 69, 73, 20, 0, 73, 2, 0, 73, 0, 0, 69, 73, 21, 0, 73, 2, 0, 73, 0, 0, 69, 73, 22, 0, 73, 2, 0, 73, 0, 0, 69, 73, 23, 0, 73, 2, 0, 73, 0, 0, 69, 73, 24, 
					0, 73, 2, 0, 73, 0, 0, 69, 73, 25, 0, 73, 2, 0, 73, 0, 0, 69, 73, 26, 0, 73, 2, 0, 73, 0, 0, 69, 73, 27, 0, 73, 2, 0, 73, 0, 0, 69, 73, 28, 0, 73, 2, 0, 73, 0, 0, 69, 73, 29, 
					0, 73, 2, 0, 73, 0, 0, 69, 73, 30, 0, 73, 2, 0, 73, 0, 0, 69, 73, 31, 0, 73, 2, 0, 73, 0, 0, 69, 73, 32, 0, 73, 2, 0, 73, 0, 0, 69, 73, 33, 0, 73, 2, 0, 73, 0, 0, 69, 73, 34, 
					0, 73, 2, 0, 73, 0, 0, 69, 73, 35, 0, 73, 2, 0, 73, 0, 0, 69, 73, 36, 0, 73, 2, 0, 73, 0, 0, 69, 73, 37, 0, 73, 2, 0, 73, 0, 0, 69, 73, 38, 0, 73, 2, 0, 73, 0, 0, 69, 73, 39, 
					0, 73, 2, 0, 73, 0, 0, 69, 73, 40, 0, 73, 2, 0, 73, 0, 0, 69, 73, 41, 0, 73, 2, 0, 73, 0, 0, 69, 73, 42, 0, 73, 2, 0, 73, 0, 0, 69, 73, 43, 0, 73, 2, 0, 73, 0, 0, 69, 73, 44, 
					0, 73, 2, 0, 73, 0, 0, 69, 73, 45, 0, 73, 2, 0, 73, 0, 0, 69, 73, 46, 0, 73, 2, 0, 73, 0, 0, 69, 73, 47, 0, 73, 2, 0, 73, 0, 0, 69, 73, 48, 0, 73, 2, 0, 73, 0, 0, 69, 73, 49, 
					0, 73, 2, 0, 73, 0, 0, 69, 73, 50, 0, 73, 2, 0, 73, 0, 0, 69, 73, 51, 0, 73, 2, 0, 73, 0, 0, 69, 73, 52, 0, 73, 2, 0, 73, 0, 0, 69, 73, 53, 0, 73, 2, 0, 73, 0, 0, 69, 73, 54, 
					0, 73, 2, 0, 73, 0, 0, 69, 73, 55, 0, 73, 2, 0, 73, 0, 0, 69, 73, 56, 0, 73, 2, 0, 73, 0, 0, 69, 73, 57, 0, 73, 2, 0, 73, 0, 0, 69, 73, 58, 0, 73, 2, 0, 73, 0, 0, 69, 73, 59, 
					0, 73, 2, 0, 73, 0, 0, 69, 73, 60, 0, 73, 2, 0, 73, 0, 0, 69, 73, 61, 0, 73, 2, 0, 73, 0, 0, 69, 73, 62, 0, 73, 2, 0, 73, 0, 0, 69, 73, 63, 0, 73, 2, 0, 73, 0, 0, 69, 73, 64, 
					0, 73, 2, 0, 73, 0, 0, 69, 73, 65, 0, 73, 2, 0, 73, 0, 0, 69, 73, 66, 0, 73, 2, 0, 73, 0, 0, 69, 73, 67, 0, 73, 2, 0, 73, 0, 0, 69, 73, 68, 0, 73, 2, 0, 73, 0, 0, 69, 77, 3, 
					1, 98, 76, 73, 64, 0, 69, 73, 0, 0, 73, 2, 0, 73, 63, 0, 69, 73, 6, 0, 73, 2, 0, 73, 63, 0, 69, 73, 7, 0, 73, 2, 0, 73, 63, 0, 69, 73, 8, 0, 73, 2, 0, 73, 63, 0, 69, 73, 9, 0, 
					73, 2, 0, 73, 63, 0, 69, 73, 10, 0, 73, 2, 0, 73, 63, 0, 69, 73, 11, 0, 73, 2, 0, 73, 63, 0, 69, 73, 12, 0, 73, 2, 0, 73, 63, 0, 69, 73, 13, 0, 73, 2, 0, 73, 63, 0, 69, 73, 14, 0, 
					73, 2, 0, 73, 63, 0, 69, 73, 15, 0, 73, 2, 0, 73, 63, 0, 69, 73, 16, 0, 73, 2, 0, 73, 63, 0, 69, 73, 17, 0, 73, 2, 0, 73, 63, 0, 69, 73, 18, 0, 73, 2, 0, 73, 63, 0, 69, 73, 19, 0, 
					73, 2, 0, 73, 63, 0, 69, 73, 20, 0, 73, 2, 0, 73, 63, 0, 69, 73, 21, 0, 73, 2, 0, 73, 63, 0, 69, 73, 22, 0, 73, 2, 0, 73, 63, 0, 69, 73, 23, 0, 73, 2, 0, 73, 63, 0, 69, 73, 24, 0, 
					73, 2, 0, 73, 63, 0, 69, 73, 25, 0, 73, 2, 0, 73, 63, 0, 69, 73, 26, 0, 73, 2, 0, 73, 63, 0, 69, 73, 27, 0, 73, 2, 0, 73, 63, 0, 69, 73, 28, 0, 73, 2, 0, 73, 63, 0, 69, 73, 29, 0, 
					73, 2, 0, 73, 63, 0, 69, 73, 30, 0, 73, 2, 0, 73, 63, 0, 69, 73, 31, 0, 73, 2, 0, 73, 63, 0, 69, 73, 32, 0, 73, 2, 0, 73, 63, 0, 69, 73, 33, 0, 73, 2, 0, 73, 63, 0, 69, 73, 34, 0, 
					73, 2, 0, 73, 63, 0, 69, 73, 35, 0, 73, 2, 0, 73, 63, 0, 69, 73, 36, 0, 73, 2, 0, 73, 63, 0, 69, 73, 37, 0, 73, 2, 0, 73, 63, 0, 69, 73, 38, 0, 73, 2, 0, 73, 63, 0, 69, 73, 39, 0, 
					73, 2, 0, 73, 63, 0, 69, 73, 40, 0, 73, 2, 0, 73, 63, 0, 69, 73, 41, 0, 73, 2, 0, 73, 63, 0, 69, 73, 42, 0, 73, 2, 0, 73, 63, 0, 69, 73, 43, 0, 73, 2, 0, 73, 63, 0, 69, 73, 44, 0, 
					73, 2, 0, 73, 63, 0, 69, 73, 45, 0, 73, 2, 0, 73, 63, 0, 69, 73, 46, 0, 73, 2, 0, 73, 63, 0, 69, 73, 47, 0, 73, 2, 0, 73, 63, 0, 69, 73, 48, 0, 73, 2, 0, 73, 63, 0, 69, 73, 49, 0, 
					73, 2, 0, 73, 63, 0, 69, 73, 50, 0, 73, 2, 0, 73, 63, 0, 69, 73, 51, 0, 73, 2, 0, 73, 63, 0, 69, 73, 52, 0, 73, 2, 0, 73, 63, 0, 69, 73, 53, 0, 73, 2, 0, 73, 63, 0, 69, 73, 54, 0, 
					73, 2, 0, 73, 63, 0, 69, 73, 55, 0, 73, 2, 0, 73, 63, 0, 69, 73, 56, 0, 73, 2, 0, 73, 63, 0, 69, 73, 57, 0, 73, 2, 0, 73, 63, 0, 69, 73, 58, 0, 73, 2, 0, 73, 63, 0, 69, 73, 59, 0, 
					73, 2, 0, 73, 63, 0, 69, 73, 60, 0, 73, 2, 0, 73, 63, 0, 69, 73, 61, 0, 73, 2, 0, 73, 63, 0, 69, 73, 62, 0, 73, 2, 0, 73, 63, 0, 69, 73, 63, 0, 73, 2, 0, 73, 63, 0, 69, 73, 64, 0, 
					73, 2, 0, 73, 63, 0, 69, 73, 65, 0, 73, 2, 0, 73, 63, 0, 69, 73, 66, 0, 73, 2, 0, 73, 63, 0, 69, 73, 67, 0, 73, 2, 0, 73, 63, 0, 69, 73, 68, 0, 73, 2, 0, 73, 63, 0, 69, 77, 7, 1, 
					98, 76, 73, 65, 0, 69, 73, 0, 0, 73, 4, 0, 73, 0, 0, 69, 73, 6, 0, 73, 1, 0, 73, 1, 0, 69, 73, 7, 0, 73, 1, 0, 73, 2, 0, 69, 73, 8, 0, 73, 1, 0, 73, 3, 0, 69, 73, 9, 0, 73, 
					1, 0, 73, 4, 0, 69, 73, 10, 0, 73, 1, 0, 73, 5, 0, 69, 73, 11, 0, 73, 1, 0, 73, 6, 0, 69, 73, 12, 0, 73, 1, 0, 73, 7, 0, 69, 73, 13, 0, 73, 1, 0, 73, 8, 0, 69, 73, 14, 0, 73, 
					1, 0, 73, 9, 0, 69, 73, 15, 0, 73, 1, 0, 73, 10, 0, 69, 73, 16, 0, 73, 1, 0, 73, 11, 0, 69, 73, 17, 0, 73, 1, 0, 73, 12, 0, 69, 73, 18, 0, 73, 1, 0, 73, 13, 0, 69, 73, 19, 0, 73, 
					1, 0, 73, 14, 0, 69, 73, 20, 0, 73, 1, 0, 73, 15, 0, 69, 73, 21, 0, 73, 1, 0, 73, 16, 0, 69, 73, 22, 0, 73, 1, 0, 73, 17, 0, 69, 73, 23, 0, 73, 1, 0, 73, 18, 0, 69, 73, 24, 0, 73, 
					1, 0, 73, 19, 0, 69, 73, 25, 0, 73, 1, 0, 73, 20, 0, 69, 73, 26, 0, 73, 1, 0, 73, 21, 0, 69, 73, 27, 0, 73, 1, 0, 73, 22, 0, 69, 73, 28, 0, 73, 1, 0, 73, 23, 0, 69, 73, 29, 0, 73, 
					1, 0, 73, 24, 0, 69, 73, 30, 0, 73, 1, 0, 73, 25, 0, 69, 73, 31, 0, 73, 1, 0, 73, 26, 0, 69, 73, 32, 0, 73, 1, 0, 73, 27, 0, 69, 73, 33, 0, 73, 1, 0, 73, 28, 0, 69, 73, 34, 0, 73, 
					1, 0, 73, 29, 0, 69, 73, 35, 0, 73, 1, 0, 73, 30, 0, 69, 73, 36, 0, 73, 1, 0, 73, 31, 0, 69, 73, 37, 0, 73, 1, 0, 73, 32, 0, 69, 73, 38, 0, 73, 1, 0, 73, 33, 0, 69, 73, 39, 0, 73, 
					1, 0, 73, 34, 0, 69, 73, 40, 0, 73, 1, 0, 73, 35, 0, 69, 73, 41, 0, 73, 1, 0, 73, 36, 0, 69, 73, 42, 0, 73, 1, 0, 73, 37, 0, 69, 73, 43, 0, 73, 1, 0, 73, 38, 0, 69, 73, 44, 0, 73, 
					1, 0, 73, 39, 0, 69, 73, 45, 0, 73, 1, 0, 73, 40, 0, 69, 73, 46, 0, 73, 1, 0, 73, 41, 0, 69, 73, 47, 0, 73, 1, 0, 73, 42, 0, 69, 73, 48, 0, 73, 1, 0, 73, 43, 0, 69, 73, 49, 0, 73, 
					1, 0, 73, 44, 0, 69, 73, 50, 0, 73, 1, 0, 73, 45, 0, 69, 73, 51, 0, 73, 1, 0, 73, 46, 0, 69, 73, 52, 0, 73, 1, 0, 73, 47, 0, 69, 73, 53, 0, 73, 1, 0, 73, 48, 0, 69, 73, 54, 0, 73, 
					1, 0, 73, 49, 0, 69, 73, 55, 0, 73, 1, 0, 73, 50, 0, 69, 73, 56, 0, 73, 1, 0, 73, 51, 0, 69, 73, 57, 0, 73, 1, 0, 73, 52, 0, 69, 73, 58, 0, 73, 1, 0, 73, 53, 0, 69, 73, 59, 0, 73, 
					1, 0, 73, 54, 0, 69, 73, 60, 0, 73, 1, 0, 73, 55, 0, 69, 73, 61, 0, 73, 1, 0, 73, 56, 0, 69, 73, 62, 0, 73, 1, 0, 73, 57, 0, 69, 73, 63, 0, 73, 1, 0, 73, 58, 0, 69, 73, 64, 0, 73, 
					1, 0, 73, 59, 0, 69, 73, 65, 0, 73, 1, 0, 73, 60, 0, 69, 73, 66, 0, 73, 1, 0, 73, 61, 0, 69, 73, 67, 0, 73, 1, 0, 73, 62, 0, 69, 73, 68, 0, 73, 1, 0, 73, 63, 0, 69, 73, 69, 0, 73, 
					3, 0, 73, 66, 0, 69, 77, 3, 1, 98, 76, 73, 66, 0, 69, 73, 0, 0, 73, 2, 0, 73, 64, 0, 69, 73, 6, 0, 73, 2, 0, 73, 64, 0, 69, 73, 7, 0, 73, 2, 0, 73, 64, 0, 69, 73, 8, 0, 73, 2, 
					0, 73, 64, 0, 69, 73, 9, 0, 73, 2, 0, 73, 64, 0, 69, 73, 10, 0, 73, 2, 0, 73, 64, 0, 69, 73, 11, 0, 73, 2, 0, 73, 64, 0, 69, 73, 12, 0, 73, 2, 0, 73, 64, 0, 69, 73, 13, 0, 73, 2, 
					0, 73, 64, 0, 69, 73, 14, 0, 73, 2, 0, 73, 64, 0, 69, 73, 15, 0, 73, 2, 0, 73, 64, 0, 69, 73, 16, 0, 73, 2, 0, 73, 64, 0, 69, 73, 17, 0, 73, 2, 0, 73, 64, 0, 69, 73, 18, 0, 73, 2, 
					0, 73, 64, 0, 69, 73, 19, 0, 73, 2, 0, 73, 64, 0, 69, 73, 20, 0, 73, 2, 0, 73, 64, 0, 69, 73, 21, 0, 73, 2, 0, 73, 64, 0, 69, 73, 22, 0, 73, 2, 0, 73, 64, 0, 69, 73, 23, 0, 73, 2, 
					0, 73, 64, 0, 69, 73, 24, 0, 73, 2, 0, 73, 64, 0, 69, 73, 25, 0, 73, 2, 0, 73, 64, 0, 69, 73, 26, 0, 73, 2, 0, 73, 64, 0, 69, 73, 27, 0, 73, 2, 0, 73, 64, 0, 69, 73, 28, 0, 73, 2, 
					0, 73, 64, 0, 69, 73, 29, 0, 73, 2, 0, 73, 64, 0, 69, 73, 30, 0, 73, 2, 0, 73, 64, 0, 69, 73, 31, 0, 73, 2, 0, 73, 64, 0, 69, 73, 32, 0, 73, 2, 0, 73, 64, 0, 69, 73, 33, 0, 73, 2, 
					0, 73, 64, 0, 69, 73, 34, 0, 73, 2, 0, 73, 64, 0, 69, 73, 35, 0, 73, 2, 0, 73, 64, 0, 69, 73, 36, 0, 73, 2, 0, 73, 64, 0, 69, 73, 37, 0, 73, 2, 0, 73, 64, 0, 69, 73, 38, 0, 73, 2, 
					0, 73, 64, 0, 69, 73, 39, 0, 73, 2, 0, 73, 64, 0, 69, 73, 40, 0, 73, 2, 0, 73, 64, 0, 69, 73, 41, 0, 73, 2, 0, 73, 64, 0, 69, 73, 42, 0, 73, 2, 0, 73, 64, 0, 69, 73, 43, 0, 73, 2, 
					0, 73, 64, 0, 69, 73, 44, 0, 73, 2, 0, 73, 64, 0, 69, 73, 45, 0, 73, 2, 0, 73, 64, 0, 69, 73, 46, 0, 73, 2, 0, 73, 64, 0, 69, 73, 47, 0, 73, 2, 0, 73, 64, 0, 69, 73, 48, 0, 73, 2, 
					0, 73, 64, 0, 69, 73, 49, 0, 73, 2, 0, 73, 64, 0, 69, 73, 50, 0, 73, 2, 0, 73, 64, 0, 69, 73, 51, 0, 73, 2, 0, 73, 64, 0, 69, 73, 52, 0, 73, 2, 0, 73, 64, 0, 69, 73, 53, 0, 73, 2, 
					0, 73, 64, 0, 69, 73, 54, 0, 73, 2, 0, 73, 64, 0, 69, 73, 55, 0, 73, 2, 0, 73, 64, 0, 69, 73, 56, 0, 73, 2, 0, 73, 64, 0, 69, 73, 57, 0, 73, 2, 0, 73, 64, 0, 69, 73, 58, 0, 73, 2, 
					0, 73, 64, 0, 69, 73, 59, 0, 73, 2, 0, 73, 64, 0, 69, 73, 60, 0, 73, 2, 0, 73, 64, 0, 69, 73, 61, 0, 73, 2, 0, 73, 64, 0, 69, 73, 62, 0, 73, 2, 0, 73, 64, 0, 69, 73, 63, 0, 73, 2, 
					0, 73, 64, 0, 69, 73, 64, 0, 73, 2, 0, 73, 64, 0, 69, 73, 65, 0, 73, 2, 0, 73, 64, 0, 69, 73, 66, 0, 73, 2, 0, 73, 64, 0, 69, 73, 67, 0, 73, 2, 0, 73, 64, 0, 69, 73, 68, 0, 73, 2, 
					0, 73, 64, 0, 69
				};
			}
		}
	}
}
