using System;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace SimpleScene
{
    public class SSShadowMap
    {
        // http://www.opengl-tutorial.org/intermediate-tutorials/tutorial-16-shadow-mapping/

        private readonly Matrix4 c_biasMatrix = new Matrix4(
            0.5f, 0.0f, 0.0f, 0.0f,
            0.0f, 0.5f, 0.0f, 0.0f,
            0.0f, 0.0f, 0.5f, 0.0f,
            0.5f, 0.5f, 0.5f, 1.0f
        );
        private const int c_texWidth = 1024;
        private const int c_texHeight = 1024;

        private int m_frameBufferID = 0;
        private int m_textureID = 0;

        public SSShadowMap()
        {
            string version_string = GL.GetString(StringName.Version);
            int major = int.Parse(version_string.Split(' ')[0]);
            int minor = int.Parse(version_string.Split(' ')[1]);
            Version version = new Version(major, minor);
            Version versionRequired = new Version(3, 0);
            if (version < versionRequired) {
                throw new Exception("framebuffers not support by the GL backed used");
            }

			m_frameBufferID = GL.GenFramebuffer();
            m_textureID = GL.GenTexture();
            bind();
            GL.TexImage2D(TextureTarget.Texture2D, 0,
                          PixelInternalFormat.DepthComponent16,
                          c_texWidth, c_texHeight, 0,
						  PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, 
                            TextureParameterName.TextureMagFilter, 
                            (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, 
                            TextureParameterName.TextureMinFilter, 
                            (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, 
                            TextureParameterName.TextureWrapS, 
                            (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, 
                            TextureParameterName.TextureWrapT, 
                            (int)TextureWrapMode.ClampToEdge);
            GL.FramebufferTexture(FramebufferTarget.Framebuffer,
                                  FramebufferAttachment.DepthAttachment,
                                  m_textureID, 0);
			assertFramebufferOK();
        }

        ~SSShadowMap() {
            Unbind ();
            GL.DeleteFramebuffer(m_frameBufferID);
            GL.DeleteTexture(m_textureID);
        }

		public void PrepareForRender(ref SSRenderConfig renderConfig) {
			GL.DrawBuffer(DrawBufferMode.None);
			// TODO: configure shadow map shader
		}

		public void Unbind() {
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			GL.BindTexture(TextureTarget.Texture2D, 0);
		}

		private void bind() {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, m_frameBufferID);
            GL.BindTexture(TextureTarget.Texture2D, m_textureID);
        }

		private void assertFramebufferOK() {
			if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete) {
				throw new Exception("Frame buffer operation failed.");
			}
		}
    }
}

