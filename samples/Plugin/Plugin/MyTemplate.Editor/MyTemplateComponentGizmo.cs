using Stride.Engine;
using Stride.Engine.Gizmos;
using Stride.Assets.Editor.AssetEditors.Gizmos;

namespace MyTemplate.Editor;

/// <summary>
/// A billboard drawn at the entity in the scene editor, so the component shows where it is.
/// </summary>
[GizmoComponent(typeof(MyTemplateComponent), true)]
public class MyTemplateComponentGizmo : BillboardingGizmo<MyTemplateComponent>
{
    public MyTemplateComponentGizmo(EntityComponent component)
        : base(component, "MyTemplate", MyTemplateEditorPlugin.LoadResource("MyTemplateGizmo.png"))
    {
    }
}
