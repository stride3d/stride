// asp3.0

// この行は3dsMAXが正しいパーサーで読み込むために使われるおまじない。
//string ParamID = "0x003";

// 変換行列
// ・このパラメータはビューポートのカメラ位置とモデルの配置が反映します。
float4x4 g_mtxWorld			: WORLD;
float4x4 g_mtxView			: VIEW;
float4x4 g_mtxProjection	: PROJECTION;
float4x4 g_mtxWorldViewProj : WORLDVIEWPROJ;
float4x4 g_mtxWorldView		: WORLDVIEW;
float4x4 g_imtxView			: VIEWI;

// ライトパラメータ
// ・このパラメータはシーン内にあるライトの情報が反映します。
// ・パラメータはView座標系で指定されます。
float3 g_vtLightDir : Direction
<  
	string UIName = "* PARALLEL LIGHT"; 
	string Object = "TargetLight";
	int refID = 0;
> = {-0.577, -0.577, 0.577};

float4 g_colorLight : LIGHTCOLOR
<
	int LightRef = 0;
> = float4(0.0f, 0.0f, 0.0f, 0.0f);

// ライティングの有効/無効
bool g_enablesLighting
<
	string UIName = "* LIGHTING";
> = true;

// 拡散反射カラーのソース選択
int g_diffuseColorSelect
<
	string UIName = "* DIFFUSE COLOR (0:Mate/1:Vert/2:Mate*Vert)";
	string UIType = "Spinner";
	int UIMin = 0;
	int UIMax = 2;
	int UIStep = 1;
> = 0;

// 拡散反射の固定カラー
float4 g_diffuseColor
<
	string UIName = "  + material color";
> = float4( 0.80f, 0.80f, 0.80f, 1.0f );

// 自己発光
float3 g_emissionColor
<
	string UIName = "  + emission color";
> = float3( 0.00f, 0.00f, 0.00f);

// ライティングにおける拡散反射輝度のスケール
float g_diffuseAttenuation
<
	string UIName = "  + luminance attenuation";
	float UIMin = 0.0f;
	float UIMax = 1.0f;
	float UIStep = 0.01f;
> = 1.0f;

// スペキュラーの設定
bool g_enablesSpecular
<
	string UIName = "* SPECULAR";
> = false;

int g_specularColorSelector
<
	string UIName = "  + color (0:Mate/1:Vert/2:Mate*Vert/3:Fragment)";
	string UIType = "Spinner";
	int UIMin = 0;
	int UIMax = 3;
	int UIStep = 1;
> = 0;

float3 g_colorSpecular
<
	string UIName = "  |  + specular color";
> = float3( 0.80f, 0.80f, 0.80f );

float g_SpecularPower
<
	string UIName = "  + specular power";
	string UIType = "FloatSpinner";
	float UIMin = 0.01f;
	float UIMax = 256.0f;
> = 32.0f;

// リムスペキュラー
bool g_enablesRimSpecular
<
	string UIName = "* RIM-SPECULAR";
> = false;

float3 g_colorRimSpecular
<
	string UIName = "  + specular color";
> = float3( 0.80f, 0.80f, 0.80f );

float g_rimSpecularPower
<
	string UIName = "  + specular power";
	string UIType = "FloatSpinner";
	float UIMin = 0.0f;
	float UIMax = 256.0f;
> = 5.0f;

float g_rimSpecularSelfLuminous
<
	string UIName = "  + self luminous";
	string UIType = "FloatSpinner";
	float UIMin = 0.0f;
	float UIMax = 2.0f;
> = 1.0f;


// ベースカラーテクスチャ
// ・ビットマップ
// ・マップチャンネル
bool g_enablesBaseColorTex
<
	string UIName = "* BASE COLOR TEXTURE";
> = false;


texture g_texBaseColor : DiffuseMap
<
	string UIName = "  + texture";
	int Texcoord = 0;
	int MapChannel = 1;
>;

sampler2D g_texsmpBaseColor = sampler_state
{
	Texture = <g_texBaseColor>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = WRAP;
	AddressV = WRAP;
};

int g_blendBaseColorTexture
<
	string UIName = "  + Blend (0:ModRGBA/1:ModRGB/2:AddRGB/...)";
	string UIType = "Spinner";
	int UIMin = 0;
	int UIMax = 6;
	int UIStep = 1;
> = 0;

// 補助カラーテクスチャ
// ・ビットマップ
// ・マップチャンネル
bool g_enablesUtilColorTex
<
	string UIName = "* UTIL COLOR TEXTURE";
> = false;

texture g_texUtilColor : DiffuseMap
<
	string UIName = "  + texture";
	int Texcoord = 1;
	int MapChannel = 1;
>;

