using Godot;
using Godot.Collections;
using System;

namespace PhantomCamera.Resources;

public enum ProjectionType : byte
{
    /// <summary>
    /// Perspective projection. Objects on the screen becomes smaller when they are far away.
    /// </summary>
    PERSPECTIVE = 0,
    /// <summary>
    /// Orthogonal projection, also known as orthographic projection. Objects remain the same size on the screen no matter how far away they are.
    /// </summary>
    ORTHOGONAL = 1,
    /// <summary>
    /// Frustum projection. This mode allows adjusting frustum_offset to create "tilted frustum" effects.
    /// </summary>
    FRUSTUM = 2,
}

/// <summary>
/// Resource for [PhantomCamera3D] to override various [Camera3D] properties.
/// The overrides defined here will be applied to the [Camera3D] upon the
/// [PhantomCamera3D] becoming active.
/// </summary>
[Icon("res://addons/phantom_camera/icons/phantom_camera_camera_3d_resource.svg")]
public partial class Camera3DResource : Resource
{

    /*  Resource for [PhantomCamera3D] to override various [Camera3D] properties.

        The overrides defined here will be applied to the [Camera3D] upon the
        [PhantomCamera3D] becoming active.
    */

    /// <summary>
    /// Overrides [member Camera3D.cull_mask].
    /// </summary>
    [Export(PropertyHint.Layers3DRender)]
    public int cull_mask = 1048575;

    /// <summary>
    /// Overrides [member Camera3D.h_offset].
    /// </summary>
    [Export(PropertyHint.Range, "0,1,0.001,hide_slider,suffix:m")]
    public float h_offset = 0f;
    /// <summary>
    /// Overrides [member Camera3D.v_offset].
    /// </summary>
    [Export(PropertyHint.Range, "0,1,0.001,hide_slider,suffix:m")]
    public float v_offset = 0f;

    private ProjectionType _projection = ProjectionType.PERSPECTIVE;
    /// <summary>
    /// Overrides [member Camera3D.projection].
    /// </summary>
    [Export]
    public ProjectionType projection
    {
        get => _projection;
        set
        {
            _projection = value;
            NotifyPropertyListChanged();
        }
    }

    /// <summary>
    /// Overrides [member Camera3D.fov].
    /// </summary>
    [Export(PropertyHint.Range, "1,179,0.1,degrees")]
    public float fov = 75f;

    /// <summary>
    /// Overrides [member Camera3D.size].
    /// </summary>
    [Export(PropertyHint.Range, "0.001,100,0.001,suffix:m,or_greater")]
    public float size = 1f;

    /// <summary>
    /// Overrides [member Camera3d.frustum_offset].
    /// </summary>
    [Export]
    public Vector2 frustum_offset = Vector2.Zero;

    /// <summary>
    /// Overrides [member Camera3D.near].
    /// </summary>
    [Export(PropertyHint.Range, "0.001,10,0.001,suffix:m,or_greater")]
    public float near = 0.05f;

    /// <summary>
    /// Overrides [member Camera3D.far].
    /// </summary>
    [Export(PropertyHint.Range, "0.01,4000,0.001,suffix:m,or_greater")]
    public float far = 4000f;

    public Camera3DResource() { }

    public override void _ValidateProperty(Dictionary property)
    {
        if (property["name"].AsStringName() == "fov" && projection != ProjectionType.PERSPECTIVE)
            property["usage"] = (int)PropertyUsageFlags.NoEditor;

        if (property["name"].AsStringName() == "size" && projection == ProjectionType.PERSPECTIVE)
            property["usage"] = (int)PropertyUsageFlags.NoEditor;

        if (property["name"].AsStringName() == "frustum_offset" && projection != ProjectionType.FRUSTUM)
            property["usage"] = (int)PropertyUsageFlags.NoEditor;
    }

}
