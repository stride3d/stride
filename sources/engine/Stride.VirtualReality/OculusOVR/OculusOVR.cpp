// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#include "../../../deps/NativePath/NativePath.h"
#include "../../Stride.Native/StrideNative.h"

#if defined(WINDOWS_DESKTOP) || !defined(__clang__)

#ifndef __clang__
//Make resharper work!
#define OVR_ALIGNAS(n)
#error "The compiler must be clang!"
#endif

#include "../../../../deps/OculusOVR/Include/OVR_CAPI.h"

typedef struct _GUID {
	unsigned long  Data1;
	unsigned short Data2;
	unsigned short Data3;
	unsigned char  Data4[8];
} GUID;

#define OVR_AUDIO_MAX_DEVICE_STR_SIZE 128

extern "C" {
	//DX specific stuff
	extern ovrResult ovr_CreateTextureSwapChainDX(ovrSession session, void* d3dPtr, const ovrTextureSwapChainDesc* desc, ovrTextureSwapChain* out_TextureSwapChain);
	extern ovrResult ovr_CreateMirrorTextureDX(ovrSession session, void* d3dPtr, const ovrMirrorTextureDesc* desc, ovrMirrorTexture* out_MirrorTexture);
	extern ovrResult ovr_GetTextureSwapChainBufferDX(ovrSession session, ovrTextureSwapChain chain, int index, GUID iid, void** out_Buffer);
	extern ovrResult ovr_GetMirrorTextureBufferDX(ovrSession session, ovrMirrorTexture mirrorTexture, GUID iid, void** out_Buffer);
	extern ovrResult ovr_GetAudioDeviceOutGuidStr(wchar_t deviceOutStrBuffer[OVR_AUDIO_MAX_DEVICE_STR_SIZE]);

	DLL_EXPORT_API npBool xnOvrStartup()
	{
		ovrResult result = ovr_Initialize(NULL);
		return OVR_SUCCESS(result);
	}

	DLL_EXPORT_API void xnOvrShutdown()
	{
		ovr_Shutdown();
	}

	DLL_EXPORT_API int xnOvrGetError(char* errorString)
	{
		ovrErrorInfo errInfo;
		ovr_GetLastErrorInfo(&errInfo);
		strcpy(errorString, errInfo.ErrorString);
		return errInfo.Result;
	}

	struct xnOvrSession
	{
		ovrSession Session;
		ovrMirrorTexture Mirror;
		ovrHmdDesc HmdDesc;
		ovrEyeRenderDesc EyeRenderDesc[2];
		ovrVector3f HmdToEyeViewOffset[2];

		ovrTextureSwapChain SwapChain;
		ovrLayerEyeFov Layer;
		ovrTrackingState CurrentState;
	};

	DLL_EXPORT_API xnOvrSession* xnOvrCreateSessionDx(int64_t* luidOut)
	{
		ovrSession session;
		ovrGraphicsLuid luid;
		ovrResult result = ovr_Create(&session, &luid);

		bool success = OVR_SUCCESS(result);
		if(success)
		{
			auto sessionOut = new xnOvrSession();
			sessionOut->Session = session;
			sessionOut->SwapChain = NULL;

			*luidOut = *((int64_t*)luid.Reserved);
			return sessionOut;
		}

		return NULL;
	}

	DLL_EXPORT_API void xnOvrDestroySession(xnOvrSession* session)
	{
		ovr_Destroy(session->Session);
	}

	DLL_EXPORT_API npBool xnOvrCreateTexturesDx(xnOvrSession* session, void* dxDevice, int* outTextureCount, float pixelPerDisplayPixel, int mirrorBufferWidth, int mirrorBufferHeight)
	{
		session->HmdDesc = ovr_GetHmdDesc(session->Session);
		ovrSizei sizel = ovr_GetFovTextureSize(session->Session, ovrEye_Left, session->HmdDesc.DefaultEyeFov[0], pixelPerDisplayPixel);
		ovrSizei sizer = ovr_GetFovTextureSize(session->Session, ovrEye_Right, session->HmdDesc.DefaultEyeFov[1], pixelPerDisplayPixel);
		ovrSizei bufferSize;
		bufferSize.w = sizel.w + sizer.w;
		bufferSize.h = fmax(sizel.h, sizer.h);

		ovrTextureSwapChainDesc texDesc = {};
		texDesc.Type = ovrTexture_2D;
		texDesc.Format = OVR_FORMAT_R8G8B8A8_UNORM_SRGB;
		texDesc.ArraySize = 1;
		texDesc.Width = bufferSize.w;
		texDesc.Height = bufferSize.h;
		texDesc.MipLevels = 1;
		texDesc.SampleCount = 1;
		texDesc.StaticImage = ovrFalse;
		texDesc.MiscFlags = ovrTextureMisc_None;
		texDesc.BindFlags = ovrTextureBind_DX_RenderTarget;

		if(!OVR_SUCCESS(ovr_CreateTextureSwapChainDX(session->Session, dxDevice, &texDesc, &session->SwapChain)))
		{
			return false;
		}

		auto count = 0;
		ovr_GetTextureSwapChainLength(session->Session, session->SwapChain, &count);
		*outTextureCount = count;
		
		//init structures
		session->EyeRenderDesc[0] = ovr_GetRenderDesc(session->Session, ovrEye_Left, session->HmdDesc.DefaultEyeFov[0]);
		session->EyeRenderDesc[1] = ovr_GetRenderDesc(session->Session, ovrEye_Right, session->HmdDesc.DefaultEyeFov[1]);
		session->HmdToEyeViewOffset[0] = session->EyeRenderDesc[0].HmdToEyeOffset;
		session->HmdToEyeViewOffset[1] = session->EyeRenderDesc[1].HmdToEyeOffset;

		session->Layer.Header.Type = ovrLayerType_EyeFov;
		session->Layer.Header.Flags = 0;
		session->Layer.ColorTexture[0] = session->SwapChain;
		session->Layer.ColorTexture[1] = session->SwapChain;
		session->Layer.Fov[0] = session->EyeRenderDesc[0].Fov;
		session->Layer.Fov[1] = session->EyeRenderDesc[1].Fov;
		session->Layer.Viewport[0].Pos.x = 0;
		session->Layer.Viewport[0].Pos.y = 0;
		session->Layer.Viewport[0].Size.w = bufferSize.w / 2;
		session->Layer.Viewport[0].Size.h = bufferSize.h;
		session->Layer.Viewport[1].Pos.x = bufferSize.w / 2;
		session->Layer.Viewport[1].Pos.y = 0;
		session->Layer.Viewport[1].Size.w = bufferSize.w / 2;
		session->Layer.Viewport[1].Size.h = bufferSize.h;

		//create mirror as well
		if (mirrorBufferHeight != 0 && mirrorBufferWidth != 0)
		{
			ovrMirrorTextureDesc mirrorDesc = {};
			mirrorDesc.Format = OVR_FORMAT_R8G8B8A8_UNORM_SRGB;
			mirrorDesc.Width = mirrorBufferWidth;
			mirrorDesc.Height = mirrorBufferHeight;
			if (!OVR_SUCCESS(ovr_CreateMirrorTextureDX(session->Session, dxDevice, &mirrorDesc, &session->Mirror)))
			{
				return false;
			}
		}
		
		return true;
	}

	struct xnOvrQuadLayer
	{
		ovrTextureSwapChain SwapChain;
		ovrLayerQuad Layer;
	};

	DLL_EXPORT_API xnOvrQuadLayer* xnOvrCreateQuadLayerTexturesDx(xnOvrSession* session, void* dxDevice, int* outTextureCount, int width, int height, int mipLevels, int sampleCount)
	{
		auto layer = new xnOvrQuadLayer;

		ovrTextureSwapChainDesc texDesc = {};
		texDesc.Type = ovrTexture_2D;
		texDesc.Format = OVR_FORMAT_R8G8B8A8_UNORM_SRGB;
		texDesc.ArraySize = 1;
		texDesc.Width = width;
		texDesc.Height = height;
		texDesc.MipLevels = mipLevels;
		texDesc.SampleCount = sampleCount;
		texDesc.StaticImage = ovrFalse;
		texDesc.MiscFlags = ovrTextureMisc_None;
		texDesc.BindFlags = ovrTextureBind_DX_RenderTarget;

		if (!OVR_SUCCESS(ovr_CreateTextureSwapChainDX(session->Session, dxDevice, &texDesc, &layer->SwapChain)))
		{
			delete layer;
			return NULL;
		}

		auto count = 0;
		ovr_GetTextureSwapChainLength(session->Session, layer->SwapChain, &count);
		*outTextureCount = count;

		layer->Layer.Header.Type = ovrLayerType_Quad;
		layer->Layer.Header.Flags = ovrLayerFlag_HighQuality;
		layer->Layer.ColorTexture = layer->SwapChain;
		layer->Layer.Viewport.Pos.x = 0;
		layer->Layer.Viewport.Pos.y = 0;
		layer->Layer.Viewport.Size.w = width;
		layer->Layer.Viewport.Size.h = height;
		layer->Layer.QuadPoseCenter.Orientation.x = 0;
		layer->Layer.QuadPoseCenter.Orientation.y = 0;
		layer->Layer.QuadPoseCenter.Orientation.z = 0;
		layer->Layer.QuadPoseCenter.Orientation.w = 1;
		layer->Layer.QuadPoseCenter.Position.x = 0;
		layer->Layer.QuadPoseCenter.Position.y = 0;
		layer->Layer.QuadPoseCenter.Position.z = -1;
		layer->Layer.QuadSize.x = 2;
		layer->Layer.QuadSize.y = 2;

		return layer;
	}

	DLL_EXPORT_API void xnOvrSetQuadLayerParams(xnOvrQuadLayer* layer, float* position, float* orientation, float* size, npBool headLocked)
	{
		memcpy(&layer->Layer.QuadPoseCenter.Orientation, orientation, sizeof(float) * 4);
		memcpy(&layer->Layer.QuadPoseCenter.Position, position, sizeof(float) * 3);
		memcpy(&layer->Layer.QuadSize, size, sizeof(float) * 2);
		layer->Layer.Header.Flags = headLocked ? ovrLayerFlag_HeadLocked | ovrLayerFlag_HighQuality : ovrLayerFlag_HighQuality;
	}

	DLL_EXPORT_API void* xnOvrGetTextureAtIndexDx(xnOvrSession* session, GUID textureGuid, int index)
	{
		void* texture = NULL;
		if (!OVR_SUCCESS(ovr_GetTextureSwapChainBufferDX(session->Session, session->SwapChain, index, textureGuid, &texture)))
		{
			return NULL;
		}
		return texture;
	}

	DLL_EXPORT_API void* xnOvrGetQuadLayerTextureAtIndexDx(xnOvrSession* session, xnOvrQuadLayer* layer, GUID textureGuid, int index)
	{
		void* texture = NULL;
		if (!OVR_SUCCESS(ovr_GetTextureSwapChainBufferDX(session->Session, layer->SwapChain, index, textureGuid, &texture)))
		{
			return NULL;
		}
		return texture;
	}

	DLL_EXPORT_API void* xnOvrGetMirrorTextureDx(xnOvrSession* session, GUID textureGuid)
	{
		void* texture = NULL;
		if (!OVR_SUCCESS(ovr_GetMirrorTextureBufferDX(session->Session, session->Mirror, textureGuid, &texture)))
		{
			return NULL;
		}
		return texture;
	}

	DLL_EXPORT_API int xnOvrGetCurrentTargetIndex(xnOvrSession* session)
	{
		int index;
		ovr_GetTextureSwapChainCurrentIndex(session->Session, session->SwapChain, &index);
		return index;
	}

	DLL_EXPORT_API int xnOvrGetCurrentQuadLayerTargetIndex(xnOvrSession* session, xnOvrQuadLayer* layer)
	{
		int index;
		ovr_GetTextureSwapChainCurrentIndex(session->Session, layer->SwapChain, &index);
		return index;
	}

#pragma pack(push, 4)
	struct xnOvrFrameProperties
	{
		//Camera properties
		float Near;
		float Far;
		float ProjLeft[16];
		float ProjRight[16];
		float PosLeft[3];
		float PosRight[3];
		float RotLeft[4];
		float RotRight[4];
	};

	struct xnOvrPosesProperties
	{
		//Head
		float PosHead[3];
		float RotHead[4];
		float AngularVelocityHead[3];
		float AngularAccelerationHead[3];
		float LinearVelocityHead[3];
		float LinearAccelerationHead[3];

		//Left hand
		float PosLeftHand[3];
		float RotLeftHand[4];
		float AngularVelocityLeftHand[3];
		float AngularAccelerationLeftHand[3];
		float LinearVelocityLeftHand[3];
		float LinearAccelerationLeftHand[3];
		int StateLeftHand;

		//Right hand
		float PosRightHand[3];
		float RotRightHand[4];
		float AngularVelocityRightHand[3];
		float AngularAccelerationRightHand[3];
		float LinearVelocityRightHand[3];
		float LinearAccelerationRightHand[3];
		int StateRightHand;
	};

	struct xnOvrInputProperties
	{
		unsigned int Buttons;
		unsigned int Touches;
		float IndexTriggerLeft;
		float IndexTriggerRight;
		float HandTriggerLeft;
		float HandTriggerRight;
		float ThumbstickLeft[2];
		float ThumbstickRight[2];
		npBool Valid;
	};
#pragma pack(pop)

	DLL_EXPORT_API void xnOvrUpdate(xnOvrSession* session)
	{
		session->EyeRenderDesc[0] = ovr_GetRenderDesc(session->Session, ovrEye_Left, session->HmdDesc.DefaultEyeFov[0]);
		session->EyeRenderDesc[1] = ovr_GetRenderDesc(session->Session, ovrEye_Right, session->HmdDesc.DefaultEyeFov[1]);
		session->HmdToEyeViewOffset[0] = session->EyeRenderDesc[0].HmdToEyeOffset;
		session->HmdToEyeViewOffset[1] = session->EyeRenderDesc[1].HmdToEyeOffset;

		session->Layer.SensorSampleTime = ovr_GetPredictedDisplayTime(session->Session, 0);
		session->CurrentState = ovr_GetTrackingState(session->Session, session->Layer.SensorSampleTime, ovrTrue);
		ovr_CalcEyePoses(session->CurrentState.HeadPose.ThePose, session->HmdToEyeViewOffset, session->Layer.RenderPose);
	}

	DLL_EXPORT_API void xnOvrGetFrameProperties(xnOvrSession* session, xnOvrFrameProperties* properties)
	{
		auto leftProj = ovrMatrix4f_Projection(session->Layer.Fov[0], properties->Near, properties->Far, 0);
		auto rightProj = ovrMatrix4f_Projection(session->Layer.Fov[1], properties->Near, properties->Far, 0);

		memcpy(properties->ProjLeft, &leftProj, sizeof(float) * 16);
		memcpy(properties->PosLeft, &session->Layer.RenderPose[0].Position, sizeof(float) * 3);
		memcpy(properties->RotLeft, &session->Layer.RenderPose[0].Orientation, sizeof(float) * 4);
		
		memcpy(properties->ProjRight, &rightProj, sizeof(float) * 16);
		memcpy(properties->PosRight, &session->Layer.RenderPose[1].Position, sizeof(float) * 3);
		memcpy(properties->RotRight, &session->Layer.RenderPose[1].Orientation, sizeof(float) * 4);
	}

	DLL_EXPORT_API void xnOvrGetPosesProperties(xnOvrSession* session, xnOvrPosesProperties* properties)
	{
		memcpy(properties->PosHead, &session->CurrentState.HeadPose.ThePose.Position, sizeof(float) * 3);
		memcpy(properties->RotHead, &session->CurrentState.HeadPose.ThePose.Orientation, sizeof(float) * 4);
		memcpy(properties->AngularVelocityHead, &session->CurrentState.HeadPose.AngularVelocity, sizeof(float) * 3);
		memcpy(properties->AngularAccelerationHead, &session->CurrentState.HeadPose.AngularAcceleration, sizeof(float) * 3);
		memcpy(properties->LinearVelocityHead, &session->CurrentState.HeadPose.LinearVelocity, sizeof(float) * 3);
		memcpy(properties->LinearAccelerationHead, &session->CurrentState.HeadPose.LinearAcceleration, sizeof(float) * 3);

		memcpy(properties->PosLeftHand, &session->CurrentState.HandPoses[0].ThePose.Position, sizeof(float) * 3);
		memcpy(properties->RotLeftHand, &session->CurrentState.HandPoses[0].ThePose.Orientation, sizeof(float) * 4);
		memcpy(properties->AngularVelocityLeftHand, &session->CurrentState.HandPoses[0].AngularVelocity, sizeof(float) * 3);
		memcpy(properties->AngularAccelerationLeftHand, &session->CurrentState.HandPoses[0].AngularAcceleration, sizeof(float) * 3);
		memcpy(properties->LinearVelocityLeftHand, &session->CurrentState.HandPoses[0].LinearVelocity, sizeof(float) * 3);
		memcpy(properties->LinearAccelerationLeftHand, &session->CurrentState.HandPoses[0].LinearAcceleration, sizeof(float) * 3);
		properties->StateLeftHand = session->CurrentState.HandStatusFlags[0];

		memcpy(properties->PosRightHand, &session->CurrentState.HandPoses[1].ThePose.Position, sizeof(float) * 3);
		memcpy(properties->RotRightHand, &session->CurrentState.HandPoses[1].ThePose.Orientation, sizeof(float) * 4);
		memcpy(properties->AngularVelocityRightHand, &session->CurrentState.HandPoses[1].AngularVelocity, sizeof(float) * 3);
		memcpy(properties->AngularAccelerationRightHand, &session->CurrentState.HandPoses[1].AngularAcceleration, sizeof(float) * 3);
		memcpy(properties->LinearVelocityRightHand, &session->CurrentState.HandPoses[1].LinearVelocity, sizeof(float) * 3);
		memcpy(properties->LinearAccelerationRightHand, &session->CurrentState.HandPoses[1].LinearAcceleration, sizeof(float) * 3);
		properties->StateRightHand = session->CurrentState.HandStatusFlags[1];
	}

	DLL_EXPORT_API void xnOvrGetInputProperties(xnOvrSession* session, xnOvrInputProperties* properties)
	{
		ovrInputState state;
		auto res = ovr_GetInputState(session->Session, ovrControllerType_Touch, &state);
		if(OVR_SUCCESS(res))
		{
			properties->Valid = true;
			properties->Buttons = state.Buttons;
			properties->Touches = state.Touches;
			properties->HandTriggerLeft = state.HandTrigger[0];
			properties->HandTriggerRight = state.HandTrigger[1];
			properties->IndexTriggerLeft = state.IndexTrigger[0];
			properties->IndexTriggerRight = state.IndexTrigger[1];
			properties->ThumbstickLeft[0] = state.Thumbstick[0].x;
			properties->ThumbstickLeft[1] = state.Thumbstick[0].y;
			properties->ThumbstickRight[0] = state.Thumbstick[1].x;
			properties->ThumbstickRight[1] = state.Thumbstick[1].y;
		}
		else
		{
			properties->Valid = false;
		}
	}

	DLL_EXPORT_API npBool xnOvrCommitFrame(xnOvrSession* session, int numberOfExtraLayers, xnOvrQuadLayer** extraLayers)
	{
		ovrLayerHeader* layers[1 + numberOfExtraLayers];
		//add the default layer first
		layers[0] = &session->Layer.Header;
		//commit the default fov layer
		ovr_CommitTextureSwapChain(session->Session, session->SwapChain);
		for (auto i = 0; i < numberOfExtraLayers; i++)
		{
			//add further quad layers
			layers[i + 1] = &extraLayers[i]->Layer.Header;
			//also commit the quad layer
			ovr_CommitTextureSwapChain(session->Session, extraLayers[i]->SwapChain);
		}

		if(!OVR_SUCCESS(ovr_SubmitFrame(session->Session, 0, NULL, layers, 1 + numberOfExtraLayers)))
		{
			return false;
		}

		ovrSessionStatus status;
		if (!OVR_SUCCESS(ovr_GetSessionStatus(session->Session, &status)))
		{
			return false;
		}

		if(status.ShouldRecenter)
		{
			ovr_RecenterTrackingOrigin(session->Session);
		}

		return true;
	}

#pragma pack(push, 4)
	struct xnOvrSessionStatus
	{
		npBool IsVisible;    ///< True if the process has VR focus and thus is visible in the HMD.
		npBool HmdPresent;   ///< True if an HMD is present.
		npBool HmdMounted;   ///< True if the HMD is on the user's head.
		npBool DisplayLost;  ///< True if the session is in a display-lost state. See ovr_SubmitFrame.
		npBool ShouldQuit;   ///< True if the application should initiate shutdown.    
		npBool ShouldRecenter;  ///< True if UX has requested re-centering. Must call ovr_ClearShouldRecenterFlag or ovr_RecenterTrackingOrigin. 
	};
#pragma pack(pop)

	DLL_EXPORT_API void xnOvrGetStatus(xnOvrSession* session, xnOvrSessionStatus* statusOut)
	{
		ovrSessionStatus status;
		if (!OVR_SUCCESS(ovr_GetSessionStatus(session->Session, &status)))
		{
			return;
		}

		statusOut->IsVisible = status.IsVisible;
		statusOut->HmdPresent = status.HmdPresent;
		statusOut->HmdMounted = status.HmdMounted;
		statusOut->DisplayLost = status.DisplayLost;
		statusOut->ShouldQuit = status.ShouldQuit;
		statusOut->ShouldRecenter = status.ShouldRecenter;
	}

	DLL_EXPORT_API void xnOvrRecenter(xnOvrSession* session)
	{
		ovr_RecenterTrackingOrigin(session->Session);
	}

	DLL_EXPORT_API void xnOvrGetAudioDeviceID(wchar_t* deviceString)
	{
		ovr_GetAudioDeviceOutGuidStr(deviceString);
	}
}

