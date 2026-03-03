using Avalonia.Controls;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Linq;
using System.Numerics;

namespace PETRenderer
{
    class Program
    {
        private static IWindow _window;
        private static IKeyboard _keyboard;
        private static Camera _camera;
        private static Scene _scene;
        private static Renderer _renderer;

        private static void DUMMYFUNCTIONREMOVEMENOW(string[] args) {
            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(1920, 1080);
            options.Title = "Cardinal Renderer";

            _window = Silk.NET.Windowing.Window.Create(options);
            _window.Load += OnLoad;
            _window.Update += OnUpdate;
            _window.Render += OnRender;
            _window.FramebufferResize += OnResize;
            _window.Closing += OnClose;

            _window.Run();
            _window.Dispose();
        }

        private static void OnLoad() {
            var input = _window.CreateInput();
            _keyboard = input.Keyboards.FirstOrDefault();
            if (_keyboard != null)
                _keyboard.KeyDown += OnKeyDown;

            _camera = new Camera();
            _scene = new Scene();
            _renderer = new Renderer();

            _renderer.OnLoadEffects += OnLoadEffects;
            _scene.OnPopulate += OnPopulateScene;



            _renderer.Initialize(GL.GetApi(_window), _window.FramebufferSize);
            _scene.Load(_renderer.Gl);

            for (int i = 0; i < input.Mice.Count; i++) {
                input.Mice[i].Cursor.CursorMode = CursorMode.Raw;
                input.Mice[i].MouseMove += (mouse, pos) => _camera.ProcessMouseMove(pos);
            }
        }

        private static void OnUpdate(double deltaTime) {
            _camera.ProcessKeyboard(_keyboard, (float)deltaTime);
            _scene.Update(_window.Time, deltaTime);
        }

        private static void OnRender(double deltaTime) {
            _renderer.Render(_scene, _camera, _window.FramebufferSize);
        }

        private static void OnResize(Vector2D<int> newSize) {
            _renderer.Resize(newSize);
        }

        private static void OnClose() {
            _scene.Dispose();
            _renderer.Dispose();
        }

        private static void OnKeyDown(IKeyboard keyboard, Key key, int arg) {
            if (key == Key.Escape)
                _window.Close();
        }

        private static void OnLoadEffects(Renderer renderer, PostProcessor postProcessor, Vector2D<int> frameBufferSize) {
            var pixelateShader = new Shader(renderer.Gl, "shaders/post.vert", "shaders/pixelate.frag");
            pixelateShader.Use();
            pixelateShader.SetUniform("uResolution", new Vector2(frameBufferSize.X, frameBufferSize.Y));
            pixelateShader.SetUniform("uPixelSize", 4f);
            postProcessor.AddEffect(pixelateShader);


            //var greyScaleShader = new Shader(Gl, "shaders/post.vert", "shaders/greyscale.frag");
            //postProcessor.AddEffect(greyScaleShader);
        }

        private static void OnPopulateScene(Scene scene) {
            var ground = new MeshNode(_renderer.Gl,
                new Model(_renderer.Gl, "models/groundPlane.obj"),
                new Texture(_renderer.Gl, "textures/testTex.png"),
                "Ground");
            scene.AddToRoot(ground);

            var parentBall = new MeshNode(_renderer.Gl,
                new Model(_renderer.Gl, "models/cineball.obj"),
                new Texture(_renderer.Gl, "textures/absolute.png"),
                "ParentBall");
            scene.AddToRoot(parentBall);

            var childBall = new MeshNode(_renderer.Gl,
                new Model(_renderer.Gl, "models/cineball.obj"),
                new Texture(_renderer.Gl, "textures/testTex.png"),
                "ChildBall");
            childBall.LocalTransform = new Transform { Position = new Vector3(2, 0, 0) };
            parentBall.AddChild(childBall);
        }
    }
}