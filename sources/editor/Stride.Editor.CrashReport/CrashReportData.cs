using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;


namespace Stride.Editor.CrashReport
{
    public class CrashReportData
    {
        public List<Tuple<string, string>> Data = new List<Tuple<string, string>>();

        public string this[string key]
        {
            get
            {
                return Data.Where(p => p.Item1 == key).FirstOrDefault();
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
                if (num != -1)
                {
                    Data[num] = Tuple.Create<string, string>(key, value);
                }
                else
                {
                    Data.Add(Tuple.Create<string, string>(key, value));
                }
            }
        }

        public override string ToString()
        {
            StringBuilder val = new StringBuilder();
            foreach (var current in Data)
            {
                val.Append(String.Concat(current.Item1, ": ", current.Item2, "\r\n"));
            }
            return val.ToString();
        }
    }
}