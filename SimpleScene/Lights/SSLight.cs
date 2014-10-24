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

	public class SSLight : SSObjectBase
	{
		private const LightName c_firstNameIdx = LightName.Light0;
		private const LightName c_lastNameIdx = LightName.Light7;
		static private readonly Queue<LightName> s_avaiableLightNames = new Queue<LightName>();

		public Vector4 Ambient = new Vector4(0.4f);
		public Vector4 Specular = new Vector4 (1.0f);
		public Vector4 Diffuse = new Vector4 (0.8f);

		protected LightName m_lightName;

		static SSLight() {
			for (LightName l = c_firstNameIdx; l <= c_lastNameIdx; ++l) {
				s_avaiableLightNames.Enqueue(l);
			}
		}

		public SSLight () : base() {
			if (s_avaiableLightNames.Count == 0) {
				string msg = "Cannot support this many lights.";
				throw new Exception (msg);
			}

			this.m_lightName = s_avaiableLightNames.Dequeue();
			this._pos = new Vector3 (0, 0, 1);
			this.calcMatFromState ();
		}

		~SSLight() {
			DisableLight ();
			s_avaiableLightNames.Enqueue (m_lightName);
		}

		public void SetupLight_alt(ref SSRenderConfig renderConfig) {
			GL.MatrixMode (MatrixMode.Modelview);			
			GL.LoadMatrix (ref this.worldMat);

			GL.Enable (EnableCap.Lighting);
			GL.ShadeModel (ShadingModel.Smooth);

			GL.Light (m_lightName, LightParameter.Position, new Vector4(this._pos,1.0f));

			int idx = m_lightName - c_firstNameIdx;
			GL.Enable (EnableCap.Light0 + idx);

		}
		public void SetupLight(ref SSRenderConfig renderConfig) {
			Matrix4 modelViewMatrix = this.worldMat * renderConfig.invCameraViewMat;
			GL.MatrixMode (MatrixMode.Modelview);
			GL.LoadMatrix (ref modelViewMatrix);

			GL.Enable (EnableCap.Lighting);
			GL.ShadeModel (ShadingModel.Smooth);

			GL.Light (m_lightName, LightParameter.Ambient, this.Ambient); // ambient light color (R,G,B,A)

			GL.Light (m_lightName, LightParameter.Diffuse, this.Diffuse); // diffuse color (R,G,B,A)

			GL.Light (m_lightName, LightParameter.Specular, this.Specular); // specular light color (R,G,B,A)

			// w=1.0 is a point light
			// w=0.0 is a directional light
			// we put it at the origin because it is transformed by the model view matrix (which already has our position)
			GL.Light (m_lightName, LightParameter.Position, new Vector4(0,0,0,1.0f)); 

			int idx = m_lightName - c_firstNameIdx;
			GL.Enable (EnableCap.Light0 + idx);
		}

		public void DisableLight() {
			int idx = m_lightName - c_firstNameIdx;
			GL.Disable (EnableCap.Light0 + idx);
		}
	}
}

