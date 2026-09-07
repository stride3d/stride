// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.IO;
using Xunit;

namespace Stride.CrashReport.Tests;

public class SenderTests
{
    [Fact]
    public async Task SendToUnreachableDsnThrowsSoTheReportIsKept()
    {
        // Every sender deletes a report once "sent". The SDK only logs a transport failure, so the sender must
        // turn an upload that never landed into an exception, or an offline user's report would be lost silently.
        var crash = StoredCrash.FromReportData(new CrashReportData { ["Application"] = "TestProbe", ["Exception"] = "boom" });
        crash.Application = "TestProbe";
        crash.Version = "0.0.0-test";
        crash.Signature = "test";

        // Port 9 (discard) on loopback: the connection is refused at once, no network needed.
        var exception = await Assert.ThrowsAsync<IOException>(() => CrashReportSender.SendAsync(crash, dump: null, dsn: "https://0123456789abcdef0123456789abcdef@127.0.0.1:9/1"));
        Assert.StartsWith("Crash report upload", exception.Message);
    }
}
