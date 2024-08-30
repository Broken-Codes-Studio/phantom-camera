using Godot;
using System;

namespace PhantomCamera.TwoDimension;

#region Enums
/// <summary>
/// Determines the positional logic for a given [param PCamPhantomCamera2D]<br/>
/// The different modes have different functionalities and purposes, so choosing
/// the correct one depends on what each [param PhantomCamera2D] is meant to do.
/// </summary>
public enum FollowMode
{
    /// <summary>
    /// Default - No follow logic is applied.
    /// </summary>
    NONE = 0,
    /// <summary>
    /// Sticks to its target.
    /// </summary>
    GLUED = 1,
    /// <summary>
    /// Follows its target with an optional offset.
    /// </summary>
	SIMPLE = 2,
    /// <summary>
    /// Follows multiple targets with option to dynamically reframe itself.
    /// </summary>
	GROUP = 3,
    /// <summary>
    /// Follows a target while being positionally confined to a [Path2D] node.
    /// </summary>
	PATH = 4,
    /// <summary>
    /// Applies a dead zone on the frame and only follows its target when it tries to leave it.
    /// </summary>
	FRAMED = 5,
}

/// <summary>
/// Determines how often an inactive [param PhantomCamera2D] should update
/// its positional and rotational values. This is meant to reduce the amount
/// of calculations inactive [param PhantomCamera2D] are doing when idling to
/// improve performance.
/// </summary>
public enum InactiveUpdateMode
{
    /// <summary>
    /// Always updates the [param PhantomCamera2D], even when it's inactive.
    /// </summary>
	ALWAYS,
    /// <summary>
    /// Never updates the [param PhantomCamera2D] when it's inactive. Reduces the amount of computational resources when inactive.
    /// </summary>
	NEVER,
    //EXPONENTIALLY,
}

#endregion
/// <summary>
/// Controls a scene's [Camera2D] and applies logic to it.
/// The scene's [param Camera2D] will follow the position of the
/// [param PhantomCamera2D] with the highest priority.
/// Each instance can have different positional and rotational logic applied
/// to them.
/// </summary>
[Tool]
[Icon("res://addons/phantom_camera/icons/phantom_camera_2d.svg")]
public partial class PhantomCamera2D : Node2D
{
}
