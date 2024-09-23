using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using Godot.Collections;

namespace PhantomCamera.ThreeDimension;

using Resources;

#region Enums
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

#endregion

/// <summary>
/// Controls a scene's [Camera3D] and applies logic to it.
/// 
/// The scene's [param Camera3D] will follow the position of the
/// [param PhantomCamera3D] with the highest priority.
/// Each instance can have different positional and rotational logic applied to them.
/// </summary>
[Tool, Icon("res://addons/phantom_camera/icons/phantom_camera_3d.svg")]
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
    /// Emitted when [member look_at_target] changes.
    /// </summary>
    [Signal]
    public delegate void LookAtTargetChangedEventHandler();

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
            if (IsInstanceValid(PcamHostOwner))
                PcamHostOwner.PcamAddedToScene(this);
        }
    }

    public bool IsActive { get; set; } = false;

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
            if (_hasValidPcamOwner() && Engine.IsEditorHint())
            {
                _priorityOverride = value;
                if (value == true)
                {
                    PcamHostOwner.PcamPriorityOverride(this);
                }
                else
                {
                    PcamHostOwner.PcamPriorityUpdated(this);
                    PcamHostOwner.PcamPriorityOverrideDisabled();
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
            if (_hasValidPcamOwner())
                PcamHostOwner.PcamPriorityUpdated(this);
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
                if (_followFramedInitialSet && FollowTarget is not null)
                {
                    _followFramedInitialSet = false;
                    DeadZoneChanged += _onDeadZoneChanged;
                }
            }
            else
            {
                if (IsConnected(SignalName.DeadZoneChanged, Callable.From(_onDeadZoneChanged)))
                    DeadZoneChanged -= _onDeadZoneChanged;
            }

            if (value == FollowMode.NONE)
                _shouldFollow = false;

            else if (value == FollowMode.GROUP && (FollowTargets is not null || FollowTarget is not null))
                _shouldFollow = true;

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
            if (_followTarget is not null && _followTarget.Equals(value))
                return;
            _followTarget = value;

            _followTargetPhysicsBased = false;
            if (IsInstanceValid(value))
            {
                _shouldFollow = true;
                _checkPhysicsBody(value);
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

                    _checkPhysicsBody(target);

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

    public Node3D _lookAtTarget = null;
    /// <summary>
    /// Determines which target should be looked at.
    /// The [param PhantomCamera3D] will update its rotational value as the
    /// target changes its position.
    /// </summary>
    [Export]
    public Node3D LookAtTarget
    {
        get => _lookAtTarget;
        set
        {
            _lookAtTarget = value;
            _checkPhysicsBody(value);
            EmitSignal(SignalName.LookAtTargetChanged);
            if (IsInstanceValid(_lookAtTarget))
                _shouldLookAt = true;
            else
                _shouldLookAt = false;

            NotifyPropertyListChanged();
        }
    }

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
            if (_lookAtTargets == value)
                return;
            _lookAtTargets = value;

            if (_lookAtTargets.Length == 0)
            {
                _shouldLookAt = false;
                _hasMultipleLookAtTargets = false;
            }
            else
            {
                int validInstances = 0;
                foreach (Node3D target in _lookAtTargets)
                {
                    if (IsInstanceValid(target))
                    {
                        ++validInstances;
                        _shouldLookAt = true;
                        _checkPhysicsBody(target);
                    }

                    if (validInstances > 1)
                        _hasMultipleLookAtTargets = true;
                    else if (validInstances == 0)
                    {
                        _shouldLookAt = false;
                        _hasMultipleLookAtTargets = false;
                    }
                }
            }

            NotifyPropertyListChanged();
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
    public TweenResource TweenResource = new();

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

    public EaseType TweenEase
    {
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
    public bool TweenOnLoad { get; set; } = true;

    private bool _tweenSkip = false;
    public bool TweenSkip
    {
        get => _tweenSkip;
        set => _tweenSkip = value;
    }

    /// <summary>
    /// Determines how often an inactive [param PhantomCamera3D] should update
    /// its positional and rotational values. This is meant to reduce the amount
    /// of calculations inactive [param PhantomCamera3Ds] are doing when idling
    /// to improve performance.
    /// </summary>
    [Export]
    public InactiveUpdateMode inactiveUpdateMode { get; set; } = InactiveUpdateMode.ALWAYS;

    #region Camera3DResouce property getters

    /// <summary>
    /// A resource type that allows for overriding the [param Camera3D] node's properties.
    /// </summary>
    [Export]
    public Camera3DResource camera3DResource { get; set; } = new();

    public uint CullMask
    {
        get => camera3DResource.CullMask;
        set
        {
            camera3DResource.CullMask = value;
            if (IsActive)
                PcamHostOwner.camera3D.CullMask = value;
        }
    }

    public float H_Offset
    {
        get => camera3DResource.H_Offset;
        set
        {
            camera3DResource.H_Offset = value;
            if (IsActive)
                PcamHostOwner.camera3D.HOffset = value;
        }
    }

    public float V_Offset
    {
        get => camera3DResource.V_Offset;
        set
        {
            camera3DResource.V_Offset = value;
            if (IsActive)
                PcamHostOwner.camera3D.VOffset = value;
        }
    }

    public ProjectionType Projection
    {
        get => camera3DResource.Projection;
        set
        {
            camera3DResource.Projection = value;
            //if(IsActive)
            //PcamHostOwner.camera3D.Projection = value;
        }
    }

    public float FOV
    {
        get => camera3DResource.FOV;
        set
        {
            camera3DResource.FOV = value;
            if (IsActive)
                PcamHostOwner.camera3D.Fov = value;
        }
    }

    public float Size
    {
        get => camera3DResource.Size;
        set
        {
            camera3DResource.Size = value;
            if (IsActive)
                PcamHostOwner.camera3D.Size = value;
        }
    }

    public Vector2 FrustumOffset
    {
        get => camera3DResource.FrustumOffset;
        set
        {
            camera3DResource.FrustumOffset = value;
            if (IsActive)
                PcamHostOwner.camera3D.FrustumOffset = value;
        }
    }

    public float Far
    {
        get => camera3DResource.Far;
        set
        {
            camera3DResource.Far = value;
            if (IsActive)
                PcamHostOwner.camera3D.Far = value;
        }
    }

    public float Near
    {
        get => camera3DResource.Near;
        set
        {
            camera3DResource.Near = value;
            if (IsActive)
                PcamHostOwner.camera3D.Near = value;
        }
    }

    #endregion

    /// <summary>
    /// Overrides the [member Camera3D.environment] resource property.
    /// </summary>
    [Export]
    public Godot.Environment Environment { get; set; } = null;

    /// <summary>
    /// Overrides the [member Camera3D.attribuets] resource property.
    /// </summary>
    [Export]
    public CameraAttributes Attributes { get; set; } = null;

    /// <summary>
    /// Offsets the [member follow_target] position.
    /// </summary>
    [ExportGroup("Follow Parameters")]
    [Export]
    public Vector3 FollowOffset { get; set; } = Vector3.Zero;

    private bool _followDamping = false;
    /// <summary>
    /// Applies a damping effect on the camera's movement.
    /// Leading to heavier / slower camera movement as the targeted node moves around.
    /// This is useful to avoid sharp and rapid camera movement.
    /// </summary>
    [Export]
    public bool FollowDamping
    {
        get => _followDamping;
        set
        {
            _followDamping = value;
            NotifyPropertyListChanged();
        }
    }

    private Vector3 _followDampingValue = new(.1f, .1f, .1f);
    /// <summary>
    /// Defines the damping amount. The ideal range should be somewhere between 0-1.<br/>
    /// The damping amount can be specified in the individual axis.<br/>
    /// <b>Lower value</b> = faster / sharper camera movement.<br/>
    /// <b>Higher value</b> = slower / heavier camera movement.
    /// </summary>
    [Export]
    public Vector3 FollowDampingValue
    {
        get => _followDampingValue;
        set
        {
            var theValue = value;
            theValue.X = value.X < 0 ? 0 : value.X;
            theValue.Y = value.Y < 0 ? 0 : value.Y;
            theValue.Z = value.Z < 0 ? 0 : value.Z;

            _followDampingValue = theValue;
        }
    }

    /// <summary>
    /// Sets a distance offset from the centre of the target's position.
    /// The distance is applied to the [param PhantomCamera3D]'s local z axis.
    /// </summary>
    [Export]
    public float FollowDistance { get; set; } = 1f;

    private bool _autoFollowDistance = false;
    /// <summary>
    /// Enables the [param PhantomCamera3D] to automatically distance
    /// itself as the [param follow targets] move further apart.<br/>
    /// It looks at the longest axis between the different targets and interpolates
    /// the distance length between the [member auto_follow_distance_min] and
    /// [member follow_group_distance] properties.<br/>
    /// <b>Note:</b> Enabling this property hides and disables the [member follow_distance]
    /// property as this effectively overrides that property.
    /// </summary>
    [Export]
    public bool AutoFollowDistance
    {
        get => _autoFollowDistance;
        set
        {
            _autoFollowDistance = value;
            NotifyPropertyListChanged();
        }
    }

    /// <summary>
    /// Sets the minimum distance between the Camera and centre of [AABB].<br/>
    /// <b>Note:</b> This distance will only ever be reached when all the targets are in
    /// the exact same [param Vector3] coordinate, which will very unlikely
    /// happen, so adjust the value here accordingly.
    /// </summary>
    [Export]
    public float AutoFollowDistanceMin { get; set; } = 1f;

    /// <summary>
    /// Sets the maximum distance between the Camera and centre of [AABB].
    /// </summary>
    [Export]
    public float AutoFollowDistanceMax { get; set; } = 5f;

    /// <summary>
    /// Determines how fast the [member auto_follow_distance] moves between the
    /// maximum and minimum distance. The higher the value, the sooner the
    /// maximum distance is reached.<br/>
    /// This value should be based on the sizes of the [member auto_follow_distance_min]
    /// and [member auto_follow_distance_max].<br/>
    /// E.g. if the value between the [member auto_follow_distance_min] and
    /// [member auto_follow_distance_max] is small, consider keeping the number low
    /// and vice versa.
    /// </summary>
    [Export]
    public float AutoFollowDistanceDivisor { get; set; } = 10f;


    private SpringArm3D _followSpringArm = null;

    public Vector3 ThirdPersonRotation
    {
        get
        {
            if (_followSpringArm is not null && IsInstanceValid(_followSpringArm))
                return _followSpringArm.Rotation;
            return Vector3.Zero;
        }
        set
        {
            if (_followSpringArm is not null && IsInstanceValid(_followSpringArm))
                _followSpringArm.Rotation = value;
        }
    }

    public Vector3 ThirdPersonRotationDegrees
    {
        get
        {
            if (_followSpringArm is not null && IsInstanceValid(_followSpringArm))
                return _followSpringArm.RotationDegrees;
            return Vector3.Zero;
        }
        set
        {
            if (_followSpringArm is not null && IsInstanceValid(_followSpringArm))
                _followSpringArm.RotationDegrees = value;
        }
    }

    public Quaternion ThirdPersonQuaternion
    {
        get
        {
            if (_followSpringArm is not null && IsInstanceValid(_followSpringArm))
                return _followSpringArm.Quaternion;
            return Quaternion.Identity;
        }
        set
        {
            if (_followSpringArm is not null && IsInstanceValid(_followSpringArm))
                _followSpringArm.Quaternion = value;
        }
    }

    private float _deadZoneWidth = 0f;
    /// <summary>
    /// Defines the horizontal dead zone area. While the target is within it, the
    /// [param PhantomCamera3D] will not move in the horizontal axis.
    /// If the targeted node leaves the horizontal bounds, the
    /// [param PhantomCamera3D] will follow the target horizontally to keep
    /// it within bounds.
    /// </summary>
    [ExportSubgroup("Dead Zones")]
    [Export(PropertyHint.Range, "0,1")]
    public float DeadZoneWidth
    {
        get => _deadZoneWidth;
        set
        {
            _deadZoneWidth = value;
            EmitSignal(SignalName.DeadZoneChanged);
        }
    }

    private float _deadZoneheight = 0f;
    /// <summary>
    /// Defines the vertical dead zone area. While the target is within it, the
    /// [param PhantomCamera3D] will not move in the vertical axis.
    /// If the targeted node leaves the vertical bounds, the
    /// [param PhantomCamera3D] will follow the target horizontally to keep
    /// it within bounds.
    /// </summary>
    [ExportSubgroup("Dead Zones")]
    [Export(PropertyHint.Range, "0,1")]
    public float DeadZoneHeight
    {
        get => _deadZoneheight;
        set
        {
            _deadZoneheight = value;
            EmitSignal(SignalName.DeadZoneChanged);
        }
    }

    /// <summary>
    /// Defines the position of the [member follow_target] within the viewport.<br/>
    /// This is only used for when [member follow_mode] is set to [param Framed].
    /// </summary>
    public Vector2 ViewportPosition { get; set; }

    /// <summary>
    /// Defines the [member SpringArm3D.spring_length].
    /// </summary>
    [ExportSubgroup("Spring Arm")]
    [Export]
    public float SpringLength
    {
        get => FollowDistance;
        set
        {
            FollowDistance = value;
            if (_followSpringArm != null && IsInstanceValid(_followSpringArm))
                _followSpringArm.SpringLength = FollowDistance;
        }
    }

    private uint _collisionMask = 1;
    /// <summary>
    /// Defines the [member SpringArm3D.collision_mask] node's Collision Mask.
    /// </summary>
    [Export(PropertyHint.Layers3DPhysics)]
    public uint CollisionMask
    {
        get => _collisionMask;
        set
        {
            _collisionMask = value;
            if (_followSpringArm != null && IsInstanceValid(_followSpringArm))
                _followSpringArm.CollisionMask = _collisionMask;
        }
    }

    private Shape3D _shape = null;
    /// <summary>
    /// Defines the [member SpringArm3D.shape] node's Shape3D.
    /// </summary>
    [Export]
    public Shape3D Shape
    {
        get => _shape;
        set
        {
            _shape = value;
            if (_followSpringArm != null && IsInstanceValid(_followSpringArm))
                _followSpringArm.Shape = _shape;
        }
    }

    private float _margin = 0.01f;
    /// <summary>
    /// Defines the [member SpringArm3D.shape] node's Shape3D.
    /// </summary>
    [Export]
    public float Margin
    {
        get => _margin;
        set
        {
            _margin = value;
            if (_followSpringArm != null && IsInstanceValid(_followSpringArm))
                _followSpringArm.Margin = _margin;
        }
    }

    /// <summary>
    /// Offsets the target's [param Vector3] position that the
    /// [param PhantomCamera3D] is looking at.
    /// </summary>
    [ExportGroup("Look At Parameters")]
    [Export]
    public Vector3 LookAtOffset { get; set; } = Vector3.Zero;

    private bool _lookAtDamping = false;
    /// <summary>
    /// Applies a damping effect on the camera's rotation.
    /// Leading to heavier / slower camera movement as the targeted node moves around.
    /// This is useful to avoid sharp and rapid camera rotation.
    /// </summary>
    [Export]
    public bool LookAtDamping
    {
        get => _lookAtDamping;
        set
        {
            _lookAtDamping = value;
            NotifyPropertyListChanged();
        }
    }

    /// <summary>
    /// Defines the Rotational damping amount. The ideal range is typicall somewhere between 0-1.<br/>
    /// The damping amount can be specified in the individual axis.<br/>
    /// <b>Lower value</b> = faster / sharper camera rotation.<br/>
    /// <b>Higher value</b> = slower / heavier camera rotation.
    /// </summary>
    [Export(PropertyHint.Range, "0.0,1.0,0.001,or_greater")]
    public float LookAtDampingValue { get; set; } = .25f;

    /// <summary>
    /// Enables the dead zones to be visible when running the game from the editor.
    /// Dead zones will never be visible in build exports.
    /// </summary>
    [Export]
    public bool ShowViewFinderInPlay = false;

    #region Private Fields
    private bool _shouldFollow = false;
    private bool _followTargetPhysicsBased = false;
    private bool _physicsInterpolationEnabled = false;

    private bool _shouldLookAt = false;
    private bool _hasMultipleFollowTargets = false;
    private bool _hasMultipleLookAtTargets = false;

    private Vector3 _followVelocityRef = Vector3.Zero;

    private bool _followFramedInitialSet = false;
    private Vector3 _followFramedOffset = Vector3.Zero;

    private Vector3 _currentRotation = Vector3.Zero;

    private PhantomCameraManager _phantomCameraManager = null;
    #endregion
    #endregion

#if TOOLS
    #region Property Validator

    public override void _ValidateProperty(Dictionary property)
    {
        #region Follow Target

        if (property["name"].AsStringName() == "FollowTarget")
            if (followMode is FollowMode.NONE or FollowMode.GROUP)
                property["usage"] = (int)PropertyUsageFlags.NoEditor;

        if (property["name"].AsStringName() == "FollowPath" && followMode is not FollowMode.PATH)
            property["usage"] = (int)PropertyUsageFlags.NoEditor;

        #endregion

        #region Follow Parameters

        if (followMode == FollowMode.NONE)
            switch (property["name"].AsStringName())
            {
                case "FollowOffset":
                case "FollowDamping":
                case "FollowDampingValue":
                    property["usage"] = (int)PropertyUsageFlags.NoEditor;
                    break;
            }

        if (property["name"].AsStringName() == "FollowOffset")
            if (followMode is FollowMode.PATH or FollowMode.GLUED)
                property["usage"] = (int)PropertyUsageFlags.NoEditor;

        if (property["name"].AsStringName() == "FollowDampingValue" && !FollowDamping)
            property["usage"] = (int)PropertyUsageFlags.NoEditor;

        if (property["name"].AsStringName() == "FollowDistance")
            if (followMode is not FollowMode.FRAMED)
                if (followMode is not FollowMode.GROUP || AutoFollowDistance)
                    property["usage"] = (int)PropertyUsageFlags.NoEditor;

        #endregion

        #region Group Follow

        if (property["name"].AsStringName() == "FollowTargets" && followMode != FollowMode.GROUP)
            property["usage"] = (int)PropertyUsageFlags.NoEditor;

        if (property["name"].AsStringName() == "AutoFollowDistance" && followMode != FollowMode.GROUP)
            property["usage"] = (int)PropertyUsageFlags.NoEditor;

        if (!AutoFollowDistance)
            switch (property["name"].AsStringName())
            {
                case "AutoFollowDistanceMin":
                case "AutoFollowDistanceMax":
                case "AutoFollowDistanceDivisor":
                    property["usage"] = (int)PropertyUsageFlags.NoEditor;
                    break;
            }

        #endregion

        #region Framed Follow

        if (followMode is not FollowMode.FRAMED)
            switch (property["name"].AsStringName())
            {
                case "DeadZoneWidth":
                case "DeadZoneHeight":
                case "ShowViewFinderInPlay":
                    property["usage"] = (int)PropertyUsageFlags.NoEditor;
                    break;
            }

        #endregion

        #region Third Person Follow

        if (followMode is not FollowMode.THIRD_PERSON)
            switch (property["name"].AsStringName())
            {
                case "SpringLength":
                case "CollisionMask":
                case "Shape":
                case "Margin":
                    property["usage"] = (int)PropertyUsageFlags.NoEditor;
                    break;
            }

        #endregion

        #region Look At

        if (lookAtMode is LookAtMode.NONE)
            switch (property["name"].AsStringName())
            {
                case "LookAtTarget":
                case "LookAtOffset":
                case "LookAtDamping":
                case "LookAtDampingValue":
                    property["usage"] = (int)PropertyUsageFlags.NoEditor;
                    break;
            }
        else if (lookAtMode is LookAtMode.GROUP)
            if (property["name"].AsStringName() == "LookAtTarget")
                property["usage"] = (int)PropertyUsageFlags.NoEditor;

        if (property["name"].AsStringName() == "LookAtTarget")
            if (lookAtMode is LookAtMode.NONE or LookAtMode.GROUP)
                property["usage"] = (int)PropertyUsageFlags.NoEditor;

        if (property["name"].AsStringName() == "LookAtTargets" && lookAtMode is not LookAtMode.GROUP)
            property["usage"] = (int)PropertyUsageFlags.NoEditor;

        if (property["name"].AsStringName() == "LookAtDampingValue" && !LookAtDamping)
            property["usage"] = (int)PropertyUsageFlags.NoEditor;

        #endregion

        NotifyPropertyListChanged();

    }

    #endregion
#endif

    #region Override Methods

    public override void _EnterTree()
    {
        _phantomCameraManager = GetTree().Root.GetNode<PhantomCameraManager>(PhantomCameraConstants.PCAM_MANAGER_NODE_NAME);

        _phantomCameraManager.PcamAdded(this);

        if (_phantomCameraManager.PhantomCameraHosts.Length > 0)
            PcamHostOwner = _phantomCameraManager.PhantomCameraHosts[0];


        VisibilityChanged += _checkVisibility;

    }

    public override void _ExitTree()
    {
        _phantomCameraManager.PcamRemoved(this);

        if (_hasValidPcamOwner())
            PcamHostOwner.PcamRemovedFromScene(this);

        if (IsConnected(SignalName.VisibilityChanged, Callable.From(_checkVisibility)))
            VisibilityChanged -= _checkVisibility;

    }

    public override void _Ready()
    {

        if (Engine.IsEditorHint())
            return;

        if (followMode == FollowMode.THIRD_PERSON)
        {
            if (!IsInstanceValid(_followSpringArm))
            {
                _followSpringArm = new();
                _followSpringArm.TopLevel = true;
                _followSpringArm.Rotation = GlobalRotation;
                _followSpringArm.Position = IsInstanceValid(FollowTarget) ? _getTargetPositionOffset() : GlobalPosition;
                _followSpringArm.SpringLength = SpringLength;
                _followSpringArm.CollisionMask = CollisionMask;
                _followSpringArm.Shape = Shape;
                _followSpringArm.Margin = Margin;
                GetParent().CallDeferred("add_child", _followSpringArm);
                CallDeferred("reparent", _followSpringArm);
            }
        }
        else if (followMode == FollowMode.FRAMED)
        {
            _followFramedOffset = GlobalPosition - _getTargetPositionOffset();
            _currentRotation = GlobalRotation;
        }
    }

    public override void _Process(double delta)
    {
        if (!_followTargetPhysicsBased)
            _processLogic(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_followTargetPhysicsBased)
            _processLogic(delta);
    }

    #endregion

    #region Public Methods

    #endregion

    #region Private Methods

    private void _processLogic(double delta)
    {
        if (!IsActive)
            switch (inactiveUpdateMode)
            {
                case InactiveUpdateMode.NEVER:
                    return;
                    // InactiveUpdateMode.EXPONENTIALLY:
                    // TODO : Trigger positional updates less frequently as more PCams gets added
            }

        if (_shouldFollow)
        {
            _followLogic(delta);
        }

        if (_shouldLookAt)
        {
            _lookatLogic();
        }
    }

    private void _followLogic(double delta)
    {
        if (followMode != FollowMode.GROUP)
            if (FollowTarget.IsQueuedForDeletion())
            {
                FollowTarget = null;
                return;
            }
        _followProcess(delta);
    }

    private void _lookatLogic()
    {
        if (LookAtTarget.IsQueuedForDeletion())
        {
            LookAtTarget = null;
            return;
        }
        _lookatProcess(); // TODO : Delta needs to be applied, pending Godot's 3D Physics Interpolation to be implemented
    }

    private void _followProcess(double delta)
    {
        Vector3 followPosition = Vector3.Zero;

        Node3D followTargetNode = this;

        switch (followMode)
        {
            case FollowMode.GLUED:
                if (FollowTarget is not null)
                    followPosition = FollowTarget.GlobalPosition;
                break;
            case FollowMode.SIMPLE:
                if (FollowTarget is not null)
                    followPosition = _getTargetPositionOffset();
                break;
            case FollowMode.GROUP:
                if (FollowTargets is not null)
                {
                    if (FollowTargets.Length == 1)
                        followPosition = FollowTargets[0].GlobalPosition + FollowOffset + Transform.Basis.Z * new Vector3(FollowDistance, FollowDistance, FollowDistance);
                    else if (FollowTargets.Length > 1)
                    {
                        Aabb bounds = new(FollowTargets[0].GlobalPosition, Vector3.Zero);

                        foreach (Node3D node in FollowTargets)
                        {
                            if (IsInstanceValid(node))
                                bounds = bounds.Expand(node.GlobalPosition);
                        }

                        float distance = FollowDistance;

                        if (AutoFollowDistance)
                        {
                            distance = Mathf.Lerp(AutoFollowDistanceMin, AutoFollowDistanceMax, bounds.GetLongestAxisSize() / AutoFollowDistanceDivisor);
                            distance = Mathf.Clamp(distance, AutoFollowDistanceMin, AutoFollowDistanceMax);
                        }

                        followPosition = bounds.GetCenter() + FollowOffset + Transform.Basis.Z * new Vector3(distance, distance, distance);

                    }
                }
                break;
            case FollowMode.PATH:
                if (FollowTarget is not null && FollowPath is not null)
                {
                    Vector3 pathPosition = FollowPath.GlobalPosition;
                    followPosition = FollowPath.Curve.GetClosestPoint(followTargetNode.GlobalPosition - pathPosition) + pathPosition;
                }
                break;
            case FollowMode.FRAMED:
                if (FollowTarget is null)
                    break;
                if (!Engine.IsEditorHint())
                {
                    if (!IsActive || PcamHostOwner.GetTriggerPcamTween())
                    {
                        followPosition = _getPositionOffsetDistance();
                        _interpolatePosition(followPosition, delta);
                        return;
                    }

                    ViewportPosition = GetViewport().GetCamera3D().UnprojectPosition(_getTargetPositionOffset());
                    Vector2 visibleRectSize = GetViewport().GetVisibleRect().Size;

                    ViewportPosition /= visibleRectSize;
                    _currentRotation = GlobalRotation;

                    if (_currentRotation != GlobalRotation)
                        followPosition = _getPositionOffsetDistance();

                    if (_getFramedSideOffset() != Vector2.Zero)
                    {
                        Vector3 targetPosition = _getTargetPositionOffset() + _followFramedOffset;
                        Vector3 gloPos = _getPositionOffsetDistance();

                        if (DeadZoneWidth == 0 || DeadZoneHeight == 0)
                        {
                            if (DeadZoneWidth == 0 && DeadZoneHeight != 0)
                            {
                                gloPos.Z = targetPosition.Z;
                                followPosition = gloPos;
                            }
                            else if (DeadZoneWidth != 0 && DeadZoneHeight == 0)
                            {
                                gloPos.X = targetPosition.X;
                                followPosition = gloPos;
                            }
                            else
                                followPosition = _getPositionOffsetDistance();
                        }
                        else
                        {
                            if (_currentRotation != GlobalRotation)
                            {
                                float opposite = Mathf.Sin(-GlobalRotation.X) * FollowDistance + _getTargetPositionOffset().Y;
                                gloPos.Y = _getTargetPositionOffset().Y + opposite;
                                gloPos.Z = Mathf.Sqrt(Mathf.Pow(FollowDistance, 2) - Mathf.Pow(opposite, 2)) + _getTargetPositionOffset().Z;
                                gloPos.X = GlobalPosition.X;

                                followPosition = gloPos;
                                _currentRotation = GlobalRotation;
                            }
                            else
                                followPosition = targetPosition;
                        }
                    }
                    else
                    {
                        _followFramedOffset = GlobalPosition - _getTargetPositionOffset();
                        _currentRotation = GlobalRotation;
                        return;
                    }

                }
                else
                {

                    if (GetViewport().GetCamera3D() is not null)
                    {
                        followPosition = _getPositionOffsetDistance();
                        Vector2 unprojectedPosition = _getRawUnprojectedPosition();
                        float viewportWidth = GetViewport().GetVisibleRect().Size.X;
                        float viewportHeight = GetViewport().GetVisibleRect().Size.Y;
                        Camera3D.KeepAspectEnum cameraAspect = GetViewport().GetCamera3D().KeepAspect;
                        Vector2 visibleRectSize = GetViewport().GetViewport().GetVisibleRect().Size;

                        unprojectedPosition -= visibleRectSize / 2;
                        if (cameraAspect is Camera3D.KeepAspectEnum.Height)
                        {
                            //Landscape View
                            float aspectRatioScale = viewportWidth / viewportHeight;
                            unprojectedPosition.X = (unprojectedPosition.X / aspectRatioScale + 1) / 2;
                            unprojectedPosition.Y = (unprojectedPosition.Y + 1) / 2;
                        }
                        else
                        {
                            //Portrait View
                            float aspectRatioScale = viewportHeight / viewportWidth;
                            unprojectedPosition.X = (unprojectedPosition.X + 1) / 2;
                            unprojectedPosition.Y = (unprojectedPosition.Y / aspectRatioScale + 1) / 2;
                        }

                        ViewportPosition = unprojectedPosition;
                    }
                }
                break;
            case FollowMode.THIRD_PERSON:
                if (FollowTarget is not null)
                    if (!Engine.IsEditorHint())
                    {
                        if (IsInstanceValid(FollowTarget) && IsInstanceValid(_followSpringArm))
                        {
                            followPosition = _getTargetPositionOffset();
                            followTargetNode = _followSpringArm;
                        }
                    }
                    else
                        followPosition = _getPositionOffsetDistance();
                break;
        }

        _interpolatePosition(followPosition, delta, followTargetNode);

    }

    private void _lookatProcess()
    {
        switch (lookAtMode)
        {
            case LookAtMode.MIMIC:
                if (LookAtTarget is not null)
                    GlobalRotation = LookAtTarget.GlobalRotation;
                break;
            case LookAtMode.SIMPLE:
                _interpolateRotation(LookAtTarget.GlobalPosition);
                break;
            case LookAtMode.GROUP:
                if (!_hasMultipleFollowTargets)
                {
                    if (LookAtTargets.Length == 0)
                        return;
                    _interpolateRotation(LookAtTargets[0].GlobalPosition);
                }
                else
                {
                    Aabb bounds = new(LookAtTargets[0].GlobalPosition, Vector3.Zero);
                    foreach (Node3D node in LookAtTargets)
                    {
                        bounds = bounds.Expand(node.GlobalPosition);
                    }
                    _interpolateRotation(bounds.GetCenter());
                }
                break;
        }
    }

    private Vector3 _getTargetPositionOffset()
    {
        return FollowTarget.GlobalPosition + FollowOffset;
    }

    private Vector3 _getPositionOffsetDistance()
    {
        return _getTargetPositionOffset() + Transform.Basis.Z + new Vector3(FollowDistance, FollowDistance, FollowDistance);
    }

    private void _setFollowVelocity(int index, float value)
    {
        _followVelocityRef[index] = value;
    }

    private void _interpolatePosition(Vector3 targetPosition, double delta, Node3D cameraTarget = null)
    {
        if (cameraTarget is null)
            cameraTarget = this;
        if (FollowDamping)
        {
            Vector3 position = cameraTarget.GlobalPosition;
            for (int index = 0; index < 3; index++)
            {
                position[index] = _smoothDamp(
                    targetPosition[index],
                    cameraTarget.GlobalPosition[index],
                    index,
                    _followVelocityRef[index],
                    new(this, MethodName._setFollowVelocity),
                    FollowDampingValue[index]
                );
            }
            cameraTarget.GlobalPosition = position;
        }
        else
            cameraTarget.GlobalPosition = targetPosition;
    }

    private void _interpolateRotation(Vector3 targetTrans)
    {
        Vector3 direction = (targetTrans - GlobalPosition + LookAtOffset).Normalized();
        Basis targetBasis = Basis.LookingAt(direction);
        Quaternion targetQuat = targetBasis.GetRotationQuaternion().Normalized();

        if (LookAtDamping)
        {
            Quaternion currentQuat = Quaternion.Normalized();

            float dampingTime = Mathf.Max(.0001f, LookAtDampingValue);
            float t = (float)Mathf.Min(1f, GetProcessDeltaTime() / dampingTime);

            float dot = currentQuat.Dot(targetQuat);

            if (dot < .0f)
            {
                targetQuat = -targetQuat;
                dot = -dot;
            }

            dot = Mathf.Clamp(dot, -1f, 1f);

            float theta = Mathf.Acos(dot) * t;

            float sinTheta = Mathf.Sin(theta);
            float sinThetaTotal = Mathf.Sin(Mathf.Acos(dot));

            // Stop interpolating once sin_theta_total reaches a very low value or 0
            if (sinThetaTotal < .00001f)
                return;

            float ratioA = Mathf.Cos(theta) - dot * sinTheta / sinThetaTotal;
            float ratioB = sinTheta / sinThetaTotal;

            Quaternion = currentQuat * ratioA + targetQuat * ratioB;

        }
        else
            Quaternion = targetQuat;
    }

    private float _smoothDamp(float targetAxis, float selfAxis, int index, float currentVelocity, Callable setVelocity, float dampingTime, bool rot = false)
    {
        dampingTime = Mathf.Max(.0001f, dampingTime);
        float omega = 2 / dampingTime;
        float delta = (float)GetProcessDeltaTime();
        float x = omega * delta;
        float exponential = 1 / (1 + x + .48f * x * x + .235f * x * x * x);
        float diff = selfAxis - targetAxis;
        float _target_Axis = targetAxis;

        float maxChange = Mathf.Inf * dampingTime;
        diff = Mathf.Clamp(diff, -maxChange, maxChange);
        targetAxis = selfAxis - diff;

        float temp = (currentVelocity + omega * diff) * delta;
        setVelocity.Call(index, (currentVelocity - omega * temp) * exponential);
        float output = targetAxis + (diff + temp) * exponential;

        //To prevent overshooting
        if ((_target_Axis - selfAxis > .0f) == (output > _target_Axis))
        {
            output = _target_Axis;
            setVelocity.Call(index, (output - _target_Axis) / delta);
        }

        return output;
    }

    private Vector2 _getRawUnprojectedPosition()
    {
        return GetViewport().GetCamera3D().UnprojectPosition(FollowTarget.GlobalPosition + FollowOffset);
    }

    private void _onDeadZoneChanged()
    {
        GlobalPosition = _getPositionOffsetDistance();
    }

    private Vector2 _getFramedSideOffset()
    {

        Vector2 frameOutBounds = Vector2.Zero;

        if (ViewportPosition.X < .5f - DeadZoneWidth / 2)
            // Is Outside left edge
            frameOutBounds.X = -1;

        if (ViewportPosition.Y < .5f - DeadZoneHeight / 2)
            // Is Outside top edge
            frameOutBounds.Y = 1;

        if (ViewportPosition.X > .5f + DeadZoneWidth / 2)
            // Is Outside right edge
            frameOutBounds.X = 1;

        if (ViewportPosition.Y < .5001f + DeadZoneHeight / 2) // 0.501 to resolve an issue where the bottom vertical Dead Zone never becoming 0 when the Dead Zone Vertical parameter is set to 0
            // Is Outside bottom edge
            frameOutBounds.Y = -1;

        return frameOutBounds;

    }

    private int _setLayer(int currentLayers, int layerNumber, bool value)
    {

        int mask = currentLayers;

        // From https://github.com/godotengine/godot/blob/51991e20143a39e9ef0107163eaf283ca0a761ea/scene/3d/camera_3d.cpp#L638

        if (layerNumber < 1 || layerNumber > 20)
            GD.PrintErr("Render layer must be between 1 and 20.");
        else
        {
            if (value)
                mask |= 1 << (layerNumber - 1);
            else
                mask &= ~(1 << (layerNumber - 1));
        }

        return mask;
    }

    private bool _hasValidPcamOwner()
    {
        if (PcamHostOwner == null)
            return false;
        if (!IsInstanceValid(PcamHostOwner))
            return false;
        if (!IsInstanceValid(PcamHostOwner.camera3D))
            return false;
        return true;
    }

    private void _checkVisibility()
    {
        if (!IsInstanceValid(PcamHostOwner))
            return;
        PcamHostOwner.RefreshPcamListPriority();
    }

    private void _checkPhysicsBody(Node3D target)
    {
        if (target is not PhysicsBody3D)
            return;

        // TODO: Enable once Godot supports 3D Physics Interpolation
    }

    #endregion

}
