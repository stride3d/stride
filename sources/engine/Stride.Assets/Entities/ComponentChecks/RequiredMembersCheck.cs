using System;
using System.Linq;
using System.Reflection;
using Stride.Core.Annotations;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.Reflection;
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
            var typeDescriptor = TypeDescriptorFactory.Default.Find(component.GetType());
            var componentName = typeDescriptor.Type.Name; // Should we check attributes for another name?

            var members = typeDescriptor.Members;

            foreach (var member in members)
            {
                if (member.Type.IsValueType)
                    continue; // value types cannot be null, and must always have a proper default value

                MemberRequiredAttribute memberRequired;
                if ((memberRequired = member.GetCustomAttributes<MemberRequiredAttribute>(true).FirstOrDefault()) != null)
                {
                    if (member.Get(component) == null)
                        WriteResult(result, componentName, targetUrlInStorage, entity.Name, member.Name, memberRequired.ReportAs);
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
