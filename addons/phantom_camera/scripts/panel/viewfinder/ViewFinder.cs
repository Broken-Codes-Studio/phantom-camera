using Godot;
using System;

namespace PhantomCamera.Inspector;

[Tool]
public partial class ViewFinder : Control
{

    # region Constants & Readonly

    private const float _overlayColorAlpha = .3f;

    #endregion

    #region Variables

    public VBoxContainer DeadZoneCenterHbox { get; private set; }
    public Panel DeadZoneCenterCenterPanel { get; private set; }
    public Panel DeadZoneLeftCenterPanel { get; private set; }
    public Panel DeadZoneRightCenterPanel { get; private set; }
    public Panel TargetPoint { get; private set; }

    public AspectRatioContainer AspectRatioContainer { get; private set; }
    public Panel CameraViewportPanel { get; private set; }
    public SubViewport Subviewport { get; private set; }

    public PhantomCameraHost[] PcamHostGroup;

    public bool IsScene;

    public bool ViewFinderVisible;

    public float MinHorizontal;
    public float MaxHorizontal;
    public float MinVertical;
    public float MaxVertical;

    public PhantomCameraHost PcamHost;

    private CompressedTexture2D _camera2DIcon;
    private CompressedTexture2D _camera3DIcon;
    private CompressedTexture2D _pcamHostIcon;
    private CompressedTexture2D _pcam2DIcon;
    private CompressedTexture2D _pcam3DIcon;
    private CompressedTexture2D _noOpenSceneIcon;

    private Control _framedViewFinder;
    private Control _deadZoneHBoxContainer;

    private Control _emptyStateControl;
    private Control _emptyStateIcon;
    private RichTextLabel _emptyStateText;
    private Button _addNodeButton;
    private RichTextLabel _addNodeButtonText;

    private Button _priorityOverrideButton;
    private Label _priorityOverrideNameLabel;

    private Camera2D _camera2D;

    private string _noOpenSceneString = "[b]2D[/b] or [b]3D[/b] scene open";

    private Node _selectedCamera;
    private Node _activePcam;

    private bool _is2D;

    private Node _rootNode;

    #endregion

    #region Override Methods
    public override void _Ready()
    {
        _camera2DIcon = GD.Load<CompressedTexture2D>("res://addons/phantom_camera/icons/viewfinder/Camera2DIcon.svg");
        _camera3DIcon = GD.Load<CompressedTexture2D>("res://addons/phantom_camera/icons/viewfinder/Camera3DIcon.svg");
        _pcamHostIcon = GD.Load<CompressedTexture2D>("res://addons/phantom_camera/icons/phantom_camera_host.svg");
        _pcam2DIcon = GD.Load<CompressedTexture2D>("res://addons/phantom_camera/icons/phantom_camera_2d.svg");
        _pcam3DIcon = GD.Load<CompressedTexture2D>("res://addons/phantom_camera/icons/phantom_camera_3d.svg");
        _noOpenSceneIcon = GD.Load<CompressedTexture2D>("res://addons/phantom_camera/icons/viewfinder/SceneTypesIcon.svg");

        DeadZoneCenterHbox = GetNode<VBoxContainer>("%DeadZoneCenterHBoxContainer");
        DeadZoneCenterCenterPanel = GetNode<Panel>("%DeadZoneCenterCenterPanel");
        DeadZoneLeftCenterPanel = GetNode<Panel>("%DeadZoneLeftCenterPanel");
        DeadZoneRightCenterPanel = GetNode<Panel>("%DeadZoneRightCenterPanel");
        TargetPoint = GetNode<Panel>("%TargetPoint");

        AspectRatioContainer = GetNode<AspectRatioContainer>("%AspectRatioContainer");
        CameraViewportPanel = AspectRatioContainer.GetChild<Panel>(0);
        Subviewport = GetNode<SubViewport>("%SubViewport");

        _framedViewFinder = GetNode<Control>("%FramedViewfinder");
        _deadZoneHBoxContainer = GetNode<Control>("%DeadZoneHBoxContainer");

        _emptyStateControl = GetNode<Control>("%EmptyStateControl");
        _emptyStateIcon = GetNode<Control>("%EmptyStateIcon");
        _emptyStateText = GetNode<RichTextLabel>("%EmptyStateText");
        _addNodeButton = GetNode<Button>("%AddNodeButton");
        _addNodeButtonText = GetNode<RichTextLabel>("%AddNodeTypeText");

        _priorityOverrideButton = GetNode<Button>("%PriorityOverrideButton");
        _priorityOverrideNameLabel = GetNode<Label>("%PriorityOverrideNameLabel");

        _camera2D = GetNode<Camera2D>("%Camera2D");
    }

    public override void _ExitTree()
    {

    }

    public override void _Process(double delta)
    {

    }
    #endregion

    #region Public Methods

    #endregion

    #region Private Methods

    #endregion

}
