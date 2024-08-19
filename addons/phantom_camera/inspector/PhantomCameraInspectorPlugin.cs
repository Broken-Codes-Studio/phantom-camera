#if TOOLS
using Godot;
using System;

namespace PhantomCamera.Inspector;

[Tool]
public partial class PhantomCameraInspectorPlugin : EditorInspectorPlugin
{

    //const string PHANTOM_CAMERA_SCRIPT_PATH = "res://addons/PhantomCamera/scripts/phantom_camera.cs";

    //TODO: Enable again once work is resumed for inspector based tasks

    /*public override bool _CanHandle(GodotObject gdObject)
    {
        
    }*/

    public override void _ParseCategory(GodotObject gdObject, string category)
    {
        MarginContainer _marginContainer = new();
        int _margin_v = 20;

        _marginContainer.AddThemeConstantOverride("margin_left", 10);
        _marginContainer.AddThemeConstantOverride("margin_top", _margin_v);
        _marginContainer.AddThemeConstantOverride("margin_right", 10);
        _marginContainer.AddThemeConstantOverride("margin_bottom", _margin_v);

        AddCustomControl(_marginContainer);

        VBoxContainer _vboxContainer = new();
        _marginContainer.AddChild(_vboxContainer);

        Button _alignWithViewButton = new();
        _alignWithViewButton.Connect("pressed", Callable.From(()=> _AlignCameraWithView(gdObject)));
        _alignWithViewButton.CustomMinimumSize = new(0,60);
        _alignWithViewButton.Text = "Align with view";
        _vboxContainer.AddChild(_alignWithViewButton);

        Button _PreviewCameraButton = new();
        _PreviewCameraButton.Connect("pressed", Callable.From(()=> _PreviewCamera(gdObject)));
        _PreviewCameraButton.CustomMinimumSize = new(0,60);
        _PreviewCameraButton.Text = "Preview Camera";
        _vboxContainer.AddChild(_PreviewCameraButton);

    }

    private void _AlignCameraWithView(GodotObject gdObject)
    {
        GD.Print("Aligning camera with view");
	    GD.Print(gdObject);
    }

    private void _PreviewCamera(GodotObject gdObject)
    {
        GD.Print("Previewing camera");
	    GD.Print(gdObject);
    }

}
#endif