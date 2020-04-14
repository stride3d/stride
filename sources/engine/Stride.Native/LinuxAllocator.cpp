// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// Provide implementation for new/delete operator for C++ code but only for our Linux
// implementation as other implementations seems to be able to get it otherwise

#if PLATFORM_LINUX

#include "../../deps/NativePath/NativeMemory.h"


void* operator new(size_t sz) {
    return calloc(sz, 1);
}
void operator delete(void* ptr) noexcept
{
	free(ptr);
}

#endif
