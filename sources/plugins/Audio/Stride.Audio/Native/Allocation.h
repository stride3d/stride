// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#pragma once

#include "../../../deps/NativePath/NativePath.h"
#include "../../../deps/NativePath/NativeMemory.h"

// The Linux library is linked without a C++ runtime, so the global operator new/delete are not available:
// classes created with new get their own on top of malloc/free, and so do the tinystl containers.
#define XN_MALLOC_NEW_DELETE \
	static void* operator new(size_t size) { return malloc(size); } \
	static void operator delete(void* ptr) { free(ptr); }

// Has to be included before the TINYSTL headers.
#ifdef TINYSTL_ALLOCATOR_H
#error "Allocation.h has to be included before the TINYSTL headers"
#endif
struct xnAllocator
{
	static void* static_allocate(size_t bytes) { return malloc(bytes); }
	static void static_deallocate(void* ptr, size_t /*bytes*/) { free(ptr); }
};
#define TINYSTL_ALLOCATOR xnAllocator
