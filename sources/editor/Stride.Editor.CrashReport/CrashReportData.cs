// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace Stride.Editor.CrashReport;

public class CrashReportData
{
    public readonly List<(string, string)> Data = [];

    public string this[string key]
    {
        get
        {
            return Data.FirstOrDefault(p => p.Item1 == key).Item2;
        }
        set
        {
            int num = -1;

            foreach(var current in Data)
            {
                if (current.Item1 == key)
                {
                    num = Data.IndexOf(current);
                    break;
                }
            }
            if(value == null)
                return;
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

    public override string ToString()
    {
        StringBuilder val = new();
        foreach (var current in Data)
        {
            val.Append(string.Concat(current.Item1, ": ", current.Item2, "\r\n"));
        }
        return val.ToString();
    }
}
