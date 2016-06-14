using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SimpleScene.Util.ssBVH;

namespace SimpleScene
{
    public class SSIndexedMesh<V> : SSAbstractMesh, ISSInstancable
        where V : struct, ISSVertexLayout
    {
        public bool useBVHForIntersections = true;
        public PrimitiveType defaultPrimType = PrimitiveType.Triangles;

        protected SSVertexBuffer<V> _vbo;
        protected SSIndexBuffer _ibo;
        protected SSIndexedMeshTrianglesBVH _bvh = null;

        public V[] lastAssignedVertices {
            get { return _vbo.lastAssignedElements; }
        }

        public UInt16[] lastAssignedIndices {
            get { return _ibo.lastAssignedIndices; }
        }

        /// <summary>
        /// Initialize based on buffer usage. Default to dynamic draw.
        /// </summary>
        public SSIndexedMesh (BufferUsageHint vertUsage = BufferUsageHint.DynamicDraw, 
                              BufferUsageHint indexUsage = BufferUsageHint.DynamicDraw)
        {
            _vbo = new SSVertexBuffer<V> (vertUsage);
            _ibo = new SSIndexBuffer (_vbo, indexUsage);
        }

        /// <summary>
        /// Initialize given arrays of vertices and/or indices.
        /// </summary>
        public SSIndexedMesh(V[] vertices, UInt16[] indices)
			: base()
        {
            if (vertices == null) {
                _vbo = new SSVertexBuffer<V> (BufferUsageHint.DynamicDraw);
            } else {
                _vbo = new SSVertexBuffer<V> (vertices);
            }

            if (indices == null) {
                _ibo = new SSIndexBuffer (_vbo, BufferUsageHint.DynamicDraw);
            } else {
                _ibo = new SSIndexBuffer (indices, _vbo);
            }
        }

		public SSIndexedMesh(SSVertexBuffer<V> vbo, SSIndexBuffer ibo)
		{
			if (vbo == null) {
				_vbo = new SSVertexBuffer<V> (BufferUsageHint.DynamicDraw);
			} else {
				_vbo = vbo;
			}

			if (ibo == null) {
				_ibo = new SSIndexBuffer (vbo, BufferUsageHint.DynamicDraw);
			} else {
				_ibo = ibo;
			}
		}

        public override void renderMesh(SSRenderConfig renderConfig)
        {
            drawSingle(renderConfig, defaultPrimType);
        }

		public void drawInstanced(SSRenderConfig renderConfig, int instanceCount, PrimitiveType primType)
        {
			base.renderMesh (renderConfig);
			_ibo.drawInstanced (renderConfig, instanceCount, primType);
        }

        public void drawSingle(SSRenderConfig renderConfig, PrimitiveType primType)
        {
            base.renderMesh (renderConfig);
            _ibo.DrawElements (renderConfig, primType);
        }

        public void updateVertices (V[] vertices)
        {
            _vbo.UpdateBufferData(vertices);
            _bvh = null; // invalidate bvh
        }

        public void updateIndices (UInt16[] indices)
        {
            _ibo.UpdateBufferData(indices);
            _bvh = null; // invalidate bvh
        }

        //---------------------

        public override bool preciseIntersect (ref SSRay localRay, out float nearestLocalRayContact)
        {
            nearestLocalRayContact = float.PositiveInfinity;

            if (useBVHForIntersections) {
                if (_bvh == null && _vbo != null && _ibo != null) {
                    // rebuilding BVH
                    // TODO try updating instead of rebuilding?
                    _bvh = new SSIndexedMeshTrianglesBVH (_vbo, _ibo);
                    for (UInt16 triIdx = 0; triIdx < _ibo.numIndices / 3; ++triIdx) {
                        _bvh.addObject(triIdx);
                    }
                    Console.WriteLine("New BVH MaxDepth = {0}",_bvh.maxDepth);
                }

                if (_bvh != null) {
                    List<ssBVHNode<UInt16>> nodesHit = _bvh.traverseRay(localRay);
                    foreach (var node in nodesHit) {
                        if (!node.IsLeaf) continue;

                        foreach (UInt16 triIdx in node.gobjects) {
                            Vector3 v0, v1, v2;
                            _readTriangleVertices(triIdx, out v0, out v1, out v2);

                            float contact;
                            if (OpenTKHelper.TriangleRayIntersectionTest(
                                    ref v0, ref v1, ref v2, ref localRay.pos, ref localRay.dir, out contact)) {
                                if (contact < nearestLocalRayContact) {
                                    nearestLocalRayContact = contact;
                                }
                            }
                        }
                    }
                }
            } else {
                _bvh = null;
                // slow, tedious intersection test
                int numTri = lastAssignedIndices.Length / 3;
                for (UInt16 triIdx = 0; triIdx < numTri; ++triIdx) {
                    Vector3 v0, v1, v2;
                    _readTriangleVertices(triIdx, out v0, out v1, out v2);
                    float contact;
                    if (OpenTKHelper.TriangleRayIntersectionTest(
                        ref v0, ref v1, ref v2, ref localRay.pos, ref localRay.dir, out contact))
                    {
                        if (contact < nearestLocalRayContact) {
                            nearestLocalRayContact = contact;
                        }
                    }
                }
            }
            return nearestLocalRayContact < float.PositiveInfinity;
        }

        protected void _readTriangleVertices(
            UInt16 triIdx, out Vector3 v0, out Vector3 v1, out Vector3 v2)
        {
            UInt16 baseOffset = (UInt16)(3 * triIdx);
            UInt16 i0 = _ibo.lastAssignedIndices [baseOffset];
            UInt16 i1 = _ibo.lastAssignedIndices [baseOffset + 1];
            UInt16 i2 = _ibo.lastAssignedIndices [baseOffset + 2];

            v0 = _vbo.lastAssignedElements [i0]._position;
            v1 = _vbo.lastAssignedElements [i1]._position;
            v2 = _vbo.lastAssignedElements [i2]._position;
        }

        public class SSIndexedMeshTriangleBVHNodeAdaptor : SSBVHNodeAdaptor<UInt16>
        {
            protected readonly SSVertexBuffer<V> _vbo;
            protected readonly SSIndexBuffer _ibo;
            protected ssBVH<UInt16> _bvh;
            protected Dictionary<UInt16, ssBVHNode<UInt16>> _indexToLeafMap
                = new Dictionary<UInt16, ssBVHNode<UInt16>> ();

            public SSIndexedMeshTriangleBVHNodeAdaptor(SSVertexBuffer<V> vbo, SSIndexBuffer ibo)
            {
                _vbo = vbo;
                _ibo = ibo;
            }

            public void setBVH(ssBVH<UInt16> bvh)
            {
                _bvh = bvh;
            }

            public ssBVH<UInt16> BVH { get { return _bvh; } }

            public Vector3 objectpos(UInt16 triIdx)
            {
                Vector3 v0, v1, v2;
                _readTriangleVertices(triIdx, out v0, out v1, out v2);
                return (v0 + v1 + v2) / 3f;
            }

            public float radius(UInt16 triIdx)
            {
                Vector3 v0, v1, v2;
                _readTriangleVertices(triIdx, out v0, out v1, out v2);
                Vector3 centroid = (v0 + v1 + v2) / 3f;
                v0 -= centroid;
                v1 -= centroid;
                v2 -= centroid;
                float maxLenSq = Math.Max(v0.LengthSquared, v1.LengthFast);
                maxLenSq = Math.Max(maxLenSq, v2.LengthSquared);
                return (float)Math.Sqrt((float)maxLenSq);
            }

            public void checkMap(UInt16 triIdx)
            {
                if (!_indexToLeafMap.ContainsKey(triIdx)) {
                    throw new Exception ("missing map for a shuffled child");
                }
            }

            public void unmapObject(UInt16 triIdx)
            {
                _indexToLeafMap.Remove(triIdx);
            }

            public void mapObjectToBVHLeaf(UInt16 triIdx, ssBVHNode<UInt16> leaf)
            {
                _indexToLeafMap [triIdx] = leaf;
            }

            public ssBVHNode<UInt16> getLeaf(UInt16 triIdx)
            {
                return _indexToLeafMap [triIdx];
            }

            protected void _readTriangleVertices(UInt16 triIdx, 
                out Vector3 v0, out Vector3 v1, out Vector3 v2)
            {
                int baseOffset = triIdx * 3;
                int i0 = _ibo.lastAssignedIndices[baseOffset];
                int i1 = _ibo.lastAssignedIndices[baseOffset+1];
                int i2 = _ibo.lastAssignedIndices[baseOffset+2];
                v0 = _vbo.lastAssignedElements [i0]._position;
                v1 = _vbo.lastAssignedElements [i1]._position;
                v2 = _vbo.lastAssignedElements [i2]._position;
            }
        }

        public class SSIndexedMeshTrianglesBVH : ssBVH<UInt16>
        {
            public SSIndexedMeshTrianglesBVH(SSVertexBuffer<V> vbo, SSIndexBuffer ibo, int maxTrianglesPerLeaf=1)
                : base(new SSIndexedMeshTriangleBVHNodeAdaptor(vbo, ibo), new List<UInt16>(), maxTrianglesPerLeaf)
            { 
            }
        }
    }
}

