using Godot;
using System;
using System.Collections.Generic;

namespace PhantomCamera;

using ThreeDimension;
using TwoDimension;

public enum InterpolationMode
{
    AUTO = 0,
    IDLE = 1,
    PHYSICS = 2,
}

// TODO: Create a PhantomCameraHost Abstract class and create two other hosts for 2D and 3D from it;

/// <summary>
/// Controls a scene's [Camera2D] (2D scenes) and [Camera3D] (3D scenes).
/// All instantiated [param PhantomCameras] in a scene are assign to and managed by a
/// PhantomCameraHost. It is what determines which [param PhantomCamera] should
/// be active.
/// </summary>
[Tool]
[Icon("res://addons/phantom_camera/icons/phantom_camera_host.svg")]
public partial class PhantomCameraHost : Node
{

    #region Signals

    /// <summary>
    /// Updates the viewfinder [param dead zones] sizes.<br/>
    /// <b>Note:</b> This is only being used in the editor viewfinder UI.
    /// </summary>
    [Signal]
    public delegate void UpdateEditorViewFinderEventHandler();

    #endregion

    #region Variables

    /// <summary>
    /// For 2D scenes, is the [Camera2D] instance the [param PhantomCameraHost] controls.
    /// </summary>
    public Camera2D camera2D { get; private set; } = null;
    /// <summary>
    /// For 3D scenes, is the [Camera3D] instance the [param PhantomCameraHost] controls.
    /// </summary>
    public Camera3D camera3D { get; private set; } = null;

    private List<Node> _pcam_list = new();

    private PhantomCamera2D _active_pcam_2d = null;
    private PhantomCamera3D _active_pcam_3d = null;

    private int _active_pcam_priority = -1;
    private bool _active_pcam_missing = true;
    private bool _active_pcam_has_damping = false;
    private bool _follow_target_physics_based = false;

    private Transform2D _prev_active_pcam_2D_transform = new();
    private Transform3D _prev_active_pcam_3D_transform = new();

    private bool _trigger_pcam_tween = false;
    private float _tween_elapsed_time = 0f;
    private float _tween_duration = 0f;
    private float _tween_transition = 0f;
    private int _tween_ease = 2;

    private bool _multiple_pcam_host = false;

    private bool _is_child_of_camera = false;
    private bool _is_2D = false;

    private Control _viewfinder_node = null;
    private bool _viewfinder_needed_check = true;

    private Vector2 _camera_zoom = Vector2.One;

    #region Camera3DResource

    private CameraAttributes _prev_cam_attributes = null;
    private int _cam_attribute_type = 0; // 0 = CameraAttributesPractical, 1 = CameraAttributesPhysical
    private bool _cam_attribute_changed = false;
    private bool _cam_attribute_assigned = false;

    // CameraAttributes Base
    private float _prev_cam_auto_exposure_scale = 0.4f;
    private bool _cam_auto_exposure_scale_changed = false;

    private float _prev_cam_auto_exposure_speed = 0.5f;
    private bool _cam_auto_exposure_speed_changed = false;

    private float _prev_cam_exposure_multiplier = 1f;
    private bool _cam_exposure_multiplier_changed = false;

    private float _prev_cam_exposure_sensitivity = 100f;
    private bool _cam_exposure_sensitivity_changed = false;

    #region  CameraAttributesPractical

    private float _prev_cam_exposure_min_sensitivity = 0f;
    private bool _cam_exposure_min_sensitivity_changed = false;

    private float _prev_cam_exposure_max_sensitivity = 800f;
    private bool _cam_exposure_max_sensitivity_changed = false;

    private float _prev_cam_dof_blur_amount = .1f;
    private bool _cam_dof_blur_amount_changed = false;

    private readonly float _cam_dof_blur_far_distance_default = 10f;
    private float _prev_cam_dof_blur_far_distance = 10f;
    private bool _cam_dof_blur_far_distance_changed = false;

    private readonly float _cam_dof_blur_far_transition_default = 5f;
    private float _prev_cam_dof_blur_far_transition = 5f;
    private bool _cam_dof_blur_far_transition_changed = false;

    private readonly float _cam_dof_blur_near_distance_default = 2f;
    private float _prev_cam_dof_blur_near_distance = 2f;
    private bool _cam_dof_blur_near_distance_changed = false;

    private readonly float _cam_dof_blur_near_transition_default = 1f;
    private float _prev_cam_dof_blur_near_transition = 1f;
    private bool _cam_dof_blur_near_transition_changed = false;

    #endregion

    #region CameraAttributesPhysical

    private float _prev_cam_exposure_min_exposure_value = 10f;
    private bool _cam_exposure_min_exposure_value_changed = false;

    private float _prev_cam_exposure_max_exposure_value = -8f;
    private bool _cam_exposure_max_exposure_value_changed = false;

    private float _prev_cam_exposure_aperture = 16f;
    private bool _cam_exposure_aperture_changed = false;

    private float _prev_cam_exposure_shutter_speed = 100f;
    private bool _cam_exposure_shutter_speed_changed = false;

    private float _prev_cam_frustum_far = 4000f;
    private bool _cam_frustum_far_changed = false;

    private float _prev_cam_frustum_focal_length = 35f;
    private bool _cam_frustum_focal_length_changed = false;

    private float _prev_cam_frustum_near = 0.05f;
    private bool _cam_frustum_near_changed = false;

    private float _prev_cam_frustum_focus_distance = 10f;
    private bool _cam_frustum_focus_distance_changed = false;

    #endregion

    private float _prev_cam_h_offset = 0f;
    private bool _cam_h_offset_changed = false;

    private float _prev_cam_v_offset = 0f;
    private bool _cam_v_offset_changed = false;

    private float _prev_cam_fov = 75f;
    private bool _cam_fov_changed = false;

    private float _prev_cam_size = 1f;
    private bool _cam_size_changed = false;

    private Vector2 _prev_cam_frustum_offset = Vector2.Zero;
    private bool _cam_frustum_offset_changed = false;

    private float _prev_cam_near = 0.05f;
    private bool _cam_near_changed = false;

