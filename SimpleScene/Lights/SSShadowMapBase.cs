using System;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System.Collections.Generic;

namespace SimpleScene
{
    public abstract class SSShadowMapBase
    {
        public const int c_maxNumberOfShadowMaps = 1;

        private static int s_numberOfShadowMaps = 0;

        private readonly int m_frameBufferID;
        private readonly int m_textureID;
        private readonly TextureUnit m_textureUnit;

        protected SSLight m_light;

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

        public SSLight Light {
            set { m_light = value; }
        }

        public SSShadowMapBase (TextureUnit texUnit)
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

            GL.TexImage2D(TextureTarget.Texture2D, 0,
                PixelInternalFormat.DepthComponent16,
                texWidth(), texHeight(), 0,
                PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);

            GL.Ext.BindFramebuffer(FramebufferTarget.Framebuffer, m_frameBufferID);
            GL.Ext.FramebufferTexture(FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthAttachment,
                m_textureID, 0);

            assertFramebufferOK();
            unbindFramebuffer();
        }

        ~SSShadowMapBase() {
            // DeleteData();
        }

        public void DeleteData() {
            // TODO: who/when calling this?
            GL.DeleteTexture(m_textureID);
            GL.Ext.DeleteFramebuffer(m_frameBufferID);
        }

        public void FinishRender(SSRenderConfig renderConfig) {
            unbindFramebuffer();
            renderConfig.drawingShadowMap = false;
        }

        public abstract void PrepareForRender (
            SSRenderConfig renderConfig,
            List<SSObject> objects,
            float fov, float aspect, float nearZ, float farZ);

        protected void unbindFramebuffer() {
            GL.Ext.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        protected void PrepareForRenderBase(SSRenderConfig renderConfig,
                                            List<SSObject> objects) {
            GL.Ext.BindFramebuffer(FramebufferTarget.Framebuffer, m_frameBufferID);
            GL.Viewport(0, 0, texWidth(), texHeight());

            // turn off reading and writing to color data
            GL.DrawBuffer(DrawBufferMode.None); 
            GL.ReadBuffer(ReadBufferMode.None);
            GL.Clear(ClearBufferMask.DepthBufferBit);
            assertFramebufferOK();

            renderConfig.drawingShadowMap = true;
        }

        private void BindShadowMapToTexture() {
            GL.ActiveTexture(m_textureUnit);
            GL.BindTexture(TextureTarget.Texture2D, m_textureID);
        }

        protected abstract int texWidth();

        protected abstract int texHeight();

        protected void assertFramebufferOK() {
            var currCode = GL.Ext.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (currCode != FramebufferErrorCode.FramebufferComplete) {
                throw new Exception("Frame buffer operation failed: " + currCode.ToString());
            }
        }

        protected void validateVersion() {
            string version_string = GL.GetString(StringName.Version);
            Version version = new Version(version_string[0], version_string[2]); // todo: improve
            Version versionRequired = new Version(2, 2);
            if (version < versionRequired) {
                throw new Exception("framebuffers not supported by the GL backend used");
            }
        }
    }
}

