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

		public SSCamera activeCamera;

		public void Update() {
			// update all objects.. TODO: add elapsed time since last update..
			foreach (var obj in objects) {
				obj.Update ();
			}
		}

		public void adjustProjectionMatrixForActiveCamera(ref Matrix4 projectionMatrix) {
			if (activeCamera != null) {
				Matrix4 cameraMatrix = Matrix4.LookAt(activeCamera.Pos,Vector3.UnitZ,Vector3.UnitY);
				projectionMatrix = cameraMatrix * projectionMatrix;
			} else {
				projectionMatrix = Matrix4.CreateTranslation (0, 0, -5) * projectionMatrix;
			}
		}

		public void Render() {
			foreach (var obj in objects) {
				GL.MatrixMode(MatrixMode.Modelview);
				GL.LoadMatrix(ref obj.localMat);
				obj.Render ();
			}
		}

		public void addObject(SSObject obj) {
			objects.Add (obj);
		}

		public SSScene ()  {  }
	}
}

