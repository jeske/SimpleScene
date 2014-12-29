using System;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSObjectBillboard : SSObjectMesh
    {
        public Vector4 Color = new Vector4 (1f);

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

                // override matrix setup to get rid of any rotation in view
                // http://stackoverflow.com/questions/5467007/inverting-rotation-in-3d-to-make-an-object-always-face-the-camera/5487981#5487981
                Matrix4 modelViewMat = this.worldMat * renderConfig.invCameraViewMat;
                Vector3 trans = modelViewMat.ExtractTranslation();
                //Vector3 scale = modelViewMat.ExtractScale();
                modelViewMat = new Matrix4 (
                    Scale.X, 0f, 0f, 0f,
                    0f, Scale.Y, 0f, 0f,
                    0f, 0f, Scale.Z, 0f,
                    trans.X, trans.Y, trans.Z, 1f);
                GL.MatrixMode(MatrixMode.Modelview);
                GL.LoadMatrix(ref modelViewMat);

                GL.Color4(Color);

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

