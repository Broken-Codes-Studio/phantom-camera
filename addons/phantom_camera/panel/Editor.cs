#if TOOLS
using Godot;
using System;

public partial class Editor : VBoxContainer
{

	//private EditorPlugin _editorPlugin;

	private Control _viewFinder;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_viewFinder = GetNode<Control>("%ViewfinderPanel");
	}

}
#endif