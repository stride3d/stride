// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Windows.Input;
using System.Windows;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModel;

namespace Stride.ConfigEditor.ViewModels
{
    public class RootViewModel : ViewModelBase
    {
        public IEnumerable<SectionViewModel> Sections { get; private set; }
        public Options Options { get; private set; }

        private readonly ObservableCollection<SectionViewModel> workingSections = new ObservableCollection<SectionViewModel>();
        private readonly XmlDocument xmlDocument = new XmlDocument();

        private OptionsViewModel optionsViewModel;

        public RootViewModel(Window window)
        {
            optionsViewModel = new OptionsViewModel();
            optionsViewModel.OptionsChanged += LoadAssemblies;

            Options = optionsViewModel.Options;

            Sections = new ReadOnlyObservableCollection<SectionViewModel>(workingSections);

            CloseCommand = new AnonymousCommand(window.Close);
        }

        public ICommand CloseCommand { get; private set; }

        private ICommand optionsCommand;
        public ICommand OptionsCommand
        {
            get
            {
                if (optionsCommand == null)
                    optionsCommand = new AnonymousCommand(_ => ShowOptionsWindow());
                return optionsCommand;
            }
        }

        private bool ShowOptionsWindow()
        {
            var optionsWindow = new OptionsWindow
            {
                DataContext = optionsViewModel,
            };

            optionsViewModel.SetOptionsWindow(optionsWindow);
            return optionsWindow.ShowDialog() == true;
        }

        private ICommand reloadCommand;
        public ICommand ReloadCommand
        {
            get
            {
                if (reloadCommand == null)
                    reloadCommand = new AnonymousCommand(_ => LoadAssemblies());
                return reloadCommand;
            }
        }

        private string GetStrideConfigurationFilename()
        {
            if (string.IsNullOrWhiteSpace(Options.StrideConfigFilename))
                return Path.Combine(Options.StridePath, @"Debug\Stride.Starter.exe.config");

            if (Path.IsPathRooted(Options.StrideConfigFilename) == false)
                return Path.Combine(Options.StridePath, Options.StrideConfigFilename);

            return Options.StrideConfigFilename;
        }

        public async void LoadAssemblies()
        {
            if (Options == null || Options.StridePath == null)
            {
                if (ShowOptionsWindow() == false)
                    return;
            }

            foreach (var svm in workingSections)
                UntrackPropertyChanged(svm);
            workingSections.Clear();

            string tempConfigFilename = GetStrideConfigurationFilename();
            try
            {
                xmlDocument.Load(tempConfigFilename);
            }
            catch
            {
                MessageBox.Show("Bad configuration file.\r\n" + tempConfigFilename, "Configuration File Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var binPaths = new[]
            {
                string.Format(@"{0}\Debug", Options.StridePath),
                string.Format(@"{0}\scripts", Options.StridePath),
            };

            foreach (var path in binPaths)
            {
                try
                {
                    var files = await Task.Factory.StartNew(p => Directory.GetFiles((string)p, "*.dll", SearchOption.AllDirectories)
                        .Concat(Directory.GetFiles((string)p, "*.exe", SearchOption.AllDirectories)), path);

                    foreach (var file in files)
                    {
                        await LoadAssembly(file);
                    }
                }
                catch { }
            }

            ApplyConfig();

            GenerateXmlText();

            foreach (var svm in workingSections)
                TrackPropertyChanged(svm);
        }

        private void ApplyConfig()
        {
            var configSectionsNodes = xmlDocument.SelectNodes("configuration/configSections");
            if (configSectionsNodes == null)
                return;

            foreach (XmlNode configSectionsNode in configSectionsNodes)
            {
                var tempNodes = configSectionsNode.SelectNodes("section");
                if (tempNodes == null)
                    continue;

                var sections = (from node in tempNodes.OfType<XmlNode>()
                                where node != null
                                let nodeAttributes = node.Attributes
                                where nodeAttributes != null
                                let typeAttr = nodeAttributes["type"]
                                let nameAttr = nodeAttributes["name"]
                                where typeAttr != null && nameAttr != null
                                select new
                                {
                                    Name = nameAttr.Value.Trim(),
                                    AssemblyQualifiedName = typeAttr.Value.Trim()
                                })
                                .ToArray();

                foreach (var sectionViewModel in workingSections)
                {
                    var sectionFound = sections.FirstOrDefault(t => t.AssemblyQualifiedName == sectionViewModel.Section.AssemblyQualifiedName);
                    if (sectionFound == null)
                        continue;

                    var step1 = sectionFound.Name;
                    sectionViewModel.Name = step1;

                    var sectionNode = xmlDocument.SelectSingleNode(string.Format("configuration/{0}", sectionFound.Name));
                    if (sectionNode == null)
                        continue;

                    var sectionNodeAttributes = sectionNode.Attributes;
                    if (sectionNodeAttributes == null)
                        continue;

                    var usedAttributes = sectionNodeAttributes.OfType<XmlAttribute>().ToArray();
                    if (usedAttributes.Length == 0)
                        continue;

                    foreach (var property in sectionViewModel.Properties)
                    {
                        var foundAttribute = usedAttributes.FirstOrDefault(attr => attr.Name == property.Attribute.Name);
                        if (foundAttribute != null)
                        {
                            property.Value = foundAttribute.Value;
                            property.IsUsed = true;
                        }
                    }
                }
            }
        }

        private async Task LoadAssembly(string assemblyFilename)
        {
            if (string.IsNullOrWhiteSpace(assemblyFilename))
                throw new ArgumentException("Invalid 'assemblyFilename' argument");

            Assembly assembly = null;

            try
            {
                assembly = await Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            return Assembly.LoadFrom(assemblyFilename);
                        }
                        catch
                        {
                            return null;
                        }
                    });
            }
            catch
            {
                return;
            }

