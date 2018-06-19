// ---------------------------------------------------------------------------------------
// Shader Model 4.0 / 4.1
// ---------------------------------------------------------------------------------------

// CalculateLevelOfDetail (DirectX HLSL Texture Object) 
// http://msdn.microsoft.com/en-us/library/windows/desktop/bb944001%28v=vs.85%29.aspx

// Gather (DirectX HLSL Texture Object) 
// http://msdn.microsoft.com/en-us/library/windows/desktop/bb944003%28v=VS.85%29.aspx

// GetDimensions (DirectX HLSL Texture Object)
// http://msdn.microsoft.com/en-us/library/bb509693%28v=VS.85%29.aspx

// GetSamplePosition (DirectX HLSL Texture Object)
// http://msdn.microsoft.com/en-us/library/bb944004%28v=VS.85%29.aspx

// Load (DirectX HLSL Texture Object)
// http://msdn.microsoft.com/en-us/library/bb509694%28v=VS.85%29.aspx

// Sample (DirectX HLSL Texture Object)
// http://msdn.microsoft.com/en-us/library/bb509695%28v=VS.85%29.aspx

// SampleBias (DirectX HLSL Texture Object)
// http://msdn.microsoft.com/en-us/library/bb944005%28v=VS.85%29.aspx

// SampleCmp (DirectX HLSL Texture Object)
// http://msdn.microsoft.com/en-us/library/bb509696%28v=VS.85%29.aspx

// SampleGrad (DirectX HLSL Texture Object)
// http://msdn.microsoft.com/en-us/library/bb509698%28v=VS.85%29.aspx

// SampleLevel (DirectX HLSL Texture Object)
// http://msdn.microsoft.com/en-us/library/bb509699%28v=VS.85%29.aspx

void GroupMemoryBarrierWithGroupSync();

class __Texture1D<T> {
	// SM 4.0
	float CalculateLevelOfDetail( sampler_state s, float1 x);
	void GetDimensions( uint MipLevel, out uint Width, out uint NumberOfLevels);
	void GetDimensions( out uint Width);
	void GetDimensions( uint MipLevel, out float Width, out float NumberOfLevels);
	void GetDimensions( out float Width);
	T Load(int2 Location);
	T Load(int2 Location, int Offset);
	float4 Sample(sampler_state S, float Location);
	float4 Sample(sampler_state S, float Location, int Offset);
	float4 SampleBias(sampler_state S, float Location, float Bias);
	float4 SampleBias(sampler_state S, float Location, float Bias, int Offset);
	float SampleCmp(sampler_state S, float Location, float CompareValue);
	float SampleCmp(sampler_state S, float Location, float CompareValue, int Offset);
	float SampleCmpLevelZero(sampler_state S, float Location, float CompareValue);
	float SampleCmpLevelZero(sampler_state S, float Location, float CompareValue, int Offset);
	float4 SampleGrad(sampler_state S, float Location, float DDX, float DDY);
	float4 SampleGrad(sampler_state S, float Location, float DDX, float DDY, int Offset);
	float4 SampleLevel( sampler_state S, float Location, float LOD);
	float4 SampleLevel( sampler_state S, float Location, float LOD, int Offset);

	// SM 5.0
	T mips.operator[][](in uint mipSlice,in  uint pos);	
	
	T operator[](in  uint pos);
};

