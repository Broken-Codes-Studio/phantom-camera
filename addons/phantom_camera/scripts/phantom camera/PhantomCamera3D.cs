using Godot;
using PhantomCamera.Resources;
using System;

namespace PhantomCamera;

/// <summary>
/// Determines the positional logic for a given [param PhantomCamera3D]
/// <br/>
/// The different modes have different functionalities and purposes, so choosing
/// the correct one depends on what each [param PhantomCamera3D] is meant to do.
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
    /// Follows a target while being positionally confined to a [Path3D] node.
    /// </summary>
    PATH = 4,
    /// <summary>
    /// Applies a dead zone on the frame and only follows its target when it tries to leave it.
    /// </summary>
    FRAMED = 5,
    /// <summary>
    /// Applies a [SpringArm3D] node to the target's position and allows for rotating around it.
    /// </summary>
    THIRD_PERSON = 6,
}

/// <summary>
/// Determines the rotational logic for a given [param PhantomCamera3D].
/// <br/>
/// The different modes has different functionalities and purposes, so
/// choosing the correct mode depends on what each [param PhantomCamera3D] is meant to do.
/// </summary>
public enum LookAtMode
{
    /// <summary>
    /// Default - No Look At logic is applied.
    /// </summary>
    NONE = 0,
    /// <summary>
    /// Copies its target's rotational value.
    /// </summary>
    MIMIC = 1,
    /// <summary>
    /// Looks at its target in a straight line.
    /// </summary>
    SIMPLE = 2,
    /// <summary>
    /// Looks at the centre of its targets.
    /// </summary>
    GROUP = 3,
}

/// <summary>
/// Determines how often an inactive [param PhantomCamera3D] should update
/// its positional and rotational values. This is meant to reduce the amount
/// of calculations inactive [param PhantomCamera3D] are doing when idling to improve performance.
/// </summary>
public enum InactiveUpdateMode
{
    /// <summary>
    /// Always updates the [param PhantomCamera3D], even when it's inactive.
    /// </summary>
    ALWAYS,
    /// <summary>
    /// Never updates the [param PhantomCamera3D] when it's inactive. Reduces the amount of computational resources when inactive.
    /// </summary>
    NEVER,
    //	EXPONENTIALLY,
}

/// <summary>
/// Controls a scene's [Camera3D] and applies logic to it.
/// 
/// The scene's [param Camera3D] will follow the position of the
/// [param PhantomCamera3D] with the highest priority.
/// Each instance can have different positional and rotational logic applied to them.
/// </summary>
[Icon("res://addons/phantom_camera/icons/phantom_camera_3d.svg")]
[Tool]
public partial class PhantomCamera3D : Node3D
{
    #region Signals

    /// <summary>
    /// Emitted when the [param PhantomCamera2D] becomes active.
    /// </summary>
    [Signal]
    public delegate void BecameActiveEventHandler();
    /// <summary>
    /// Emitted when the [param PhantomCamera2D] becomes inactive.
    /// </summary>
    [Signal]
    public delegate void BecameInactiveEventHandler();

    /// <summary>
    /// Emitted when [member follow_target] changes.
    /// </summary>
    [Signal]
    public delegate void FollowTargetChangedEventHandler();

    /// <summary>
    /// Emitted when dead zones changes.
    /// <br/>
    /// <b>Note:</b> Only applicable in [param Framed] [member FollowMode].
    /// </summary>
    [Signal]
    public delegate void DeadZoneChangedEventHandler();

    /// <summary>
    /// Emitted when the [param Camera3D] starts to tween to another [param PhantomCamera3D].
    /// </summary>
    [Signal]
    public delegate void TweenStartedEventHandler();

    /// <summary>
    /// Emitted when the [param Camera3D] is to tweening towards another [param PhantomCamera3D].
    /// </summary>
    [Signal]
    public delegate void IsTweeningEventHandler();

    /// <summary>
    /// Emitted when the tween is interrupted due to another [param PhantomCamera3D]
    /// becoming active. The argument is the [param PhantomCamera3D] that interrupted the tween.
    /// </summary>
    [Signal]
    public delegate void TweenInterruptedEventHandler(PhantomCamera3D pcam3D);

    /// <summary>
    /// Emitted when the [param Camera3D] completes its tween to the [param PhantomCamera3D].
    /// </summary>
    [Signal]
    public delegate void TweenCompletedEventHandler();

    #endregion

    #region Variables

