using Silk.NET.OpenGL;
using System.Numerics;

namespace PETRenderer
{
    public class MeshNode : Node
    {
        private GL _gl;
        public Model Model { get; private set; }
        public Texture Texture { get; private set; }
        public Texture? NormalMap { get; private set; }

        public float NormalStrength { get; set; } = 1.0f;

        public MeshNode(GL gl, Model model, Texture texture, string name = "MeshNode")
        {
            _gl = gl;
            Model = model;
            Texture = texture;
            Name = name;
        }

        public MeshNode(GL gl, Model model, Texture texture, Texture normalMap, string name = "MeshNode") {
            _gl = gl;
            Model = model;
            Texture = texture;
            NormalMap = normalMap;
            Name = name;
        }

        public override unsafe void Render(Shader shader, Matrix4x4 view, Matrix4x4 projection) 
        {


            foreach (var mesh in Model.Meshes)
            {
                mesh.Bind();
                shader.Use();
                Texture.Bind(TextureUnit.Texture0);
                shader.SetUniform("uTexture0", 0);

                if (NormalMap != null) {
                    NormalMap.Bind(TextureUnit.Texture1);
                    shader.SetUniform("uNormalMap", 1);
                    shader.SetUniform("uHasNormalMap", 1);
                    shader.SetUniform("uNormalStrength", NormalStrength);
                } else {
                    Texture.Bind(TextureUnit.Texture1);
                    shader.SetUniform("uNormalMap", 1);
                    shader.SetUniform("uHasNormalMap", 0);
                    shader.SetUniform("uNormalStrength", 0.0f);
                }
                shader.SetUniform("uModel", WorldMatrix);
                shader.SetUniform("uView", view);
                shader.SetUniform("uProjection", projection);
                shader.SetUniform("uLightDir", new Vector3(-1.0f, -1.0f, -0.5f));
                shader.SetUniform("uAmbient", 0.15f);

                _gl.DrawElements(PrimitiveType.Triangles,
                    (uint)mesh.Indices.Length,
                    DrawElementsType.UnsignedInt, null);
            }

            base.Render(shader, view, projection);
        }

        public override void Dispose()
        {
            Model.Dispose();
            Texture.Dispose();
            base.Dispose();
        }
    }
}