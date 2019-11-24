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
		public SSObjectMesh () { 
            this.renderState.castsShadow = true;    // SSObjectMesh casts shadow by default
            this.renderState.receivesShadows = true; // SSObjectMesh receives shadow by default
        }        
        public SSObjectMesh (SSAbstractMesh mesh) : this() {
            this.Mesh = mesh;        
            this.renderState.castsShadow = true;    // SSObjectMesh casts shadow by default
            this.renderState.receivesShadows = true; // SSObjectMesh receives shadow by default
			_setupMesh ();
        }
		
        private SSAbstractMesh _mesh;
        public SSAbstractMesh Mesh {
          get { return _mesh; }
          set { _mesh = value; _setupMesh(); }
        }

		public override bool alphaBlendingEnabled {
			get {
				return base.alphaBlendingEnabled || _mesh.alphaBlendingEnabled;
			}
			set {
				base.alphaBlendingEnabled = value;
			}
		}

		public override Vector3 localBoundingSphereCenter {
			get {
				return _mesh.boundingSphereCenter;
			}
		}

		public override float localBoundingSphereRadius {
			get {
				return _mesh.boundingSphereRadius;
			}
		}

        public bool enableLineStipple = false;
        public ushort lineStipplePattern = 0xF0;
        
        public override void Render (SSRenderConfig renderConfig)
		{
			if (_mesh != null) {
				base.Render (renderConfig);
                if (enableLineStipple) {
                    GL.Enable(EnableCap.LineStipple);
                    GL.LineStipple(1, lineStipplePattern);
                }
				this._mesh.renderMesh (renderConfig);
            }
        }

        private void _setupMesh() {
            if (_mesh != null) {
                // compute and setup bounding sphere

				// TODO: make a more detailed collision mesh

				// notify listeners..
				NotifyPositionOrSizeChanged(); 
			} 
        }

		public override bool PreciseIntersect (ref SSRay worldSpaceRay, ref float distanceAlongWorldRay)
		{
            SSRay localRay = worldSpaceRay.Transformed (this.worldMat.Inverted ());
            if (this.Mesh != null) {
                float localNearestContact;
                bool ret = this.Mesh.preciseIntersect(ref localRay, out localNearestContact);
                if (ret) {
                    Vector3 localContactPt = localRay.pos + localNearestContact * localRay.dir;
                    // some_name code start 24112019
                    //Vector3 worldContactPt = Vector3.Transform(localContactPt, this.worldMat);
                    Vector3 worldContactPt = (new Vector4(localContactPt.X, localContactPt.Y, localContactPt.Z, 1) * this.worldMat).Xyz;
                    // some_name code end
                    distanceAlongWorldRay = (worldContactPt - worldSpaceRay.pos).Length;
                    //Console.WriteLine ("Nearest Triangle Hit @ {0} vs Sphere {1} : Object {2}", worldSpaceContactDistance, distanceAlongRay, Name);
                } else {
                    distanceAlongWorldRay = float.PositiveInfinity;
                }
                return ret;
            } else {
                distanceAlongWorldRay = float.PositiveInfinity;
                return false;
            }
		}

		public override void Update(float elapsedSecs) 
		{
            if (_mesh == null) return;

			if (_mesh.updateSource.Target == null) {
				_mesh.updateSource.Target = this;
			}
            if (_mesh.updateSource.Target == this) {
                _mesh.update (elapsedSecs);
			}
		}

		private void MeshPositionOrSizeChanged() {
			NotifyPositionOrSizeChanged ();
		}
    }
}

