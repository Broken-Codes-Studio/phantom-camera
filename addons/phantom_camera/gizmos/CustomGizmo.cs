using Godot;
using Godot.NativeInterop;
using System;

public partial class CustomPluginGizmo : EditorNode3DGizmoPlugin
{

    string _gizmo_name;
    public string gizmo_name
    {
        set => _gizmo_name = value;
    }

    Texture2D _gizmo_icon;
    public Texture2D gizmo_icon
    {
        set => _gizmo_icon = value;
    }

    Script _gizmo_spatial_script;
    public Script gizmo_spatial_script
    {
        set => _gizmo_spatial_script = value;
    }

    float _gizmo_scale = .035f;

    public CustomPluginGizmo()
    {
        HandleMaterials();
    }

    protected void HandleMaterials(){
        CreateIconMaterial(_gizmo_name, _gizmo_icon, false, Colors.White);
        CreateMaterial("main", Color.Color8(252, 127, 127, 255));
    }

    public override string _GetGizmoName()
    {
        return _gizmo_name;
    }

    public override bool _HasGizmo(Node3D spatial)
    {
        return spatial.GetScript().As<Script>() == _gizmo_spatial_script;
    }

    public override void _Redraw(EditorNode3DGizmo gizmo)
    {
        gizmo.Clear();

        Material icon = GetMaterial(_gizmo_name, gizmo);
        gizmo.AddUnscaledBillboard(icon, _gizmo_scale);

        Material material = GetMaterial("main", gizmo);
        gizmo.AddLines(_DrawFrustum(), material);

    }

    private Vector3[] _DrawFrustum()
    {

        Vector3[] lines = new Vector3[16];

        float dis = 0.25f;
        float width = dis * 1.25f;
        float len = dis * 1.5f;

        //Trapezoid
        lines[0] = Vector3.Zero;
        lines[1] = new Vector3(-width, dis, -len);

        lines[2] = Vector3.Zero;
        lines[3] = new Vector3(width, dis, -len);

        lines[4] = Vector3.Zero;
        lines[5] = new Vector3(-width, -dis, -len);

        lines[6] = Vector3.Zero;
        lines[7] = new Vector3(width, -dis, -len);

        #region Square

        //Left
        lines[8] = new Vector3(-width, dis, -len);
        lines[9] = new Vector3(-width, -dis, -len);

        //Buttom
        lines[10] = new Vector3(-width, -dis, -len);
        lines[11] = new Vector3(width, -dis, -len);

        //Right
        lines[12] = new Vector3(width, -dis, -len);
        lines[13] = new Vector3(width, dis, -len);

        //Top
        lines[14] = new Vector3(width, dis, -len);
        lines[15] = new Vector3(-width, dis, -len);

        #endregion

        return lines;

    }

}
