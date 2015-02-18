using System;

namespace SimpleScene
{
    public class SSMeshQuad : SSIndexedMesh<SSVertex_PosTex1>
    {
        private static readonly SSVertex_PosTex1[] c_vertices = {
            // CCW quad; no indexing
            new SSVertex_PosTex1(-.5f, -.5f, 0f, 0f, 1f),
            new SSVertex_PosTex1(+.5f, -.5f, 0f, 1f, 1f),
            new SSVertex_PosTex1(+.5f, +.5f, 0f, 1f, 0f),
            new SSVertex_PosTex1(-.5f, +.5f, 0f, 0f, 0f),
        };

        private static readonly UInt16[] c_indices = {
            0, 1, 2,
            0, 2, 3
        };

        public SSMeshQuad()
            : base(c_vertices, c_indices)
        {
        }
    }
}

