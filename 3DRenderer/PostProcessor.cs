using Silk.NET.OpenGL;
using System.Collections.Generic;
using System;

namespace PETRenderer
{
    public class PostProcessor : IDisposable
    {
        private GL _gl;
        private uint _fbo;
        private uint _colorTexture;
        private uint _rbo;
        private BufferObject<float> _vbo;
        private VertexArrayObject<float, uint> _vao;
        private Shader _passthroughShader;

        private List<Shader> _effectShaders = new();
        private uint _pingTexture;
        private uint _pongTexture;
        private uint _pingFbo;
        private uint _pongFbo;

        private static readonly float[] QuadVertices = {
            -1f,  1f,     0f, 1f,
            -1f, -1f,     0f, 0f,
             1f, -1f,     1f, 0f,
            -1f,  1f,     0f, 1f,
             1f, -1f,     1f, 0f,
             1f,  1f,     1f, 1f
        };

        public PostProcessor(GL gl, uint width, uint height) {
            _gl = gl;
            SetupQuad();
            SetupFramebuffers(width, height);

            _passthroughShader = new Shader(gl, "3DRenderer/shaders/post.vert", "3DRenderer/shaders/passthrough.frag");
        }

        public void AddEffect(Shader shader) {
            _effectShaders.Add(shader);
        }

        public void BeginCapture() {
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
            _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        public void EndCaptureAndRender(int targetFramebuffer = 0) {
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)targetFramebuffer);

            if (_effectShaders.Count == 0) {
                BlitToScreen(_colorTexture, targetFramebuffer);
                return;
            }

            uint sourceTexture = _colorTexture;

            for(int i = 0; i < _effectShaders.Count; i++) {
                bool isLast = i == _effectShaders.Count - 1;

                if (isLast) {
                    _gl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)targetFramebuffer); ;
                } else {
                    uint targetFbo = (i % 2 == 0) ? _pingFbo : _pongFbo;
                    _gl.BindFramebuffer(FramebufferTarget.Framebuffer, targetFbo);
                    _gl.Clear(ClearBufferMask.ColorBufferBit);

                }

                _gl.Disable(EnableCap.DepthTest);
                _vao.Bind();
                _effectShaders[i].Use();
                _gl.BindTexture(TextureTarget.Texture2D, sourceTexture);
                _effectShaders[i].SetUniform("uScreenTexture", 0);
                _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
                _gl.Enable(EnableCap.DepthTest);

                sourceTexture = (i % 2 == 0) ? _pingTexture : _pongTexture;

            }
        }

        private unsafe void SetupFramebuffers(uint width, uint height) {
            _fbo = _gl.GenFramebuffer();
            _colorTexture = CreateColorTexture(width, height);
            _rbo = CreateDepthStencilRbo(width, height);

            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
            _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _colorTexture, 0);
            _gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, _rbo);

            _pingFbo = _gl.GenFramebuffer();
            _pingTexture = CreateColorTexture(width, height);
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _pingFbo);
            _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _pingTexture, 0);

            _pongFbo = _gl.GenFramebuffer();
            _pongTexture = CreateColorTexture(width, height);
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _pongFbo);
            _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _pongTexture, 0);

            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        private unsafe uint CreateColorTexture(uint width, uint height) {
            uint tex = _gl.GenTexture();
            _gl.BindTexture(TextureTarget.Texture2D, tex);
            _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb, width, height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, null);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            return tex;
        }

        private unsafe uint CreateDepthStencilRbo(uint width, uint height) {
            uint rbo = _gl.GenRenderbuffer();
            _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rbo);
            _gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.Depth24Stencil8, width, height);
            return rbo;
        }

        private void SetupQuad() {
            _vbo = new BufferObject<float>(_gl, QuadVertices, BufferTargetARB.ArrayBuffer);
            _vao = new VertexArrayObject<float, uint>(_gl, _vbo, null);
            _vao.VertexAttributePointer(0, 2, VertexAttribPointerType.Float, 4, 0);
            _vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 4, 2);
        }

        private void BlitToScreen(uint texture, int targetFramebuffer) {
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)targetFramebuffer);
            _gl.Disable(EnableCap.DepthTest);
            _vao.Bind();
            _gl.BindTexture(TextureTarget.Texture2D, texture);
            _passthroughShader.Use();
            _passthroughShader.SetUniform("uScreenTexture", 0);
            _gl.DrawArrays(PrimitiveType.Triangles, 0, 6);

            _gl.Enable(EnableCap.DepthTest);
        }

        public void Dispose() {
            _gl.DeleteFramebuffer(_fbo);
            _gl.DeleteFramebuffer(_pingFbo);
            _gl.DeleteFramebuffer(_pongFbo);
            _gl.DeleteTexture(_colorTexture);
            _gl.DeleteTexture(_pingTexture);
            _gl.DeleteTexture(_pongTexture);
            _gl.DeleteRenderbuffer(_rbo);
            _vao.Dispose();
            _vbo.Dispose();
            foreach (var shader in _effectShaders) {
                shader.Dispose();
            }
        }
    }
}
