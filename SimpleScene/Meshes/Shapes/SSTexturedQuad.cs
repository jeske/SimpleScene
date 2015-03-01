using System;

namespace SimpleScene
{
    public static class SSTexturedQuad
    {
		public static readonly SSVertexBuffer<SSVertex_PosTex1> Instance;

		static SSTexturedQuad()
		{
			Instance = new SSVertexBuffer<SSVertex_PosTex1>(c_vertices);
		}

        private static readonly SSVertex_PosTex1[] c_vertices = {
            // CCW quad; no indexing
            new SSVertex_PosTex1(-.5f, -.5f, 0f, 0f, 1f),
            new SSVertex_PosTex1(+.5f, -.5f, 0f, 1f, 1f),
            new SSVertex_PosTex1(+.5f, +.5f, 0f, 1f, 0f),

            new SSVertex_PosTex1(-.5f, -.5f, 0f, 0f, 1f),
            new SSVertex_PosTex1(+.5f, +.5f, 0f, 1f, 0f),
            new SSVertex_PosTex1(-.5f, +.5f, 0f, 0f, 0f),
        };
    }
}

