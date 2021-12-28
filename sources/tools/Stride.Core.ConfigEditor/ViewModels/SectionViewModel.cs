// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Collections.ObjectModel;
using System.Reflection;
using System.ComponentModel;
using System.Xml;

using Stride.Core.Presentation.ViewModel;

namespace Stride.ConfigEditor.ViewModels
{
    public class SectionViewModel : ViewModelBase
    {
        public Assembly Assembly { get; private set; }
        public Type Section { get; private set; }
        public IEnumerable<PropertyViewModel> Properties { get; private set; }

        private readonly List<PropertyViewModel> workingProperties = new List<PropertyViewModel>();

        public SectionViewModel(Assembly assembly, Type section)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");
            if (section == null)
                throw new ArgumentNullException("section");

            Assembly = assembly;
            Section = section;
            Name = Section.DeclaringType != null ? Section.DeclaringType.Name : "";
            Properties = new ReadOnlyCollection<PropertyViewModel>(workingProperties);
        }

        public void AddProperty(PropertyViewModel property)
        {
            if (property.Parent != this)
                throw new InvalidOperationException("Bad parent");
            workingProperties.Add(property);

            property.PropertyChanged += OnPropertyViewModelPropertyChanged;
        }

        private void OnPropertyViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsUsed")
                IsUsed = workingProperties.Any(p => p.IsUsed);
        }

        private bool isUsed;
        public bool IsUsed
        {
            get { return isUsed; }
            set { SetValue(ref isUsed, value, "IsUsed"); }
        }

        public string TypeName { get { return Section.FullName; } }

        private string name;
        public string Name
        {
            get { return name; }
            set
            {
                if (SetValue(ref name, value, "Name"))
                    IsValidName = !string.IsNullOrWhiteSpace(name);
            }
        }

        private bool isValidName;
        public bool IsValidName
        {
            get { return isValidName; }
            set { SetValue(ref isValidName, value, "IsValidName"); }
        }

        public bool CreateXmlNodes(XmlDocument doc, out XmlNode sectionNode, out XmlNode definitionNode)
        {
            sectionNode = null;
            definitionNode = null;

            if (IsUsed == false || string.IsNullOrWhiteSpace(Name))
                return false;

            try
            {
                // ---------------------------------------------------------------------------------
                var localSectionNode = doc.CreateElement("section");

                var attr = doc.CreateAttribute("name");
                attr.Value = Name;
                localSectionNode.Attributes.Append(attr);

                attr = doc.CreateAttribute("type");
                attr.Value = Section.AssemblyQualifiedName;
                localSectionNode.Attributes.Append(attr);
                // ---------------------------------------------------------------------------------

                // ---------------------------------------------------------------------------------
                var localDefinitionNode = doc.CreateElement(Name);
                foreach (var property in workingProperties)
                {
                    if (property.IsUsed == false)
                        continue;
                    attr = doc.CreateAttribute(property.Attribute.Name);
                    attr.Value = property.Value != null ? property.Value.ToString() : "";
                    localDefinitionNode.Attributes.Append(attr);
                }
                // ---------------------------------------------------------------------------------

                sectionNode = localSectionNode;
                definitionNode = localDefinitionNode;
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
