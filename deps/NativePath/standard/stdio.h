#ifndef stdio_h
#define stdio_h

#include "../NativePath.h"
#include "stdarg.h"

//TODO more stdio stuff

#ifdef __cplusplus
extern "C" {
#endif

typedef void FILE;

#undef stderr
#define stderr npGetStderr()
#undef stdin
#define stdin npGetStdin()
#undef stdout
#define stdout npGetStdout()
#undef fflush
#define fflush npFflush
#undef printf
#define printf npPrintf
#undef sprintf
#define sprintf npSprintf
#undef fprintf
#define fprintf npFprintf
#undef vsnprintf
#define vsnprintf npVsnprintf

extern FILE* npGetStderr();
extern FILE* npGetStdin();
extern FILE* npGetStdout();
extern int npFflush(FILE* file);
extern int npPrintf(const char* format, ...);
extern int npSprintf(char* buffer, const char* format, ...);
extern int npFprintf(FILE *stream, const char *format, ...);
extern int npVsnprintf(char * s, size_t n, const char * format, va_list arg);

#ifdef __cplusplus
}
#endif

#endif