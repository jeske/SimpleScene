// Adapted from this excellent IcoSphere tutorial by Andreas Kahler
// http://blog.andreaskahler.com/2009/06/creating-icosphere-mesh-in-code.html
// Changes Copyright (C) 2014 by David Jeske, and donated to the public domain.

using System;
using System.Collections.Generic;

using OpenTK;

namespace Util.IcoSphere
{

	public struct TriangleIndices
	{
		public int v1;
		public int v2;
		public int v3;

		public TriangleIndices (int v1, int v2, int v3)
		{
			this.v1 = v1;
			this.v2 = v2;
			this.v3 = v3;
		}
	}

	public class MeshGeometry3D
	{
		public List<Vector3> Positions = new List<Vector3> ();
		public List<int> MeshIndicies = new List<int> ();
		public List<TriangleIndices> Faces = new List<TriangleIndices> ();

	}

	public class IcoSphereCreator
	{
		private MeshGeometry3D geometry;
		private int index;
		private Dictionary<Int64, int> middlePointIndexCache;

		// add vertex to mesh, fix position to be on unit sphere, return index
		private int addVertex (Vector3 p)
		{
			float length = (float)Math.Sqrt (p.X * p.X + p.Y * p.Y + p.Z * p.Z);
			geometry.Positions.Add (new Vector3 (p.X / length, p.Y / length, p.Z / length));
			return index++;
		}

		// return index of point in the middle of p1 and p2
		private int getMiddlePoint (int p1, int p2)
		{
			// first check if we have it already
			bool firstIsSmaller = p1 < p2;
			Int64 smallerIndex = firstIsSmaller ? p1 : p2;
			Int64 greaterIndex = firstIsSmaller ? p2 : p1;
			Int64 key = (smallerIndex << 32) + greaterIndex;

			int ret;
			if (this.middlePointIndexCache.TryGetValue (key, out ret)) {
				return ret;
			}

			// not in cache, calculate it
			Vector3 point1 = this.geometry.Positions [p1];
			Vector3 point2 = this.geometry.Positions [p2];
			Vector3 middle = new Vector3 (
				                 (point1.X + point2.X) / 2.0f, 
				                 (point1.Y + point2.Y) / 2.0f, 
				                 (point1.Z + point2.Z) / 2.0f);

			// add vertex makes sure point is on unit sphere
			int i = addVertex (middle); 

			// store it, return index
			this.middlePointIndexCache.Add (key, i);
			return i;
		}

		public MeshGeometry3D Create (int recursionLevel)
		{
			this.geometry = new MeshGeometry3D ();
			this.middlePointIndexCache = new Dictionary<long, int> ();
			this.index = 0;

			// create 12 vertices of a icosahedron
			float t = (float)((1.0f + Math.Sqrt (5.0)) / 2.0);

			addVertex (new Vector3 (-1, t, 0f));
			addVertex (new Vector3 (1, t, 0f));
			addVertex (new Vector3 (-1, -t, 0f));
			addVertex (new Vector3 (1, -t, 0f));

			addVertex (new Vector3 (0, -1, t));
			addVertex (new Vector3 (0, 1, t));
			addVertex (new Vector3 (0, -1, -t));
			addVertex (new Vector3 (0, 1, -t));

			addVertex (new Vector3 (t, 0, -1));
			addVertex (new Vector3 (t, 0, 1));
			addVertex (new Vector3 (-t, 0, -1));
			addVertex (new Vector3 (-t, 0, 1));


			// create 20 triangles of the icosahedron
			var faces = new List<TriangleIndices> ();

			// 5 faces around point 0
			faces.Add (new TriangleIndices (0, 11, 5));
			faces.Add (new TriangleIndices (0, 5, 1));
			faces.Add (new TriangleIndices (0, 1, 7));
			faces.Add (new TriangleIndices (0, 7, 10));
			faces.Add (new TriangleIndices (0, 10, 11));

			// 5 adjacent faces 
			faces.Add (new TriangleIndices (1, 5, 9));
			faces.Add (new TriangleIndices (5, 11, 4));
			faces.Add (new TriangleIndices (11, 10, 2));
			faces.Add (new TriangleIndices (10, 7, 6));
			faces.Add (new TriangleIndices (7, 1, 8));

			// 5 faces around point 3
			faces.Add (new TriangleIndices (3, 9, 4));
			faces.Add (new TriangleIndices (3, 4, 2));
			faces.Add (new TriangleIndices (3, 2, 6));
			faces.Add (new TriangleIndices (3, 6, 8));
			faces.Add (new TriangleIndices (3, 8, 9));

			// 5 adjacent faces 
			faces.Add (new TriangleIndices (4, 9, 5));
			faces.Add (new TriangleIndices (2, 4, 11));
			faces.Add (new TriangleIndices (6, 2, 10));
			faces.Add (new TriangleIndices (8, 6, 7));
			faces.Add (new TriangleIndices (9, 8, 1));


			// refine triangles
			for (int i = 0; i < recursionLevel; i++) {
				var faces2 = new List<TriangleIndices> ();
				foreach (var tri in faces) {
					// replace triangle by 4 triangles
					int a = getMiddlePoint (tri.v1, tri.v2);
					int b = getMiddlePoint (tri.v2, tri.v3);
					int c = getMiddlePoint (tri.v3, tri.v1);

					faces2.Add (new TriangleIndices (tri.v1, a, c));
					faces2.Add (new TriangleIndices (tri.v2, b, a));
					faces2.Add (new TriangleIndices (tri.v3, c, b));
					faces2.Add (new TriangleIndices (a, b, c));
				}
				faces = faces2;
			}

			this.geometry.Faces = faces;

			// done, now add triangles to mesh
			foreach (var tri in faces) {
				this.geometry.MeshIndicies.Add (tri.v1);
				this.geometry.MeshIndicies.Add (tri.v2);
				this.geometry.MeshIndicies.Add (tri.v3);
			}

			return this.geometry;        
		}
	}
}

