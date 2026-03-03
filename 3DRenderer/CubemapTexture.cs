using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;

namespace PETRenderer
{
    public class CubemapTexture : IDisposable
    {
        private uint _handle;
        private GL _gl;

        public unsafe CubemapTexture(GL gl, string[] facePaths) {
            _gl = gl;
            _handle = _gl.GenTexture();
            _gl.BindTexture(TextureTarget.TextureCubeMap, _handle);

            for (int i = 0; i < facePaths.Length; i++) {
                using var img = Image.Load<Rgba32>(facePaths[i]);

                gl.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0, InternalFormat.Rgba8, (uint)img.Width, (uint)img.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);

                img.ProcessPixelRows(accessor => {
                    for (int y = 0; y < accessor.Height; y++)
                        fixed (void* data = accessor.GetRowSpan(y))
                            gl.TexSubImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0, 0, y,
                                (uint)accessor.Width, 1, PixelFormat.Rgba, PixelType.UnsignedByte, data);
                });
            }

            gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
            gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
            gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
            gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)GLEnum.ClampToEdge);
        }

        public void Bind() {
            _gl.BindTexture(TextureTarget.TextureCubeMap, _handle);
        }


        public void Dispose() {
            _gl.DeleteTexture(_handle);
        }
    }
}