sampler2D g_texsmpUtilColor = sampler_state
{
	Texture = <g_texUtilColor>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = WRAP;
	AddressV = WRAP;
};

int g_blendUtilColorTexture
<
	string UIName = "  + Blend (0:ModRGBA/1:ModRGB/2:AddRGB)";
	string UIType = "Spinner";
	int UIMin = 0;
	int UIMax = 8;
	int UIStep = 1;
> = 0;

// グロスマップ
bool g_enablesGlossMap
<
	string UIName = "* GLOSS MAP";
> = false;

int g_indexGlossSrc
<
	string UIName = "  + source (0:BaseTex/1:UtilTex/2:Fragment)";
	int UIMin = 0;
	int UIMax = 2;
	int UIStep = 1;
> = 0;

// 法線マップテクスチャ
// ・ビットマップ
// ・マップチャンネル
// ・基底ベクトル
bool g_enablesNormalMap
<
	string UIName = "* NORMAL MAP";
> = false;

texture g_texNormalMap : NormalMap
<
	string UIName = "  + texture";
	int Texcoord = 2;
	int MapChannel = 1;
>;

sampler2D g_texsmpNormalMap = sampler_state
{
	Texture = <g_texNormalMap>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = WRAP;
	AddressV = WRAP;
};

// 法線マップの基底ベクトルの選択(ローカル座標系/接線座標系)
int g_normalMapType
<
	string UIName = "  + Type (0:ObjLocal/1:Tangent)";
	string UIType = "Spinner";
	int UIMin = 0;
	int UIMax = 1;
	int UIStep = 1;
> = 0;

// ローカル座標系での法線マップのRGBの基底ベクトル。
int g_normalMapAxisR
<
	string UIName = "  |  + base axis of R";
	string UIWidget = "Slider";
	int UIMin = 0;
	int UIMax = 5;
	int UIStep = 1;
> = 0;

int g_normalMapAxisG
<
	string UIName = "  |  + base axis of G";
	string UIWidget = "Slider";
	int UIMin = 0;
	int UIMax = 5;
	int UIStep = 1;
> = 2;

int g_normalMapAxisB
<
	string UIName = "  |  + base axis of B";
	string UIWidget = "Slider";
	int UIMin = 0;
	int UIMax = 5;
	int UIStep = 1;
> = 4;

const float3 g_vtBaseAxis[6] =
{
	{  1, 0, 0, },		// +X
	{ -1, 0, 0, },		// -X
	{ 0,  1, 0, },		// +Y
	{ 0, -1, 0, },		// -Y
	{ 0, 0,  1, },		// +Z
	{ 0, 0, -1, },		// -Z
};

// 接線座標系での法線マップの、RとGを交換。
bool g_swapTangentAndBinormal
<
	string UIName = "  |  + swap Tangent and Binormal.";
> = false;

// 接線座標系のTangent基底ベクトルの向きを反転する。
bool g_flipTangent
<
	string UIName = "  |  + flip Tangent";
> = false;

// 接線座標系のBinormal基底ベクトルの向きを反転する。
bool g_flipBinormal
<
	string UIName = "  |  + flip Binormal";
> = false;

// 法線マップのデバッグ
bool g_debugShowNormalDir
<
	string UIName = "  + output normal direction to color (debug)";
> = false;


// 環境マップテクスチャ
// ・ビットマップ
// ・調整カラー
// ・フレネル項
bool g_enablesEnvironmentMap
<
	string UIName = "* ENVIRONMENT MAP";
> = false;

texture g_texCubeEnvironmentMap
<
	string UIName = "  + cube map texture";
	string Type = "Cube";
>;

samplerCUBE	g_texsmpCubeEnvironmentMap = sampler_state
{
	Texture = <g_texCubeEnvironmentMap>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = WRAP;
	AddressV = WRAP;
};

texture g_texSphereEnvironmentMap
<
	string UIName = "  + sphere map texture";
>;

sampler2D	g_texsmpSphericalEnvironmentMap = sampler_state
{
	Texture = <g_texSphereEnvironmentMap>;
	MinFilter = Linear;
	MagFilter = Linear;
	MipFilter = Linear;
	AddressU = WRAP;
	AddressV = WRAP;
};

float4 g_colorEnvironmentMapModulate
<
	string UIName = "  + adjust color";
> = float4( 1.00f, 1.00f, 1.00f, 1.0f );

int g_environmentMapType
<
	string UIName = "  + type (0:Cube/1:Spherical)";
	int UIMin = 0;
	int UIMax = 1;
	int UIStep = 1;
> = 0;

bool g_enablesFresnelTerm
<
	string UIName = "  + use fresnel";
> = false;

float g_fresnelR0
<
	string UIName = "    + front face coefficient";
	float UIMin = 0.0f;
	float UIMax = 1.0f;
	float UIStep = 0.01f;
