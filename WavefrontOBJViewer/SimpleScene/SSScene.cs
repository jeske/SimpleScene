// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.


using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace WavefrontOBJViewer
{
	public sealed class SSScene
	{
		public ICollection<SSObject> objects = new List<SSObject>();

		public SSObject activeCamera;

		public void Update() {
			// update all objects.. TODO: add elapsed time since last update..
			foreach (var obj in objects) {
				obj.Update ();
			}
		}

		public void adjustProjectionMatrixForActiveCamera(ref Matrix4 projectionMatrix) {
			if (activeCamera != null) {
				projectionMatrix = activeCamera.worldMat * projectionMatrix;
			} else {
				projectionMatrix = Matrix4.CreateTranslation (0, 0, -5) * projectionMatrix;
			}
		}

		public void Render() {

			foreach (var obj in objects) {
				// should load this objects projection matrix instead..
				Matrix4 modelview = Matrix4.LookAt(Vector3.Zero, Vector3.UnitZ, Vector3.UnitY);
				GL.MatrixMode(MatrixMode.Modelview);
				GL.LoadMatrix(ref modelview);


				obj.Render ();
			}
		}

		public void addObject(SSObject obj) {
			objects.Add (obj);
		}

		public SSScene ()  {  }
	}
}

