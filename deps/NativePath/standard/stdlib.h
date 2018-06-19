#ifndef stdlib_h
#define stdlib_h

#include "../NativePath.h"
#include "../NativeMemory.h"

#ifdef __cplusplus
extern "C" {
#endif

//TODO more stdlib stuff

#undef exit
#define exit npExit

extern void npExit(int code);

#undef qsort
#define qsort npQsort

extern void npQsort(void *base, size_t nitems, size_t size, int (*compar)(const void *, const void*));

#undef rand
#define rand npRand

extern int npRand();

#ifdef __cplusplus
}
#endif

#endif