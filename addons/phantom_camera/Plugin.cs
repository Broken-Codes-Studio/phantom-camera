#if TOOLS
using Godot;
using PhantomCamera.Gizmo;
using System;

namespace PhantomCamera;

[Tool]
public partial class Plugin : EditorPlugin
{

	#region Constants
	private const string PCAM_HOST = "PhantomCameraHost";
	private const string PCAM_2D = "PhantomCamera2D";
	private const string PCAM_3D = "PhantomCamera3D";

	private readonly StringName PHANTOM_CAMERA_MANAGER = "PhantomCameraManager";
	#endregion

	#region Variables

	private PhantomCameraGizmoPlugin3D _pcam3DGizmoPlugin = new();

	private Control _editorPanelInstance;
	private Button panelButton;

	#endregion

	#region Override Methods


	public override void _EnterTree()
	{
		AddAutoloadSingleton(PHANTOM_CAMERA_MANAGER, "res://addons/phantom_camera/scripts/managers/PhantomCameraManager.cs");

		// Phantom Camera Nodes
		//AddCustomType(PCAM_2D,"Node2D",GD.Load<Script>("res://addons/phantom_camera/scripts/phantom camera/PhantomCamera2D.cs"),GD.Load<Texture2D>("res://addons/phantom_camera/icons/phantom_camera_2d.svg"));
		AddCustomType(PCAM_3D, "Node3D", GD.Load<Script>("res://addons/phantom_camera/scripts/phantom camera/PhantomCamera3D.cs"), GD.Load<Texture2D>("res://addons/phantom_camera/icons/phantom_camera_3d.svg"));
		AddCustomType(PCAM_HOST, "Node", GD.Load<Script>("res://addons/phantom_camera/scripts/phantom camera host/PhantomCameraHost.cs"), GD.Load<Texture2D>("res://addons/phantom_camera/icons/phantom_camera_host.svg"));

		// Phantom Camera 3D Gizmo
		AddNode3DGizmoPlugin(_pcam3DGizmoPlugin);

		// TODO: Should be disabled unless in editor
		// Viewfinder

	}

	public override void _ExitTree()
	{

		RemoveNode3DGizmoPlugin(_pcam3DGizmoPlugin);

		//RemoveCustomType(PCAM_2D);
		RemoveCustomType(PCAM_3D);
		RemoveCustomType(PCAM_HOST);

		RemoveAutoloadSingleton(PHANTOM_CAMERA_MANAGER);
	}

	#endregion

	#region Private Methods

	private void _BtnToggled(bool toggledOn)
	{

	}

	private void _makeVisible(bool visible)
	{

	}

	private void _sceneChanged(Node sceneRoot)
	{

	}

	#endregion

	#region Public Methods

	public string Getversion()
	{
		ConfigFile config = new();
		config.Load(GetScript().As<Script>().ResourcePath.GetBaseDir() + "/plugin.cfg");
		return config.GetValue("plugin", "version").AsString();
	}

	#endregion

}
#endif