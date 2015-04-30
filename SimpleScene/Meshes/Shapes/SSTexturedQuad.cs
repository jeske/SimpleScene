using System;

namespace SimpleScene
{
    public static class SSTexturedQuad
    {
		public static readonly SSVertexBuffer<SSVertex_PosTex> SingleFaceInstance;
		public static readonly SSVertexBuffer<SSVertex_PosTex> DoubleFaceInstance;

		static SSTexturedQuad()
		{
			SingleFaceInstance = new SSVertexBuffer<SSVertex_PosTex>(c_singleFaceVertices);
			DoubleFaceInstance = new SSVertexBuffer<SSVertex_PosTex>(c_doubleFaceVertices);
		}

		public static readonly SSVertex_PosTex[] c_singleFaceVertices = {
			// CCW quad; no indexing
			new SSVertex_PosTex(-.5f, -.5f, 0f, 0f, 1f),
			new SSVertex_PosTex(+.5f, -.5f, 0f, 1f, 1f),
			new SSVertex_PosTex(+.5f, +.5f, 0f, 1f, 0f),

			new SSVertex_PosTex(-.5f, -.5f, 0f, 0f, 1f),
			new SSVertex_PosTex(+.5f, +.5f, 0f, 1f, 0f),
			new SSVertex_PosTex(-.5f, +.5f, 0f, 0f, 0f),
		};

		public static readonly SSVertex_PosTex[] c_doubleFaceVertices = {
			// CCW quad; no indexing
			new SSVertex_PosTex(-.5f, -.5f, 0f, 0f, 1f),
			new SSVertex_PosTex(+.5f, -.5f, 0f, 1f, 1f),
			new SSVertex_PosTex(+.5f, +.5f, 0f, 1f, 0f),

			new SSVertex_PosTex(-.5f, -.5f, 0f, 0f, 1f),
			new SSVertex_PosTex(+.5f, +.5f, 0f, 1f, 0f),
			new SSVertex_PosTex(-.5f, +.5f, 0f, 0f, 0f),

			new SSVertex_PosTex(-.5f, -.5f, 0f, 0f, 1f),
			new SSVertex_PosTex(+.5f, +.5f, 0f, 1f, 0f),
			new SSVertex_PosTex(+.5f, -.5f, 0f, 1f, 1f),

			new SSVertex_PosTex(-.5f, -.5f, 0f, 0f, 1f),
			new SSVertex_PosTex(-.5f, +.5f, 0f, 0f, 0f),
			new SSVertex_PosTex(+.5f, +.5f, 0f, 1f, 0f),
		};
    }
}

