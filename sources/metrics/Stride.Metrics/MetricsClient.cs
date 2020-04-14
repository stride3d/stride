using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Xenko.Metrics
{
    /// <summary>
    /// Client for publishing metrics.
    /// </summary>
    public class MetricsClient : IDisposable
    {
        private const string RegistryRootKey = @"Software\Xenko";
        private const string RegistryInstallKey = @"InstallGuid";
        private const string RegistryLastCommonMetricsKey = @"LastCommonMetrics";
        private const string EnvSpecial = "XenkoMetricsSpecial";
        private readonly HttpClient httpClient;
        private readonly Guid installGuid;
        private readonly MetricAppId appId;
        private readonly RegistryKey rootRegistryKey;
        private readonly RegistryKey appRegistryKey;
        private int currentSessionId;
        private bool initialized;
        private bool disposing;
        private bool disposed;
        private readonly BlockingCollection<Action> scheduledTasks;
        private readonly Thread metricSenderThread;
        private readonly Guid specialGuid;
        private readonly string versionOverride;
        private int eventProgressiveCount;
        private readonly Stopwatch sessionActiveTime = new Stopwatch();
        private bool activeState;
        private readonly Stopwatch heartbeatStopwatch = new Stopwatch();
        private readonly int heartbeatRate = 5*60*1000;

        [StructLayout(LayoutKind.Sequential)]
        private struct Lastinputinfo
        {
            private static readonly int SizeOf = Marshal.SizeOf(typeof(Lastinputinfo));

            [MarshalAs(UnmanagedType.U4)]
            public uint cbSize;
            [MarshalAs(UnmanagedType.U4)]
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref Lastinputinfo plii);

        private static uint GetLastInputTime()
        {
            uint idleTime = 0;
            var lastInputInfo = new Lastinputinfo();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            lastInputInfo.dwTime = 0;

            var envTicks = (uint)Environment.TickCount;

            if (GetLastInputInfo(ref lastInputInfo))
            {
                var lastInputTick = lastInputInfo.dwTime;

                idleTime = envTicks - lastInputTick;
            }

            return ((idleTime > 0) ? (idleTime / 1000) : 0);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsClient" /> class.
        /// </summary>
        /// <param name="appId">The application identifier.</param>
        /// <param name="heartbeatRateMs">The rate the heartbeats are sent in ms </param>
        /// <param name="endpointUrl">The endpoint URL.</param>
        /// <param name="versionOverride">The version number to override.</param>
        /// <exception cref="System.ArgumentNullException">endpointUrl
        /// or
        /// appId</exception>
        public MetricsClient(MetricAppId appId, int? heartbeatRateMs = null, string endpointUrl = null, string versionOverride = null)
        {
            if (heartbeatRateMs != null)
            {
                heartbeatRate = heartbeatRateMs.Value;
            }

            if (endpointUrl == null)
            {
                endpointUrl = "https://metrics.xenko.com";
            }
            if (appId == null) throw new ArgumentNullException(nameof(appId));

            // Make sure the endpoint ends with /
            if (!endpointUrl.EndsWith("/"))
            {
                endpointUrl += "/";
            }

            this.versionOverride = versionOverride;

            // Get special env variable
            var disableIdStr = Environment.GetEnvironmentVariable(EnvSpecial);
            if (disableIdStr != null)
            {
                Guid.TryParse(disableIdStr, out specialGuid);
            }

            scheduledTasks = new BlockingCollection<Action>();
            httpClient = new HttpClient {BaseAddress = new Uri(endpointUrl)};
            this.appId = appId;

            // Get installation guid
            installGuid = GetInstallGuid(appId, out rootRegistryKey, out appRegistryKey);

            heartbeatStopwatch.Start();

            // Use a custom task scheduler
            metricSenderThread = new Thread(MetricThreadSender) { Name = "MetricThreadSender" };
            metricSenderThread.Start();

            scheduledTasks.Add(Initialize);
        }

        private void MetricThreadSender()
        {
            while (scheduledTasks.Count > 0 || !disposing)
            {
                var lastInput = GetLastInputTime();
                var active = lastInput < 5 * 60; //5mins

                if (active)
                {
                    if (!sessionActiveTime.IsRunning && activeState) //restart timer if was stopped before for inactivity
                    {
                        sessionActiveTime.Start();
                    }
                }
               
                if ((sessionActiveTime.IsRunning && !activeState) || (!active && sessionActiveTime.IsRunning && activeState))
                {
                    sessionActiveTime.Stop();
                }

                if (heartbeatStopwatch.ElapsedMilliseconds > heartbeatRate) //5 mins
                {
                    SendMetricSync(CommonMetrics.SessionHeartbeat2, sessionActiveTime.Elapsed.TotalSeconds);
                    heartbeatStopwatch.Restart();
                }

                Action action;
                if (scheduledTasks.TryTake(out action, 100))
                {
                    action();
                }
            }
        }

        private void SendMetric<T>(MetricKey<T> metricKey, T metricValue)
        {
            scheduledTasks.Add(() => SendMetricSync(metricKey, metricValue));
        }

        private void Initialize()
        {
            if (initialized)
            {
                throw new InvalidOperationException("Cannot initialize this instance already initialized");
            }

            OpenApplication();
            UpdateCommonMetrics();
            initialized = true;
        }

        public void Dispose()
        {
            if (disposed)
            {
                throw new InvalidOperationException("Cannot dispose an object already disposed");
            }

            scheduledTasks.Add(CloseApplication);
            metricSenderThread.Join(TimeSpan.FromMinutes(1));

            httpClient.Dispose();

            disposed = true;
        }

        private void OpenApplication()
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
            var version = versionOverride;
            
            // Get the version of the current assembly
            if (version == null)
            {
                var infoVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                version = !string.IsNullOrWhiteSpace(infoVersion?.InformationalVersion) ? infoVersion.InformationalVersion : assembly.GetName().Version.ToString();
            }

            if (currentSessionId > 0)
            {
                CloseApplication();
            }

            currentSessionId = GetNextSession();
            SendMetricSync(CommonMetrics.OpenApplication, version);
        }

        private void CloseApplication()
        {
            if (currentSessionId <= 0) return;

            SendMetricSync(CommonMetrics.CloseApplication, string.Empty);
            currentSessionId = -1;

            disposing = true;
        }

        /// <summary>
        /// Opens a new xenko project session
        /// </summary>
        /// <param name="platformsData">platforms used by this project</param>
        public void OpenProjectSession(string platformsData)
        {
            SendMetric(CommonMetrics.OpenSession2, platformsData);
        }

        /// <summary>
        /// Closes a xenko project session
        /// </summary>
        public void CloseProjectSession()
        {
            SendMetric(CommonMetrics.CloseSession2, sessionActiveTime.Elapsed.TotalSeconds);
        }

        /// <summary>
        /// Reports that a xenko project session crashed
        /// </summary>
        /// <param name="reportSent">if the user sent a report</param>
        public void CrashedSession(bool reportSent)
        {
            SendMetricSync(CommonMetrics.CrashedSession, reportSent);
        }

        public void DownloadPackage(string downloadInfo)
        {
            SendMetric(CommonMetrics.DownloadPackage, downloadInfo);
        }

        private static KeyValuePair<string, string> GetOsVersionAndCaption()
        {
            var kvpOsSpecs = new KeyValuePair<string, string>("", "");
            var searcher = new ManagementObjectSearcher("SELECT Caption, Version FROM Win32_OperatingSystem");
            try
            {
                foreach (var os in searcher.Get())
                {
                    var version = os["Version"].ToString();
                    var productName = os["Caption"].ToString();
                    kvpOsSpecs = new KeyValuePair<string, string>(productName, version);
                }
            }
            catch
            {
                // ignored
            }

            return kvpOsSpecs;
        }

        private void SendCommonMetrics()
        {
            // ----------------------------
            // Send OS informations
            // ----------------------------
            var osVersion = GetOsVersionAndCaption();
            SendMetricSync(CommonMetrics.OsVersion, $"{osVersion.Key} {osVersion.Value}"); //todo wrong
            SendMetricSync(CommonMetrics.Os64Bit, Environment.Is64BitOperatingSystem);
            SendMetricSync(CommonMetrics.OsLanguage, CultureInfo.InstalledUICulture.Name);

            // ----------------------------
            // Send Cpu informations
            // ----------------------------
            string cpuVendor;
            string cpuModel;
            
            MetricUtil.GetCpuVendorAndModel(out cpuVendor, out cpuModel);
            SendMetricSync(CommonMetrics.CpuVendor, cpuVendor);
            SendMetricSync(CommonMetrics.CpuModel, cpuModel);

            var coreCount = new ManagementClass("Win32_Processor").GetInstances().Cast<ManagementBaseObject>().Sum(item => int.Parse(item["NumberOfCores"].ToString()));
            SendMetricSync(CommonMetrics.CpuCore, coreCount);
            SendMetricSync(CommonMetrics.SystemMemory, MetricUtil.GetSystemMemoryInMb());

            // ----------------------------
            // Send Gpu informations
            // ----------------------------
            var videoControllers = new ManagementClass("Win32_VideoController").GetInstances();

            // Display only the first adapter
            // TODO: Not accurate in case of multiple video controllers (SLI, notebooks with discrete GPUs...etc.)
            foreach (var videoController in videoControllers)
            {
                if (videoController["CurrentHorizontalResolution"] == null ||
                    videoController["CurrentVerticalResolution"] == null)
                {
                    continue;
                }

                var gpuModel = videoController["Name"];
                if (gpuModel != null)
                {
                    SendMetricSync(CommonMetrics.GpuModel, gpuModel.ToString());
                }

                var gpuVendor = videoController["AdapterCompatibility"];
                if (gpuVendor != null)
                {
                    SendMetricSync(CommonMetrics.GpuVendor, gpuVendor.ToString());
                }

                var adapterRam = videoController["AdapterRAM"];
                if (adapterRam != null)
                {
                    var adapterRamValue = (int) ((long)Convert.ChangeType(adapterRam, typeof (long))/(1024*1024));
                    SendMetricSync(CommonMetrics.VideoMemory, adapterRamValue);
                }

                SendMetricSync(CommonMetrics.DisplayResolution, $"{videoController["CurrentHorizontalResolution"]}x{videoController["CurrentVerticalResolution"]}");

                break;
            }

            // ----------------------------
            // Send Misc informations
            // ----------------------------

            var unityVersion = MetricUtil.GetUnityVersionInstalled();
            if (unityVersion != null)
            {
                SendMetricSync(CommonMetrics.UnityVersion, unityVersion);
            }
        }

        private void SendMetricSync<T>(MetricKey metricKey, T metricValue)
        {
            SendMetricSync(metricKey, metricKey.ValueToString(metricValue));
        }

        private void SendMetricSync(MetricKey metricKey, string metricValue)
        {
            var metricMessage = new NewMetricMessage
            {
                InstallId = installGuid,
                MetricId = metricKey.Guid,
                ApplicationId = appId.Guid,
                SessionId = currentSessionId,
                EventId = ++eventProgressiveCount,
                Value = metricValue ?? string.Empty
            };

            SendMetricSync(metricMessage);
        }

        private void SendMetricSync(NewMetricMessage metricMsg)
        {
            try
            {
                metricMsg.SpecialId = specialGuid;
                var response = Task.Run(() => httpClient.PostAsJsonAsync("api/push-metric", metricMsg)).Result;
            }
            catch (Exception)
            {
                // ignored
            }
        }

        /// <summary>
        /// Sets the session status
        /// </summary>
        /// <param name="isActive">if true the session is set to active state, if false inactive</param>
        public void SetActiveState(bool isActive)
        {
            activeState = isActive;
        }

        private bool NeedUpdateCommonMetrics()
        {
            // Log only common metrics for editor
            if (appId.Guid != CommonApps.XenkoEditorAppId.Guid)
            {
                return false;
            }

            var lastTimeObj = rootRegistryKey.GetValue(RegistryLastCommonMetricsKey);
            var lastTimeValue = new DateTime(0);
            if (lastTimeObj != null)
            {
                DateTime.TryParse(lastTimeObj.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out lastTimeValue);
            }

            var nowTime = DateTime.Now;
            return (nowTime.Month != lastTimeValue.Month && nowTime.Year != lastTimeValue.Year);
        }

        private void UpdateCommonMetrics()
        {
            if (NeedUpdateCommonMetrics())
            {
                SendCommonMetrics();
                rootRegistryKey.SetValue(RegistryLastCommonMetricsKey, DateTime.Now.ToString(CultureInfo.InvariantCulture));
            }
        }

        private int GetNextSession()
        {
            var sessionId = 0;
            var valueObj = appRegistryKey.GetValue("SessionId");
            var valueStr = valueObj?.ToString() ?? "0";
            int.TryParse(valueStr, out sessionId);
            sessionId++;
            appRegistryKey.SetValue("SessionId", sessionId);
            return sessionId;
        }

        private static Guid GetInstallGuid(MetricAppId appId, out RegistryKey rootRegistryKey, out RegistryKey appKey)
        {
            rootRegistryKey = Registry.CurrentUser.OpenSubKey(RegistryRootKey, RegistryKeyPermissionCheck.ReadWriteSubTree) ??
                              Registry.CurrentUser.CreateSubKey(RegistryRootKey, RegistryKeyPermissionCheck.ReadWriteSubTree);

            var shortName = appId.Name.Replace("Xenko", string.Empty);

            // Get the appKey
            if (rootRegistryKey != null)
            {
                appKey = rootRegistryKey.OpenSubKey(shortName, RegistryKeyPermissionCheck.ReadWriteSubTree) ??
                         rootRegistryKey.CreateSubKey(shortName, RegistryKeyPermissionCheck.ReadWriteSubTree);

                // Get or create InstallGuid
                var guidObj = rootRegistryKey.GetValue(RegistryInstallKey);
                var guid = Guid.Empty;
                if (guidObj != null)
                {
                    Guid.TryParse(guidObj.ToString(), out guid);
                }

                if (guid == Guid.Empty)
                {
                    guid = Guid.NewGuid();
                    rootRegistryKey.SetValue(RegistryInstallKey, guid);
                }

                return guid;
            }

            appKey = null;
            return Guid.Empty;
        }
    }
}
