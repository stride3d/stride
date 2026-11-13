// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using GNU.Gettext;
using Stride.Core.Reflection;
using Stride.Core.Translation.Providers;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// A plugin loaded by an <see cref="AssemblyContainer"/> comes from memory, so it has no <see cref="Assembly.Location"/>: its
/// translations are read from the folder of the file the container loaded it from, as Gettext reads them next to an assembly.
/// </summary>
public sealed class TestPluginTranslations : IDisposable
{
    private const string PluginName = "Stride.TranslatedPluginProbe";
    private readonly string directory = Path.Combine(Path.GetTempPath(), "Stride.Core.Assets.Editor.Tests", Guid.NewGuid().ToString("N"));
    private readonly CultureInfo uiCulture = CultureInfo.CurrentUICulture;

    public TestPluginTranslations()
    {
        Directory.CreateDirectory(directory);
    }

    public void Dispose()
    {
        CultureInfo.CurrentUICulture = uiCulture;
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (IOException)
        {
            // A satellite loaded in the default context keeps no handle (it's read from memory), so this is only a best effort
        }
    }

    [Fact]
    public void APluginLoadedFromMemoryIsTranslatedFromTheFolderOfItsFile()
    {
        var pluginPath = Path.Combine(directory, PluginName + ".dll");
        SavePlugin(pluginPath);
        SaveSatellite(Path.Combine(directory, "fr", GettextResourceManager.GetSatelliteAssemblyName(PluginName)), new CultureInfo("fr"), new Hashtable
        {
            [GettextResourceManager.MakeContextMsgid("Settings", "Don't ask again")] = "Ne demandez plus",
            ["Delete"] = "Supprimer",
        });

        var plugin = new AssemblyContainer().LoadAssemblyFromPath(pluginPath);
        Assert.NotNull(plugin);
        Assert.Empty(plugin.Location);
        Assert.Equal(pluginPath, AssemblyContainer.GetLoadedPath(plugin));

        var provider = new GettextTranslationProvider(plugin, AssemblyContainer.GetLoadedPath(plugin));
        CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
        Assert.Equal("Ne demandez plus", provider.GetParticularString("Settings", "Don't ask again"));
        Assert.Equal("Supprimer", provider.GetString("Delete"));
        // A string the catalog lacks stays as it is
        Assert.Equal("Cancel", provider.GetString("Cancel"));

        // A culture with no catalog keeps the text
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Assert.Equal("Delete", provider.GetString("Delete"));
    }

    [Fact]
    public void AnAssemblyNoContainerLoadedHasNoLoadedPath()
    {
        Assert.Null(AssemblyContainer.GetLoadedPath(typeof(TestPluginTranslations).Assembly));
    }

    private static void SavePlugin(string path)
    {
        var assembly = new PersistedAssemblyBuilder(new AssemblyName(PluginName), typeof(object).Assembly);
        var module = assembly.DefineDynamicModule(PluginName);
        module.DefineType(PluginName + ".Plugin", TypeAttributes.Public | TypeAttributes.Sealed).CreateType();
        assembly.Save(path);
    }

    // What msgfmt builds: a satellite with one GettextResourceSet type per culture, whose constructor fills the catalog
    private static void SaveSatellite(string path, CultureInfo culture, Hashtable catalog)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var name = new AssemblyName(PluginName + ".Messages.resources") { CultureName = culture.Name };
        var assembly = new PersistedAssemblyBuilder(name, typeof(object).Assembly);
        var module = assembly.DefineDynamicModule(name.Name!);
        var type = module.DefineType(GettextResourceManager.MakeResourceSetClassName(PluginName, culture), TypeAttributes.Public, typeof(GettextResourceSet));
        var constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
        var il = constructor.GetILGenerator();
        var table = il.DeclareLocal(typeof(Hashtable));
        il.Emit(OpCodes.Newobj, typeof(Hashtable).GetConstructor(Type.EmptyTypes)!);
        il.Emit(OpCodes.Stloc, table);
        foreach (DictionaryEntry entry in catalog)
        {
            il.Emit(OpCodes.Ldloc, table);
            il.Emit(OpCodes.Ldstr, (string)entry.Key);
            il.Emit(OpCodes.Ldstr, (string)entry.Value!);
            il.Emit(OpCodes.Callvirt, typeof(Hashtable).GetMethod(nameof(Hashtable.Add))!);
        }
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldloc, table);
        il.Emit(OpCodes.Newobj, typeof(GettextResourceReader).GetConstructor([typeof(Hashtable)])!);
        il.Emit(OpCodes.Call, typeof(GettextResourceSet).GetConstructor([typeof(System.Resources.IResourceReader)])!);
        il.Emit(OpCodes.Ret);
        type.CreateType();
        assembly.Save(path);
    }
}
