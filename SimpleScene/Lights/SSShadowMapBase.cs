using System;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System.Collections.Generic;
using SimpleScene.Util3d;
using SimpleScene.Util;

namespace SimpleScene
{
    public abstract class SSShadowMapBase
    {
        public const int c_maxNumberOfShadowMaps = 1;

        private static int s_numberOfShadowMaps = 0;

        public SSFrustumCuller FrustumCuller = null;

        private readonly int m_frameBufferID;
        private readonly int m_textureID;
        private readonly TextureUnit m_textureUnit;
        private readonly int m_textureWidth;
        private readonly int m_textureHeight;

        protected SSLightBase m_light;
		protected bool m_isValid = false;

        /// <summary>
        /// Used for lookups into the a texture previous used by the framebuffer
        /// </summary>
        protected readonly Matrix4 c_biasMatrix = new Matrix4(
            0.5f, 0.0f, 0.0f, 0.0f,
            0.0f, 0.5f, 0.0f, 0.0f,
            0.0f, 0.0f, 0.5f, 0.0f,
            0.5f, 0.5f, 0.5f, 1.0f
        );

        public int TextureID {
            get { return m_textureID; }
        }

        public TextureUnit TextureUnit {
            get { return m_textureUnit; }
        }

		public bool IsValid {
			get { return m_isValid; }
		}

        public SSLightBase Light {
            set { m_light = value; }
        }

        public SSShadowMapBase (TextureUnit texUnit, int textureWidth, int textureHeight)
        {
			if (!OpenTKHelper.areFramebuffersSupported()) {
				m_isValid = false;
				return;
			}
            #if false
            if (s_numberOfShadowMaps >= c_maxNumberOfShadowMaps) {
                throw new Exception ("Unsupported number of shadow maps: " 
                    + (c_maxNumberOfShadowMaps + 1));
            }
            #endif
            ++s_numberOfShadowMaps;

			m_frameBufferID = GL.Ext.GenFramebuffer();
			if (m_frameBufferID < 0) {
				throw new Exception ("gen fb failed");
			}
            m_textureID = GL.GenTexture();
            m_textureWidth = textureWidth;
            m_textureHeight = textureHeight;

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

            GL.TexImage2D(TextureTarget.Texture2D, 0,
				PixelInternalFormat.DepthComponent32f,
                m_textureWidth, m_textureHeight, 0,
				PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
			// done creating texture, unbind
			GL.BindTexture (TextureTarget.Texture2D, 0); 


			// ----------------------------
			// now bind the texture to framebuffer..
			GL.Ext.BindFramebuffer(FramebufferTarget.DrawFramebuffer, m_frameBufferID);
			GL.Ext.BindFramebuffer(FramebufferTarget.ReadFramebuffer, m_frameBufferID);

			GL.Ext.FramebufferTexture2D(FramebufferTarget.DrawFramebuffer,FramebufferAttachment.DepthAttachment,
				TextureTarget.Texture2D, m_textureID, 0);
			//GL.Ext.FramebufferTexture (FramebufferTarget.FramebufferExt, FramebufferAttachment.Color,
			//(int)All.None, 0);

			GL.Viewport (0, 0, m_textureWidth, m_textureHeight);
			GL.DrawBuffer (DrawBufferMode.None);
			GL.ReadBuffer (ReadBufferMode.None);

			if (!assertFramebufferOK (FramebufferTarget.DrawFramebuffer)) {
				throw new Exception ("failed to create-and-bind shadowmap FBO");
			}

			// leave in a sane state...
            unbindFramebuffer();
			GL.ActiveTexture(TextureUnit.Texture0);
			if (!assertFramebufferOK (FramebufferTarget.DrawFramebuffer)) {
				throw new Exception ("failed to ubind shadowmap FBO");
			}        
		}

        ~SSShadowMapBase() {
            //DeleteData();
            --s_numberOfShadowMaps;
        }

        public void DeleteData() {
            // TODO: who/when calling this?
            GL.DeleteTexture(m_textureID);
            GL.Ext.DeleteFramebuffer(m_frameBufferID);
        }

        public virtual void FinishRender(SSRenderConfig renderConfig) {
            unbindFramebuffer();
            renderConfig.drawingShadowMap = false;
        }

        public abstract void PrepareForRender (
            SSRenderConfig renderConfig,
            List<SSObject> objects,
            float fov, float aspect, float nearZ, float farZ);

        public void PrepareForRead()
        {
            GL.ActiveTexture(TextureUnit);
            GL.Enable(EnableCap.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, TextureID);
        }

        public void FinishRead()
        {
            GL.ActiveTexture(TextureUnit);
            GL.Disable(EnableCap.Texture2D);
        }

        protected void unbindFramebuffer() {
			GL.Ext.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
			GL.Ext.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);

        }

