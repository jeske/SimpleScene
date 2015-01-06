// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;

namespace SimpleScene
{
	// http://www.opentk.com/node/2142
	// http://msdn.microsoft.com/en-us/library/windows/desktop/dd373578(v=vs.85).aspx
	// http://www.opengl.org/discussion_boards/showthread.php/174851-Two-spot-lights
	// http://stackoverflow.com/questions/10768950/opentk-c-sharp-perspective-lighting-looks-glitchy

    // TODO: update shadowmap mvp when position of SSLight changes

	public class SSLight : SSObjectBase
	{
        // Light type is somewhat of a placeholder for now.
        // Currently need a way to find objects "between" AABB and the light
        public enum LightType { Directional, PointSource };        

        private const LightName c_firstName = LightName.Light0;

		public Vector4 Ambient = new Vector4(0.4f);
		public Vector4 Specular = new Vector4 (1.0f);
		public Vector4 Diffuse = new Vector4 (0.8f);
        public LightType Type = LightType.Directional;

        private SSShadowMapBase m_shadowMap = null;

        public Vector3 Direction {
            get { return Pos; }
            set { Pos = value; }
        }

        public SSShadowMapBase ShadowMap {
            get { return m_shadowMap; }
            set {
                m_shadowMap = value;
                if (m_shadowMap != null) {
                    m_shadowMap.Light = this;
                }
            }
        }

		protected LightName m_lightName;

        public SSLight(LightName lightName)
        {
            this.m_lightName = lightName;
            this._pos = new Vector3 (0f, 0f, 1f);
			this.calcMatFromState ();
		}

		~SSLight() { 
            // this isn't valid, because the GL context can be gone before this destructor is called
            // If we're going to do this, we need to do something to find out that the owning GL context is still alive.
                    
			// DisableLight ();
			// s_avaiableLightNames.Enqueue (m_lightName);
		}

		public void SetupLight_alt(ref SSRenderConfig renderConfig) {
			GL.MatrixMode (MatrixMode.Modelview);			
			GL.LoadMatrix (ref this.worldMat);

			GL.Enable (EnableCap.Lighting);
			GL.ShadeModel (ShadingModel.Smooth);

			GL.Light (m_lightName, LightParameter.Position, new Vector4(this._pos,1.0f));

			int idx = m_lightName - c_firstName;
			GL.Enable (EnableCap.Light0 + idx);

		}
		public void SetupLight(ref SSRenderConfig renderConfig) {
			// we only use the invCameraViewMatrix, because we specify the pos/dir later below...
			Matrix4 modelViewMatrix = renderConfig.invCameraViewMat;
			GL.MatrixMode (MatrixMode.Modelview);
			GL.LoadMatrix (ref modelViewMatrix);

			GL.Enable (EnableCap.Lighting);
			GL.ShadeModel (ShadingModel.Smooth);

			GL.Light (m_lightName, LightParameter.Ambient, this.Ambient); // ambient light color (R,G,B,A)

			GL.Light (m_lightName, LightParameter.Diffuse, this.Diffuse); // diffuse color (R,G,B,A)

			GL.Light (m_lightName, LightParameter.Specular, this.Specular); // specular light color (R,G,B,A)

            float w = (Type == LightType.Directional ? 0.0f : 1.0f);
            GL.Light (m_lightName, LightParameter.Position, new Vector4(Pos, w)); 

			int idx = m_lightName - c_firstName;
			GL.Enable (EnableCap.Light0 + idx);
		}

		public void DisableLight() {
			int idx = m_lightName - c_firstName;
			GL.Disable (EnableCap.Light0 + idx);
		}
	}
}

