// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xenko.Core;
using Xenko.Core.Reflection;
using Xenko.Core.Serialization;
using Xenko.Core.Yaml;
using Xenko.Core.Yaml.Events;
using Xenko.Core.Yaml.Serialization;
using Xenko.Debugger.Target;
using Xenko.Engine;

namespace Xenko.Debugger
{
    public static class LiveAssemblyReloader
    {
        public static void Reload(Game game, AssemblyContainer assemblyContainer, List<Assembly> assembliesToUnregister, List<Assembly> assembliesToRegister)
        {
            List<Entity> entities = new List<Entity>();

            if (game != null)
                entities.AddRange(game.SceneSystem.SceneInstance);

            CloneReferenceSerializer.References = new List<object>();

            var loadedAssembliesSet = new HashSet<Assembly>(assembliesToUnregister);
            var reloadedComponents = new List<ReloadedComponentEntryLive>();

            throw new NotImplementedException("Need to reimplement this to use IUnloadable");
#if FALSE
            foreach (var assembly in assembliesToUnregister)
            {
                // Unregisters assemblies that have been registered in Package.Load => Package.LoadAssemblyReferencesForPackage
                AssemblyRegistry.Unregister(assembly);

                // Unload binary serialization
                DataSerializerFactory.UnregisterSerializationAssembly(assembly);

                // Unload assembly
                assemblyContainer.UnloadAssembly(assembly);
            }

            foreach (var assembly in assembliesToRegister)
            {
                ModuleRuntimeHelpers.RunModuleConstructor(assembly.ManifestModule);

                // Unregisters assemblies that have been registered in Package.Load => Package.LoadAssemblyReferencesForPackage
                AssemblyRegistry.Register(assembly, AssemblyCommonCategories.Assets);

                DataSerializerFactory.RegisterSerializationAssembly(assembly);
            }

            // First pass of deserialization: recreate the scripts
            foreach (ReloadedComponentEntryLive reloadedScript in reloadedComponents)
            {
                // Try to create object
                var objectStart = reloadedScript.YamlEvents.OfType<MappingStart>().FirstOrDefault();
                if (objectStart != null)
                {
                    // Get type info
                    var objectStartTag = objectStart.Tag;
                    bool alias;
                    var componentType = AssetYamlSerializer.Default.GetSerializerSettings().TagTypeRegistry.TypeFromTag(objectStartTag, out alias);
                    if (componentType != null)
                    {
                        reloadedScript.NewComponent = (EntityComponent)Activator.CreateInstance(componentType);
                    }
                }
            }

            // Second pass: update script references in live objects
            // As a result, any script references processed by Yaml serializer will point to updated objects (script reference cycle will work!)
            for (int index = 0; index < CloneReferenceSerializer.References.Count; index++)
            {
                var component = CloneReferenceSerializer.References[index] as EntityComponent;
                if (component != null)
                {
                    var reloadedComponent = reloadedComponents.FirstOrDefault(x => x.OriginalComponent == component);
                    if (reloadedComponent != null)
                    {
                        CloneReferenceSerializer.References[index] = reloadedComponent.NewComponent;
                    }
                }
            }

            // Third pass: deserialize
            reloadedComponents.ForEach(x => ReplaceComponent(game, x));

            CloneReferenceSerializer.References = null;
#endif
        }

        private static EntityComponent DeserializeComponent(ReloadedComponentEntryLive reloadedComponent)
        {
            var eventReader = new EventReader(new MemoryParser(reloadedComponent.YamlEvents));
            var components = new EntityComponentCollection();

            // Use the newly created component during second pass for proper cycle deserialization
            var newComponent = reloadedComponent.NewComponent;
            if (newComponent != null)
                components.Add(newComponent);

            // Try to create component first
            PropertyContainer properties;
            AssetYamlSerializer.Default.Deserialize(eventReader, components, typeof(EntityComponentCollection), out properties);
            var component = components.Count == 1 ? components[0] : null;
            return component;
        }

        private static List<ParsingEvent> SerializeComponent(EntityComponent component)
        {
            // Wrap component in a EntityComponentCollection to properly handle errors
            var components = new Entity { component }.Components;

            // Serialize with Yaml layer
            var parsingEvents = new List<ParsingEvent>();
            // We also want to serialize live component variables
            var serializerContextSettings = new SerializerContextSettings { MemberMask = DataMemberAttribute.DefaultMask | ScriptComponent.LiveScriptingMask };
            AssetYamlSerializer.Default.Serialize(new ParsingEventListEmitter(parsingEvents), components, typeof(EntityComponentCollection), serializerContextSettings);
            return parsingEvents;
        }

        private static void ReplaceComponent(Game game, ReloadedComponentEntryLive reloadedComponent)
        {
            // Create new component instance
            var newComponent = DeserializeComponent(reloadedComponent);

            // Dispose and unregister old component (and their MicroThread, if any)
            var oldComponent = reloadedComponent.Entity.Components[reloadedComponent.ComponentIndex];

            // Flag scripts as being live reloaded
            if (game != null && oldComponent is ScriptComponent)
            {
                game.Script.LiveReload((ScriptComponent)oldComponent, (ScriptComponent)newComponent);
            }

            // Replace with new component
            // TODO: Remove component before serializing it, so cancellation code can run
            reloadedComponent.Entity.Components[reloadedComponent.ComponentIndex] = newComponent;

            // TODO: Dispose or Cancel on script?
            (oldComponent as ScriptComponent)?.Cancel();
        }

        private class ReloadedComponentEntryLive
        {
            public Entity Entity { get { throw new NotImplementedException(); } }

            public int ComponentIndex { get { throw new NotImplementedException(); } }

            public readonly List<ParsingEvent> YamlEvents;

            public EntityComponent OriginalComponent { get { throw new NotImplementedException(); } }

            public EntityComponent NewComponent { get; set; }
        }
    }
}
