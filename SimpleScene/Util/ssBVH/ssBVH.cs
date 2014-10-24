// based on: Bounding Volume Hierarchies (BVH) – A brief tutorial on what they are and how to implement them
//              http://www.3dmuve.com/3dmblog/?p=182
//
// changes Copyright(C) David W. Jeske, 2013, and released to the public domain. 
//
// see also:  Space Partitioning: Octree vs. BVH
//            http://thomasdiewald.com/blog/?p=1488
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;

// TODO: add BVH ray-traversal
// TODO: add BVH sphere-intersection 
// TODO: add method to "add an object to the existing BVH"
// TODO: add method to "move an object in the existing BVH"

namespace SimpleScene.Util.ssBVH
{
    public enum Axis {
        X,Y,Z,
    }

    public interface SSBVHNodeAdaptor<GO> {
        ssBVH<GO> BVH { get; }
        void setBVH(ssBVH<GO> bvh);
        Vector3 objectpos(GO obj);
        float radius(GO obj);
        void mapObjectToBVHLeaf(GO obj, ssBVHNode<GO> leaf);
        void unmapObject(GO obj);
        void checkMap(GO obj);
    }

    public class ssBVH<GO>
    {
        public ssBVHNode<GO> rootBVH;
        public SSBVHNodeAdaptor<GO> nAda;
        public readonly int LEAF_OBJ_MAX;
        public int nodeCount = 0;

        public HashSet<ssBVHNode<GO>> refitNodes = new HashSet<ssBVHNode<GO>>();

        private void traverseRay(ssBVHNode<GO> curNode, SSRay ray, List<ssBVHNode<GO>> hitlist) {
            if (curNode == null) { return; }
            SSAABB box = curNode.box;            
            float tnear = 0f, tfar = 0f;
            
            if (OpenTKHelper.intersectRayAABox1(ray,box,ref tnear, ref tfar)) {
                hitlist.Add(curNode);
                traverseRay(curNode.left,ray,hitlist);
                traverseRay(curNode.right,ray,hitlist);
            }
        }

        public List<ssBVHNode<GO>> traverseRay(SSRay ray) {
            var hits = new List<ssBVHNode<GO>>();

            traverseRay(rootBVH,ray,hits);
            return hits;
        }

        public void optimize() {            
            while (refitNodes.Count > 0) {                
                int maxdepth = refitNodes.Max( n => n.depth );
            
                var sweepNodes = refitNodes.Where( n => n.depth == maxdepth ).ToList();
                sweepNodes.ForEach( n => refitNodes.Remove(n) );

                sweepNodes.ForEach( n => n.tryRotate(this) );                
            }            
        }

        public ssBVH(SSBVHNodeAdaptor<GO> nodeAdaptor, List<GO> objects, int LEAF_OBJ_MAX = 1) {
            this.LEAF_OBJ_MAX = LEAF_OBJ_MAX;
            nodeAdaptor.setBVH(this);
            this.nAda = nodeAdaptor;
            rootBVH = new ssBVHNode<GO>(this,objects);
        }
    }   
}
