// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#ifndef ATITC_LIB_WRAPPER_H
#define ATITC_LIB_WRAPPER_H

#define ATITC_API __declspec(dllexport)

#include "ATI_Compress.h"

extern "C" {

    ATITC_API int atitcCalculateBufferSize(const ATI_TC_Texture* pTexture);
    ATITC_API ATI_TC_ERROR  atitcConvertTexture(const ATI_TC_Texture* pSourceTexture, ATI_TC_Texture* pDestTexture, const ATI_TC_CompressOptions* pOptions);
	ATITC_API void atitcDeleteData(ATI_TC_Texture* pTexture);

}; // extern "C"



#endif // ATITC_LIB_WRAPPER_H
