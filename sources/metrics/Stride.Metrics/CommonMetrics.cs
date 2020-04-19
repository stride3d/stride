using System;

namespace Stride.Metrics
{
    /// <summary>
    /// Common metrics used.
    /// </summary>
    public class CommonMetrics
    {
        /// <summary>
        /// The open application event.
        /// </summary>
        internal static readonly MetricKey<string> OpenApplication = new MetricKey<string>("OpenApplication", new Guid("09C6DEA3-3264-4E22-8DF1-5FB104792B5F"));

        /// <summary>
        /// The end application event.
        /// </summary>
        internal static readonly MetricKey<string> CloseApplication = new MetricKey<string>("CloseApplication", new Guid("2E4A5775-D2F5-4562-9B3F-031FF441B397"));

        /// <summary>
        /// User is opening a session
        /// </summary>
        internal static readonly MetricKey<string> OpenSession = new MetricKey<string>("OpenSession", new Guid("26CDE1FE-E9EE-4123-8B24-94F0BA7CC38A"));
        internal static readonly MetricKey<string> OpenSession2 = new MetricKey<string>("OpenSession2", new Guid("491A0259-B098-4DD2-B9FE-01228247C116"));

        /// <summary>
        /// User is closing a session, the double value represent the active seconds in the session
        /// </summary>
        internal static readonly MetricKey<double> CloseSession = new MetricKey<double>("CloseSession", new Guid("3699C15B-FDC4-4774-9FBB-14AE751E84C2"));
        internal static readonly MetricKey<double> CloseSession2 = new MetricKey<double>("CloseSession2", new Guid("2F7E809F-7043-4C60-8D39-98A158C3476F"));

        /// <summary>
        /// If the session ended with a crash, and if the crash report has been sent
        /// </summary>
        public static readonly MetricKey<bool> CrashedSession = new MetricKey<bool>("CrashedSession", new Guid("3EB0B9A0-3454-4284-A9DF-E4DE13AD672F"));

        /// <summary>
        /// Periodic session heartbeat event, the double data is equal to active seconds (with the user using the game studio actively)
        /// </summary>
        internal static readonly MetricKey<double> SessionHeartbeat = new MetricKey<double>("SessionHeartbeat", new Guid("4B555416-F2D8-4C04-BFFE-5227044BBFC2"));
        internal static readonly MetricKey<double> SessionHeartbeat2 = new MetricKey<double>("SessionHeartbeat2", new Guid("1674C9FB-F3A5-4CC8-B13B-264F9E733AEE"));

        /// <summary>
        /// Events related to download of packages
        /// </summary>
        internal static readonly MetricKey<string> DownloadPackage = new MetricKey<string>("DownloadPackage", new Guid("A519E37E-E43B-4C01-ADA6-56E0FC8A95EC"));


        // ----------------------------
        // OS informations
        // ----------------------------

        /// <summary>
        /// The os version
        /// </summary>
        internal static readonly MetricKey<string> OsVersion = new MetricKey<string>("OsVersion", new Guid("5800BA4B-6353-49DD-B6F4-FDFAD7E8574F"));

        /// <summary>
        /// The os 32/64 bits
        /// </summary>
        internal static readonly MetricKey<bool> Os64Bit = new MetricKey<bool>("Os64Bit", new Guid("5ED51284-10BC-498E-B7FF-B880DC2482AF"));

        /// <summary>
        /// The os language
        /// </summary>
        internal static readonly MetricKey<string> OsLanguage = new MetricKey<string>("OsLanguage", new Guid("DCA0B68C-2809-4DA0-B32A-E69780610137"));

        // ----------------------------
        // CPU informations
        // ----------------------------

        /// <summary>
        /// The cpu vendor
        /// </summary>
        internal static readonly MetricKey<string> CpuVendor = new MetricKey<string>("CpuVendor", new Guid("810FBFB1-27CB-4E16-B97B-B2EB73737E0F"));

        /// <summary>
        /// The cpu model
        /// </summary>
        internal static readonly MetricKey<string> CpuModel = new MetricKey<string>("CpuModel", new Guid("74571FD6-C7FD-4671-9229-C60643DA12EA"));

        /// <summary>
        /// The cpu number of cores
        /// </summary>
        internal static readonly MetricKey<int> CpuCore = new MetricKey<int>("CpuCore", new Guid("0560FD32-4922-4A30-BA2E-3407D6138CA6"));

        /// <summary>
        /// The system memory in MB
        /// </summary>
        internal static readonly MetricKey<int> SystemMemory = new MetricKey<int>("SystemMemory", new Guid("E377D57E-44A8-4FE1-A2A7-368F642D3CA1"));

        // ----------------------------
        // Graphics informations
        // ----------------------------

        /// <summary>
        /// The gpu vendor
        /// </summary>
        internal static readonly MetricKey<string> GpuVendor = new MetricKey<string>("GpuVendor", new Guid("7D8D6850-CACD-41ED-826E-A9068ED6724B"));

        /// <summary>
        /// The gpu model
        /// </summary>
        internal static readonly MetricKey<string> GpuModel = new MetricKey<string>("GpuModel", new Guid("78947879-C0E9-49D8-AA0D-1E41AF05E473"));

        /// <summary>
        /// The video memory in MB
        /// </summary>
        internal static readonly MetricKey<int> VideoMemory = new MetricKey<int>("VideoMemory", new Guid("68CE071E-A4D7-479A-B3A2-0892FCBC7B89"));

        /// <summary>
        /// The display resolution in the format [Width]x[Height]
        /// </summary>
        internal static readonly MetricKey<string> DisplayResolution = new MetricKey<string>("DisplayResolution", new Guid("16E41EA1-09C9-48A8-A089-C74D7F3FE033"));

        // ---------------------------------
        // Mist informations
        // ---------------------------------

        /// <summary>
        /// The unity version installed
        /// </summary>
        internal static readonly MetricKey<string> UnityVersion = new MetricKey<string>("UnityVersion", new Guid("45F58FCE-A6EC-403D-AEE6-F4E2B1F6A684"));
    }
}