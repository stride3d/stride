using System;

namespace Stride.Core.Annotations
{
    /// <summary>
    /// This attribute signals the asset compiler that the field/property
    /// is required to have a value (i.e. not null) when compiling assets.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class MemberRequiredAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the reporting level (warning/error) of the notification that the field/property is null.
        /// </summary>
        public MemberRequiredReportType ReportAs { get; set; } = MemberRequiredReportType.Warning;
    }

    /// <summary>
    /// Specifies the reporting level for a missing value of a field/property with a <see cref="MemberRequiredAttribute"/>.
    /// </summary>
    public enum MemberRequiredReportType
    {
        /// <summary>
        /// Report missing required member as a warning.
        /// </summary>
        Warning,

        /// <summary>
        /// Report missing required member as an error.
        /// </summary>
        Error,
    }
}
