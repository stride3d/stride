// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Reflection;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Settings
{
    [YamlSerializerFactory(YamlProfile)]
    internal class SettingsProfileSerializer : SettingsDictionarySerializer
    {
        public const string YamlProfile = "Settings";

        public override IYamlSerializable TryCreate(SerializerContext context, ITypeDescriptor typeDescriptor)
        {
            var type = typeDescriptor.Type;
            return type == typeof(SettingsProfile) ? this : null;
        }

        protected override void CreateOrTransformObject(ref ObjectContext objectContext)
        {
            var settingsProfile = (SettingsProfile)objectContext.Instance;
            var settingsDictionary = new SettingsDictionary { Profile = settingsProfile };

            if (objectContext.SerializerContext.IsSerializing)
            {
                settingsProfile.Container.EncodeSettings(settingsProfile, settingsDictionary);
            }

            objectContext.Instance = settingsDictionary;

            base.CreateOrTransformObject(ref objectContext);
        }

        protected override void TransformObjectAfterRead(ref ObjectContext objectContext)
        {
            if (!objectContext.SerializerContext.IsSerializing)
            {
                var settingsDictionary = (SettingsDictionary)objectContext.Instance;
                var settingsProfile = settingsDictionary.Profile;

                settingsProfile.Container.DecodeSettings(settingsDictionary, settingsProfile);

                objectContext.Instance = settingsProfile;
            }
        }
    }
}
