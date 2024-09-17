#if TOOLS
using Godot;
using System;

namespace PhantomCamera;

using Gizmo;
using Inspector;

[Tool]
public partial class Plugin : EditorPlugin
{

	#region Constants
	private const string PCAM_HOST = "PhantomCameraHost";
	private const string PCAM_2D = "PhantomCamera2D";
	private const string PCAM_3D = "PhantomCamera3D";

	private const string EDITOR_PANEL_PATH = "res://addons/phantom_camera/panel/editor.tscn";

	private readonly StringName PHANTOM_CAMERA_MANAGER = "PhantomCameraManager";
	#endregion

	#region Variables

	private PhantomCameraGizmoPlugin3D _pcam3DGizmoPlugin = new();

	private EditorContainer _editorPanelInstance = ResourceLoader.Load<PackedScene>(EDITOR_PANEL_PATH).Instantiate<EditorContainer>();
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
		// VBoxContainer test = ResourceLoader.Load<PackedScene>(EDITOR_PANEL_PATH).Instantiate<VBoxContainer>();
		// _editorPanelInstance = (EditorContainer)test;
		_editorPanelInstance.editorPlugin = this;
		panelButton = AddControlToBottomPanel(_editorPanelInstance, "Phantom Camera");

		// Trigger events in the viewfinder whenever
		panelButton.Toggled += _BtnToggled;

		SceneChanged += _editorPanelInstance.viewFinder.SceneChanged;

		SceneChanged += _sceneChanged;

	}

	public override void _ExitTree()
	{

		panelButton.Toggled -= _BtnToggled;

		SceneChanged -= _editorPanelInstance.viewFinder.SceneChanged;

		SceneChanged -= _sceneChanged;

		RemoveControlFromBottomPanel(_editorPanelInstance);
		_editorPanelInstance.QueueFree();

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
		if (toggledOn)
		{
			_editorPanelInstance.viewFinder.ViewFinderVisible = true;
			_editorPanelInstance.viewFinder.VisibilityCheck();
		}
		else
			_editorPanelInstance.viewFinder.ViewFinderVisible = false;
	}

	private void _makeVisible(bool visible)
	{
		if (_editorPanelInstance is not null)
			_editorPanelInstance.Visible = visible;
	}

	private void _sceneChanged(Node sceneRoot)
	{
		_editorPanelInstance.viewFinder.SceneChanged(sceneRoot);
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