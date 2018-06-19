/*!****************************************************************************

 @file         PVRTError.h
 @copyright    Copyright (c) Imagination Technologies Limited.
 @brief        PVRT error codes.  

******************************************************************************/
#ifndef _PVRTERROR_H_
#define _PVRTERROR_H_

#if defined(ANDROID)
	#include <android/log.h>
#else
	#if defined(_WIN32)
		#include <windows.h>
	#else
		#include <stdio.h>
	#endif
#endif
/*!***************************************************************************
 Macros
*****************************************************************************/

/*! Outputs a string to the standard error if built for debugging. */
#if !defined(PVRTERROR_OUTPUT_DEBUG)
	#if defined(_DEBUG) || defined(DEBUG)
		#if defined(ANDROID)
			#define PVRTERROR_OUTPUT_DEBUG(A) __android_log_print(ANDROID_LOG_INFO, "PVRTools", A);
		#elif defined(_WIN32) && !defined(UNDER_CE)
			#define PVRTERROR_OUTPUT_DEBUG(A) OutputDebugStringA(A);
		#else
			#define PVRTERROR_OUTPUT_DEBUG(A) fprintf(stderr,"%s",A);
		#endif
	#else
		#define PVRTERROR_OUTPUT_DEBUG(A)
	#endif
#endif


/*!***************************************************************************
 Enums
*****************************************************************************/
/*!***************************************************************************
 @enum  			EPVRTError
 @brief         	EPVRT error conditions.
*****************************************************************************/
enum EPVRTError
{
	PVR_SUCCESS = 0,    /*!< Success! :D */
	PVR_FAIL = 1,       /*!< Failed :( */
	PVR_OVERFLOW = 2    /*!< Overflow error :| */
};

/*!***************************************************************************
 @brief     		Outputs a string to the standard error.
 @param[in]			format		printf style format followed by arguments it requires.
*****************************************************************************/
void PVRTErrorOutputDebug(char const * const format, ...);

#endif // _PVRTERROR_H_

/*****************************************************************************
End of file (PVRTError.h)
*****************************************************************************/

