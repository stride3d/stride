// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// Headless runtime check, run by the packaging tests after building: the compiled content must
// resolve from the deployed database by rooted URL and by bare URL through the alias table.
// Arguments override the URLs to check.
using Stride.Core.IO;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;

var content = new ContentManager(new DatabaseFileProviderService(new DatabaseFileProvider(ObjectDatabase.CreateDefaultDatabase())));

var urls = args.Length > 0 ? args : ["/StrideAssetPlugin/PluginPage", "PluginPage", "/Consumer/Page", "Page"];
foreach (var url in urls)
{
    if (!content.Exists(url))
    {
        System.Console.WriteLine($"CONTENT MISSING {url}");
        return 1;
    }
    using var stream = content.OpenAsStream(url);
    System.Console.WriteLine($"CONTENT OK {url} {stream.Length}");
}
return 0;
