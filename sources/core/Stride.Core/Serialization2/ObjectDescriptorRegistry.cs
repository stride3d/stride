using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Reflection;

namespace Stride.Core.Serialization2;
public static class ObjectDescriptorRegistry
{
    public static List<(ObjectDescriptorAttribute, Type)> Types = new();
    private static bool Init;
    public static Type Find(Type type)
    {
        if(!Init)
        {
            var dlls = AssemblyRegistry.FindAll();
            foreach (var dll in dlls)
            {
                foreach (var typ in dll.GetTypes())
                {
                    var att = typ.GetCustomAttribute<ObjectDescriptorAttribute>();
                    if (att != null)
                    {
                        Types.Add((att, typ));
                    }
                }
            }
            Init = true;
        }
        var s = Types.FirstOrDefault(x => x.Item1.Type == type);
        Type res = null;
        if(s != default)
            res = s.Item2;

        return res;
    }
    public static void RegisterAll()
    {
        AssemblyRegistry.AssemblyUnregistered += Register;
        AssemblyRegistry.AssemblyRegistered += Register;
    }
    public static void Register(object sender, AssemblyRegisteredEventArgs e)
    {
        Types.Clear();
        var dlls = AssemblyRegistry.FindAll();
        foreach (var dll in dlls)
        {
            foreach (var type in dll.GetTypes())
            {
                var att = type.GetCustomAttribute<ObjectDescriptorAttribute>();
                if (att != null)
                {
                    Types.Add((att, type));
                }
            }
        }
    }
    public static void Register()
    {
        var dlls = AssemblyRegistry.FindAll();
        Types.Clear();
        foreach (var dll in dlls)
        {
            foreach (var type in dll.GetTypes())
            {
                var att = type.GetCustomAttribute<ObjectDescriptorAttribute>();
                if (att != null)
                {
                    Types.Add((att, type));
                }
            }
        }
    }
}
