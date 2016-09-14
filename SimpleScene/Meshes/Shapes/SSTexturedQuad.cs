using System;

namespace SimpleScene
{
    public static class SSTexturedQuad
    {
		public static readonly SSVertexBuffer<SSVertex_PosTex> singleFaceInstance;
		public static readonly SSVertexBuffer<SSVertex_PosTex> doubleFaceInstance;
        public static readonly SSVertexBuffer<SSVertex_PosTex> doubleFaceCrossBarInstance;

		static SSTexturedQuad()
		{
			singleFaceInstance = new SSVertexBuffer<SSVertex_PosTex>(singleFaceVertices);
			doubleFaceInstance = new SSVertexBuffer<SSVertex_PosTex>(doubleFaceVertices);
            doubleFaceCrossBarInstance = new SSVertexBuffer<SSVertex_PosTex> (doubleFaceCrossBarVertices);
		}

		public static readonly SSVertex_PosTex[] singleFaceVertices = {
			// CCW quad; no indexing
			new SSVertex_PosTex(-.5f, -.5f, 0f, 0f, 1f),
			new SSVertex_PosTex(+.5f, -.5f, 0f, 1f, 1f),
			new SSVertex_PosTex(+.5f, +.5f, 0f, 1f, 0f),

			new SSVertex_PosTex(-.5f, -.5f, 0f, 0f, 1f),
			new SSVertex_PosTex(+.5f, +.5f, 0f, 1f, 0f),
			new SSVertex_PosTex(-.5f, +.5f, 0f, 0f, 0f),
		};

		public static readonly SSVertex_PosTex[] doubleFaceVertices = {
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

        public static readonly SSVertex_PosTex[] doubleFaceCrossBarVertices = {
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

            // ----- 

            new SSVertex_PosTex(-.5f, 0f, -.5f, 0f, 1f),
            new SSVertex_PosTex(+.5f, 0f, -.5f, 1f, 1f),
            new SSVertex_PosTex(+.5f, 0f, +.5f, 1f, 0f),

            new SSVertex_PosTex(-.5f, 0f, -.5f, 0f, 1f),
            new SSVertex_PosTex(+.5f, 0f, +.5f, 1f, 0f),
            new SSVertex_PosTex(-.5f, 0f, +.5f, 0f, 0f),

            new SSVertex_PosTex(-.5f, 0f, -.5f, 0f, 1f),
            new SSVertex_PosTex(+.5f, 0f, +.5f, 1f, 0f),
            new SSVertex_PosTex(+.5f, 0f, -.5f, 1f, 1f),

            new SSVertex_PosTex(-.5f, 0f, -.5f, 0f, 1f),
            new SSVertex_PosTex(-.5f, 0f, +.5f, 0f, 0f),
            new SSVertex_PosTex(+.5f, 0f, +.5f, 1f, 0f),
        };
    }
}

