// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.VisualStudio.TextTemplating;
using Xenko.Core;
using Xenko.Core.Diagnostics;
using Xenko.Core.IO;

namespace Xenko.Core.ProjectTemplating
{
    internal class ProjectTemplatingHost : Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngineHost, ITextTemplatingSessionHost
    {
        public ProjectTemplatingHost(ILogger log, string templateFile, string rootDirectory, ExpandoObject expando, IEnumerable<string> assemblies)
        {
            if (log == null) throw new ArgumentNullException("log");
            if (templateFile == null) throw new ArgumentNullException("templateFile");
            if (rootDirectory == null) throw new ArgumentNullException("rootDirectory");
            if (expando == null) throw new ArgumentNullException("expando");
            this.log = log;
            this.TemplateFile = templateFile;
            this.rootDirectory = rootDirectory;

            Session = new CustomTemplatingSession(expando);

            var assembliesToLoad = new List<string>()
                {
                    "netstandard",
                    "System.Core",
                    typeof(RuntimeBinderException).Assembly.FullName,
                    "Mono.TextTemplating",
                    typeof(PlatformType).Assembly.FullName,
                    typeof(UPath).Assembly.FullName,
                    "Xenko.Core.ProjectTemplating"
                };
            assembliesToLoad.AddRange(assemblies);
            StandardAssemblyReferences = assembliesToLoad;

            StandardImports = new List<string>()
                {
                    "System.Linq",
                    "System.Text",
                    "System.Collections.Generic",
                    "System.Dynamic",
                    "Xenko.Core.ProjectTemplating"
                };
        }

        public IList<string> StandardAssemblyReferences { get; private set; }

        public IList<string> StandardImports { get; private set; }

        public string TemplateFile { get; set; }

        private readonly string rootDirectory;

        private readonly ILogger log;

        public object GetHostOption(string optionName)
        {
            object value = null;
            if (Session != null)
            {
                Session.TryGetValue(optionName, out value);
            }
            return value;
        }

        public bool LoadIncludeText(string requestFileName, out string content, out string location)
        {
            content = null;
            location = Path.Combine(rootDirectory, requestFileName);
            if (File.Exists(location))
            {
                content = File.ReadAllText(location);
                return true;
            }
            return false;
        }

        public void LogErrors(CompilerErrorCollection errors)
        {
            for (int i = 0; i < errors.Count; i++)
            {
                var error = errors[i];
                // error.FileName can be null resulting in an exception in error.ToString()
                var msg = error.FileName == null ? error.ErrorText : error.ToString();
                log.Error(msg);
            }
        }

        public AppDomain ProvideTemplatingAppDomain(string content)
        {
            return AppDomain.CurrentDomain;
        }

        public string ResolveAssemblyReference(string assemblyReference)
        {
            var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name.Equals(assemblyReference, StringComparison.Ordinal) || x.GetName().FullName.Equals(assemblyReference, StringComparison.Ordinal));
            if (loadedAssembly != null)
                return loadedAssembly.Location;

            try
            {


                Assembly assembly = Assembly.Load(assemblyReference);
                if (assembly != null)
                {
                    return assembly.Location;
                }
            }
            catch (FileNotFoundException) { }
            catch (FileLoadException) { }
            catch (BadImageFormatException) { }

            return null;
        }

        public Type ResolveDirectiveProcessor(string processorName)
        {
            throw new NotImplementedException();
        }

        public string ResolveParameterValue(string directiveId, string processorName, string parameterName)
        {
            throw new NotImplementedException();
        }

        public string ResolvePath(string path)
        {
            throw new NotImplementedException();
        }

        public void SetFileExtension(string extension)
        {
        }

        public void SetOutputEncoding(Encoding encoding, bool fromOutputDirective)
        {
        }

        public ITextTemplatingSession CreateSession()
        {
            throw new NotImplementedException();
        }

        public ITextTemplatingSession Session { get; set; }
    }
}