> = 0.2f;

float g_fresnelR1
<
	string UIName = "    + side face coefficient";
	float UIMin = 0.0f;
	float UIMax = 10.0f;
	float UIStep = 0.01f;
> = 0.8f;

// パララックスマップ
bool g_enablesParallaxMap
<
	string UIName = "* PARALLAX MAP";
> = false;

int g_parallaxMapSource
<
	string UIName = "  + source (0:BaseTex/1:UtilTex/2:NormalTex)";
	int UIMin = 0;
	int UIMax = 2;
	int UIStep = 1;
> = 0;

float g_parallaxMapStride
<
	string UIName = "  + stride";
> = 0.03f;


// 輪郭のカラー
float4 g_edgeColor
<
	string UIName = "* Edge color";
> = float4( 0.0f, 0.0f, 0.0f, 1.0f );

// 輪郭の幅
float g_edgeSize
<
	string UIName = "  + edge size(1/1000)";
> = 4.0f;

// フォグの影響
bool g_enablesFog
<
	string UIName = "* FOG";
> = true;

// 環境光(環境グローバル)
float3 g_ambientLightColor
<
	string UIName = "* AMBIENT LIGHT COLOR";
> = float3(0.3, 0.3, 0.3);

// フォグ(環境グローバル)
// ・種類
// ・フォグカラー
// ・リニアフォグの開始/終了距離
// ・指数フォグの濃度係数
int g_fogDensityType
<
	string UIName = "* FOG DENSITY(0:non/ 1:linear/ 2:exp/ 3:exp2)";
	int UIMin = 0;
	int UIMax = 3;
	int UIStep = 1;
> = 0;

float4 g_fogColor
<
	string UIName = "  + color";
> = float4( 0.80f, 0.80f, 0.80f, 1.0f );

float g_fogStart
<
	string UIName = "  + start distance(only linear fog)";
	int UIMin = 0.0;
	int UIMax = 1000000.0;
	float UIStep = 1.0;
> = 1.0;

float g_fogEnd
<
	string UIName = "  + end distance(only linear fog)";
	int UIMin = 0.0;
	int UIMax = 1000000.0;
	float UIStep = 1.0;
> = 100.0;

float g_fogDensity
<
	string UIName = "  + density(only exp and exp2 fog)";
	int UIMin = 0.0;
	int UIMax = 100.0;
	float UIStep = 0.01;
> = 1.0;

//----------------------------------------------------------------------------

// 頂点カラーを頂点シェーダに入力したい場合には、TEXCOORDに割り当てる。
int texcoord3 : Texcoord
<
	int Texcoord = 3;		// 頂点カラーを受け取るTEXCOORD番号
	int MapChannel = 0;		// 頂点カラーを指すMAXでのマップチャンネル番号
>;

struct VSOutput
{
	float4 vtPosition			: POSITION;
   	float3 vtViewDirection		: TEXCOORD0;
   	float2 uv0					: TEXCOORD1;
   	float2 uv1					: TEXCOORD2;
   	float2 uv2					: TEXCOORD3;
   	
   	float3 Nv					: TEXCOORD4;
   	float3 Ns					: TEXCOORD5;
   	float3 Nt					: TEXCOORD6;

	float4 color				: TEXCOORD7;
	
	float fogDistance			: FOG;
};

struct PSInput
{
   	float3 vtViewDirection		: TEXCOORD0;
   	float2 uv0					: TEXCOORD1;
   	float2 uv1					: TEXCOORD2;
   	float2 uv2					: TEXCOORD3;

   	float3 Nv					: TEXCOORD4;
   	float3 Ns					: TEXCOORD5;
   	float3 Nt					: TEXCOORD6;

	float4 color				: TEXCOORD7;

	float fogDistance			: FOG;
};

