using System;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace SimpleScene
{
    public class SSShadowMap
    {
        // http://www.opengl-tutorial.org/intermediate-tutorials/tutorial-16-shadow-mapping/

        // TODO: update shadowmap mvp when position of SSLight changes

        public const int c_maxNumberOfShadowMaps = 4;

        private readonly Matrix4 c_biasMatrix = new Matrix4(
            0.5f, 0.0f, 0.0f, 0.0f,
            0.0f, 0.5f, 0.0f, 0.0f,
            0.0f, 0.0f, 0.5f, 0.0f,
            0.5f, 0.5f, 0.5f, 1.0f
        );
        private const int c_texWidth = 1024;
        private const int c_texHeight = 1024;
        private static int s_numberOfShadowMaps = 0;

        private readonly int m_frameBufferID;
        private readonly int m_textureID;
        private readonly TextureUnit m_textureUnit;
        private bool m_isBound = false;

        public static int NumberOfShadowMaps { get { return s_numberOfShadowMaps; } }

        public Matrix4 DepthBiasVP {
            get { return m_viewMatrix * m_projMatrix * c_biasMatrix; }
        }

        public int TextureID {
            get { return m_textureID; }
        }

        public TextureUnit TextureUnit {
            get { return m_textureUnit; }
        }

        private Matrix4 m_projMatrix = Matrix4.CreateOrthographicOffCenter(-5000f, 5000f, -5000f, 5000f, 1f, 10000f);
        #if true
        private Matrix4 m_viewMatrix = Matrix4.LookAt(
            new Vector3 (0f, 0f, 4000f),
            new Vector3 (0f, 0f, -4000f),
            new Vector3 (0f, 1f, 0f));
        #else
        Matrix4 m_viewMatrix = Matrix4.CreateTranslation(0.0f, 0.0f, -4000f);
        #endif

        private Matrix4 m_projTemp;
        private Matrix4 m_viewTemp;

        public SSShadowMap(TextureUnit texUnit)
        {
            validateVersion();
            if (s_numberOfShadowMaps >= c_maxNumberOfShadowMaps) {
                throw new Exception ("Unsupported number of shadow maps: " 
                                     + (c_maxNumberOfShadowMaps + 1));
            }
            ++s_numberOfShadowMaps;

            m_frameBufferID = GL.Ext.GenFramebuffer();
            m_textureID = GL.GenTexture();

			// bind the texture and set it up...
            m_textureUnit = texUnit;
            BindShadowMapToTexture();
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

			// GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.GenerateMipmap, (float)1.0f);

            GL.TexImage2D(TextureTarget.Texture2D, 0,
                PixelInternalFormat.DepthComponent16,
                c_texWidth, c_texHeight, 0,
                PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);

			// bind the framebuffer and set it up..
			GL.Ext.BindFramebuffer(FramebufferTarget.Framebuffer, m_frameBufferID);
            GL.Ext.FramebufferTexture(FramebufferTarget.Framebuffer,
                                      FramebufferAttachment.DepthAttachment,
                                      m_textureID, 0);

			// turn off reading and writing to color data
			GL.DrawBuffer(DrawBufferMode.None); 
			GL.ReadBuffer(ReadBufferMode.None);
            //GL.Ext.FramebufferTexture(
            //	FramebufferTarget.Framebuffer,
            //    FramebufferAttachment.Depth,0,0);


			assertFramebufferOK();
        }

        ~SSShadowMap() {
            // DeleteData();
        }

        public void DeleteData() {
            // TODO: who/when calling this?
            GL.DeleteTexture(m_textureID);
            GL.Ext.DeleteFramebuffer(m_frameBufferID);
        }

        public void PrepareForRender(SSRenderConfig renderConfig) {
            m_projTemp = renderConfig.projectionMatrix;
            m_viewTemp = renderConfig.invCameraViewMat;

            GL.Ext.BindFramebuffer(FramebufferTarget.Framebuffer, m_frameBufferID);
            GL.Viewport(0, 0, c_texWidth, c_texHeight);


            renderConfig.projectionMatrix = m_projMatrix;
            renderConfig.invCameraViewMat = m_viewMatrix;
            renderConfig.drawingShadowMap = true;
            SSShaderProgram.DeactivateAll();

            GL.DrawBuffer(DrawBufferMode.None);
            GL.Clear(ClearBufferMask.DepthBufferBit);

			assertFramebufferOK();
		}

        public void FinishRender(SSRenderConfig renderConfig) {
			GL.Ext.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            renderConfig.drawingShadowMap = false;
            renderConfig.projectionMatrix = m_projTemp;
            renderConfig.invCameraViewMat = m_viewTemp;
        }

		public void BindShadowMapToTexture() {
			GL.ActiveTexture(TextureUnit.Texture4);
			GL.BindTexture(TextureTarget.Texture2D, m_textureID);
        }

		private void assertFramebufferOK() {
            var currCode = GL.Ext.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (currCode != FramebufferErrorCode.FramebufferComplete) {
                throw new Exception("Frame buffer operation failed: " + currCode.ToString());
			}
		}

        private void validateVersion() {
            string version_string = GL.GetString(StringName.Version);
            Version version = new Version(version_string[0], version_string[2]); // todo: improve
            Version versionRequired = new Version(2, 2);
            if (version < versionRequired) {
                throw new Exception("framebuffers not supported by the GL backend used");
            }
        }
    }
}

