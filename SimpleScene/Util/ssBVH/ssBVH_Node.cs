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

namespace SimpleScene.Util.ssBVH
{
    public class ssBVHNode<GO> {
        public float minX;
        public float maxX;
        public float minY;
        public float maxY;
        public float minZ;
        public float maxZ;

        public ssBVHNode<GO> parent;
        public Axis splitAxis;
        public float splitOverlap = 0.0f;
        public ssBVHNode<GO> left;
        public ssBVHNode<GO> right;

        public int depth;
                 
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

        public void refit_ObjectChanged(SSBVHNodeAdaptor<GO> nAda, GO obj) {
            if (parent == null) { throw new Exception("dangling leaf!"); }
            recomputeVolume(nAda);
        }

        private void assignVolume(Vector3 objectpos, float radius) {
            minX = objectpos.X - radius;
            maxX = objectpos.X + radius;
            minY = objectpos.Y - radius;
            maxY = objectpos.Y + radius;
            minZ = objectpos.Z - radius;
            maxZ = objectpos.Z + radius;
        }      
        
        internal void recomputeVolume(SSBVHNodeAdaptor<GO> nAda) {
            if (gobjects.Count == 0) { return; }  // TODO: fix this... we should never get called in this case...
            assignVolume( nAda.objectpos(gobjects[0]), nAda.radius(gobjects[0]));
            for(int i=1; i<gobjects.Count;i++) {
                expandVolume(nAda, nAda.objectpos(gobjects[i]) , nAda.radius(gobjects[i]) );
            }
            if (parent != null) parent.childRefit(nAda);
        }
        
        internal void addObjects(SSBVHNodeAdaptor<GO> nAda, List<GO> objects) {
            if (gobjects != null) {
                foreach (var obj in objects) {
                    gobjects.Add(obj);
                    nAda.mapObjectToBVHLeaf(obj,this);
                }
                recomputeVolume(nAda);
                // TODO: add splitting if we have more than MAX_OBJECTS 
                return;
            } 
            
            // find the approximate child gap midpoint
            float midpoint;
            ssBVHNode<GO> lower = left,higher = right;
            switch (splitAxis) 
            { 
                case Axis.X:
                    if (left.minX < right.minX) {
                        midpoint = (left.maxX + right.minX) / 2.0f;
                    } else { 
                        midpoint = (right.maxX + left.minX) / 2.0f;
                        lower = right; higher = left;
                    }                    
                    break;
                case Axis.Y:
                    if (left.minY < right.minY) {
                        midpoint = (left.maxY + right.minY) / 2.0f;
                    } else { 
                        midpoint = (right.maxY + left.minY) / 2.0f;
                        lower = right; higher = left;
                    }                    
                    break;
                case Axis.Z:
                    if (left.minZ < right.minZ) {
                        midpoint = (left.maxZ + right.minZ) / 2.0f;
                    } else { 
                        midpoint = (right.maxZ + left.minZ) / 2.0f;
                        lower = right; higher = left;
                    }                    
                    break;
                default:
                    throw new NotImplementedException();
            }

            var sendToLower = new List<GO>();
            var sendToUpper = new List<GO>();
            foreach(var o in objects) {
                switch (splitAxis) {
                    case Axis.X:
                        if (nAda.objectpos(o).X < midpoint) {
                            sendToLower.Add(o);                            
                        } else {
                            sendToUpper.Add(o);
                        }
                        break;
                    case Axis.Y:
                        if (nAda.objectpos(o).X < midpoint) {
                            sendToLower.Add(o);                            
                        } else {
                            sendToUpper.Add(o);
                        }
                        break;
                    case Axis.Z:
                        if (nAda.objectpos(o).X < midpoint) {
                            sendToLower.Add(o);                            
                        } else {
                            sendToUpper.Add(o);
                        }
                        break;                      
                }
            }
            lower.addObjects(nAda,sendToLower);
            higher.addObjects(nAda,sendToUpper);            
        }

        internal ssBVHNode<GO> rootNode() {
            ssBVHNode<GO> cur = this;
            while (cur.parent != null) { cur = cur.parent; }
            return cur;
        }

       
        internal void findOverlappingLeaves(SSBVHNodeAdaptor<GO> nAda, Vector3 origin, float radius, List<ssBVHNode<GO>> overlapList) {
            if (toAABB().intersectsSphere(origin,radius)) {
                if (gobjects != null) {    
                    overlapList.Add(this);
                } else {
                    left.findOverlappingLeaves(nAda,origin,radius,overlapList);
                    right.findOverlappingLeaves(nAda,origin,radius,overlapList);
                }
            }
        }

        internal void findOverlappingLeaves(SSBVHNodeAdaptor<GO> nAda, SSAABB aabb, List<ssBVHNode<GO>> overlapList) {
            if (toAABB().intersectsAABB(aabb)) {
                if (gobjects != null) {    
                    overlapList.Add(this);
                } else {
                    left.findOverlappingLeaves(nAda,aabb,overlapList);
                    right.findOverlappingLeaves(nAda,aabb,overlapList);
                }
            }
        }
        
        internal SSAABB toAABB() {
            SSAABB aabb = new SSAABB();
            aabb.min.X = minX;
            aabb.min.Y = minY;
            aabb.min.Z = minZ;
            aabb.max.X = maxX;
            aabb.max.Y = maxY;
            aabb.max.Z = maxZ;
            return aabb;
        }

