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
            // Note that below is expected to already be setup by the code managing scenes' rendering

            // setup infinity projection by turning off depth testing and masking..     
            //GL.Disable(EnableCap.DepthTest);
            //GL.DepthMask(false);
            //GL.Disable(EnableCap.DepthClamp);

            // Renders the mesh
			base.Render (ref renderConfig);
        }
	}
}

