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

    public class SSDirectionalLight : SSLightBase
	{
        public Vector3 Direction;

        public SSDirectionalLight(LightName lightName)
            : base(lightName)
        {
            this.Direction = new Vector3 (0f, 0f, 1f);
		}

        public override void setupLight(SSRenderConfig renderConfig) {
            base.setupLight(renderConfig);
            GL.Light (_lightName, LightParameter.Position, new Vector4(Direction, 0f));

            int dirLightIndex = _lightName - _firstName;
            if (renderConfig.mainShader != null) {
                renderConfig.mainShader.Activate();
                renderConfig.mainShader.UniDirectionalLightIndex = dirLightIndex;
            }
            if (renderConfig.instanceShader != null) {
                renderConfig.instanceShader.Activate();
                renderConfig.instanceShader.UniDirectionalLightIndex = dirLightIndex;
            }
        }

        public override void DisableLight (SSRenderConfig renderConfig)
        {
            base.DisableLight(renderConfig);

            if (renderConfig.mainShader != null) {
                renderConfig.mainShader.Activate();
                renderConfig.mainShader.UniDirectionalLightIndex = -1;
            }
            if (renderConfig.instanceShader != null) {
                renderConfig.instanceShader.Activate();
                renderConfig.instanceShader.UniDirectionalLightIndex = -1;
            }
        }

        #if false
        public void SetupLight_alt(ref SSRenderConfig renderConfig) {
        GL.MatrixMode (MatrixMode.Modelview);           
        GL.LoadMatrix (ref this.worldMat);

        GL.Enable (EnableCap.Lighting);
        GL.ShadeModel (ShadingModel.Smooth);

        GL.Light (m_lightName, LightParameter.Position, new Vector4(this._pos,1.0f));

        int idx = m_lightName - c_firstName;
        GL.Enable (EnableCap.Light0 + idx);
        }
        #endif
	}
}

