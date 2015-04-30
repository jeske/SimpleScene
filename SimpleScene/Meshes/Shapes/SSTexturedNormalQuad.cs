using System;
using OpenTK;

namespace SimpleScene
{
	public static class SSTexturedNormalQuad
	{
		public static readonly SSVertexBuffer<SSVertex_PosNormTex> SingleFaceInstance;
		public static readonly SSVertexBuffer<SSVertex_PosNormTex> DoubleFaceInstance;

		static SSTexturedNormalQuad()
		{
			SingleFaceInstance = new SSVertexBuffer<SSVertex_PosNormTex>(c_singleFaceVertices);
			DoubleFaceInstance = new SSVertexBuffer<SSVertex_PosNormTex>(c_doubleFaceVertices);
		}

		public static readonly SSVertex_PosNormTex[] c_singleFaceVertices = {
			// CCW quad; no indexing
			new SSVertex_PosNormTex(new Vector3(-.5f, -.5f, 0f), Vector3.UnitZ, new Vector2(0f, 1f)),
			new SSVertex_PosNormTex(new Vector3(+.5f, -.5f, 0f), Vector3.UnitZ, new Vector2(1f, 1f)),
			new SSVertex_PosNormTex(new Vector3(+.5f, +.5f, 0f), Vector3.UnitZ, new Vector2(1f, 0f)),

			new SSVertex_PosNormTex(new Vector3(-.5f, -.5f, 0f), Vector3.UnitZ, new Vector2(0f, 1f)),
			new SSVertex_PosNormTex(new Vector3(+.5f, +.5f, 0f), Vector3.UnitZ, new Vector2(1f, 0f)),
			new SSVertex_PosNormTex(new Vector3(-.5f, +.5f, 0f), Vector3.UnitZ, new Vector2(0f, 0f)),
		};

		public static readonly SSVertex_PosNormTex[] c_doubleFaceVertices = {
			// CCW quad; no indexing
			new SSVertex_PosNormTex(new Vector3(-.5f, -.5f, 0f), Vector3.UnitZ, new Vector2(0f, 1f)),
			new SSVertex_PosNormTex(new Vector3(+.5f, -.5f, 0f), Vector3.UnitZ, new Vector2(1f, 1f)),
			new SSVertex_PosNormTex(new Vector3(+.5f, +.5f, 0f), Vector3.UnitZ, new Vector2(1f, 0f)),

			new SSVertex_PosNormTex(new Vector3(-.5f, -.5f, 0f), Vector3.UnitZ, new Vector2(0f, 1f)),
			new SSVertex_PosNormTex(new Vector3(+.5f, +.5f, 0f), Vector3.UnitZ, new Vector2(1f, 0f)),
			new SSVertex_PosNormTex(new Vector3(-.5f, +.5f, 0f), Vector3.UnitZ, new Vector2(0f, 0f)),

			new SSVertex_PosNormTex(new Vector3(-.5f, -.5f, 0f), -Vector3.UnitZ, new Vector2(0f, 1f)),
			new SSVertex_PosNormTex(new Vector3(+.5f, +.5f, 0f), -Vector3.UnitZ, new Vector2(1f, 0f)),
			new SSVertex_PosNormTex(new Vector3(+.5f, -.5f, 0f), -Vector3.UnitZ, new Vector2(1f, 1f)),

			new SSVertex_PosNormTex(new Vector3(-.5f, -.5f, 0f), -Vector3.UnitZ, new Vector2(0f, 1f)),
			new SSVertex_PosNormTex(new Vector3(-.5f, +.5f, 0f), -Vector3.UnitZ, new Vector2(0f, 0f)),
			new SSVertex_PosNormTex(new Vector3(+.5f, +.5f, 0f), -Vector3.UnitZ, new Vector2(1f, 0f)),
		};
	}
}