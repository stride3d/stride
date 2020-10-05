// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.ProjectModel;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Stride.Core.Assets
{
    class NuGetAssemblyResolver
    {
        public const string DevSource = @"%LocalAppData%\Stride\NugetDev";

        static bool assembliesResolved;
        static object assembliesLock = new object();
        static List<string> assemblies;

        internal static void DisableAssemblyResolve()
        {
            assembliesResolved = true;
        }

        internal static void SetupNuGet(string packageName, string packageVersion)
        {
            // Make sure our nuget local store is added to nuget config
            var folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string strideFolder = null;
            while (folder != null)
            {
                if (File.Exists(Path.Combine(folder, @"build\Stride.sln")))
                {
                    strideFolder = folder;
                    var settings = NuGet.Configuration.Settings.LoadDefaultSettings(null);

                    Directory.CreateDirectory(Environment.ExpandEnvironmentVariables(DevSource));
                    CheckPackageSource(settings, "Stride Dev", NuGet.Configuration.Settings.ApplyEnvironmentTransform(DevSource));

                    settings.SaveToDisk();
                    break;
                }
                folder = Path.GetDirectoryName(folder);
            }

            // Note: we perform nuget restore inside the assembly resolver rather than top level module ctor (otherwise it freezes)
            AppDomain.CurrentDomain.AssemblyResolve += (sender, eventArgs) =>
            {
                // Check if already loaded.
                // Somehow it happens for Microsoft.NET.Build.Tasks -> NuGet.ProjectModel, probably due to the specific way it's loaded.
                var matchingAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.FullName == eventArgs.Name);
                if (matchingAssembly != null)
                    return matchingAssembly;

                if (!assembliesResolved)
                {
                    lock (assembliesLock)
                    {
                        // Note: using NuGet will try to recursively resolve NuGet.*.resources.dll, so set assembliesResolved right away so that it bypasses everything
                        assembliesResolved = true;

                        var logger = new Logger();

#if STRIDE_NUGET_RESOLVER_UX
                        var dialogNotNeeded = new TaskCompletionSource<bool>();
                        var dialogClosed = new TaskCompletionSource<bool>();

                        // Display splash screen after a 500 msec (when NuGet takes some time to restore)
                        var newWindowThread = new Thread(() =>
                        {
                            Thread.Sleep(500);
                            if (!dialogNotNeeded.Task.IsCompleted)
                            {
                                var splashScreen = new Stride.NuGetResolver.SplashScreenWindow();
                                splashScreen.Show();

                                // Register log
                                logger.SetupLogAction((level, message) =>
                                {
                                    splashScreen.Dispatcher.InvokeAsync(() =>
                                    {
                                        splashScreen.AppendMessage(level, message);
                                    });
                                });

                                dialogNotNeeded.Task.ContinueWith(t =>
                                {
                                    splashScreen.Dispatcher.Invoke(() => splashScreen.Close());
                                });

                                splashScreen.Closed += (sender2, e2) =>
                                    splashScreen.Dispatcher.InvokeShutdown();

                                System.Windows.Threading.Dispatcher.Run();

                                splashScreen.Close();
                            }
                            dialogClosed.SetResult(true);
                        });
                        newWindowThread.SetApartmentState(ApartmentState.STA);
                        newWindowThread.IsBackground = true;
                        newWindowThread.Start();
#endif

                        var previousSynchronizationContext = SynchronizationContext.Current;
                        try
                        {
                            // Since we execute restore synchronously, we don't want any surprise concerning synchronization context (i.e. Avalonia one doesn't work with this)
                            SynchronizationContext.SetSynchronizationContext(null);

                            // Determine current TFM
                            var framework = Assembly
                                .GetEntryAssembly()?
                                .GetCustomAttribute<TargetFrameworkAttribute>()?
                                .FrameworkName ?? ".NETFramework,Version=v4.7.2";
                            var nugetFramework = NuGetFramework.ParseFrameworkName(framework, DefaultFrameworkNameProvider.Instance);

                            // Only allow this specific version
                            var versionRange = new VersionRange(new NuGetVersion(packageVersion), true, new NuGetVersion(packageVersion), true);
                            var (request, result) = RestoreHelper.Restore(logger, nugetFramework, "win", packageName, versionRange);
                            if (!result.Success)
                            {
                                throw new InvalidOperationException($"Could not restore NuGet packages");
                            }

                            assemblies = RestoreHelper.ListAssemblies(result.LockFile);
                        }
                        catch (Exception e)
                        {
                            var logText = $@"Error restoring NuGet packages!

==== Exception details ====

{e}

==== Log ====

{string.Join(Environment.NewLine, logger.Logs.Select(x => $"[{x.Level}] {x.Message}"))}
";
#if STRIDE_NUGET_RESOLVER_UX
                            dialogClosed.Task.Wait();
#else
                            // Display log in console
                            Console.WriteLine(logText);
#endif
                            Environment.Exit(1);
                        }
                        finally
                        {
#if STRIDE_NUGET_RESOLVER_UX
                            dialogNotNeeded.TrySetResult(true);
#endif
                            SynchronizationContext.SetSynchronizationContext(previousSynchronizationContext);
                        }
                    }
                }

                if (assemblies != null)
                {
                    var aname = new AssemblyName(eventArgs.Name);
                    if (aname.Name.StartsWith("Microsoft.Build") && aname.Name != "Microsoft.Build.Locator")
                        return null;
                    var assemblyPath = assemblies.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x) == aname.Name);
                    if (assemblyPath != null)
                    {
                        return Assembly.LoadFrom(assemblyPath);
                    }
                }
                return null;
            };
        }

        private static void RemoveSources(ISettings settings, string prefixName)
        {
            var packageSources = settings.GetSection("packageSources");
            if (packageSources != null)
            {
                foreach (var packageSource in packageSources.Items.OfType<SourceItem>().ToList())
                {
                    var path = packageSource.GetValueAsPath();

                    if (packageSource.Key.StartsWith(prefixName))
                    {
                        // Remove entry from packageSources
                        settings.Remove("packageSources", packageSource);
                    }
                }
            }
        }

        private static void CheckPackageSource(ISettings settings, string name, string url)
        {
            settings.AddOrUpdate("packageSources", new SourceItem(name, url));
        }

        public class Logger : ILogger
        {
            private object logLock = new object();
            private Action<LogLevel, string> action;
            public List<(LogLevel Level, string Message)> Logs { get; } = new List<(LogLevel, string)>();

            public void SetupLogAction(Action<LogLevel, string> action)
            {
                lock (logLock)
                {
                    this.action = action;
                    foreach (var log in Logs)
                        action.Invoke(log.Level, log.Message);
                }
            }

            public void LogDebug(string data)
            {
                Log(LogLevel.Debug, data);
            }

            public void LogVerbose(string data)
            {
                Log(LogLevel.Verbose, data);
            }

            public void LogInformation(string data)
            {
                Log(LogLevel.Information, data);
            }

            public void LogMinimal(string data)
            {
                Log(LogLevel.Minimal, data);
            }

            public void LogWarning(string data)
            {
                Log(LogLevel.Warning, data);
            }

            public void LogError(string data)
            {
                Log(LogLevel.Error, data);
            }

            public void LogInformationSummary(string data)
            {
                Log(LogLevel.Information, data);
            }

            public void LogErrorSummary(string data)
            {
                Log(LogLevel.Error, data);
            }

            public void Log(LogLevel level, string data)
            {
                lock (logLock)
                {
                    Debug.WriteLine($"[{level}] {data}");
                    Logs.Add((level, data));
                    action?.Invoke(level, data);
                }
            }

            public Task LogAsync(LogLevel level, string data)
            {
                Log(level, data);
                return Task.CompletedTask;
            }

            public void Log(ILogMessage message)
            {
                Log(message.Level, message.Message);
            }

            public Task LogAsync(ILogMessage message)
            {
                Log(message);
                return Task.CompletedTask;
            }
        }
    }
}
