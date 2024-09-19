using Godot;
using System;

public partial class PlayerController : CharacterBody3D
{
	[Export]
	public float Speed = 5.0f;
	[Export]
	public float JumpVelocity = 4.5f;
	[Export]
	public bool EnableGravity = true;

	public bool MovementEnabled = true;

	private Camera3D _camera;
	private Node3D _playerVisual;

	private Transform3D _physicsBodyTransLast;
	private Transform3D _physicsBodyTransCurrent;

	public override void _Ready()
	{
		_playerVisual = GetNode<Node3D>("%PlayerVisual");
		_camera = Owner.GetNode<Camera3D>("%MainCamera3D");

		_playerVisual.TopLevel = true;
	}

	public override void _PhysicsProcess(double delta)
	{
		_physicsBodyTransLast = _physicsBodyTransCurrent;
		_physicsBodyTransCurrent = GlobalTransform;

		Vector3 velocity = Velocity;

		// Add the gravity.
		if (EnableGravity && !IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		if (!MovementEnabled)
			return;

		// Handle Jump.
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	public override void _Process(double delta)
	{
		_playerVisual.GlobalTransform = _physicsBodyTransLast.InterpolateWith(_physicsBodyTransCurrent, (float)Engine.GetPhysicsInterpolationFraction());
	}

}
