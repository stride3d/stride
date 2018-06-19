/*
Copyright (c) 2015 Giovanni Petrantoni

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

//
//  NativeMemory.h
//  NativePath
//
//  Created by Giovanni Petrantoni on 11/18/15.
//  Copyright © 2015 Giovanni Petrantoni. All rights reserved.
//

#ifndef NativeMemory_h
#define NativeMemory_h

#include "NativePath.h"

#ifdef __cplusplus
extern "C" {
#endif

extern void* npMalloc(size_t size);
#define malloc npMalloc

extern void npFree(void* block);
#define free npFree

extern void* npRealloc(void* ptr, size_t size);
#define realloc npRealloc

extern size_t npMallocSize(void* ptr);
#define malloc_size npMallocSize

extern void* npCalloc(size_t num, size_t size);
#define calloc npCalloc

#ifdef __cplusplus
}
#endif

#endif /* NativeMemory_h */