VSOutput VS(
	float3 iPosition	: POSITION,
	float3 iNormal		: NORMAL,
	float2 iTexCoord0	: TEXCOORD0,
	float2 iTexCoord1	: TEXCOORD1,
	float2 iTexCoord2	: TEXCOORD2,

	float3 iColor		: TEXCOORD3,

	float3 iTangent		: TANGENT,
	float3 iBinormal	: BINORMAL)
{
	VSOutput o;

	// ラスタライズの為に頂点の同時座標を出力。
	o.vtPosition = mul(float4(iPosition,1), g_mtxWorldViewProj);

	// View座標系でのピクセルから視点へのベクトルを計算。
	// PerPixelLightingにおいて参照される。
	if(g_mtxProjection[3][3] == 0.0f)
	{
		// 透視変換の場合。普通。
		o.vtViewDirection = -1.0f * mul(float4(iPosition,1), g_mtxWorldView).xyz;
	}
	else
	{
		// 正射影行列の場合、ピクセルの位置にかかわらず視線方向は平行になる。
		o.vtViewDirection = float3(0,0,-1);
	}

	if(g_enablesNormalMap || g_parallaxMapStride)
	{
		float3 tmpNs;
		float3 tmpNt;
		float3 tmpNv;
		if(g_enablesNormalMap && (g_normalMapType == 0))
		{
			// モデルのローカル座標系での法線マップ
			// 法線マップの基底ベクトル用に、モデル座標系の基底ベクトルを
			// ビュー座標系へ変換して出力。
			tmpNs = g_vtBaseAxis[g_normalMapAxisR];
			tmpNt = g_vtBaseAxis[g_normalMapAxisG];
			tmpNv = cross(tmpNs, tmpNt);
			if(dot(g_vtBaseAxis[g_normalMapAxisB], tmpNv) < 0)
			{
				tmpNv *= -1.0f;
			}
		}
		else
		{
			// テクスチャの接線空間での法線マップ
			// MAXのプレビューで使用されるTangentとBinormalは逆になっている！？
			// r -> binormal
			// g -> tangent
			tmpNs = g_swapTangentAndBinormal? iTangent: iBinormal;
			tmpNt = g_swapTangentAndBinormal? iBinormal: iTangent;
			tmpNv = cross(iTangent, iBinormal);
			if(dot(iNormal, tmpNv) < 0)
			{
				tmpNv *= -1.0f;
			}
			tmpNs *= g_flipTangent? -1.0f: 1.0f;
			tmpNt *= g_flipBinormal? -1.0f: 1.0f;
		}
		o.Ns = mul(tmpNs, (float3x3)g_mtxWorldView);
		o.Nt = mul(tmpNt, (float3x3)g_mtxWorldView);
		o.Nv = mul(tmpNv, (float3x3)g_mtxWorldView);
	}
	else
	{
		// View座標系での法線ベクトルを出力。
		o.Nv = mul(iNormal, (float3x3)g_mtxWorldView);
		// 法線マップ用の基底ベクトルは出力しない。
		o.Ns = float3(0, 0, 0);
		o.Nt = float3(0, 0, 0);
	}

	// テクスチャのUVアドレスをそのまま出力。
	// (MAXの)MapChannelとTexCoordとの割当は、MAXのDirectX9Shaderが
	// マッピングを処理する。
	o.uv0 = iTexCoord0;
	o.uv1 = iTexCoord1;
	o.uv2 = iTexCoord2;

	// カラーをそのまま出力
	o.color = float4(iColor, 1);

	// フォグの距離を出力
	// 線形フォグでは、開始距離から終了距離の間での正規化を済ませておく。
	if(g_fogDensityType == 1)
	{
		o.fogDistance = (g_fogEnd - o.vtPosition.w) / (g_fogEnd - g_fogStart);
	}
	else
	{
		o.fogDistance = o.vtPosition.w;
	}

	return o;
}