class __Texture1DArray<T> {
	// SM 4.0
	float CalculateLevelOfDetail( sampler_state s, float1 x);
	void GetDimensions( uint MipLevel, out uint Width, out uint Elements, out uint NumberOfLevels);
	void GetDimensions( out uint Width, out uint Elements);
	void GetDimensions( uint MipLevel, out float Width, out float Elements, out float NumberOfLevels);
	void GetDimensions( out float Width, out float Elements);
	T Load(int3 Location);
	T Load(int3 Location, int Offset);
	float4 Sample(sampler_state S, float2 Location);
	float4 Sample(sampler_state S, float2 Location, int Offset);
	float4 SampleBias(sampler_state S, float2 Location, float Bias);
	float4 SampleBias(sampler_state S, float2 Location, float Bias, int Offset);
	float SampleCmp(sampler_state S, float2 Location, float CompareValue);
	float SampleCmp(sampler_state S, float2 Location, float CompareValue, int Offset);
	float SampleCmpLevelZero(sampler_state S, float2 Location, float CompareValue);
	float SampleCmpLevelZero(sampler_state S, float2 Location, float CompareValue, int Offset);
	float4 SampleGrad(sampler_state S, float2 Location, float DDX, float DDY);
	float4 SampleGrad(sampler_state S, float2 Location, float DDX, float DDY, int Offset);
	float4 SampleLevel( sampler_state S, float2 Location, float LOD);
	float4 SampleLevel( sampler_state S, float2 Location, float LOD, int Offset);

	// SM 5.0
	T mips.operator[][](in  uint mipSlice,in  uint2 pos);	

    T operator[](in  uint2 pos);
};

class __Texture2D<T> {
	// SM 4.0
	float CalculateLevelOfDetail( sampler_state s, float2 x);
	vector<__T_base,4> Gather( sampler_state S, float2 Location);
	vector<__T_base,4> Gather( sampler_state S, float2 Location, int2 Offset );
	void GetDimensions( uint MipLevel, out uint Width, out uint Height, out uint NumberOfLevels);
	void GetDimensions( out uint Width, out uint Height);
	void GetDimensions( uint MipLevel, out float Width, out float Height, out float NumberOfLevels);
	void GetDimensions( out float Width, out float Height);
	T Load(int3 Location);
	T Load(int3 Location, int2 Offset);
	float4 Sample(sampler_state S, float2 Location);
	float4 Sample(sampler_state S, float2 Location, int2 Offset);
	float4 SampleBias(sampler_state S, float2 Location, float Bias);
	float4 SampleBias(sampler_state S, float2 Location, float Bias, int2 Offset);
	float SampleCmp(sampler_state S, float2 Location, float CompareValue);
	float SampleCmp(sampler_state S, float2 Location, float CompareValue, int2 Offset);
	float SampleCmpLevelZero(sampler_state S, float2 Location, float CompareValue);
	float SampleCmpLevelZero(sampler_state S, float2 Location, float CompareValue, int2 Offset);
	float4 SampleGrad(sampler_state S, float2 Location, float2 DDX, float2 DDY);
	float4 SampleGrad(sampler_state S, float2 Location, float2 DDX, float2 DDY, int2 Offset);
	float4 SampleLevel( sampler_state S, float2 Location, float LOD);
	float4 SampleLevel( sampler_state S, float2 Location, float LOD, int2 Offset);
	
	// SM 5.0
	T Gather(
	  in  sampler s,
	  in  float2 location,
	  in  int2 offset
	);	
	
	T GatherRed(
		in  sampler s,
		in  float2 location
		);

	T GatherGreen(
		in  sampler s,
		in  float2 location
		);

	T GatherBlue(
		in  sampler s,
		in  float2 location
		);

	T GatherRed(
	  in  sampler s,
	  in  float2 location,
	  in  int2 offset
	);

	T GatherGreen(
	  in  sampler s,
	  in  float2 location,
	  in  int2 offset
	);
	
	T GatherBlue(
	  in  sampler s,
	  in  float2 location,
	  in  int2 offset
	);	

	T GatherAlpha(
	  in  sampler s,
	  in  float2 location,
	  in  int2 offset
	);

	T GatherRed(
	  in  sampler s,
	  in  float2 location,
	  in  int2 offset1,
	  in  int2 offset2,
	  in  int2 offset3,
	  in  int2 offset4
	);

	T GatherGreen(
	  in  sampler s,
	  in  float2 location,
	  in  int2 offset1,
	  in  int2 offset2,
	  in  int2 offset3,
	  in  int2 offset4
	);
	
