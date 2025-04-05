// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Stride.Core;

namespace Stride.Input
{
    /// <summary>
    /// Keeps track of <see cref="GamePadLayout"/>
    /// </summary>
    public static class GamePadLayouts
    {
        private static readonly List<GamePadLayout> layouts = new List<GamePadLayout>();

        static GamePadLayouts()
        {
            // XInput device layout for any plaform that does not support xinput directly
            AddLayout(new GamePadLayoutXInput());

            // Support for DualShock4 controllers
            AddLayout(new GamePadLayoutDS4());

            LoadGenericSDLLayouts();
        }

        /// <summary>
        /// Adds a new layout that cane be used for mapping gamepads to <see cref="GamePadState"/>
        /// </summary>
        /// <param name="layout">The layout to add</param>
        public static void AddLayout(GamePadLayout layout)
        {
            lock (layouts)
            {
                if (!layouts.Contains(layout))
                {
                    layouts.Add(layout);
                }
            }
        }

        /// <summary>
        /// Finds a layout matching the given gamepad
        /// </summary>
        /// <param name="source">The source that the <paramref name="device"/> came from</param>
        /// <param name="device">The device to find a layout for</param>
        /// <returns>The gamepad layout that was found, or null if none was found</returns>
        public static GamePadLayout FindLayout(IInputSource source, IGameControllerDevice device)
        {
            lock (layouts)
            {
                foreach (var layout in layouts)
                {
                    if (layout.MatchDevice(source, device))
                        return layout;
                }
                return null;
            }
        }

        private static void LoadGenericSDLLayouts()
        {
            const string filename = "gamecontrollerdb.txt";

            string[] contents = null;
            foreach (var searchpath in new[]
            {
                Path.GetDirectoryName(typeof(GamePadLayoutGenericSDL).Assembly.Location),
                Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName),
                Environment.CurrentDirectory,
            })
            {
                var filePath = Path.Combine(searchpath ?? string.Empty, filename);
                if (File.Exists(filePath))
                {
                    contents = File.ReadAllLines(filePath);
                    break;
                }
            }

            if (contents == null)
                return; // file was not found

            foreach (var line in contents)
            {
                if (line.StartsWith('#') || string.IsNullOrWhiteSpace(line))
                    continue;

                var chunks = line.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (chunks.Length < 3 || !TryParseSDLGuid(chunks[0], out Guid id))
                    continue; // invalid format

                var name = chunks[1];
                var properties = new Dictionary<string, string>(chunks.Length - 2);

                for (int i = 2; i < chunks.Length; i++)
                {
                    var indexOfSeparator = chunks[i].IndexOf(':');
                    var key = chunks[i].Substring(0, indexOfSeparator);
                    var value = chunks[i].Substring(indexOfSeparator + 1);
                    properties.Add(key, value);
                }

                var controllerPlatform = properties.GetValueOrDefault("platform");

                if (CompatibleWithCurrentPlatform(controllerPlatform))
                    AddLayout(new GamePadLayoutGenericSDL(id, name, properties));
            }
        }

        private static bool TryParseSDLGuid(string input, out Guid guid)
        {
            guid = default;

            if (input.Length != 32)
                return false;

            // The guid is in big endian hex format - which aligns with the Guid constructor from bytes
            Span<byte> bytes = stackalloc byte[16];
            for (int i = 0; i < 32; i += 2)
            {
                if (!byte.TryParse(input.AsSpan(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
                    return false;
                bytes[i / 2] = b;
            }

            guid = new Guid(bytes);
            return true;
        }

        private static bool CompatibleWithCurrentPlatform(string controllerPlatform)
        {
            return
                (controllerPlatform == "Windows" && (Platform.Type == PlatformType.Windows || Platform.Type == PlatformType.UWP)) ||
                (controllerPlatform == "Mac OS X" && Platform.Type == PlatformType.macOS) ||
                (controllerPlatform == "Linux" && Platform.Type == PlatformType.Linux) ||
                (controllerPlatform == "Android" && Platform.Type == PlatformType.Android) ||
                (controllerPlatform == "iOS" && Platform.Type == PlatformType.iOS);
        }
    }
}
