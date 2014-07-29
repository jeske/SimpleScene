// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SimpleScene
{
    public class SSObjectMesh : SSObject
    {
        private SSAbstractMesh _mesh;
		public SSAbstractMesh Mesh {
          get { return _mesh; }
          set { _mesh = value; _setupMesh(); }
        }
        
        public override void Render (ref SSRenderConfig renderConfig)
		{
			if (_mesh != null) {
				base.Render (ref renderConfig);
				this._mesh.RenderMesh (ref renderConfig);

				if (renderConfig.renderBoundingSpheres && this.boundingSphere != null) {
                    this.boundingSphere.Render(ref renderConfig);

				}
				if (renderConfig.renderCollisionShells && this.collisionShell != null) {
                    this.collisionShell.Pos = this.Pos;
                    this.collisionShell.Scale = this.Scale;
                    this.collisionShell.Render(ref renderConfig);
				}
            }
        }

        private void _setupMesh() {
            if (_mesh != null) {
                // compute and setup bounding sphere
                float radius = 0f;
                foreach(var point in _mesh.EnumeratePoints()) {
	                radius = Math.Max(radius,point.Length);
                }
				this.boundingSphere = new SSObjectSphere(radius);
				this.Changed += (sender) => { 
					this.boundingSphere.Pos = this.Pos;
					this.boundingSphere.Scale = this.Scale;
				};
				// Console.WriteLine("constructed collision shell of radius {0}",radius);

				// TODO: make a more detailed collision mesh

				// notify listeners..
				ObjectChanged(); 
			} 
        }


        public SSObjectMesh (SSAbstractMesh mesh) : base() {
            this.Mesh = mesh;
        }
		public SSObjectMesh () {
		}
    }
}

