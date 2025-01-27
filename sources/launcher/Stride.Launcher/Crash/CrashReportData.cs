// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using System.Text.Json;

namespace Stride.Crash;

public sealed class CrashReportData
{
    public List<(string key, object? value)> Data = [];

    public object? this[string key]
    {
        get => Data.Find(p => p.key == key).value;
        set
        {
            if (value == null)
                return;

            int num = -1;
            foreach (var current in Data)
            {
                if (current.key == key)
                {
                    num = Data.IndexOf(current);
                    break;
                }
            }
            if (num != -1)
            {
                Data[num] = (key, value);
            }
            else
            {
                Data.Add((key, value));
            }
        }
    }

    public string ToJson() => JsonSerializer.Serialize(Data.ToDictionary());

    public override string ToString()
    {
        StringBuilder val = new();
        foreach (var (key, value) in Data)
        {
            val.AppendLine($"{key}: {value}");
        }
        return val.ToString();
    }
}
