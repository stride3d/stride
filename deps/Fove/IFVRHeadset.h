#pragma once

#ifndef _IFVRHEADSET_H
#define _IFVRHEADSET_H

#include "FoveTypes.h"

namespace Fove
{
	class IFVRHeadset
	{
	public:
		// members
		// General
		virtual bool Initialise() = 0;
		virtual bool Initialise(EFVR_ClientCapabilities capabilities) = 0;
		//! hardware connected
		virtual bool IsHardwareConnected() = 0;
		//! the hardware for their requested capabilities started
		virtual bool IsHardwareReady() = 0;
		// Deprecated in v0.7.3 released after 30th Sep 2016
		virtual FVR_DEPRECATED(bool IsHeadsetMounted()) = 0;
		// Deprecated in v0.6.2 released 2nd Sep 2016
		virtual FVR_DEPRECATED(float GetVersion()) = 0;
		virtual EFVR_ErrorCode CheckRuntimeVersion() = 0;
		virtual EFVR_ErrorCode GetLastError() = 0;
		
		//! eye tracking
		virtual SFVR_GazeScreenCoord GetGazePoint() = 0;
		virtual SFVR_WorldGaze GetWorldGaze() = 0;
		//! start and stop the subsystem
		// Deprecated in v0.6.2 released 2nd Sep 2016
		virtual bool FVR_DEPRECATED(DisableEyeTracking()) = 0;
		// Deprecated in v0.6.2 released 2nd Sep 2016
		virtual bool FVR_DEPRECATED(EnableEyeTracking()) = 0;
		//! temp
		virtual SFVR_EyeImage GetFrameData() = 0;
		virtual SFVR_EyeImage GetPositionImageData() = 0;
		//! status
		virtual bool IsEyeTracking() = 0;
		virtual bool IsEyeTrackingReady() = 0;
		virtual bool IsCalibrated() = 0;
		virtual bool IsCalibrating() = 0;
		virtual EFVR_Eye CheckEyesClosed() = 0;

		//! motion sensor
		virtual bool IsMotionReady() = 0;
		// Deprecated in v0.6.1 released 29th Aug 2016
		virtual FVR_DEPRECATED(SFVR_HeadOrientation GetOrientation()) = 0;
		virtual bool TareOrientationSensor() = 0;

		//! position tracking
		virtual bool IsPositionReady() = 0;
		// Deprecated in v0.6.1 released 29th Aug 2016
		virtual FVR_DEPRECATED(SFVR_Pose GetPosition()) = 0;
		virtual bool TarePositionSensors() = 0;

		virtual SFVR_Pose GetHMDPose() = 0;
		virtual SFVR_Pose GetPoseByIndex(int id) = 0;

		//! metrics
		virtual SFVR_Matrix44 GetProjectionMatrixLH(EFVR_Eye whichEye, float zNear, float zFar) = 0;
		virtual SFVR_Matrix44 GetProjectionMatrixRH(EFVR_Eye whichEye, float zNear, float zFar) = 0;
		//! Returns values at 1 unit away. Please convert yourself by multiplying by zNear.
		virtual void AssignRawProjectionValues(EFVR_Eye whichEye, float *l, float *r, float *t, float *b) = 0;
		virtual SFVR_Matrix44 GetEyeToHeadMatrix(EFVR_Eye whichEye) = 0;

		//! calibration
		virtual void StartCalibration() = 0;
		virtual SFVR_CalibrationTarget TickCalibration(float deltaTime) = 0;
		virtual EFVR_ErrorCode ManualDriftCorrection(float screenX, float screenY, EFVR_Eye eye) = 0;
		virtual EFVR_ErrorCode ManualDriftCorrection3D(SFVR_Vec3 position) = 0;

		//! constructor & destructor
		virtual ~IFVRHeadset();
		virtual void Destroy() = 0;
	};

	inline IFVRHeadset::~IFVRHeadset() { }

	FVR_EXPORT IFVRHeadset* GetFVRHeadset();
}
#endif // _IFVRHEADSET_H