// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#include "Common.h"

#if defined(WINDOWS_DESKTOP) || defined(UWP) || defined(WINDOWS_STORE) || defined(WINDOWS_PHONE) || !defined(__clang__)

#include "../../../deps/NativePath/NativePath.h"
#include "../../../deps/NativePath/NativeThreading.h"
#include "../../../deps/NativePath/NativeDynamicLinking.h"
#include "../../Xenko.Native/XenkoNative.h"

extern "C" {
	class SpinLock
	{
	public:
		SpinLock();

		void Lock();

		void Unlock();

	private:
		volatile bool mLocked;
	};

	namespace XAudio2
	{
		typedef struct _GUID {
			unsigned long  Data1;
			unsigned short Data2;
			unsigned short Data3;
			unsigned char  Data4[8];
		} GUID;

		typedef GUID IID;

		typedef unsigned long       DWORD;
		typedef int                 BOOL;
		typedef unsigned char       BYTE;
		typedef unsigned short      WORD;
		typedef float               FLOAT;
		typedef FLOAT               *PFLOAT;
		typedef int                 INT;
		typedef unsigned int        UINT;
		typedef unsigned int        *PUINT;

#define REFIID const IID &
#define HRESULT long
#define UINT32 unsigned int 
#define ULONG unsigned long
#define THIS_
#define THIS void
#define PURE = 0;
#define _Outptr_
#define _In_
#define _In_opt_
#define _Out_
#define X2DEFAULT(x) =x
#define _Reserved_
#define _In_reads_bytes_(_xxx_)
#define _Out_writes_bytes_(_xxx_)
#define _Out_writes_(_xxx_)
#define _In_reads_(_xxx_)
#define STDMETHOD(method) virtual HRESULT __stdcall method
#define STDMETHOD_(type,method) virtual type __stdcall method
#define XAUDIO2_COMMIT_NOW              0             // Used as an OperationSet argument
#define XAUDIO2_COMMIT_ALL              0             // Used in IXAudio2::CommitChanges
#define XAUDIO2_INVALID_OPSET           (UINT32)(-1)  // Not allowed for OperationSet arguments
#define XAUDIO2_NO_LOOP_REGION          0             // Used in XAUDIO2_BUFFER.LoopCount
#define XAUDIO2_LOOP_INFINITE           255           // Used in XAUDIO2_BUFFER.LoopCount
#define XAUDIO2_DEFAULT_CHANNELS        0             // Used in CreateMasteringVoice
#define XAUDIO2_DEFAULT_SAMPLERATE      0             // Used in CreateMasteringVoice
#define BYTE char
#define UINT64 unsigned __int64 
#define _In_opt_z_
#define FAILED(hr) (((HRESULT)(hr)) < 0)
#define _Inout_

#define X3DAUDIO_PI  3.141592654f
#define X3DAUDIO_2PI 6.283185307f

#if !defined(_SPEAKER_POSITIONS_)
#define _SPEAKER_POSITIONS_
#define SPEAKER_FRONT_LEFT            0x00000001
#define SPEAKER_FRONT_RIGHT           0x00000002
#define SPEAKER_FRONT_CENTER          0x00000004
#define SPEAKER_LOW_FREQUENCY         0x00000008
#define SPEAKER_BACK_LEFT             0x00000010
#define SPEAKER_BACK_RIGHT            0x00000020
#define SPEAKER_FRONT_LEFT_OF_CENTER  0x00000040
#define SPEAKER_FRONT_RIGHT_OF_CENTER 0x00000080
#define SPEAKER_BACK_CENTER           0x00000100
#define SPEAKER_SIDE_LEFT             0x00000200
#define SPEAKER_SIDE_RIGHT            0x00000400
#define SPEAKER_TOP_CENTER            0x00000800
#define SPEAKER_TOP_FRONT_LEFT        0x00001000
#define SPEAKER_TOP_FRONT_CENTER      0x00002000
#define SPEAKER_TOP_FRONT_RIGHT       0x00004000
#define SPEAKER_TOP_BACK_LEFT         0x00008000
#define SPEAKER_TOP_BACK_CENTER       0x00010000
#define SPEAKER_TOP_BACK_RIGHT        0x00020000
#define SPEAKER_RESERVED              0x7FFC0000 // bit mask locations reserved for future use
#define SPEAKER_ALL                   0x80000000 // used to specify that any possible permutation of speaker configurations
#endif

		// standard speaker geometry configurations, used with X3DAudioInitialize
#if !defined(SPEAKER_MONO)
#define SPEAKER_MONO             SPEAKER_FRONT_CENTER
#define SPEAKER_STEREO           (SPEAKER_FRONT_LEFT | SPEAKER_FRONT_RIGHT)
#define SPEAKER_2POINT1          (SPEAKER_FRONT_LEFT | SPEAKER_FRONT_RIGHT | SPEAKER_LOW_FREQUENCY)
#define SPEAKER_SURROUND         (SPEAKER_FRONT_LEFT | SPEAKER_FRONT_RIGHT | SPEAKER_FRONT_CENTER | SPEAKER_BACK_CENTER)
#define SPEAKER_QUAD             (SPEAKER_FRONT_LEFT | SPEAKER_FRONT_RIGHT | SPEAKER_BACK_LEFT | SPEAKER_BACK_RIGHT)
#define SPEAKER_4POINT1          (SPEAKER_FRONT_LEFT | SPEAKER_FRONT_RIGHT | SPEAKER_LOW_FREQUENCY | SPEAKER_BACK_LEFT | SPEAKER_BACK_RIGHT)
#define SPEAKER_5POINT1          (SPEAKER_FRONT_LEFT | SPEAKER_FRONT_RIGHT | SPEAKER_FRONT_CENTER | SPEAKER_LOW_FREQUENCY | SPEAKER_BACK_LEFT | SPEAKER_BACK_RIGHT)
#define SPEAKER_7POINT1          (SPEAKER_FRONT_LEFT | SPEAKER_FRONT_RIGHT | SPEAKER_FRONT_CENTER | SPEAKER_LOW_FREQUENCY | SPEAKER_BACK_LEFT | SPEAKER_BACK_RIGHT | SPEAKER_FRONT_LEFT_OF_CENTER | SPEAKER_FRONT_RIGHT_OF_CENTER)
#define SPEAKER_5POINT1_SURROUND (SPEAKER_FRONT_LEFT | SPEAKER_FRONT_RIGHT | SPEAKER_FRONT_CENTER | SPEAKER_LOW_FREQUENCY | SPEAKER_SIDE_LEFT  | SPEAKER_SIDE_RIGHT)
#define SPEAKER_7POINT1_SURROUND (SPEAKER_FRONT_LEFT | SPEAKER_FRONT_RIGHT | SPEAKER_FRONT_CENTER | SPEAKER_LOW_FREQUENCY | SPEAKER_BACK_LEFT | SPEAKER_BACK_RIGHT | SPEAKER_SIDE_LEFT  | SPEAKER_SIDE_RIGHT)
#endif

#define X3DAUDIO_CALCULATE_MATRIX          0x00000001 // enable matrix coefficient table calculation
#define X3DAUDIO_CALCULATE_DELAY           0x00000002 // enable delay time array calculation (stereo final mix only)
#define X3DAUDIO_CALCULATE_LPF_DIRECT      0x00000004 // enable LPF direct-path coefficient calculation
#define X3DAUDIO_CALCULATE_LPF_REVERB      0x00000008 // enable LPF reverb-path coefficient calculation
#define X3DAUDIO_CALCULATE_REVERB          0x00000010 // enable reverb send level calculation
#define X3DAUDIO_CALCULATE_DOPPLER         0x00000020 // enable doppler shift factor calculation
#define X3DAUDIO_CALCULATE_EMITTER_ANGLE   0x00000040 // enable emitter-to-listener interior angle calculation

#define HRTF_MAX_GAIN_LIMIT                  12.0f
#define HRTF_MIN_GAIN_LIMIT                  -96.0f
#define HRTF_MIN_UNITY_GAIN_DISTANCE         0.05f
#define HRTF_DEFAULT_UNITY_GAIN_DISTANCE     1.0f
#define HRTF_DEFAULT_CUTOFF_DISTANCE         FLT_MAX

		struct IUnknown
		{
			// NAME: IXAudio2::QueryInterface
			// DESCRIPTION: Queries for a given COM interface on the XAudio2 object.
			//              Only IID_IUnknown and IID_IXAudio2 are supported.
			//
			// ARGUMENTS:
			//  riid - IID of the interface to be obtained.
			//  ppvInterface - Returns a pointer to the requested interface.
			//
			STDMETHOD(QueryInterface) (THIS_ REFIID riid, _Outptr_ void** ppvInterface) PURE;

			// NAME: IXAudio2::AddRef
			// DESCRIPTION: Adds a reference to the XAudio2 object.
			//
			STDMETHOD_(ULONG, AddRef) (THIS) PURE;

			// NAME: IXAudio2::Release
			// DESCRIPTION: Releases a reference to the XAudio2 object.
			//
			STDMETHOD_(ULONG, Release) (THIS) PURE;
		};

		//! Represents a position in 3D space, using a right-handed coordinate system.
		typedef struct HrtfPosition
		{
			float x;
			float y;
			float z;
		} HrtfPosition;

		//! Indicates the orientation of an HRTF directivity object. This is a row-major 3x3 rotation matrix.
		typedef struct HrtfOrientation
		{
			float element[9];
		} HrtfOrientation;

		//! Indicates one of several stock directivity patterns.
		typedef enum HrtfDirectivityType
		{
			//! The sound emission is in all directions.
			OmniDirectional = 0,
			//! The sound emission is a cardiod shape.
			Cardioid,
			//! The sound emission is a cone.
			Cone
		} HrtfDirectivityType;

		//! Indicates one of several stock environment types.
		typedef enum HrtfEnvironment
		{
			//! A small room.
			Small = 0,
			//! A medium-sized room.
			Medium,
			//! A large enclosed space.
			Large,
			//! An outdoor space.
			Outdoors,
		} HrtfEnvironment;

		//
		//! Base directivity pattern descriptor. Describes the type of directivity applied to a sound.
		//! The scaling parameter is used to interpolate between directivity behavior and omnidirectional; it determines how much attenuation is applied to the source outside of the directivity pattern and controls how directional the source is.
		//
		typedef struct HrtfDirectivity
		{
			//! Indicates the type of directivity.
			HrtfDirectivityType type;
			//! A normalized value between zero and one. Specifies the amount of linear interpolation between omnidirectional sound and the full directivity pattern, where 0 is fully omnidirectional and 1 is fully directional.
			float               scaling;
		} HrtfDirectivity;

		//! Describes a cardioid directivity pattern.
		typedef struct HrtfDirectivityCardioid
		{
			//! Descriptor for the cardioid pattern. The type parameter must be set to HrtfDirectivityType.Cardioid.
			HrtfDirectivity directivity;
			//! Order controls the shape of the cardioid. The higher order the shape, the narrower it is. Must be greater than 0 and less than or equal to 32.
			float           order;
		} HrtfDirectivityCardioid;

		//
		//! Describes a cone directivity.
		//! Attenuation is 0 inside the inner cone.
		//! Attenuation is linearly interpolated between the inner cone, which is defined by innerAngle, and the outer cone, which is defined by outerAngle.
		//
		typedef struct HrtfDirectivityCone
		{
			//! Descriptor for the cone pattern. The type parameter must be set to HrtfDirectivityType.Cone.
			HrtfDirectivity directivity;
			//! Angle, in radians, that defines the inner cone. Must be between 0 and 2 * pi.
			float           innerAngle;
			//! Angle, in radians, that defines the outer cone. Must be between 0 and 2 * pi.
			float           outerAngle;
		} HrtfDirectivityCone;

		//
		//! Indicates a distance-based decay type applied to a sound.
		//
		typedef enum HrtfDistanceDecayType
		{
			//! Simulates natural decay with distance, as constrained by minimum and maximum gain distance limits. Drops to silence at rolloff distance.
			NaturalDecay = 0,
			//! Used to set up a custom gain curve, within the maximum and minimum gain limit.
			CustomDecay
		} HrtfDistanceDecayType;

		//
		//! Describes a distance-based decay behavior.
		//
		typedef struct HrtfDistanceDecay
		{
			//! The type of decay behavior, natural or custom.
			HrtfDistanceDecayType type;
			//! The maximum gain limit applied at any distance. Applies to both natural and custom decay. This value is specified in dB, with a range from -96 to 12 inclusive. The default value is 12 dB.
			float                 maxGain;
			//! The minimum gain limit applied at any distance. Applies to both natural and custom decay. This value is specified in dB, with a range from -96 to 12 inclusive. The default value is -96 dB.
			float                 minGain;
			//! The distance at which the gain is 0dB. Applies to natural decay only. This value is specified in meters, with a range from 0.05 to infinity (FLT_MAX). The default value is 1 meter.
			float                 unityGainDistance;
			//! The distance at which output is silent. Applies to natural decay only. This value is specified in meters, with a range from zero (non-inclusive) to infinity (FLT_MAX). The default value is infinity.
			float                 cutoffDistance;
		} HrtfDistanceDecay;

		//
		//! Specifies parameters used to initialize HRTF.
		//! 
		//! Instances of the XAPO interface are created by using the CreateHrtfApo() API:
		//!   ```STDAPI CreateHrtfApo(_In_ const HrtfApoInit* pInit, _Outptr_ IXAPO** ppXapo);```
		//! 
		//
		typedef struct HrtfApoInit
		{
			//! The decay type. If you pass in nullptr, the default value will be used. The default is natural decay.
			HrtfDistanceDecay* distanceDecay;
			//! The directivity type. If you pass in nullptr, the default value will be used. The default directivity is omni-directional.
			HrtfDirectivity*   directivity;
		} HrtfApoInit;

		struct XMFLOAT3
		{
			float x;
			float y;
			float z;

			XMFLOAT3();

			XMFLOAT3(float _x, float _y, float _z);

			explicit XMFLOAT3(_In_reads_(3) const float* pArray);

			XMFLOAT3& operator=(const XMFLOAT3& Float3);
		};

		typedef float FLOAT32; // 32-bit IEEE float
		typedef XMFLOAT3 X3DAUDIO_VECTOR; // float 3D vector

#pragma pack(push, 1)

		typedef struct X3DAUDIO_CONE
		{
			FLOAT32 InnerAngle; // inner cone angle in radians, must be within [0.0f, X3DAUDIO_2PI]
			FLOAT32 OuterAngle; // outer cone angle in radians, must be within [InnerAngle, X3DAUDIO_2PI]

			FLOAT32 InnerVolume; // volume level scaler on/within inner cone, used only for matrix calculations, must be within [0.0f, 2.0f] when used
			FLOAT32 OuterVolume; // volume level scaler on/beyond outer cone, used only for matrix calculations, must be within [0.0f, 2.0f] when used
			FLOAT32 InnerLPF;    // LPF (both direct and reverb paths) coefficient subtrahend on/within inner cone, used only for LPF (both direct and reverb paths) calculations, must be within [0.0f, 1.0f] when used
			FLOAT32 OuterLPF;    // LPF (both direct and reverb paths) coefficient subtrahend on/beyond outer cone, used only for LPF (both direct and reverb paths) calculations, must be within [0.0f, 1.0f] when used
			FLOAT32 InnerReverb; // reverb send level scaler on/within inner cone, used only for reverb calculations, must be within [0.0f, 2.0f] when used
			FLOAT32 OuterReverb; // reverb send level scaler on/beyond outer cone, used only for reverb calculations, must be within [0.0f, 2.0f] when used
		} X3DAUDIO_CONE, *LPX3DAUDIO_CONE;
		static const X3DAUDIO_CONE X3DAudioDefault_DirectionalCone = { X3DAUDIO_PI / 2, X3DAUDIO_PI, 1.0f, 0.708f, 0.0f, 0.25f, 0.708f, 1.0f };

		typedef struct X3DAUDIO_LISTENER
		{
			X3DAUDIO_VECTOR OrientFront; // orientation of front direction, used only for matrix and delay calculations or listeners with cones for matrix, LPF (both direct and reverb paths), and reverb calculations, must be normalized when used
			X3DAUDIO_VECTOR OrientTop;   // orientation of top direction, used only for matrix and delay calculations, must be orthonormal with OrientFront when used

			X3DAUDIO_VECTOR Position; // position in user-defined world units, does not affect Velocity
			X3DAUDIO_VECTOR Velocity; // velocity vector in user-defined world units/second, used only for doppler calculations, does not affect Position

			X3DAUDIO_CONE* pCone; // sound cone, used only for matrix, LPF (both direct and reverb paths), and reverb calculations, NULL specifies omnidirectionality
		} X3DAUDIO_LISTENER, *LPX3DAUDIO_LISTENER;

		typedef struct X3DAUDIO_DISTANCE_CURVE_POINT
		{
			FLOAT32 Distance;   // normalized distance, must be within [0.0f, 1.0f]
			FLOAT32 DSPSetting; // DSP setting
		} X3DAUDIO_DISTANCE_CURVE_POINT, *LPX3DAUDIO_DISTANCE_CURVE_POINT;

		typedef struct X3DAUDIO_DISTANCE_CURVE
		{
			X3DAUDIO_DISTANCE_CURVE_POINT* pPoints;    // distance curve point array, must have at least PointCount elements with no duplicates and be sorted in ascending order with respect to Distance
			UINT32                         PointCount; // number of distance curve points, must be >= 2 as all distance curves must have at least two endpoints, defining DSP settings at 0.0f and 1.0f normalized distance
		} X3DAUDIO_DISTANCE_CURVE, *LPX3DAUDIO_DISTANCE_CURVE;
		static const X3DAUDIO_DISTANCE_CURVE_POINT X3DAudioDefault_LinearCurvePoints[2] = { {0.0f, 1.0f}, {1.0f, 0.0f} };
		static const X3DAUDIO_DISTANCE_CURVE       X3DAudioDefault_LinearCurve = { (X3DAUDIO_DISTANCE_CURVE_POINT*)&X3DAudioDefault_LinearCurvePoints[0], 2 };

		typedef struct X3DAUDIO_EMITTER
		{
			X3DAUDIO_CONE* pCone; // sound cone, used only with single-channel emitters for matrix, LPF (both direct and reverb paths), and reverb calculations, NULL specifies omnidirectionality
			X3DAUDIO_VECTOR OrientFront; // orientation of front direction, used only for emitter angle calculations or with multi-channel emitters for matrix calculations or single-channel emitters with cones for matrix, LPF (both direct and reverb paths), and reverb calculations, must be normalized when used
			X3DAUDIO_VECTOR OrientTop;   // orientation of top direction, used only with multi-channel emitters for matrix calculations, must be orthonormal with OrientFront when used

			X3DAUDIO_VECTOR Position; // position in user-defined world units, does not affect Velocity
			X3DAUDIO_VECTOR Velocity; // velocity vector in user-defined world units/second, used only for doppler calculations, does not affect Position

			FLOAT32 InnerRadius;      // inner radius, must be within [0.0f, FLT_MAX]
			FLOAT32 InnerRadiusAngle; // inner radius angle, must be within [0.0f, X3DAUDIO_PI/4.0)

			UINT32 ChannelCount;       // number of sound channels, must be > 0
			FLOAT32 ChannelRadius;     // channel radius, used only with multi-channel emitters for matrix calculations, must be >= 0.0f when used
			FLOAT32* pChannelAzimuths; // channel azimuth array, used only with multi-channel emitters for matrix calculations, contains positions of each channel expressed in radians along the channel radius with respect to the front orientation vector in the plane orthogonal to the top orientation vector, or X3DAUDIO_2PI to specify an LFE channel, must have at least ChannelCount elements, all within [0.0f, X3DAUDIO_2PI] when used

			X3DAUDIO_DISTANCE_CURVE* pVolumeCurve;    // volume level distance curve, used only for matrix calculations, NULL specifies a default curve that conforms to the inverse square law, calculated in user-defined world units with distances <= CurveDistanceScaler clamped to no attenuation
			X3DAUDIO_DISTANCE_CURVE* pLFECurve;       // LFE level distance curve, used only for matrix calculations, NULL specifies a default curve that conforms to the inverse square law, calculated in user-defined world units with distances <= CurveDistanceScaler clamped to no attenuation
			X3DAUDIO_DISTANCE_CURVE* pLPFDirectCurve; // LPF direct-path coefficient distance curve, used only for LPF direct-path calculations, NULL specifies the default curve: [0.0f,1.0f], [1.0f,0.75f]
			X3DAUDIO_DISTANCE_CURVE* pLPFReverbCurve; // LPF reverb-path coefficient distance curve, used only for LPF reverb-path calculations, NULL specifies the default curve: [0.0f,0.75f], [1.0f,0.75f]
			X3DAUDIO_DISTANCE_CURVE* pReverbCurve;    // reverb send level distance curve, used only for reverb calculations, NULL specifies the default curve: [0.0f,1.0f], [1.0f,0.0f]

			FLOAT32 CurveDistanceScaler; // curve distance scaler, used to scale normalized distance curves to user-defined world units and/or exaggerate their effect, used only for matrix, LPF (both direct and reverb paths), and reverb calculations, must be within [FLT_MIN, FLT_MAX] when used
			FLOAT32 DopplerScaler;       // doppler shift scaler, used to exaggerate doppler shift effect, used only for doppler calculations, must be within [0.0f, FLT_MAX] when used
		} X3DAUDIO_EMITTER, *LPX3DAUDIO_EMITTER;

		typedef struct X3DAUDIO_DSP_SETTINGS
		{
			FLOAT32* pMatrixCoefficients; // [inout] matrix coefficient table, receives an array representing the volume level used to send from source channel S to destination channel D, stored as pMatrixCoefficients[SrcChannelCount * D + S], must have at least SrcChannelCount*DstChannelCount elements
			FLOAT32* pDelayTimes;         // [inout] delay time array, receives delays for each destination channel in milliseconds, must have at least DstChannelCount elements (stereo final mix only)
			UINT32 SrcChannelCount;       // [in] number of source channels, must equal number of channels in respective emitter
			UINT32 DstChannelCount;       // [in] number of destination channels, must equal number of channels of the final mix

			FLOAT32 LPFDirectCoefficient; // [out] LPF direct-path coefficient
			FLOAT32 LPFReverbCoefficient; // [out] LPF reverb-path coefficient
			FLOAT32 ReverbLevel; // [out] reverb send level
			FLOAT32 DopplerFactor; // [out] doppler shift factor, scales resampler ratio for doppler shift effect, where the effective frequency = DopplerFactor * original frequency
			FLOAT32 EmitterToListenerAngle; // [out] emitter-to-listener interior angle, expressed in radians with respect to the emitter's front orientation

			FLOAT32 EmitterToListenerDistance; // [out] distance in user-defined world units from the emitter base to listener position, always calculated
			FLOAT32 EmitterVelocityComponent; // [out] component of emitter velocity vector projected onto emitter->listener vector in user-defined world units/second, calculated only for doppler
			FLOAT32 ListenerVelocityComponent; // [out] component of listener velocity vector projected onto emitter->listener vector in user-defined world units/second, calculated only for doppler
		} X3DAUDIO_DSP_SETTINGS, *LPX3DAUDIO_DSP_SETTINGS;

#define X3DAUDIO_HANDLE_BYTESIZE 20
		typedef BYTE X3DAUDIO_HANDLE[X3DAUDIO_HANDLE_BYTESIZE];

#define SPEED_OF_SOUND 343.5f

		extern HRESULT __stdcall CoInitializeEx(void* ppXAudio2, DWORD dwCoInit);

		typedef HRESULT (__stdcall * XAudio2CreatePtr)(void** ppXAudio2, UINT32 flags, UINT32 processor);
		typedef HRESULT (_cdecl * X3DAudioInitializePtr)(UINT32 SpeakerChannelMask, float SpeedOfSound, _Out_writes_bytes_(X3DAUDIO_HANDLE_BYTESIZE) X3DAUDIO_HANDLE Instance);
		typedef void (_cdecl * X3DAudioCalculatePtr)(_In_reads_bytes_(X3DAUDIO_HANDLE_BYTESIZE) const X3DAUDIO_HANDLE Instance, _In_ const X3DAUDIO_LISTENER* pListener, _In_ const X3DAUDIO_EMITTER* pEmitter, UINT32 Flags, _Inout_ X3DAUDIO_DSP_SETTINGS* pDSPSettings);

		typedef HRESULT(__stdcall * CreateHrtfApoPtr)(const HrtfApoInit* init, IUnknown** xApo);
		CreateHrtfApoPtr CreateHrtfApoFunc = NULL;

#ifndef WINDOWS_DESKTOP
		extern HRESULT __stdcall XAudio2Create(void** ppXAudio2, UINT32 flags, UINT32 processor);
		XAudio2CreatePtr XAudio2CreateFunc = XAudio2Create;
		extern HRESULT _cdecl X3DAudioInitialize(UINT32 SpeakerChannelMask, float SpeedOfSound, _Out_writes_bytes_(X3DAUDIO_HANDLE_BYTESIZE) X3DAUDIO_HANDLE Instance);
		X3DAudioInitializePtr X3DAudioInitializeFunc = X3DAudioInitialize;
		extern void _cdecl X3DAudioCalculate(_In_reads_bytes_(X3DAUDIO_HANDLE_BYTESIZE) const X3DAUDIO_HANDLE Instance, _In_ const X3DAUDIO_LISTENER* pListener, _In_ const X3DAUDIO_EMITTER* pEmitter, UINT32 Flags, _Inout_ X3DAUDIO_DSP_SETTINGS* pDSPSettings);
		X3DAudioCalculatePtr X3DAudioCalculateFunc = X3DAudioCalculate;
#else
		XAudio2CreatePtr XAudio2CreateFunc = NULL;
		X3DAudioInitializePtr X3DAudioInitializeFunc = NULL;
		X3DAudioCalculatePtr X3DAudioCalculateFunc = NULL;
#endif

		struct IXAudio2Voice;

		typedef struct XAUDIO2_VOICE_DETAILS
		{
			UINT32 CreationFlags;               // Flags the voice was created with.
			UINT32 ActiveFlags;                 // Flags currently active.
			UINT32 InputChannels;               // Channels in the voice's input audio.
			UINT32 InputSampleRate;             // Sample rate of the voice's input audio.
		} XAUDIO2_VOICE_DETAILS;

		typedef struct XAUDIO2_SEND_DESCRIPTOR
		{
			UINT32 Flags;                       // Either 0 or XAUDIO2_SEND_USEFILTER.
			IXAudio2Voice* pOutputVoice;        // This send's destination voice.
		} XAUDIO2_SEND_DESCRIPTOR;

		typedef struct XAUDIO2_VOICE_SENDS
		{
			UINT32 SendCount;                   // Number of sends from this voice.
			XAUDIO2_SEND_DESCRIPTOR* pSends;    // Array of SendCount send descriptors.
		} XAUDIO2_VOICE_SENDS;

		typedef struct XAUDIO2_EFFECT_DESCRIPTOR
		{
			IUnknown* pEffect;                  // Pointer to the effect object's IUnknown interface.
			BOOL InitialState;                  // TRUE if the effect should begin in the enabled state.
			UINT32 OutputChannels;              // How many output channels the effect should produce.
		} XAUDIO2_EFFECT_DESCRIPTOR;

		typedef struct XAUDIO2_EFFECT_CHAIN
		{
			UINT32 EffectCount;                 // Number of effects in this voice's effect chain.
			XAUDIO2_EFFECT_DESCRIPTOR* pEffectDescriptors; // Array of effect descriptors.
		} XAUDIO2_EFFECT_CHAIN;

		typedef enum XAUDIO2_FILTER_TYPE
		{
			LowPassFilter,                      // Attenuates frequencies above the cutoff frequency (state-variable filter).
			BandPassFilter,                     // Attenuates frequencies outside a given range      (state-variable filter).
			HighPassFilter,                     // Attenuates frequencies below the cutoff frequency (state-variable filter).
			NotchFilter,                        // Attenuates frequencies inside a given range       (state-variable filter).
			LowPassOnePoleFilter,               // Attenuates frequencies above the cutoff frequency (one-pole filter, XAUDIO2_FILTER_PARAMETERS.OneOverQ has no effect)
			HighPassOnePoleFilter               // Attenuates frequencies below the cutoff frequency (one-pole filter, XAUDIO2_FILTER_PARAMETERS.OneOverQ has no effect)
		} XAUDIO2_FILTER_TYPE;

		typedef struct XAUDIO2_FILTER_PARAMETERS
		{
			XAUDIO2_FILTER_TYPE Type;           // Filter type.
			float Frequency;                    // Filter coefficient.
												//  must be >= 0 and <= XAUDIO2_MAX_FILTER_FREQUENCY
												//  See XAudio2CutoffFrequencyToRadians() for state-variable filter types and
												//  XAudio2CutoffFrequencyToOnePoleCoefficient() for one-pole filter types.
			float OneOverQ;                     // Reciprocal of the filter's quality factor Q;
												//  must be > 0 and <= XAUDIO2_MAX_FILTER_ONEOVERQ.
												//  Has no effect for one-pole filters.
		} XAUDIO2_FILTER_PARAMETERS;

		struct IXAPOHrtfParameters : IUnknown
		{
			// HRTF params
			//! The position of the sound relative to the listener.
			STDMETHOD(SetSourcePosition)(THIS_ _In_ const HrtfPosition* position);
			//! The rotation matrix for the source orientation, with respect to the listener's frame of reference (the listener's coordinate system).
			STDMETHOD(SetSourceOrientation)(THIS_ _In_ const HrtfOrientation* orientation);
			//! The custom direct path gain value for the current source position. Valid only for sounds played with the HrtfDistanceDecayType. Custom decay type.
			STDMETHOD(SetSourceGain)(THIS_ _In_ float gain);

			// Distance cue params
			//! Selects the acoustic environment to simulate.
			STDMETHOD(SetEnvironment)(THIS_ _In_ HrtfEnvironment environment);
		};

		struct IXAudio2EngineCallback
		{
			// Called by XAudio2 just before an audio processing pass begins.
			STDMETHOD_(void, OnProcessingPassStart) (THIS) PURE;

			// Called just after an audio processing pass ends.
			STDMETHOD_(void, OnProcessingPassEnd) (THIS) PURE;

			// Called in the event of a critical system error which requires XAudio2
			// to be closed down and restarted.  The error code is given in Error.
			STDMETHOD_(void, OnCriticalError) (THIS_ HRESULT Error) PURE;
		};

		struct IXAudio2Voice
		{
			/* NAME: IXAudio2Voice::GetVoiceDetails
			// DESCRIPTION: Returns the basic characteristics of this voice.
			//
			// ARGUMENTS:
			//  pVoiceDetails - Returns the voice's details.
			*/
			STDMETHOD_(void, GetVoiceDetails) (THIS_ _Out_ XAUDIO2_VOICE_DETAILS* pVoiceDetails) PURE;

			/* NAME: IXAudio2Voice::SetOutputVoices
			// DESCRIPTION: Replaces the set of submix/mastering voices that receive
			//              this voice's output.
			//
			// ARGUMENTS:
			//  pSendList - Optional list of voices this voice should send audio to.
			*/
			STDMETHOD(SetOutputVoices) (THIS_ _In_opt_ const XAUDIO2_VOICE_SENDS* pSendList) PURE;

			/* NAME: IXAudio2Voice::SetEffectChain
			// DESCRIPTION: Replaces this voice's current effect chain with a new one.
			//
			// ARGUMENTS:
			//  pEffectChain - Structure describing the new effect chain to be used.
			*/
			STDMETHOD(SetEffectChain) (THIS_ _In_opt_ const XAUDIO2_EFFECT_CHAIN* pEffectChain) PURE;

			/* NAME: IXAudio2Voice::EnableEffect
			// DESCRIPTION: Enables an effect in this voice's effect chain.
			//
			// ARGUMENTS:
			//  EffectIndex - Index of an effect within this voice's effect chain.
			//  OperationSet - Used to identify this call as part of a deferred batch.
			*/
			STDMETHOD(EnableEffect) (THIS_ UINT32 EffectIndex,
				UINT32 OperationSet X2DEFAULT(XAUDIO2_COMMIT_NOW)) PURE;

			/* NAME: IXAudio2Voice::DisableEffect
			// DESCRIPTION: Disables an effect in this voice's effect chain.
			//
			// ARGUMENTS:
			//  EffectIndex - Index of an effect within this voice's effect chain.
			//  OperationSet - Used to identify this call as part of a deferred batch.
			*/
			STDMETHOD(DisableEffect) (THIS_ UINT32 EffectIndex,
				UINT32 OperationSet X2DEFAULT(XAUDIO2_COMMIT_NOW)) PURE;

			/* NAME: IXAudio2Voice::GetEffectState
			// DESCRIPTION: Returns the running state of an effect.
			//
			// ARGUMENTS:
			//  EffectIndex - Index of an effect within this voice's effect chain.
			//  pEnabled - Returns the enabled/disabled state of the given effect.
			*/
			STDMETHOD_(void, GetEffectState) (THIS_ UINT32 EffectIndex, _Out_ BOOL* pEnabled) PURE;

			/* NAME: IXAudio2Voice::SetEffectParameters
			// DESCRIPTION: Sets effect-specific parameters.
			//
			// REMARKS: Unlike IXAPOParameters::SetParameters, this method may
			//          be called from any thread.  XAudio2 implements
			//          appropriate synchronization to copy the parameters to the
			//          realtime audio processing thread.
			//
			// ARGUMENTS:
			//  EffectIndex - Index of an effect within this voice's effect chain.
			//  pParameters - Pointer to an effect-specific parameters block.
			//  ParametersByteSize - Size of the pParameters array  in bytes.
			//  OperationSet - Used to identify this call as part of a deferred batch.
			*/
			STDMETHOD(SetEffectParameters) (THIS_ UINT32 EffectIndex,
				_In_reads_bytes_(ParametersByteSize) const void* pParameters,
				UINT32 ParametersByteSize,
				UINT32 OperationSet X2DEFAULT(XAUDIO2_COMMIT_NOW)) PURE;

			/* NAME: IXAudio2Voice::GetEffectParameters
			// DESCRIPTION: Obtains the current effect-specific parameters.
			//
			// ARGUMENTS:
			//  EffectIndex - Index of an effect within this voice's effect chain.
			//  pParameters - Returns the current values of the effect-specific parameters.
			//  ParametersByteSize - Size of the pParameters array in bytes.
			*/
			STDMETHOD(GetEffectParameters) (THIS_ UINT32 EffectIndex,
				_Out_writes_bytes_(ParametersByteSize) void* pParameters,
				UINT32 ParametersByteSize) PURE;

			/* NAME: IXAudio2Voice::SetFilterParameters
			// DESCRIPTION: Sets this voice's filter parameters.
			//
			// ARGUMENTS:
			//  pParameters - Pointer to the filter's parameter structure.
			//  OperationSet - Used to identify this call as part of a deferred batch.
			*/
			STDMETHOD(SetFilterParameters) (THIS_ _In_ const XAUDIO2_FILTER_PARAMETERS* pParameters,
				UINT32 OperationSet X2DEFAULT(XAUDIO2_COMMIT_NOW)) PURE;

			/* NAME: IXAudio2Voice::GetFilterParameters
			// DESCRIPTION: Returns this voice's current filter parameters.
			//
			// ARGUMENTS:
			//  pParameters - Returns the filter parameters.
			*/
			STDMETHOD_(void, GetFilterParameters) (THIS_ _Out_ XAUDIO2_FILTER_PARAMETERS* pParameters) PURE;

			/* NAME: IXAudio2Voice::SetOutputFilterParameters
			// DESCRIPTION: Sets the filter parameters on one of this voice's sends.
			//
			// ARGUMENTS:
			//  pDestinationVoice - Destination voice of the send whose filter parameters will be set.
			//  pParameters - Pointer to the filter's parameter structure.
			//  OperationSet - Used to identify this call as part of a deferred batch.
			*/
			STDMETHOD(SetOutputFilterParameters) (THIS_ _In_opt_ IXAudio2Voice* pDestinationVoice,
				_In_ const XAUDIO2_FILTER_PARAMETERS* pParameters,
				UINT32 OperationSet X2DEFAULT(XAUDIO2_COMMIT_NOW)) PURE;

			/* NAME: IXAudio2Voice::GetOutputFilterParameters
			// DESCRIPTION: Returns the filter parameters from one of this voice's sends.
			//
			// ARGUMENTS:
			//  pDestinationVoice - Destination voice of the send whose filter parameters will be read.
			//  pParameters - Returns the filter parameters.
			*/
			STDMETHOD_(void, GetOutputFilterParameters) (THIS_ _In_opt_ IXAudio2Voice* pDestinationVoice,
				_Out_ XAUDIO2_FILTER_PARAMETERS* pParameters) PURE;

			/* NAME: IXAudio2Voice::SetVolume
			// DESCRIPTION: Sets this voice's overall volume level.
			//
			// ARGUMENTS:
			//  Volume - New overall volume level to be used, as an amplitude factor.
			//  OperationSet - Used to identify this call as part of a deferred batch.
			*/
			STDMETHOD(SetVolume) (THIS_ float Volume,
				UINT32 OperationSet X2DEFAULT(XAUDIO2_COMMIT_NOW)) PURE;

			/* NAME: IXAudio2Voice::GetVolume
			// DESCRIPTION: Obtains this voice's current overall volume level.
			//
			// ARGUMENTS:
			//  pVolume: Returns the voice's current overall volume level.
			*/
			STDMETHOD_(void, GetVolume) (THIS_ _Out_ float* pVolume) PURE;

			/* NAME: IXAudio2Voice::SetChannelVolumes
			// DESCRIPTION: Sets this voice's per-channel volume levels.
			//
			// ARGUMENTS:
			//  Channels - Used to confirm the voice's channel count.
			//  pVolumes - Array of per-channel volume levels to be used.
			//  OperationSet - Used to identify this call as part of a deferred batch.
			*/
			STDMETHOD(SetChannelVolumes) (THIS_ UINT32 Channels, _In_reads_(Channels) const float* pVolumes,
				UINT32 OperationSet X2DEFAULT(XAUDIO2_COMMIT_NOW)) PURE; \

				/* NAME: IXAudio2Voice::GetChannelVolumes
				// DESCRIPTION: Returns this voice's current per-channel volume levels.
				//
				// ARGUMENTS:
				//  Channels - Used to confirm the voice's channel count.
				//  pVolumes - Returns an array of the current per-channel volume levels.
				*/
				STDMETHOD_(void, GetChannelVolumes) (THIS_ UINT32 Channels, _Out_writes_(Channels) float* pVolumes) PURE;

			/* NAME: IXAudio2Voice::SetOutputMatrix
			// DESCRIPTION: Sets the volume levels used to mix from each channel of this
			//              voice's output audio to each channel of a given destination
			//              voice's input audio.
			//
			// ARGUMENTS:
			//  pDestinationVoice - The destination voice whose mix matrix to change.
			//  SourceChannels - Used to confirm this voice's output channel count
			//   (the number of channels produced by the last effect in the chain).
			//  DestinationChannels - Confirms the destination voice's input channels.
			//  pLevelMatrix - Array of [SourceChannels * DestinationChannels] send
			//   levels.  The level used to send from source channel S to destination
			//   channel D should be in pLevelMatrix[S + SourceChannels * D].
			//  OperationSet - Used to identify this call as part of a deferred batch.
			*/
			STDMETHOD(SetOutputMatrix) (THIS_ _In_opt_ IXAudio2Voice* pDestinationVoice,
				UINT32 SourceChannels, UINT32 DestinationChannels,
				_In_reads_(SourceChannels * DestinationChannels) const float* pLevelMatrix,
				UINT32 OperationSet X2DEFAULT(XAUDIO2_COMMIT_NOW)) PURE;

			/* NAME: IXAudio2Voice::GetOutputMatrix
			// DESCRIPTION: Obtains the volume levels used to send each channel of this
			//              voice's output audio to each channel of a given destination
			//              voice's input audio.
			//
			// ARGUMENTS:
			//  pDestinationVoice - The destination voice whose mix matrix to obtain.
			//  SourceChannels - Used to confirm this voice's output channel count
			//   (the number of channels produced by the last effect in the chain).
			//  DestinationChannels - Confirms the destination voice's input channels.
			//  pLevelMatrix - Array of send levels, as above.
			*/
			STDMETHOD_(void, GetOutputMatrix) (THIS_ _In_opt_ IXAudio2Voice* pDestinationVoice,
				UINT32 SourceChannels, UINT32 DestinationChannels,
				_Out_writes_(SourceChannels * DestinationChannels) float* pLevelMatrix) PURE;
			/* NAME: IXAudio2Voice::DestroyVoice
			// DESCRIPTION: Destroys this voice, stopping it if necessary and removing
			//              it from the XAudio2 graph.
			*/
			STDMETHOD_(void, DestroyVoice) (THIS) PURE;
		};

		typedef struct XAUDIO2_BUFFER
		{
			UINT32 Flags;                       // Either 0 or XAUDIO2_END_OF_STREAM.
			UINT32 AudioBytes;                  // Size of the audio data buffer in bytes.
			const BYTE* pAudioData;             // Pointer to the audio data buffer.
			UINT32 PlayBegin;                   // First sample in this buffer to be played.
			UINT32 PlayLength;                  // Length of the region to be played in samples,
												//  or 0 to play the whole buffer.
			UINT32 LoopBegin;                   // First sample of the region to be looped.
			UINT32 LoopLength;                  // Length of the desired loop region in samples,
												//  or 0 to loop the entire buffer.
			UINT32 LoopCount;                   // Number of times to repeat the loop region,
												//  or XAUDIO2_LOOP_INFINITE to loop forever.
			void* pContext;                     // Context value to be passed back in callbacks.
		} XAUDIO2_BUFFER;

		typedef struct XAUDIO2_BUFFER_WMA
		{
			const UINT32* pDecodedPacketCumulativeBytes; // Decoded packet's cumulative size array.
														 //  Each element is the number of bytes accumulated
														 //  when the corresponding XWMA packet is decoded in
														 //  order.  The array must have PacketCount elements.
			UINT32 PacketCount;                          // Number of XWMA packets submitted. Must be >= 1 and
														 //  divide evenly into XAUDIO2_BUFFER.AudioBytes.
		} XAUDIO2_BUFFER_WMA;

		typedef struct XAUDIO2_VOICE_STATE
		{
			void* pCurrentBufferContext;        // The pContext value provided in the XAUDIO2_BUFFER
												//  that is currently being processed, or NULL if
												//  there are no buffers in the queue.
			UINT32 BuffersQueued;               // Number of buffers currently queued on the voice
												//  (including the one that is being processed).
			UINT64 SamplesPlayed;               // Total number of samples produced by the voice since
												//  it began processing the current audio stream.
												//  If XAUDIO2_VOICE_NOSAMPLESPLAYED is specified
												//  in the call to IXAudio2SourceVoice::GetState,
												//  this member will not be calculated, saving CPU.
		} XAUDIO2_VOICE_STATE;

		struct IXAudio2SourceVoice : IXAudio2Voice
		{
			// NAME: IXAudio2SourceVoice::Start
			// DESCRIPTION: Makes this voice start consuming and processing audio.
			//
			// ARGUMENTS:
			//  Flags - Flags controlling how the voice should be started.
			//  OperationSet - Used to identify this call as part of a deferred batch.
			//
			STDMETHOD(Start) (THIS_ UINT32 Flags X2DEFAULT(0), UINT32 OperationSet X2DEFAULT(XAUDIO2_COMMIT_NOW)) PURE;

			// NAME: IXAudio2SourceVoice::Stop
			// DESCRIPTION: Makes this voice stop consuming audio.
			//
			// ARGUMENTS:
			//  Flags - Flags controlling how the voice should be stopped.
			//  OperationSet - Used to identify this call as part of a deferred batch.
			//
			STDMETHOD(Stop) (THIS_ UINT32 Flags X2DEFAULT(0), UINT32 OperationSet X2DEFAULT(XAUDIO2_COMMIT_NOW)) PURE;

			// NAME: IXAudio2SourceVoice::SubmitSourceBuffer
			// DESCRIPTION: Adds a new audio buffer to this voice's input queue.
			//
			// ARGUMENTS:
			//  pBuffer - Pointer to the buffer structure to be queued.
			//  pBufferWMA - Additional structure used only when submitting XWMA data.
			//
			STDMETHOD(SubmitSourceBuffer) (THIS_ _In_ const XAUDIO2_BUFFER* pBuffer, _In_opt_ const XAUDIO2_BUFFER_WMA* pBufferWMA X2DEFAULT(NULL)) PURE;

			// NAME: IXAudio2SourceVoice::FlushSourceBuffers
			// DESCRIPTION: Removes all pending audio buffers from this voice's queue.
			//
			STDMETHOD(FlushSourceBuffers) (THIS) PURE;

			// NAME: IXAudio2SourceVoice::Discontinuity
			// DESCRIPTION: Notifies the voice of an intentional break in the stream of
			//              audio buffers (e.g. the end of a sound), to prevent XAudio2
			//              from interpreting an empty buffer queue as a glitch.
			//
			STDMETHOD(Discontinuity) (THIS) PURE;

			// NAME: IXAudio2SourceVoice::ExitLoop
			// DESCRIPTION: Breaks out of the current loop when its end is reached.
			//
			// ARGUMENTS:
			//  OperationSet - Used to identify this call as part of a deferred batch.
			//
			STDMETHOD(ExitLoop) (THIS_ UINT32 OperationSet X2DEFAULT(XAUDIO2_COMMIT_NOW)) PURE;

			// NAME: IXAudio2SourceVoice::GetState
			// DESCRIPTION: Returns the number of buffers currently queued on this voice,
			//              the pContext value associated with the currently processing
			//              buffer (if any), and other voice state information.
			//
			// ARGUMENTS:
			//  pVoiceState - Returns the state information.
			//  Flags - Flags controlling what voice state is returned.
			//
			STDMETHOD_(void, GetState) (THIS_ _Out_ XAUDIO2_VOICE_STATE* pVoiceState, UINT32 Flags X2DEFAULT(0)) PURE;

			// NAME: IXAudio2SourceVoice::SetFrequencyRatio
			// DESCRIPTION: Sets this voice's frequency adjustment, i.e. its pitch.
			//
			// ARGUMENTS:
			//  Ratio - Frequency change, expressed as source frequency / target frequency.
			//  OperationSet - Used to identify this call as part of a deferred batch.
			//
			STDMETHOD(SetFrequencyRatio) (THIS_ float Ratio,
				UINT32 OperationSet X2DEFAULT(XAUDIO2_COMMIT_NOW)) PURE;

			// NAME: IXAudio2SourceVoice::GetFrequencyRatio
			// DESCRIPTION: Returns this voice's current frequency adjustment ratio.
			//
			// ARGUMENTS:
			//  pRatio - Returns the frequency adjustment.
			//
			STDMETHOD_(void, GetFrequencyRatio) (THIS_ _Out_ float* pRatio) PURE;

			// NAME: IXAudio2SourceVoice::SetSourceSampleRate
			// DESCRIPTION: Reconfigures this voice to treat its source data as being
			//              at a different sample rate than the original one specified
			//              in CreateSourceVoice's pSourceFormat argument.
			//
			// ARGUMENTS:
			//  UINT32 - The intended sample rate of further submitted source data.
			//
			STDMETHOD(SetSourceSampleRate) (THIS_ UINT32 NewSourceSampleRate) PURE;
		};

		struct IXAudio2SourceVoice1 : IXAudio2Voice
		{
			// NAME: IXAudio2SourceVoice::Start
			// DESCRIPTION: Makes this voice start consuming and processing audio.
			//
			// ARGUMENTS:
			//  Flags - Flags controlling how the voice should be started.
			//  OperationSet - Used to identify this call as part of a deferred batch.
			//
			STDMETHOD(Start) (THIS_ UINT32 Flags X2DEFAULT(0), UINT32 OperationSet X2DEFAULT(XAUDIO2_COMMIT_NOW)) PURE;

			// NAME: IXAudio2SourceVoice::Stop
			// DESCRIPTION: Makes this voice stop consuming audio.
			//
			// ARGUMENTS:
			//  Flags - Flags controlling how the voice should be stopped.
			//  OperationSet - Used to identify this call as part of a deferred batch.
			//
			STDMETHOD(Stop) (THIS_ UINT32 Flags X2DEFAULT(0), UINT32 OperationSet X2DEFAULT(XAUDIO2_COMMIT_NOW)) PURE;

			// NAME: IXAudio2SourceVoice::SubmitSourceBuffer
			// DESCRIPTION: Adds a new audio buffer to this voice's input queue.
			//
			// ARGUMENTS:
			//  pBuffer - Pointer to the buffer structure to be queued.
			//  pBufferWMA - Additional structure used only when submitting XWMA data.
			//
			STDMETHOD(SubmitSourceBuffer) (THIS_ const XAUDIO2_BUFFER* pBuffer, const XAUDIO2_BUFFER_WMA* pBufferWMA X2DEFAULT(NULL)) PURE;

			// NAME: IXAudio2SourceVoice::FlushSourceBuffers
			// DESCRIPTION: Removes all pending audio buffers from this voice's queue.
			//
			STDMETHOD(FlushSourceBuffers) (THIS) PURE;

			// NAME: IXAudio2SourceVoice::Discontinuity
			// DESCRIPTION: Notifies the voice of an intentional break in the stream of
			//              audio buffers (e.g. the end of a sound), to prevent XAudio2
			//              from interpreting an empty buffer queue as a glitch.
			//
			STDMETHOD(Discontinuity) (THIS) PURE;

			// NAME: IXAudio2SourceVoice::ExitLoop
			// DESCRIPTION: Breaks out of the current loop when its end is reached.
			//
			// ARGUMENTS:
			//  OperationSet - Used to identify this call as part of a deferred batch.
			//
			STDMETHOD(ExitLoop) (THIS_ UINT32 OperationSet X2DEFAULT(XAUDIO2_COMMIT_NOW)) PURE;

			// NAME: IXAudio2SourceVoice::GetState
			// DESCRIPTION: Returns the number of buffers currently queued on this voice,
			//              the pContext value associated with the currently processing
			//              buffer (if any), and other voice state information.
			//
			// ARGUMENTS:
			//  pVoiceState - Returns the state information.
			//
			STDMETHOD_(void, GetState) (THIS_ XAUDIO2_VOICE_STATE* pVoiceState) PURE;

			// NAME: IXAudio2SourceVoice::SetFrequencyRatio
			// DESCRIPTION: Sets this voice's frequency adjustment, i.e. its pitch.
			//
			// ARGUMENTS:
			//  Ratio - Frequency change, expressed as source frequency / target frequency.
			//  OperationSet - Used to identify this call as part of a deferred batch.
			//
			STDMETHOD(SetFrequencyRatio) (THIS_ float Ratio,
				UINT32 OperationSet X2DEFAULT(XAUDIO2_COMMIT_NOW)) PURE;

			// NAME: IXAudio2SourceVoice::GetFrequencyRatio
			// DESCRIPTION: Returns this voice's current frequency adjustment ratio.
			//
			// ARGUMENTS:
			//  pRatio - Returns the frequency adjustment.
			//
			STDMETHOD_(void, GetFrequencyRatio) (THIS_ float* pRatio) PURE;

			// NAME: IXAudio2SourceVoice::SetSourceSampleRate
			// DESCRIPTION: Reconfigures this voice to treat its source data as being
			//              at a different sample rate than the original one specified
			//              in CreateSourceVoice's pSourceFormat argument.
			//
			// ARGUMENTS:
			//  UINT32 - The intended sample rate of further submitted source data.
			//
			STDMETHOD(SetSourceSampleRate) (THIS_ UINT32 NewSourceSampleRate) PURE;
		};

		struct IXAudio2SubmixVoice : IXAudio2SourceVoice
		{
		};

		typedef struct tWAVEFORMATEX
		{
			WORD        wFormatTag;         /* format type */
			WORD        nChannels;          /* number of channels (i.e. mono, stereo...) */
			DWORD       nSamplesPerSec;     /* sample rate */
			DWORD       nAvgBytesPerSec;    /* for buffer estimation */
			WORD        nBlockAlign;        /* block size of data */
			WORD        wBitsPerSample;     /* number of bits per sample of mono data */
			WORD        cbSize;             /* the count in bytes of the size of */
											/* extra information (after cbSize) */
		} WAVEFORMATEX;

#define XAUDIO2_MAX_BUFFER_BYTES        0x80000000    // Maximum bytes allowed in a source buffer
#define XAUDIO2_MAX_QUEUED_BUFFERS      64            // Maximum buffers allowed in a voice queue
#define XAUDIO2_MAX_BUFFERS_SYSTEM      2             // Maximum buffers allowed for system threads (Xbox 360 only)
#define XAUDIO2_MAX_AUDIO_CHANNELS      64            // Maximum channels in an audio stream
#define XAUDIO2_MIN_SAMPLE_RATE         1000          // Minimum audio sample rate supported
#define XAUDIO2_MAX_SAMPLE_RATE         200000        // Maximum audio sample rate supported
#define XAUDIO2_MAX_VOLUME_LEVEL        16777216.0f   // Maximum acceptable volume level (2^24)
#define XAUDIO2_MIN_FREQ_RATIO          (1/1024.0f)   // Minimum SetFrequencyRatio argument
#define XAUDIO2_MAX_FREQ_RATIO          1024.0f       // Maximum MaxFrequencyRatio argument
#define XAUDIO2_DEFAULT_FREQ_RATIO      2.0f          // Default MaxFrequencyRatio argument
#define XAUDIO2_MAX_FILTER_ONEOVERQ     1.5f          // Maximum XAUDIO2_FILTER_PARAMETERS.OneOverQ
#define XAUDIO2_MAX_FILTER_FREQUENCY    1.0f          // Maximum XAUDIO2_FILTER_PARAMETERS.Frequency
#define XAUDIO2_MAX_LOOP_COUNT          254           // Maximum non-infinite XAUDIO2_BUFFER.LoopCount
#define XAUDIO2_MAX_INSTANCES           8             // Maximum simultaneous XAudio2 objects on Xbox 360

#define XAUDIO2_DEBUG_ENGINE                  0x0001    // Used in XAudio2Create
#define XAUDIO2_VOICE_NOPITCH                 0x0002    // Used in IXAudio2::CreateSourceVoice
#define XAUDIO2_VOICE_NOSRC                   0x0004    // Used in IXAudio2::CreateSourceVoice
#define XAUDIO2_VOICE_USEFILTER               0x0008    // Used in IXAudio2::CreateSource/SubmixVoice
#define XAUDIO2_PLAY_TAILS                    0x0020    // Used in IXAudio2SourceVoice::Stop
#define XAUDIO2_END_OF_STREAM                 0x0040    // Used in XAUDIO2_BUFFER.Flags
#define XAUDIO2_SEND_USEFILTER                0x0080    // Used in XAUDIO2_SEND_DESCRIPTOR.Flags
#define XAUDIO2_VOICE_NOSAMPLESPLAYED         0x0100    // Used in IXAudio2SourceVoice::GetState
#define XAUDIO2_STOP_ENGINE_WHEN_IDLE         0x2000    // Used in XAudio2Create to force the engine to Stop when no source voices are Started, and Start when a voice is Started
#define XAUDIO2_1024_QUANTUM                  0x8000    // Used in XAudio2Create to specify nondefault processing quantum of 21.33 ms (1024 samples at 48KHz)
#define XAUDIO2_NO_VIRTUAL_AUDIO_CLIENT          0x10000   // Used in CreateMasteringVoice to create a virtual audio client

#define WAVE_FORMAT_PCM 1

		struct IXAudio2MasteringVoice : IXAudio2Voice
		{
			// NAME: IXAudio2MasteringVoice::GetChannelMask
			// DESCRIPTION: Returns the channel mask for this voice
			//
			// ARGUMENTS:
			//  pChannelMask - returns the channel mask for this voice.  This corresponds
			//                 to the dwChannelMask member of WAVEFORMATEXTENSIBLE.
			//
			STDMETHOD(GetChannelMask) (THIS_ _Out_ DWORD* pChannelmask) PURE;
		};

		struct IXAudio2VoiceCallback
		{
			// Called just before this voice's processing pass begins.
			STDMETHOD_(void, OnVoiceProcessingPassStart) (THIS_ UINT32 BytesRequired) PURE;

			// Called just after this voice's processing pass ends.
			STDMETHOD_(void, OnVoiceProcessingPassEnd) (THIS) PURE;

			// Called when this voice has just finished playing a buffer stream
			// (as marked with the XAUDIO2_END_OF_STREAM flag on the last buffer).
			STDMETHOD_(void, OnStreamEnd) (THIS) PURE;

			// Called when this voice is about to start processing a new buffer.
			STDMETHOD_(void, OnBufferStart) (THIS_ void* pBufferContext) PURE;

			// Called when this voice has just finished processing a buffer.
			// The buffer can now be reused or destroyed.
			STDMETHOD_(void, OnBufferEnd) (THIS_ void* pBufferContext) PURE;

			// Called when this voice has just reached the end position of a loop.
			STDMETHOD_(void, OnLoopEnd) (THIS_ void* pBufferContext) PURE;

			// Called in the event of a critical error during voice processing,
			// such as a failing xAPO or an error from the hardware XMA decoder.
			// The voice may have to be destroyed and re-created to recover from
			// the error.  The callback arguments report which buffer was being
			// processed when the error occurred, and its HRESULT code.
			STDMETHOD_(void, OnVoiceError) (THIS_ void* pBufferContext, HRESULT Error) PURE;
		};

		typedef enum _AUDIO_STREAM_CATEGORY
		{
			AudioCategory_Other = 0,
			AudioCategory_ForegroundOnlyMedia,
			AudioCategory_BackgroundCapableMedia,
			AudioCategory_Communications,
			AudioCategory_Alerts,
			AudioCategory_SoundEffects,
			AudioCategory_GameEffects,
			AudioCategory_GameMedia,
		} AUDIO_STREAM_CATEGORY;

		typedef struct XAUDIO2_PERFORMANCE_DATA
		{
			// CPU usage information
			UINT64 AudioCyclesSinceLastQuery;   // CPU cycles spent on audio processing since the
												//  last call to StartEngine or GetPerformanceData.
			UINT64 TotalCyclesSinceLastQuery;   // Total CPU cycles elapsed since the last call
												//  (only counts the CPU XAudio2 is running on).
			UINT32 MinimumCyclesPerQuantum;     // Fewest CPU cycles spent processing any one
												//  audio quantum since the last call.
			UINT32 MaximumCyclesPerQuantum;     // Most CPU cycles spent processing any one
												//  audio quantum since the last call.

												// Memory usage information
			UINT32 MemoryUsageInBytes;          // Total heap space currently in use.

												// Audio latency and glitching information
			UINT32 CurrentLatencyInSamples;     // Minimum delay from when a sample is read from a
												//  source buffer to when it reaches the speakers.
			UINT32 GlitchesSinceEngineStarted;  // Audio dropouts since the engine was started.

												// Data about XAudio2's current workload
			UINT32 ActiveSourceVoiceCount;      // Source voices currently playing.
			UINT32 TotalSourceVoiceCount;       // Source voices currently existing.
			UINT32 ActiveSubmixVoiceCount;      // Submix voices currently playing/existing.

			UINT32 ActiveResamplerCount;        // Resample xAPOs currently active.
			UINT32 ActiveMatrixMixCount;        // MatrixMix xAPOs currently active.

												// Usage of the hardware XMA decoder (Xbox 360 only)
			UINT32 ActiveXmaSourceVoices;       // Number of source voices decoding XMA data.
			UINT32 ActiveXmaStreams;            // A voice can use more than one XMA stream.
		} XAUDIO2_PERFORMANCE_DATA;

		typedef struct XAUDIO2_DEBUG_CONFIGURATION
		{
			UINT32 TraceMask;                   // Bitmap of enabled debug message types.
			UINT32 BreakMask;                   // Message types that will break into the debugger.
			BOOL LogThreadID;                   // Whether to log the thread ID with each message.
			BOOL LogFileline;                   // Whether to log the source file and line number.
			BOOL LogFunctionName;               // Whether to log the function name.
			BOOL LogTiming;                     // Whether to log message timestamps.
		} XAUDIO2_DEBUG_CONFIGURATION;

		struct IXAudio2 : IUnknown
		{
			// NAME: IXAudio2::RegisterForCallbacks
			// DESCRIPTION: Adds a new client to receive XAudio2's engine callbacks.
			//
			// ARGUMENTS:
			//  pCallback - Callback interface to be called during each processing pass.
			//
			STDMETHOD(RegisterForCallbacks) (_In_ IXAudio2EngineCallback* pCallback) PURE;

			// NAME: IXAudio2::UnregisterForCallbacks
			// DESCRIPTION: Removes an existing receiver of XAudio2 engine callbacks.
			//
			// ARGUMENTS:
			//  pCallback - Previously registered callback interface to be removed.
			//
			STDMETHOD_(void, UnregisterForCallbacks) (_In_ IXAudio2EngineCallback* pCallback) PURE;

			// NAME: IXAudio2::CreateSourceVoice
			// DESCRIPTION: Creates and configures a source voice.
			//
			// ARGUMENTS:
			//  ppSourceVoice - Returns the new object's IXAudio2SourceVoice interface.
			//  pSourceFormat - Format of the audio that will be fed to the voice.
			//  Flags - XAUDIO2_VOICE flags specifying the source voice's behavior.
			//  MaxFrequencyRatio - Maximum SetFrequencyRatio argument to be allowed.
			//  pCallback - Optional pointer to a client-provided callback interface.
			//  pSendList - Optional list of voices this voice should send audio to.
			//  pEffectChain - Optional list of effects to apply to the audio data.
			//
			STDMETHOD(CreateSourceVoice) (THIS_ _Outptr_ IXAudio2SourceVoice** ppSourceVoice,
				_In_ const WAVEFORMATEX* pSourceFormat,
				UINT32 Flags X2DEFAULT(0),
				float MaxFrequencyRatio X2DEFAULT(XAUDIO2_DEFAULT_FREQ_RATIO),
				_In_opt_ IXAudio2VoiceCallback* pCallback X2DEFAULT(NULL),
				_In_opt_ const XAUDIO2_VOICE_SENDS* pSendList X2DEFAULT(NULL),
				_In_opt_ const XAUDIO2_EFFECT_CHAIN* pEffectChain X2DEFAULT(NULL)) PURE;

			// NAME: IXAudio2::CreateSubmixVoice
			// DESCRIPTION: Creates and configures a submix voice.
			//
			// ARGUMENTS:
			//  ppSubmixVoice - Returns the new object's IXAudio2SubmixVoice interface.
			//  InputChannels - Number of channels in this voice's input audio data.
			//  InputSampleRate - Sample rate of this voice's input audio data.
			//  Flags - XAUDIO2_VOICE flags specifying the submix voice's behavior.
			//  ProcessingStage - Arbitrary number that determines the processing order.
			//  pSendList - Optional list of voices this voice should send audio to.
			//  pEffectChain - Optional list of effects to apply to the audio data.
			//
			STDMETHOD(CreateSubmixVoice) (THIS_ _Outptr_ IXAudio2SubmixVoice** ppSubmixVoice,
				UINT32 InputChannels, UINT32 InputSampleRate,
				UINT32 Flags X2DEFAULT(0), UINT32 ProcessingStage X2DEFAULT(0),
				_In_opt_ const XAUDIO2_VOICE_SENDS* pSendList X2DEFAULT(NULL),
				_In_opt_ const XAUDIO2_EFFECT_CHAIN* pEffectChain X2DEFAULT(NULL)) PURE;


			// NAME: IXAudio2::CreateMasteringVoice
			// DESCRIPTION: Creates and configures a mastering voice.
			//
			// ARGUMENTS:
			//  ppMasteringVoice - Returns the new object's IXAudio2MasteringVoice interface.
			//  InputChannels - Number of channels in this voice's input audio data.
			//  InputSampleRate - Sample rate of this voice's input audio data.
			//  Flags - XAUDIO2_VOICE flags specifying the mastering voice's behavior.
			//  szDeviceId - Identifier of the device to receive the output audio.
			//  pEffectChain - Optional list of effects to apply to the audio data.
			//  StreamCategory - The audio stream category to use for this mastering voice
			//
			STDMETHOD(CreateMasteringVoice) (THIS_ _Outptr_ IXAudio2MasteringVoice** ppMasteringVoice,
				UINT32 InputChannels X2DEFAULT(XAUDIO2_DEFAULT_CHANNELS),
				UINT32 InputSampleRate X2DEFAULT(XAUDIO2_DEFAULT_SAMPLERATE),
				UINT32 Flags X2DEFAULT(0), _In_opt_z_ void* szDeviceId X2DEFAULT(NULL),
				_In_opt_ const XAUDIO2_EFFECT_CHAIN* pEffectChain X2DEFAULT(NULL),
				_In_ AUDIO_STREAM_CATEGORY StreamCategory X2DEFAULT(AudioCategory_GameEffects)) PURE;

			// NAME: IXAudio2::StartEngine
			// DESCRIPTION: Creates and starts the audio processing thread.
			//
			STDMETHOD(StartEngine) (THIS) PURE;

			// NAME: IXAudio2::StopEngine
			// DESCRIPTION: Stops and destroys the audio processing thread.
			//
			STDMETHOD_(void, StopEngine) (THIS) PURE;

			// NAME: IXAudio2::CommitChanges
			// DESCRIPTION: Atomically applies a set of operations previously tagged
			//              with a given identifier.
			//
			// ARGUMENTS:
			//  OperationSet - Identifier of the set of operations to be applied.
			//
			STDMETHOD(CommitChanges) (THIS_ UINT32 OperationSet) PURE;

			// NAME: IXAudio2::GetPerformanceData
			// DESCRIPTION: Returns current resource usage details: memory, CPU, etc.
			//
			// ARGUMENTS:
			//  pPerfData - Returns the performance data structure.
			//
			STDMETHOD_(void, GetPerformanceData) (THIS_ _Out_ XAUDIO2_PERFORMANCE_DATA* pPerfData) PURE;

			// NAME: IXAudio2::SetDebugConfiguration
			// DESCRIPTION: Configures XAudio2's debug output (in debug builds only).
			//
			// ARGUMENTS:
			//  pDebugConfiguration - Structure describing the debug output behavior.
			//  pReserved - Optional parameter; must be NULL.
			//
			STDMETHOD_(void, SetDebugConfiguration) (THIS_ _In_opt_ const XAUDIO2_DEBUG_CONFIGURATION* pDebugConfiguration,
				_Reserved_ void* pReserved X2DEFAULT(NULL)) PURE;
		};

		struct IXAudio2_7 : IUnknown
		{

			// NAME: IXAudio2::GetDeviceCount
			// DESCRIPTION: Returns the number of audio output devices available.
			//
			// ARGUMENTS:
			//  pCount - Returns the device count.
			//
			STDMETHOD(GetDeviceCount) (THIS_ UINT32* pCount) PURE;

			// NAME: IXAudio2::GetDeviceDetails
			// DESCRIPTION: Returns information about the device with the given index.
			//
			// ARGUMENTS:
			//  Index - Index of the device to be queried.
			//  pDeviceDetails - Returns the device details.
			//
			STDMETHOD(GetDeviceDetails) (THIS_ UINT32 Index, void* pDeviceDetails) PURE;

			// NAME: IXAudio2::Initialize
			// DESCRIPTION: Sets global XAudio2 parameters and prepares it for use.
			//
			// ARGUMENTS:
			//  Flags - Flags specifying the XAudio2 object's behavior.  Currently unused.
			//  XAudio2Processor - An XAUDIO2_PROCESSOR enumeration value that specifies
			//  the hardware thread (Xbox) or processor (Windows) that XAudio2 will use.
			//  The enumeration values are platform-specific; platform-independent code
			//  can use XAUDIO2_DEFAULT_PROCESSOR to use the default on each platform.
			//
			STDMETHOD(Initialize) (THIS_ UINT32 Flags X2DEFAULT(0), UINT32 XAudio2Processor = 0x00000001) PURE;

			// NAME: IXAudio2::RegisterForCallbacks
			// DESCRIPTION: Adds a new client to receive XAudio2's engine callbacks.
			//
			// ARGUMENTS:
			//  pCallback - Callback interface to be called during each processing pass.
			//
			STDMETHOD(RegisterForCallbacks) (_In_ IXAudio2EngineCallback* pCallback) PURE;

			// NAME: IXAudio2::UnregisterForCallbacks
			// DESCRIPTION: Removes an existing receiver of XAudio2 engine callbacks.
			//
			// ARGUMENTS:
			//  pCallback - Previously registered callback interface to be removed.
			//
			STDMETHOD_(void, UnregisterForCallbacks) (_In_ IXAudio2EngineCallback* pCallback) PURE;

			// NAME: IXAudio2::CreateSourceVoice
			// DESCRIPTION: Creates and configures a source voice.
			//
			// ARGUMENTS:
			//  ppSourceVoice - Returns the new object's IXAudio2SourceVoice interface.
			//  pSourceFormat - Format of the audio that will be fed to the voice.
			//  Flags - XAUDIO2_VOICE flags specifying the source voice's behavior.
			//  MaxFrequencyRatio - Maximum SetFrequencyRatio argument to be allowed.
			//  pCallback - Optional pointer to a client-provided callback interface.
			//  pSendList - Optional list of voices this voice should send audio to.
			//  pEffectChain - Optional list of effects to apply to the audio data.
			//
			STDMETHOD(CreateSourceVoice) (THIS_ _Outptr_ IXAudio2SourceVoice** ppSourceVoice,
				_In_ const WAVEFORMATEX* pSourceFormat,
				UINT32 Flags X2DEFAULT(0),
				float MaxFrequencyRatio X2DEFAULT(XAUDIO2_DEFAULT_FREQ_RATIO),
				_In_opt_ IXAudio2VoiceCallback* pCallback X2DEFAULT(NULL),
				_In_opt_ const XAUDIO2_VOICE_SENDS* pSendList X2DEFAULT(NULL),
				_In_opt_ const XAUDIO2_EFFECT_CHAIN* pEffectChain X2DEFAULT(NULL)) PURE;

			// NAME: IXAudio2::CreateSubmixVoice
			// DESCRIPTION: Creates and configures a submix voice.
			//
			// ARGUMENTS:
			//  ppSubmixVoice - Returns the new object's IXAudio2SubmixVoice interface.
			//  InputChannels - Number of channels in this voice's input audio data.
			//  InputSampleRate - Sample rate of this voice's input audio data.
			//  Flags - XAUDIO2_VOICE flags specifying the submix voice's behavior.
			//  ProcessingStage - Arbitrary number that determines the processing order.
			//  pSendList - Optional list of voices this voice should send audio to.
			//  pEffectChain - Optional list of effects to apply to the audio data.
			//
			STDMETHOD(CreateSubmixVoice) (THIS_ _Outptr_ IXAudio2Voice** ppSubmixVoice,
				UINT32 InputChannels, UINT32 InputSampleRate,
				UINT32 Flags X2DEFAULT(0), UINT32 ProcessingStage X2DEFAULT(0),
				_In_opt_ const XAUDIO2_VOICE_SENDS* pSendList X2DEFAULT(NULL),
				_In_opt_ const XAUDIO2_EFFECT_CHAIN* pEffectChain X2DEFAULT(NULL)) PURE;


			// NAME: IXAudio2::CreateMasteringVoice
			// DESCRIPTION: Creates and configures a mastering voice.
			//
			// ARGUMENTS:
			//  ppMasteringVoice - Returns the new object's IXAudio2MasteringVoice interface.
			//  InputChannels - Number of channels in this voice's input audio data.
			//  InputSampleRate - Sample rate of this voice's input audio data.
			//  Flags - XAUDIO2_VOICE flags specifying the mastering voice's behavior.
			//  DeviceIndex - Identifier of the device to receive the output audio.
			//  pEffectChain - Optional list of effects to apply to the audio data.
			//
			STDMETHOD(CreateMasteringVoice) (THIS_ IXAudio2MasteringVoice** ppMasteringVoice,
				UINT32 InputChannels X2DEFAULT(XAUDIO2_DEFAULT_CHANNELS),
				UINT32 InputSampleRate X2DEFAULT(XAUDIO2_DEFAULT_SAMPLERATE),
				UINT32 Flags X2DEFAULT(0), UINT32 DeviceIndex X2DEFAULT(0),
				const XAUDIO2_EFFECT_CHAIN* pEffectChain X2DEFAULT(NULL)) PURE;

			// NAME: IXAudio2::StartEngine
			// DESCRIPTION: Creates and starts the audio processing thread.
			//
			STDMETHOD(StartEngine) (THIS) PURE;

			// NAME: IXAudio2::StopEngine
			// DESCRIPTION: Stops and destroys the audio processing thread.
			//
			STDMETHOD_(void, StopEngine) (THIS) PURE;

			// NAME: IXAudio2::CommitChanges
			// DESCRIPTION: Atomically applies a set of operations previously tagged
			//              with a given identifier.
			//
			// ARGUMENTS:
			//  OperationSet - Identifier of the set of operations to be applied.
			//
			STDMETHOD(CommitChanges) (THIS_ UINT32 OperationSet) PURE;

			// NAME: IXAudio2::GetPerformanceData
			// DESCRIPTION: Returns current resource usage details: memory, CPU, etc.
			//
			// ARGUMENTS:
			//  pPerfData - Returns the performance data structure.
			//
			STDMETHOD_(void, GetPerformanceData) (THIS_ _Out_ XAUDIO2_PERFORMANCE_DATA* pPerfData) PURE;

			// NAME: IXAudio2::SetDebugConfiguration
			// DESCRIPTION: Configures XAudio2's debug output (in debug builds only).
			//
			// ARGUMENTS:
			//  pDebugConfiguration - Structure describing the debug output behavior.
			//  pReserved - Optional parameter; must be NULL.
			//
			STDMETHOD_(void, SetDebugConfiguration) (THIS_ _In_opt_ const XAUDIO2_DEBUG_CONFIGURATION* pDebugConfiguration,
				_Reserved_ void* pReserved X2DEFAULT(NULL)) PURE;
		};

#pragma pack(pop)

#define AUDIO_CHANNELS 2

		//Windows 7 has no XAudio by default , it is taken from DX sdk and its loaded using COM...
		bool xnAudioWindows7Hacks = false;

		void* xnHrtfApoLib;

		typedef IID *LPIID;
		IID xnHrtfParamsIID;

#ifdef WINDOWS_DESKTOP
		void* xnXAudioLib;
		typedef /* [unique] */ IUnknown *LPUNKNOWN;
		typedef void *LPVOID;
		typedef GUID IID;
#define REFCLSID const IID &
		extern HRESULT __stdcall CoCreateInstance(_In_ REFCLSID rclsid, _In_opt_ LPUNKNOWN pUnkOuter, _In_ DWORD dwClsContext, _In_ REFIID riid, LPVOID* ppv);

		typedef IID *LPIID;
		typedef wchar_t WCHAR;
		typedef WCHAR OLECHAR;
		typedef /* [string] */  const OLECHAR *LPCOLESTR;

		extern HRESULT __stdcall IIDFromString(_In_ LPCOLESTR lpsz, _Out_ LPIID lpiid);
#endif

		DLL_EXPORT_API npBool xnAudioInit()
		{
			CoInitializeEx(NULL, 0x0);

			//On Windows, not desktop platforms we are using XAudio2.lib
			//On Windows Desktop it's more complicated specially because Windows 7, we try from 2.9 to 2.7 (COM loaded)

#ifdef WINDOWS_DESKTOP
			xnXAudioLib = LoadDynamicLibrary("XAudio2_9"); //win10+
			xnHrtfApoLib = LoadDynamicLibrary("HrtfApo"); //win10+
			
			if (!xnXAudioLib) xnXAudioLib = LoadDynamicLibrary("XAudio2_8"); //win8+
			
			if(xnXAudioLib)
			{
				XAudio2CreateFunc = (XAudio2CreatePtr)GetSymbolAddress(xnXAudioLib, "XAudio2Create");
				if (!XAudio2CreateFunc) return false;

				if(xnHrtfApoLib)
				{
					CreateHrtfApoFunc = (CreateHrtfApoPtr)GetSymbolAddress(xnHrtfApoLib, "CreateHrtfApo");
					IIDFromString(L"{15B3CD66-E9DE-4464-B6E6-2BC3CF63D455}", &xnHrtfParamsIID);
				}
			}
			else
			{
				xnAudioWindows7Hacks = true;

				//also load X3daudio
				xnXAudioLib = LoadDynamicLibrary("X3DAudio1_7");
				if (!xnXAudioLib) return false;
            }
			
			if (!xnXAudioLib) return false;

			X3DAudioInitializeFunc = (X3DAudioInitializePtr)GetSymbolAddress(xnXAudioLib, "X3DAudioInitialize");
			if (!X3DAudioInitializeFunc) return false;
			X3DAudioCalculateFunc = (X3DAudioCalculatePtr)GetSymbolAddress(xnXAudioLib, "X3DAudioCalculate");
			if (!X3DAudioCalculateFunc) return false;
#endif

			return true;
		}

		struct xnAudioDevice
		{
			IXAudio2* x_audio2_;
			IXAudio2_7* x_audio2_7_;
			X3DAUDIO_HANDLE x3_audio_;
			IXAudio2MasteringVoice* mastering_voice_;
			bool hrtf_;
		};

		struct xnAudioSource;
		DLL_EXPORT_API void xnAudioSourceStop(xnAudioSource* source);

		struct xnAudioBuffer
		{
			XAUDIO2_BUFFER buffer_;
			int length_;
			BufferType type_;
		};

		struct xnAudioListener
		{
			xnAudioDevice* device_;
			X3DAUDIO_LISTENER listener_;
			Matrix worldTransform_;
		};

		enum xnAudioDeviceFlags
		{
			xnAudioDeviceFlagsNone,
			xnAudioDeviceFlagsHrtf
		};

		DLL_EXPORT_API xnAudioDevice* xnAudioCreate(void* deviceName, xnAudioDeviceFlags flags) //Device name is actually LPCWSTR, on C# side encoding is Unicode!
		{
			xnAudioDevice* res = new xnAudioDevice;

			HRESULT result;

#ifdef WINDOWS_DESKTOP
			if(xnAudioWindows7Hacks)
			{
#define CLSCTX_INPROC_SERVER 0x1
				IID cid, iid;
				IIDFromString(L"{5a508685-a254-4fba-9b82-9a24b00306af}", &cid);
				IIDFromString(L"{8bcf1f58-9fe7-4583-8ac6-e2adc465c8bb}", &iid);
				result = CoCreateInstance(cid, NULL, CLSCTX_INPROC_SERVER, iid, (void**)&res->x_audio2_7_);
				if (FAILED(result))
				{
					printf("CoCreateInstance failed to create XAudio2 instance.\n");
					delete res;
					return NULL;
				}

				result = res->x_audio2_7_->Initialize(0, 0x00000001);
				if (FAILED(result))
				{
					printf("Failed to init XAudio2 instance.\n");
					delete res;
					return NULL;
				}

				result = res->x_audio2_7_->CreateMasteringVoice(&res->mastering_voice_, AUDIO_CHANNELS);
				if (FAILED(result))
				{
					printf("Failed to create XAudio2 MasteringVoice.\n");
					delete res;
					return NULL;
				}

				result = res->x_audio2_7_->StartEngine();
				if (FAILED(result))
				{
					printf("Failed to create start XAudio2 engine.\n");
					delete res;
					return NULL;
				}
			}
			else
#endif
			{
				res->hrtf_ = xnHrtfApoLib && (flags & xnAudioDeviceFlagsHrtf);

				//XAudio2, no flags, processor 1
				result = XAudio2CreateFunc(reinterpret_cast<void**>(&res->x_audio2_), res->hrtf_ ? XAUDIO2_1024_QUANTUM : 0, 0x00000001);
				if (FAILED(result))
				{
					delete res;
					return NULL;
				}

				//this means opening the real audio device, which will be virtual actually so in the case of default device change Xaudio will deal with it for us.
				result = res->x_audio2_->CreateMasteringVoice(&res->mastering_voice_, AUDIO_CHANNELS, 0, 0, deviceName);
				if (FAILED(result))
				{
					delete res;
					return NULL;
				}

				//start audio rendering
				result = res->x_audio2_->StartEngine();
				if (FAILED(result))
				{
					delete res;
					return NULL;
				}		
			}

			//X3DAudio
			result = X3DAudioInitializeFunc(SPEAKER_STEREO, SPEED_OF_SOUND, res->x3_audio_);
			if (FAILED(result))
			{
				delete res;
				return NULL;
			}

			return res;
		}

		DLL_EXPORT_API void xnAudioDestroy(xnAudioDevice* device)
		{
			if(xnAudioWindows7Hacks)
			{
				device->x_audio2_7_->StopEngine();
			}
			else
			{
				device->x_audio2_->StopEngine();
			}

			device->mastering_voice_->DestroyVoice();
			
			delete device;
		}

		DLL_EXPORT_API void xnAudioUpdate(xnAudioDevice* device)
		{
		}

		DLL_EXPORT_API void xnAudioSetMasterVolume(xnAudioDevice* device, float volume)
		{
			device->mastering_voice_->SetVolume(volume);
		}

		DLL_EXPORT_API xnAudioListener* xnAudioListenerCreate(xnAudioDevice* device)
		{
			auto res = new xnAudioListener;
			res->device_ = device;
			memset(&res->listener_, 0x0, sizeof(X3DAUDIO_LISTENER));
			return res;
		}

		DLL_EXPORT_API void xnAudioListenerDestroy(xnAudioListener* listener)
		{
			delete listener;
		}

		DLL_EXPORT_API npBool xnAudioListenerEnable(xnAudioListener* listener)
		{
			//unused in Xaudio2
			(void)listener;
			return true;
		}

		DLL_EXPORT_API void xnAudioListenerDisable(xnAudioListener* listener)
		{
			//unused in Xaudio2
			(void)listener;
		}

		struct xnAudioSource : IXAudio2VoiceCallback
		{
			IXAudio2MasteringVoice* mastering_voice_;
			IXAudio2SourceVoice* source_voice_;
			X3DAUDIO_EMITTER* emitter_;
			X3DAUDIO_DSP_SETTINGS* dsp_settings_;
			IXAPOHrtfParameters* hrtf_params_;
			xnAudioListener* listener_;
			volatile bool playing_;
			volatile bool pause_;
			volatile bool looped_;
			int sampleRate_;
			bool mono_;
			bool streamed_;
			volatile float pitch_ = 1.0f;
			volatile float doppler_pitch_ = 1.0f;

			SpinLock bufferLock_;
			SpinLock apply3DLock_;
			xnAudioBuffer** freeBuffers_;
			int freeBuffersMax_;

			XAUDIO2_BUFFER single_buffer_;

			volatile int samplesAtBegin = 0;

			void __stdcall OnVoiceProcessingPassStart(UINT32 BytesRequired) override;

			void __stdcall OnVoiceProcessingPassEnd() override;

			void __stdcall OnStreamEnd() override;

			void __stdcall OnBufferStart(void* context) override;

			void __stdcall OnBufferEnd(void* context) override;

			void __stdcall OnLoopEnd(void* context) override;

			void __stdcall OnVoiceError(void* context, HRESULT error) override;

			void GetState(XAUDIO2_VOICE_STATE* state)
			{
				if (xnAudioWindows7Hacks)
				{
					auto win7Voice = reinterpret_cast<IXAudio2SourceVoice1*>(source_voice_);
					win7Voice->GetState(state);
				}
				else
				{
					source_voice_->GetState(state, 0);
				}
			}
		};

		DLL_EXPORT_API xnAudioSource* xnAudioSourceCreate(xnAudioListener* listener, int sampleRate, int maxNBuffers, npBool mono, npBool spatialized, npBool streamed, npBool hrtf, float directionFactor, HrtfEnvironment environment)
		{
			(void)streamed;

			auto res = new xnAudioSource;
			res->hrtf_params_ = NULL;
			res->listener_ = listener;
			res->playing_ = false;
			res->sampleRate_ = sampleRate;
			res->mono_ = mono;
			res->streamed_ = streamed;
			res->looped_ = false;
			res->mastering_voice_ = listener->device_->mastering_voice_;
			if((spatialized && !hrtf) || (hrtf && !res->listener_->device_->hrtf_))
			{
				//if spatialized we also need those structures to calculate 3D audio
				res->emitter_ = new X3DAUDIO_EMITTER;
				memset(res->emitter_, 0x0, sizeof(X3DAUDIO_EMITTER));
				res->emitter_->ChannelCount = 1;
				res->emitter_->CurveDistanceScaler = 1;
				res->emitter_->DopplerScaler = 1;

				res->dsp_settings_ = new X3DAUDIO_DSP_SETTINGS;
				memset(res->dsp_settings_, 0x0, sizeof(X3DAUDIO_DSP_SETTINGS));
				res->dsp_settings_->SrcChannelCount = 1;
				res->dsp_settings_->DstChannelCount = AUDIO_CHANNELS;
				res->dsp_settings_->pMatrixCoefficients = new float[AUDIO_CHANNELS];
				res->dsp_settings_->pDelayTimes = new float[AUDIO_CHANNELS];
			}
			else
			{
				res->emitter_ = NULL;
				res->dsp_settings_ = NULL;
			}

			//we could have used a tinystl vector but it did not link properly on ARM windows... so we just use an array
			res->freeBuffers_ = new xnAudioBuffer*[maxNBuffers];
			res->freeBuffersMax_ = maxNBuffers;
			for (auto i = 0; i < maxNBuffers; i++)
			{
				res->freeBuffers_[i] = NULL;
			}

			//Normal PCM formal 16 bit shorts
			WAVEFORMATEX pcmWaveFormat = {};
			pcmWaveFormat.wFormatTag = WAVE_FORMAT_PCM;
			pcmWaveFormat.nChannels = mono ? 1 : 2;
			pcmWaveFormat.nSamplesPerSec = sampleRate;
			pcmWaveFormat.nAvgBytesPerSec = sampleRate * pcmWaveFormat.nChannels * sizeof(short);
			pcmWaveFormat.wBitsPerSample = 16;
			pcmWaveFormat.nBlockAlign = pcmWaveFormat.nChannels * pcmWaveFormat.wBitsPerSample / 8;

			if(xnAudioWindows7Hacks)
			{
				HRESULT result = listener->device_->x_audio2_7_->CreateSourceVoice(&res->source_voice_, &pcmWaveFormat, 0, XAUDIO2_MAX_FREQ_RATIO, res);
				if (FAILED(result))
				{
					delete res;
					return NULL;
				}
			}
			else
			{
				HRESULT result = listener->device_->x_audio2_->CreateSourceVoice(&res->source_voice_, &pcmWaveFormat, 0, XAUDIO2_MAX_FREQ_RATIO, res);
				if (FAILED(result))
				{
					delete res;
					return NULL;
				}
			}

			if (spatialized && res->listener_->device_->hrtf_ && hrtf)
			{
				IXAudio2SubmixVoice* submixVoice = NULL;

				HrtfApoInit params = {};
				HrtfDirectivity directivity;
				directivity.type = OmniDirectional;
				directivity.scaling = directionFactor;
				params.directivity = &directivity;

				IUnknown* apoRoot;
				HRESULT result = CreateHrtfApoFunc(&params, &apoRoot);
				if (FAILED(result))
				{
					delete res;
					return NULL;
				}

				apoRoot->QueryInterface(xnHrtfParamsIID, reinterpret_cast<void**>(&res->hrtf_params_));
					
				XAUDIO2_EFFECT_DESCRIPTOR fxDesc{};
				fxDesc.InitialState = true;
				fxDesc.OutputChannels = 2; // Stereo output
				fxDesc.pEffect = res->hrtf_params_; // HRTF xAPO set as the effect.

				XAUDIO2_EFFECT_CHAIN fxChain{};
				fxChain.EffectCount = 1;
				fxChain.pEffectDescriptors = &fxDesc;

				XAUDIO2_VOICE_SENDS sends = {};
				XAUDIO2_SEND_DESCRIPTOR sendDesc = {};
				sendDesc.pOutputVoice = res->mastering_voice_;
				sends.SendCount = 1;
				sends.pSends = &sendDesc;

				// HRTF APO expects mono 48kHz input, so we configure the submix voice for that format.
				result = listener->device_->x_audio2_->CreateSubmixVoice(&submixVoice, 1, 48000, 0, 0, &sends, &fxChain);
				if (FAILED(result))
				{
					delete res;
					return NULL;
				}

				res->hrtf_params_->SetEnvironment(environment);

				XAUDIO2_VOICE_SENDS voice_sends = {};
				XAUDIO2_SEND_DESCRIPTOR voice_sendDesc = {};
				voice_sendDesc.pOutputVoice = submixVoice;
				voice_sends.SendCount = 1;
				voice_sends.pSends = &voice_sendDesc;
				result = res->source_voice_->SetOutputVoices(&voice_sends);
				if (FAILED(result))
				{
					delete res;
					return NULL;
				}
			}

			return res;
		}

		DLL_EXPORT_API void xnAudioSourceDestroy(xnAudioSource* source)
		{
			source->source_voice_->Stop();
			source->source_voice_->DestroyVoice();
			if (source->emitter_) delete source->emitter_;
			if (source->dsp_settings_)
			{		
				delete[] source->dsp_settings_->pMatrixCoefficients;
				delete[] source->dsp_settings_->pDelayTimes;
				delete source->dsp_settings_;
			}
			delete source;
		}

		DLL_EXPORT_API void xnAudioSourceSetLooping(xnAudioSource* source, npBool looping)
		{
			source->looped_ = looping;

			if (!source->streamed_)
			{
				if (!source->looped_)
				{
					source->single_buffer_.LoopBegin = 0;
					source->single_buffer_.LoopLength = 0;
					source->single_buffer_.LoopCount = 0;
					source->single_buffer_.Flags = XAUDIO2_END_OF_STREAM;
				}
				else
				{
					source->single_buffer_.LoopBegin = source->single_buffer_.PlayBegin;
					source->single_buffer_.LoopLength = source->single_buffer_.PlayLength;
					source->single_buffer_.LoopCount = XAUDIO2_LOOP_INFINITE;
					source->single_buffer_.Flags = 0;
				}

				source->source_voice_->FlushSourceBuffers();
				source->source_voice_->SubmitSourceBuffer(&source->single_buffer_, NULL);
			}
		}

		DLL_EXPORT_API void xnAudioSourceSetBuffer(xnAudioSource* source, xnAudioBuffer* buffer)
		{
			//this function is called only when the audio source is acutally fully cached in memory, so we deal only with the first buffer
			source->streamed_ = false;
			source->freeBuffers_[0] = buffer;
			memcpy(&source->single_buffer_, &buffer->buffer_, sizeof(XAUDIO2_BUFFER));
			source->source_voice_->SubmitSourceBuffer(&source->single_buffer_, NULL);
		}

		DLL_EXPORT_API xnAudioBuffer* xnAudioSourceGetFreeBuffer(xnAudioSource* source)
		{
			//this is used only when we are streaming audio, to fetch the next free buffer to fill
			source->bufferLock_.Lock();

			xnAudioBuffer* buffer = NULL;
			for (int i = 0; i < source->freeBuffersMax_; i++)
			{
				if (source->freeBuffers_[i] != NULL)
				{
					buffer = source->freeBuffers_[i];
					source->freeBuffers_[i] = NULL;
					break;
				}
			}
			
			source->bufferLock_.Unlock();
			
			return buffer;
		}

		DLL_EXPORT_API void xnAudioSourcePlay(xnAudioSource* source)
		{
			source->source_voice_->Start();
			source->playing_ = true;

			if(!source->streamed_ && !source->pause_)
			{
				XAUDIO2_VOICE_STATE state;
				source->GetState(&state);
				source->samplesAtBegin = state.SamplesPlayed;
			}

			source->pause_ = false;
		}

		DLL_EXPORT_API void xnAudioSourceSetPan(xnAudioSource* source, float pan)
		{
			if (source->mono_)
			{
				float panning[2];
				if (pan < 0)
				{
					panning[0] = 1.0f;
					panning[1] = 1.0f + pan;
				}
				else
				{
					panning[0] = 1.0f - pan;
					panning[1] = 1.0f;
				}
				source->source_voice_->SetOutputMatrix(source->mastering_voice_, 1, AUDIO_CHANNELS, panning);
			}
			else
			{
				float panning[4];
				if (pan < 0)
				{
					panning[0] = 1.0f;
					panning[1] = 0.0f;
					panning[2] = 0.0f;
					panning[3] = 1.0f + pan;
				}
				else
				{
					panning[0] = 1.0f - pan;
					panning[1] = 0.0f;
					panning[2] = 0.0f;
					panning[3] = 1.0f;
				}
				source->source_voice_->SetOutputMatrix(source->mastering_voice_, 2, AUDIO_CHANNELS, panning);
			}
		}

		DLL_EXPORT_API double xnAudioSourceGetPosition(xnAudioSource* source)
		{
			XAUDIO2_VOICE_STATE state;
			source->GetState(&state);

			if (!source->streamed_)
				return double(source->single_buffer_.PlayBegin + state.SamplesPlayed - source->samplesAtBegin) / double(source->sampleRate_);
			
			//things work different for streamed sources, but anyway we simply subtract the snapshotted samples at begin of the stream ( could be the begin of the loop )
			return double(state.SamplesPlayed - source->samplesAtBegin) / double(source->sampleRate_);
		}

		DLL_EXPORT_API void xnAudioSourceSetRange(xnAudioSource* source, double startTime, double stopTime)
		{
			if(!source->streamed_)
			{
				auto singleBuffer = source->freeBuffers_[0];
				if(startTime == 0 && stopTime == 0)
				{
					source->single_buffer_.PlayBegin = 0;
					source->single_buffer_.PlayLength = singleBuffer->length_;
				}
				else
				{					
					auto sampleStart = int(double(source->sampleRate_) * startTime);
					auto sampleStop = int(double(source->sampleRate_) * stopTime);

					if (sampleStart > singleBuffer->length_)
					{
						return; //the starting position must be less then the total length of the buffer
					}

					if (sampleStop > singleBuffer->length_) //if the end point is more then the length of the buffer fix the value
					{
						sampleStop = singleBuffer->length_;
					}

					auto len = sampleStop - sampleStart;
					if (len > 0)
					{
						source->single_buffer_.PlayBegin = sampleStart;
						source->single_buffer_.PlayLength = len;
					}
				}

				//sort looping properties and re-submit buffer
				source->source_voice_->Stop();
				xnAudioSourceSetLooping(source, source->looped_);
			}
		}

		DLL_EXPORT_API void xnAudioSourceSetGain(xnAudioSource* source, float gain)
		{
			source->source_voice_->SetVolume(gain);
		}

		DLL_EXPORT_API void xnAudioSourceSetPitch(xnAudioSource* source, float pitch)
		{
			source->pitch_ = pitch;
			source->source_voice_->SetFrequencyRatio(source->doppler_pitch_ * source->pitch_);
		}

        void xnAudioSource::OnVoiceProcessingPassStart(unsigned BytesRequired)
		{
		}

        void xnAudioSource::OnVoiceProcessingPassEnd()
		{
		}

        void xnAudioSource::OnStreamEnd()
		{
			if (playing_)
			{
				if (streamed_)
				{
					//buffer was flagged as end of stream
					//looping is handled by the streamer, in the top level layer
					xnAudioSourceStop(this);
				}
				else if (!looped_)
				{
					playing_ = false;
					pause_ = false;
				}
			}
		}

        void xnAudioSource::OnBufferStart(void* context)
		{
			if (streamed_)
			{
				auto buffer = static_cast<xnAudioBuffer*>(context);

				if (buffer->type_ == BeginOfStream)
				{
					//we need this info to compute position of stream
					XAUDIO2_VOICE_STATE state;
					GetState(&state);

					samplesAtBegin = state.SamplesPlayed;
				}
			}
		}

		void xnAudioSource::OnBufferEnd(void* context)
		{
			if (streamed_)
			{
				auto buffer = static_cast<xnAudioBuffer*>(context);

				bufferLock_.Lock();

				for (int i = 0; i < freeBuffersMax_; i++)
				{
					if (freeBuffers_[i] == NULL)
					{
						freeBuffers_[i] = buffer;
						break;
					}
				}

				bufferLock_.Unlock();
			}			
		}

        void xnAudioSource::OnLoopEnd(void* context)
		{
			if (!streamed_)
			{
				XAUDIO2_VOICE_STATE state;
				GetState(&state);

				samplesAtBegin = state.SamplesPlayed;
			}
		}

		DLL_EXPORT_API void xnAudioSourceQueueBuffer(xnAudioSource* source, xnAudioBuffer* buffer, short* pcm, int bufferSize, BufferType type)
		{
			//used only when streaming, to fill a buffer, often..
			source->streamed_ = true;

			//flag the stream
			buffer->buffer_.Flags = type == EndOfStream ? XAUDIO2_END_OF_STREAM : 0;
			buffer->type_ = type;
			
			buffer->length_ = buffer->buffer_.AudioBytes = bufferSize;
			memcpy(const_cast<char*>(buffer->buffer_.pAudioData), pcm, bufferSize);
			source->source_voice_->SubmitSourceBuffer(&buffer->buffer_);
		}

		DLL_EXPORT_API void xnAudioSourcePause(xnAudioSource* source)
		{
			source->source_voice_->Stop();
			source->playing_ = false;
			source->pause_ = true;
		}

		XMFLOAT3::XMFLOAT3(): x(0), y(0), z(0)
		{
		}

		XMFLOAT3::XMFLOAT3(float _x, float _y, float _z): x(_x), y(_y), z(_z)
		{
		}

		XMFLOAT3::XMFLOAT3(const float* pArray): x(pArray[0]), y(pArray[1]), z(pArray[2])
		{
		}

		XMFLOAT3& XMFLOAT3::operator=(const XMFLOAT3& Float3)
		{
			x = Float3.x;
			y = Float3.y;
			z = Float3.z;
			return *this;
		}

		DLL_EXPORT_API void xnAudioSourceFlushBuffers(xnAudioSource* source)
		{
			source->apply3DLock_.Lock();

			source->source_voice_->FlushSourceBuffers();

			source->apply3DLock_.Unlock();
		}

		DLL_EXPORT_API void xnAudioSourceStop(xnAudioSource* source)
		{
			source->apply3DLock_.Lock();

			source->source_voice_->Stop();
			source->source_voice_->FlushSourceBuffers();
			source->playing_ = false;
			source->pause_ = false;

			//since we flush we also rebuffer in this case
			if (!source->streamed_)
			{
				source->source_voice_->SubmitSourceBuffer(&source->single_buffer_, NULL);
			}

			source->apply3DLock_.Unlock();
		}

		DLL_EXPORT_API void xnAudioListenerPush3D(xnAudioListener* listener, float* pos, float* forward, float* up, float* vel, Matrix* worldTransform)
		{
			memcpy(&listener->listener_.Position, pos, sizeof(float) * 3);
			memcpy(&listener->listener_.Velocity, vel, sizeof(float) * 3);
			memcpy(&listener->listener_.OrientFront, forward, sizeof(float) * 3);
			memcpy(&listener->listener_.OrientTop, up, sizeof(float) * 3);
			memcpy(&listener->worldTransform_, worldTransform, sizeof(Matrix));
		}

		DLL_EXPORT_API void xnAudioSourcePush3D(xnAudioSource* source, float* pos, float* forward, float* up, float* vel, Matrix* worldTransform)
		{
			if(source->hrtf_params_)
			{
				Matrix localTransform;
				Matrix invListener;
				memcpy(&invListener, &source->listener_->worldTransform_, sizeof(Matrix));
				xnMatrixInvert(&invListener);
				xnMatrixMultiply(worldTransform, &invListener, &localTransform);

				HrtfPosition hrtfEmitterPos{ localTransform.Flat.M41, localTransform.Flat.M42, localTransform.Flat.M43 };
				source->hrtf_params_->SetSourcePosition(&hrtfEmitterPos);

				//set orientation, relative to head, already computed c# side, todo c++ side
				HrtfOrientation hrtfEmitterRot { 
					localTransform.Flat.M11, localTransform.Flat.M12, localTransform.Flat.M13,
					localTransform.Flat.M21, localTransform.Flat.M22, localTransform.Flat.M23,
					localTransform.Flat.M31, localTransform.Flat.M32, localTransform.Flat.M33 };
				source->hrtf_params_->SetSourceOrientation(&hrtfEmitterRot);
			}
			else
			{
				if (!source->emitter_) return;

				memcpy(&source->emitter_->Position, pos, sizeof(float) * 3);
				memcpy(&source->emitter_->Velocity, vel, sizeof(float) * 3);
				memcpy(&source->emitter_->OrientFront, forward, sizeof(float) * 3);
				memcpy(&source->emitter_->OrientTop, up, sizeof(float) * 3);

				source->apply3DLock_.Lock(); //todo is that really needed?

				//everything is calculated by Xaudio for us
				X3DAudioCalculateFunc(source->listener_->device_->x3_audio_, &source->listener_->listener_, source->emitter_,
					X3DAUDIO_CALCULATE_MATRIX | X3DAUDIO_CALCULATE_DOPPLER | X3DAUDIO_CALCULATE_LPF_DIRECT | X3DAUDIO_CALCULATE_REVERB, source->dsp_settings_);

				source->source_voice_->SetOutputMatrix(source->mastering_voice_, 1, AUDIO_CHANNELS, source->dsp_settings_->pMatrixCoefficients);
				source->doppler_pitch_ = source->dsp_settings_->DopplerFactor;
				source->source_voice_->SetFrequencyRatio(source->dsp_settings_->DopplerFactor * source->pitch_);
				XAUDIO2_FILTER_PARAMETERS filter_parameters = { LowPassFilter, 2.0f * (float)sin(X3DAUDIO_PI / 6.0f * source->dsp_settings_->LPFDirectCoefficient), 1.0f };
				if (!xnAudioWindows7Hacks) source->source_voice_->SetFilterParameters(&filter_parameters);

				source->apply3DLock_.Unlock();
			}
		}

		DLL_EXPORT_API npBool xnAudioSourceIsPlaying(xnAudioSource* source)
		{
			return source->playing_ || source->pause_;
		}

		DLL_EXPORT_API xnAudioBuffer* xnAudioBufferCreate(int maxBufferSize)
		{
			auto buffer = new xnAudioBuffer;
			buffer->length_ = 0;
			buffer->buffer_ = {};
			buffer->buffer_.pContext = buffer;
			buffer->buffer_.PlayBegin = 0;
			buffer->buffer_.PlayLength = 0;
			buffer->buffer_.LoopBegin = 0;
			buffer->buffer_.LoopLength = 0;
			buffer->buffer_.LoopCount = 0;
			buffer->buffer_.pAudioData = new BYTE[maxBufferSize];
			return buffer;
		}

		DLL_EXPORT_API void xnAudioBufferDestroy(xnAudioBuffer* buffer)
		{
			delete[] buffer->buffer_.pAudioData;
			delete buffer;
		}

		DLL_EXPORT_API void xnAudioBufferFill(xnAudioBuffer* buffer, short* pcm, int bufferSize, int sampleRate, npBool mono)
		{
			(void)sampleRate;
			
			buffer->buffer_.AudioBytes = bufferSize;
			
			buffer->buffer_.PlayBegin = 0;
			buffer->buffer_.PlayLength = buffer->length_ = (bufferSize / sizeof(short)) / (mono ? 1 : 2);
			
			memcpy(const_cast<char*>(buffer->buffer_.pAudioData), pcm, bufferSize);
		}

		void xnAudioSource::OnVoiceError(void* context, long error)
		{
		}
	}
}

SpinLock::SpinLock()
{
	mLocked = false;
}

void SpinLock::Lock()
{
	while (!__sync_bool_compare_and_swap(&mLocked, false, true))
	{
	}
}

void SpinLock::Unlock()
{
	mLocked = false;
}

#endif
