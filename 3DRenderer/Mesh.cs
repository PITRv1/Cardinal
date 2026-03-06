// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;

namespace PETRenderer
{
    public class Mesh : IDisposable
    {
        public Mesh(GL gl, float[] vertices, uint[] indices, List<Texture> textures) {
            GL = gl;
            Vertices = vertices;
            Indices = indices;
            Textures = textures;
            SetupMesh();
        }

        public float[] Vertices { get; private set; }
        public uint[] Indices { get; private set; }
        public IReadOnlyList<Texture> Textures { get; private set; }
        public VertexArrayObject<float, uint> VAO { get; set; }
        public BufferObject<float> VBO { get; set; }
        public BufferObject<uint> EBO { get; set; }
        public GL GL { get; }

        public unsafe void SetupMesh() {
            EBO = new BufferObject<uint>(GL, Indices, BufferTargetARB.ElementArrayBuffer);
            VBO = new BufferObject<float>(GL, Vertices, BufferTargetARB.ArrayBuffer);
            VAO = new VertexArrayObject<float, uint>(GL, VBO, EBO);
            VAO.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 14, 0);  // position
            VAO.VertexAttributePointer(1, 3, VertexAttribPointerType.Float, 14, 3);  // normal
            VAO.VertexAttributePointer(2, 3, VertexAttribPointerType.Float, 14, 6);  // tangent
            VAO.VertexAttributePointer(3, 3, VertexAttribPointerType.Float, 14, 9);  // bitangent
            VAO.VertexAttributePointer(4, 2, VertexAttribPointerType.Float, 14, 12); // uv

        }

        public void Bind() {
            VAO.Bind();
        }

        public void Dispose() {
            Textures = null;
            VAO.Dispose();
            VBO.Dispose();
            EBO.Dispose();
        }
    }
}
