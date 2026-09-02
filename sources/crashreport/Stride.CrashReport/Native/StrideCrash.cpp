// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// Native crash trigger for out-of-process capture. On a native access violation it spawns the crash
// reporter, which writes the minidump from outside the dying process (MiniDumpWriteDump + ClientPointers),
// then lets the fault propagate so the process terminates as it would have. Windows-only; a no-op stub
// elsewhere (Unix native capture arrives with .NET 12's SetFatalErrorHandler). Kept deliberately small and
// managed-code-free: a managed vectored handler is unsupported (dotnet/runtime#119142), a native one is not.
//
// Stride's native build compiles with clang against the minimal NativePath runtime (no <windows.h>, no wide
// CRT), so the small Win32 surface used here is declared directly and wide-string helpers are hand-rolled.

#include <stdint.h>

#if defined(_WIN32)
#define STRIDE_CRASH_API __declspec(dllexport)
#else
#define STRIDE_CRASH_API
#endif

#if defined(_WIN32)

// ---- Minimal Win32 surface (x64, undecorated exports; kernel32 is on the link line) -------------------------

extern "C" {
typedef struct _EXCEPTION_RECORD {
    uint32_t ExceptionCode;
    uint32_t ExceptionFlags;
    struct _EXCEPTION_RECORD* ExceptionRecord;
    void* ExceptionAddress;
    uint32_t NumberParameters;
    uint32_t __unusedAlignment;
    uintptr_t ExceptionInformation[15];
} EXCEPTION_RECORD;

typedef struct _EXCEPTION_POINTERS {
    EXCEPTION_RECORD* ExceptionRecord;
    void* ContextRecord;
} EXCEPTION_POINTERS;

typedef struct _STARTUPINFOW {
    uint32_t cb;
    void* lpReserved;
    void* lpDesktop;
    void* lpTitle;
    uint32_t dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
    uint16_t wShowWindow, cbReserved2;
    void* lpReserved2;
    void* hStdInput; void* hStdOutput; void* hStdError;
} STARTUPINFOW;

typedef struct _PROCESS_INFORMATION {
    void* hProcess; void* hThread; uint32_t dwProcessId; uint32_t dwThreadId;
} PROCESS_INFORMATION;

void* AddVectoredExceptionHandler(uint32_t First, void* Handler);
int   GetModuleHandleExW(uint32_t dwFlags, const wchar_t* lpModuleName, void** phModule);
uint32_t GetModuleFileNameW(void* hModule, wchar_t* lpFilename, uint32_t nSize);
uint32_t GetCurrentProcessId(void);
uint32_t GetCurrentThreadId(void);
void* CreateEventW(void* lpEventAttributes, int bManualReset, int bInitialState, const wchar_t* lpName);
uint32_t WaitForSingleObject(void* hHandle, uint32_t dwMilliseconds);
int   CloseHandle(void* hObject);
int   CreateProcessW(const wchar_t* lpApplicationName, wchar_t* lpCommandLine, void* lpProcessAttributes,
                     void* lpThreadAttributes, int bInheritHandles, uint32_t dwCreationFlags, void* lpEnvironment,
                     const wchar_t* lpCurrentDirectory, void* lpStartupInfo, void* lpProcessInformation);
}

static const uint32_t kExceptionContinueSearch = 0;
static const uint32_t kExceptionAccessViolation = 0xC0000005u;
static const uint32_t kFromAddressUnchanged = 0x00000004u /* FROM_ADDRESS */ | 0x00000002u /* UNCHANGED_REFCOUNT */;
static const uint32_t kCreateNoWindow = 0x08000000u;

// ---- Tiny wide-string helpers (no CRT wide functions available) ---------------------------------------------

static uint32_t w_len(const wchar_t* s) { uint32_t n = 0; if (s) while (s[n]) n++; return n; }

static void w_put(wchar_t* dst, uint32_t cap, uint32_t* pos, wchar_t c) {
    if (*pos + 1 < cap) dst[(*pos)++] = c;
}
static void w_puts(wchar_t* dst, uint32_t cap, uint32_t* pos, const wchar_t* s) {
    if (s) while (*s) w_put(dst, cap, pos, *s++);
}
static void w_putu(wchar_t* dst, uint32_t cap, uint32_t* pos, unsigned long long v) {
    wchar_t tmp[24]; int i = 0;
    if (v == 0) tmp[i++] = L'0';
    while (v) { tmp[i++] = (wchar_t)(L'0' + (v % 10)); v /= 10; }
    while (i > 0) w_put(dst, cap, pos, tmp[--i]);
}
static void w_copy(wchar_t* dst, uint32_t cap, const wchar_t* s) {
    uint32_t i = 0; if (s) while (s[i] && i + 1 < cap) { dst[i] = s[i]; i++; } dst[i] = 0;
}
// Case-insensitive ASCII compare (module file names are ASCII).
static bool w_ieq_ascii(const wchar_t* a, const wchar_t* b) {
    for (;; a++, b++) {
        wchar_t ca = *a, cb = *b;
        if (ca >= L'A' && ca <= L'Z') ca = (wchar_t)(ca - L'A' + L'a');
        if (cb >= L'A' && cb <= L'Z') cb = (wchar_t)(cb - L'A' + L'a');
        if (ca != cb) return false;
        if (ca == 0) return true;
    }
}

// ---- Handler ------------------------------------------------------------------------------------------------

static wchar_t g_reporterPath[1024];
static wchar_t g_dumpDir[1024];
static uint32_t g_timeoutMs = 0;
static long g_handled = 0; // dump at most once