float4 PS(
	PSInput i,
	uniform bool enablesBaseTexture,
	uniform bool enablesUtilTexture,
	uniform bool enablesEnvironmentMap
	) : COLOR0
{
	// 光線の方向をView座標系へ変換。
	float3 vtLightDir = mul(float4(g_vtLightDir,0), g_mtxView);
	// ピクセルから視点へ向かう方向の単位ベクトルを計算。
	float3 vtEye = normalize(i.vtViewDirection);

	// パララックスマップによるテクスチャアドレスのオフセットを計算
	float2 uvStride = 0;
	if(g_enablesParallaxMap)
	{
		float height;
		if(g_parallaxMapSource == 0)
		{
			height = tex2D(g_texsmpBaseColor, i.uv0).a;
		}
		else if(g_parallaxMapSource == 1)
		{
			height = tex2D(g_texsmpUtilColor, i.uv1).a;
		}
		else
		{
			height = tex2D(g_texsmpNormalMap, i.uv2).a;
		}
		uvStride =
			(1.0f - height) * -g_parallaxMapStride
		  * float2(dot(i.Ns, vtEye), dot(i.Nt, vtEye));
	}

	// 法線ベクトルを決定
	float3 vtNormal;
	if(!g_enablesNormalMap)
	{
		// 頂点シェーダから出力された法線ベクトルを使う。
		vtNormal = i.Nv;
	}
	else
	{
		// 法線マップからベクトルを読み取る。
		float3 n = tex2D(g_texsmpNormalMap, i.uv2+uvStride).xyz;
		n = 2.0f * (n - 0.5f);
		vtNormal = (n.x * i.Ns) + (n.y * i.Nt) + (n.z * i.Nv);
	}
	// 法線ベクトルを正規化。
	vtNormal = normalize(vtNormal);

	// ベーステクスチャをフェッチ
	float4 textureColor[2];
	textureColor[0] = enablesBaseTexture? tex2D(g_texsmpBaseColor, i.uv0+uvStride): float4(1,1,1,1);
	textureColor[1] = enablesUtilTexture? tex2D(g_texsmpUtilColor, i.uv1+uvStride): float4(1,1,1,1);

	// ポリゴンのベースカラーを決定
	float4 colorMaterial =
		// マテリアルカラー
		(step(g_diffuseColorSelect, 0) * step(0, g_diffuseColorSelect) * g_diffuseColor)
		// 頂点カラー
	  + (step(g_diffuseColorSelect, 1) * step(1, g_diffuseColorSelect) * i.color)
		// マテリアルと頂点カラーの積
	  + (step(2, g_diffuseColorSelect) * g_diffuseColor * i.color);

	// マテリアルのベースカラーに対するライティング結果の輝度
	float4 lumDiffuse = { 0, 0, 0, 1 };
	float3 lumSpecular = { 0, 0, 0 };
	float3 lumRimSpecular = { 0, 0, 0 };

	// ライティング
	if(g_enablesLighting != 0)
	{
		// 白色のマテリアルに対しての拡散反射を計算。
		float d = max(0, dot(vtNormal, vtLightDir));
		lumDiffuse.rgb += d * g_colorLight.rgb;

		// 環境光を加算
		lumDiffuse.rgb += g_ambientLightColor;

		// 白色のマテリアルに対してのスペキュラーを計算。
		if(g_enablesSpecular)
		{
			float3 vtHalf = normalize(vtLightDir + vtEye);
			float s = max(0, dot(vtNormal, vtHalf));
			s = pow(s, g_SpecularPower);
			lumSpecular = s * g_colorLight.rgb;
		}

		// 白色のマテリアルに対してのリムスペキュラーを計算。
		if(g_enablesRimSpecular)
		{
			float fr = pow((1.0f - dot(vtNormal, vtEye)), g_rimSpecularPower);
			float lumi = saturate(dot(vtNormal, vtLightDir) + g_rimSpecularSelfLuminous);
			lumRimSpecular = (fr * lumi) * g_colorLight.rgb;
		}

		// 拡散反射の減衰を適用。
		lumDiffuse.rgb *= g_diffuseAttenuation;
		// 自己発光の輝度を加算。
		lumDiffuse.rgb += g_emissionColor;

		// マテリアルカラーに拡散反射のシェーディングを適用。
		colorMaterial *= lumDiffuse;
	}
	else
	{
		lumDiffuse = float4(1,1,1,1);
		lumSpecular = float3(0,0,0);
	}

	// テクスチャカラー/アルファを適用
	// ピクセルの最終カラーを代入する変数。
	float4 pixelColor = colorMaterial;
	if(enablesBaseTexture)
	{
		// ベースカラーテクスチャの合成
		pixelColor =
			// ModulateRGBA
			(step(g_blendBaseColorTexture,0) * step(0,g_blendBaseColorTexture) *
				pixelColor * textureColor[0])
			// ModulateRGB
		  + (step(g_blendBaseColorTexture,1) * step(1,g_blendBaseColorTexture) *
				float4((pixelColor * textureColor[0]).rgb, pixelColor.a))
			// AddRGB
		  + (step(g_blendBaseColorTexture,2) * step(2,g_blendBaseColorTexture) *
				float4((pixelColor + textureColor[0]).rgb, pixelColor.a))
			// DecalRGBA_Ma 
		  + (step(g_blendBaseColorTexture,3) * step(3,g_blendBaseColorTexture) *
				float4(lerp(pixelColor, textureColor[0], pixelColor.a).rgb,
					   lerp(1, textureColor[0].a, pixelColor.a)))
			// DecalRGB_Ta 
		  + (step(g_blendBaseColorTexture,4) * step(4,g_blendBaseColorTexture) *
				float4(lerp(pixelColor, textureColor[0], textureColor[0].a).rgb,
					   pixelColor.a))
			// ModulateDecalRGBA_Ma 
		  + (step(g_blendBaseColorTexture,5) * step(5,g_blendBaseColorTexture) *
				float4(pixelColor * lerp(float4(1,1,1,1), textureColor[0], pixelColor.a).rgb,
					   lerp(1, textureColor[0].a, pixelColor.a)))
			// ModulateDecalRGB_Ta 
		  + (step(g_blendBaseColorTexture,6) * step(6,g_blendBaseColorTexture) *
				float4(pixelColor * lerp(float4(1,1,1,1), textureColor[0], textureColor[0].a).rgb,
					   pixelColor.a));
	}
	if(enablesUtilTexture)
	{
		// 補助カラーテクスチャの合成
		pixelColor =
			// ModulateRGBA
			(step(g_blendUtilColorTexture,0) * step(0,g_blendUtilColorTexture) *
				(pixelColor * textureColor[1]))
			// ModulateRGB
		  + (step(g_blendUtilColorTexture,1) * step(1,g_blendUtilColorTexture) *
				float4((pixelColor * textureColor[1]).rgb, pixelColor.a))
		  // AddRGB
		  + (step(g_blendUtilColorTexture,2) * step(2,g_blendUtilColorTexture) *
				float4((pixelColor + textureColor[1]).rgb, pixelColor.a))
		  // DecalRGB_Ma
		  + (step(g_blendUtilColorTexture,3) * step(3,g_blendUtilColorTexture) *
				float4(lerp((colorMaterial*textureColor[0]), textureColor[1], colorMaterial.a).rgb,
					   1.0))
		  // DecalRGBA_Ma
		  + (step(g_blendUtilColorTexture,4) * step(4,g_blendUtilColorTexture) *
				float4(lerp((colorMaterial*textureColor[0]), textureColor[1], colorMaterial.a).rgb,
					   lerp(textureColor[0].a, textureColor[1].a, colorMaterial.a)))
		  // DecalRGBA_Ta
		  + (step(g_blendUtilColorTexture,5) * step(5,g_blendUtilColorTexture) *
				float4(lerp((colorMaterial*textureColor[0]), textureColor[1], colorMaterial.a).rgb,
					   lerp(colorMaterial.a*textureColor[0].a, colorMaterial.a, textureColor[1].a)))
		  // ModulateDecalRGB_Ma
		  + (step(g_blendUtilColorTexture,6) * step(6,g_blendUtilColorTexture) *
				float4((colorMaterial * lerp(textureColor[0], textureColor[1], colorMaterial.a)).rgb,
					   1.0))
		  // ModulateDecalRGBA_Ma
		  + (step(g_blendUtilColorTexture,7) * step(7,g_blendUtilColorTexture) *
				float4((colorMaterial * lerp(textureColor[0], textureColor[1], colorMaterial.a)).rgb,
					   lerp(textureColor[0].a, textureColor[1].a, colorMaterial.a)))
		  // ModulateDecalRGBA_Ta
		  + (step(g_blendUtilColorTexture,8) * step(8,g_blendUtilColorTexture) *
				float4((colorMaterial * lerp(textureColor[0], textureColor[1], textureColor[1].a)).rgb,
					   colorMaterial.a * lerp(textureColor[0].a, 1.0, textureColor[1].a)));
	}

	// グロスマップ値を代入
	float glossMap;
	if(!g_enablesGlossMap)
	{
		// 無効
		glossMap = 1.0f;
	}
	else
	{
		glossMap = 
			// ベースカラーテクスチャのアルファ
			(step(g_indexGlossSrc,0) * step(0,g_indexGlossSrc) * textureColor[0].a)
			// 補助カラーテクスチャのアルファ
		  + (step(g_indexGlossSrc,1) * step(1,g_indexGlossSrc) * textureColor[1].a)
			// フラグメントのアルファ
		  + (step(2,g_indexGlossSrc) * pixelColor.a);
	}

	// スペキュラーカラーを決定
	if(g_enablesSpecular)
	{
		float3 colorSpecular = 
			// マテリアルカラー
			(step(g_specularColorSelector,0) * step(0,g_specularColorSelector) * g_colorSpecular)
			// 頂点カラー
		  + (step(g_specularColorSelector,1) * step(1,g_specularColorSelector) * i.color.rgb)
			// マテリアルと頂点カラーの積
		  + (step(g_specularColorSelector,2) * step(2,g_specularColorSelector) * g_colorSpecular * i.color.rgb)
			// フラグメントのカラー。
		  + (step(3,g_specularColorSelector) * g_colorSpecular * pixelColor.rgb);

		// スペキュラーをのせる。
		pixelColor.rgb += glossMap * lumSpecular * colorSpecular;
	}

	// リムスペキュラーをのせる。
	if(g_enablesRimSpecular)
	{
		float3 colorRimSpecular = 
			// マテリアルカラー
			(step(g_specularColorSelector,0) * step(0,g_specularColorSelector) * g_colorRimSpecular)
			// 頂点カラー
		  + (step(g_specularColorSelector,1) * step(1,g_specularColorSelector) * i.color.rgb)
			// マテリアルと頂点カラーの積
		  + (step(g_specularColorSelector,2) * step(2,g_specularColorSelector) * g_colorRimSpecular * i.color.rgb)
			// フラグメントのカラー。
		  + (step(3,g_specularColorSelector) * g_colorRimSpecular * pixelColor.rgb);

		pixelColor.rgb += glossMap * lumRimSpecular * colorRimSpecular;
	}

	// 環境マップをのせる
	if(enablesEnvironmentMap)
	{
		float3 colEnvironmentMap;

		half3 rv = normalize(half3(reflect(vtEye, vtNormal)));

		if(g_environmentMapType == 0)
		{
			// ワールド座標系でのキューブ環境マップを行う。
			rv = mul(half4(rv,0), g_imtxView);

			// MAXとDirect3Dの座標系の違いを修正。
			rv = half3(-1,-1,-1) * rv.xzy;

			colEnvironmentMap =
				g_colorEnvironmentMapModulate
			  * texCUBE(g_texsmpCubeEnvironmentMap, rv).rgb;
		}
		else
		{
			// ビュー座標系での球状環境マップを行う
			half3 r = rv;
			r.z += 1.0;
			r = r * r;
			half m = rsqrt(r.x + r.y + r.z);
			half2 uv = 0.5 * (rv.xy * m + half2(1.0, 1.0));

			colEnvironmentMap =
				g_colorEnvironmentMapModulate
			  * tex2D(g_texsmpSphericalEnvironmentMap, uv).rgb;
		}

		if(g_enablesFresnelTerm)
		{
			// フレネル項によって環境マップの強さを調整する。
			float f = pow(1.0f - dot(vtEye, vtNormal), 4.0f);
			f = g_fresnelR0 + g_fresnelR1 * f;
			f *= glossMap;
			pixelColor.rgb = lerp(pixelColor.rgb, colEnvironmentMap.rgb, f);
		}
		else
		{
			// そのままピクセルの色へ加算する。
			pixelColor.rgb += glossMap * colEnvironmentMap.rgb;
		}
	}

	// フォグとの合成
	//  フォグスイッチがOFFならばフォグを無効にする。
	int fogType = (g_enablesFog? 1: 0) * g_fogDensityType;
	if(fogType == 0)
	{
		// フォグ無効
	}
	else if(fogType == 1)
	{
		// 線形フォグ
		pixelColor.rgb = lerp(
			g_fogColor.rgb,
			pixelColor.rgb,
			clamp(i.fogDistance, 0, 1));
	}
	else if(fogType == 2)
	{
		// 指数フォグ
		float d = ((g_fogDensity * 0.001) * i.fogDistance);
		pixelColor.rgb = lerp(
			g_fogColor.rgb,
			pixelColor.rgb,
			exp(-d));
	}
	else if(fogType == 3)
	{
		// 指数フォグ2
		float d = ((g_fogDensity * 0.001) * i.fogDistance);
		pixelColor.rgb = lerp(
			g_fogColor.rgb,
			pixelColor.rgb,
			exp(-d*d));
	}

	// デバッグ目的で、法線方向をカラーで出力。
	if(g_debugShowNormalDir)
	{
		pixelColor.rgb = float3(vtNormal);
	}

	return pixelColor;
}

