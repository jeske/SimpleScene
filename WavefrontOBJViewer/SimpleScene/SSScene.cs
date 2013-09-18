// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace WavefrontOBJViewer
{
	public class SSRenderConfig {
		public bool drawGLSL = true;
		public bool drawWireframes = true;
	}

	public sealed class SSScene
	{
		public ICollection<SSObject> objects = new List<SSObject>();
		public ICollection<SSLight> lights = new List<SSLight> ();

		public SSCamera activeCamera;

		public readonly SSRenderConfig renderConfig = new SSRenderConfig ();

		public void Update() {
			// update all objects.. TODO: add elapsed time since last update..
			foreach (var obj in objects) {
				obj.Update ();
			}
		}

		public void SetupLights(Matrix4 cameraViewMat) {
			GL.Enable (EnableCap.Lighting);
			foreach (var light in lights) {
				GL.MatrixMode (MatrixMode.Modelview);
				Matrix4 modelViewMat = cameraViewMat * light.worldMat;
				GL.LoadMatrix (ref modelViewMat);

				light.SetupLight ();
			}
		}

		public void Render(Matrix4 cameraViewMat) {
			foreach (var obj in objects) {
				// compute and set the modelView matrix, by combining the cameraViewMat
				// with the object's world matrix
				//    ... http://www.songho.ca/opengl/gl_transform.html
				//    ... http://stackoverflow.com/questions/5798226/3d-graphics-processing-how-to-calculate-modelview-matrix
				
				GL.MatrixMode(MatrixMode.Modelview);
				Matrix4 modelViewMat = cameraViewMat * obj.worldMat;
				GL.LoadMatrix(ref modelViewMat);
				
				// now render the object
				obj.Render (renderConfig);
			}
		}

		public void addObject(SSObject obj) {
			objects.Add (obj);
		}

		public void addLight(SSLight light) {
			lights.Add (light);
		}

		public SSScene ()  {  }
	}
}

