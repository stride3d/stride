// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#include "../../../../deps/NativePath/NativeThreading.h"
#include "CoreNative.h"

#ifdef __cplusplus
extern "C" {
#endif

	DLL_EXPORT_API void cnSleep(int milliseconds)
	{
		npThreadSleep(milliseconds);
	}

	DLL_EXPORT_API void cnSetup(void* printDebugPtr)
	{
		cnDebugPrintLine = (CnPrintDebugFunc)printDebugPtr;
	}

#ifdef __cplusplus
}
#endif
