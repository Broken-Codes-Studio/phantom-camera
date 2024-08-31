using Godot;
using PhantomCamera.ThreeDimension;
using PhantomCamera.TwoDimension;
using System;
using System.Collections.Generic;

namespace PhantomCamera;

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

    private PhantomCamera2D _activate_pcam_2d = null;
    private PhantomCamera3D _activate_pcam_3d = null;

    private int _active_pcam_priority = -1;
    private bool _activate_pcam_mission = true;
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

    private float _prev_can_auto_exposure_speed = 0.5f;
    private bool _cam_auto_exposure_speed_changed = false;

    private float _prev_cam_auto_exposure_Multiplier = 1f;
    private bool _cam_exposure_multiplier_changed = false;

    private float _prev_cam_exposure_sensivity = 100f;
    private bool _cam_exposure_sensivity_changed = false;

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

    #region Private Methods

    private void _checkCameraHostAmount()
    {
        _multiple_pcam_host = _phantom_camera_manager.PhantomCameraHosts.Length > 1 ? true : false;
    }

    #endregion

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

        _phantom_camera_manager.PcamAdded(this);

        _checkCameraHostAmount();

    }

    public override void _ExitTree()
    {

    }

    public override void _Ready()
    {

    }

    public override void _Process(double delta)
    {

    }

    public override void _PhysicsProcess(double delta)
    {

    }

    #endregion

}
