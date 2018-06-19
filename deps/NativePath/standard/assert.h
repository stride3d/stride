#ifndef assert_h
#define assert_h

#include "../NativePath.h"
#include "stdio.h"
#include "stdbool.h"

#ifdef __cplusplus
extern "C" {
#endif

#undef assert
inline void assert(bool condition)
{
    if(!condition)
    {
        printf("Assert condition failed");
        debugtrap();
        abort();
    }
}

#ifdef __cplusplus
}
#endif

#endif