	T GatherBlue(
	  in  sampler s,
	  in  float2 location,
	  in  int2 offset1,
	  in  int2 offset2,
	  in  int2 offset3,
	  in  int2 offset4
	);	

	T GatherAlpha(
	  in  sampler s,
	  in  float2 location,
	  in  int2 offset1,
	  in  int2 offset2,
	  in  int2 offset3,
	  in  int2 offset4
	);

	float4 GatherCmp(
	  in  SamplerComparisonState s,
	  in  float2 location,
	  in  float compare_value,
	  in  int2 offset
	);	
	
	float4 GatherCmpRed(
	  in  SamplerComparisonState s,
	  in  float2 location,
	  in  float compare_value,
	  in  int2 offset
	);	
	
	float4 GatherCmpGreen(
	  in  SamplerComparisonState s,
	  in  float2 location,
	  in  float compare_value,
	  in  int2 offset
	);	
	
	float4 GatherCmpBlue(
	  in  SamplerComparisonState s,
	  in  float2 location,
	  in  float compare_value,
	  in  int2 offset
	);
	
	float4 GatherCmpAlpha(
	  in  SamplerComparisonState s,
	  in  float2 location,
	  in  float compare_value,
	  in  int2 offset
	);
	
	T mips.operator[][](in uint mipSlice, in  uint2 pos);
		
	T operator[](in  uint2 pos);
};

class __Texture2DArray<T> {
	// SM 4.0
	float CalculateLevelOfDetail( sampler_state s, float2 x);
	vector<__T_base,4> Gather( sampler_state S, float3 Location, int2 Offset );
	void GetDimensions( uint MipLevel, out uint Width, out uint Height, out uint Elements, out uint NumberOfLevels);
	void GetDimensions( out uint Width, out uint Height, out uint Elements);
	void GetDimensions( uint MipLevel, out float Width, out float Height, out float Elements, out float NumberOfLevels);
	void GetDimensions( out float Width, out float Height, out float Elements);
	T Load(int4 Location);
	T Load(int4 Location, int2 Offset);
	T Load(int4 Location, int3 Offset);
	float4 Sample(sampler_state S, float3 Location);
	float4 Sample(sampler_state S, float3 Location, int2 Offset);
	float4 SampleBias(sampler_state S, float3 Location, float Bias);
	float4 SampleBias(sampler_state S, float3 Location, float Bias, int2 Offset);
	float SampleCmp(sampler_state S, float3 Location, float CompareValue);
	float SampleCmp(sampler_state S, float3 Location, float CompareValue, int2 Offset);
	float SampleCmpLevelZero(sampler_state S, float3 Location, float CompareValue);
	float SampleCmpLevelZero(sampler_state S, float3 Location, float CompareValue, int2 Offset);
	float4 SampleGrad(sampler_state S, float3 Location, float2 DDX, float2 DDY);
	float4 SampleGrad(sampler_state S, float3 Location, float2 DDX, float2 DDY, int2 Offset);
	float4 SampleLevel( sampler_state S, float3 Location, float LOD);
	float4 SampleLevel( sampler_state S, float3 Location, float LOD, int2 Offset);

		// SM 5.0
	T Gather(
	  in  sampler s,
	  in  float3 location,
	  in  int2 offset
	);	
	
	T GatherRed(
	  in  sampler s,
	  in  float3 location,
	  in  int2 offset
	);

	T GatherGreen(
	  in  sampler s,
	  in  float3 location,
	  in  int2 offset
	);
	
	T GatherBlue(
	  in  sampler s,
	  in  float3 location,
	  in  int2 offset
	);	

	T GatherAlpha(
	  in  sampler s,
	  in  float3 location,
	  in  int2 offset
	);

	float4 GatherCmp(
	  in  SamplerComparisonState s,
	  in  float3 location,
	  in  float compare_value,
	  in  int2 offset
	);	
	
	float4 GatherCmpRed(
	  in  SamplerComparisonState s,
	  in  float3 location,
	  in  float compare_value,
	  in  int2 offset
	);	
	
