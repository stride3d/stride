// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#ifndef _CoreNative_h_
#define _CoreNative_h_

/*
 * Some platforms requires a special declaration before the function declaration to export them 
 * in the shared library. Defining NEED_DLL_EXPORT will define DLL_EXPORT_API to do the right thing
 * for those platforms.
 *
 * To export void foo(int a), do:
 *
 *   DLL_EXPORT_API void foo (int a);
 */
#ifdef NEED_DLL_EXPORT
#define DLL_EXPORT_API __declspec(dllexport)
#else
#define DLL_EXPORT_API
#endif

typedef void(*CnPrintDebugFunc)(const char* string);

DLL_EXPORT_API CnPrintDebugFunc cnDebugPrintLine;

#endif