//----------------------------------------------------------------------------

//----------------------------------------------------------------------------
struct ExpandMeshVSOutput
{
	float4 vtPosition			: POSITION;
};

ExpandMeshVSOutput ExpandMeshVS(
	float4 iPosition	: POSITION,
	float3 iNormal		: NORMAL)
{
	ExpandMeshVSOutput o;

	// ラスタライズの為に頂点の同時座標を出力。
	o.vtPosition = mul(iPosition, g_mtxWorldViewProj);

	// スクリーン座標上で法線方向に頂点を拡大する。
	float2 expand = mul(float4(iNormal,0.0), g_mtxWorldViewProj).xy;
	if(0.000000000001 < dot(expand, expand))
	{
		expand = normalize(expand);
	}
	o.vtPosition.xy += (0.001 * g_edgeSize * o.vtPosition.w) * expand;

	return o;
}

float4 ExpandMeshPS(ExpandMeshVSOutput i) : COLOR0
{
	return g_edgeColor;
}

//----------------------------------------------------------------------------

technique OPACITY
{
    pass P0 
    {		
		VertexShader	= compile vs_3_0 VS();

		PixelShader		= compile ps_3_0 PS(
			g_enablesBaseColorTex,
			g_enablesUtilColorTex,
			g_enablesEnvironmentMap);
		
		ZWriteEnable		= True;
		ZFunc				= LessEqual;

		AlphaBlendEnable	= False;
		AlphaTestEnable		= False;
		
		Wrap0				= COORD0;
    }
}

