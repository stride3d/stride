// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Runtime.InteropServices;
using Stride.CrashReport;

// Crash probe for Stride.CrashReport.Tests: each mode arms the crash capture and then dies (or survives)
// in a specific way, so the test can assert the capture truth table per platform. The capture behavior
// depends on runtime internals (corrupted-state fast-fail, vectored handler ordering), so these probes
// exist to catch silent behavior changes on .NET upgrades.
//
// Usage: Stride.CrashReport.TestProbe <work-dir> <mode>

if (args.Length < 2)
{
    Console.Error.WriteLine("usage: Stride.CrashReport.TestProbe <work-dir> <mode>");
    return 2;
}

var dir = args[0];
var mode = args[1];
var marker = Path.Combine(dir, "callback-marker.txt");

if (mode == "store")
{
    // Integration path: the public API products call. STRIDE_CRASH_DIR points the store at the work dir.
    NativeCrashReporting.Install("TestProbe");
    Trigger.NativeAccessViolation();
    Console.Error.WriteLine("probe: survived the AV (unexpected)");
    return 3;
}

// Handler-level paths: arm the same shared handler the products use, with a marker callback standing in
// for "spawn the reporter".
// Marker records the dump path and, on the second line, the faulting frame the handler resolved
// ("module+0x<rva>" for a real native fault via the vectored handler; empty for the FirstChance/SEH path).
Stride.NativeCrashHandler.InstallForReporting(dir, Stride.NativeCrashHandler.TriageDump,
    (dumpPath, faultingFrame) => File.WriteAllText(marker, dumpPath + "\n" + (faultingFrame ?? "")));

switch (mode)
{
    case "managedthrow":
        // Ordinary software exceptions must dispatch untouched while the handler is armed.
        for (int i = 0; i < 3; i++)
        {
            try { throw new InvalidOperationException("boom " + i); }
            catch (InvalidOperationException) { }
        }
        return 0;

    case "managednull":
        // A managed hardware null-check must stay a catchable NullReferenceException — the handler
        // observing it (or worse, dumping) is the regression class that once made these fatal.
        try
        {
            var s = Trigger.OpaqueNull(args);
            _ = s!.Length;
        }
        catch (NullReferenceException)
        {
            Console.Error.WriteLine("probe: caught NullReferenceException");
            return 0;
        }
        return 3;

    case "av":
        // Wild-pointer write inside native code: the classic importer/driver crash.
        Trigger.NativeAccessViolation();
        break;

    case "nullnative":
        // Native null dereference: faults below 64 KB but the faulting instruction is native — the
        // instruction-based filter must capture it (an address-only filter missed it).
        Trigger.NativeNullDereference();
        break;

    case "raise":
        // Native SEH exception surfacing as managed SEHException (Windows only): the FirstChanceException leg.
        Trigger.RaiseSehException();
        break;

    case "stackoverflow":
        // Known in-process gap: no dump possible, but the process must terminate (never hang).
        Trigger.StackOverflow(0);
        break;

    default:
        Console.Error.WriteLine($"unknown mode '{mode}'");
        return 2;
}

Console.Error.WriteLine("probe: survived the crash (unexpected)");
return 3;

internal static partial class Trigger
{
    [DllImport("ucrtbase.dll", EntryPoint = "memset", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr MemsetWindows(IntPtr dest, int c, IntPtr count);

    [DllImport("libc", EntryPoint = "memset")]
    private static extern IntPtr MemsetUnix(IntPtr dest, int c, IntPtr count);

    [DllImport("kernel32.dll")]
    private static extern void RaiseException(uint dwExceptionCode, uint dwExceptionFlags, uint nNumberOfArguments, IntPtr lpArguments);

    private static IntPtr Memset(IntPtr dest, int c, IntPtr count)
        => OperatingSystem.IsWindows() ? MemsetWindows(dest, c, count) : MemsetUnix(dest, c, count);

    // A write to a high unmapped address, faulting with the instruction pointer inside libc/ucrtbase.
    public static void NativeAccessViolation() => Memset(unchecked((IntPtr)0x00007FFFDEADBEEF), 0, 64);

    // A write to address 0 from native code.
    public static void NativeNullDereference() => Memset(IntPtr.Zero, 0, 64);

    public static void RaiseSehException() => RaiseException(0xE0000001, 0, 0, IntPtr.Zero);

    // Null the JIT cannot see through, so the null-check is a hardware fault, not a software throw.
    public static string? OpaqueNull(string[] args) => args.Length > 99 ? "x" : null;

    public static int StackOverflow(int depth)
    {
        // The span keeps frames fat (fast overflow) and the return value defeats tail-call optimization.
        Span<byte> pad = stackalloc byte[1024];
        pad[0] = (byte)depth;
        return StackOverflow(depth + 1) + pad[0];
    }
}
