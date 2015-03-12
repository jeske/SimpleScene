using System;

namespace SimpleScene
{
    public static class SSTexturedQuad
    {
		public static readonly SSVertexBuffer<SSVertex_PosTex1> SingleFaceInstance;
		public static readonly SSVertexBuffer<SSVertex_PosTex1> DoubleFaceInstance;

		static SSTexturedQuad()
		{
			SingleFaceInstance = new SSVertexBuffer<SSVertex_PosTex1>(c_singleFaceVertices);
			DoubleFaceInstance = new SSVertexBuffer<SSVertex_PosTex1>(c_doubleFaceVertices);
		}

		private static readonly SSVertex_PosTex1[] c_singleFaceVertices = {
			// CCW quad; no indexing
			new SSVertex_PosTex1(-.5f, -.5f, 0f, 0f, 1f),
			new SSVertex_PosTex1(+.5f, -.5f, 0f, 1f, 1f),
			new SSVertex_PosTex1(+.5f, +.5f, 0f, 1f, 0f),

			new SSVertex_PosTex1(-.5f, -.5f, 0f, 0f, 1f),
			new SSVertex_PosTex1(+.5f, +.5f, 0f, 1f, 0f),
			new SSVertex_PosTex1(-.5f, +.5f, 0f, 0f, 0f),
		};

		private static readonly SSVertex_PosTex1[] c_doubleFaceVertices = {
			// CCW quad; no indexing
			new SSVertex_PosTex1(-.5f, -.5f, 0f, 0f, 1f),
			new SSVertex_PosTex1(+.5f, -.5f, 0f, 1f, 1f),
			new SSVertex_PosTex1(+.5f, +.5f, 0f, 1f, 0f),

			new SSVertex_PosTex1(-.5f, -.5f, 0f, 0f, 1f),
			new SSVertex_PosTex1(+.5f, +.5f, 0f, 1f, 0f),
			new SSVertex_PosTex1(-.5f, +.5f, 0f, 0f, 0f),

			new SSVertex_PosTex1(-.5f, -.5f, 0f, 0f, 1f),
			new SSVertex_PosTex1(+.5f, +.5f, 0f, 1f, 0f),
			new SSVertex_PosTex1(+.5f, -.5f, 0f, 1f, 1f),

			new SSVertex_PosTex1(-.5f, -.5f, 0f, 0f, 1f),
			new SSVertex_PosTex1(-.5f, +.5f, 0f, 0f, 0f),
			new SSVertex_PosTex1(+.5f, +.5f, 0f, 1f, 0f),
		};
    }
}

