using System;

namespace Stride.VisualStudio.Commands
{
    static class TypeExtensions
    {
        public static Type ToType(this string configName)
        {
            try
            {
                var result = Type.GetType(configName);
                return result;
                //var parts = (from n in configName.Split(',') select n.Trim()).ToArray();
                //var assembly = Assembly.Load(new AssemblyName(parts[1]));
                //var type = assembly.GetType(parts[0]);
                //return type;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return null;
        }
    }
}
