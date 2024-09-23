using Godot;
using PhantomCamera.ThreeDimension;
using System;

namespace PhantomCamera.Example;

public partial class PlayerControllerThirdPerson : PlayerController
{

	[Export]
	public float MouseSensitivity = .05f;

	[Export]
	public float MinPitch = -89.9f;
	[Export]
	public float MaxPitch = 50f;

	[Export]
	public float MinYaw = 0f;
	[Export]
	public float MaxYaw = 360f;

	private PhantomCamera3D _playerPcam;
	private PhantomCamera3D _aimPcam;
	private Node3D _playerDirection;
	private PhantomCamera3D _ceilingPcam;

	public override void _Ready()
	{
		base._Ready();

		_playerPcam = Owner.GetNode<PhantomCamera3D>("%PlayerPhantomCamera3D");
		_aimPcam = Owner.GetNode<PhantomCamera3D>("%PlayerAimPhantomCamera3D");
		_ceilingPcam = Owner.GetNode<PhantomCamera3D>("%CeilingPhantomCamera3D");

		_playerDirection = GetNode<Node3D>("%PlayerDirection");

		if (_playerPcam.followMode == FollowMode.THIRD_PERSON)
			Input.MouseMode = Input.MouseModeEnum.Captured;

	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		if (Velocity.Length() > 0.2)
		{
			Vector2 lookDirection = new(Velocity.Z, Velocity.X);

			Vector3 playerDirRotation = _playerDirection.Rotation;
			playerDirRotation.Y = lookDirection.Angle();
			_playerDirection.Rotation = playerDirRotation;
		}
	}

	public override void _UnhandledInput(InputEvent @inputEvent)
	{
		if (_playerPcam.followMode != FollowMode.THIRD_PERSON)
			return;

		if (IsInstanceValid(_aimPcam))
		{
			_setPcamRotation(_playerPcam, inputEvent);
			_setPcamRotation(_aimPcam, inputEvent);
			if (_playerPcam.Priority > _aimPcam.Priority)
				_toggleAimPcam(inputEvent);
			else
				_toggleAimPcam(inputEvent);
		}

		if (inputEvent is InputEventKey eventKey && eventKey.Pressed)
		{
			if (eventKey.Keycode == Key.Space)
			{
				if (_ceilingPcam.Priority < 30 && _playerPcam.IsActive)
					_ceilingPcam.Priority = 30;
				else
					_ceilingPcam.Priority = 1;
			}
		}

	}

	private void _setPcamRotation(PhantomCamera3D pcam, InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseMotion mouseMotionEvent)
		{
			Vector3 pcamRotationDegrees;

			// Assigns the current 3D rotation of the SpringArm3D node - so it starts off where it is in the editor
			pcamRotationDegrees = pcam.ThirdPersonRotationDegrees;

			// Change the X rotation
			pcamRotationDegrees.X -= mouseMotionEvent.Relative.Y * MouseSensitivity;

			// Clamp the rotation in the X axis so it go over or under the target
			pcamRotationDegrees.X = Mathf.Clamp(pcamRotationDegrees.X, MinPitch, MaxPitch);

			// Change the Y rotation value
			pcamRotationDegrees.Y -= mouseMotionEvent.Relative.X * MouseSensitivity;

			// Sets the rotation to fully loop around its target, but witout going below or exceeding 0 and 360 degrees respectively
			pcamRotationDegrees.Y = Mathf.Wrap(pcamRotationDegrees.Y, MinYaw, MaxYaw);

			//Change the SpringArm3D node's rotation and rotate around its target
			pcam.ThirdPersonRotationDegrees = pcamRotationDegrees;

		}
	}

	private void _toggleAimPcam(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseButton inputMouseButton && inputMouseButton.IsPressed() && inputMouseButton.ButtonIndex == MouseButton.Right && (_playerPcam.IsActive || _aimPcam.IsActive))
		{
			if (_playerPcam.Priority > _aimPcam.Priority)
				_aimPcam.Priority = 30;
			else
				_aimPcam.Priority = 0;
		}
	}

}