    private PhantomCameraHost _pcamHostOwner = null;
    /// <summary>
    /// The [PhantomCameraHost] that owns this [param PhantomCamera2D].
    /// </summary>
    public PhantomCameraHost PcamHostOwner
    {
        get => _pcamHostOwner;
        set
        {
            _pcamHostOwner = value;
            // if(IsInstanceValid(PcamHostOwner))
            //     PcamHostOwner.PcamAddedToScene(this);
        }
    }

    public bool IsActive { get; private set; } = false;

    private bool _priorityOverride = false;
    /// <summary>
    /// To quickly preview a [param PhantomCamera3D] without adjusting its
    /// [member Priority], this property allows the selected [param PhantomCamera3D]
    /// to ignore the Priority system altogether and forcefully become the active
    /// one. It's partly designed to work within the [param viewfinder], and will be
    /// disabled when running a build export of the game.
    /// </summary>
    [Export]
    public bool PriorityOverride
    {
        get => _priorityOverride;
        set
        {
            if (HasValidPcamOwner() && Engine.IsEditorHint())
            {
                _priorityOverride = value;
                if (value == true)
                {
                    // PcamHostOwner.PcamPriorityOverride(this);
                }
                else
                {
                    // PcamHostOwner.PcamPriorityUpdated(this);
                    // PcamHostOwner.PcamPriorityOverrideDisabled();
                }
            }
        }
    }

    private int _priority = 0;
    /// <summary>
    /// It defines which [param PhantomCamera3D] a scene's [param Camera3D] should
    /// be corresponding with and be attached to. This is decided by the
    /// [param PhantomCamera3D] with the highest [param priority].
    /// <br/>
    /// Changing [param priority] will send an event to the scene's
    /// [PhantomCameraHost], which will then determine whether if the
    /// [param priority] value is greater than or equal to the currently
    /// highest [param PhantomCamera3D]'s in the scene. The [param PhantomCamera3D]
    /// with the highest value will then reattach the Camera accordingly.
    /// </summary>
    [Export]
    public int Priority
    {
        get => _priority;
        set
        {
            _priority = value < 0 ? 0 : value;
            // if (HasValidPcamOwner())
            //     PcamHostOwner.PcamPriorityUpdated(this);
        }
    }

    private FollowMode _followMode = FollowMode.NONE;
    /// <summary>
    /// Determines the positional logic for a given [param PhantomCamera3D].
    /// The different modes have different functionalities and purposes, so
    /// choosing the correct one depends on what each [param PhantomCamera3D]
    /// is meant to do.
    /// </summary>
    [Export]
    public FollowMode followMode
    {
        get => _followMode;
        set
        {
            _followMode = value;

            if (value == FollowMode.FRAMED)
            {

            }
            else
            {

            }

            // if(value == FollowMode.NONE)

            // else if (value == FollowMode.GROUP && ())

            NotifyPropertyListChanged();
        }
    }

    private Node3D _followTarget = null;
    /// <summary>
    /// Determines which target should be followed.
    /// The [param Camera3D] will follow the position of the Follow Target based on
    /// the [member follow_mode] type and its parameters.
    /// </summary>
    [Export]
    public Node3D FollowTarget
    {
        get => _followTarget;
        set
        {
            if (_followTarget.Equals(value))
                return;
            _followTarget = value;

            _followTargetPhysicsBased = false;
            if (IsInstanceValid(value))
            {
                _shouldFollow = true;
                //CheckPhysicsBody(value);
            }
            else
                _shouldFollow = false;

            EmitSignal(SignalName.FollowTargetChanged);
            NotifyPropertyListChanged();
        }
    }

    private Node3D[] _followTargets = { };
    /// <summary>
    /// Defines the targets that the [param PhantomCamera3D] should be following.
    /// </summary>
    [Export]
    public Node3D[] FollowTargets
    {
        get => _followTargets;
        set
        {
            if (_followTargets.Equals(value))
                return;
            _followTargets = value;

            if (_followTargets.Length == 0)
            {
                _shouldFollow = false;
                _hasMultipleFollowTargets = false;
                _followTargetPhysicsBased = false;
                return;
            }

            int validInstances = 0;
            _followTargetPhysicsBased = false;
            foreach (Node3D target in FollowTargets)
            {
                if (IsInstanceValid(target))
                {
                    _shouldFollow = true;
                    ++validInstances;
                    //CheckPhysicsBody(target)
                    if (validInstances > 1)
                        _hasMultipleFollowTargets = true;
                }
            }

        }
    }

    private Path3D _followPath = null;
    /// <summary>
    /// Determines the [Path3D] node the [param PhantomCamera3D]
    /// should be bound to.
    /// The [param PhantomCamera3D] will follow the position of the
    /// [member follow_target] while sticking to the closest point on this path.
    /// </summary>
    [Export]
    public Path3D FollowPath
    {
        get => _followPath;
        set => _followPath = value;
    }