	float4 GatherCmpGreen(
	  in  SamplerComparisonState s,
	  in  float3 location,
	  in  float compare_value,
	  in  int2 offset
	);	
	
	float4 GatherCmpBlue(
	  in  SamplerComparisonState s,
	  in  float3 location,
	  in  float compare_value,
	  in  int2 offset
	);
	
	float4 GatherCmpAlpha(
	  in  SamplerComparisonState s,
	  in  float3 location,
	  in  float compare_value,
	  in  int2 offset
	);
	
	T mips.operator[][](in  uint mipSlice, in  uint3 pos);
		
	T operator[](in  uint3 pos);
};


class __Texture3D<T> {
	// SM 4.0
	float CalculateLevelOfDetail( sampler_state s, float3 x);
	void GetDimensions( uint MipLevel, out uint Width, out uint Height, out uint Depth, out uint NumberOfLevels);
	void GetDimensions( out uint Width, out uint Height, out uint Depth);
	void GetDimensions( uint MipLevel, out float Width, out float Height, out float Depth, out float NumberOfLevels);
	void GetDimensions( out float Width, out float Height, out float Depth);
	T Load(int4 Location);
	T Load(int4 Location, int3 Offset);
	float4 Sample(sampler_state S, float3 Location);
	float4 Sample(sampler_state S, float3 Location, int3 Offset);
	float4 SampleBias(sampler_state S, float3 Location, float Bias);
	float4 SampleBias(sampler_state S, float3 Location, float Bias, int3 Offset);
	float SampleCmp(sampler_state S, float3 Location, float CompareValue);
	float SampleCmp(sampler_state S, float3 Location, float CompareValue, int3 Offset);
	float4 SampleGrad(sampler_state S, float3 Location, float3 DDX, float3 DDY);
	float4 SampleGrad(sampler_state S, float3 Location, float3 DDX, float3 DDY, int3 Offset);
	float4 SampleLevel( sampler_state S, float3 Location, float LOD);
	float4 SampleLevel( sampler_state S, float3 Location, float LOD, int3 Offset);
	
	// SM 5.0
	T mips.operator[][](in uint mipSlice,in  uint3 pos);
		
	T operator[](in  uint3 pos);
};

class __TextureCube<T> {
	// SM 4.0
	float CalculateLevelOfDetail( sampler_state s, float3 x);
	vector<__T_base,4> Gather( sampler_state S, float3 Location);
	void GetDimensions( uint MipLevel, out uint Width, out uint Height, out uint NumberOfLevels);
	void GetDimensions( out uint Width, out uint Height);
	void GetDimensions( uint MipLevel, out float Width, out float Height, out uint NumberOfLevels);
	void GetDimensions( out float Width, out float Height);
	float4 Sample(sampler_state S, float3 Location);
	float4 SampleBias(sampler_state S, float3 Location, float Bias);
	float SampleCmp(sampler_state S, float3 Location, float CompareValue);
	float SampleCmpLevelZero(sampler_state S, float3 Location, float CompareValue);
	float4 SampleGrad(sampler_state S, float3 Location, float3 DDX, float3 DDY);
	float4 SampleLevel( sampler_state S, float3 Location, float LOD);
};

class __TextureCubeArray<T> {
	// SM 4.0
	float CalculateLevelOfDetail( sampler_state s, float3 x);
	vector<__T_base,4> Gather( sampler_state S, float4 Location);
	void GetDimensions( uint MipLevel, out uint Width, out uint Height, out uint Elements, out uint NumberOfLevels);
	void GetDimensions( out uint Width, out uint Height, out uint Elements);
	void GetDimensions( uint MipLevel, out float Width, out float Height, out float Elements, out float NumberOfLevels);
	void GetDimensions( out float Width, out float Height, out float Elements);
	float4 Sample(sampler_state S, float4 Location);
	float4 SampleBias(sampler_state S, float4 Location, float Bias);
	float SampleCmp(sampler_state S, float4 Location, float CompareValue);
	float SampleCmpLevelZero(sampler_state S, float4 Location, float CompareValue);
	float4 SampleGrad(sampler_state S, float4 Location, float3 DDX, float3 DDY);
	float4 SampleLevel( sampler_state S, float4 Location, float LOD);
};

