using System;
using System.Reflection;
using Stride.Core.Annotations;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Engine;

namespace Stride.Assets.Entities.ComponentChecks
{
    /// <summary>
    /// Checks if all members of a component with a <see cref="MemberRequiredAttribute"/> are assigned a value.
    /// </summary>
    public class RequiredMembersCheck : IEntityComponentCheck
    {
        /// <inheritdoc/>
        public bool AppliesTo(Type componentType)
        {
            return true; // applies to any component
        }

        /// <inheritdoc/>
        public void Check(EntityComponent component, Entity entity, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var type = component.GetType();
            var componentName = type.Name; // QUESTION: Should we check attributes for another name?

            var fields = type.GetFields(); // public fields, only those can be serialized?
            var properties = type.GetRuntimeProperties(); // all properties may have a DataMember attribute

            foreach (var field in fields)
            {
                if (field.FieldType.IsValueType)
                    continue; // value types cannot be null, and must always have a proper default value

                MemberRequiredAttribute memberRequired;
                if ((memberRequired = field.GetCustomAttribute<MemberRequiredAttribute>()) != null)
                {
                    if (field.GetValue(component) == null)
                        WriteResult(result, componentName, targetUrlInStorage, entity.Name, field.Name, memberRequired.ReportAs);
                }
            }

            foreach (var prop in properties)
            {
                if (prop.PropertyType.IsValueType)
                    continue; // value types cannot be null, and must always have a proper default value

                MemberRequiredAttribute memberRequired;
                if ((memberRequired = prop.GetCustomAttribute<MemberRequiredAttribute>()) != null)
                {
                    if (prop.GetValue(component) == null)
                        WriteResult(result, componentName, targetUrlInStorage, entity.Name, prop.Name, memberRequired.ReportAs);
                }
            }
        }

        private void WriteResult(AssetCompilerResult result, string componentName, string targetUrlInStorage, string entityName, string memberName, MemberRequiredReportType reportType)
        {
            var logMsg = $"The component {componentName} on entity [{targetUrlInStorage}:{entityName}] is missing a value on a required field '{memberName}'.";
            switch (reportType)
            {
                case MemberRequiredReportType.Warning:
                    result.Warning(logMsg);
                    break;
                case MemberRequiredReportType.Error:
                    result.Error(logMsg);
                    break;
            }
        }
    }
}
