using System;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System.Collections.Generic;

namespace SimpleScene
{
    public class SSShadowMap
    {
        // http://www.opengl-tutorial.org/intermediate-tutorials/tutorial-16-shadow-mapping/

        public const int c_maxNumberOfShadowMaps = 1;
        public const int c_numberOfSplits = 4;

        private static readonly Matrix4 c_biasMatrix = new Matrix4(
            0.5f, 0.0f, 0.0f, 0.0f,
            0.0f, 0.5f, 0.0f, 0.0f,
            0.0f, 0.0f, 0.5f, 0.0f,
            0.5f, 0.5f, 0.5f, 1.0f
        );

        private const int c_texWidth = 2048;
        private const int c_texHeight = 2048;
        private static int s_numberOfShadowMaps = 0;

        private readonly int m_frameBufferID;
        private readonly int m_textureID;
        private readonly TextureUnit m_textureUnit;
        protected readonly SSLight m_light;

        private Matrix4[] m_viewProjMatrices = new Matrix4[c_numberOfSplits];
        private float[] m_viewSplits = new float[c_numberOfSplits];

        public static int NumberOfShadowMaps { get { return s_numberOfShadowMaps; } }

        public Matrix4[] ViewProjectionMatrices {
            get { return m_viewProjMatrices; }
        }

        public Matrix4[] BiasViewProjectionMatrices {
            get {
                Matrix4[] ret = new Matrix4[c_numberOfSplits];
                for (int i = 0; i < c_numberOfSplits; ++i) {
                    ret [i] = m_viewProjMatrices[i] * c_biasMatrix;
                }
                return ret;
            }
        }

        public int TextureID {
            get { return m_textureID; }
        }

        public TextureUnit TextureUnit {
            get { return m_textureUnit; }
        }

        public float[] ViewSplits {
            get { return m_viewSplits; } 
        }

        public SSShadowMap(SSLight light, TextureUnit texUnit)
        {
            validateVersion();
            if (s_numberOfShadowMaps >= c_maxNumberOfShadowMaps) {
                throw new Exception ("Unsupported number of shadow maps: " 
                                     + (c_maxNumberOfShadowMaps + 1));
            }
            ++s_numberOfShadowMaps;

            m_light = light;
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

        public void PrepareForRender(SSRenderConfig renderConfig, 
                                     List<SSObject> objects,
                                     float fov, float aspect, float nearZ, float farZ) {
            GL.Ext.BindFramebuffer(FramebufferTarget.Framebuffer, m_frameBufferID);
            GL.Viewport(0, 0, c_texWidth, c_texHeight);

            #if true
            Util3d.Projections.ParallelShadowmapProjections(
                objects, m_light,
                renderConfig.invCameraViewMat,
                renderConfig.projectionMatrix,
                fov, aspect, nearZ, farZ,
                c_numberOfSplits, m_viewProjMatrices, m_viewSplits);
            #else
            Matrix4 shadowView, shadowProj;
            Util3d.Projections.SimpleShadowmapProjection(
                objects, m_light, 
                renderConfig.invCameraViewMat, renderConfig.projectionMatrix,
                out shadowView, out shadowProj);
            m_viewProjMatrices[0] = shadowView * shadowProj;
            #endif

            renderConfig.drawingShadowMap = true;
            renderConfig.ShadowmapShader.Activate();
            renderConfig.ShadowmapShader.UpdateShadowMapVPs(m_viewProjMatrices);
            GL.DrawBuffer(DrawBufferMode.None);
            GL.Clear(ClearBufferMask.DepthBufferBit);

			assertFramebufferOK();
		}

        public void FinishRender(SSRenderConfig renderConfig) {
			GL.Ext.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            renderConfig.drawingShadowMap = false;
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