class __Texture2DMS<T> {
	// SM 4.0
	void GetDimensions( out uint Width, out uint Height, out uint Samples);
	void GetDimensions( out float Width, out float Height, out float Samples);
	float2 GetSamplePosition(int s);
	T Load(int2 Location);
	T Load(int2 Location, int2 Offset);
	T Load(int2 Location, int2 Offset, int SampleIndex);
	
	
	// SM 5.0
	float2 GetSamplePosition(
	  in  int sampleindex
	);	
	
	T Load(
	  in  int2 coord,
	  in  int sampleindex
	);	
	
	T sample.operator[][]( in  uint sampleSlice, in  uint3 pos);	
};

class __Texture2DMSArray<T> {
	// SM 4.0
	void GetDimensions( out uint Width, out uint Height, out uint Elements, out uint Samples);
	void GetDimensions( out float Width, out float Height, out float Elements, out float Samples);
	float2 GetSamplePosition(int s);
	T Load(int3 Location); 
	T Load(int3 Location, int2 Offset); 
	T Load(int3 Location, int2 Offset, int SampleIndex); 

	// SM 5.0
	float2 GetSamplePosition(
	  in  int sampleindex
	);	

	T Load(
	  in  int3 coord,
	  in  int sampleindex
	);	

	T sample.operator[][]( in  uint sampleSlice, in  uint3 pos);
};

class __Buffer<T> {
	// SM 4.0
	T Load(int Location);

	void GetDimensions(out  uint dim);	
	
	T operator[](in  uint pos);
};

// Stream-Output Object (DirectX HLSL)
// http://msdn.microsoft.com/en-us/library/bb509661%28v=VS.85%29.aspx
// StreamOutputObject <T>   Name
// StreamOutputObject: PointStream, LineStream, TriangleStream
class __PointStream<T> {
	void Append(T StreamDataType);
	void RestartStrip();
};

class __LineStream<T> {
	void Append(T StreamDataType);
	void RestartStrip();
};

class __TriangleStream<T> {
	void Append(T StreamDataType);
	void RestartStrip();
};

// ---------------------------------------------------------------------------------------
// Shader Model 5.0 
// ---------------------------------------------------------------------------------------

// AppendStructuredBuffer<T>
// http://msdn.microsoft.com/en-us/library/ff471448%28v=VS.85%29.aspx
class __AppendStructuredBuffer<T> {
	void Append(T value);
	void GetDimensions(out uint numStructs, out uint stride);
};

// ByteAddressBuffer
// http://msdn.microsoft.com/en-us/library/ff471453%28v=VS.85%29.aspx
class __ByteAddressBuffer {
	void GetDimensions(out  uint dim);
	uint Load(in  uint address);
	uint2 Load2(in  uint address);
	uint3 Load3(in  uint address);
	uint4 Load4(in  uint address);
};

// ConsumeStructuredBuffer<T>
// http://msdn.microsoft.com/en-us/library/ff471459%28v=VS.85%29.aspx
class __ConsumeStructuredBuffer<T> {
	T Consume(void);
	void GetDimensions(out  uint numStructs, out  uint stride);
};

// InputPatch<T,N>
// http://msdn.microsoft.com/en-us/library/ff471462%28v=VS.85%29.aspx
class __InputPatch<T,N> {
	uint Length;
	T operator[](in uint n);
};

// OutputPatch<T,N>
// http://msdn.microsoft.com/en-us/library/ff471464%28v=VS.85%29.aspx
class __OutputPatch<T,N> {
	uint Length;
	T operator[](in uint n);
};

// RWBuffer<T>
// http://msdn.microsoft.com/en-us/library/ff471472%28v=VS.85%29.aspx
class __RWBuffer<T> {
	void GetDimensions(out  uint dim);
	T operator[](in uint pos);
};

