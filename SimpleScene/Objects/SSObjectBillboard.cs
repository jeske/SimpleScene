using System;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSObjectBillboard : SSObjectMesh
    {
        private bool m_isOcclusionQueueryEnabled;
        private int m_queuery;

        public int OcclusionQueueryResult {
            get {
                int ret;
                GL.GetQueryObject(m_queuery, GetQueryObjectParam.QueryResult, out ret);
                return ret;
            }
        }

        public SSObjectBillboard(SSAbstractMesh mesh, bool enableOcclusionTest = false)
        {
            Mesh = mesh;
            m_isOcclusionQueueryEnabled = enableOcclusionTest;
            if (m_isOcclusionQueueryEnabled) {
                m_queuery = GL.GenQuery();
            }
        }

        public override void Render(ref SSRenderConfig renderConfig)
        {
            if (Mesh != null) {
                base.Render(ref renderConfig);

                Matrix4 modelView = this.worldMat * renderConfig.invCameraViewMat;
                modelView = OpenTKHelper.BillboardMatrix(ref modelView);
                GL.MatrixMode(MatrixMode.Modelview);
                GL.LoadMatrix(ref modelView);

                if (m_isOcclusionQueueryEnabled) {
                    GL.BeginQuery(QueryTarget.SamplesPassed, m_queuery);
                }

                Mesh.RenderMesh(ref renderConfig);

                if (m_isOcclusionQueueryEnabled) {
                    GL.EndQuery(QueryTarget.SamplesPassed);
                }
            }
        }
    }
}

