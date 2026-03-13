using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Cardinal.Views;
using PETRenderer;
using Silk.NET.Assimp;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Text.RegularExpressions;
using Camera = PETRenderer.Camera;
using Scene = PETRenderer.Scene;
using Shader = PETRenderer.Shader;
using Texture = PETRenderer.Texture;
using Vector2 = System.Numerics.Vector2;
using Cardinal.Backend;


namespace Cardinal;


public class PETRendererController : OpenGlControlBase
{
    public static event Action? MineralsLoaded;
    private GL _gl;
    private Camera _camera;
    private Scene _scene;
    private Renderer _renderer;
    private Window _mainWindow;

    private Stopwatch _stopwatch = Stopwatch.StartNew();
    private double _lastTime = 0;

    private Map map = new();


    private MeshNode roverModel;
    private Vector2 prevRoverPos;

    protected override void OnOpenGlInit(GlInterface glInterface) {
        double scaling = VisualRoot?.RenderScaling ?? 1.0;
        uint width = (uint)(Bounds.Width * scaling);
        uint height = (uint)(Bounds.Height * scaling);

        _gl = GL.GetApi(proc => glInterface.GetProcAddress(proc));

        _camera = new Camera();
        _scene = new Scene();
        _renderer = new Renderer();
        _mainWindow = TopLevel.GetTopLevel(this) as Window;

        _renderer.OnLoadEffects += OnLoadEffects;
        _scene.OnPopulate += OnPopulateScene;

        IPointer _pointer = null;


        _mainWindow.PointerPressed += (sender, e) => {
            _mainWindow.Focus();
            _pointer = e.Pointer;
            e.Pointer.Capture(_mainWindow); 
            };


        _mainWindow.PointerReleased += (sender, e) => {
            e.Pointer.Capture(null);
            _camera.EndDrag();
        };

        _mainWindow.PointerMoved += (mouse, e) => {
            if (e.Pointer.Captured == _mainWindow)
            {
                var mousePos = e.GetPosition(_mainWindow);
                _camera.ProcessMouseMove(new System.Numerics.Vector2((float)mousePos.X, (float)mousePos.Y));
            }
            };

        _mainWindow.PointerWheelChanged += (sender, e) => {
            _camera.OrthoScaler = Math.Clamp(
                _camera.OrthoScaler - (float)e.Delta.Y * 0.005f,
                0.001f,
                0.1f
            );
        };

        Global.ProgramEventManager.StepDataSent += _OnStepDataRecieved;

        map = Map.Load("./Backend/maps/mars_map_50x50.csv");


        _renderer.Initialize(_gl, width, height);
        _scene.Load(_renderer.Gl);
    }

    private void _OnStepDataRecieved(StepData stepData)
    {
        Vector2 dir = new Vector2(prevRoverPos.X, prevRoverPos.Y) - new Vector2(stepData.position.X, stepData.position.Y);

        roverModel.LocalTransform.Position = new Vector3(stepData.position.X,0, stepData.position.Y);

        prevRoverPos = new Vector2(stepData.position.X, stepData.position.Y);
    }

