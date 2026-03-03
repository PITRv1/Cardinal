using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Diagnostics;
using System.Numerics;
using Shader = PETRenderer.Shader;
using Texture = PETRenderer.Texture;
using PETRenderer;
using System;

namespace Cardinal;


public class PETRendererController : OpenGlControlBase
{
    private GL _gl;
    private Camera _camera;
    private Scene _scene;
    private Renderer _renderer;

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

        _renderer.OnLoadEffects += OnLoadEffects;
        _scene.OnPopulate += OnPopulateScene;

        //_camera.Position = new Vector3(0, 0, 0);
        //_camera.Pitch = -30f;

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

        //_camera.ProcessKeyboard(null, (float)deltaTime);
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
        var pixelateShader = new Shader(_gl, "3DRenderer/shaders/post.vert", "3DRenderer/shaders/pixelate.frag");
        pixelateShader.Use();
        pixelateShader.SetUniform("uResolution", new Vector2(framebufferSize.X, framebufferSize.Y));
        pixelateShader.SetUniform("uPixelSize", 4f);
        postProcessor.AddEffect(pixelateShader);
    }

    private void OnPopulateScene(Scene scene) {
        var ground = new MeshNode(_gl,
            new Model(_gl, "3DRenderer/models/groundPlane.obj"),
            new Texture(_gl, "3DRenderer/textures/testTex.png"),
            "Ground");
        scene.AddToRoot(ground);

        var parentBall = new MeshNode(_gl,
            new Model(_gl, "3DRenderer/models/cineball.obj"),
            new Texture(_gl, "3DRenderer/textures/absolute.png"),
            "ParentBall");
        scene.AddToRoot(parentBall);

        var childBall = new MeshNode(_gl,
            new Model(_gl, "3DRenderer/models/cineball.obj"),
            new Texture(_gl, "3DRenderer/textures/testTex.png"),
            "ChildBall");
        childBall.LocalTransform = new Transform { Position = new Vector3(2, 0, 0) };
        parentBall.AddChild(childBall);
    }
}
