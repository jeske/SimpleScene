// Copyright(C) David W. Jeske, 2014, and released to the public domain. 
//
// Dynamic BVH (Bounding Volume Hierarchy) using incremental refit and tree-rotations
//
// initial BVH build based on: Bounding Volume Hierarchies (BVH) – A brief tutorial on what they are and how to implement them
//              http://www.3dmuve.com/3dmblog/?p=182
//
// Dynamic Updates based on: "Fast, Effective BVH Updates for Animated Scenes" (Kopta, Ize, Spjut, Brunvand, David, Kensler)
//              http://www.cs.utah.edu/~thiago/papers/rotations.pdf
//
// see also:  Space Partitioning: Octree vs. BVH
//            http://thomasdiewald.com/blog/?p=1488
//
//

// TODO: implement node merge/split, to handle updates when LEAF_OBJ_MAX > 1
// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK;

namespace SimpleScene.Util.ssBVH
{
    public class ssBVHNode<GO> {
        public SSAABB box;

        public ssBVHNode<GO> parent;
        public Axis splitAxis;
        public float splitOverlap = 0.0f;
        public ssBVHNode<GO> left;
        public ssBVHNode<GO> right;

        public int depth;
                 
        public List<GO> gobjects;  // only populated in leaf nodes

        private Axis pickSplitAxis(Axis cur) {            
            float axis_x = box.max.X - box.min.X; 
            float axis_y = box.max.Y - box.min.Y;
            float axis_z = box.max.Z - box.min.Z;

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
        public bool IsLeaf {
            get {            
                bool isLeaf = (this.gobjects != null);
                // if we're a leaf, then both left and right should be null..
                if (isLeaf &&  ( (right != null) || (left != null) ) ) {
                        throw new Exception("ssBVH Leaf has objects and left/right pointers!");
                }
                return isLeaf;

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
            if ( recomputeVolume(nAda) ) {
                // add our parent to the optimize list...
                if (parent != null) {                    
                    nAda.BVH.refitNodes.Add(parent); 

                    // you can force an optimize every time something moves, but it's not very efficient
                    // instead we do this per-frame after a bunch of updates.
                    // nAda.BVH.optimize();                    
                }
             }
        }

        private void assignVolume(Vector3 objectpos, float radius) {
            box.min.X = objectpos.X - radius;
            box.max.X = objectpos.X + radius;
            box.min.Y = objectpos.Y - radius;
            box.max.Y = objectpos.Y + radius;
            box.min.Z = objectpos.Z - radius;
            box.max.Z = objectpos.Z + radius;
        }      
        
        internal bool recomputeVolume(SSBVHNodeAdaptor<GO> nAda) {
            if (gobjects.Count == 0) { throw new NotImplementedException(); }  // TODO: fix this... we should never get called in this case...

            SSAABB oldbox = box;

            assignVolume( nAda.objectpos(gobjects[0]), nAda.radius(gobjects[0]));
            for(int i=1; i<gobjects.Count;i++) {
                expandVolume(nAda, nAda.objectpos(gobjects[i]) , nAda.radius(gobjects[i]) );
            }
            if (!box.Equals(oldbox)) {
                if (parent != null) parent.childRefit(nAda);
                return true;
            } else {
                return false;
            }
        }
        
        internal float SAH(SSAABB box) {
            float x_size = box.max.X - box.min.X;
            float y_size = box.max.Y - box.min.Y;
            float z_size = box.max.Z - box.min.Z;

            return 2.0f * ( (x_size * y_size) + (x_size * z_size) + (y_size * z_size) );
            
        }
        internal float SAH(ref SSAABB box) {
            float x_size = box.max.X - box.min.X;
            float y_size = box.max.Y - box.min.Y;
            float z_size = box.max.Z - box.min.Z;

            return 2.0f * ( (x_size * y_size) + (x_size * z_size) + (y_size * z_size) );
            
        }
        internal float SAH(ssBVHNode<GO> node) {            
            float x_size = node.box.max.X - node.box.min.X;
            float y_size = node.box.max.Y - node.box.min.Y;
            float z_size = node.box.max.Z - node.box.min.Z;

            return 2.0f * ( (x_size * y_size) + (x_size * z_size) + (y_size * z_size) );
        }
        internal float SAH(SSBVHNodeAdaptor<GO> nAda, GO obj) {            
            float radius = nAda.radius(obj);
            return (float)(4.0 * Math.PI * radius * radius);  // bounding sphere surface area
        }

        

        internal SSAABB AABBofPair(ssBVHNode<GO> nodea, ssBVHNode<GO> nodeb) {
            SSAABB box = nodea.box;
            box.expandToFit(nodeb.box);
            return box;
        }

        internal float SAHofPair(ssBVHNode<GO> nodea, ssBVHNode<GO> nodeb) {
            SSAABB box = nodea.box;
            box.expandToFit(nodeb.box);
            return SAH(ref box);
        }
        internal float SAHofPair(SSAABB boxa, SSAABB boxb) {
            SSAABB pairbox = boxa;
            pairbox.expandToFit(boxb);
            return SAH(ref pairbox);
        }

        // The list of all candidate rotations, from "Fast, Effective BVH Updates for Animated Scenes", Figure 1.
        internal enum Rot {
            NONE, L_RL, L_RR, R_LL, R_LR, LL_RR, LL_RL,
        }
        
        internal class rotOpt : IComparable<rotOpt> {  // rotation option
            public float SAH;
            public Rot rot;
            internal rotOpt(float SAH, Rot rot) {
                this.SAH = SAH;
                this.rot = rot;
            }
            public int CompareTo(rotOpt other) {
                return SAH.CompareTo(other.SAH);
            }
        }        

        internal List<Rot>_rots = new List<Rot> ((Rot[])Enum.GetValues(typeof(Rot)));
        internal List<Rot> eachRot {
            get {
                return _rots;
            }
        }
        
        /// <summary>
        /// tryRotate looks at all candidate rotations, and executes the rotation with the best resulting SAH (if any)
        /// </summary>
        /// <param name="bvh"></param>
        internal void tryRotate(ssBVH<GO> bvh) {    
            SSBVHNodeAdaptor<GO> nAda = bvh.nAda;                                
                                           
            // if we are not a grandparent, then we can't rotate, so queue our parent and bail out
            if (left.IsLeaf && right.IsLeaf) {
                if (parent != null) {
                    bvh.refitNodes.Add(parent);
                    return;
                }
            }

            // for each rotation, check that there are grandchildren as necessary (aka not a leaf)
            // then compute total SAH cost of our branches after the rotation.

            float mySAH = SAH(left) + SAH(right);

            rotOpt bestRot = eachRot.Min( (rot) => { 
                switch (rot) {
                 case Rot.NONE: return new rotOpt(mySAH,Rot.NONE);
                 // child to grandchild rotations
                 case Rot.L_RL: 
                    if (right.IsLeaf) return new rotOpt(float.MaxValue,Rot.NONE);
                    else return new rotOpt(SAH(right.left) + SAH(AABBofPair(left,right.right)), rot);   
                 case Rot.L_RR: 
                    if (right.IsLeaf) return new rotOpt(float.MaxValue,Rot.NONE);
                    else return new rotOpt(SAH(right.right) + SAH(AABBofPair(left,right.left)), rot);
                 case Rot.R_LL: 
                    if (left.IsLeaf) return new rotOpt(float.MaxValue,Rot.NONE);
                    else return new rotOpt(SAH(AABBofPair(right,left.right)) + SAH(left.left), rot);
                 case Rot.R_LR: 
                    if (left.IsLeaf) return new rotOpt(float.MaxValue,Rot.NONE);
                    else return new rotOpt(SAH(AABBofPair(right,left.left)) + SAH(left.right), rot); 
                 // grandchild to grandchild rotations
                 case Rot.LL_RR: 
                    if (left.IsLeaf || right.IsLeaf) return new rotOpt(float.MaxValue,Rot.NONE);
                    else return new rotOpt(SAH(AABBofPair(right.right,left.right)) + SAH(AABBofPair(right.left,left.left)), rot);
                 case Rot.LL_RL:
                    if (left.IsLeaf || right.IsLeaf) return new rotOpt(float.MaxValue,Rot.NONE);
                    else return new rotOpt(SAH(AABBofPair(right.left,left.right)) + SAH(AABBofPair(left.left,right.right)), rot);
                 // unknown...
                 default: throw new NotImplementedException("missing implementation for BVH Rotation SAH Computation .. " + rot.ToString());                                     
                }
            });                                
                   
            // perform the best rotation...            
            if (bestRot.rot != Rot.NONE) {
                // if the best rotation is no-rotation... we check our parents anyhow..                
                if (parent != null) { 
                    // but only do it some random percentage of the time.
                    if ((DateTime.Now.Ticks % 100) < 2) {
                        bvh.refitNodes.Add(parent); 
                    }
                }                
            } else {

                if (parent != null) { bvh.refitNodes.Add(parent); }

                if ( ((mySAH - bestRot.SAH) / mySAH ) < 0.3f) {
                    return; // the benefit is not worth the cost
                }
                Console.WriteLine("BVH swap {0} from {1} to {2}", bestRot.rot.ToString(), mySAH, bestRot.SAH);

                // in order to swap we need to:
                //  1. swap the node locations
                //  2. update the depth (if child-to-grandchild)
                //  3. update the parent pointers
                //  4. refit the boundary box
                ssBVHNode<GO> swap = null;
                switch (bestRot.rot) {
                    case Rot.NONE: break;
                    // child to grandchild rotations
                    case Rot.L_RL: swap = left;  swap.depth++; left  = right.left;  left.parent = this; left.depth--;  right.left  = swap;  swap.parent = right; right.childRefit(nAda); break;
                    case Rot.L_RR: swap = left;  swap.depth++; left  = right.right; left.parent = this; left.depth--;  right.right = swap;  swap.parent = right; right.childRefit(nAda); break;
                    case Rot.R_LL: swap = right; swap.depth++; right =  left.left;  right.parent = this; right.depth--; left.left  = swap;  swap.parent = left;   left.childRefit(nAda); break;
                    case Rot.R_LR: swap = right; swap.depth++; right =  left.right; right.parent = this; right.depth--; left.right = swap;  swap.parent = left;   left.childRefit(nAda); break;
                    
                    // grandchild to grandchild rotations
                    case Rot.LL_RR: swap = left.left; left.left = right.right; right.right = swap; left.left.parent = left; swap.parent = right; left.childRefit(nAda,recurse:false); right.childRefit(nAda); break;
                    case Rot.LL_RL: swap = left.left; left.left = right.left;  right.left  = swap; left.left.parent = left; swap.parent = right; left.childRefit(nAda,recurse:false); right.childRefit(nAda); break;
                    
                    // unknown...
                    default: throw new NotImplementedException("missing implementation for BVH Rotation .. " + bestRot.rot.ToString());                                     
                }
                
            }

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
                    if (left.box.min.X < right.box.min.X) {
                        midpoint = (left.box.max.X + right.box.min.X) / 2.0f;
                    } else { 
                        midpoint = (right.box.max.X + left.box.min.X) / 2.0f;
                        lower = right; higher = left;
                    }                    
                    break;
                case Axis.Y:
                    if (left.box.min.Y < right.box.min.Y) {
                        midpoint = (left.box.max.Y + right.box.min.Y) / 2.0f;
                    } else { 
                        midpoint = (right.box.max.Y + left.box.min.Y) / 2.0f;
                        lower = right; higher = left;
                    }                    
                    break;
                case Axis.Z:
                    if (left.box.min.Z < right.box.min.Z) {
                        midpoint = (left.box.max.Z + right.box.min.Z) / 2.0f;
                    } else { 
                        midpoint = (right.box.max.Z + left.box.min.Z) / 2.0f;
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
            aabb.min.X = box.min.X;
            aabb.min.Y = box.min.Y;
            aabb.min.Z = box.min.Z;
            aabb.max.X = box.max.X;
            aabb.max.Y = box.max.Y;
            aabb.max.Z = box.max.Z;
            return aabb;
        }

        internal void childExpanded(SSBVHNodeAdaptor<GO> nAda, ssBVHNode<GO> child) {
            bool expanded = false;
             
            if (child.box.min.X < box.min.X) {
                box.min.X = child.box.min.X; expanded = true;                
            }
            if (child.box.max.X > box.max.X) {
                box.max.X = child.box.max.X; expanded = true;                
            }
            if (child.box.min.Y < box.min.Y) {
                box.min.Y = child.box.min.Y; expanded = true;                 
            }
            if (child.box.max.Y > box.max.Y) {
                box.max.Y = child.box.max.Y; expanded = true;
            }             
            if (child.box.min.Z < box.min.Z) {            
                box.min.Z = child.box.min.Z; expanded = true;
            }
            if (child.box.max.Z > box.max.Z) {
                box.max.Z = child.box.max.Z; expanded = true;
            }

            if (expanded && parent != null) {
                parent.childExpanded(nAda, this);
            }
        }  

        internal void childRefit(SSBVHNodeAdaptor<GO> nAda, bool recurse=true) {
            SSAABB oldbox = box;           
            box.min.X = left.box.min.X; box.max.X = left.box.max.X;
            box.min.Y = left.box.min.Y; box.max.Y = left.box.max.Y;
            box.min.Z = left.box.min.Z; box.max.Z = left.box.max.Z;

            if (right.box.min.X < box.min.X) { box.min.X = right.box.min.X; }            
            if (right.box.min.Y < box.min.Y) { box.min.Y = right.box.min.Y; }            
            if (right.box.min.Z < box.min.Z) { box.min.Z = right.box.min.Z; }

            if (right.box.max.X > box.max.X) { box.max.X = right.box.max.X; }
            if (right.box.max.Y > box.max.Y) { box.max.Y = right.box.max.Y; }            
            if (right.box.max.Z > box.max.Z) { box.max.Z = right.box.max.Z; }
            
            if (recurse && parent != null) { parent.childRefit(nAda); }
        }


        private void expandVolume(SSBVHNodeAdaptor<GO> nAda, Vector3 objectpos, float radius) {
            bool expanded = false;

            // test min X and max X against the current bounding volume
            if ((objectpos.X - radius) < box.min.X) {
                box.min.X = (objectpos.X - radius); expanded = true;                
            }
            if ((objectpos.X + radius) > box.max.X) {
                box.max.X = (objectpos.X + radius); expanded = true;                
            }         
            // test min Y and max Y against the current bounding volume
            if ((objectpos.Y - radius) < box.min.Y) {
                box.min.Y = (objectpos.Y - radius); expanded = true;                
            }
            if ((objectpos.Y + radius) > box.max.Y) {
                box.max.Y = (objectpos.Y + radius); expanded = true;                
            }           
            // test min Z and max Z against the current bounding volume
            if ( (objectpos.Z - radius) < box.min.Z ) {
                box.min.Z = (objectpos.Z - radius); expanded = true;                
            }
            if ( (objectpos.Z + radius) > box.max.Z ) {
                box.max.Z = (objectpos.Z + radius); expanded = true;                
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
            if (gobjectlist == null || end < start) {
                throw new Exception("ssBVHNode constructed with invalid paramaters");
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