    protected override void OnOpenGlRender(GlInterface glInterface, int fb) {
        double scaling = VisualRoot?.RenderScaling ?? 1.0;
        uint width = (uint)(Bounds.Width * scaling);
        uint height = (uint)(Bounds.Height * scaling);

        _renderer.Gl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)fb);
        _renderer.Gl.Viewport(0, 0, width,height);

        double now = _stopwatch.Elapsed.TotalSeconds;
        double deltaTime = now - _lastTime;
        _lastTime = now;

        _scene.Update(now, deltaTime);

        var size = new Vector2D<int>((int)width, (int)height);
        _renderer.Render(_scene, _camera, size, fb);


        RequestNextFrameRendering();
    }

    protected override void OnOpenGlDeinit(GlInterface glInterface) {
        _scene.Dispose();
        _renderer.Dispose();
    }

    private void OnLoadEffects(Renderer renderer, PostProcessor postProcessor, Vector2D<int> framebufferSize) {
        //var pixelateShader = new Shader(_gl, "3DRenderer/shaders/post.vert", "3DRenderer/shaders/pixelate.frag");
        //pixelateShader.Use();
        //pixelateShader.SetUniform("uResolution", new Vector2(framebufferSize.X, framebufferSize.Y));
        //pixelateShader.SetUniform("uPixelSize", 4f);
        //postProcessor.AddEffect(pixelateShader);
    }

    private void OnPopulateScene(Scene scene) {
        //Map
        var insideGround = new MeshNode(_gl,
            new Model(_gl, "3DRenderer/models/insideGround.obj"),
            new Texture(_gl, "3DRenderer/textures/insideGroundColor.png", TextureType.None, GLEnum.Nearest),
            "insideGround");
        scene.AddToRoot(insideGround);

        var outsideGround = new MeshNode(_gl,
            new Model(_gl, "3DRenderer/models/outsideGround.obj"),
            new Texture(_gl, "3DRenderer/textures/marsSurface.png"),
            new Texture(_gl, "3DRenderer/textures/marsSurface_normal.png", TextureType.Normals, GLEnum.Linear),
            "outsideGround");
        outsideGround.NormalStrength = 0.2f;
        scene.AddToRoot(outsideGround);
        //---

        var mapHolderNode = new SceneNode("MyNode");
        mapHolderNode.LocalTransform = new Transform { Position = new Vector3(-24.5f, 0, -24.5f) };
        scene.AddToRoot(mapHolderNode);

        //Tri.c.h.a.e.l.
        roverModel = new MeshNode(_gl,
            new Model(_gl, "3DRenderer/models/Trichael.obj"),
            new Texture(_gl, "3DRenderer/textures/TrichaelColor.png"),
            "rover");

        roverModel.LocalTransform = new Transform {
            Rotation = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), MathHelper.DegreesToRadians(180f))
        };
        mapHolderNode.AddChild(roverModel);
        //---

        //Crystal
        foreach (var currentWorldRow in map.WorldMap)
        {

            foreach (var node in currentWorldRow)
            {

                switch (node.Character)
                {
                    case '#':
                        var rockModel = new MeshNode(_gl,
                        new Model(_gl, "3DRenderer/models/Rock.obj"),
                        new Texture(_gl, "3DRenderer/textures/absolute.png"),
                        $"rock{node.ToString()}");

                        rockModel.LocalTransform = new Transform
                        {
                            Position = new Vector3(node.Coords.X, 0, node.Coords.Y)
                        };
                        mapHolderNode.AddChild(rockModel);
                        break;
                    case 'B':
                        var blueCrystalModel = new MeshNode(_gl,
                        new Model(_gl, "3DRenderer/models/Crystal.obj"),
                        new Texture(_gl, "3DRenderer/textures/BlueCrystalColor.png"),
                        $"crystal{node.ToString()}");

                        blueCrystalModel.LocalTransform = new Transform
                        {
                            Position = new Vector3(node.Coords.X, 0, node.Coords.Y)
                        };
                        mapHolderNode.AddChild(blueCrystalModel);
                        break;
                    case 'Y':
                        var yellowCrystalModel = new MeshNode(_gl,
                        new Model(_gl, "3DRenderer/models/Crystal.obj"),
                        new Texture(_gl, "3DRenderer/textures/YellowCrystalColor.png"),
                        $"crystal{node.ToString()}");

                        yellowCrystalModel.LocalTransform = new Transform
                        {
                            Position = new Vector3(node.Coords.X, 0, node.Coords.Y)
                        };
                        mapHolderNode.AddChild(yellowCrystalModel);
                        break;
                    case 'G':
                        var greenCrystalModel = new MeshNode(_gl,
                        new Model(_gl, "3DRenderer/models/Crystal.obj"),
                        new Texture(_gl, "3DRenderer/textures/GreenCrystalColor.png"),
                        $"crystal{node.ToString()}");

                        greenCrystalModel.LocalTransform = new Transform
                        {
                            Position = new Vector3(node.Coords.X, 0, node.Coords.Y)
                        };
                        mapHolderNode.AddChild(greenCrystalModel);

                        break;
                    case 'S':
                        break;
                }
            }
        }

        MineralsLoaded?.Invoke();
    }
}
