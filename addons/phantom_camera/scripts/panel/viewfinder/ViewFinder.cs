using Godot;
using PhantomCamera.ThreeDimension;
using PhantomCamera.TwoDimension;
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
    private TextureRect _emptyStateIcon;
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
        _emptyStateIcon = GetNode<TextureRect>("%EmptyStateIcon");
        _emptyStateText = GetNode<RichTextLabel>("%EmptyStateText");
        _addNodeButton = GetNode<Button>("%AddNodeButton");
        _addNodeButtonText = GetNode<RichTextLabel>("%AddNodeTypeText");

        _priorityOverrideButton = GetNode<Button>("%PriorityOverrideButton");
        _priorityOverrideNameLabel = GetNode<Label>("%PriorityOverrideNameLabel");

        _camera2D = GetNode<Camera2D>("%Camera2D");

        if (!Engine.IsEditorHint())
        {
            SetProcess(true);
            Color selfModulate = CameraViewportPanel.SelfModulate;
            selfModulate.A = 0;
            CameraViewportPanel.SelfModulate = selfModulate;
        }

        _rootNode = GetTree().CurrentScene;

        if (_rootNode is Node2D || _rootNode is Node3D)
        {
            GetNode<Container>("%SubViewportContainer").Visible = false;

            if (_rootNode is Node2D)
                _is2D = true;
            else
                _is2D = false;

            _setViewfinder(_rootNode, false);

        }

        if (Engine.IsEditorHint())
        {
            // BUG - Both signals below are called whenever a noe is selected in the scenetree
            // Should only be triggered whenever a node is added or removed.
            GetTree().NodeAdded += _nodeAddedOrRemoved;
            GetTree().NodeRemoved += _nodeAddedOrRemoved;
        }
        else
            _emptyStateControl.Visible = false;

        _priorityOverrideButton.Visible = false;

        // Triggered when viewport size is changed in Project Settings
        ProjectSettings.SettingsChanged += _settingsChanged;

    }

    public override void _ExitTree()
    {
        if (Engine.IsEditorHint())
        {
            if (GetTree().IsConnected("NodeAdded", Callable.From<Node>(_nodeAddedOrRemoved)))
            {
                GetTree().NodeAdded -= _nodeAddedOrRemoved;
                GetTree().NodeRemoved -= _nodeAddedOrRemoved;
            }
        }

        if (AspectRatioContainer.IsConnected("Resized", Callable.From(_resized)))
            AspectRatioContainer.Resized -= _resized;

        if (_addNodeButton.IsConnected("Pressed", Callable.From(VisibilityCheck)))
            _addNodeButton.Pressed -= VisibilityCheck;

        if (IsInstanceValid(_activePcam))
            if (_activePcam.IsConnected("DeadZoneChanged", Callable.From(_onDeadZoneChanged)))
                _activePcam.Disconnect("DeadZoneChanged", Callable.From(_onDeadZoneChanged));

        if (_priorityOverrideButton.IsConnected("Pressed", Callable.From(_selectOverridePcam)))
            _priorityOverrideButton.Pressed -= _selectOverridePcam;
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint() && !ViewFinderVisible)
            return;
        if (!IsInstanceValid(_activePcam))
            return;


        Vector2 unprojectedPositionClamped = new();

        if (_activePcam is PhantomCamera2D pcam2D)
        {
            // TODO: Complete the 2D parts.
            // unprojectedPositionClamped = new(Mathf.Clamp(pcam2D., MinHorizontal, MaxHorizontal),
            //                                  Mathf.Clamp(pcam2D., MinVertical, MaxVertical));
        }
        else if (_activePcam is PhantomCamera3D pcam3D)
        {
            unprojectedPositionClamped = new(Mathf.Clamp(pcam3D.ViewportPosition.X, MinHorizontal, MaxHorizontal),
                                             Mathf.Clamp(pcam3D.ViewportPosition.Y, MinVertical, MaxVertical));
        }
        else
            return;

        if (!Engine.IsEditorHint())
            TargetPoint.Position = CameraViewportPanel.Size * unprojectedPositionClamped - TargetPoint.Size / 2;

        if (_is2D)
        {
            if (!IsInstanceValid(PcamHost))
                return;
            if (!IsInstanceValid(PcamHost.camera2D))
                return;

            float windowSizeHeight = ProjectSettings.GetSetting("display/window/size/viewport_height").As<float>();
            Subviewport.Size2DOverride = Subviewport.Size * (int)(windowSizeHeight / Subviewport.Size.Y);

            _camera2D.GlobalTransform = PcamHost.camera2D.GlobalTransform;
            _camera2D.Offset = PcamHost.camera2D.Offset;
            _camera2D.Zoom = PcamHost.camera2D.Zoom;
            _camera2D.IgnoreRotation = PcamHost.camera2D.IgnoreRotation;
            _camera2D.AnchorMode = PcamHost.camera2D.AnchorMode;
            _camera2D.LimitLeft = PcamHost.camera2D.LimitLeft;
            _camera2D.LimitTop = PcamHost.camera2D.LimitTop;
            _camera2D.LimitRight = PcamHost.camera2D.LimitRight;
            _camera2D.LimitBottom = PcamHost.camera2D.LimitBottom;
        }

    }
    #endregion

    #region Public Methods

    public void VisibilityCheck()
    {
        if (!ViewFinderVisible)
            return;

        PhantomCameraHost phantomCameraHost = null;
        bool hasCamera = false;

        if (GetTree().Root.GetNode<PhantomCameraManager>(PhantomCameraConstants.PCAM_MANAGER_NODE_NAME).PhantomCameraHosts.Length > 0)
        {
            hasCamera = true;
            phantomCameraHost = GetTree().Root.GetNode<PhantomCameraManager>(PhantomCameraConstants.PCAM_MANAGER_NODE_NAME).PhantomCameraHosts[0];
        }
        else
            return;

        Node root = EditorInterface.Singleton.GetEditedSceneRoot();

        if (root is Node2D)
        {
            // TODO: Complete the 2D parts
        }
        else if (root is Node3D)
        {
            Camera3D camera_3D;

            if (hasCamera)
                camera_3D = phantomCameraHost.camera3D;
            else
                camera_3D = root.GetViewport().GetCamera3D();

            _is2D = false;
            IsScene = true;
            _addNodeButton.Visible = true;
            _checkCamera(root, camera_3D, false);
        }
        else
        {
            IsScene = false;
            // Is not a 2D or 3D scene;
            _setEmptyViewfinderState(_noOpenSceneString, _noOpenSceneIcon);
            _addNodeButton.Visible = false;
        }

        if (!_priorityOverrideButton.IsConnected("Pressed", Callable.From(_selectOverridePcam)))
            _priorityOverrideButton.Pressed += _selectOverridePcam;
    }

    public void UpdateDeadZone()
    {
        _setViewfinder(_rootNode, true);
    }

    public void SceneChanged(Node sceneRoot)
    {
        if (sceneRoot is not Node2D && sceneRoot is not Node3D)
        {
            IsScene = false;
            _setEmptyViewfinderState(_noOpenSceneString, _noOpenSceneIcon);
            _addNodeButton.Visible = false;
        }
    }

    #endregion

    #region Private Methods

    private void _setViewfinder(Node root, bool editor)
    {

    }

    private void _setViewfinderState()
    {
        _emptyStateControl.Visible = false;

        _framedViewFinder.Visible = true;

        if (IsInstanceValid(_activePcam))
        {
            if (_activePcam is PhantomCamera2D pcam2D)
            {
                // TODO: Complete 2D parts
            }
            else if (_activePcam is PhantomCamera3D pcam3D)
            {
                if (pcam3D.followMode == ThreeDimension.FollowMode.FRAMED)
                {
                    _deadZoneHBoxContainer.Visible = true;
                    TargetPoint.Visible = true;
                }
                else
                {
                    _deadZoneHBoxContainer.Visible = false;
                    TargetPoint.Visible = false;
                }
            }
        }
    }

    private void _setEmptyViewfinderState(string text, CompressedTexture2D icon)
    {
        _framedViewFinder.Visible = false;
        TargetPoint.Visible = false;

        _emptyStateControl.Visible = true;
        _emptyStateIcon.Texture = icon;
        if (icon == _noOpenSceneIcon)
            _emptyStateText.Text = "[center]No " + text + "[/center]";
        else
            _emptyStateText.Text = "[center]No [b]" + text + "[/b] in scene[/center]";

        if (_addNodeButton.IsConnected("Pressed", Callable.From(() => _addNode(text))))
            _addNodeButton.Disconnect("Pressed", Callable.From(() => _addNode(text)));

        _addNodeButton.Pressed += () => _addNode(text);
    }

    private void _nodeAddedOrRemoved(Node node)
    {
        VisibilityCheck();
    }

    private void _settingsChanged()
    {
        float viewportWidth = ProjectSettings.GetSetting("display/window/size/viewport_width").As<float>();
        float viewportHeight = ProjectSettings.GetSetting("display/window/size/viewport_height").As<float>();
        float ratio = viewportWidth / viewportHeight;
        AspectRatioContainer.Ratio = ratio;

        Vector2 cameraViewportPanelSize = CameraViewportPanel.Size;

        cameraViewportPanelSize.X = viewportWidth / (viewportHeight / Subviewport.Size.Y);

        CameraViewportPanel.Size = cameraViewportPanelSize;

    }

    private void _resized()
    {
        _onDeadZoneChanged();
    }

    private void _onDeadZoneChanged()
    {

    }

    private void _checkCamera(Node root, Node camera, bool is2D)
    {

    }

    private void _updateButton(string text, CompressedTexture2D icon, Color color)
    {
        _addNodeButtonText.Text = "[center]Add [img=32]" + icon.ResourcePath + "[/img] [b]" + text + "[/b][/center]";
        StyleBoxFlat buttonThemeHover = _addNodeButton.GetThemeStylebox("hover") as StyleBoxFlat;
        buttonThemeHover.BorderColor = color;
        _addNodeButton.AddThemeStyleboxOverride("hover", buttonThemeHover);
    }

    private void _addNode(string nodeType)
    {

    }

    // TODO: Complete 2D parts
    // private Camera2D _getCamera2D(){}

    private void _instantiateNode(Node root, Node node, string name)
    {
        node.Name = name;
        root.AddChild(node);
        node.Owner = GetTree().EditedSceneRoot;
    }

    #region Priority Override Methods

    private void _onUpdateEditorViewfinder(PhantomCameraHost pcamHost)
    {
        Node activePcam = pcamHost.GetActivePcame();

        if (activePcam is PhantomCamera2D activePcam2D)
        {
            // TODO: Complete 2D Parts.
        }
        else if (activePcam is PhantomCamera3D activePcam3D)
        {
            if (activePcam3D.PriorityOverride)
            {
                _activePcam = pcamHost.GetActivePcame();
                _priorityOverrideButton.Visible = true;
                _priorityOverrideNameLabel.Text = _activePcam.Name;
                _priorityOverrideButton.TooltipText = activePcam.Name;
            }
            else
                _priorityOverrideButton.Visible = false;
        }
    }

    private void _selectOverridePcam()
    {
        var editorInterface = EditorInterface.Singleton;

        editorInterface.GetSelection().Clear();
        editorInterface.GetSelection().AddNode(_activePcam);
    }

    #endregion

    #endregion

}
