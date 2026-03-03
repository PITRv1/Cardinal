using System;
using Avalonia.Controls;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Silk.NET.OpenGL;

namespace Cardinal;

public class MyGl3DControl : OpenGlControlBase
{
    private GL? _gl;
    private uint _vao, _vbo, _ebo, _shaderProgram;
    private float _rotation = 0f;


    protected override void OnOpenGlInit(GlInterface glInterface)
    {
        _gl = GL.GetApi(proc => glInterface.GetProcAddress(proc));
        SetupGeometry();
        SetupShaders();
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        _gl!.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)fb);
        _gl.Viewport(0, 0, (uint)Bounds.Width, (uint)Bounds.Height);
        _gl.ClearColor(0.1f, 1.0f, 0.15f, 1f);
        _gl.Clear(ClearBufferMask.ColorBufferBit);

        _rotation += 0.02f;

        _gl.UseProgram(_shaderProgram);
        _gl.Uniform1(_gl.GetUniformLocation(_shaderProgram, "uRotation"), _rotation);
        _gl.BindVertexArray(_vao);
        unsafe {
            _gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, (void*)0);
        }
        RequestNextFrameRendering();
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        _gl?.DeleteVertexArray(_vao);
        _gl?.DeleteBuffer(_vbo);
        _gl?.DeleteProgram(_shaderProgram);
    }

    private void SetupGeometry()
    {
        // X, Y, R, G, B
        float[] vertices = {
            -0.5f,  0.5f,   1.0f, 0.2f, 0.2f,  // top-left
             0.5f,  0.5f,   0.2f, 1.0f, 0.4f,  // top-right
            -0.5f, -0.5f,   0.2f, 0.5f, 1.0f,  // bottom-left
             0.5f, -0.5f,   1.0f, 1.0f, 0.2f,  // bottom-right
        };

        uint[] indices = { 0, 1, 2, 1, 2, 3 };

        _vao = _gl!.GenVertexArray();
        _vbo = _gl.GenBuffer();
        _ebo = _gl.GenBuffer();

        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        unsafe {
            fixed (float* ptr = vertices)
                _gl.BufferData(BufferTargetARB.ArrayBuffer,
                    (nuint)(vertices.Length * sizeof(float)), ptr,
                    BufferUsageARB.StaticDraw);
        }

        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        unsafe {
            fixed (uint* ptr = indices)
                _gl.BufferData(BufferTargetARB.ElementArrayBuffer,
                    (nuint)(indices.Length * sizeof(float)), ptr,
                    BufferUsageARB.StaticDraw);
        }


        unsafe { 
            uint stride = 5 * sizeof(float);
            _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, (void*)0);
            _gl.EnableVertexAttribArray(0);
            _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, (void*)(2 * sizeof(float)));
            _gl.EnableVertexAttribArray(1);
        }
    }

    private void SetupShaders()
    {

    const string vert =
    "#version 300 es\n" +
    "precision mediump float;\n" +
    "layout(location = 0) in vec2 aPos;\n" +
    "layout(location = 1) in vec3 aColor;\n" +
    "out vec3 vColor;\n" +
    "uniform float uRotation;\n" +
    "void main() {\n" +
    "    float c = cos(uRotation);\n" +
    "    float s = sin(uRotation);\n" +
    "    gl_Position = vec4(\n" +
    "        aPos.x * c - aPos.y * s,\n" +
    "        aPos.x * s + aPos.y * c,\n" +
    "        0.0, 1.0);\n" +
    "    vColor = aColor;\n" +
    "}\n";

    const string frag =
    "#version 300 es\n" +
    "precision mediump float;\n" +
    "in vec3 vColor;\n" +
    "out vec4 FragColor;\n" +
    "void main() {\n" +
    "    FragColor = vec4(vColor, 1.0);\n" +
    "}\n";

        var vs = _gl!.CreateShader(ShaderType.VertexShader);
        _gl.ShaderSource(vs, vert);
        _gl.CompileShader(vs);
        _gl.GetShader(vs, ShaderParameterName.CompileStatus, out int vsStatus);

        var fs = _gl.CreateShader(ShaderType.FragmentShader);
        _gl.ShaderSource(fs, frag);
        _gl.CompileShader(fs);
        _gl.GetShader(fs, ShaderParameterName.CompileStatus, out int fsStatus);

        _shaderProgram = _gl.CreateProgram();
        _gl.AttachShader(_shaderProgram, vs);
        _gl.AttachShader(_shaderProgram, fs);
        _gl.LinkProgram(_shaderProgram);
        _gl.GetProgram(_shaderProgram, ProgramPropertyARB.LinkStatus, out int linkStatus);

        _gl.DeleteShader(vs);
        _gl.DeleteShader(fs);
    }
}