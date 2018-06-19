#pragma once

#ifndef _FOVETYPES_H
#define _FOVETYPES_H

#ifdef __GNUC__
#define FVR_DEPRECATED(func) func __attribute__ ((deprecated))
#define FVR_EXPORT __attribute__((visibility("default")))
#elif defined(_MSC_VER)
#define FVR_DEPRECATED(func) __declspec(deprecated) func
#define FVR_EXPORT __declspec(dllexport)
#else
#pragma message("WARNING: You need to implement DEPRECATED for this compiler")
#define FVR_DEPRECATED(func) func
#define FVR_EXPORT
#endif

#include "../NativePath/standard/math.h"
#include "../NativePath/standard/stdint.h"

namespace Fove
{
	//! EFVR_ClientCapabilities
	/*! To be passed to the initialisation function of the client library to  */
	enum class EFVR_ClientCapabilities
	{
		Gaze = 0x01,
		Orientation = 0x02,
		Position = 0x04
	};

	inline EFVR_ClientCapabilities operator|(EFVR_ClientCapabilities a, EFVR_ClientCapabilities b)
	{
		return static_cast<EFVR_ClientCapabilities>(static_cast<int>(a) | static_cast<int>(b));
	}
	inline EFVR_ClientCapabilities operator&(EFVR_ClientCapabilities a, EFVR_ClientCapabilities b)
	{
		return static_cast<EFVR_ClientCapabilities>(static_cast<int>(a) & static_cast<int>(b));
	}

	//! EFVR_ErrorCode enum
	/*! An enum of error codes that the system may return from GetLastError(). These will eventually be mapped to localised strings. */
	enum class EFVR_ErrorCode
	{
		None = 0,

		//! Connection Errors
		Connection_General = 1,
		Connect_NotConnected = 7,
		Connect_ServerUnreachable = 2,
		Connect_RegisterFailed = 3,
		Connect_DeregisterFailed = 6,
		Connect_WrongRuntimeVersion = 4,
		Connect_HeartbeatNoReply = 5,

		//! Data Errors
		Data_General = 10,
		Data_RegisteredWrongVersion = 11,
		Data_UnreadableNotFound = 12,
		Data_NoUpdate = 13,
		Data_Uncalibrated = 14,

		//! Hardware Errors
		Hardware_General = 20,
		Hardware_CoreFault = 21,
		Hardware_CameraFault = 22,
		Hardware_IMUFault = 23,
		Hardware_ScreenFault = 24,
		Hardware_SecurityFault = 25,
		Hardware_Disconnected = 26,
		Hardware_WrongFirmwareVersion = 27,

		//! Server Response Errors
		Server_General = 30,
		Server_HardwareInterfaceInvalid = 31,
		Server_HeartbeatNotRegistered = 32,
		Server_DataCreationError = 33,
		Server_ModuleError_ET = 34,

		//! Code and placeholders
		Code_NotImplementedYet = 40,
		Code_FunctionDeprecated = 41,

		//! Position Tracking
		Position_NoObjectsInView = 50,
		Position_NoDlibRegressor = 51,
		Position_NoCascadeClassifier = 52,
		Position_NoModel = 53,
		Position_NoImages = 54,
		Position_InvalidFile = 55,
		Position_NoCamParaSet = 56,
		Position_CantUpdateOptical = 57,
		Position_ObjectNotTracked = 58,
		
		//! Eye Tracking
		Eye_Left_NoDlibRegressor = 60,
		Eye_Right_NoDlibRegressor = 61,
	};

	//! EFVR_DataType enum
	/*! The different data types that are passed internally within the FoveVR system.
		Clients can subscribe to these (the default is to subscribe all at the moment).
	*/
	enum class EFVR_DataType
	{
		HeadsetState = 0,	// headsetState -> SFVR_HeadsetState_2
		Orientation = 1,	// headOrientation -> SFVR_HeadOrientation		// deprecated. replaced with Position entirely now (contains orientation AND estimated position)
		Position = 2,		// pose -> SFVR_Positions						// updated from SFVR_Pose as that had no ability to hold predicted results
		Gaze = 3,			// gaze -> SFVR_Gaze_2
		ImageData = 4,		// eyeImage -> SFVR_EyeImage
		Message = 5,		// message -> char* \0 terminated
		PositionImage = 6	// posImage -> SFVR_EyeImage
	};

	//! EFVR_ClientType
	/*! Corresponds to the order in which clients are composited (Base, then Overlay, then Diagnostic) */
	enum class EFVR_ClientType
	{
		Base = 0,
		OverlayWorld = 0x10000,
		OverlayScreen = 0x20000,
		Diagnostic = 0x30000
	};

	//! SFVR_Quaternion struct
	/*! A quaternion represents an orientation in 3d space.*/
	struct SFVR_Quaternion
	{
		float x = 0;
		float y = 0;
		float z = 0;
		float w = 1;

