#ifndef setjmp_h
#define setjmp_h

#include "../NativePath.h"

#undef jmp_buf
//we oversize our jmp_buf for safety
typedef void* jmp_buf[64];

#endif