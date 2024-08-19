using Godot;
using Godot.Collections;
using System;

namespace PhantomCamera.Resources;

public enum ProjectionType : byte
{
    PERSPECTIVE = 0, // Perspective projection. Objects on the screen becomes smaller when they are far away.
    ORTHOGONAL = 1, // Orthogonal projection, also known as orthographic projection. Objects remain the same size on the screen no matter how far away they are.
    FRUSTUM = 2, // Frustum projection. This mode allows adjusting frustum_offset to create "tilted frustum" effects.
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

    // Overrides [member Camera3D.cull_mask].
    [Export(PropertyHint.Layers3DRender)]
    public int cull_mask = 1048575;

    //Overrides [member Camera3D.h_offset].
    [Export(PropertyHint.Range, "0,1,0.001,hide_slider,suffix:m")]
    public float h_offset = 0f;
    // Overrides [member Camera3D.v_offset].
    [Export(PropertyHint.Range, "0,1,0.001,hide_slider,suffix:m")]
    public float v_offset = 0f;

    // Overrides [member Camera3D.projection].
    [Export]
    public ProjectionType projection
    {
        get => projection;
        set
        {
            projection = value;
            NotifyPropertyListChanged();
        }
    }

    //Overrides [member Camera3D.fov].
    [Export(PropertyHint.Range, "1,179,0.1,degrees")]
    public float fov = 75f;

    // Overrides [member Camera3D.size].
    [Export(PropertyHint.Range, "0.001,100,0.001,suffix:m,or_greater")]
    public float size = 1f;

    // Overrides [member Camera3d.frustum_offset].
    [Export]
    public Vector2 frustum_offset = Vector2.Zero;

    // Overrides [member Camera3D.near].
    [Export(PropertyHint.Range, "0.001,10,0.001,suffix:m,or_greater")]
    public float near = 0.05f;

    // Overrides [member Camera3D.far].
    [Export(PropertyHint.Range, "0.01,4000,0.001,suffix:m,or_greater")]
    public float far = 4000f;

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
