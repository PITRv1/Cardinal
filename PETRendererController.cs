using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Cardinal.Views;
using PETRenderer;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System;
using System.Diagnostics;
using System.Numerics;
using Shader = PETRenderer.Shader;
using Texture = PETRenderer.Texture;
using Vector2 = System.Numerics.Vector2;

namespace Cardinal;


public class PETRendererController : OpenGlControlBase
{
    private GL _gl;
    private Camera _camera;
    private Scene _scene;
    private Renderer _renderer;
    private Window _mainWindow;

    private Stopwatch _stopwatch = Stopwatch.StartNew();
    private double _lastTime = 0;

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
                _camera.OrthoScaler - (float)e.Delta.Y * 0.01f,
                0.001f,
                0.1f
            );
        };


        _renderer.Initialize(_gl, width, height);
        _scene.Load(_renderer.Gl);

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
        var insideGround = new MeshNode(_gl,
            new Model(_gl, "3DRenderer/models/trueTexturedGround.obj"),
            new Texture(_gl, "3DRenderer/textures/uvGrid.png"),
            "Ground");
        //insideGround.LocalTransform = new Transform { Position = new Vector3(0,0.01f,0) };
        scene.AddToRoot(insideGround);

        //var groundGrid = new MeshNode(_gl,
        //    new Model(_gl, "3DRenderer/models/groundGrid.obj"),
        //    new Texture(_gl, "3DRenderer/textures/uvGrid.png"),
        //    "Grid");
        //insideGround.LocalTransform = new Transform { Position = new Vector3(0, 0.01f, 0) };
        //scene.AddToRoot(groundGrid);


        var outsideGround = new MeshNode(_gl,
            new Model(_gl, "3DRenderer/models/outsideGround.obj"),
            new Texture(_gl, "3DRenderer/textures/marsSurface.png"),
            new Texture(_gl, "3DRenderer/textures/marsSurface_normal.png"),
            "ParentBall");
        outsideGround.NormalStrength = 0.5f;
        scene.AddToRoot(outsideGround);
    }
}