// Decides whether an access violation is a genuine native crash, vs the CLR's own hardware null-check (which
// becomes a NullReferenceException the runtime handles). The signal is where the faulting instruction lives:
// JIT'd managed code is backed by no module. Mirrors ShouldCaptureNativeFault in NativeCrashHandler.cs.
static bool should_capture(EXCEPTION_RECORD* record) {
    if (record->ExceptionCode != kExceptionAccessViolation)
        return false;

    void* ip = record->ExceptionAddress;
    uintptr_t faultAddr = record->NumberParameters >= 2 ? record->ExceptionInformation[1] : 0;

    void* mod = nullptr;
    if (!GetModuleHandleExW(kFromAddressUnchanged, (const wchar_t*)ip, &mod) || mod == nullptr)
        return faultAddr >= 0x10000; // no backing module -> JIT'd managed code; sub-64 KB is a hardware null-check

    wchar_t path[260];
    if (GetModuleFileNameW(mod, path, 260) > 0) {
        const wchar_t* file = path;
        for (const wchar_t* p = path; *p; p++) if (*p == L'\\' || *p == L'/') file = p + 1;
        if (w_ieq_ascii(file, L"coreclr.dll") || w_ieq_ascii(file, L"clrjit.dll") ||
            w_ieq_ascii(file, L"clrgc.dll") || w_ieq_ascii(file, L"clrgcexp.dll"))
            return faultAddr >= 0x10000; // runtime modules also fixup managed null checks
    }
    return true; // native module: even a low-address dereference is a real crash
}

// Runs in the raw native-fault context (registered last, first=0, so the runtime's own handler redirects managed
// null checks first). Spawns the reporter to capture from outside, waits for it, then lets the fault propagate.
static long on_vectored(EXCEPTION_POINTERS* info) {
    if (info == nullptr || info->ExceptionRecord == nullptr)
        return kExceptionContinueSearch;
    if (!should_capture(info->ExceptionRecord))
        return kExceptionContinueSearch;
    if (__sync_lock_test_and_set(&g_handled, 1) != 0)
        return kExceptionContinueSearch;

    unsigned long long pid = GetCurrentProcessId();
    unsigned long long tid = GetCurrentThreadId();

    // The reporter signals this once the out-of-process dump is written; we stay frozen until then so the
    // crashing thread's context and memory remain readable across the process boundary.
    wchar_t eventName[64]; uint32_t ep = 0;
    w_puts(eventName, 64, &ep, L"Local\\StrideCrash_");
    w_putu(eventName, 64, &ep, pid); w_put(eventName, 64, &ep, L'_'); w_putu(eventName, 64, &ep, tid);
    eventName[ep] = 0;
    void* ev = CreateEventW(nullptr, 1 /* manual reset */, 0, eventName);

    // "<reporter>" --capture <pid> <tid> <exception-pointers-addr> --event <name> --dump-dir "<dir>"
    wchar_t cmd[2400]; uint32_t cp = 0;
    w_put(cmd, 2400, &cp, L'"'); w_puts(cmd, 2400, &cp, g_reporterPath); w_put(cmd, 2400, &cp, L'"');
    w_puts(cmd, 2400, &cp, L" --capture "); w_putu(cmd, 2400, &cp, pid);
    w_put(cmd, 2400, &cp, L' '); w_putu(cmd, 2400, &cp, tid);
    w_put(cmd, 2400, &cp, L' '); w_putu(cmd, 2400, &cp, (unsigned long long)(uintptr_t)info);
    w_puts(cmd, 2400, &cp, L" --event "); w_puts(cmd, 2400, &cp, eventName);
    w_puts(cmd, 2400, &cp, L" --dump-dir \""); w_puts(cmd, 2400, &cp, g_dumpDir); w_put(cmd, 2400, &cp, L'"');
    cmd[cp] = 0;

    STARTUPINFOW si; for (uint32_t i = 0; i < sizeof(si); i++) ((char*)&si)[i] = 0; si.cb = sizeof(si);
    PROCESS_INFORMATION pi; for (uint32_t i = 0; i < sizeof(pi); i++) ((char*)&pi)[i] = 0;
    if (CreateProcessW(nullptr, cmd, nullptr, nullptr, 0, kCreateNoWindow, nullptr, nullptr, &si, &pi)) {
        if (ev != nullptr) WaitForSingleObject(ev, g_timeoutMs);
        CloseHandle(pi.hThread);
        CloseHandle(pi.hProcess);
    }
    if (ev != nullptr) CloseHandle(ev);

    return kExceptionContinueSearch; // observe only: let the process terminate as it would have
}
#endif // _WIN32

extern "C" {

// Registers the native vectored exception handler. Called once from healthy managed code with the reporter
// path already resolved (so nothing is resolved in the fault context). reporterPath/dumpDir are UTF-16;
// timeoutMs bounds the wait for the reporter to finish capturing before the process dies.
STRIDE_CRASH_API void stride_crash_install(const wchar_t* reporterPath, const wchar_t* dumpDir, uint32_t timeoutMs)
{
#if defined(_WIN32)
    w_copy(g_reporterPath, 1024, reporterPath);
    w_copy(g_dumpDir, 1024, dumpDir);
    g_timeoutMs = timeoutMs;
    AddVectoredExceptionHandler(0 /* first=0: after the runtime's own handler */, (void*)&on_vectored);
#else
    (void)reporterPath;
    (void)dumpDir;
    (void)timeoutMs;
#endif
}

}
