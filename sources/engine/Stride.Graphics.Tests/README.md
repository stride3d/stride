This is the Stride.Graphics.Tests project used to perform testing for the selected platform.

To add/remove/update tests for all platforms, check the associated Stride.Graphics.Tests.Shared project.
To add/remove/update tests for a specific platform, add them here and make sure to add the Label "Stride.DoNotSync" as in:
    <Compile Label="Stride.DoNotSync" Include="MyPlatformSpecificType.cs" />
