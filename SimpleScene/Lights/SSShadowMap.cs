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
        private bool m_isBound = false;

        public SSShadowMap()
        {
            validateVersion();
            m_textureID = GL.GenTexture();
            m_frameBufferID = GL.GenFramebuffer();
            bind();
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
            GL.TexImage2D(TextureTarget.Texture2D, 0,
                PixelInternalFormat.DepthComponent16,
                c_texWidth, c_texHeight, 0,
                PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);

            GL.FramebufferTexture(FramebufferTarget.Framebuffer,
                                  FramebufferAttachment.DepthAttachment,
                                  m_textureID, 0);
			assertFramebufferOK();
            Unbind();
        }

        ~SSShadowMap() {
            Unbind ();
            // DeleteData();
        }

        public void DeleteData() {
            // TODO: who/when calling this?
            GL.DeleteTexture(m_textureID);
            GL.DeleteFramebuffer(m_frameBufferID);
        }

        public void PrepareForRender(ref SSRenderConfig renderConfig) {
			GL.DrawBuffer(DrawBufferMode.None);
            //TODO: configure shadow map shader
		}

		public void Unbind() {
            if (m_isBound) {
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                m_isBound = false;
            }
		}

		private void bind() {
            m_isBound = true;
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, m_frameBufferID);
            GL.BindTexture(TextureTarget.Texture2D, m_textureID);
        }

		private void assertFramebufferOK() {
            var currCode = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (currCode != FramebufferErrorCode.FramebufferComplete) {
                throw new Exception("Frame buffer operation failed: " + currCode.ToString());
			}
		}

        private void validateVersion() {
            string version_string = GL.GetString(StringName.Version);
            Version version = new Version(version_string[0], version_string[2]); // todo: improve
            Version versionRequired = new Version(3, 0);
            if (version < versionRequired) {
                throw new Exception("framebuffers not supported by the GL backend used");
            }
        }
    }
}

