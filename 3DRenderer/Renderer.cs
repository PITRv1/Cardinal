using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;

namespace PETRenderer
{
    public class Renderer : IDisposable
    {
        public event Action<Renderer, PostProcessor, Vector2D<int>> OnLoadEffects;


        public GL Gl { get; private set; }

        private Shader _shader;
        private PostProcessor _postProcessor;

        public void Initialize(GL gl, Vector2D<int> framebufferSize) {
            Gl = gl;

            var size = framebufferSize;
            _postProcessor = new PostProcessor(Gl, (uint)size.X, (uint)size.Y);

            OnLoadEffects?.Invoke(this, _postProcessor, size);

            _shader = new Shader(Gl, "3DRenderer/shaders/shader.vert", "3DRenderer/shaders/shader.frag");
        }

        public void Render(Scene scene, Camera camera, Vector2D<int> framebufferSize) {
            Gl.Enable(EnableCap.DepthTest);
            Gl.Enable(EnableCap.CullFace);
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            var view = camera.GetViewMatrix();
            var projection = camera.GetProjectionMatrix(framebufferSize);

            _postProcessor.BeginCapture();

            scene.Render(_shader, view, projection, framebufferSize);

            _postProcessor.EndCaptureAndRender();
        }

        public void Resize(Vector2D<int> newSize) {
            Gl.Viewport(newSize);
        }

        public void Dispose() {
            _shader.Dispose();
            _postProcessor.Dispose();
        }
    }
}