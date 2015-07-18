using System;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSObjectOcclusionQueuery : SSObjectMesh
    {
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
                GL.BeginQuery(QueryTarget.SamplesPassed, GL_query_id);
                base.Render(renderConfig);
                GL.EndQuery(QueryTarget.SamplesPassed);
            }
        }
    }
}