        internal void childExpanded(SSBVHNodeAdaptor<GO> nAda, ssBVHNode<GO> child) {
            bool expanded = false;
             
            if (child.minX < minX) {
                minX = child.minX; expanded = true;                
            }
            if (child.maxX > maxX) {
                maxX = child.maxX; expanded = true;                
            }
            if (child.minY < minY) {
                minY = child.minY; expanded = true;                 
            }
            if (child.maxY > maxY) {
                maxY = child.maxY; expanded = true;
            }             
            if (child.minZ < minZ) {            
                minZ = child.minZ; expanded = true;
            }
            if (child.maxZ > maxZ) {
                maxZ = child.maxZ; expanded = true;
            }

            if (expanded && parent != null) {
                parent.childExpanded(nAda, this);
            }
        }  

        internal void childRefit(SSBVHNodeAdaptor<GO> nAda) {
            minX = left.minX; maxX = left.maxX;
            minY = left.minY; maxY = left.maxY;
            minZ = left.minZ; maxZ = left.maxZ;

            ssBVHNode<GO> child = right;
            if (child.minX < minX) { minX = child.minX; }
            if (child.maxX > maxX) { maxX = child.maxX; }
            if (child.minY < minY) { minY = child.minY; }
            if (child.maxY > maxY) { maxY = child.maxY; }            
            if (child.minZ < minZ) { minZ = child.minZ; }
            if (child.maxZ > maxZ) { maxZ = child.maxZ; }
            
            if (parent != null) parent.childRefit(nAda);
        }


        private void expandVolume(SSBVHNodeAdaptor<GO> nAda, Vector3 objectpos, float radius) {
            bool expanded = false;

            // test min X and max X against the current bounding volume
            if ((objectpos.X - radius) < minX) {
                minX = (objectpos.X - radius); expanded = true;                
            }
            if ((objectpos.X + radius) > maxX) {
                maxX = (objectpos.X + radius); expanded = true;                
            }         
            // test min Y and max Y against the current bounding volume
            if ((objectpos.Y - radius) < minY) {
                minY = (objectpos.Y - radius); expanded = true;                
            }
            if ((objectpos.Y + radius) > maxY) {
                maxY = (objectpos.Y + radius); expanded = true;                
            }           
            // test min Z and max Z against the current bounding volume
            if ( (objectpos.Z - radius) < minZ ) {
                minZ = (objectpos.Z - radius); expanded = true;                
            }
            if ( (objectpos.Z + radius) > maxZ ) {
                maxZ = (objectpos.Z + radius); expanded = true;                
            }

            if (expanded && parent != null) {                
                parent.childExpanded(nAda, this);
            }           
        }
        
        internal ssBVHNode(ssBVH<GO> bvh, List<GO> gobjectlist) : this (bvh,gobjectlist,null,0,gobjectlist.Count-1, Axis.X,0)
         { }
        
        private ssBVHNode(ssBVH<GO> bvh, List<GO> gobjectlist, ssBVHNode<GO> lparent, int start, int end, Axis lastSplitAxis, int curdepth) {   
            SSBVHNodeAdaptor<GO> nAda = bvh.nAda;                 
            int center;
            int loop;
            int span = end - start;            
            int count = span + 1;  // because end is inclusive            
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
                depth = curdepth;
                left = null;
                right = null;
                gobjects = null;

                return;
            }
 
            // Check if we’re at our LEAF node, and if so, save the objects and stop recursing.  Also store the min/max for the leaf node and update the parent appropriately
            if (count <= bvh.LEAF_OBJ_MAX)
            {
                // once we reach the leaf node, we must set prev/next to null to signify the end
                left = null;
                right = null;
                // at the leaf node we store the remaining objects, so initialize a list
                gobjects = new List<GO>();
                
                // We need to find the aggregate min/max for all 5 remaining objects
                // Start by recording the min max of the first object to have a starting point, then we’ll loop through the remaining
                assignVolume( nAda.objectpos(gobjectlist[start]), nAda.radius(gobjectlist[start]));                                                    
                // loop through all the objects to add them to our leaf node, and calculate the min/max values as we go 
                for (loop = start; loop <= end; loop++)
                {
                    Vector3 objectpos = nAda.objectpos(gobjectlist[loop]);
                    float radius = nAda.radius(gobjectlist[loop]);

                    expandVolume(nAda, objectpos,radius);

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
            //  .. while we do this, we compute our volume over those objects
            List<GO> newgolist = new List<GO>();
            assignVolume( nAda.objectpos(gobjectlist[start]) , nAda.radius(gobjectlist[start]) );            
            for (loop = start; loop <= end; loop++) {
                newgolist.Add(gobjectlist[loop]);
                expandVolume(nAda, nAda.objectpos(gobjectlist[loop]) , nAda.radius(gobjectlist[loop]) );
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
            center = (int) (span * 0.5f); // Find the center object in our current sub-list

            gobjects = null;
            depth = curdepth;
            // if we’re here then we’re still *NOT* in a leaf node.  therefore we need to split prev/next and keep branching until we reach the leaf node            
            left = new ssBVHNode<GO>(bvh, newgolist, this, 0, center, splitAxis, curdepth+1); // Split the Hierarchy to the left
            right = new ssBVHNode<GO>(bvh, newgolist, this, center + 1, span, splitAxis,curdepth+1); // Split the Hierarchy to the right                      
        }

    }
}
