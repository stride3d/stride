// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if (STRIDE_UI_WINFORMS || STRIDE_UI_WPF)
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharpDX;
using SharpDX.DirectInput;

namespace Stride.Input
{
    /// <summary>
    /// Provides support for various game controllers on windows
    /// </summary>
    internal partial class InputSourceWindowsDirectInput : InputSourceBase
    {
        private readonly Control? uiControl;
        private readonly HashSet<Guid> devicesToRemove = new HashSet<Guid>();
        private InputManager? inputManager;
        private IEnumerable<string> xInputDevices = Enumerable.Empty<string>();

        // Created and enumerated by the scan worker; used by the input thread only while no scan runs.
        private DirectInput? directInput;

        private readonly object scanLock = new object();
        private bool scanRunning;
        private bool rescanRequested;
        private bool disposed;
        private ScanResult? pendingScan;

        private sealed record ScanResult(IList<DeviceInstance> Devices, List<string> XInputDevices);

        [GeneratedRegex(@"VID_(\w+)?&PID_(\w+)?")]
        private static partial Regex XInputDeviceIdRegex { get; }

        /// <summary>
        /// Creates the DirectInput source.
        /// </summary>
        /// <param name="uiControl">
        /// The game's control. While its top-level window has no focus, controllers only return towards rest (see <see cref="BackgroundInputFilter"/>).
        /// With <c>null</c>, controllers are not filtered.
        /// </param>
        public InputSourceWindowsDirectInput(Control? uiControl)
        {
            this.uiControl = uiControl;
        }

        public override void Initialize(InputManager inputManager)
        {
            this.inputManager = inputManager;

            Scan();
        }

        public override void Dispose()
        {
            // Dispose all the gamepads
            foreach (var pair in Devices)
            {
                (pair.Value as IDisposable)?.Dispose();
            }

            // Unregisters all devices
            base.Dispose();

            lock (scanLock)
            {
                disposed = true;
                pendingScan = null;

                // A running scan disposes DirectInput when it ends
                if (!scanRunning)
                {
                    directInput?.Dispose();
                    directInput = null;
                }
            }
        }

        public override void Update()
        {
            // Process device removals
            foreach (var deviceIdToRemove in devicesToRemove)
            {
                var gameController = Devices[deviceIdToRemove];
                UnregisterDevice(gameController);
                (gameController as IDisposable)?.Dispose();
            }
            devicesToRemove.Clear();

            lock (scanLock)
            {
                // A pending rescan gives a newer result
                if (scanRunning || pendingScan is null)
                    return;

                xInputDevices = pendingScan.XInputDevices;
                foreach (var device in pendingScan.Devices)
                {
                    if (!Devices.ContainsKey(device.InstanceGuid))
                        OpenDevice(device);
                }
                pendingScan = null;
            }
        }

        /// <summary>
        /// Starts a scan for new devices on a worker thread. The devices it finds are added by a later <see cref="Update"/>.
        /// </summary>
        public override void Scan()
        {
            lock (scanLock)
            {
                if (disposed)
                    return;

                if (scanRunning)
                {
                    rescanRequested = true;
                    return;
                }

                scanRunning = true;
                Task.Run(ScanDevices);
            }
        }

        private void ScanDevices()
        {
            while (true)
            {
                try
                {
                    directInput ??= new DirectInput();
                    var devices = directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);
                    var xInputDeviceIds = GetAllXInputDevices();

                    lock (scanLock)
                        pendingScan = new ScanResult(devices, xInputDeviceIds);
                }
                catch (Exception ex)
                {
                    InputManager.Logger.Warning("DirectInput device scan failed.", ex);
                }

                lock (scanLock)
                {
                    if (disposed)
                    {
                        pendingScan = null;
                        directInput?.Dispose();
                        directInput = null;
                    }
                    else if (rescanRequested)
                    {
                        rescanRequested = false;
                        continue;
                    }

                    scanRunning = false;
                    return;
                }
            }
        }

