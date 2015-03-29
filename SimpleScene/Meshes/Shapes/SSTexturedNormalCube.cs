using System;
using OpenTK;

namespace SimpleScene
{
	public static class SSTexturedNormalCube
	{
		public static readonly SSVertexBuffer<SSVertex_PosNormTex> Instance;

		private static readonly SSVertex_PosNormTex[] c_vertices = {
			// CCW quad; no indexing
			new SSVertex_PosNormTex(new Vector3(-.5f, -.5f, +0.5f), Vector3.UnitZ, new Vector2(0f, 1f)),
			new SSVertex_PosNormTex(new Vector3(+.5f, -.5f, +0.5f), Vector3.UnitZ, new Vector2(1f, 1f)),
			new SSVertex_PosNormTex(new Vector3(+.5f, +.5f, +0.5f), Vector3.UnitZ, new Vector2(1f, 0f)),

			new SSVertex_PosNormTex(new Vector3(-.5f, -.5f, +0.5f), Vector3.UnitZ, new Vector2(0f, 1f)),
			new SSVertex_PosNormTex(new Vector3(+.5f, +.5f, +0.5f), Vector3.UnitZ, new Vector2(1f, 0f)),
			new SSVertex_PosNormTex(new Vector3(-.5f, +.5f, +0.5f), Vector3.UnitZ, new Vector2(0f, 0f)),
		};

		private static readonly Quaternion[] c_faceTransforms = {
			Quaternion.Identity,
			Quaternion.FromAxisAngle(Vector3.UnitY, (float)Math.PI * 0.5f),
			Quaternion.FromAxisAngle(Vector3.UnitY, (float)Math.PI),
			Quaternion.FromAxisAngle(Vector3.UnitY, (float)Math.PI * 1.5f),

			Quaternion.FromAxisAngle(Vector3.UnitX, (float)Math.PI * 0.5f),
			Quaternion.FromAxisAngle(Vector3.UnitX, (float)Math.PI * 1.5f),
		};

		static SSTexturedNormalCube()
		{
			int verticesPerQuad = c_vertices.Length;
			SSVertex_PosNormTex[] vertices = new SSVertex_PosNormTex[verticesPerQuad * 6];
			for (int f = 0; f < 6; ++f) {
				for (int v = 0; v < verticesPerQuad; ++v) {
					SSVertex_PosNormTex vertex = c_vertices [v];
					vertex.Position = Vector3.Transform(vertex.Position, c_faceTransforms [f]);
					vertex.Normal = Vector3.Transform (vertex.Normal, c_faceTransforms [f]);
					vertices [f * verticesPerQuad + v] = vertex;
				}
			}
			Instance = new SSVertexBuffer<SSVertex_PosNormTex> (vertices);
		}
	}
}
