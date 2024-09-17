#if TOOLS
using Godot;
using System;

namespace PhantomCamera.Inspector;

public partial class Editor : VBoxContainer
{

	public EditorPlugin editorPlugin;

	public ViewFinder viewFinder;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		viewFinder = GetNode<ViewFinder>("%ViewfinderPanel");
	}

}
#endif