        /// <summary>
        /// Select all device IDs that contain "IG_". If so, it's an XInput device.
        /// This information cannot be found from DirectInput.
        /// </summary>
        private static List<string> GetAllXInputDevices()
        {
            // An XInput device's raw-input name carries the "&IG_" marker; pull the VID/PID out of any
            // such name to match against DirectInput's ProductGuid prefix. Uses the Raw Input API
            // (user32) rather than WMI/Microsoft.Management.Infrastructure, which isn't AOT/trim-safe.
            return GetRawInputDeviceNames()
                .Where(name => name.Contains("&IG_", StringComparison.OrdinalIgnoreCase))
                .Select(name => XInputDeviceIdRegex.Match(name))
                .Where(match => match.Success)
                .Select(match => (match.Groups[2].Value + match.Groups[1].Value).ToLowerInvariant())
                .Distinct()
                .ToList();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RawInputDeviceList
        {
            public IntPtr Device;
            public uint Type;
        }

        private const uint RidiDeviceName = 0x20000007;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetRawInputDeviceList([Out] RawInputDeviceList[]? rawInputDeviceList, ref uint numDevices, uint size);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetRawInputDeviceInfoW")]
        private static extern uint GetRawInputDeviceInfo(IntPtr device, uint command, IntPtr data, ref uint dataCharCount);

        /// <summary>
        /// Gets whether the game window has focus.
        /// </summary>
        internal bool HasFocus => WindowsInputFocus.HasFocus(uiControl);

        /// <summary>Enumerates the device name string of every raw-input device.</summary>
        private static List<string> GetRawInputDeviceNames()
        {
            var names = new List<string>();
            uint deviceCount = 0;
            uint listEntrySize = (uint)Marshal.SizeOf<RawInputDeviceList>();

            // First call with a null list just fills in the device count.
            GetRawInputDeviceList(null, ref deviceCount, listEntrySize);
            if (deviceCount == 0)
                return names;

            var devices = new RawInputDeviceList[deviceCount];
            if (GetRawInputDeviceList(devices, ref deviceCount, listEntrySize) == unchecked((uint)-1))
                return names;

            foreach (var device in devices)
            {
                uint charCount = 0;
                // First call queries the buffer size (in characters) for the device name.
                if (GetRawInputDeviceInfo(device.Device, RidiDeviceName, IntPtr.Zero, ref charCount) != 0 || charCount == 0)
                    continue;

                var buffer = Marshal.AllocHGlobal((int)charCount * sizeof(char));
                try
                {
                    if (GetRawInputDeviceInfo(device.Device, RidiDeviceName, buffer, ref charCount) != unchecked((uint)-1))
                        names.Add(Marshal.PtrToStringUni(buffer) ?? string.Empty);
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }

            return names;
        }


        /// <summary>
        /// Opens a new game controller or gamepad
        /// </summary>
        /// <param name="deviceInstance">The device instance</param>
        public void OpenDevice(DeviceInstance deviceInstance)
        {
            // Ignore XInput devices since they are handled by XInput
            if (IsXInputDevice(deviceInstance.ProductGuid))
                return;

            if (Devices.ContainsKey(deviceInstance.InstanceGuid))
                throw new InvalidOperationException($"DirectInput GameController already opened {deviceInstance.InstanceGuid}/{deviceInstance.InstanceName}");

            GameControllerDirectInput controller;
            try
            {
                controller = new GameControllerDirectInput(this, directInput, deviceInstance);
            }
            catch (SharpDXException ex)
            {
                InputManager.Logger.Warning($"DirectInput game controller {deviceInstance.InstanceName.TrimEnd('\0')} ({deviceInstance.InstanceGuid}) could not be opened and is ignored. [{ex.Message}]");
                return;
            }

            // Find gamepad layout
            var layout = GamePadLayouts.FindLayout(this, controller);
            if (layout != null)
            {
                // Create a gamepad wrapping around the controller
                var gamePad = new GamePadDirectInput(this, inputManager, controller, layout);
                controller.Disconnected += (sender, args) =>
                {
                    // Queue device for removal
                    devicesToRemove.Add(gamePad.Id);
                };
                RegisterDevice(gamePad); // Register gamepad instead
            }
            else
            {
                controller.Disconnected += (sender, args) =>
                {
                    // Queue device for removal
                    devicesToRemove.Add(controller.Id);
                };
                RegisterDevice(controller);
            }
        }

        private bool IsXInputDevice(Guid productGuid)
        {
            string productGuidStr = productGuid.ToString();
            
            // Loop over all devices
            foreach (var deviceId in xInputDevices)
            {
                if (productGuidStr.StartsWith(deviceId))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

#endif
