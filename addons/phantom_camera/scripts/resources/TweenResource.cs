using Godot;
using System;

namespace PhantomCamera.Resources;

public enum TransitionType : byte
{
    /// <summary>
    /// The animation is interpolated linearly.
    /// </summary>
    LINEAR = 0,
    /// <summary>
    /// The animation is interpolated using a sine function.
    /// </summary>
    SINE = 1,
    /// <summary>
    /// The animation is interpolated with a quintic (to the power of 5) function.
    /// </summary>
    QUINT = 2,
    /// <summary>
    /// The animation is interpolated with a quartic (to the power of 4) function.
    /// </summary>
    QUART = 3,
    /// <summary>
    /// The animation is interpolated with a quadratic (to the power of 2) function.
    /// </summary>
    QUAD = 4,
    /// <summary>
    /// The animation is interpolated with an exponential (to the power of x) function.
    /// </summary>
    EXPO = 5,
    /// <summary>
    /// The animation is interpolated with elasticity, wiggling around the edges.
    /// </summary>
    ELASTIC = 6,
    /// <summary>
    /// The animation is interpolated with a cubic (to the power of 3) function.
    /// </summary>
    CUBIC = 7,
    /// <summary>
    /// The animation is interpolated with a function using square roots.
    /// </summary>
    CIRC = 8,
    /// <summary>
    /// The animation is interpolated by bouncing at the end.
    /// </summary>
    BOUNCE = 9,
    /// <summary>
    /// The animation is interpolated backing out at ends.
    /// </summary>
    BACK = 10,
    //CUSTOM 	= 11,
    //NONE 	= 12,
}

public enum EaseType : byte
{
    /// <summary>
    /// The interpolation starts slowly and speeds up towards the end.
    /// </summary>
    EASE_IN = 0,
    /// <summary>
    /// The interpolation starts quickly and slows down towards the end.
    /// </summary>
    EASE_OUT = 1,
    /// <summary>
    /// A combination of EASE_IN and EASE_OUT. The interpolation is slowest at both ends.
    /// </summary>
    EASE_IN_OUT = 2,
    /// <summary>
    /// A combination of EASE_IN and EASE_OUT. The interpolation is fastest at both ends.
    /// </summary>
    EASE_OUT_IN = 3,
}

/// <summary>
/// Tweening resource for [PhantomCamera2D] and [PhantomCamera3D].
/// Defines how [param PhantomCameras] transition between one another.
/// Changing the tween values for a given [param PhantomCamera] determines how
/// transitioning to that instance will look like.
/// </summary>
[Icon("res://addons/phantom_camera/icons/phantom_camera_tween.svg")]
public partial class PhantomCameraTween : Resource
{
    /// <summary>
    /// The time it takes to tween to this PhantomCamera in [param seconds].
    /// </summary>
    [Export]
    public float duration { get; set; } = 1;

    /// <summary>
    /// The transition bezier type for the tween. The options are defined in the [enum TransitionType].
    /// </summary>
    [Export]
    public TransitionType transition { get; set; } = TransitionType.LINEAR;

    /// <summary>
    /// The ease type for the tween. The options are defined in the [enum EaseType].
    /// </summary>
    [Export]
    public EaseType ease { get; set; } = EaseType.EASE_IN_OUT;

    public PhantomCameraTween()
    {

    }

}
