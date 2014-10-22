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

    public class ssBVHNode<GO> {
        public float minX;
        public float maxX;
        public float minY;
        public float maxY;
        public float minZ;
        public float maxZ;

        public ssBVHNode<GO> parent;
        public Axis splitAxis;
        public ssBVHNode<GO> left;
        public ssBVHNode<GO> right;
                 
        public List<GO> gobjects;  // only populated in leaf nodes


        private Axis pickSplitAxis(Axis cur) {            
            float axis_x = maxX - minX; 
            float axis_y = maxY - minY;
            float axis_z = maxZ - minZ;

            // return the biggest axis
            if (axis_x > axis_y) {
                if (axis_x > axis_z) {
                    return Axis.X;
                } else {
                    return Axis.Z;
                }
            } else {
                if (axis_y > axis_z) {
                    return Axis.Y;
                } else {
                    return Axis.Z;
                }
            }

        }

        private Axis NextAxis(Axis cur) {
            switch(cur) {
                case Axis.X: return Axis.Y;
                case Axis.Y: return Axis.Z;
                case Axis.Z: return Axis.X;
                default: throw new NotSupportedException();
            }
        }

        private void assignVolume(Vector3 objectpos, float radius) {
            minX = objectpos.X - radius;
            maxX = objectpos.X + radius;
            minY = objectpos.Y - radius;
            maxY = objectpos.Y + radius;
            minZ = objectpos.Z - radius;
            maxZ = objectpos.Z + radius;
        }
        private void expandVolume(Vector3 objectpos, float radius) {
            // test min X and max X against the current bounding volume
            if ((objectpos.X - radius) < minX)
                minX = (objectpos.X - radius);
            if ((objectpos.X + radius) > maxX)
                maxX = (objectpos.X + radius);
            // Update the leaf node’s parent if appropriate with the min/max
            if (parent != null && minX < parent.minX)
                parent.minX = minX;
            if (parent != null && maxX > parent.maxX)
                parent.maxX = maxX;
            // test min Y and max Y against the current bounding volume
            if ((objectpos.Y - radius) < minY)
                minY = (objectpos.Y - radius);
            if ((objectpos.Y + radius) > maxY)
                maxY = (objectpos.Y + radius);
            // Update the leaf node’s parent if appropriate with the min/max
            if (parent != null && minY < parent.minY)
                parent.minY = minY;
            if (parent != null && maxY > parent.maxY)
                parent.maxY = maxY;
 
            // test min Z and max Z against the current bounding volume
            if ( (objectpos.Z - radius) < minZ )
                minZ = (objectpos.Z - radius);
            if ( (objectpos.Z + radius) > maxZ )
                maxZ = (objectpos.Z + radius);
            // Update the leaf node’s parent if appropriate with the min/max
            if (parent != null && minZ < parent.minZ)
                parent.minZ = minZ;
            if (parent != null && maxZ > parent.maxZ)
                parent.maxZ = maxZ;
        }
        
        internal ssBVHNode(ssBVH<GO> bvh, List<GO> gobjectlist) : this (bvh,gobjectlist,null,0,gobjectlist.Count-1, Axis.X)
         { }
        
        private ssBVHNode(ssBVH<GO> bvh, List<GO> gobjectlist, ssBVHNode<GO> lparent, int start, int end, Axis lastSplitAxis) {   
            SSBVHNodeAdaptor<GO> nAda = bvh.nAda;                 
            int center;
            int loop;
            int count = end - start;            
            List<GO> newgolist = new List<GO>();
            bvh.nodeCount++;
 
            parent = lparent; // save off the parent BVHGObj Node
            // Early out check due to bad data
            // If the list is empty then we have no BVHGObj, or invalid parameters are passed in
            if (gobjectlist == null || end < start)
            {
                minX = 0;
                maxX = 0;
                minY = 0;
                maxY = 0;
                minZ = 0;
                maxZ = 0;
                left = null;
                right = null;
                gobjects = null;

                return;
            }
 
            // Check if we’re at our LEAF node, and if so, save the objects and stop recursing.  Also store the min/max for the leaf node and update the parent appropriately
            if (count < bvh.LEAF_OBJ_MAX)
            {
                // We need to find the aggregate min/max for all 5 remaining objects
                // Start by recording the min max of the first object to have a starting point, then we’ll loop through the remaining
                assignVolume( nAda.objectpos(gobjectlist[start]), nAda.radius(gobjectlist[start]));
                                    
                // once we reach the leaf node, we must set prev/next to null to signify the end
                left = null;
                right = null;
                // at the leaf node we store the remaining objects, so initialize a list
                gobjects = new List<GO>();
                // loop through all the objects to add them to our leaf node, and calculate the min/max values as we go 
                for (loop = start; loop <= end; loop++)
                {

                    Vector3 objectpos = nAda.objectpos(gobjectlist[loop]);
                    float radius = nAda.radius(gobjectlist[loop]);

                    expandVolume(objectpos,radius);

                    // store our object into this nodes object list
                    gobjects.Add(gobjectlist[loop]);
                    // store this BVH leaf into our world-object so we can quickly find what BVH leaf node our object is stored in
                    nAda.mapObjectToBVHLeaf(gobjectlist[loop],this);                    
                }
                // done with this branch, return recursively and on return update the parent min/max bounding volume
                return;
            }
 
            // --------------------------------------------------------------------------------------------
            // if we have more than (bvh.LEAF_OBJECT_COUNT) objects, then sort and split
            
            // first, create a new list using just the subject of objects from the old list
            //  .. while we do this, we compute the volume
            assignVolume( nAda.objectpos(gobjectlist[start]) , nAda.radius(gobjectlist[start]) );            
            for (loop = start; loop <= end; loop++) {
                newgolist.Add(gobjectlist[loop]);
                expandVolume( nAda.objectpos(gobjectlist[loop]) , nAda.radius(gobjectlist[loop]) );
            }

            // second, decide which axis to split on, and sort..
            splitAxis = pickSplitAxis(lastSplitAxis);
            switch (splitAxis) // sort along the appropriate axis
            {
                case Axis.X: 
                    newgolist.Sort(delegate(GO go1, GO go2) { return nAda.objectpos(go1).X.CompareTo(nAda.objectpos(go2).X); }); 
                    break;
                case Axis.Y: 
                    newgolist.Sort(delegate(GO go1, GO go2) { return nAda.objectpos(go1).Y.CompareTo(nAda.objectpos(go2).Y); }); 
                    break;
                case Axis.Z: 
                    newgolist.Sort(delegate(GO go1, GO go2) { return nAda.objectpos(go1).Z.CompareTo(nAda.objectpos(go2).Z); }); 
                    break;
            }
            center = (int) (count * 0.5f); // Find the center object in our current sub-list

            // Initialize the branch to a starting value, then we’ll update it based on the leaf node recursion updating the parent
            assignVolume( nAda.objectpos(newgolist[0]) , nAda.radius(newgolist[0]) );
            gobjects = null;

            // if we’re here then we’re still in a leaf node.  therefore we need to split prev/next and keep branching until we reach the leaf node            
            left = new ssBVHNode<GO>(bvh, newgolist, this, 0, center, splitAxis); // Split the Hierarchy to the left
            right = new ssBVHNode<GO>(bvh, newgolist, this, center + 1, count, splitAxis); // Split the Hierarchy to the right
            // Update the parent bounding box to ensure it includes the children. Note: the leaf node already updated it’s parent, but now that parent needs to keep updating it’s branch parent until we reach the root level
            if (parent != null && minX < parent.minX)
                parent.minX = minX;
            if (parent != null && maxX > parent.maxX)
                parent.maxX = maxX;
            if (parent != null && minY < parent.minY)
                parent.minY = minY;
            if (parent != null && maxY > parent.maxY)
                parent.maxY = maxY;
            if (parent != null && minZ < parent.minZ)
                parent.minZ = minZ;
            if (parent != null && maxZ > parent.maxZ)
                parent.maxZ = maxZ;
        }

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

        public ssBVH(SSBVHNodeAdaptor<GO> nodeAdaptor, List<GO> objects, int LEAF_OBJ_MAX = 5) {
            this.LEAF_OBJ_MAX = LEAF_OBJ_MAX;
            this.nAda = nodeAdaptor;
            rootBVH = new ssBVHNode<GO>(this,objects);
        }
    }
}
