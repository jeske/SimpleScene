// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace WavefrontOBJViewer
{
	// http://www.opentk.com/node/2142
	// http://msdn.microsoft.com/en-us/library/windows/desktop/dd373578(v=vs.85).aspx
	// http://www.opengl.org/discussion_boards/showthread.php/174851-Two-spot-lights
	// http://stackoverflow.com/questions/10768950/opentk-c-sharp-perspective-lighting-looks-glitchy

	public class SSLight : SSObjectBase
	{
		LightName glLightName;

		// TODO: should decouple the light-name from this class and auto-assign it based on the renderer

		public SSLight (LightName glLightName) : base() {
			this.glLightName = glLightName;
			this._pos = new Vector3 (0, 0, 1);
			this.updateMat ();
		}

		public void SetupLight_alt(ref SSRenderConfig renderConfig) {
			GL.MatrixMode (MatrixMode.Modelview);
			Matrix4 modelViewMat = this.worldMat * renderConfig.invCameraViewMat;
			GL.LoadMatrix (ref modelViewMat);

			GL.Enable (EnableCap.Lighting);
			GL.ShadeModel (ShadingModel.Smooth);

			GL.Light (glLightName, LightParameter.Position, new Vector4(this._pos,1.0f));

			GL.Enable (EnableCap.Light0 + (glLightName - LightName.Light0));

		}
		public void SetupLight(ref SSRenderConfig renderConfig) {
			GL.MatrixMode (MatrixMode.Modelview);
			Matrix4 modelViewMat = this.worldMat * renderConfig.invCameraViewMat;
			GL.LoadMatrix (ref modelViewMat);

			GL.Enable (EnableCap.Lighting);
			GL.ShadeModel (ShadingModel.Smooth);

			GL.Light (glLightName, LightParameter.Diffuse, new Vector4 (0.5f)); // diffuse color (R,G,B,A)
			GL.Light (glLightName, LightParameter.Ambient, new Vector4 (0.1f)); // ambient light color (R,G,B,A)
			GL.Light (glLightName, LightParameter.Specular, new Vector4 (0.5f)); // specular light color (R,G,B,A)

			// w=1.0 is a point light
			// w=0.0 is a directional light
			GL.Light (glLightName, LightParameter.Position, new Vector4(this._pos,1.0f));


			// GL.Light (glLightName, LightParameter.SpotDirection, new Vector4 (this._dir, 0.0f));

			GL.Enable (EnableCap.Light0 + (glLightName - LightName.Light0));


		}
	}
}

