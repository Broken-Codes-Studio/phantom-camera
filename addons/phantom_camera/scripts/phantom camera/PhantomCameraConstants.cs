using Godot;
using System;

namespace PhantomCamera;

public static class PhantomCameraConstants
{

    #region Constants

    public static readonly StringName CAMERA_2D_NODE_NAME = "Camera2D";
    public static readonly StringName CAMERA_3D_NODE_NAME = "Camera3D";
    public static readonly StringName PCAM_HOST_NODE_NAME = "PhantomCameraHost";
    public static readonly StringName PCAM_MANAGER_NODE_NAME = "PhantomCameraManager";
    public static readonly StringName PCAM_2D_NODE_NAME = "PhantomCamera2D";
    public static readonly StringName PCAM_3D_NODE_NAME = "PhantomCamera3D";
    public static readonly StringName PCAM_HOST = "phantom_camera_host";

    public static readonly Color COLOR_2D = new("8DA5F3");
    public static readonly Color COLOR_3D = new("FC7F7F");
    public static readonly Color COLOR_PCAM = new("3AB99A");
    public static readonly Color COLOR_PCAM_33 = new("3ab99a33");
    public static readonly Color PCAM_HOST_COLOR = new("E0E0E0");

    #endregion

    #region Group Names

    public static readonly StringName PCAM_GROUP_NAME = "phantom_camera_group";
    public static readonly StringName PCAM_HOST_GROUP_NAME = "phantom_camera_host_group";

    #endregion
}
