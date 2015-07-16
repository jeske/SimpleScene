using System;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSObjectOcclusionQueuery : SSObjectMesh
    {
		public bool doBillboarding = true;

        private int GL_query_id;

        public int OcclusionQueueryResult {
            get {
                int ret;
                GL.GetQueryObject(GL_query_id, GetQueryObjectParam.QueryResult, out ret);
                return ret;
            }
        }

        public SSObjectOcclusionQueuery(SSAbstractMesh mesh)
        {
            Mesh = mesh;
            GL_query_id = GL.GenQuery();
        }

        public override void Render(SSRenderConfig renderConfig)
        {
            if (Mesh != null) {
                base.Render(renderConfig);

				if (doBillboarding) {
					Matrix4 modelView = this.worldMat * renderConfig.invCameraViewMatrix;
					modelView = OpenTKHelper.BillboardMatrix (ref modelView);
					GL.MatrixMode (MatrixMode.Modelview);
					GL.LoadMatrix (ref modelView);
				}

                GL.BeginQuery(QueryTarget.SamplesPassed, GL_query_id);
                Mesh.renderMesh(renderConfig);
                GL.EndQuery(QueryTarget.SamplesPassed);
            }
        }
    }
}

