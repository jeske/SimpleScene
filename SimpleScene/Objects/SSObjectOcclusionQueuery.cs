using System;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSObjectOcclusionQueuery : SSObjectMesh
    {
        private int _gl_query_id;
        protected int _result;

        public int OcclusionQueueryResult { get { return _result; } }

        public SSObjectOcclusionQueuery(SSAbstractMesh mesh)
        {
            Mesh = mesh;
            _gl_query_id = GL.GenQuery();
        }

        public override void Render(SSRenderConfig renderConfig)
        {
            if (Mesh != null) {
                GL.BeginQuery(QueryTarget.SamplesPassed, _gl_query_id);
                base.Render(renderConfig);
                GL.EndQuery(QueryTarget.SamplesPassed);
                GL.GetQueryObject(_gl_query_id, GetQueryObjectParam.QueryResult, out _result);
            } else {
                _result = 0;
            }
        }
    }
}