technique ALPHATEST
{
    pass P0 
    {		
		VertexShader	= compile vs_3_0 VS();

		PixelShader		= compile ps_3_0 PS(
			g_enablesBaseColorTex,
			g_enablesUtilColorTex,
			g_enablesEnvironmentMap);
		
		ZWriteEnable		= True;
		ZFunc				= LessEqual;

		AlphaBlendEnable	= False;
		AlphaTestEnable		= True;
		AlphaRef			= 80;
    }
}

technique BLEND
{
    pass P0 
    {		
		VertexShader	= compile vs_3_0 VS();

		PixelShader		= compile ps_3_0 PS(
			g_enablesBaseColorTex,
			g_enablesUtilColorTex,
			g_enablesEnvironmentMap);
		
		ZWriteEnable		= False;
		ZFunc				= LessEqual;

		AlphaBlendEnable	= True;
        SrcBlend			= SrcAlpha;
        DestBlend			= InvSrcAlpha;  

		AlphaTestEnable		= True;
		AlphaRef			= 1;
    }
}

technique BLEND_2PASS
{
    pass P0 
    {		
		VertexShader	= compile vs_3_0 VS();

		PixelShader		= compile ps_3_0 PS(
			g_enablesBaseColorTex,
			g_enablesUtilColorTex,
			g_enablesEnvironmentMap);
		
		ZWriteEnable		= False;
		ZFunc				= LessEqual;

		AlphaBlendEnable	= True;
        SrcBlend			= SrcAlpha;
        DestBlend			= InvSrcAlpha;  

		AlphaTestEnable		= True;
		AlphaRef			= 1;
    }
}

