using System;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;

namespace SimpleScene
{
    public abstract class SSLightBase
    {
        private const LightName c_firstName = LightName.Light0;

        public Vector4 Ambient = new Vector4(0.4f);
        public Vector4 Specular = new Vector4 (1.0f);
        public Vector4 Diffuse = new Vector4 (0.8f);

        protected SSShadowMapBase m_shadowMap = null;
        protected LightName m_lightName;


        public SSShadowMapBase ShadowMap {
            get { return m_shadowMap; }
            set {
                m_shadowMap = value;
                if (m_shadowMap != null) {
                    m_shadowMap.Light = this;
                }
            }
        }

        public SSLightBase(LightName lightName)
        {
            this.m_lightName = lightName;
        }

        ~SSLightBase() { 
            // this isn't valid, because the GL context can be gone before this destructor is called
            // If we're going to do this, we need to do something to find out that the owning GL context is still alive.

            // DisableLight ();
            // s_avaiableLightNames.Enqueue (m_lightName);
        }

        public virtual void SetupLight(ref SSRenderConfig renderConfig) {
            // we only use the invCameraViewMatrix, because we specify the pos/dir later below...
            Matrix4 modelViewMatrix = renderConfig.invCameraViewMatrix;
            GL.MatrixMode (MatrixMode.Modelview);
            GL.LoadMatrix (ref modelViewMatrix);

            GL.Enable (EnableCap.Lighting);
            GL.ShadeModel (ShadingModel.Smooth);

            GL.Light (m_lightName, LightParameter.Ambient, this.Ambient); // ambient light color (R,G,B,A)

            GL.Light (m_lightName, LightParameter.Diffuse, this.Diffuse); // diffuse color (R,G,B,A)

            GL.Light (m_lightName, LightParameter.Specular, this.Specular); // specular light color (R,G,B,A)

            int idx = m_lightName - c_firstName;
            GL.Enable (EnableCap.Light0 + idx);

            if (ShadowMap != null) {
                ShadowMap.PrepareForRead();
            }
        }

        public void DisableLight() {
            int idx = m_lightName - c_firstName;
            GL.Disable (EnableCap.Light0 + idx);

            if (ShadowMap != null) {
                ShadowMap.FinishRead();
            }
        }

    }
}