        protected virtual void PrepareForRenderBase(SSRenderConfig renderConfig,
                                                    List<SSObject> objects) {
			GL.Ext.BindFramebuffer(FramebufferTarget.DrawFramebuffer, m_frameBufferID);
			// GL.Ext.FramebufferTexture2D(FramebufferTarget.FramebufferExt,FramebufferAttachment.DepthAttachment,
			//	TextureTarget.Texture2D, m_textureID, 0);

            GL.Viewport(0, 0, m_textureWidth, m_textureHeight);

            // turn off reading and writing to color data
            GL.DrawBuffer(DrawBufferMode.None);     

			if (!assertFramebufferOK (FramebufferTarget.DrawFramebuffer)) {
				throw new Exception ("failed to bind framebuffer for drawing");
			}

			GL.Clear(ClearBufferMask.DepthBufferBit);

			configureDrawShader (renderConfig, renderConfig.mainShader);
			configureDrawShader (renderConfig, renderConfig.instanceShader);

            renderConfig.drawingShadowMap = true;
        }

		private void configureDrawShader(SSRenderConfig renderConfig, SSMainShaderProgram pgm)
		{
			if (pgm != null) {
				pgm.Activate();
				pgm.UniPoissonSamplingEnabled = renderConfig.usePoissonSampling;
				if (renderConfig.usePoissonSampling) {
					pgm.UniNumPoissonSamples = renderConfig.numPoissonSamples;
				}
			}
		}

        private void BindShadowMapToTexture() {
            GL.ActiveTexture(m_textureUnit);
            GL.BindTexture(TextureTarget.Texture2D, m_textureID);
        }

		protected bool assertFramebufferOK(FramebufferTarget target) {
            var currCode = GL.Ext.CheckFramebufferStatus(target);
			if (currCode != FramebufferErrorCode.FramebufferComplete) {
				Console.WriteLine ("Frame buffer operation failed: " + currCode.ToString ());
				m_isValid = false;
				DeleteData ();
			} else {
				m_isValid = true;
			}
			return m_isValid;
        }

        protected static void viewProjFromLightAlignedBB(ref SSAABB bb, 
                                                         ref Matrix4 lightTransform, 
                                                         ref Vector3 lightY,
                                                         out Matrix4 viewMatrix,
                                                         out Matrix4 projMatrix)
        {
            // Use center of AABB in regular coordinates to get the view matrix
            Vector3 targetLightSpace = bb.Center();                
            Vector3 eyeLightSpace = new Vector3 (targetLightSpace.X, 
                targetLightSpace.Y, 
                bb.Min.Z);
            Vector3 viewTarget = Vector3.Transform(targetLightSpace, lightTransform.Inverted()); 
            Vector3 viewEye = Vector3.Transform(eyeLightSpace, lightTransform.Inverted());
            Vector3 viewUp = lightY;
            viewMatrix = Matrix4.LookAt(viewEye, viewTarget, viewUp);

            // Finish the projection matrix
            Vector3 diff = bb.Diff();
            float width, height, nearZ, farZ;
            width = diff.X;
            height = diff.Y;
            nearZ = 1f;
            farZ = 1f + diff.Z;
            projMatrix = Matrix4.CreateOrthographic(width, height, nearZ, farZ);
        }
    }
}