    private LookAtMode _lookatMode = LookAtMode.NONE;
    /// <summary>
    /// Determines the rotational logic for a given [param PhantomCamera3D].
    /// The different modes has different functionalities and purposes,
    /// so choosing the correct mode depends on what each
    /// [param PhantomCamera3D] is meant to do.
    /// </summary>
    [Export]
    public LookAtMode lookAtMode
    {
        get => _lookatMode;
        set
        {
            _lookatMode = value;
            if (LookAtTarget is Node3D)
                _shouldLookAt = true;

            if (_lookatMode == LookAtMode.NONE)
                _shouldLookAt = false;
            else if (_lookatMode == LookAtMode.GROUP && (LookAtTarget != null || LookAtTarget != null))
                _shouldLookAt = true;

            NotifyPropertyListChanged();
        }
    }

    /// <summary>
    /// Determines which target should be looked at.
    /// The [param PhantomCamera3D] will update its rotational value as the
    /// target changes its position.
    /// </summary>
    [Export]
    public Node3D LookAtTarget { get; set; } = null;

    private Node3D[] _lookAtTargets = { };
    /// <summary>
    /// Defines the targets that the camera should looking at.
    /// It will be looking at the centre of all the assigned targets.
    /// </summary>
    [Export]
    public Node3D[] LookAtTargets
    {
        get => _lookAtTargets;
        set
        {
            _lookAtTargets = value;
        }
    }

    /// <summary>
    /// Defines how [param ]PhantomCamera3Ds] transition between one another.
    /// Changing the tween values for a given [param PhantomCamera3D]
    /// determines how transitioning to that instance will look like.
    /// This is a resource type that can be either used for one
    /// [param PhantomCamera] or reused across multiple - both 2D and 3D.
    /// By default, all [param PhantomCameras] will use a [param linear]
    /// transition, [param easeInOut] ease with a [param 1s] duration.
    /// </summary>
    [Export]
    public PhantomCameraTween TweenResource { get; set; } = new();

    public float TweenDuration
    {
        get => TweenResource.duration;
        set => TweenResource.duration = value;
    }

    public TransitionType TweenTransition
    {
        get => TweenResource.transition;
        set => TweenResource.transition = value;
    }

    public EaseType TweenEase{
        get => TweenResource.ease;
        set => TweenResource.ease = value;
    }

    /// <summary>
    /// If enabled, the moment a [param PhantomCamera3D] is instantiated into
    /// a scene, and has the highest priority, it will perform its tween transition.
    /// This is most obvious if a [param PhantomCamera3D] has a long duration and
    /// is attached to a playable character that can be moved the moment a scene
    /// is loaded. Disabling the [param tween_on_load] property will
    /// disable this behaviour and skip the tweening entirely when instantiated.
    /// </summary>
    [Export]
    public bool TweenOnLoad {get; set;} = true;

    /// <summary>
    /// Determines how often an inactive [param PhantomCamera3D] should update
    /// its positional and rotational values. This is meant to reduce the amount
    /// of calculations inactive [param PhantomCamera3Ds] are doing when idling
    /// to improve performance.
    /// </summary>
    [Export]
    public InactiveUpdateMode inactiveUpdateMode {get; set;} = InactiveUpdateMode.ALWAYS;

    #region Camera3DResouce property getters

    /// <summary>
    /// A resource type that allows for overriding the [param Camera3D] node's properties.
    /// </summary>
    [Export]
    public Camera3DResource camera3DResource {get;set;} = new();

    public int CullMask {
        get => camera3DResource.cull_mask;
        set{
            camera3DResource.cull_mask = value;
            // if(IsActive)
            //     PcamHostOwner.Camera3DCullMask = value;
        }
    }

    //TODO: Add Rest of the variables

    #endregion

    private bool _shouldFollow = false;
    private bool _followTargetPhysicsBased = false;
    private bool _physicsInterpolationEnabled = false;

    private bool _shouldLookAt = false;
    private bool _hasMultipleFollowTargets = false;

    private Node3D[] _validLookAtTargets;

    private bool _tweenSkip = false;

    private Vector3 _followVelocityRef = Vector3.Zero;

    private Vector3 _currentRotation;

    private Node _phantomCameraManager;

    #endregion

    #region Property Validator

    private bool HasValidPcamOwner()
    {
        if (PcamHostOwner == null)
            return false;
        if (!IsInstanceValid(PcamHostOwner))
            return false;
        // if(!IsInstanceValid(PcamHostOwner.Camera_3D))
        //     return false;
        return true;
    }

    #endregion

}
