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
        Vector3 objectpos(GO obj);
        float radius(GO obj);
        void mapObjectToBVHLeaf(GO obj, ssBVHNode<GO> leaf);
    }

    public class ssBVH<GO>
    {
        public ssBVHNode<GO> rootBVH;
        public SSBVHNodeAdaptor<GO> nAda;
        public int LEAF_OBJ_MAX = 5;
        public int nodeCount = 0;


        private void traverseRay(ssBVHNode<GO> curNode, SSRay ray, List<ssBVHNode<GO>> hitlist) {
            if (curNode == null) { return; }
            SSAABB box = new SSAABB();
            box.min.X = curNode.minX;
            box.min.Y = curNode.minY;
            box.min.Z = curNode.minZ;
            box.max.X = curNode.maxX;
            box.max.Y = curNode.maxY;
            box.max.Z = curNode.maxZ;
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


        public ssBVH(SSBVHNodeAdaptor<GO> nodeAdaptor, List<GO> objects, int LEAF_OBJ_MAX = 5) {
            this.LEAF_OBJ_MAX = LEAF_OBJ_MAX;
            this.nAda = nodeAdaptor;
            rootBVH = new ssBVHNode<GO>(this,objects);
        }
    }
}
