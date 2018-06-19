/*
Copyright (c) 2016 Giovanni Petrantoni

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
//  NativeThreading.h
//  NativePath
//
//  Created by Giovanni Petrantoni on 06/20/16.
//  Copyright © 2016 Giovanni Petrantoni. All rights reserved.
//

#ifndef NativeThreading_h
#define NativeThreading_h

#ifdef __cplusplus
extern "C" {
#endif

typedef void* Thread;
typedef void (*npThreadDelegate)(void);

extern Thread npThreadStart(npThreadDelegate func);
extern void npThreadJoin(Thread thread);
extern void npThreadSleep(int milliseconds);
extern void npThreadYield();

#ifdef __cplusplus
}
#endif

#endif /* NativeThreading_h */