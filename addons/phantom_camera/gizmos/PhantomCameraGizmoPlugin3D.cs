using Godot;
using System;

namespace PhantomCamera.Gizmo;

public partial class PhantomCameraGizmoPlugin3D : CustomPluginGizmo
{
	
	const string SPATIAL_SCRIPT_PATH = "res://addons/phantom_camera/scripts/phantom_camera/phantom_camera_3d.cs";
	const string ICON_PATH = "res://addons/phantom_camera/icons/phantom_camera_gizmo.svg";

	public PhantomCameraGizmoPlugin3D() {
		gizmo_name = "PhantomCamera";
		gizmo_spatial_script = GD.Load<Script>(SPATIAL_SCRIPT_PATH);
		gizmo_icon = GD.Load<Texture2D>(ICON_PATH);
		HandleMaterials();
	}
	
}