#else

extern "C" {
	typedef struct _GUID {
		unsigned long  Data1;
		unsigned short Data2;
		unsigned short Data3;
		unsigned char  Data4[8];
	} GUID;

	DLL_EXPORT_API npBool xnOvrStartup()
	{
		return true;
	}

	DLL_EXPORT_API void xnOvrShutdown()
	{
		
	}

	DLL_EXPORT_API int xnOvrGetError(char* errorString)
	{
		return 0;
	}

	DLL_EXPORT_API void* xnOvrCreateSessionDx(void* luidOut)
	{
		return 0;
	}

	DLL_EXPORT_API void xnOvrDestroySession(void* session)
	{
		
	}

	DLL_EXPORT_API npBool xnOvrCreateTexturesDx(void* session, void* dxDevice, int* outTextureCount)
	{
		return true;
	}

	DLL_EXPORT_API void* xnOvrGetTextureAtIndexDx(void* session, GUID textureGuid, int index)
	{
		return 0;
	}

	DLL_EXPORT_API void* xnOvrGetMirrorTextureDx(void* session, GUID textureGuid)
	{
		return 0;
	}

	DLL_EXPORT_API int xnOvrGetCurrentTargetIndex(void* session)
	{
		return 0;
	}

	DLL_EXPORT_API void xnOvrUpdate(void* session)
	{
		
	}

	DLL_EXPORT_API void xnOvrGetFrameProperties(void* session, void* params)
	{
		
	}

	DLL_EXPORT_API void xnOvrGetPosesProperties(void* session, void* params)
	{

	}

	DLL_EXPORT_API void xnOvrGetInputProperties(void* session, void* properties)
	{
		
	}

	DLL_EXPORT_API npBool xnOvrCommitFrame(void* session, int numberOfExtraLayers, void** extraLayers)
	{
		return true;
	}

#pragma pack(push, 4)
	DLL_EXPORT_API struct xnOvrSessionStatus
	{
		npBool IsVisible;    ///< True if the process has VR focus and thus is visible in the HMD.
		npBool HmdPresent;   ///< True if an HMD is present.
		npBool HmdMounted;   ///< True if the HMD is on the user's head.
		npBool DisplayLost;  ///< True if the session is in a display-lost state. See ovr_SubmitFrame.
		npBool ShouldQuit;   ///< True if the application should initiate shutdown.    
		npBool ShouldRecenter;  ///< True if UX has requested re-centering. Must call ovr_ClearShouldRecenterFlag or ovr_RecenterTrackingOrigin. 
	};
#pragma pack(pop)

	DLL_EXPORT_API void xnOvrGetStatus(void* session, void* statusOut)
	{
	}

	DLL_EXPORT_API void xnOvrRecenter(void* session)
	{
	}

	DLL_EXPORT_API void xnOvrGetAudioDeviceID(wchar_t* deviceString)
	{
	}

	DLL_EXPORT_API void* xnOvrCreateQuadLayerTexturesDx(void* session, void* dxDevice, int* outTextureCount, int width, int height, int mipLevels, int sampleCount)
	{
		return NULL;
	}

	DLL_EXPORT_API int xnOvrGetCurrentQuadLayerTargetIndex(void* session, void* layer)
	{
		return 0;
	}

	DLL_EXPORT_API void* xnOvrGetQuadLayerTextureAtIndexDx(void* session, void* layer, GUID textureGuid, int index)
	{
		return NULL;
	}

	DLL_EXPORT_API void xnOvrSetQuadLayerParams(void* layer, float* position, float* orientation, float* size, npBool headLocked)
	{
	}
}

#endif