    private float _prev_cam_far = 4000f;
    private bool _cam_far_changed = false;

    #endregion

    private Transform2D _active_pcam_2d_glob_transform = new();
    private Transform3D _active_pcam_3d_glob_transform = new();

    #endregion

    // NOTE: Temp solution until Godot has better plugin autoload recognition out-of-the-box.
    private PhantomCameraManager _phantom_camera_manager;

    #region Override Methods

    public override string[] _GetConfigurationWarnings()
    {
        Node parent = GetParent();

        if (_is_2D)
        {
            if (parent is not Camera2D)
                return ["Needs to be a child of a Camera2D in order to work."];
            else
                return [];
        }
        else
        {
            if (parent is not Camera3D)
                return ["Needs to be a child of a Camera3D in order to work."];
            else
                return [];
        }

    }

    public override void _EnterTree()
    {
        _phantom_camera_manager = GetTree().Root.GetNode<PhantomCameraManager>(PhantomCameraConstants.PCAM_MANAGER_NODE_NAME);

        Node parent = GetParent();

        if (parent is not Camera2D or Camera3D)
            return;

        _is_child_of_camera = true;

        if (parent is Camera2D parent2D)
        {
            _is_2D = true;
            camera2D = parent2D;
            // Force applies position smoothing to be disabled
            // This is to prevent overlap with the interpolation of the PCam2D.
            camera2D.PositionSmoothingEnabled = false;
        }
        else if (parent is Camera3D parent3D)
        {
            _is_2D = false;
            camera3D = parent3D;

            // Clears existing resource on Camera3D to prevent potentially messing with external Attribute resource
            if (camera3D.Attributes != null && !Engine.IsEditorHint())
                camera3D.Attributes = null;
        }

        _phantom_camera_manager.PcamHostAdded(this);

        _checkCameraHostAmount();

        if (_multiple_pcam_host)
        {
            GD.PrintErr(
                "Only one PhantomCameraHost can exist in a scene \n Multiple PhantomCameraHosts will be supported in https://github.com/ramokz/phantom-camera/issues/26");
            QueueFree();
        }

        if (_is_2D)
        {
            PhantomCamera2D[] PhantomCamera2Ds = _phantom_camera_manager.PhantomCamera2Ds;
            if (PhantomCamera2Ds.Length > 0)
                foreach (PhantomCamera2D pcam2D in PhantomCamera2Ds)
                {
                    PcamAddedToScene(pcam2D);
                    //pcam2D.SetPcamHostOwner(this);
                }
        }
        else
        {
            PhantomCamera3D[] PhantomCamera3Ds = _phantom_camera_manager.PhantomCamera3Ds;
            if (PhantomCamera3Ds.Length > 0)
                foreach (PhantomCamera3D pcam3D in PhantomCamera3Ds)
                {
                    PcamAddedToScene(pcam3D);
                    pcam3D.PcamHostOwner = this;
                }
        }

    }

    public override void _ExitTree()
    {
        _phantom_camera_manager.PcamHostRemoved(this);
        _checkCameraHostAmount();
    }

    public override void _Ready()
    {
        if (!(IsInstanceValid(_active_pcam_2d) || IsInstanceValid(_active_pcam_3d)))
            return;

        if (_is_2D)
            _active_pcam_2d_glob_transform = _active_pcam_2d.GlobalTransform;
        else
            _active_pcam_3d_glob_transform = _active_pcam_3d.GlobalTransform;
    }

