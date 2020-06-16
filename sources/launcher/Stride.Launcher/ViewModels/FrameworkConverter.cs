using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NuGet.Frameworks;
using Stride.Core.Annotations;
using Stride.Core.Presentation.ValueConverters;

namespace Stride.LauncherApp.ViewModels
{
    class FrameworkConverter : ValueConverterBase<FrameworkConverter>
    {
        public override object Convert(object value, [NotNull] Type targetType, object parameter, CultureInfo culture)
        {
            var frameworkFolder = (string)value;

            var framework = NuGetFramework.ParseFolder(frameworkFolder);
            if (framework.Framework == ".NETFramework")
                return $".NET {framework.Version.ToString(3)}";
            else if (framework.Framework == ".NETCoreApp")
                return $".NET Core {framework.Version.ToString(2)}";

            // fallback
            return $"{framework.Framework} {framework.Version.ToString(3)}";
        }

        public override object ConvertBack(object value, [NotNull] Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
