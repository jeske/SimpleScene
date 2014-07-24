using System;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
	// a Sky is an object which is projected at infinity...

	public class SSObjectMeshSky : SSObjectMesh
	{
		public SSObjectMeshSky (SSAbstractMesh mesh) : base(mesh) {  }

		public override void Render(ref SSRenderConfig renderConfig) {
			base.Render (ref renderConfig);
			
			// setup infinity projection by turning off depth testing and masking..		
			GL.Disable(EnableCap.DepthTest);
			GL.DepthMask(false);
			GL.Disable(EnableCap.DepthClamp);

            this.Mesh.RenderMesh(ref renderConfig);
        }
	}
}

