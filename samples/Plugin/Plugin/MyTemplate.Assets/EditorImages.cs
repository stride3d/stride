using MyTemplate;
using Stride.Core.Assets.Editor.Annotations;

// The component's icon in the property grid and the scene editor, an embedded resource of this assembly. Declared here
// rather than in the Editor package, so a plugin without one still gets its icons.
[assembly: TypeImage(typeof(MyTemplateComponent), "MyTemplateComponent.png")]
