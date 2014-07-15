using System;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace WavefrontOBJViewer
{

	// a Sky is an object which is projected at infinity...

	public class SSObjectMeshSky : SSObjectMesh
	{
		public SSObjectMeshSky (SSAbstractMesh mesh) : base(mesh) {  }

		public override void Render(ref SSRenderConfig renderConfig) {
			// base.Render (ref renderConfig);

			// setup infinity projection
			Matrix4 modelViewMat = this.worldMat * renderConfig.invCameraViewMat;

			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadMatrix(ref modelViewMat);

			GL.Disable(EnableCap.DepthTest);
			GL.DepthMask(false);
			GL.Disable(EnableCap.DepthClamp);

            this.mesh.RenderMesh(ref renderConfig);
        }
	}
}

