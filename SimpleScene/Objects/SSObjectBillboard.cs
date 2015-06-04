using System;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSObjectBillboard : SSObjectMesh
    {
        private bool isOcclusionQueueryEnabled;
        private int GL_query_id;

        public int OcclusionQueueryResult {
            get {
                int ret;
                GL.GetQueryObject(GL_query_id, GetQueryObjectParam.QueryResult, out ret);
                return ret;
            }
        }

        public SSObjectBillboard(SSAbstractMesh mesh, bool enableOcclusionTest = false)
        {
            Mesh = mesh;
            isOcclusionQueueryEnabled = enableOcclusionTest;
            if (isOcclusionQueueryEnabled) {
                GL_query_id = GL.GenQuery();
            }
        }

        public override void Render(ref SSRenderConfig renderConfig)
        {
            if (Mesh != null) {
                base.Render(ref renderConfig);

                Matrix4 modelView = this.worldMat * renderConfig.invCameraViewMatrix;
                modelView = OpenTKHelper.BillboardMatrix(ref modelView);
                GL.MatrixMode(MatrixMode.Modelview);
                GL.LoadMatrix(ref modelView);

                if (isOcclusionQueueryEnabled) {
                    GL.BeginQuery(QueryTarget.SamplesPassed, GL_query_id);
                }

                Mesh.renderMesh(ref renderConfig);

                if (isOcclusionQueueryEnabled) {
                    GL.EndQuery(QueryTarget.SamplesPassed);
                }
            }
        }
    }
}