            if (assembly == null)
                return;

            IEnumerable<Type> configurationSections = null;

            try
            {
                configurationSections = assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(ConfigurationSection)));
            }
            catch
            {
                return;
            }

            foreach (var configSection in configurationSections)
            {
                var hasValidProperties = false;
                var sectionViewModel = new SectionViewModel(assembly, configSection);

                var properties = await Task.Factory.StartNew(arg => ((Type)arg).GetProperties(BindingFlags.Public | BindingFlags.Instance), configSection);

                foreach (var property in properties.OrderBy(p => p.Name))
                {
                    var attribute = property.GetCustomAttributes(typeof(ConfigurationPropertyAttribute), true)
                        .Cast<ConfigurationPropertyAttribute>()
                        .FirstOrDefault();

                    if (attribute != null)
                    {
                        var propertyViewModel = new PropertyViewModel(sectionViewModel, property, attribute);
                        sectionViewModel.AddProperty(propertyViewModel);

                        hasValidProperties = true;
                    }
                }

                if (hasValidProperties)
                    workingSections.Add(sectionViewModel);
            }
        }

        private void UntrackPropertyChanged(SectionViewModel sectionViewModel)
        {
            sectionViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            foreach (var child in sectionViewModel.Properties)
                child.PropertyChanged -= OnViewModelPropertyChanged;
        }

        private void TrackPropertyChanged(SectionViewModel sectionViewModel)
        {
            sectionViewModel.PropertyChanged += OnViewModelPropertyChanged;
            foreach (var child in sectionViewModel.Properties)
                child.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            GenerateXmlText();
        }

        private void GenerateXmlText()
        {
            // cleanup
            var confNode = xmlDocument.SelectSingleNode("configuration");
            if (confNode == null)
                return;

            var sectionsNodes = confNode.SelectNodes("configSections");
            foreach (XmlNode sectionsNode in sectionsNodes)
            {
                foreach (XmlNode sectionNode in sectionsNode.SelectNodes("section"))
                {
                    var attr = sectionNode.Attributes["name"];
                    if (attr != null)
                    {
                        var sectionDefNode = confNode.SelectSingleNode(attr.Value.Trim());
                        if (sectionDefNode != null)
                            sectionDefNode.ParentNode.RemoveChild(sectionDefNode);
                    }
                }
                sectionsNode.ParentNode.RemoveChild(sectionsNode);
            }

            // re-add
            var configSectionsNode = xmlDocument.CreateElement("configSections");

            // WARNING!!!
            // If the 'configSections' markup is not the first child of the 'configuration' parent markup
            // then the fucking System.Configuration.Configuration will screw up the config file.
            confNode.PrependChild(configSectionsNode);

            foreach (var svm in workingSections)
            {
                if (string.IsNullOrWhiteSpace(svm.Name))
                    continue;

                try
                {
                    var toBeRemoved = confNode.SelectNodes(svm.Name).Cast<XmlNode>().ToArray();
                    foreach (var child in toBeRemoved)
                        confNode.RemoveChild(child);

                    XmlNode sectionNode;
                    XmlNode definitionNode;
                    if (svm.CreateXmlNodes(xmlDocument, out sectionNode, out definitionNode))
                    {
                        configSectionsNode.AppendChild(sectionNode);
                        confNode.AppendChild(definitionNode);
                    }
                }
                catch { }
            }

            // dump as text

            var ms = new MemoryStream();
            xmlDocument.Save(ms);

            XmlData = ms.ToArray();
            XmlText = Encoding.UTF8.GetString(XmlData);
        }

        private ICommand saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                if (saveCommand == null)
                    saveCommand = new AnonymousCommand(_ => Save());
                return saveCommand;
            }
        }

        private void Save()
        {
            xmlDocument.Save(GetStrideConfigurationFilename());
        }

        private byte[] xmlData;
        public byte[] XmlData
        {
            get { return xmlData; }
            set { SetValue(ref xmlData, value, "XmlData"); }
        }

        private string xmlText;
        public string XmlText
        {
            get { return xmlText; }
            set { SetValue(ref xmlText, value, "XmlText"); }
        }
    }
}
