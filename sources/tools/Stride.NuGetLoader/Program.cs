using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Stride.Core.Assets;

namespace Stride.NuGetLoader
{
    class Program
    {
#if STRIDE_STA_THREAD_ATTRIBUTE_ON_MAIN
        [STAThread]
#endif
        static void Main(string[] args)
        {
            // Get loader data (text file, format is "PackageName/PackageId")
            var loaderDataFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Stride.NuGetLoader.loaderdata");
            var loaderData = File.ReadLines(loaderDataFile).First().Split('/');

            var packageName = loaderData[0];
            var packageVersion = loaderData[1];

            NuGetAssemblyResolver.SetupNuGet(packageName, packageVersion);
            AppDomain.CurrentDomain.ExecuteAssemblyByName(packageName, args);
        }
    }
}