    public override void _Process(double delta)
    {
        if (_follow_target_physics_based || _active_pcam_missing)
            return;
        _tweenFollowChecker(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_follow_target_physics_based || _active_pcam_missing)
            return;
        _tweenFollowChecker(delta);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Called when a [param PhantomCamera] is added to the scene.<br/>
    /// <b>Note:</b> This can only be called internally from a
    /// [param PhantomCamera] node.
    /// </summary>
    /// <param name="pcam"></param>
    public void PcamAddedToScene(Node pcam)
    {
        if (pcam is not PhantomCamera2D or PhantomCamera3D)
        {
            GD.PrintErr("This function should only be called from PhantomCamera scripts");
            return;
        }

        if (!_pcam_list.Contains(pcam))
        {
            _pcam_list.Add(pcam);
            // if (pcam is PhantomCamera2D pcam2D && !pcam2D.TweeOnLoad)
            //     pcam2D.setTweenSkip(this, true);

            /*else*/
            if (pcam is PhantomCamera3D pcam3D && !pcam3D.TweenOnLoad)
                pcam3D.TweenSkip = true;

            _findPcamWithHighestPriority();
        }

    }

    /// <summary>
    /// Called when a [param PhantomCamera] is removed from the scene.<br/>
    /// <b>Note:</b> This can only be called internally from a
    /// [param PhantomCamera] node.
    /// </summary>
    /// <param name="pcam"></param>
    public void PcamRemovedFromScene(Node pcam)
    {
        if (pcam is not PhantomCamera2D or PhantomCamera3D)
        {
            GD.PrintErr("This function should only be called from PhantomCamera scripts");
            return;
        }

        if (_pcam_list.Contains(pcam))
        {
            _pcam_list.Remove(pcam);


            if (_is_2D)
            {
                // TODO: Complete The 2D Part.
            }
            else
            {
                if (pcam is PhantomCamera3D pcam3D && pcam3D == _active_pcam_3d)
                {
                    _active_pcam_missing = true;
                    _active_pcam_priority = -1;
                    _findPcamWithHighestPriority();
                }
            }
        }
    }

    /// <summary>
    /// Triggers a recalculation to determine which PhantomCamera has the highest priority.
    /// </summary>
    /// <param name="pcam"></param>
    public void PcamPriorityUpdated(Node pcam)
    {
#if TOOLS
        if (Engine.IsEditorHint())
        {
            if (_is_2D)
            {
                // if(_active_pcam_2d.PriorityOverride)
                // return;
            }
            else
            {
                if (_active_pcam_3d.PriorityOverride)
                    return;
            }
        }
#endif

        if (!IsInstanceValid(pcam))
            return;

        int currentPcamPriority = 0;

        if (pcam is PhantomCamera2D pcam2D)
        {
            //currentPcamPriority = pcam2D.Priority;
        }
        else if (pcam is PhantomCamera3D pcam3D)
        {
            currentPcamPriority = pcam3D.Priority;
        }

        if (currentPcamPriority >= _active_pcam_priority)
        {
            if (_is_2D)
            {
                // TODO: Complete The 2D Part
            }
            else
            {
                if (pcam != _active_pcam_3d)
                    _assignNewActivePcam(pcam);
            }
        }

        if (pcam == _active_pcam_2d || pcam == _active_pcam_3d)
        {
            if (currentPcamPriority <= _active_pcam_priority)
            {
                _active_pcam_priority = currentPcamPriority;
                _findPcamWithHighestPriority();
            }
            else
                _active_pcam_priority = currentPcamPriority;
        }
    }

#if TOOLS
    /// <summary>
    /// Updates the viewfinder when a [param PhantomCamera] has its
    /// [param priority_ovrride] enabled.<br/>
    /// <b>Note:</b> This only affects the editor.
    /// </summary>
    /// <param name="pcam"></param>
    public void PcamPriorityOverride(Node pcam)
    {
#if TOOLS
        if (Engine.IsEditorHint())
        {
            if (_is_2D)
            {
                // if (_active_pcam_2d.PriorityOverride)
                //     _active_pcam_2d.PriorityOverride = false;
            }
            else
            {
                if (_active_pcam_3d.PriorityOverride)
                    _active_pcam_3d.PriorityOverride = false;
            }
        }
#endif
        _assignNewActivePcam(pcam);
        EmitSignal(SignalName.UpdateEditorViewFinder);
    }

    /// <summary>
    /// Updates the viewfinder when a [param PhantomCamera] has its
    /// [param priority_ovrride] disabled.<br/>
    /// <b>Note:</b> This only affects the editor.
    /// </summary>
    public void PcamPriorityOverrideDisabled()
    {
        EmitSignal(SignalName.UpdateEditorViewFinder);
    }
#endif

    /// <summary>
    /// Returns the currently active [param PhantomCamera]
    /// </summary>
    public Node GetActivePcame()
    {
        if (_is_2D)
            return _active_pcam_2d;
        else
            return _active_pcam_3d;
    }

    /// <summary>
    /// Returns whether if a [param PhantomCamera] should tween when it becomes
    /// active. If it's already active, the value will always be false.<br/>
    /// <b>Note:</b> This can only be called internally from a
    /// [param PhantomCamera] node.
    /// </summary>
    /// <returns></returns>
    public bool GetTriggerPcamTween()
    {
        return _trigger_pcam_tween;
    }

    /// <summary>
    /// Refreshes the [param PhantomCamera] list and checks for the highest priority.<br/>
    /// <b>Note:</b> This should <b>not</b> be necessary to call manually.
    /// </summary>
    public void RefreshPcamListPriority()
    {
        _active_pcam_priority = -1;
        _findPcamWithHighestPriority();
    }

    #endregion

    #region Private Methods

    private void _checkCameraHostAmount()
    {
        _multiple_pcam_host = _phantom_camera_manager.PhantomCameraHosts.Length > 1 ? true : false;
    }

    private void _assignNewActivePcam(Node pcam)
    {
        bool noPreviousPcam = false;

        if (IsInstanceValid(_active_pcam_2d) || IsInstanceValid(_active_pcam_3d))
        {
            if (_is_2D)
            {
                _prev_active_pcam_2D_transform = camera2D.GlobalTransform;
                _active_pcam_2d.QueueRedraw();
                // _active_pcam_2d.IsActive = false;
                //_active_pcam_2d.EmitSignal(SignalName.BecomeInactive);

                // if (_trigger_pcam_tween)
                //     _active_pcam_2d.EmitSignal(SignalName.TweenInterrupted, pcam);
            }
            else
            {
                _prev_active_pcam_3D_transform = camera3D.GlobalTransform;

                if (camera3D.Attributes is not null)
                {
                    CameraAttributes attributes = camera3D.Attributes;

                    _prev_cam_exposure_multiplier = attributes.ExposureMultiplier;
                    _prev_cam_auto_exposure_scale = attributes.AutoExposureScale;
                    _prev_cam_auto_exposure_speed = attributes.AutoExposureSpeed;

                    if (attributes is CameraAttributesPractical attributesPractical)
                    {
                        _prev_cam_dof_blur_amount = attributesPractical.DofBlurAmount;

                        if (attributesPractical.DofBlurFarEnabled)
                        {
                            _prev_cam_dof_blur_far_distance = attributesPractical.DofBlurFarDistance;
                            _prev_cam_dof_blur_far_transition = attributesPractical.DofBlurFarTransition;
                        }
                        else
                        {
                            _prev_cam_dof_blur_far_distance = _cam_dof_blur_far_distance_default;
                            _prev_cam_dof_blur_far_transition = _cam_dof_blur_far_transition_default;
                        }

                        if (attributesPractical.DofBlurNearEnabled)
                        {
                            _prev_cam_dof_blur_near_distance = attributesPractical.DofBlurNearDistance;
                            _prev_cam_dof_blur_near_transition = attributesPractical.DofBlurNearTransition;
                        }
                        else
                        {
                            _prev_cam_dof_blur_near_distance = _cam_dof_blur_near_distance_default;
                            _prev_cam_dof_blur_near_transition = _cam_dof_blur_near_transition_default;
                        }

                        if (attributesPractical.AutoExposureEnabled)
                        {
                            _prev_cam_exposure_max_sensitivity = attributesPractical.AutoExposureMaxSensitivity;
                            _prev_cam_exposure_min_sensitivity = attributesPractical.AutoExposureMinSensitivity;
                        }
                    }
                    else if (attributes is CameraAttributesPhysical attributesPhysical)
                    {
                        _prev_cam_frustum_focus_distance = attributesPhysical.FrustumFocusDistance;
                        _prev_cam_frustum_focal_length = attributesPhysical.FrustumFocalLength;
                        _prev_cam_frustum_far = attributesPhysical.FrustumFar;
                        _prev_cam_frustum_near = attributesPhysical.FrustumNear;
                        _prev_cam_exposure_aperture = attributesPhysical.ExposureAperture;
                        _prev_cam_exposure_shutter_speed = attributesPhysical.ExposureShutterSpeed;

                        if (attributesPhysical.AutoExposureEnabled)
                        {
                            _prev_cam_exposure_min_exposure_value = attributesPhysical.AutoExposureMinExposureValue;
                            _prev_cam_exposure_max_exposure_value = attributesPhysical.AutoExposureMaxExposureValue;
                        }
                    }

                }

                _prev_cam_h_offset = camera3D.HOffset;
                _prev_cam_v_offset = camera3D.VOffset;
                _prev_cam_fov = camera3D.Fov;
                _prev_cam_size = camera3D.Size;
                _prev_cam_frustum_offset = camera3D.FrustumOffset;
                _prev_cam_near = camera3D.Near;
                _prev_cam_far = camera3D.Far;

                _active_pcam_3d.IsActive = false;
                _active_pcam_3d.EmitSignal("BecameInactive");

                if (_trigger_pcam_tween)
                    _active_pcam_3d.EmitSignal("TweenInterrupted", pcam);

            }
        }
        else
            noPreviousPcam = true;

        // Assign newly active pcam
        if (_is_2D)
        {
            _active_pcam_2d = pcam as PhantomCamera2D;
            // _active_pcam_priority = _active_pcam_2d.Priority;
            // _active_pcam_has_damping = _active_pcam_2d.FollowDamping;
            // _tween_duration = _active_pcam_2d.TweenDuration;
        }
        else
        {
            _active_pcam_3d = pcam as PhantomCamera3D;
            _active_pcam_priority = _active_pcam_3d.Priority;
            _active_pcam_has_damping = _active_pcam_3d.FollowDamping;
            _tween_duration = _active_pcam_3d.TweenDuration;

            // Checks if the Camera3DResource has changed from the previous active PCam3D

            if (_active_pcam_3d.camera3DResource is not null)
            {
                if (_prev_cam_h_offset != _active_pcam_3d.H_Offset)
                    _cam_h_offset_changed = true;
                if (_prev_cam_v_offset != _active_pcam_3d.V_Offset)
                    _cam_v_offset_changed = true;
                if (_prev_cam_fov != _active_pcam_3d.FOV)
                    _cam_fov_changed = true;
                if (_prev_cam_size != _active_pcam_3d.Size)
                    _cam_size_changed = true;
                if (_prev_cam_frustum_offset != _active_pcam_3d.FrustumOffset)
                    _cam_frustum_offset_changed = true;
                if (_prev_cam_near != _active_pcam_3d.Near)
                    _cam_near_changed = true;
                if (_prev_cam_far != _active_pcam_3d.Far)
                    _cam_far_changed = true;
            }

            if (_active_pcam_3d.Attributes is null)
                _cam_attribute_changed = false;
            else
            {
                if (_prev_cam_attributes != _active_pcam_3d.Attributes)
                {
                    _prev_cam_attributes = _active_pcam_3d.Attributes;
                    _cam_attribute_changed = true;

                    CameraAttributes attributes = _active_pcam_3d.Attributes;

                    if (_prev_cam_auto_exposure_scale != attributes.AutoExposureScale)
                        _cam_auto_exposure_scale_changed = true;
                    if (_prev_cam_auto_exposure_speed != attributes.AutoExposureSpeed)
                        _cam_auto_exposure_speed_changed = true;
                    if (_prev_cam_exposure_multiplier != attributes.ExposureMultiplier)
                        _cam_exposure_multiplier_changed = true;
                    if (_prev_cam_exposure_sensitivity != attributes.ExposureSensitivity)
                        _cam_exposure_sensitivity_changed = true;

                    if (attributes is CameraAttributesPractical attributesPractical)
                    {
                        _cam_attribute_type = 0;

                        if (camera3D.Attributes is null)
                        {
                            camera3D.Attributes = new CameraAttributesPractical();
                            camera3D.Attributes = _active_pcam_3d.Attributes.Duplicate() as CameraAttributesPractical;
                            _cam_attribute_assigned = true;
                        }

                        if (_prev_cam_exposure_min_sensitivity != attributesPractical.AutoExposureMinSensitivity)
                            _cam_exposure_min_sensitivity_changed = true;
                        if (_prev_cam_exposure_max_sensitivity != attributesPractical.AutoExposureMaxSensitivity)
                            _cam_exposure_max_sensitivity_changed = true;

                        if (_prev_cam_dof_blur_amount != attributesPractical.DofBlurAmount)
                            _cam_dof_blur_amount_changed = true;

                        CameraAttributesPractical camAttributesPractical = camera3D.Attributes as CameraAttributesPractical;

                        if (_prev_cam_dof_blur_far_distance != attributesPractical.DofBlurFarDistance)
                        {
                            _cam_dof_blur_far_distance_changed = true;
                            camAttributesPractical.DofBlurFarEnabled = true;
                        }
                        if (_prev_cam_dof_blur_far_transition != attributesPractical.DofBlurFarTransition)
                        {
                            _cam_dof_blur_far_transition_changed = true;
                            camAttributesPractical.DofBlurFarEnabled = true;
                        }

                        if (_prev_cam_dof_blur_near_distance != attributesPractical.DofBlurNearDistance)
                        {
                            _cam_dof_blur_near_distance_changed = true;
                            camAttributesPractical.DofBlurNearEnabled = true;
                        }
                        if (_prev_cam_dof_blur_near_transition != attributesPractical.DofBlurNearTransition)
                        {
                            _cam_dof_blur_near_transition_changed = true;
                            camAttributesPractical.DofBlurNearEnabled = true;
                        }

                        camera3D.Attributes = camAttributesPractical;

                    }
                    else if (attributes is CameraAttributesPhysical attributesPhysical)
                    {
                        _cam_attribute_type = 1;

                        if (camera3D.Attributes is null)
                        {
                            camera3D.Attributes = new CameraAttributesPhysical();
                            camera3D.Attributes = _active_pcam_3d.Attributes.Duplicate() as CameraAttributesPhysical;
                        }

                        if (_prev_cam_exposure_min_exposure_value != attributesPhysical.AutoExposureMinExposureValue)
                            _cam_exposure_min_exposure_value_changed = true;
                        if (_prev_cam_exposure_max_exposure_value != attributesPhysical.AutoExposureMaxExposureValue)
                            _cam_exposure_max_exposure_value_changed = true;

                        if (_prev_cam_exposure_aperture != attributesPhysical.ExposureAperture)
                            _cam_exposure_aperture_changed = true;
                        if (_prev_cam_exposure_shutter_speed != attributesPhysical.ExposureShutterSpeed)
                            _cam_exposure_shutter_speed_changed = true;

                        if (_prev_cam_frustum_far != attributesPhysical.FrustumFar)
                            _cam_frustum_far_changed = true;

                        if (_prev_cam_frustum_focal_length != attributesPhysical.FrustumFocalLength)
                            _cam_frustum_focal_length_changed = true;

                        if (_prev_cam_frustum_focus_distance != attributesPhysical.FrustumFocusDistance)
                            _cam_frustum_focus_distance_changed = true;

                        if (_prev_cam_frustum_near != attributesPhysical.FrustumNear)
                            _cam_frustum_near_changed = true;

                    }
                }
            }
        }

        if (_is_2D)
        {
            // if(_active_pcam_2d.ShowViewfinderInPlay)
            //     _viewfinder_needed_check = true;

            // TODO : Complete the 2D part

        }
        else
        {
            _follow_target_physics_based = false;
            if (_active_pcam_3d.ShowViewFinderInPlay)
                _viewfinder_needed_check = true;

            _active_pcam_3d.IsActive = true;
            _active_pcam_3d.EmitSignal("BecameActive");
            if (_active_pcam_3d.camera3DResource is not null)
            {
                camera3D.CullMask = _active_pcam_3d.CullMask;
                camera3D.Projection = (Camera3D.ProjectionType)_active_pcam_3d.Projection;
            }
        }

        if (noPreviousPcam)
        {
            if (_is_2D)
                _prev_active_pcam_2D_transform = _active_pcam_2d.GlobalTransform;
            else
                _prev_active_pcam_3D_transform = _active_pcam_3d.GlobalTransform;
        }

        if (pcam is PhantomCamera2D pcam2D)
        {
            // if (pcam2D.TweenSkip)
            //     _tween_elapsed_time = pcam2D.TweenDuration;
            // else
            //     _tween_elapsed_time = 0;
        }
        else if (pcam is PhantomCamera3D pcam3D)
        {
            if (pcam3D.TweenSkip)
                _tween_elapsed_time = pcam3D.TweenDuration;
            else
                _tween_elapsed_time = 0;
        }

        _trigger_pcam_tween = true;

    }

    private void _findPcamWithHighestPriority()
    {
        foreach (Node pcam in _pcam_list)
        {
            if (pcam is PhantomCamera2D pcam2D)
            {
                if (!pcam2D.Visible)
                    continue;
                // if(pcam2D.GetPriority() > _active_pcam_priority){
                //     _assignNewActivePcam(pcam);
                // }
                // pcam2D.SetTweenSkip(this, false);
                _active_pcam_missing = false;
            }
            else if (pcam is PhantomCamera3D pcam3D)
            {
                if (!pcam3D.Visible)
                    continue;
                if (pcam3D.Priority > _active_pcam_priority)
                {
                    _assignNewActivePcam(pcam);
                }
                pcam3D.TweenSkip = false;
                _active_pcam_missing = false;
            }
        }
    }

    private void _tweenFollowChecker(double delta)
    {
        if (_is_2D)
            _active_pcam_2d_glob_transform = _active_pcam_2d.GlobalTransform;
        else
            _active_pcam_3d_glob_transform = _active_pcam_3d.GlobalTransform;

        if (_trigger_pcam_tween)
            _pcamTween(delta);

        else
            _pcamFollow(delta);
    }

    private void _pcamFollow(double delta)
    {
        if (_is_2D)
        {
            if (!IsInstanceValid(_active_pcam_2d))
                return;
        }
        else
        {
            if (!IsInstanceValid(_active_pcam_3d))
                return;
        }

        if (_active_pcam_missing || !_is_child_of_camera)
            return;

        // When following

        if (_is_2D)
        {
            // TODO: Complete the 2D Part.
        }
        else
            camera3D.GlobalTransform = _active_pcam_3d_glob_transform;

#if TOOLS
        if (_viewfinder_needed_check)
        {
            _showViewfinderInPlay();
            _viewfinder_needed_check = false;
        }
#endif
        // TODO: Should be able to find a more efficient way using signals
        if (Engine.IsEditorHint())
        {
            if (!_is_2D)
            {
                if (_active_pcam_3d.camera3DResource is not null)
                {
                    camera3D.CullMask = _active_pcam_3d.CullMask;
                    camera3D.HOffset = _active_pcam_3d.H_Offset;
                    camera3D.VOffset = _active_pcam_3d.V_Offset;
                    camera3D.Projection = (Camera3D.ProjectionType)_active_pcam_3d.Projection;
                    camera3D.Fov = _active_pcam_3d.FOV;
                    camera3D.Size = _active_pcam_3d.Size;
                    camera3D.FrustumOffset = _active_pcam_3d.FrustumOffset;
                    camera3D.Near = _active_pcam_3d.Near;
                    camera3D.Far = _active_pcam_3d.Far;
                }

                if (_active_pcam_3d.Attributes != null)
                    camera3D.Attributes = _active_pcam_3d.Attributes.Duplicate() as CameraAttributes;

                if (_active_pcam_3d.Environment != null)
                    camera3D.Environment = _active_pcam_3d.Environment.Duplicate() as Godot.Environment;
            }
        }

    }

    private void _pcamTween(double delta)
    {
        // Run at the first tween frame
        if (_tween_elapsed_time == 0)
        {
            if (_is_2D)
            {
                // _active_pcam_2d.EmitSignal("TweenStarted");
                // _active_pcam_2d.ResetLimit();
            }
            else
                _active_pcam_3d.EmitSignal("TweenStarted");
        }

        _tween_elapsed_time = (float)Mathf.Min(_tween_duration, _tween_elapsed_time + delta);

        if (_is_2D)
        {

        }
        else
        {
            _active_pcam_3d.EmitSignal("IsTweening");
            camera3D.GlobalPosition = Tween.InterpolateValue(
                _prev_active_pcam_3D_transform.Origin,
                _active_pcam_3d_glob_transform.Origin - _prev_active_pcam_3D_transform.Origin,
                _tween_elapsed_time,
                _active_pcam_3d.TweenDuration,
                (Tween.TransitionType)_active_pcam_3d.TweenTransition,
                (Tween.EaseType)_active_pcam_3d.TweenEase
            ).AsVector3();

            Quaternion prevActivePcam3DQuat = new(_prev_active_pcam_3D_transform.Basis.Orthonormalized());
            camera3D.Quaternion = Tween.InterpolateValue(
                prevActivePcam3DQuat,
                (prevActivePcam3DQuat.Inverse() * new Quaternion(_active_pcam_3d_glob_transform.Basis.Orthonormalized())) - prevActivePcam3DQuat,
                _tween_elapsed_time,
                _active_pcam_3d.TweenDuration,
                (Tween.TransitionType)_active_pcam_3d.TweenTransition,
                (Tween.EaseType)_active_pcam_3d.TweenEase

            ).AsQuaternion();

            if (_cam_attribute_changed)
            {
                if (_active_pcam_3d.Attributes.AutoExposureEnabled)
                {
                    if (_cam_auto_exposure_scale_changed)
                    {
                        camera3D.Attributes.AutoExposureScale = Tween.InterpolateValue(
                            _prev_cam_auto_exposure_scale,
                            _active_pcam_3d.Attributes.AutoExposureScale - _prev_cam_auto_exposure_scale,
                            _tween_elapsed_time,
                            _active_pcam_3d.TweenDuration,
                            (Tween.TransitionType)_active_pcam_3d.TweenTransition,
                            (Tween.EaseType)_active_pcam_3d.TweenEase
                        ).As<float>();
                    }

                    if (_cam_auto_exposure_speed_changed)
                    {
                        camera3D.Attributes.AutoExposureSpeed = Tween.InterpolateValue(
                            _prev_cam_auto_exposure_scale,
                            _active_pcam_3d.Attributes.AutoExposureSpeed - _prev_cam_auto_exposure_scale,
                            _tween_elapsed_time,
                            _active_pcam_3d.TweenDuration,
                            (Tween.TransitionType)_active_pcam_3d.TweenTransition,
                            (Tween.EaseType)_active_pcam_3d.TweenEase
                        ).As<float>();
                    }
                }
                if (_cam_attribute_type == 0 /*CameraAttributePractical*/)
                {

                    CameraAttributesPractical camAttributesPractical = camera3D.Attributes as CameraAttributesPractical;
                    CameraAttributesPractical activeCamAttributesPractical = _active_pcam_3d.Attributes as CameraAttributesPractical;

                    if (_active_pcam_3d.Attributes.AutoExposureEnabled)
                    {
                        if (_cam_exposure_min_sensitivity_changed)
                        {
                            camAttributesPractical.AutoExposureMinSensitivity = Tween.InterpolateValue(
                                _prev_cam_exposure_min_sensitivity,
                                activeCamAttributesPractical.AutoExposureMinSensitivity - _prev_cam_exposure_min_sensitivity,
                                _tween_elapsed_time,
                                _active_pcam_3d.TweenDuration,
                                (Tween.TransitionType)_active_pcam_3d.TweenTransition,
                                (Tween.EaseType)_active_pcam_3d.TweenEase
                            ).As<float>();
                        }
                        if (_cam_exposure_max_sensitivity_changed)
                        {
                            camAttributesPractical.AutoExposureMaxSensitivity = Tween.InterpolateValue(
                                _prev_cam_exposure_max_sensitivity,
                                activeCamAttributesPractical.AutoExposureMaxSensitivity - _prev_cam_exposure_max_sensitivity,
                                _tween_elapsed_time,
                                _active_pcam_3d.TweenDuration,
                                (Tween.TransitionType)_active_pcam_3d.TweenTransition,
                                (Tween.EaseType)_active_pcam_3d.TweenEase
                            ).As<float>();
                        }
                    }
                    if (_cam_dof_blur_amount_changed)
                    {
                        camAttributesPractical.DofBlurAmount = Tween.InterpolateValue(
                            _prev_cam_dof_blur_amount,
                            activeCamAttributesPractical.DofBlurAmount - _prev_cam_dof_blur_amount,
                            _tween_elapsed_time,
                            _active_pcam_3d.TweenDuration,
                            (Tween.TransitionType)_active_pcam_3d.TweenTransition,
                            (Tween.EaseType)_active_pcam_3d.TweenEase
                        ).As<float>();
                    }
                    if (_cam_dof_blur_far_distance_changed)
                    {
                        camAttributesPractical.DofBlurFarDistance = Tween.InterpolateValue(
                            _prev_cam_dof_blur_far_distance,
                            activeCamAttributesPractical.DofBlurFarDistance - _prev_cam_dof_blur_far_distance,
                            _tween_elapsed_time,
                            _active_pcam_3d.TweenDuration,
                            (Tween.TransitionType)_active_pcam_3d.TweenTransition,
                            (Tween.EaseType)_active_pcam_3d.TweenEase
                        ).As<float>();
                    }
                    if (_cam_dof_blur_far_transition_changed)
                    {
                        camAttributesPractical.DofBlurFarTransition = Tween.InterpolateValue(
                            _prev_cam_dof_blur_far_transition,
                            activeCamAttributesPractical.DofBlurFarTransition - _prev_cam_dof_blur_far_transition,
                            _tween_elapsed_time,
                            _active_pcam_3d.TweenDuration,
                            (Tween.TransitionType)_active_pcam_3d.TweenTransition,
                            (Tween.EaseType)_active_pcam_3d.TweenEase
                        ).As<float>();
                    }
                    if (_cam_dof_blur_near_distance_changed)
                    {
                        camAttributesPractical.DofBlurNearDistance = Tween.InterpolateValue(
                            _prev_cam_dof_blur_near_distance,
                            activeCamAttributesPractical.DofBlurNearDistance - _prev_cam_dof_blur_near_distance,
                            _tween_elapsed_time,
                            _active_pcam_3d.TweenDuration,
                            (Tween.TransitionType)_active_pcam_3d.TweenTransition,
                            (Tween.EaseType)_active_pcam_3d.TweenEase
                        ).As<float>();
                    }
                    if (_cam_dof_blur_near_transition_changed)
                    {
                        camAttributesPractical.DofBlurNearTransition = Tween.InterpolateValue(
                            _prev_cam_dof_blur_near_transition,
                            activeCamAttributesPractical.DofBlurNearTransition - _prev_cam_dof_blur_near_transition,
                            _tween_elapsed_time,
                            _active_pcam_3d.TweenDuration,
                            (Tween.TransitionType)_active_pcam_3d.TweenTransition,
                            (Tween.EaseType)_active_pcam_3d.TweenEase
                        ).As<float>();
                    }

                    camera3D.Attributes = camAttributesPractical;

                }
                else if (_cam_attribute_type == 1 /*CameraAttributePhysical*/)
                {

                    CameraAttributesPhysical camAttributesPhysical = camera3D.Attributes as CameraAttributesPhysical;
                    CameraAttributesPhysical activeCamAttributesPhysical = _active_pcam_3d.Attributes as CameraAttributesPhysical;

                    if (_cam_dof_blur_near_transition_changed)
                    {
                        camAttributesPhysical.AutoExposureMaxExposureValue = Tween.InterpolateValue(
                            _prev_cam_exposure_max_exposure_value,
                            activeCamAttributesPhysical.AutoExposureMaxExposureValue - _prev_cam_exposure_max_exposure_value,
                            _tween_elapsed_time,
                            _active_pcam_3d.TweenDuration,
                            (Tween.TransitionType)_active_pcam_3d.TweenTransition,
                            (Tween.EaseType)_active_pcam_3d.TweenEase
                        ).As<float>();
                    }
                    if (_cam_exposure_min_exposure_value_changed)
                    {
                        camAttributesPhysical.AutoExposureMinExposureValue = Tween.InterpolateValue(
                            _prev_cam_exposure_min_exposure_value,
                            activeCamAttributesPhysical.AutoExposureMinExposureValue - _prev_cam_exposure_min_exposure_value,
                            _tween_elapsed_time,
                            _active_pcam_3d.TweenDuration,
                            (Tween.TransitionType)_active_pcam_3d.TweenTransition,
                            (Tween.EaseType)_active_pcam_3d.TweenEase
                        ).As<float>();
                    }
                    if (_cam_exposure_aperture_changed)
                    {
                        camAttributesPhysical.ExposureAperture = Tween.InterpolateValue(
                            _prev_cam_exposure_aperture,
                            activeCamAttributesPhysical.ExposureAperture - _prev_cam_exposure_aperture,
                            _tween_elapsed_time,
                            _active_pcam_3d.TweenDuration,
                            (Tween.TransitionType)_active_pcam_3d.TweenTransition,
                            (Tween.EaseType)_active_pcam_3d.TweenEase
                        ).As<float>();
                    }
                    if (_cam_exposure_shutter_speed_changed)
                    {
                        camAttributesPhysical.ExposureShutterSpeed = Tween.InterpolateValue(
                            _prev_cam_exposure_shutter_speed,
                            activeCamAttributesPhysical.ExposureShutterSpeed - _prev_cam_exposure_shutter_speed,
                            _tween_elapsed_time,
                            _active_pcam_3d.TweenDuration,
                            (Tween.TransitionType)_active_pcam_3d.TweenTransition,
                            (Tween.EaseType)_active_pcam_3d.TweenEase
                        ).As<float>();
                    }
                    if (_cam_frustum_far_changed)
                    {
                        camAttributesPhysical.FrustumFar = Tween.InterpolateValue(
                            _prev_cam_frustum_far,
                            activeCamAttributesPhysical.FrustumFar - _prev_cam_frustum_far,
                            _tween_elapsed_time,
                            _active_pcam_3d.TweenDuration,
                            (Tween.TransitionType)_active_pcam_3d.TweenTransition,
                            (Tween.EaseType)_active_pcam_3d.TweenEase
                        ).As<float>();
                    }
                    if (_cam_frustum_near_changed)
                    {
                        camAttributesPhysical.FrustumNear = Tween.InterpolateValue(
                            _prev_cam_frustum_near,
                            activeCamAttributesPhysical.FrustumNear - _prev_cam_frustum_near,
                            _tween_elapsed_time,
                            _active_pcam_3d.TweenDuration,
                            (Tween.TransitionType)_active_pcam_3d.TweenTransition,
                            (Tween.EaseType)_active_pcam_3d.TweenEase
                        ).As<float>();
                    }
                    if (_cam_frustum_focal_length_changed)
                    {
                        camAttributesPhysical.FrustumFocalLength = Tween.InterpolateValue(
                            _prev_cam_frustum_focal_length,
                            activeCamAttributesPhysical.FrustumFocalLength - _prev_cam_frustum_focal_length,
                            _tween_elapsed_time,
                            _active_pcam_3d.TweenDuration,
                            (Tween.TransitionType)_active_pcam_3d.TweenTransition,
                            (Tween.EaseType)_active_pcam_3d.TweenEase
                        ).As<float>();
                    }
                    if (_cam_frustum_focus_distance_changed)
                    {
                        camAttributesPhysical.FrustumFocusDistance = Tween.InterpolateValue(
                            _prev_cam_frustum_focus_distance,
                            activeCamAttributesPhysical.FrustumFocusDistance - _prev_cam_frustum_focus_distance,
                            _tween_elapsed_time,
                            _active_pcam_3d.TweenDuration,
                            (Tween.TransitionType)_active_pcam_3d.TweenTransition,
                            (Tween.EaseType)_active_pcam_3d.TweenEase
                        ).As<float>();
                    }

                    camera3D.Attributes = camAttributesPhysical;
                }
            }

            if (_cam_h_offset_changed)
            {
                camera3D.HOffset = Tween.InterpolateValue(
                    _prev_cam_h_offset,
                    _active_pcam_3d.H_Offset - _prev_cam_h_offset,
                    _tween_elapsed_time,
                    _active_pcam_3d.TweenDuration,
                    (Tween.TransitionType)_active_pcam_3d.TweenTransition,
                    (Tween.EaseType)_active_pcam_3d.TweenEase
                ).As<float>();
            }
            if (_cam_v_offset_changed)
            {
                camera3D.VOffset = Tween.InterpolateValue(
                    _prev_cam_v_offset,
                    _active_pcam_3d.V_Offset - _prev_cam_v_offset,
                    _tween_elapsed_time,
                    _active_pcam_3d.TweenDuration,
                    (Tween.TransitionType)_active_pcam_3d.TweenTransition,
                    (Tween.EaseType)_active_pcam_3d.TweenEase
                ).As<float>();
            }
            if (_cam_fov_changed)
            {
                camera3D.Fov = Tween.InterpolateValue(
                    _prev_cam_fov,
                    _active_pcam_3d.FOV - _prev_cam_fov,
                    _tween_elapsed_time,
                    _active_pcam_3d.TweenDuration,
                    (Tween.TransitionType)_active_pcam_3d.TweenTransition,
                    (Tween.EaseType)_active_pcam_3d.TweenEase
                ).As<float>();
            }
            if (_cam_size_changed)
            {
                camera3D.Size = Tween.InterpolateValue(
                    _prev_cam_size,
                    _active_pcam_3d.Size - _prev_cam_size,
                    _tween_elapsed_time,
                    _active_pcam_3d.TweenDuration,
                    (Tween.TransitionType)_active_pcam_3d.TweenTransition,
                    (Tween.EaseType)_active_pcam_3d.TweenEase
                ).As<float>();
            }
            if (_cam_frustum_offset_changed)
            {
                camera3D.FrustumOffset = Tween.InterpolateValue(
                    _prev_cam_frustum_offset,
                    _active_pcam_3d.FrustumOffset - _prev_cam_frustum_offset,
                    _tween_elapsed_time,
                    _active_pcam_3d.TweenDuration,
                    (Tween.TransitionType)_active_pcam_3d.TweenTransition,
                    (Tween.EaseType)_active_pcam_3d.TweenEase
                ).As<Vector2>();
            }
            if (_cam_near_changed)
            {
                camera3D.Near = Tween.InterpolateValue(
                    _prev_cam_near,
                    _active_pcam_3d.Near - _prev_cam_near,
                    _tween_elapsed_time,
                    _active_pcam_3d.TweenDuration,
                    (Tween.TransitionType)_active_pcam_3d.TweenTransition,
                    (Tween.EaseType)_active_pcam_3d.TweenEase
                ).As<float>();
            }
            if (_cam_far_changed)
            {
                camera3D.Far = Tween.InterpolateValue(
                    _prev_cam_far,
                    _active_pcam_3d.Far - _prev_cam_far,
                    _tween_elapsed_time,
                    _active_pcam_3d.TweenDuration,
                    (Tween.TransitionType)_active_pcam_3d.TweenTransition,
                    (Tween.EaseType)_active_pcam_3d.TweenEase
                ).As<float>();
            }

        }

        if (_tween_elapsed_time < _tween_duration)
            return;

        _trigger_pcam_tween = false;
        _tween_elapsed_time = 0;
        if (_is_2D)
        {
            // TODO : Complete The 2D Part.
        }
        else
        {
            if (_active_pcam_3d.Attributes != null)
            {
                if (_cam_attribute_type == 0)
                {

                    CameraAttributesPractical camAttributesPractical = camera3D.Attributes as CameraAttributesPractical;
                    CameraAttributesPractical activeCamAttributesPractical = _active_pcam_3d.Attributes as CameraAttributesPractical;

                    if (!activeCamAttributesPractical.DofBlurFarEnabled)
                        camAttributesPractical.DofBlurFarEnabled = false;

                    if (!activeCamAttributesPractical.DofBlurNearEnabled)
                        camAttributesPractical.DofBlurNearEnabled = false;

                    camera3D.Attributes = camAttributesPractical;
                }
            }

            _cam_h_offset_changed = false;
            _cam_v_offset_changed = false;
            _cam_fov_changed = false;
            _cam_size_changed = false;
            _cam_frustum_offset_changed = false;
            _cam_near_changed = false;
            _cam_far_changed = false;
            _cam_attribute_changed = false;

            _active_pcam_3d.EmitSignal("TweenCompleted");

        }

    }

#if TOOLS
    private void _showViewfinderInPlay()
    {
        // Don't show the viewfinder in the actual editor or project builds
        if (Engine.IsEditorHint())
            return;

        // We default the viewfinder node to hidden
        if (IsInstanceValid(_viewfinder_node))
            _viewfinder_node.Visible = false;

        if (_is_2D)
        {

            // TODO: Complete The 2D part.

        }
        else
        {
            if (!_active_pcam_3d.ShowViewFinderInPlay || _active_pcam_3d.followMode != ThreeDimension.FollowMode.FRAMED)
                return;
        }

        CanvasLayer canvasLayer = new();
        GetTree().Root.AddChild(canvasLayer);

        // Instantiate the viewfinder scene if it isn't already
        if (!IsInstanceValid(_viewfinder_node))
        {
            PackedScene viewfinderScene = GD.Load<PackedScene>("res://addons/phantom_camera/panel/viewfinder/viewfinder_panel.tscn");
            _viewfinder_node = viewfinderScene.Instantiate() as Control;
            canvasLayer.AddChild(_viewfinder_node);
        }

        _viewfinder_node.Visible = true;
        //_viewfinder_node.UpdateDeadZone();

    }
#endif

    #endregion

}
