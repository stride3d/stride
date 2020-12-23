// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Mdb;
using Mono.Cecil.Pdb;
using Mono.Cecil.Rocks;
using Stride.Core;
using Stride.Core.Storage;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace Stride.Core.AssemblyProcessor
{
    public class AssemblyProcessorApp
    {
        private TextWriter log;

        static AssemblyProcessorApp()
        {
            // Force inclusion of Mono.Cecil.Pdb.dll and Mono.Cecil.Mdb.dll by referencing them
            typeof(NativePdbReader).ToString();
            typeof(MdbReader).ToString();
        }

        public AssemblyProcessorApp(TextWriter info)
        {
            this.log = info ?? Console.Out;

            SearchDirectories = new List<string>();
            References = new List<string>();
            ReferencesToAdd = new List<string>();
            MemoryReferences = new List<AssemblyDefinition>();
            ModuleInitializer = true;
        }

        public bool AutoNotifyProperty { get; set; }

        public bool ParameterKey { get; set; }

        public bool ModuleInitializer { get; set; }

        public bool SerializationAssembly { get; set; }

        public string DocumentationFile { get; set; }

        public string NewAssemblyName { get; set; }

        internal PlatformType Platform { get; set; }

        public string TargetFramework { get; set; }

        public List<string> SearchDirectories { get; set; }

        public List<string> References { get; set; }

        public List<AssemblyDefinition> MemoryReferences { get; set; }

        public List<string> ReferencesToAdd { get; set; }

        public string SignKeyFile { get; set; }

        public bool UseSymbols { get; set; }

        public bool TreatWarningsAsErrors { get; set; }
        public bool DeleteOutputOnError { get; set; }

        /// <summary>
        /// Should we keep a copy of the original assembly? Useful for debugging.
        /// </summary>
        public bool KeepOriginal { get; internal set; }

        public Action<string, Exception> OnErrorEvent;

        public Action<string> OnInfoEvent;

        public bool Run(string inputFile, string outputFile = null)
        {
            if (inputFile == null) throw new ArgumentNullException("inputFile");
            if (outputFile == null)
            {
                outputFile = inputFile;
            }

            CustomAssemblyResolver assemblyResolver = null;
            AssemblyDefinition assemblyDefinition = null;

            try
            {
                try
                {
                    assemblyResolver = CreateAssemblyResolver();
                    var readWriteSymbols = UseSymbols;
                    // Double check that 
                    var symbolFile = Path.ChangeExtension(inputFile, "pdb");
                    if (!File.Exists(symbolFile))
                    {
                        readWriteSymbols = false;
                    }

                    assemblyDefinition = AssemblyDefinition.ReadAssembly(inputFile, new ReaderParameters { AssemblyResolver = assemblyResolver, ReadSymbols = readWriteSymbols, ReadWrite = true });
                    bool modified;

                    // Check if pdb was actually read
                    readWriteSymbols = assemblyDefinition.MainModule.SymbolReader != null;

                    var symbolWriterProvider = assemblyDefinition.MainModule.SymbolReader?.GetWriterProvider();

                    var result = Run(ref assemblyDefinition, ref readWriteSymbols, out modified, out var serializationHash);
                    if (modified || inputFile != outputFile)
                    {
                        // Make sure output directory is created
                        var outputDirectory = Path.GetDirectoryName(outputFile);
                        if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
                        {
                            Directory.CreateDirectory(outputDirectory);
                        }

                        // Keep the original assembly by adding a .old prefix to the current extension
                        if (KeepOriginal)
                        {
                            var copiedFile = Path.ChangeExtension(inputFile, "old" + Path.GetExtension(inputFile));
                            File.Copy(inputFile, copiedFile, true);
                        }

                        if (assemblyDefinition.MainModule.FileName != outputFile)
                        {
                            // Note: using FileShare.Read otherwise often had access conflict (maybe with antivirus or window ssearch?)
                            assemblyDefinition.MainModule.Write(outputFile, new WriterParameters() { WriteSymbols = readWriteSymbols, SymbolWriterProvider = symbolWriterProvider });
                        }
                        else
                        {
                            assemblyDefinition.MainModule.Write(new WriterParameters() { WriteSymbols = readWriteSymbols });
                        }
                    }

                    if (serializationHash != null)
                    {
                        var assemblySerializationHashFile = Path.ChangeExtension(outputFile, ".sdserializationhash");
                        // Check and update current value (if it exists)
                        var serializationHashString = serializationHash.Value.ToString();
                        if (!File.Exists(assemblySerializationHashFile) || File.ReadAllText(assemblySerializationHashFile, Encoding.UTF8) != serializationHashString)
                        {
                            File.WriteAllText(assemblySerializationHashFile, serializationHashString, Encoding.UTF8);
                        }
                    }
                    return result;
                }
                finally
                {
                    assemblyResolver?.Dispose();
                    assemblyDefinition?.Dispose();
                }
            }
            catch (Exception e)
            {
                OnErrorAction(null, e);
                if (DeleteOutputOnError)
                    File.Delete(outputFile);
                return false;
            }
        }

        public CustomAssemblyResolver CreateAssemblyResolver()
        {
            var assemblyResolver = new CustomAssemblyResolver();
            assemblyResolver.RemoveSearchDirectory(".");
            foreach (string searchDirectory in SearchDirectories)
                assemblyResolver.AddSearchDirectory(searchDirectory);
            return assemblyResolver;
        }

        public bool Run(ref AssemblyDefinition assemblyDefinition, ref bool readWriteSymbols, out bool modified, out ObjectId? serializationHash)
        {
            modified = false;
            serializationHash = null;

            try
            {
                var assemblyResolver = (CustomAssemblyResolver) assemblyDefinition.MainModule.AssemblyResolver;

                // Register self
                assemblyResolver.Register(assemblyDefinition);

                var processors = new List<IAssemblyDefinitionProcessor>();

                // We are no longer using it so we are deactivating it for now to avoid processing
                //if (AutoNotifyProperty)
                //{
                //    processors.Add(new NotifyPropertyProcessor());
                //}

                processors.Add(new AddReferenceProcessor(ReferencesToAdd));

                if (ParameterKey)
                {
                    processors.Add(new ParameterKeyProcessor());
                }

                if (NewAssemblyName != null)
                {
                    processors.Add(new RenameAssemblyProcessor(NewAssemblyName));
                }

                //processors.Add(new AsyncBridgeProcessor());

                // Always applies the interop processor
                processors.Add(new InteropProcessor());
                processors.Add(new MonoFixedProcessor());

                processors.Add(new AssemblyVersionProcessor());

                if (DocumentationFile != null)
                {
                    processors.Add(new GenerateUserDocumentationProcessor(DocumentationFile));
                }

                if (SerializationAssembly)
                {
                    processors.Add(new AssemblyScanProcessor());
                    processors.Add(new SerializationProcessor());
                }

                if (ModuleInitializer)
                {
                    processors.Add(new ModuleInitializerProcessor());
                }

                processors.Add(new InitLocalsProcessor());
                processors.Add(new DispatcherProcessor());

                // Check if there is already a AssemblyProcessedAttribute (in which case we can skip processing, it has already been done).
                // Note that we should probably also match the command line as well so that we throw an error if processing is different (need to rebuild).
                if (
                    assemblyDefinition.CustomAttributes.Any(
                        x => x.AttributeType.FullName == "Stride.Core.AssemblyProcessedAttribute"))
                {
                    OnInfoAction($"Assembly [{assemblyDefinition.Name}] has already been processed, skip it.");
                    return true;
                }

                // Register references so that our assembly resolver can use them
                foreach (var reference in References)
                {
                    assemblyResolver.RegisterReference(reference);
                }

                var assemblyProcessorContext = new AssemblyProcessorContext(assemblyResolver, assemblyDefinition,
                    Platform, log);

                foreach (var processor in processors)
                    modified = processor.Process(assemblyProcessorContext) || modified;

                // Assembly might have been recreated (i.e. il-repack), so let's use it from now on
                assemblyDefinition = assemblyProcessorContext.Assembly;
                serializationHash = assemblyProcessorContext.SerializationHash;

                if (modified)
                {
                    // In case assembly has been modified,
                    // add AssemblyProcessedAttribute to assembly so that it doesn't get processed again
                    var mscorlibAssembly = CecilExtensions.FindCorlibAssembly(assemblyDefinition);
                    if (mscorlibAssembly == null)
                    {
                        OnErrorAction("Missing reference to mscorlib.dll or System.Runtime.dll in assembly!");
                        return false;
                    }

                    var attributeType = mscorlibAssembly.MainModule.GetTypeResolved(typeof(Attribute).FullName);
                    var attributeTypeRef = assemblyDefinition.MainModule.ImportReference(attributeType);
                    var attributeCtorRef =
                        assemblyDefinition.MainModule.ImportReference(
                            attributeType.GetConstructors().Single(x => x.Parameters.Count == 0));
                    var voidType = assemblyDefinition.MainModule.TypeSystem.Void;

                    // Create custom attribute
                    var assemblyProcessedAttributeType = new TypeDefinition("Stride.Core",
                        "AssemblyProcessedAttribute",
                        TypeAttributes.BeforeFieldInit | TypeAttributes.AnsiClass | TypeAttributes.AutoClass |
                        TypeAttributes.Public, attributeTypeRef);

                    // Add constructor (call parent constructor)
                    var assemblyProcessedAttributeConstructor = new MethodDefinition(".ctor",
                        MethodAttributes.RTSpecialName | MethodAttributes.SpecialName | MethodAttributes.HideBySig |
                        MethodAttributes.Public, voidType);
                    assemblyProcessedAttributeConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                    assemblyProcessedAttributeConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Call,
                        attributeCtorRef));
                    assemblyProcessedAttributeConstructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                    assemblyProcessedAttributeType.Methods.Add(assemblyProcessedAttributeConstructor);

                    // Add AssemblyProcessedAttribute to assembly
                    assemblyDefinition.MainModule.Types.Add(assemblyProcessedAttributeType);
                    assemblyDefinition.CustomAttributes.Add(new CustomAttribute(assemblyProcessedAttributeConstructor));
                }
            }
            catch (Exception e)
            {
                OnErrorAction(null, e);
                return false;
            }
            finally
            {
            }

            return true;
        }

        public static string ByteArrayToString(byte[] bytes)
        {
            var result = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
                result.AppendFormat("{0:x2}", b);
            return result.ToString();
        }

        private void OnErrorAction(string errorMessage, Exception exception = null)
        {
            if (OnErrorEvent == null)
            {
                if (errorMessage != null)
                {
                    log.WriteLine(errorMessage);
                }
                if (exception != null)
                {
                    log.WriteLine(exception.ToString());
                }
            }
            else
            {
                OnErrorEvent(errorMessage, exception);
            }
        }
 
        private void OnInfoAction(string infoMessage)
        {
            if (OnInfoEvent == null)
            {
                log.WriteLine(infoMessage);
            }
            else
            {
                OnInfoEvent(infoMessage);
            }
        }
    }
}
