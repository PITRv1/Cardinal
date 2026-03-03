using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Numerics;
using System;


namespace PETRenderer
{
    public class Skybox : IDisposable
    {
        private BufferObject<float> _vbo;
        private VertexArrayObject<float, uint> _vao;
        private CubemapTexture _texture;
        private Shader _shader;
        private GL _gl;

        private static readonly float[] SkyboxVertices = {
            -1f,  1f, -1f,  -1f, -1f, -1f,   1f, -1f, -1f,   1f, -1f, -1f,   1f,  1f, -1f,  -1f,  1f, -1f,
            -1f, -1f,  1f,  -1f, -1f, -1f,  -1f,  1f, -1f,  -1f,  1f, -1f,  -1f,  1f,  1f,  -1f, -1f,  1f,
             1f, -1f, -1f,   1f, -1f,  1f,   1f,  1f,  1f,   1f,  1f,  1f,   1f,  1f, -1f,   1f, -1f, -1f,
            -1f, -1f,  1f,  -1f,  1f,  1f,   1f,  1f,  1f,   1f,  1f,  1f,   1f, -1f,  1f,  -1f, -1f,  1f,
            -1f,  1f, -1f,   1f,  1f, -1f,   1f,  1f,  1f,   1f,  1f,  1f,  -1f,  1f,  1f,  -1f,  1f, -1f,
            -1f, -1f, -1f,  -1f, -1f,  1f,   1f, -1f, -1f,   1f, -1f, -1f,  -1f, -1f,  1f,   1f, -1f,  1f
        };

        public Skybox(GL gl, string[] facePaths) {
            _gl = gl;
            _vbo = new BufferObject<float>(gl, SkyboxVertices, BufferTargetARB.ArrayBuffer);
            _vao = new VertexArrayObject<float, uint>(gl, _vbo, null);
            _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 3, 0);
            _texture = new CubemapTexture(gl, facePaths);
            _shader = new Shader(gl, "3DRenderer/shaders/skybox.vert", "3DRenderer/shaders/skybox.frag");
        }

        public void Render(Matrix4x4 view, Matrix4x4 projection, Vector2D<int> framebufferSize) {
            _gl.Disable(EnableCap.CullFace);
            _gl.DepthFunc(DepthFunction.Lequal);
            _vao.Bind();
            _shader.Use();

            var skyboxView = new Matrix4x4(
                view.M11, view.M12, view.M13, 0,
                view.M21, view.M22, view.M23, 0,
                view.M31, view.M32, view.M33, 0,
                0, 0, 0, 1);

            var skyboxProjection = Matrix4x4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(45f),
                (float)framebufferSize.X / framebufferSize.Y,
                0.1f, 100.0f);

            _shader.SetUniform("uView", skyboxView);
            _shader.SetUniform("uProjection", skyboxProjection);
            _gl.ActiveTexture(TextureUnit.Texture0);
            _texture.Bind();
            _shader.SetUniform("uSkybox", 0);

            _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);
            _gl.DepthFunc(DepthFunction.Less);
            _gl.Enable(EnableCap.CullFace);
        }

        public void Dispose() {
            _vao.Dispose();
            _vbo.Dispose();
            _texture.Dispose();
            _shader.Dispose();
        }
    }
}