technique ADD
{
    pass P0 
    {		
		VertexShader	= compile vs_3_0 VS();

		PixelShader		= compile ps_3_0 PS(
			g_enablesBaseColorTex,
			g_enablesUtilColorTex,
			g_enablesEnvironmentMap);
		
		ZWriteEnable		= False;
		ZFunc				= LessEqual;

		AlphaBlendEnable	= True;
        SrcBlend			= SrcAlpha;
        DestBlend			= One;  

		AlphaTestEnable		= True;
		AlphaRef			= 1;
    }
}

technique SUB
{
    pass P0 
    {		
		VertexShader	= compile vs_3_0 VS();

		PixelShader		= compile ps_3_0 PS(
			g_enablesBaseColorTex,
			g_enablesUtilColorTex,
			g_enablesEnvironmentMap);
		
		ZWriteEnable		= False;
		ZFunc				= LessEqual;

		AlphaBlendEnable	= True;
        SrcBlend			= SrcAlpha;
        DestBlend			= One;
        BlendOp				= RevSubtract;

		AlphaTestEnable		= True;
		AlphaRef			= 1;
    }
}

technique MODULATE
{
    pass P0 
    {		
		VertexShader	= compile vs_3_0 VS();

		PixelShader		= compile ps_3_0 PS(
			g_enablesBaseColorTex,
			g_enablesUtilColorTex,
			g_enablesEnvironmentMap);
		
		ZWriteEnable		= False;
		ZFunc				= LessEqual;

		AlphaBlendEnable	= True;
        SrcBlend			= DestColor;
        DestBlend			= Zero;  

		AlphaTestEnable		= True;
		AlphaRef			= 1;
    }
}

technique CUSTOM
{
    pass P0 
    {		
		VertexShader	= compile vs_3_0 VS();

		PixelShader		= compile ps_3_0 PS(
			g_enablesBaseColorTex,
			g_enablesUtilColorTex,
			g_enablesEnvironmentMap);
		
		ZWriteEnable		= False;
		ZFunc				= LessEqual;

		AlphaBlendEnable	= True;
        SrcBlend			= SrcAlpha;
        DestBlend			= InvSrcAlpha;  

		AlphaTestEnable		= True;
		AlphaRef			= 1;
    }
}

technique OPACITY_EDGE
{
    pass P0
    {		
		VertexShader	= compile vs_3_0 ExpandMeshVS();

		PixelShader		= compile ps_3_0 ExpandMeshPS();

		CullMode		= CCW;
		ZWriteEnable	= True;
		ZFunc			= LessEqual;

		AlphaBlendEnable	= False;
		AlphaTestEnable		= False;
    }

    pass P1
    {		
		VertexShader	= compile vs_3_0 VS();

		PixelShader		= compile ps_3_0 PS(
			g_enablesBaseColorTex,
			g_enablesUtilColorTex,
			g_enablesEnvironmentMap);

		CullMode		= CW;
		ZWriteEnable	= True;
		ZFunc			= LessEqual;
		
		AlphaBlendEnable	= False;
		AlphaTestEnable		= False;
    }
}

technique SPECULAR_2PASS
{
    pass P0 
    {		
		VertexShader	= compile vs_3_0 VS();

		PixelShader		= compile ps_3_0 PS(
			g_enablesBaseColorTex,
			g_enablesUtilColorTex,
			g_enablesEnvironmentMap);
		
		ZWriteEnable		= False;
		ZFunc				= LessEqual;

		AlphaBlendEnable	= True;
        SrcBlend			= SrcAlpha;
        DestBlend			= InvSrcAlpha;  

		AlphaTestEnable		= True;
		AlphaRef			= 1;
    }
}
