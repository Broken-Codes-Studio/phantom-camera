using Godot;
using System.Collections.Generic;

namespace PhantomCamera;

using TwoDimension;
using ThreeDimension;

#if TOOLS
[Tool]
#endif
public partial class PhantomCameraManager : Node
{

    private List<PhantomCameraHost> _phantomCameraHosts = new();
    public PhantomCameraHost[] PhantomCameraHosts => _phantomCameraHosts.ToArray();

    private List<PhantomCamera2D> _phantomCamera2Ds = new();
    public PhantomCamera2D[] PhantomCamera2Ds => _phantomCamera2Ds.ToArray();

    private List<PhantomCamera3D> _phantomCamera3Ds = new();
    public PhantomCamera3D[] PhantomCamera3Ds => _phantomCamera3Ds.ToArray();

    public override void _EnterTree()
    {
        Engine.PhysicsJitterFix = 0;
    }

    public void PcamHostAdded(PhantomCameraHost caller)
    {
        _phantomCameraHosts.Add(caller);
    }

    public void PcamHostRemoved(PhantomCameraHost caller)
    {
        _phantomCameraHosts.Remove(caller);
    }

    public void PcamAdded(Node caller, int hostSlot = 0)
    {
        if (caller is PhantomCamera2D caller2D)
            _phantomCamera2Ds.Add(caller2D);
        else if (caller is PhantomCamera3D caller3D)
            _phantomCamera3Ds.Add(caller3D);

        // if(PhantomCameraHosts.Count > 0)
        //    PhantomCameraHosts[hostSlot].PcamAddedToScene(caller);
    }

    public void PcamRemoved(Node caller)
    {
        if (caller is PhantomCamera2D caller2D)
            _phantomCamera2Ds.Remove(caller2D);
        else if (caller is PhantomCamera3D caller3D)
            _phantomCamera3Ds.Remove(caller3D);
        else
            GD.PrintErr("This method can only be called from a PhantomCamera node");
    }

    public void SceneChanged()
    {

        _phantomCameraHosts.Clear();
        _phantomCamera2Ds.Clear();
        _phantomCamera3Ds.Clear();

    }

}
