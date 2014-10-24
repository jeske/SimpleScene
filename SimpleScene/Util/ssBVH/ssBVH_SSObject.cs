// Copyright(C) David W. Jeske, 2013
// Released to the public domain.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Drawing;

using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene.Util.ssBVH
{

    /// <summary>
    /// An adaptor for ssBVH to understand SSObject nodes.
    /// </summary>
    public class SSObjectNodeAdaptor : SSBVHNodeAdaptor<SSObject> {
        Dictionary<SSObject, ssBVHNode<SSObject>> ssToLeafMap = new Dictionary<SSObject, ssBVHNode<SSObject>>();

        public Vector3 objectpos(SSObject obj) {
            return obj.Pos;
        }
        public float radius(SSObject obj) {
            if (obj.boundingSphere != null) {
                return obj.boundingSphere.radius;
            } else {
                return 1.0f;
            }
        }
        public void mapObjectToBVHLeaf(SSObject obj, ssBVHNode<SSObject> leaf) {
            // TODO: implement a way for object movement to update object position.
            obj.OnChanged += obj_OnChanged;

            ssToLeafMap[obj] = leaf;
        }

        void obj_OnChanged(SSObject sender)
        {
            ssToLeafMap[sender].refit_ObjectChanged(this, sender);            
        }

        public SSObjectNodeAdaptor() {}
    }




    /// <summary>
    /// This is a 3d render representation for an ssBVH tree.
    /// </summary>
    public class SSBVHRender : SSObject {
        ssBVH<SSObject> bvh;
        public HashSet<ssBVHNode<SSObject>> highlightNodes = new HashSet<ssBVHNode<SSObject>>();

        public SSBVHRender(ssBVH<SSObject> bvh) {
            this.bvh = bvh;
        }

         private void drawQuadEdges(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) {            
            GL.Vertex3(p0); GL.Vertex3(p1);
            GL.Vertex3(p1); GL.Vertex3(p2);
            GL.Vertex3(p2); GL.Vertex3(p3);
            GL.Vertex3(p3); GL.Vertex3(p0);
        }

        public void renderCells(ssBVHNode<SSObject> n, int depth=0) {  
            if (highlightNodes.Contains(n)) {
                if (n.gobjects == null) {
                    GL.Color4(Color.FromArgb(255,25,25,100));
                } else {
                    GL.Color4(Color.Green); 
                }                
            } else {
                if (n.gobjects == null) {
                    GL.Color4(Color.FromArgb(255,20,20,20));
                } else {
                    GL.Color4(Color.DarkRed);            
                }
            }
                                                 
           	var p0 = new Vector3 (n.minX,  n.minY,  n.maxZ);  
			var p1 = new Vector3 (n.maxX,  n.minY,  n.maxZ);
			var p2 = new Vector3 (n.maxX,  n.maxY,  n.maxZ);  
			var p3 = new Vector3 (n.minX,  n.maxY,  n.maxZ);
			var p4 = new Vector3 (n.minX,  n.minY,  n.minZ);
			var p5 = new Vector3 (n.maxX,  n.minY,  n.minZ);
			var p6 = new Vector3 (n.maxX,  n.maxY,  n.minZ);
			var p7 = new Vector3 (n.minX,  n.maxY,  n.minZ);

            drawQuadEdges(p0, p1, p2, p3);            
            drawQuadEdges(p7, p6, p5, p4);
            drawQuadEdges(p1, p0, p4, p5);
            drawQuadEdges(p2, p1, p5, p6);
            drawQuadEdges(p3, p2, p6, p7);
            drawQuadEdges(p0, p3, p7, p4);

            if (n.right != null) renderCells(n.right, depth:depth + 1);
            if (n.left != null) renderCells(n.left, depth:depth + 1);
        }

        public override void Render(ref SSRenderConfig renderConfig) {
			base.Render(ref renderConfig);
            GL.UseProgram(0);
            GL.Color4(Color.Red);          
			GL.Disable(EnableCap.Texture2D);
			GL.Disable(EnableCap.Lighting);	
            GL.LineWidth(1.0f);
   			
            GL.Begin(BeginMode.Lines);
            GL.Color4(Color.Red);          
            this.renderCells(bvh.rootBVH);
            GL.End();
        }
    }

}