		SFVR_Quaternion() = default;
		SFVR_Quaternion(float ix, float iy, float iz, float iw) : x(ix), y(iy), z(iz), w(iw) {}

		//! Generate and return a conjugate of this quaternion
		SFVR_Quaternion Conjugate() const
		{
			return SFVR_Quaternion(-x, -y, -z, w);
		}

		SFVR_Quaternion Normalize() const
		{
			float d = sqrt(w*w + x*x + y*y + z*z);
			SFVR_Quaternion result(x / d, y / d, z / d, w / d);
			return result;
		}

		//! Return the result of multiplying this quaternion Q1 by another Q2 such that OUT = Q1 * Q2
		SFVR_Quaternion MultiplyBefore(const SFVR_Quaternion &second) const
		{
			auto nx =  x * second.w + y * second.z - z * second.y + w * second.x;
			auto ny = -x * second.z + y * second.w + z * second.x + w * second.y;
			auto nz =  x * second.y - y * second.x + z * second.w + w * second.z;
			auto nw = -x * second.x - y * second.y - z * second.z + w * second.w;
			return SFVR_Quaternion(nx, ny, nz, nw);
		}

		//! Return the result of multiplying this quaternion Q2 by another Q1 such that OUT = Q1 * Q2
		SFVR_Quaternion MultiplyAfter(const SFVR_Quaternion &first) const
		{
			auto nx =  first.x * w + first.y * z - first.z * y + first.w * x;
			auto ny = -first.x * z + first.y * w + first.z * x + first.w * y;
			auto nz =  first.x * y - first.y * x + first.z * w + first.w * z;
			auto nw = -first.x * x - first.y * y - first.z * z + first.w * w;
			return SFVR_Quaternion(nx, ny, nz, nw);
		}
	};

	//! SFVR_HeadOrientation struct
	/*! Represents the orientation of the Fove headset in 3d space along with some meta-information about how old this data is. */
	struct SFVR_HeadOrientation
	{
		//! Error Code
		/*! error:	if true => the rest of the data is in an unknown state. */
		EFVR_ErrorCode error = EFVR_ErrorCode::None;
		//! ID
		/*! Incremental counter which tells if the coord captured is a fresh value at a given frame */
		uint64_t id = 0;
		//! Timestamp
		/*! The time at which the coord was captured, based on system time */
		uint64_t timestamp = 0;
		//! Quaternion
		/*! The Quaternion which represents the orientation of the head. */
		SFVR_Quaternion quat;
	};

	//! SFVR_Vec3 struct
	/*! A vector that represents an position in 3d space. */
	struct SFVR_Vec3
	{
		float x = 0;
		float y = 0;
		float z = 0;

		SFVR_Vec3() = default;
		SFVR_Vec3(float ix, float iy, float iz) : x(ix), y(iy), z(iz) {}
	};

	//! SFVR_Vec2 struct
	/*! A vector that represents an position in 2d space. Usually used when refering to screen or image coordinates. */
	struct SFVR_Vec2
	{
		float x = 0;
		float y = 0;

		SFVR_Vec2() = default;
		SFVR_Vec2(float ix, float iy) : x(ix), y(iy) {}
	};

	//! SFVR_Pose struct
	/*! This structure is a combination of the Fove headset position and orientation in 3d space, collectively known as the "pose".
		In the future this may also contain accelleration information for the headset, and may also be used for controllers.
	*/
	struct SFVR_Pose
	{
		//! Error Code
		/*! error:	if true => the rest of the data is in an unknown state. */
		EFVR_ErrorCode error = EFVR_ErrorCode::None;
		//! ID
		/*! Incremental counter which tells if the coord captured is a fresh value at a given frame */
		uint64_t id = 0;
		//! Timestamp
		/*! The time at which the coord was captured, based on system time */
		uint64_t timestamp = 0;
		//! Quaternion
		/*! The Quaternion which represents the orientation of the head. */
		SFVR_Quaternion orientation;
		//! Vector3
		/*! The position of headset in 3D space */
		SFVR_Vec3 position;
	};

	//! SFVR_WorldGaze struct
	/*! FUTURE USE ONLY: The vector (from the center of the player's head in game space) that represents the point that the user is looking at.
		The accuracy is the radius of the sphere in meters of this information.
	*/
	struct SFVR_WorldGaze
	{
		EFVR_ErrorCode error = EFVR_ErrorCode::None;
		uint64_t id = 0;
		uint64_t timestamp = 0;
		float accuracy = 0;
		SFVR_Vec3 left_vec = { 0, 0, 1 };
		SFVR_Vec3 right_vec = { 0, 0, 1 };
		SFVR_Vec3 convergence = { 0, 0, 1 };
	};

