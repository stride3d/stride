// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;

using Stride.Core.Mathematics;

namespace Stride.Core.Assets.Editor.ViewModel
{
    public static class GplPaletteParser
    {
        public static Dictionary<string, Color3>? TryParse(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            var colors = new Dictionary<string, Color3>();

            try
            {
                var lines = File.ReadAllLines(filePath);

                if (lines.Length == 0 || !lines[0].Trim().Equals("GIMP Palette", StringComparison.OrdinalIgnoreCase))
                    return null;

                int index = 1;
                int unnamedIndex = 1;

                for (; index < lines.Length; index++)
                {
                    var line = lines[index].Trim();

                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || line.StartsWith("Name:") || line.StartsWith("Columns:"))
                        continue;

                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length < 3)
                        continue;

                    if (!byte.TryParse(parts[0], out byte r) ||
                        !byte.TryParse(parts[1], out byte g) ||
                        !byte.TryParse(parts[2], out byte b))
                        continue;

                    var name = parts.Length >= 4
                        ? string.Join(" ", parts, 3, parts.Length - 3).Trim()
                        : $"Color {unnamedIndex++}";

                    var key = name;
                    int suffix = 2;
                    while (colors.ContainsKey(key))
                        key = $"{name} {suffix++}";

                    colors[key] = new Color3(r / 255f, g / 255f, b / 255f);
                }

                return colors.Count > 0 ? colors : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
