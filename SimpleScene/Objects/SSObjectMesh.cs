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
        
        public override void Render (ref SSRenderConfig renderConfig)
		{
			if (_mesh != null) {
				base.Render (ref renderConfig);
				this._mesh.RenderMesh (ref renderConfig);
            }
        }

        private void _setupMesh() {
            if (_mesh != null) {
                // compute and setup bounding sphere

				// TODO: make a more detailed collision mesh

				// notify listeners..
				ObjectChanged(); 
			} 
        }

		public override bool PreciseIntersect (ref SSRay worldSpaceRay, ref float distanceAlongRay)
		{
			SSRay localRay = worldSpaceRay.Transformed (this.worldMat.Inverted ());
			SSAbstractMesh mesh = this._mesh;
			bool hit = false;			  
			float localNearestContact = float.MaxValue;
			if (mesh == null) {
				return true; // no mesh to test
			} else {
				// precise meshIntersect
				bool global_hit = mesh.TraverseTriangles ((state, V1, V2, V3) => {
					float contact;
					if (OpenTKHelper.TriangleRayIntersectionTest (V1, V2, V3, localRay.pos, localRay.dir, out contact)) {
						hit = true;
						localNearestContact = Math.Min (localNearestContact, contact);
						Console.WriteLine ("Triangle Hit @ {0} : Object {1}", contact, Name);
					}
					return false; // don't short circuit
				});
				if (hit) {
					float worldSpaceContactDistance = -localNearestContact * this.Scale.LengthFast;
					Console.WriteLine ("Nearest Triangle Hit @ {0} vs Sphere {1} : Object {2}", worldSpaceContactDistance, distanceAlongRay, Name);
					distanceAlongRay = worldSpaceContactDistance;
				}
				return global_hit || hit;
			}			     
		}

		public override void Update(float elapsedS) {
			Mesh.Update (elapsedS);
		}
    }
}