	//! SFVR_GazeScreenCoord struct
	/*! The coordinate on the screen that the user is looking at. (0,0 is the top left) */
	struct SFVR_GazeScreenCoord
	{
		//! Error Code
		/*! error:	if true => the rest of the data is in an unknown state. */
		EFVR_ErrorCode error = EFVR_ErrorCode::None;
		//! ID
		/*! Incremental counter which tells if the coord captured is a fresh value at a given frame */
		uint64_t id = 0;
		//! Timestamp
		/*! The time at which the coord was captured, based on system time */
		uint64_t timestamp = 0;
		//! Vector2
		/*! The Coordinate position is based on the Normalized 0,1 plane */
		SFVR_Vec2 coord;
	};

	//! SFVR_EyeImage struct
	/*! DEPRICATED: For researcher use only, To be Moved to Fove Internal Types */
	struct SFVR_EyeImage
	{
		EFVR_ErrorCode error = EFVR_ErrorCode::None;
		uint8_t eye = 0;
		uint64_t frameNumber = 0;
		uint32_t length = 0;
		uint64_t timestamp = 0;
		unsigned char* imageData = nullptr;
	};

	//! SFVR_CalibrationTarget struct
	/*! To be Moved to Fove Internal Types */
	struct SFVR_CalibrationTarget
	{
		bool isCalibrationComplete = false;
		float x = 0;
		float y = 0;
		float z = 0;
		float scale = 0;
	};

	//! EFVR_Eye enum
	/*! This is usually returned with any eye tracking information and tells the client which eye(s) the information is based on. */
	enum class EFVR_Eye
	{
		Neither = 0,
		Left = 1,
		Right = 2,
		Both = 3
	};

	//! SFVR_Matrix44 struct
	/*! A rectangular array of numbers, symbols, or expressions, arranged in rows and columns.  */
	struct SFVR_Matrix44
	{
		float mat[4][4] = {};
	};

	//! SFVR_Matrix34 struct
	/*! A rectangular array of numbers, symbols, or expressions, arranged in rows and columns.  */
	struct SFVR_Matrix34
	{
		float mat[3][4] = {};
	};

	//! EFVR_CompositorError enum
	/*! Errors pertaining to Compositor */
	enum class EFVR_CompositorError
	{
		None = 0,

		UnableToCreateDeviceAndContext = 100,
		UnableToUseTexture = 101,
		DeviceMismatch = 102,
		IncompatibleCompositorVersion = 103,

		UnableToFindRuntime = 200,
		RuntimeAlreadyClaimed = 201,
		DisconnectedFromRuntime = 202,

		ErrorCreatingShaders = 300,
		ErrorCreatingTexturesOnDevice = 301,

		NoEyeSpecifiedForSubmit = 400,

		UnknownError = 99999,
	};

	//! EFVR_GraphicsAPI enum
	/*! Type of Graphics API */
	enum class EFVR_GraphicsAPI
	{
		DirectX,
		OpenGL
	};

	//! SFVR_CompositorTexture struct
	/*! Texture structure used by the Compositor */
	struct SFVR_CompositorTexture
	{
		//! Texture Pointer
		/*! D3D: Native texture pointer
			OpenGL: Pointer to a texture ID */
		void* pTexture = nullptr;
		EFVR_GraphicsAPI api = EFVR_GraphicsAPI::DirectX;
	};

	//! SFVR_TextureBounds struct
	/*! Coordinates in normalized space where 0 is left/top and 1 is bottom/right */
	struct SFVR_TextureBounds
	{
		float left = 0, top = 0, right = 0, bottom = 0;
	};

	// Logging extensions to write common types to the fove log
	class Log;
	Log& operator << (Log&, EFVR_ClientCapabilities);
	Log& operator << (Log&, EFVR_ErrorCode);
	Log& operator << (Log&, EFVR_DataType);
	Log& operator << (Log&, EFVR_ClientType);
	Log& operator << (Log&, EFVR_Eye);
	Log& operator << (Log&, EFVR_CompositorError);
	Log& operator << (Log&, EFVR_GraphicsAPI);
	Log& operator << (Log&, const SFVR_Quaternion&);
	Log& operator << (Log&, const SFVR_HeadOrientation&);
	Log& operator << (Log&, const SFVR_Vec3&);
	Log& operator << (Log&, const SFVR_Vec2&);
	Log& operator << (Log&, const SFVR_Pose&);
	Log& operator << (Log&, const SFVR_WorldGaze&);
	Log& operator << (Log&, const SFVR_GazeScreenCoord&);
	Log& operator << (Log&, const SFVR_EyeImage&);
	Log& operator << (Log&, const SFVR_CalibrationTarget&);
	Log& operator << (Log&, const SFVR_Matrix44&);
	Log& operator << (Log&, const SFVR_Matrix34&);
	Log& operator << (Log&, const SFVR_TextureBounds&);
}
#endif // _FOVETYPES_H