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
		public bool drawWireframes = false;
		public Matrix4 invCameraViewMat;
	}

	public sealed class SSScene
	{
		public ICollection<SSObject> objects = new List<SSObject>();
		public ICollection<SSLight> lights = new List<SSLight> ();

		public SSCamera activeCamera;

		public SSRenderConfig renderConfig = new SSRenderConfig ();

		public void Update() {
			// update all objects.. TODO: add elapsed time since last update..
			foreach (var obj in objects) {
				obj.Update ();
			}
		}

		public void SetupLights(Matrix4 invCameraViewMat) {
			renderConfig.invCameraViewMat = invCameraViewMat;

			GL.Enable (EnableCap.Lighting);
			foreach (var light in lights) {
				light.SetupLight (ref renderConfig);
			}
		}
			
		public void Render(Matrix4 invCameraViewMat) {
			renderConfig.invCameraViewMat = invCameraViewMat;

			foreach (var obj in objects) {	
				obj.Render (ref renderConfig);
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

