// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using Xenko.Core.Assets;
using Xenko.Core.Assets.Editor;
using Xenko.Core.Assets.Editor.Components.TemplateDescriptions;
using Xenko.Core.Assets.Editor.Components.TemplateDescriptions.ViewModels;
using Xenko.Core.Assets.Editor.Components.TemplateDescriptions.Views;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Editor.Settings;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core;
using Xenko.Core.Diagnostics;
using Xenko.Core.Extensions;
using Xenko.Core.IO;
using Xenko.Core.MostRecentlyUsedFiles;
using Xenko.Core.Presentation.Interop;
using Xenko.Core.Presentation.View;
using Xenko.Core.Presentation.ViewModel;
using Xenko.Core.Presentation.Windows;
using Xenko.Core.Translation;
using Xenko.Core.Translation.Providers;
using Xenko.Core.VisualStudio;
using Xenko.Assets.Presentation;
using Xenko.Editor.Build;
using Xenko.Editor.Engine;
using Xenko.Editor.Preview;
using Xenko.GameStudio.View;
using Xenko.Graphics;
using Xenko.Metrics;
using Xenko.PrivacyPolicy;
using EditorSettings = Xenko.Core.Assets.Editor.Settings.EditorSettings;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace Xenko.GameStudio
{
    public static class Program
    {
        private static App app;
        private static IntPtr windowHandle;
        private static bool terminating;
        private static Dispatcher mainDispatcher;
        private static RenderDocManager renderDocManager;
        private static readonly ConcurrentQueue<string> LogRingbuffer = new ConcurrentQueue<string>();

        [STAThread]
        public static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            EditorPath.EditorTitle = XenkoGameStudio.EditorName;

            if (IntPtr.Size == 4)
            {
                MessageBox.Show("Xenko GameStudio requires a 64bit OS to run.", "Xenko", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }

            PrivacyPolicyHelper.RestartApplication = RestartApplication;
            PrivacyPolicyHelper.EnsurePrivacyPolicyXenko30();

            // We use MRU of the current version only when we're trying to reload last session.
            var mru = new MostRecentlyUsedFileCollection(InternalSettings.LoadProfileCopy, InternalSettings.MostRecentlyUsedSessions, InternalSettings.WriteFile);
            mru.LoadFromSettings();

            EditorSettings.Initialize();
            Thread.CurrentThread.Name = "Main thread";

            // Install Metrics for the editor
            using (XenkoGameStudio.MetricsClient = new MetricsClient(CommonApps.XenkoEditorAppId))
            {
                try
                {
                    // Environment.GetCommandLineArgs correctly process arguments regarding the presence of '\' and '"'
                    var args = Environment.GetCommandLineArgs().Skip(1).ToList();
                    var startupSessionPath = XenkoEditorSettings.StartupSession.GetValue();
                    var lastSessionPath = EditorSettings.ReloadLastSession.GetValue() ? mru.MostRecentlyUsedFiles.FirstOrDefault() : null;
                    var initialSessionPath = !UPath.IsNullOrEmpty(startupSessionPath) ? startupSessionPath : lastSessionPath?.FilePath;

                    // Handle arguments
                    for (var i = 0; i < args.Count; i++)
                    {
                        if (args[i] == "/LauncherWindowHandle")
                        {
                            windowHandle = new IntPtr(long.Parse(args[++i]));
                        }
                        else if (args[i] == "/NewProject")
                        {
                            initialSessionPath = null;
                        }
                        else if (args[i] == "/DebugEditorGraphics")
                        {
                            EmbeddedGame.DebugMode = true;
                        }
                        else if (args[i] == "/RenderDoc")
                        {
                            // TODO: RenderDoc is not working here (when not in debug)
                            GameStudioPreviewService.DisablePreview = true;
                            renderDocManager = new RenderDocManager();
                        }
                        else if (args[i] == "/Reattach")
                        {
                            var debuggerProcessId = int.Parse(args[++i]);

                            if (!System.Diagnostics.Debugger.IsAttached)
                            {
                                using (var debugger = VisualStudioDebugger.GetByProcess(debuggerProcessId))
                                {
                                    debugger?.Attach();
                                }
                            }
                        }
                        else if (args[i] == "/RecordEffects")
                        {
                            GameStudioBuilderService.GlobalEffectLogPath = args[++i];
                        }
                        else
                        {
                            initialSessionPath = args[i];
                        }
                    }
                    RuntimeHelpers.RunModuleConstructor(typeof(Asset).Module.ModuleHandle);

                    //listen to logger for crash report
                    GlobalLogger.GlobalMessageLogged += GlobalLoggerOnGlobalMessageLogged;

                    mainDispatcher = Dispatcher.CurrentDispatcher;
                    mainDispatcher.InvokeAsync(() => Startup(initialSessionPath));

                    using (new WindowManager(mainDispatcher))
                    {
                        app = new App { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                        app.Activated += (sender, eventArgs) =>
                        {
                            XenkoGameStudio.MetricsClient?.SetActiveState(true);
                        };
                        app.Deactivated += (sender, eventArgs) =>
                        {
                            XenkoGameStudio.MetricsClient?.SetActiveState(false);
                        };

                        app.InitializeComponent();
                        app.Run();
                    }

                    renderDocManager?.Shutdown();
                }
                catch (Exception e)
                {
                    HandleException(e, 0);
                }
            }
        }

        private static void GlobalLoggerOnGlobalMessageLogged(ILogMessage logMessage)
        {
            if (logMessage.Type <= LogMessageType.Warning) return;

            LogRingbuffer.Enqueue(logMessage.ToString());
            while (LogRingbuffer.Count > 5)
            {
                string msg;
                LogRingbuffer.TryDequeue(out msg);
            }
        }

        private class CrashReportArgs
        {
            public int Location;
            public Exception Exception;
            public string[] Log;
            public string ThreadName;
        }

        private static void CrashReport(object data)
        {
            var args = (CrashReportArgs)data;

            //Stop the game studio rendering thread
            mainDispatcher?.InvokeAsync(() => Thread.CurrentThread.Join());

            CrashReportHelper.SendReport(args.Exception.FormatFull(), args.Location, args.Log, args.ThreadName);

            //Make sure we stop now.. more exceptions might come but we just grab the first one
            Environment.Exit(0);
        }

        private static void HandleException(Exception exception, int location)
        {
            if (exception == null) return;

            //prevent multiple crash reports
            if (terminating) return;
            terminating = true;

            // In case assembly resolve was not done yet, disable it altogether
            NuGetAssemblyResolver.DisableAssemblyResolve();

            var englishCulture = new CultureInfo("en-US");
            var crashLogThread = new Thread(CrashReport) { CurrentUICulture = englishCulture, CurrentCulture = englishCulture };
            crashLogThread.SetApartmentState(ApartmentState.STA);
            crashLogThread.Start(new CrashReportArgs { Exception = exception, Location = location, Log = LogRingbuffer.ToArray(), ThreadName = Thread.CurrentThread.Name });
            crashLogThread.Join();
        }

        [SecurityCritical]
        [HandleProcessCorruptedStateExceptions]
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.IsTerminating)
            {
                HandleException(e.ExceptionObject as Exception, 1);
            }
        }

        private static async void Startup(UFile initialSessionPath)
        {
            try
            {
                InitializeLanguageSettings();
                var serviceProvider = InitializeServiceProvider();

                try
                {
                    PackageSessionPublicHelper.FindAndSetMSBuildVersion();
                }
                catch (Exception e)
                {
                    var message = "Could not find a compatible version of MSBuild.\r\n\r\n" +
                                  "Check that you have a valid installation with the required workloads, or go to [www.visualstudio.com/downloads](https://www.visualstudio.com/downloads) to install a new one.\r\n\r\n" +
                                  e;
                    await serviceProvider.Get<IEditorDialogService>().MessageBox(message, Core.Presentation.Services.MessageBoxButton.OK, Core.Presentation.Services.MessageBoxImage.Error);
                    app.Shutdown();
                    return;
                }

                // We use a MRU that contains the older version projects to display in the editor
                var mru = new MostRecentlyUsedFileCollection(InternalSettings.LoadProfileCopy, InternalSettings.MostRecentlyUsedSessions, InternalSettings.WriteFile);
                mru.LoadFromSettings();
                var editor = new GameStudioViewModel(serviceProvider, mru);
                AssetsPlugin.RegisterPlugin(typeof(XenkoDefaultAssetsPlugin));
                AssetsPlugin.RegisterPlugin(typeof(XenkoEditorPlugin));

                // Attempt to load the startup session, if available
                if (!UPath.IsNullOrEmpty(initialSessionPath))
                {
                    var sessionLoaded = await editor.OpenInitialSession(initialSessionPath);
                    if (sessionLoaded == true)
                    {
                        var mainWindow = new GameStudioWindow(editor);
                        Application.Current.MainWindow = mainWindow;
                        WindowManager.ShowMainWindow(mainWindow);
                        return;
                    }
                }

                // No session successfully loaded, open the new/open project window
                bool? completed;
                // The user might cancel after chosing a template to instantiate, in this case we'll reopen the window
                var startupWindow = new ProjectSelectionWindow
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ShowInTaskbar = true,
                };
                var viewModel = new NewOrOpenSessionTemplateCollectionViewModel(serviceProvider, startupWindow);
                startupWindow.Templates = viewModel;
                startupWindow.ShowDialog();

                // The user selected a template to instantiate
                if (startupWindow.NewSessionParameters != null)
                {
                    // Clean existing entry in the MRU data
                    var directory = startupWindow.NewSessionParameters.OutputDirectory;
                    var name = startupWindow.NewSessionParameters.OutputName;
                    var mruData = new MRUAdditionalDataCollection(InternalSettings.LoadProfileCopy, GameStudioInternalSettings.MostRecentlyUsedSessionsData, InternalSettings.WriteFile);
                    mruData.RemoveFile(UFile.Combine(UDirectory.Combine(directory, name), new UFile(name + SessionViewModel.SolutionExtension)));

                    completed = await editor.NewSession(startupWindow.NewSessionParameters);
                }
                // The user selected a path to open
                else if (startupWindow.ExistingSessionPath != null)
                {
                    completed = await editor.OpenSession(startupWindow.ExistingSessionPath);
                }
                // The user cancelled from the new/open project window, so exit the application
                else
                {
                    completed = true;
                }

                if (completed != true)
                {
                    var windowsClosed = new List<Task>();
                    foreach (var window in Application.Current.Windows.Cast<Window>().Where(x => x.IsLoaded))
                    {
                        var tcs = new TaskCompletionSource<int>();
                        window.Unloaded += (s, e) => tcs.SetResult(0);
                        windowsClosed.Add(tcs.Task);
                    }

                    await Task.WhenAll(windowsClosed);

                    // When a project has been partially loaded, it might already have initialized some plugin that could conflict with
                    // the next attempt to start something. Better start the application again.
                    var commandLine = string.Join(" ", Environment.GetCommandLineArgs().Skip(1).Select(x => $"\"{x}\""));
                    var process = new Process { StartInfo = new ProcessStartInfo(typeof(Program).Assembly.Location, commandLine) };
                    process.Start();
                    app.Shutdown();
                    return;
                }

                if (editor.Session != null)
                {
                    // If a session was correctly loaded, show the main window
                    var mainWindow = new GameStudioWindow(editor);
                    Application.Current.MainWindow = mainWindow;
                    WindowManager.ShowMainWindow(mainWindow);
                }
                else
                {
                    // Otherwise, exit.
                    app.Shutdown();
                }
            }
            catch (Exception)
            {
                app.Shutdown();
            }
        }

        private static void RestartApplication()
        {
            var args = Environment.GetCommandLineArgs();
            var startInfo = new ProcessStartInfo(Assembly.GetEntryAssembly().Location)
            {
                Arguments = string.Join(" ", args.Skip(1)),
                WorkingDirectory = Environment.CurrentDirectory,
            };
            Process.Start(startInfo);
            Environment.Exit(0);
        }

        private static IViewModelServiceProvider InitializeServiceProvider()
        {
            // TODO: this should be done elsewhere
            var dispatcherService = new DispatcherService(Dispatcher.CurrentDispatcher);
            var dialogService = new XenkoDialogService(dispatcherService, XenkoGameStudio.EditorName);
            var pluginService = new PluginService();
            var services = new List<object>{ new DispatcherService(Dispatcher.CurrentDispatcher), dialogService, pluginService };
            if (renderDocManager != null)
                services.Add(renderDocManager);
            var serviceProvider = new ViewModelServiceProvider(services);
            return serviceProvider;
        }

        private static void InitializeLanguageSettings()
        {
            TranslationManager.Instance.RegisterProvider(new GettextTranslationProvider());
            switch (EditorSettings.Language.GetValue())
            {
                case SupportedLanguage.MachineDefault:
                    TranslationManager.Instance.CurrentLanguage = CultureInfo.InstalledUICulture;
                    break;
                case SupportedLanguage.English:
                    TranslationManager.Instance.CurrentLanguage = new CultureInfo("en-US");
                    break;
                case SupportedLanguage.French:
                    TranslationManager.Instance.CurrentLanguage = new CultureInfo("fr-FR");
                    break;
                case SupportedLanguage.Japanese:
                    TranslationManager.Instance.CurrentLanguage = new CultureInfo("ja-JP");
                    break;
                case SupportedLanguage.Russian:
                    TranslationManager.Instance.CurrentLanguage = new CultureInfo("ru-RU");
                    break;
                case SupportedLanguage.German:
                    TranslationManager.Instance.CurrentLanguage = new CultureInfo("de-DE");
                    break;
                case SupportedLanguage.Spanish:
                    TranslationManager.Instance.CurrentLanguage = new CultureInfo("es-ES");
                    break;
                case SupportedLanguage.ChineseSimplified:
                    TranslationManager.Instance.CurrentLanguage = new CultureInfo("zh-Hans");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal static void NotifyGameStudioStarted()
        {
            if (windowHandle != IntPtr.Zero)
            {
                NativeHelper.SendMessage(windowHandle, NativeHelper.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }
        }
    }
}
