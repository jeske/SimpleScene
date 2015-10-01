using System;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;

namespace SimpleScene
{
    public abstract class SSLightBase
    {
        protected const LightName _firstName = LightName.Light0;

        public Vector4 Ambient = new Vector4(0.4f);
        public Vector4 Specular = new Vector4 (1.0f);
        public Vector4 Diffuse = new Vector4 (0.8f);

        protected SSShadowMapBase _shadowMap = null;
        protected LightName _lightName;


        public SSShadowMapBase ShadowMap {
            get { return _shadowMap; }
            set {
                _shadowMap = value;
                if (_shadowMap != null) {
                    _shadowMap.Light = this;
                }
            }
        }

        public SSLightBase(LightName lightName)
        {
            this._lightName = lightName;
        }

        ~SSLightBase() { 
            // this isn't valid, because the GL context can be gone before this destructor is called
            // If we're going to do this, we need to do something to find out that the owning GL context is still alive.

            // DisableLight ();
            // s_avaiableLightNames.Enqueue (m_lightName);
        }

        public virtual void setupLight(SSRenderConfig renderConfig) {
            // we only use the invCameraViewMatrix, because we specify the pos/dir later below...
            Matrix4 modelViewMatrix = renderConfig.invCameraViewMatrix;
            GL.MatrixMode (MatrixMode.Modelview);
            GL.LoadMatrix (ref modelViewMatrix);

            GL.Light (_lightName, LightParameter.Ambient, this.Ambient); // ambient light color (R,G,B,A)

            GL.Light (_lightName, LightParameter.Diffuse, this.Diffuse); // diffuse color (R,G,B,A)

            GL.Light (_lightName, LightParameter.Specular, this.Specular); // specular light color (R,G,B,A)

            int idx = _lightName - _firstName;
            GL.Enable (EnableCap.Light0 + idx);

            if (ShadowMap != null) {
                ShadowMap.PrepareForRead();
            }
        }

        public virtual void DisableLight(SSRenderConfig renderConfig) {
            int idx = _lightName - _firstName;
            GL.Disable (EnableCap.Light0 + idx);

            if (ShadowMap != null) {
                ShadowMap.FinishRead();
            }
        }
    }
}