// RWByteAddressBuffer
// http://msdn.microsoft.com/en-us/library/ff471475%28v=VS.85%29.aspx
class __RWByteAddressBuffer {
	void GetDimensions(out  uint dim);
	void InterlockedAdd(in   uint dest, in   uint value, out  uint original_value);
	void InterlockedAnd(
		in   uint dest,
		in   uint value,
		out  uint original_value
	);
	void InterlockedCompareExchange(
		in   uint dest,
		in   uint compare_value,
		in   uint value,
		out  uint original_value
	);
	void InterlockedCompareStore(
	  in  uint dest,
	  in  uint compare_value,
	  in  uint value
	);
	void InterlockedExchange(
	  in   uint dest,
	  in   uint value,
	  out  uint original_value
	);
	void InterlockedMax(
	  in   uint dest,
	  in   uint value,
	  out  uint original_value
	);	
	void InterlockedMin(
	  in   uint dest,
	  in   uint value,
	  out  uint original_value
	);	
	void InterlockedOr(
	  in   uint dest,
	  in   uint value,
	  out  uint original_value
	);	
	void InterlockedXor(
	  in   uint dest,
	  in   uint value,
	  out  uint original_value
	);	
	uint Load(
	  in  uint address
	);	
	uint2 Load2(
	  in  uint address
	);	
	uint3 Load3(
	  in  uint address
	);	
	uint4 Load4(
	  in  uint address
	);	
	void Store(
	  in  uint address,
	  in  uint value
	);	
	void Store2(
	  in  uint address,
	  in  uint2 values
	);	
	void Store3(
	  in  uint address,
	  in  uint3 values
	);	
	void Store4(
	  in  uint address,
	  in  uint4 values
	);	
};

// RWStructuredBuffer<T>
// http://msdn.microsoft.com/en-us/library/ff471494%28v=VS.85%29.aspx
class __RWStructuredBuffer<T> {

	uint DecrementCounter(void);

	void GetDimensions(
	  out  uint numStructs,
	  out  uint stride
	);

	uint IncrementCounter(void);

	T operator[](in uint pos);
};

// RWTexture1D<T>
// http://msdn.microsoft.com/en-us/library/ff471499%28v=VS.85%29.aspx
class __RWTexture1D<T> {
	void GetDimensions(
	  out  uint Width
	);
	T operator[](in  uint pos);
};

// RWTexture1DArray<T>
// http://msdn.microsoft.com/en-us/library/ff471500%28v=VS.85%29.aspx
class __RWTexture1DArray<T> {
	void GetDimensions(
	  out  uint Width,
	  out  uint Elements
	);

	T operator[](in  uint2 pos);
};

// RWTexture2D<T>
// http://msdn.microsoft.com/en-us/library/ff471505%28v=VS.85%29.aspx
class __RWTexture2D<T> {
	void GetDimensions(
	  out  uint Width,
	  out  uint Height
	);

      T operator[](in  uint2 pos);
};

// RWTexture2DArray<T>
// http://msdn.microsoft.com/en-us/library/ff471506%28v=VS.85%29.aspx
class __RWTexture2DArray<T> {
	void GetDimensions(
	  out  uint Width,
	  out  uint Height,
	  out  uint Elements
	);
	T operator[](in  uint3 pos);
};

// RWTexture3D<T>
// http://msdn.microsoft.com/en-us/library/ff471511%28v=VS.85%29.aspx
class __RWTexture3D<T> {
	void GetDimensions(
	  out  uint Width,
	  out  uint Height,
	  out  uint Depth
	);

	T operator[](in  uint3 pos);
};

// StructuredBuffer<T>
// http://msdn.microsoft.com/en-us/library/ff471514%28v=VS.85%29.aspx
class __StructuredBuffer<T> {
	void GetDimensions(
	  out  uint numStructs,
	  out  uint stride
	);

	T operator[](in  uint pos);	
};