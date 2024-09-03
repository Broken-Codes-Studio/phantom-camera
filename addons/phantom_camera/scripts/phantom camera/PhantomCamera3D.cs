using Godot;
using Godot.Collections;
using PhantomCamera.Resources;

namespace PhantomCamera.ThreeDimension;

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
            if (_hasValidPcamOwner() && Engine.IsEditorHint())
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


            if (_followTarget is not null && _followTarget.Equals(value))
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
            // if(IsActive)
            //     PcamHostOwner.camera3D.cull_mask = value;
        }
    }

    public float H_Offset
    {
        get => camera3DResource.H_Offset;
        set
        {
            camera3DResource.H_Offset = value;
            // if(IsActive)
            //     PcamHostOwner.camera3D.h_offset = value;
        }
    }

    public float V_Offset
    {
        get => camera3DResource.V_Offset;
        set
        {
            camera3DResource.V_Offset = value;
            // if(IsActive)
            //     PcamHostOwner.camera3D.V_Offset = value;
        }
    }

    public ProjectionType Projection
    {
        get => camera3DResource.Projection;
        set
        {
            camera3DResource.Projection = value;
            // if(IsActive)
            //     PcamHostOwner.camera3D.Projection = value;
        }
    }

    public float FOV
    {
        get => camera3DResource.FOV;
        set
        {
            camera3DResource.FOV = value;
            // if(IsActive)
            //     PcamHostOwner.camera3D.FOV = value;
        }
    }

    public float Size
    {
        get => camera3DResource.Size;
        set
        {
            camera3DResource.Size = value;
            // if(IsActive)
            //     PcamHostOwner.camera3D.Size = value;
        }
    }

    public Vector2 FrustumOffset
    {
        get => camera3DResource.FrustumOffset;
        set
        {
            camera3DResource.FrustumOffset = value;
            // if(IsActive)
            //     PcamHostOwner.camera3D.FrustumOffset = value;
        }
    }

    public float Far
    {
        get => camera3DResource.Far;
        set
        {
            camera3DResource.Far = value;
            // if(IsActive)
            //     PcamHostOwner.camera3D.Far = value;
        }
    }

    public float Near
    {
        get => camera3DResource.Near;
        set
        {
            camera3DResource.Near = value;
            // if(IsActive)
            //     PcamHostOwner.camera3D.Near = value;
        }
    }

    #endregion

    /// <summary>
    /// Overrides the [member Camera3D.environment] resource property.
    /// </summary>
    [Export]
    public Environment Environment { get; set; } = null;

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

    private Node3D[] _validLookAtTargets = { };

    private bool _tweenSkip = false;

    private Vector3 _followVelocityRef = Vector3.Zero;

    private bool _followFramedInitialSet = false;
    private Vector3 _followFramedOffset = Vector3.Zero;

    private SpringArm3D _followSpringArm = null;

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

        // if(_hasValidPcamOwner())
        //     PcamHostOwner.PcamRemovedFromScene(this);

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
                //_followSpringArm.Position = IsInstanceValid(FollowTarget) ? _getTargetPositionOffset() : GlobalPosition;
                _followSpringArm.SpringLength = SpringLength;
                _followSpringArm.CollisionMask = CollisionMask;
                _followSpringArm.Shape = Shape;
                _followSpringArm.Margin = Margin;
                //GetParent().AddChild(_followSpringArm);
                //Reparent(_followSpringArm);
            }
        }
        else if (followMode == FollowMode.FRAMED)
        {
            //_followFramedOffset = GlobalPosition - _getTargetPositionOffset();
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

    #region Private Methods

    private void _processLogic(double delta)
    {

    }

    private void _follow(double delta)
    {

    }

    private void _lookat()
    {

    }

    private bool _hasValidPcamOwner()
    {
        if (PcamHostOwner == null)
            return false;
        if (!IsInstanceValid(PcamHostOwner))
            return false;
        // if(!IsInstanceValid(PcamHostOwner.Camera_3D))
        //     return false;
        return true;
    }

    private void _checkVisibility()
    {
        if (!IsInstanceValid(PcamHostOwner))
            return;
        PcamHostOwner.RefreshPcamListPriority();
    }

    #endregion

}
